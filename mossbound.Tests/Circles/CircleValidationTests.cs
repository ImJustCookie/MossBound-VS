using Mossbound.Core;
using static Mossbound.Tests.TestGlyphs;

namespace Mossbound.Tests;

public class CircleValidationTests
{
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
    public void SourceIsChosenFromBarkUpAndMustBeKnown()
    {
        var land = new Circle { Glyphs = { Place("ember", CircleRing.Heart) }, Source = LifeSource.Land };
        Assert.True(CircleValidation.FitsTier(land, CircleTier.Bark));
        Assert.False(CircleValidation.FitsTier(land, CircleTier.Moss));
        land.Source = (LifeSource)42;
        Assert.False(CircleValidation.FitsTier(land, CircleTier.Stone));
    }

    [Fact]
    public void SizesAreCappedPerTier()
    {
        var strong = Place("ember", CircleRing.Heart);
        strong.Size = 3;
        Assert.True(CircleValidation.FitsTier(new Circle { Glyphs = { strong } }, CircleTier.Bark));
        Assert.False(CircleValidation.FitsTier(new Circle { Glyphs = { strong } }, CircleTier.Moss)); // moss stops at 2
        var negative = Place("ember", CircleRing.Heart);
        negative.Size = -1;
        Assert.False(CircleValidation.FitsTier(new Circle { Glyphs = { negative } }, CircleTier.Stone));
    }

    [Fact]
    public void InscribedLinksMustFollowTheGrammar()
    {
        var neighbours = new Circle
        {
            Glyphs = { Place("water", CircleRing.Heart), Place("life", CircleRing.Shape) },
            Links = { new CircleLink { From = 0, To = 1 } },
        };
        Assert.True(CircleValidation.LinksFollowTheGrammar(neighbours, CircleTier.Parchment));

        var skipsARing = new Circle
        {
            Glyphs = { Place("water", CircleRing.Heart), Place("life", CircleRing.Aim) },
            Links = { new CircleLink { From = 0, To = 1 } },
        };
        Assert.True(CircleValidation.FitsTier(skipsARing, CircleTier.Parchment));
        Assert.False(CircleValidation.LinksFollowTheGrammar(skipsARing, CircleTier.Parchment));
    }

    [Fact]
    public void StripsWrittenBeforeSizesReadAsSizeOne() =>
        Assert.Equal(1, Place("ember", CircleRing.Heart).EffectiveSize);
}
