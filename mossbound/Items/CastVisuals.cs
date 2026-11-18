using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Mossbound.Items;

// How a cast looks while it charges and when it is released. One record per spell look; the medium picks
// one in ItemCircleMedium.CastStyleFor (same answer on client and server). Rates are particles per second at full charge.
public sealed record CastStyle(
    int[] Colours,              // picked at random per particle
    float TwinkleRate = 6,      // tiny sparkles that blink in place around the medium (this client only)
    float TwinkleRadius = 0.2f,
    float GroundRate = 300,     // spinning ring of sparks around the caster's feet
    float GroundRadius = 0.5f,
    float GroundSpin = 1.6f,    // turns per second of the spark heads at full charge
    float FlyOffShare = 0.03f,  // share of ring sparks thrown out of the spin
    float SiphonShare = 0.2f,   // share of ring sparks drawn up into the caster's legs and waist
    int ReleaseBurst = 30,      // sparks thrown out from the medium on release
    byte Glow = 160)            // 0-255, how much the particles light themselves
{
    public static readonly CastStyle Default = new(new[]
    {
        ColorUtil.ToRgba(220, 120, 220, 110),
        ColorUtil.ToRgba(200, 170, 240, 140),
        ColorUtil.ToRgba(180, 230, 250, 200),
    });
}

// Particles for a player casting with a medium. Charging: each client spawns them for every caster it renders,
// from the synced cast animation, so everyone sees them with no extra traffic. Release: the server spawns the
// burst (the medium may be used up at that moment, so a client could miss it) and it reaches every nearby client.
public static class CastVisuals
{
    static Random rand => Random.Shared; // client and server threads share this class in singleplayer

    // Where the medium floats above the hand, estimated from the hold pose (see tools/pose-preview):
    // low right of the view for the local player in first person, else beside the body.
    public static Vec3d SigilPosition(IWorldAccessor world, EntityAgent holder) =>
        world.Api is ICoreClientAPI capi && holder == capi.World.Player.Entity && capi.World.Player.CameraMode == EnumCameraMode.FirstPerson
            ? FirstPersonSigil(holder)
            : ThirdPersonSigil(holder);

    static Vec3d FirstPersonSigil(EntityAgent holder)
    {
        var look = holder.Pos.GetViewVector();
        var right = new Vec3f(-look.Z, 0, look.X).Normalize();
        var up = right.Cross(look);
        var offset = look * 0.55f + right * 0.3f - up * 0.22f;
        return holder.Pos.XYZ.Add(offset.X, holder.LocalEyePos.Y + offset.Y, offset.Z);
    }

    // Forward 0.5, right 0.3, up 1.2 from the feet.
    static Vec3d ThirdPersonSigil(EntityAgent holder)
    {
        var fwd = EntityPos.GetViewVector(0, holder.BodyYaw);
        return holder.Pos.XYZ.Add(fwd.X * 0.5 - fwd.Z * 0.3, 1.2, fwd.Z * 0.5 + fwd.X * 0.3);
    }

    // Called every frame while charging (charge 0-1). Each frame's expected count (rate x dt) is rounded at
    // random, so the rates hold at any frame rate without carrying state.
    public static void Charge(IWorldAccessor world, EntityAgent holder, CastStyle style, float charge, float dt)
    {
        if (charge <= 0 || dt <= 0) return;
        Twinkles(world, style, SigilPosition(world, holder), Count(style.TwinkleRate * charge * dt));
        FootRing(world, style, holder.Pos.XYZ.Add(0, 0.05, 0), charge, Count(style.GroundRate * charge * dt));
    }

    // Around the medium: a few motionless pinpricks that blink and shrink away where they appear.
    static void Twinkles(IWorldAccessor world, CastStyle style, Vec3d sigil, int count)
    {
        double r = style.TwinkleRadius;
        for (int i = 0; i < count; i++)
            Spawn(world, style, sigil.AddCopy(Rand(-r, r), Rand(-r, r), Rand(-r, r)), new Vec3f(), life: 0.3f, size: 0.12f);
    }

    // Around the caster: a spinning ring of sparks at the feet. Particles fly straight, so the spin is drawn by
    // where they appear: three spark heads race around the ring, and each short-lived spark leaves an arc behind.
    // A few sparks fly off the ring, some are drawn up into the caster (the siphon), the rest trail the heads.
    static void FootRing(IWorldAccessor world, CastStyle style, Vec3d feet, float charge, int count)
    {
        double heads = world.ElapsedMilliseconds / 1000.0 * style.GroundSpin * (0.5 + 0.5 * charge) * Math.PI * 2;
        for (int i = 0; i < count; i++)
        {
            double a = heads + rand.Next(3) * Math.PI * 2 / 3 - rand.NextDouble() * 0.9; // trail behind one of 3 heads
            var outward = new Vec3f((float)Math.Cos(a), 0, (float)Math.Sin(a));
            var along = new Vec3f(-outward.Z, 0, outward.X); // direction of spin
            var at = feet.AddCopy(outward.X * style.GroundRadius, 0, outward.Z * style.GroundRadius);

            double roll = rand.NextDouble();
            if (roll < style.FlyOffShare) FlyOffSpark(world, style, at, outward, along);
            else if (roll < style.FlyOffShare + style.SiphonShare) SiphonSpark(world, style, at, feet, along);
            else Spawn(world, style, at, along * 0.8f, life: 0.25f, size: 0.25f);
        }
    }

    // Flicks a short way out of the spin.
    static void FlyOffSpark(IWorldAccessor world, CastStyle style, Vec3d at, Vec3f outward, Vec3f along) =>
        Spawn(world, style, at, along * (float)Rand(0.5, 0.8) + (outward + new Vec3f(0, 0.05f, 0)) * (float)Rand(0.1, 0.2), life: 0.35f, size: 0.22f);

    // Heads for a point inside the lower body, still swirling a little. Its speed would reach that point in 0.5 s,
    // but it lives 0.35 s: at full life the sparks went past the body.
    static void SiphonSpark(IWorldAccessor world, CastStyle style, Vec3d at, Vec3d feet, Vec3f along)
    {
        var target = feet.AddCopy(0, Rand(0.2, 0.9), 0);
        var toward = new Vec3f((float)(target.X - at.X), (float)(target.Y - at.Y), (float)(target.Z - at.Z)) * 2f;
        Spawn(world, style, at, toward + along * 0.4f, life: 0.35f, size: 0.2f);
    }

    static int Count(float expected) => (int)(expected + rand.NextDouble());

    // Called on the server: each spawn call is one network message, so one call per colour, not per spark.
    public static void Release(IWorldAccessor world, EntityAgent holder, CastStyle style)
    {
        var at = SigilPosition(world, holder);
        int each = Math.Max(1, style.ReleaseBurst / style.Colours.Length);
        foreach (int colour in style.Colours)
        {
            var burst = Particles(style, colour, at, new Vec3f(-1.2f, -0.4f, -1.2f), life: 0.6f, size: 0.4f);
            burst.MinQuantity = each;
            burst.AddVelocity = new Vec3f(2.4f, 1.6f, 2.4f);
            world.SpawnParticles(burst);
        }
    }

    static void Spawn(IWorldAccessor world, CastStyle style, Vec3d at, Vec3f velocity, float life, float size) =>
        world.SpawnParticles(Particles(style, style.Colours[rand.Next(style.Colours.Length)], at, velocity, life, size));

    static SimpleParticleProperties Particles(CastStyle style, int colour, Vec3d at, Vec3f velocity, float life, float size) =>
        new()
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

    static double Rand(double min, double max) => min + rand.NextDouble() * (max - min);
}
