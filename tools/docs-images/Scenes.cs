using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cairo;
using Mossbound.Client;
using Mossbound.Core;
using Vintagestory.API.Client;

// Every circle shown in the docs. Each scene is a CircleDraft drawn by the mod's own code; scenes.json
// records slot centres and radii (and, for the diagram, the word circles) so labels can point at them.
static class Scenes
{
    const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
    static readonly Assembly mod = typeof(GuiDialogCircleCanvas).Assembly;
    static readonly Type layoutType = mod.GetType("Mossbound.Client.CanvasLayout")!;
    static readonly Dictionary<string, object> info = new();
    static readonly Dictionary<string, ImageSurface> arts = new();

    // Each art file is loaded once and reused by every scene.
    static ImageSurface Art(string file) => arts.TryGetValue(file, out var a) ? a : arts[file] = new ImageSurface(art + file);
    static string art = "";

    static SlotKey C(int i) => new(CircleRing.Heart, i);
    static SlotKey I(int i) => new(CircleRing.Shape, i);
    static SlotKey O(int i) => new(CircleRing.Aim, i);

    public static void Run(WorldScript script, string artFolder)
    {
        art = artFolder;
        GlyphFigure G(string code) => script.FigureOf(code);
        CircleDraft New(LifeSource source = LifeSource.Medium) => new() { Source = source };
        void Link(CircleDraft d, SlotKey a, SlotKey b) => d.Links.Add(CircleDraft.Normalize(a, b));

        // 1. A glyph: every kind of stroke and mark, on the panel and on a circle.
        var anatomy = GlyphFigure.Empty.WithLine(0, 1).WithLine(1, 2).WithLine(2, 6).WithLine(6, 4).WithLine(4, 5)
            .WithMark(1, Mark.Dot).WithMark(3, Mark.Ring).WithMark(5, Mark.BigDot).WithMark(6, Mark.DoubleRing);
        Panel(CircleTier.Parchment, anatomy, "anatomy_panel", 448);
        { var d = New(); d.Figures[C(0)] = anatomy; Canvas(CircleTier.Parchment, d, "anatomy_canvas"); }

        // 2. Strength: three separate hearts at sizes 1, 2, 3.
        { var d = New(); d.Figures[C(0)] = G("ember"); d.Figures[C(1)] = G("ember"); d.Figures[C(2)] = G("ember");
          d.SetSize(C(1), 2); d.SetSize(C(2), 3); Canvas(CircleTier.Bark, d, "size"); }

        // 3. Mixing, on moss: apart, linked, opposites.
        { var d = New(); d.Figures[C(0)] = G("water"); d.Figures[C(1)] = G("frost"); Canvas(CircleTier.Moss, d, "mix_apart"); }
        { var d = New(); d.Figures[C(0)] = G("water"); d.Figures[C(1)] = G("frost"); Link(d, C(0), C(1)); Canvas(CircleTier.Moss, d, "mix_linked"); }
        { var d = New(); d.Figures[C(0)] = G("ember"); d.Figures[C(1)] = G("frost"); Link(d, C(0), C(1)); Canvas(CircleTier.Moss, d, "mix_opposites"); }

        // 4. Casts, on bark: one cast, one heart feeding two casts, an unlinked typo.
        { var d = New(); d.Figures[C(0)] = G("life"); d.Figures[I(0)] = G("ward"); Link(d, C(0), I(0)); Canvas(CircleTier.Bark, d, "cast_one"); }
        { var d = New(); d.Figures[C(0)] = G("ember"); d.Figures[I(1)] = G("wind"); d.Figures[I(3)] = G("stone");
          Link(d, C(0), I(1)); Link(d, C(0), I(3)); Canvas(CircleTier.Bark, d, "cast_two"); }
        { var d = New(); d.Figures[C(0)] = G("life"); d.Figures[I(0)] = G("ward"); Link(d, C(0), I(0)); d.Figures[I(2)] = G("rot");
          Canvas(CircleTier.Bark, d, "cast_typo"); }

        // 5. Source, on parchment (its finer ink shows the border best): the same spell three ways.
        foreach (var source in Enum.GetValues<LifeSource>())
        {
            var d = New(source); d.Figures[C(0)] = G("life"); d.Figures[I(0)] = G("ward"); Link(d, C(0), I(0));
            Canvas(CircleTier.Parchment, d, $"source_{source.ToString().ToLower()}");
        }

        // 6. Aim, on parchment: stops at the shape ring, or reaches the aim ring.
        { var d = New(); d.Figures[C(0)] = G("ember"); d.Figures[I(1)] = G("wind"); Link(d, C(0), I(1)); Canvas(CircleTier.Parchment, d, "aim_no"); }
        { var d = New(); d.Figures[C(0)] = G("ember"); d.Figures[I(1)] = G("wind"); d.Figures[O(1)] = G("stone"); d.Figures[O(2)] = G("rot");
          Link(d, C(0), I(1)); Link(d, I(1), O(1)); Link(d, O(1), O(2)); Canvas(CircleTier.Parchment, d, "aim_yes"); }

        // 7. The full example, written vs drawn, and (at 2x) the numbered grammar diagram.
        var full = New(LifeSource.Self);
        full.Figures[C(0)] = G("water"); full.SetSize(C(0), 3); full.Figures[C(1)] = G("frost"); Link(full, C(0), C(1));
        full.Figures[I(1)] = G("ember"); full.SetSize(I(1), 2); Link(full, C(1), I(1));
        full.Figures[I(2)] = G("wind"); Link(full, I(1), I(2));
        full.Figures[O(2)] = G("stone"); Link(full, I(1), O(2)); full.Figures[O(3)] = G("rot"); Link(full, O(2), O(3));
        full.Figures[C(2)] = G("life"); full.Figures[I(4)] = G("ward"); Link(full, C(2), I(4));
        Canvas(CircleTier.Parchment, full, "full");
        Bare(CircleTier.Parchment, full, "full_bare");
        var diagram = CircleDraft.FromCircle(full.ToCircle());
        diagram.Figures[O(7)] = G("ember"); // the diagram also shows an unlinked typo
        Canvas(CircleTier.Parchment, diagram, "diagram", 896, extra: true);

        // The inscription window on each tier: a typical circle, its first heart glyph selected, and the
        // glyph panel showing that glyph (docs/images/tier-interfaces.png).
        {
            var moss = New(); moss.Figures[C(0)] = G("water"); moss.SetSize(C(0), 2); moss.Figures[C(1)] = G("frost"); Link(moss, C(0), C(1));
            var bark = New(LifeSource.Self);
            bark.Figures[C(0)] = G("water"); bark.Figures[C(1)] = G("frost"); Link(bark, C(0), C(1));
            bark.Figures[I(1)] = G("ember"); bark.SetSize(I(1), 2); Link(bark, C(1), I(1)); bark.Figures[I(2)] = G("wind"); Link(bark, I(1), I(2));
            bark.Figures[C(2)] = G("life"); bark.Figures[I(3)] = G("ward"); Link(bark, C(2), I(3));
            foreach (var (tier, d) in new[] { (CircleTier.Moss, moss), (CircleTier.Bark, bark), (CircleTier.Parchment, full) })
            {
                Canvas(tier, d, $"window_{tier.Art}", selected: C(0));
                Panel(tier, d.Figures[C(0)], $"window_{tier.Art}_panel", 256);
            }
        }

        // 8. The media, empty.
        foreach (var tier in new[] { CircleTier.Moss, CircleTier.Bark, CircleTier.Parchment })
            Canvas(tier, New(), $"empty_{tier.Art}");

        File.WriteAllText("out/scenes.json", JsonSerializer.Serialize(info));
        foreach (var a in arts.Values) a.Dispose();
    }

    static object Layout(CircleTier tier) => Activator.CreateInstance(layoutType, tier)!;
    static (double x, double y) Centre(object layout, SlotKey k, int size) => ((double, double))layoutType.GetMethod("SlotCentre")!.Invoke(layout, new object[] { k, (double)size })!;
    static double Letter(object layout, int glyphSize) => (double)layoutType.GetMethod("LetterRadius")!.Invoke(layout, new object[] { glyphSize })!;
    static object Style(CircleTier tier) => mod.GetType("Mossbound.Client.InkStyle")!.GetMethod("For")!.Invoke(null, new object[] { tier })!;

    static GuiDialogCircleCanvas Dialog(CircleTier tier, CircleDraft d, object layout, SlotKey? selected = null)
    {
        var t = typeof(GuiDialogCircleCanvas);
        var dlg = (GuiDialogCircleCanvas)RuntimeHelpers.GetUninitializedObject(t);
        t.GetField("draft", F)!.SetValue(dlg, d);
        t.GetField("layout", F)!.SetValue(dlg, layout);
        t.GetField("style", F)!.SetValue(dlg, Style(tier));
        t.GetField("selected", F)!.SetValue(dlg, selected ?? new SlotKey(CircleRing.Aim, 99)); // none by default
        t.GetField("canvasArt", F)!.SetValue(dlg, Art(tier.Art + "/canvas.png"));
        return dlg;
    }

    static ElementBounds Bounds(int size)
    {
        var b = ElementBounds.Fixed(0, 0, size, size);
        b.absInnerWidth = size;
        b.absInnerHeight = size;
        return b;
    }

    // The circle exactly as the inscription window draws it.
    static void Canvas(CircleTier tier, CircleDraft d, string name, int size = 448, bool extra = false, SlotKey? selected = null)
    {
        var layout = Layout(tier);
        var dlg = Dialog(tier, d, layout, selected);
        using (var surf = new ImageSurface(Format.Argb32, size, size))
        using (var ctx = new Context(surf))
        {
            typeof(GuiDialogCircleCanvas).GetMethod("DrawCanvas", F)!.Invoke(dlg, new object[] { ctx, surf, Bounds(size) });
            surf.WriteToPng($"out/{name}.png");
        }
        double gr = (double)layoutType.GetProperty("GlyphRadius")!.GetValue(layout)! * size;
        var scene = new Dictionary<string, object>
        {
            ["size"] = size,
            ["glyphRadius"] = gr,
            ["slots"] = d.Figures.Keys.ToDictionary(k => $"{k.Ring}{k.Slot}", k =>
            {
                var (x, y) = Centre(layout, k, size);
                return new[] { x, y, Letter(layout, d.SizeOf(k)) * size };
            }),
        };
        if (extra)
        {
            scene["words"] = CircleStructure.Casts(d.Figures.Keys, d.Links).Select(w =>
            {
                var members = w.Heart.Concat(w.Body).ToList();
                var (x, y, r) = WordCircle.Place(members.Select(k => Centre(layout, k, size)).ToList(), gr, (size / 2.0, size / 2.0),
                    size * 0.45, extraWeight: members.Sum(k => d.SizeOf(k) - 1));
                return new { x, y, r };
            }).ToList();
            var (ex, ey) = Centre(layout, I(0), size);
            scene["empty"] = new[] { ex, ey };
        }
        info[name] = scene;
    }

    // The glyph panel with one glyph selected, as when writing it.
    static void Panel(CircleTier tier, GlyphFigure figure, string name, int size)
    {
        var d = new CircleDraft();
        d.Figures[C(0)] = figure;
        var t = typeof(GuiDialogCircleCanvas);
        var dlg = Dialog(tier, d, Layout(tier));
        t.GetField("selected", F)!.SetValue(dlg, C(0));
        t.GetField("panelArt", F)!.SetValue(dlg, Art(tier.Art + "/glyphpanel.png"));
        using var surf = new ImageSurface(Format.Argb32, size, size);
        using var ctx = new Context(surf);
        t.GetMethod("DrawPanel", F)!.Invoke(dlg, new object[] { ctx, surf, Bounds(size) });
        surf.WriteToPng($"out/{name}.png");
    }

    // Only what the writer writes: the border's source, faint slot circles, plain links, and each glyph
    // exactly as on the panel (no letter circle), with the canvas's own ink. None of the drawn extras.
    static void Bare(CircleTier tier, CircleDraft d, string name, int size = 448)
    {
        var ink = mod.GetType("Mossbound.Client.Ink")!;
        var painter = mod.GetType("Mossbound.Client.GlyphPainter")!;
        var circlePainter = mod.GetType("Mossbound.Client.CirclePainter")!;
        var layout = Layout(tier);
        var style = Style(tier);
        T Prop<T>(string p) => (T)style.GetType().GetProperty(p)!.GetValue(style)!;
        double u = size / 448.0, px = Math.Max(1, Math.Round(Prop<double>("CanvasGrain") * u));
        double gr = (double)layoutType.GetProperty("GlyphRadius")!.GetValue(layout)! * size;
        Func<double, double> W = w => Math.Max(w * Prop<double>("Weight"), px);
        (double, double) P(SlotKey k) => Centre(layout, k, size);
        double R(SlotKey k) => Letter(layout, d.SizeOf(k)) * size;
        int Seed(SlotKey k) => (int)painter.GetMethod("SlotSeed")!.Invoke(null, new object[] { k })!;
        var use = ink.GetMethod("Use")!;
        var stroke = style.GetType().GetMethod("Stroke")!;

        using var surf = new ImageSurface(Format.Argb32, size, size);
        using var target = new Context(surf);
        ink.GetMethod("PaintArt")!.Invoke(null, new object?[] { target, Art(tier.Art + "/canvas.png"), (double)size });
        Action<Context> draw = ctx =>
        {
            circlePainter.GetMethod("Border")!.Invoke(null, new object[] { ctx, d.Source, style, (double)size, W, Array.Empty<(double, double, double)>() });

            use.Invoke(null, new object[] { ctx, Prop<object>("Colour"), 0.25 });
            ctx.LineWidth = W(1.5 * u);
            foreach (var k in (IEnumerable<SlotKey>)layoutType.GetMethod("AllSlots")!.Invoke(layout, null)!)
            {
                var (sx, sy) = P(k);
                ctx.NewPath(); ctx.Arc(sx, sy, d.Figures.ContainsKey(k) ? R(k) : gr, 0, 2 * Math.PI); ctx.Stroke();
            }

            ctx.PushGroup();
            ctx.LineWidth = W(2.8 * u);
            foreach (var (a, b) in d.Links)
                painter.GetMethod("Link")!.Invoke(null, new object?[] { ctx, P(a), P(b), size / 2.0, (int?)(Seed(a) * 31 + Seed(b)), style, null, null });
            ctx.Operator = Operator.Clear;
            foreach (var k in d.Figures.Keys) { var (x, y) = P(k); ctx.NewPath(); ctx.Arc(x, y, R(k) * 1.05, 0, 2 * Math.PI); ctx.Fill(); }
            ctx.Operator = Operator.Over;
            ctx.PopGroupToSource();
            ctx.Paint();

            var segment = painter.GetMethod("Segment")!;
            var drawMark = painter.GetMethod("DrawMark", F)!;
            var markRadius = painter.GetMethod("MarkRadius", F)!;
            var pointAt = layoutType.GetMethod("RosettePoint")!;
            foreach (var (k, f) in d.Figures)
            {
                var (x, y) = P(k);
                double rot = (double)layoutType.GetMethod("GlyphRotation")!.Invoke(layout, new object[] { k })!;
                double r = R(k), lw = W(gr * 0.09);
                (double, double) Point(int p) => ((double, double))pointAt.Invoke(null, new object[] { x, y, r, rot, p })!;
                ctx.PushGroup();
                ctx.LineWidth = lw;
                foreach (var (pa, pb) in f.LinePairs())
                    segment.Invoke(null, new object[] { ctx, x, y, r, rot, pa, pb, Seed(k), style, true, 1.0 });
                ctx.Operator = Operator.Clear;
                for (int p = 0; p < GlyphFigure.PointCount; p++)
                {
                    if (f.MarkAt(p) == Mark.None) continue;
                    var (mx, my) = Point(p);
                    ctx.NewPath(); ctx.Arc(mx, my, (double)markRadius.Invoke(null, new object[] { r, f.MarkAt(p) })! + lw, 0, 2 * Math.PI); ctx.Fill();
                }
                ctx.Operator = Operator.Over;
                for (int p = 0; p < GlyphFigure.PointCount; p++)
                {
                    if (f.MarkAt(p) == Mark.None) continue;
                    var (mx, my) = Point(p);
                    use.Invoke(null, new object[] { ctx, stroke.Invoke(style, new object[] { Seed(k) + p })!, 1.0 });
                    drawMark.Invoke(null, new object[] { ctx, mx, my, r, f.MarkAt(p), lw, Seed(k) + p, Prop<double>("Roughness") });
                }
                ctx.PopGroupToSource();
                ctx.Paint();
            }
        };
        ink.GetMethod("Pixelated")!.Invoke(null, new object[] { target, (double)size, px, draw, Prop<double>("Noise") });
        surf.WriteToPng($"out/{name}.png");
    }
}
