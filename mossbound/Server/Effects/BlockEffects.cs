using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Mossbound.Server;

// Effects on the blocks around where a spell lands, reaching further with the amount. Only where the caster may build.
static class Around
{
    public static IEnumerable<BlockPos> Blocks(SpellHit hit, float amount, int maxReach)
    {
        int reach = Math.Clamp((int)amount, 1, maxReach);
        var centre = hit.Position.AsBlockPos;
        for (int x = -reach; x <= reach; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -reach; z <= reach; z++)
                {
                    var pos = centre.AddCopy(x, y, z);
                    if (hit.CanChange(pos)) yield return pos;
                }
    }
}

// Still water around the spot turns to lake ice.
sealed class FreezeWater : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var blocks = hit.Sapi.World.BlockAccessor;
        if (hit.Sapi.World.GetBlock(new AssetLocation("game:lakeice")) is not { } ice) return;
        foreach (var pos in Around.Blocks(hit, amount, 3))
        {
            if (blocks.GetBlock(pos, BlockLayersAccess.Fluid).Code?.Path.StartsWith("water-still") != true) continue;
            blocks.SetBlock(0, pos, BlockLayersAccess.Fluid);
            blocks.SetBlock(ice.BlockId, pos);
        }
    }
}

// Soil around the spot grows one step more grass (none, very sparse, sparse, normal).
sealed class Regrow : ISpellEffect
{
    static readonly string[] coverage = { "none", "verysparse", "sparse", "normal" };

    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var world = hit.Sapi.World;
        foreach (var pos in Around.Blocks(hit, amount, 3))
        {
            var soil = world.BlockAccessor.GetBlock(pos);
            int step = Array.IndexOf(coverage, soil.Variant["grasscoverage"]);
            if (soil.Code?.Path.StartsWith("soil-") != true || step < 0 || step == coverage.Length - 1) continue;
            if (world.GetBlock(soil.CodeWithVariant("grasscoverage", coverage[step + 1])) is { } grown) world.BlockAccessor.SetBlock(grown.BlockId, pos);
        }
    }
}

// Plants around the spot rot away: grass is stripped from the soil, tall grass and flowers vanish.
sealed class Wither : ISpellEffect
{
    public void Apply(SpellHit hit, float amount, float seconds)
    {
        var world = hit.Sapi.World;
        foreach (var pos in Around.Blocks(hit, amount, 3))
        {
            var block = world.BlockAccessor.GetBlock(pos);
            string path = block.Code?.Path ?? "";
            if (path.StartsWith("tallgrass-") || path.StartsWith("flower-")) world.BlockAccessor.SetBlock(0, pos);
            else if (path.StartsWith("soil-") && block.Variant["grasscoverage"] != "none"
                && world.GetBlock(block.CodeWithVariant("grasscoverage", "none")) is { } bare) world.BlockAccessor.SetBlock(bare.BlockId, pos);
        }
    }
}
