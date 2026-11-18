using System.Collections.Generic;
using System.Linq;
using Cairo;
using Mossbound.Core;
using Mossbound.Network;
using Vintagestory.API.Client;

namespace Mossbound.Client;

// Admin-only window listing this world's glyphs with their names, one section per glyph kind. Opened by
// `/mossbound glyphs`; its data comes from the server only after the privilege check. Drawn as clean reference
// figures (exact vector lines, the 7 points shown) rather than in any tier's hand-drawn ink.
public class GuiDialogGlyphAtlas : GuiDialog
{
    const int Columns = 4;
    const double Cell = 128;  // GUI units
    const double Label = 22;
    const double Heading = 30;

    // No wobble, no colour drift, no grain: every hand-drawn imperfection scales with these to zero.
    static readonly InkStyle clean = new(new(0.10, 0.08, 0.07), 0, 0, 1, 1, 0, 1);

    public override string? ToggleKeyCombinationCode => null;

    public GuiDialogGlyphAtlas(ICoreClientAPI capi, GlyphAtlas atlas) : base(capi)
    {
        var bg = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bg.BothSizing = ElementSizing.FitToChildren;
        var compo = capi.Gui
            .CreateCompo("mossbound-glyphatlas", ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
            .AddShadedDialogBG(bg)
            .AddDialogTitleBar(ModText.Get("atlas-title"), () => TryClose());

        double top = GuiStyle.TitleBarHeight;
        foreach (var section in atlas.Entries.GroupBy(KindOf).OrderBy(g => g.Key))
            top = AddSection(compo, bg, section.Key, section.ToList(), top);
        SingleComposer = compo.Compose();
    }

    static GlyphKind KindOf(GlyphAtlasEntry entry) => GlyphRegistry.All.First(g => g.Code == entry.Code).Kind;

    // A heading, then the kind's glyphs in a grid with their names. Returns where the next section starts.
    static double AddSection(GuiComposer compo, ElementBounds bg, GlyphKind kind, List<GlyphAtlasEntry> entries, double top)
    {
        var headingBounds = ElementBounds.Fixed(0, top, Columns * Cell, Heading);
        int rows = (entries.Count + Columns - 1) / Columns;
        var gridBounds = ElementBounds.Fixed(0, top + Heading, Columns * Cell, rows * (Cell + Label));
        bg.WithChildren(headingBounds, gridBounds);
        compo.AddStaticText(ModText.Get("atlas-kind-" + kind.ToString().ToLowerInvariant()), CairoFont.WhiteSmallishText(), headingBounds)
            .AddDynamicCustomDraw(gridBounds, (ctx, _, bounds) => DrawGrid(ctx, bounds, entries));

        for (int i = 0; i < entries.Count; i++)
        {
            var labelBounds = ElementBounds.Fixed(i % Columns * Cell, top + Heading + i / Columns * (Cell + Label) + Cell, Cell, Label);
            bg.WithChildren(labelBounds);
            compo.AddStaticText(ModText.Get("glyph-" + entries[i].Code), CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), labelBounds);
        }
        return top + Heading + rows * (Cell + Label);
    }

    static void DrawGrid(Context ctx, ElementBounds bounds, List<GlyphAtlasEntry> entries)
    {
        double scale = bounds.InnerWidth / (Columns * Cell), cell = Cell * scale, r = cell * 0.3;  // leaves room for ring marks on the rim
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            ctx.Save();
            ctx.Translate(i % Columns * cell, i / Columns * (Cell + Label) * scale);
            Ink.Use(ctx, Ink.Paper, 1);
            ctx.Rectangle(cell * 0.04, cell * 0.04, cell * 0.92, cell * 0.92);
            ctx.Fill();
            GlyphPainter.DrawGlyph(ctx, cell / 2, cell / 2, r, 0, new GlyphFigure(e.Lines, e.Marks), r * 0.06, 0, clean,
                LetterCircle.Guides, guideAlpha: 0.45, guideDot: 0.035); // points well below a Dot mark (0.07)
            ctx.Restore();
        }
    }
}
