using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace Nutrion.Messaging;

public interface IMessageConsumer : IAsyncDisposable
{
    /*
     Task StartConsumingAsync(
        string queueName,
        Func<string, CancellationToken, Task> onMessageAsync,
        CancellationToken cancellationToken = default);
    */

    Task StartConsumingAsync(
        string topicPattern,
        Func<string /*routingKey*/, ReadOnlyMemory<byte> /*body*/, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken);

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

    public async Task StartConsumingAsync(
        string topicPattern,
        Func<string /*routingKey*/, ReadOnlyMemory<byte> /*body*/, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken = default)
    {
        var connection = await _provider.GetConnectionAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync();

        const string exchangeName = "game.events.exchange";

        // 🧩 Make sure the exchange exists and is a Topic exchange
        await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: true);

        await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: true);

        // 2️⃣ Server-named, auto-delete queue (like in tutorial)
        var q = await _channel.QueueDeclareAsync();
        string queueName = q.QueueName;

        // 3️⃣ Bind to all game events
        await _channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: "game.events.#");
        _logger.LogInformation("🐇 Bound queue {Queue} to {Exchange} with pattern game.events.#", queueName, exchangeName);

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
                    var routingKey = ea.RoutingKey;
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body.Span);

                    _logger.LogInformation("📨 Received from {RoutingKey}: {Message}", routingKey, message);

                    await onMessage(routingKey, body, cancellationToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
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

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
        _logger.LogInformation("🐇 Listening on {Exchange} with pattern '{Pattern}'", exchangeName, topicPattern);
    }

    public async ValueTask DisposeAsync()
    {
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
