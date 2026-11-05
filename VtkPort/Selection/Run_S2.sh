#!/bin/bash
# Slice S2: pictures of the mouse selection highlights per mode on the meshed solid, the hinge and the solid geometry,
# ActiViz against the native selection layer. The output folder is $1; PREPOMAX_EXE selects another build.
set -e
ROOT_U="$(cd "$(dirname "$0")/../.." && pwd)"
ROOT="$(cygpath -w "$ROOT_U")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortS2}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
rm -rf "$(cygpath -u "$OUT")"
for entry in solid_femodel:femodel hinge_femodel:femodel solid_geometry:geometry; do
    scene="${entry%%:*}"
    modes="${entry#*:}"
    mapfile -t args < <(py -3 "$ROOT_U/VtkPort/Harness/scenes.py" "$scene" | tr -d '\r')
    "$EXE" "${args[@]}" --script "$ROOT\VtkPort\Selection\Record_Selection_Highlights.py" \
        --scriptArgs "$ROOT\VtkPort\Harness|$scene|$modes|$OUT" -g No -w "$WORK" |
        grep -E "SELECTION|Highlights|Error|Exception|line "
done
py -3 "$ROOT_U/VtkPort/Selection/Compare_Highlights.py" "$OUT"
