using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nutrion.Lib.Database.Hydration;
using Nutrion.GameLib.Database.Entities;

namespace Nutrion.GameServer.SignalR;

public class GameHubNotifier
{
    private readonly IHubContext<GameHub> _hub;
    private readonly IReadRepository<Account> _readRepo;

    public GameHubNotifier(IHubContext<GameHub> hub, IReadRepository<Account> readRepository)
    {
        _hub = hub;
        _readRepo = readRepository;
    }

    public async Task SendToSessionAsync(string sessionId, string eventName, object payload)
    {
        if (!GameHub.Sessions.ContainsKey(sessionId))
        {
            Console.WriteLine($"⚠️ Tried to send to non-existent session: {sessionId}");
            return;
        }

        switch (eventName)
        {
            case "AccountState":

                Console.WriteLine($"🟢 GetAccount: {sessionId} playerName={payload}");
                var account = await _readRepo.GetAsync(
                    a => a.Player.Name == payload.ToString(),
                    include: q => q.Include(a => a.Player)
                                    .Include(a => a.Resources)
                );
                Console.WriteLine($"🟢 HOW MANY : {account}");
                //var account = await _readRepo.FindAsync(a => a.Player.Name == playerName);

                await _hub.Clients.Client(sessionId).SendAsync(eventName, account);
                break;
            default:
                await _hub.Clients.Client(sessionId).SendAsync(eventName, payload);
                break;
        }
    }

    public async Task BroadcastAsync(string eventName, object payload)
    {
        await _hub.Clients.All.SendAsync(eventName, payload);
    }

    public async Task BroadcastExceptAsync(string excludedSessionId, string eventName, object payload)
    {
        await _hub.Clients.AllExcept(excludedSessionId).SendAsync(eventName, payload);
    }

    // --- Specific typed event helpers ---

    public Task BroadcastTileClaimedAsync(Tile tile, CancellationToken ct = default)
    {
        Console.WriteLine($"📡 Broadcasting TileClaimed ({tile.Q},{tile.R}) by {tile.Color}");
        return _hub.Clients.All.SendAsync("TileClaimed", tile, ct);
    }
    
    public Task BroadcastPlayerJoinedAsync(Player player, CancellationToken ct = default)
    {
        Console.WriteLine($"📡 Broadcasting UserJoined: {player.Name}");
        return _hub.Clients.All.SendAsync("UserJoined", player, ct);
    }
    public Task BroadcastTileBuiltAsync(TileContent tileContent, CancellationToken ct = default)
    {
        Console.WriteLine($"📡 Broadcasting TileBuilt: {tileContent.GLTFComponent}");
        return _hub.Clients.All.SendAsync("TileBuilt", tileContent, ct);
    }
    public Task BroadcastBuildingBuiltAsync(Building building, CancellationToken ct = default)
    {
        Console.WriteLine($"📡 Broadcasting BuildingBuilt: {building.BuildingType}");
        return _hub.Clients.All.SendAsync("BuildingBuilt", building, ct);
    }
}
