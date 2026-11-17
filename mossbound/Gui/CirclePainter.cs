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

        // Drawing from the land: roots behind everything, from the heart out through the border.
        if (draft.Source == LifeSource.Land) Roots(ctx, layout, style, size, W);

        // Sentence circle: the border (doubled if the tier's style says so), in its own layer so the
        // bites and the hand's opening cut only the border. Big circles stay centred: hand-drawn
        // without drifting.
        ctx.PushGroup();
        Ink.Use(ctx, style.Stroke(1), 0.9);
        Ink.StrokeCircle(ctx, c, c, outer, W(5 * u), seed: 1, centred: true, rough: style.Roughness);
        if (style.DoubleBorder)
        {
            Ink.Use(ctx, style.Stroke(2), 0.9);
            Ink.StrokeCircle(ctx, c, c, inner, W(2.5 * u), seed: 2, centred: true, rough: style.Roughness);
        }

        // Drawing from yourself: beads threaded along the border, like a pulse.
        if (draft.Source == LifeSource.Self) Beads(ctx, style, size, W);

        // Words bite the border (beads and all).
        ctx.Operator = Operator.Clear;
        foreach (var (wx, wy, wr) in wordCircles) Ink.FillCircle(ctx, wx, wy, wr);
        ctx.Operator = Operator.Over;
        ctx.PopGroupToSource();
        ctx.Paint();

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

    // A thinner second line inside a circle: the concentric doubling of the references.
    static void Echo(Context ctx, InkStyle style, double x, double y, double r, double width, int seed)
    {
        Ink.Use(ctx, style.Stroke(seed), 0.8);
        Ink.StrokeCircle(ctx, x, y, r, width, seed, centred: true, rough: style.Roughness);
    }

    // Seven roots from near the heart out past the border, each forking once on the way.
    static void Roots(Context ctx, CanvasLayout layout, InkStyle style, double size, Func<double, double> W)
    {
        const int Count = 7;
        double c = size / 2, u = size / CanvasLayout.CanvasSize;
        double from = layout.RingRadius(CircleRing.Centre) * size * 0.5, to = size * 0.485, fork = from + (to - from) * 0.62;
        var rng = new Random(77);
        ctx.LineWidth = W(1.2 * u);
        for (int i = 0; i < Count; i++)
        {
            double a = 2 * Math.PI * i / Count + Ink.Jitter(rng, 0.25), af = a + Ink.Jitter(rng, 0.08);
            Ink.Use(ctx, style.Stroke(Ink.Seed(77, i)), 0.6);
            Line(ctx, Polar(c, from, a), Polar(c, fork, af), rng, style);
            Line(ctx, Polar(c, fork, af), Polar(c, to, af - 0.13), rng, style);
            Line(ctx, Polar(c, fork, af), Polar(c, to, af + 0.13), rng, style);
        }
    }

    static void Beads(Context ctx, InkStyle style, double size, Func<double, double> W)
    {
        const int Count = 20;
        double c = size / 2, u = size / CanvasLayout.CanvasSize, outer = size * Outer;
        Ink.Use(ctx, style.Stroke(78), 0.95);
        for (int i = 0; i < Count; i++)
        {
            var (x, y) = Polar(c, outer, 2 * Math.PI * (i + 0.5) / Count);
            Ink.FillCircle(ctx, x, y, W(6 * u));
        }
    }

    static void Line(Context ctx, (double x, double y) a, (double x, double y) b, Random rng, InkStyle style)
    {
        ctx.NewPath();
        Ink.TraceHandLine(ctx, a.x, a.y, b.x, b.y, rng, style.Roughness * 0.5);
        ctx.Stroke();
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
