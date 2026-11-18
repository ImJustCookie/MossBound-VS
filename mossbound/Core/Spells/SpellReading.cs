using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

// One spell from a circle: what it is (aspect), how it travels (form), how hard (strength) and how big (scale).
public sealed record SpellCell(Aspect Aspect, Form Form, int Strength, int Scale);

public sealed record SpellPlan(IReadOnlyList<SpellCell> Cells, IReadOnlyList<Mishap> Mishaps)
{
    public bool Fizzles => Mishaps.Count > 0;
}

// Reads a circle with the world's script. Each heart mixes into one aspect; each form group linked to it mixes
// into one form and makes one spell; a heart with no form group makes one Touch spell. Sizes add up to strength
// (heart) and scale (form group). Any failure fails the whole circle.
public static class SpellReading
{
    // Call only after CircleValidation.FitsTier.
    public static SpellPlan Read(Circle circle, WorldScript script, CircleTier tier) =>
        new Reader(circle, script, tier.Limits).Read();

    sealed class Reader
    {
        readonly Dictionary<SlotKey, PlacedGlyph> glyphs;
        readonly Dictionary<SlotKey, GlyphDef?> meanings;
        readonly List<SlotKey> keys;
        readonly List<(SlotKey, SlotKey)> links;
        readonly List<Cast> casts;
        readonly SpellLimits limits;
        readonly List<Mishap> mishaps = new();

        public Reader(Circle circle, WorldScript script, SpellLimits limits)
        {
            glyphs = circle.Glyphs.ToDictionary(g => g.Key);
            meanings = circle.Glyphs.ToDictionary(g => g.Key, g => script.Recognize(g.Figure));
            keys = circle.Glyphs.Select(g => g.Key).ToList();
            links = circle.Links.Select(l => (keys[l.From], keys[l.To])).ToList();
            casts = CircleStructure.Casts(keys, links);
            this.limits = limits;
        }

        public SpellPlan Read()
        {
            if (keys.Any(IsMisplaced)) Add(Mishap.Misplaced);
            if (links.Any(l => IsUnknown(l.Item1) || IsUnknown(l.Item2))) Add(Mishap.Unknown);
            var spells = CircleStructure.Hearts(keys, links).SelectMany(SpellsOf).ToList();
            if (!keys.Any(IsElement)) Add(Mishap.NoElement);
            if (spells.Count > limits.Spells) Add(Mishap.TooManySpells);
            return new SpellPlan(mishaps.Count > 0 ? Array.Empty<SpellCell>() : spells, mishaps);
        }

        List<SpellCell> SpellsOf(List<SlotKey> heart)
        {
            var spells = new List<SpellCell>();
            var elements = heart.Where(IsElement).ToList();
            if (elements.Count == 0 || Mix(Recipes.Hearts, elements, ElementOf, limits.Strength, Mishap.Overload) is not { } heartMix) return spells;

            var formGroups = FormGroupsOf(heart);
            if (formGroups.Count == 0) spells.Add(new SpellCell(heartMix.result, Form.Touch, heartMix.total, 0));
            foreach (var group in formGroups)
                if (Mix(Recipes.Forms, group, FormOf, limits.Scale, Mishap.Oversized) is { } formMix)
                    spells.Add(new SpellCell(heartMix.result, formMix.result, heartMix.total, formMix.total));
            return spells;
        }

        // The form glyphs of each cast fed by this heart; casts with no form are left out.
        List<List<SlotKey>> FormGroupsOf(List<SlotKey> heart) => casts
            .Where(cast => cast.Heart.SequenceEqual(heart))
            .Select(cast => cast.Body.Where(IsForm).ToList())
            .Where(group => group.Count > 0)
            .ToList();

        // A group's one result and the sum of its sizes; null when it mixes into nothing or is over the limit.
        (TResult result, int total)? Mix<TPart, TResult>(Mixer<TPart, TResult> mixer, List<SlotKey> group, Func<SlotKey, TPart> partOf,
            int limit, Mishap overLimit) where TPart : struct, Enum where TResult : struct
        {
            var (result, mishap) = mixer.Mix(group.Select(partOf).ToList());
            int total = group.Sum(k => glyphs[k].EffectiveSize);
            if (mishap is Mishap failed) Add(failed);
            else if (total > limit) Add(overLimit);
            else return (result!.Value, total);
            return null;
        }

        bool IsUnknown(SlotKey key) => meanings[key] == null;
        bool IsElement(SlotKey key) => meanings[key]?.Element != null;
        bool IsForm(SlotKey key) => meanings[key]?.Form != null;
        Element ElementOf(SlotKey key) => meanings[key]!.Element!.Value;
        Form FormOf(SlotKey key) => meanings[key]!.Form!.Value;

        bool IsMisplaced(SlotKey key) =>
            IsElement(key) && key.Ring != CircleRing.Heart || IsForm(key) && key.Ring != CircleRing.Shape;

        void Add(Mishap mishap)
        {
            if (!mishaps.Contains(mishap)) mishaps.Add(mishap);
        }
    }
}
