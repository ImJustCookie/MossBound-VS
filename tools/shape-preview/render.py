"""Offline preview of a Vintage Story shape JSON: textured boxes, orthographic, z-buffered.

    python3 -I tools/shape-preview/render.py <shape.json> <out.png> [gui-transform-json]

Without a transform it draws front, three-quarter and side views; with one (the item's guiTransform,
for example '{"rotation":{"x":44,"y":193,"z":24},"origin":[0.5,0.43,0.51]}') it draws the inventory view.
FIXED=1 keeps one scale for every model. Textures: game: from $VINTAGE_STORY, mossbound: from the mod's
assets. Approximate: glowing faces are drawn unshaded, face rotation only in steps of 90 degrees."""
import json, math, os, sys
import numpy as np
from PIL import Image

shape_path, out_path = sys.argv[1:3]
game = os.environ["VINTAGE_STORY"]
mod_assets = os.path.join(os.path.dirname(os.path.abspath(__file__)), "../../mossbound/assets")
localdir = os.path.dirname(os.path.abspath(shape_path))
shape = json.load(open(shape_path))
tw, th = shape.get("textureWidth", 16), shape.get("textureHeight", 16)
sizes = shape.get("textureSizes", {})

def load_tex(key):
    path = shape["textures"].get(key, "")
    if path:
        dom, _, p = path.rpartition(":")
        f = (f"{mod_assets}/{dom}/textures/{p}.png" if dom not in ("", "game")
             else f"{game}/assets/survival/textures/{p}.png")
    else:  # Blockbench left the path empty: use the local file named by the key
        f = f"{localdir}/{key if key.endswith('.png') else key + '.png'}"
    return np.asarray(Image.open(f).convert("RGBA"), dtype=np.float32) / 255

texs = {k: load_tex(k) for k in shape["textures"]}

def rot(ax, deg):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    m = np.eye(4)
    if ax == "x": m[1:3, 1:3] = [[c, -s], [s, c]]
    if ax == "y": m[0, 0], m[0, 2], m[2, 0], m[2, 2] = c, s, -s, c
    if ax == "z": m[0:2, 0:2] = [[c, -s], [s, c]]
    return m

def tr(v):
    m = np.eye(4); m[:3, 3] = v; return m

# Corners of each face in the box's local space (0..size), ordered top-left, top-right, bottom-right, bottom-left as seen from outside.
def face_corners(d, sx, sy, sz):
    return {
        "north": [(sx, sy, 0), (0, sy, 0), (0, 0, 0), (sx, 0, 0)],
        "south": [(0, sy, sz), (sx, sy, sz), (sx, 0, sz), (0, 0, sz)],
        "east":  [(sx, sy, sz), (sx, sy, 0), (sx, 0, 0), (sx, 0, sz)],
        "west":  [(0, sy, 0), (0, sy, sz), (0, 0, sz), (0, 0, 0)],
        "up":    [(0, sy, 0), (sx, sy, 0), (sx, sy, sz), (0, sy, sz)],
        "down":  [(0, 0, sz), (sx, 0, sz), (sx, 0, 0), (0, 0, 0)],
    }[d]

quads = []
def walk(el, parent):
    o = el.get("rotationOrigin", [0, 0, 0])
    m = parent @ tr(o)
    for ax in "xyz":
        a = el.get("rotation" + ax.upper(), 0)
        if a: m = m @ rot(ax, a)
    m = m @ tr([-v for v in o]) @ tr(el["from"])
    sx, sy, sz = (el["to"][i] - el["from"][i] for i in range(3))
    for d, f in (el.get("faces") or {}).items():
        if f.get("enabled") is False or f.get("texture", "#null") == "#null": continue
        key = f["texture"][1:]
        if key not in texs: continue
        u1, v1, u2, v2 = f["uv"]
        uvs = [(u1, v1), (u2, v1), (u2, v2), (u1, v2)]
        r = int(f.get("rotation", 0)) // 90 % 4
        uvs = uvs[r:] + uvs[:r]
        pts = [(m @ np.array([*c, 1.0]))[:3] for c in face_corners(d, sx, sy, sz)]
        quads.append((pts, uvs, key, f.get("glow", 0)))
    for c in el.get("children", []):
        walk(c, m @ np.eye(4))

for e in shape["elements"]:
    walk(e, np.eye(4))

def render(yaw, pitch, size=512, pad=1.2, pre=None):
    view = rot("x", pitch) @ rot("y", yaw) @ tr([-8, -8, -8]) @ (pre if pre is not None else np.eye(4))
    allp = np.array([(view @ np.array([*p, 1]))[:3] for q in quads for p in q[0]])
    lo, hi = allp[:, :2].min(0), allp[:, :2].max(0)
    scale = size / (max(hi - lo) * pad)
    centre = (lo + hi) / 2
    if os.environ.get("FIXED"):  # same scale for every model: a 16-unit block fills the tile
        scale, centre = size / 17, np.zeros(2)
    img = np.zeros((size, size, 4), np.float32)
    zbuf = np.full((size, size), -1e9)
    light = np.array([0.35, 0.85, -0.4]); light /= np.linalg.norm(light)
    for pts, uvs, key, glow in quads:
        P = np.array([(view @ np.array([*p, 1]))[:3] for p in pts])
        n = np.cross(P[1] - P[0], P[3] - P[0])
        if np.linalg.norm(n) < 1e-9: continue
        n /= np.linalg.norm(n)
        if n[2] > 0: continue  # facing away (camera looks down -z of view space toward +z? we keep faces with n.z<=0)
        wn = np.cross(pts[1] - pts[0], pts[3] - pts[0]); wn /= np.linalg.norm(wn) or 1
        shade = 1.0 if glow else 0.55 + 0.45 * max(0.0, float(wn @ light))
        tex = texs[key]; Th, Tw = tex.shape[:2]
        tsx, tsy = sizes.get(key, [tw, th])
        S = np.column_stack([(P[:, 0] - centre[0]) * scale + size / 2, -(P[:, 1] - centre[1]) * scale + size / 2, P[:, 2]])
        UV = np.array([(u * Tw / tsx, v * Th / tsy) for u, v in uvs])
        for tri in ((0, 1, 2), (0, 2, 3)):
            a, b, c = S[list(tri)]; ta, tb, tc = UV[list(tri)]
            x0, x1 = int(max(0, min(a[0], b[0], c[0]))), int(min(size - 1, max(a[0], b[0], c[0]) + 1))
            y0, y1 = int(max(0, min(a[1], b[1], c[1]))), int(min(size - 1, max(a[1], b[1], c[1]) + 1))
            if x1 < x0 or y1 < y0: continue
            den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
            if abs(den) < 1e-12: continue
            xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
            w0 = ((b[1] - c[1]) * (xs - c[0]) + (c[0] - b[0]) * (ys - c[1])) / den
            w1 = ((c[1] - a[1]) * (xs - c[0]) + (a[0] - c[0]) * (ys - c[1])) / den
            w2 = 1 - w0 - w1
            inside = (w0 >= -1e-6) & (w1 >= -1e-6) & (w2 >= -1e-6)
            z = w0 * a[2] + w1 * b[2] + w2 * c[2]
            u = w0 * ta[0] + w1 * tb[0] + w2 * tc[0]
            v = w0 * ta[1] + w1 * tb[1] + w2 * tc[1]
            tu = np.clip(np.floor(u).astype(int), 0, Tw - 1); tv = np.clip(np.floor(v).astype(int), 0, Th - 1)
            col = tex[tv, tu]
            sub = zbuf[y0:y1 + 1, x0:x1 + 1]
            # view space: camera at -z looking toward +z, so smaller z is nearer
            ok = inside & (col[..., 3] > 0.5) & (-z > sub)
            sub[ok] = -z[ok]
            px = img[y0:y1 + 1, x0:x1 + 1]
            px[ok] = np.concatenate([np.clip(col[ok][:, :3] * shade, 0, 1), np.ones((ok.sum(), 1))], 1)
    return img

gui = json.loads(sys.argv[3]) if len(sys.argv) > 3 else None
if gui:  # the item's guiTransform, seen from +z like the inventory camera
    o = [v * 16 for v in gui["origin"]]
    m = tr(o)
    for ax in "xyz": m = m @ rot(ax, gui["rotation"].get(ax, 0))
    m = m @ tr([-v for v in o])
    tiles = [render(180, 0, pre=m)]
else:
    views = [("three-quarter", -60, 20)] if os.environ.get("FIXED") else [("front", 0, 0), ("three-quarter", -35, 25), ("side", -80, 10)]
    tiles = [render(y, p) for _, y, p in views]
bg = np.array([0.24, 0.20, 0.17, 1.0])
out = np.concatenate([t * t[..., 3:4] + bg * (1 - t[..., 3:4]) for t in tiles], 1)
Image.fromarray((out * 255).astype(np.uint8)).save(out_path)
print("quads:", len(quads), "->", out_path)
