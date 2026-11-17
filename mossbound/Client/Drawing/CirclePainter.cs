using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Mossbound.Core;

namespace Mossbound.Client;

// Draws what the game adds around the written glyphs: the border (with its source), the word circle of
// each cast, mix circles and aim lines. All of it is read from how the circle is written
// (CircleStructure), never from what the glyphs mean: the client doesn't know the script.
static class CirclePainter
{
    const double Outer = 0.47, Inner = 0.45; // border radii, as fractions of the canvas size

    public static void Structure(Context ctx, CircleDraft draft, CanvasLayout layout, InkStyle style, double size, Func<double, double> W)
    {
        var casts = CircleStructure.Casts(draft.Figures.Keys, draft.Links);
        var wordCircles = casts.Select(cast => PlaceWordCircle(cast, draft, layout, size)).ToList();
        Border(ctx, draft.Source, style, size, W, wordCircles);
        DrawWordCircles(ctx, casts, wordCircles, style, size, W);
        DrawMixCircles(ctx, draft, layout, style, size, W);
        DrawAimLines(ctx, casts, layout, style, size, W);
    }

    // The outer circle, with the source worked into it (plain for the medium, thorn-diamonds for the
    // body, a bramble for the land), cut where words bite into it. Drawn in its own layer so a bite
    // cuts only the border.
    public static void Border(Context ctx, LifeSource source, InkStyle style, double size, Func<double, double> W,
        IEnumerable<(double x, double y, double r)> bites)
    {
        double u = size / CanvasLayout.CanvasSize;
        var (c, outer) = (Centre(size), size * Outer);
        var biteList = bites.ToList();

        // Ornaments reach past the border line, so they are left out near a bite instead of being cut,
        // which would leave their tips sticking out beyond the word's arc.
        bool Bitten(double angle, double reach) =>
            biteList.Any(b => Geometry.Distance(Geometry.OnCircle(c, outer, angle), (b.x, b.y)) < b.r + reach);

        ctx.PushGroup();
        Ink.Use(ctx, style.Stroke(1), 0.9);
        Ink.StrokeCircle(ctx, c.x, c.y, outer, W(5 * u), seed: 1, centred: true, rough: style.Roughness);
        if (style.DoubleBorder)
        {
            Ink.Use(ctx, style.Stroke(2), 0.9);
            Ink.StrokeCircle(ctx, c.x, c.y, size * Inner, W(2.5 * u), seed: 2, centred: true, rough: style.Roughness);
        }
        if (source == LifeSource.Self) Diamonds(ctx, style, size, Bitten);
        if (source == LifeSource.Land) Bramble(ctx, style, size, W, Bitten);

        ctx.Operator = Operator.Clear;
        foreach (var (bx, by, br) in biteList) Ink.FillCircle(ctx, bx, by, br);
        ctx.Operator = Operator.Over;
        ctx.PopGroupToSource();
        ctx.Paint();
    }

    public static bool IsOnBorder(double x, double y, double size)
    {
        double fromCentre = Geometry.Distance((x, y), Centre(size)) / size;
        return fromCentre >= Inner - 0.02 && fromCentre <= 0.49;
    }

    static (double x, double y) Centre(double size) => (size / 2, size / 2);

    static (double x, double y, double r) PlaceWordCircle(Cast cast, CircleDraft draft, CanvasLayout layout, double size)
    {
        var members = cast.Heart.Concat(cast.Body).ToList();
        return WordCircle.Place(members.Select(k => layout.SlotCentre(k, size)).ToList(), layout.GlyphRadius * size, Centre(size),
            size * Inner, extraWeight: members.Sum(k => draft.SizeOf(k) - 1));
    }

    // Clipped to the border, so a word's own arc closes the bite it made. A long cast (three glyphs or
    // more past its heart) gets a doubled word circle, where the ink allows it.
    static void DrawWordCircles(Context ctx, List<Cast> casts, List<(double x, double y, double r)> circles, InkStyle style, double size,
        Func<double, double> W)
    {
        double u = size / CanvasLayout.CanvasSize;
        ctx.Save();
        ctx.NewPath();
        ctx.Arc(size / 2, size / 2, size * Outer, 0, 2 * Math.PI);
        ctx.Clip();
        for (int i = 0; i < casts.Count; i++)
        {
            var (x, y, r) = circles[i];
            int seed = Ink.Seed(GlyphPainter.SlotSeed(casts[i].Body[0]), 3);
            Ink.Use(ctx, style.Stroke(seed), 0.95);
            Ink.StrokeCircle(ctx, x, y, r, W(3 * u), seed, centred: true, rough: style.Roughness);
            if (style.Doubles && casts[i].Body.Count >= 3) DrawInnerEcho(ctx, style, x, y, r - 7 * u, W(1.3 * u), seed + 1);
        }
        ctx.Restore();
    }

    // Around heart glyphs linked together; doubled for a mix of three.
    static void DrawMixCircles(Context ctx, CircleDraft draft, CanvasLayout layout, InkStyle style, double size, Func<double, double> W)
    {
        double u = size / CanvasLayout.CanvasSize;
        foreach (var mix in CircleStructure.Mixes(draft.Figures.Keys, draft.Links))
        {
            var middle = Middle(mix, layout, size);
            double r = mix.Max(k => Geometry.Distance(layout.SlotCentre(k, size), middle)) + layout.GlyphRadius * size * 1.3;
            int seed = Ink.Seed(GlyphPainter.SlotSeed(mix[0]), 5);
            Ink.Use(ctx, style.Stroke(seed), 0.95);
            Ink.StrokeCircle(ctx, middle.x, middle.y, r, W(2.6 * u), seed, centred: true, rough: style.Roughness);
            if (style.Doubles && mix.Count >= 3) DrawInnerEcho(ctx, style, middle.x, middle.y, r - 6 * u, W(1.3 * u), seed + 1);
        }
    }

    // A cast that reaches the aim ring gets a line from its heart through the end of its aim chain,
    // right across the circle.
    static void DrawAimLines(Context ctx, List<Cast> casts, CanvasLayout layout, InkStyle style, double size, Func<double, double> W)
    {
        ctx.LineWidth = W(1.6 * size / CanvasLayout.CanvasSize);
        foreach (var cast in casts.Where(c => c.IsAimed))
        {
            var heart = Middle(cast.Heart, layout, size);
            var end = layout.SlotCentre(cast.AimEnd!.Value, size);
            if (Geometry.LineAcrossCircle(heart, end, Centre(size), size * Inner) is not { } line) continue;
            int seed = Ink.Seed(GlyphPainter.SlotSeed(cast.Body[0]), 7);
            Ink.Use(ctx, style.Stroke(seed), 0.9);
            ctx.NewPath();
            Ink.TraceHandLine(ctx, line.from.x, line.from.y, line.to.x, line.to.y, new Random(seed), style.Roughness * 0.3);
            ctx.Stroke();
        }
    }

    static (double x, double y) Middle(IEnumerable<SlotKey> slots, CanvasLayout layout, double size)
    {
        var centres = slots.Select(k => layout.SlotCentre(k, size)).ToList();
        return (centres.Average(p => p.x), centres.Average(p => p.y));
    }

    // The thin second line of a doubled circle.
    static void DrawInnerEcho(Context ctx, InkStyle style, double x, double y, double r, double width, int seed)
    {
        Ink.Use(ctx, style.Stroke(seed), 0.8);
        Ink.StrokeCircle(ctx, x, y, r, width, seed, centred: true, rough: style.Roughness);
    }

    // Long, pointed diamonds threaded on the border, pointing out on both sides.
    static void Diamonds(Context ctx, InkStyle style, double size, Func<double, double, bool> bitten)
    {
        const int Count = 20;
        const double HalfWidth = 0.028; // along the border, in radians
        double reach = 14 * size / CanvasLayout.CanvasSize, outer = size * Outer;
        var c = Centre(size);
        Ink.Use(ctx, style.Stroke(79), 0.95);
        for (int i = 0; i < Count; i++)
        {
            double a = 2 * Math.PI * (i + 0.5) / Count;
            if (bitten(a, reach)) continue;
            var corners = new[]
            {
                Geometry.OnCircle(c, outer + reach, a),
                Geometry.OnCircle(c, outer, a + HalfWidth),
                Geometry.OnCircle(c, outer - reach, a),
                Geometry.OnCircle(c, outer, a - HalfWidth),
            };
            FillPolygon(ctx, corners);
        }
    }

    // A thin vine weaving across the border, with a hooked thorn on every crest: pointing out on the
    // outer crests, in on the inner ones.
    static void Bramble(Context ctx, InkStyle style, double size, Func<double, double> W, Func<double, double, bool> bitten)
    {
        const int Waves = 16, Steps = 720;
        double u = size / CanvasLayout.CanvasSize, outer = size * Outer, amplitude = 7 * u, thorn = 11 * u;
        var c = Centre(size);
        Ink.Use(ctx, style.Stroke(82), 0.9);
        ctx.LineWidth = W(2.2 * u);

        bool drawing = false; // the vine is drawn in runs that stop short of every bite
        for (int i = 0; i <= Steps; i++)
        {
            double a = 2 * Math.PI * i / Steps;
            if (bitten(a, amplitude + 3 * u))
            {
                if (drawing) ctx.Stroke();
                drawing = false;
                continue;
            }
            var (x, y) = Geometry.OnCircle(c, outer + amplitude * Math.Sin(a * Waves), a);
            if (drawing) ctx.LineTo(x, y);
            else
            {
                ctx.NewPath();
                ctx.MoveTo(x, y);
                drawing = true;
            }
        }
        if (drawing) ctx.Stroke();

        for (int i = 0; i < Waves; i++)
        {
            double outerCrest = 2 * Math.PI * (i + 0.25) / Waves, innerCrest = 2 * Math.PI * (i + 0.75) / Waves;
            if (!bitten(outerCrest, amplitude + thorn)) Thorn(ctx, c, outer + amplitude - 2 * u, thorn, outerCrest);
            if (!bitten(innerCrest, amplitude + thorn)) Thorn(ctx, c, outer - amplitude + 2 * u, -thorn, innerCrest);
        }
    }

    // A small hooked thorn rising `height` from radius `r` (a negative height points inward).
    static void Thorn(Context ctx, (double x, double y) c, double r, double height, double angle) =>
        FillPolygon(ctx, new[]
        {
            Geometry.OnCircle(c, r, angle - 0.03),
            Geometry.OnCircle(c, r + height, angle + 0.045),
            Geometry.OnCircle(c, r, angle + 0.03),
        });

    static void FillPolygon(Context ctx, IReadOnlyList<(double x, double y)> corners)
    {
        ctx.NewPath();
        ctx.MoveTo(corners[0].x, corners[0].y);
        foreach (var (x, y) in corners.Skip(1)) ctx.LineTo(x, y);
        ctx.ClosePath();
        ctx.Fill();
    }
}
