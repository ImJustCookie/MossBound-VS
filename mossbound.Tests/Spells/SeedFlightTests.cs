using Mossbound.Core;

namespace Mossbound.Tests;

public class SeedFlightTests
{
    [Fact]
    public void BiggerSeedsFlyFurtherWithinTheTimeLimit()
    {
        var small = SeedFlight.For(Motion.Straight, 1);
        var big = SeedFlight.For(Motion.Straight, 3);
        Assert.Equal(SeedFlight.BaseRange, small.Speed * small.Seconds, 3);
        Assert.True(big.Seconds > small.Seconds);
        Assert.All(Enum.GetValues<Motion>(), motion => Assert.InRange(SeedFlight.For(motion, 5).Seconds, 0.1, SeedFlight.MaxSeconds));
    }

    [Fact]
    public void OnlyHeavySeedsFallAndMotionsFlyDifferently()
    {
        Assert.All(Enum.GetValues<Motion>().Where(m => m != Motion.Heavy), motion => Assert.Equal(0, SeedFlight.For(motion, 1).Gravity));
        Assert.True(SeedFlight.For(Motion.Heavy, 1).Gravity > 0);
        Assert.True(SeedFlight.For(Motion.Heavy, 1).Sinks);
        Assert.False(SeedFlight.For(Motion.Straight, 1).Sinks);
        Assert.True(SeedFlight.For(Motion.Wobble, 1).Sway > 0);
        Assert.True(SeedFlight.For(Motion.Hover, 1).Speed < SeedFlight.For(Motion.Straight, 1).Speed);
    }
}
