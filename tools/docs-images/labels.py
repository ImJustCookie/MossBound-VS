"""Turns the renders in out/ (from `dotnet run`) into the docs images: the guide cards
(docs/images/guide/*.png) and the numbered grammar diagram (docs/images/circle-grammar.png).
Run from this folder, after the renderer (see run.sh)."""
import json, math, os
import cairo

DOCS = "../../docs/images"
SCENES = json.load(open("out/scenes.json"))
BG = (0.17, 0.13, 0.11)
TEXT = (0.95, 0.91, 0.82)
DIM = (0.74, 0.70, 0.62)
FAINT = (0.58, 0.55, 0.49)
ACCENT = (0.93, 0.62, 0.22)
GOOD = (0.60, 0.80, 0.45)
BAD = (0.90, 0.40, 0.30)
PAD = 40


# ---------- drawing helpers ----------

def font(g, size, bold=False):
    g.select_font_face("DejaVu Sans", 0, 1 if bold else 0)
    g.set_font_size(size)


def text(g, x, y, s, size=20, colour=TEXT, bold=False, centre=False):
    font(g, size, bold)
    w = g.text_extents(s).x_advance
    g.set_source_rgb(*colour)
    g.move_to(x - w / 2 if centre else x, y)
    g.show_text(s)
    return w


def wrapped(g, x, y, s, width, size=17, colour=DIM, line=23, centre=False):
    """Draws `s` wrapped to `width`; returns the y below the last line."""
    font(g, size)
    lines, cur = [], ""
    for w in s.split():
        t = (cur + " " + w).strip()
        if g.text_extents(t).x_advance > width and cur:
            lines.append(cur)
            cur = w
        else:
            cur = t
    if cur:
        lines.append(cur)
    for l in lines:
        text(g, x, y, l, size, colour, centre=centre)
        y += line
    return y


def label(g, x, y, s, tx, ty, size=16):
    """A label at (x, y) with a leader line to (tx, ty); the text sits on the side away from it."""
    g.set_source_rgb(*ACCENT)
    g.set_line_width(2)
    g.new_path(); g.move_to(x, y - 5); g.line_to(tx, ty); g.stroke()
    g.new_path(); g.arc(tx, ty, 4, 0, 2 * math.pi); g.fill()
    font(g, size, True)
    w = g.text_extents(s).x_advance
    lx = x + 6 if tx <= x else x - w - 6
    g.set_source_rgb(*BG)
    g.rectangle(lx - 4, y - size, w + 8, size + 8); g.fill()
    text(g, lx, y, s, size, ACCENT, True)


def badge(g, x, y, n):
    g.set_source_rgb(*ACCENT)
    g.new_path(); g.arc(x, y, 15, 0, 2 * math.pi); g.fill()
    font(g, 16, True)
    e = g.text_extents(str(n))
    g.set_source_rgb(0.12, 0.08, 0.05)
    g.move_to(x - e.x_advance / 2, y + e.height / 2)
    g.show_text(str(n))


def paint_scene(g, scene, x, y, width, crop=None):
    """Draws out/<scene>.png at (x, y), `width` wide; `crop` = (x0, y0, w) in scene pixels to zoom in."""
    im = cairo.ImageSurface.create_from_png(f"out/{scene}.png")
    x0, y0, cw, ch = (tuple(crop) + (crop[2],))[:4] if crop else (0, 0, im.get_width(), im.get_width())
    k = width / cw
    g.save(); g.rectangle(x, y, width, ch * k); g.clip()
    g.translate(x, y); g.scale(k, k)
    g.set_source_surface(im, -x0, -y0); g.get_source().set_filter(cairo.FILTER_NEAREST); g.paint(); g.restore()
    return k, x0, y0


def slot(scene, key):
    return SCENES[scene]["slots"][key]


# ---------- guide cards ----------

CROPS = {}


def card(name, title, subtitle, panels, img=400, notes=None):
    """panels: (scene, heading, caption, colour, callouts[(lx, ly, text, tx, ty) in scene pixels])."""
    n = len(panels)
    W = max(n * img + (n + 1) * PAD, 1000)
    left = (W - (n * img + (n - 1) * PAD)) / 2
    top = wrapped(cairo.Context(cairo.ImageSurface(cairo.FORMAT_ARGB32, 1, 1)), PAD, 96, subtitle, W - 2 * PAD, 19) + 20
    H = top + img + 150 + (40 if notes else 0)
    s = cairo.ImageSurface(cairo.FORMAT_ARGB32, W, H)
    g = cairo.Context(s)
    g.set_source_rgb(*BG); g.paint()
    text(g, PAD, 58, title, 34, TEXT, True)
    wrapped(g, PAD, 96, subtitle, W - 2 * PAD, 19)
    for i, (scene, heading, caption, colour, callouts) in enumerate(panels):
        x, y = left + i * (img + PAD), top
        k, x0, y0 = paint_scene(g, scene, x, y, img, CROPS.get(scene))
        for (lx, ly, s_, tx, ty) in callouts:
            label(g, x + (lx - x0) * k, y + (ly - y0) * k, s_, x + (tx - x0) * k, y + (ty - y0) * k)
        cy = y + img + 36
        text(g, x + img / 2, cy, heading, 21, colour, True, centre=True)
        wrapped(g, x + img / 2, cy + 30, caption, img - 10, 17, centre=True)
    if notes:
        wrapped(g, PAD, H - 30, notes, W - 2 * PAD, 16, FAINT)
    s.write_to_png(f"{DOCS}/guide/{name}.png")


os.makedirs(f"{DOCS}/guide", exist_ok=True)

# 1. A glyph
P = 448
pc, pr = P / 2, P * 0.38
pt = lambda k: (pc, pc) if k == 6 else (pc + math.cos(math.radians(-90 + 60 * k)) * pr, pc + math.sin(math.radians(-90 + 60 * k)) * pr)
mid = lambda a, b: ((pt(a)[0] + pt(b)[0]) / 2, (pt(a)[1] + pt(b)[1]) / 2)
arc01 = (pc + math.cos(math.radians(-60)) * pr, pc + math.sin(math.radians(-60)) * pr)
g0 = slot("anatomy_canvas", "Centre0")
CROPS["anatomy_canvas"] = (g0[0] - 60, g0[1] - 60, 120)
card("01-glyph", "A glyph",
     "Every glyph is drawn on 7 points: 6 on a circle and 1 in the middle. Lines go along the circle (arcs) or to the "
     "middle (spokes). Marks sit on points. The lines plus the marks are the glyph: what they mean is secret, different in every world.",
     [("anatomy_panel", "On the glyph panel", "Sweep across the points to draw lines. Click a point to cycle its mark.", TEXT,
       [(400, 60, "arc", *arc01), (395, 300, "spoke", *mid(2, 6)), (300, 420, "ring", pt(3)[0], pt(3)[1] + 30),
        (110, 440, "big dot", *pt(5)), (420, 200, "dot", *pt(1)), (150, 250, "double ring", pc - 25, pc),
        (130, 30, "a point", *pt(0))]),
      ("anatomy_canvas", "Written on the circle", "The same glyph once written: the game adds a bold letter circle "
       "and draws the arcs just inside it.", TEXT, [])],
     img=480)

# 2. Strength
sz = lambda k: slot("size", k)
card("02-strength", "Strength: the size of a glyph",
     "Scroll the mouse wheel over a written glyph to write it bigger. Bigger is stronger: a glyph of size 2 counts as "
     "that element written twice.",
     [("size", "Sizes 1, 2 and 3", "The same element three times, at each size. Full strength (3) gets a doubled "
       "letter circle. Moss stops at size 2.", TEXT,
       [(sz("Centre0")[0] + 120, sz("Centre0")[1] - 60, "1", sz("Centre0")[0] + sz("Centre0")[2], sz("Centre0")[1]),
        (sz("Centre1")[0] + 110, sz("Centre1")[1] + 90, "2", sz("Centre1")[0] + sz("Centre1")[2] * 0.7, sz("Centre1")[1] + sz("Centre1")[2] * 0.7),
        (sz("Centre2")[0] - 120, sz("Centre2")[1] + 90, "3", sz("Centre2")[0] - sz("Centre2")[2] * 0.8, sz("Centre2")[1] + sz("Centre2")[2] * 0.6)])],
     img=520)

# 3. Mixing
card("03-mixing", "Mixing elements",
     "The heart is the centre of the circle. Link two element glyphs there (drag from one to the other) and they mix "
     "into something new. What a mix does is only revealed when you cast it.",
     [("mix_apart", "Not linked: two hearts", "Water and Frost stay separate and are cast side by side.", TEXT, []),
      ("mix_linked", "Linked: one heart", "Water + Frost mix into Ice. The mix circle shows the link.", GOOD, []),
      ("mix_opposites", "Opposites: it backfires", "Ember + Frost (or Wind + Ward) cancel out when linked: smoke and "
       "a small hurt.", BAD, [])])

# 4. Casts
t1 = slot("cast_typo", "Inner2")
card("04-casts", "Casts and word circles",
     "A cast is a heart plus everything you reach from it by following links outward. The game draws each cast as a "
     "word circle: a big ring around it that bites into the border. Heavier casts make bigger words.",
     [("cast_one", "One cast", "A heart linked to one glyph of the shape ring: one word circle.", TEXT, []),
      ("cast_two", "One heart, two casts", "Two words from the same heart. Both are cast at once.", GOOD, []),
      ("cast_typo", "A glyph linked to nothing", "No word around it: a harmless typo that does nothing.", BAD,
       [(t1[0] + 140, t1[1] - 60, "typo", t1[0] + t1[2] * 0.8, t1[1] - t1[2] * 0.6)])])

# 5. Source
card("05-source", "Where the life comes from",
     "Every spell costs life. The border shows who pays. Click the border to change it, right-click to go back "
     "(bark and up; moss always uses the medium).",
     [("source_medium", "Plain border: the medium", "The medium's own life, then yours. The default.", TEXT, []),
      ("source_self", "Diamonds: your body", "Long thorn-like diamonds through the border. Hunger first, then health.", TEXT, []),
      ("source_land", "Bramble: the land", "A thorny vine woven through the border. Plants, soil and animals nearby; "
       "careless use leaves the land dead for a season.", TEXT, [])],
     notes="The source is saved and checked today; the cost itself arrives with life essence (planned).")

# 6. Aim
card("06-aim", "Aiming",
     "The outer ring of parchment says what a spell lands on (planned: foes, kin, the closest...). A cast that reaches "
     "it gets an aim line right across the circle, through the last glyph of its aim chain.",
     [("aim_no", "Stops at the shape ring", "No aim: it acts on what you look at, within reach.", TEXT, []),
      ("aim_yes", "Reaches the aim ring", "The thin line crossing the circle marks an aimed cast. It doesn't show the "
       "direction the spell goes.", GOOD, [])],
     img=480)

# 7. Written vs drawn
card("07-written-vs-drawn", "What you write, and what the game draws",
     "Only the glyphs, their sizes, the links and the source change the spell. Everything else is drawn by the game "
     "from those, to help you read a circle at a glance. The server never sees the drawing.",
     [("full_bare", "What you write", "The glyphs as drawn on the panel, their sizes, the links, and the border's "
       "source (diamonds here: drawn from yourself). This is the whole spell.", ACCENT, []),
      ("full", "What the game draws on top", "Letter circles, word circles and their bites, the mix circle, the aim "
       "line, doubled lines for strength.", TEXT, [])],
     img=520)

# 8. The media
card("08-media", "Each medium holds more",
     "Better media add rings and slots, so a circle can say more. Higher tiers are more precise, not more powerful.",
     [("empty_moss", "Moss sigil (I)", "A heart of 2. Sizes up to 2. Always draws on the medium.", TEXT, []),
      ("empty_bark", "Bark strip (II)", "A heart of 3 and a shape ring of 4. Choose the source.", TEXT, []),
      ("empty_parchment", "Hide parchment (III)", "Heart 4, shape 6 and an aim ring of 8.", TEXT, [])],
     notes="Planned: carved stone (IV) adds a rim of 4 for triggers, and circles that call other circles.")


# ---------- the numbered grammar diagram ----------

def diagram():
    W, H = 2000, 1150
    CX, CY = 40, 140
    d = SCENES["diagram"]
    S = d["size"]
    s = cairo.ImageSurface(cairo.FORMAT_ARGB32, W, H)
    g = cairo.Context(s)
    g.set_source_rgb(*BG); g.paint()
    text(g, CX, 62, "Reading a Mossbound circle", 38, TEXT, True)
    text(g, CX, 100, "what it draws on  ·  what it does  ·  how it travels  ·  at whom  ·  when", 20, DIM)
    paint_scene(g, "diagram", CX, CY, S)

    def callout(n, target, offset):
        tx, ty = CX + target[0], CY + target[1]
        bx, by = tx + offset[0], ty + offset[1]
        g.set_source_rgb(*ACCENT); g.set_line_width(2)
        g.new_path(); g.move_to(bx, by); g.line_to(tx, ty); g.stroke()
        g.new_path(); g.arc(tx, ty, 4, 0, 2 * math.pi); g.fill()
        badge(g, bx, by, n)

    c = S / 2
    sl = d["slots"]
    a, b = d["words"][0], d["words"][1]
    water, frost, life, typo = sl["Centre0"], sl["Centre1"], sl["Centre2"], sl["Outer7"]
    i1, o2 = sl["Inner1"], sl["Outer2"]
    mix_x, mix_y = (water[0] + frost[0]) / 2, (water[1] + frost[1]) / 2
    mix_r = math.hypot(water[0] - mix_x, water[1] - mix_y) + d["glyphRadius"] * 1.3
    callout(1, (c + math.cos(-1.05) * S * 0.47, c + math.sin(-1.05) * S * 0.47), (60, -40))
    callout(2, (mix_x + mix_r * math.cos(math.radians(160)), mix_y + mix_r * math.sin(math.radians(160))), (-60, 0))
    callout(3, (water[0] - water[2] * 1.12 * 0.7, water[1] - water[2] * 1.12 * 0.7), (-55, -45))
    callout(4, (a["x"] - a["r"], a["y"]), (-35, -55))
    callout(5, (b["x"] - b["r"] * 0.55, b["y"] + b["r"] * 0.84), (-45, 50))
    callout(6, (330, 140), (-60, 10))
    callout(7, ((i1[0] + o2[0]) / 2, (i1[1] + o2[1]) / 2), (30, -60))
    callout(8, (typo[0] + typo[2] * 0.7, typo[1] + typo[2] * 0.7), (45, 40))
    callout(9, (d["empty"][0], d["empty"][1] - d["glyphRadius"]), (0, -55))
    callout(10, (life[0] + life[2] * 0.3, life[1] + life[2] * 0.95), (25, 60))

    X, WIDTH = 1000, 940

    def item(n, title, body, y):
        badge(g, X + 15, y - 6, n)
        text(g, X + 42, y, title, 20, TEXT, True)
        return wrapped(g, X + 42, y + 26, body, WIDTH - 50) + 10

    def section(title, tier, y):
        w = text(g, X, y, title, 16, ACCENT, True)
        if tier:
            text(g, X + w + 14, y, tier, 16, GOOD, True)
        return y + 30

    y = section("THE CIRCLE", "", 160)
    y = item(1, "Border: the whole spell, and where its life comes from",
             "Plain: the medium pays, then you. Diamonds (shown here): your own body. Bramble: the land around it. "
             "Click the border to choose (bark and up).", y)
    # Swatches cut from real renders of the top of the border.
    for i, (src, name) in enumerate((("source_medium", "medium"), ("source_self", "self"), ("source_land", "land"))):
        sx = X + 42 + i * 150
        paint_scene(g, src, sx, y - 4, 130, (174, 0, 100, 40))
        text(g, sx, y + 70, name, 15, DIM)
    y += 94

    y = section("CENTRE · the heart: WHAT", "Tier I · moss", y)
    y = item(2, "Mix circle", "Elements linked together mix into one heart (Water + Frost = Ice). "
             "Unlinked hearts are cast separately. Doubled when three elements mix.", y)
    y = item(3, "Letter size = strength", "Scroll over a glyph to write it more strongly, 1 to 3. Size is the "
             "element's intensity (and, later, its cost). Full strength is doubled.", y)
    y = section("INNER RING · the shape: HOW", "Tier II · bark", y)
    y = item(4, "Word circle: one cast", "A heart and everything linked outward from it. Heavier casts make "
             "bigger words that bite deeper into the border; long words are doubled.", y)
    y = item(5, "A second cast", "Two words in one circle are cast together (multicast).", y)
    y = section("OUTER RING · the aim: AT WHOM", "Tier III · parchment", y)
    y = item(6, "Aim line", "A cast that reaches the aim ring points outward: its line runs through the end of its "
             "aim chain, right across the circle.", y)
    y = section("WRITING", "", y)
    y = item(7, "Links", "Straight; they join neighbouring rings or neighbouring slots, and bend around glyphs "
             "they don't join.", y)
    y = item(8, "Unlinked glyph", "A harmless typo: it does nothing.", y)
    y = item(9, "Empty slot", "Room to write. Each tier's medium has more rings and slots.", y)
    item(10, "A letter (glyph)", "Arcs, spokes to its centre and marks on its 7 points. Shapes are secret "
         "and different in every world.", y)
    wrapped(g, CX, H - 26, "Coming next: modifier marks on forms (count, reach, duration, speed) and the rim (tier IV "
            "triggers). Forms and aims use element glyphs as placeholders here.", W - 2 * CX, 15, FAINT)
    s.write_to_png(f"{DOCS}/circle-grammar.png")


diagram()
print("wrote docs/images")
