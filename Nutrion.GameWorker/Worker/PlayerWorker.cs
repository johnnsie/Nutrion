using global::Nutrion.Data;
using global::Nutrion.Messaging;
using Nutrion.GameWorker.Services;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.Database.Game.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Drawing;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;

namespace Nutrion.GameServer.Worker;

public class PlayerWorker : BackgroundService
{
    private readonly ILogger<PlayerWorker> _logger;
    private readonly IMessageProducer _producer;
    private readonly IMessageConsumer _consumer;
    private readonly IServiceScopeFactory _scopeFactory;

    public PlayerWorker(
        ILogger<PlayerWorker> logger,
        IMessageProducer producerService,
        IMessageConsumer consumerService,
        IServiceScopeFactory scopeFactory
        )
    {
        _logger = logger;
        _producer = producerService;
        _consumer = consumerService;
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
                "game.commands.player.join", 
                HandleMessageAsync, stoppingToken,
                queueName: "game.commands.playerworker");

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
            var cmd = JsonSerializer.Deserialize<Player>(message);
            if (cmd == null) return;

            // Create a scope to access scoped services (DbContext, repository)
            using var scope = _scopeFactory.CreateScope();
            var tileRepo = scope.ServiceProvider.GetRequiredService<IRepository<Player>>();

            // Generic save handles both insert/update
            await tileRepo.SaveAsync(
                entity: cmd,
                match: t => t.OwnerId == cmd.OwnerId,
                cancellationToken: ct
            );

            // ✅ Publish event to "game.eventss.tile.claimed"
            await _producer.PublishTopicAsync("game.events.exchange","game.events.player.joined", cmd, cancellationToken: ct);
            _logger.LogInformation("Published game.events.player.join {User}", cmd.Name);
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
