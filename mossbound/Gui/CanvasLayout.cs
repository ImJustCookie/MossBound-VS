using System;
using System.Collections.Generic;
using Mossbound.Script;

namespace Mossbound.Gui;

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

    public CircleTier Tier { get; }
    public double GlyphRadius { get; } // fraction of canvas size

    public CanvasLayout(CircleTier tier)
    {
        Tier = tier;
        var (rings, glyph) = arrangements[Math.Clamp(tier.Rings.Count, 1, arrangements.Length) - 1];
        for (int i = 0; i < tier.Rings.Count; i++) ringRadius[tier.Rings[i]] = rings[Math.Min(i, rings.Length - 1)];
        GlyphRadius = glyph;
    }

    public double RingRadius(CircleRing ring) => ringRadius[ring];

    public IEnumerable<SlotKey> AllSlots()
    {
        foreach (var ring in Tier.Rings)
        {
            for (int s = 0; s < Tier.SlotCount(ring); s++) yield return new SlotKey(ring, s);
        }
    }

    double SlotAngle(SlotKey k) => -Math.PI / 2 + 2 * Math.PI * k.Slot / Tier.SlotCount(k.Ring);

    // Glyphs are turned so their point 0 faces outward, like letters around a Gallifreyan word.
    public double GlyphRotation(SlotKey k) => SlotAngle(k) + Math.PI / 2;

    public (double x, double y) SlotCentre(SlotKey k, double size)
    {
        double a = SlotAngle(k), r = RingRadius(k.Ring) * size;
        return (size / 2 + Math.Cos(a) * r, size / 2 + Math.Sin(a) * r);
    }

    public SlotKey? SlotAt(double x, double y, double canvasSize)
    {
        double reach = GlyphRadius * canvasSize * 1.3;
        foreach (var k in AllSlots())
        {
            var (cx, cy) = SlotCentre(k, canvasSize);
            if (Math.Abs(x - cx) < reach && Math.Abs(y - cy) < reach) return k;
        }
        return null;
    }

    // Point k of a rosette; point 0 is "up" before rotation, 6 is the centre.
    public static (double x, double y) RosettePoint(double cx, double cy, double r, double rotation, int k)
    {
        if (k == GlyphFigure.Centre) return (cx, cy);
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
