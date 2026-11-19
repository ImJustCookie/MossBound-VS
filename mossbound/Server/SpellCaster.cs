using System;
using System.Collections.Generic;
using Mossbound.Core;
using Mossbound.Entities;
using Mossbound.Items;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// Where a spell lands: the creature looked at, else the block looked at (no entity), else the caster.
readonly record struct SpellTarget(Entity? Entity, Vec3d Position);

// Casting an inscribed medium. The server reads the circle with the world's secret script,
// consumes the strip, then applies the spell or its mishap. All work happens once, at cast time.
class SpellCaster
{
    const long CooldownMs = 2000;
    const double TouchRange = 5; // shortcut: only the Touch form exists; Seed/Ring come with form glyphs

    readonly ICoreServerAPI sapi;
    readonly Func<WorldScript?> script;
    readonly SpellEffects effects;
    readonly Dictionary<string, long> lastCastMs = new();

    public SpellCaster(ICoreServerAPI sapi, Func<WorldScript?> script, ShieldBubbles shields)
    {
        this.sapi = sapi;
        this.script = script;
        effects = new SpellEffects(sapi, shields);
        sapi.Event.PlayerDisconnect += player => lastCastMs.Remove(player.PlayerUID);
    }

    public void Cast(IServerPlayer player, ItemSlot slot, EntitySelection? entitySel, BlockSelection? blockSel)
    {
        var worldScript = script();
        var circle = ItemCircleMedium.ReadCircle(slot.Itemstack);
        if (worldScript == null || circle == null || slot.Itemstack?.Collectible is not ItemCircleMedium medium
            || !CircleValidation.FitsTier(circle, medium.TierOf(slot.Itemstack))) return;
        var tier = medium.TierOf(slot.Itemstack);

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

        var plan = SpellReading.Read(circle, worldScript, tier);
        var target = ResolveTouch(player, entitySel, blockSel);
        if (plan.Fizzles)
        {
            effects.ApplyMishap(player, plan.Mishaps, tier.Limits);
            return;
        }
        // shortcut: Ring still acts by touch until its delivery exists (roadmap phase 3).
        foreach (var cell in plan.Cells)
        {
            if (cell.Form == Form.Seed) Sow(player, cell);
            else effects.Apply(player, target, cell);
        }
    }

    // A Seed flies from just below the caster's eyes where they look, in its aspect's motion, and applies the spell where it lands.
    void Sow(IServerPlayer player, SpellCell cell)
    {
        if (effects.PersonalityOf(cell.Aspect) is not { } personality) return;
        var type = sapi.World.GetEntityType(new AssetLocation("mossbound", "seed-" + cell.Aspect.ToString().ToLowerInvariant()));
        if (type == null || sapi.World.ClassRegistry.CreateEntity(type) is not EntitySeed seed) return;

        var caster = player.Entity;
        var look = caster.Pos.GetViewVector().ToVec3d();
        var start = caster.Pos.XYZ.Add(0, caster.LocalEyePos.Y - 0.2, 0).Add(look.X * 0.8, look.Y * 0.8, look.Z * 0.8);
        seed.Launch(caster, start, look, SeedFlight.For(personality.Motion, cell.Scale), personality.Kit,
            personality.Impact == Impact.PassThrough, target => effects.Apply(player, target, cell));
        sapi.World.SpawnEntity(seed);
    }

    // The client says what the player looks at; the server only accepts it within reach.
    static SpellTarget ResolveTouch(IServerPlayer player, EntitySelection? entitySel, BlockSelection? blockSel)
    {
        var caster = player.Entity;
        var eye = caster.Pos.XYZ.Add(0, caster.LocalEyePos.Y, 0);

        if (entitySel?.Entity is Entity e && e.Alive && e.Pos.DistanceTo(eye) <= TouchRange + e.SelectionBox.XSize)
        {
            return new SpellTarget(e, LastingEffect.Middle(e));
        }
        if (blockSel != null)
        {
            // Just above the block looked at; a plant counts as the ground it grows on, so the soil under it is reached too.
            bool plant = caster.World.BlockAccessor.GetBlock(blockSel.Position).BlockMaterial == EnumBlockMaterial.Plant;
            var point = blockSel.Position.ToVec3d().Add(0.5, plant ? 0 : 1, 0.5);
            if (point.DistanceTo(eye) <= TouchRange + 1) return new SpellTarget(null, point);
        }
        return new SpellTarget(caster, LastingEffect.Middle(caster));
    }
}
