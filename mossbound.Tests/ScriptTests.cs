using Mossbound.Script;
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
                int i = GlyphFigure.LinkIndex(a, b);
                Assert.InRange(i, 0, GlyphFigure.LinkCount - 1);
                Assert.True(seen.Add(i));
                Assert.Equal(i, GlyphFigure.LinkIndex(b, a));
            }
        Assert.Equal(GlyphFigure.LinkCount, seen.Count);
    }

    [Fact]
    public void ArcsAreNeighbouringEdgePointsOnly()
    {
        Assert.True(GlyphFigure.IsArc(0, 1));
        Assert.True(GlyphFigure.IsArc(5, 0));
        Assert.False(GlyphFigure.IsArc(0, 2));
        Assert.False(GlyphFigure.IsArc(0, GlyphFigure.Centre));
    }

    [Fact]
    public void StrokeOrderDoesNotMatterAndMarksRoundTrip()
    {
        var a = new GlyphFigure(0, 0).WithLink(0, 1).WithLink(1, 6).WithMark(3, Mark.DoubleRing);
        var b = new GlyphFigure(0, 0).WithMark(3, Mark.DoubleRing).WithLink(6, 1).WithLink(1, 0);
        Assert.Equal(a, b);
        Assert.Equal(Mark.DoubleRing, a.MarkAt(3));
        Assert.Equal(Mark.None, a.MarkAt(2));
        Assert.Equal(Mark.Dot, a.WithMark(3, Mark.Dot).MarkAt(3));
    }

    [Fact]
    public void EmptyOrMalformedFiguresAreInvalidButDotOnlyIsFine()
    {
        Assert.False(new GlyphFigure(0, 0).IsValid);
        Assert.True(new GlyphFigure(0, 0).WithMark(1, Mark.Dot).IsValid);
        Assert.False(new GlyphFigure(1u << 30, 0).IsValid);
        Assert.False(new GlyphFigure(0, 0).WithLink(0, 3).IsValid); // straight chords don't exist
        Assert.False(new GlyphFigure(0, 7u).IsValid);                // mark value 7 doesn't exist
        Assert.True(new GlyphFigure(0, 0).WithMark(6, Mark.BigDot).WithMark(5, Mark.DoubleRing).IsValid);
        Assert.False(new GlyphFigure(0, 0).WithLink(0, 2).IsValid);
        Assert.True(new GlyphFigure(0, 0).WithLink(0, 6).WithLink(6, 3).WithLink(3, 4).IsValid);
    }
}

public class WorldScriptTests
{
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

            var (point, mark) = script.MotifOf(g.Kind);
            Assert.Equal(mark, f.MarkAt(point));

            var links = f.LinkPairs().ToList();
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
        var all = new GlyphFigure(0, 0);
        for (int a = 0; a < GlyphFigure.PointCount; a++)
            for (int b = a + 1; b < GlyphFigure.PointCount; b++)
                all = all.WithLink(a, b);
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
        Assert.False(Mix(Element.Water, Element.Water, Element.Water).Failed);
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
        return new PlacedGlyph { Ring = ring, Slot = slot, Links = f.Links, Marks = f.Marks };
    }

    [Fact]
    public void GlyphSizeIsIntensityAndIsCappedPerTier()
    {
        var strong = Place("ember", CircleRing.Centre); strong.Size = 3;
        var plan = CircleParser.Parse(new Circle { Glyphs = { strong } }, script);
        Assert.Equal((Aspect.Ember, 3), Assert.Single(plan.Aspects));

        // A water glyph of size 2 next to a plain frost: Ice, at the smaller count (1).
        var water = Place("water", CircleRing.Centre); water.Size = 2;
        var mix = CircleParser.Parse(new Circle { Glyphs = { water, Place("frost", CircleRing.Centre, 1) }, Links = { new CircleLink { From = 0, To = 1 } } }, script);
        Assert.Contains((Aspect.Ice, 1), mix.Aspects);

        Assert.True(CircleParser.IsWithinBounds(new Circle { Glyphs = { strong } }, CircleTier.Bark));
        Assert.False(CircleParser.IsWithinBounds(new Circle { Glyphs = { strong } }, CircleTier.Moss)); // moss caps at 2
        var negative = Place("ember", CircleRing.Centre); negative.Size = -1;
        Assert.False(CircleParser.IsWithinBounds(new Circle { Glyphs = { negative } }, CircleTier.Stone));
        // Strips written before sizes existed (Size 0) read as size 1.
        Assert.Equal(1, Place("ember", CircleRing.Centre).Power);
    }

    [Fact]
    public void SourceIsChosenFromBarkUpAndMustBeKnown()
    {
        var land = new Circle { Glyphs = { Place("ember", CircleRing.Centre) }, Source = LifeSource.Land };
        Assert.True(CircleParser.IsWithinBounds(land, CircleTier.Bark));
        Assert.False(CircleParser.IsWithinBounds(land, CircleTier.Moss));
        land.Source = (LifeSource)42;
        Assert.False(CircleParser.IsWithinBounds(land, CircleTier.Stone));
    }

    [Fact]
    public void LinkedHeartsMix()
    {
        var circle = new Circle { Glyphs = { Place("water", CircleRing.Centre), Place("frost", CircleRing.Centre, 1) }, Links = { new CircleLink { From = 0, To = 1 } } };
        var plan = CircleParser.Parse(circle, script);
        Assert.False(plan.Fizzles);
        Assert.Equal(new[] { (Aspect.Ice, 1) }, plan.Aspects);
    }

    [Fact]
    public void UnlinkedHeartsStaySeparate()
    {
        // Opposites only cancel when they are linked into the same heart.
        var circle = new Circle { Glyphs = { Place("ember", CircleRing.Centre), Place("frost", CircleRing.Centre, 1) } };
        var plan = CircleParser.Parse(circle, script);
        Assert.False(plan.Fizzles);
        Assert.Equal(new[] { (Aspect.Ember, 1), (Aspect.Frost, 1) }, plan.Aspects);

        circle.Links.Add(new CircleLink { From = 0, To = 1 });
        Assert.Contains(Mishap.Opposition, CircleParser.Parse(circle, script).Mishaps);
    }

    [Fact]
    public void ElementOutsideCentreIsMisplaced()
    {
        var circle = new Circle { Glyphs = { Place("water", CircleRing.Centre), Place("frost", CircleRing.Outer) } };
        var plan = CircleParser.Parse(circle, script);
        Assert.Contains(Mishap.Misplaced, plan.Mishaps);
    }

    [Fact]
    public void UnknownFiguresAreCollectedNotCrashed()
    {
        var junk = new PlacedGlyph { Ring = CircleRing.Centre, Links = (1u << GlyphFigure.LinkCount) - 1 };
        var circle = new Circle { Glyphs = { Place("ember", CircleRing.Centre), junk } };
        var plan = CircleParser.Parse(circle, script);
        Assert.Single(plan.Unrecognized);
        Assert.Equal(new[] { (Aspect.Ember, 1) }, plan.Aspects);
    }

    [Fact]
    public void BoundsRejectOversizedOrMalformedInput()
    {
        var ok = new Circle { Glyphs = { Place("water", CircleRing.Centre), Place("life", CircleRing.Centre, 1) } };
        Assert.True(CircleParser.IsWithinBounds(ok, CircleTier.Moss));

        var tooMany = new Circle();
        for (int i = 0; i <= CircleParser.MaxGlyphs; i++) tooMany.Glyphs.Add(Place("water", CircleRing.Centre));
        Assert.False(CircleParser.IsWithinBounds(tooMany, CircleTier.Stone));

        var sameSlot = new Circle { Glyphs = { Place("water", CircleRing.Centre), Place("life", CircleRing.Centre) } };
        Assert.False(CircleParser.IsWithinBounds(sameSlot, CircleTier.Stone));

        var badLink = new Circle { Glyphs = { Place("water", CircleRing.Centre) }, Links = { new CircleLink { From = 0, To = 5 } } };
        Assert.False(CircleParser.IsWithinBounds(badLink, CircleTier.Stone));

        var badSlot = new Circle { Glyphs = { Place("water", CircleRing.Centre, CircleTier.Stone.SlotCount(CircleRing.Centre)) } };
        Assert.False(CircleParser.IsWithinBounds(badSlot, CircleTier.Stone));

        var badRing = new Circle { Glyphs = { Place("water", (CircleRing)42) } };
        Assert.False(CircleParser.IsWithinBounds(badRing, CircleTier.Stone));

        var badFigure = new Circle { Glyphs = { new PlacedGlyph { Ring = CircleRing.Centre } } }; // empty figure
        Assert.False(CircleParser.IsWithinBounds(badFigure, CircleTier.Stone));
    }

    [Fact]
    public void CircleSurvivesNetworkSerialization()
    {
        var circle = new Circle
        {
            Glyphs = { Place("water", CircleRing.Centre), Place("wind", CircleRing.Centre, 3) },
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
    static readonly SlotKey c0 = new(CircleRing.Centre, 0), c1 = new(CircleRing.Centre, 1), o3 = new(CircleRing.Outer, 3);
    static readonly GlyphFigure line = new GlyphFigure(0, 0).WithLink(0, 1);

    [Fact]
    public void LinksJoinNeighbouringRingsOrNeighbouringSlots()
    {
        var bark = CircleTier.Bark; // heart 3, shape 4
        Assert.True(CircleDraft.CanLink(c0, new(CircleRing.Inner, 2), bark));      // next ring
        Assert.True(CircleDraft.CanLink(new(CircleRing.Inner, 0), new(CircleRing.Inner, 3), bark)); // wraps
        Assert.False(CircleDraft.CanLink(new(CircleRing.Inner, 0), new(CircleRing.Inner, 2), bark)); // across
        Assert.False(CircleDraft.CanLink(c0, o3, CircleTier.Parchment));          // skips a ring
        Assert.False(CircleDraft.CanLink(c0, o3, bark));                          // no such slot
        Assert.False(CircleDraft.CanLink(c0, c0, bark));
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

    [Fact]
    public void ToggleLinkIsOrderIndependentAndToggles()
    {
        var d = new CircleDraft();
        d.ToggleLink(o3, c0);
        Assert.Contains(CircleDraft.Normalize(c0, o3), d.Links);
        d.ToggleLink(c0, o3);
        Assert.Empty(d.Links);
        d.ToggleLink(c0, c0);
        Assert.Empty(d.Links);
    }

    [Fact]
    public void EmptyGlyphsAndTheirLinksAreNotSent()
    {
        var d = new CircleDraft();
        d.Figures[c0] = line;
        d.Figures[c1] = new GlyphFigure(0, 0).WithMark(2, Mark.Dot); // dot-only: kept
        d.Figures[o3] = new GlyphFigure(0, 0);                       // empty: dropped
        d.ToggleLink(c0, c1);
        d.ToggleLink(c0, o3);

        var circle = d.ToCircle();
        Assert.Equal(2, circle.Glyphs.Count);
        Assert.Single(circle.Links);
        Assert.True(CircleParser.IsWithinBounds(circle, CircleTier.Parchment));
    }

    [Fact]
    public void SizesAndSourceSurviveTheRoundTrip()
    {
        var d = new CircleDraft();
        d.Figures[c0] = line;
        d.SetSize(c0, 3);
        d.Source = LifeSource.Self;
        var back = CircleDraft.FromCircle(d.ToCircle());
        Assert.Equal(3, back.SizeOf(c0));
        Assert.Equal(LifeSource.Self, back.Source);
        back.Clear(c0);
        Assert.Equal(1, back.SizeOf(c0));
    }

    [Fact]
    public void DraftRoundTripsThroughCircle()
    {
        var d = new CircleDraft();
        d.Figures[c0] = line;
        d.Figures[o3] = line.WithMark(6, Mark.Ring);
        d.ToggleLink(c0, o3);

        var back = CircleDraft.FromCircle(d.ToCircle());
        Assert.Equal(d.Figures, back.Figures);
        Assert.Equal(d.Links, back.Links);
    }

    [Fact]
    public void ClearRemovesGlyphAndItsLinks()
    {
        var d = new CircleDraft();
        d.Figures[c0] = line;
        d.Figures[o3] = line;
        d.ToggleLink(c0, o3);
        d.Clear(o3);
        Assert.False(d.Figures.ContainsKey(o3));
        Assert.Empty(d.Links);
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
        var f = new GlyphFigure(0, 0);
        foreach (var (a, b) in Draw(1, 4, 5).Segments()) f = f.WithLink(a, b);
        Assert.True(f.IsValid);
        Assert.True(f.HasLink(1, 6) && f.HasLink(6, 4) && f.HasLink(4, 5));
    }
}

public class CircleTierTests
{
    static readonly WorldScript script = new(5);

    static PlacedGlyph Place(CircleRing ring, int slot)
    {
        var f = script.FigureOf("life");
        return new PlacedGlyph { Ring = ring, Slot = slot, Links = f.Links, Marks = f.Marks };
    }

    [Fact]
    public void EachTierAddsARingOutward()
    {
        Assert.Equal(new[] { CircleRing.Centre }, CircleTier.Moss.Rings);
        Assert.Equal(new[] { CircleRing.Centre, CircleRing.Inner }, CircleTier.Bark.Rings);
        Assert.Equal(new[] { CircleRing.Centre, CircleRing.Inner, CircleRing.Outer }, CircleTier.Parchment.Rings);
        Assert.Equal(new[] { CircleRing.Centre, CircleRing.Inner, CircleRing.Outer, CircleRing.Rim }, CircleTier.Stone.Rings);
        Assert.Equal(2, CircleTier.Moss.SlotCount(CircleRing.Centre));
        Assert.Equal(0, CircleTier.Moss.SlotCount(CircleRing.Inner));
        Assert.Equal((3, 4), (CircleTier.Bark.SlotCount(CircleRing.Centre), CircleTier.Bark.SlotCount(CircleRing.Inner)));
        Assert.Equal((4, 6), (CircleTier.Parchment.SlotCount(CircleRing.Centre), CircleTier.Parchment.SlotCount(CircleRing.Inner)));
    }

    [Fact]
    public void ServerRejectsRingsAndSlotsTheMediumDoesNotHave()
    {
        var heartOnly = new Circle { Glyphs = { Place(CircleRing.Centre, 0), Place(CircleRing.Centre, 1) } };
        Assert.True(CircleParser.IsWithinBounds(heartOnly, CircleTier.Moss));

        var thirdHeartSlot = new Circle { Glyphs = { Place(CircleRing.Centre, 2) } };
        Assert.False(CircleParser.IsWithinBounds(thirdHeartSlot, CircleTier.Moss));
        Assert.True(CircleParser.IsWithinBounds(thirdHeartSlot, CircleTier.Bark));

        var shape = new Circle { Glyphs = { Place(CircleRing.Inner, 0) } };
        Assert.False(CircleParser.IsWithinBounds(shape, CircleTier.Moss));
        Assert.True(CircleParser.IsWithinBounds(shape, CircleTier.Bark));
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
    static SlotKey C(int s) => new(CircleRing.Centre, s);
    static SlotKey I(int s) => new(CircleRing.Inner, s);
    static SlotKey O(int s) => new(CircleRing.Outer, s);

    [Fact]
    public void LinkedHeartsMixAndEachBodyFedByAHeartIsAWord()
    {
        var glyphs = new[] { C(0), C(1), C(2), I(0), I(1), O(0), I(3), O(5) };
        var links = new[] { (C(0), C(1)), (C(1), I(0)), (I(0), I(1)), (I(0), O(0)), (C(2), I(3)) };

        var mix = Assert.Single(CircleShape.MixGroups(glyphs, links));
        Assert.Equal(new[] { C(0), C(1) }, mix);

        var words = CircleShape.Words(glyphs, links);
        Assert.Equal(2, words.Count);
        Assert.Equal(new[] { I(0), I(1), O(0) }, words[0].Body);
        Assert.Equal(O(0), words[0].AimEnd);
        Assert.Equal(new[] { C(2) }, words[1].Heart);
        Assert.False(words[1].Aimed);
        // O(5) is linked to nothing: not part of any word.
        Assert.DoesNotContain(words, w => w.Body.Contains(O(5)));
    }

    [Fact]
    public void TheAimLineFollowsTheEndOfTheAimChain()
    {
        // Heart -> shape -> Foes -> Closest: the chain ends at O(1), wherever the slots happen to sit.
        var glyphs = new[] { C(0), I(1), O(2), O(1) };
        var links = new[] { (C(0), I(1)), (I(1), O(2)), (O(2), O(1)) };
        Assert.Equal(O(1), Assert.Single(CircleShape.Words(glyphs, links)).AimEnd);
    }

    [Fact]
    public void WordCirclesBiteTheBorder()
    {
        var (x, y, r) = CircleShape.WordCircle(new[] { (100.0, 0.0), (140.0, 0.0) }, 10, (0, 0), border: 200);
        Assert.Equal(0, y, 6);
        Assert.Equal(200 + r * 0.1, x + r, 6); // reaches past the border by 10% of its radius
    }

    [Fact]
    public void HeavierWordsAreBigger()
    {
        var glyphs = new[] { (100.0, 0.0), (140.0, 0.0) };
        double plain = CircleShape.WordCircle(glyphs, 10, (0, 0), 200).r;
        double strong = CircleShape.WordCircle(glyphs, 10, (0, 0), 200, extraWeight: 3).r;
        double capped = CircleShape.WordCircle(glyphs, 10, (0, 0), 200, extraWeight: 99).r;
        Assert.Equal(plain * 1.3, strong, 6);
        Assert.Equal(plain * 1.5, capped, 6);
        // However heavy, a word stays well inside the circle it bites.
        var spread = new[] { (-150.0, 0.0), (150.0, 0.0) };
        Assert.True(CircleShape.WordCircle(spread, 10, (0, 0), 200, extraWeight: 5).r <= 200 * 0.85);
    }

    [Fact]
    public void ChordRunsAcrossTheWholeCircle()
    {
        var chord = CircleShape.Chord((0, 0), (10, 0), (0, 0), 50);
        Assert.NotNull(chord);
        Assert.Equal((-50.0, 0.0), chord.Value.from);
        Assert.Equal((50.0, 0.0), chord.Value.to);
    }
}
