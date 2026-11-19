using System;
using Mossbound.Core;
using Mossbound.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Mossbound.Entities;

// How a seed in flight looks and sounds on one client: its aspect's travel loop and its trail, both placed where this
// client draws the seed. The server writes what they need on the seed when it launches (Write).
sealed class SeedLooks(EntitySeed seed, IClientWorldAccessor world)
{
    const string LoopKey = "mossbound:loop";
    const string TrailStyleKey = "mossbound:trail", TrailColoursKey = "mossbound:trailColours";
    const double TrailSpacing = 0.4; // blocks between trail puffs
    const int MaxPuffsPerTick = 16;

    ILoadedSound? loop;
    Kit? trailKit;
    Vec3d? lastPuff;

    public static void Write(EntitySeed seed, Kit kit)
    {
        if (kit.LoopSound != null) seed.WatchedAttributes.SetString(LoopKey, kit.LoopSound);
        seed.WatchedAttributes.SetString(TrailStyleKey, kit.Particles.ToString());
        seed.WatchedAttributes.SetString(TrailColoursKey, string.Join(",", kit.Colours));
    }

    public void Tick()
    {
        var here = DrawnCentre();
        Hum(here);
        Trail(here);
    }

    public void Dispose()
    {
        loop?.Stop();
        loop?.Dispose();
        loop = null;
    }

    void Hum(Vec3d here)
    {
        if (loop == null && seed.WatchedAttributes.GetString(LoopKey) is { } sound)
        {
            loop = world.LoadSound(new SoundParams
            {
                Location = new AssetLocation(sound),
                ShouldLoop = true,
                DisposeOnFinish = false,
                Position = here.ToVec3f(),
                Range = 24,
                Volume = 0.6f,
            });
            loop?.Start();
        }
        loop?.SetPosition(here.ToVec3f());
    }

    // Puffs laid evenly along the whole stretch drawn since the last tick, so the trail has no gaps however fast the
    // seed or slow the updates.
    void Trail(Vec3d here)
    {
        trailKit ??= TrailKit();
        lastPuff ??= here;
        double distance = lastPuff.DistanceTo(here);
        if (trailKit == null || distance < TrailSpacing) return;
        int puffs = Math.Min(MaxPuffsPerTick, (int)(distance / TrailSpacing));
        for (int i = 1; i <= puffs; i++) KitParticles.Trail(world, trailKit, EntitySeed.Along(lastPuff, here, (double)i / puffs));
        lastPuff = here;
    }

    // Where this client draws the seed: the blend between the last two server updates that the game's interpolation
    // renders. The seed's Pos jumps to the newest server position whenever an update arrives, ahead of the drawn model.
    Vec3d DrawnCentre()
    {
        if (seed.GetBehavior<EntityBehaviorInterpolatePosition>() is not { } smoothing) return seed.Centre;
        var (last, next) = (smoothing.pL, smoothing.pN);
        if (last.x == 0 && last.y == 0 && last.z == 0) return seed.Centre; // no second update yet
        double t = smoothing.wait != 0 || next.interval <= 0 ? 1 : Math.Clamp(smoothing.dtAccum / next.interval, 0, 1);
        return new Vec3d(GameMath.Lerp(last.x, next.x, t), GameMath.Lerp(last.y, next.y, t) + EntitySeed.CentreHeight, GameMath.Lerp(last.z, next.z, t));
    }

    Kit? TrailKit()
    {
        if (!Enum.TryParse<ParticleStyle>(seed.WatchedAttributes.GetString(TrailStyleKey), out var style)) return null;
        string[] colours = seed.WatchedAttributes.GetString(TrailColoursKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        return colours.Length == 0 ? null : new Kit(colours, style, "");
    }
}
