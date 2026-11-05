#!/bin/bash
# Records slice W1 of the port on Joint_Hinge and compares the ActiViz widgets with their native ports.
# The output folder is $1 (default: a temporary folder); PREPOMAX_EXE selects another build.
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../.." && pwd)")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortW1}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
"$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\VtkPort\Widgets\Record_W1.py" \
    --scriptArgs "$ROOT\VtkPort\Harness|$OUT" -g No -w "$WORK" | grep -E "W1|States|Error|Exception|line"
py -3 "$(cygpath -u "$ROOT")/VtkPort/Widgets/Compare_W1.py" "$OUT"
