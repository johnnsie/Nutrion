using Microsoft.AspNetCore.SignalR;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Messaging;
using System.Text;
using System.Text.Json;
using Player = Nutrion.Lib.Database.Game.Entities.Player;
using Tile = Nutrion.Lib.Database.Game.Entities.Tile;

namespace Nutrion.GameServer.SignalR;

public class GameEventConsumer : BackgroundService
{
    private readonly IMessageConsumer _consumer;
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameEventConsumer> _logger;

    private const string ExchangeName = "game.events.exchange";

    public GameEventConsumer(
        IMessageConsumer consumer,
        IHubContext<GameHub> hub,
        ILogger<GameEventConsumer> logger)
    {
        _consumer = consumer;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎧 Starting GameEventConsumer...");

        try
        {
            // 🧩 Subscribe to all topic patterns under the exchange
            await _consumer.StartConsumingTopicAsync(
                exchangeName: ExchangeName,
                topicPattern: "game.events.#",
                onMessage: async (routingKey, body, ct) =>
                {
                    await HandleMessageAsync(routingKey, body, ct);
                },
                cancellationToken: stoppingToken,
                queueName: "game.events.ui");


            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("🛑 GameEventConsumer canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 GameEventConsumer crashed");
        }
    }

    private async Task HandleMessageAsync(string routingKey, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        var json = Encoding.UTF8.GetString(body.Span);
        _logger.LogInformation("📨 Received {RoutingKey}: {Json}", routingKey, json);

        try
        {
            switch (routingKey)
            {
                case "game.events.tile.claimed":
                    await HandleTileClaimedAsync(json, ct);
                    break;

                case "game.events.player.joined":
                    await HandlePlayerJoinedAsync(json, ct);
                    break;

                default:
                    _logger.LogWarning("⚠️ No handler for routing key {RoutingKey}", routingKey);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing message {RoutingKey}", routingKey);
        }
    }

    private async Task HandleTileClaimedAsync(string json, CancellationToken ct)
    {
        var tile = JsonSerializer.Deserialize<Tile>(json, _jsonOpts);
        if (tile == null)
        {
            _logger.LogWarning("⚠️ Invalid tile event JSON: {Json}", json);
            return;
        }

        _logger.LogInformation("➡️ Broadcasting TileClaimed ({Q},{R}) by {Color}", tile.Q, tile.R, tile.Color);
        await _hub.Clients.All.SendAsync("TileClaimed", tile, ct);
    }

    private async Task HandlePlayerJoinedAsync(string json, CancellationToken ct)
    {
        var player = JsonSerializer.Deserialize<Player>(json, _jsonOpts);
        if (player == null)
        {
            _logger.LogWarning("⚠️ Invalid player event JSON: {Json}", json);
            return;
        }

        _logger.LogInformation("➡️ Broadcasting PlayerJoined: {Name}", player.Name);
        await _hub.Clients.All.SendAsync("UserJoined", new { player.Id, player.Color }, ct);
    }

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
