using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database;
using Nutrion.GameLib.Database.Entities;
using Nutrion.Lib.GameLogic.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nutrion.GameLib.TheDomain.Actions;

public sealed class ConstructBuildingAction : IGameAction<Building>
{
    public required string SessionId { get; init; }
    public required Guid BuildingTypeId { get; init; }
    public required int OriginTileId { get; init; }

    private readonly ILogger<ConstructBuildingAction>? _logger;

    public ConstructBuildingAction(ILogger<ConstructBuildingAction>? logger = null)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateAsync(AppDbContext db)
    {
        // Load account + player
        var account = await db.Account
            .Include(a => a.Player)
                .ThenInclude(p => p.Color)
            .Include(a => a.Resources)
            .FirstOrDefaultAsync(a => a.Player.OwnerId == SessionId);

        if (account == null)
            return ValidationResult.Fail("Account not found for session.");

        var player = account.Player;
        if (player == null)
            return ValidationResult.Fail("Player not found for account.");

        // Building type and cost
        var buildingType = await db.BuildingType
            .Include(bt => bt.BuildingCost)
                .ThenInclude(bc => bc.RssImpact)
            .FirstOrDefaultAsync(bt => bt.Id == BuildingTypeId);

        if (buildingType == null)
            return ValidationResult.Fail("Invalid building type.");

        var originTile = await db.Tile
            .Include(t => t.Contents)
            .FirstOrDefaultAsync(t => t.Id == OriginTileId);

        if (originTile == null)
            return ValidationResult.Fail("Invalid target tile.");

        // Rule 1: Tile must not contain another building
        if (originTile.Contents.Any(c => c.Type == "Building" && c.Status != TileContentStatus.Destroyed))
            return ValidationResult.Fail("Tile already contains a building.");

        // Rule 2: Tile must not contain a resource
        if (originTile.Contents.Any(c => c.Type == "Resource"))
            return ValidationResult.Fail("Tile contains a resource.");

        // Rule 3: Player must have enough resources
        if (buildingType.BuildingCost != null)
        {
            foreach (var costRes in buildingType.BuildingCost.RssImpact)
            {
                var playerRes = account.Resources.FirstOrDefault(r => r.Name == costRes.Name);
                if (playerRes == null || playerRes.Quantity < costRes.Quantity)
                    return ValidationResult.Fail($"Not enough {costRes.Name}: need {costRes.Quantity}, have {playerRes?.Quantity ?? 0}.");
            }
        }

        // Rule 1: Tile must not contain another building
        if (originTile.Contents.Any(c => c.Type == "Building" && c.Status != TileContentStatus.Destroyed))
            return ValidationResult.Fail("Tile already contains a building.");

        // Rule 2: Tile must not contain a resource content
        if (originTile.Contents.Any(c => c.Type == "Resource"))
            return ValidationResult.Fail("Tile contains a resource.");

        // Rule 3: Tile must not have a map resource entity
        bool hasMapResource = await db.Resource
            .AnyAsync(r => r.OriginTileId == originTile.Id && r.ResourceType == ResourceType.MapResource);
        if (hasMapResource)
            return ValidationResult.Fail("Cannot build on a tile that contains a map resource.");

        // Rule 4: Player must have enough resources
        if (buildingType.BuildingCost != null)
        {
            foreach (var costRes in buildingType.BuildingCost.RssImpact)
            {
                var playerRes = account.Resources.FirstOrDefault(r => r.Name == costRes.Name);
                if (playerRes == null || playerRes.Quantity < costRes.Quantity)
                    return ValidationResult.Fail(
                        $"Not enough {costRes.Name}: need {costRes.Quantity}, have {playerRes?.Quantity ?? 0}.");
            }
        }

        return ValidationResult.Success();

    }

    public async Task<Building> ExecuteAsync(AppDbContext db)
    {
        var account = await db.Account
            .Include(a => a.Player)
                .ThenInclude(p => p.Color)
            .Include(a => a.Resources)
            .FirstAsync(a => a.Player.OwnerId == SessionId);

        var player = account.Player;
        var buildingType = await db.BuildingType
            .Include(bt => bt.BuildingCost)
                .ThenInclude(bc => bc.RssImpact)
            .FirstAsync(bt => bt.Id == BuildingTypeId);

        var originTile = await db.Tile
            .Include(t => t.Contents)
            .FirstAsync(t => t.Id == OriginTileId);

        // Final safety: double-check no map resource now exists
        bool hasMapResource = await db.Resource
            .AnyAsync(r => r.OriginTileId == originTile.Id && r.ResourceType == ResourceType.MapResource);
        if (hasMapResource)
            throw new InvalidOperationException("Cannot build on a tile that contains a map resource.");


        // Deduct resources
        if (buildingType.BuildingCost != null)
        {
            foreach (var costRes in buildingType.BuildingCost.RssImpact)
            {
                var playerRes = account.Resources.First(r => r.Name == costRes.Name);
                playerRes.Quantity -= costRes.Quantity;
            }
        }

        // Determine affected tiles via hex radius
        var radius = buildingType.TileRadius;
        var coordsInRadius = HexHelper.GetHexCoordsInRadius(originTile.Q, originTile.R, radius).ToList();

        var allTiles = await db.Tile
            .Include(t => t.Contents)
            .Include(t => t.Players)
            .ToListAsync();

        var occupiedTiles = allTiles
            .Where(t => coordsInRadius.Any(c => c.Q == t.Q && c.R == t.R))
            .ToList();

        _logger?.LogDebug("📏 Building radius={Radius} affects {Count} tiles", radius, occupiedTiles.Count);

        List<Tile> moddedOccupiedTiles = new List<Tile>();

        // Apply tile effects (ownership + content)
        foreach (var tile in occupiedTiles)
        {
            if (tile.Id == originTile.Id)
            {
                var buildingContent = new TileContent
                {
                    TileId = tile.Id,
                    Type = "Building",
                    GLTFComponent = buildingType.GLTFModelPath ?? "Building_Default.glb",
                    OwnerId = player.OwnerId,
                    Status = TileContentStatus.Built,
                    Progress = 100,
                    LastUpdated = DateTimeOffset.UtcNow
                };
                db.TileContent.Add(buildingContent);

                _logger?.LogWarning(tile.Players.Any(p => p.OwnerId == player.OwnerId)
                    ? "⚠️ Tile ({Q},{R}) already owned by player {Player}"
                    : "ℹ️ Tile ({Q},{R}) now owned by player {Player}",
                    tile.Q, tile.R, player.Name);


                if (!tile.Players.Any(p => p.OwnerId == player.OwnerId))
                {
                    //tile.Players.Add(player);
                }
                tile.OwnerId = player.OwnerId;
                tile.Color = player.Color?.HexCode ?? "#FFFFFF";
            }
            else
            {
                var busyContent = new TileContent
                {
                    TileId = tile.Id,
                    Type = "Busy",
                    GLTFComponent = "owned.glb",
                    OwnerId = player.OwnerId,
                    Status = TileContentStatus.Built,
                    Progress = 100,
                    LastUpdated = DateTimeOffset.UtcNow
                };
                db.TileContent.Add(busyContent);

                _logger?.LogWarning(tile.Players.Any(p => p.OwnerId == player.OwnerId)
                    ? "⚠️ Tile ({Q},{R}) already owned by player {Player}"
                    : "ℹ️ Tile ({Q},{R}) now owned by player {Player}",
                    tile.Q, tile.R, player.Name);

                if (!tile.Players.Any(p => p.OwnerId == player.OwnerId))
                {
                    tile.Players.Add(player);
                }
                tile.OwnerId = player.OwnerId;
                tile.Color = player.Color?.HexCode ?? "#AAAAAA";
            }

            tile.LastUpdated = DateTimeOffset.UtcNow;
        }

        // Create Building
        var newBuilding = new Building
        {
            Id = Guid.NewGuid(),
            PlayerOwnerId = player.Id,
            PlayerOwner = player,
            Name = buildingType.Name,
            OriginTileId = originTile.Id,
            OriginTile = originTile,
            OccupiedTiles = occupiedTiles,
            BuildingTypeId = buildingType.Id,
            BuildingType = buildingType,
            LastUpdated = DateTimeOffset.UtcNow
        };

        db.Building.Add(newBuilding);

        await db.SaveChangesAsync();

        _logger?.LogInformation(
            "✅ Building '{Name}' created by {Player} at ({Q},{R}) — occupied {Count} tiles",
            buildingType.Name, player.Name, originTile.Q, originTile.R, occupiedTiles.Count
        );

        return newBuilding;
    }
}
