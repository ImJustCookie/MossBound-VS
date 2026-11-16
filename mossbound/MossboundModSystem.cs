using Mossbound.Gui;
using Mossbound.Script;
using Mossbound.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Mossbound;

// Wiring only: registrations shared by both sides, then each side's services.
public class MossboundModSystem : ModSystem
{
    public const string ChannelName = "mossbound";

    IClientNetworkChannel? clientChannel;

    // Server only: the world's secret glyph script, built from the seed once the save is loaded.
    public WorldScript? Script { get; private set; }
    internal SpellCaster? Caster { get; private set; }

    // Both sides, same order: item classes and network message types.
    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("mossbound.ItemCircleMedium", typeof(ItemCircleMedium));
        api.Network.RegisterChannel(ChannelName)
            .RegisterMessageType<InscribeRequest>()
            .RegisterMessageType<GlyphAtlas>();
    }

    // Authoritative game logic: validation, spell execution, costs.
    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.SaveGameLoaded += () => Script = new WorldScript(api.World.Seed);
        var channel = api.Network.GetChannel(ChannelName).SetMessageHandler<InscribeRequest>(new Inscription(api).OnRequest);
        Caster = new SpellCaster(api, () => Script);
        AdminCommands.Register(api, channel, () => Script);
    }

    // Presentation only: GUI and input. Never decides gameplay.
    public override void StartClientSide(ICoreClientAPI api)
    {
        clientChannel = api.Network.GetChannel(ChannelName)
            .SetMessageHandler<GlyphAtlas>(atlas => new GuiDialogGlyphAtlas(api, atlas).TryOpen());
    }

    public void SendInscribe(Circle circle) => clientChannel?.SendPacket(new InscribeRequest { Circle = circle });
}
