using Nutrion.Lib.Database.Game.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

[ExportTsClass]
[Table("PlayerColor")]
public class PlayerColor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string HexCode { get; set; } = default!;

    // Optional: track who is using this color
    [ForeignKey(nameof(Player))]
    public Guid? PlayerId { get; set; }   // nullable = available in palette

    [JsonIgnore] // 🚀 Prevent circular reference
    public Player? Player { get; set; }   // navigation
}
