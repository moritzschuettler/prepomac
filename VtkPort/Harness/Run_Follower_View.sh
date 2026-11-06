#!/bin/bash
# Stream G: the follower view moves the native camera as it moves the ActiViz camera, frame by frame through an
# animation. The output folder is $1; PREPOMAX_EXE selects another build.
set -e
ROOT_U="$(cd "$(dirname "$0")/../.." && pwd)"
ROOT="$(cygpath -w "$ROOT_U")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortFollowerView}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
rm -rf "$(cygpath -u "$OUT")"
RESULT="$("$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\VtkPort\Harness\Test_Follower_View.py" \
    --scriptArgs "$ROOT\VtkPort\Harness|$OUT" -g No -w "$WORK" | grep -E "^ok|^FAIL|TEST|Error|Exception|line")"
echo "$RESULT"
echo "$RESULT" | grep -q "TEST PASSED"
