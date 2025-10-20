using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nutrion.GameLib.Database.Entities;
using Nutrion.GameLib.Database.EntityRepository;
using Nutrion.Lib.Database.Hydration;
using Nutrion.Lib.Messaging.DTO;
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
    private readonly IReadRepository<Tile> _tileRepo;
    private readonly IReadRepository<Account> _readRepo;
    private readonly IReadRepository<Building> _buildingRepo;
    private readonly IReadRepository<BuildingType> _buildingTypeRepo;

    public GameHub(
        IMessageProducer bus,
        IReadRepository<Tile> tileRepo, 
        IReadRepository<Account> readRepo,
        IReadRepository<Building> buildingRepo,
        IReadRepository<BuildingType> buildingTypeRepo
        )
    {
        _bus = bus;
        _tileRepo = tileRepo;
        _readRepo = readRepo;
        _buildingRepo = buildingRepo;
        _buildingTypeRepo = buildingTypeRepo;
    }

    public override async Task OnConnectedAsync()
    {
        var id = Context.ConnectionId;
        var session = new PlayerSession(id);
        Sessions[id] = session;

        Console.WriteLine($"🟢 Connected: {id}");

        await Clients.Caller.SendAsync("Connected", new { id });

        var tiles = await _tileRepo.GetAllAsync(
            include: q => q.Include(t => t.Contents));

        await Clients.Caller.SendAsync("BoardState", new Board(tiles));

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

    // --------------------------
    // 📨 Generic Publish Method
    // --------------------------
    public async Task SendEvent(GameClientEvent message)
    {
        Console.WriteLine("📨 SendEvent called", message.Payload);

        if (!Sessions.TryGetValue(Context.ConnectionId, out var session))
            return;

        if (string.IsNullOrWhiteSpace(message.Topic))
        {
            Console.WriteLine("⚠️ Missing topic field in Publish()");
            return;
        }

        Console.WriteLine($"📤 [{session.Id}] Publishing topic '{message.Topic}'");

        try
        {
            message.OwnerSessionId = session.Id;
            // Generic pass-through to message bus
            await _bus.PublishTopicAsync("game.commands.exchange", message.Topic, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Hub publish error: {ex}");
            throw;
        }
    }

    public async Task<object> GetData(GameClientEvent request)
    {
        switch (request.Topic)
        {
            case "game.commands.get.buildingtypes":
                return await _buildingTypeRepo.GetAllAsync(
                            include: q => q.Include(bt => bt.BuildingCost)
                                            .ThenInclude(bc => bc.RssImpact)
                        );
            case "game.commands.get.buildings":
                return await _buildingRepo.GetAllAsync(
                            include: q => q.Include(b => b.PlayerOwner)
                                           .ThenInclude(p => p.PlayerColor)
                                           .Include(b => b.BuildingType)
                                               .ThenInclude(bt => bt.BuildingCost)
                                                   .ThenInclude(bc => bc.RssImpact)
                        );
            default:
                throw new Exception($"Unknown topic: {request.Topic}");
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

        Console.WriteLine($"🟢 GetAccount: {id} playerName={playerName}");

        //var account = await _readRepo.FindAsync(a => a.Player.Name == playerName);
        var account = await _readRepo.GetAsync(
            a => a.Player.Name == playerName,
            include: q => q.Include(a => a.Player)
                           .ThenInclude(p => p.PlayerColor)
                           .Include(a => a.Resources)
        );

        Console.WriteLine($"🟢 HOW MANY : {account}");
        Console.WriteLine($"🟢 my super color : {account.Player.PlayerColor.HexCode}");

        await Clients.Caller.SendAsync("AccountState", account);
    }



}

