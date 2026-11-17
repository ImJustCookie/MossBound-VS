using System;
using System.Collections.Generic;

namespace Mossbound.Script;

// Where a link between two glyphs bends. A link is a quadratic curve from a to b (its control point
// is returned); by default it is straight, like Gallifreyan lines. If it would pass within an
// obstacle's radius (a glyph it doesn't join, which would look like two links), it bows to one side,
// then the other, further each time, until it clears every obstacle.
public static class LinkRoute
{
    static readonly double[] sideways = { 0.35, -0.35, 0.6, -0.6, 0.85, -0.85 }; // × link length
    const int Samples = 24;

    // `centre` (the canvas centre) only picks which side to try first: toward it.
    public static (double x, double y) Control((double x, double y) a, (double x, double y) b, (double x, double y) centre,
        IReadOnlyList<(double x, double y, double r)> avoid)
    {
        double mx = (a.x + b.x) / 2, my = (a.y + b.y) / 2;
        var straight = (mx, my);
        if (Clear(a, straight, b, avoid)) return straight;

        double dx = b.x - a.x, dy = b.y - a.y, len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return straight;
        double nx = -dy / len, ny = dx / len;
        if (nx * (centre.x - mx) + ny * (centre.y - my) < 0) (nx, ny) = (-nx, -ny);
        foreach (double s in sideways)
        {
            var q = (mx + nx * s * len, my + ny * s * len);
            if (Clear(a, q, b, avoid)) return q;
        }
        return straight; // nothing clears: stay straight
    }

    public static (double x, double y) PointOn((double x, double y) a, (double x, double y) q, (double x, double y) b, double t)
    {
        double u = 1 - t;
        return (u * u * a.x + 2 * u * t * q.x + t * t * b.x, u * u * a.y + 2 * u * t * q.y + t * t * b.y);
    }

    static bool Clear((double x, double y) a, (double x, double y) q, (double x, double y) b,
        IReadOnlyList<(double x, double y, double r)> avoid)
    {
        for (int i = 1; i < Samples; i++)
        {
            var p = PointOn(a, q, b, (double)i / Samples);
            foreach (var o in avoid)
            {
                if ((p.x - o.x) * (p.x - o.x) + (p.y - o.y) * (p.y - o.y) < o.r * o.r) return false;
            }
        }
        return true;
    }
}
