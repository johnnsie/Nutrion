using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.Lib.Messaging.DTO;

public class GameCommandMessage<TPayload>
{
    public string Type { get; set; } = string.Empty; // e.g. "tile.build"
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public TPayload Payload { get; set; } = default!;
}

public class BuildTileCommand
{
    public string OwnerId { get; set; } = string.Empty;
    public string BuildingType { get; set; } = string.Empty;
    public int Q { get; set; }
    public int R { get; set; }
    public string GLTFComponent { get; set; } = string.Empty;
}
