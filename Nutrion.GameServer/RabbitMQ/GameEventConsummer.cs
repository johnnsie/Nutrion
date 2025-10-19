using Nutrion.GameServer.SignalR;
using Nutrion.Messaging;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nutrion.GameLib.Database.Entities;

namespace Nutrion.GameServer.RabbitMQ;

public class GameEventConsumer : BackgroundService
{
    private readonly IMessageConsumer _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GameEventConsumer> _logger;

    private const string ExchangeName = "game.events.exchange";

    public GameEventConsumer(
        IMessageConsumer consumer,
        IServiceScopeFactory scopeFactory,
        ILogger<GameEventConsumer> logger)
    {
        _consumer = consumer;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎧 Starting GameEventConsumer...");

        try
        {
            await _consumer.StartConsumingTopicAsync(
                exchangeName: ExchangeName,
                topicPattern: "game.events.#",
                onMessageWithResult: async (routingKey, body, ct) =>
                {
                    // 🔸 Create a scoped lifetime for each message
                    using var scope = _scopeFactory.CreateScope();
                    var notifier = scope.ServiceProvider.GetRequiredService<GameHubNotifier>();

                    await HandleMessageAsync(notifier, routingKey, body, ct);

                    return MessageResult.Ack;
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

    private async Task HandleMessageAsync(GameHubNotifier notifier, string routingKey, ReadOnlyMemory<byte> body, CancellationToken ct)
    {
        var json = Encoding.UTF8.GetString(body.Span);
        _logger.LogInformation("📨 Received {RoutingKey}: {Json}", routingKey, json);

        try
        {
            switch (routingKey)
            {
                case "game.events.tile.claimed":
                    var tile = JsonSerializer.Deserialize<Tile>(json, _jsonOpts);
                    if (tile != null)
                        await notifier.BroadcastTileClaimedAsync(tile, ct);
                    else
                        _logger.LogWarning("⚠️ Invalid tile event JSON: {Json}", json);
                    break;
                case "game.events.tile.built":
                    var tileContent = JsonSerializer.Deserialize<TileContent>(json, _jsonOpts);
                    if (tileContent != null)
                        await notifier.BroadcastTileBuiltAsync(tileContent, ct);
                    else
                        _logger.LogWarning("⚠️ Invalid tile event JSON: {Json}", json);
                    break;
                case "game.events.player.joined":
                    var player = JsonSerializer.Deserialize<Player>(json, _jsonOpts);
                    if (player != null)
                    {
                        await notifier.BroadcastPlayerJoinedAsync(player, ct);
                        await notifier.SendToSessionAsync(player.OwnerId, "AccountState", player.Name);
                    }
                    else
                        _logger.LogWarning("⚠️ Invalid player event JSON: {Json}", json);
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

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
