using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Mossbound.Server;

// Sets what was hit on fire, or the ground looked at: fire placed on top of blocks the way vanilla lights one, so the
// game decides what catches (flammability, reinforcement). Strength widens the patch: one block at 1, a 3x3 square at
// 2, 5x5 at 3. Only where the caster may build.
sealed class Ignite : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        if (hit.Entity != null)
        {
            hit.Harmable?.Ignite();
            return;
        }
        var world = hit.Sapi.World;
        if (world.GetBlock(new AssetLocation("game:fire")) is not { } fire) return;
        var centre = hit.Position.AsBlockPos;
        int reach = Math.Clamp((int)amount - 1, 0, 3);
        for (int x = -reach; x <= reach; x++)
            for (int z = -reach; z <= reach; z++)
                if (FireSpot(world, centre.AddCopy(x, 0, z)) is { } firePos && hit.CanChange(firePos))
                    Light(world, fire, firePos, hit.Caster.PlayerUID);
    }

    // The air block above the ground at this column, a block up or down allowed for uneven ground.
    static BlockPos? FireSpot(IWorldAccessor world, BlockPos pos)
    {
        foreach (int dy in new[] { 0, 1, -1 })
        {
            var at = pos.AddCopy(0, dy, 0);
            if (world.BlockAccessor.GetBlock(at).Replaceable >= 6000 && world.BlockAccessor.GetBlock(at.DownCopy()).Replaceable < 6000) return at;
        }
        return null;
    }

    static void Light(IWorldAccessor world, Block fire, BlockPos firePos, string casterUid)
    {
        world.BlockAccessor.SetBlock(fire.BlockId, firePos);
        world.BlockAccessor.GetBlockEntity(firePos)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(firePos, firePos.DownCopy(), casterUid);
    }
}

// Puts out whoever was hit, and fire blocks around the spot: fire has no selection box, so the block looked at is
// the burning one, and its fire can be on any side. The reach grows with the amount.
sealed class Extinguish : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        if (hit.Entity != null) hit.Entity.IsOnFire = false;
        var blocks = hit.Sapi.World.BlockAccessor;
        int reach = Math.Clamp(1 + (int)amount, 2, 4);
        var centre = hit.Position.AsBlockPos;
        var pos = new BlockPos(centre.dimension);
        for (int x = -reach; x <= reach; x++)
            for (int y = -reach; y <= reach; y++)
                for (int z = -reach; z <= reach; z++)
                {
                    pos.Set(centre.X + x, centre.Y + y, centre.Z + z);
                    if (blocks.GetBlock(pos).Code?.Path == "fire") blocks.SetBlock(0, pos);
                }
    }
}
