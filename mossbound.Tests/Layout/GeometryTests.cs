using Mossbound.Core;

namespace Mossbound.Tests;

public class GeometryTests
{
    [Fact]
    public void LineAcrossCircleReachesBothEdges()
    {
        var chord = Geometry.LineAcrossCircle((0, 0), (10, 0), (0, 0), 50);
        Assert.NotNull(chord);
        Assert.Equal((-50.0, 0.0), chord.Value.from);
        Assert.Equal((50.0, 0.0), chord.Value.to);
    }
}
