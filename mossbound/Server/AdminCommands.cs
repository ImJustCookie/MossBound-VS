using System;
using System.Collections.Generic;
using System.Linq;
using Mossbound.Core;
using Mossbound.Items;
using Mossbound.Network;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// `/mossbound inspect` and `/mossbound glyphs`, for players with the controlserver privilege.
static class AdminCommands
{
    public static void Register(ICoreServerAPI sapi, IServerNetworkChannel channel, Func<WorldScript?> script)
    {
        sapi.ChatCommands.Create("mossbound")
            .WithDescription(ModText.Get("command-mossbound"))
            .RequiresPrivilege(Privilege.controlserver)
            .BeginSubCommand("inspect")
                .WithDescription(ModText.Get("command-inspect"))
                .HandleWith(args => Inspect(args, script()))
            .EndSubCommand()
            .BeginSubCommand("glyphs")
                .WithDescription(ModText.Get("command-glyphs"))
                .HandleWith(args => SendAtlas(args, channel, script()))
            .EndSubCommand();
    }

    // The atlas reveals the world's secret script, so it goes to the admin who asked and nobody else.
    static TextCommandResult SendAtlas(TextCommandCallingArgs args, IServerNetworkChannel channel, WorldScript? script)
    {
        if (CheckCaller(args, script) is { } error) return error;
        var atlas = new GlyphAtlas();
        foreach (var glyph in GlyphRegistry.All)
        {
            var f = script!.FigureOf(glyph.Code);
            atlas.Entries.Add(new GlyphAtlasEntry { Code = glyph.Code, Lines = f.Lines, Marks = f.Marks });
        }
        channel.SendPacket(atlas, (IServerPlayer)args.Caller.Player);
        return TextCommandResult.Success();
    }

    // What casting the held medium would do, without casting it.
    static TextCommandResult Inspect(TextCommandCallingArgs args, WorldScript? script)
    {
        if (CheckCaller(args, script) is { } error) return error;
        string Say(string key, params object[] values) => ModText.In(args.LanguageCode, key, values);

        var stack = ((IServerPlayer)args.Caller.Player).InventoryManager.ActiveHotbarSlot?.Itemstack;
        if (stack?.Collectible is not ItemCircleMedium medium) return TextCommandResult.Error(Say("error-hold-medium"));
        var circle = ItemCircleMedium.ReadCircle(stack);
        if (circle == null) return TextCommandResult.Success(Say("inspect-blank", stack.GetName(), medium.Tier.Level));

        var lines = new List<string>
        {
            Say("inspect-header", stack.GetName(), medium.Tier.Level, circle.Glyphs.Count, circle.Links.Count, Say("source-" + Lower(circle.Source))),
        };
        if (!CircleValidation.FitsTier(circle, medium.Tier))
        {
            lines.Add(Say("inspect-out-of-bounds"));
            return TextCommandResult.Success(string.Join("\n", lines));
        }
        string SlotName(PlacedGlyph g) => Say("inspect-slot", Say("ring-" + Lower(g.Ring)), g.Slot);
        string GlyphName(PlacedGlyph g) => script!.Recognize(g.Figure) is { } def ? Say("glyph-" + def.Code) : Say("inspect-unknown-glyph");
        foreach (var g in circle.Glyphs) lines.Add(Say("inspect-glyph", SlotName(g), GlyphName(g), g.EffectiveSize));
        foreach (var l in circle.Links) lines.Add(Say("inspect-link", SlotName(circle.Glyphs[l.From]), SlotName(circle.Glyphs[l.To])));

        var plan = SpellReading.Read(circle, script!);
        lines.Add(plan.Fizzles
            ? Say("inspect-mishaps", string.Join(", ", plan.Mishaps.Select(m => Say("mishap-" + Lower(m)))))
            : Say("inspect-aspects", string.Join(", ", plan.Aspects.Select(a => Say("inspect-aspect", Say("aspect-" + Lower(a.aspect)), a.intensity)))));
        return TextCommandResult.Success(string.Join("\n", lines));
    }

    static TextCommandResult? CheckCaller(TextCommandCallingArgs args, WorldScript? script)
    {
        if (args.Caller.Player is not IServerPlayer) return TextCommandResult.Error(ModText.In(args.LanguageCode, "error-in-game-only"));
        if (script == null) return TextCommandResult.Error(ModText.In(args.LanguageCode, "error-script-not-ready"));
        return null;
    }

    static string Lower<T>(T value) where T : Enum => value.ToString().ToLowerInvariant();
}
