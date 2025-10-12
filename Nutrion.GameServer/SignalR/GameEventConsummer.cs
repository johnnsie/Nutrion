using Microsoft.AspNetCore.SignalR;
using Nutrion.Data;
using Nutrion.GameServer.Messages;
using Nutrion.Messaging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nutrion.GameServer.SignalR;

public class GameEventConsumer : BackgroundService
{
    private readonly IMessageConsumer _consumer;  // your RabbitMQ consumer abstraction
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameEventConsumer> _logger = default!;

    public GameEventConsumer(IMessageConsumer consumer, IHubContext<GameHub> hub, ILogger<GameEventConsumer> logger)
    {
        _consumer = consumer;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("ExecuteAsync started");
            await _consumer.StartConsumingAsync("game.events.tile.claimed", HandleMessageAsync, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExecuteAsync crashed");
            throw;
        }
    }

    private async Task HandleMessageAsync(string message, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing message: {Message}", message);

            var evt = JsonSerializer.Deserialize<TileClaimedEvent>(message);
            if (evt != null)
            {
                Console.WriteLine($"📩 TileClaimed({evt.Q},{evt.R}) by {evt.UserId}");
                await _hub.Clients.All.SendAsync("TileClaimed", evt, stoppingToken);
            }

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
