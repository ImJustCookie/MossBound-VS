#!/usr/bin/env bash
# Rebuilds every image in docs/images from the mod's real drawing code. See README.md.
set -euo pipefail
cd "$(dirname "$0")"
: "${VINTAGE_STORY:?set VINTAGE_STORY to the game folder (see CLAUDE.md)}"
dotnet build ../../mossbound/mossbound.csproj -c Debug -v q -nologo "$@"
dotnet run -v q "$@"
python3 labels.py
