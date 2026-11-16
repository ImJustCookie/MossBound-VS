using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Script;

// What a spell is made of after mixing: the 8 pure elements plus the compounds.
public enum Aspect
{
    Water, Ember, Frost, Stone, Life, Rot, Wind, Ward,
    Ice, Steam, Rain, Mire, Root, Spore, Bark, Gale, Smoke, Coal, Wall, Blight,
    Moss,
}

public enum Mishap
{
    Overload,     // more than MaxIntensity of one element
    Opposition,   // opposite elements in the same mix
    Misplaced,    // a glyph in a ring where its kind doesn't belong
    NoElement,    // nothing to cast
}

public sealed record MixResult(IReadOnlyList<(Aspect aspect, int intensity)> Aspects, IReadOnlyList<Mishap> Mishaps)
{
    public bool Failed => Mishaps.Count > 0;
}

public static class ElementMixer
{
    public const int MaxIntensity = 3;

    static readonly (Element a, Element b)[] opposites =
    {
        (Element.Ember, Element.Frost),
        (Element.Wind, Element.Ward),
    };

    // Checked in this order; an element joins at most one compound.
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
        var count = new int[8];
        foreach (var e in elements) count[(int)e]++;

        var mishaps = new List<Mishap>();
        if (count.All(c => c == 0)) mishaps.Add(Mishap.NoElement);
        if (count.Any(c => c > MaxIntensity)) mishaps.Add(Mishap.Overload);
        if (opposites.Any(o => count[(int)o.a] > 0 && count[(int)o.b] > 0)) mishaps.Add(Mishap.Opposition);
        if (mishaps.Count > 0) return new MixResult(new List<(Aspect, int)>(), mishaps);

        var aspects = new List<(Aspect, int)>();
        var mixed = new bool[8];

        int moss = new[] { count[(int)Element.Life], count[(int)Element.Water], count[(int)Element.Stone] }.Min();
        if (moss > 0)
        {
            aspects.Add((Aspect.Moss, moss));
            foreach (var e in new[] { Element.Life, Element.Water, Element.Stone })
            {
                count[(int)e] -= moss;
                mixed[(int)e] = true;
            }
        }

        foreach (var (a, b, result) in pairs)
        {
            if (mixed[(int)a] || mixed[(int)b]) continue;
            int n = System.Math.Min(count[(int)a], count[(int)b]);
            if (n == 0) continue;
            aspects.Add((result, n));
            count[(int)a] -= n;
            count[(int)b] -= n;
            mixed[(int)a] = mixed[(int)b] = true;
        }

        for (int e = 0; e < count.Length; e++)
        {
            if (count[e] > 0) aspects.Add(((Aspect)e, count[e]));
        }
        return new MixResult(aspects, mishaps);
    }
}
