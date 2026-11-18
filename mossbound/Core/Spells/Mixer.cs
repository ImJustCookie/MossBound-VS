using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

// Mixes a group of glyphs into exactly one result. A single glyph is always itself; a group must match a recipe.
// Only which glyphs are in the group counts, never their sizes, and a glyph repeated counts twice. A group
// matching no recipe fails; opposites in one group fail harder.
public sealed class Mixer<TPart, TResult> where TPart : struct, Enum where TResult : struct
{
    readonly Func<TPart, TResult> alone;
    readonly (TPart[] parts, TResult result)[] recipes;
    readonly (TPart a, TPart b)[] opposites;

    public Mixer(Func<TPart, TResult> alone, (TPart[] parts, TResult result)[] recipes, (TPart a, TPart b)[] opposites)
    {
        this.alone = alone;
        this.recipes = recipes.Select(r => (r.parts.Order().ToArray(), r.result)).ToArray();
        this.opposites = opposites;
    }

    public (TResult? result, Mishap? mishap) Mix(IReadOnlyCollection<TPart> parts)
    {
        if (parts.Count == 1) return (alone(parts.First()), null);
        if (opposites.Any(o => parts.Contains(o.a) && parts.Contains(o.b))) return (null, Mishap.Opposition);
        var sorted = parts.Order().ToArray();
        foreach (var (recipe, result) in recipes)
            if (recipe.SequenceEqual(sorted)) return (result, null);
        return (null, Mishap.NoMix);
    }
}
