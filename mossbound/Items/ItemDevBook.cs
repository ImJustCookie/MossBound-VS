using Mossbound.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Mossbound.Items;

// Creative-only dev tool: right-click asks the server to open the dev book (Server/DevBook.cs checks who may).
public class ItemDevBook : Item
{
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        handling = EnumHandHandling.PreventDefault;
        if (firstEvent && byEntity.World.Api is ICoreClientAPI capi && byEntity == capi.World.Player.Entity)
            capi.ModLoader.GetModSystem<MossboundModSystem>().Send(new DevBookOpen());
    }
}
