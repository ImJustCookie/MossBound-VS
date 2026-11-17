using Mossbound.Core;
using static Mossbound.Tests.TestGlyphs;

namespace Mossbound.Tests;

public class CircleTierTests
{
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
        var heartOnly = new Circle { Glyphs = { Place("life", CircleRing.Heart, 0), Place("life", CircleRing.Heart, 1) } };
        Assert.True(CircleValidation.FitsTier(heartOnly, CircleTier.Moss));

        var thirdHeartSlot = new Circle { Glyphs = { Place("life", CircleRing.Heart, 2) } };
        Assert.False(CircleValidation.FitsTier(thirdHeartSlot, CircleTier.Moss));
        Assert.True(CircleValidation.FitsTier(thirdHeartSlot, CircleTier.Bark));

        var shape = new Circle { Glyphs = { Place("life", CircleRing.Shape, 0) } };
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

    [Fact]
    public void LinksJoinNeighbouringRingsOrNeighbouringSlots()
    {
        var bark = CircleTier.Bark; // heart 3, shape 4
        Assert.True(bark.AllowsLink(Heart(0), new(CircleRing.Shape, 2)));                         // next ring
        Assert.True(bark.AllowsLink(new(CircleRing.Shape, 0), new(CircleRing.Shape, 3)));   // wraps
        Assert.False(bark.AllowsLink(new(CircleRing.Shape, 0), new(CircleRing.Shape, 2)));  // across
        Assert.False(CircleTier.Parchment.AllowsLink(Heart(0), Aim(3)));          // skips a ring
        Assert.False(bark.AllowsLink(Heart(0), Aim(3)));                          // no such slot
        Assert.False(bark.AllowsLink(Heart(0), Heart(0)));
    }
}
