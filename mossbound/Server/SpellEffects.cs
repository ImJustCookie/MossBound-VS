using System;
using System.Collections.Generic;
using System.Linq;
using Mossbound.Core;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// What a spell does where it lands: its aspect's particles and sound, then each effect of its payload, scaled by
// the spell's strength. Each aspect's look and payload come from its personality.
class SpellEffects
{
    readonly ICoreServerAPI sapi;
    readonly PersonalityTable personalities;
    readonly Dictionary<EffectKind, ISpellEffect> effects;

    public SpellEffects(ICoreServerAPI sapi, ShieldBubbles shields)
    {
        this.sapi = sapi;
        personalities = Personalities.Load(sapi);
        effects = new()
        {
            [EffectKind.Burn] = new Damage(EnumDamageType.Fire),
            [EffectKind.Freeze] = new Damage(EnumDamageType.Frost),
            [EffectKind.Bash] = new Damage(EnumDamageType.BluntAttack),
            [EffectKind.Poison] = new Damage(EnumDamageType.Poison),
            [EffectKind.Heal] = new Heal(),
            [EffectKind.Push] = new Push(),
            [EffectKind.Ignite] = new Ignite(),
            [EffectKind.Extinguish] = new Extinguish(),
            [EffectKind.Slow] = new Slows(sapi),
            [EffectKind.Shield] = shields,
        };
        var missing = Enum.GetValues<EffectKind>().Where(kind => !effects.ContainsKey(kind)).ToList();
        if (missing.Count > 0) throw new InvalidOperationException("[mossbound] effects with no class: " + string.Join(", ", missing));
    }

    public void Apply(IServerPlayer caster, SpellTarget target, SpellCell cell)
    {
        if (!personalities.Aspects.TryGetValue(cell.Aspect, out var personality)) return;
        var hit = new SpellHit(sapi, caster, target.Entity, target.Position, personality.Kit);
        Show(personality.Kit, target.Position, cell.Strength);
        foreach (var step in personality.Payload) effects[step.Effect].Apply(hit, step.Amount * cell.Strength, step.Seconds);
    }

    public void ApplyMishap(IServerPlayer caster, IReadOnlyList<Mishap> mishaps, SpellLimits limits)
    {
        Show(personalities.Mishap, caster.Entity.Pos.XYZ.Add(0, 1, 0), 2);
        if (mishaps.Any(m => m.Backfires()))
        {
            caster.Entity.ReceiveDamage(new DamageSource { Source = EnumDamageSource.Internal, Type = EnumDamageType.Injury }, 1);
            caster.Tell("cast-backfire");
        }
        else
        {
            caster.Tell("cast-fizzle");
        }
        // A hint per mishap, so players learn the grammar. It never names a glyph: the script stays secret.
        foreach (var mishap in mishaps)
            caster.Tell("mishap-hint-" + mishap.ToString().ToLowerInvariant(), limits.Strength, limits.Scale, limits.Spells);
    }

    void Show(Kit kit, Vec3d at, int strength)
    {
        KitParticles.Burst(sapi.World, kit, at, strength);
        sapi.World.PlaySoundAt(new AssetLocation(kit.Sound), at.X, at.Y, at.Z, null, true, 24);
    }
}
