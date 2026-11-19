using System;
using Mossbound.Core;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Mossbound.Server;

// An aspect's particles: each style moves its own way (speed, direction, gravity, life), so spells read apart even
// at a glance. Bursts are spawned on the server, one call per colour, so every nearby client sees the same burst;
// trails are spawned by each client where it draws what leaves them (EntitySeed).
static class KitParticles
{
    // A burst with the aspect's sound: how a spell shows where it lands.
    public static void Show(IWorldAccessor world, Kit kit, Vec3d at, int strength, float range = 24)
    {
        Burst(world, kit, at, strength);
        world.PlaySoundAt(new AssetLocation(kit.Sound), at.X, at.Y, at.Z, null, true, range);
    }

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

    // One puff of a meteor trail along the path of something in flight: the aspect's own particles, left hanging where
    // they fall (no rising or falling), drifting slowly outward, growing and fading, so the trail widens and dissipates
    // behind it. Heavy styles (dust, ooze) keep their weight and rain down from the path. Airy styles (gusts, steam,
    // smoke) have no solid body to show, so their trail is thicker and wider.
    public static void Trail(IWorldAccessor world, Kit kit, Vec3d at)
    {
        var rand = world.Rand;
        bool airy = kit.Particles is ParticleStyle.Gust or ParticleStyle.Steam or ParticleStyle.Smoke;
        bool heavy = kit.Particles is ParticleStyle.Dust or ParticleStyle.Ooze;
        double jitter = airy ? 0.25 : 0.12;
        float drift = airy ? 0.35f : 0.18f;
        foreach (string hex in kit.Colours)
        {
            var particles = For(kit.Particles, at);
            var centre = at.AddCopy((rand.NextDouble() * 2 - 1) * jitter, (rand.NextDouble() * 2 - 1) * jitter, (rand.NextDouble() * 2 - 1) * jitter);
            particles.Color = ColorUtil.Hex2Int(hex) | unchecked((int)0xFF000000);
            particles.MinPos = centre.AddCopy(-0.06, -0.06, -0.06);
            particles.AddPos = new Vec3d(0.12, 0.12, 0.12);
            particles.MinVelocity = new Vec3f(-drift, -drift, -drift);
            particles.AddVelocity = new Vec3f(2 * drift, 2 * drift, 2 * drift);
            if (!heavy) particles.GravityEffect = 0;
            particles.LifeLength = (airy ? 0.8f : 0.45f) * (0.7f + 0.6f * (float)rand.NextDouble());
            particles.addLifeLength = particles.LifeLength * 0.5f;
            particles.MinSize *= 0.5f + 0.5f * (float)rand.NextDouble();
            particles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, particles.MaxSize * (airy ? 1.8f : 0.8f));
            particles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -255);
            particles.MinQuantity = airy ? 3 : 1;
            particles.AddQuantity = airy ? 2 : 1;
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
