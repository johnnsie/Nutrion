using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Nutrion.Messaging;

public interface IMessageConsumer : IAsyncDisposable
{
    Task StartConsumingTopicAsync(
        string exchangeName,
        string topicPattern,
        Func<string, ReadOnlyMemory<byte>, CancellationToken, Task<MessageResult>> onMessageWithResult,
        CancellationToken cancellationToken = default,
        string? queueName = null);
}

public enum MessageResult
{
    Ack,
    NackRequeue,
    NackDrop
}


public class RabbitMqConsumer : IMessageConsumer
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IRabbitMqConnectionProvider _provider;
    private IChannel? _channel;
    private string? _consumerTag;
    private bool _disposed;

    public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger, IRabbitMqConnectionProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    public async Task StartConsumingTopicAsync(
        string exchangeName,
        string topicPattern,
        Func<string, ReadOnlyMemory<byte>, CancellationToken, Task<MessageResult>> onMessageWithResult,
        CancellationToken cancellationToken = default,
        string? queueName = null)
    {
        var connection = await _provider.GetConnectionAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync();

        // 🧩 Ensure exchange exists and is a Topic type
        await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: true);

        // 🧩 Determine queue name (use provided or generate unique one)
        var queue = queueName ?? $"{exchangeName}.{Guid.NewGuid()}";

        // 1️⃣ Declare queue
        await _channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // 2️⃣ Bind queue to exchange with provided topic pattern
        await _channel.QueueBindAsync(queue, exchangeName, topicPattern);
        _logger.LogInformation("🐇 Bound queue {Queue} to {Exchange} with pattern {Pattern}",
                               queue, exchangeName, topicPattern);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 5, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        var semaphore = new SemaphoreSlim(5);

        consumer.ReceivedAsync += async (ch, ea) =>
        {
            await semaphore.WaitAsync(cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleMessageAsync(ea, onMessageWithResult, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
        };

        _consumerTag = await _channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer);
        _logger.LogInformation("🐇 Listening on queue {Queue} (exchange={Exchange}, pattern='{Pattern}')", queue, exchangeName, topicPattern);
    }

    /// <summary>
    /// Handles a single message including acknowledgment or requeue logic.
    /// </summary>
    private async Task HandleMessageAsync(
        BasicDeliverEventArgs ea,
        Func<string, ReadOnlyMemory<byte>, CancellationToken, Task<MessageResult>> onMessage,
        CancellationToken cancellationToken)
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized");

        var routingKey = ea.RoutingKey;
        var body = ea.Body;
        var message = Encoding.UTF8.GetString(body.Span);

        _logger.LogInformation("📨 Received from {RoutingKey}: {Message}", routingKey, message);

        MessageResult result;
        try
        {
            result = await onMessage(routingKey, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in message handler — requeueing message");
            result = MessageResult.NackRequeue;
        }

        switch (result)
        {
            case MessageResult.Ack:
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                break;

            case MessageResult.NackRequeue:
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                break;

            case MessageResult.NackDrop:
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_channel != null)
            {
                if (_consumerTag != null)
                    await _channel.BasicCancelAsync(_consumerTag);

                await _channel.CloseAsync();
                await _channel.DisposeAsync();
                _channel = null;
            }

            _logger.LogInformation("🐇 RabbitMQ consumer stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing consumer");
        }
    }
}
