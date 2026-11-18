using System.Collections.Generic;

namespace Mossbound.Core;

public enum GlyphKind
{
    Element,
    Form,
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

// How a spell travels and acts. Touch is not a glyph: it is what a cast with no form does.
public enum Form
{
    Touch,
    Seed,
    Ring,
}

public sealed record GlyphDef(string Code, GlyphKind Kind, Element? Element = null, Form? Form = null);

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
        new("seed", GlyphKind.Form, Form: Form.Seed),
        new("ring", GlyphKind.Form, Form: Form.Ring),
    };
}
