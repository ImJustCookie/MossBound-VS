using Mossbound.Core;

namespace Mossbound.Tests;

public class PatternStrokeTests
{
    static PatternStroke Draw(params int[] hovered)
    {
        var s = new PatternStroke();
        foreach (var p in hovered) s.Hover(p);
        return s;
    }

    [Fact]
    public void PointsSnapInOrderAndRepeatsAreIgnored() =>
        Assert.Equal(new[] { 0, 1, 2 }, Draw(0, 0, 1, 1, 2).Points);

    [Fact]
    public void MovingBackUnsnapsTheLastPoint() =>
        Assert.Equal(new[] { 0, 1 }, Draw(0, 1, 2, 1).Points);

    [Fact]
    public void DiameterGoesThroughTheCentreAndBacksOffInOneStep()
    {
        Assert.Equal(new[] { 0, 6, 3 }, Draw(0, 3).Points);
        Assert.Equal(new[] { 0 }, Draw(0, 3, 0).Points);
        // 0-6 already used in this stroke: crossing again is refused instead of looping forever.
        Assert.Equal(new[] { 0, 6, 1, 0 }, Draw(0, 6, 1, 0, 3).Points);
    }

    [Fact]
    public void SkippingARimPointGoesAlongTheRim() =>
        Assert.Equal(new[] { 0, 1, 2 }, Draw(0, 2).Points);

    [Fact]
    public void ALineIsNeverAddedTwiceButLoopsCanClose()
    {
        Assert.Equal(new[] { 0, 1, 6, 0 }, Draw(0, 1, 6, 0).Points);    // wedge closes
        Assert.Equal(new[] { 0, 1, 6, 0 }, Draw(0, 1, 6, 0, 1).Points); // 0-1 already drawn
    }

    [Fact]
    public void StrokeSegmentsMakeAValidFigure()
    {
        var f = GlyphFigure.Empty;
        foreach (var (a, b) in Draw(1, 4, 5).Segments()) f = f.WithLine(a, b);
        Assert.True(f.IsValid);
        Assert.True(f.HasLine(1, 6) && f.HasLine(6, 4) && f.HasLine(4, 5));
    }
}
