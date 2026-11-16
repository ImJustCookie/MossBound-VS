using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Script;

public readonly record struct SlotKey(CircleRing Ring, int Slot);

// The editable drawing on the client. Converted to/from the network Circle.
public sealed class CircleDraft
{
    public Dictionary<SlotKey, GlyphFigure> Figures { get; } = new();
    public HashSet<(SlotKey A, SlotKey B)> Links { get; } = new();

    public static (SlotKey, SlotKey) Normalize(SlotKey a, SlotKey b) =>
        (a.Ring, a.Slot).CompareTo((b.Ring, b.Slot)) <= 0 ? (a, b) : (b, a);

    // The grammar's linking rule: a link joins neighbouring rings, or neighbouring slots on one ring
    // (wrapping around). Links never skip a ring, so they don't run under glyphs they don't join.
    public static bool CanLink(SlotKey a, SlotKey b, CircleTier tier)
    {
        if (!tier.HasSlot(a.Ring, a.Slot) || !tier.HasSlot(b.Ring, b.Slot) || a == b) return false;
        if (a.Ring != b.Ring) return System.Math.Abs(a.Ring - b.Ring) == 1;
        int n = tier.SlotCount(a.Ring), d = System.Math.Abs(a.Slot - b.Slot);
        return d == 1 || d == n - 1;
    }

    public void ToggleLink(SlotKey a, SlotKey b)
    {
        if (a == b) return;
        var key = Normalize(a, b);
        if (!Links.Remove(key)) Links.Add(key);
    }

    public void Clear(SlotKey slot)
    {
        Figures.Remove(slot);
        Links.RemoveWhere(l => l.A == slot || l.B == slot);
    }

    // Only finished glyphs (with at least one line) are sent; links to unfinished ones are dropped.
    public Circle ToCircle()
    {
        var keys = Figures.Where(f => f.Value.IsValid).Select(f => f.Key)
            .OrderBy(k => k.Ring).ThenBy(k => k.Slot).ToList();
        var index = keys.Select((k, i) => (k, i)).ToDictionary(t => t.k, t => t.i);

        var circle = new Circle();
        foreach (var k in keys)
        {
            var f = Figures[k];
            circle.Glyphs.Add(new PlacedGlyph { Ring = k.Ring, Slot = k.Slot, Links = f.Links, Marks = f.Marks });
        }
        foreach (var (a, b) in Links)
        {
            if (index.TryGetValue(a, out int ia) && index.TryGetValue(b, out int ib))
            {
                circle.Links.Add(new CircleLink { From = ia, To = ib });
            }
        }
        return circle;
    }

    public static CircleDraft FromCircle(Circle? circle)
    {
        var draft = new CircleDraft();
        if (circle == null) return draft;
        foreach (var g in circle.Glyphs) draft.Figures[new SlotKey(g.Ring, g.Slot)] = g.Figure;
        foreach (var l in circle.Links)
        {
            if (l.From < 0 || l.To < 0 || l.From >= circle.Glyphs.Count || l.To >= circle.Glyphs.Count) continue;
            var a = circle.Glyphs[l.From];
            var b = circle.Glyphs[l.To];
            draft.Links.Add(Normalize(new SlotKey(a.Ring, a.Slot), new SlotKey(b.Ring, b.Slot)));
        }
        return draft;
    }
}
