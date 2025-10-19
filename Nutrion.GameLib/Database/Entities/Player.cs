﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.GameLib.Database.Entities;

[ExportTsClass]
[Table("Player")]
public class Player
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public int? PlayerColorId { get; set; }
    public PlayerColor? PlayerColor { get; set; }
}
