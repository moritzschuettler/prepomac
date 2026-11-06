#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, integration: the native view in the window receives one handler for its mouse selection. With the
#widgets area on, the native style raises the pick, hover and clear events that vtkInteractorStyleControl raises and
#the widgets handler routes them; without it, the selection area handles the raw button and move events. In both
#cases a real click (Win32 messages to the VTK window) adds exactly one selection node and a move over the model
#draws a hover highlight. Writes window_events.json.
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Test_Window_Events.py --scriptArgs "<harness>|<out>" -g No -w <work>
#
#Expected process exit code 0 and "TEST PASSED".

import ctypes
import json
import os
import sys
import time
from ctypes import wintypes

HARNESS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402
from CaeGlobals import vtkSelectBy, vtkSelectItem  # noqa: E402

VTK = vtkport.VTK
CONTROLLER = vtkport.CONTROLLER
on_ui = vtkport.on_ui
WM_MOUSEMOVE, WM_LBUTTONDOWN, WM_LBUTTONUP = 0x0200, 0x0201, 0x0202
MK_LBUTTON = 0x0001
SIZE = (800, 600)
HOVER_WAIT = 0.5  # the native style picks on a selection timer, as vtkInteractorStyleControl does
RESULTS = []
USER32 = ctypes.WinDLL("user32", use_last_error=True)
USER32.GetWindow.restype = wintypes.HWND
USER32.GetWindow.argtypes = [wintypes.HWND, wintypes.UINT]
USER32.SendMessageW.restype = ctypes.c_ssize_t
USER32.SendMessageW.argtypes = [wintypes.HWND, wintypes.UINT, ctypes.c_size_t, ctypes.c_ssize_t]


def check(condition, text, detail=""):
    RESULTS.append(["ok" if condition else "FAIL", text, str(detail)])


def handler_names():
    def run():
        field = vtkport.get_field(VTK.NativeMirror, "NativeEvent")
        if field is None:
            return []
        return sorted(d.Method.Name for d in field.GetInvocationList())
    return on_ui(run)


def hit_point():
    width, height = SIZE
    for j in range(5):
        for i in range(5):
            x = int(round((i + 0.5) * width / 5))
            y = int(round((j + 0.5) * height / 5))
            _, values = on_ui(lambda: vtkport.invoke(VTK, "NativeMirrorPickPoint", x, y, count=4))
            if values[2] is not None:
                return x, y
    return None


def run_mode(areas, record):
    label = "+".join(areas)
    vtkport.enable_native(areas, in_window=True)
    vtkport.set_viewport_size(*SIZE)
    vtkport.frame_both(vtkport.camera_list(pmx.view.get_parameters()), "Isometric")
    names = handler_names()
    widgets = "widgets" in areas
    check(("NativeMirror_InteractionEvent" in names) == widgets, label + ": the widgets handler only with widgets",
          names)
    check(("NativeWindow_SelectionEvent" in names) != widgets, label + ": the raw selection handler only without "
          "widgets", names)
    child, height = on_ui(lambda: [USER32.GetWindow(VTK.NativeHost.Handle.ToInt64(), 5),
                                   VTK.NativeHost.ClientSize.Height])
    point = hit_point()
    check(point is not None, label + ": a point of the view hits the model")
    if point is None:
        vtkport.disable_native()
        return

    def send(message, wparam, x, y):
        wy = height - 1 - y
        on_ui(lambda: USER32.SendMessageW(child, message, wparam, (wy << 16) | (x & 0xFFFF)))

    def set_mode():
        CONTROLLER.ClearSelectionHistory()
        CONTROLLER.SelectItem = vtkSelectItem.Element
        CONTROLLER.SelectBy = vtkSelectBy.Element
    on_ui(set_mode)
    base = os.path.join(OUT, label + "_base.png")
    hover = os.path.join(OUT, label + "_hover.png")
    send(WM_MOUSEMOVE, 0, 2, 2)
    time.sleep(HOVER_WAIT)
    on_ui(lambda: VTK.NativeMirror.WritePng(base, 1))
    x, y = point
    send(WM_MOUSEMOVE, 0, x, y)
    time.sleep(HOVER_WAIT)
    on_ui(lambda: VTK.NativeMirror.WritePng(hover, 1))
    check(open(base, "rb").read() != open(hover, "rb").read(), label + ": a move over the model draws a hover")
    send(WM_LBUTTONDOWN, MK_LBUTTON, x, y)
    send(WM_LBUTTONUP, 0, x, y)
    nodes, ids = on_ui(lambda: [CONTROLLER.Selection.Nodes.Count, list(CONTROLLER.GetSelectionIds())])
    check(nodes == 1, label + ": a click adds one selection node", nodes)
    check(len(ids) == 1, label + ": a click selects one element", ids)
    record[label] = {"handlers": names, "point": point, "nodes": nodes, "ids": [int(i) for i in ids]}
    on_ui(lambda: setattr(CONTROLLER, "SelectBy", vtkSelectBy.Off))
    on_ui(CONTROLLER.ClearSelectionHistory)
    vtkport.disable_native()


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    vtkport.PMX = pmx
    pmx.view.current = "femodel"
    record = {}
    run_mode(["geometry", "selection"], record)
    run_mode(["geometry", "widgets", "selection"], record)
    record["results"] = RESULTS
    with open(os.path.join(OUT, "window_events.json"), "w") as f:
        json.dump(record, f, indent=1)
    for result in RESULTS:
        print("%s %s %s" % tuple(result))
    print("TEST PASSED" if all(r[0] == "ok" for r in RESULTS) else "TEST FAILED")


main()
