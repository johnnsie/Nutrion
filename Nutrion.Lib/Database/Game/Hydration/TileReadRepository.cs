using Microsoft.Extensions.Logging;
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
        var tiles = await GetAllAsync(null,cancellationToken);
        return new Board(tiles);
    }
}
