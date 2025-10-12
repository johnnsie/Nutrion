using Microsoft.EntityFrameworkCore;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameWorker.Persistence;

public interface ITileRepository
{
    Task<Tile?> GetTileAsync(int q, int r, CancellationToken cancellationToken = default);
    Task SaveTileAsync(Tile tile, CancellationToken cancellationToken = default);
}

public class TileRepository : ITileRepository
{
    private readonly AppDbContext _db;

    public TileRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Tile?> GetTileAsync(int q, int r, CancellationToken cancellationToken = default)
    {
        return await _db.Tile.FirstOrDefaultAsync(t => t.Q == q && t.R == r, cancellationToken);
    }

    public async Task SaveTileAsync(Tile tile, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Tile.FirstOrDefaultAsync(t => t.Q == tile.Q && t.R == tile.R, cancellationToken);

        if (existing == null)
        {
            _db.Tile.Add(tile);
        }
        else
        {
            existing.Color = tile.Color;
            existing.OwnerId = tile.OwnerId;
            existing.LastUpdated = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}