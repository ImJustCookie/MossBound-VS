"""Offline preview: seraph posed by keyframe values, holding an item via tpHandTransform (EntityShapeRenderer math).
Usage: python3 -I pose.py config.json out.png
config: {pose:{Element:{rotationX..,offsetX..}}, item:shape.json, transform:{translation,rotation,origin,scale}, extraRot:[x,y,z]}"""
import json, math, sys, os
import numpy as np
from PIL import Image
cfg = json.load(open(sys.argv[1])); out = sys.argv[2]
VS = os.environ["VINTAGE_STORY"]; MOD = os.path.join(os.path.dirname(os.path.abspath(__file__)), "../../mossbound/assets/mossbound")

def rot(ax, deg):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a); m = np.eye(4)
    if ax == "x": m[1:3, 1:3] = [[c, -s], [s, c]]
    if ax == "y": m[0, 0], m[0, 2], m[2, 0], m[2, 2] = c, s, -s, c
    if ax == "z": m[0:2, 0:2] = [[c, -s], [s, c]]
    return m
def rxyz(x, y, z): return rot("x", x) @ rot("y", y) @ rot("z", z)
def tr(v): m = np.eye(4); m[:3, 3] = v; return m
def sc(v): m = np.eye(4); m[0, 0], m[1, 1], m[2, 2] = v; return m

def corners(d, sx, sy, sz):
    return {"north": [(sx, sy, 0), (0, sy, 0), (0, 0, 0), (sx, 0, 0)], "south": [(0, sy, sz), (sx, sy, sz), (sx, 0, sz), (0, 0, sz)],
            "east": [(sx, sy, sz), (sx, sy, 0), (sx, 0, 0), (sx, 0, sz)], "west": [(0, sy, 0), (0, sy, sz), (0, 0, sz), (0, 0, 0)],
            "up": [(0, sy, 0), (sx, sy, 0), (sx, sy, sz), (0, sy, sz)], "down": [(0, 0, sz), (sx, 0, sz), (sx, 0, 0), (0, 0, 0)]}[d]

quads = []  # (world pts, uvs or None, tex or colour, glow)
anchor = {}
pose = cfg.get("pose", {})
BODY = {"LowerTorso","UpperTorso","UpperArmR","LowerArmR","UpperArmL","LowerArmL","Neck","Head","UpperFootL","LowerFootL","UpperFootR","LowerFootR"}
def body(el, parent):
    p = pose.get(el["name"], {})
    o = np.array(el.get("rotationOrigin", [0, 0, 0])) / 16
    off = np.array([p.get("offset" + a, 0) for a in "XYZ"]) / 16
    m = parent @ tr(o) @ rxyz(*[el.get("rotation" + a, 0) + p.get("rotation" + a, 0) for a in "XYZ"]) @ tr(np.array(el["from"]) / 16 + off - o)
    for ap in el.get("attachmentpoints") or []:
        anchor[ap["code"]] = (m, ap)
    sx, sy, sz = ((el["to"][i] - el["from"][i]) / 16 for i in range(3))
    if el["name"] in BODY:
        colr = (0.85, 0.55, 0.45) if el["name"].endswith("R") else (0.7, 0.7, 0.75)
        if el["name"] == "Head": colr = (0.9, 0.8, 0.6)
        for d in ("north", "south", "east", "west", "up", "down"):
            quads.append(([(m @ np.array([*c, 1]))[:3] for c in corners(d, sx, sy, sz)], None, colr, 0))
    for c in el.get("children") or []: body(c, m)
seraph = json.load(open(f"{VS}/assets/game/shapes/entity/humanoid/seraph-faceless.json"))
for e in seraph["elements"]: body(e, np.eye(4))

# item, per EntityShapeRenderer.RenderItem
item = json.load(open(cfg["item"])); T = cfg["transform"]
am, ap = anchor["RightHand"]
o = np.array([T["origin"][a] for a in "xyz"]); t = np.array([T["translation"][a] for a in "xyz"]); s = T["scale"]
r = [T["rotation"][a] + float(ap["rotation" + a.upper()]) + cfg.get("extraRot", [0, 0, 0])[i] for i, a in enumerate("xyz")]
apos = np.array([float(ap["pos" + a]) for a in "XYZ"]) / 16
im = am @ tr(o) @ sc([s] * 3) @ tr(apos + t) @ rxyz(*r) @ tr(-o)
def load_tex(path):
    dom, _, p = path.rpartition(":")
    f = f"{MOD}/textures/{p}.png" if dom == "mossbound" else f"{VS}/assets/survival/textures/{p}.png"
    return np.asarray(Image.open(f).convert("RGBA"), dtype=np.float32) / 255
texs = {k: load_tex(v) for k, v in item["textures"].items()}
sizes = item.get("textureSizes", {})
def itemwalk(el, parent):
    o2 = np.array(el.get("rotationOrigin", [0, 0, 0])) / 16
    m = parent @ tr(o2) @ rxyz(*[el.get("rotation" + a, 0) for a in "XYZ"]) @ tr(np.array(el["from"]) / 16 - o2)
    sx, sy, sz = ((el["to"][i] - el["from"][i]) / 16 for i in range(3))
    for d, f in (el.get("faces") or {}).items():
        k = f.get("texture", "#null")[1:]
        if k not in texs or f.get("enabled") is False: continue
        u1, v1, u2, v2 = f["uv"]; uvs = [(u1, v1), (u2, v1), (u2, v2), (u1, v2)]
        rr = int(f.get("rotation", 0)) // 90 % 4; uvs = uvs[rr:] + uvs[:rr]
        tw, th = sizes.get(k, [16, 16]); tex = texs[k]
        uvs = [(u * tex.shape[1] / tw, v * tex.shape[0] / th) for u, v in uvs]
        quads.append(([(m @ np.array([*c, 1]))[:3] for c in corners(d, sx, sy, sz)], uvs, tex, f.get("glow", 0)))
    for c in el.get("children") or []: itemwalk(c, m)
for e in item["elements"]: itemwalk(e, im)
# report the anchor frame axes in world (y up, seraph faces -x, right hand at -z)
fr = am @ rxyz(*[float(ap["rotation" + a]) for a in "XYZ"])
print("hand frame axes world:", {a: np.round(fr[:3, i], 2).tolist() for i, a in enumerate("xyz")})

def render(yaw, pitch, size=420, centre=None, zoom=1.0):
    view = rot("x", pitch) @ rot("y", yaw)
    W = 2.2 / zoom; img = np.zeros((size, size, 3)) + [0.24, 0.20, 0.17]; zb = np.full((size, size), -1e9)
    L = np.array([0.4, 0.8, -0.45]); L /= np.linalg.norm(L)
    c0 = view[:3, :3] @ (centre if centre is not None else np.array([0.5, 1.1, 0.5]))
    for pts, uvs, tex, glow in quads:
        P = np.array([view[:3, :3] @ p for p in pts]) - c0
        n = np.cross(P[1] - P[0], P[3] - P[0])
        if np.linalg.norm(n) < 1e-12 or n[2] > 0: continue
        wn = np.cross(pts[1] - pts[0], pts[3] - pts[0]); wn /= np.linalg.norm(wn)
        shade = 1.0 if glow else 0.55 + 0.45 * max(0.0, float(wn @ L))
        S = np.column_stack([P[:, 0] / W * size + size / 2, -P[:, 1] / W * size + size / 2, P[:, 2]])
        for tri in ((0, 1, 2), (0, 2, 3)):
            a, b, c = S[list(tri)]
            x0, x1 = int(max(0, min(a[0], b[0], c[0]))), int(min(size - 1, max(a[0], b[0], c[0]) + 1))
            y0, y1 = int(max(0, min(a[1], b[1], c[1]))), int(min(size - 1, max(a[1], b[1], c[1]) + 1))
            if x1 < x0 or y1 < y0: continue
            den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
            if abs(den) < 1e-12: continue
            xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
            w0 = ((b[1] - c[1]) * (xs - c[0]) + (c[0] - b[0]) * (ys - c[1])) / den
            w1 = ((c[1] - a[1]) * (xs - c[0]) + (a[0] - c[0]) * (ys - c[1])) / den
            w2 = 1 - w0 - w1
            ins = (w0 >= -1e-6) & (w1 >= -1e-6) & (w2 >= -1e-6)
            z = w0 * a[2] + w1 * b[2] + w2 * c[2]
            if uvs is None:
                col = np.broadcast_to(np.array(tex) * shade, xs.shape + (3,)); alpha = np.ones(xs.shape, bool)
            else:
                ta, tb, tc = [np.array(uvs[i]) for i in tri]
                u = w0 * ta[0] + w1 * tb[0] + w2 * tc[0]; v = w0 * ta[1] + w1 * tb[1] + w2 * tc[1]
                px = tex[np.clip(v.astype(int), 0, tex.shape[0] - 1), np.clip(u.astype(int), 0, tex.shape[1] - 1)]
                col = px[..., :3] * shade; alpha = px[..., 3] > 0.5
            sub = zb[y0:y1 + 1, x0:x1 + 1]; ok = ins & alpha & (-z > sub)
            sub[ok] = -z[ok]; img[y0:y1 + 1, x0:x1 + 1][ok] = np.clip(col[ok], 0, 1)
    return img
hand = (am @ np.array([0, 0, 0, 1]))[:3]
tiles = [render(90, 10), render(150, 15), render(-140, 25), render(150, 35, centre=hand, zoom=3)]
Image.fromarray((np.concatenate(tiles, 1) * 255).astype(np.uint8)).save(out)
print("->", out)
inv = np.linalg.inv(im)
iq = [q for q in quads if q[1] is not None]
P = np.array([p for q in iq for p in q[0]]); L = np.array([(inv @ np.array([*p, 1]))[:3] for p in P])
print("item model bbox (block units):", np.round(L.min(0), 3).tolist(), np.round(L.max(0), 3).tolist())
hc = (anchor["RightHand"][0] @ np.array([0.0625, 0.0625, 0.0625, 1]))[:3]
print("item world centre - hand centre:", np.round(P.mean(0) - hc, 3).tolist())
