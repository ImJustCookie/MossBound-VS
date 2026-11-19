using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// Ward's bubble: absorbs up to its amount of damage, then pops; or fades after its duration. Recasting adds to it.
// Unlike the one-off effects it lasts, so it keeps state: who has a bubble, how much is left, its timers.
// It absorbs before the game handles the hit (a Harmony prefix on Entity.ReceiveDamage, wired in the mod
// system): a hit fully absorbed never happens, so no hurt flash, no knockback, no fleeing. The flag is synced to
// clients, which skip their own predicted hit too (an attacker's client flashes the target before the server answers).
sealed class ShieldBubbles : ISpellEffect
{
    const string Flag = "mossbound:bubble"; // in the entity's watched attributes: entities without one skip the lookup
    const float DefaultSeconds = 15;
    const int ShimmerMs = 500;

    sealed class Bubble
    {
        public required Entity Owner;
        public required SpellHit Hit;
        public float Left;
        public long ShimmerListener;
    }

    readonly ICoreServerAPI sapi;
    readonly Dictionary<long, Bubble> bubbles = new();

    public ShieldBubbles(ICoreServerAPI sapi) => this.sapi = sapi;

    public void Apply(SpellHit hit, float amount, float seconds)
    {
        if (hit.Entity is not { } owner) return;
        if (bubbles.TryGetValue(owner.EntityId, out var existing))
        {
            existing.Left += amount;
            return;
        }
        var bubble = new Bubble { Owner = owner, Hit = hit, Left = amount };
        bubble.ShimmerListener = sapi.Event.RegisterGameTickListener(_ => Shimmer(bubble), ShimmerMs);
        sapi.Event.RegisterCallback(_ => End(bubble), (int)(1000 * (seconds > 0 ? seconds : DefaultSeconds)));
        bubbles[owner.EntityId] = bubble;
        owner.WatchedAttributes.SetBool(Flag, true);
    }

    // The Harmony prefix: false skips the game's damage handling, when the bubble took the whole hit.
    public static bool BeforeReceiveDamage(Entity __instance, DamageSource damageSource, ref float damage, ref bool __result)
    {
        if (!__instance.WatchedAttributes.GetBool(Flag) || damageSource.Type == EnumDamageType.Heal) return true;
        if (__instance.World.Side == EnumAppSide.Client) return false; // the server decides; no predicted flash
        if (__instance.Api.ModLoader.GetModSystem<MossboundModSystem>().Shields is not { } shields) return true;
        damage = shields.Absorb(__instance, damage);
        if (damage > 0) return true;
        __result = false;
        return false;
    }

    // Returns what is left of the damage once the bubble has taken its share.
    float Absorb(Entity owner, float damage)
    {
        if (!bubbles.TryGetValue(owner.EntityId, out var bubble)) return damage;
        float absorbed = Math.Min(damage, bubble.Left);
        bubble.Left -= absorbed;
        var at = LastingEffect.Middle(owner);
        KitParticles.Show(sapi.World, bubble.Hit.Kit, at, 1, range: 16);
        if (bubble.Left <= 0) End(bubble);
        return damage - absorbed;
    }

    void Shimmer(Bubble bubble)
    {
        if (!bubble.Owner.Alive) End(bubble);
        else KitParticles.Burst(sapi.World, bubble.Hit.Kit, LastingEffect.Middle(bubble.Owner), 0);
    }

    void End(Bubble bubble)
    {
        if (!bubbles.Remove(bubble.Owner.EntityId)) return;
        bubble.Owner.WatchedAttributes.RemoveAttribute(Flag);
        sapi.Event.UnregisterGameTickListener(bubble.ShimmerListener);
        var at = LastingEffect.Middle(bubble.Owner);
        if (bubble.Hit.Kit.EndSound is string sound) sapi.World.PlaySoundAt(new AssetLocation(sound), at.X, at.Y, at.Z, null, true, 16);
        KitParticles.Burst(sapi.World, bubble.Hit.Kit, at, 2);
    }

}
