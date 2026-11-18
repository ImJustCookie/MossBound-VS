# Changelog

All notable changes to Mossbound. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [semantic versioning](https://semver.org/). New entries go under **Unreleased**, which is renamed to the version and date on release.

## [Unreleased]

First version.

### Added
- A secret glyph script per world: glyph shapes generated from the seed, with a family mark per glyph kind.
- Three media: moss sigil (tier I), bark sigil (tier II, crafted from a knife and a placed log), parchment sigil (tier III, creative only). Each with its own model and textures, the written side glowing faintly.
- The inscription window: draw glyphs pattern-lock style, link them, set their size with the mouse wheel, choose the source on the border. Hand-drawn pixel ink per tier.
- Circle rules: hearts mix their elements when linked, casts, the linking rule; 8 elements and 13 mixes.
- Casting at test scale: hold right-click to charge, release to cast (charge time per medium, `castSeconds`); Life heals, Ember burns, Wind pushes; mishaps backfire or fizzle.
- Held medium poses: it floats and turns above the palm, and spins up while a cast charges (first and third person).
- Casting effects: a spinning spark ring at the caster's feet, and the medium's own glyphs rising into rings around the caster, one ring per cast in the spell.
- Admin commands: `/mossbound glyphs` and `/mossbound inspect`.
- Every text in `lang/en.json`, shown in each player's language.
- Licence: CC BY-NC-SA 4.0 for code, art and documentation.
