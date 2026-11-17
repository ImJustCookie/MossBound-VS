using Mossbound.Core;

namespace Mossbound.Tests;

public class LinkRouteTests
{
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
}
