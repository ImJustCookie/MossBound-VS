using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Mossbound.Server;

static class ServerChat
{
    public static void Tell(this IServerPlayer player, string langKey) =>
        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get(langKey), EnumChatType.Notification);
}
