#!/bin/bash
# Runs the phase 1 groundwork checks inside PrePoMax and sets the picture the view takes after the test scene beside
# the ActiViz picture of phase 0 (reported, not judged). The output folder is $1 (default: a temporary folder).
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../.." && pwd)")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\\VtkPilotPhase1}"
EXE="${PREPOMAX_EXE:-$ROOT\\PrePoMax\\bin\\x64\\Debug\\PrePoMax.com}"
mkdir -p "$(cygpath -u "$OUT")/compare"
"$(cygpath -u "$EXE")" -f "$ROOT\\Tests\\Joints\\Joint_Hinge.pmx" \
    --script "$ROOT\\VtkPilot\\Phase1\\Test_Groundwork.py" --scriptArgs "$OUT|$ROOT\\VtkPilot\\Reference" -g No |
    grep -E "^(ok|info) |TEST|Error|Exception"
cp "$(cygpath -u "$OUT")/hinge_femodel_800x600_Isometric.png" "$(cygpath -u "$OUT")/compare/"
mkdir -p "$(cygpath -u "$OUT")/reference"
cp "$(cygpath -u "$ROOT")/VtkPilot/Reference/hinge/hinge_femodel_800x600_Isometric.png" "$(cygpath -u "$OUT")/reference/"
py -3 "$(cygpath -u "$ROOT")/VtkPilot/Phase0/Compare_Reference.py" "$OUT\\reference" "$OUT\\compare"
