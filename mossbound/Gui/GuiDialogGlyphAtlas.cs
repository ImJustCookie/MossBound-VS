using System;
using Cairo;
using Mossbound.Script;
using Vintagestory.API.Client;

namespace Mossbound.Gui;

// Admin-only window listing this world's glyphs with their names. Opened by `/mossbound glyphs`;
// its data comes from the server only after the privilege check.
public class GuiDialogGlyphAtlas : GuiDialog
{
    const int Columns = 4;
    const double Cell = 128;  // GUI units; the glyph panel art scaled down
    const double Label = 22;

    readonly GlyphAtlas atlas;
    readonly ImageSurface? art;

    public override string? ToggleKeyCombinationCode => null;

    public GuiDialogGlyphAtlas(ICoreClientAPI capi, GlyphAtlas atlas) : base(capi)
    {
        this.atlas = atlas;
        art = Ink.LoadArt(capi, $"mossbound:textures/gui/{CircleTier.Moss.Art}/glyphpanel.png");

        int rows = (atlas.Entries.Count + Columns - 1) / Columns;
        double top = GuiStyle.TitleBarHeight;
        var gridBounds = ElementBounds.Fixed(0, top, Columns * Cell, rows * (Cell + Label));
        var bg = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bg.BothSizing = ElementSizing.FitToChildren;
        bg.WithChildren(gridBounds);

        var compo = capi.Gui
            .CreateCompo("mossbound-glyphatlas", ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
            .AddShadedDialogBG(bg)
            .AddDialogTitleBar("Glyph atlas (admin)", () => TryClose())
            .AddDynamicCustomDraw(gridBounds, DrawGrid);
        for (int i = 0; i < atlas.Entries.Count; i++)
        {
            var labelBounds = ElementBounds.Fixed(i % Columns * Cell, top + i / Columns * (Cell + Label) + Cell, Cell, Label);
            bg.WithChildren(labelBounds);
            compo.AddStaticText(atlas.Entries[i].Code, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), labelBounds);
        }
        SingleComposer = compo.Compose();
    }

    void DrawGrid(Context ctx, ImageSurface surface, ElementBounds bounds)
    {
        double scale = bounds.InnerWidth / (Columns * Cell), cell = Cell * scale, r = cell * CanvasLayout.PanelGlyphRadius;
        var style = InkStyle.For(CircleTier.Moss);
        double px = Ink.PixelSize(scale, style.PanelGrain);
        for (int i = 0; i < atlas.Entries.Count; i++)
        {
            var e = atlas.Entries[i];
            ctx.Save();
            ctx.Translate(i % Columns * cell, i / Columns * (Cell + Label) * scale);
            if (!Ink.PaintArt(ctx, art, cell))
            {
                Ink.Use(ctx, Ink.Paper, 1);
                Ink.FillCircle(ctx, cell / 2, cell / 2, cell * 0.48);
            }
            Ink.Pixelated(ctx, cell, px, low => GlyphPainter.Rosette(low, cell / 2, cell / 2, r, 0,
                new GlyphFigure(e.Links, e.Marks), guides: false, r * 0.07 * style.Weight, Ink.Seed(e.Code), style), style.Noise);
            ctx.Restore();
        }
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        art?.Dispose();
    }
}
