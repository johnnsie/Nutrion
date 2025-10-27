using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.GameLib.Database.Entities;

[ExportTsClass]
[Table("Resource")]
public class Resource
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string Description { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountId))]
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; } = null!;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(BuildingCostId))]
    public Guid? BuildingCostId { get; set; }
    public BuildingCost? BuildingCost { get; set; }

    // For resources found on the map
    public int? OriginTileId { get; set; }

    [ForeignKey(nameof(OriginTileId))]
    public Tile? OriginTile { get; set; } = null!;

    public string? GLTFModelPath { get; set; } = string.Empty;

    public ResourceType ResourceType { get; set; } = ResourceType.Generic;
}


public enum ResourceType
{
    Generic = 0,   // fallback
    Account = 1,   // belongs to player
    BuildingCost = 2,  // used as cost template
    MapResource = 3 // resource found on map
}