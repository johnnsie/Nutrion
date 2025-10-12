using Microsoft.AspNetCore.SignalR;
using Nutrion.Lib.Database.Game.Persistence;
using Nutrion.Messaging;
using System;
using System.Collections.Concurrent;

namespace Nutrion.GameServer.SignalR;

// -------------------
//  Helpers / Classes
// -------------------

public class GameHub : Hub
{
    private static readonly ConcurrentDictionary<string, PlayerSession> Sessions = new();

    private readonly ColorAllocator _colors;
    private readonly IMessageProducer _bus;
    private readonly IReadOnlyTileRepository _tileRepo;

    public GameHub(ColorAllocator colors, IMessageProducer bus, IReadOnlyTileRepository tileRepo)
    {
        _colors = colors;
        _bus = bus;
        _tileRepo = tileRepo;   
    }

    public override async Task OnConnectedAsync()
    {
        var id = Context.ConnectionId;
        var color = _colors.AssignColor();

        var session = new PlayerSession(id, color);
        Sessions[id] = session;

        Console.WriteLine($"🟢 Connected: {id} color={color}");

        var board = await _tileRepo.GetAllAsync();

        await Clients.Caller.SendAsync("Connected", new { id, color });
        await Clients.Others.SendAsync("UserJoined", new { id, color });

        
        await Clients.Caller.SendAsync("BoardState", board);

    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        if (Sessions.TryRemove(Context.ConnectionId, out var session))
        {
            _colors.ReleaseColor(session.Color);
            Console.WriteLine($"🔴 Disconnected: {session.Id}");
            await Clients.All.SendAsync("UserLeft", new { id = session.Id });
        }
        await base.OnDisconnectedAsync(ex);
    }

    public async Task ClaimTile(int q, int r)
    {
        try
        {
            if (!Sessions.TryGetValue(Context.ConnectionId, out var session))
                return;

            //await Clients.All.SendAsync("TileClaimed",
            //    new { q, r, color = session.Color, user = session.Id });

            await _bus.PublishAsync("game.commands.tile.claim",
                new { UserId = session.Id, Color = session.Color, Q = q, R = r });

            Console.WriteLine($"📤 published claim ({q},{r})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Hub error: {ex}");
            throw;
        }
    }

}

public record PlayerSession(string Id, string Color);
