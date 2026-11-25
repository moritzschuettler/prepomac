#!/bin/bash
# Runs the checks of the VTK native port: every suite, or those whose name contains one of the --filter parts.
#
#   bash Dev/VtkPort/Harness/Run_Port_Checks.sh [--filter <part> ...] [--out <folder>] [--reference <folder>]
#
# --reference names the output folder of a run on a build that still had ActiViz: the ActiViz side of every suite is
# taken from it (graft_reference.py) before the comparison, so a build without ActiViz is compared with ActiViz.
#
# The build folder is copied first and the copy is run, so a build meanwhile does not fail on locked files. A suite
# is a recorder run in PrePoMax for each of its scenes and a comparison; a stream adds its suites to SUITES.
set -o pipefail
ROOT_U="$(cd "$(dirname "$0")/../../.." && pwd)"
ROOT="$(cygpath -w "$ROOT_U")"
HARNESS="$ROOT\Dev\VtkPort\Harness"
OUT="$(cygpath -w "${TEMP:-/tmp}")\VtkPortChecks"
FILTERS=()
while [ $# -gt 0 ]; do
    case "$1" in
        --filter) shift; while [ $# -gt 0 ] && [ "${1#--}" = "$1" ]; do FILTERS+=("$1"); shift; done ;;
        --out) OUT="$2"; shift 2 ;;
        --reference) REFERENCE="$2"; shift 2 ;;
        *) echo "Unknown argument: $1"; exit 2 ;;
    esac
done

# name | how it runs: a pilot runner (relative to the repository), or scenes of Record_Scene.py with their areas
SUITES=(
    "groundwork|pilot Dev/VtkPilot/Phase1/Run_Groundwork.sh"
    "geometry_scenes|scenes geometry solid_geometry solid_femodel hinge_femodel"
    "geometry_slice_a|pilot Dev/VtkPilot/Phase2/Run_SliceA.sh"
    "widgets_slice_b|pilot Dev/VtkPilot/Phase3/Run_SliceB.sh"
    "selection_slice_c|pilot Dev/VtkPilot/Phase4/Run_SliceC.sh"
    # Stream G (geometry)
    "geometry_g1|scenes geometry shell_femodel springs_femodel hinge_highlight hinge_highlight_constraint hinge_hidden solid_geometry_highlight"
    "geometry_g2|scenes geometry hinge_symbols springs_symbols slider_symbols ball_symbols plates_weld_symbols hinge_surface_loads hinge_annotate_materials hinge_reference_points hinge_reference_point_alone"
    "geometry_g3|scenes geometry hinge_result_disp hinge_result_mises hinge_result_undeformed_wireframe"
    "geometry_g4|scenes geometry hinge_section hinge_result_section solid_geometry_section hinge_exploded hinge_result_symmetry hinge_pattern"
    "geometry_g5|scenes geometry hinge_animation_frame"
    "geometry_g5_png|pilot Dev/VtkPort/Harness/Run_Png.sh"
    "geometry_g6|scenes geometry hinge_result_vector hinge_result_tensor hinge_vector_animation_frame hinge_tensor_animation_frame"
    "geometry_g7|scenes geometry hinge_captions hinge_item_ids hinge_measurement"
    "geometry_g8|scenes geometry hinge_coordinate_systems"
    "geometry_g9|scenes geometry hinge_section_element_wise hinge_result_section_element_wise hinge_section_element_wise_side"
    "geometry_g10|scenes geometry hinge_animated_section hinge_animated_section_element_wise"
    "geometry_g10_follower|pilot Dev/VtkPort/Harness/Run_Follower_View.sh"
    "geometry_g10_directional|scenes geometry hinge_result_symmetry_u1 hinge_result_pattern_s11 hinge_result_symmetry_field hinge_result_symmetry_section"
    "geometry_g10_exploded|scenes geometry hinge_exploded_animated"
    # Stream W: widgets and interaction
    "widgets_w1|pilot Dev/VtkPort/Widgets/Run_W1.sh"
    "widgets_w2|pilot Dev/VtkPort/Widgets/Run_W2.sh"
    "widgets_w3|pilot Dev/VtkPort/Widgets/Run_W3.sh"
    # Stream S
    "selection_s1|pilot Dev/VtkPort/Selection/Run_S1.sh"
    "selection_s2|pilot Dev/VtkPort/Selection/Run_S2.sh"
    "selection_s3|pilot Dev/VtkPort/Selection/Run_S3.sh"
    "selection_s4|pilot Dev/VtkPort/Selection/Run_S4.sh"
    "selection_s5_probe|pilot Dev/VtkPort/Selection/Run_S5.sh"
    # Integration
    "integration_window|pilot Dev/VtkPort/Harness/Run_Window_Events.sh"
)

selected() {
    [ ${#FILTERS[@]} -eq 0 ] && return 0
    for part in "${FILTERS[@]}"; do case "$1" in *"$part"*) return 0 ;; esac; done
    return 1
}

# PREPOMAX_BIN names another build folder to copy, for a run while PrePoMax.exe of the build is in use
COPY_U="$(cygpath -u "${TEMP:-/tmp}")/VtkPortBuild"
mkdir -p "$(dirname "$COPY_U")"
rm -rf "${COPY_U:?}" && cp -r "$(cygpath -u "${PREPOMAX_BIN:-$ROOT_U/PrePoMax/bin/x64/Debug}")" "$COPY_U"
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
        PORT_REFERENCE="${REFERENCE:+$REFERENCE\\$name}" PREPOMAX_EXE="$EXE_W" bash "$ROOT_U/$rest" "$OUT\\$name" 2>&1 |
            grep -E "PASS|FAIL|pass|TEST|COMPARE|GRAFT|Error|Exception" |
            grep -vE "^PASS " | tail -15
        status=${PIPESTATUS[0]}
    else
        read -r areas scene_list <<< "$rest"
        status=0
        for scene in $scene_list; do
            mapfile -t args < <(py -3 "$ROOT_U/Dev/VtkPort/Harness/scenes.py" "$scene" | tr -d '\r')
            PORT_REFERENCE="${REFERENCE:+$REFERENCE\\$name}" "$(cygpath -u "$EXE_W")" "${args[@]}" \
                --script "$HARNESS\Record_Scene.py" \
                --scriptArgs "$HARNESS|$scene|$areas|$OUT\\$name" -g No -w "$WORK" | grep -E "SCENE|Scene|Error|Exception"
        done
        graft_status=0
        if [ -n "$REFERENCE" ]; then
            py -3 "$ROOT_U/Dev/VtkPort/Harness/graft_reference.py" "$REFERENCE\\$name" "$OUT\\$name"
            graft_status=$?
        fi
        py -3 "$ROOT_U/Dev/VtkPort/Harness/checks.py" scenes "$OUT\\$name" | grep -vE "^PASS "
        status=$?
        [ "$graft_status" -eq 0 ] || status=1
    fi
    [ "$status" -eq 0 ] || FAILED+=("$name")
done
echo "=== results in $OUT"
if [ ${#FAILED[@]} -eq 0 ]; then echo "ALL PASSED"; else echo "FAILED: ${FAILED[*]}"; exit 1; fi
