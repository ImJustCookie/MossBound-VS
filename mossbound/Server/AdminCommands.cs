using System;
using System.Collections.Generic;
using System.Linq;
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
            .BeginSubCommand("inspect")
                .WithDescription("Show how the server reads the circle on the medium in your hand (spoilers!)")
                .HandleWith(args => Inspect(args, script()))
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

    // Read-only: reports what casting the held medium would do, without casting it.
    static TextCommandResult Inspect(TextCommandCallingArgs args, WorldScript? script)
    {
        if (args.Caller.Player is not IServerPlayer player) return TextCommandResult.Error("Run this in game, not from the console.");
        if (script == null) return TextCommandResult.Error("Script not ready yet.");
        var stack = player.InventoryManager.ActiveHotbarSlot?.Itemstack;
        if (stack?.Collectible is not ItemCircleMedium medium) return TextCommandResult.Error("Hold a Mossbound medium first.");
        var circle = ItemCircleMedium.ReadCircle(stack);
        if (circle == null) return TextCommandResult.Success($"{stack.GetName()} (tier {medium.Tier.Level}): blank.");

        var lines = new List<string> { $"{stack.GetName()} (tier {medium.Tier.Level}): {circle.Glyphs.Count} glyphs, {circle.Links.Count} links" };
        if (!CircleParser.IsWithinBounds(circle, medium.Tier))
        {
            lines.Add("Out of bounds for this tier: casting does nothing.");
            return TextCommandResult.Success(string.Join("\n", lines));
        }

        string Slot(PlacedGlyph g) => $"{g.Ring} {g.Slot}";
        foreach (var g in circle.Glyphs) lines.Add($"  {Slot(g)}: {script.Recognize(g.Figure)?.Code ?? "(not a glyph)"}");
        foreach (var l in circle.Links) lines.Add($"  link {Slot(circle.Glyphs[l.From])} - {Slot(circle.Glyphs[l.To])}");

        var plan = CircleParser.Parse(circle, script);
        lines.Add(plan.Fizzles
            ? "Result: mishap (" + string.Join(", ", plan.Mishaps) + ")"
            : "Result: " + string.Join(", ", plan.Mix.Aspects.Select(a => $"{a.aspect} x{a.intensity}")));
        return TextCommandResult.Success(string.Join("\n", lines));
    }
}
