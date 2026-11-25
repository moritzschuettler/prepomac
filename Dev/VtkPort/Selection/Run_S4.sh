#!/bin/bash
# Slice S4: the pick plan on a section view (the plain cut and the element-wise one) and on transformed copies beside
# the parts. The output folder is $1.
#
# Known differences, which NativeExactPicking decides and which pass as such: a pixel on the rim of the hole of the
# lever in hinge_symmetry, where ActiViz rebuilds the point on the top face from the depth buffer and the exact native
# pick follows the ray of the pixel into the hole. Where a copy lies in front of a part (hinge_pattern, not run) ActiViz
# takes the depth of the copy and the exact pick finds the part surface behind it.
OUT="${1:-$(cygpath -w "${TEMP:-/tmp}")\VtkPortS4}"
KNOWN="hinge_symmetry (225, 425)"
RESULT="$(bash "$(dirname "$0")/Run_Selection_Picks.sh" "$OUT" femodel \
    hinge_section hinge_section_element_wise hinge_symmetry)"
STATUS=$?
echo "$RESULT"
[ $STATUS -eq 0 ] && exit 0
ROOT_U="$(cd "$(dirname "$0")/../../.." && pwd)"
py -3 "$ROOT_U/Dev/VtkPilot/Phase4/Compare_SliceC.py" "$OUT" | py -3 -c "
import sys
known_case, known_point = sys.argv[1].split(' ', 1)
case, unexpected = None, []
for line in sys.stdin:
    if ': camera difference' in line:
        case = line.split(':')[0]
    elif line.startswith('  FAIL') and not (case == known_case and known_point in line):
        unexpected.append(case + ' ' + line.strip())
print('S4 differences beyond the known one: %d' % len(unexpected))
for line in unexpected[:10]:
    print(line)
print('COMPARE PASSED' if not unexpected else 'COMPARE FAILED')
sys.exit(0 if not unexpected else 1)" "$KNOWN"
