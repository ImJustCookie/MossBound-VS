using Mossbound.Core;
using ProtoBuf;
using static Mossbound.Tests.TestGlyphs;

namespace Mossbound.Tests;

public class CircleTests
{
    [Fact]
    public void CircleSurvivesNetworkSerialization()
    {
        var circle = new Circle
        {
            Glyphs = { Place("water", CircleRing.Heart), Place("wind", CircleRing.Heart, 3) },
            Links = { new CircleLink { From = 0, To = 1 } },
        };
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, circle);
        ms.Position = 0;
        var back = Serializer.Deserialize<Circle>(ms);

        Assert.Equal(circle.Glyphs.Select(g => (g.Ring, g.Slot, g.Figure)), back.Glyphs.Select(g => (g.Ring, g.Slot, g.Figure)));
        Assert.Equal(1, back.Links.Single().To);
    }
}
