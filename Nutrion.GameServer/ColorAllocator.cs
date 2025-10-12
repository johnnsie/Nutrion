using System.Collections.Concurrent;

namespace Nutrion.GameServer;
public class ColorAllocator
{
    private readonly string[] _palette = new[]
    {
        "#E91E63", "#9C27B0", "#03A9F4", "#4CAF50",
        "#FF9800", "#F44336", "#00BCD4", "#8BC34A",
        "#FFC107", "#FF5722", "#607D8B"
    };

    private int _next = 0;
    private readonly ConcurrentQueue<string> _available = new();

    public ColorAllocator()
    {
        foreach (var c in _palette)
            _available.Enqueue(c);
    }

    public string AssignColor()
    {
        if (_available.TryDequeue(out var color))
            return color;

        // fallback: rotate if we run out
        var idx = Interlocked.Increment(ref _next);
        return _palette[idx % _palette.Length];
    }

    public void ReleaseColor(string color)
    {
        _available.Enqueue(color);
    }
}