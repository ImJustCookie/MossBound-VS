using Mossbound.Core;
using static Mossbound.Tests.TestGlyphs;

namespace Mossbound.Tests;

public class SpellReadingTests
{
    static SpellPlan Read(CircleTier tier, PlacedGlyph[] glyphs, params (int from, int to)[] links)
    {
        var circle = new Circle();
        circle.Glyphs.AddRange(glyphs);
        foreach (var (from, to) in links) circle.Links.Add(new CircleLink { From = from, To = to });
        return SpellReading.Read(circle, Script, tier);
    }

    static PlacedGlyph Sized(string code, CircleRing ring, int slot, int size)
    {
        var glyph = Place(code, ring, slot);
        glyph.Size = size;
        return glyph;
    }

    [Fact]
    public void AHeartAloneIsOneTouchSpell() =>
        Assert.Equal(new SpellCell(Aspect.Ember, Form.Touch, 2, 0),
            Assert.Single(Read(CircleTier.Moss, new[] { Sized("ember", CircleRing.Heart, 0, 2) }).Cells));

    [Fact]
    public void SizesSetStrengthNotTheMix()
    {
        var plan = Read(CircleTier.Bark, new[] { Sized("water", CircleRing.Heart, 0, 2), Place("ember", CircleRing.Heart, 1) }, (0, 1));
        Assert.Equal(new SpellCell(Aspect.Steam, Form.Touch, 3, 0), Assert.Single(plan.Cells));
    }

    [Fact]
    public void StrengthOverTheTierLimitOverloads()
    {
        var plan = Read(CircleTier.Moss, new[] { Sized("water", CircleRing.Heart, 0, 2), Place("ember", CircleRing.Heart, 1) }, (0, 1));
        Assert.Equal(new[] { Mishap.Overload }, plan.Mishaps);
        Assert.Empty(plan.Cells);
    }

    [Fact]
    public void ALinkedFormGivesItsFormAndScale()
    {
        var plan = Read(CircleTier.Bark, new[] { Place("ember", CircleRing.Heart, 0), Sized("seed", CircleRing.Shape, 0, 2) }, (0, 1));
        Assert.Equal(new SpellCell(Aspect.Ember, Form.Seed, 1, 2), Assert.Single(plan.Cells));
    }

    [Fact]
    public void ScaleOverTheTierLimitOverloads()
    {
        var plan = Read(CircleTier.Bark, new[] { Place("ember", CircleRing.Heart, 0), Sized("seed", CircleRing.Shape, 0, 3) }, (0, 1));
        Assert.Equal(new[] { Mishap.Oversized }, plan.Mishaps);
    }

    [Fact]
    public void LinkedFormsMixIntoOneForm()
    {
        // Seed + Ring has no recipe yet: the group mixes into nothing.
        var plan = Read(CircleTier.Bark, new[] { Place("ember", CircleRing.Heart, 0), Place("seed", CircleRing.Shape, 0), Place("ring", CircleRing.Shape, 1) },
            (0, 1), (1, 2));
        Assert.Equal(new[] { Mishap.NoMix }, plan.Mishaps);
    }

    [Fact]
    public void LooseFormsAreSeparateSpells()
    {
        var plan = Read(CircleTier.Bark, new[] { Place("ember", CircleRing.Heart, 0), Place("seed", CircleRing.Shape, 0), Place("ring", CircleRing.Shape, 2) },
            (0, 1), (0, 2));
        Assert.Equal(new[] { Form.Seed, Form.Ring }, plan.Cells.Select(c => c.Form).Order());
    }

    [Fact]
    public void MoreSpellsThanTheTierAllowsOverload()
    {
        // Two separate hearts on moss: two spells where moss allows one.
        var plan = Read(CircleTier.Moss, new[] { Place("ember", CircleRing.Heart, 0), Place("water", CircleRing.Heart, 1) });
        Assert.Equal(new[] { Mishap.TooManySpells }, plan.Mishaps);
        Assert.Equal(2, Read(CircleTier.Bark, new[] { Place("ember", CircleRing.Heart, 0), Place("water", CircleRing.Heart, 1) }).Cells.Count);
    }

    [Fact]
    public void OppositesOnlyFailWhenLinked()
    {
        var glyphs = new[] { Place("ember", CircleRing.Heart, 0), Place("frost", CircleRing.Heart, 1) };
        Assert.Contains(Mishap.Opposition, Read(CircleTier.Bark, glyphs, (0, 1)).Mishaps);
        Assert.Equal(2, Read(CircleTier.Bark, glyphs).Cells.Count);
    }

    [Fact]
    public void GlyphsOnTheWrongRingAreMisplaced()
    {
        Assert.Contains(Mishap.Misplaced, Read(CircleTier.Bark, new[] { Place("ember", CircleRing.Heart, 0), Place("water", CircleRing.Shape, 0) }).Mishaps);
        Assert.Contains(Mishap.Misplaced, Read(CircleTier.Bark, new[] { Place("ember", CircleRing.Heart, 0), Place("seed", CircleRing.Heart, 1) }).Mishaps);
    }

    [Fact]
    public void NoElementIsNothingToCast() =>
        Assert.Contains(Mishap.NoElement, Read(CircleTier.Bark, new[] { Place("seed", CircleRing.Shape, 0) }).Mishaps);

    [Fact]
    public void UnknownFiguresAreIgnoredUnlessLinked()
    {
        var junk = new PlacedGlyph { Ring = CircleRing.Shape, Slot = 0, Lines = (1u << GlyphFigure.PairCount) - 1 };
        var glyphs = new[] { Place("ember", CircleRing.Heart, 0), junk };
        Assert.Equal(Aspect.Ember, Assert.Single(Read(CircleTier.Bark, glyphs).Cells).Aspect);
        Assert.Equal(new[] { Mishap.Unknown }, Read(CircleTier.Bark, glyphs, (0, 1)).Mishaps);
    }

    [Fact]
    public void OnlyOppositesAndLimitsBackfire()
    {
        Assert.True(Mishap.Opposition.Backfires());
        Assert.True(Mishap.TooManySpells.Backfires());
        Assert.False(Mishap.NoMix.Backfires());
        Assert.False(Mishap.Unknown.Backfires());
    }
}
