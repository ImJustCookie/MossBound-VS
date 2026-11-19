# Changelog

All notable changes to Mossbound. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [semantic versioning](https://semver.org/). New entries go under **Unreleased**, which is renamed to the version and date on release.

## [Unreleased]

First version.

### Added
- A secret glyph script per world: glyph shapes generated from the seed, with a family mark per glyph kind.
- Three media: moss sigil (tier I), bark sigil (tier II, crafted from a knife and a placed log), parchment sigil (tier III, creative only). Each with its own model and textures, the written side glowing faintly.
- The inscription window: draw glyphs pattern-lock style, link them, set their size with the mouse wheel, choose the source on the border. Hand-drawn pixel ink per tier.
- Circle rules: a heart mixes its linked element glyphs into exactly one result, or fails; sizes add up to its strength, capped per medium, as is the number of spells per circle. Castable now: every element alone, and Steam, Rain, Smoke.
- Form glyphs Seed and Ring on the shape ring, read with the same rules (one form per linked group); for now every spell still acts by touch.
- Casting at test scale: hold right-click to charge, release to cast (charge time per medium, `castSeconds`); each aspect with its own particles, sound and effects (burning, slowing, healing, pushing, putting out fire, a damage-absorbing bubble for Ward); mishaps backfire or fizzle, with a hint on what went wrong.
- A signature per aspect: Ember melts ice and snow, Water wets, Frost chills and freezes still water, Stone roots, Life cures poison and thickens grass, Rot poisons over time and withers plants, Wind lifts and scatters dropped items, Rain heals over time, Smoke calms creatures. Block changes respect land claims.
- Reactions: Frost freezes a wet target in place, Ember on a wet target bursts into scalding steam, Ember thaws a chilled target, Water or Rain douses a burning one, Wind fans its flames.
- Held medium poses: it floats and turns above the palm, and spins up while a cast charges (first and third person).
- Casting effects: a spinning spark ring at the caster's feet, and the medium's own glyphs rising into rings around the caster, one ring per cast in the spell, each glyph sized like the written one.
- Admin commands: `/mossbound glyphs` (sorted by glyph kind) and `/mossbound inspect`.
- Every text in `lang/en.json`, shown in each player's language.
- Licence: CC BY-NC-SA 4.0 for code, art and documentation.
