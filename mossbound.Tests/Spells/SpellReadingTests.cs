using Mossbound.Core;
using static Mossbound.Tests.TestGlyphs;

namespace Mossbound.Tests;

public class SpellReadingTests
{
    [Fact]
    public void GlyphSizeIsIntensity()
    {
        var strong = Place("ember", CircleRing.Heart);
        strong.Size = 3;
        Assert.Equal((Aspect.Ember, 3), Assert.Single(SpellReading.Read(new Circle { Glyphs = { strong } }, Script).Aspects));

        // Water at size 2 linked to a plain Frost: Ice at the smaller count (1), one Water left over.
        var water = Place("water", CircleRing.Heart);
        water.Size = 2;
        var mix = SpellReading.Read(new Circle { Glyphs = { water, Place("frost", CircleRing.Heart, 1) }, Links = { new CircleLink { From = 0, To = 1 } } }, Script);
        Assert.Contains((Aspect.Ice, 1), mix.Aspects);
    }

    [Fact]
    public void LinkedHeartsMix()
    {
        var circle = new Circle { Glyphs = { Place("water", CircleRing.Heart), Place("frost", CircleRing.Heart, 1) }, Links = { new CircleLink { From = 0, To = 1 } } };
        var plan = SpellReading.Read(circle, Script);
        Assert.False(plan.Fizzles);
        Assert.Equal(new[] { (Aspect.Ice, 1) }, plan.Aspects);
    }

    [Fact]
    public void UnlinkedHeartsStaySeparate()
    {
        // Opposites only cancel when they are linked into the same heart.
        var circle = new Circle { Glyphs = { Place("ember", CircleRing.Heart), Place("frost", CircleRing.Heart, 1) } };
        var plan = SpellReading.Read(circle, Script);
        Assert.False(plan.Fizzles);
        Assert.Equal(new[] { (Aspect.Ember, 1), (Aspect.Frost, 1) }, plan.Aspects);

        circle.Links.Add(new CircleLink { From = 0, To = 1 });
        Assert.Contains(Mishap.Opposition, SpellReading.Read(circle, Script).Mishaps);
    }

    [Fact]
    public void ElementOutsideTheHeartIsMisplaced()
    {
        var circle = new Circle { Glyphs = { Place("water", CircleRing.Heart), Place("frost", CircleRing.Aim) } };
        var plan = SpellReading.Read(circle, Script);
        Assert.Contains(Mishap.Misplaced, plan.Mishaps);
    }

    [Fact]
    public void UnknownFiguresAreIgnored()
    {
        var junk = new PlacedGlyph { Ring = CircleRing.Heart, Slot = 1, Lines = (1u << GlyphFigure.PairCount) - 1 };
        var circle = new Circle { Glyphs = { Place("ember", CircleRing.Heart), junk } };
        var plan = SpellReading.Read(circle, Script);
        Assert.Equal(new[] { (Aspect.Ember, 1) }, plan.Aspects);
    }
}
