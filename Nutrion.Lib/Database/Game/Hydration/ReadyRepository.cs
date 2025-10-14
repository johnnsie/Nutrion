using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nutrion.Lib.Database.Game.Hydration;

public interface IReadRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}

public class ReadRepository<TEntity> : IReadRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext _db;
    protected readonly ILogger _logger;
    private readonly DbSet<TEntity> _set;

    public ReadRepository(AppDbContext db, ILoggerFactory loggerFactory)
    {
        _db = db;
        _logger = loggerFactory.CreateLogger($"{typeof(TEntity).Name}ReadRepository");
        _set = _db.Set<TEntity>();
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString()[..8];
        _logger.LogDebug("[{Id}] 🔍 GetAsync<{Entity}> started", correlationId, typeof(TEntity).Name);

        var entity = await _set.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

        if (entity == null)
            _logger.LogWarning("[{Id}] ❌ No {Entity} matched predicate", correlationId, typeof(TEntity).Name);
        else
            _logger.LogInformation("[{Id}] ✅ Found {Entity}: {@Entity}", correlationId, typeof(TEntity).Name, entity);

        _logger.LogDebug("[{Id}] 🏁 Completed GetAsync<{Entity}>", correlationId, typeof(TEntity).Name);
        return entity;
    }

    public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString()[..8];
        _logger.LogDebug("[{Id}] 📦 Fetching all {Entity} entities", correlationId, typeof(TEntity).Name);

        var result = await _set.AsNoTracking().ToListAsync(cancellationToken);
        _logger.LogInformation("[{Id}] ✅ Retrieved {Count} {Entity} records", correlationId, result.Count, typeof(TEntity).Name);

        return result;
    }

    public async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString()[..8];
        _logger.LogDebug("[{Id}] 🔎 Finding {Entity} with filter", correlationId, typeof(TEntity).Name);

        var result = await _set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
        _logger.LogInformation("[{Id}] ✅ Found {Count} matching {Entity} records", correlationId, result.Count, typeof(TEntity).Name);

        return result;
    }
}
