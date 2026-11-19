using System;
using System.Collections.Generic;
using Mossbound.Core;
using Mossbound.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Mossbound.Entities;

// A Seed spell in flight. The server moves it along its aspect's motion and lands it on the first creature or solid
// block in its path, or on water unless it is heavy enough to sink; plants and other non-solid blocks never stop it,
// and a pass-through seed goes on through creatures. At the end of its range it fizzles.
// Clients only draw it (entities/seed.json, one variant per aspect) with its sound and trail (SeedLooks). Never saved,
// so a seed in flight when its chunk unloads or the server stops is simply gone.
// The seed is simulated at its model's centre: the game draws the model centred CentreHeight above Pos and turns it
// around that point, as long as the hitbox in entities/seed.json is twice that tall.
public class EntitySeed : Entity
{
    const int MaxPassedThrough = 5;
    internal const double CentreHeight = 0.5;
    const double Radius = 0.2; // how close a creature must pass to be struck
    const double SwayPerSecond = 6; // radians
    // Clients draw a seed about one network update behind where the server has it: the hit waits this long at the
    // impact point, so the drawn seed arrives as the spell goes off.
    const int LandDelayMs = 150;

    Entity? caster;
    SeedFlight flight;
    Vec3d velocity = new();
    Vec3d side = new();
    Kit? kit;
    bool passesThrough;
    Action<SpellTarget>? land;
    readonly HashSet<long> passed = new();
    double age;
    (SpellTarget Target, long DueMs)? landing;
    SeedLooks? looks; // client only

    public override bool StoreWithChunk => false;
    public override bool IsInteractable => false;
    public override bool ShouldReceiveDamage(DamageSource damageSource, float damage) => false;

    internal void Launch(Entity from, Vec3d start, Vec3d direction, SeedFlight seedFlight, Kit seedKit, bool through, Action<SpellTarget> onLand)
    {
        caster = from;
        flight = seedFlight;
        velocity = direction.Clone().Normalize().Mul(seedFlight.Speed);
        side = new Vec3d(-direction.Z, 0, direction.X).Normalize();
        kit = seedKit;
        passesThrough = through;
        land = onLand;
        SeedLooks.Write(this, seedKit);
        Pos.SetPos(start.AddCopy(0, -CentreHeight, 0));
        Face();
    }

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);
        if (World is IClientWorldAccessor client)
        {
            (looks ??= new SeedLooks(this, client)).Tick();
            return;
        }
        if (!Alive) return;
        if (landing is { } due)
        {
            if (World.ElapsedMilliseconds < due.DueMs) return;
            land!(due.Target);
            Die(EnumDespawnReason.Expire);
            return;
        }
        if (land == null || age > flight.Seconds)
        {
            Fizzle();
            return;
        }

        double sway = flight.Sway * (Math.Sin((age + dt) * SwayPerSecond) - Math.Sin(age * SwayPerSecond));
        age += dt;
        velocity.Y -= flight.Gravity * dt;
        var from = Centre;
        var to = from.AddCopy(velocity.X * dt + side.X * sway, velocity.Y * dt, velocity.Z * dt + side.Z * sway);
        if (Strike(from, to)) return;

        Pos.SetPos(to.AddCopy(0, -CentreHeight, 0));
        Face();
    }

    internal Vec3d Centre => Pos.XYZ.Add(0, CentreHeight, 0);

    // What lies between from and to. Returns true once the seed has struck its last target.
    bool Strike(Vec3d from, Vec3d to)
    {
        // Blocks only: the game's trace looks for entities only near the start of the step, missing most creatures.
        BlockSelection? blockSel = null;
        EntitySelection? none = null;
        World.RayTraceForSelection(from, to, ref blockSel, ref none, (_, block) => block.CollisionBoxes is { Length: > 0 }, _ => false);
        double step = Math.Max(1e-6, from.DistanceTo(to));
        double blockAt = blockSel == null ? double.MaxValue : from.DistanceTo(blockSel.Position.ToVec3d().Add(blockSel.HitPosition)) / step;

        if (FirstCreature(from, to) is { } struck && struck.At <= blockAt)
        {
            var hit = struck.Creature;
            var target = new SpellTarget(hit, LastingEffect.Middle(hit));
            if (passesThrough && passed.Count < MaxPassedThrough)
            {
                passed.Add(hit.EntityId);
                land!(target);
                return false;
            }
            return Settle(target, Along(from, to, struck.At));
        }
        if (blockSel != null)
        {
            // Just off the face it struck, so block effects reach the surface there.
            var face = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5).Add(blockSel.Face.Normald.Clone().Mul(0.75));
            return Settle(new SpellTarget(null, face), Along(from, to, blockAt));
        }
        var at = to.AsBlockPos;
        if (!flight.Sinks && World.BlockAccessor.GetBlock(at, BlockLayersAccess.Fluid).IsLiquid())
            return Settle(new SpellTarget(null, at.ToVec3d().Add(0.5, 0.5, 0.5)), to);
        return false;
    }

    internal static Vec3d Along(Vec3d from, Vec3d to, double fraction) =>
        from.AddCopy((to.X - from.X) * fraction, (to.Y - from.Y) * fraction, (to.Z - from.Z) * fraction);

    // Parks the seed where it struck; the spell goes off there once the drawn seed has caught up.
    bool Settle(SpellTarget target, Vec3d impact)
    {
        Pos.SetPos(impact.AddCopy(0, -CentreHeight, 0));
        landing = (target, World.ElapsedMilliseconds + LandDelayMs);
        return true;
    }

    // The nearest creature the step passes through (its selection box, grown by the seed's radius), not yet struck.
    (Entity Creature, double At)? FirstCreature(Vec3d from, Vec3d to)
    {
        float reach = (float)from.DistanceTo(to) + 3;
        (Entity, double)? first = null;
        foreach (var e in World.GetEntitiesAround(from, reach, reach, e => e is EntityAgent { Alive: true } && e != caster && !passed.Contains(e.EntityId)))
        {
            var box = e.SelectionBox;
            var at = SegmentHit.Enters((from.X, from.Y, from.Z), (to.X, to.Y, to.Z),
                (e.Pos.X + box.X1 - Radius, e.Pos.Y + box.Y1 - Radius, e.Pos.Z + box.Z1 - Radius),
                (e.Pos.X + box.X2 + Radius, e.Pos.Y + box.Y2 + Radius, e.Pos.Z + box.Z2 + Radius));
            if (at is double t && (first == null || t < first.Value.Item2)) first = (e, t);
        }
        return first;
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        looks?.Dispose();
        base.OnEntityDespawn(despawn);
    }

    void Fizzle()
    {
        if (kit != null) KitParticles.Burst(World, kit, Centre, 0);
        Die(EnumDespawnReason.Expire);
    }

    // Seed models point their tip toward -Z (north): tipped along the flight, up or down, and spun around their length
    // at their aspect's spin (Core/Spells/SeedFacing.cs).
    void Face()
    {
        var (pitch, yaw, roll) = SeedFacing.For(velocity.X, velocity.Y, velocity.Z, age * (kit?.Spin ?? 0) * 2 * Math.PI);
        Pos.Pitch = (float)pitch;
        Pos.Yaw = (float)yaw;
        Pos.Roll = (float)roll;
    }
}
