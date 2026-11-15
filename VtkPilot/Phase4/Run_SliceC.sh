#!/bin/bash
# Records slice C for the solid and the hinge case and compares the picks of the ActiViz and the native path.
# The output folder is $1 (default: a temporary folder); PREPOMAX_EXE selects another build.
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../.." && pwd)")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPilotSliceC}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
SCRIPT="$ROOT\VtkPilot\Phase4\Record_SliceC.py"
"$EXE" -f "$ROOT\APIs\Python\Test_Scripts\Models\L_bracket_r.step" -u MM_TON_S_C --script "$SCRIPT" \
    --scriptArgs "solid|$OUT" -g No -w "$WORK" | grep -E "SLICE|Picks|Error|Exception|line "
"$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$SCRIPT" --scriptArgs "hinge|$OUT" -g No -w "$WORK" |
    grep -E "SLICE|Picks|Error|Exception|line "
[ -z "$PORT_REFERENCE" ] || py -3 "$(cygpath -u "$ROOT")"/VtkPort/Harness/graft_reference.py "$PORT_REFERENCE" "$OUT"
py -3 "$(cygpath -u "$ROOT")/VtkPilot/Phase4/Compare_SliceC.py" "$OUT"
