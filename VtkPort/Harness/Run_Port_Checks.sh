#!/bin/bash
# Runs the checks of the VTK native port: every suite, or those whose name contains one of the --filter parts.
#
#   bash VtkPort/Harness/Run_Port_Checks.sh [--filter <part> ...] [--out <folder>]
#
# The build folder is copied first and the copy is run, so a build meanwhile does not fail on locked files. A suite
# is a recorder run in PrePoMax for each of its scenes and a comparison; a stream adds its suites to SUITES.
set -o pipefail
ROOT_U="$(cd "$(dirname "$0")/../.." && pwd)"
ROOT="$(cygpath -w "$ROOT_U")"
HARNESS="$ROOT\VtkPort\Harness"
OUT="$(cygpath -w "${TEMP:-/tmp}")\VtkPortChecks"
FILTERS=()
while [ $# -gt 0 ]; do
    case "$1" in
        --filter) shift; while [ $# -gt 0 ] && [ "${1#--}" = "$1" ]; do FILTERS+=("$1"); shift; done ;;
        --out) OUT="$2"; shift 2 ;;
        *) echo "Unknown argument: $1"; exit 2 ;;
    esac
done

# name | how it runs: a pilot runner (relative to the repository), or scenes of Record_Scene.py with their areas
SUITES=(
    "groundwork|pilot VtkPilot/Phase1/Run_Groundwork.sh"
    "geometry_scenes|scenes geometry solid_geometry solid_femodel hinge_femodel"
    "geometry_slice_a|pilot VtkPilot/Phase2/Run_SliceA.sh"
    "widgets_slice_b|pilot VtkPilot/Phase3/Run_SliceB.sh"
    "selection_slice_c|pilot VtkPilot/Phase4/Run_SliceC.sh"
    # Stream S
    "selection_s1|pilot VtkPort/Selection/Run_S1.sh"
    "selection_s2|pilot VtkPort/Selection/Run_S2.sh"
    "selection_s3|pilot VtkPort/Selection/Run_S3.sh"
)

selected() {
    [ ${#FILTERS[@]} -eq 0 ] && return 0
    for part in "${FILTERS[@]}"; do case "$1" in *"$part"*) return 0 ;; esac; done
    return 1
}

COPY_U="$(cygpath -u "${TEMP:-/tmp}")/VtkPortBuild"
rm -rf "$COPY_U" && cp -r "$ROOT_U/PrePoMax/bin/x64/Debug" "$COPY_U"
EXE_W="$(cygpath -w "$COPY_U")\PrePoMax.com"
WORK="$(cygpath -w "${TEMP:-/tmp}")\VtkPilotWork"
mkdir -p "$(cygpath -u "$WORK")"

FAILED=()
for suite in "${SUITES[@]}"; do
    name="${suite%%|*}"
    how="${suite#*|}"
    selected "$name" || continue
    echo "=== $name"
    kind="${how%% *}"
    rest="${how#* }"
    if [ "$kind" = "pilot" ]; then
        PREPOMAX_EXE="$EXE_W" bash "$ROOT_U/$rest" "$OUT\\$name" 2>&1 | grep -E "PASS|FAIL|pass|TEST|COMPARE|Error|Exception" |
            grep -vE "^PASS " | tail -15
        status=${PIPESTATUS[0]}
    else
        read -r areas scene_list <<< "$rest"
        status=0
        for scene in $scene_list; do
            mapfile -t args < <(py -3 "$ROOT_U/VtkPort/Harness/scenes.py" "$scene" | tr -d '\r')
            "$(cygpath -u "$EXE_W")" "${args[@]}" --script "$HARNESS\Record_Scene.py" \
                --scriptArgs "$HARNESS|$scene|$areas|$OUT\\$name" -g No -w "$WORK" | grep -E "SCENE|Scene|Error|Exception"
        done
        py -3 "$ROOT_U/VtkPort/Harness/checks.py" scenes "$OUT\\$name" | grep -vE "^PASS "
        status=$?
    fi
    [ "$status" -eq 0 ] || FAILED+=("$name")
done
echo "=== results in $OUT"
if [ ${#FAILED[@]} -eq 0 ]; then echo "ALL PASSED"; else echo "FAILED: ${FAILED[*]}"; exit 1; fi
