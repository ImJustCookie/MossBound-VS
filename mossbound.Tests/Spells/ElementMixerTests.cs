using Mossbound.Core;

namespace Mossbound.Tests;

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
