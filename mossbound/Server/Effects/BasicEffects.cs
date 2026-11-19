using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Mossbound.Server;

sealed class Damage(EnumDamageType type) : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds) => hit.Harmable?.ReceiveDamage(new DamageSource
    {
        Source = EnumDamageSource.Player,
        SourceEntity = hit.Caster.Entity,
        CauseEntity = hit.Caster.Entity,
        Type = type,
    }, amount);
}

// No source entity: the game knocks back even on heals, from the source, and healing yourself would push along
// (you - you).Normalize() = NaN, which crashes the client.
sealed class Heal : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds) =>
        hit.Beneficiary.ReceiveDamage(new DamageSource { Source = EnumDamageSource.Internal, Type = EnumDamageType.Heal }, amount);
}

// Pushes away from the caster; cast on yourself, it throws you where you look.
sealed class Push : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        if (hit.Harmable is not { } target) return;
        var caster = hit.Caster.Entity;
        var away = target == caster ? caster.Pos.GetViewVector().ToVec3d() : target.Pos.XYZ.Sub(caster.Pos.XYZ);
        away.Y = 0;
        if (away.Length() < 0.01) return; // looking straight up or down, or no gap: no direction to push
        away.Normalize();
        Knockback.Give(target, away.X * 0.15 * amount, 0.08 * amount, away.Z * 0.15 * amount);
    }
}

// Motion that reaches players too: a player's movement runs on their own client, which ignores motion set on the
// server. Vanilla's shockwave does it this way: the knockback goes in watched attributes and a tiny hurt (no red
// flash) tells whichever side moves the entity to apply it.
static class Knockback
{
    public static void Give(Entity target, double x, double y, double z)
    {
        var attributes = target.WatchedAttributes;
        attributes.SetDouble("kbdirX", x);
        attributes.SetDouble("kbdirY", y);
        attributes.SetDouble("kbdirZ", z);
        attributes.SetInt("onHurtCounter", attributes.GetInt("onHurtCounter") + 1);
        attributes.SetFloat("onHurt", 0.01f);
    }
}
