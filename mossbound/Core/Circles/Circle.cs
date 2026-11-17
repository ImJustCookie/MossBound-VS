using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Mossbound.Core;

// What is saved on a medium and sent to the server: the writing only, never its meaning.
// Protobuf field numbers are permanent: add new numbers, never reuse or renumber one.
[ProtoContract]
public class Circle
{
    [ProtoMember(1)] public List<PlacedGlyph> Glyphs { get; set; } = new();
    [ProtoMember(2)] public List<CircleLink> Links { get; set; } = new();
    [ProtoMember(3)] public LifeSource Source { get; set; }
}

[ProtoContract]
public class PlacedGlyph
{
    [ProtoMember(1)] public CircleRing Ring { get; set; }
    [ProtoMember(2)] public int Slot { get; set; }
    [ProtoMember(3)] public uint Lines { get; set; }
    [ProtoMember(4)] public uint Marks { get; set; }
    [ProtoMember(5)] public int Size { get; set; } // 0 on strips written before sizes existed

    public SlotKey Key => new(Ring, Slot);
    public GlyphFigure Figure => new(Lines, Marks);
    public int EffectiveSize => Math.Max(1, Size);
}

// Two glyphs joined, by their index in Circle.Glyphs.
[ProtoContract]
public class CircleLink
{
    [ProtoMember(1)] public int From { get; set; }
    [ProtoMember(2)] public int To { get; set; }
}
