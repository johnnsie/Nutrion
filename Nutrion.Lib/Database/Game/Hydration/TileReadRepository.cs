using Microsoft.Extensions.Logging;
using Nutrion.Contracts;
using Nutrion.Lib.Database.Game.Entities;
using Tile = Nutrion.Lib.Database.Game.Entities.Tile;

namespace Nutrion.Lib.Database.Game.Hydration;

public interface ITileReadRepository : IReadRepository<Tile>
{
    Task<Board> GetBoardAsync(CancellationToken cancellationToken = default);
}

public class TileReadRepository : ReadRepository<Tile>, ITileReadRepository
{
    public TileReadRepository(AppDbContext db, ILoggerFactory loggerFactory)
        : base(db, loggerFactory) { }

    public async Task<Board> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var tiles = await GetAllAsync(cancellationToken);
        var contractTiles = tiles
            .Select(t => new Nutrion.Contracts.Tile(t.Q, t.R, t.Color, 0))
            .ToList();

        return new Board(contractTiles);
    }
}
