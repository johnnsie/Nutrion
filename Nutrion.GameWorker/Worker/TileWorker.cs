using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Messaging;
using Nutrion.Lib.Database.Game.Persistence;
using Nutrion.Lib.GameLogic.Systems;

namespace Nutrion.GameServer.Worker;

public class TileWorker : MessageWorkerBase<Tile>
{
    private readonly IMessageProducer _producer;
    private readonly ILogger<TileWorker> _logger;

    public TileWorker(
        ILogger<TileWorker> logger,
        IMessageProducer producer,
        IMessageConsumer consumer,
        IServiceScopeFactory scopeFactory)
        : base(logger, producer, consumer, scopeFactory,
               exchange: "game.commands.exchange",
               topicPattern: "game.commands.tile.claim",
               queueName: "game.commands.tileworker")
    {
        _producer = producer;
        _logger = logger;
    }

    protected override async Task<MessageResult> HandleMessageAsync(Tile tile, IServiceScope scope, CancellationToken ct)
    {
        _logger.LogInformation("📩 Received ClaimTile request from {OwnerId} for tile ({Q},{R})",
            tile.OwnerId, tile.Q, tile.R);

        string previousOwner = tile.OwnerId;    
        var tileSystem = scope.ServiceProvider.GetRequiredService<TileSystem>();

        // 1️⃣ Always process the claim
        var updatedTile = await tileSystem.ClaimTileAsync(tile.OwnerId, tile, ct);

        if (updatedTile == null)
        {
            _logger.LogWarning("⚠️ ClaimTileAsync returned null for tile ({Q},{R})", tile.Q, tile.R);
            return MessageResult.Ack;
        }

        // 3️⃣ Publish event since ownership actually changed
        await _producer.PublishTopicAsync(
            "game.events.exchange",
            "game.events.tile.claimed",
            updatedTile,
            cancellationToken: ct);

        _logger.LogInformation("🎨 Tile ({Q},{R}) now owned by {OwnerId}. Event published.",
            updatedTile.Q, updatedTile.R, updatedTile.OwnerId);

        return MessageResult.Ack;

    }

}
