using Microsoft.EntityFrameworkCore;
using Nutrion.Contracts;
using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Tile = Nutrion.Lib.Database.Game.Entities.Tile;

namespace Nutrion.Lib.Database.Game.Hydration;

public interface IReadOnlyRepository
{
    Task<List<Tile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Board> GetBoardAsync(CancellationToken cancellationToken = default);
}

public class ReadOnlyRepository : IReadOnlyRepository
{
    private readonly AppDbContext _db;

    public ReadOnlyRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Tile>> GetAllAsync(CancellationToken cancellationToken = default)
        => _db.Tile.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Board> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        // Query EF entities as no-tracking to keep it light
        var entities = await _db.Tile.AsNoTracking().ToListAsync(cancellationToken);

        // Project directly into your contract type
        var contractTiles = entities
            .Select(t => new Nutrion.Contracts.Tile(
                t.Q,
                t.R,
                t.Color,
                0 // Version or whatever logic you want here
            ))
            .ToList();

        return new Board(contractTiles);
    }

}
