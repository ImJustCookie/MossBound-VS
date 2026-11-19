using System.Collections.Generic;
using System.Linq;
using Mossbound.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Mossbound.Server;

// Leaves a condition (Wet, Chilled) on what was hit, for the next spell to react to. Kept in the entity's watched
// attributes, so it lasts on the entity itself.
sealed class Afflict(ICoreServerAPI sapi, Condition condition, float defaultSeconds) : LastingEffect(sapi, defaultSeconds)
{
    protected override void Begin(Entity target, float amount) => target.WatchedAttributes.SetBool(Conditions.Key(condition), true);
    protected override void Finish(Entity target) => target.WatchedAttributes.RemoveAttribute(Conditions.Key(condition));
}

static class Conditions
{
    public static string Key(Condition condition) => "mossbound:" + condition.ToString().ToLowerInvariant();

    // What a spell finds on an entity: our own conditions, and the game's fire and poison.
    public static List<Condition> On(Entity entity)
    {
        var on = new[] { Condition.Wet, Condition.Chilled }.Where(c => entity.WatchedAttributes.GetBool(Key(c))).ToList();
        if (entity.IsOnFire) on.Add(Condition.Burning);
        if (entity.GetBehavior<EntityBehaviorHealth>()?.ActiveDoTEffects.Any(e => e.EffectType == (int)EnumDamageOverTimeEffectType.Poison && e.TicksLeft > 0) == true)
            on.Add(Condition.Poisoned);
        return on;
    }
}
