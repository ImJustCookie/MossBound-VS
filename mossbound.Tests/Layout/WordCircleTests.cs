using Mossbound.Core;

namespace Mossbound.Tests;

public class WordCircleTests
{
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
}
