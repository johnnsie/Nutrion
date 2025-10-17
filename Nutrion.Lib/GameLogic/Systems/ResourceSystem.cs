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

        _logger.LogInformation("⚙️ Starting resource tick for player {PlayerName} ({PlayerId})", player.Name, player.OwnerId);

        // Default to a minimal interval if never updated before
        var lastUpdate = player.LastUpdated == default
            ? DateTime.UtcNow
            : player.LastUpdated.ToUniversalTime();

        var now = DateTime.UtcNow;
        var delta = now - lastUpdate;

        _logger.LogDebug("🕒 LastUpdated={LastUpdated:o}, Now={Now:o}, Δt={DeltaSeconds:F1}s", lastUpdate, now, delta.TotalSeconds);

        // Ignore negative or zero intervals (clock drift or same-tick)
        if (delta <= TimeSpan.Zero)
        {
            _logger.LogWarning(
                "⏭️ Skipping resource tick for player {PlayerName} because Δt={DeltaSeconds:F3}s (zero or negative)",
                player.Name,
                delta.TotalSeconds
            );
            return Task.FromResult(account);
        }

        // Apply regeneration for each resource
        foreach (var resource in account.Resources)
        {
            if (!ResourceRules.RegenerationRatesPerMinute.TryGetValue(resource.Name, out var rate))
            {
                _logger.LogWarning(
                    "⚠️ No regeneration rate defined for resource {ResourceName}. Skipping.",
                    resource.Name
                );
                continue;
            }

            var bonus = 1;
            var gain = (int)(rate * delta.TotalSeconds * bonus);

            if (gain <= 0)
            {
                _logger.LogTrace(
                    "🔸 Resource {ResourceName} gained 0 this tick (rate={Rate}/min, bonus={Bonus:F2}, Δt={DeltaMinutes:F2}min)",
                    resource.Name,
                    rate,
                    bonus,
                    delta.TotalMinutes
                );
                continue;
            }

            var oldQuantity = resource.Quantity;
            var newQuantity = Math.Min(ResourceRules.MaxQuantities[resource.Name], resource.Quantity + gain);
            resource.Quantity = newQuantity;

            _logger.LogInformation(
                "💰 Resource {ResourceName}: +{Gain} (old={Old}, new={New}, cap={Cap}) for player {PlayerName}, rate={Rate}/min, bonus={Bonus:F2}, Δt={DeltaMinutes:F2}min",
                resource.Name,
                gain,
                oldQuantity,
                newQuantity,
                ResourceRules.MaxQuantities[resource.Name],
                player.Name,
                rate,
                bonus,
                delta.TotalMinutes
            );
        }

        // Update timestamp
        player.LastUpdated = now;

        _logger.LogInformation(
            "✅ Completed resource tick for player {PlayerName}. Next baseline timestamp: {Timestamp:o}",
            player.Name,
            now
        );

        return Task.FromResult(account);
    }
}
