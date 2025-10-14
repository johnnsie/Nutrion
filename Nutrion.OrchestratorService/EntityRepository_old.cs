using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameWorker.Persistence.OLD;

public interface IEntityRepository
{
    Task SaveTileAsync(Tile tile, CancellationToken cancellationToken = default);
    Task SavePlayerAsync(Player player, CancellationToken cancellationToken = default);
}

public class EntityRepository_old : IEntityRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<EntityRepository_old> _logger;

    public EntityRepository_old(AppDbContext db, ILogger<EntityRepository_old> logger)
    {
        _db = db;
        _logger = logger;
    }



    public async Task SaveTileAsync(Tile tile, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("💾 Saving tile at Q={Q}, R={R} (Owner={OwnerId}, Color={Color})",
            tile.Q, tile.R, tile.OwnerId, tile.Color);

        var existing = await _db.Tile.FirstOrDefaultAsync(
            t => t.Q == tile.Q && t.R == tile.R,
            cancellationToken
        );

        if (existing == null)
        {
            _logger.LogDebug("🆕 Inserting new tile at Q={Q}, R={R}", tile.Q, tile.R);
            _db.Tile.Add(tile);
        }
        else
        {
            _logger.LogDebug("♻️ Updating existing tile at Q={Q}, R={R}", tile.Q, tile.R);
            existing.Color = tile.Color;
            existing.OwnerId = tile.OwnerId;
            existing.LastUpdated = DateTimeOffset.UtcNow;
        }

        try
        {
            var changes = await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("✅ Tile saved successfully (Changes={Changes})", changes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error while saving tile at Q={Q}, R={R}", tile.Q, tile.R);
            throw;
        }
    }

    public async Task SavePlayerAsync(Player player, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("💾 Saving player (OwnerId={OwnerId}, Name={Name}, Color={Color})",
            player.OwnerId, player.Name, player.Color);

        var existing = await _db.Player.FirstOrDefaultAsync(
            p => p.OwnerId == player.OwnerId,
            cancellationToken
        );

        if (existing == null)
        {
            _logger.LogDebug("🧍 Adding new player (OwnerId={OwnerId})", player.OwnerId);
            player.LastUpdated = DateTimeOffset.UtcNow;
            _db.Player.Add(player);
        }
        else
        {
            _logger.LogDebug("🔄 Updating existing player (OwnerId={OwnerId})", player.OwnerId);
            existing.Color = player.Color;
            existing.Name = player.Name;
            existing.LastUpdated = DateTimeOffset.UtcNow;
        }

        try
        {
            var changes = await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("✅ Player saved successfully (Changes={Changes})", changes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error while saving player (OwnerId={OwnerId})", player.OwnerId);
            throw;
        }
    }
}
