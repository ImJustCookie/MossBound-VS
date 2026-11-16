using System;
using System.Runtime.InteropServices;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Mossbound.Gui;

// Drawing toolkit for the hand-inked, pixelated look. Every imperfection comes from a seed, so the
// same element always wobbles the same way and redraws (on every mouse move) never flicker.
static class Ink
{
    public readonly record struct Colour(double R, double G, double B);

    public static readonly Colour Paper = new(0.86, 0.79, 0.64);    // fallback when there is no art
    public static readonly Colour Charcoal = new(0.14, 0.11, 0.09);  // bark ink (see InkStyle)
    public static readonly Colour Highlight = new(0.95, 0.72, 0.25); // amber: stands out on every tier's art
    public static readonly Colour Erase = new(0.7, 0.2, 0.15);

    public static void Use(Context ctx, Colour c, double alpha) => ctx.SetSourceRGBA(c.R, c.G, c.B, alpha);

    // ---------- pixelation ----------

    public static double PixelSize(double guiScale, double grain) => Math.Max(1, Math.Round(grain * guiScale));

    // Draws on a low-resolution layer without anti-aliasing, then scales it up with hard pixels.
    // `draw` keeps working in full-size coordinates. `noise` (0–1) roughens the ink like a dry stroke.
    public static void Pixelated(Context target, double size, double pixel, Action<Context> draw, double noise = 0)
    {
        int n = (int)Math.Ceiling(size / pixel);
        using var low = new ImageSurface(Format.Argb32, n, n);
        using (var ctx = new Context(low))
        {
            ctx.Antialias = Antialias.None;
            ctx.Scale(1 / pixel, 1 / pixel);
            draw(ctx);
        }
        low.Flush();
        if (noise > 0) DryBrush(low, noise);
        target.Save();
        target.Scale(pixel, pixel);
        target.SetSourceSurface(low, 0, 0);
        ((SurfacePattern)target.GetSource()).Filter = Filter.Nearest;
        target.Paint();
        target.Restore();
    }

    // Optional art: null until the PNG exists, so callers fall back to flat colours.
    public static ImageSurface? LoadArt(ICoreClientAPI capi, string path)
    {
        var loc = new AssetLocation(path);
        return capi.Assets.TryGet(loc) == null ? null : GuiElement.getImageSurfaceFromAsset(capi, loc);
    }

    // Knocks out or fades some ink pixels, like moss sap or charcoal skipping on a rough surface.
    // Keyed to pixel positions, so a pixel always gets the same treatment and redraws never flicker.
    static void DryBrush(ImageSurface surface, double amount)
    {
        int width = surface.Width, height = surface.Height, stride = surface.Stride;
        var data = new byte[stride * height];
        Marshal.Copy(surface.DataPtr, data, 0, data.Length);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * stride + x * 4;
                if (data[i + 3] == 0) continue;
                double roll = PixelHash(x, y);
                if (roll >= amount) continue;
                double keep = roll < amount * 0.5 ? 0 : 0.55; // half the hits drop out, half fade
                for (int c = 0; c < 4; c++) data[i + c] = (byte)(data[i + c] * keep); // premultiplied: scale all
            }
        }
        Marshal.Copy(data, 0, surface.DataPtr, data.Length);
        surface.MarkDirty();
    }

    static double PixelHash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) / (double)uint.MaxValue;
        }
    }

    // Paints pixel art stretched over a square of `size`, keeping pixels crisp. False if there is no art.
    public static bool PaintArt(Context ctx, ImageSurface? art, double size)
    {
        if (art == null) return false;
        ctx.Save();
        ctx.Scale(size / art.Width, size / art.Height);
        ctx.SetSourceSurface(art, 0, 0);
        ((SurfacePattern)ctx.GetSource()).Filter = Filter.Nearest;
        ctx.Paint();
        ctx.Restore();
        return true;
    }

    // ---------- shapes ----------

    public static void FillCircle(Context ctx, double x, double y, double radius)
    {
        ctx.NewPath();
        ctx.Arc(x, y, radius, 0, 2 * Math.PI);
        ctx.Fill();
    }

    // No seed = an exact circle (interface). With a seed = hand-drawn: a gentle wobble, and the pen's
    // start and end don't quite meet (a small gap or overlap). `centred` keeps big circles from drifting;
    // `rough` scales every imperfection (see InkStyle).
    public static void StrokeCircle(Context ctx, double x, double y, double radius, double width, int? seed = null, bool centred = false, double rough = 1)
    {
        if (seed is not int s)
        {
            ctx.LineWidth = width;
            ctx.NewPath();
            ctx.Arc(x, y, radius, 0, 2 * Math.PI);
            ctx.Stroke();
            return;
        }

        // Rough hands don't draw a circle in one go: moss gets 3–4 finger strokes, bark 2–3, steady
        // tiers one. Each stroke sits slightly off the others, is a bit thicker or thinner, and leaves
        // a small gap or overlap where it meets the next.
        var rng = new Random(Seed(s, 7));
        int strokes = rough >= 1.5 ? 3 + rng.Next(2) : rough >= 1.1 ? 2 + rng.Next(2) : 1;
        double start = rng.NextDouble() * 2 * Math.PI, share = 2 * Math.PI / strokes;
        for (int k = 0; k < strokes; k++)
        {
            double from = start + k * share + (strokes > 1 ? Jitter(rng, 0.12 * rough) : 0);
            double sweep = share + (-0.10 + rng.NextDouble() * 0.30) * rough;
            double offset = strokes > 1 ? radius * Jitter(rng, 0.008 * rough) : 0;
            ctx.LineWidth = strokes > 1 ? width * (1 + Jitter(rng, 0.2 * rough)) : width;
            ctx.NewPath();
            TraceWobblyArc(ctx, x, y, radius + offset, from, sweep, s, centred, rough);
            ctx.Stroke();
        }
    }

    // ---------- hand-drawn imperfections ----------

    // The radius wobble depends on the absolute angle, so every arc traced with the same seed lies on
    // the same imperfect circle. `centred` drops the first harmonic (which in effect shifts the centre)
    // and keeps gentle 2- and 3-lobed deformations.
    public static void TraceWobblyArc(Context ctx, double cx, double cy, double radius, double start, double sweep, int seed, bool centred = false, double rough = 1)
    {
        var rng = new Random(seed);
        // Small circles wobble relatively more, like a real hand (at least ~1 ink pixel of wobble).
        double minWobble = Math.Min(0.06, 1.2 / Math.Max(radius, 1));
        double a1 = Math.Max(0.015 + rng.NextDouble() * 0.015, minWobble) * rough, p1 = rng.NextDouble() * 2 * Math.PI;
        double a2 = rng.NextDouble() * 0.012 * rough, p2 = rng.NextDouble() * 2 * Math.PI;
        // A shaky hand: small, quick wobbles that only show on rough tiers (zero for steady ones).
        double shake = 0.005 * Math.Max(0, rough - 0.6), p3 = rng.NextDouble() * 2 * Math.PI, p4 = rng.NextDouble() * 2 * Math.PI;
        int n = Math.Max(8, (int)(radius * Math.Abs(sweep) / 2));
        for (int i = 0; i <= n; i++)
        {
            double t = start + sweep * i / n;
            double rr = centred
                ? radius * (1 + 0.004 * rough * Math.Sin(2 * t + p1) + a2 * 0.5 * Math.Sin(3 * t + p2))
                : radius * (1 + a1 * Math.Sin(t + p1) + a2 * Math.Sin(2 * t + p2));
            rr += radius * shake * (Math.Sin(7 * t + p3) + 0.6 * Math.Sin(13 * t + p4));
            double px = cx + Math.Cos(t) * rr, py = cy + Math.Sin(t) * rr;
            if (i == 0) ctx.MoveTo(px, py);
            else ctx.LineTo(px, py);
        }
    }

    // A straight stroke that bows slightly and over/undershoots its end a touch.
    public static void TraceHandLine(Context ctx, double ax, double ay, double bx, double by, Random rng, double rough = 1)
    {
        double dx = bx - ax, dy = by - ay;
        double bow = Jitter(rng, 0.06 * rough), over = Jitter(rng, 0.05 * rough);
        double qx = (ax + bx) / 2 - dy * bow, qy = (ay + by) / 2 + dx * bow;
        double ex = bx + dx * over, ey = by + dy * over;
        ctx.MoveTo(ax, ay);
        ctx.CurveTo(ax + (qx - ax) * 2 / 3, ay + (qy - ay) * 2 / 3, ex + (qx - ex) * 2 / 3, ey + (qy - ey) * 2 / 3, ex, ey);
    }

    public static double Jitter(Random rng, double amount) => (rng.NextDouble() * 2 - 1) * amount;

    // Stable across runs (HashCode.Combine is randomised per process).
    public static int Seed(int a, int b, int c = 0) => unchecked(a * 73856093 ^ b * 19349663 ^ c * 83492791);

    public static int Seed(string text)
    {
        int h = 17;
        foreach (char ch in text) h = unchecked(h * 31 + ch);
        return h;
    }
}
