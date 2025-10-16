using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.Database.Game.Persistence;
using Nutrion.Lib.GameLogic.Systems;
using Nutrion.Lib.Messaging.DTO;
using Nutrion.Messaging;
using System.Text.Json;

namespace Nutrion.GameServer.Worker;

public record CommandHandlerRegistration(
    Type PayloadType,
    Func<object,string, IServiceScope, CancellationToken, Task<MessageResult>> Handler);


public class TileWorker : MessageWorkerBase<GameClientEvent>
{
    private readonly IMessageProducer _producer;
    private readonly ILogger<TileWorker> _logger;
    private readonly Dictionary<string, CommandHandlerRegistration> _handlers = new();

    public TileWorker(
        ILogger<TileWorker> logger,
        IMessageProducer producer,
        IMessageConsumer consumer,
        IServiceScopeFactory scopeFactory)
        : base(logger, producer, consumer, scopeFactory,
               exchange: "game.commands.exchange",
               topicPattern: "game.commands.tile.*",
               queueName: "game.commands.tileworker")
    {
        _producer = producer;
        _logger = logger;

        // Register command handlers here:
        _handlers["game.commands.tile.claim"] = new CommandHandlerRegistration(
            typeof(Tile),
            HandleClaimTileGenericAsync);

        _handlers["game.commands.tile.build"] = new CommandHandlerRegistration(
            typeof(TileContent),
            HandleBuildTileGenericAsync);
        
    }

    protected override async Task<MessageResult> HandleMessageAsync(GameClientEvent evt, IServiceScope scope, CancellationToken ct)
    {
        var topic = evt.Topic ?? "unknown";
        var owner = evt.OwnerSessionId ?? "unknown";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogDebug("🎯 Dispatching event from {Owner} with topic '{Topic}'", owner, topic);

        try
        {
            if (!_handlers.TryGetValue(topic, out var registration))
            {
                _logger.LogWarning("⚠️ No handler registered for topic '{Topic}'", topic);
                return MessageResult.NackDrop;
            }

            // Generic deserialization
            var payload = evt.DeserializePayload(registration.PayloadType);
            if (payload == null)
            {
                _logger.LogWarning("⚠️ Failed to deserialize payload for {Topic}", topic);
                return MessageResult.NackDrop;
            }

            // Execute handler
            var result = await registration.Handler(payload, owner, scope, ct);

            stopwatch.Stop();
            _logger.LogDebug("✅ Completed {Topic} for {Owner} in {Elapsed}ms → {Result}",
                topic, owner, stopwatch.ElapsedMilliseconds, result);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "💥 Exception while handling {Topic} for {Owner} after {Elapsed}ms",
                topic, owner, stopwatch.ElapsedMilliseconds);
            return MessageResult.NackDrop;
        }
    }


    private async Task<MessageResult> HandleClaimTileGenericAsync(object payload,string sessionId, IServiceScope scope, CancellationToken ct)
    {
        var tile = (Tile)payload;
        var tileSystem = scope.ServiceProvider.GetRequiredService<TileSystem>();

        _logger.LogInformation("📩 [Claim] Owner={OwnerId} claiming ({Q},{R})", tile.OwnerId, tile.Q, tile.R);

        var updated = await tileSystem.ClaimTileAsync(sessionId, tile, ct);
        if (updated == null)
            return MessageResult.NackDrop;

        await _producer.PublishTopicAsync("game.events.exchange", "game.events.tile.claimed", updated, ct);
        return MessageResult.Ack;
    }

    private async Task<MessageResult> HandleBuildTileGenericAsync(object payload, string sessionId, IServiceScope scope, CancellationToken ct)
    {
        var tileContent = (TileContent)payload;
        tileContent.OwnerId = sessionId;
        var tileSystem = scope.ServiceProvider.GetRequiredService<TileSystem>();

        _logger.LogInformation("🏗️ [Build] Building {Type} on tile ({Q},{R})", tileContent.Type, tileContent.Tile.Q, tileContent.Tile.R);

        var built = await tileSystem.BuildOnTileAsync(tileContent, ct);
        await _producer.PublishTopicAsync("game.events.exchange", "game.events.tile.built", built, ct);

        return MessageResult.Ack;
    }

}

