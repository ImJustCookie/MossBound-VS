using Mossbound.Core;

namespace Mossbound.Tests;

public class SegmentHitTests
{
    static readonly (double, double, double) min = (0, 0, 0), max = (1, 1, 1);

    [Fact]
    public void AStepThroughTheBoxEntersWhereItCrossesTheFace() =>
        Assert.Equal(0.25, SegmentHit.Enters((-1, 0.5, 0.5), (3, 0.5, 0.5), min, max)!.Value, 6);

    [Fact]
    public void AStepBesideOrShortOfTheBoxMisses()
    {
        Assert.Null(SegmentHit.Enters((-1, 2, 0.5), (3, 2, 0.5), min, max));
        Assert.Null(SegmentHit.Enters((-2, 0.5, 0.5), (-1, 0.5, 0.5), min, max));
    }

    [Fact]
    public void AStepStartingInsideHitsAtOnce() => Assert.Equal(0, SegmentHit.Enters((0.5, 0.5, 0.5), (3, 3, 3), min, max));
}
