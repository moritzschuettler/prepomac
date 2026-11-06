#!/bin/bash
# Records slice W2 of the port on Joint_Hinge: the scripted input on both styles, then the embedded native view
# driven by Win32 messages. The output folder is $1 (default: a temporary folder); PREPOMAX_EXE selects another build.
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../.." && pwd)")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortW2}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
"$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\VtkPort\Widgets\Record_W2.py" \
    --scriptArgs "$ROOT\VtkPort\Harness|$OUT" -g No -w "$WORK" | grep -E "W2|Sequences|Error|Exception|line"
"$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\VtkPort\Widgets\Test_Embedded_W2.py" \
    --scriptArgs "$ROOT\VtkPort\Harness|$OUT" -g No -w "$WORK" | grep -E "^ok|^FAIL|TEST|Error|Exception|line"
py -3 "$(cygpath -u "$ROOT")/VtkPort/Widgets/Compare_W2.py" "$OUT"
