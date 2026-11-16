#!/bin/bash
# Records slice W3 of the port on Joint_Hinge: touchpad gestures and the widget context menu on both paths.
# The output folder is $1 (default: a temporary folder); PREPOMAX_EXE selects another build.
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../../.." && pwd)")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortW3}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
"$EXE" -f "$ROOT\Dev\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\Dev\VtkPort\Widgets\Record_W3.py" \
    --scriptArgs "$ROOT\Dev\VtkPort\Harness|$OUT" -g No -w "$WORK" | grep -E "W3|States|Error|Exception|line"
[ -z "$PORT_REFERENCE" ] || py -3 "$(cygpath -u "$ROOT")"/Dev/VtkPort/Harness/graft_reference.py "$PORT_REFERENCE" "$OUT"
py -3 "$(cygpath -u "$ROOT")/Dev/VtkPort/Widgets/Compare_W3.py" "$OUT"
