using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Script;

public sealed record SpellPlan(
    MixResult Mix,
    IReadOnlyList<Mishap> Mishaps,
    IReadOnlyList<PlacedGlyph> Unrecognized)
{
    public bool Fizzles => Mishaps.Count > 0;
}

public static class CircleParser
{
    // Hard limits on client input; anything larger is rejected before parsing.
    public const int MaxGlyphs = 48;
    public const int MaxLinks = 96;

    // Everything a client sends is checked against the medium's tier before it is stored or cast.
    public static bool IsWithinBounds(Circle circle, CircleTier tier) =>
        circle.Glyphs.Count <= MaxGlyphs
        && circle.Links.Count <= MaxLinks
        && circle.Glyphs.All(g =>
            tier.HasSlot(g.Ring, g.Slot)
            && g.Figure.IsValid)
        && circle.Glyphs.Select(g => (g.Ring, g.Slot)).Distinct().Count() == circle.Glyphs.Count
        && circle.Links.All(l =>
            l.From >= 0 && l.From < circle.Glyphs.Count
            && l.To >= 0 && l.To < circle.Glyphs.Count
            && l.From != l.To);

    // Call only after IsWithinBounds.
    public static SpellPlan Parse(Circle circle, WorldScript script)
    {
        var mishaps = new List<Mishap>();
        var unrecognized = new List<PlacedGlyph>();
        var elements = new List<Element>();

        foreach (var placed in circle.Glyphs)
        {
            var glyph = script.Recognize(placed.Figure);
            if (glyph == null)
            {
                unrecognized.Add(placed);
                continue;
            }
            if (glyph.Kind == GlyphKind.Element)
            {
                if (placed.Ring == CircleRing.Centre) elements.Add(glyph.Element!.Value);
                else if (!mishaps.Contains(Mishap.Misplaced)) mishaps.Add(Mishap.Misplaced);
            }
        }

        var mix = ElementMixer.Mix(elements);
        mishaps.AddRange(mix.Mishaps);
        return new SpellPlan(mix, mishaps, unrecognized);
    }
}
