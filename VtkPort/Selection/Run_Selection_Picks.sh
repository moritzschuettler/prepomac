#!/bin/bash
# Records the pick plan of stream S for the given scenes and compares the ids of the ActiViz path (ActiViz helpers)
# and of the native path (native helpers) with the comparison of slice C.
#
#   bash Run_Selection_Picks.sh <out dir> <modes> <scene> ...
#
# <modes>: femodel or geometry (Record_Selection_Picks.py). PREPOMAX_EXE selects another build.
set -e
ROOT_U="$(cd "$(dirname "$0")/../.." && pwd)"
ROOT="$(cygpath -w "$ROOT_U")"
OUT="$1"
MODES="$2"
shift 2
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
rm -rf "$(cygpath -u "$OUT")"
for scene in "$@"; do
    mapfile -t args < <(py -3 "$ROOT_U/VtkPort/Harness/scenes.py" "$scene" | tr -d '\r')
    "$EXE" "${args[@]}" --script "$ROOT\VtkPort\Selection\Record_Selection_Picks.py" \
        --scriptArgs "$ROOT\VtkPort\Harness|$scene|$MODES|$OUT" -g No -w "$WORK" |
        grep -E "SELECTION|Picks|Error|Exception|line "
done
py -3 "$ROOT_U/VtkPilot/Phase4/Compare_SliceC.py" "$OUT"
py -3 -c "
import glob, json, os, sys
worst = max(json.load(open(f)).get('closest_cells_worst', 0.0) for f in glob.glob(os.path.join(sys.argv[1], '*_slice_c.json')))
print('COMPARE closest cells on the parts: largest difference %.1e %s' % (worst, 'PASSED' if worst < 1e-6 else 'FAILED'))
sys.exit(0 if worst < 1e-6 else 1)" "$OUT"
