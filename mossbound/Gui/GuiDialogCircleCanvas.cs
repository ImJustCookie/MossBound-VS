using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Mossbound.Script;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using static Mossbound.Gui.CanvasLayout;

namespace Mossbound.Gui;

// The inscription table.
// Canvas (left): the main circle. Click a slot to select it; drag from one glyph to another to link them.
// Panel (right): the selected glyph, enlarged. Sweep across points to draw (pattern-lock style), sweep
// with the right button to erase, click a point to cycle its mark, right-click to clear it.
public class GuiDialogCircleCanvas : GuiDialog
{
    const double Gap = 20;

    readonly CircleDraft draft;
    readonly CanvasLayout layout;
    readonly InkStyle style;
    readonly ImageSurface? canvasArt;
    readonly ImageSurface? panelArt;
    ElementBounds canvasBounds = null!;
    ElementBounds panelBounds = null!;

    SlotKey selected = new(CircleRing.Centre, 0);
    SlotKey? dragSlot;         // canvas: glyph a link is being dragged from
    PatternStroke? stroke;     // panel: lines being drawn, or erased if `erasing`
    bool erasing;              // right button: the panel sweep or the canvas drag removes instead of adds
    int? pressedPoint;         // panel: point under the cursor at press, to tell a click from a stroke
    double mouseX, mouseY;     // local to the pane being dragged in

    public override string? ToggleKeyCombinationCode => null;

    // The tier comes from the medium being written: it decides the rings, the slots and the art.
    public GuiDialogCircleCanvas(ICoreClientAPI capi, Circle? existing, CircleTier tier) : base(capi)
    {
        draft = CircleDraft.FromCircle(existing);
        layout = new CanvasLayout(tier);
        style = InkStyle.For(tier);
        canvasArt = Ink.LoadArt(capi, $"mossbound:textures/gui/{tier.Art}/canvas.png");
        panelArt = Ink.LoadArt(capi, $"mossbound:textures/gui/{tier.Art}/glyphpanel.png");
        Compose();
    }

    void Compose()
    {
        double top = GuiStyle.TitleBarHeight, right = CanvasSize + Gap;
        canvasBounds = ElementBounds.Fixed(0, top, CanvasSize, CanvasSize);
        panelBounds = ElementBounds.Fixed(right, top, PanelSize, PanelSize);
        var hintBounds = ElementBounds.Fixed(right, top + PanelSize + 10, PanelSize, 110);
        var clearBounds = ElementBounds.Fixed(right, top + CanvasSize - 30, 120, 30);
        var inscribeBounds = ElementBounds.Fixed(right + PanelSize - 120, top + CanvasSize - 30, 120, 30);

        var bg = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bg.BothSizing = ElementSizing.FitToChildren;
        bg.WithChildren(canvasBounds, panelBounds, hintBounds, clearBounds, inscribeBounds);

        SingleComposer = capi.Gui
            .CreateCompo("mossbound-circlecanvas", ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
            .AddShadedDialogBG(bg)
            .AddDialogTitleBar(Lang.Get("mossbound:canvas-title"), () => TryClose())
            .AddDynamicCustomDraw(canvasBounds, DrawCanvas, "canvas")
            .AddDynamicCustomDraw(panelBounds, DrawPanel, "panel")
            .AddStaticText(Lang.Get("mossbound:canvas-hint"), CairoFont.WhiteDetailText(), hintBounds)
            .AddSmallButton(Lang.Get("mossbound:canvas-clear"), OnClear, clearBounds)
            .AddSmallButton(Lang.Get("mossbound:canvas-inscribe"), OnInscribe, inscribeBounds)
            .Compose();
    }

    void Redraw(bool canvas = true, bool panel = true)
    {
        if (canvas) SingleComposer.GetCustomDraw("canvas").Redraw();
        if (panel) SingleComposer.GetCustomDraw("panel").Redraw();
    }

    // ---------- input ----------

    static (double x, double y) Local(ElementBounds b, MouseEvent e) => (e.X - b.absX, e.Y - b.absY);

    public override void OnMouseDown(MouseEvent args)
    {
        bool left = args.Button == EnumMouseButton.Left, right = args.Button == EnumMouseButton.Right;
        if ((left || right) && panelBounds.PointInside(args.X, args.Y))
        {
            (mouseX, mouseY) = Local(panelBounds, args);
            stroke = new PatternStroke();
            erasing = right;
            pressedPoint = PointAt(mouseX, mouseY, panelBounds.InnerWidth);
            if (pressedPoint is int p) stroke.Hover(p);
            args.Handled = true;
            return;
        }
        if ((left || right) && canvasBounds.PointInside(args.X, args.Y))
        {
            (mouseX, mouseY) = Local(canvasBounds, args);
            erasing = right;
            dragSlot = layout.SlotAt(mouseX, mouseY, canvasBounds.InnerWidth);
            // A click on the border (not on a slot) cycles where the spell takes its life from.
            if (dragSlot == null && layout.Tier.ChoosesSource && CirclePainter.OnBorder(mouseX, mouseY, canvasBounds.InnerWidth))
            {
                int count = Enum.GetValues<LifeSource>().Length;
                draft.Source = (LifeSource)(((int)draft.Source + (right ? count - 1 : 1)) % count);
                Redraw(panel: false);
            }
            args.Handled = true;
            return;
        }
        base.OnMouseDown(args);
    }

    public override void OnMouseMove(MouseEvent args)
    {
        if (stroke != null)
        {
            (mouseX, mouseY) = Local(panelBounds, args);
            if (PointAt(mouseX, mouseY, panelBounds.InnerWidth) is int p) stroke.Hover(p);
            Redraw(canvas: false);
            args.Handled = true;
            return;
        }
        if (dragSlot != null)
        {
            (mouseX, mouseY) = Local(canvasBounds, args);
            Redraw(panel: false);
            args.Handled = true;
            return;
        }
        base.OnMouseMove(args);
    }

    // The wheel over a written glyph on the circle sets how strongly it is written (its size).
    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        int mx = capi.Input.MouseX, my = capi.Input.MouseY;
        if (canvasBounds.PointInside(mx, my)
            && layout.SlotAt(mx - canvasBounds.absX, my - canvasBounds.absY, canvasBounds.InnerWidth) is SlotKey k
            && draft.Figures.ContainsKey(k))
        {
            draft.SetSize(k, Math.Clamp(draft.SizeOf(k) + Math.Sign(args.delta), 1, layout.Tier.MaxSize));
            Redraw(panel: false);
            args.SetHandled(true);
            return;
        }
        base.OnMouseWheel(args);
    }

    public override void OnMouseUp(MouseEvent args)
    {
        if (stroke != null)
        {
            ApplyStroke();
            stroke = null;
            pressedPoint = null;
            Redraw();
            args.Handled = true;
            return;
        }
        if (dragSlot is SlotKey start)
        {
            var (x, y) = Local(canvasBounds, args);
            if (layout.SlotAt(x, y, canvasBounds.InnerWidth) is SlotKey end)
            {
                // Left drag adds a link, right drag removes one; a left click selects.
                if (end == start) { if (!erasing) selected = start; }
                else if (erasing) draft.Links.Remove(CircleDraft.Normalize(start, end));
                else if (CanLinkTo(start, end)) draft.Links.Add(CircleDraft.Normalize(start, end));
            }
            dragSlot = null;
            Redraw();
            args.Handled = true;
            return;
        }
        base.OnMouseUp(args);
    }

    // A sweep adds (or erases) its lines; a click without a sweep cycles (or clears) the mark.
    void ApplyStroke()
    {
        var figure = draft.Figures.GetValueOrDefault(selected);
        if (stroke!.Points.Count >= 2)
        {
            foreach (var (a, b) in stroke.Segments()) figure = erasing ? figure.WithoutLink(a, b) : figure.WithLink(a, b);
        }
        else if (pressedPoint is int p && stroke.Points.Count == 1)
        {
            figure = figure.WithMark(p, erasing ? Mark.None : NextMark(figure.MarkAt(p)));
        }

        if (figure.Links == 0 && figure.Marks == 0) draft.Clear(selected); // drops its links too
        else draft.Figures[selected] = figure;
    }

    // Both ends written, and the grammar allows the pair (neighbouring rings or slots).
    bool CanLinkTo(SlotKey from, SlotKey to) =>
        draft.Figures.ContainsKey(from) && draft.Figures.ContainsKey(to) && CircleDraft.CanLink(from, to, layout.Tier);

    static Mark NextMark(Mark m) => m == Mark.DoubleRing ? Mark.None : m + 1;

    bool OnClear()
    {
        draft.Clear(selected);
        Redraw();
        return true;
    }

    bool OnInscribe()
    {
        var circle = draft.ToCircle();
        if (circle.Glyphs.Count == 0)
        {
            capi.ShowChatMessage(Lang.Get("mossbound:canvas-empty"));
            return true;
        }
        capi.ModLoader.GetModSystem<MossboundModSystem>().SendInscribe(circle);
        TryClose();
        return true;
    }

    // ---------- drawing ----------

    void DrawCanvas(Context target, ImageSurface surface, ElementBounds bounds)
    {
        double size = bounds.InnerWidth, c = size / 2, u = size / CanvasSize, gr = layout.GlyphRadius * size;
        double px = Ink.PixelSize(u, style.CanvasGrain);
        double W(double width) => Math.Max(width * style.Weight, px); // tier weight, never thinner than one ink pixel
        double R(SlotKey k) => layout.LetterRadius(draft.SizeOf(k)) * size;
        bool Doubled(SlotKey k) => draft.SizeOf(k) >= 3 && style.Doubles; // full strength: a doubled letter circle

        if (!Ink.PaintArt(target, canvasArt, size))
        {
            Ink.Use(target, Ink.Paper, 1);
            Ink.FillCircle(target, c, c, size * 0.49);
        }

        Ink.Pixelated(target, size, px, ctx =>
        {
            var glyphs = draft.Figures.Keys.ToList();
            ctx.PushGroup();
            CirclePainter.Structure(ctx, draft, layout, style, size, W);

            // Links: straight, bowing only around glyphs they don't join (so they never look like two).
            ctx.LineWidth = W(2.8 * u);
            foreach (var (a, b) in draft.Links)
            {
                var avoid = glyphs.Where(k => k != a && k != b).Select(k => { var (x, y) = layout.SlotCentre(k, size); return (x, y, R(k) * 1.3); }).ToList();
                // The selected glyph's own links show in the highlight colour.
                GlyphPainter.Link(ctx, layout.SlotCentre(a, size), layout.SlotCentre(b, size), c,
                    Ink.Seed(GlyphPainter.SlotSeed(a), GlyphPainter.SlotSeed(b)), style, avoid,
                    a == selected || b == selected ? Ink.Highlight : null);
            }
            if (dragSlot is SlotKey from)
            {
                Ink.Use(ctx, erasing ? Ink.Erase : Ink.Highlight, 0.8);
                GlyphPainter.Link(ctx, layout.SlotCentre(from, size), (mouseX, mouseY), c, seed: null, style);
            }

            // Letters cut every line they sit on.
            ctx.Operator = Operator.Clear;
            foreach (var k in glyphs)
            {
                var (x, y) = layout.SlotCentre(k, size);
                Ink.FillCircle(ctx, x, y, R(k) * (Doubled(k) ? 1.2 : 1.05));
            }
            ctx.Operator = Operator.Over;
            ctx.PopGroupToSource();
            ctx.Paint();

            // Glyphs, empty slots (faint, exact) and the selection highlight.
            foreach (var k in layout.AllSlots())
            {
                var (x, y) = layout.SlotCentre(k, size);
                if (draft.Figures.TryGetValue(k, out var figure))
                {
                    // Strokes scale with the glyph, so a big tier I glyph isn't drawn with hairlines.
                    GlyphPainter.Rosette(ctx, x, y, R(k), layout.GlyphRotation(k), figure, guides: false, W(gr * 0.09), GlyphPainter.SlotSeed(k), style, bold: true);
                    if (Doubled(k))
                    {
                        int seed = Ink.Seed(GlyphPainter.SlotSeed(k), 11);
                        Ink.Use(ctx, style.Stroke(seed), 0.9);
                        Ink.StrokeCircle(ctx, x, y, R(k) * 1.12, W(1.3 * u), seed, rough: style.Roughness);
                    }
                }
                else
                {
                    Ink.Use(ctx, style.Colour, 0.25);
                    Ink.StrokeCircle(ctx, x, y, gr, W(1.5 * u));
                }
                if (k == selected)
                {
                    Ink.Use(ctx, Ink.Highlight, 0.9);
                    Ink.StrokeCircle(ctx, x, y, R(k) * 1.4, W(2 * u));
                }
                else if (dragSlot is SlotKey linkFrom
                    ? (erasing ? draft.Links.Contains(CircleDraft.Normalize(linkFrom, k)) : CanLinkTo(linkFrom, k))
                    : draft.Links.Contains(CircleDraft.Normalize(selected, k)))
                {
                    // Dragging: where a link may be added, or (red) which links may be removed.
                    // Otherwise: the glyphs the selected one is linked to.
                    Ink.Use(ctx, dragSlot != null && erasing ? Ink.Erase : Ink.Highlight, 0.45);
                    Ink.StrokeCircle(ctx, x, y, R(k) * 1.4, W(1.5 * u));
                }
            }
        }, style.Noise);
    }

    void DrawPanel(Context target, ImageSurface surface, ElementBounds bounds)
    {
        double size = bounds.InnerWidth, c = size / 2, r = size * PanelGlyphRadius;
        double px = Ink.PixelSize(size / PanelSize, style.PanelGrain);
        int seed = GlyphPainter.SlotSeed(selected);

        if (!Ink.PaintArt(target, panelArt, size))
        {
            Ink.Use(target, Ink.Paper, 1);
            target.Rectangle(0, 0, size, size);
            target.Fill();
        }

        Ink.Pixelated(target, size, px, ctx =>
        {
            GlyphPainter.Rosette(ctx, c, c, r, 0, draft.Figures.GetValueOrDefault(selected), guides: true, r * 0.07 * style.Weight, seed, style);
            if (stroke == null || stroke.Points.Count == 0) return;

            // The sweep so far, drawn as it will look, then a loose line to the cursor. Red while erasing.
            Ink.Use(ctx, erasing ? Ink.Erase : Ink.Highlight, 0.85);
            ctx.LineWidth = r * 0.06 * style.Weight;
            foreach (var (a, b) in stroke.Segments()) GlyphPainter.Segment(ctx, c, c, r, 0, a, b, seed, style, ownColour: false);
            var (lx, ly) = RosettePoint(c, c, r, 0, stroke.Points[^1]);
            ctx.NewPath();
            ctx.MoveTo(lx, ly);
            ctx.LineTo(mouseX, mouseY);
            ctx.Stroke();
        }, style.Noise);
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        canvasArt?.Dispose();
        panelArt?.Dispose();
    }
}
