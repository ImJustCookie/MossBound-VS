using System.Collections.Generic;
using ProtoBuf;

namespace Mossbound.Script;

public enum CircleRing
{
    Centre,
    Inner,
    Outer,
    Rim,
}

// What the client sends: the raw drawing. The server decides what it means.
// Protobuf field numbers are permanent: only add new numbers, never reuse or renumber.
[ProtoContract]
public class PlacedGlyph
{
    [ProtoMember(1)] public CircleRing Ring { get; set; }
    [ProtoMember(2)] public int Slot { get; set; }
    [ProtoMember(3)] public uint Links { get; set; }
    [ProtoMember(4)] public uint Marks { get; set; }

    public GlyphFigure Figure => new(Links, Marks);
}

[ProtoContract]
public class CircleLink
{
    [ProtoMember(1)] public int From { get; set; }
    [ProtoMember(2)] public int To { get; set; }
}

[ProtoContract]
public class Circle
{
    [ProtoMember(1)] public List<PlacedGlyph> Glyphs { get; set; } = new();
    [ProtoMember(2)] public List<CircleLink> Links { get; set; } = new();
}
