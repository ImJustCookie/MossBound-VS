using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Mossbound.Items;

// Spark particles of a cast. Charging: every client spawns them for each caster it renders, from the synced cast
// animation. Release: the server spawns the burst (in survival the medium is gone by then, so clients could miss it).
public static class CastSparks
{
    static Random Rand => Random.Shared; // client and server threads share this class in singleplayer
    static double Between(double min, double max) => min + Rand.NextDouble() * (max - min);

    // Each frame's expected count is rounded at random, so rates hold at any frame rate.
    static int Count(float expected) => (int)(expected + Rand.NextDouble());

    public static void Charge(IWorldAccessor world, EntityAgent holder, CastStyle style, float charge, float dt)
    {
        if (charge <= 0 || dt <= 0) return;
        Twinkles(world, style, CasterAnchors.Sigil(world, holder), Count(style.TwinkleRate * charge * dt));
        FootRing(world, style, CasterAnchors.Feet(world, holder).Add(0, 0.05, 0), charge, Count(style.GroundRate * charge * dt));
    }

    public static void Release(IWorldAccessor world, EntityAgent holder, CastStyle style)
    {
        var at = CasterAnchors.Sigil(world, holder);
        int each = Math.Max(1, style.ReleaseBurst / style.Colours.Length);
        foreach (int colour in style.Colours) // one network message per call, so per colour, not per spark
        {
            var burst = Particles(style, colour, at, new Vec3f(-1.2f, -0.4f, -1.2f), life: 0.6f, size: 0.4f);
            burst.MinQuantity = each;
            burst.AddVelocity = new Vec3f(2.4f, 1.6f, 2.4f);
            world.SpawnParticles(burst);
        }
    }

    static void Twinkles(IWorldAccessor world, CastStyle style, Vec3d sigil, int count)
    {
        double r = style.TwinkleRadius;
        for (int i = 0; i < count; i++)
            Spawn(world, style, sigil.AddCopy(Between(-r, r), Between(-r, r), Between(-r, r)), new Vec3f(), life: 0.3f, size: 0.12f);
    }

    // Particles fly straight, so the spin is drawn by where they appear: three heads race around the ring and
    // each short-lived spark trails one of them.
    static void FootRing(IWorldAccessor world, CastStyle style, Vec3d feet, float charge, int count)
    {
        double heads = world.ElapsedMilliseconds / 1000.0 * style.GroundSpin * (0.5 + 0.5 * charge) * Math.PI * 2;
        for (int i = 0; i < count; i++)
        {
            double a = heads + Rand.Next(3) * Math.PI * 2 / 3 - Rand.NextDouble() * 0.9;
            var outward = new Vec3f((float)Math.Cos(a), 0, (float)Math.Sin(a));
            var along = new Vec3f(-outward.Z, 0, outward.X);
            var at = feet.AddCopy(outward.X * style.GroundRadius, 0, outward.Z * style.GroundRadius);

            double roll = Rand.NextDouble();
            if (roll < style.FlyOffShare) FlyOffSpark(world, style, at, outward, along);
            else if (roll < style.FlyOffShare + style.SiphonShare) SiphonSpark(world, style, at, feet, along);
            else Spawn(world, style, at, along * 0.8f, life: 0.25f, size: 0.25f);
        }
    }

    static void FlyOffSpark(IWorldAccessor world, CastStyle style, Vec3d at, Vec3f outward, Vec3f along) =>
        Spawn(world, style, at, along * (float)Between(0.5, 0.8) + (outward + new Vec3f(0, 0.05f, 0)) * (float)Between(0.1, 0.2), life: 0.35f, size: 0.22f);

    // Aimed to reach the lower body in 0.5 s but living 0.35 s: at full life the sparks overshot.
    static void SiphonSpark(IWorldAccessor world, CastStyle style, Vec3d at, Vec3d feet, Vec3f along)
    {
        var target = feet.AddCopy(0, Between(0.2, 0.9), 0);
        var toward = new Vec3f((float)(target.X - at.X), (float)(target.Y - at.Y), (float)(target.Z - at.Z)) * 2f;
        Spawn(world, style, at, toward + along * 0.4f, life: 0.35f, size: 0.2f);
    }

    static void Spawn(IWorldAccessor world, CastStyle style, Vec3d at, Vec3f velocity, float life, float size) =>
        world.SpawnParticles(Particles(style, style.Colours[Rand.Next(style.Colours.Length)], at, velocity, life, size));

    static SimpleParticleProperties Particles(CastStyle style, int colour, Vec3d at, Vec3f velocity, float life, float size) => new()
    {
        MinQuantity = 1,
        Color = colour,
        MinPos = at,
        MinVelocity = velocity,
        LifeLength = life,
        GravityEffect = 0,
        MinSize = size * 0.6f,
        MaxSize = size,
        SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -size),
        VertexFlags = style.Glow,
        ParticleModel = EnumParticleModel.Quad,
    };
}
