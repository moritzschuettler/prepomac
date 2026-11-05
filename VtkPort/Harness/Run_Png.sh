#!/bin/bash
# Stream G: records RenderToPNG of the hinge through both paths (opaque and transparent, magnified 1 and 2 times) and
# compares the pictures. The output folder is $1; PREPOMAX_EXE selects another build.
set -e
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/../.." && pwd)")"
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortPng}"
EXE="$(cygpath -u "${PREPOMAX_EXE:-$ROOT\PrePoMax\bin\x64\Debug\PrePoMax.com}")"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"
"$EXE" -f "$ROOT\Tests\Joints\Joint_Hinge.pmx" --script "$ROOT\VtkPort\Harness\Record_Png.py" \
    --scriptArgs "$ROOT\VtkPort\Harness|$OUT" -g No -w "$WORK" | grep -E "PNG RECORDED|Error"
py -3 "$(cygpath -u "$ROOT")/VtkPort/Harness/checks.py" scenes "$OUT"
