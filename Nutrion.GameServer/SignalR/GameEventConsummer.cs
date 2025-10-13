using Microsoft.AspNetCore.SignalR;
using Nutrion.Contracts;
using Nutrion.Messaging;
using System.Text.Json;

namespace Nutrion.GameServer.SignalR;

public class GameEventConsumer : BackgroundService
{
    private readonly IMessageConsumer _consumer;  // your RabbitMQ consumer abstraction
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameEventConsumer> _logger;
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _handlers;

    public GameEventConsumer(IMessageConsumer consumer, IHubContext<GameHub> hub, ILogger<GameEventConsumer> logger)
    {
        _consumer = consumer;
        _hub = hub;
        _logger = logger;

        // Map routing keys to handler methods
        _handlers = new()
        {
            ["game.events.tile.claimed"] = async (json, ct) =>
            {
                var evt = JsonSerializer.Deserialize<Tile>(json);
                if (evt != null)
                    await HandleTileClaimedAsync(evt, ct);
            },
            ["game.events.player.joined"] = async (json, ct) =>
            {
                var evt = JsonSerializer.Deserialize<Player>(json);
                if (evt != null)
                    await HandlePlayerJoinedAsync(evt, ct);
            }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎧 Starting GameEventConsumer...");

        try
        {
            // Subscribe to all events under game.events.*
            await _consumer.StartConsumingAsync("game.events.#", async (routingKey, body, ct) =>
            {
                await HandleMessageAsync(routingKey, body, ct);
            }, stoppingToken);

            // Keep the background service alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("🛑 GameEventConsumer canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 GameEventConsumer crashed");
        }
    }

    private async Task HandleMessageAsync(string routingKey, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(body.Span);
            _logger.LogInformation("📨 Received {RoutingKey}: {Json}", routingKey, json);

            if (_handlers.TryGetValue(routingKey, out var handler))
            {
                await handler(json, ct);
            }
            else
            {
                _logger.LogWarning("⚠️ No handler for {RoutingKey}", routingKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for {RoutingKey}", routingKey);
        }
    }

    private async Task HandleTileClaimedAsync(Tile message, CancellationToken ct)
    {
        _logger.LogInformation("➡️ Broadcasting TileClaimed({q},{r}) by {color}", message.q, message.r, message.color);
        await _hub.Clients.All.SendAsync("TileClaimed", message, ct);
    }

    private async Task HandlePlayerJoinedAsync(Player evt, CancellationToken ct)
    {
        _logger.LogInformation("➡️ Broadcasting PlayerJoined: {name}", evt.Name);
        await _hub.Clients.All.SendAsync("UserJoined", evt, new { evt.Id, evt.Color });
    }
}
