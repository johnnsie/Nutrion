using global::Nutrion.Data;
using global::Nutrion.Messaging;
using Nutrion.GameServer.Messages;
using Nutrion.GameWorker.Services;
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

    public TileWorker(
        ILogger<TileWorker> logger,
        IMessageProducer producerService,
        IMessageConsumer consumerService,
        TileStateService tileState
        )
    {
        _logger = logger;
        _producer = producerService;
        _consumer = consumerService;
        _tileState = tileState;

        _logger.LogInformation("Worker constructed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("ExecuteAsync started");
            await _consumer.StartConsumingAsync("game.commands.tile.claim", HandleMessageAsync, stoppingToken);
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

    private async Task HandleMessageAsync(string message, CancellationToken stoppingToken)
    {
        try
        {
            var cmd = JsonSerializer.Deserialize<TileClaimCommand>(message);
            if (cmd == null) return;

            _logger.LogInformation("Received ClaimTile ({Q},{R}) from {User}", cmd.Q, cmd.R, cmd.UserId);

            var changed = await _tileState.ClaimTileAsync(cmd.UserId, cmd.Color, cmd.Q, cmd.R, stoppingToken);
            if (!changed) return;

            var evt = new TileClaimedEvent
            {
                UserId = cmd.UserId,
                Color = cmd.Color,
                Q = cmd.Q,
                R = cmd.R,
                Timestamp = DateTimeOffset.UtcNow
            };

            // ✅ Publish event to "game.events.tile.claimed"
            await _producer.PublishAsync("game.events.tile.claimed", evt, cancellationToken: stoppingToken);
            _logger.LogInformation("Published TileClaimed ({Q},{R}) by {User}", cmd.Q, cmd.R, cmd.UserId);

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
