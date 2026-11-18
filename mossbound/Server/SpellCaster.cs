using System;
using System.Collections.Generic;
using Mossbound.Core;
using Mossbound.Items;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// Where a spell lands: a creature, or else a point (looked-at block or the caster).
readonly record struct SpellTarget(Entity? Entity, Vec3d Position);

// Casting an inscribed medium. The server reads the circle with the world's secret script,
// consumes the strip, then applies the spell or its mishap. All work happens once, at cast time.
class SpellCaster
{
    const long CooldownMs = 2000;
    const double TouchRange = 5; // shortcut: only the Touch form exists; Seed/Ring come with form glyphs

    readonly ICoreServerAPI sapi;
    readonly Func<WorldScript?> script;
    readonly Dictionary<string, long> lastCastMs = new();

    public SpellCaster(ICoreServerAPI sapi, Func<WorldScript?> script)
    {
        this.sapi = sapi;
        this.script = script;
        sapi.Event.PlayerDisconnect += player => lastCastMs.Remove(player.PlayerUID);
    }

    public void Cast(IServerPlayer player, ItemSlot slot, EntitySelection? entitySel, BlockSelection? blockSel)
    {
        var worldScript = script();
        var circle = ItemCircleMedium.ReadCircle(slot.Itemstack);
        if (worldScript == null || circle == null || slot.Itemstack?.Collectible is not ItemCircleMedium medium
            || !CircleValidation.FitsTier(circle, medium.Tier)) return;

        long now = sapi.World.ElapsedMilliseconds;
        if (lastCastMs.TryGetValue(player.PlayerUID, out long last) && now - last < CooldownMs)
        {
            player.Tell("cast-cooldown");
            return;
        }
        lastCastMs[player.PlayerUID] = now;
        CastSparks.Release(sapi.World, player.Entity, ItemCircleMedium.CastStyleFor(slot.Itemstack));

        // Creative players keep the strip so they can recast while experimenting.
        if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            slot.TakeOut(1);
            slot.MarkDirty();
        }

        var plan = SpellReading.Read(circle, worldScript);
        var target = ResolveTouch(player, entitySel, blockSel);
        if (plan.Fizzles)
        {
            SpellEffects.ApplyMishap(sapi, player, plan.Mishaps);
            return;
        }
        foreach (var (aspect, intensity) in plan.Aspects) SpellEffects.Apply(sapi, player, target, aspect, intensity);
    }

    // The client says what the player looks at; the server only accepts it within reach.
    static SpellTarget ResolveTouch(IServerPlayer player, EntitySelection? entitySel, BlockSelection? blockSel)
    {
        var caster = player.Entity;
        var eye = caster.Pos.XYZ.Add(0, caster.LocalEyePos.Y, 0);

        if (entitySel?.Entity is Entity e && e.Alive && e.Pos.DistanceTo(eye) <= TouchRange + e.SelectionBox.XSize)
        {
            return new SpellTarget(e, e.Pos.XYZ.Add(0, e.SelectionBox.Y2 / 2, 0));
        }
        if (blockSel != null)
        {
            var point = blockSel.Position.ToVec3d().Add(0.5, 1, 0.5);
            if (point.DistanceTo(eye) <= TouchRange + 1) return new SpellTarget(null, point);
        }
        return new SpellTarget(null, caster.Pos.XYZ.Add(0, 1, 0));
    }
}
