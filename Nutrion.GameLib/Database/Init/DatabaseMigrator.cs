﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database.Entities;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Hydration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameLib.Database.Init;

public interface IDatabaseMigrator
{
    /// <summary>
    /// Applies any pending migrations and optionally seeds initial data if the DB is empty.
    /// </summary>
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);
}

public class DatabaseMigrator : IDatabaseMigrator
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(AppDbContext db, ILogger<DatabaseMigrator> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Applies any pending migrations and optionally seeds initial data if the DB is empty.
    /// </summary>
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗄️  Applying database migrations...");
        await _db.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("✅  Migrations applied successfully.");

        await SeedBuildingTypes(cancellationToken);

        if (!await _db.Tile.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("🌱  Seeding initial hex tiles...");

            // Axial -> world conversion (same as your frontend)
            (double x, double z) TileToWorld(int q, int r, double radius = 1.0)
            {
                double w = radius * 1.5;
                double h = Math.Sqrt(3) * radius;
                double x = q * w;
                double z = r * h + (q % 2 == 0 ? 0 : h / 2);
                return (x, z);
            }

            int axialRadius = 120;         // search bounds for q/r loops
            double circleRadius = 90.0;    // world-space radius ≈ 10,000 tiles

            var tiles = new List<Tile>();

            for (int q = -axialRadius; q <= axialRadius; q++)
            {
                for (int r = -axialRadius; r <= axialRadius; r++)
                {
                    var (x, z) = TileToWorld(q, r);
                    double distance = Math.Sqrt(x * x + z * z);

                    if (distance <= circleRadius)
                    {
                        tiles.Add(new Tile
                        {
                            Q = q,
                            R = r,
                            OwnerId = "",
                            Color = "#000000",
                            LastUpdated = DateTimeOffset.UtcNow,
                            Player = null,
                            PlayerId = null,
                        });
                    }
                }
            }

            _logger.LogInformation("✅  Seeded {Count} circular hex tiles.", tiles.Count);

            await _db.Tile.AddRangeAsync(tiles, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("✅  Seed complete ({Count} hex tiles).", tiles.Count);
        }
    }

    public async Task SeedBuildingTypes(CancellationToken cancellationToken = default)
    {

        // Optional: seed initial buildings if empty
        if (!await _db.BuildingCost.AnyAsync(cancellationToken))
        {
            // Define the building costs
            var stationCost = new BuildingCost
            {
                Level = 1,
                LevelMultiplier = 2,
                RssImpact = new List<Resource>
        {
            new Resource { Name = "Energy", Quantity = +300 },
            new Resource { Name = "Metal", Quantity = -300 },
            new Resource { Name = "Fuel", Quantity = 0 },
            new Resource { Name = "Population", Quantity = +500 },
            new Resource { Name = "Food", Quantity = 0 },
        }
            };

            var factoryCost = new BuildingCost
            {
                Level = 1,
                LevelMultiplier = 2,
                RssImpact = new List<Resource>
        {
            new Resource { Name = "Energy", Quantity = +500 },
            new Resource { Name = "Metal", Quantity = -600 },
            new Resource { Name = "Fuel", Quantity = +300 },
            new Resource { Name = "Population", Quantity = -400 },
            new Resource { Name = "Food", Quantity = -200 },
        }
            };

            var turretCost = new BuildingCost
            {
                Level = 1,
                LevelMultiplier = 2,
                RssImpact = new List<Resource>
        {
            new Resource { Name = "Energy", Quantity = -100 },
            new Resource { Name = "Metal", Quantity = -200 },
            new Resource { Name = "Fuel", Quantity = 0 },
            new Resource { Name = "Population", Quantity = -100 },
            new Resource { Name = "Food", Quantity = -100 },
        }
            };

            // Add BuildingCosts first
            await _db.BuildingCost.AddRangeAsync(stationCost, factoryCost, turretCost);
            await _db.SaveChangesAsync(cancellationToken);

            // Then link them to their BuildingTypes
            var stationType = new BuildingType
            {
                Name = "Station",
                BuildingCostId = stationCost.Id,
                BuildingCost = stationCost,
                GLTFModelPath = "models/buildings/station.glb",
                Description = "A central hub for your operations, providing essential services and infrastructure.",
                TileRadius = 3,
        
            };

            var factoryType = new BuildingType
            {
                Name = "Factory",
                BuildingCostId = factoryCost.Id,
                BuildingCost = factoryCost,
                GLTFModelPath  = "models/buildings/factory.glb",
                TileRadius = 1,
                Description = "Produces goods and materials necessary for your empire's growth and sustainability.",

            };

            var turretType = new BuildingType
            {
                Name = "Turret",
                BuildingCostId = turretCost.Id,
                BuildingCost = turretCost,
                Description = "Defensive structure designed to protect your assets from enemy attacks.",
                GLTFModelPath  = "models/buildings/turret.glb",
                TileRadius = 0,
            };

            await _db.BuildingType.AddRangeAsync(stationType, factoryType, turretType);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Seeded initial BuildingType and BuildingCost data.");
        }


    }
}