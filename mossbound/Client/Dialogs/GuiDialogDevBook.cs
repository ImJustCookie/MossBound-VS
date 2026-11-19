using System.Collections.Generic;
using System.Linq;
using Cairo;
using Mossbound.Core;
using Mossbound.Items;
using Mossbound.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Mossbound.Client;

// The dev book: the inscription canvas with a choice of medium, and the world's glyphs by name to click instead of
// drawing them. Links, sizes, the source and drawing by hand are the canvas's own. Write asks the server for a fresh
// medium of the chosen tier with the circle on it, and stays open for the next one.
public sealed class GuiDialogDevBook(ICoreClientAPI clientApi, GlyphAtlas atlas, CircleTier tier, Circle? circle)
    : GuiDialogCircleCanvas(clientApi, circle, tier)
{
    const int Columns = 5;
    const double TierRow = 30, Label = 16;

    // Field initialisers run before the base constructor, which builds the window with them (AddSide).
    readonly GlyphAtlas glyphs = atlas;
    readonly List<(CircleTier Tier, string Name)> media = MediaOf(clientApi);
    ElementBounds paletteBounds = null!;
    double cellWidth, cellHeight;

    protected override string TitleKey => "devbook-title";
    protected override string InscribeKey => "devbook-write";

    // Media the game has, one per tier: the medium picker's buttons.
    static List<(CircleTier, string)> MediaOf(ICoreClientAPI capi) => capi.World.Items.OfType<ItemCircleMedium>()
        .GroupBy(medium => medium.Tier).Select(g => (g.Key, new ItemStack(g.First()).GetName())).OrderBy(m => m.Key.Level).ToList();

    static string KeyOf(CircleTier tier) => "tier-" + tier.Level;

    protected override void AddSide(GuiComposer compo, ElementBounds bounds)
    {
        double width = bounds.fixedWidth, buttonWidth = width / media.Count;
        foreach (var (medium, i) in media.Select((m, i) => (m, i)))
        {
            var button = ElementBounds.Fixed(i * buttonWidth, 0, buttonWidth - 4, TierRow - 6);
            bounds.WithChild(button);
            compo.AddToggleButton(medium.Name, CairoFont.WhiteSmallText(), on => OnMedium(medium.Tier), button, KeyOf(medium.Tier));
        }

        int rows = (glyphs.Entries.Count + Columns - 1) / Columns;
        cellWidth = width / Columns;
        cellHeight = (bounds.fixedHeight - TierRow) / rows;
        paletteBounds = ElementBounds.Fixed(0, TierRow, width, rows * cellHeight);
        bounds.WithChild(paletteBounds);
        compo.AddDynamicCustomDraw(paletteBounds, DrawPalette, "palette");
        for (int i = 0; i < glyphs.Entries.Count; i++)
        {
            var label = ElementBounds.Fixed(i % Columns * cellWidth, TierRow + (i / Columns + 1) * cellHeight - Label, cellWidth, Label);
            bounds.WithChild(label);
            compo.AddStaticText(ModText.Get("glyph-" + glyphs.Entries[i].Code), CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), label);
        }
    }

    public override bool TryOpen()
    {
        SingleComposer.GetToggleButton(KeyOf(layout.Tier))?.SetValue(true);
        return base.TryOpen();
    }

    // Switching medium keeps what the new one can hold.
    void OnMedium(CircleTier picked)
    {
        if (picked == layout.Tier)
        {
            SingleComposer.GetToggleButton(KeyOf(picked)).SetValue(true);
            return;
        }
        draft.FitTo(picked);
        var switched = new GuiDialogDevBook(capi, glyphs, picked, draft.ToCircle());
        TryClose();
        switched.TryOpen();
    }

    public override void OnMouseDown(MouseEvent args)
    {
        if (args.Button != EnumMouseButton.Left || !paletteBounds.PointInside(args.X, args.Y))
        {
            base.OnMouseDown(args);
            return;
        }
        double scale = paletteBounds.InnerWidth / paletteBounds.fixedWidth;
        int column = (int)((args.X - paletteBounds.absX) / scale / cellWidth), row = (int)((args.Y - paletteBounds.absY) / scale / cellHeight);
        int index = row * Columns + column;
        if (column < Columns && index < glyphs.Entries.Count) Pick(glyphs.Entries[index]);
        args.Handled = true;
    }

    // Writes the glyph in the selected slot, then moves on to the next empty slot of that ring, so a heart is a few clicks.
    void Pick(GlyphAtlasEntry entry)
    {
        var selected = input.Selected;
        draft.Write(selected, new GlyphFigure(entry.Lines, entry.Marks));
        var next = layout.AllSlots().Where(k => k.Ring == selected.Ring && k.Slot > selected.Slot && !draft.IsWritten(k)).Cast<SlotKey?>().FirstOrDefault();
        if (next is { } slot) input.Select(slot);
        RedrawPanes(Redraw.Both);
    }

    private protected override void RedrawPanes(Redraw redraw)
    {
        base.RedrawPanes(redraw);
        SingleComposer.GetCustomDraw("palette")?.Redraw();
    }

    protected override void Inscribe(Circle circle) =>
        capi.ModLoader.GetModSystem<MossboundModSystem>().Send(new DevBookWrite { Circle = circle, TierLevel = layout.Tier.Level });

    // The glyph in the selected slot is lit, so you see what you're overwriting.
    void DrawPalette(Context ctx, ImageSurface surface, ElementBounds bounds)
    {
        double scale = bounds.InnerWidth / bounds.fixedWidth, w = cellWidth * scale, h = cellHeight * scale;
        double glyphHeight = h - Label * scale, r = glyphHeight * 0.36;
        var current = draft.FigureAt(input.Selected);
        for (int i = 0; i < glyphs.Entries.Count; i++)
        {
            var e = glyphs.Entries[i];
            var figure = new GlyphFigure(e.Lines, e.Marks);
            ctx.Save();
            ctx.Translate(i % Columns * w, i / Columns * h);
            if (figure == current) ctx.SetSourceRGB(0.35, 0.85, 0.70);
            else Ink.Use(ctx, Ink.Paper, 1);
            ctx.Rectangle(w * 0.06, glyphHeight * 0.06, w * 0.88, glyphHeight * 0.88);
            ctx.Fill();
            GlyphPainter.DrawGlyph(ctx, w / 2, glyphHeight / 2, r, 0, figure, r * 0.08, 0, InkStyle.Clean, LetterCircle.Guides, guideAlpha: 0.3, guideDot: 0.03);
            ctx.Restore();
        }
    }
}
