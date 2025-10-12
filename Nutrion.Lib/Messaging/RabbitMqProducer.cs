using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Nutrion.Messaging;

public interface IMessageProducer : IAsyncDisposable
{
    Task PublishAsync<T>(string queueName, T message, byte priority = 0, CancellationToken cancellationToken = default);
}


public class RabbitMqProducer : IMessageProducer
{
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly IRabbitMqConnectionProvider _provider;

    //public IConnection? Connection { get; private set; }
    //public IChannel? Channel { get; private set; }
    private bool _disposed;

    public RabbitMqProducer(ILogger<RabbitMqProducer> logger, IRabbitMqConnectionProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    /*
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        Console.WriteLine("[RabbitMQ] Connection and channel established.");

    }


    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var rabbitMqConnection = _config.GetConnectionString("RabbitMQ")
                        ?? "amqp://guest:guest@ubuntu-devjn:5672/";

        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitMqConnection),
            AutomaticRecoveryEnabled = false // we handle reconnect ourselves
        };

        try
        {
            Connection = await factory.CreateConnectionAsync(cancellationToken);
            Channel = await Connection.CreateChannelAsync();

            // 🧩 subscribe to shutdown events to auto-reconnect
            Connection.ConnectionShutdownAsync += async (_, args) =>
            {
                if (_disposed) return;
                Console.WriteLine($"[RabbitMQ] Connection shutdown detected: {args.ReplyText}");
            };

            Channel.ChannelShutdownAsync += async (_, args) =>
            {
                Console.WriteLine($"[RabbitMQ] Channel closed by {args.Initiator} ({args.ReplyCode}): {args.ReplyText}");

                if (_disposed) return;
                Console.WriteLine($"[RabbitMQ] Channel shutdown detected: {args.ReplyText}");
            };


        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RabbitMQ] Initial connection failed: {ex.Message}");
        }
}


    
    private async Task SafeCloseAsync()
    {
        try
        {
            Console.WriteLine("[RabbitMQ] Closing connection...");
            if (Channel != null)
            {
                await Channel.CloseAsync();
                await Channel.DisposeAsync();
                Channel = null;
            }

            if (Connection != null)
            {
                await Connection.CloseAsync();
                await Connection.DisposeAsync();
                Connection = null;
            }

            Console.WriteLine("[RabbitMQ] Connection closed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RabbitMQ] Error while closing: {ex}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await SafeCloseAsync();
    }


     */


    public async Task PublishAsync<T>(string queueName, T message, byte priority = 0, CancellationToken cancellationToken = default)
    {
        var connection = await _provider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync();

        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));

        /*
        if (connection is null || !connection.IsOpen || channel is null || !channel.IsOpen)
        {
            _logger.LogWarning("[RabbitMQ] Connection or channel not ready. Attempting reconnect...");
            await ConnectAsync(cancellationToken);
        }
        */

        if (channel is null)
        {
            _logger.LogError("[RabbitMQ] Channel unavailable after reconnect attempt.");
            return;
        }

        await channel.QueueDeclareAsync(
            queueName, 
            durable: true, 
            exclusive: false, 
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-max-priority"] = 10 // 0–10 levels of priority
            });


        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var props = new BasicProperties { 
            Persistent = true,
            Priority = priority // higher = more important
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken
         );

        _logger.LogInformation("📤 Published message to queue '{Queue}' ({Size} bytes)", queueName, body.Length);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

}
