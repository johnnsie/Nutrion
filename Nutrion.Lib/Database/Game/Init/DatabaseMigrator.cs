using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database;
using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameWorker.Database;

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
            _logger.LogInformation("🌱  Seeding initial tiles...");
            var tiles = new List<Tile>();

            for (var q = 0; q < 10; q++)
            {
                for (var r = 0; r < 10; r++)
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

            await _db.Tile.AddRangeAsync(tiles, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("✅  Seed complete ({Count} tiles).", tiles.Count);
        }
    }
}