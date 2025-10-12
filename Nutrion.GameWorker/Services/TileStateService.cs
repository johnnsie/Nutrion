using Nutrion.GameWorker.Persistence;
using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameWorker.Services;

public class TileStateService
{
    private readonly ILogger<TileStateService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<(int q, int r), string> _tiles = new();

    public TileStateService(ILogger<TileStateService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> ClaimTileAsync(string userId, string color, int q, int r, CancellationToken cancellationToken = default)
    {
        var key = (q, r);
        var changed = !_tiles.TryGetValue(key, out var existingColor) || existingColor != color;

        if (!changed)
        {
            _logger.LogDebug("No change detected for tile ({Q},{R})", q, r);
            return false;
        }

        _tiles[key] = color;

        // Create a scope to access scoped services (DbContext, repository)
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITileRepository>();

        await repo.SaveTileAsync(new Tile
        {
            Q = q,
            R = r,
            Color = color,
            OwnerId = userId,
            LastUpdated = DateTimeOffset.UtcNow
        }, cancellationToken);

        _logger.LogInformation("Persisted tile ({Q},{R}) as {Color}", q, r, color);
        return true;
    }
}
