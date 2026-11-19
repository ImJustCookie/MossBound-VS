using System.Collections.Generic;
using System.Linq;
using Mossbound.Core;
using Mossbound.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Mossbound.Client;

// The glyph rings of every caster this client renders, started and ended from each held medium's cast state, with
// the cast's sounds.
sealed class GlyphRings : IRenderer
{
    const int MaxEnding = 16;
    const long StaleMs = 300;
    // A cast must read as stopped this long before it ends: right after a camera switch the cast animation
    // flickers between stopped and running, and ending on the first flicker restarted the rings every frame.
    const long EndAfterMs = 150;

    readonly ICoreClientAPI capi;
    readonly GlyphSprites sprites;
    readonly GlyphQuads quads;
    readonly CastSounds sounds;
    readonly Dictionary<long, CastRings> live = new();
    readonly List<CastRings> ending = new();
    readonly List<CastRings> stale = new();

    public double RenderOrder => 0.5;
    public int RenderRange => 64;

    public GlyphRings(ICoreClientAPI capi)
    {
        this.capi = capi;
        sprites = new GlyphSprites(capi);
        quads = new GlyphQuads(capi);
        sounds = new CastSounds(capi);
        capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "mossbound-glyph-rings");
    }

    // Called every frame the medium is drawn in a hand.
    public void Update(EntityAgent holder, ItemStack medium, CastStyle style, float charge, bool casting, float castSeconds)
    {
        live.TryGetValue(holder.EntityId, out var rings);
        long now = capi.ElapsedMilliseconds;
        if (!casting)
        {
            if (rings != null && now - rings.LastCastingMs > EndAfterMs)
                End(rings, rings.Charge >= CastRings.ReadyCharge ? RingsEnding.Release : RingsEnding.Cancel);
            return;
        }
        rings ??= Start(holder, medium, style, castSeconds);
        if (rings == null) return;
        rings.Charge = charge;
        rings.LastCastingMs = now;
    }

    CastRings? Start(EntityAgent holder, ItemStack medium, CastStyle style, float castSeconds)
    {
        var circle = ItemCircleMedium.ReadCircle(medium);
        if (circle == null || circle.Glyphs.Count == 0 || style.GlyphMax <= 0) return null;
        if (sprites.Count + circle.Glyphs.Count > GlyphSprites.MaxCached) ClearAll();

        var groups = Capped(CircleStructure.CastGroups(circle, CastRings.MaxRings), style.GlyphMax);
        sounds.Start(holder, style);
        return live[holder.EntityId] = new CastRings(holder, groups, style, castSeconds, sprites);
    }

    // At most `max` glyphs in all, dropped from the last rings first.
    static List<List<PlacedGlyph>> Capped(List<List<PlacedGlyph>> groups, int max)
    {
        var capped = new List<List<PlacedGlyph>>();
        int left = max;
        foreach (var group in groups.TakeWhile(_ => left > 0))
        {
            capped.Add(group.Take(left).ToList());
            left -= capped[^1].Count;
        }
        return capped;
    }

    void End(CastRings rings, RingsEnding how)
    {
        live.Remove(rings.Holder.EntityId);
        rings.End(how);
        sounds.End(rings.Holder, how == RingsEnding.Release);
        if (ending.Count >= MaxEnding) ending.RemoveAt(0);
        ending.Add(rings);
    }

    public void OnRenderFrame(float dt, EnumRenderStage stage)
    {
        if (live.Count == 0 && ending.Count == 0) return;
        EndStale();
        ending.RemoveAll(r => r.Finished);

        quads.Begin();
        foreach (var rings in live.Values)
        {
            rings.Draw(quads, CasterAnchors.Feet(capi.World, rings.Holder), dt);
            sounds.Follow(rings.Holder);
        }
        foreach (var rings in ending) rings.Draw(quads, CasterAnchors.Feet(capi.World, rings.Holder), dt);
        quads.End();
    }

    // Casters who died or put the medium away.
    void EndStale()
    {
        long now = capi.ElapsedMilliseconds;
        stale.AddRange(live.Values.Where(r => !r.Holder.Alive || now - r.LastCastingMs > StaleMs));
        foreach (var rings in stale) End(rings, RingsEnding.Cancel);
        stale.Clear();
    }

    void ClearAll()
    {
        live.Clear();
        ending.Clear();
        sounds.StopAll();
        sprites.Clear();
    }

    public void Dispose()
    {
        ClearAll();
        quads.Dispose();
        capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
    }
}
