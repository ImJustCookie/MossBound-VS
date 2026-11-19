using System.Collections.Generic;
using Mossbound.Core;
using ProtoBuf;

namespace Mossbound.Network;

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
    [ProtoMember(2)] public bool ForDevBook { get; set; } // open the dev book's canvas instead of the atlas window

    public static GlyphAtlas Of(WorldScript script)
    {
        var atlas = new GlyphAtlas();
        foreach (var glyph in GlyphRegistry.All)
        {
            var f = script.FigureOf(glyph.Code);
            atlas.Entries.Add(new GlyphAtlasEntry { Code = glyph.Code, Lines = f.Lines, Marks = f.Marks });
        }
        return atlas;
    }
}

[ProtoContract]
public class GlyphAtlasEntry
{
    [ProtoMember(1)] public string Code { get; set; } = "";
    [ProtoMember(2)] public uint Lines { get; set; }
    [ProtoMember(3)] public uint Marks { get; set; }
}

// Client → server: "open the dev book I'm holding". Answered with the glyph atlas (ForDevBook) after the checks.
[ProtoContract]
public class DevBookOpen
{
}

// Client → server: "write this circle, for this tier, on the dev book I'm holding".
[ProtoContract]
public class DevBookWrite
{
    [ProtoMember(1)] public Circle? Circle { get; set; }
    [ProtoMember(2)] public int TierLevel { get; set; }
}
