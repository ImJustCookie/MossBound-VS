using System;

namespace Mossbound.Core;

// The angles that point a seed model's tip (-Z) along its flight, tipped up or down with it, spun around its length.
// The game turns an entity's model by roll (Z), then yaw (Y, plus 90 degrees), then pitch (world X), so the wanted
// rotation (spin, then tip up or down, then turn toward the flight) is rebuilt and split into those three angles.
public static class SeedFacing
{
    public static (double Pitch, double Yaw, double Roll) For(double dx, double dy, double dz, double spin)
    {
        double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (length < 1e-9) return (0, 0, spin);
        double turn = Math.Atan2(-dx, -dz), tip = Math.Asin(Math.Clamp(dy / length, -1, 1));
        var m = Multiply(Multiply(RotY(turn), RotX(tip)), RotZ(spin));
        double yaw = Math.Asin(Math.Clamp(m[0, 2], -1, 1));
        return (Math.Atan2(-m[1, 2], m[2, 2]), yaw - Math.PI / 2, Math.Atan2(-m[0, 1], m[0, 0]));
    }

    public static double[,] RotX(double a) => new[,] { { 1, 0, 0 }, { 0, Math.Cos(a), -Math.Sin(a) }, { 0, Math.Sin(a), Math.Cos(a) } };
    public static double[,] RotY(double a) => new[,] { { Math.Cos(a), 0, Math.Sin(a) }, { 0, 1, 0 }, { -Math.Sin(a), 0, Math.Cos(a) } };
    public static double[,] RotZ(double a) => new[,] { { Math.Cos(a), -Math.Sin(a), 0 }, { Math.Sin(a), Math.Cos(a), 0 }, { 0, 0, 1 } };

    public static double[,] Multiply(double[,] a, double[,] b)
    {
        var m = new double[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++) m[i, j] += a[i, k] * b[k, j];
        return m;
    }
}
