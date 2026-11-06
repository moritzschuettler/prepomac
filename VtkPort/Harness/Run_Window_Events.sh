#!/bin/bash
# Integration: the mouse selection of the native view in the window, with and without the widgets area, driven by
# Win32 messages. The output folder is $1; PREPOMAX_EXE selects another build.
set -e
ROOT_U="$(cd "$(dirname "$0")/../.." && pwd)"
ROOT="$(cygpath -w "$ROOT_U")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortWindowEvents}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
rm -rf "$(cygpath -u "$OUT")"
RESULT="$("$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\VtkPort\Harness\Test_Window_Events.py" \
    --scriptArgs "$ROOT\VtkPort\Harness|$OUT" -g No -w "$WORK" | grep -E "^ok|^FAIL|TEST|Error|Exception|line")"
echo "$RESULT"
echo "$RESULT" | grep -q "TEST PASSED"
