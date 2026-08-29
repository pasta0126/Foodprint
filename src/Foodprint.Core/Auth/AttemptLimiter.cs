using System.Collections.Concurrent;

namespace Foodprint.Core.Auth;

/// <summary>
/// Fixed-window attempt limiter, partitioned by an arbitrary key (IP, email, ...).
/// In-memory: correct for a single instance, which is all void-server runs.
/// </summary>
public interface IAttemptLimiter
{
    /// <summary>Records an attempt for <paramref name="key"/>; returns false once the window limit is exceeded.</summary>
    bool TryRecord(string key, int limit, TimeSpan window);

    void Reset(string key);
}

public sealed class InMemoryAttemptLimiter(TimeProvider clock) : IAttemptLimiter
{
    private readonly ConcurrentDictionary<string, Window> _windows = new();

    public bool TryRecord(string key, int limit, TimeSpan window)
    {
        var now = clock.GetUtcNow();
        var entry = _windows.AddOrUpdate(
            key,
            _ => new Window(now, 1),
            (_, w) => now - w.Start >= window ? new Window(now, 1) : w with { Count = w.Count + 1 });

        return entry.Count <= limit;
    }

    public void Reset(string key) => _windows.TryRemove(key, out _);

    private readonly record struct Window(DateTimeOffset Start, int Count);
}
