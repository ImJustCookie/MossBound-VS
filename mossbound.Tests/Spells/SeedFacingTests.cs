using Mossbound.Core;

namespace Mossbound.Tests;

// Rebuilds the model rotation the way the game does (pitch, then yaw plus 90 degrees, then roll) and checks the tip
// (-Z) ends up along the flight, whatever the flight and the spin.
public class SeedFacingTests
{
    [Theory]
    [InlineData(0, 0, 1, 0)]
    [InlineData(1, 0, 0, 1.3)]
    [InlineData(0.3, -0.9, 0.2, 4)]
    [InlineData(-0.5, 0.7, -0.4, 2.2)]
    [InlineData(0, -1, 0.01, 0.5)]
    public void TheTipPointsAlongTheFlight(double dx, double dy, double dz, double spin)
    {
        var (pitch, yaw, roll) = SeedFacing.For(dx, dy, dz, spin);
        var game = SeedFacing.Multiply(SeedFacing.Multiply(SeedFacing.RotX(pitch), SeedFacing.RotY(yaw + Math.PI / 2)), SeedFacing.RotZ(roll));
        double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        Assert.Equal(dx / length, -game[0, 2], 6);
        Assert.Equal(dy / length, -game[1, 2], 6);
        Assert.Equal(dz / length, -game[2, 2], 6);
    }
}
