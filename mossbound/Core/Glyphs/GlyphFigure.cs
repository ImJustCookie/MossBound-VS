using System;
using System.Collections.Generic;

namespace Mossbound.Core;

// In the order a click cycles through them.
public enum Mark : byte
{
    None,
    Dot,
    BigDot,
    Ring,
    DoubleRing,
}

// A glyph as drawn: 7 points (0-5 on the rim, clockwise from the top, and the middle), the lines
// between them and a mark on each point. Only arcs (between rim neighbours) and spokes (to the middle)
// are allowed, so lines never cross. Two figures are the same glyph when both numbers are equal.
//   Lines: bit i set = the i-th point pair (pairs numbered row by row, see LineIndex).
//   Marks: 3 bits per point.
public readonly record struct GlyphFigure(uint Lines, uint Marks)
{
    public const int RimPoints = 6;
    public const int PointCount = 7;
    public const int Middle = 6;
    public const int PairCount = PointCount * (PointCount - 1) / 2;

    const int MarkBits = 3;
    const uint MarkMask = (1u << MarkBits) - 1;
    const uint AllMarkBits = (1u << (PointCount * MarkBits)) - 1;
    static readonly uint AllowedLines = AllowedLineBits();

    public static GlyphFigure Empty => new(0, 0);

    public bool IsEmpty => Lines == 0 && Marks == 0;

    public bool IsValid => !IsEmpty && (Lines & ~AllowedLines) == 0 && (Marks & ~AllMarkBits) == 0 && AllMarksKnown();

    public static bool IsArc(int a, int b) =>
        a < RimPoints && b < RimPoints && (Math.Abs(a - b) == 1 || Math.Abs(a - b) == RimPoints - 1);

    public static bool IsAllowedLine(int a, int b) => a != b && (IsArc(a, b) || a == Middle || b == Middle);

    // The point a line from a to b must pass through, or null if a-b is a single allowed line: two rim
    // steps apart go along the rim, opposite points go through the middle.
    public static int? PointBetween(int a, int b)
    {
        if (IsAllowedLine(a, b)) return null;
        return ((b - a + RimPoints) % RimPoints) switch
        {
            2 => (a + 1) % RimPoints,
            4 => (a + RimPoints - 1) % RimPoints,
            _ => Middle,
        };
    }

    public static int LineIndex(int a, int b)
    {
        if (a == b || a < 0 || b < 0 || a >= PointCount || b >= PointCount)
        {
            throw new ArgumentOutOfRangeException(nameof(b), $"invalid point pair {a}-{b}");
        }
        if (a > b) (a, b) = (b, a);
        return a * (2 * PointCount - a - 1) / 2 + (b - a - 1);
    }

    public bool HasLine(int a, int b) => (Lines & LineBit(a, b)) != 0;
    public GlyphFigure WithLine(int a, int b) => this with { Lines = Lines | LineBit(a, b) };
    public GlyphFigure WithoutLine(int a, int b) => this with { Lines = Lines & ~LineBit(a, b) };

    public Mark MarkAt(int point) => (Mark)((Marks >> (point * MarkBits)) & MarkMask);
    public GlyphFigure WithMark(int point, Mark mark) =>
        this with { Marks = (Marks & ~(MarkMask << (point * MarkBits))) | ((uint)mark << (point * MarkBits)) };

    public IEnumerable<(int a, int b)> LinePairs()
    {
        for (int a = 0; a < PointCount; a++)
            for (int b = a + 1; b < PointCount; b++)
                if (HasLine(a, b)) yield return (a, b);
    }

    static uint LineBit(int a, int b) => 1u << LineIndex(a, b);

    bool AllMarksKnown()
    {
        for (int p = 0; p < PointCount; p++)
            if (MarkAt(p) > Mark.DoubleRing) return false;
        return true;
    }

    static uint AllowedLineBits()
    {
        uint bits = 0;
        for (int a = 0; a < PointCount; a++)
            for (int b = a + 1; b < PointCount; b++)
                if (IsAllowedLine(a, b)) bits |= LineBit(a, b);
        return bits;
    }
}
