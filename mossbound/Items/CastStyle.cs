using Vintagestory.API.MathTools;

namespace Mossbound.Items;

// How a cast looks. One per spell look, picked by ItemCircleMedium.CastStyleFor. Rates are per second at full charge.
public sealed record CastStyle(
    int[] Colours,
    float TwinkleRate = 6,      // sparkles blinking around the medium
    float TwinkleRadius = 0.2f,
    float GroundRate = 300,     // sparks in the spinning ring at the feet
    float GroundRadius = 0.5f,
    float GroundSpin = 1.6f,    // turns per second at full charge
    float FlyOffShare = 0.03f,  // ring sparks thrown out of the spin
    float SiphonShare = 0.2f,   // ring sparks drawn up into the caster
    int ReleaseBurst = 30,
    int GlyphMin = 3,           // glyphs rising around the caster: one per written glyph, within these limits
    int GlyphMax = 12,
    float GlyphSpin = 0.12f,    // glyph ring turns per second once ready
    byte Glow = 160)
{
    public static readonly CastStyle Default = new(new[]
    {
        ColorUtil.ToRgba(220, 120, 220, 110),
        ColorUtil.ToRgba(200, 170, 240, 140),
        ColorUtil.ToRgba(180, 230, 250, 200),
    });
}
