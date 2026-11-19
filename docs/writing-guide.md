# Writing circles

How to write and cast a spell, one concept per picture. Every circle shown is drawn by the game's own code. Items marked *(planned)* are designed but not in the game yet. The full rules are in [design.md](design.md).

## Getting started

1. Get a medium. Bark sigil: craft a knife with a placed log in the crafting grid. Moss sigil and parchment sigil: creative inventory for now.
2. Hold the medium and right-click. The inscription window opens.
3. Write glyphs, link them, and press **Inscribe**. The server checks the circle and writes it on one medium.
4. Hold right-click with the inscribed medium: it spins faster as it charges. After one second, release to cast it; releasing sooner cancels. Sneak + right-click edits it again.

## 1. A glyph

A glyph is a word. You draw it on the glyph panel; the circle shows it in place.

![A glyph](images/guide/01-glyph.png)

## 2. Strength

The size of a glyph sets how strong it is. A heart's strength is the sum of its glyph sizes, up to the medium's limit.

![Strength](images/guide/02-strength.png)

## 3. Mixing

Element glyphs linked together in the heart mix into one result. Only which glyphs you link counts, not their sizes, and two of the same glyph count as two. If the glyphs make no known mix, the circle fails.

![Mixing](images/guide/03-mixing.png)

## 4. Casts

A cast is a heart and everything linked outward from it. One circle can hold several casts.

![Casts](images/guide/04-casts.png)

Form glyphs for the shape ring (Touch, Seed, Ring...) are *(planned)*. Until they exist every spell acts by touch, and these pictures use element glyphs as stand-ins.

## 5. Source

The border shows what pays for the spell: the medium, you, or the land.

![Source](images/guide/05-source.png)

## 6. Aiming

A cast that reaches the aim ring is aimed. Aim glyphs are *(planned)*.

![Aiming](images/guide/06-aim.png)

## 7. Written versus drawn

Only part of what you see changes the spell. The game draws the rest to make the circle readable.

![Written vs drawn](images/guide/07-written-vs-drawn.png)

## 8. Media

Better media have more rings and slots.

![Media](images/guide/08-media.png)

## Full circle

![Reading a Mossbound circle](images/circle-grammar.png)

## Reference

### What changes the spell

| On the circle | Changes the spell? |
|---|---|
| Glyphs (lines and marks) | Yes: they are the words |
| The ring a glyph is on | Yes: elements must be in the heart |
| Glyph size | Yes: strength |
| Links | Yes: which hearts mix, which glyphs form a cast |
| Border pattern (source) | Yes: who pays *(the cost is planned)* |
| Mix circles, word circles, bites, aim lines, doubled lines | No: drawn by the game |
| Slot positions, empty slots, highlights | No: interface only |

### When it goes wrong

| Mishap | Cause | Result |
|---|---|---|
| Opposites | Ember with Frost, or Wind with Ward, in one heart | Backfires: smoke and slight damage to you |
| No mix | Glyphs linked in a heart that make no known mix | Fizzles |
| Too strong | A heart's sizes add up past the medium's limit (moss 2, bark 3, parchment 4) | Backfires |
| Too large | A form group's sizes add up past the medium's limit (bark 2, parchment 3) | Backfires |
| Too many spells | More spells than the medium holds (moss 1, bark 2, parchment 3) | Backfires |
| Unknown glyph | A linked figure that is no glyph (an unlinked one is ignored) | Fizzles |
| Misplaced | An element glyph outside the heart | Fizzles |
| Nothing to cast | No element in any heart | Fizzles |

If any heart fails, the whole circle fails. After a mishap, a message says what went wrong and how to fix it, without naming any glyph.

### Controls

| Where | Action | Result |
|---|---|---|
| Circle | Left click a slot | Select it |
| Circle | Left drag / right drag between two glyphs | Link / unlink them |
| Circle | Mouse wheel over a glyph | Change its size |
| Circle | Click / right-click the border | Next / previous source (bark and up) |
| Panel | Sweep across points / sweep with the right button | Draw / erase lines. A skipped point is routed along the rim or through the middle |
| Panel | Click / right-click a point | Cycle / clear its mark |
| Buttons | Clear glyph | Erase the selected glyph |
| Buttons | Inscribe | Send the circle to the server, which checks it and writes it on the medium |
| In hand | Hold right-click 1 s, release | Cast, on what you look at, or on yourself |
| In hand | Sneak + right-click | Edit the circle again |

### Casting today
Each aspect has its own particles, sound and effects (from `config/personalities.json`): Ember burns and sets on fire (a wider patch of ground with more strength); Water puts out fire and pushes lightly; Frost chills and slows; Stone bashes and knocks back; Life heals; Rot poisons and slows; Wind pushes hard; Ward puts a bubble on its target that absorbs damage until it pops or fades; Steam scalds; Rain heals and puts out fire; Smoke slows. Strength multiplies every effect. Harmful effects never reach other players; a spell cast at nothing lands on yourself. Every spell still acts by touch, whatever its form. Two seconds between casts. The medium is used up, except in creative mode.

### Planned
Form, behaviour and aim glyphs; modifier marks on forms; carved stone with triggers and nested circles; the life-essence cost; making media in the world; learning glyphs from the world. See [design.md](design.md) and the [roadmap](roadmap.md).
