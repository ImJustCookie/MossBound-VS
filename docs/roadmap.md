# Mossbound roadmap

Milestones in build order. The design they implement is in [design.md](design.md). Each milestone ends with something playable on the dedicated server setup. Version 1.0 covers tiers I and II.

## Milestones

| # | Milestone | State | Scope |
|---|---|---|---|
| M0 | Workspace | done | Project template, dedicated server and client launch, packaging, tests, CI |
| M1 | Script core | done | Glyph figures (arcs, spokes, 4 marks); a secret script per world with family marks; element mixing; the bounded circle model |
| M2 | Inscription window | done | Circle and glyph panel with pattern-lock drawing; hand-drawn pixel ink per tier; server-validated inscription |
| M3 | First casting | test scale | Hold to cast, sneak to edit, medium used up except in creative, cooldown; Touch only; Life, Ember and Wind effects; a coloured burst per aspect; backfire and fizzle; `/mossbound glyphs` |
| M3b | Circle grammar and look | done | Letter, word and mix circles, bites, aim lines, doubled lines; glyph sizes; source on the border; hearts mix only when linked; the linking rule; `/mossbound inspect`; the writing guide and `tools/docs-images` |
| M4 | Making the media (tiers I and II) | next | Work steps with block animations, a knapping-style carving surface and a station. Moss: scrape, lay, flatten, carve, inscribe with a stick. Bark: strip, build the scribing stump, flatten, carve, charcoal stick. Each medium unlocks its rings |
| M5 | Forms (tier II) | | Form and behaviour glyphs on the shape ring (Touch, Seed, Ring first); modifier marks; self-cost (hunger, then health); real effects for more aspects |
| M6 | Aim (tier III) | | Aim glyphs read as a chain; the tier III medium and its workstation |
| M7 | Learning glyphs | | Listening for elements, fragments and rubbings, resonance, the field notebook |
| M8 | Functions (tier IV) | | Carved stone and ground circles; rim triggers; nested circles with parameters, named circles, If, Echo, Each; hard limits |
| M9 | Life essence | | The full cost: medium, then self or land; land drain and recovery; stored life; land claims |
| M10 | Release 1.0 | | Handbook (grammar only), casting animation, sounds, balancing, config, release on the mod DB |

## Next steps

1. Test M3 and M3b on the dedicated server: sizes, the source on the border, linking, reopening a medium, old media still casting, `/mossbound inspect`.
2. M4, in the phases below. Art needed: models for each flattening stage (moss and bark), sheet textures for carving, stump stages, block animations (optional), and items (living moss, moss sigil fresh and wilted, fresh bark, bark sigil, charcoal stick).
3. M5: Touch, Seed and Ring forms, modifier marks, self-cost, and effects for Water, Frost and Moss.
4. A release build with the mod icon, after a dedicated server test.

## M4 phases

| Phase | Builds | Shown in game by |
|---|---|---|
| 1. Work steps | The step format, tool matching and variant mapping (unit-tested); the `Workable` block behaviour: hold to work, player animation, sound, particles, tool wear, claims | Scraping moss off mossy blocks; stripping bark off placed logs |
| 2. Block animations | Animations stretched to the step's length, and an overlay for vanilla blocks, synced to nearby players | Bark peeling while it is stripped |
| 3. Moss | Laying 5 living moss on stone, the flattening stages, the carving surface (10×10, knife, recipes, pattern picker) | Moss flattened into a sheet and carved into a sigil |
| 4. Station | The scribing stump (chopped in stages), slots that show their items, bark flattening and carving on the stump, the charcoal stick | A bark sigil made on the stump; whittling charcoal |
| 5. Inscribing on surfaces | Opening the window from a surface or station, tier rings in the canvas, server checks | Writing and casting a moss sigil and a bark sigil |

## Modifier marks: open decisions

Amounts (count, reach, duration, speed) will be marks on a form's letter circle, 1 to 3 each, outside for more and inside for less. To decide before M5:

1. Which media allow them: parchment and up, or bark and up.
2. How to write them: buttons under the glyph panel, or clicking faint placeholder marks around the glyph on the panel.
3. Whether the canvas switches to a thin letter circle with thick rim arcs, so a glyph looks the same as on the panel and the inside of the letter is free for "less" marks.

## Rules for every milestone

- The server is authoritative, every input is bounded, and nothing runs every tick unless it must.
- The glyph registry is append-only and protobuf field numbers never change, so existing worlds and media survive updates.
- Pure logic gets unit tests. Drawing changes get an offline render, then `tools/docs-images/run.sh` so the docs match.
- CI (lint, warning-free build, tests, packaging) passes before merging.
