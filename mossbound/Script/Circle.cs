using System;
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
    [ProtoMember(5)] public int Size { get; set; }  // how strongly it is written, 1-3; 0 = written before sizes existed

    public GlyphFigure Figure => new(Links, Marks);
    public int Power => Math.Max(1, Size);
}

[ProtoContract]
public class CircleLink
{
    [ProtoMember(1)] public int From { get; set; }
    [ProtoMember(2)] public int To { get; set; }
}

// Where a spell takes its life from. Not secret vocabulary: it is shown by how the border is drawn.
public enum LifeSource
{
    Medium, // the medium's own life, then the caster's (the default; also every strip written before sources)
    Self,   // the caster's body only
    Land,   // the living things around the circle (plants, soil, animals)
}

[ProtoContract]
public class Circle
{
    [ProtoMember(1)] public List<PlacedGlyph> Glyphs { get; set; } = new();
    [ProtoMember(2)] public List<CircleLink> Links { get; set; } = new();
    [ProtoMember(3)] public LifeSource Source { get; set; }
}
