using System;
using System.Collections.Generic;

namespace Mossbound.Script;

// Gallifreyan-style decorations, in the order a click cycles through them.
public enum Mark : byte
{
    None,
    Dot,
    BigDot,
    Ring,
    DoubleRing,
}

// A rosette glyph: 7 points (0..5 on the edge clockwise from the top, 6 = centre).
// Links: bit i of Links = the i-th unordered point pair (21 pairs), but only 12 are allowed: the 6
// arcs between neighbouring edge points and the 6 spokes to the centre. Like Gallifreyan, strokes are
// curves and radial lines only, so they never cross except at the centre.
// Marks: 3 bits per point. Equality of the two numbers = same glyph (order of strokes is irrelevant).
public readonly record struct GlyphFigure(uint Links, uint Marks)
{
    public const int EdgePoints = 6;
    public const int PointCount = 7;
    public const int Centre = 6;
    public const int LinkCount = PointCount * (PointCount - 1) / 2;

    const uint AllLinks = (1u << LinkCount) - 1;
    const int MarkBits = 3;
    const uint MarkMask = (1u << MarkBits) - 1;
    const uint AllMarks = (1u << (PointCount * MarkBits)) - 1;

    static readonly uint AllowedLinks = ComputeAllowedLinks();

    // Needs at least one line or one mark; dot-only glyphs are allowed.
    public bool IsValid =>
        (Links != 0 || Marks != 0) && (Links & ~AllowedLinks) == 0 && (Marks & ~AllMarks) == 0 && MarksInRange();

    bool MarksInRange()
    {
        for (int p = 0; p < PointCount; p++) if (MarkAt(p) > Mark.DoubleRing) return false;
        return true;
    }

    public static bool IsAllowedLink(int a, int b) => a != b && (IsArc(a, b) || a == Centre || b == Centre);

    // The point a stroke must pass through to get from a to b with allowed links, or null if a-b is
    // allowed directly: two steps apart on the rim go along the rim, opposite points through the centre.
    public static int? Via(int a, int b)
    {
        if (a == Centre || b == Centre || IsArc(a, b)) return null;
        return ((b - a + EdgePoints) % EdgePoints) switch
        {
            2 => (a + 1) % EdgePoints,
            4 => (a + EdgePoints - 1) % EdgePoints,
            _ => Centre,
        };
    }

    static uint ComputeAllowedLinks()
    {
        uint mask = 0;
        for (int a = 0; a < PointCount; a++)
            for (int b = a + 1; b < PointCount; b++)
                if (IsAllowedLink(a, b)) mask |= 1u << LinkIndex(a, b);
        return mask;
    }

    public static int LinkIndex(int a, int b)
    {
        if (a == b || a < 0 || b < 0 || a >= PointCount || b >= PointCount)
        {
            throw new ArgumentOutOfRangeException(nameof(b), $"invalid point pair {a}-{b}");
        }
        if (a > b) (a, b) = (b, a);
        // Pairs (0,1),(0,2)..(0,6),(1,2).. numbered row by row.
        return a * (2 * PointCount - a - 1) / 2 + (b - a - 1);
    }

    public static bool IsArc(int a, int b) =>
        a < EdgePoints && b < EdgePoints && (Math.Abs(a - b) == 1 || Math.Abs(a - b) == EdgePoints - 1);

    public bool HasLink(int a, int b) => (Links & (1u << LinkIndex(a, b))) != 0;
    public GlyphFigure WithLink(int a, int b) => this with { Links = Links | (1u << LinkIndex(a, b)) };
    public GlyphFigure WithoutLink(int a, int b) => this with { Links = Links & ~(1u << LinkIndex(a, b)) };

    public Mark MarkAt(int point) => (Mark)((Marks >> (point * MarkBits)) & MarkMask);
    public GlyphFigure WithMark(int point, Mark mark) =>
        this with { Marks = (Marks & ~(MarkMask << (point * MarkBits))) | ((uint)mark << (point * MarkBits)) };

    public IEnumerable<(int a, int b)> LinkPairs()
    {
        for (int a = 0; a < PointCount; a++)
        {
            for (int b = a + 1; b < PointCount; b++)
            {
                if (HasLink(a, b)) yield return (a, b);
            }
        }
    }
}
