using System.Collections.Generic;
using System.Linq;
using Mossbound.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Mossbound.Client;

// The sounds of every cast this client sees: a hum at the caster while it charges, faded out when it ends, and a burst
// when it is released. Driven by GlyphRings, which already follows every caster's charge.
sealed class CastSounds(ICoreClientAPI capi)
{
    const float Range = 24;
    const float FadeSeconds = 0.3f;

    readonly Dictionary<long, (ILoadedSound Hum, CastStyle Style)> hums = new();

    public void Start(EntityAgent caster, CastStyle style)
    {
        Stop(caster.EntityId);
        var hum = capi.World.LoadSound(new SoundParams
        {
            Location = new AssetLocation(style.ChargeSound),
            ShouldLoop = true,
            DisposeOnFinish = false,
            Position = caster.Pos.XYZFloat,
            Range = Range,
            Volume = 0.5f,
        });
        if (hum == null) return;
        hum.Start();
        hums[caster.EntityId] = (hum, style);
    }

    public void Follow(EntityAgent caster)
    {
        if (hums.TryGetValue(caster.EntityId, out var cast)) cast.Hum.SetPosition(caster.Pos.XYZFloat);
    }

    public void End(EntityAgent caster, bool released)
    {
        if (!hums.TryGetValue(caster.EntityId, out var cast)) return;
        Stop(caster.EntityId);
        if (released) capi.World.PlaySoundAt(new AssetLocation(cast.Style.ReleaseSound), caster.Pos.X, caster.Pos.InternalY + 1, caster.Pos.Z, null, true, Range);
    }

    void Stop(long casterId)
    {
        if (!hums.Remove(casterId, out var cast)) return;
        cast.Hum.FadeOutAndStop(FadeSeconds);
        capi.Event.RegisterCallback(_ => cast.Hum.Dispose(), (int)(FadeSeconds * 1000) + 100);
    }

    public void StopAll()
    {
        foreach (long id in hums.Keys.ToList()) Stop(id);
    }
}
