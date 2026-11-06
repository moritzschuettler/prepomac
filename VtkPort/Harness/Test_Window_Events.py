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
send_now_function = None
USER32 = ctypes.WinDLL("user32", use_last_error=True)
USER32.GetWindow.restype = wintypes.HWND
USER32.GetWindow.argtypes = [wintypes.HWND, wintypes.UINT]
USER32.SendMessageW.restype = ctypes.c_ssize_t
USER32.SendMessageW.argtypes = [wintypes.HWND, wintypes.UINT, ctypes.c_size_t, ctypes.c_ssize_t]
USER32.GetCursor.restype = wintypes.HANDLE
USER32.LoadCursorW.restype = wintypes.HANDLE
USER32.LoadCursorW.argtypes = [wintypes.HINSTANCE, ctypes.c_void_p]
IDC_HAND = 32649


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

    def send_now(message, wparam, x, y):
        wy = height - 1 - y
        USER32.SendMessageW(child, message, wparam, (wy << 16) | (x & 0xFFFF))
    global send_now_function
    send_now_function = send_now

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
    if widgets:
        record[label].update(check_widgets_style(label, send, x, y))
    on_ui(lambda: setattr(CONTROLLER, "SelectBy", vtkSelectBy.Off))
    on_ui(CONTROLLER.ClearSelectionHistory)
    vtkport.disable_native()


def check_widgets_style(label, send, x, y):
    """The native style of the widgets area: a box selection by a drag in the window selects what the same box picks
    on the ActiViz path; the triad shows the hand cursor; a click while the camera animates selects nothing"""
    record = {}
    # Box selection by a drag
    x1, y1, x2, y2 = x - 80, y - 60, x + 80, y + 60

    def activiz_box():
        CONTROLLER.ClearSelectionHistory()
        VTK.Pick(x1, y1, True, x2, y2)
        return sorted(int(i) for i in CONTROLLER.GetSelectionIds())
    expected = on_ui(activiz_box)
    on_ui(CONTROLLER.ClearSelectionHistory)
    drag_events = []

    def drag_event(sender, e):
        drag_events.append([int(e.Event)] + [float(v) for v in e.Values])
    def subscribe(on):
        if on:
            VTK.NativeMirror.NativeEvent += drag_event
        else:
            VTK.NativeMirror.NativeEvent -= drag_event
    on_ui(lambda: subscribe(True))
    send(WM_MOUSEMOVE, 0, x1, y1)
    send(WM_LBUTTONDOWN, MK_LBUTTON, x1, y1)
    for i in range(1, 6):
        send(WM_MOUSEMOVE, MK_LBUTTON, x1 + (x2 - x1) * i // 5, y1 + (y2 - y1) * i // 5)
    send(WM_LBUTTONUP, 0, x2, y2)
    on_ui(lambda: subscribe(False))
    record["drag_events"] = [ev for ev in drag_events if ev[0] in (22, 23, 28)]
    dragged = on_ui(lambda: sorted(int(i) for i in CONTROLLER.GetSelectionIds()))
    check(len(expected) > 1 and dragged == expected, label + ": a drag selects the elements of the box",
          [len(expected), len(dragged)])
    record["box"] = {"activiz": len(expected), "drag": len(dragged)}
    on_ui(CONTROLLER.ClearSelectionHistory)
    # The box zoom of the toolbar: the next drag zooms and selects nothing
    scale = on_ui(lambda: VTK.NativeMirror.GetCamera()[9])
    on_ui(VTK.StartBoxZoom)
    send(WM_LBUTTONDOWN, MK_LBUTTON, x1, y1)
    for i in range(1, 6):
        send(WM_MOUSEMOVE, MK_LBUTTON, x1 + (x2 - x1) * i // 5, y1 + (y2 - y1) * i // 5)
    send(WM_LBUTTONUP, 0, x2, y2)
    zoomed = on_ui(lambda: VTK.NativeMirror.GetCamera()[9])
    nodes = on_ui(lambda: CONTROLLER.Selection.Nodes.Count)
    check(zoomed < 0.8 * scale and nodes == 0, label + ": the box zoom of the toolbar zooms and selects nothing",
          [scale, zoomed, nodes])
    record["box_zoom"] = [scale, zoomed]
    vtkport.frame_both(vtkport.camera_list(pmx.view.get_parameters()), "Isometric")
    # The hand cursor over the triad
    hand = USER32.LoadCursorW(None, IDC_HAND)
    rectangle = on_ui(lambda: VTK.GetNativeMirrorWidget(VTK.NativeTriadName).GetRectangle())
    cx = rectangle.X + rectangle.Width // 2
    cy = rectangle.Y + rectangle.Height // 2
    hovered = False
    for dx, dy in [(0, 0), (6, 0), (0, 6), (12, 0), (0, 12), (-6, 0), (0, -6), (18, 0), (0, 18)]:
        send(WM_MOUSEMOVE, 0, cx + dx, cy + dy)
        if on_ui(lambda: USER32.GetCursor()) == hand:
            hovered = True
            break
    check(hovered, label + ": the triad shows the hand cursor", [cx, cy])
    send(WM_MOUSEMOVE, 0, x, y)
    time.sleep(HOVER_WAIT)
    check(on_ui(lambda: USER32.GetCursor()) != hand, label + ": the cursor is back away from the triad")
    # A click while the camera of the control animates
    clicked = {}

    def start():
        import System.Windows.Forms as forms
        timer = forms.Timer()
        timer.Interval = 60

        def tick(sender, args):
            timer.Stop()
            events = []

            def native_event(sender, e):
                if int(e.Event) == 22:
                    events.append(1)
            VTK.NativeMirror.NativeEvent += native_event
            try:
                send_now_function(WM_LBUTTONDOWN, MK_LBUTTON, x, y)
                send_now_function(WM_LBUTTONUP, 0, x, y)
            finally:
                VTK.NativeMirror.NativeEvent -= native_event
            clicked["pick_events"] = len(events)
            clicked["nodes"] = CONTROLLER.Selection.Nodes.Count
            clicked["animating"] = bool(vtkport.get_field(VTK, "_animating"))
        timer.Tick += tick
        CONTROLLER.ClearSelectionHistory()
        VTK.ViewAnimationEnabled = True
        timer.Start()
        VTK.SetFrontBackView(True, True)
        VTK.ViewAnimationEnabled = False
    on_ui(start)
    check(clicked.get("animating") is True and clicked.get("pick_events") == 0 and clicked.get("nodes") == 0,
          label + ": a click while the camera animates selects nothing", clicked)
    record["animating_click"] = clicked
    on_ui(CONTROLLER.ClearSelectionHistory)
    return record


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
