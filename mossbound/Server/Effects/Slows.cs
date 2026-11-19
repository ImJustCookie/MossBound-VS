using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// Slows what was hit, or roots it in place: a Harmony postfix on EntityAgent.GetWalkSpeedMultiplier (wired in the mod
// system) multiplies its walking by factors kept in its watched attributes, so the server and every client move it
// the same way. That is how the game slows walking on cobwebs and honey. Slow and root keep separate factors, so a
// slow landing on a rooted target doesn't free it. A rooted creature's AI is held too, so it doesn't run in place.
sealed class Slows : LastingEffect
{
    const string SlowKey = "mossbound:slow", RootKey = "mossbound:root";

    readonly string key;
    readonly System.Func<float, float> factorFor;
    readonly AiHold? hold;

    Slows(ICoreServerAPI sapi, string key, System.Func<float, float> factorFor, float defaultSeconds, AiHold? hold) : base(sapi, defaultSeconds)
    {
        this.key = key;
        this.factorFor = factorFor;
        this.hold = hold;
    }

    public static Slows Slow(ICoreServerAPI sapi) => new(sapi, SlowKey, amount => 1 - Math.Min(0.8f, 0.4f * amount), 3, null);
    public static Slows Root(ICoreServerAPI sapi) => new(sapi, RootKey, _ => 0, 1.5f, new AiHold(_ => true));

    protected override void Begin(Entity target, float amount)
    {
        target.WatchedAttributes.SetFloat(key, factorFor(amount));
        hold?.Begin(target);
    }

    protected override void Finish(Entity target)
    {
        target.WatchedAttributes.RemoveAttribute(key);
        hold?.Release(target);
    }

    // The Harmony postfix.
    public static void AfterWalkSpeed(EntityAgent __instance, ref double __result) =>
        __result *= __instance.WatchedAttributes.GetFloat(SlowKey, 1) * __instance.WatchedAttributes.GetFloat(RootKey, 1);
}
