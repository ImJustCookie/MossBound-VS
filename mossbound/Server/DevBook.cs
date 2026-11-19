using System;
using Mossbound.Core;
using Mossbound.Items;
using Mossbound.Network;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// The dev book: writes circles from named glyphs on itself, for testing spells without drawing them. It reveals the
// world's script, so only a creative-mode player with server control, holding the book, can use it. A written circle
// passes the same checks as inscribing by hand.
class DevBook(ICoreServerAPI sapi, IServerNetworkChannel channel, Func<WorldScript?> script)
{
    public void OnOpen(IServerPlayer player, DevBookOpen _)
    {
        if (!MayUse(player) || script() is not { } world) return;
        var atlas = GlyphAtlas.Of(world);
        atlas.ForDevBook = true;
        channel.SendPacket(atlas, player);
    }

    public void OnWrite(IServerPlayer player, DevBookWrite msg)
    {
        if (!MayUse(player)) return;
        var tier = CircleTier.ForLevel(msg.TierLevel);
        if (msg.Circle is not { } circle || !CircleValidation.IsWritable(circle, tier))
        {
            sapi.Logger.Warning("[mossbound] rejected malformed dev book circle from {0}", player.PlayerName);
            return;
        }
        var slot = player.InventoryManager.ActiveHotbarSlot;
        var book = slot.Itemstack!; // MayUse checked it is the book
        ItemCircleMedium.WriteCircle(book, circle);
        ItemDevBook.WriteTier(book, tier);
        slot.MarkDirty();
        player.Tell("inscribe-done");
    }

    static bool MayUse(IServerPlayer player) => player.WorldData.CurrentGameMode == EnumGameMode.Creative
        && player.HasPrivilege(Privilege.controlserver)
        && player.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible is ItemDevBook;
}
