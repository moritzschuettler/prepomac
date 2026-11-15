#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, slice W2: the native view in the window, driven by real Win32 messages to its VTK window, so the
#input takes the path of the interactor and its timers: a middle drag turns the camera, with shift it pans, the
#arrow keys turn it, and the wheel zooms smoothly at the mouse until the target is reached. The same input then goes
#from the same start through vtkn_view_handle_input, the path of Record_W2.py, and both must end alike. Writes
#w2_embedded.json.
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
from vtkControl.Native import VtkNativeInput, VtkNativeNavigation  # noqa: E402

VTK = vtkport.VTK
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
# VTK 9 sends the arrow keys with a zero key code and their key symbol
KEY_SYMBOLS = {37: "Left", 38: "Up"}


def check(condition, text, detail=""):
    RESULTS.append(["ok" if condition else "FAIL", text, detail])


def relative(a, b):
    size = max(1.0, max(abs(v) for v in a[:6]))
    return max(abs(x - y) for x, y in zip(a, b)) / size


def native_input(events):
    """(kind, x, y, shift, key code) in VTK display coordinates to the native style through vtkn_view_handle_input"""
    def run():
        for kind, x, y, shift, code in events:
            VTK.NativeMirror.HandleInput(getattr(VtkNativeInput, kind), x, y, False, shift,
                                         0 if code in KEY_SYMBOLS else code, KEY_SYMBOLS.get(code))
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

    def camera():
        return on_ui(lambda: vtkport.camera_list(VTK.NativeMirror.GetCamera()))

    def begin():
        vtkport.frame_both(start_camera, "Isometric")
        on_ui(lambda: VTK.NativeMirror.Navigate(VtkNativeNavigation.ResetRotationCenter))
        return camera()

    def wait_for_smooth_zoom():
        end_time = time.time() + 5
        while time.time() < end_time and on_ui(lambda: VTK.NativeMirror.GetInteractionState()[3] == 1):
            time.sleep(0.05)
        on_ui(lambda: VTK.NativeMirror.Navigate(VtkNativeNavigation.FinishSmoothZoom))

    def compare(name, run_messages, run_input):
        """Runs the Win32 messages and then the same input from the same start; returns whether the messages
        started the smooth zoom"""
        start = [begin()]
        stepping = run_messages()
        ends = [camera()]
        start.append(begin())
        run_input()
        ends.append(camera())
        check(relative(*start) <= CAMERA_TOLERANCE and relative(*ends) <= CAMERA_TOLERANCE and
              relative(start[0], ends[0]) > 1e-6, name, "end %.1e" % relative(*ends))
        return stepping

    def middle_drag(start, step, count, shift):
        flags = MK_SHIFT if shift else 0
        x, y = start
        events = [("MouseMove", x, y, shift, 0), ("MiddleButtonDown", x, y, shift, 0)]
        for i in range(1, count + 1):
            events.append(("MouseMove", x + i * step[0], y + i * step[1], shift, 0))
        end = (x + count * step[0], y + count * step[1])
        events.append(("MiddleButtonUp", end[0], end[1], shift, 0))

        def messages():
            send(WM_MOUSEMOVE, flags, x, y)
            send(WM_MBUTTONDOWN, flags | MK_MBUTTON, x, y)
            for i in range(1, count + 1):
                send(WM_MOUSEMOVE, flags | MK_MBUTTON, x + i * step[0], y + i * step[1])
            send(WM_MBUTTONUP, flags, end[0], end[1])
        return messages, lambda: native_input(events)

    for name, shift in [("a middle drag turns the camera as the style input does", False),
                        ("a middle drag with shift pans the camera as the style input does", True)]:
        compare(name, *middle_drag((400, 300), (14, -6), 6, shift))
    # The arrow keys
    keys = [(VK_LEFT, 37), (VK_UP, 38)]

    def key_messages():
        for vk, code in keys:
            on_ui(lambda: USER32.SendMessageW(child, WM_KEYDOWN, vk, 1))
            on_ui(lambda: USER32.SendMessageW(child, WM_KEYUP, vk, (1 << 30) | (1 << 31) | 1))

    def key_input():
        for vk, code in keys:
            native_input([("KeyPress", 400, 300, False, code), ("KeyRelease", 400, 300, False, code)])
    compare("the arrow keys turn the camera as the style input does", key_messages, key_input)
    # The smooth zoom runs on the timer of the native window
    def smooth(on):
        def run():
            VTK.GetType().GetField("_smoothZoom", vtkport.FLAGS).SetValue(VTK, System.Boolean(on))
            invoke(VTK, "ApplyNavigationSensitivity")
        on_ui(run)
    smooth(True)

    def wheel_messages():
        send(WM_MOUSEMOVE, 0, 300, 250)
        for _ in range(3):
            wheel(300, 250, 1)
        stepping = on_ui(lambda: VTK.NativeMirror.GetInteractionState()[3] == 1)
        wait_for_smooth_zoom()
        return stepping

    def wheel_input():
        native_input([("MouseMove", 300, 250, False, 0)] + [("WheelForward", 300, 250, False, 0)] * 3)
        wait_for_smooth_zoom()
    stepping = compare("the smooth zoom reaches the camera of the style input", wheel_messages, wheel_input)
    check(stepping, "the wheel starts the smooth zoom of the native window")
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
