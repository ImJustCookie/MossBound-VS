using Mossbound.Client;
using Mossbound.Core;
using Mossbound.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Mossbound.Items;

// Creative-only dev medium: held and cast like any sigil, but written from named glyphs on any tier, which it keeps on
// the stack. Editing asks the server to open the ledger (Server/DevBook.cs checks who may).
public class ItemDevBook : ItemCircleMedium
{
    const string TierAttribute = "mossbound:tier";

    public override CircleTier TierOf(ItemStack? stack) => CircleTier.ForLevel(stack?.Attributes.GetInt(TierAttribute, CircleTier.Parchment.Level) ?? CircleTier.Parchment.Level);

    public static void WriteTier(ItemStack stack, CircleTier tier) => stack.Attributes.SetInt(TierAttribute, tier.Level);

    protected override void OpenEditor(ICoreClientAPI capi, ItemSlot slot) =>
        capi.ModLoader.GetModSystem<MossboundModSystem>().Send(new DevBookOpen());

    // The server's answer: the world's glyphs, for the ledger on the book in hand.
    public static void OpenLedger(ICoreClientAPI capi, GlyphAtlas atlas)
    {
        var stack = capi.World.Player.InventoryManager.ActiveHotbarSlot?.Itemstack;
        if (stack?.Collectible is ItemDevBook book) new GuiDialogDevBook(capi, atlas, book.TierOf(stack), ReadCircle(stack)).TryOpen();
    }
}
