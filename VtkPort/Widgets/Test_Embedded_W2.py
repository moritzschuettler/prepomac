#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, slice W2: the native view in the window, driven by real Win32 messages to its VTK window, so the
#input takes the path of the interactor and its timers: a middle drag turns the camera, with shift it pans, the
#arrow keys turn it, and the wheel zooms smoothly at the mouse until the target is reached. The same input goes to
#vtkInteractorStyleControl through its handlers, and both cameras must end alike. Writes w2_embedded.json.
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Test_Embedded_W2.py --scriptArgs "<harness>|<out>" -g No -w <work>
#
#Expected process exit code 0 and "TEST PASSED".

import ctypes
import json
import os
import sys
import time
from ctypes import wintypes

import System

HARNESS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402
from vtkControl.Native import VtkNativeNavigation  # noqa: E402

VTK = vtkport.VTK
STYLE = vtkport.get_field(VTK, "_style")
RWI = vtkport.get_field(VTK, "_renderWindowInteractor")
on_ui = vtkport.on_ui
invoke = vtkport.invoke
get_field = vtkport.get_field
WM_MOUSEMOVE, WM_MBUTTONDOWN, WM_MBUTTONUP, WM_MOUSEWHEEL = 0x0200, 0x0207, 0x0208, 0x020A
WM_KEYDOWN, WM_KEYUP = 0x0100, 0x0101
MK_SHIFT, MK_MBUTTON = 0x0004, 0x0010
VK_LEFT, VK_UP = 0x25, 0x26
CAMERA_TOLERANCE = 1e-9
RESULTS = []
USER32 = ctypes.WinDLL("user32", use_last_error=True)
USER32.GetWindow.restype = wintypes.HWND
USER32.GetWindow.argtypes = [wintypes.HWND, wintypes.UINT]
USER32.SendMessageW.restype = ctypes.c_ssize_t
USER32.SendMessageW.argtypes = [wintypes.HWND, wintypes.UINT, ctypes.c_size_t, ctypes.c_ssize_t]
USER32.ClientToScreen.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.POINT)]
OLD_HANDLERS = {"MiddleButtonDown": "vtkInteractorStyleControl_MiddleButtonPressEvt",
                "MiddleButtonUp": "vtkInteractorStyleControl_MiddleButtonReleaseEvt",
                "MouseMove": "vtkInteractorStyleControl_MouseMoveEvt",
                "WheelForward": "vtkInteractorStyleControl_MouseWheelForwardEvt",
                "KeyPress": "VtkInteractorStyleControl_KeyPressEvt",
                "KeyRelease": "VtkInteractorStyleControl_KeyReleaseEvt"}


def check(condition, text, detail=""):
    RESULTS.append(["ok" if condition else "FAIL", text, detail])


def relative(a, b):
    size = max(1.0, max(abs(v) for v in a[:6]))
    return max(abs(x - y) for x, y in zip(a, b)) / size


def old_input(events):
    """(kind, x, y, shift, key code) in VTK display coordinates to vtkInteractorStyleControl"""
    def run():
        for kind, x, y, shift, code in events:
            RWI.SetEventInformation(x, y, 0, int(shift), System.SByte(code), 0, None)
            invoke(STYLE, OLD_HANDLERS[kind], None, None)
    on_ui(run)


def main():
    vtkport.PMX = pmx
    pmx.view.current = "femodel"
    vtkport.set_viewport_size(800, 600)
    start_camera = vtkport.camera_list(pmx.view.get_parameters())
    vtkport.enable_native(["geometry", "widgets"], in_window=True)
    # The two navigations are driven apart from the same start and compared where they end
    on_ui(lambda: setattr(VTK, "NativeWindowIndependent", True))
    vtkport.parts_only(True)
    state = on_ui(lambda: {"child": USER32.GetWindow(VTK.NativeHost.Handle.ToInt64(), 5),
                           "height": VTK.NativeHost.ClientSize.Height})
    child, height = state["child"], state["height"]
    check(bool(child), "the VTK window is a child of the host")

    def win(x, y):
        return x, height - 1 - y

    def send(message, wparam, x, y):
        wx, wy = win(x, y)
        on_ui(lambda: USER32.SendMessageW(child, message, wparam, (wy << 16) | (wx & 0xFFFF)))

    def wheel(x, y, notches):
        point = wintypes.POINT(*win(x, y))
        USER32.ClientToScreen(child, ctypes.byref(point))
        delta = (120 * notches) & 0xFFFF
        on_ui(lambda: USER32.SendMessageW(child, WM_MOUSEWHEEL, delta << 16,
                                          ((point.y & 0xFFFF) << 16) | (point.x & 0xFFFF)))

    def cameras():
        old = vtkport.camera_list(pmx.view.get_parameters())
        new = on_ui(lambda: vtkport.camera_list(VTK.NativeMirror.GetCamera()))
        return old, new

    def begin():
        vtkport.frame_both(start_camera, "Isometric")
        on_ui(lambda: invoke(STYLE, "ResetRotationCenter"))
        on_ui(lambda: VTK.NativeMirror.Navigate(VtkNativeNavigation.ResetRotationCenter))
        return cameras()

    def middle_drag(start, step, count, shift):
        flags = MK_SHIFT if shift else 0
        x, y = start
        send(WM_MOUSEMOVE, flags, x, y)
        send(WM_MBUTTONDOWN, flags | MK_MBUTTON, x, y)
        events = [("MouseMove", x, y, shift, 0), ("MiddleButtonDown", x, y, shift, 0)]
        for i in range(1, count + 1):
            send(WM_MOUSEMOVE, flags | MK_MBUTTON, x + i * step[0], y + i * step[1])
            events.append(("MouseMove", x + i * step[0], y + i * step[1], shift, 0))
        end = (x + count * step[0], y + count * step[1])
        send(WM_MBUTTONUP, flags, end[0], end[1])
        events.append(("MiddleButtonUp", end[0], end[1], shift, 0))
        old_input(events)

    for name, shift in [("a middle drag turns both cameras alike", False),
                        ("a middle drag with shift pans both cameras alike", True)]:
        start = begin()
        middle_drag((400, 300), (14, -6), 6, shift)
        old, new = cameras()
        check(relative(*start) <= CAMERA_TOLERANCE and relative(old, new) <= CAMERA_TOLERANCE and
              relative(start[0], old) > 1e-6, name, "end %.1e" % relative(old, new))
    # The arrow keys
    start = begin()
    for vk, code in [(VK_LEFT, 37), (VK_UP, 38)]:
        on_ui(lambda: USER32.SendMessageW(child, WM_KEYDOWN, vk, 1))
        on_ui(lambda: USER32.SendMessageW(child, WM_KEYUP, vk, (1 << 30) | (1 << 31) | 1))
        old_input([("KeyPress", 400, 300, False, code), ("KeyRelease", 400, 300, False, code)])
    old, new = cameras()
    check(relative(old, new) <= CAMERA_TOLERANCE and relative(start[0], old) > 1e-6,
          "the arrow keys turn both cameras alike", "end %.1e" % relative(old, new))
    # The smooth zoom runs on the timer of the native window
    def smooth(on):
        def run():
            VTK.GetType().GetField("_smoothZoom", vtkport.FLAGS).SetValue(VTK, System.Boolean(on))
            invoke(VTK, "ApplyNavigationSensitivity")
        on_ui(run)
    smooth(True)
    start = begin()
    send(WM_MOUSEMOVE, 0, 300, 250)
    old_input([("MouseMove", 300, 250, False, 0)])
    for _ in range(3):
        wheel(300, 250, 1)
        old_input([("WheelForward", 300, 250, False, 0)])
    stepping = on_ui(lambda: VTK.NativeMirror.GetInteractionState()[3] == 1)
    end_time = time.time() + 5
    while time.time() < end_time and (on_ui(lambda: VTK.NativeMirror.GetInteractionState()[3] == 1) or
                                      on_ui(lambda: bool(get_field(STYLE, "_smoothZoomTimer").IsEnabled))):
        time.sleep(0.05)
    old, new = cameras()
    check(stepping, "the wheel starts the smooth zoom of the native window")
    check(relative(old, new) <= CAMERA_TOLERANCE and relative(start[0], old) > 1e-6,
          "the smooth zoom reaches the same camera", "end %.1e" % relative(old, new))
    smooth(False)
    vtkport.parts_only(False)
    on_ui(lambda: setattr(VTK, "NativeWindowIndependent", False))
    vtkport.disable_native()
    with open(os.path.join(os.path.abspath(OUT), "w2_embedded.json"), "w") as stream:
        json.dump(RESULTS, stream, indent=1)
    for status, text, detail in RESULTS:
        print("%-4s %s %s" % (status, text, detail))
    print("TEST PASSED" if all(r[0] == "ok" for r in RESULTS) else "TEST FAILED")


main()
