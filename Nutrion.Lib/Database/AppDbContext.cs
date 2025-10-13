using Microsoft.EntityFrameworkCore;
using Nutrion.Lib.Database.Game.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nutrion.Lib.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<OutboxMessage> OutboxMessage { get; set; }
    public DbSet<OpenAIRequest> OpenAIRequest { get; set; }

    public DbSet<Tile> Tile => Set<Tile>();
    public DbSet<Player> Player { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(o => new { o.ProcessedOn, o.Topic }); // For faster querying pending messages
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

public enum MessageStatus {
    Init,
    InProgress,
    Processed,
    Failed
}

public class OpenAIRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public MessageStatus Status { get; set; } = MessageStatus.Init;

    public string Model { get; set; } = string.Empty;

    // JSON blob of the messages array
    public string MessagesJson { get; set; } = string.Empty;

    public string ReplyMessage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}


public class MealPlanRequest
{
    public string Style { get; set; } = string.Empty;
    public int MealsPerDay { get; set; }
    public int NumPeople { get; set; }
}

public class CustomiseMealPlanRequest : MealPlanRequest
{
    public List<string> AdditionalFood { get; set; } = new();
    public List<string> RemoveFood { get; set; } = new();
    public string? OriginalMealPlanJson { get; set; } // Holds the initial full meal plan JSON
}

public class RecipePromptTemplate
{
    public string? Title { get; set; }
    public string? BaseTemplate { get; set; }
}

