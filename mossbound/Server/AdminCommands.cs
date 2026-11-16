using System;
using System.Collections.Generic;
using Mossbound.Script;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Mossbound.Server;

static class AdminCommands
{
    public static void Register(ICoreServerAPI sapi, IServerNetworkChannel channel, Func<WorldScript?> script)
    {
        sapi.ChatCommands.Create("mossbound")
            .WithDescription("Mossbound admin tools")
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("script")
                .WithDescription("List this world's glyphs as text (spoilers!)")
                .HandleWith(_ => TextCommandResult.Success(Describe(script())))
            .EndSubCommand()
            .BeginSubCommand("glyphs")
                .WithDescription("Open a window showing this world's glyphs (spoilers!)")
                .HandleWith(args => SendAtlas(args, channel, script()))
            .EndSubCommand();
    }

    // Only reached by callers with the controlserver privilege; the atlas goes to that player alone.
    static TextCommandResult SendAtlas(TextCommandCallingArgs args, IServerNetworkChannel channel, WorldScript? script)
    {
        if (args.Caller.Player is not IServerPlayer player) return TextCommandResult.Error("Run this in game, not from the console.");
        if (script == null) return TextCommandResult.Error("Script not ready yet.");

        var atlas = new GlyphAtlas();
        foreach (var glyph in GlyphRegistry.All)
        {
            var f = script.FigureOf(glyph.Code);
            atlas.Entries.Add(new GlyphAtlasEntry { Code = glyph.Code, Links = f.Links, Marks = f.Marks });
        }
        channel.SendPacket(atlas, player);
        return TextCommandResult.Success();
    }

    static string Describe(WorldScript? script)
    {
        if (script == null) return "Script not ready yet.";
        var lines = new List<string> { "Points: 0-5 clockwise from top, c = centre." };
        foreach (GlyphKind kind in Enum.GetValues<GlyphKind>())
        {
            var (point, mark) = script.MotifOf(kind);
            lines.Add($"{kind} family motif: {(point == GlyphFigure.Centre ? "c" : point.ToString())}:{mark}");
        }
        foreach (var glyph in GlyphRegistry.All) lines.Add($"  {glyph.Code}: {script.FigureOf(glyph.Code).Describe()}");
        return string.Join("\n", lines);
    }
}
