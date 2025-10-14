using Microsoft.AspNetCore.SignalR;
using Nutrion.Contracts;
using Nutrion.Data;
using Nutrion.Messaging;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nutrion.GameServer.SignalR.OLD;

public class GameEventConsumer : BackgroundService
{
    private readonly IMessageConsumer _consumer;  // your RabbitMQ consumer abstraction
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameEventConsumer> _logger = default!;
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _handlers;

    public GameEventConsumer(IMessageConsumer consumer, IHubContext<GameHub> hub, ILogger<GameEventConsumer> logger)
    {
        _consumer = consumer;
        _hub = hub;
        _logger = logger;

        _handlers = new()
        {
            ["game.events.tile.claimed"] = async (json, ct) =>
            {
                var evt = JsonSerializer.Deserialize<Tile>(json);
                await HandleTileClaimedAsync(evt!, ct);
            },
            ["game.events.player.joined"] = async (json, ct) =>
            {
                var evt = JsonSerializer.Deserialize<Player>(json);
                await HandlePlayerJoinedAsync(evt!, ct);
            }
        };
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }

    private async Task HandlePlayerJoinedAsync(Player evt, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing message: {Message}", evt);
            if (evt != null)
            {
                Console.WriteLine($"📩 PlayerJoined");
                await _hub.Clients.All.SendAsync("UserJoined", new { evt.Id, evt.Color });
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
    private async Task HandleTileClaimedAsync(Tile message, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing message: {Message}", message);
            if (message != null)
            {
                Console.WriteLine($"📩 TileClaimed({message.q},{message.r}) by {message.color}");
                await _hub.Clients.All.SendAsync("TileClaimed", message, stoppingToken);
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
