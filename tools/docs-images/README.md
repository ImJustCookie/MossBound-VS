# docs-images

Rebuilds the images in `docs/images/` (the guide cards and the numbered circle diagram) from the mod's own drawing code, so the docs always show what the game draws.

```bash
tools/docs-images/run.sh
```

What it does:
1. builds the mod (`mossbound/bin/Debug`);
2. `dotnet run` renders every example circle with the real canvas and panel code to `out/` (ignored by git), plus `out/scenes.json` with where glyphs and word circles landed;
3. `labels.py` adds titles, captions and callouts and writes `docs/images/guide/*.png` and `docs/images/circle-grammar.png`.

Needs `VINTAGE_STORY` (the game folder, as for the mod), the .NET 10 SDK, the system Cairo library (`libcairo.so.2`) and Python 3 with `pycairo`. Inside Claude's sandbox, prefix with `XDG_RUNTIME_DIR=$TMPDIR` and pass `-p:EnableSourceControlManagerQueries=false`.

To change an example, edit its scene in `Scenes.cs`; to change a caption or a callout, edit `labels.py`. This folder is not part of the mod and is never packaged.
