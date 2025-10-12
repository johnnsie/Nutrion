using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Nutrion.Messaging;

public interface IRabbitMqConnectionProvider : IAsyncDisposable
{
    ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
}

public class RabbitMqConnectionProvider : IRabbitMqConnectionProvider
{
    private readonly IConfiguration _config;
    private IConnection? _connection;

    public RabbitMqConnectionProvider(IConfiguration config)
    {
        _config = config;
    }

    public async ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_config.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@ubuntu-devjn:5672/"),
            AutomaticRecoveryEnabled = true
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        return _connection;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}
