using Foodprint.Core.Auth;
using Foodprint.Tests.Infrastructure;

namespace Foodprint.Tests.Auth;

public class AttemptLimiterTests
{
    [Fact]
    public void Allows_up_to_the_limit_then_blocks()
    {
        var clock = new MutableClock(DateTimeOffset.UnixEpoch);
        var limiter = new InMemoryAttemptLimiter(clock);
        var window = TimeSpan.FromMinutes(15);

        for (var i = 0; i < 5; i++)
        {
            Assert.True(limiter.TryRecord("signin-email:a@b.c", 5, window));
        }

        Assert.False(limiter.TryRecord("signin-email:a@b.c", 5, window));
    }

    [Fact]
    public void Window_resets_after_it_elapses()
    {
        var clock = new MutableClock(DateTimeOffset.UnixEpoch);
        var limiter = new InMemoryAttemptLimiter(clock);
        var window = TimeSpan.FromMinutes(15);

        for (var i = 0; i < 5; i++)
        {
            limiter.TryRecord("k", 5, window);
        }

        Assert.False(limiter.TryRecord("k", 5, window));

        clock.Advance(TimeSpan.FromMinutes(16));
        Assert.True(limiter.TryRecord("k", 5, window));
    }

    [Fact]
    public void Keys_are_independent()
    {
        var limiter = new InMemoryAttemptLimiter(new MutableClock(DateTimeOffset.UnixEpoch));
        Assert.True(limiter.TryRecord("ip:1", 1, TimeSpan.FromMinutes(15)));
        Assert.True(limiter.TryRecord("ip:2", 1, TimeSpan.FromMinutes(15)));
        Assert.False(limiter.TryRecord("ip:1", 1, TimeSpan.FromMinutes(15)));
    }
}
