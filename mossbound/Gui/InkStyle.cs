using System;
using Mossbound.Script;

namespace Mossbound.Gui;

// How a tier's circles are inked. Lower tiers are drawn more roughly and less evenly, like a finger in
// moss; higher tiers are steadier, like a pen or a chisel.
//   Colour:          the base colour of every stroke.
//   ColourVariation: how much each stroke's shade may drift from it (seeded, so it never flickers).
//   Roughness:       multiplier on every hand-drawn imperfection (wobble, gaps, bowing, overshoot).
//   CanvasGrain / PanelGrain: GUI units per ink pixel; bigger = chunkier pixel-art lines.
//   Noise:           share of ink pixels knocked out or faded (0–1), like a dry, skipping stroke.
//   Weight:          multiplier on every stroke width (a finger is broader than a pen).
//   DoubleBorder:    a second, thinner outer circle; formal media (parchment up) get one.
readonly record struct InkStyle(Ink.Colour Colour, double ColourVariation, double Roughness,
    double CanvasGrain, double PanelGrain, double Noise, double Weight, bool DoubleBorder = false)
{
    public static InkStyle For(CircleTier tier) => tier.Level switch
    {
        //          colour                   variation rough canvas panel noise weight double border
        1 => new(new(0.58, 0.60, 0.44), 0.12, 1.8, 3, 4, 0.18, 1.8, false), // moss: dry lichen grey-green, finger-drawn
        2 => new(Ink.Charcoal,           0.06, 1.2, 2, 3, 0.10, 1.3, false), // bark: charcoal
        3 => new(new(0.10, 0.11, 0.20), 0.04, 0.7, 1, 3, 0.03, 1.0, true),  // parchment: blue-black ink
        _ => new(new(0.90, 0.88, 0.82), 0.03, 0.5, 1, 3, 0.10, 1.2, true),  // stone: chalk-white carving
    };

    // This stroke's colour: the base shade, nudged per stroke (brightness and a slight tint).
    public Ink.Colour Stroke(int seed)
    {
        var rng = new Random(seed);
        double variation = ColourVariation, shade = 1 + Ink.Jitter(rng, variation);
        double C(double v) => Math.Clamp(v * shade + Ink.Jitter(rng, variation * 0.3), 0, 1);
        return new(C(Colour.R), C(Colour.G), C(Colour.B));
    }
}
