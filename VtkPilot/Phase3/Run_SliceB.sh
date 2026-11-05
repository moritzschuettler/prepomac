#!/bin/bash
# Records slice B on the solved Joint_Hinge and compares the ActiViz and the native scalar bar.
# The output folder is $1 (default: a temporary folder); PREPOMAX_EXE selects another build.
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../.." && pwd)")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPilotSliceB}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
"$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\VtkPilot\Phase3\Record_SliceB.py" \
    --scriptArgs "$OUT" -g No -w "$WORK" | grep -E "SLICE|States|Error|Exception|line"
"$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\VtkPilot\Phase3\Test_Embedded_ScalarBar.py" \
    -g No -w "$WORK" | grep -E "^ok|^FAIL|TEST|Error|Exception"
py -3 "$(cygpath -u "$ROOT")/VtkPilot/Phase3/Compare_SliceB.py" "$OUT"
