using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

// Where a cast's word circle goes: around its glyphs, then pushed outward until it bites the border.
public static class WordCircle
{
    const double Margin = 1.18;          // how much bigger than its glyphs a word is drawn
    const double BiteDepth = 0.1;        // how far past the border it reaches, as a share of its radius
    const double GrowthPerWeight = 0.1;  // per size point past 1, summed over the cast's glyphs
    const int MaxWeight = 5;
    const double MaxShareOfBorder = 0.85;

    public static (double x, double y, double r) Place(IReadOnlyList<(double x, double y)> glyphCentres, double glyphRadius,
        (double x, double y) centre, double border, int extraWeight = 0)
    {
        var middle = Geometry.Centroid(glyphCentres);
        double radius = glyphCentres.Max(p => Geometry.Distance(p, middle)) + glyphRadius * 0.6;
        radius *= Margin * (1 + GrowthPerWeight * Math.Clamp(extraWeight, 0, MaxWeight));
        radius = Math.Min(radius, border * MaxShareOfBorder);

        double fromCentre = Geometry.Distance(middle, centre);
        if (fromCentre < 1e-9) return (middle.x, middle.y, radius);
        double biting = Math.Max(0, border + radius * BiteDepth - radius);
        double angle = Math.Atan2(middle.y - centre.y, middle.x - centre.x);
        var (x, y) = Geometry.OnCircle(centre, biting, angle);
        return (x, y, radius);
    }
}
