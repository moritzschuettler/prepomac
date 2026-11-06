#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, slice W2: the interaction style. The geometry and widgets areas are on with the mirror as the
#target and both paths show only the parts. Each sequence starts both cameras from the same framed view and sends
#the same input to both: to vtkInteractorStyleControl through its handlers, to the native style through
#vtkn_view_handle_input. Recorded: both cameras after each sequence and the box selection state, for Compare_W2.py.
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Record_W2.py --scriptArgs "<harness>|<out>" -g No -w <work>
#
#Expected process exit code 0.

import json
import os
import sys
import time

import System

HARNESS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402
from vtkControl.Native import VtkNativeInput, VtkNativeNavigation  # noqa: E402

VTK = vtkport.VTK
STYLE = vtkport.get_field(VTK, "_style")
RWI = vtkport.get_field(VTK, "_renderWindowInteractor")
on_ui = vtkport.on_ui
invoke = vtkport.invoke
get_field = vtkport.get_field
WIDTH, HEIGHT = 800, 600
OLD_HANDLERS = {"MiddleButtonDown": "vtkInteractorStyleControl_MiddleButtonPressEvt",
                "MiddleButtonUp": "vtkInteractorStyleControl_MiddleButtonReleaseEvt",
                "LeftButtonDown": "vtkInteractorStyleControl_LeftButtonPressEvt",
                "LeftButtonUp": "vtkInteractorStyleControl_LeftButtonReleaseEvt",
                "MouseMove": "vtkInteractorStyleControl_MouseMoveEvt",
                "WheelForward": "vtkInteractorStyleControl_MouseWheelForwardEvt",
                "WheelBackward": "vtkInteractorStyleControl_MouseWheelBackwardEvt",
                "KeyPress": "VtkInteractorStyleControl_KeyPressEvt",
                "KeyRelease": "VtkInteractorStyleControl_KeyReleaseEvt"}
# VTK 5.8 reported the virtual key of an arrow key as its key code; VTK 9 reports the key symbol
KEYS = {"Left": (37, "Left"), "Up": (38, "Up"), "Right": (39, "Right"), "Down": (40, "Down"),
        "Escape": (27, "Escape"), "z": (122, "z")}


def event(kind, x=0, y=0, ctrl=False, shift=False, key=None):
    return (kind, x, y, ctrl, shift, key)


def send(events):
    """The same input to the old style, then to the native one"""
    def old():
        for kind, x, y, ctrl, shift, key in events:
            code, symbol = KEYS[key] if key else (0, None)
            RWI.SetEventInformation(x, y, int(ctrl), int(shift), System.SByte(code), 0, symbol)
            invoke(STYLE, OLD_HANDLERS[kind], None, None)

    def new():
        for kind, x, y, ctrl, shift, key in events:
            code, symbol = KEYS[key] if key else (0, None)
            # VTK 9 sends the arrow keys with a zero key code
            VTK.NativeMirror.HandleInput(getattr(VtkNativeInput, kind), x, y, ctrl, shift,
                                         0 if code < 41 and key != "Escape" else code, symbol)
    on_ui(old)
    on_ui(new)


def drag(button, start, step, count, ctrl=False, shift=False):
    x, y = start
    events = [event("MouseMove", x, y, ctrl, shift), event(button + "Down", x, y, ctrl, shift)]
    for i in range(1, count + 1):
        events.append(event("MouseMove", x + i * step[0], y + i * step[1], ctrl, shift))
    events.append(event(button + "Up", x + count * step[0], y + count * step[1], ctrl, shift))
    return events


def cameras():
    old = vtkport.camera_list(pmx.view.get_parameters())
    new = on_ui(lambda: vtkport.camera_list(VTK.NativeMirror.GetCamera()))
    return old, new


def wait(condition, seconds=5.0):
    end = time.time() + seconds
    while time.time() < end and not condition():
        time.sleep(0.05)
    time.sleep(0.1)


def old_animating():
    return on_ui(lambda: bool(get_field(VTK, "_animating")))


def old_smooth_zooming():
    return on_ui(lambda: bool(get_field(STYLE, "_smoothZoomTimer").IsEnabled))


def set_smooth_zoom(on):
    def run():
        VTK.GetType().GetField("_smoothZoom", vtkport.FLAGS).SetValue(VTK, System.Boolean(on))
        invoke(VTK, "ApplyNavigationSensitivity")
    on_ui(run)


def start(start_camera):
    old, new = vtkport.frame_both(start_camera, "Isometric")

    def reset():
        invoke(STYLE, "ResetRotationCenter")
        VTK.NativeMirror.Navigate(VtkNativeNavigation.ResetRotationCenter)
    on_ui(reset)
    return old, new


def main():
    out_dir = os.path.abspath(OUT)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    vtkport.PMX = pmx
    started = time.time()
    pmx.view.current = "femodel"
    vtkport.set_viewport_size(WIDTH, HEIGHT)
    meta = {"sequences": []}
    vtkport.enable_native(["geometry", "widgets"])
    vtkport.parts_only(True)
    start_camera = vtkport.camera_list(pmx.view.get_parameters())
    set_smooth_zoom(False)
    sequences = [
        ("rotate", [drag("MiddleButton", (400, 300), (12, 7), 6)], None),
        ("rotate_long", [drag("MiddleButton", (300, 400), (-25, -15), 12)], None),
        ("pan", [drag("MiddleButton", (400, 300), (-9, 6), 5, shift=True)], None),
        ("slow_zoom", [drag("MiddleButton", (400, 300), (3, 8), 5, ctrl=True)], None),
        # The release of a drag without a horizontal move counts as a click and picks the rotation center
        ("slow_zoom_vertical", [drag("MiddleButton", (400, 300), (0, 8), 5, ctrl=True)], "picked"),
        ("wheel", [[event("MouseMove", 250, 350), event("WheelForward", 250, 350), event("WheelForward", 250, 350),
                    event("WheelForward", 250, 350), event("MouseMove", 520, 200), event("WheelBackward", 520, 200)]],
         None),
        ("wheel_ctrl", [[event("WheelForward", 600, 150, ctrl=True), event("WheelForward", 600, 150, ctrl=True)]],
         None),
        ("keys", [[event("KeyPress", 400, 300, key="Left"), event("KeyRelease", 400, 300, key="Left"),
                   event("KeyPress", 400, 300, key="Up"), event("KeyRelease", 400, 300, key="Up"),
                   event("KeyPress", 400, 300, key="Right"), event("KeyRelease", 400, 300, key="Right"),
                   event("KeyPress", 400, 300, key="Down"), event("KeyRelease", 400, 300, key="Down")]], None),
        ("rotation_center_beside_model", [drag("MiddleButton", (15, 585), (0, 0), 0),
                                          drag("MiddleButton", (400, 300), (12, -9), 6)], None),
        ("rotation_center_on_model", [drag("MiddleButton", (400, 300), (0, 0), 0),
                                      drag("MiddleButton", (300, 250), (-10, 12), 6)], "picked"),
        ("zoom_then_rotate", [[event("WheelForward", 200, 200), event("WheelForward", 200, 200)],
                              drag("MiddleButton", (200, 200), (15, 5), 6)], None),
        ("box_zoom", [[event("KeyPress", 200, 150, key="z")], drag("LeftButton", (200, 150), (60, 54), 5),
                      [event("KeyRelease", 500, 420, key="z")]], "animated"),
    ]
    for name, groups, kind in sequences:
        old_start, new_start = start(start_camera)
        for events in groups:
            send(events)
            if kind == "animated":
                wait(lambda: not old_animating())
        old, new = cameras()
        meta["sequences"].append({"name": name, "kind": kind, "start": [old_start, new_start], "old": old,
                                  "new": new})
    # The box selection: drawn while the left button drags, cancelled by escape
    start(start_camera)
    send([event("MouseMove", 300, 200), event("LeftButtonDown", 300, 200), event("MouseMove", 340, 240),
          event("MouseMove", 450, 350)])
    old_active = on_ui(lambda: bool(STYLE.IsBoxSelectionActive))
    new_active = on_ui(lambda: VTK.NativeMirror.GetInteractionState()[1] == 1)
    send([event("KeyPress", 450, 350, key="Escape"), event("KeyRelease", 450, 350, key="Escape")])
    old_cancelled = on_ui(lambda: not bool(STYLE.IsBoxSelectionActive))
    new_cancelled = on_ui(lambda: VTK.NativeMirror.GetInteractionState()[1] == 0)
    send([event("LeftButtonUp", 450, 350)])
    meta["box_selection"] = {"old": [old_active, old_cancelled], "new": [new_active, new_cancelled]}
    # The smooth zoom: the notches accumulate and the camera approaches the target over a few frames; the native
    # style is stepped to the end at once, as no timer runs off screen
    set_smooth_zoom(True)
    old_start, new_start = start(start_camera)
    send([event("MouseMove", 300, 300), event("WheelForward", 300, 300), event("WheelForward", 300, 300),
          event("WheelBackward", 300, 300), event("WheelForward", 300, 300)])
    pending = on_ui(lambda: VTK.NativeMirror.GetInteractionState()[3] == 1)
    wait(lambda: not old_smooth_zooming())
    on_ui(lambda: VTK.NativeMirror.Navigate(VtkNativeNavigation.FinishSmoothZoom))
    old, new = cameras()
    meta["sequences"].append({"name": "smooth_zoom", "kind": None, "start": [old_start, new_start], "old": old,
                              "new": new, "native_pending_before_finish": pending})
    set_smooth_zoom(False)
    vtkport.parts_only(False)
    vtkport.disable_native()
    meta["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, "w2.json"), "w") as stream:
        json.dump(meta, stream, indent=1)
    print("Sequences: %d, %.1f s" % (len(meta["sequences"]), meta["seconds"]))
    print("W2 RECORDED")


main()
