using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Game.Entities;

namespace Nutrion.Lib.GameLogic.Systems;

public class TileSystem
{
    private readonly AppDbContext _db;
    private readonly ILogger<TileSystem> _logger;

    public TileSystem(AppDbContext db, ILogger<TileSystem> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Claims a tile for the player matching the given sessionId.
    /// Updates ownership and color if the tile does not already belong to that player.
    /// </summary>
    public async Task<Tile?> ClaimTileAsync(string sessionId, Tile tile, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("🧩 Player {SessionId} attempting to claim tile ({Q},{R})", sessionId, tile.Q, tile.R);

        // 1️⃣ Validate player
        var player = await _db.Player
            .Include(p => p.PlayerColor)
            .FirstOrDefaultAsync(p => p.OwnerId == sessionId, cancellationToken);

        if (player == null)
        {
            _logger.LogWarning("❌ No player found for SessionId {SessionId}", sessionId);
            throw new InvalidOperationException("Player not found for session");
        }

        // 2️⃣ Load the existing tile using Q/R (not Id)
        var existingTile = await _db.Tile
            .FirstOrDefaultAsync(t => t.Q == tile.Q && t.R == tile.R, cancellationToken);


        if (existingTile == null)
        {
            _logger.LogWarning("❌ No tile found at coordinates ({Q},{R})", tile.Q, tile.R);
            return null;
        }

        // 3️⃣ Skip if already owned
        if (existingTile.PlayerId == player.Id)
        {
            _logger.LogDebug("✅ Tile ({Q},{R}) already belongs to player {PlayerName}", existingTile.Q, existingTile.R, player.Name);
            return null;
        }

        // 4️⃣ Assign ownership + color
        _logger.LogInformation("🎨 Assigning tile ({Q},{R}) to player {PlayerName} ({Color})",
            existingTile.Q, existingTile.R, player.Name, player.PlayerColor?.HexCode ?? "#FFFFFF");

        existingTile.PlayerId = player.Id;
        existingTile.OwnerId = player.OwnerId;
        existingTile.Color = player.PlayerColor?.HexCode ?? "#FFFFFF";
        existingTile.LastUpdated = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ Tile ({Q},{R}) successfully claimed by {PlayerName}.", existingTile.Q, existingTile.R, player.Name);

        return existingTile;
    }

}
