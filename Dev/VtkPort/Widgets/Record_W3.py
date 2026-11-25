#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, slice W3: the touchpad and trackpad navigation and the widget context menu of the native view,
#which shows only the parts. Gestures go through the path of vtkControl itself (the gesture recognizer's update, the
#flush on idle, the end of the gesture); the camera is recorded after each. Then a right click on an annotation goes
#to the native style and must open its menu. Writes w3.json for Compare_W3.py. ActiViz is gone: the old side is
#written as the new one and replaced by the ActiViz run of the reference (graft_reference.py, PORT_REFERENCE).
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Record_W3.py --scriptArgs "<harness>|<out>" -g No -w <work>
#
#Expected process exit code 0.

import clr
import json
import os
import sys
import time

import System

HARNESS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402

clr.AddReference("System.Drawing")
clr.AddReference("System.Windows.Forms")
from System.Drawing import Rectangle  # noqa: E402
from System.Windows.Forms import Keys, MouseEventArgs  # noqa: E402
from vtkControl.Native import VtkNativeInput, VtkNativeNavigation  # noqa: E402

VTK = vtkport.VTK
on_ui = vtkport.on_ui
invoke = vtkport.invoke


def gesture(updates):
    """(drag x mm, drag y mm, spread ratio) updates of the gesture recognizer, each flushed as on idle"""
    def run():
        for drag_x, drag_y, spread in updates:
            invoke(VTK, "gestureRecognizer_GestureUpdated", float(drag_x), float(drag_y), float(spread))
            invoke(VTK, "FlushTrackpadGesture")
        invoke(VTK, "trackpadGestureEndTimer_Tick", None, System.EventArgs.Empty)
    on_ui(run)


def cameras():
    old = vtkport.camera_list(pmx.view.get_parameters())
    new = on_ui(lambda: vtkport.camera_list(VTK.NativeMirror.GetCamera()))
    return old, new


def settings(two_finger_drag_pans=False, invert_x=False, pan_sensitivity=1.0):
    on_ui(lambda: VTK.SetTrackpadNavigation(True, True, two_finger_drag_pans, pan_sensitivity, invert_x, False))


def start(start_camera):
    old, new = vtkport.frame_both(start_camera, "Isometric")

    def reset():
        VTK.NativeMirror.Navigate(VtkNativeNavigation.ResetRotationCenter)
    on_ui(reset)
    return [old, new]


def main():
    out_dir = os.path.abspath(OUT)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    vtkport.PMX = pmx
    started = time.time()
    pmx.view.current = "femodel"
    vtkport.set_viewport_size(800, 600)
    vtkport.enable_native(["geometry", "widgets"])
    vtkport.parts_only(True)
    start_camera = vtkport.camera_list(pmx.view.get_parameters())
    meta = {"sequences": []}
    sequences = [
        ("two_finger_rotate", {}, [(3.0, -2.0, 1.0)] * 4),
        ("pinch_zoom", {}, [(0.0, 0.0, 1.08)] * 3 + [(0.0, 0.0, 0.95)]),
        ("rotate_and_zoom", {}, [(1.0, 1.5, 1.05)] * 3),
        ("two_finger_pan", {"two_finger_drag_pans": True, "pan_sensitivity": 1.5}, [(2.5, 1.5, 1.0)] * 4),
        ("inverted_x", {"invert_x": True}, [(-4.0, 1.0, 1.0)] * 3),
    ]
    for name, options, updates in sequences:
        settings(**options)
        first = start(start_camera)
        gesture(updates)
        old, new = cameras()
        meta["sequences"].append({"name": name, "start": first, "old": old, "new": new})
    settings()
    # A right click on an annotation opens its menu through Form_WidgetPicked on both paths
    clicks = []

    def on_picked(e, keys, name, rectangle):
        clicks.append([name, [rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height], e.Button.ToString()])
    picked_handler = System.Action[MouseEventArgs, Keys, System.String, Rectangle](on_picked)

    def setup_menu():
        saved = [VTK.Form_WidgetPicked, VTK.SelectBy]
        VTK.Form_WidgetPicked = picked_handler
        VTK.SelectBy = System.Enum.ToObject(VTK.SelectBy.GetType(), 1)
        VTK.SetWidgetInteractionEnabled(True)
        camera = vtkport.camera_list(pmx.view.get_parameters())
        anchor = System.Array[System.Double](camera[3:6])
        VTK.AddArrowWidget("Arrow_Menu", "Menu test", "G4", anchor, True, True, True)
        return saved
    saved = on_ui(setup_menu)
    rectangle = on_ui(lambda: VTK.GetNativeMirrorWidget("Arrow_Menu").GetRectangle())
    x, y = rectangle.X + rectangle.Width // 2, rectangle.Y + rectangle.Height // 2

    def new_click():
        VTK.NativeMirror.HandleInput(VtkNativeInput.RightButtonDown, x, y)
        VTK.NativeMirror.HandleInput(VtkNativeInput.RightButtonUp, x, y)
    on_ui(new_click)
    meta["context_menu"] = {"point": [x, y], "old": list(clicks), "new": list(clicks)}

    def restore():
        VTK.RemoveArrowWidgets(System.Array[System.String](["Arrow_Menu"]))
        VTK.Form_WidgetPicked = saved[0]
        VTK.SelectBy = saved[1]
    on_ui(restore)
    vtkport.parts_only(False)
    vtkport.disable_native()
    meta["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, "w3.json"), "w") as stream:
        json.dump(meta, stream, indent=1)
    print("Sequences: %d, %.1f s" % (len(meta["sequences"]), meta["seconds"]))
    print("W3 RECORDED")


main()
