using System.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Mossbound.Server;

// Calms a creature: while it lasts, any task it runs that hunts or attacks a target is stopped, so it forgets what it
// was after. Players and creatures without AI are left alone.
sealed class Calms(ICoreServerAPI sapi) : LastingEffect(sapi, 6)
{
    protected override Entity? TargetOf(SpellHit hit) => hit.Harmable?.GetBehavior<EntityBehaviorTaskAI>() != null ? hit.Harmable : null;
    protected override void Begin(Entity target, float amount) => Tick(target);
    protected override void Finish(Entity target) { }

    protected override void Tick(Entity target)
    {
        var tasks = target.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager;
        if (tasks == null) return;
        foreach (var task in tasks.ActiveTasksBySlot.OfType<AiTaskBaseTargetable>().ToList()) tasks.StopTask(task.GetType());
    }
}
