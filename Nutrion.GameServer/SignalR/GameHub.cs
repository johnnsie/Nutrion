using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.Database.Game.Hydration;
using Nutrion.Messaging;
using System;
using System.Collections.Concurrent;
using System.Drawing;

namespace Nutrion.GameServer.SignalR;

// -------------------
//  Helpers / Classes
// -------------------
public record PlayerSession(string Id);

public class GameHub : Hub
{
    internal static readonly ConcurrentDictionary<string, PlayerSession> Sessions = new();

    private readonly IMessageProducer _bus;
    private readonly ITileReadRepository _tileRepo;
    private readonly IReadRepository<Account> _readRepo;

    public GameHub(IMessageProducer bus, ITileReadRepository tileRepo, IReadRepository<Account> readRepo)
    {
        _bus = bus;
        _tileRepo = tileRepo;
        _readRepo = readRepo;
    }

    public override async Task OnConnectedAsync()
    {
        var id = Context.ConnectionId;
        var session = new PlayerSession(id);
        Sessions[id] = session;

        Console.WriteLine($"🟢 Connected: {id}");

        await Clients.Caller.SendAsync("Connected", new { id });

        var boardSecond = await _tileRepo.GetBoardAsync();

        await Clients.Caller.SendAsync("BoardState", boardSecond);

    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        if (Sessions.TryRemove(Context.ConnectionId, out var session))
        {
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

            await _bus.PublishTopicAsync("game.commands.exchange", "game.commands.tile.claim", 
                new Tile() {  
                    OwnerId = session.Id, 
                    Q = q, 
                    R = r });

            Console.WriteLine($"📤 published claim ({q},{r})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Hub error: {ex}");
            throw;
        }
    }

    public async Task JoinGame(string playerName)
    {
        var id = Context.ConnectionId;
        //var color = _colors.AssignColor();

        Console.WriteLine($"🟢 GetAccount: {id} playerName={playerName}");

        var newPlayer = new Player()
        {
            Name = playerName,
            OwnerId = id,
            Id = Guid.NewGuid()
        };
        
        Console.WriteLine($"🟢 Player joined: {playerName} ({id}) color=NoneYet");

        await _bus.PublishTopicAsync("game.commands.exchange", "game.commands.player.join", newPlayer);
    }

    public async Task GetAccount(string playerName)
    {
        var id = Context.ConnectionId;
        //var color = _colors.AssignColor();

        Console.WriteLine($"🟢 GetAccount: {id} playerName={playerName}");

        //var account = await _readRepo.FindAsync(a => a.Player.Name == playerName);
        var account = await _readRepo.GetAsync(
            a => a.Player.Name == playerName,
            include: q => q.Include(a => a.Player)
                           .Include(a => a.Resources)
        );

        Console.WriteLine($"🟢 HOW MANY : {account}");

        await Clients.Caller.SendAsync("AccountState", account);
    }


}

