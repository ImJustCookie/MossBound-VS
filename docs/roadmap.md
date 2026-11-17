# Mossbound roadmap

The design is in `design.md`. Each milestone ends with something playable on the dedicated server setup.

| # | Milestone | State | Done when |
|---|---|---|---|
| M0 | Workspace | done | Template project, dedicated server and client launch, packaging, tests, builds that work in the sandbox |
| M1 | Script core | done | Rosette glyphs (arcs and spokes, 4 marks), a secret script per world with family marks, element mixing, a bounded circle model and parser |
| M2 | Inscription window | done | Bark strip (knife and log); canvas and glyph panel with pattern-lock drawing; Gallifreyan-style, pixelated hand-drawn ink on parchment art; inscription validated by the server |
| M3 | First casting | test scale | Hold to cast, sneak to edit, used up except in creative, cooldown; Touch only; Life, Ember and Wind effects; a coloured burst for every aspect; backfire and fizzle; admin glyph atlas (`/mossbound glyphs`) |
| M3b | Circle look and grammar | done | Gallifreyan-style drawing (letter, word and mix circles, bites, aim lines, doubled lines); glyph sizes as strength (mouse wheel); source chosen on the border (medium, self, land); linked hearts mix, unlinked ones stay apart; the linking rule; `/mossbound inspect`; the visual guide and `tools/docs-images` |
| M4 | Tiers I and II: making the media | | Work steps with block animations, a knapping-style carving surface (10×10, knife, pattern recipes) and a station. Moss: scrape, lay 5 on stone, flatten in stages, carve, inscribe with a stick. Bark: strip placed logs, build the scribing stump, flatten in stages, carve, charcoal stick. Each medium unlocks its rings in the canvas |
| M5 | Tier II: forms | | Form glyphs (Touch, Seed, Ring) and behaviour glyphs on the shape ring; modifier marks on forms; self-cost (hunger, then health); real effects for more aspects |
| M6 | Tier III: aim | | Aim glyphs on the aim ring, read as a chain; hide parchment and/or clay tablet, with its workstation |
| M7 | Discovery | | Listening to nature for elements, lichen fragments and rubbings, resonance hints, the field notebook |
| M8 | Tier IV: functions | | Carved stone and ground circles; rim triggers; nested circles as functions (pronouns as parameters, named circles, If, Echo, Each); hard limits |
| M9 | Life essence | | Casting costs life: from the medium, then satiety and health (which can kill), or from the land when the border says so (plants, soil and animals, with desolation the game heals over seasons); stored life (charged moss, fed stones); land claims |
| M10 | Polish and 1.0 | | Handbook (grammar only), casting animation, sounds, balancing, config, release on the mod DB |

## Next (v1 is tiers I and II)

1. Test M3 and M3b in game on the dedicated server (the owner): sizes with the wheel, the source on the border, linking, reopening a strip, old strips still casting, `/mossbound inspect`, and smooth dragging on parchment.
2. M4, following the phase plan below. Art needed from the owner: models for the flattening stages (moss and bark), sheet textures for carving, stump stages, optional block animations, and items (living moss, moss sigil fresh and wilted, fresh bark, bark strip, charcoal stick).
3. M5: the Touch, Seed and Ring forms, modifier marks, self-cost, and effects for Water, Frost and Moss.
4. A release zip with the mod icon, after a test on the dedicated server.

## Modifier marks: open decisions (for M5)

Amounts (count, reach, duration, speed) become marks on a form's letter circle, 1 to 3 each, outside for more and inside for less. To decide before building:
1. Which media allow them: parchment and up, or bark and up.
2. How to write them: buttons under the glyph panel, or clicking faint ghost marks drawn around the glyph on the panel.
3. Whether the canvas first switches to a thin letter circle with thick arcs on the rim, so a glyph looks the same as on the panel and the inside of the letter is free for "less" marks.

## Rules for every milestone
- The server is authoritative, inputs are bounded, and nothing runs every tick unless it has to.
- The glyph registry is append-only and protobuf field numbers never change, so worlds and saved strips survive updates.
- Pure logic gets unit tests. Drawing changes get an offline render, then `tools/docs-images/run.sh` so the docs match.

## M4 phase plan

| Phase | Builds | Shown in game by |
|---|---|---|
| 1. Work steps | The step format, tool matching and variant mapping (unit-tested); the `Workable` block behaviour (hold to work, player animation, sound, particles, tool wear, claims) | Scraping moss off mossy cobblestone and lichen; stripping bark off placed logs |
| 2. Block animations | Block animations stretched to the step's length, and the overlay for vanilla blocks, synced to nearby players | Bark visibly peeling while you strip it |
| 3. Moss | Laying 5 living moss on stone, the flattening stages, the carving surface (10×10, knife, recipes, pattern picker) | A heap of moss turned into a flat sheet, then carved into a moss sigil |
| 4. Station | The scribing stump (chopped in stages from a debarked log), slots that show their items, bark flattening and carving on the stump, the charcoal stick | Bark made into a strip on the stump; whittling charcoal |
| 5. Inscribing on surfaces | Inscribing through a surface or station (moss on stone with a stick, bark on the stump with the charcoal stick), tier rings in the canvas, server checks | Writing and casting a moss sigil and a bark strip |
