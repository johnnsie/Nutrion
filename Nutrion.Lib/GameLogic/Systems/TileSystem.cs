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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("⚙️ [TileSystem] ClaimTileAsync started for ({Q},{R}) owner={OwnerId}", tile.Q, tile.R, sessionId);

        try
        {
            // 1️⃣ Validate player
            _logger.LogDebug("🔍 Fetching player for SessionId {SessionId}", sessionId);
            var player = await _db.Player
                .Include(p => p.PlayerColor)
                .FirstOrDefaultAsync(p => p.OwnerId == sessionId, cancellationToken);

            if (player == null)
            {
                _logger.LogWarning("❌ No player found for SessionId {SessionId}", sessionId);
                return null;
            }

            _logger.LogDebug("✅ Player found: {PlayerName} (Id={PlayerId}, Color={Color})",
                player.Name, player.Id, player.PlayerColor?.HexCode ?? "#FFFFFF");

            // 2️⃣ Load the existing tile using Q/R (not Id)
            _logger.LogDebug("🔍 Searching tile at coordinates ({Q},{R})", tile.Q, tile.R);
            var existingTile = await _db.Tile
                .FirstOrDefaultAsync(t => t.Q == tile.Q && t.R == tile.R, cancellationToken);

            if (existingTile == null)
            {
                _logger.LogWarning("❌ No tile found at coordinates ({Q},{R})", tile.Q, tile.R);
                return null;
            }

            _logger.LogDebug("✅ Tile found: Id={TileId}, OwnerId={OwnerId}, PlayerId={PlayerId}",
                existingTile.Id, existingTile.OwnerId ?? "<none>", existingTile.PlayerId);

            // 3️⃣ Skip if already owned
            if (existingTile.PlayerId == player.Id)
            {
                _logger.LogDebug("⚠️ Tile ({Q},{R}) already belongs to player {PlayerName} (Id={PlayerId})",
                    existingTile.Q, existingTile.R, player.Name, player.Id);
                return null;
            }

            // 4️⃣ Assign ownership + color
            var color = player.PlayerColor?.HexCode ?? "#FFFFFF";
            _logger.LogInformation("🎨 Assigning tile ({Q},{R}) to player {PlayerName} (Color={Color})",
                existingTile.Q, existingTile.R, player.Name, color);

            existingTile.PlayerId = player.Id;
            existingTile.OwnerId = player.OwnerId;
            existingTile.Color = color;
            existingTile.LastUpdated = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("✅ Tile ({Q},{R}) successfully claimed by {PlayerName} in {Elapsed}ms.",
                existingTile.Q, existingTile.R, player.Name, stopwatch.ElapsedMilliseconds);

            return existingTile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Exception in ClaimTileAsync for ({Q},{R}) owner={OwnerId}", tile.Q, tile.R, sessionId);
            return null;
        }
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

        _logger.LogDebug("🔎 Tile ID {TileId} -> DB ID {DbTileId}", content.TileId, tile.Id);
        foreach (var c in tile.Contents)
        {
            _logger.LogDebug("   Content: {Type} (TileId={TileId}, FK={FK})",
                c.Type, c.TileId, c.Tile?.Id);
        }


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
