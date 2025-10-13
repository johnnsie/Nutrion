using Microsoft.EntityFrameworkCore;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameWorker.Persistence;

public interface IEntityRepository
{
    Task<Tile?> GetTileAsync(int q, int r, CancellationToken cancellationToken = default);
    Task SaveTileAsync(Tile tile, CancellationToken cancellationToken = default);
    Task SavePlayerAsync(Player tile, CancellationToken cancellationToken = default);
}

public class EntityRepository : IEntityRepository
{
    private readonly AppDbContext _db;

    public EntityRepository(AppDbContext db)
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

    public async Task SavePlayerAsync(Player player, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Player
            .FirstOrDefaultAsync(p => p.OwnerId == player.OwnerId, cancellationToken);

        if (existing == null)
        {
            // New player → insert
            player.LastUpdated = DateTimeOffset.UtcNow;
            _db.Player.Add(player);
        }
        else
        {
            // Existing player → update
            existing.Color = player.Color;
            existing.Name = player.Name;
            existing.LastUpdated = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

}