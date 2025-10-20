using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database;
using Nutrion.GameLib.Database.Entities;
using Nutrion.Lib.GameLogic.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nutrion.Lib.GameLogic.Validation
{
    public class BuildingValidator
    {
        private readonly ILogger<BuildingValidator> _logger;
        private readonly AppDbContext _db;

        public BuildingValidator(AppDbContext db, ILogger<BuildingValidator> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(bool IsValid, string? Reason)> ValidatePlacementAsync(
            Player player, BuildingType buildingType, Tile originTile, CancellationToken cancellationToken = default)
        {
            // 🧱 RULE 1: Origin tile must be unowned by any player
            if (originTile.PlayerId != null)
                return (false, $"Tile ({originTile.Q},{originTile.R}) already belongs to a player.");

            // 🧱 (optional fallback for legacy data)
            if (!string.IsNullOrWhiteSpace(originTile.OwnerId) &&
                !originTile.OwnerId.Equals("none", StringComparison.OrdinalIgnoreCase))
                return (false, $"Tile ({originTile.Q},{originTile.R}) already claimed via session '{originTile.OwnerId}'.");

            // 🧱 RULE 2: Origin tile must be empty of contents
            if (originTile.Contents.Any())
                return (false, $"Tile ({originTile.Q},{originTile.R}) not empty — it already contains objects.");

            // 🧱 RULE 3: Check radius overlap for nearby buildings
            var allTiles = await _db.Tile
                .Include(t => t.Contents)
                .ToListAsync(cancellationToken);

            var tilesInRadius = allTiles
                .Where(t => HexHelper.WithinRadius(originTile.Q, originTile.R, t.Q, t.R, buildingType.TileRadius))
                .ToList();

            foreach (var t in tilesInRadius)
            {
                if (t.Contents.Any(c => c.Type == "Building" && c.Status != TileContentStatus.Destroyed))
                    return (false, $"Tile ({t.Q},{t.R}) within radius already occupied by another building.");
            }

            return (true, null);
        }




        public async Task<(bool IsValid, string? Reason)> ValidateResourcesAsync(
            Player player, BuildingType buildingType, CancellationToken cancellationToken = default)
        {
            if (buildingType.BuildingCost == null || buildingType.BuildingCost.RssImpact.Count == 0)
                return (true, null); // no cost = valid

            var account = await _db.Account
                .Include(a => a.Resources)
                .FirstOrDefaultAsync(a => a.Player.Id == player.Id, cancellationToken);

            if (account == null)
                return (false, $"Player {player.Name} has no account for resource validation");

            foreach (var cost in buildingType.BuildingCost.RssImpact)
            {
                var res = account.Resources.FirstOrDefault(r => r.Name == cost.Name);
                if (res == null || res.Quantity < cost.Quantity)
                    return (false, $"Not enough {cost.Name}: have {res?.Quantity ?? 0}, need {cost.Quantity}");
            }

            return (true, null);
        }
    }
}
