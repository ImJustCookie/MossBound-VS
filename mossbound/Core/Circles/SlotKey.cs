using System;

namespace Mossbound.Core;

// Sorted by ring, then slot.
public readonly record struct SlotKey(CircleRing Ring, int Slot) : IComparable<SlotKey>
{
    public int CompareTo(SlotKey other) => (Ring, Slot).CompareTo((other.Ring, other.Slot));
}
