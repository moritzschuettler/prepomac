#!/bin/bash
# Records slice A for the solid and the hinge case and compares the ActiViz and the native pictures and cameras.
# The output folder is $1 (default: a temporary folder); PREPOMAX_EXE selects another build.
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../.." && pwd)")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPilotSliceA}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
SCRIPT="$ROOT\VtkPilot\Phase2\Record_SliceA.py"
"$EXE" -f "$ROOT\APIs\Python\Test_Scripts\Models\L_bracket_r.step" -u MM_TON_S_C --script "$SCRIPT" \
    --scriptArgs "solid|$OUT" -g No | grep -E "SLICE|Mirrored|Error"
"$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$SCRIPT" --scriptArgs "hinge|$OUT" -g No |
    grep -E "SLICE|Mirrored|Error"
py -3 "$(cygpath -u "$ROOT")/VtkPilot/Phase2/Compare_SliceA.py" "$OUT"
