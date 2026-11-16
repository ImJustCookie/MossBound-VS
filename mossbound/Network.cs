using System.Collections.Generic;
using Mossbound.Script;
using ProtoBuf;

namespace Mossbound;

// Client → server: "inscribe this drawing on the medium I'm holding". The server checks the held
// item and the drawing's bounds; it never tells the client what the glyphs mean.
[ProtoContract]
public class InscribeRequest
{
    [ProtoMember(1)] public Circle? Circle { get; set; }
}

// Server → one admin client, only after `/mossbound glyphs` passed the privilege check: the world's
// secret glyphs, for the admin atlas window. Never sent to anyone else.
[ProtoContract]
public class GlyphAtlas
{
    [ProtoMember(1)] public List<GlyphAtlasEntry> Entries { get; set; } = new();
}

[ProtoContract]
public class GlyphAtlasEntry
{
    [ProtoMember(1)] public string Code { get; set; } = "";
    [ProtoMember(2)] public uint Links { get; set; }
    [ProtoMember(3)] public uint Marks { get; set; }
}
