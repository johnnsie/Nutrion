using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database.Entities;
using Nutrion.Lib.Database.Hydration;
using Nutrion.Lib.Database.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameLib.Database.EntityRepository;

public class EntityRepository
{
    public IRepository<Tile> Tiles { get; }
    public IRepository<Player> Players { get; }
    public IRepository<Account> Accounts { get; }

    public EntityRepository(IRepository<Tile> tiles, IRepository<Player> players, IRepository<Account> accounts)
    {
        Tiles = tiles;
        Players = players;
        Accounts = accounts;
    }
}

/*  
public interface ITileCustomDemoRepository : IRepository<Tile> { }

public class TileCustomDemoRepository : Repository<Tile>, ITileCustomDemoRepository
{
    public TileCustomDemoRepository(AppDbContext db, ILoggerFactory loggerFactory)
        : base(db, loggerFactory) { }
}
*/