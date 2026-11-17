using System.Collections.Generic;
using System.Linq;
using Mossbound.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// What each aspect does when it lands. Test scale for v1: every aspect shows its colour, and Life,
// Ember and Wind have a real effect. Spells never hurt or push players (no PvP griefing).
static class SpellEffects
{
    static readonly Dictionary<Aspect, int> colours = new()
    {
        [Aspect.Water] = ColorUtil.ToRgba(255, 60, 120, 220),
        [Aspect.Ember] = ColorUtil.ToRgba(255, 240, 120, 40),
        [Aspect.Frost] = ColorUtil.ToRgba(255, 200, 235, 255),
        [Aspect.Stone] = ColorUtil.ToRgba(255, 130, 120, 110),
        [Aspect.Life] = ColorUtil.ToRgba(255, 90, 200, 80),
        [Aspect.Rot] = ColorUtil.ToRgba(255, 110, 90, 50),
        [Aspect.Wind] = ColorUtil.ToRgba(255, 225, 225, 210),
        [Aspect.Ward] = ColorUtil.ToRgba(255, 220, 200, 120),
        [Aspect.Moss] = ColorUtil.ToRgba(255, 70, 140, 50),
    };
    static readonly int compoundColour = ColorUtil.ToRgba(255, 170, 140, 210);
    static readonly int smokeColour = ColorUtil.ToRgba(180, 70, 70, 70);

    public static void Apply(ICoreServerAPI sapi, IServerPlayer caster, SpellTarget target, Aspect aspect, int intensity)
    {
        Burst(sapi, target.Position, colours.GetValueOrDefault(aspect, compoundColour), 10 + 10 * intensity);

        var creature = target.Entity is EntityPlayer ? null : target.Entity;
        switch (aspect)
        {
            case Aspect.Life:
                (target.Entity ?? caster.Entity).ReceiveDamage(Source(caster, EnumDamageType.Heal), 2 * intensity);
                break;
            case Aspect.Ember:
                creature?.ReceiveDamage(Source(caster, EnumDamageType.Fire), 2 * intensity);
                break;
            case Aspect.Wind when creature != null && creature.Pos.XYZ.SquareDistanceTo(caster.Entity.Pos.XYZ) > 0.01:
                var away = creature.Pos.XYZ.Sub(caster.Entity.Pos.XYZ).Normalize(); // guarded: a zero vector normalises to NaN
                creature.Pos.Motion.Add(away.X * 0.3 * intensity, 0.15 * intensity, away.Z * 0.3 * intensity);
                break;
        }
    }

    // Opposite or overloaded elements backfire a little; a circle with no usable element just fizzles.
    public static void ApplyMishap(ICoreServerAPI sapi, IServerPlayer caster, IReadOnlyList<Mishap> mishaps)
    {
        var at = caster.Entity.Pos.XYZ.Add(0, 1, 0);
        Burst(sapi, at, smokeColour, 25);
        if (mishaps.Contains(Mishap.Opposition) || mishaps.Contains(Mishap.Overload))
        {
            caster.Entity.ReceiveDamage(new DamageSource { Source = EnumDamageSource.Internal, Type = EnumDamageType.Injury }, 1);
            caster.Tell("cast-backfire");
        }
        else
        {
            caster.Tell("cast-fizzle");
        }
    }

    // Heals carry no source entity: the game applies knockback even to heals, from the source's
    // position, and healing yourself makes that direction (you - you).Normalize() = NaN, which
    // crashes the client.
    static DamageSource Source(IServerPlayer caster, EnumDamageType type) => type == EnumDamageType.Heal
        ? new() { Source = EnumDamageSource.Internal, Type = type }
        : new() { Source = EnumDamageSource.Player, SourceEntity = caster.Entity, CauseEntity = caster.Entity, Type = type };

    // Spawned server-side with no "except player": every nearby client sees it.
    static void Burst(ICoreServerAPI sapi, Vec3d at, int colour, int quantity) =>
        sapi.World.SpawnParticles(new SimpleParticleProperties
        {
            MinQuantity = quantity,
            AddQuantity = quantity / 2f,
            Color = colour,
            MinPos = at.AddCopy(-0.5, -0.5, -0.5),
            AddPos = new Vec3d(1, 1, 1),
            MinVelocity = new Vec3f(-0.4f, 0.2f, -0.4f),
            AddVelocity = new Vec3f(0.8f, 0.6f, 0.8f),
            LifeLength = 1.2f,
            GravityEffect = -0.03f,
            MinSize = 0.3f,
            MaxSize = 0.7f,
            ParticleModel = EnumParticleModel.Quad,
        });
}
