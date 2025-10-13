using TypeGen.Core.SpecGeneration;
using TypeGen.Core.TypeAnnotations;

namespace Nutrion.Contracts;

public class GameContractsSpec : GenerationSpec
{
    public GameContractsSpec()
    {
        AddClass<Player>();
        AddClass<PlayerState>();
        AddClass<ResourceRate>();
    }
}


// Nutrion.Contracts/GameContracts.cs
public record GameEvent<T>(string Type, T Payload, DateTime Timestamp);

[ExportTsClass]
public record Player(
    Guid Id,
    string OwnerId,
    string Color,
    string Name,
    DateTime LastUpdated
);

[ExportTsClass]
public record Board(
    List<Tile> tiles
);

[ExportTsClass]
public record Tile(
    int q,
    int r,
    string? color,
    int version
);

[ExportTsClass]
public record PlayerState(
    string Id,
    string Name,
    int Resources,
    int PowerLevel
);

[ExportTsClass]
public record ResourceRate(
    string ResourceType,
    int RatePerHour,
    DateTime LastSync
);
