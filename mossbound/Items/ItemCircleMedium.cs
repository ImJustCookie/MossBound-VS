using System;
using System.Text;
using Mossbound.Client;
using Mossbound.Core;
using Mossbound.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Mossbound.Items;

// Anything a circle can be written on: moss, bark and parchment sigils, later stone. The item
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
    // hold right-click on an inscribed medium to charge it, release to cast.
    static bool IsEditing(ItemSlot slot, EntityAgent byEntity) =>
        slot.Itemstack?.Attributes.HasAttribute(CircleAttribute) != true || byEntity.Controls.ShiftKey;

    // Player animations added by patches/player-cast-animation.json. Every client picks them from the
    // holder's synced held item and controls, so other players see them too.
    const string HoldAnimation = "mossbound-hold";
    const string CastAnimation = "mossbound-cast";
    const string ReadyAnimation = "mossbound-ready";

    // The particle look of a cast (Items/CastVisuals.cs), used by every client while charging and by the server
    // on release. One look for now. Per spell: have the server write the look's code on the stack when inscribing
    // (clients can't read a circle's meaning without the world's secret script) and map it to a CastStyle here.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Per-spell looks will read the stack")]
    public static CastStyle CastStyleFor(ItemStack? stack) => CastStyle.Default;

    public override string? GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand) =>
        hand == EnumHand.Right ? HoldAnimation : base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);

    // The game plays the "ready" pose after a use ends and on switching items; vanilla's holds the arm
    // further out, so the arm would jump there between casts. Ours is the hold pose under its own code.
    public override string? GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand) =>
        hand == EnumHand.Right ? ReadyAnimation : base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand);

    // Writing keeps the hold pose (the game stops the idle pose during any use, so it must be named here too).
    public override string? GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity) =>
        forEntity is EntityAgent agent && IsEditing(activeHotbarSlot, agent) ? HoldAnimation : CastAnimation;

    readonly HeldMediumMotion motion = new();

    // Each frame the medium is drawn in a hand: move it (spin, bob, lift) and, while charging, spawn its particles.
    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        if (target != EnumItemRenderTarget.HandTp || renderinfo.Transform == null) return;
        if ((renderinfo.InSlot?.Inventory as InventoryBasePlayer)?.Player?.Entity is not { AnimManager: not null } holder) return;

        long now = capi.ElapsedMilliseconds;
        var cast = holder.AnimManager.GetAnimationState(CastAnimation);
        float charge = Charge(cast);
        float dt = motion.Advance(holder, charge, now);
        if (cast is { Active: true }) CastVisuals.Charge(capi.World, holder, CastStyleFor(itemstack), charge, dt);
        renderinfo.Transform = motion.Apply(renderinfo.Transform, holder, now);
    }

    // 0-1: how far the cast animation is into its first second, scaled by how much it is blended in
    // (so it falls smoothly when the cast is released or cancelled).
    static float Charge(RunningAnimation? cast) =>
        cast is { Running: true } ? Math.Clamp(cast.CurrentFrame / 29f, 0, 1) * cast.EasingFactor : 0;

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

    // Charge while held; the spell fires on release (Stop) once charged, like drawing a bow. Firing on release
    // keeps the medium in hand while the button is down: firing at full charge emptied the hand mid-hold, and
    // the game then played its empty-hand reach animation. Releasing early cancels.
    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) =>
        !IsEditing(slot, byEntity);

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
