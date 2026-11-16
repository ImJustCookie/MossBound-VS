using System;
using System.Collections.Generic;
using Cairo;
using Mossbound.Script;
using static Mossbound.Gui.CanvasLayout;

namespace Mossbound.Gui;

// How glyphs and links look. Readability rules borrowed from Circular Gallifreyan: strokes are arcs
// and radial spokes only, every glyph is a letter circle, marks sit on their point and break the lines
// around them, and line weights go from heavy (outer circle) to light (links).
static class GlyphPainter
{
    // Seed for a glyph's hand-drawn wobble: tied to its slot, so it looks the same in both panes and
    // doesn't change when lines are added.
    public static int SlotSeed(SlotKey k) => Ink.Seed((int)k.Ring + 1, k.Slot + 1);

    // `guides` = the panel's work view: exact faint letter circle plus the 7 snap points. A written
    // glyph gets a hand-drawn letter circle instead, with its strokes drawn heavier on top.
    // `style` gives the tier's ink colour, per-stroke colour variation and roughness. `guideAlpha` and `guideDot` set
    // how strongly the guides show (the admin atlas wants them clearer than the panel).
    public static void Rosette(Context ctx, double cx, double cy, double r, double rotation, GlyphFigure figure, bool guides, double lineWidth, int seed, InkStyle style, double guideAlpha = 0.3, double guideDot = 0.05)
    {
        ctx.PushGroup();

        Ink.Use(ctx, guides ? style.Colour : style.Stroke(Ink.Seed(seed, 99)), guides ? guideAlpha : 0.55);
        // The letter circle stays thin like an empty slot's outline; the strokes on top carry the weight.
        Ink.StrokeCircle(ctx, cx, cy, r, lineWidth * 0.5, guides ? null : seed, rough: style.Roughness);
        if (guides)
        {
            for (int k = 0; k < GlyphFigure.PointCount; k++)
            {
                var (x, y) = RosettePoint(cx, cy, r, rotation, k);
                Ink.FillCircle(ctx, x, y, r * guideDot);
            }
        }

        ctx.LineWidth = lineWidth;
        foreach (var (a, b) in figure.LinkPairs()) Segment(ctx, cx, cy, r, rotation, a, b, seed, style);

        // Break the lines around each mark, like a Gallifreyan vowel on its line.
        ctx.Operator = Operator.Clear;
        for (int k = 0; k < GlyphFigure.PointCount; k++)
        {
            var mark = figure.MarkAt(k);
            if (mark == Mark.None) continue;
            var (x, y) = RosettePoint(cx, cy, r, rotation, k);
            Ink.FillCircle(ctx, x, y, MarkRadius(r, mark) + lineWidth);
        }
        ctx.Operator = Operator.Over;

        for (int k = 0; k < GlyphFigure.PointCount; k++)
        {
            var mark = figure.MarkAt(k);
            if (mark == Mark.None) continue;
            var (x, y) = RosettePoint(cx, cy, r, rotation, k);
            int markSeed = Ink.Seed(seed, k);
            Ink.Use(ctx, style.Stroke(markSeed), 1);
            DrawMark(ctx, x, y, r, mark, lineWidth, markSeed, style.Roughness);
        }

        ctx.PopGroupToSource();
        ctx.Paint();
    }

    // One allowed stroke: an arc along the rim between neighbours, or a spoke to the centre. Arcs
    // share the glyph's rim wobble (so they sit on its letter circle); spokes bow a little.
    // `ownColour: false` keeps the current source colour (the green or red drawing preview).
    public static void Segment(Context ctx, double cx, double cy, double r, double rotation, int a, int b, int glyphSeed, InkStyle style, bool ownColour = true)
    {
        int seed = Ink.Seed(glyphSeed, a, b);
        if (ownColour) Ink.Use(ctx, style.Stroke(seed), 1);
        var rng = new Random(seed);
        double rough = style.Roughness;
        ctx.NewPath();
        if (GlyphFigure.IsArc(a, b))
        {
            // Clockwise from the lower point of the pair (5→0 wraps), overshooting a hair at each end.
            int start = (b == (a + 1) % GlyphFigure.EdgePoints) ? a : b;
            double angle = rotation - Math.PI / 2 + start * Math.PI / 3;
            double over0 = Ink.Jitter(rng, 0.04 * rough), over1 = Ink.Jitter(rng, 0.04 * rough);
            Ink.TraceWobblyArc(ctx, cx, cy, r, angle - over0, Math.PI / 3 + over0 + over1, glyphSeed, rough: rough);
        }
        else
        {
            var (ax, ay) = RosettePoint(cx, cy, r, rotation, a);
            var (bx, by) = RosettePoint(cx, cy, r, rotation, b);
            Ink.TraceHandLine(ctx, ax, ay, bx, by, rng, rough);
        }
        ctx.Stroke();
    }

    // A link between two glyphs, bowed toward the canvas centre so links don't run along the ring
    // lines, and around any glyph in `avoid` (see LinkRoute); its ends are hidden under the glyph
    // halos. No seed = exact, in the current colour (the drag preview); with a seed = hand-drawn, in
    // the tier's ink or in `colour` (a highlighted link keeps its exact hand-drawn shape).
    public static void Link(Context ctx, (double x, double y) a, (double x, double y) b, double centre, int? seed, InkStyle style,
        IReadOnlyList<(double x, double y)>? avoid = null, double clearance = 0, Ink.Colour? colour = null)
    {
        var (qx, qy) = LinkRoute.Control(a, b, (centre, centre), avoid ?? Array.Empty<(double, double)>(), clearance);
        if (seed is int s)
        {
            Ink.Use(ctx, colour ?? style.Stroke(s), 1);
            var rng = new Random(s);
            double len = Math.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));
            qx += Ink.Jitter(rng, 0.02 * len * style.Roughness);
            qy += Ink.Jitter(rng, 0.02 * len * style.Roughness);
        }
        ctx.NewPath();
        ctx.MoveTo(a.x, a.y);
        ctx.CurveTo(a.x + (qx - a.x) * 2 / 3, a.y + (qy - a.y) * 2 / 3, b.x + (qx - b.x) * 2 / 3, b.y + (qy - b.y) * 2 / 3, b.x, b.y);
        ctx.Stroke();
    }

    // Footprint of each mark (lines are cleared inside it): dot < big dot < ring = double ring (a third of the glyph).
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
