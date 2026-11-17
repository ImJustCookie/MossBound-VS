using System;
using System.Collections.Generic;
using Mossbound.Core;

namespace Mossbound.Client;

// Geometry of the inscription table for one tier. Positions are element-local, already GUI-scaled
// pixels; `size` is the element's width (canvas and panel are square). Fewer rings means bigger
// glyphs spread over the parchment, so a moss sigil isn't a tiny circle in an empty canvas.
sealed class CanvasLayout
{
    public const double CanvasSize = 448; // GUI units; 112 px art × 4
    public const double PanelSize = 256;  // GUI units; 64 px art × 4
    public const double PanelGlyphRadius = 0.38; // fraction of the panel size

    // Per ring count: ring radii from the centre outward, and the glyph radius (fractions of canvas size).
    static readonly (double[] rings, double glyph)[] arrangements =
    {
        (new[] { 0.16 }, 0.10),
        (new[] { 0.13, 0.33 }, 0.075),
        (new[] { 0.11, 0.27, 0.41 }, 0.058),
        (new[] { 0.10, 0.24, 0.36, 0.44 }, 0.05),
    };

    readonly Dictionary<CircleRing, double> ringRadius = new();
    // Each slot's place, worked out once: its angle and its distance from the centre (fraction of size).
    readonly Dictionary<SlotKey, (double angle, double radius)> places = new();
    readonly List<SlotKey> slots = new();

    public CircleTier Tier { get; }
    public double GlyphRadius { get; } // fraction of canvas size

    public CanvasLayout(CircleTier tier)
    {
        Tier = tier;
        var (rings, glyph) = arrangements[Math.Clamp(tier.Rings.Count, 1, arrangements.Length) - 1];
        for (int i = 0; i < tier.Rings.Count; i++) ringRadius[tier.Rings[i]] = rings[Math.Min(i, rings.Length - 1)];
        GlyphRadius = glyph;

        // Each slot sits a little off its exact place, seeded by the slot so it never moves while you
        // write: circles look laid out by hand instead of on a grid.
        foreach (var ring in tier.Rings)
        {
            for (int s = 0; s < tier.SlotCount(ring); s++)
            {
                var k = new SlotKey(ring, s);
                var rng = new Random((int)ring * 31 + s);
                double angle = -Math.PI / 2 + 2 * Math.PI * s / tier.SlotCount(ring) + (rng.NextDouble() * 2 - 1) * 0.06;
                double radius = ringRadius[ring] + (rng.NextDouble() * 2 - 1) * 0.2 * glyph;
                places[k] = (angle, radius);
                slots.Add(k);
            }
        }
    }

    public double RingRadius(CircleRing ring) => ringRadius[ring];

    // A glyph's size is its strength: each step past 1 draws its letter a fifth larger (fraction of size).
    public double LetterRadius(int glyphSize) => GlyphRadius * (1 + 0.2 * (glyphSize - 1));

    public IReadOnlyList<SlotKey> AllSlots() => slots;

    // Glyphs are turned so their point 0 faces outward, like letters around a Gallifreyan word.
    public double GlyphRotation(SlotKey k) => places[k].angle + Math.PI / 2;

    public (double x, double y) SlotCentre(SlotKey k, double size)
    {
        var (a, r) = places[k];
        return (size / 2 + Math.Cos(a) * r * size, size / 2 + Math.Sin(a) * r * size);
    }

    // The slot whose centre is nearest, within reach (nearest, so big glyphs that overlap a neighbour
    // still pick the one you aimed at).
    public SlotKey? SlotAt(double x, double y, double canvasSize)
    {
        double reach = GlyphRadius * canvasSize * 1.4;
        SlotKey? best = null;
        double bestDistance = reach * reach;
        foreach (var k in slots)
        {
            var (cx, cy) = SlotCentre(k, canvasSize);
            double d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
            if (d < bestDistance) (best, bestDistance) = (k, d);
        }
        return best;
    }

    // Point k of a rosette; point 0 is "up" before rotation, 6 is the centre.
    public static (double x, double y) RosettePoint(double cx, double cy, double r, double rotation, int k)
    {
        if (k == GlyphFigure.Middle) return (cx, cy);
        double a = rotation - Math.PI / 2 + k * Math.PI / 3;
        return (cx + Math.Cos(a) * r, cy + Math.Sin(a) * r);
    }

    public static int? PointAt(double x, double y, double panelSize)
    {
        double c = panelSize / 2, reach = panelSize * 0.08;
        for (int k = 0; k < GlyphFigure.PointCount; k++)
        {
            var (px, py) = RosettePoint(c, c, panelSize * PanelGlyphRadius, 0, k);
            if (Math.Abs(x - px) < reach && Math.Abs(y - py) < reach) return k;
        }
        return null;
    }
}
