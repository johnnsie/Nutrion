using Nutrion.Data.Migrations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.GameLib.Database.Entities;

[ExportTsClass]
[Table("Building")]
public class Building
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey(nameof(PlayerOwnerId))]
    public Guid? PlayerOwnerId { get; set; }              
    public Player PlayerOwner { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    // --- New: deterministic link to the origin tile (single entry point)
    public int OriginTileId { get; set; }

    [ForeignKey(nameof(OriginTileId))]
    public Tile OriginTile { get; set; } = null!;

    public ICollection<Tile> OccupiedTiles { get; set; } = new List<Tile>();

    // Navigation
    [ForeignKey(nameof(BuildingType))]
    public Guid? BuildingTypeId { get; set; }
    public BuildingType? BuildingType { get; set; }
}

[ExportTsClass]
[Table("BuildingType")]
public class BuildingType
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int TileRadius { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public string GLTFModelPath { get; set; } = string.Empty;
    [ForeignKey(nameof(BuildingCost))]
    public Guid? BuildingCostId { get; set; }
    public BuildingCost? BuildingCost { get; set; }
}

[ExportTsClass]
[Table("BuildingCost")]
public class BuildingCost
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Level{ get; set; } = 1;
    public float LevelMultiplier{ get; set; } = 2;
    public List<Resource> RssImpact { get; set; } = new();
}