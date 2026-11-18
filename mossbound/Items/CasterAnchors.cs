using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Mossbound.Items;

// Where cast effects attach to a caster.
public static class CasterAnchors
{
    // The local player moves in fixed physics steps and is drawn between the last two; effects that follow the
    // raw position jump against the model while moving. Other players are already smoothed by the game.
    public static Vec3d Feet(IWorldAccessor world, EntityAgent holder) =>
        world.Api is ICoreClientAPI capi && holder == capi.World.Player.Entity
            ? capi.World.Player.Entity.CameraPos.SubCopy(capi.World.Player.Entity.CameraPosOffset)
            : holder.Pos.XYZ;

    // The medium above the hand in the hold pose (see tools/pose-preview).
    public static Vec3d Sigil(IWorldAccessor world, EntityAgent holder) =>
        world.Api is ICoreClientAPI capi && holder == capi.World.Player.Entity && capi.World.Player.CameraMode == EnumCameraMode.FirstPerson
            ? FirstPersonSigil(Feet(world, holder), holder)
            : ThirdPersonSigil(Feet(world, holder), holder);

    static Vec3d FirstPersonSigil(Vec3d feet, EntityAgent holder)
    {
        var look = holder.Pos.GetViewVector();
        var right = new Vec3f(-look.Z, 0, look.X).Normalize();
        var up = right.Cross(look);
        var offset = look * 0.55f + right * 0.3f - up * 0.22f;
        return feet.Add(offset.X, holder.LocalEyePos.Y + offset.Y, offset.Z);
    }

    static Vec3d ThirdPersonSigil(Vec3d feet, EntityAgent holder)
    {
        var forward = EntityPos.GetViewVector(0, holder.BodyYaw);
        return feet.Add(forward.X * 0.5 - forward.Z * 0.3, 1.2, forward.Z * 0.5 + forward.X * 0.3);
    }
}
