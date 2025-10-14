using global::Nutrion.Data;
using global::Nutrion.Messaging;
using Nutrion.GameWorker.Services;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.Database.Game.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Nutrion.GameServer.Worker;

public class TileWorker : BackgroundService
{
    private readonly ILogger<TileWorker> _logger;
    private readonly IMessageProducer _producer;
    private readonly IMessageConsumer _consumer;
    private readonly TileStateService _tileState;
    private readonly IServiceScopeFactory _scopeFactory;

    public TileWorker(
        ILogger<TileWorker> logger,
        IMessageProducer producerService,
        IMessageConsumer consumerService,
        TileStateService tileState,
        IServiceScopeFactory scopeFactory
        )
    {
        _logger = logger;
        _producer = producerService;
        _consumer = consumerService;
        _tileState = tileState;
        _scopeFactory = scopeFactory;

        _logger.LogInformation("Worker constructed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("ExecuteAsync started");
            await _consumer.StartConsumingTopicAsync(
                "game.commands.exchange", 
                "game.commands.tile.claim", 
                HandleMessageAsync, stoppingToken,    
                queueName: "game.commands.tileworker");


            await Task.Delay(Timeout.Infinite, stoppingToken);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExecuteAsync crashed");
            throw;
        }

    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(string routingKey, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        try
        {
            var message = System.Text.Encoding.UTF8.GetString(body.Span);
            var cmd = JsonSerializer.Deserialize<Tile>(message);
            if (cmd == null) return;

            _logger.LogInformation($"Received ClaimTile from {cmd.OwnerId}");

            // Create a scope to access scoped services (DbContext, repository)
            using var scope = _scopeFactory.CreateScope();
            var tileRepo = scope.ServiceProvider.GetRequiredService<IRepository<Tile>>();
            // Can do DB stuff here instead of the service -- For later maybe

            var changed = await _tileState.ClaimTileAsync(cmd.OwnerId, cmd.Color, cmd.Q, cmd.R, ct);
            
            if (!changed) return;

            // ✅ Publish event to "game.events.tile.claimed"
            await _producer.PublishTopicAsync("game.events.exchange","game.events.tile.claimed", cmd, cancellationToken: ct);
            _logger.LogInformation($"Published TileClaimed by {cmd.OwnerId}");

        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Message processing was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
        }
    }

}
