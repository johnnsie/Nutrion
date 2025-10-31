using Microsoft.EntityFrameworkCore;
using Nutrion.GameLib.Database;
using Nutrion.GameLib.Database.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nutrion.GameLib.TheDomain.Actions;

public sealed class ConstructBuildingAction : IGameAction<Building>
{
    public required string SessionId { get; init; }   // <-- Use session-based ownership
    public required Guid BuildingTypeId { get; init; }
    public required int TileId { get; init; }

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

        // Tile validation
        var tile = await db.Tile
            .Include(t => t.Contents)
            .FirstOrDefaultAsync(t => t.Id == TileId);

        if (tile == null)
            return ValidationResult.Fail("Invalid target tile.");

        // Rule 1: Tile must not contain another building
        if (tile.Contents.Any(c => c.Type == "Building" && c.Status != TileContentStatus.Destroyed))
            return ValidationResult.Fail("Tile already contains a building.");

        // Rule 2: Tile must not contain a resource
        if (tile.Contents.Any(c => c.Type == "Resource"))
            return ValidationResult.Fail("Tile contains a resource.");

        // Rule 3: Player must have enough resources
        if (buildingType.BuildingCost != null)
        {
            foreach (var costRes in buildingType.BuildingCost.RssImpact)
            {
                var playerRes = account.Resources
                    .FirstOrDefault(r => r.Name == costRes.Name);

                if (playerRes == null || playerRes.Quantity < costRes.Quantity)
                {
                    return ValidationResult.Fail(
                        $"Not enough {costRes.Name}: need {costRes.Quantity}, have {playerRes?.Quantity ?? 0}."
                    );
                }
            }
        }

        return ValidationResult.Success();
    }

    public async Task<Building> ExecuteAsync(AppDbContext db)
    {
        var account = await db.Account
            .Include(a => a.Player)
            .Include(a => a.Resources)
            .FirstAsync(a => a.Player.OwnerId == SessionId);

        var player = account.Player;
        var buildingType = await db.BuildingType
            .Include(bt => bt.BuildingCost)
                .ThenInclude(bc => bc.RssImpact)
            .FirstAsync(bt => bt.Id == BuildingTypeId);

        var tile = await db.Tile.Include(t => t.Contents)
            .FirstAsync(t => t.Id == TileId);

        // Deduct resources
        if (buildingType.BuildingCost != null)
        {
            foreach (var costRes in buildingType.BuildingCost.RssImpact)
            {
                var playerRes = account.Resources.First(r => r.Name == costRes.Name);
                playerRes.Quantity -= costRes.Quantity;
            }
        }

        // Create and persist the new building
        var building = new Building
        {
            PlayerOwner = player!,
            BuildingType = buildingType,
            Name = buildingType.Name,
            OriginTileId = tile.Id,
            LastUpdated = DateTimeOffset.UtcNow
        };

        db.Building.Add(building);
            
        // Create tile content for rendering
        var tileContent = new TileContent
        {
            TileId = tile.Id,
            GLTFComponent = buildingType.GLTFModelPath,
            Type = "Building",
            OwnerId = player!.OwnerId,
            Status = TileContentStatus.Building,
            Progress = 0
        };
        db.TileContent.Add(tileContent);

        await db.SaveChangesAsync();
        return building;
    }
}
