using Vintagestory.API.Server;
using Mossbound.Network;
using Mossbound.Core;
using Mossbound.Items;

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
        if (msg.Circle == null || msg.Circle.Glyphs.Count == 0 || !CircleValidation.FitsTier(msg.Circle, medium.Tier))
        {
            sapi.Logger.Warning("[mossbound] rejected malformed inscription from {0}", player.PlayerName);
            return;
        }

        // Inscribe one medium; the rest of the stack stays blank.
        var inscribed = slot.TakeOut(1);
        ItemCircleMedium.WriteCircle(inscribed, msg.Circle);
        if (slot.Empty) slot.Itemstack = inscribed;
        else if (!player.InventoryManager.TryGiveItemstack(inscribed, true)) sapi.World.SpawnItemEntity(inscribed, player.Entity.Pos.XYZ);
        slot.MarkDirty();

        player.Tell("inscribe-done");
    }
}
