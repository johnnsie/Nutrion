using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.Database.Game.Persistence;
using Nutrion.Lib.GameLogic.Systems;
using Nutrion.Lib.Messaging.DTO;
using Nutrion.Messaging;

namespace Nutrion.GameServer.Worker;

public class BuildWorker : MessageWorkerBase<GameCommandMessage<BuildTileCommand>>
{
    private readonly IMessageProducer _producer;
    private readonly ILogger<BuildWorker> _logger;

    public BuildWorker(
        ILogger<BuildWorker> logger,
        IMessageProducer producer,
        IMessageConsumer consumer,
        IServiceScopeFactory scopeFactory)
        : base(logger, producer, consumer, scopeFactory,
               exchange: "game.commands.exchange",
               topicPattern: "game.commands.tile.build",
               queueName: "game.commands.tileworker")
    {
        _producer = producer;
        _logger = logger;
    }

    protected override async Task<MessageResult> HandleMessageAsync(GameCommandMessage<BuildTileCommand> message, IServiceScope scope, CancellationToken ct)
    {
        var tileSystem = scope.ServiceProvider.GetRequiredService<TileSystem>();
        var payload = message.Payload;

        _logger.LogInformation("🏗️ [BuildWorker] Handling {Type} (CorrelationId: {Id}) on tile ({Q},{R})",
            message.Type, message.CorrelationId, payload.Q, payload.R);

        var tileContent = new TileContent
        {
            GLTFComponent = payload.GLTFComponent,
            Type = payload.BuildingType,
            OwnerId = payload.OwnerId,
            Tile = new Tile { Q = payload.Q, R = payload.R }
        };

        var tilecontent = await tileSystem.BuildOnTileAsync(tileContent, ct);

        await _producer.PublishTopicAsync(
            "game.events.exchange",
            "game.events.tile.built",
            tilecontent,
            cancellationToken: ct);


        return MessageResult.Ack;
    }

}
