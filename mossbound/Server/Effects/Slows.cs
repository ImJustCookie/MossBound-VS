using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// Slows what was hit: a Harmony postfix on EntityAgent.GetWalkSpeedMultiplier (wired in the mod system) multiplies its
// walking by a factor kept in its watched attributes, so the server and every client move it the same way. That is
// how the game slows walking on cobwebs and honey. The spell's particles shimmer on it meanwhile. Recasting replaces it.
sealed class Slows(ICoreServerAPI sapi) : ISpellEffect
{
    const string Factor = "mossbound:slow";
    const float DefaultSeconds = 3;
    const int ShimmerMs = 400;

    readonly Dictionary<long, long> shimmers = new();

    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var target = hit.Harmable;
        if (target == null) return;
        Stop(target);

        target.WatchedAttributes.SetFloat(Factor, 1 - Math.Min(0.9f, 0.5f * amount));
        long shimmer = sapi.Event.RegisterGameTickListener(_ =>
            KitParticles.Burst(sapi.World, hit.Kit, target.Pos.XYZ.Add(0, target.SelectionBox.Y2 / 2, 0), 0), ShimmerMs);
        shimmers[target.EntityId] = shimmer;
        sapi.Event.RegisterCallback(_ => { if (shimmers.GetValueOrDefault(target.EntityId) == shimmer) Stop(target); },
            (int)(1000 * (seconds > 0 ? seconds : DefaultSeconds)));
    }

    // The Harmony postfix.
    public static void AfterWalkSpeed(EntityAgent __instance, ref double __result)
    {
        float factor = __instance.WatchedAttributes.GetFloat(Factor, 1);
        if (factor < 1) __result *= factor;
    }

    void Stop(Entity target)
    {
        target.WatchedAttributes.RemoveAttribute(Factor);
        if (shimmers.Remove(target.EntityId, out long shimmer)) sapi.Event.UnregisterGameTickListener(shimmer);
    }
}
