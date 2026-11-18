using Cairo;
using Mossbound.Core;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using static Mossbound.Client.CanvasLayout;

namespace Mossbound.Client;

// The inscription window: the circle on the left, the selected glyph's panel on the right, a hint
// and two buttons. Mouse handling is in CanvasInput, drawing in CanvasRenderer.
public class GuiDialogCircleCanvas : GuiDialog
{
    const double Gap = 20;

    readonly CircleDraft draft;
    readonly CanvasLayout layout;
    readonly InkStyle style;
    readonly CanvasInput input;
    readonly ImageSurface? canvasArt;
    readonly ImageSurface? panelArt;
    ElementBounds canvasBounds = null!;
    ElementBounds panelBounds = null!;

    public override string? ToggleKeyCombinationCode => null;

    public GuiDialogCircleCanvas(ICoreClientAPI capi, Circle? existing, CircleTier tier) : base(capi)
    {
        draft = CircleDraft.FromCircle(existing);
        layout = new CanvasLayout(tier);
        style = InkStyle.For(tier);
        input = new CanvasInput(draft, layout);
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

        var background = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        background.BothSizing = ElementSizing.FitToChildren;
        background.WithChildren(canvasBounds, panelBounds, hintBounds, clearBounds, inscribeBounds);

        SingleComposer = capi.Gui
            .CreateCompo("mossbound-circlecanvas", ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
            .AddShadedDialogBG(background)
            .AddDialogTitleBar(ModText.Get("canvas-title"), () => TryClose())
            .AddDynamicCustomDraw(canvasBounds, DrawCanvas, "canvas")
            .AddDynamicCustomDraw(panelBounds, DrawPanel, "panel")
            .AddStaticText(ModText.Get("canvas-hint"), CairoFont.WhiteDetailText(), hintBounds)
            .AddSmallButton(ModText.Get("canvas-clear"), OnClear, clearBounds)
            .AddSmallButton(ModText.Get("canvas-inscribe"), OnInscribe, inscribeBounds)
            .Compose();
    }


    public override void OnMouseDown(MouseEvent args)
    {
        bool right = args.Button == EnumMouseButton.Right;
        if (args.Button is not (EnumMouseButton.Left or EnumMouseButton.Right)) { base.OnMouseDown(args); return; }

        if (panelBounds.PointInside(args.X, args.Y)) Handle(args, input.PressPanel(PanelX(args.X), PanelY(args.Y), panelBounds.InnerWidth, right));
        else if (canvasBounds.PointInside(args.X, args.Y)) Handle(args, input.PressCanvas(CanvasX(args.X), CanvasY(args.Y), canvasBounds.InnerWidth, right));
        else base.OnMouseDown(args);
    }

    public override void OnMouseMove(MouseEvent args)
    {
        if (input.IsDrawingOnPanel) Handle(args, input.MoveOnPanel(PanelX(args.X), PanelY(args.Y), panelBounds.InnerWidth));
        else if (input.IsLinkingOnCanvas) Handle(args, input.MoveOnCanvas(CanvasX(args.X), CanvasY(args.Y)));
        else base.OnMouseMove(args);
    }

    public override void OnMouseUp(MouseEvent args)
    {
        if (input.IsDrawingOnPanel) Handle(args, input.ReleasePanel());
        else if (input.IsLinkingOnCanvas) Handle(args, input.ReleaseCanvas(CanvasX(args.X), CanvasY(args.Y), canvasBounds.InnerWidth));
        else base.OnMouseUp(args);
    }

    // The wheel event carries no position, so it is read from the input state.
    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        int x = capi.Input.MouseX, y = capi.Input.MouseY;
        var redraw = canvasBounds.PointInside(x, y) ? input.Scroll(CanvasX(x), CanvasY(y), canvasBounds.InnerWidth, args.delta) : Redraw.None;
        if (redraw == Redraw.None) { base.OnMouseWheel(args); return; }
        RedrawPanes(redraw);
        args.SetHandled(true);
    }

    void Handle(MouseEvent args, Redraw redraw)
    {
        RedrawPanes(redraw);
        args.Handled = true;
    }

    double CanvasX(double x) => x - canvasBounds.absX;
    double CanvasY(double y) => y - canvasBounds.absY;
    double PanelX(double x) => x - panelBounds.absX;
    double PanelY(double y) => y - panelBounds.absY;


    bool OnClear()
    {
        draft.Erase(input.Selected);
        RedrawPanes(Redraw.Both);
        return true;
    }

    bool OnInscribe()
    {
        var circle = draft.ToCircle();
        if (circle.Glyphs.Count == 0)
        {
            capi.ShowChatMessage(ModText.Get("canvas-empty"));
            return true;
        }
        capi.ModLoader.GetModSystem<MossboundModSystem>().SendInscribe(circle);
        TryClose();
        return true;
    }


    void RedrawPanes(Redraw redraw)
    {
        if (redraw.HasFlag(Redraw.Canvas)) SingleComposer.GetCustomDraw("canvas").Redraw();
        if (redraw.HasFlag(Redraw.Panel)) SingleComposer.GetCustomDraw("panel").Redraw();
    }

    void DrawCanvas(Context ctx, ImageSurface surface, ElementBounds bounds) =>
        CanvasRenderer.DrawCanvas(ctx, bounds.InnerWidth, draft, layout, style, canvasArt, input.CanvasView);

    void DrawPanel(Context ctx, ImageSurface surface, ElementBounds bounds) =>
        CanvasRenderer.DrawPanel(ctx, bounds.InnerWidth, draft.FigureAt(input.Selected), GlyphPainter.SlotSeed(input.Selected), style, panelArt, input.PanelView);

    // Each open makes a new dialog. The game keeps closed dialogs loaded and redraws them on a window
    // resize (moving to another screen), so unload this one before freeing the art it draws with.
    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        capi.Gui.LoadedGuis.Remove(this);
        Dispose();
    }

    public override void Dispose()
    {
        base.Dispose();
        canvasArt?.Dispose();
        panelArt?.Dispose();
    }
}
