using System;
using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Script;

// What a medium can hold: which rings it has and how many glyph slots each ring offers. Each medium
// item declares its tier; the server validates every inscription against it.
public sealed class CircleTier
{
    public static readonly CircleTier Moss = new(1, "moss", (CircleRing.Centre, 2));
    public static readonly CircleTier Bark = new(2, "bark", (CircleRing.Centre, 3), (CircleRing.Inner, 4));
    public static readonly CircleTier Parchment = new(3, "parchment", (CircleRing.Centre, 4), (CircleRing.Inner, 6), (CircleRing.Outer, 8));
    public static readonly CircleTier Stone = new(4, "stone", (CircleRing.Centre, 4), (CircleRing.Inner, 6), (CircleRing.Outer, 8), (CircleRing.Rim, 4));

    static readonly CircleTier[] all = { Moss, Bark, Parchment, Stone };

    readonly Dictionary<CircleRing, int> slots;

    public int Level { get; }

    // Folder under textures/gui/ holding this tier's canvas.png and glyphpanel.png.
    public string Art { get; }

    // Rings this tier offers, from the centre outward.
    public IReadOnlyList<CircleRing> Rings { get; }

    CircleTier(int level, string art, params (CircleRing ring, int slots)[] rings)
    {
        Level = level;
        Art = art;
        slots = rings.ToDictionary(r => r.ring, r => r.slots);
        Rings = rings.Select(r => r.ring).ToArray();
    }

    public int SlotCount(CircleRing ring) => slots.GetValueOrDefault(ring);

    public bool HasSlot(CircleRing ring, int slot) => slot >= 0 && slot < SlotCount(ring);

    // Unknown levels clamp to the nearest tier, so a typo in item JSON can't crash the game.
    public static CircleTier ForLevel(int level) => all[Math.Clamp(level, 1, all.Length) - 1];
}
