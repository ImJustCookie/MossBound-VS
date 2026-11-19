using System.Collections.Generic;
using System.Linq;

namespace Mossbound.Core;

// A state a spell can find on what it hits. Wet and Chilled are left by spells; Burning is the game's own fire.
public enum Condition { Wet, Chilled, Burning }

public enum Reaction
{
    Frozen,     // Frost on something wet: rooted in ice
    SteamBurst, // Ember on something wet: no fire, a scalding puff instead
    Doused,     // Water or Rain on something burning: a hiss of steam
    Fanned,     // Wind on something burning: the fire spreads to the ground around
    Thawed,     // Ember on something chilled: the chill ends
}

// What happens when a spell meets a condition: the moment spells combine.
public static class Reactions
{
    static readonly (Condition on, Aspect[] incoming, Reaction reaction)[] table =
    {
        (Condition.Wet, new[] { Aspect.Frost }, Reaction.Frozen),
        (Condition.Wet, new[] { Aspect.Ember }, Reaction.SteamBurst),
        (Condition.Burning, new[] { Aspect.Water, Aspect.Rain }, Reaction.Doused),
        (Condition.Burning, new[] { Aspect.Wind }, Reaction.Fanned),
        (Condition.Chilled, new[] { Aspect.Ember }, Reaction.Thawed),
    };

    public static List<Reaction> For(IReadOnlyCollection<Condition> on, Aspect incoming) =>
        table.Where(r => on.Contains(r.on) && r.incoming.Contains(incoming)).Select(r => r.reaction).ToList();

    // The reaction replaces the spell's own payload.
    public static bool ReplacesPayload(Reaction reaction) => reaction == Reaction.SteamBurst;
}
