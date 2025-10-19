using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.GameLib.Database.Entities;

[ExportTsClass]
[Table("Account")]
public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Player Player { get; set; } = null!;

    public ICollection<Resource> Resources { get; set; } = new List<Resource>();

}
