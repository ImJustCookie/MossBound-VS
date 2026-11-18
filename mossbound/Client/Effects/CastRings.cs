using System;
using System.Collections.Generic;
using System.Linq;
using Mossbound.Core;
using Mossbound.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Mossbound.Client;

enum RingsEnding { None, Release, Cancel }

// One caster's glyph rings: one ring per cast in the spell, stacked around the waist. The glyphs appear one by one
// and rise into place, then bob; once ready the rings turn, in alternate directions. Release flings them outward;
// cancel sinks them.
sealed class CastRings
{
    public const int MaxRings = 3;
    public const float ReadyCharge = 0.99f;
    const float WaistHeight = 0.95f, RingGap = 0.3f, RingRadius = 0.85f, StartHeight = 0.15f;
    const float ChargingBob = 0.04f, ReadyBob = 0.16f, GlyphSize = 0.36f, AppearShare = 0.8f;
    const float ReleaseSeconds = 0.6f, CancelSeconds = 0.5f;

    sealed class Ring
    {
        public float Height, Radius, Direction, SpeedFactor, Spin;
    }

    sealed class RingGlyph
    {
        public required Ring Ring;
        public required LoadedTexture Sprite;
        public required Vec4f Colour;
        public float Angle, Distance, Height, RiseSpeed, Delay, BobPhase, BobSpeed;
    }

    static Random Rand => Random.Shared;
    static float Between(float min, float max) => min + Rand.NextSingle() * (max - min);

    readonly List<Ring> rings = new();
    readonly List<RingGlyph> glyphs = new();
    readonly float spinSpeed; // radians per second
    float age, endAge, ready;

    public EntityAgent Holder { get; }
    public float Charge { get; set; }
    public long LastCastingMs { get; set; }
    public RingsEnding Ending { get; private set; }
    public bool Finished => endAge >= (Ending == RingsEnding.Release ? ReleaseSeconds : CancelSeconds);

    public CastRings(EntityAgent holder, List<List<PlacedGlyph>> groups, CastStyle style, float castSeconds, GlyphSprites sprites)
    {
        Holder = holder;
        spinSpeed = style.GlyphSpin * MathF.PI * 2;
        int total = groups.Sum(g => g.Count), appeared = 0;
        for (int r = 0; r < groups.Count; r++)
        {
            var ring = NewRing(r, groups.Count);
            rings.Add(ring);
            float start = Between(0, MathF.PI * 2);
            for (int i = 0; i < groups[r].Count; i++)
            {
                float delay = AppearShare * castSeconds * appeared++ / total;
                glyphs.Add(NewGlyph(ring, sprites.For(groups[r][i]), style, start + i * MathF.PI * 2 / groups[r].Count, delay, castSeconds));
            }
        }
    }

    static Ring NewRing(int index, int count)
    {
        float fromMiddle = index - (count - 1) / 2f;
        return new()
        {
            Height = WaistHeight + fromMiddle * RingGap,
            Radius = RingRadius * (1 + 0.12f * fromMiddle),
            Direction = index % 2 == 0 ? 1 : -1,
            SpeedFactor = Between(0.8f, 1.2f),
        };
    }

    static RingGlyph NewGlyph(Ring ring, LoadedTexture sprite, CastStyle style, float angle, float delay, float castSeconds) => new()
    {
        Ring = ring,
        Sprite = sprite,
        Colour = ColorUtil.ToRGBAVec4f(style.Colours[Rand.Next(style.Colours.Length)]),
        Angle = angle,
        Distance = ring.Radius + Between(-0.15f, 0.15f),
        Height = StartHeight,
        RiseSpeed = Between(0.6f, 1.1f) / castSeconds,
        Delay = delay,
        BobPhase = Between(0, MathF.PI * 2),
        BobSpeed = Between(1.1f, 1.9f),
    };

    public void End(RingsEnding how) => Ending = how;

    public void Draw(GlyphQuads quads, Vec3d feet, float dt)
    {
        Advance(dt);
        float end = Ending switch
        {
            RingsEnding.Release => endAge / ReleaseSeconds,
            RingsEnding.Cancel => endAge / CancelSeconds,
            _ => 0,
        };
        float spread = Ending == RingsEnding.Release ? 1 + 2.5f * end * end : 1;
        float scale = Ending switch
        {
            RingsEnding.Release => 1 + 1.5f * end,
            RingsEnding.Cancel => 1 - 0.6f * end,
            _ => 1,
        };
        float sink = Ending == RingsEnding.Cancel ? 0.4f * end : 0;
        float bob = ChargingBob + (ReadyBob - ChargingBob) * ready;

        foreach (var g in glyphs)
        {
            float shownFor = age - g.Delay;
            if (shownFor <= 0) continue;
            float height = g.Height + bob * Drift(g) - sink;
            float radius = g.Distance * spread;
            float angle = g.Angle + g.Ring.Spin;
            quads.Draw(g.Sprite, g.Colour, Math.Min(1, shownFor * 4) * (1 - end),
                feet.X + Math.Cos(angle) * radius, feet.Y + height, feet.Z + Math.Sin(angle) * radius, angle, GlyphSize * scale);
        }
    }

    void Advance(float dt)
    {
        age += dt;
        if (Ending != RingsEnding.None) endAge += dt;
        bool isReady = Charge >= ReadyCharge;
        float speed = Ending == RingsEnding.Release ? spinSpeed * 3 : spinSpeed;
        if (isReady) foreach (var ring in rings) ring.Spin += ring.Direction * ring.SpeedFactor * speed * dt;
        ready += ((isReady ? 1 : 0) - ready) * Math.Min(1, dt * 3);
        foreach (var g in glyphs)
            if (age > g.Delay) g.Height = Math.Min(g.Ring.Height, g.Height + g.RiseSpeed * dt);
    }

    // Two sines at unrelated speeds: an uneven drift rather than a steady wave.
    float Drift(RingGlyph g) =>
        0.7f * MathF.Sin(age * g.BobSpeed + g.BobPhase) + 0.3f * MathF.Sin(age * g.BobSpeed * 2.3f + g.BobPhase * 1.7f);
}
