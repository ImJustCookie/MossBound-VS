using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

// The editable drawing on the client. Converted to/from the network Circle.
public sealed class CircleDraft
{
    public Dictionary<SlotKey, GlyphFigure> Figures { get; } = new();
    public HashSet<(SlotKey A, SlotKey B)> Links { get; } = new();
    public LifeSource Source { get; set; }
    readonly Dictionary<SlotKey, int> sizes = new(); // only glyphs bigger than 1

    public int SizeOf(SlotKey k) => sizes.GetValueOrDefault(k, 1);

    // Sizes below 1 count as 1; the tier's cap is the caller's to apply.
    public void SetSize(SlotKey k, int size)
    {
        if (size > 1) sizes[k] = size;
        else sizes.Remove(k);
    }

    public static (SlotKey, SlotKey) Normalize(SlotKey a, SlotKey b) =>
        (a.Ring, a.Slot).CompareTo((b.Ring, b.Slot)) <= 0 ? (a, b) : (b, a);

    public void ToggleLink(SlotKey a, SlotKey b)
    {
        if (a == b) return;
        var key = Normalize(a, b);
        if (!Links.Remove(key)) Links.Add(key);
    }

    public void Clear(SlotKey slot)
    {
        Figures.Remove(slot);
        sizes.Remove(slot);
        Links.RemoveWhere(l => l.A == slot || l.B == slot);
    }

    // Only finished glyphs (with at least one line) are sent; links to unfinished ones are dropped.
    public Circle ToCircle()
    {
        var keys = Figures.Where(f => f.Value.IsValid).Select(f => f.Key)
            .OrderBy(k => k.Ring).ThenBy(k => k.Slot).ToList();
        var index = keys.Select((k, i) => (k, i)).ToDictionary(t => t.k, t => t.i);

        var circle = new Circle { Source = Source };
        foreach (var k in keys)
        {
            var f = Figures[k];
            circle.Glyphs.Add(new PlacedGlyph { Ring = k.Ring, Slot = k.Slot, Lines = f.Lines, Marks = f.Marks, Size = SizeOf(k) });
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
        draft.Source = circle.Source;
        foreach (var g in circle.Glyphs)
        {
            draft.Figures[g.Key] = g.Figure;
            draft.SetSize(g.Key, g.EffectiveSize);
        }
        foreach (var l in circle.Links)
        {
            if (l.From < 0 || l.To < 0 || l.From >= circle.Glyphs.Count || l.To >= circle.Glyphs.Count) continue;
            draft.Links.Add(Normalize(circle.Glyphs[l.From].Key, circle.Glyphs[l.To].Key));
        }
        return draft;
    }
}
