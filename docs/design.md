# Mossbound design

Mossbound is a druidic spell-writing mod for Vintage Story. You discover a hidden script in the world, write spells as magic circles in the style of Circular Gallifreyan, and cast them as quiet, grounded folk magic.

What exists today: the hidden script, the circle model and its rules, element mixing, the inscription window and its look, and a first round of casting (Touch only, a few effects, simple mishaps). Everything else here is design that hasn't been built yet. The roadmap tracks it milestone by milestone. For a step-by-step guide to writing circles, from a first moss sigil to a full parchment circle, see `writing-guide.md`.

## 1. The feel

> You find a stone half-swallowed by moss. Under the lichen there is a pattern: a ring, three marks, a line that curls into a spiral. It means nothing. You make a charcoal rubbing anyway.
>
> Weeks later, by candlelight, you scratch the same marks into a strip of bark. The candle flame leans toward you. The flame was what moved, not the air.
>
> You write it down: *"The curl-mark pulls things toward the one who reads it. Or toward warmth? Test with a cold object."*

Mossbound is not a power fantasy. It should feel like being a hedge-witch or an old-world druid: patient, observant, a little afraid of what you don't understand. Magic is quiet, local and slow, and it makes your homestead feel alive. Above all it is a language you are decoding, and your notebook is your real progression.

It should feel like folk magic, field notes, wonder and slow personal craft, with consequences. It should not feel like fireballs, mana bars, DPS numbers, skill trees, or something you solve with a wiki.

Influences:
- Hex Casting (Minecraft): writing spells by drawing patterns, with real logic and targeting.
- Magicka 2: a few basic elements that mix into new ones, and opposites that cancel.
- Noita: combining forms, modifiers and triggers into surprising spells, and multicast.
- Circular Gallifreyan: the look of the script. Letters are circles on a line, with dots and rings, and sentences are nested circles.

Every spell costs real life energy, taken from the medium, from your own body or from the living things around you. Used carelessly, magic drains the land until it is desolate, or kills the caster.

## 2. The core loop

1. Discover glyphs in the world: listen to nature, find lichen fragments, follow resonance hints. Never by brute force.
2. Write a circle on a medium (moss, bark, parchment, stone).
3. Cast it and pay for it in life essence, from the medium, your body or the land. Watch what happens. Effects show in the world, never as numbers.
4. Record what you learned in your notebook, name glyphs yourself, and share with other players.
5. Move on to better media, which unlock more rings and so more expressive spells.

## 3. The hidden script

### Fixed meanings, secret shapes (built)
Meanings, rules and effects are the same in every world. Frost is always frost and the grammar never changes. Only the shapes of the glyphs are generated, from the world seed, so players have to rediscover which pattern means what. On a server everyone shares one script, which makes knowledge social, but a wiki can't give you your world's glyphs.

The script lives on the server only. Clients never receive it, so it can't be datamined.

### A glyph is a rosette (built)
- A glyph is a small circle with 7 points: 6 on the rim and 1 in the centre.
- Lines are either arcs along the rim between neighbouring points, or spokes to the centre. As in Gallifreyan, strokes never cross except at the centre.
- Marks sit on points: a dot, a big dot, a ring or a double ring. Rings cut the lines they sit on, like a Gallifreyan vowel on its line.
- A glyph is its set of lines plus the mark on each point. Stroke order doesn't matter, but orientation does: point 0 faces outward on the circle. Recognition is exact, never fuzzy.
- Generated glyphs have 4 to 6 connected lines and 2 or 3 marks.

### Family marks (built)
All glyphs of one kind (elements, forms, aim glyphs and so on) share a family mark: the same mark on the same point. It is picked at random per world, differs for each kind, and never changes once a world exists. Players learn to tell a glyph's kind from its shape ("it has the centre dot, so it's an element") while its meaning is still unknown.

## 4. The circle: tiers and grammar

![Reading a Mossbound circle](images/circle-grammar.png)

### Each medium unlocks one more ring
Progression is about what a circle can say, not about raw power. Higher tiers are more precise and last longer; they don't hit harder.

| Tier | Medium | Rings | What you can say | Lifetime |
|---|---|---|---|---|
| I | Moss sigil: living moss pressed on stone, written with a stick | Heart (centre, 2 slots) | "Ember", "Life + Water" | One use, wilts in a day |
| II | Bark strip: stripped from a log, flattened and trimmed, carved at the scribing stump | Heart (3 slots) + Shape (inner ring, 4) | "Ember as a thrown seed" | One use |
| III | Hide parchment (tanning) or fired clay tablet (pottery) | Heart (4) + Shape (6) + Aim (outer ring, 8) | "...at the closest foe", "...on my kin" | Parchment: a few uses. Clay: reusable after a recharge |
| IV | Carved stone or a circle on the ground (chisel, chalk, ash) | + Rim (triggers, nested circles as functions) | "...when something enters, roots burst there" | Permanent, placed in the world |

Tier I is the sandbox for discovery: cheap and quick, for learning what elements and mixes do. The medium limits complexity, not knowledge. You may know a trigger glyph from a ruin long before you can carve stone.

### Making the media in the world
This takes after the immersive crafting mods (sawing and chopping in the world, spinning, weaving) and after vanilla knapping and clay forming. Work happens in the world, step by step, with tools in hand, and the workpiece visibly changes at each step.

The rule: prepare materials and tools, then work at the tier's workstation. Tier I has no workstation, only a stone surface. Each tier from II up has one workstation, more elaborate at each tier.

**Tier I: moss sigil** (no workstation)
1. Scrape: hold right-click with a knife on a mossy block (mossy cobblestone, lichen). The moss peels off with particles and sound, and the block loses its moss. You get living moss, which is perishable and dries out in a few days.
2. Lay: put 5 living moss on top of any stone block: rock, cobblestone, slab, polished stone, bricks, anything whose material is stone.
3. Flatten: hold right-click with a loose stone in hand, once per stage. The moss goes from a heap to lumpy, then pressed, then a flat sheet.
4. Carve: cut the flat sheet to shape with a knife, as in knapping (see Carving below). This gives a moss sigil. It wilts in about a day (the game's freshness bar), and a wilted sigil can't be cast.
5. Inscribe: with the sigil lying on a stone block, sneak and right-click it with a stick. The stick is the stylus, and the inscription window opens.
6. Cast: pick the sigil back up and hold right-click.

**Tier II: bark strip** (workstation: the scribing stump)
1. Strip: hold right-click with a knife on a placed log (not a standing tree). Fresh, curled bark comes off, 2 or 3 strips per log, and the log becomes the vanilla debarked log, so nothing is wasted.
2. Build the stump, once: stand a debarked log upright and chop it with an axe in stages, from a rough cut to a flat-topped scribing stump with a groove for the stylus.
3. Flatten: put fresh bark on the stump and hold right-click with a loose stone, once per stage. The bark goes from curled to half-open, then pressed, then a flat sheet.
4. Carve: cut the sheet to shape on the stump with a knife, as in knapping. This gives a bark strip you can write on.
5. Make the stylus: whittle vanilla charcoal with a knife on the stump to get a charcoal stick. It wears down with use, and its model gets shorter.
6. Inscribe: with the charcoal stick in the stump's groove, right-click the bark strip on the stump. The window opens, and each inscription wears the stick down.
7. Cast: pick the strip back up and hold right-click.

The scribing stump has visible slots. The top holds the medium being worked, the groove holds the tool, and later a spot on the side holds consumables such as an ink pot or a tuft of moss. Right-click with an item to put it in its slot, and with an empty hand to take the last item back.

**Carving** works like vanilla knapping, so players recognise it straight away:
- The flat sheet becomes a 10×10 voxel grid. You pick a pattern (moss sigil, bark strip) and its outline shows on the sheet.
- Each knife click removes one voxel. Bits cut off from the main piece fall away.
- Matching the pattern produces the item. Offcuts are lost; there are no scraps.
- Patterns are JSON recipes in the vanilla knapping format, so new shapes are pure data.

**Showing progress**
- Stage models are what persists: each flattening stage is its own model, saved in the world.
- Block animations play while you work: a short keyframe animation such as bark peeling off the log or moss squashing under the stone. It is stretched to the length of the step and nearby players see it. Letting go early snaps it back. On vanilla blocks (logs, mossy cobblestone) it plays as an overlay on the real block, so vanilla blocks need no extra data.
- Player animations come from the game where they fit (`knifescrape`, `knap`, `knifecut`, `axechop`). Custom ones can come later.
- Until the art exists, a step falls back to particles and sound, so everything works before the models do.

**Under the hood.** Three reusable pieces keep later tiers mostly in JSON:
- Work steps: hold-to-work steps with a tool, a hold time, sound, particles, player and block animations, and a result (a new block, an item or drops). Scraping, stripping, chopping, whittling and every flattening stage use them.
- Carving surface: the knapping-style voxel sheet, knife only, with patterns as recipes.
- Station: a block with named, visible slots that accept specific items, like the scribing stump.

All three are server-authoritative. Progress lives on the server, clicks are validated and rate-limited, only one player works a piece at a time, and nothing ticks outside an active hold.

### Names
One name per part, used the same way in the game, the guide and the code.

**Written** (these change the spell):

| Name | What it is | In code |
|---|---|---|
| medium | What a circle is written on: moss sigil, bark strip, hide parchment, carved stone | `ItemCircleMedium` |
| tier | The medium's level, I to IV. Sets the rings, slots and limits | `CircleTier` |
| circle | Everything written on one medium | `Circle` (saved, sent), `CircleDraft` (being edited) |
| ring | A row of slots: the heart, the shape ring, the aim ring, the rim | `CircleRing`: `Heart`, `Shape`, `Aim`, `Rim` |
| slot | The place for one glyph on a ring | `SlotKey` |
| glyph | One written word: its lines and marks | `GlyphFigure` |
| point | One of a glyph's 7 points: 6 on its rim, 1 in its middle | |
| line | An arc (along the rim, between neighbouring points) or a spoke (to the middle) | `GlyphFigure.Lines` |
| mark | A dot, big dot, ring or double ring on a point | `Mark` |
| size | How strongly a glyph is written, 1 to 3 | `PlacedGlyph.Size` |
| link | A connection between two glyphs | `CircleLink` |
| source | Where the spell takes its life: medium, self or land | `LifeSource` |
| heart | Heart-ring glyphs linked together. One heart mixes its elements | `CircleStructure.Hearts` |
| cast | A heart plus every glyph reached from it by following links outward | `Cast` |
| form | Shape-ring glyph: how a cast travels (Touch, Seed, Ring...) *(planned)* | |
| behaviour | Shape-ring glyph linked to a form, changing how it acts (Seeking, Delayed...) *(planned)* | |
| aim glyph | Aim-ring glyph: what a cast lands on (Me, Foes, Closest...) *(planned)* | |
| aim chain | A cast's aim glyphs, read link by link | `Cast.AimEnd` (its last glyph) |
| trigger | Rim glyph: when a nested circle fires *(planned)* | |
| mishap | What a broken circle does instead of its spell | `Mishap` |

**Drawn** by the game from what is written (they never change the spell):

| Name | What it is | In code |
|---|---|---|
| border | The circle's outer line. Its style shows the source: plain, diamonds or bramble | `CirclePainter.Border` |
| letter circle | The bold circle drawn around each written glyph | `GlyphPainter.Rosette` |
| mix circle | The circle around a heart of two glyphs or more | `CircleStructure.Mixes` |
| word circle | The big circle around one cast | `WordCircle.Place` |
| bite | Where a word circle cuts into the border | `CirclePainter.Border` |
| aim line | The line across the circle for a cast that reaches the aim ring | `CirclePainter` |
| doubled line | A thin second line beside a strong one | `InkStyle.Doubles` |

### Reading a circle

```
heart       WHAT       elements; linked ones mix       tier I
shape ring  HOW        forms and their behaviours      tier II
aim ring    AT WHOM    aim glyphs, read as a chain     tier III
rim         WHEN       triggers and nested circles     tier IV
border      WHO PAYS   medium, self or land            tier II
```

The rules:
1. A link joins neighbouring rings, or neighbouring slots on one ring. It never skips a ring.
2. Heart glyphs linked together form one heart and mix. Unlinked heart glyphs are separate hearts.
3. A cast is a heart plus everything reached from it by following links outward. A circle can hold several casts, all cast together.
4. A glyph's size is its strength. In a heart, a glyph of size N counts as its element written N times.
5. A glyph in no cast is a harmless typo.
6. The source is chosen on the border, not written as a glyph: medium (the default, and the only one on moss), self or land.
7. *(Planned)* A form carries its cast, behaviours change the form they are linked to, the aim chain says what the cast lands on, and a trigger fires a nested circle.

There are no numbers and no stack. Amounts come from sizes (and later modifier marks), positions from aim glyphs.

### What is written, what is drawn
The server reads only what is written: the glyphs, the rings they sit on, their sizes, the links and the source. Everything in the second table above is drawn by the client from those, so a circle reads at a glance:
- a word circle grows with its cast's total size and bites the border;
- a mix circle sits around each heart of two glyphs or more;
- an aim line runs from a cast's heart through the end of its aim chain, across the circle;
- doubled lines mark size 3, a three-element mix, a cast of three glyphs or more past its heart, and the border from parchment up. Never on moss, whose coarse ink can't hold them.

`writing-guide.md` shows each of these with pictures.

### Heart: elements and mixing
The rules are built; the effects exist only at test scale.

| Element | On its own |
|---|---|
| Water | wets, puts out fire, fills |
| Ember | warms, dries, ignites |
| Frost | chills, slows, preserves |
| Stone | hardens, pushes up earth |
| Life | grows, heals, soothes |
| Rot | decays, composts, weakens |
| Wind | pushes, carries, scatters |
| Ward | shields, stills, holds |

| Mix | Result |
|---|---|
| Water + Frost | Ice: freezes water, slick ground, preserves |
| Water + Ember | Steam: fog, scalding, softens |
| Water + Life | Rain: tends crops, slow healing |
| Water + Rot | Mire: bog, sinks and slows |
| Life + Stone | Root: entangles, binds to the ground |
| Life + Rot | Spore: fungus, mushrooms, a sleep cloud |
| Life + Ward | Bark: bark-skin, protection |
| Frost + Wind | Gale: cold gust, knockback |
| Ember + Wind | Smoke: blinds, calms bees, cures meat |
| Ember + Stone | Coal: lasting heat |
| Stone + Ward | Wall: raises earth or stone |
| Rot + Wind | Blight: a withering cloud |
| Life + Water + Stone | Moss: spreads living moss. The mod's signature. |

- An element's intensity in a heart is the total size of its glyphs there, 1 to 3. More than 3 is a mishap.
- Opposites cancel into a mishap when they share a heart: Ember and Frost, Wind and Ward. Life and Rot are not opposites; together they make Spore.
- Each element joins at most one mix. A mix's intensity is the smaller of its counts; leftovers stay pure.
- If any heart fails, the whole circle fails.
- What a mix does is only revealed by casting it, never while writing.

### Shape ring: forms and behaviours *(planned)*
Only Touch exists so far, implicitly. An element glyph written on the shape ring is a Misplaced mishap.

| Form | How the heart shows up |
|---|---|
| Touch | on what you touch or look at, within reach |
| Seed | thrown forward, acts where it lands |
| Ring | an area around the source |
| Mist | a lingering cloud |
| Path | a line along the ground ahead |
| Ward | stays in place and keeps working (tier IV) |
| Imbue | soaks into an item: a tool, an arrow, food, a door |

| Behaviour | Effect |
|---|---|
| Seeking | drifts toward the target |
| Delayed | waits before acting |
| Echo | repeats a few times |
| Gentle | weaker but cheaper, safe near friends and crops |

**Modifier marks** *(next version)*. Amounts are marks on a form's letter circle, not glyphs: dots for count, short lines for reach, small circles for duration, ticks for speed, 1 to 3 of each. Outside the letter means more, inside means less. They are public, like punctuation. Open decisions are listed in the roadmap.

### Aim ring: aim glyphs *(planned)*
Hex Casting's freedom of targeting, but in words. Aim glyphs chain link by link: creatures, near, not kin, closest.

| Kind | Glyphs |
|---|---|
| Pointers | Me, What-I-look-at, Here, Beneath, Above, the Sky |
| Gatherers | Creatures, Plants, Kin (players), Foes, Items, Water, Ground, Crops, Trees |
| Filters | Near / Far, Closest, All, Not, Wounded, Sleeping, Small / Large, Mine |
| Pronouns (in nested circles) | It (what the caller hit), There (where it landed), Source (the caller's target) |
| Names | Bound: a place, item or creature you marked earlier, like your home stone or your horse |

### Rim: triggers and nested circles *(tier IV, planned)*
At tier IV a nested circle works like a function. This is where the Hex Casting kind of depth lives.

- Triggers: on landing, on touch, when entered, when it ends, at dawn or dusk, when it rains, when hurt.
- Nested circles can have their own triggers and nested circles.
- A nested circle receives It, There and Source as its parameters.
- Named circles: bind a circle to a name glyph and call it from other circles, building a spell library carved on your home stone.
- Control flow in words: If (only when an aim glyph finds something), Echo (repeat), Each (once per target). No variables and no arithmetic.
- Hard limits keep it lag-safe: a maximum nesting depth, a cap on glyph executions and targets per cast, and bounded recursion. A circle over the limit fizzles, and the server never loops.

## 5. Example spells

**Paying in life essence**
- Life, Ring, on bark, drawn from yourself: you heal your friends and get hungry.
- Life, Ring, Gentle, drawn from the land, in a meadow: the grass around you pales a little and nobody goes hungry.
- Ember at size 3, Seed, many, drawn from the land, in a dry clearing: the fire flies, and the clearing behind you turns grey and dead for a season.

**Tier I: moss sigil** (heart only)
- Life: a green glow, and your wound closes a little.
- Water linked to Frost: Ice. A puddle at your feet freezes.
- Ember and Frost: opposites. The moss bursts in your hand. A mishap, but you learn something.

**Tier II: bark strip** (adds the shape)
- Ember → Seed: throw a spark that lands and lights the kindling.
- Life → Ring: a soft ring of healing around you.
- Water + Life → Ring → Wide: rain over your field, and the crops drink.

**Tier III: parchment or clay** (adds the aim)
- Two casts from one circle, cast together: Ember → Seed → Many → Foes → Closest throws a handful of fire seeds at the nearest hostile, while Life → Ring → Kin heals the friends around you.
- Life + Rot → Mist → Gentle → Foes → Closest: a spore cloud puts the nearest drifter to sleep.
- Wind → Seed → Seeking → Bound (home stone): a seed that drifts toward home, like a compass.

**Tier IV: carved stone or ground circle** (adds triggers and functions)

The seed that plants a grove: send a seed of light forward, and if it lands on soil, a tree grows there.
```
main circle    heart: Light             shape: Seed (thrown forward)
   rim         trigger: On landing  ->  nested circle (receives There)
nested circle  aim: If [Ground, is soil] at There
               heart: Life ×2 + Stone (Root)    shape: Touch, a sapling takes root
               rim: When it ends  ->  nested: Light, Ring, There (the tree glows softly)
```
The result is a sapling that then grows normally, not an instant full tree.

The warden stone: Ward + Life → Ward form → Wide → Creatures, not Foes, near Here. It soothes your animals. Its rim adds If [Foes near Here] → Frost + Wind → Ring, a cold blast that only fires when hostiles come close.

**Homestead magic**, the scale we're aiming for:
- A keeping-plank in the cellar: food spoils a bit more slowly.
- Hearth-bark: keeps a small room a few degrees warmer through winter.
- A grower's circle: nearby crops grow slightly faster, at the cost of soil nutrients.
- A hush-charm: quieter footsteps for a while.
- A moss lantern: soft light that grows on stone, with no fire.
- Path marks: glyphs that glow only for players who know them, a secret trail on a server.
- A speaking tree: carve a message into a tree, and another player hears it on touching it.
- A mending wrap: a tool wrapped in inscribed bark slowly regains durability.
- Water-sense or ore-whisper: a vague direction, never exact coordinates.
- Rust-soothing: slightly calms temporal instability, so nature pushes back against the rust.

## 6. Discovery

The space of possible glyphs is deliberately huge, so random doodling never finds anything. The vocabulary, which figure means what, is learned from the world. The combinations (mixes, forms, chains) are learned by experimenting, and that space is small and fun: 8 elements make only 28 pairs.

Ways to learn glyphs:
1. Listening to nature, for elements. Each element speaks somewhere: Water at springs and rivers, Ember by a long-burning fire, Frost on snow at night, Stone deep in caves, Life in old forest in bloom, Rot in bogs and compost, Wind on peaks during storms, Ward at ancient ruins. Sit there with blank bark and listen. The glyph's lines appear one by one over a few minutes, and the listening can be interrupted.
2. Lichen fragments, for everything else. Stones, ruin walls and ancient trees carry glyphs, often weathered, with some lines or marks missing. A charcoal rubbing carries a fragment home, and can be laid under the inscription window as a tracing guide.
3. Resonance. Casting a figure close to a real glyph gives hot-and-cold feedback, never the answer. With the right lines but wrong marks, the bark hums. One line off, it warms. Far off, nothing.
4. Family marks let players tell a glyph's kind from a fragment.
5. Dreams, rarely. Sleeping beside an old carved stone can show you a fragment.
6. Other players. Rubbings, scrolls and notebook pages can be traded or copied, and knowledge spreads like folklore.

The field notebook sketches every glyph you have drawn or rubbed, and logs what you saw when casting ("When cast near the goat, it lay down."). You name glyphs yourself, and there are no stat numbers. Using a glyph attunes you to it.

## 7. Life essence: the cost of magic

A spell costs the same energy as doing the thing by hand, and that energy has to come from somewhere. Every cast has a life-essence cost, worked out once at cast time from what the spell does: intensity (glyph sizes), number of casts, area, duration and nesting. Complexity costs more, not only power.

Only mishaps exist so far, at test scale. The rest is design.

### Where the essence comes from, in order

| Source | In game | When it runs out |
|---|---|---|
| 1. The medium | Living media hold some life: fresh moss the most, bark a little, stone almost none unless charged | It crumbles or wilts as it is spent |
| 2. Yourself | Satiety first (you grow hungry and weak), then health | Overreaching can knock you down, and can kill you |
| 3. The land (land source) | Grass, leaves, crops, moss, soil nutrients and nearby animals | The area dies |

Drawing from the land is always a visible choice: it is chosen on the border, which then shows a bramble. Moss can't draw from the land.

### Treating the land well or badly
- Done well (the land source with Gentle, a small reach, a lush place), a little is taken from many living things. Grass yellows for a while and animals tire, and then it recovers.
- Done badly (a big spell, a barren place, no Gentle), everything is drained. Grass dies back to dirt, leaves wither, crops rot, the soil loses its nutrients, and animals sicken and can die. The place becomes desolate: grey, silent and bare.
- Recovery takes seasons. A desolate patch heals slowly from its edges through the game's own grass spread and growth, and a druid can help with Life and Moss spells or by replanting. An overused grove stays a visible scar, part of the history of your homestead or server.
- Rot and Blight are the dark mirror: magic that takes life on purpose. They are allowed, but they leave desolation behind.

### Stored life
- Living moss can be grown and charged (moss jars, moss stones near water and light) and used as a battery, carried or built into a circle.
- A carved stone at tier IV can be fed over days by the living things around it, then cast without draining anyone. That is how permanent wards work.

### Overreaching
A circle that asks for more than its sources can give pulls harder. It drains you past hunger into health, and if it draws from the land it bursts out into the land as an uncontrolled wave of rot. A reckless caster can die of it.

### Fair play and performance
- You can't draw from someone else's land claim, and spells never hurt or push other players, so desolation can't be used for griefing.
- The cost is computed once per cast. Draining the land changes a capped number of blocks and creatures, once, at cast time. Nothing ticks afterwards; the game's own grass spread and plant growth do the healing.
- Materials are used up: the medium, inks and reagents. Creative mode keeps the medium (built).
- Mishaps: opposite elements, overloaded hearts and broken circles backfire a little, with smoke or a jolt. Later: thorns, or a wisp that follows you.

## 8. The inscription window (built)

**Controls**

| Where | Action | Does |
|---|---|---|
| Circle | Left click a slot | Select it |
| Circle | Left drag / right drag between glyphs | Link / unlink |
| Circle | Mouse wheel over a glyph | Change its size |
| Circle | Click / right-click the border | Next / previous source (bark and up) |
| Panel | Sweep / right-button sweep across points | Draw / erase lines (pattern-lock style; a skipped point is routed along the rim or through the middle) |
| Panel | Click / right-click a point | Cycle / clear its mark |
| Buttons | Clear glyph, Inscribe | Wipe the selected glyph; send the circle to the server, which checks it and writes it on the medium |
| In hand | Hold right-click / sneak + right-click | Cast / edit again |

Once the media are crafted in the world (M4), inscribing moves onto them: a stick on moss lying on stone for tier I, the charcoal stick at the scribing stump for tier II.

**Look**

![The inscription window on moss, bark and parchment](images/tier-interfaces.png)

*The same kind of circle on moss (tier I), bark (tier II) and parchment (tier III): each medium has its own art, rings and ink.*

- It reads like Circular Gallifreyan: bold letter circles, word circles biting the border, mix circles, straight links that bend only around glyphs they don't join, and doubled lines where they are earned (see "What is written, what is drawn").
- The ink is pixelated and hand-drawn, with a style per tier (`Gui/InkStyle.cs`). There is no anti-aliasing, and every imperfection is seeded so nothing flickers.

  | Tier | Ink | Feel |
  |---|---|---|
  | I, moss | dry lichen grey-green | drawn with a finger: coarse pixels, circles in 3 or 4 strokes, shaky, dry-brush gaps, thick |
  | II, bark | charcoal | 2 or 3 passes per circle, light grain |
  | III, parchment | blue-black ink | a single steady pen line, nearly clean |
  | IV, stone | chalk white | steady, with a chiselled grain |

- Each tier has its own art: `textures/gui/<moss|bark|parchment|stone>/canvas.png` (112 px) and `glyphpanel.png` (64 px), where 1 art pixel is 4 GUI units. A missing file falls back to a flat colour.
- Every image in `docs/images/` is rebuilt from the real drawing code by `tools/docs-images/run.sh`.

Later: a casting animation where light runs along the lines in reading order, soft and green-gold; carved circles that slowly gather moss; sounds of creaking wood, soft chimes and wind in leaves. No magic whooshes.

## 9. Multiplayer (built)
The server is authoritative. It alone knows the script, and it validates every drawing, resolves targets within reach, applies effects and consumes media. Every input a player controls is bounded: slots, glyphs, links, sizes and the source now, and nesting depth and executions later.

## 10. Open questions
1. Tier III: hide parchment, fired clay, or both (parchment is portable, clay is reusable)?
2. Inks: at what point should they start to matter?
3. How do we teach the grammar without giving away the vocabulary? Maybe a tutorial book written in a dead example script.
4. Should carving into living trees ("the tree remembers") come back as a tier IV variant?
5. Solo play: discovery should still work alone, as a slower, more personal notebook.
