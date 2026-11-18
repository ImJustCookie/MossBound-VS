using Mossbound.Core;

namespace Mossbound.Tests;

public class RecipesTests
{
    static (Aspect? aspect, Mishap? mishap) Heart(params Element[] e) => Recipes.Hearts.Mix(e);

    [Fact]
    public void EveryElementAloneIsItsOwnAspect()
    {
        foreach (var element in Enum.GetValues<Element>())
            Assert.Equal(Enum.Parse<Aspect>(element.ToString()), Heart(element).aspect);
    }

    [Fact]
    public void AMixIgnoresTheOrderOfItsGlyphs()
    {
        Assert.Equal(Aspect.Steam, Heart(Element.Water, Element.Ember).aspect);
        Assert.Equal(Aspect.Steam, Heart(Element.Ember, Element.Water).aspect);
    }

    [Fact]
    public void GlyphsWithNoRecipeMixIntoNothing()
    {
        Assert.Equal(Mishap.NoMix, Heart(Element.Ember, Element.Ember).mishap); // a repeated glyph counts twice
        Assert.Equal(Mishap.NoMix, Heart(Element.Frost, Element.Stone).mishap); // no such mix yet
        Assert.Equal(Mishap.NoMix, Heart(Element.Water, Element.Ember, Element.Life).mishap);
    }

    [Theory]
    [InlineData(Element.Ember, Element.Frost)]
    [InlineData(Element.Wind, Element.Ward)]
    public void OppositesFailHarder(Element a, Element b) => Assert.Equal(Mishap.Opposition, Heart(a, b).mishap);

    [Fact]
    public void FormsMixByTheSameRule()
    {
        Assert.Equal(Form.Seed, Recipes.Forms.Mix(new[] { Form.Seed }).result);
        Assert.Equal(Mishap.NoMix, Recipes.Forms.Mix(new[] { Form.Seed, Form.Ring }).mishap);
    }
}
