using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Mossbound.Server;

// Where a spell changes blocks: a ragged disc around the spot (edge columns in or out by chance, so patches aren't
// squares), changed outward from the middle over about a second rather than all at once. Only where the caster may build.
static class Around
{
    public static double Radius(float amount) => Math.Min(2.5, 0.5 + 0.6 * amount);

    public static IEnumerable<(int X, int Z, double Distance)> Disc(Random rand, double radius)
    {
        int reach = (int)Math.Ceiling(radius);
        for (int x = -reach; x <= reach; x++)
            for (int z = -reach; z <= reach; z++)
            {
                double distance = Math.Sqrt(x * x + z * z);
                if (distance <= radius * (0.7 + 0.3 * rand.NextDouble())) yield return (x, z, distance);
            }
    }

    // Each block of the disc a layer above and below the spot (uneven ground), changed after its delay.
    public static void Change(SpellHit hit, float amount, Action<BlockPos> change)
    {
        var centre = hit.Position.AsBlockPos;
        foreach (var (x, z, distance) in Disc(hit.Sapi.World.Rand, Radius(amount)))
            for (int y = -1; y <= 1; y++)
            {
                var pos = centre.AddCopy(x, y, z);
                if (hit.CanChange(pos)) Later(hit, distance, () => change(pos));
            }
    }

    public static void Later(SpellHit hit, double distance, Action change) =>
        hit.Sapi.Event.RegisterCallback(_ => change(), (int)(distance * 350 + hit.Sapi.World.Rand.Next(300)));
}

// Still water around the spot turns to lake ice.
sealed class FreezeWater : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var blocks = hit.Sapi.World.BlockAccessor;
        if (hit.Sapi.World.GetBlock(new AssetLocation("game:lakeice")) is not { } ice) return;
        Around.Change(hit, amount, pos =>
        {
            if (blocks.GetBlock(pos, BlockLayersAccess.Fluid).Code?.Path.StartsWith("water-still") != true) return;
            blocks.SetBlock(0, pos, BlockLayersAccess.Fluid);
            blocks.SetBlock(ice.BlockId, pos);
        });
    }
}

// Grass coverage on soil, from bare to full; Life moves it a step up, Rot a step down.
static class Grass
{
    static readonly string[] coverage = { "none", "verysparse", "sparse", "normal" };

    public static void Step(IWorldAccessor world, BlockPos pos, int by)
    {
        var soil = world.BlockAccessor.GetBlock(pos);
        if (soil.Code?.Path.StartsWith("soil-") != true) return;
        int step = Array.IndexOf(coverage, soil.Variant["grasscoverage"]) + by;
        if (step < 0 || step >= coverage.Length) return;
        if (world.GetBlock(soil.CodeWithVariant("grasscoverage", coverage[step])) is { } changed) world.BlockAccessor.SetBlock(changed.BlockId, pos);
    }
}

sealed class Regrow : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds) => Around.Change(hit, amount, pos => Grass.Step(hit.Sapi.World, pos, 1));
}

// Plants around the spot die (crops, tall grass and flowers become dead plants) and the soil loses a step of grass.
sealed class Wither : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var world = hit.Sapi.World;
        if (world.GetBlock(new AssetLocation("game:deadcrop")) is not { } dead) return;
        Around.Change(hit, amount, pos =>
        {
            var block = world.BlockAccessor.GetBlock(pos);
            string path = block.Code?.Path ?? "";
            if (block is BlockCrop) Kill(world, dead, block, pos);
            else if (path.StartsWith("tallgrass-") || path.StartsWith("flower-")) world.BlockAccessor.SetBlock(dead.BlockId, pos);
            else Grass.Step(world, pos, -1);
        });
    }

    // As the game kills a crop: the dead plant keeps the crop, so it is named after it and drops what a dead crop drops.
    static void Kill(IWorldAccessor world, Block dead, Block crop, BlockPos pos)
    {
        world.BlockAccessor.SetBlock(dead.BlockId, pos);
        if (world.BlockAccessor.GetBlockEntity(pos) is not BlockEntityDeadCrop corpse) return;
        corpse.Inventory[0].Itemstack = new ItemStack(crop);
        corpse.MarkDirty(true);
    }
}
