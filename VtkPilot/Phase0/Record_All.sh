#!/bin/bash
# Records the phase 0 reference of all three cases into VtkPilot/Reference (or the folder given as $1).
# Paths are passed to PrePoMax as Windows paths: a /c/... path is read by the application as C:\c\...
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../.." && pwd)")"
OUT="${1:-$ROOT\VtkPilot\Reference}"
EXE="${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
SCRIPT="$ROOT\VtkPilot\Phase0\Record_Reference.py"
"$(cygpath -u "$EXE")" -f "$ROOT\APIs\Python\Test_Scripts\Models\L_bracket_r.step" -u MM_TON_S_C \
    --script "$SCRIPT" --scriptArgs "solid $OUT\solid" -g No -w "$WORK" | grep -E "REFERENCE|Error|picks hit"
"$(cygpath -u "$EXE")" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" \
    --script "$SCRIPT" --scriptArgs "hinge $OUT\hinge" -g No -w "$WORK" | grep -E "REFERENCE|Error|picks hit"
"$(cygpath -u "$EXE")" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" \
    --script "$SCRIPT" --scriptArgs "result $OUT\result" -g No -w "$WORK" | grep -E "REFERENCE|Error|Images"
