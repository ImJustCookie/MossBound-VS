using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Mossbound.Server;

static class ServerChat
{
    // `key` is a lang key without the "mossbound:" domain; the text is in the player's language.
    public static void Tell(this IServerPlayer player, string key, params object[] args) =>
        player.SendMessage(GlobalConstants.GeneralChatGroup, ModText.For(player, key, args), EnumChatType.Notification);
}
