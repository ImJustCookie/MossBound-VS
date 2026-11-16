using System;
using System.Collections.Generic;

namespace Mossbound.Script;

// Where a link between two glyphs bends. A link is a quadratic curve from a to b; by default it bows
// toward the canvas centre so it doesn't run along a ring line. If that curve would pass under a
// glyph it doesn't join (which would look like two links), it bows further, or to the other side,
// until it clears every such glyph.
public static class LinkRoute
{
    public const double DefaultBow = 0.3; // share of the way from the link's middle to the canvas centre
    static readonly double[] sideways = { 0.35, -0.35, 0.6, -0.6, 0.85, -0.85 }; // × link length
    const int Samples = 24;

    public static (double x, double y) Control((double x, double y) a, (double x, double y) b, (double x, double y) centre,
        IReadOnlyList<(double x, double y)> avoid, double clearance)
    {
        double mx = (a.x + b.x) / 2, my = (a.y + b.y) / 2;
        var preferred = (mx + (centre.x - mx) * DefaultBow, my + (centre.y - my) * DefaultBow);
        if (Clear(a, preferred, b, avoid, clearance)) return preferred;

        // Normal to the link, turned toward the canvas centre, so "+" keeps the usual bending side.
        double dx = b.x - a.x, dy = b.y - a.y, len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return preferred;
        double nx = -dy / len, ny = dx / len;
        if (nx * (centre.x - mx) + ny * (centre.y - my) < 0) (nx, ny) = (-nx, -ny);
        foreach (double s in sideways)
        {
            var q = (mx + nx * s * len, my + ny * s * len);
            if (Clear(a, q, b, avoid, clearance)) return q;
        }
        return preferred; // nothing clears: keep the usual curve
    }

    public static (double x, double y) PointOn((double x, double y) a, (double x, double y) q, (double x, double y) b, double t)
    {
        double u = 1 - t;
        return (u * u * a.x + 2 * u * t * q.x + t * t * b.x, u * u * a.y + 2 * u * t * q.y + t * t * b.y);
    }

    static bool Clear((double x, double y) a, (double x, double y) q, (double x, double y) b,
        IReadOnlyList<(double x, double y)> avoid, double clearance)
    {
        for (int i = 1; i < Samples; i++)
        {
            var p = PointOn(a, q, b, (double)i / Samples);
            foreach (var o in avoid)
            {
                if ((p.x - o.x) * (p.x - o.x) + (p.y - o.y) * (p.y - o.y) < clearance * clearance) return false;
            }
        }
        return true;
    }
}
