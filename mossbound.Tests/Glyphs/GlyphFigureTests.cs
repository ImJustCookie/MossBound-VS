using Mossbound.Core;

namespace Mossbound.Tests;

public class GlyphFigureTests
{
    [Fact]
    public void LineIndexCoversAllPairsUniquely()
    {
        var seen = new HashSet<int>();
        for (int a = 0; a < GlyphFigure.PointCount; a++)
            for (int b = a + 1; b < GlyphFigure.PointCount; b++)
            {
                int i = GlyphFigure.LineIndex(a, b);
                Assert.InRange(i, 0, GlyphFigure.PairCount - 1);
                Assert.True(seen.Add(i));
                Assert.Equal(i, GlyphFigure.LineIndex(b, a));
            }
        Assert.Equal(GlyphFigure.PairCount, seen.Count);
    }

    [Fact]
    public void ArcsJoinNeighbouringRimPointsOnly()
    {
        Assert.True(GlyphFigure.IsArc(0, 1));
        Assert.True(GlyphFigure.IsArc(5, 0));
        Assert.False(GlyphFigure.IsArc(0, 2));
        Assert.False(GlyphFigure.IsArc(0, GlyphFigure.Middle));
    }

    [Fact]
    public void StrokeOrderDoesNotMatterAndMarksRoundTrip()
    {
        var a = GlyphFigure.Empty.WithLine(0, 1).WithLine(1, 6).WithMark(3, Mark.DoubleRing);
        var b = GlyphFigure.Empty.WithMark(3, Mark.DoubleRing).WithLine(6, 1).WithLine(1, 0);
        Assert.Equal(a, b);
        Assert.Equal(Mark.DoubleRing, a.MarkAt(3));
        Assert.Equal(Mark.None, a.MarkAt(2));
        Assert.Equal(Mark.Dot, a.WithMark(3, Mark.Dot).MarkAt(3));
    }

    [Fact]
    public void EmptyOrMalformedFiguresAreInvalidButDotOnlyIsFine()
    {
        Assert.False(GlyphFigure.Empty.IsValid);
        Assert.True(GlyphFigure.Empty.WithMark(1, Mark.Dot).IsValid);
        Assert.False(new GlyphFigure(1u << 30, 0).IsValid);
        Assert.False(GlyphFigure.Empty.WithLine(0, 3).IsValid); // straight chords don't exist
        Assert.False(new GlyphFigure(0, 7u).IsValid);                // mark value 7 doesn't exist
        Assert.True(GlyphFigure.Empty.WithMark(6, Mark.BigDot).WithMark(5, Mark.DoubleRing).IsValid);
        Assert.False(GlyphFigure.Empty.WithLine(0, 2).IsValid);
        Assert.True(GlyphFigure.Empty.WithLine(0, 6).WithLine(6, 3).WithLine(3, 4).IsValid);
    }
}
