using System;
using System.Text;
using Mossbound.Client;
using Mossbound.Core;
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
    float castSeconds = 1;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        Tier = CircleTier.ForLevel(Attributes?["circleTier"].AsInt(1) ?? 1);
        castSeconds = Attributes?["castSeconds"].AsFloat(1) ?? 1;
    }

    // Charge time before release fires: `castSeconds` in the item JSON. Over ~9 s, lengthen the 300-frame cast
    // animations in patches/player-cast-animation.json (the game restarts a use animation once 95% done).
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Per-spell cast times will read the stack")]
    public float CastSecondsFor(ItemStack? stack) => castSeconds;

    // The rings and limits this stack's circle is written for.
    public virtual CircleTier TierOf(ItemStack? stack) => Tier;

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

    // Decided once per interaction: the canvas keeps right-click held, so re-checking sneak each tick turned an edit
    // into a cast. Set on each side when its interaction starts.
    const string EditingAttribute = "mossbound:editing";
    static bool InteractionIsEditing(EntityAgent byEntity) => byEntity.Attributes.GetBool(EditingAttribute);

    // From patches/player-cast-animation.json.
    const string HoldAnimation = "mossbound-hold";
    const string CastAnimation = "mossbound-cast";
    const string ReadyAnimation = "mossbound-ready";
    const string WriteAnimation = "mossbound-write";

    // Per spell later: clients can't read a circle's meaning, so the server would write a look code on the stack.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "Per-spell looks will read the stack")]
    public static CastStyle CastStyleFor(ItemStack? stack) => CastStyle.Default;

    public override string? GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand) =>
        hand == EnumHand.Right ? HoldAnimation : base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);

    // Played after a use and on switching items; vanilla's holds the arm further out, so it jumped between casts.
    public override string? GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand) =>
        hand == EnumHand.Right ? ReadyAnimation : base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand);

    // Played whenever right-click reads as held, even with the canvas open. The write pose is the hold pose under its
    // own name: sharing the idle pose's name made the game restart it (a twitch).
    public override string? GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity) =>
        forEntity is EntityAgent agent && IsCasting(agent) ? CastAnimation : WriteAnimation;

    // Controls on the holder's own client, server-synced controls for everyone else's view of them.
    static bool IsCasting(EntityAgent agent) => !InteractionIsEditing(agent)
        && (agent.Controls.HandUse == EnumHandInteract.HeldItemInteract || agent.ServerControls.HandUse == EnumHandInteract.HeldItemInteract);

    readonly HeldMediumMotion motion = new();

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        if (target != EnumItemRenderTarget.HandTp || renderinfo.Transform == null) return;
        if ((renderinfo.InSlot?.Inventory as InventoryBasePlayer)?.Player?.Entity is not { AnimManager: not null } holder) return;

        long now = capi.ElapsedMilliseconds;
        var cast = holder.AnimManager.GetAnimationState(CastAnimation);
        float seconds = CastSecondsFor(itemstack);
        float charge = Charge(cast, seconds);
        float dt = motion.Advance(holder, charge, now);
        var style = CastStyleFor(itemstack);
        bool casting = cast is { Active: true };
        if (casting) CastSparks.Charge(capi.World, holder, style, charge, dt);
        capi.ModLoader.GetModSystem<MossboundModSystem>().Glyphs?.Update(holder, itemstack, style, charge, casting, seconds);
        renderinfo.Transform = motion.Apply(renderinfo.Transform, holder, now);
    }

    // 0-1, scaled by the animation's blend so it falls smoothly on release or cancel.
    static float Charge(RunningAnimation? cast, float castSeconds)
    {
        if (cast is not { Running: true }) return 0;
        float played = cast.CurrentFrame / (30f * (cast.meta?.AnimationSpeed ?? 1));
        return Math.Clamp(played / castSeconds, 0, 1) * cast.EasingFactor;
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (!firstEvent) return;
        handling = EnumHandHandling.PreventDefault;

        bool editing = IsEditing(slot, byEntity);
        byEntity.Attributes.SetBool(EditingAttribute, editing);
        if (editing && byEntity.World.Api is ICoreClientAPI capi && byEntity == capi.World.Player.Entity) OpenEditor(capi, slot);
    }

    protected virtual void OpenEditor(ICoreClientAPI capi, ItemSlot slot) =>
        new GuiDialogCircleCanvas(capi, ReadCircle(slot.Itemstack), TierOf(slot.Itemstack)).TryOpen();

    // Fires on release once charged, like a bow: firing at full charge emptied the hand mid-hold, and the game
    // then played its empty-hand reach animation.
    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel) =>
        !InteractionIsEditing(byEntity);

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        if (secondsUsed < CastSecondsFor(slot.Itemstack) || InteractionIsEditing(byEntity)) return;
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
