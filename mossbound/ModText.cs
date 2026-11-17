using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Mossbound;

// Every text a player reads comes from assets/mossbound/lang/<language>.json. On the server, always
// pass the player's language: the server's own language may not be theirs.
static class ModText
{
    public static string Get(string key, params object[] args) => Lang.Get("mossbound:" + key, args);

    public static string In(string languageCode, string key, params object[] args) => Lang.GetL(languageCode, "mossbound:" + key, args);

    public static string For(IServerPlayer player, string key, params object[] args) => In(player.LanguageCode, key, args);
}
