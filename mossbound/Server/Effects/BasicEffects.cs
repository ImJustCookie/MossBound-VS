using Vintagestory.API.Common;

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

sealed class Push : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var target = hit.Harmable;
        var from = hit.Caster.Entity.Pos.XYZ;
        if (target == null || target.Pos.XYZ.SquareDistanceTo(from) < 0.01) return; // a zero vector normalises to NaN
        var away = target.Pos.XYZ.Sub(from).Normalize();
        target.Pos.Motion.Add(away.X * 0.15 * amount, 0.08 * amount, away.Z * 0.15 * amount);
    }
}
