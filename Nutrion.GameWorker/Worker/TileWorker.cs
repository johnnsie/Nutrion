using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Messaging;
using Nutrion.GameWorker.Services;
using Nutrion.Lib.Database.Game.Persistence;

namespace Nutrion.GameServer.Worker;

public class TileWorker : MessageWorkerBase<Tile>
{
    private readonly TileStateService _tileState;
    private readonly IMessageProducer _producer;
    private readonly ILogger<TileWorker> _logger;

    public TileWorker(
        ILogger<TileWorker> logger,
        IMessageProducer producer,
        IMessageConsumer consumer,
        TileStateService tileState,
        IServiceScopeFactory scopeFactory)
        : base(logger, producer, consumer, scopeFactory,
               exchange: "game.commands.exchange",
               topicPattern: "game.commands.tile.claim",
               queueName: "game.commands.tileworker")
    {
        _tileState = tileState;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task HandleMessageAsync(Tile cmd, IServiceScope scope, CancellationToken ct)
    {
        _logger.LogInformation("Received ClaimTile from {Owner}", cmd.OwnerId);

        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Tile>>();
        var changed = await _tileState.ClaimTileAsync(cmd.OwnerId, cmd.Color, cmd.Q, cmd.R, ct);

        if (!changed) return;

        await _producer.PublishTopicAsync(
            "game.events.exchange",
            "game.events.tile.claimed",
            cmd,
            cancellationToken: ct);

        _logger.LogInformation("Tile claimed by {Owner}", cmd.OwnerId);
    }
}
