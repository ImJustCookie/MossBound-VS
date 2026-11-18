using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Mossbound.Client;

// Draws sprites as tinted, self-lit quads in the world with the standard shader, the way the game's signs draw
// their text: depth-tested, so blocks and the player hide them. Call Begin, then Draw for each, then End.
sealed class GlyphQuads : IDisposable
{
    static readonly Vec4f fullLight = new(1, 1, 1, 1);

    readonly ICoreClientAPI capi;
    readonly MeshRef quad;
    readonly Matrixf model = new();
    readonly Vec4f tint = new();
    IStandardShaderProgram? prog;

    public GlyphQuads(ICoreClientAPI capi)
    {
        this.capi = capi;
        var mesh = QuadMeshUtil.GetQuad(); // 2x2, facing +Z
        mesh.Uv = new float[] { 1, 1, 0, 1, 0, 0, 1, 0 }; // flipped for a Cairo texture, as signs do
        mesh.Rgba = new byte[16];
        Array.Fill(mesh.Rgba, byte.MaxValue);
        quad = capi.Render.UploadMesh(mesh);
    }

    public void Begin()
    {
        var render = capi.Render;
        render.GlToggleBlend(true, EnumBlendMode.Standard);
        render.GlDisableCullFace();
        var camera = capi.World.Player.Entity.CameraPos;
        prog = render.PreparedStandardShader((int)camera.X, (int)camera.Y, (int)camera.Z);
        prog.ViewMatrix = render.CameraMatrixOriginf;
        prog.ProjectionMatrix = render.CurrentProjectionMatrix;
        prog.NormalShaded = 0;
        prog.ExtraGodray = 0;
        prog.SsaoAttn = 0;
        prog.AlphaTest = 0.05f;
        prog.OverlayOpacity = 0;
        prog.ExtraGlow = 255;
        prog.RgbaLightIn = fullLight;
    }

    // A quad of `size` blocks centred on (x, y, z), its front turned toward `facing` (radians around Y, 0 = +X).
    public void Draw(LoadedTexture sprite, Vec4f colour, float alpha, double x, double y, double z, float facing, float size)
    {
        if (prog == null) return;
        var camera = capi.World.Player.Entity.CameraPos;
        prog.Tex2D = sprite.TextureId;
        prog.RgbaTint = tint.Set(colour.R, colour.G, colour.B, alpha);
        prog.ModelMatrix = model.Identity()
            .Translate(x - camera.X, y - camera.Y, z - camera.Z)
            .RotateY(MathF.PI / 2 - facing)
            .Scale(size / 2, size / 2, size / 2)
            .Values;
        capi.Render.RenderMesh(quad);
    }

    public void End()
    {
        prog?.Stop();
        prog = null;
        capi.Render.GlEnableCullFace();
        capi.Render.GlToggleBlend(true, EnumBlendMode.Standard);
    }

    public void Dispose() => quad.Dispose();
}
