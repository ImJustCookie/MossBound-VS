using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Mossbound.Server;

// Damage or healing spread over time, through the game's own damage-over-time (shown, saved, curable).
sealed class OverTime(EnumDamageType type, EnumDamageOverTimeEffectType kind, bool harmful) : ISpellEffect
{
    const float DefaultSeconds = 6;

    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var target = harmful ? hit.Harmable : hit.Beneficiary;
        float duration = seconds > 0 ? seconds : DefaultSeconds;
        target?.GetBehavior<EntityBehaviorHealth>()?.ApplyDoTEffect(
            harmful ? EnumDamageSource.Player : EnumDamageSource.Internal, type, 0, amount, TimeSpan.FromSeconds(duration), (int)Math.Ceiling(duration), kind);
    }
}

sealed class Cure : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds) =>
        hit.Beneficiary.GetBehavior<EntityBehaviorHealth>()?.StopDoTEffect((int)EnumDamageOverTimeEffectType.Poison);
}

// Thrown upward, less the bigger it is (its hitbox against a player's): a chicken flies, a bear barely hops.
sealed class Lift : ISpellEffect
{
    const double PlayerVolume = 0.6 * 0.6 * 1.85;

    public void Apply(SpellHit hit, float amount, float seconds)
    {
        if (hit.Harmable is not { } target) return;
        var box = target.SelectionBox;
        double size = Math.Clamp(box.XSize * box.YSize * box.ZSize / PlayerVolume, 0.5, 4);
        Knockback.Give(target, 0, 0.15 * amount / size, 0);
    }
}

// Dropped items around the spot are thrown up and outward.
sealed class Scatter : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var rand = hit.Sapi.World.Rand;
        foreach (var item in hit.Sapi.World.GetEntitiesAround(hit.Position, 2 + amount, 2, e => e is EntityItem))
            item.Pos.Motion.Add((rand.NextDouble() - 0.5) * 0.4 * amount, 0.15 * amount, (rand.NextDouble() - 0.5) * 0.4 * amount);
    }
}
