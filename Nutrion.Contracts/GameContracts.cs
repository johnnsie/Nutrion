namespace Nutrion.Contracts;

// Nutrion.Contracts/GameContracts.cs
public record Envelope<T>(string Type, T Payload, DateTime Timestamp);

public record TileUpdated(
    string Id,
    string OwnerId,
    int ProductionRate
);

public record PlayerState(
    string Id,
    string Name,
    int Resources,
    int PowerLevel
);

public record ResourceRate(
    string ResourceType,
    int RatePerHour,
    DateTime LastSync
);
