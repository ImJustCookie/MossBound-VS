using System.Collections.Generic;

namespace Mossbound.Core;

public enum GlyphKind
{
    Element,
}

public enum Element
{
    Water,
    Ember,
    Frost,
    Stone,
    Life,
    Rot,
    Wind,
    Ward,
}

public sealed record GlyphDef(string Code, GlyphKind Kind, Element? Element = null);

public static class GlyphRegistry
{
    // Append only: each world generates glyph shapes in this order from its seed, so reordering or
    // removing an entry would change the script of every existing world.
    public static readonly IReadOnlyList<GlyphDef> All = new GlyphDef[]
    {
        new("water", GlyphKind.Element, Element.Water),
        new("ember", GlyphKind.Element, Element.Ember),
        new("frost", GlyphKind.Element, Element.Frost),
        new("stone", GlyphKind.Element, Element.Stone),
        new("life", GlyphKind.Element, Element.Life),
        new("rot", GlyphKind.Element, Element.Rot),
        new("wind", GlyphKind.Element, Element.Wind),
        new("ward", GlyphKind.Element, Element.Ward),
    };
}
