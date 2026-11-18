using System.Text.Json;
using Cairo;
using Mossbound.Client;
using Mossbound.Core;

// Every circle shown in the docs, drawn with the mod's own renderer. out/scenes.json records where
// glyphs (and, for the diagram, word circles) landed, so labels.py can point at them.
static class Scenes
{
    static readonly Dictionary<string, object> info = new();
    static readonly Dictionary<string, ImageSurface> arts = new();
    static string artFolder = "";
    static WorldScript script = null!;

    static SlotKey Heart(int i) => new(CircleRing.Heart, i);
    static SlotKey Shape(int i) => new(CircleRing.Shape, i);
    static SlotKey Aim(int i) => new(CircleRing.Aim, i);

    public static void Run(WorldScript worldScript, string art)
    {
        script = worldScript;
        artFolder = art;
        var moss = CircleTier.Moss;
        var bark = CircleTier.Bark;
        var parchment = CircleTier.Parchment;

        // 1. A glyph: every kind of line and mark, on the panel and on a circle.
        var anatomy = GlyphFigure.Empty.WithLine(0, 1).WithLine(1, 2).WithLine(2, 6).WithLine(6, 4).WithLine(4, 5)
            .WithMark(1, Mark.Dot).WithMark(3, Mark.Ring).WithMark(5, Mark.BigDot).WithMark(6, Mark.DoubleRing);
        Panel(parchment, anatomy, "anatomy_panel", 448);
        var single = new Scene(parchment);
        single.Draft.Write(Heart(0), anatomy);
        Canvas(single, "anatomy_canvas");

        // 2. Strength: three separate hearts at sizes 1, 2 and 3.
        Canvas(new Scene(bark).Glyph(Heart(0), "ember").Glyph(Heart(1), "ember", 2).Glyph(Heart(2), "ember", 3), "size");

        // 3. Mixing, on bark (moss holds one spell): apart, linked, opposites.
        Canvas(new Scene(bark).Glyph(Heart(0), "water").Glyph(Heart(1), "ember"), "mix_apart");
        Canvas(new Scene(bark).Glyph(Heart(0), "water").Glyph(Heart(1), "ember").Link(Heart(0), Heart(1)), "mix_linked");
        Canvas(new Scene(bark).Glyph(Heart(0), "ember").Glyph(Heart(1), "frost").Link(Heart(0), Heart(1)), "mix_opposites");

        // 4. Casts, on bark: one cast, one heart feeding two casts, an unlinked typo.
        Canvas(new Scene(bark).Glyph(Heart(0), "life").Glyph(Shape(0), "ward").Link(Heart(0), Shape(0)), "cast_one");
        Canvas(new Scene(bark).Glyph(Heart(0), "ember").Glyph(Shape(1), "wind").Glyph(Shape(3), "stone")
            .Link(Heart(0), Shape(1)).Link(Heart(0), Shape(3)), "cast_two");
        Canvas(new Scene(bark).Glyph(Heart(0), "life").Glyph(Shape(0), "ward").Link(Heart(0), Shape(0)).Glyph(Shape(2), "rot"), "cast_typo");

        // 5. Source, on parchment (its finer ink shows the border best): the same spell three ways.
        foreach (var source in Enum.GetValues<LifeSource>())
            Canvas(new Scene(parchment, source).Glyph(Heart(0), "life").Glyph(Shape(0), "ward").Link(Heart(0), Shape(0)), $"source_{source.ToString().ToLower()}");

        // 6. Aim, on parchment: stops at the shape ring, or reaches the aim ring.
        Canvas(new Scene(parchment).Glyph(Heart(0), "ember").Glyph(Shape(1), "wind").Link(Heart(0), Shape(1)), "aim_no");
        Canvas(new Scene(parchment).Glyph(Heart(0), "ember").Glyph(Shape(1), "wind").Glyph(Aim(1), "stone").Glyph(Aim(2), "rot")
            .Link(Heart(0), Shape(1)).Link(Shape(1), Aim(1)).Link(Aim(1), Aim(2)), "aim_yes");

        // 7. The full example: written vs drawn, and (at 2x, with a typo) the numbered grammar diagram.
        Scene Full() => new Scene(parchment, LifeSource.Self)
            .Glyph(Heart(0), "water", 3).Glyph(Heart(1), "frost").Link(Heart(0), Heart(1))
            .Glyph(Shape(1), "ember", 2).Link(Heart(1), Shape(1))
            .Glyph(Shape(2), "wind").Link(Shape(1), Shape(2))
            .Glyph(Aim(2), "stone").Link(Shape(1), Aim(2)).Glyph(Aim(3), "rot").Link(Aim(2), Aim(3))
            .Glyph(Heart(2), "life").Glyph(Shape(4), "ward").Link(Heart(2), Shape(4));
        Canvas(Full(), "full");
        WrittenOnly(Full(), "full_bare");
        Canvas(Full().Glyph(Aim(7), "ember"), "diagram", 896, withWords: true);

        // The inscription window on each tier: a typical circle with its first heart glyph selected,
        // and the glyph panel showing that glyph.
        var windows = new[]
        {
            new Scene(moss).Glyph(Heart(0), "water", 2).Glyph(Heart(1), "frost").Link(Heart(0), Heart(1)),
            new Scene(bark, LifeSource.Self).Glyph(Heart(0), "water").Glyph(Heart(1), "frost").Link(Heart(0), Heart(1))
                .Glyph(Shape(1), "ember", 2).Link(Heart(1), Shape(1)).Glyph(Shape(2), "wind").Link(Shape(1), Shape(2))
                .Glyph(Heart(2), "life").Glyph(Shape(3), "ward").Link(Heart(2), Shape(3)),
            Full(),
        };
        foreach (var window in windows)
        {
            Canvas(window, $"window_{window.Tier.Art}", selected: Heart(0));
            Panel(window.Tier, window.Draft.FigureAt(Heart(0)), $"window_{window.Tier.Art}_panel", 256);
        }

        // 8. The media, empty.
        foreach (var tier in new[] { moss, bark, parchment }) Canvas(new Scene(tier), $"empty_{tier.Art}");

        File.WriteAllText("out/scenes.json", JsonSerializer.Serialize(info));
        foreach (var surface in arts.Values) surface.Dispose();
    }

    // A circle to draw, built glyph by glyph with the editor's own rules.
    sealed class Scene
    {
        public Scene(CircleTier tier, LifeSource source = LifeSource.Medium)
        {
            Tier = tier;
            Draft = new CircleDraft { Source = source };
        }

        public CircleTier Tier { get; }
        public CircleDraft Draft { get; }

        public Scene Glyph(SlotKey slot, string code, int size = 1)
        {
            Draft.Write(slot, script.FigureOf(code));
            Draft.SetSize(slot, size);
            return this;
        }

        public Scene Link(SlotKey a, SlotKey b)
        {
            Draft.Link(a, b, Tier);
            return this;
        }
    }

    static ImageSurface Art(CircleTier tier, string file)
    {
        string path = $"{artFolder}{tier.Art}/{file}";
        return arts.TryGetValue(path, out var surface) ? surface : arts[path] = new ImageSurface(path);
    }

    static void Canvas(Scene scene, string name, int size = 448, bool withWords = false, SlotKey? selected = null)
    {
        var layout = new CanvasLayout(scene.Tier);
        using (var surface = new ImageSurface(Format.Argb32, size, size))
        using (var ctx = new Context(surface))
        {
            CanvasRenderer.DrawCanvas(ctx, size, scene.Draft, layout, InkStyle.For(scene.Tier), Art(scene.Tier, "canvas.png"),
                new CanvasView(selected, null, false, (0, 0)));
            surface.WriteToPng($"out/{name}.png");
        }
        info[name] = Positions(scene.Draft, layout, size, withWords);
    }

    static void Panel(CircleTier tier, GlyphFigure figure, string name, int size)
    {
        using var surface = new ImageSurface(Format.Argb32, size, size);
        using var ctx = new Context(surface);
        CanvasRenderer.DrawPanel(ctx, size, figure, GlyphPainter.SlotSeed(Heart(0)), InkStyle.For(tier), Art(tier, "glyphpanel.png"),
            new PanelView(null, false, (0, 0)));
        surface.WriteToPng($"out/{name}.png");
    }

    // Only what the writer writes: the border's source, faint slots, plain links, and each glyph as
    // drawn on the panel (no letter circle). None of what the game draws on top.
    static void WrittenOnly(Scene scene, string name, int size = 448)
    {
        var layout = new CanvasLayout(scene.Tier);
        var style = InkStyle.For(scene.Tier);
        var draft = scene.Draft;
        double unit = size / CanvasLayout.CanvasSize, pixel = Ink.PixelSize(unit, style.CanvasGrain);
        double glyphRadius = layout.GlyphRadius * size;
        double Width(double w) => Math.Max(w * style.Weight, pixel);
        double LetterRadius(SlotKey k) => layout.LetterRadius(draft.SizeOf(k)) * size;

        using var surface = new ImageSurface(Format.Argb32, size, size);
        using var target = new Context(surface);
        Ink.PaintArt(target, Art(scene.Tier, "canvas.png"), size);
        Ink.Pixelated(target, size, pixel, ctx =>
        {
            CirclePainter.Border(ctx, draft.Source, style, size, Width, Array.Empty<(double, double, double)>());

            Ink.Use(ctx, style.Colour, 0.25);
            foreach (var slot in layout.AllSlots())
            {
                var (x, y) = layout.SlotCentre(slot, size);
                Ink.StrokeCircle(ctx, x, y, draft.IsWritten(slot) ? LetterRadius(slot) : glyphRadius, Width(1.5 * unit));
            }

            ctx.PushGroup();
            ctx.LineWidth = Width(2.8 * unit);
            foreach (var (a, b) in draft.Links)
                GlyphPainter.DrawLink(ctx, layout.SlotCentre(a, size), layout.SlotCentre(b, size), size / 2,
                    GlyphPainter.SlotSeed(a) * 31 + GlyphPainter.SlotSeed(b), style);
            ctx.Operator = Operator.Clear;
            foreach (var slot in draft.Figures.Keys)
            {
                var (x, y) = layout.SlotCentre(slot, size);
                Ink.FillCircle(ctx, x, y, LetterRadius(slot) * 1.05);
            }
            ctx.Operator = Operator.Over;
            ctx.PopGroupToSource();
            ctx.Paint();

            foreach (var (slot, figure) in draft.Figures)
            {
                var (x, y) = layout.SlotCentre(slot, size);
                GlyphPainter.DrawGlyph(ctx, x, y, LetterRadius(slot), layout.GlyphRotation(slot), figure, Width(glyphRadius * 0.09),
                    GlyphPainter.SlotSeed(slot), style, LetterCircle.None);
            }
        }, style.Noise);
        surface.WriteToPng($"out/{name}.png");
    }

    static Dictionary<string, object> Positions(CircleDraft draft, CanvasLayout layout, int size, bool withWords)
    {
        double glyphRadius = layout.GlyphRadius * size;
        var positions = new Dictionary<string, object>
        {
            ["size"] = size,
            ["glyphRadius"] = glyphRadius,
            ["slots"] = draft.Figures.Keys.ToDictionary(k => $"{k.Ring}{k.Slot}", k =>
            {
                var (x, y) = layout.SlotCentre(k, size);
                return new[] { x, y, layout.LetterRadius(draft.SizeOf(k)) * size };
            }),
        };
        if (!withWords) return positions;
        positions["words"] = CircleStructure.Casts(draft.Figures.Keys, draft.Links).Select(cast =>
        {
            var members = cast.Heart.Concat(cast.Body).ToList();
            var (x, y, r) = WordCircle.Place(members.Select(k => layout.SlotCentre(k, size)).ToList(), glyphRadius,
                (size / 2.0, size / 2.0), size * 0.45, extraWeight: members.Sum(k => draft.SizeOf(k) - 1));
            return new { x, y, r };
        }).ToList();
        var (ex, ey) = layout.SlotCentre(Shape(0), size);
        positions["empty"] = new[] { ex, ey };
        return positions;
    }
}
