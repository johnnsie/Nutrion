using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nutrion.Messaging;
using System.Text;
using System.Text.Json;

namespace Nutrion.GameServer.Worker;

public abstract class MessageWorkerBase<TMessage> : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IMessageProducer _producer;
    private readonly IMessageConsumer _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _exchange;
    private readonly string _topicPattern;
    private readonly string _queueName;

    protected MessageWorkerBase(
        ILogger logger,
        IMessageProducer producer,
        IMessageConsumer consumer,
        IServiceScopeFactory scopeFactory,
        string exchange,
        string topicPattern,
        string queueName)
    {
        _logger = logger;
        _producer = producer;
        _consumer = consumer;
        _scopeFactory = scopeFactory;
        _exchange = exchange;
        _topicPattern = topicPattern;
        _queueName = queueName;
    }

    protected abstract Task HandleMessageAsync(TMessage message, IServiceScope scope, CancellationToken ct);

    protected virtual Task OnBeforeConsumeAsync(CancellationToken ct) => Task.CompletedTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Worker {Worker} started consuming {Topic}", GetType().Name, _topicPattern);
            await OnBeforeConsumeAsync(stoppingToken);

            await _consumer.StartConsumingTopicAsync(
                _exchange,
                _topicPattern,
                async (routingKey, body, ct) =>
                {
                    var message = DeserializeMessage(body);
                    if (message == null)
                    {
                        _logger.LogWarning("Invalid message for {Worker}", GetType().Name);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    await HandleMessageAsync(message, scope, ct);
                },
                stoppingToken,
                queueName: _queueName
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker {Worker} crashed", GetType().Name);
        }
    }

    private TMessage? DeserializeMessage(ReadOnlyMemory<byte> body)
    {
        try
        {
            var json = Encoding.UTF8.GetString(body.Span);
            return JsonSerializer.Deserialize<TMessage>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize message in {Worker}", GetType().Name);
            return default;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
