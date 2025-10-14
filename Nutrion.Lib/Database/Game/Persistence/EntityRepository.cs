using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.Lib.Database.Game.Persistence;

public class EntityRepository
{
    public IRepository<Tile> Tiles { get; }
    public IRepository<Player> Players { get; }

    public EntityRepository(IRepository<Tile> tiles, IRepository<Player> players)
    {
        Tiles = tiles;
        Players = players;
    }
}

