using System;
using System.Linq;

namespace Mossbound.Core;

// The server's checks on a circle a client sent.
public static class CircleValidation
{
    public const int MaxGlyphs = 48;
    public const int MaxLinks = 96;

    // Everything the medium allows, checked before a circle is stored or cast. Old strips may hold
    // links the editor now forbids, so the linking rule is checked separately, on inscription only.
    public static bool FitsTier(Circle circle, CircleTier tier) =>
        Enum.IsDefined(circle.Source)
        && (circle.Source == LifeSource.Medium || tier.ChoosesSource)
        && circle.Glyphs.Count <= MaxGlyphs
        && circle.Links.Count <= MaxLinks
        && circle.Glyphs.All(g => tier.HasSlot(g.Key) && g.Size >= 0 && g.Size <= tier.MaxSize && g.Figure.IsValid)
        && circle.Glyphs.Select(g => g.Key).Distinct().Count() == circle.Glyphs.Count
        && circle.Links.All(l => IsGlyphIndex(circle, l.From) && IsGlyphIndex(circle, l.To) && l.From != l.To);

    // Call only after FitsTier.
    public static bool LinksFollowTheGrammar(Circle circle, CircleTier tier) =>
        circle.Links.All(l => tier.AllowsLink(circle.Glyphs[l.From].Key, circle.Glyphs[l.To].Key));

    // What may be written on a medium: something, that fits it, linked by the rules.
    public static bool IsWritable(Circle circle, CircleTier tier) =>
        circle.Glyphs.Count > 0 && FitsTier(circle, tier) && LinksFollowTheGrammar(circle, tier);

    static bool IsGlyphIndex(Circle circle, int i) => i >= 0 && i < circle.Glyphs.Count;
}
