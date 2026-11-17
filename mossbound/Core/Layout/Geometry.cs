using System;

namespace Mossbound.Core;

public static class Geometry
{
    public static double Distance((double x, double y) a, (double x, double y) b) =>
        Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));

    public static (double x, double y) OnCircle((double x, double y) centre, double radius, double angle) =>
        (centre.x + Math.Cos(angle) * radius, centre.y + Math.Sin(angle) * radius);

    // Where the straight line through a and b crosses a circle, at both ends; null if it misses.
    public static ((double x, double y) from, (double x, double y) to)? LineAcrossCircle(
        (double x, double y) a, (double x, double y) b, (double x, double y) centre, double radius)
    {
        double length = Distance(a, b);
        if (length < 1e-9) return null;
        double dx = (b.x - a.x) / length, dy = (b.y - a.y) / length;
        double px = a.x - centre.x, py = a.y - centre.y;
        double along = px * dx + py * dy, offCentre = px * px + py * py - radius * radius;
        double discriminant = along * along - offCentre;
        if (discriminant < 0) return null;
        double half = Math.Sqrt(discriminant);
        (double, double) At(double t) => (a.x + dx * t, a.y + dy * t);
        return (At(-along - half), At(-along + half));
    }
}
