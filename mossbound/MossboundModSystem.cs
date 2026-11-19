using HarmonyLib;
using Mossbound.Client;
using Mossbound.Core;
using Mossbound.Items;
using Mossbound.Network;
using Mossbound.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Mossbound;

// Wiring only: registrations shared by both sides, then each side's services.
public class MossboundModSystem : ModSystem
{
    public const string ChannelName = "mossbound";

    IClientNetworkChannel? clientChannel;

    // Client only: glyph particles around casters (disposed with the mod).
    internal GlyphRings? Glyphs { get; private set; }

    // Server only: the world's secret glyph script, built from the seed once the save is loaded.
    public WorldScript? Script { get; private set; }
    internal SpellCaster? Caster { get; private set; }
    internal ShieldBubbles? Shields { get; private set; }
    Harmony? harmony;

    // Both sides, same order: item classes and network message types.
    public override void Start(ICoreAPI api)
    {
        PatchGame();
        api.RegisterItemClass("mossbound.ItemCircleMedium", typeof(ItemCircleMedium));
        api.RegisterItemClass("mossbound.ItemDevBook", typeof(ItemDevBook));
        api.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<InscribeRequest>()
            .RegisterMessageType<GlyphAtlas>()
            .RegisterMessageType<DevBookOpen>()
            .RegisterMessageType<DevBookWrite>();
    }

    // Authoritative game logic: validation, spell execution, costs.
    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.SaveGameLoaded += () => Script = new WorldScript(api.World.Seed);
        var channel = api.Network.GetChannel(ChannelName).SetMessageHandler<InscribeRequest>(new Inscription(api).OnRequest);
        Shields = new ShieldBubbles(api);
        Caster = new SpellCaster(api, () => Script, Shields);
        AdminCommands.Register(api, channel, () => Script);
        var devBook = new DevBook(api, channel, () => Script);
        channel.SetMessageHandler<DevBookOpen>(devBook.OnOpen).SetMessageHandler<DevBookWrite>(devBook.OnWrite);
    }

    // Presentation only: GUI and input. Never decides gameplay.
    public override void StartClientSide(ICoreClientAPI api)
    {
        clientChannel = api.Network.GetChannel(ChannelName)
            .SetMessageHandler<GlyphAtlas>(atlas => (atlas.ForDevBook ? new GuiDialogDevBook(api, atlas, CircleTier.Parchment, null) : (GuiDialog)new GuiDialogGlyphAtlas(api, atlas)).TryOpen());
        Glyphs = new GlyphRings(api);
    }

    // The mod's only Harmony patches: the Ward bubble cancels absorbed hits, and slows scale walking. Applied once per
    // process (singleplayer starts this system twice), on both sides: clients predict hits and move their own player.
    void PatchGame()
    {
        if (Harmony.HasAnyPatches(Mod.Info.ModID)) return;
        harmony = new Harmony(Mod.Info.ModID);
        harmony.Patch(AccessTools.Method(typeof(Entity), nameof(Entity.ReceiveDamage)),
            prefix: new HarmonyMethod(typeof(ShieldBubbles), nameof(ShieldBubbles.BeforeReceiveDamage)));
        harmony.Patch(AccessTools.Method(typeof(EntityAgent), nameof(EntityAgent.GetWalkSpeedMultiplier)),
            postfix: new HarmonyMethod(typeof(Slows), nameof(Slows.AfterWalkSpeed)));
    }

    public override void Dispose()
    {
        Glyphs?.Dispose();
        harmony?.UnpatchAll(Mod.Info.ModID);
    }

    public void Send<T>(T message) => clientChannel?.SendPacket(message);
}
