using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Mossbound.Core;
using static Mossbound.Client.CanvasLayout;

namespace Mossbound.Client;

// Draws the inscription window's two panes: the circle (canvas) and the selected glyph (panel).
// Also used by tools/docs-images, so the docs show exactly what the game draws.
static class CanvasRenderer
{
    public static void DrawCanvas(Context target, double size, CircleDraft draft, CanvasLayout layout, InkStyle style, ImageSurface? art, CanvasView view)
    {
        var pen = new Pen(size, style);
        PaintBackground(target, art, size, round: true);
        Ink.Pixelated(target, size, pen.Pixel, ctx =>
        {
            ctx.PushGroup();
            CirclePainter.Structure(ctx, draft, layout, style, size, pen.Width);
            DrawLinks(ctx, draft, layout, style, size, pen, view);
            CutLinesUnderLetters(ctx, draft, layout, style, size);
            ctx.PopGroupToSource();
            ctx.Paint();
            foreach (var slot in layout.AllSlots())
            {
                DrawSlot(ctx, draft, layout, style, size, pen, slot);
                DrawSlotHighlight(ctx, draft, layout, size, pen, slot, view);
            }
        }, style.Noise);
    }

    public static void DrawPanel(Context target, double size, GlyphFigure figure, int seed, InkStyle style, ImageSurface? art, PanelView view)
    {
        double c = size / 2, r = size * PanelGlyphRadius;
        PaintBackground(target, art, size, round: false);
        Ink.Pixelated(target, size, Ink.PixelSize(size / PanelSize, style.PanelGrain), ctx =>
        {
            GlyphPainter.DrawGlyph(ctx, c, c, r, 0, figure, r * 0.07 * style.Weight, seed, style, LetterCircle.Guides);
            if (view.Stroke is { Points.Count: > 0 } stroke) DrawStrokeInProgress(ctx, c, r, seed, style, stroke, view);
        }, style.Noise);
    }

    // Line widths for one canvas: scaled with it, weighted by the tier's ink, never under one ink pixel.
    readonly record struct Pen(double Size, InkStyle Style)
    {
        public double Unit => Size / CanvasSize;
        public double Pixel => Ink.PixelSize(Unit, Style.CanvasGrain);
        public double Width(double width) => Math.Max(width * Style.Weight, Pixel);
    }

    static void PaintBackground(Context target, ImageSurface? art, double size, bool round)
    {
        if (Ink.PaintArt(target, art, size)) return;
        Ink.Use(target, Ink.Paper, 1);
        if (round) Ink.FillCircle(target, size / 2, size / 2, size * 0.49);
        else
        {
            target.Rectangle(0, 0, size, size);
            target.Fill();
        }
    }

    static double LetterRadius(CircleDraft draft, CanvasLayout layout, SlotKey slot, double size) => layout.LetterRadius(draft.SizeOf(slot)) * size;

    static bool HasDoubledLetter(CircleDraft draft, InkStyle style, SlotKey slot) => draft.SizeOf(slot) >= 3 && style.Doubles;

    // Straight links, bowing only around glyphs they don't join. The selected glyph's links are highlighted.
    static void DrawLinks(Context ctx, CircleDraft draft, CanvasLayout layout, InkStyle style, double size, Pen pen, CanvasView view)
    {
        ctx.LineWidth = pen.Width(2.8 * pen.Unit);
        foreach (var (a, b) in draft.Links)
        {
            bool highlighted = a == view.Selected || b == view.Selected;
            GlyphPainter.DrawLink(ctx, layout.SlotCentre(a, size), layout.SlotCentre(b, size), size / 2,
                Ink.Seed(GlyphPainter.SlotSeed(a), GlyphPainter.SlotSeed(b)), style, GlyphsInTheWay(draft, layout, size, a, b), highlighted ? Ink.Highlight : null);
        }
        if (view.LinkingFrom is SlotKey from)
        {
            Ink.Use(ctx, view.Removing ? Ink.Erase : Ink.Highlight, 0.8);
            GlyphPainter.DrawLink(ctx, layout.SlotCentre(from, size), view.Cursor, size / 2, seed: null, style);
        }
    }

    // Every written glyph a link from a to b must bend around: all but its own two ends.
    static List<(double x, double y, double r)> GlyphsInTheWay(CircleDraft draft, CanvasLayout layout, double size, SlotKey a, SlotKey b) =>
        draft.Figures.Keys.Where(k => k != a && k != b)
            .Select(k =>
            {
                var (x, y) = layout.SlotCentre(k, size);
                return (x, y, LetterRadius(draft, layout, k, size) * 1.3);
            })
            .ToList();

    // Letters cut every line they sit on, like Gallifreyan letters.
    static void CutLinesUnderLetters(Context ctx, CircleDraft draft, CanvasLayout layout, InkStyle style, double size)
    {
        ctx.Operator = Operator.Clear;
        foreach (var slot in draft.Figures.Keys)
        {
            var (x, y) = layout.SlotCentre(slot, size);
            Ink.FillCircle(ctx, x, y, LetterRadius(draft, layout, slot, size) * (HasDoubledLetter(draft, style, slot) ? 1.2 : 1.05));
        }
        ctx.Operator = Operator.Over;
    }

    static void DrawSlot(Context ctx, CircleDraft draft, CanvasLayout layout, InkStyle style, double size, Pen pen, SlotKey slot)
    {
        var (x, y) = layout.SlotCentre(slot, size);
        double glyphRadius = layout.GlyphRadius * size;
        if (!draft.IsWritten(slot))
        {
            Ink.Use(ctx, style.Colour, 0.25);
            Ink.StrokeCircle(ctx, x, y, glyphRadius, pen.Width(1.5 * pen.Unit));
            return;
        }
        double r = LetterRadius(draft, layout, slot, size);
        GlyphPainter.DrawGlyph(ctx, x, y, r, layout.GlyphRotation(slot), draft.FigureAt(slot), pen.Width(glyphRadius * 0.09),
            GlyphPainter.SlotSeed(slot), style, LetterCircle.Bold);
        if (HasDoubledLetter(draft, style, slot))
        {
            int seed = Ink.Seed(GlyphPainter.SlotSeed(slot), 11);
            Ink.Use(ctx, style.Stroke(seed), 0.9);
            Ink.StrokeCircle(ctx, x, y, r * 1.12, pen.Width(1.3 * pen.Unit), seed, rough: style.Roughness);
        }
    }

    // Amber ring on the selected slot. Fainter rings: while dragging a link, where it may go (or, red,
    // which links may be removed); otherwise, the glyphs linked to the selected one.
    static void DrawSlotHighlight(Context ctx, CircleDraft draft, CanvasLayout layout, double size, Pen pen, SlotKey slot, CanvasView view)
    {
        var (x, y) = layout.SlotCentre(slot, size);
        double r = LetterRadius(draft, layout, slot, size) * 1.4;
        if (slot == view.Selected)
        {
            Ink.Use(ctx, Ink.Highlight, 0.9);
            Ink.StrokeCircle(ctx, x, y, r, pen.Width(2 * pen.Unit));
            return;
        }
        bool hinted = view.LinkingFrom is SlotKey from
            ? view.Removing ? draft.HasLink(from, slot) : draft.CanLink(from, slot, layout.Tier)
            : view.Selected is SlotKey selected && draft.HasLink(selected, slot);
        if (!hinted) return;
        Ink.Use(ctx, view.LinkingFrom != null && view.Removing ? Ink.Erase : Ink.Highlight, 0.45);
        Ink.StrokeCircle(ctx, x, y, r, pen.Width(1.5 * pen.Unit));
    }

    // The sweep so far, drawn as it will look, then a loose line to the cursor. Red while erasing.
    static void DrawStrokeInProgress(Context ctx, double c, double r, int seed, InkStyle style, PatternStroke stroke, PanelView view)
    {
        Ink.Use(ctx, view.Removing ? Ink.Erase : Ink.Highlight, 0.85);
        ctx.LineWidth = r * 0.06 * style.Weight;
        foreach (var (a, b) in stroke.Segments()) GlyphPainter.DrawLine(ctx, c, c, r, 0, a, b, seed, style, ownColour: false);
        var (lx, ly) = RosettePoint(c, c, r, 0, stroke.Points[^1]);
        ctx.NewPath();
        ctx.MoveTo(lx, ly);
        ctx.LineTo(view.Cursor.x, view.Cursor.y);
        ctx.Stroke();
    }
}
