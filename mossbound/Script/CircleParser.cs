using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Script;

public sealed record SpellPlan(
    IReadOnlyList<(Aspect aspect, int intensity)> Aspects,
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
        Enum.IsDefined(circle.Source)
        && (circle.Source == LifeSource.Medium || tier.ChoosesSource)
        && circle.Glyphs.Count <= MaxGlyphs
        && circle.Links.Count <= MaxLinks
        && circle.Glyphs.All(g =>
            tier.HasSlot(g.Ring, g.Slot)
            && g.Size >= 0 && g.Size <= tier.MaxSize
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
        var keys = circle.Glyphs.Select(g => new SlotKey(g.Ring, g.Slot)).ToList();
        var elementsAt = new Dictionary<SlotKey, Element[]>();

        for (int i = 0; i < circle.Glyphs.Count; i++)
        {
            var placed = circle.Glyphs[i];
            var glyph = script.Recognize(placed.Figure);
            if (glyph == null)
            {
                unrecognized.Add(placed);
                continue;
            }
            if (glyph.Kind == GlyphKind.Element)
            {
                // A bigger glyph counts as that element written several times: size is intensity.
                if (placed.Ring == CircleRing.Centre) elementsAt[keys[i]] = Enumerable.Repeat(glyph.Element!.Value, placed.Power).ToArray();
                else if (!mishaps.Contains(Mishap.Misplaced)) mishaps.Add(Mishap.Misplaced);
            }
        }

        // Hearts linked together mix; an unlinked heart is mixed on its own (the canvas draws them the
        // same way, through CircleShape). Any heart that fails makes the whole circle fail.
        var aspects = new List<(Aspect, int)>();
        var links = circle.Links.Select(l => (keys[l.From], keys[l.To]));
        bool anyElement = false;
        foreach (var heart in CircleShape.Hearts(keys, links))
        {
            var elements = heart.SelectMany(k => elementsAt.GetValueOrDefault(k) ?? Array.Empty<Element>()).ToList();
            if (elements.Count == 0) continue;
            anyElement = true;
            var mix = ElementMixer.Mix(elements);
            aspects.AddRange(mix.Aspects);
            foreach (var m in mix.Mishaps) if (!mishaps.Contains(m)) mishaps.Add(m);
        }
        if (!anyElement) mishaps.Add(Mishap.NoElement);
        return new SpellPlan(aspects, mishaps, unrecognized);
    }
}
