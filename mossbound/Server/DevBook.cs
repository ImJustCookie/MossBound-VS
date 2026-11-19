using System;
using System.Linq;
using Mossbound.Core;
using Mossbound.Items;
using Mossbound.Network;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// The dev book: writes circles from named glyphs, for testing spells without drawing them. It reveals the world's
// script, so only a creative-mode player with server control, holding the book, can use it. A written circle passes
// the same checks as inscribing by hand.
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
        if (MediumOf(tier) is not { } medium || msg.Circle is not { Glyphs.Count: > 0 } circle
            || !CircleValidation.FitsTier(circle, tier) || !CircleValidation.LinksFollowTheGrammar(circle, tier))
        {
            sapi.Logger.Warning("[mossbound] rejected malformed dev book circle from {0}", player.PlayerName);
            return;
        }
        var stack = new ItemStack(medium);
        ItemCircleMedium.WriteCircle(stack, circle);
        if (!player.InventoryManager.TryGiveItemstack(stack, true)) sapi.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ);
        player.Tell("devbook-written", stack.GetName());
    }

    static bool MayUse(IServerPlayer player) => player.WorldData.CurrentGameMode == EnumGameMode.Creative
        && player.HasPrivilege(Privilege.controlserver)
        && player.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible is ItemDevBook;

    Item? MediumOf(CircleTier tier) => sapi.World.Items.FirstOrDefault(item => item is ItemCircleMedium medium && medium.Tier == tier);
}
