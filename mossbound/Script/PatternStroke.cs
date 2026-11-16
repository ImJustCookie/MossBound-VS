using System.Collections.Generic;

namespace Mossbound.Script;

// A pattern-lock style stroke being drawn on a rosette (arcs and spokes only): points snap in as the cursor passes over
// them; moving back onto the previous point un-snaps the last one.
public sealed class PatternStroke
{
    readonly List<int> points = new();

    public IReadOnlyList<int> Points => points;

    public void Hover(int p)
    {
        if (points.Count > 0 && points[^1] == p) return;
        // Only arcs and spokes exist, so a jump is routed through the point in between (along the rim,
        // or through the centre), like a phone lock auto-adds a skipped dot. Going through Hover twice
        // also makes backing off such a jump a single step.
        if (points.Count > 0 && GlyphFigure.Via(points[^1], p) is int via)
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
