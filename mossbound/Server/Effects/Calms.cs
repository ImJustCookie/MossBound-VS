using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Mossbound.Server;

// Calms a creature: while it lasts it can't hunt, attack or flee, so it forgets what it was after and goes back to
// grazing. Players and creatures without AI are left alone.
sealed class Calms(ICoreServerAPI sapi) : LastingEffect(sapi, 6)
{
    readonly AiHold hold = new(task => task is AiTaskBaseTargetable);

    protected override Entity? TargetOf(SpellHit hit) => hit.Harmable is { } target && AiHold.HasAi(target) ? target : null;
    protected override void Begin(Entity target, float amount) => hold.Begin(target);
    protected override void Finish(Entity target) => hold.Release(target);
}
