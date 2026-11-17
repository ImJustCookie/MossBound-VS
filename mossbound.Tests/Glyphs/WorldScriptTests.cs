using Mossbound.Core;

namespace Mossbound.Tests;

public class WorldScriptTests
{
    // Recorded from the original generator: a change here would change every existing world's Script.
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
    public void EveryGlyphIsUniqueRecognizableAndCarriesItsFamilyMark(long seed)
    {
        var Script = new WorldScript(seed);
        var figures = new HashSet<GlyphFigure>();
        foreach (var g in GlyphRegistry.All)
        {
            var f = Script.FigureOf(g.Code);
            Assert.True(f.IsValid);
            Assert.True(figures.Add(f));
            Assert.Same(g, Script.Recognize(f));

            var (point, mark) = Script.FamilyMarkOf(g.Kind);
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
        var Script = new WorldScript(7);
        var known = GlyphRegistry.All.Select(g => Script.FigureOf(g.Code)).ToHashSet();
        var all = GlyphFigure.Empty;
        for (int a = 0; a < GlyphFigure.PointCount; a++)
            for (int b = a + 1; b < GlyphFigure.PointCount; b++)
                all = all.WithLine(a, b);
        Assert.DoesNotContain(all, known);
        Assert.Null(Script.Recognize(all));
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
