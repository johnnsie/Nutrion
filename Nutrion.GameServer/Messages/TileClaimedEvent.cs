namespace Nutrion.GameServer.Messages;

public class TileClaimedEvent
{
    public string UserId { get; set; } = default!;
    public string Color { get; set; } = default!;
    public int Q { get; set; }
    public int R { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
