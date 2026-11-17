using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

public sealed record MixResult(IReadOnlyList<(Aspect aspect, int intensity)> Aspects, IReadOnlyList<Mishap> Mishaps);

// Mixes the elements of one heart. A compound uses the smaller count of its elements; each element
// joins at most one compound; what is left over stays pure.
public static class ElementMixer
{
    public const int MaxIntensity = 3;

    static readonly (Element a, Element b)[] opposites =
    {
        (Element.Ember, Element.Frost),
        (Element.Wind, Element.Ward),
    };

    static readonly Element[] mossTriple = { Element.Life, Element.Water, Element.Stone };

    // Tried in this order.
    static readonly (Element a, Element b, Aspect result)[] pairs =
    {
        (Element.Water, Element.Frost, Aspect.Ice),
        (Element.Water, Element.Ember, Aspect.Steam),
        (Element.Water, Element.Life, Aspect.Rain),
        (Element.Water, Element.Rot, Aspect.Mire),
        (Element.Life, Element.Stone, Aspect.Root),
        (Element.Life, Element.Rot, Aspect.Spore),
        (Element.Life, Element.Ward, Aspect.Bark),
        (Element.Frost, Element.Wind, Aspect.Gale),
        (Element.Ember, Element.Wind, Aspect.Smoke),
        (Element.Ember, Element.Stone, Aspect.Coal),
        (Element.Stone, Element.Ward, Aspect.Wall),
        (Element.Rot, Element.Wind, Aspect.Blight),
    };

    public static MixResult Mix(IEnumerable<Element> elements)
    {
        var count = new int[Enum.GetValues<Element>().Length];
        foreach (var e in elements) count[(int)e]++;

        var mishaps = MishapsOf(count);
        if (mishaps.Count > 0) return new MixResult(Array.Empty<(Aspect, int)>(), mishaps);

        var aspects = new List<(Aspect, int)>();
        var used = new bool[count.Length];
        TakeCompound(count, used, aspects, mossTriple, Aspect.Moss);
        foreach (var (a, b, result) in pairs) TakeCompound(count, used, aspects, new[] { a, b }, result);
        for (int e = 0; e < count.Length; e++)
            if (count[e] > 0) aspects.Add(((Aspect)e, count[e]));
        return new MixResult(aspects, mishaps);
    }

    static List<Mishap> MishapsOf(int[] count)
    {
        var mishaps = new List<Mishap>();
        if (count.All(c => c == 0)) mishaps.Add(Mishap.NoElement);
        if (count.Any(c => c > MaxIntensity)) mishaps.Add(Mishap.Overload);
        if (opposites.Any(o => count[(int)o.a] > 0 && count[(int)o.b] > 0)) mishaps.Add(Mishap.Opposition);
        return mishaps;
    }

    static void TakeCompound(int[] count, bool[] used, List<(Aspect, int)> aspects, Element[] parts, Aspect result)
    {
        if (parts.Any(e => used[(int)e])) return;
        int n = parts.Min(e => count[(int)e]);
        if (n == 0) return;
        aspects.Add((result, n));
        foreach (var e in parts)
        {
            count[(int)e] -= n;
            used[(int)e] = true;
        }
    }
}
