using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace Nutrion.Messaging;

public interface IMessageProducer : IAsyncDisposable
{
    Task PublishTopicAsync<T>(
        string exchangeName,
        string routingKey,
        T message,
        CancellationToken cancellationToken = default);
}

public class RabbitMqProducer : IMessageProducer
{
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly IRabbitMqConnectionProvider _provider;

    public RabbitMqProducer(ILogger<RabbitMqProducer> logger, IRabbitMqConnectionProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    public async Task PublishTopicAsync<T>(
        string exchangeName,
        string routingKey,
        T message,
        CancellationToken cancellationToken = default)
    {
        var connection = await _provider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync();

        // 1️⃣ Make sure the exchange exists
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: true);

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
        };
        // 2️⃣ Serialize body
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, options));

        // 3️⃣ Publish to topic exchange
        var props = new BasicProperties { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("📤 Published to {Exchange} ({RoutingKey}) [{Bytes} bytes]",
            exchangeName, routingKey, body.Length);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
