using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// An effect that lasts on an entity: one per entity (recasting replaces it), with the spell's particles shimmering on
// it, ended after its duration or when ended early (a reaction).
abstract class LastingEffect(ICoreServerAPI sapi, float defaultSeconds) : ISpellEffect
{
    const int ShimmerMs = 400;

    readonly Dictionary<long, long> running = new(); // entity id → its shimmer listener

    protected ICoreServerAPI Sapi => sapi;

    public void Apply(SpellHit hit, float amount, float seconds)
    {
        if (TargetOf(hit) is not { } target) return;
        End(target);
        Begin(target, amount);
        long shimmer = sapi.Event.RegisterGameTickListener(_ =>
        {
            Tick(target);
            KitParticles.Burst(sapi.World, hit.Kit, Middle(target), 0);
        }, ShimmerMs);
        running[target.EntityId] = shimmer;
        sapi.Event.RegisterCallback(_ => { if (running.GetValueOrDefault(target.EntityId) == shimmer) End(target); },
            (int)(1000 * (seconds > 0 ? seconds : defaultSeconds)));
    }

    public void End(Entity target)
    {
        if (!running.Remove(target.EntityId, out long shimmer)) return;
        sapi.Event.UnregisterGameTickListener(shimmer);
        Finish(target);
    }

    public static Vec3d Middle(Entity entity) => entity.Pos.XYZ.Add(0, entity.SelectionBox.Y2 / 2, 0);

    protected virtual Entity? TargetOf(SpellHit hit) => hit.Harmable;
    protected abstract void Begin(Entity target, float amount);
    protected virtual void Tick(Entity target) { }
    protected abstract void Finish(Entity target);
}
