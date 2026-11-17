using Mossbound.Core;
using ProtoBuf;

namespace Mossbound.Tests;

public class GlyphFigureTests
{
    [Fact]
    public void LinkIndexCoversAllPairsUniquely()
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
    public void ArcsAreNeighbouringEdgePointsOnly()
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

public class WorldScriptTests
{
    // Recorded from the original generator: a change here would change every existing world's script.
    [Theory]
        [InlineData(1, "water", 132129u, 36896u)]
        [InlineData(1, "ember", 165952u, 131104u)]
        [InlineData(1, "frost", 19505u, 12576u)]
        [InlineData(1, "stone", 802832u, 262176u)]
        [InlineData(1, "life", 150592u, 132128u)]
        [InlineData(1, "rot", 296976u, 16416u)]
        [InlineData(1, "wind", 1049633u, 1064992u)]
        [InlineData(1, "ward", 1852480u, 12321u)]
        [InlineData(12345, "water", 1346560u, 1028u)]
        [InlineData(12345, "ember", 1180736u, 132u)]
        [InlineData(12345, "frost", 950272u, 4100u)]
        [InlineData(12345, "stone", 296976u, 65540u)]
        [InlineData(12345, "life", 525408u, 28u)]
        [InlineData(12345, "rot", 1048625u, 260u)]
        [InlineData(12345, "wind", 951297u, 1540u)]
        [InlineData(12345, "ward", 1736736u, 32772u)]
        [InlineData(-987654321, "water", 951296u, 136192u)]
        [InlineData(-987654321, "ember", 543744u, 131138u)]
        [InlineData(-987654321, "frost", 183360u, 131088u)]
        [InlineData(-987654321, "stone", 1572913u, 131076u)]
        [InlineData(-987654321, "life", 1966112u, 921600u)]
        [InlineData(-987654321, "rot", 1230848u, 393280u)]
        [InlineData(-987654321, "wind", 819232u, 393216u)]
        [InlineData(-987654321, "ward", 1181728u, 135680u)]
    public void GlyphShapesNeverChangeForAGivenSeed(long seed, string code, uint lines, uint marks) =>
        Assert.Equal(new GlyphFigure(lines, marks), new WorldScript(seed).FigureOf(code));

    [Theory]
        [InlineData(1, 1, Mark.DoubleRing)]
        [InlineData(12345, 0, Mark.DoubleRing)]
        [InlineData(-987654321, 5, Mark.DoubleRing)]
    public void FamilyMarksNeverChangeForAGivenSeed(long seed, int point, Mark mark) =>
        Assert.Equal((point, mark), new WorldScript(seed).FamilyMarkOf(GlyphKind.Element));

    [Fact]
    public void SameSeedGivesSameScript()
    {
        var a = new WorldScript(12345);
        var b = new WorldScript(12345);
        foreach (var g in GlyphRegistry.All) Assert.Equal(a.FigureOf(g.Code), b.FigureOf(g.Code));
    }

    [Fact]
    public void DifferentSeedsGiveDifferentScripts()
    {
        var a = new WorldScript(1);
        var b = new WorldScript(2);
        Assert.Contains(GlyphRegistry.All, g => a.FigureOf(g.Code) != b.FigureOf(g.Code));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(42L)]
    [InlineData(-987654321L)]
    public void EveryGlyphIsUniqueRecognizableAndCarriesItsFamilyMotif(long seed)
    {
        var script = new WorldScript(seed);
        var figures = new HashSet<GlyphFigure>();
        foreach (var g in GlyphRegistry.All)
        {
            var f = script.FigureOf(g.Code);
            Assert.True(f.IsValid);
            Assert.True(figures.Add(f));
            Assert.Same(g, script.Recognize(f));

            var (point, mark) = script.FamilyMarkOf(g.Kind);
            Assert.Equal(mark, f.MarkAt(point));

            var links = f.LinePairs().ToList();
            Assert.InRange(links.Count, 4, 6);
            Assert.True(IsConnected(links));

            int marks = Enumerable.Range(0, GlyphFigure.PointCount).Count(p => f.MarkAt(p) != Mark.None);
            Assert.InRange(marks, 2, 3); // family motif + 1–2 extra
        }
    }

    [Fact]
    public void UnknownFigureIsNotRecognized()
    {
        var script = new WorldScript(7);
        var known = GlyphRegistry.All.Select(g => script.FigureOf(g.Code)).ToHashSet();
        var all = GlyphFigure.Empty;
        for (int a = 0; a < GlyphFigure.PointCount; a++)
            for (int b = a + 1; b < GlyphFigure.PointCount; b++)
                all = all.WithLine(a, b);
        Assert.DoesNotContain(all, known);
        Assert.Null(script.Recognize(all));
    }

    static bool IsConnected(List<(int a, int b)> links)
    {
        var reached = new HashSet<int> { links[0].a };
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (var (a, b) in links)
            {
                if (reached.Contains(a) && reached.Add(b)) grew = true;
                if (reached.Contains(b) && reached.Add(a)) grew = true;
            }
        }
        return links.All(l => reached.Contains(l.a));
    }
}

public class ElementMixerTests
{
    static MixResult Mix(params Element[] e) => ElementMixer.Mix(e);

    [Fact]
    public void PairMakesCompound() =>
        Assert.Equal(new[] { (Aspect.Ice, 1) }, Mix(Element.Water, Element.Frost).Aspects);

    [Fact]
    public void CompoundIntensityIsSmallerCountAndLeftoverStaysPure() =>
        Assert.Equal(new[] { (Aspect.Ice, 1), (Aspect.Water, 1) },
            Mix(Element.Water, Element.Water, Element.Frost).Aspects);

    [Fact]
    public void LifeWaterStoneMakesMoss() =>
        Assert.Equal(new[] { (Aspect.Moss, 1) }, Mix(Element.Life, Element.Water, Element.Stone).Aspects);

    [Fact]
    public void LifeAndRotMakeSpore() =>
        Assert.Equal(new[] { (Aspect.Spore, 1) }, Mix(Element.Life, Element.Rot).Aspects);

    [Fact]
    public void AnElementJoinsOnlyOneCompound() =>
        // Frost goes to Ice (earlier in the table), so Wind stays pure instead of making Gale.
        Assert.Equal(new[] { (Aspect.Ice, 1), (Aspect.Wind, 1) },
            Mix(Element.Water, Element.Frost, Element.Wind).Aspects);

    [Theory]
    [InlineData(Element.Ember, Element.Frost)]
    [InlineData(Element.Wind, Element.Ward)]
    public void OppositesCancel(Element a, Element b)
    {
        var r = Mix(a, b);
        Assert.Contains(Mishap.Opposition, r.Mishaps);
        Assert.Empty(r.Aspects);
    }

    [Fact]
    public void MoreThanThreeOfOneElementOverloads()
    {
        Assert.Empty(Mix(Element.Water, Element.Water, Element.Water).Mishaps);
        Assert.Contains(Mishap.Overload, Mix(Element.Water, Element.Water, Element.Water, Element.Water).Mishaps);
    }

    [Fact]
    public void NothingToMixIsAMishap() => Assert.Contains(Mishap.NoElement, Mix().Mishaps);
}

public class CircleParserTests
{
    static readonly WorldScript script = new(99);

    static PlacedGlyph Place(string code, CircleRing ring, int slot = 0)
    {
        var f = script.FigureOf(code);
        return new PlacedGlyph { Ring = ring, Slot = slot, Lines = f.Lines, Marks = f.Marks };
    }

    [Fact]
    public void GlyphSizeIsIntensityAndIsCappedPerTier()
    {
        var strong = Place("ember", CircleRing.Heart); strong.Size = 3;
        var plan = SpellReading.Read(new Circle { Glyphs = { strong } }, script);
        Assert.Equal((Aspect.Ember, 3), Assert.Single(plan.Aspects));

        // A water glyph of size 2 next to a plain frost: Ice, at the smaller count (1).
        var water = Place("water", CircleRing.Heart); water.Size = 2;
        var mix = SpellReading.Read(new Circle { Glyphs = { water, Place("frost", CircleRing.Heart, 1) }, Links = { new CircleLink { From = 0, To = 1 } } }, script);
        Assert.Contains((Aspect.Ice, 1), mix.Aspects);

        Assert.True(CircleValidation.FitsTier(new Circle { Glyphs = { strong } }, CircleTier.Bark));
        Assert.False(CircleValidation.FitsTier(new Circle { Glyphs = { strong } }, CircleTier.Moss)); // moss caps at 2
        var negative = Place("ember", CircleRing.Heart); negative.Size = -1;
        Assert.False(CircleValidation.FitsTier(new Circle { Glyphs = { negative } }, CircleTier.Stone));
        // Strips written before sizes existed (Size 0) read as size 1.
        Assert.Equal(1, Place("ember", CircleRing.Heart).EffectiveSize);
    }

    [Fact]
    public void SourceIsChosenFromBarkUpAndMustBeKnown()
    {
        var land = new Circle { Glyphs = { Place("ember", CircleRing.Heart) }, Source = LifeSource.Land };
        Assert.True(CircleValidation.FitsTier(land, CircleTier.Bark));
        Assert.False(CircleValidation.FitsTier(land, CircleTier.Moss));
        land.Source = (LifeSource)42;
        Assert.False(CircleValidation.FitsTier(land, CircleTier.Stone));
    }

    [Fact]
    public void LinkedHeartsMix()
    {
        var circle = new Circle { Glyphs = { Place("water", CircleRing.Heart), Place("frost", CircleRing.Heart, 1) }, Links = { new CircleLink { From = 0, To = 1 } } };
        var plan = SpellReading.Read(circle, script);
        Assert.False(plan.Fizzles);
        Assert.Equal(new[] { (Aspect.Ice, 1) }, plan.Aspects);
    }

    [Fact]
    public void UnlinkedHeartsStaySeparate()
    {
        // Opposites only cancel when they are linked into the same heart.
        var circle = new Circle { Glyphs = { Place("ember", CircleRing.Heart), Place("frost", CircleRing.Heart, 1) } };
        var plan = SpellReading.Read(circle, script);
        Assert.False(plan.Fizzles);
        Assert.Equal(new[] { (Aspect.Ember, 1), (Aspect.Frost, 1) }, plan.Aspects);

        circle.Links.Add(new CircleLink { From = 0, To = 1 });
        Assert.Contains(Mishap.Opposition, SpellReading.Read(circle, script).Mishaps);
    }

    [Fact]
    public void ElementOutsideCentreIsMisplaced()
    {
        var circle = new Circle { Glyphs = { Place("water", CircleRing.Heart), Place("frost", CircleRing.Aim) } };
        var plan = SpellReading.Read(circle, script);
        Assert.Contains(Mishap.Misplaced, plan.Mishaps);
    }

    [Fact]
    public void UnknownFiguresAreIgnored()
    {
        var junk = new PlacedGlyph { Ring = CircleRing.Heart, Slot = 1, Lines = (1u << GlyphFigure.PairCount) - 1 };
        var circle = new Circle { Glyphs = { Place("ember", CircleRing.Heart), junk } };
        var plan = SpellReading.Read(circle, script);
        Assert.Equal(new[] { (Aspect.Ember, 1) }, plan.Aspects);
    }

    [Fact]
    public void BoundsRejectOversizedOrMalformedInput()
    {
        var ok = new Circle { Glyphs = { Place("water", CircleRing.Heart), Place("life", CircleRing.Heart, 1) } };
        Assert.True(CircleValidation.FitsTier(ok, CircleTier.Moss));

        var tooMany = new Circle();
        for (int i = 0; i <= CircleValidation.MaxGlyphs; i++) tooMany.Glyphs.Add(Place("water", CircleRing.Heart));
        Assert.False(CircleValidation.FitsTier(tooMany, CircleTier.Stone));

        var sameSlot = new Circle { Glyphs = { Place("water", CircleRing.Heart), Place("life", CircleRing.Heart) } };
        Assert.False(CircleValidation.FitsTier(sameSlot, CircleTier.Stone));

        var badLink = new Circle { Glyphs = { Place("water", CircleRing.Heart) }, Links = { new CircleLink { From = 0, To = 5 } } };
        Assert.False(CircleValidation.FitsTier(badLink, CircleTier.Stone));

        var badSlot = new Circle { Glyphs = { Place("water", CircleRing.Heart, CircleTier.Stone.SlotCount(CircleRing.Heart)) } };
        Assert.False(CircleValidation.FitsTier(badSlot, CircleTier.Stone));

        var badRing = new Circle { Glyphs = { Place("water", (CircleRing)42) } };
        Assert.False(CircleValidation.FitsTier(badRing, CircleTier.Stone));

        var badFigure = new Circle { Glyphs = { new PlacedGlyph { Ring = CircleRing.Heart } } }; // empty figure
        Assert.False(CircleValidation.FitsTier(badFigure, CircleTier.Stone));
    }

    [Fact]
    public void CircleSurvivesNetworkSerialization()
    {
        var circle = new Circle
        {
            Glyphs = { Place("water", CircleRing.Heart), Place("wind", CircleRing.Heart, 3) },
            Links = { new CircleLink { From = 0, To = 1 } },
        };
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, circle);
        ms.Position = 0;
        var back = Serializer.Deserialize<Circle>(ms);

        Assert.Equal(circle.Glyphs.Select(g => (g.Ring, g.Slot, g.Figure)), back.Glyphs.Select(g => (g.Ring, g.Slot, g.Figure)));
        Assert.Equal(1, back.Links.Single().To);
    }
}

public class CircleDraftTests
{
    static readonly SlotKey c0 = new(CircleRing.Heart, 0), c1 = new(CircleRing.Heart, 1), o3 = new(CircleRing.Aim, 3);
    static readonly GlyphFigure line = GlyphFigure.Empty.WithLine(0, 1);

    [Fact]
    public void LinksJoinNeighbouringRingsOrNeighbouringSlots()
    {
        var bark = CircleTier.Bark; // heart 3, shape 4
        Assert.True(bark.AllowsLink(c0, new(CircleRing.Shape, 2)));                         // next ring
        Assert.True(bark.AllowsLink(new(CircleRing.Shape, 0), new(CircleRing.Shape, 3)));   // wraps
        Assert.False(bark.AllowsLink(new(CircleRing.Shape, 0), new(CircleRing.Shape, 2)));  // across
        Assert.False(CircleTier.Parchment.AllowsLink(c0, o3));          // skips a ring
        Assert.False(bark.AllowsLink(c0, o3));                          // no such slot
        Assert.False(bark.AllowsLink(c0, c0));
    }

    [Fact]
    public void LinksBendAroundGlyphsTheyDoNotJoin()
    {
        (double x, double y) a = (0, 0), b = (100, 0), centre = (50, 200), blocker = (50, 0);
        var q = LinkRoute.Control(a, b, centre, new[] { (blocker.x, blocker.y, 40.0) }); // straight would run right through it
        for (int i = 1; i < 24; i++)
        {
            var p = LinkRoute.PointOn(a, q, b, i / 24.0);
            Assert.True(Math.Sqrt((p.x - blocker.x) * (p.x - blocker.x) + (p.y - blocker.y) * (p.y - blocker.y)) >= 40);
        }
        // Nothing in the way: straight.
        Assert.Equal((50.0, 0.0), LinkRoute.Control(a, b, centre, Array.Empty<(double, double, double)>()));
    }

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

public class CircleTierTests
{
    static readonly WorldScript script = new(5);

    static PlacedGlyph Place(CircleRing ring, int slot)
    {
        var f = script.FigureOf("life");
        return new PlacedGlyph { Ring = ring, Slot = slot, Lines = f.Lines, Marks = f.Marks };
    }

    [Fact]
    public void EachTierAddsARingOutward()
    {
        Assert.Equal(new[] { CircleRing.Heart }, CircleTier.Moss.Rings);
        Assert.Equal(new[] { CircleRing.Heart, CircleRing.Shape }, CircleTier.Bark.Rings);
        Assert.Equal(new[] { CircleRing.Heart, CircleRing.Shape, CircleRing.Aim }, CircleTier.Parchment.Rings);
        Assert.Equal(new[] { CircleRing.Heart, CircleRing.Shape, CircleRing.Aim, CircleRing.Rim }, CircleTier.Stone.Rings);
        Assert.Equal(2, CircleTier.Moss.SlotCount(CircleRing.Heart));
        Assert.Equal(0, CircleTier.Moss.SlotCount(CircleRing.Shape));
        Assert.Equal((3, 4), (CircleTier.Bark.SlotCount(CircleRing.Heart), CircleTier.Bark.SlotCount(CircleRing.Shape)));
        Assert.Equal((4, 6), (CircleTier.Parchment.SlotCount(CircleRing.Heart), CircleTier.Parchment.SlotCount(CircleRing.Shape)));
    }

    [Fact]
    public void ServerRejectsRingsAndSlotsTheMediumDoesNotHave()
    {
        var heartOnly = new Circle { Glyphs = { Place(CircleRing.Heart, 0), Place(CircleRing.Heart, 1) } };
        Assert.True(CircleValidation.FitsTier(heartOnly, CircleTier.Moss));

        var thirdHeartSlot = new Circle { Glyphs = { Place(CircleRing.Heart, 2) } };
        Assert.False(CircleValidation.FitsTier(thirdHeartSlot, CircleTier.Moss));
        Assert.True(CircleValidation.FitsTier(thirdHeartSlot, CircleTier.Bark));

        var shape = new Circle { Glyphs = { Place(CircleRing.Shape, 0) } };
        Assert.False(CircleValidation.FitsTier(shape, CircleTier.Moss));
        Assert.True(CircleValidation.FitsTier(shape, CircleTier.Bark));
    }

    [Theory]
    [InlineData(-3, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(99, 4)]
    public void UnknownLevelsClampToARealTier(int level, int expected) =>
        Assert.Equal(expected, CircleTier.ForLevel(level).Level);
}

public class CircleShapeTests
{
    static SlotKey C(int s) => new(CircleRing.Heart, s);
    static SlotKey I(int s) => new(CircleRing.Shape, s);
    static SlotKey O(int s) => new(CircleRing.Aim, s);

    [Fact]
    public void LinkedHeartsMixAndEachBodyFedByAHeartIsAWord()
    {
        var glyphs = new[] { C(0), C(1), C(2), I(0), I(1), O(0), I(3), O(5) };
        var links = new[] { (C(0), C(1)), (C(1), I(0)), (I(0), I(1)), (I(0), O(0)), (C(2), I(3)) };

        var mix = Assert.Single(CircleStructure.Mixes(glyphs, links));
        Assert.Equal(new[] { C(0), C(1) }, mix);

        var words = CircleStructure.Casts(glyphs, links);
        Assert.Equal(2, words.Count);
        Assert.Equal(new[] { I(0), I(1), O(0) }, words[0].Body);
        Assert.Equal(O(0), words[0].AimEnd);
        Assert.Equal(new[] { C(2) }, words[1].Heart);
        Assert.False(words[1].IsAimed);
        // O(5) is linked to nothing: not part of any word.
        Assert.DoesNotContain(words, w => w.Body.Contains(O(5)));
    }

    [Fact]
    public void TheAimLineFollowsTheEndOfTheAimChain()
    {
        // Heart -> shape -> Foes -> Closest: the chain ends at O(1), wherever the slots happen to sit.
        var glyphs = new[] { C(0), I(1), O(2), O(1) };
        var links = new[] { (C(0), I(1)), (I(1), O(2)), (O(2), O(1)) };
        Assert.Equal(O(1), Assert.Single(CircleStructure.Casts(glyphs, links)).AimEnd);
    }

    [Fact]
    public void WordCirclesBiteTheBorder()
    {
        var (x, y, r) = WordCircle.Place(new[] { (100.0, 0.0), (140.0, 0.0) }, 10, (0, 0), border: 200);
        Assert.Equal(0, y, 6);
        Assert.Equal(200 + r * 0.1, x + r, 6); // reaches past the border by 10% of its radius
    }

    [Fact]
    public void HeavierWordsAreBigger()
    {
        var glyphs = new[] { (100.0, 0.0), (140.0, 0.0) };
        double plain = WordCircle.Place(glyphs, 10, (0, 0), 200).r;
        double strong = WordCircle.Place(glyphs, 10, (0, 0), 200, extraWeight: 3).r;
        double capped = WordCircle.Place(glyphs, 10, (0, 0), 200, extraWeight: 99).r;
        Assert.Equal(plain * 1.3, strong, 6);
        Assert.Equal(plain * 1.5, capped, 6);
        // However heavy, a word stays well inside the circle it bites.
        var spread = new[] { (-150.0, 0.0), (150.0, 0.0) };
        Assert.True(WordCircle.Place(spread, 10, (0, 0), 200, extraWeight: 5).r <= 200 * 0.85);
    }

    [Fact]
    public void ChordRunsAcrossTheWholeCircle()
    {
        var chord = Geometry.LineAcrossCircle((0, 0), (10, 0), (0, 0), 50);
        Assert.NotNull(chord);
        Assert.Equal((-50.0, 0.0), chord.Value.from);
        Assert.Equal((50.0, 0.0), chord.Value.to);
    }
}
