using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.GameLogic.Rules;

namespace Nutrion.Lib.GameLogic.Systems;

public class ResourceSystem
{
    private readonly ILogger<ResourceSystem> _logger;

    public ResourceSystem(ILogger<ResourceSystem> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Applies resource regeneration logic based on how long it has been
    /// since the player's last update (using UTC timestamps).
    /// </summary>
    public Task<Account> ApplyResourceTickAsync(Account account, CancellationToken ct = default)
    {
        var player = account.Player;

        // Default to a minimal interval if never updated before
        var lastUpdate = player.LastUpdated == default
            ? DateTime.UtcNow
            : player.LastUpdated.ToUniversalTime();

        var now = DateTime.UtcNow;
        var delta = now - lastUpdate;

        // Ignore negative or zero intervals (clock drift or same-tick)
        if (delta <= TimeSpan.Zero)
            return Task.FromResult(account);

        foreach (var resource in account.Resources)
        {
            //if (!ResourceRules.RegenerationRatesPerMinute.TryGetValue(resource.Name, out var rate))
            //    continue;

            var bonus = ResourceRules.GetBonus(player);
            var gain = (int)(1 * delta.TotalMinutes * bonus);

            resource.Quantity = resource.Quantity * 2;
            //if (gain <= 0)
            //     continue;

            var oldQuantity = resource.Quantity;
            resource.Quantity = Math.Min(
                ResourceRules.MaxQuantities[resource.Name],
                resource.Quantity + gain);

            _logger.LogDebug(
                "⏱️ {ResName}: +{Gain} (old={Old}, new={New}) for player {Player}, Δt={DeltaMinutes:F1}min",
                resource.Name,
                gain,
                oldQuantity,
                resource.Quantity,
                player.Name,
                delta.TotalMinutes
            );
        }

        // Update timestamp
        player.LastUpdated = now;

        return Task.FromResult(account);
    }
}
