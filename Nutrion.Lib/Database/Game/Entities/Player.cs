using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace Nutrion.Lib.Database.Game.Entities;

[Table("Player")]
public class Player
{
    [Key]
    [JsonIgnore]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

// Move the extension method to a non-generic static class
public static class PlayerExtensions
{
    public static Nutrion.Contracts.Player ToContract(this Player entity)
        => new(
            entity.Id,
            entity.OwnerId,
            entity.Color,
            entity.Name,
            entity.LastUpdated.UtcDateTime
        );

    public static Player ToEntity(this Nutrion.Contracts.Player contract)
    => new()
    {
        Id = contract.Id,
        OwnerId = contract.OwnerId,
        Color = contract.Color,
        Name = contract.Name,
        LastUpdated = contract.LastUpdated
    };
}