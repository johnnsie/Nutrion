using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.GameLib.Database.Entities;

[ExportTsClass]
[Table("Color")]
public class Color
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string HexCode { get; set; } = default!;
    
    [ForeignKey(nameof(PlayerId))]
    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }
}
