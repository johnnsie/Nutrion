using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.Lib.Database.Game.Entities;

[ExportTsClass]
[Table("Player")]
public class Player
{
    [Key]
    [JsonIgnore]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    // Navigation back to color
    public PlayerColor? PlayerColor { get; set; }
}
