using Mossbound.Core;
using static Mossbound.Tests.TestGlyphs;

namespace Mossbound.Tests;

public class CircleStructureTests
{
    [Fact]
    public void LinkedHeartsMixAndEachBodyFedByAHeartIsACast()
    {
        var glyphs = new[] { Heart(0), Heart(1), Heart(2), Shape(0), Shape(1), Aim(0), Shape(3), Aim(5) };
        var links = new[] { (Heart(0), Heart(1)), (Heart(1), Shape(0)), (Shape(0), Shape(1)), (Shape(0), Aim(0)), (Heart(2), Shape(3)) };

        var mix = Assert.Single(CircleStructure.Mixes(glyphs, links));
        Assert.Equal(new[] { Heart(0), Heart(1) }, mix);

        var words = CircleStructure.Casts(glyphs, links);
        Assert.Equal(2, words.Count);
        Assert.Equal(new[] { Shape(0), Shape(1), Aim(0) }, words[0].Body);
        Assert.Equal(Aim(0), words[0].AimEnd);
        Assert.Equal(new[] { Heart(2) }, words[1].Heart);
        Assert.False(words[1].IsAimed);
        // Aim(5) is linked to nothing: not part of any word.
        Assert.DoesNotContain(words, w => w.Body.Contains(Aim(5)));
    }

    [Fact]
    public void TheAimLineFollowsTheEndOfTheAimChain()
    {
        // Heart -> shape -> Foes -> Closest: the chain ends at Aim(1), wherever the slots happen to sit.
        var glyphs = new[] { Heart(0), Shape(1), Aim(2), Aim(1) };
        var links = new[] { (Heart(0), Shape(1)), (Shape(1), Aim(2)), (Aim(2), Aim(1)) };
        Assert.Equal(Aim(1), Assert.Single(CircleStructure.Casts(glyphs, links)).AimEnd);
    }
}
