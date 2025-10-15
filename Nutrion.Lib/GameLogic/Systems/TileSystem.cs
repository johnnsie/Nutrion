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
            return null;
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

    /// <summary>
    /// Builds a new structure or unit on a tile. 
    /// The TileContent parameter should contain at least the Tile coordinates (Q,R) and Type.
    /// </summary>
    public async Task<TileContent?> BuildOnTileAsync(TileContent content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🏗️ Attempting to build {Type} on tile ({Q},{R})", content.Type, content.Tile?.Q, content.Tile?.R);

        // 1️⃣ Find the target tile
        var tile = await _db.Tile
            .Include(t => t.Contents)
            .FirstOrDefaultAsync(t => t.Q == content.Tile!.Q && t.R == content.Tile.R, cancellationToken);

        if (tile == null)
        {
            _logger.LogWarning("❌ Cannot build — no tile found at ({Q},{R})", content.Tile?.Q, content.Tile?.R);
            return null;
        }

        // 2️⃣ (Optional) Check ownership
        if (string.IsNullOrWhiteSpace(tile.OwnerId))
        {
            _logger.LogWarning("⚠️ Cannot build — tile ({Q},{R}) is unclaimed", tile.Q, tile.R);
            return null;
        }

        // 3️⃣ Prevent duplicate buildings of same type
        if (tile.Contents.Any(c => c.Type == content.Type && c.Status != TileContentStatus.Destroyed))
        {
            _logger.LogWarning("⛔ A {Type} already exists on tile ({Q},{R})", content.Type, tile.Q, tile.R);
            return null;
        }

        // 4️⃣ Create the new TileContent entity
        var newContent = new TileContent
        {
            TileId = tile.Id,
            GLTFComponent = content.GLTFComponent,
            Type = content.Type,
            OwnerId = tile.OwnerId,
            Status = TileContentStatus.Building,
            Progress = 0,
            LastUpdated = DateTimeOffset.UtcNow
        };

        _db.TileContent.Add(newContent);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ Created new {Type} on tile ({Q},{R}) with status {Status}", newContent.Type, tile.Q, tile.R, newContent.Status);

        return newContent;
    }
}
