using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database;
using Nutrion.GameLib.Database.Entities;
using Nutrion.GameLib.Database.EntityRepository;
using Nutrion.Lib.Database;
using System.Drawing;

namespace Nutrion.Lib.GameLogic.Systems;

public class PlayerSystem
{
    private readonly ILogger<PlayerSystem> _logger;
    private readonly AppDbContext _db;
    private readonly EntityRepository _repo;
    private readonly Random _random = new();

    public PlayerSystem(
        ILogger<PlayerSystem> logger,
        EntityRepository repo,
        AppDbContext db)
    {
        _logger = logger;
        _repo = repo;
        _db = db;
    }

    public async Task<Player> GetOrCreateAsync(Player player, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("🔍 Checking if player with OwnerId '{OwnerId}' exists...", player.OwnerId);

        var existingPlayer = await _repo.Players.GetAsync(p => p.OwnerId == player.OwnerId, cancellationToken);
        if (existingPlayer != null)
        {
            _logger.LogInformation("✅ Found existing player '{PlayerName}' (OwnerId: {OwnerId})",
                existingPlayer.Name, player.OwnerId);
            return existingPlayer;
        }

        _logger.LogWarning("⚠️ No player found for OwnerId '{OwnerId}'. Creating a new player and account...", player.OwnerId);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1️⃣ Create new player
            var newPlayer = new Player
            {
                OwnerId = player.OwnerId,
                Name = player.Name,
                LastUpdated = DateTimeOffset.UtcNow
            };

            await _db.Player.AddAsync(newPlayer, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            // 2️⃣ Generate a random, unused color
            string uniqueColor = await GenerateUniqueColorAsync(cancellationToken);

            // 3️⃣ Create PlayerColor entry
            var playerColor = new PlayerColor
            {
                HexCode = uniqueColor,
                PlayerId = newPlayer.Id
            };

            await _db.PlayerColor.AddAsync(playerColor, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            // 4️⃣ Create associated Account
            var account = new Account
            {
                Player = newPlayer,
                Resources = new List<Resource>
                {
                    new Resource { Name = "Gold", Quantity = 100, Description = "Basic currency" },
                    new Resource { Name = "Wood", Quantity = 50, Description = "Building material" },
                    new Resource { Name = "Stone", Quantity = 50, Description = "Construction resource" }
                }
            };

            await _db.Account.AddAsync(account, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("🎉 Created new player '{PlayerName}' with random color {Color}.",
                newPlayer.Name, uniqueColor);

            return newPlayer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create new player '{PlayerName}'. Rolling back transaction.", player.Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Generates a random color (#RRGGBB) that doesn't exist in PlayerColor table.
    /// </summary>
    private async Task<string> GenerateUniqueColorAsync(CancellationToken ct)
    {
        const int maxAttempts = 20;

        for (int i = 0; i < maxAttempts; i++)
        {
            var color = $"#{_random.Next(0x1000000):X6}"; // #RRGGBB
            bool exists = await _db.PlayerColor.AnyAsync(c => c.HexCode == color, ct);

            if (!exists)
            {
                _logger.LogDebug("🎨 Generated unique player color: {Color}", color);
                return color;
            }
        }

        throw new InvalidOperationException("Failed to generate a unique color after several attempts.");
    }
}
