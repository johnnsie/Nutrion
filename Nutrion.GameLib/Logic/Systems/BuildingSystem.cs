using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database;
using Nutrion.GameLib.Database.Entities;
using Nutrion.GameLib.Database.EntityRepository;
using Nutrion.GameLib.TheDomain;
using Nutrion.GameLib.TheDomain.Actions;
using Nutrion.Lib.Database;
using Nutrion.Lib.GameLogic.Helpers;
using Nutrion.Lib.GameLogic.Validation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TypeGen.Core.Logging;

namespace Nutrion.Lib.GameLogic.Systems;

public interface IBuildingSystem
{
    Task<Building?> CreateBuildingAsync(
        string sessionId,
        Guid buildingTypeId,
        int originTileId,
        CancellationToken cancellationToken = default);

    Task<Building?> CreateBuildingActionAsync(string sessionId, Guid buildingTypeId, int tileId);
}

public class BuildingSystem : IBuildingSystem
{
    private readonly ILogger<BuildingSystem> _logger;
    private readonly AppDbContext _db;
    private readonly EntityRepository _repo;
    private readonly BuildingValidator _validator;
    private readonly GameActionService _gameActionService;

    public async Task<Building?> CreateBuildingActionAsync(string sessionId, Guid buildingTypeId, int tileId)
    {
        var constructAction = new ConstructBuildingAction
        {
            SessionId = sessionId,
            BuildingTypeId = buildingTypeId,
            TileId = tileId
        };

        var result = await _gameActionService.ExecuteAsync(constructAction);

        if (!result.Success)
            _logger.LogWarning("Building failed: {Msg}", result.Message);
        else
            _logger.LogInformation("Building successfully constructed.");

        return result.Data;
    }

    public BuildingSystem(
        ILogger<BuildingSystem> logger,
        EntityRepository repo,
        AppDbContext db,
        BuildingValidator validator)
    {
        _logger = logger;
        _repo = repo;
        _db = db;
        _validator = validator;
    }

    public async Task<Building?> CreateBuildingAsync(
        string sessionId,
        Guid buildingTypeId,
        int originTileId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🏗️ [BuildingSystem] CreateBuildingAsync -> session={SessionId}, type={TypeId}, tile={TileId}",
            sessionId, buildingTypeId, originTileId);

        try
        {
            var account = await _db.Account
                .Include(a => a.Player)
                    .ThenInclude(p => p.Color)
                .Include(a => a.Resources)
                .FirstOrDefaultAsync(a => a.Player.OwnerId == sessionId, cancellationToken);

            // 1️⃣ Load player and building type
            var player = account?.Player;
                
            if (player == null) return LogAndReturn("Player not found");

            var buildingType = await _db.BuildingType
                .Include(bt => bt.BuildingCost)
                    .ThenInclude(bc => bc.RssImpact)
                .FirstOrDefaultAsync(bt => bt.Id == buildingTypeId, cancellationToken);
            if (buildingType == null) return LogAndReturn("BuildingType not found");

            var originTile = await _db.Tile
                .Include(t => t.Contents)
                .FirstOrDefaultAsync(t => t.Id == originTileId, cancellationToken);
            if (originTile == null) return LogAndReturn("Origin tile not found");

            // 2️⃣ Validate resources and placement
            var (resValid, resReason) = await _validator.ValidateResourcesAsync(player, buildingType, cancellationToken);
            if (!resValid) return LogAndReturn(resReason);

            var (placeValid, placeReason) = await _validator.ValidatePlacementAsync(player, buildingType, originTile, cancellationToken);
            if (!placeValid) return LogAndReturn(placeReason);

            foreach (var cost in buildingType.BuildingCost!.RssImpact)
            {
                var res = account!.Resources.First(r => r.Name == cost.Name);
                res.Quantity -= cost.Quantity;
            }

            // 4️⃣ Find all tiles in the radius (exact hex pattern)
            var radius = buildingType.TileRadius;
            var coordsInRadius = HexHelper.GetHexCoordsInRadius(originTile.Q, originTile.R, radius).ToList();

            var allTiles = await _db.Tile
                .Include(t => t.Contents)
                .ToListAsync(cancellationToken);

            var occupiedTiles = allTiles
                .Where(t => coordsInRadius.Any(c => c.Q == t.Q && c.R == t.R))
                .ToList();

            _logger.LogDebug("📏 Building radius={Radius} affects {Count} tiles", radius, occupiedTiles.Count);

            // 5️⃣ Create Building entity
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
            _db.Building.Add(newBuilding);

            // 6️⃣ Mark origin + radius contents
            foreach (var tile in occupiedTiles)
            {
                // origin tile = actual building
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
                    _db.TileContent.Add(buildingContent);

                    // Auto-claim this tile
                    tile.PlayerId = player.Id;
                    tile.OwnerId = player.OwnerId;
                    tile.Color = player.Color?.HexCode ?? "#FFFFFF";
                }
                else
                {
                    // surrounding tiles become "Busy"
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
                    _db.TileContent.Add(busyContent);

                    // Optionally mark tile as temporarily owned by same player
                    tile.PlayerId = player.Id;
                    tile.OwnerId = player.OwnerId;
                    tile.Color = player.Color?.HexCode ?? "#AAAAAA";
                }

                tile.LastUpdated = DateTimeOffset.UtcNow;
            }

            // 7️⃣ Save all changes
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "✅ Building '{Name}' created by {Player} at ({Q},{R}) — occupied {Count} tiles",
                buildingType.Name, player.Name, originTile.Q, originTile.R, occupiedTiles.Count
            );

            return newBuilding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error creating building for {SessionId}", sessionId);
            return null;
        }

        Building? LogAndReturn(string? reason)
        {
            _logger.LogWarning("⛔ Build failed: {Reason}", reason);
            return null;
        }
    }


}

