using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Mossbound.Server;

// Keeps a creature's AI from running the tasks it refuses while held: the running ones stop, with their walk or attack
// animation, and the AI can't start them again until released. Stopping them on a timer instead restarted them in between.
sealed class AiHold(System.Func<IAiTask, bool> refuses)
{
    readonly Dictionary<long, ActionBoolReturn<IAiTask>> held = new();

    public void Begin(Entity target)
    {
        if (TasksOf(target) is not { } tasks) return;
        Release(target);
        ActionBoolReturn<IAiTask> allows = task => !refuses(task);
        tasks.OnShouldExecuteTask += allows;
        held[target.EntityId] = allows;
        foreach (var task in tasks.ActiveTasksBySlot.OfType<IAiTask>().Where(refuses).ToList()) tasks.StopTask(task.Slot);
    }

    public void Release(Entity target)
    {
        if (held.Remove(target.EntityId, out var allows) && TasksOf(target) is { } tasks) tasks.OnShouldExecuteTask -= allows;
    }

    public static bool HasAi(Entity entity) => TasksOf(entity) != null;

    static AiTaskManager? TasksOf(Entity entity) => entity.GetBehavior<EntityBehaviorTaskAI>()?.TaskManager;
}
