using Microsoft.AspNetCore.SignalR;

namespace Nutrion.GameServer.SignalR;

/// <summary>
/// Forces all SignalR clients to disconnect immediately when the app is shutting down.
/// </summary>
public class SignalRShutdownService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IHubContext<GameHub> _hub;

    public SignalRShutdownService(IHostApplicationLifetime lifetime, IHubContext<GameHub> hub)
    {
        _lifetime = lifetime;
        _hub = hub;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStopping.Register(async () =>
        {
            Console.WriteLine("⚠️ Server stopping — closing all SignalR connections...");

            // Copy keys to avoid collection modified issues
            var connections = GameHub.Sessions.Keys.ToList();

            foreach (var connectionId in connections)
            {
                try
                {
                    // Inform the client to disconnect gracefully
                    await _hub.Clients.Client(connectionId)
                        .SendCoreAsync("ForceDisconnect", Array.Empty<object>());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error disconnecting connection {connectionId}: {ex.Message}");
                }
            }

            GameHub.Sessions.Clear();
            Console.WriteLine("✅ All SignalR sessions notified and cleared.");
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
