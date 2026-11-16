# Mossbound roadmap

The design is in `design.md`. Each milestone ends with something playable on the dedicated server setup.

| # | Milestone | State | Done when |
|---|---|---|---|
| M0 | Workspace | done | Template project, dedicated server and client launch, packaging, tests, builds that work in the sandbox |
| M1 | Script core | done | Rosette glyphs (arcs and spokes, 4 marks), a secret script per world with family marks, element mixing, a bounded circle model and parser |
| M2 | Inscription window | done | Bark strip (knife and log); canvas and glyph panel with pattern-lock drawing; Gallifreyan-style, pixelated hand-drawn ink on parchment art; inscription validated by the server |
| M3 | First casting | test scale | Hold to cast, sneak to edit, used up except in creative, cooldown; Touch only; Life, Ember and Wind effects; a coloured burst for every aspect; backfire and fizzle; admin glyph atlas (`/mossbound glyphs`) |
| M4 | Tiers I and II: making the media | | Work steps with block animations, a knapping-style carving surface (10×10, knife, pattern recipes) and a station. Moss: scrape, lay 5 on stone, flatten in stages, carve, inscribe with a stick. Bark: strip placed logs, build the scribing stump, flatten in stages, carve, charcoal stick. Each medium unlocks its rings in the canvas |
| M5 | Tier II: forms and branches | | Form glyphs (Touch, Seed, Ring) and modifiers in the inner ring; branches, with links as grammar and multicast; self-cost (hunger, then health); real effects for more aspects |
| M6 | Tier III: aim | | Selector glyphs in the outer ring, chained clockwise; hide parchment and/or clay tablet, with its workstation |
| M7 | Discovery | | Listening to nature for elements, lichen fragments and rubbings, resonance hints, the field notebook |
| M8 | Tier IV: functions | | Carved stone and ground circles; rim triggers; nested circles as functions (pronouns as parameters, named circles, If, Echo, Each); hard limits |
| M9 | Life essence | | Casting costs life: from the medium, then satiety and health (which can kill), or from the land through the Draw glyph (plants, soil and animals, with desolation the game heals over seasons); stored life (charged moss, fed stones); land claims |
| M10 | Polish and 1.0 | | Handbook (grammar only), casting animation, sounds, balancing, config, release on the mod DB |

## Next (v1 is tiers I and II)

1. Test M3 in game (the owner): casting, creative keeping the strip, the atlas window, the latest canvas look.
2. M4, following the phase plan below. Art needed from the owner: models for the flattening stages (moss and bark), sheet textures for carving, stump stages, optional block animations, and items (living moss, moss sigil fresh and wilted, fresh bark, bark strip, charcoal stick).
3. M5: the Touch, Seed and Ring forms with branch reading, self-cost, and effects for Water, Frost and Moss.
4. A release zip with the mod icon, after a test on the dedicated server.

## Rules for every milestone
- The server is authoritative, inputs are bounded, and nothing runs every tick unless it has to.
- The glyph registry is append-only and protobuf field numbers never change, so worlds and saved strips survive updates.
- Pure logic gets unit tests. Drawing changes get an offline render.

## M4 phase plan

| Phase | Builds | Shown in game by |
|---|---|---|
| 1. Work steps | The step format, tool matching and variant mapping (unit-tested); the `Workable` block behaviour (hold to work, player animation, sound, particles, tool wear, claims) | Scraping moss off mossy cobblestone and lichen; stripping bark off placed logs |
| 2. Block animations | Block animations stretched to the step's length, and the overlay for vanilla blocks, synced to nearby players | Bark visibly peeling while you strip it |
| 3. Moss | Laying 5 living moss on stone, the flattening stages, the carving surface (10×10, knife, recipes, pattern picker) | A heap of moss turned into a flat sheet, then carved into a moss sigil |
| 4. Station | The scribing stump (chopped in stages from a debarked log), slots that show their items, bark flattening and carving on the stump, the charcoal stick | Bark made into a strip on the stump; whittling charcoal |
| 5. Inscribing on surfaces | Inscribing through a surface or station (moss on stone with a stick, bark on the stump with the charcoal stick), tier rings in the canvas, server checks | Writing and casting a moss sigil and a bark strip |
