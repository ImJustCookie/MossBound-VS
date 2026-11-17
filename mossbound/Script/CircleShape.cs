using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Script;

// One cast as written: the heart it draws from, and the glyphs linked outward from that heart.
// Drawn as a Gallifreyan "word" circle around them. `AimEnd` is the last glyph of its aim chain (the
// aim glyph the most links away from the heart), or null when the cast doesn't reach the aim ring.
public sealed record Word(IReadOnlyList<SlotKey> Heart, IReadOnlyList<SlotKey> Body, SlotKey? AimEnd)
{
    // The cast reaches the aim ring: it points at something.
    public bool Aimed => AimEnd != null;
}

// The structure the circle's drawing is built from, read from where glyphs sit and how they are
// linked (never from what they mean: the client doesn't know the script).
public static class CircleShape
{
    // Hearts: heart glyphs linked together form one heart (a mix); an unlinked one is a heart alone.
    public static List<List<SlotKey>> Hearts(IEnumerable<SlotKey> glyphs, IEnumerable<(SlotKey A, SlotKey B)> links) =>
        Components(glyphs.Where(IsHeart), links);

    // The hearts that mix two elements or more (drawn with a mix circle).
    public static List<List<SlotKey>> MixGroups(IEnumerable<SlotKey> glyphs, IEnumerable<(SlotKey A, SlotKey B)> links) =>
        Hearts(glyphs, links).Where(g => g.Count >= 2).ToList();

    // One word per (heart group, linked body): a body is a group of linked non-heart glyphs. A body
    // fed by two hearts gives two words; one with no heart is not a cast and gets no word.
    public static List<Word> Words(IEnumerable<SlotKey> glyphs, IEnumerable<(SlotKey A, SlotKey B)> links)
    {
        var all = glyphs.ToHashSet();
        var linkList = links.Where(l => all.Contains(l.A) && all.Contains(l.B)).ToList();
        var hearts = Components(all.Where(IsHeart), linkList);
        var bodies = Components(all.Where(k => !IsHeart(k)), linkList);
        var words = new List<Word>();
        foreach (var heart in hearts)
        {
            foreach (var body in bodies)
            {
                if (linkList.Any(l => heart.Contains(l.A) && body.Contains(l.B) || heart.Contains(l.B) && body.Contains(l.A)))
                {
                    words.Add(new Word(heart, body, AimEnd(heart, body, linkList)));
                }
            }
        }
        return words;
    }

    static bool IsHeart(SlotKey k) => k.Ring == CircleRing.Centre;

    // The end of the aim chain: the aim glyph the most link steps from the heart (ties: the later slot).
    static SlotKey? AimEnd(List<SlotKey> heart, List<SlotKey> body, List<(SlotKey A, SlotKey B)> links)
    {
        var steps = heart.ToDictionary(k => k, _ => 0);
        var queue = new Queue<SlotKey>(heart);
        while (queue.Count > 0)
        {
            var k = queue.Dequeue();
            foreach (var (a, b) in links)
            {
                var next = a == k ? b : b == k ? a : (SlotKey?)null;
                if (next is SlotKey n && body.Contains(n) && !steps.ContainsKey(n))
                {
                    steps[n] = steps[k] + 1;
                    queue.Enqueue(n);
                }
            }
        }
        return body.Where(k => k.Ring == CircleRing.Outer && steps.ContainsKey(k))
            .OrderBy(k => steps[k]).ThenBy(k => k.Slot)
            .Select(k => (SlotKey?)k).LastOrDefault();
    }

    // Groups of glyphs joined by links among themselves, in a stable order.
    static List<List<SlotKey>> Components(IEnumerable<SlotKey> members, IEnumerable<(SlotKey A, SlotKey B)> links)
    {
        var set = members.OrderBy(k => k.Ring).ThenBy(k => k.Slot).ToList();
        var inSet = set.ToHashSet();
        var next = new Dictionary<SlotKey, List<SlotKey>>();
        foreach (var (a, b) in links)
        {
            if (!inSet.Contains(a) || !inSet.Contains(b)) continue;
            (next.TryGetValue(a, out var la) ? la : next[a] = new()).Add(b);
            (next.TryGetValue(b, out var lb) ? lb : next[b] = new()).Add(a);
        }
        var seen = new HashSet<SlotKey>();
        var groups = new List<List<SlotKey>>();
        foreach (var start in set)
        {
            if (!seen.Add(start)) continue;
            var group = new List<SlotKey>();
            var todo = new Stack<SlotKey>();
            todo.Push(start);
            while (todo.Count > 0)
            {
                var k = todo.Pop();
                group.Add(k);
                foreach (var n in next.GetValueOrDefault(k) ?? new()) if (seen.Add(n)) todo.Push(n);
            }
            groups.Add(group.OrderBy(k => k.Ring).ThenBy(k => k.Slot).ToList());
        }
        return groups;
    }

    // A word's circle: around its glyphs, then pushed out until it cuts into the circle's border by
    // `bite` of its own radius, like a Gallifreyan word biting its sentence circle. A heavier word
    // (`extraWeight` = how far its glyphs' sizes go past 1, summed) is bigger: +10% per point, up to
    // +50%, and never more than 85% of the circle it bites.
    public static (double x, double y, double r) WordCircle(IReadOnlyList<(double x, double y)> glyphCentres, double glyphRadius,
        (double x, double y) centre, double border, double scale = 1.18, double bite = 0.1, int extraWeight = 0)
    {
        double wx = glyphCentres.Average(p => p.x), wy = glyphCentres.Average(p => p.y);
        double wr = glyphCentres.Max(p => Math.Sqrt((p.x - wx) * (p.x - wx) + (p.y - wy) * (p.y - wy))) + glyphRadius * 0.6;
        wr = Math.Min(wr * scale * (1 + 0.1 * Math.Clamp(extraWeight, 0, 5)), border * 0.85);
        double dx = wx - centre.x, dy = wy - centre.y, d = Math.Sqrt(dx * dx + dy * dy);
        if (d < 1e-9) return (wx, wy, wr);
        double want = Math.Max(0, border + wr * bite - wr); // distance from the centre that makes the bite
        return (centre.x + dx / d * want, centre.y + dy / d * want, wr);
    }

    // Where the line through a and b meets a circle (both ends), for lines running across the circle.
    public static ((double x, double y) from, (double x, double y) to)? Chord((double x, double y) a, (double x, double y) b,
        (double x, double y) centre, double radius)
    {
        double dx = b.x - a.x, dy = b.y - a.y, len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return null;
        dx /= len; dy /= len;
        double px = a.x - centre.x, py = a.y - centre.y;
        double p = px * dx + py * dy, q = px * px + py * py - radius * radius, disc = p * p - q;
        if (disc < 0) return null;
        double s = Math.Sqrt(disc);
        return ((a.x + dx * (-p - s), a.y + dy * (-p - s)), (a.x + dx * (-p + s), a.y + dy * (-p + s)));
    }
}
