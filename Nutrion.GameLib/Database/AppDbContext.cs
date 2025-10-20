using Microsoft.EntityFrameworkCore;
using Nutrion.GameLib.Database.Entities;
using Nutrion.Lib.Database.Hydration;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nutrion.GameLib.Database;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<OutboxMessage> OutboxMessage { get; set; }
    public DbSet<Account> Account { get; set; }
    public DbSet<Player> Player { get; set; }
    public DbSet<Resource> Resource { get; set; }
    public DbSet<Tile> Tile { get; set; }
    public DbSet<Color> Color { get; set; }
    public DbSet<TileContent> TileContent { get; set; }
    public DbSet<Building> Building { get; set; }
    public DbSet<BuildingType> BuildingType { get; set; }
    public DbSet<BuildingCost> BuildingCost { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(o => new { o.ProcessedOn, o.Topic }); // For faster querying pending messages

        modelBuilder.Entity<Tile>()
            .HasMany(t => t.Contents)
            .WithOne(c => c.Tile)
            .HasForeignKey(c => c.TileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Building → OriginTile (1:1)
        modelBuilder.Entity<Building>()
            .HasOne(b => b.OriginTile)
            .WithMany()
            .HasForeignKey(b => b.OriginTileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Building → OccupiedTiles (1:many)
        modelBuilder.Entity<Building>()
            .HasMany(b => b.OccupiedTiles)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Player>()
            .HasOne(p => p.Color)
            .WithOne(c => c.Player)
            .HasForeignKey<Player>(p => p.ColorId)
            .OnDelete(DeleteBehavior.Cascade);


    }
}

[Table("OutboxMessage")]
public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    // Optional: store related aggregate/entity info for debugging
    [MaxLength(200)]
    public string? AggregateType { get; set; }

    [MaxLength(100)]
    public string? AggregateId { get; set; }
    public DateTime OccurredOn { get; init; }
    public DateTime? ProcessedOn { get; init; }

    // Optional: for retry and failure handling
    public int RetryCount { get; set; } = 0;
    public string? Error { get; init; }
}


