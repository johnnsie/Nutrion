using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database;
using Nutrion.Lib.Database;
using Nutrion.Lib.GameLogic.Systems;

namespace Nutrion.Lib.GameLogic.Engine;

public class GameTickService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GameTickService> _logger;

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(10);

    public GameTickService(IServiceScopeFactory scopeFactory, ILogger<GameTickService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🕹️ GameTickService started. Tick interval: {Seconds}s", TickInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var resourceSystem = scope.ServiceProvider.GetRequiredService<ResourceSystem>();

                var accounts = await db.Account
                    .Include(a => a.Player)
                    .Include(a => a.Resources)
                    .ToListAsync(stoppingToken);

                foreach (var account in accounts)
                {
                    await resourceSystem.ApplyResourceTickAsync(account, stoppingToken);
                }

                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("✅ Applied resource tick and saved {Count} accounts", accounts.Count);

                // Wait until next tick
                await Task.Delay(TickInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error during game tick execution cycle");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
