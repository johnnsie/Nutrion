using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Nutrion.Lib.Database.Game.Entities;

[Table("Tile")]
public class Tile
{
    public int Id { get; set; }
    public int Q { get; set; }
    public int R { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

// Move the extension method to a non-generic static class
public static class TileExtensions
{
    public static Nutrion.Contracts.Tile ToContract(this Tile entity)
        => new(
            entity.Q,
            entity.R,
            entity.Color,
            0
        );
}