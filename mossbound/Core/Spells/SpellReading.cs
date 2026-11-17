using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

public sealed record SpellPlan(IReadOnlyList<(Aspect aspect, int intensity)> Aspects, IReadOnlyList<Mishap> Mishaps)
{
    public bool Fizzles => Mishaps.Count > 0;
}

// Reads a circle with the world's script: which elements each heart holds, and what they mix into.
public static class SpellReading
{
    // Call only after CircleValidation.FitsTier.
    public static SpellPlan Read(Circle circle, WorldScript script)
    {
        var mishaps = new List<Mishap>();
        var elementsAt = new Dictionary<SlotKey, Element[]>();
        foreach (var glyph in circle.Glyphs)
        {
            if (script.Recognize(glyph.Figure) is not { Kind: GlyphKind.Element, Element: Element element }) continue;
            if (glyph.Ring == CircleRing.Heart) elementsAt[glyph.Key] = Enumerable.Repeat(element, glyph.EffectiveSize).ToArray();
            else AddOnce(mishaps, Mishap.Misplaced);
        }

        var aspects = new List<(Aspect, int)>();
        var keys = circle.Glyphs.Select(g => g.Key).ToList();
        var links = circle.Links.Select(l => (keys[l.From], keys[l.To]));
        foreach (var heart in CircleStructure.Hearts(keys, links))
        {
            var elements = heart.SelectMany(k => elementsAt.GetValueOrDefault(k) ?? Array.Empty<Element>()).ToList();
            if (elements.Count == 0) continue;
            var mix = ElementMixer.Mix(elements);
            aspects.AddRange(mix.Aspects);
            foreach (var m in mix.Mishaps) AddOnce(mishaps, m);
        }
        if (elementsAt.Count == 0) AddOnce(mishaps, Mishap.NoElement);
        return new SpellPlan(aspects, mishaps);
    }

    static void AddOnce(List<Mishap> mishaps, Mishap mishap)
    {
        if (!mishaps.Contains(mishap)) mishaps.Add(mishap);
    }
}
