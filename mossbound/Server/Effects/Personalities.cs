using System.Linq;
using Mossbound.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// Loads assets/mossbound/config/personalities.json. A castable aspect left out is logged, and its spells do nothing.
static class Personalities
{
    public static PersonalityTable Load(ICoreServerAPI sapi)
    {
        var table = PersonalityTable.Parse(sapi.Assets.Get(new AssetLocation("mossbound", "config/personalities.json")).ToText());
        foreach (var aspect in Recipes.CastableAspects.Where(a => !table.Aspects.ContainsKey(a)))
            sapi.Logger.Error("[mossbound] no personality for castable aspect {0}: its spells will do nothing", aspect);
        return table;
    }
}
