using Microsoft.EntityFrameworkCore;
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

        // Optional: seed initial board if empty
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

            int radius = 60;              // adjust until ~2000 tiles
            double circleRadius = 40.0;   // world-space radius

            var tiles = new List<Tile>();

            for (int q = -radius; q <= radius; q++)
            {
                for (int r = -radius; r <= radius; r++)
                {
                    var (x, z) = TileToWorld(q, r);
                    double distance = Math.Sqrt(x * x + z * z);

                    if (distance <= circleRadius)
                    {
                        tiles.Add(new Tile
                        {
                            Q = q,
                            R = r,
                            OwnerId = "none",
                            Color = "#000000",
                            LastUpdated = DateTimeOffset.UtcNow
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
}