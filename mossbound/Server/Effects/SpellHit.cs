using Mossbound.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Mossbound.Server;

// What a spell reached, and who cast it. Spells never harm other players (no PvP griefing): harmful effects reach
// creatures and the caster (a spell cast at nothing lands on yourself); helpful ones reach whoever was hit.
readonly record struct SpellHit(ICoreServerAPI Sapi, IServerPlayer Caster, Entity? Entity, Vec3d Position, Kit Kit)
{
    public Entity? Harmable => Entity is EntityPlayer player && player != Caster.Entity ? null : Entity;
    public Entity Beneficiary => Entity ?? Caster.Entity;

    // Spells change blocks only where the caster may build.
    public bool CanChange(BlockPos pos) =>
        Sapi.World.Claims.TestAccess(Caster, pos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.Granted;
}

// One effect of a payload. Seconds is how long it lasts, or 0 for the effect's own default.
interface ISpellEffect
{
    void Apply(SpellHit hit, float amount, float seconds);
}
