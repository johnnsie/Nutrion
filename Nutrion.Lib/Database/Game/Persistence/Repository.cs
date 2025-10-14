using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nutrion.Lib.Database.Game.Persistence;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task SaveAsync(TEntity entity, Expression<Func<TEntity, bool>> match, CancellationToken cancellationToken = default);
}

public interface ITileCustomDemoRepository : IRepository<Tile> { }

public class TileCustomDemoRepository : Repository<Tile>, ITileCustomDemoRepository
{
    public TileCustomDemoRepository(AppDbContext db, ILoggerFactory loggerFactory)
        : base(db, loggerFactory) { }
}

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext _db;
    protected readonly ILogger _logger;
    private readonly DbSet<TEntity> _set;

    public Repository(AppDbContext db, ILoggerFactory loggerFactory)
    {
        _db = db;
        _logger = loggerFactory.CreateLogger($"{typeof(TEntity).Name}Repository");
        _set = _db.Set<TEntity>();
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString()[..8];
        _logger.LogDebug("[{Id}] 🔍 Starting GetAsync<{Entity}>", correlationId, typeof(TEntity).Name);

        var result = await _set.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
        if (result == null)
            _logger.LogWarning("[{Id}] ❌ No {Entity} matched predicate.", correlationId, typeof(TEntity).Name);
        else
            _logger.LogInformation("[{Id}] ✅ Found {Entity}: {@Entity}", correlationId, typeof(TEntity).Name, result);

        _logger.LogDebug("[{Id}] 🏁 Completed GetAsync<{Entity}>", correlationId, typeof(TEntity).Name);
        return result;
    }

    public async Task SaveAsync(TEntity entity, Expression<Func<TEntity, bool>> match, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString()[..8];
        _logger.LogInformation("[{Id}] 💾 Starting SaveAsync<{Entity}>: {@Entity}", correlationId, typeof(TEntity).Name, entity);

        try
        {
            var existing = await _set.AsTracking().FirstOrDefaultAsync(match, cancellationToken);
            var operation = existing == null ? "Insert" : "Update";

            if (existing == null)
            {
                _logger.LogInformation("[{Id}] 🆕 Adding new {Entity}", correlationId, typeof(TEntity).Name);
                _set.Add(entity);
            }
            else
            {
                _logger.LogInformation("[{Id}] ♻️ Updating existing {Entity}", correlationId, typeof(TEntity).Name);

                // Show field-by-field differences
                var props = typeof(TEntity)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite)
                    .ToList();

                bool anyChange = false;
                foreach (var prop in props)
                {
                    var oldVal = prop.GetValue(existing);
                    var newVal = prop.GetValue(entity);

                    if (!Equals(oldVal, newVal))
                    {
                        anyChange = true;
                        _logger.LogDebug("[{Id}] 🔄 {Entity}.{Property} changed: '{Old}' → '{New}'",
                            correlationId, typeof(TEntity).Name, prop.Name,
                            oldVal ?? "(null)", newVal ?? "(null)");
                    }
                }

                if (!anyChange)
                    _logger.LogDebug("[{Id}] ⚪ No field changes detected for {Entity}", correlationId, typeof(TEntity).Name);

                // Get key property names so we can skip them
                var keyProps = _db.Model.FindEntityType(typeof(TEntity))?
                    .FindPrimaryKey()?
                    .Properties
                    .Select(p => p.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

                // Copy non-key fields only
                foreach (var property in _db.Entry(existing).Properties)
                {
                    if (keyProps.Contains(property.Metadata.Name))
                        continue; // skip Id or composite key fields

                    var newValue = property.Metadata.PropertyInfo?.GetValue(entity);
                    property.CurrentValue = newValue;
                }

            }

            var changes = await _db.SaveChangesAsync(cancellationToken);
            if (changes > 0)
                _logger.LogInformation("[{Id}] ✅ {Entity} {Operation} succeeded (Changes={Changes})",
                    correlationId, typeof(TEntity).Name, operation, changes);
            else
                _logger.LogWarning("[{Id}] ⚠️ {Entity} {Operation} made no changes",
                    correlationId, typeof(TEntity).Name, operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Id}] 💥 SaveAsync<{Entity}> failed: {Error}", correlationId, typeof(TEntity).Name, ex.Message);
            throw;
        }
        finally
        {
            _logger.LogDebug("[{Id}] 🏁 Completed SaveAsync<{Entity}>", correlationId, typeof(TEntity).Name);
        }
    }
}
