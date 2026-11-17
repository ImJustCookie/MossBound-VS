using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

// The circle being written in the inscription window. Holds every editing rule, so the window only
// turns mouse actions into calls here. Converted to and from the saved Circle.
public sealed class CircleDraft
{
    readonly Dictionary<SlotKey, GlyphFigure> figures = new();
    readonly HashSet<(SlotKey A, SlotKey B)> links = new();
    readonly Dictionary<SlotKey, int> sizes = new(); // only glyphs bigger than 1

    public LifeSource Source { get; set; }
    public IReadOnlyDictionary<SlotKey, GlyphFigure> Figures => figures;
    public IReadOnlyCollection<(SlotKey A, SlotKey B)> Links => links;

    public bool IsWritten(SlotKey slot) => figures.ContainsKey(slot);
    public GlyphFigure FigureAt(SlotKey slot) => figures.GetValueOrDefault(slot);
    public int SizeOf(SlotKey slot) => sizes.GetValueOrDefault(slot, 1);
    public bool HasLink(SlotKey a, SlotKey b) => links.Contains(Ordered(a, b));
    public bool CanLink(SlotKey a, SlotKey b, CircleTier tier) => IsWritten(a) && IsWritten(b) && tier.AllowsLink(a, b);

    // ---------- glyphs ----------

    // Writing an empty figure erases the glyph, with its size and links.
    public void Write(SlotKey slot, GlyphFigure figure)
    {
        if (figure.IsEmpty) Erase(slot);
        else figures[slot] = figure;
    }

    public void Erase(SlotKey slot)
    {
        figures.Remove(slot);
        sizes.Remove(slot);
        links.RemoveWhere(l => l.A == slot || l.B == slot);
    }

    public void DrawLines(SlotKey slot, IEnumerable<(int a, int b)> lines) =>
        Write(slot, lines.Aggregate(FigureAt(slot), (figure, line) => figure.WithLine(line.a, line.b)));

    public void EraseLines(SlotKey slot, IEnumerable<(int a, int b)> lines) =>
        Write(slot, lines.Aggregate(FigureAt(slot), (figure, line) => figure.WithoutLine(line.a, line.b)));

    public void CycleMark(SlotKey slot, int point) => Write(slot, FigureAt(slot).WithMark(point, NextMark(FigureAt(slot).MarkAt(point))));

    public void ClearMark(SlotKey slot, int point) => Write(slot, FigureAt(slot).WithMark(point, Mark.None));

    static Mark NextMark(Mark mark) => mark == Mark.DoubleRing ? Mark.None : mark + 1;

    // ---------- size ----------

    public void SetSize(SlotKey slot, int size)
    {
        if (size > 1) sizes[slot] = size;
        else sizes.Remove(slot);
    }

    public void ChangeSize(SlotKey slot, int delta, CircleTier tier)
    {
        if (IsWritten(slot)) SetSize(slot, Math.Clamp(SizeOf(slot) + delta, 1, tier.MaxSize));
    }

    // ---------- links ----------

    public void Link(SlotKey a, SlotKey b, CircleTier tier)
    {
        if (CanLink(a, b, tier)) links.Add(Ordered(a, b));
    }

    public void Unlink(SlotKey a, SlotKey b) => links.Remove(Ordered(a, b));

    static (SlotKey, SlotKey) Ordered(SlotKey a, SlotKey b) =>
        (a.Ring, a.Slot).CompareTo((b.Ring, b.Slot)) <= 0 ? (a, b) : (b, a);

    // ---------- source ----------

    public void CycleSource(CircleTier tier, bool backwards)
    {
        if (!tier.ChoosesSource) return;
        int count = Enum.GetValues<LifeSource>().Length;
        Source = (LifeSource)(((int)Source + (backwards ? count - 1 : 1)) % count);
    }

    // ---------- saving ----------

    // Only valid glyphs are saved; links to anything else are dropped.
    public Circle ToCircle()
    {
        var keys = figures.Where(f => f.Value.IsValid).Select(f => f.Key).OrderBy(k => k.Ring).ThenBy(k => k.Slot).ToList();
        var indexOf = keys.Select((k, i) => (k, i)).ToDictionary(t => t.k, t => t.i);
        var circle = new Circle { Source = Source };
        foreach (var k in keys)
        {
            var f = figures[k];
            circle.Glyphs.Add(new PlacedGlyph { Ring = k.Ring, Slot = k.Slot, Lines = f.Lines, Marks = f.Marks, Size = SizeOf(k) });
        }
        foreach (var (a, b) in links)
            if (indexOf.TryGetValue(a, out int from) && indexOf.TryGetValue(b, out int to))
                circle.Links.Add(new CircleLink { From = from, To = to });
        return circle;
    }

    // Keeps every link of a saved circle, even ones the linking rule now forbids.
    public static CircleDraft FromCircle(Circle? circle)
    {
        var draft = new CircleDraft();
        if (circle == null) return draft;
        draft.Source = circle.Source;
        foreach (var g in circle.Glyphs)
        {
            draft.figures[g.Key] = g.Figure;
            draft.SetSize(g.Key, g.EffectiveSize);
        }
        foreach (var l in circle.Links)
            if (l.From >= 0 && l.To >= 0 && l.From < circle.Glyphs.Count && l.To < circle.Glyphs.Count)
                draft.links.Add(Ordered(circle.Glyphs[l.From].Key, circle.Glyphs[l.To].Key));
        return draft;
    }
}
