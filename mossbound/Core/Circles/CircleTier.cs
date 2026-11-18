using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

// What a medium can hold: its rings, their slots, and what may be written on it.
public sealed class CircleTier
{
    public static readonly CircleTier Moss = new(1, "moss", new(2, 0, 1), (CircleRing.Heart, 2));
    public static readonly CircleTier Bark = new(2, "bark", new(3, 2, 2), (CircleRing.Heart, 3), (CircleRing.Shape, 4));
    public static readonly CircleTier Parchment = new(3, "parchment", new(4, 3, 3), (CircleRing.Heart, 4), (CircleRing.Shape, 6), (CircleRing.Aim, 8));
    public static readonly CircleTier Stone = new(4, "stone", new(5, 4, 4), (CircleRing.Heart, 4), (CircleRing.Shape, 6), (CircleRing.Aim, 8), (CircleRing.Rim, 4));

    static readonly CircleTier[] all = { Moss, Bark, Parchment, Stone };

    readonly Dictionary<CircleRing, int> slotCounts;

    public int Level { get; }
    public string Art { get; } // folder under textures/gui/ holding canvas.png and glyphpanel.png
    public IReadOnlyList<CircleRing> Rings { get; }
    public SpellLimits Limits { get; }

    CircleTier(int level, string art, SpellLimits limits, params (CircleRing ring, int slots)[] rings)
    {
        Level = level;
        Art = art;
        Limits = limits;
        slotCounts = rings.ToDictionary(r => r.ring, r => r.slots);
        Rings = rings.Select(r => r.ring).ToArray();
    }

    public static CircleTier ForLevel(int level) => all[Math.Clamp(level, 1, all.Length) - 1];

    public int MaxSize => Level == 1 ? 2 : 3;
    public bool ChoosesSource => Level >= 2;

    public int SlotCount(CircleRing ring) => slotCounts.GetValueOrDefault(ring);

    public bool HasSlot(SlotKey key) => key.Slot >= 0 && key.Slot < SlotCount(key.Ring);

    // A link joins neighbouring rings, or neighbouring slots of one ring (the last slot neighbours the
    // first). It never skips a ring.
    public bool AllowsLink(SlotKey a, SlotKey b)
    {
        if (a == b || !HasSlot(a) || !HasSlot(b)) return false;
        if (a.Ring != b.Ring) return Math.Abs(a.Ring - b.Ring) == 1;
        int apart = Math.Abs(a.Slot - b.Slot);
        return apart == 1 || apart == SlotCount(a.Ring) - 1;
    }
}

// The most a medium's spells can hold: summed heart sizes, summed form sizes, spells per circle.
public readonly record struct SpellLimits(int Strength, int Scale, int Spells);
