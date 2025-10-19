using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.GameLib.Database.Entities;

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

    public Player? Player { get; set; }   // navigation
}
