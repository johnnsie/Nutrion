using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.Lib.Database.Game.Entities;

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

    // Foreign key to Account
    [ForeignKey(nameof(AccountId))]
    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;
}