using Mossbound.Core;

namespace Mossbound.Tests;

public class CircleDraftTests
{
    static readonly SlotKey c0 = new(CircleRing.Heart, 0), c1 = new(CircleRing.Heart, 1), o3 = new(CircleRing.Aim, 3);

    static readonly GlyphFigure line = GlyphFigure.Empty.WithLine(0, 1);

    static readonly SlotKey s0 = new(CircleRing.Shape, 0);

    static readonly CircleTier parchment = CircleTier.Parchment;

    static CircleDraft Written(params SlotKey[] slots)
    {
        var d = new CircleDraft();
        foreach (var slot in slots) d.Write(slot, line);
        return d;
    }

    [Fact]
    public void LinksOnlyJoinWrittenGlyphsTheGrammarAllows()
    {
        var d = Written(c0, c1, o3);
        d.Link(c1, c0, parchment);
        Assert.True(d.HasLink(c0, c1));      // order doesn't matter
        d.Link(c0, o3, parchment);
        Assert.False(d.HasLink(c0, o3));     // skips a ring
        d.Link(c0, s0, parchment);
        Assert.False(d.HasLink(c0, s0));     // s0 is not written
        d.Unlink(c0, c1);
        Assert.Empty(d.Links);
    }

    [Fact]
    public void ErasingTheLastLineErasesTheGlyphAndItsLinks()
    {
        var d = Written(c0, c1);
        d.Link(c0, c1, parchment);
        d.SetSize(c0, 2);
        d.EraseLines(c0, new[] { (0, 1) });
        Assert.False(d.IsWritten(c0));
        Assert.Empty(d.Links);
        Assert.Equal(1, d.SizeOf(c0));
    }

    [Fact]
    public void DrawingAddsLinesToTheGlyph()
    {
        var d = new CircleDraft();
        d.DrawLines(c0, new[] { (0, 1), (1, 6) });
        Assert.Equal(GlyphFigure.Empty.WithLine(0, 1).WithLine(1, 6), d.FigureAt(c0));
    }

    [Fact]
    public void ClickingAPointCyclesItsMark()
    {
        var d = new CircleDraft();
        var seen = new List<Mark>();
        for (int i = 0; i < 5; i++)
        {
            d.CycleMark(c0, 3);
            seen.Add(d.FigureAt(c0).MarkAt(3));
        }
        Assert.Equal(new[] { Mark.Dot, Mark.BigDot, Mark.Ring, Mark.DoubleRing, Mark.None }, seen);
        Assert.False(d.IsWritten(c0)); // back to nothing: the glyph is gone
        d.CycleMark(c0, 3);
        d.ClearMark(c0, 3);
        Assert.False(d.IsWritten(c0));
    }

    [Fact]
    public void SizeStaysWithinTheTierAndOnlyOnWrittenGlyphs()
    {
        var d = Written(c0);
        for (int i = 0; i < 5; i++) d.ChangeSize(c0, +1, CircleTier.Moss);
        Assert.Equal(2, d.SizeOf(c0));
        d.ChangeSize(c0, -5, CircleTier.Moss);
        Assert.Equal(1, d.SizeOf(c0));
        d.ChangeSize(c1, +1, CircleTier.Moss);
        Assert.Equal(1, d.SizeOf(c1));
    }

    [Fact]
    public void SourceCyclesFromBarkUp()
    {
        var d = new CircleDraft();
        d.CycleSource(CircleTier.Moss, backwards: false);
        Assert.Equal(LifeSource.Medium, d.Source);
        d.CycleSource(CircleTier.Bark, backwards: false);
        Assert.Equal(LifeSource.Self, d.Source);
        d.CycleSource(CircleTier.Bark, backwards: true);
        d.CycleSource(CircleTier.Bark, backwards: true);
        Assert.Equal(LifeSource.Land, d.Source);
    }

    [Fact]
    public void OnlyWrittenGlyphsAndTheirLinksAreSaved()
    {
        var d = Written(c0);
        d.Write(c1, GlyphFigure.Empty.WithMark(2, Mark.Dot)); // a dot alone is a glyph
        d.Link(c0, c1, parchment);
        var circle = d.ToCircle();
        Assert.Equal(2, circle.Glyphs.Count);
        Assert.Single(circle.Links);
        Assert.True(CircleValidation.FitsTier(circle, parchment));
    }

    [Fact]
    public void DraftRoundTripsThroughCircle()
    {
        var d = Written(c0, s0);
        d.Write(s0, line.WithMark(6, Mark.Ring));
        d.Link(c0, s0, parchment);
        d.SetSize(c0, 3);
        d.Source = LifeSource.Self;

        var back = CircleDraft.FromCircle(d.ToCircle());
        Assert.Equal(d.Figures, back.Figures);
        Assert.Equal(d.Links, back.Links);
        Assert.Equal(3, back.SizeOf(c0));
        Assert.Equal(LifeSource.Self, back.Source);
    }
}
