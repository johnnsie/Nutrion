using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database.Entities;
using Nutrion.GameServer.Worker;
using Nutrion.Lib.GameLogic.Systems;
using Nutrion.Messaging;

namespace Nutrion.GameServer.Worker;

public class PlayerWorker : MessageWorkerBase<Player>
{
    private readonly IMessageProducer _producer;
    private readonly ILogger<PlayerWorker> _logger;

    public PlayerWorker(
        ILogger<PlayerWorker> logger,
        IMessageProducer producer,
        IMessageConsumer consumer,
        IServiceScopeFactory scopeFactory)
        : base(logger, producer, consumer, scopeFactory,
               exchange: "game.commands.exchange",
               topicPattern: "game.commands.player.join",
               queueName: "game.commands.playerworker")
    {
        _producer = producer;
        _logger = logger;
    }

    protected override async Task<MessageResult> HandleMessageAsync(Player player, IServiceScope scope, CancellationToken ct)
    {
        var playerSystem = scope.ServiceProvider.GetRequiredService<PlayerSystem>();

        var newplayer = await playerSystem.GetOrCreateAsync(player, ct);

        await _producer.PublishTopicAsync(
            "game.events.exchange",
            "game.events.player.joined",
            player,
            cancellationToken: ct);

        _logger.LogInformation("Player {User} joined", newplayer.Name);

        return MessageResult.Ack;
    }
}
