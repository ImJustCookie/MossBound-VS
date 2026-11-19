using Mossbound.Core;
using Mossbound.Items;
using Mossbound.Network;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// Server side of inscribing: validates the client's drawing against the held medium's tier and writes it on it.
class Inscription
{
    readonly ICoreServerAPI sapi;

    public Inscription(ICoreServerAPI sapi) => this.sapi = sapi;

    public void OnRequest(IServerPlayer player, InscribeRequest msg)
    {
        var slot = player.InventoryManager.ActiveHotbarSlot;
        if (slot?.Itemstack?.Collectible is not ItemCircleMedium medium)
        {
            player.Tell("inscribe-no-medium");
            return;
        }
        var tier = medium.TierOf(slot.Itemstack);
        if (msg.Circle is not { Glyphs.Count: > 0 } circle || !CircleValidation.FitsTier(circle, tier)
            || !CircleValidation.LinksFollowTheGrammar(circle, tier))
        {
            sapi.Logger.Warning("[mossbound] rejected malformed inscription from {0}", player.PlayerName);
            return;
        }

        // Inscribe one medium; the rest of the stack stays blank.
        var inscribed = slot.TakeOut(1);
        ItemCircleMedium.WriteCircle(inscribed, circle);
        if (slot.Empty) slot.Itemstack = inscribed;
        else if (!player.InventoryManager.TryGiveItemstack(inscribed, true)) sapi.World.SpawnItemEntity(inscribed, player.Entity.Pos.XYZ);
        slot.MarkDirty();

        player.Tell("inscribe-done");
    }
}
