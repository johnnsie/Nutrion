using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nutrion.Lib.Database.Game.Entities;
using Nutrion.Lib.Database.Game.Persistence;

namespace Nutrion.Lib.GameLogic.Systems;

public class PlayerSystem
{
    private readonly ILogger<PlayerSystem> _logger;
    private readonly EntityRepository _repo;


    public PlayerSystem(
        ILogger<PlayerSystem> logger,
        EntityRepository repo)
    {
        _logger = logger;
        _repo = repo;
    }

    public async Task<Player> GetOrCreateAsync(Player player, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("🔍 Checking if player with OwnerId '{OwnerId}' exists...", player.OwnerId);

        var existingPlayer = await _repo.Players.GetAsync(p => p.Name == player.Name, cancellationToken);
        if (existingPlayer != null)
        {
            _logger.LogInformation("✅ Found existing player '{PlayerName}' (OwnerId: {OwnerId})",
                existingPlayer.Name, player.OwnerId);
            return existingPlayer;
        }

        _logger.LogWarning("⚠️ No player found for OwnerId '{OwnerId}'. Creating a new player and account...", player.OwnerId);

        var newPlayer = new Player
        {
            OwnerId = player.OwnerId,
            Name = player.Name,
            Color = "#FFFFFF",
            LastUpdated = DateTimeOffset.UtcNow
        };

        _logger.LogDebug("🧱 Created new Player object: {PlayerName}, Color={Color}, Timestamp={Timestamp}",
            newPlayer.Name, newPlayer.Color, newPlayer.LastUpdated);

        var account = new Account { 
            Player = newPlayer,
            Resources = new List<Resource>
            {
                new Resource { Name = "Gold", Quantity = 100, Description = "Basic currency" },
                new Resource { Name = "Wood", Quantity = 50, Description = "Building material" },
                new Resource { Name = "Stone", Quantity = 50, Description = "Construction resource" }
            }
        };

        _logger.LogDebug("💰 Initialized Account with {ResourceCount} default resources for player '{PlayerName}'.",
            account.Resources.Count, newPlayer.Name);

        await _repo.Accounts.SaveAsync(account, a => a.Player.OwnerId == player.OwnerId, cancellationToken);

        _logger.LogInformation("🎉 Created new account and player successfully for OwnerId '{OwnerId}'", player.OwnerId);

        return newPlayer;
    }
}
