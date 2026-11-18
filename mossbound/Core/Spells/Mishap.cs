namespace Mossbound.Core;

// What a broken circle does instead of its spell.
public enum Mishap
{
    Overload,      // a heart's sizes over the medium's strength limit
    Opposition,    // opposites mixed together
    Misplaced,     // an element outside the heart, or a form outside the shape ring
    NoElement,     // nothing to cast
    NoMix,         // glyphs that mix into nothing
    Unknown,       // a linked figure that is no glyph
    Oversized,     // a form group's sizes over the medium's scale limit
    TooManySpells, // more spells than the medium holds
}

public static class MishapExtensions
{
    // The rest fizzle.
    public static bool Backfires(this Mishap mishap) =>
        mishap is Mishap.Opposition or Mishap.Overload or Mishap.Oversized or Mishap.TooManySpells;
}
