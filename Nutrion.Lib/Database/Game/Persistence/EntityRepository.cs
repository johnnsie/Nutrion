using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.Lib.Database.Game.Persistence;

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

