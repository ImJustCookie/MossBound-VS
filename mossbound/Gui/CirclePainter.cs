using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Mossbound.Script;

namespace Mossbound.Gui;

// How a written circle is drawn around its letters, Gallifreyan-style. Everything here is read from
// how the circle is written (CircleShape), never from what its glyphs mean: the client doesn't know
// the script. Draws into the caller's current group; the caller then adds links and cuts every line
// where a letter sits.
static class CirclePainter
{
    // Fractions of the canvas size.
    const double Outer = 0.47, Inner = 0.45;

    public static void Structure(Context ctx, CircleDraft draft, CanvasLayout layout, InkStyle style, double size, Func<double, double> W)
    {
        double c = size / 2, u = size / CanvasLayout.CanvasSize, gr = layout.GlyphRadius * size;
        double outer = size * Outer, inner = size * Inner;
        var glyphs = draft.Figures.Keys.ToList();
        var words = CircleShape.Words(glyphs, draft.Links);
        var wordCircles = words
            .Select(w =>
            {
                var members = w.Heart.Concat(w.Body).ToList();
                return CircleShape.WordCircle(members.Select(k => layout.SlotCentre(k, size)).ToList(), gr, (c, c), inner,
                    extraWeight: members.Sum(k => draft.SizeOf(k) - 1));
            })
            .ToList();
        (double x, double y) Centroid(IEnumerable<SlotKey> keys)
        {
            var pts = keys.Select(k => layout.SlotCentre(k, size)).ToList();
            return (pts.Average(p => p.x), pts.Average(p => p.y));
        }

        Border(ctx, draft.Source, style, size, W, wordCircles);

        // Each word's own arc closes the bite it made (clipped to the border).
        ctx.Save();
        ctx.NewPath();
        ctx.Arc(c, c, outer, 0, 2 * Math.PI);
        ctx.Clip();
        for (int i = 0; i < words.Count; i++)
        {
            // A long word (three glyphs or more past its heart) is doubled, where the ink allows it.
            var (wx, wy, wr) = wordCircles[i];
            int seed = Ink.Seed(GlyphPainter.SlotSeed(words[i].Body[0]), 3);
            Ink.Use(ctx, style.Stroke(seed), 0.95);
            Ink.StrokeCircle(ctx, wx, wy, wr, W(3 * u), seed, centred: true, rough: style.Roughness);
            if (style.Doubles && words[i].Body.Count >= 3) Echo(ctx, style, wx, wy, wr - 7 * u, W(1.3 * u), seed + 1);
        }
        ctx.Restore();

        // Mixes: a circle around heart elements linked together; doubled for a triple mix.
        foreach (var mix in CircleShape.MixGroups(glyphs, draft.Links))
        {
            var (mx, my) = Centroid(mix);
            double mr = mix.Max(k => Distance(layout.SlotCentre(k, size), (mx, my))) + gr * 1.3;
            int seed = Ink.Seed(GlyphPainter.SlotSeed(mix[0]), 5);
            Ink.Use(ctx, style.Stroke(seed), 0.95);
            Ink.StrokeCircle(ctx, mx, my, mr, W(2.6 * u), seed, centred: true, rough: style.Roughness);
            if (style.Doubles && mix.Count >= 3) Echo(ctx, style, mx, my, mr - 6 * u, W(1.3 * u), seed + 1);
        }

        // An aimed cast points outward: a line from its heart through the end of its aim chain, right
        // across the circle.
        ctx.LineWidth = W(1.6 * u);
        foreach (var w in words.Where(w => w.Aimed))
        {
            var heart = Centroid(w.Heart);
            var end = layout.SlotCentre(w.AimEnd!.Value, size);
            if (CircleShape.Chord(heart, end, (c, c), inner) is not { } chord) continue;
            int seed = Ink.Seed(GlyphPainter.SlotSeed(w.Body[0]), 7);
            Ink.Use(ctx, style.Stroke(seed), 0.9);
            ctx.NewPath();
            Ink.TraceHandLine(ctx, chord.from.x, chord.from.y, chord.to.x, chord.to.y, new Random(seed), style.Roughness * 0.3);
            ctx.Stroke();
        }
    }

    // The sentence circle: the border (doubled if the tier's style says so), with where the life comes
    // from worked into it (plain for the medium, long thorn-diamonds for your own body, a thorny
    // bramble for the land), then cut where words bite into it. Drawn in its own layer so the bites
    // cut only the border. Big circles stay centred: hand-drawn without drifting.
    public static void Border(Context ctx, LifeSource source, InkStyle style, double size, Func<double, double> W,
        IEnumerable<(double x, double y, double r)> bites)
    {
        double c = size / 2, u = size / CanvasLayout.CanvasSize;
        ctx.PushGroup();
        Ink.Use(ctx, style.Stroke(1), 0.9);
        Ink.StrokeCircle(ctx, c, c, size * Outer, W(5 * u), seed: 1, centred: true, rough: style.Roughness);
        if (style.DoubleBorder)
        {
            Ink.Use(ctx, style.Stroke(2), 0.9);
            Ink.StrokeCircle(ctx, c, c, size * Inner, W(2.5 * u), seed: 2, centred: true, rough: style.Roughness);
        }
        if (source == LifeSource.Self) Diamonds(ctx, style, size, W);
        if (source == LifeSource.Land) Bramble(ctx, style, size, W);

        ctx.Operator = Operator.Clear;
        foreach (var (bx, by, br) in bites) Ink.FillCircle(ctx, bx, by, br);
        ctx.Operator = Operator.Over;
        ctx.PopGroupToSource();
        ctx.Paint();
    }

    // A thinner second line inside a circle: the concentric doubling of the references.
    static void Echo(Context ctx, InkStyle style, double x, double y, double r, double width, int seed)
    {
        Ink.Use(ctx, style.Stroke(seed), 0.8);
        Ink.StrokeCircle(ctx, x, y, r, width, seed, centred: true, rough: style.Roughness);
    }

    // Long, pointed diamonds threaded on the border, pointing out on both sides.
    static void Diamonds(Context ctx, InkStyle style, double size, Func<double, double> W)
    {
        const int Count = 20;
        const double Along = 0.028, Across = 14; // half-width along the border (radians), reach each side (GUI units)
        double c = size / 2, u = size / CanvasLayout.CanvasSize, outer = size * Outer;
        Ink.Use(ctx, style.Stroke(79), 0.95);
        for (int i = 0; i < Count; i++)
        {
            double a = 2 * Math.PI * (i + 0.5) / Count;
            var tipOut = Polar(c, outer + Across * u, a);
            var side1 = Polar(c, outer, a + Along);
            var tipIn = Polar(c, outer - Across * u, a);
            var side2 = Polar(c, outer, a - Along);
            ctx.NewPath();
            ctx.MoveTo(tipOut.x, tipOut.y); ctx.LineTo(side1.x, side1.y); ctx.LineTo(tipIn.x, tipIn.y); ctx.LineTo(side2.x, side2.y);
            ctx.ClosePath();
            ctx.Fill();
        }
    }

    // A thin vine weaving across the border, a hooked thorn on every crest, pointing out on the outer
    // crests and in on the inner ones.
    static void Bramble(Context ctx, InkStyle style, double size, Func<double, double> W)
    {
        const int Waves = 16;
        double c = size / 2, u = size / CanvasLayout.CanvasSize, outer = size * Outer, amp = 7 * u;
        Ink.Use(ctx, style.Stroke(82), 0.9);
        ctx.LineWidth = W(2.2 * u);
        ctx.NewPath();
        for (int i = 0; i <= 720; i++)
        {
            double a = 2 * Math.PI * i / 720;
            var (x, y) = Polar(c, outer + amp * Math.Sin(a * Waves), a);
            if (i == 0) ctx.MoveTo(x, y);
            else ctx.LineTo(x, y);
        }
        ctx.Stroke();
        for (int i = 0; i < Waves; i++)
        {
            Thorn(ctx, c, outer + amp - 2 * u, 11 * u, 2 * Math.PI * (i + 0.25) / Waves);   // outer crest, pointing out
            Thorn(ctx, c, outer - amp + 2 * u, -11 * u, 2 * Math.PI * (i + 0.75) / Waves);  // inner crest, pointing in
        }
    }

    // A small hooked thorn rising `height` from radius `r` at angle `a` (negative height points inward).
    static void Thorn(Context ctx, double c, double r, double height, double a)
    {
        var (x0, y0) = Polar(c, r, a - 0.03);
        var (x1, y1) = Polar(c, r, a + 0.03);
        var (tx, ty) = Polar(c, r + height, a + 0.045);
        ctx.NewPath(); ctx.MoveTo(x0, y0); ctx.LineTo(tx, ty); ctx.LineTo(x1, y1); ctx.ClosePath();
        ctx.Fill();
    }

    static (double x, double y) Polar(double c, double r, double a) => (c + Math.Cos(a) * r, c + Math.Sin(a) * r);

    static double Distance((double x, double y) a, (double x, double y) b) => Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));

    // A click on the border band (not on a slot) picks the source.
    public static bool OnBorder(double x, double y, double size)
    {
        double d = Distance((x, y), (size / 2, size / 2)) / size;
        return d >= Inner - 0.02 && d <= 0.49;
    }
}
