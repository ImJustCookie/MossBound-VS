using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

// One cast: a heart and every glyph reached from it by following links outward. AimEnd is the last
// glyph of its aim chain, or null when the cast doesn't reach the aim ring.
public sealed record Cast(IReadOnlyList<SlotKey> Heart, IReadOnlyList<SlotKey> Body, SlotKey? AimEnd)
{
    public bool IsAimed => AimEnd != null;
}

// How a circle is put together, read from where its glyphs sit and how they are linked. Used both by
// the server (which hearts mix) and by the drawing (casts, mixes), so the two always agree.
public static class CircleStructure
{
    // Heart glyphs linked together form one heart; an unlinked one is a heart on its own.
    public static List<List<SlotKey>> Hearts(IEnumerable<SlotKey> glyphs, IEnumerable<(SlotKey A, SlotKey B)> links) =>
        LinkedGroups(glyphs.Where(IsHeart), links);

    public static List<List<SlotKey>> Mixes(IEnumerable<SlotKey> glyphs, IEnumerable<(SlotKey A, SlotKey B)> links) =>
        Hearts(glyphs, links).Where(heart => heart.Count >= 2).ToList();

    // One cast per (heart, linked body). A body fed by two hearts gives two casts; a body with no
    // heart is not a cast.
    public static List<Cast> Casts(IEnumerable<SlotKey> glyphs, IEnumerable<(SlotKey A, SlotKey B)> links)
    {
        var written = glyphs.ToHashSet();
        var validLinks = links.Where(l => written.Contains(l.A) && written.Contains(l.B)).ToList();
        var bodies = LinkedGroups(written.Where(k => !IsHeart(k)), validLinks);
        var casts = new List<Cast>();
        foreach (var heart in LinkedGroups(written.Where(IsHeart), validLinks))
            foreach (var body in bodies.Where(body => AreLinked(heart, body, validLinks)))
                casts.Add(new Cast(heart, body, AimEnd(heart, body, validLinks)));
        return casts;
    }

    static bool IsHeart(SlotKey k) => k.Ring == CircleRing.Heart;

    static bool AreLinked(List<SlotKey> a, List<SlotKey> b, List<(SlotKey A, SlotKey B)> links) =>
        links.Any(l => a.Contains(l.A) && b.Contains(l.B) || a.Contains(l.B) && b.Contains(l.A));

    // The aim glyph the most link steps away from the heart (ties go to the later slot).
    static SlotKey? AimEnd(List<SlotKey> heart, List<SlotKey> body, List<(SlotKey A, SlotKey B)> links)
    {
        var steps = StepsFrom(heart, body, links);
        return body.Where(k => k.Ring == CircleRing.Aim && steps.ContainsKey(k))
            .OrderBy(k => steps[k]).ThenBy(k => k.Slot)
            .Select(k => (SlotKey?)k).LastOrDefault();
    }

    static Dictionary<SlotKey, int> StepsFrom(List<SlotKey> start, List<SlotKey> within, List<(SlotKey A, SlotKey B)> links)
    {
        var steps = start.ToDictionary(k => k, _ => 0);
        var queue = new Queue<SlotKey>(start);
        while (queue.Count > 0)
        {
            var k = queue.Dequeue();
            foreach (var next in Neighbours(k, links).Where(n => within.Contains(n) && !steps.ContainsKey(n)))
            {
                steps[next] = steps[k] + 1;
                queue.Enqueue(next);
            }
        }
        return steps;
    }

    static IEnumerable<SlotKey> Neighbours(SlotKey k, IEnumerable<(SlotKey A, SlotKey B)> links) =>
        links.Where(l => l.A == k || l.B == k).Select(l => l.A == k ? l.B : l.A);

    // Groups of glyphs joined by links among themselves, sorted by ring then slot.
    static List<List<SlotKey>> LinkedGroups(IEnumerable<SlotKey> members, IEnumerable<(SlotKey A, SlotKey B)> links)
    {
        var sorted = members.Order().ToList();
        var inside = sorted.ToHashSet();
        var insideLinks = links.Where(l => inside.Contains(l.A) && inside.Contains(l.B)).ToList();
        var seen = new HashSet<SlotKey>();
        var groups = new List<List<SlotKey>>();
        foreach (var start in sorted)
        {
            if (!seen.Add(start)) continue;
            var group = new List<SlotKey>();
            var todo = new Stack<SlotKey>(new[] { start });
            while (todo.Count > 0)
            {
                var k = todo.Pop();
                group.Add(k);
                foreach (var n in Neighbours(k, insideLinks))
                    if (seen.Add(n)) todo.Push(n);
            }
            groups.Add(group.Order().ToList());
        }
        return groups;
    }
}
