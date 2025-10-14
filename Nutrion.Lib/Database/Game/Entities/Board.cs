using System;
using System.Collections.Generic;
using System.Text;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.Lib.Database.Game.Entities;

public record GameEvent<T>(string Type, T Payload, DateTime Timestamp);

[ExportTsClass]
public class Board
{
    public Board(List<Tile> tiles)
    {
        Tiles = tiles;
    }

    public List<Tile> Tiles { get; set; } = new();
}