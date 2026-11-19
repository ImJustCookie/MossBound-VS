using System;
using System.Collections.Generic;
using System.Linq;
using Mossbound.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// What a spell does where it lands: its aspect's particles and sound, then its reactions with what it finds on the
// target (docs/spell-system.md), then each effect of its payload, scaled by the spell's strength.
class SpellEffects
{
    readonly ICoreServerAPI sapi;
    readonly PersonalityTable personalities;
    readonly Dictionary<EffectKind, ISpellEffect> effects;
    readonly Afflict wet, chill;
    readonly Slows slow, root;

    public SpellEffects(ICoreServerAPI sapi, ShieldBubbles shields)
    {
        this.sapi = sapi;
        personalities = Personalities.Load(sapi);
        wet = new Afflict(sapi, Condition.Wet, 10);
        chill = new Afflict(sapi, Condition.Chilled, 8);
        slow = Slows.Slow(sapi);
        root = Slows.Root(sapi);
        effects = new()
        {
            [EffectKind.Burn] = new Damage(EnumDamageType.Fire),
            [EffectKind.Freeze] = new Damage(EnumDamageType.Frost),
            [EffectKind.Bash] = new Damage(EnumDamageType.BluntAttack),
            [EffectKind.Heal] = new Heal(),
            [EffectKind.Push] = new Push(),
            [EffectKind.Ignite] = new Ignite(),
            [EffectKind.Extinguish] = new Extinguish(),
            [EffectKind.Slow] = slow,
            [EffectKind.Shield] = shields,
            [EffectKind.Soak] = wet,
            [EffectKind.Chill] = chill,
            [EffectKind.FreezeWater] = new FreezeWater(),
            [EffectKind.Root] = root,
            [EffectKind.Regrow] = new Regrow(),
            [EffectKind.Cure] = new Cure(),
            [EffectKind.PoisonOverTime] = new OverTime(EnumDamageType.Poison, EnumDamageOverTimeEffectType.Poison, harmful: true),
            [EffectKind.HealOverTime] = new OverTime(EnumDamageType.Heal, EnumDamageOverTimeEffectType.Unknown, harmful: false),
            [EffectKind.Wither] = new Wither(),
            [EffectKind.Lift] = new Lift(),
            [EffectKind.Scatter] = new Scatter(),
            [EffectKind.Calm] = new Calms(sapi),
        };
        var missing = Enum.GetValues<EffectKind>().Where(kind => !effects.ContainsKey(kind)).ToList();
        if (missing.Count > 0) throw new InvalidOperationException("[mossbound] effects with no class: " + string.Join(", ", missing));
    }

    public void Apply(IServerPlayer caster, SpellTarget target, SpellCell cell)
    {
        if (!personalities.Aspects.TryGetValue(cell.Aspect, out var personality)) return;
        var hit = new SpellHit(sapi, caster, target.Entity, target.Position, personality.Kit);
        Show(personality.Kit, target.Position, cell.Strength);

        var reactions = target.Entity is { } entity ? Reactions.For(Conditions.On(entity), cell.Aspect) : new List<Reaction>();
        foreach (var reaction in reactions) React(reaction, hit, target.Entity!, cell.Strength);
        if (reactions.Any(Reactions.ReplacesPayload)) return;
        foreach (var step in personality.Payload) effects[step.Effect].Apply(hit, step.Amount * cell.Strength * Jitter(), step.Seconds * Jitter());
    }

    // Each effect lands a little stronger or weaker, and lasts a little longer or shorter, so no two casts are alike.
    float Jitter() => 0.85f + 0.3f * (float)sapi.World.Rand.NextDouble();

    void React(Reaction reaction, SpellHit hit, Entity target, int strength)
    {
        switch (reaction)
        {
            case Reaction.Frozen:
                wet.End(target);
                root.Apply(hit with { Kit = KitOf(Aspect.Frost) }, 1, 2f * strength);
                Show(KitOf(Aspect.Frost), hit.Position, strength + 1);
                break;
            case Reaction.SteamBurst:
                wet.End(target);
                Show(KitOf(Aspect.Steam), hit.Position, strength + 1);
                effects[EffectKind.Burn].Apply(hit with { Kit = KitOf(Aspect.Steam) }, strength, 0);
                break;
            case Reaction.Doused:
                Show(KitOf(Aspect.Steam), hit.Position, 1);
                break;
            case Reaction.Fanned:
                effects[EffectKind.Ignite].Apply(hit with { Entity = null, Position = target.Pos.XYZ }, strength, 0);
                break;
            case Reaction.Thawed:
                chill.End(target);
                slow.End(target);
                break;
        }
    }

    Kit KitOf(Aspect aspect) => personalities.Aspects.TryGetValue(aspect, out var p) ? p.Kit : personalities.Mishap;

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
