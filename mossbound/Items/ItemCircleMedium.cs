using System.Text;
using Mossbound.Client;
using Mossbound.Core;
using Mossbound.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Mossbound.Items;

// Anything a circle can be written on: moss sigil, bark strip, later parchment and stone. The item
// JSON sets `circleTier` (1–4), which decides the rings and slots available. The circle itself is
// stored in the stack attributes (written only by the server, synced to the owner's client).
public class ItemCircleMedium : Item
{
    public const string CircleAttribute = "mossbound:circle";

    public CircleTier Tier { get; private set; } = CircleTier.Moss;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        Tier = CircleTier.ForLevel(Attributes?["circleTier"].AsInt(1) ?? 1);
    }

    public static Circle? ReadCircle(ItemStack? stack)
    {
        byte[]? bytes = stack?.Attributes.GetBytes(CircleAttribute);
        return bytes == null ? null : SerializerUtil.Deserialize<Circle>(bytes);
    }

    public static void WriteCircle(ItemStack stack, Circle circle) =>
        stack.Attributes.SetBytes(CircleAttribute, SerializerUtil.Serialize(circle));

    // Right-click a blank medium (or sneak + right-click an inscribed one) to open the canvas;
    // hold right-click on an inscribed medium to cast it.
    static bool IsEditing(ItemSlot slot, EntityAgent byEntity) =>
        slot.Itemstack?.Attributes.HasAttribute(CircleAttribute) != true || byEntity.Controls.ShiftKey;

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (!firstEvent) return;
        handling = EnumHandHandling.PreventDefault;

        if (IsEditing(slot, byEntity) && byEntity.World.Api is ICoreClientAPI capi && byEntity == capi.World.Player.Entity)
        {
            new GuiDialogCircleCanvas(capi, ReadCircle(slot.Itemstack), Tier).TryOpen();
        }
    }

    // Keep holding until the cast time is reached; returning false ends the hold and calls Stop.
    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) =>
        !IsEditing(slot, byEntity) && secondsUsed < SpellCaster.CastSeconds;

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        if (secondsUsed < SpellCaster.CastSeconds || IsEditing(slot, byEntity)) return;
        if (byEntity.World.Api is ICoreServerAPI sapi && byEntity is EntityPlayer { Player: IServerPlayer player })
        {
            sapi.ModLoader.GetModSystem<MossboundModSystem>().Caster?.Cast(player, slot, entitySel, blockSel);
        }
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        var circle = ReadCircle(inSlot.Itemstack);
        dsc.AppendLine(circle == null
            ? ModText.Get("medium-blank")
            : ModText.Get("medium-inscribed", circle.Glyphs.Count));
    }
}
