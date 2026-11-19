using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// Slows what was hit, or roots it (factor 0): a Harmony postfix on EntityAgent.GetWalkSpeedMultiplier (wired in the mod
// system) multiplies its walking by a factor kept in its watched attributes, so the server and every client move it
// the same way. That is how the game slows walking on cobwebs and honey.
sealed class Slows(ICoreServerAPI sapi, System.Func<float, float> factorFor, float defaultSeconds) : LastingEffect(sapi, defaultSeconds)
{
    const string Factor = "mossbound:slow";

    public static Slows Slow(ICoreServerAPI sapi) => new(sapi, amount => 1 - Math.Min(0.9f, 0.5f * amount), 3);
    public static Slows Root(ICoreServerAPI sapi) => new(sapi, _ => 0, 1.5f);

    protected override void Begin(Entity target, float amount) => target.WatchedAttributes.SetFloat(Factor, factorFor(amount));
    protected override void Finish(Entity target) => target.WatchedAttributes.RemoveAttribute(Factor);

    // The Harmony postfix.
    public static void AfterWalkSpeed(EntityAgent __instance, ref double __result)
    {
        float factor = __instance.WatchedAttributes.GetFloat(Factor, 1);
        if (factor < 1) __result *= factor;
    }
}
