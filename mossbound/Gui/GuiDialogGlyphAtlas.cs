using Cairo;
using Mossbound.Script;
using Vintagestory.API.Client;

namespace Mossbound.Gui;

// Admin-only window listing this world's glyphs with their names. Opened by `/mossbound glyphs`;
// its data comes from the server only after the privilege check. Drawn as clean reference figures
// (exact vector lines, the 7 points shown) rather than in any tier's hand-drawn ink.
public class GuiDialogGlyphAtlas : GuiDialog
{
    const int Columns = 4;
    const double Cell = 128;  // GUI units
    const double Label = 22;

    // No wobble, no colour drift, no grain: every hand-drawn imperfection scales with these to zero.
    static readonly InkStyle Clean = new(new(0.10, 0.08, 0.07), 0, 0, 1, 1, 0, 1);

    readonly GlyphAtlas atlas;

    public override string? ToggleKeyCombinationCode => null;

    public GuiDialogGlyphAtlas(ICoreClientAPI capi, GlyphAtlas atlas) : base(capi)
    {
        this.atlas = atlas;

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
        double scale = bounds.InnerWidth / (Columns * Cell), cell = Cell * scale, r = cell * 0.3;  // leaves room for ring marks on the rim
        for (int i = 0; i < atlas.Entries.Count; i++)
        {
            var e = atlas.Entries[i];
            ctx.Save();
            ctx.Translate(i % Columns * cell, i / Columns * (Cell + Label) * scale);
            Ink.Use(ctx, Ink.Paper, 1);
            ctx.Rectangle(cell * 0.04, cell * 0.04, cell * 0.92, cell * 0.92);
            ctx.Fill();
            GlyphPainter.Rosette(ctx, cell / 2, cell / 2, r, 0, new GlyphFigure(e.Links, e.Marks),
                guides: true, r * 0.06, 0, Clean, guideAlpha: 0.45, guideDot: 0.035);  // well below a Dot mark (0.07)
            ctx.Restore();
        }
    }
}
