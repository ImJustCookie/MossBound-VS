using System.Collections.Generic;

namespace Mossbound.Core;

// The path of a link between two glyphs: straight, unless that would pass over a glyph it doesn't
// join (which would look like two links). Then it bows to one side, then the other, further each
// time, until it clears every obstacle. Returns the control point of a quadratic curve from a to b.
public static class LinkRoute
{
    static readonly double[] bows = { 0.35, -0.35, 0.6, -0.6, 0.85, -0.85 }; // share of the link's length
    const int Samples = 24;

    // `centre` only decides which side is tried first: toward it.
    public static (double x, double y) Control((double x, double y) a, (double x, double y) b, (double x, double y) centre,
        IReadOnlyList<(double x, double y, double r)> obstacles)
    {
        (double x, double y) middle = ((a.x + b.x) / 2, (a.y + b.y) / 2);
        if (Clears(a, middle, b, obstacles)) return middle;

        double length = Geometry.Distance(a, b);
        if (length < 1e-9) return middle;
        var (nx, ny) = (-(b.y - a.y) / length, (b.x - a.x) / length);
        if (nx * (centre.x - middle.x) + ny * (centre.y - middle.y) < 0) (nx, ny) = (-nx, -ny);
        foreach (double bow in bows)
        {
            (double x, double y) control = (middle.x + nx * bow * length, middle.y + ny * bow * length);
            if (Clears(a, control, b, obstacles)) return control;
        }
        return middle;
    }

    public static (double x, double y) PointOn((double x, double y) a, (double x, double y) control, (double x, double y) b, double t)
    {
        double u = 1 - t;
        return (u * u * a.x + 2 * u * t * control.x + t * t * b.x, u * u * a.y + 2 * u * t * control.y + t * t * b.y);
    }

    static bool Clears((double x, double y) a, (double x, double y) control, (double x, double y) b,
        IReadOnlyList<(double x, double y, double r)> obstacles)
    {
        for (int i = 1; i < Samples; i++)
        {
            var p = PointOn(a, control, b, (double)i / Samples);
            foreach (var o in obstacles)
                if (Geometry.Distance(p, (o.x, o.y)) < o.r) return false;
        }
        return true;
    }
}
