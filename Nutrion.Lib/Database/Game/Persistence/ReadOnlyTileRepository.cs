using Microsoft.EntityFrameworkCore;
using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.Lib.Database.Game.Persistence;

public interface IReadOnlyTileRepository
{
    Task<List<Tile>> GetAllAsync(CancellationToken cancellationToken = default);
}

public class ReadOnlyTileRepository : IReadOnlyTileRepository
{
    private readonly AppDbContext _db;

    public ReadOnlyTileRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Tile>> GetAllAsync(CancellationToken cancellationToken = default)
        => _db.Tile.AsNoTracking().ToListAsync(cancellationToken);
}
