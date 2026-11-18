using System;
using System.Collections.Generic;
using Cairo;
using Mossbound.Core;
using Vintagestory.API.Client;

namespace Mossbound.Client;

// Each glyph figure as a small, crisp white pixel sprite: no hand-drawn wobble, no antialiasing, no smoothing.
sealed class GlyphSprites : IDisposable
{
    public const int MaxCached = 64;
    const int Pixels = 24;
    static readonly InkStyle clean = new(new(1, 1, 1), 0, 0, 1, 1, 0, 1);

    readonly ICoreClientAPI capi;
    readonly Dictionary<(uint, uint), LoadedTexture> cache = new();

    public GlyphSprites(ICoreClientAPI capi) => this.capi = capi;

    public int Count => cache.Count;

    public LoadedTexture For(PlacedGlyph glyph)
    {
        var key = (glyph.Lines, glyph.Marks);
        if (!cache.TryGetValue(key, out var texture)) cache[key] = texture = Draw(glyph.Figure);
        return texture;
    }

    LoadedTexture Draw(GlyphFigure figure)
    {
        using var surface = new ImageSurface(Format.Argb32, Pixels, Pixels);
        using (var ctx = new Context(surface))
        {
            ctx.Antialias = Antialias.None;
            GlyphPainter.DrawGlyph(ctx, Pixels / 2.0, Pixels / 2.0, Pixels * 0.4, 0, figure, 1.5, 0, clean, LetterCircle.Bold);
        }
        var texture = new LoadedTexture(capi);
        capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref texture);
        return texture;
    }

    public void Clear()
    {
        foreach (var texture in cache.Values) texture.Dispose();
        cache.Clear();
    }

    public void Dispose() => Clear();
}
