using Mossbound.Core;

namespace Mossbound.Tests;

// Glyphs placed with one fixed world's script, and short names for slots.
static class TestGlyphs
{
    public static readonly WorldScript Script = new(99);

    public static PlacedGlyph Place(string code, CircleRing ring, int slot = 0)
    {
        var f = Script.FigureOf(code);
        return new PlacedGlyph { Ring = ring, Slot = slot, Lines = f.Lines, Marks = f.Marks };
    }

    public static SlotKey Heart(int slot) => new(CircleRing.Heart, slot);
    public static SlotKey Shape(int slot) => new(CircleRing.Shape, slot);
    public static SlotKey Aim(int slot) => new(CircleRing.Aim, slot);
}
