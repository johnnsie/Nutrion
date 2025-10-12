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
    Task StartConsumingAsync(
        string queueName,
        Func<string, CancellationToken, Task> onMessageAsync,
        CancellationToken cancellationToken = default);
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

    public async Task StartConsumingAsync(string queueName, Func<string, CancellationToken, Task> onMessageAsync, CancellationToken cancellationToken = default)
    {
        var connection = await _provider.GetConnectionAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-max-priority"] = 10 // 0–10 levels of priority
            });


        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 3, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        var semaphore = new SemaphoreSlim(3); // max 10 concurrent tasks

        consumer.ReceivedAsync += async (ch, ea) =>
        {
            await semaphore.WaitAsync(cancellationToken); // respect outer cancel

            _ = Task.Run(async () =>
            {

                string message = string.Empty;

                try
                {
                    var body = ea.Body.ToArray();
                    message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("📥 Received message from '{Queue}': {Message}", queueName, message);

                    await onMessageAsync(message, cancellationToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing message from {Queue}: {Message}", queueName, message);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
                finally
                {
                    semaphore.Release();
                }

            }, cancellationToken);
        };

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("🐇 RabbitMQ consumer started on queue '{Queue}'", queueName);
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