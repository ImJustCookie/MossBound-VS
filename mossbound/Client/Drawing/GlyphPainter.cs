using System;
using System.Collections.Generic;
using Cairo;
using Mossbound.Core;
using static Mossbound.Client.CanvasLayout;

namespace Mossbound.Client;

// How the circle around a glyph's 7 points is drawn.
enum LetterCircle
{
    Guides, // faint and exact, with the 7 points: the glyph panel and the admin atlas
    Bold,   // a solid letter, its arcs drawn just inside: a glyph written on the circle
    None,   // no circle at all: only what was drawn
}

// How a glyph, its lines and its marks, and the links between glyphs look.
static class GlyphPainter
{
    const double InnerArcScale = 0.78; // where arcs go inside a bold letter circle, so they still show

    // Tied to the slot, so a glyph wobbles the same way in both panes and keeps its look as lines are added.
    public static int SlotSeed(SlotKey k) => Ink.Seed((int)k.Ring + 1, k.Slot + 1);

    public static void DrawGlyph(Context ctx, double cx, double cy, double r, double rotation, GlyphFigure figure, double lineWidth,
        int seed, InkStyle style, LetterCircle letter, double guideAlpha = 0.3, double guideDot = 0.05)
    {
        ctx.PushGroup();
        DrawLetterCircle(ctx, cx, cy, r, rotation, lineWidth, seed, style, letter, guideAlpha, guideDot);
        ctx.LineWidth = lineWidth;
        double arcScale = letter == LetterCircle.Bold ? InnerArcScale : 1;
        foreach (var (a, b) in figure.LinePairs()) DrawLine(ctx, cx, cy, r, rotation, a, b, seed, style, arcScale: arcScale);
        CutLinesUnderMarks(ctx, cx, cy, r, rotation, figure, lineWidth);
        DrawMarks(ctx, cx, cy, r, rotation, figure, lineWidth, seed, style);
        ctx.PopGroupToSource();
        ctx.Paint();
    }

    // An arc along the rim between neighbouring points, or a spoke to the middle. `ownColour: false`
    // keeps the current colour (the highlighted stroke being drawn on the panel).
    public static void DrawLine(Context ctx, double cx, double cy, double r, double rotation, int a, int b, int glyphSeed, InkStyle style,
        bool ownColour = true, double arcScale = 1)
    {
        int seed = Ink.Seed(glyphSeed, a, b);
        if (ownColour) Ink.Use(ctx, style.Stroke(seed), 1);
        var rng = new Random(seed);
        ctx.NewPath();
        if (GlyphFigure.IsArc(a, b))
        {
            int start = b == (a + 1) % GlyphFigure.RimPoints ? a : b; // arcs run clockwise; 5->0 wraps
            double angle = rotation - Math.PI / 2 + start * Math.PI / 3;
            double overStart = Ink.Jitter(rng, 0.04 * style.Roughness), overEnd = Ink.Jitter(rng, 0.04 * style.Roughness);
            Ink.TraceWobblyArc(ctx, cx, cy, r * arcScale, angle - overStart, Math.PI / 3 + overStart + overEnd, glyphSeed, rough: style.Roughness);
        }
        else
        {
            var (ax, ay) = RosettePoint(cx, cy, r, rotation, a);
            var (bx, by) = RosettePoint(cx, cy, r, rotation, b);
            Ink.TraceHandLine(ctx, ax, ay, bx, by, rng, style.Roughness);
        }
        ctx.Stroke();
    }

    // A link between two glyphs (see LinkRoute for its path). Without a seed it is exact and uses the
    // current colour (the link being dragged); with one it is hand-drawn in the ink, or in `colour`.
    public static void DrawLink(Context ctx, (double x, double y) a, (double x, double y) b, double centre, int? seed, InkStyle style,
        IReadOnlyList<(double x, double y, double r)>? avoid = null, Ink.Colour? colour = null)
    {
        var (qx, qy) = LinkRoute.Control(a, b, (centre, centre), avoid ?? Array.Empty<(double, double, double)>());
        if (seed is int s)
        {
            Ink.Use(ctx, colour ?? style.Stroke(s), 1);
            var rng = new Random(s);
            double wobble = 0.02 * Geometry.Distance(a, b) * style.Roughness;
            qx += Ink.Jitter(rng, wobble);
            qy += Ink.Jitter(rng, wobble);
        }
        ctx.NewPath();
        ctx.MoveTo(a.x, a.y);
        Ink.QuadraticTo(ctx, a, (qx, qy), b);
        ctx.Stroke();
    }

    static void DrawLetterCircle(Context ctx, double cx, double cy, double r, double rotation, double lineWidth, int seed, InkStyle style,
        LetterCircle letter, double guideAlpha, double guideDot)
    {
        switch (letter)
        {
            case LetterCircle.Guides:
                Ink.Use(ctx, style.Colour, guideAlpha);
                Ink.StrokeCircle(ctx, cx, cy, r, lineWidth * 0.5);
                for (int k = 0; k < GlyphFigure.PointCount; k++)
                {
                    var (x, y) = RosettePoint(cx, cy, r, rotation, k);
                    Ink.FillCircle(ctx, x, y, r * guideDot);
                }
                break;
            case LetterCircle.Bold:
                Ink.Use(ctx, style.Stroke(Ink.Seed(seed, 99)), 1);
                Ink.StrokeCircle(ctx, cx, cy, r, lineWidth, seed, rough: style.Roughness);
                break;
        }
    }

    // Lines break around each mark, like a Gallifreyan vowel on its line.
    static void CutLinesUnderMarks(Context ctx, double cx, double cy, double r, double rotation, GlyphFigure figure, double lineWidth)
    {
        ctx.Operator = Operator.Clear;
        foreach (var (point, mark) in MarksOf(figure))
        {
            var (x, y) = RosettePoint(cx, cy, r, rotation, point);
            Ink.FillCircle(ctx, x, y, MarkRadius(r, mark) + lineWidth);
        }
        ctx.Operator = Operator.Over;
    }

    static void DrawMarks(Context ctx, double cx, double cy, double r, double rotation, GlyphFigure figure, double lineWidth, int seed, InkStyle style)
    {
        foreach (var (point, mark) in MarksOf(figure))
        {
            var (x, y) = RosettePoint(cx, cy, r, rotation, point);
            int markSeed = Ink.Seed(seed, point);
            Ink.Use(ctx, style.Stroke(markSeed), 1);
            DrawMark(ctx, x, y, r, mark, lineWidth, markSeed, style.Roughness);
        }
    }

    static IEnumerable<(int point, Mark mark)> MarksOf(GlyphFigure figure)
    {
        for (int p = 0; p < GlyphFigure.PointCount; p++)
            if (figure.MarkAt(p) != Mark.None) yield return (p, figure.MarkAt(p));
    }

    // The area a mark covers: dot < big dot < ring = double ring (a third of the glyph).
    static double MarkRadius(double r, Mark mark) => r * mark switch { Mark.Dot => 0.07, Mark.BigDot => 0.17, _ => 0.33 };

    static void DrawMark(Context ctx, double x, double y, double r, Mark mark, double lineWidth, int seed, double rough)
    {
        switch (mark)
        {
            case Mark.Dot:
            case Mark.BigDot:
                Ink.FillCircle(ctx, x, y, MarkRadius(r, mark));
                break;
            case Mark.Ring:
                Ink.StrokeCircle(ctx, x, y, r * 0.33, lineWidth, seed, rough: rough);
                break;
            case Mark.DoubleRing:
                Ink.StrokeCircle(ctx, x, y, r * 0.33, lineWidth, seed, rough: rough);
                Ink.StrokeCircle(ctx, x, y, r * 0.2, lineWidth, Ink.Seed(seed, 1), rough: rough);
                break;
        }
    }
}
