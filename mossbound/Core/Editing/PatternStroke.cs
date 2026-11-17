using System.Collections.Generic;

namespace Mossbound.Core;

// A stroke being drawn on the glyph panel, like a phone's pattern lock: points snap in as the cursor
// passes over them, and moving back onto the previous point takes the last one off.
public sealed class PatternStroke
{
    readonly List<int> points = new();

    public IReadOnlyList<int> Points => points;

    public void Hover(int p)
    {
        if (points.Count > 0 && points[^1] == p) return;
        // A jump between points that no single line joins goes through the point in between, like a
        // phone lock adds a skipped dot. Hovering both makes backing off such a jump a single step.
        if (points.Count > 0 && GlyphFigure.PointBetween(points[^1], p) is int via)
        {
            int before = points.Count;
            Hover(via);
            if (points.Count != before) Hover(p); // in-between line already drawn: stop here
            return;
        }
        if (points.Count >= 2 && points[^2] == p)
        {
            points.RemoveAt(points.Count - 1);
            return;
        }
        if (points.Count > 0 && HasSegment(points[^1], p)) return;
        points.Add(p);
    }

    bool HasSegment(int a, int b)
    {
        for (int i = 1; i < points.Count; i++)
        {
            if ((points[i - 1] == a && points[i] == b) || (points[i - 1] == b && points[i] == a)) return true;
        }
        return false;
    }

    public IEnumerable<(int a, int b)> Segments()
    {
        for (int i = 1; i < points.Count; i++) yield return (points[i - 1], points[i]);
    }
}
