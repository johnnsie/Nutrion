using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database.Entities;
using Nutrion.GameServer.Worker;
using Nutrion.Lib.GameLogic.Systems;
using Nutrion.Lib.Messaging.DTO;
using Nutrion.Messaging;

namespace Nutrion.GameServer.Worker;

public class BuildingWorker : MessageWorkerBase<GameClientEvent>
{
    private readonly IMessageProducer _producer;
    private readonly ILogger<BuildingWorker> _logger;
    private readonly Dictionary<string, CommandHandlerRegistration> _handlers = new();

    public BuildingWorker(
        ILogger<BuildingWorker> logger,
        IMessageProducer producer,
        IMessageConsumer consumer,
        IServiceScopeFactory scopeFactory)
        : base(logger, producer, consumer, scopeFactory,
               exchange: "game.commands.exchange",
               topicPattern: "game.commands.building.*",
               queueName: "game.commands.buildingworker")
    {
        _producer = producer;
        _logger = logger;

        // Register command handlers here:
        _handlers["game.commands.building.build"] = new CommandHandlerRegistration(
            typeof(Building),
            HandleBuildBuildingAsync);
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

    private async Task<MessageResult> HandleBuildBuildingAsync(object payload, string sessionId, IServiceScope scope, CancellationToken ct)
    {
        var building = (Building)payload;
        var buildingSystem = scope.ServiceProvider.GetRequiredService<BuildingSystem>();


        var updated = await buildingSystem.CreateBuildingAsync(sessionId, (Guid)building.BuildingTypeId, building.OriginTileId, ct);
        if (updated == null)
            return MessageResult.NackDrop;

        await _producer.PublishTopicAsync("game.events.exchange", "game.events.building.built", updated, ct);
        return MessageResult.Ack;
    }


}
