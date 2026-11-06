#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, stream G: the follower view moves the camera of the native view as it moves the ActiViz camera.
#A scale factor animation of the solved hinge is framed the same on both paths, the follower view is applied to one
#node and then to three nodes of the lever, and after every frame the two cameras must agree. Writes
#follower_view.json.
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Test_Follower_View.py --scriptArgs "<harness>|<out>" -g No -w <work>
#
#Expected process exit code 0 and "TEST PASSED".

import json
import os
import sys

import System

HARNESS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import scenes  # noqa: E402
import vtkport  # noqa: E402

VTK = vtkport.VTK
CONTROLLER = vtkport.CONTROLLER
on_ui = vtkport.on_ui
TOLERANCE = 1e-7  # the animated coordinates are single precision on the ActiViz side
NUMBER_OF_FRAMES = 8
RESULTS = []


def compare_cameras(old, new):
    """The largest difference of the camera values relative to the size of the positions, as checks.py"""
    size = max(1.0, max(abs(v) for v in old[:6]))
    return max(abs(a - b) for a, b in zip(old, new)) / size


def check(condition, text, detail=""):
    RESULTS.append(["ok" if condition else "FAIL", text, str(detail)])


def node_ids(part_name, count):
    def run():
        actors = vtkport.get_field(VTK, "_actors")
        data = actors[part_name].GeometryMapper.GetInputAsDataSet()
        ids = data.GetPointData().GetGlobalIds()
        number = data.GetNumberOfPoints()
        return [int(ids.GetValue(int(i * number / count))) for i in range(count)]
    return on_ui(run)


def run_follower(label, part_names, ids, record):
    on_ui(lambda: VTK.SetAnimationFrame(0, False))
    start = vtkport.camera_list(pmx.view.get_parameters())
    old_camera, new_camera = vtkport.frame_both(start, "Isometric")
    framed = old_camera
    check(compare_cameras(old_camera, new_camera) <= TOLERANCE, label + ": framed the same",
          compare_cameras(old_camera, new_camera))
    on_ui(lambda: VTK.ApplyFollowerView(System.Array[System.String](part_names), System.Array[System.Int32](ids)))
    differences = []
    moved = 0.0
    for frame in range(1, NUMBER_OF_FRAMES):
        on_ui(lambda: VTK.SetAnimationFrame(frame, False))
        old_camera = vtkport.camera_list(pmx.view.get_parameters())
        new_camera = on_ui(lambda: vtkport.camera_list(VTK.NativeMirror.GetCamera()))
        differences.append(compare_cameras(old_camera, new_camera))
        moved = max(moved, compare_cameras(framed, old_camera))
    on_ui(VTK.RemoveFollowerView)
    check(moved > 1e-4, label + ": the follower view moved the camera", moved)
    check(max(differences) <= TOLERANCE, label + ": the cameras agree at every frame", max(differences))
    record[label] = {"part_names": part_names, "node_ids": ids, "differences": differences, "moved": moved}


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    vtkport.PMX = pmx
    scenes.setup("hinge_result_disp", pmx)
    vtkport.enable_native(["geometry"])
    vtkport.set_viewport_size(800, 600)
    on_ui(lambda: CONTROLLER.DrawScaleFactorAnimation(NUMBER_OF_FRAMES))
    record = {}
    run_follower("point", ["Lever"], node_ids("Lever", 1), record)
    run_follower("plane", ["Lever", "Lever", "Lever"], node_ids("Lever", 3), record)
    record["mirror_errors"] = list(on_ui(lambda: list(VTK.GetNativeMirrorErrors())))
    check(len(record["mirror_errors"]) == 0, "no mirror errors", record["mirror_errors"])
    vtkport.disable_native()
    record["results"] = RESULTS
    with open(os.path.join(OUT, "follower_view.json"), "w") as f:
        json.dump(record, f, indent=1)
    for result in RESULTS:
        print("%s %s %s" % tuple(result))
    print("TEST PASSED" if all(r[0] == "ok" for r in RESULTS) else "TEST FAILED")


main()
