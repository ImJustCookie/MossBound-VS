using System;
using Mossbound.Core;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Mossbound.Server;

// An aspect's particles: each style moves its own way (speed, direction, gravity, life), so spells read apart even
// at a glance. Spawned on the server, one call per colour: every nearby client sees the same burst.
static class KitParticles
{
    public static void Burst(IWorldAccessor world, Kit kit, Vec3d at, int strength)
    {
        int quantity = 10 + 8 * strength;
        foreach (string hex in kit.Colours)
        {
            var particles = For(kit.Particles, at);
            particles.Color = ColorUtil.Hex2Int(hex) | unchecked((int)0xFF000000);
            particles.MinQuantity = quantity / kit.Colours.Count;
            particles.AddQuantity = particles.MinQuantity / 2;
            world.SpawnParticles(particles);
        }
    }

    // Position spread, then velocity (min, added), gravity, life, size and glow per style.
    static SimpleParticleProperties For(ParticleStyle style, Vec3d at) => style switch
    {
        ParticleStyle.Flame => Make(at, 0.4, new(-0.3f, 0.6f, -0.3f), new(0.6f, 0.9f, 0.6f), -0.05f, 0.6f, 0.2f, 0.5f, 200),
        ParticleStyle.Splash => Make(at, 0.3, new(-1.2f, 1.2f, -1.2f), new(2.4f, 1.6f, 2.4f), 1f, 1f, 0.15f, 0.35f, 0),
        ParticleStyle.Shards => Make(at, 0.2, new(-2.5f, 0.1f, -2.5f), new(5f, 0.6f, 5f), 0.3f, 0.4f, 0.15f, 0.3f, 80),
        ParticleStyle.Dust => Make(at, 0.5, new(-0.8f, 0.5f, -0.8f), new(1.6f, 1f, 1.6f), 1f, 1.2f, 0.4f, 0.8f, 0, cube: true),
        ParticleStyle.Motes => Make(at, 0.8, new(-0.15f, 0.2f, -0.15f), new(0.3f, 0.4f, 0.3f), -0.02f, 2.2f, 0.15f, 0.3f, 160),
        ParticleStyle.Ooze => Make(at, 0.5, new(-0.2f, 0.3f, -0.2f), new(0.4f, 0.3f, 0.4f), 0.5f, 1.6f, 0.25f, 0.5f, 0),
        ParticleStyle.Gust => Make(at, 0.3, new(-3f, 0f, -3f), new(6f, 0.3f, 6f), 0f, 0.5f, 0.1f, 0.25f, 0),
        ParticleStyle.Bubble => Make(at, 1.2, new(-0.05f, -0.05f, -0.05f), new(0.1f, 0.1f, 0.1f), 0f, 1.5f, 0.2f, 0.35f, 200),
        ParticleStyle.Steam => Make(at, 0.6, new(-0.2f, 0.4f, -0.2f), new(0.4f, 0.5f, 0.4f), -0.08f, 2.5f, 0.6f, 1.2f, 40),
        ParticleStyle.Rain => Make(at.AddCopy(0, 2, 0), 0.8, new(0f, -3f, 0f), new(0f, -1f, 0f), 1f, 0.8f, 0.1f, 0.2f, 0),
        ParticleStyle.Smoke => Make(at, 0.6, new(-0.15f, 0.2f, -0.15f), new(0.3f, 0.3f, 0.3f), -0.03f, 3f, 0.7f, 1.4f, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, null),
    };

    static SimpleParticleProperties Make(Vec3d at, double spread, Vec3f minVelocity, Vec3f addVelocity, float gravity,
        float life, float minSize, float maxSize, int glow, bool cube = false)
    {
        return new SimpleParticleProperties
        {
            MinPos = at.AddCopy(-spread, -spread / 2, -spread),
            AddPos = new Vec3d(spread * 2, spread, spread * 2),
            MinVelocity = minVelocity,
            AddVelocity = addVelocity,
            GravityEffect = gravity,
            LifeLength = life,
            MinSize = minSize,
            MaxSize = maxSize,
            VertexFlags = glow,
            ParticleModel = cube ? EnumParticleModel.Cube : EnumParticleModel.Quad,
            SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -maxSize * 0.5f),
        };
    }
}
