using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TypeGen.Core.TypeAnnotations; // for [ExportTsClass]

namespace Nutrion.Lib.Database.Game.Entities;

[ExportTsClass]
[Table("Tile")]
public class Tile
{
    [Key]
    public int Id { get; set; }

    public int Q { get; set; }
    public int R { get; set; }

    // OwnerId is the external session identifier (SignalR / front end)
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    // Foreign key to Player entity
    [ForeignKey(nameof(Player))]
    public Guid? PlayerId { get; set; }

    public Player? Player { get; set; }

    public string Color { get; set; } = string.Empty;

    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<TileContent> Contents { get; set; } = new List<TileContent>();

}
