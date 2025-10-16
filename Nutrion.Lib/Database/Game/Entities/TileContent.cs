using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.Lib.Database.Game.Entities;

[ExportTsClass]
[Table("TileContent")]
public class TileContent
{
    [Key]
    [JsonIgnore]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key to parent Tile
    [Required]
    [ForeignKey(nameof(Tile))]
    public int TileId { get; set; }

    public Tile Tile { get; set; } = null!;

    /// <summary>
    /// The GLTF component name or path used by the front-end to render this content.
    /// </summary>
    [Required]
    public string GLTFComponent { get; set; } = string.Empty;

    /// <summary>
    /// Logical category/type of content (e.g. Building, Unit, Decoration)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Optional owner, allows shared or neutral content.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Current status of the content (building, built, destroyed).
    /// </summary>
    [Required]
    public TileContentStatus Status { get; set; } = TileContentStatus.Building;

    /// <summary>
    /// Optional progress (0–100%) for construction or destruction.
    /// </summary>
    public int Progress { get; set; } = 0;

    /// <summary>
    /// Time when the content was created or last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

[ExportTsEnum]
public enum TileContentStatus
{
    Building = 0,
    Built = 1,
    Destroyed = 2
}
