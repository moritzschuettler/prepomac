#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native pilot, slice B: the scalar bar in the embedded VTK 9.7 view, driven by real Win32 mouse messages to the
#VTK window, so the events take the interactor path: a drag on the bar moves the bar and not the camera, a drag
#beside it turns the camera, and a double click on a value label reaches the C# event with that label.
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Test_Embedded_ScalarBar.py -g No -w <work>
#
#Expected process exit code 0 and "TEST PASSED".

import clr
import time

import System
from System import Action
from System.Reflection import BindingFlags

clr.AddReference("vtkControl")
clr.AddReference("System.Drawing")
from vtkControl.Native import VtkNativeScalarBar, VtkNativeTextStyle, VtkNativeWidgetAnchor

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
WM_MOUSEMOVE = 0x0200
WM_LBUTTONDOWN = 0x0201
WM_LBUTTONUP = 0x0202
WM_MBUTTONDOWN = 0x0207
WM_MBUTTONUP = 0x0208
MK_LBUTTON = 1
MK_MBUTTON = 0x0010
RESULTS = []
EMPTY = System.Drawing.Rectangle.Empty


def main_form():
    assembly = [a for a in System.AppDomain.CurrentDomain.GetAssemblies() if a.GetName().Name == "PrePoMax"][0]
    return assembly.GetType("PrePoMax.Program").GetField("_mainForm", FLAGS).GetValue(None)


FORM = main_form()
VTK = FORM.GetType().GetField("_vtk", FLAGS).GetValue(FORM)


def on_ui(function):
    box = {}

    def run():
        try:
            box["value"] = function()
        except Exception as ex:
            box["error"] = ex
    FORM.Invoke(Action(run))
    if "error" in box:
        raise box["error"]
    return box.get("value")


def check(condition, text, detail=""):
    RESULTS.append("%s %s %s" % ("ok  " if condition else "FAIL", text, detail))


def define_user32():
    import ctypes
    from ctypes import wintypes

    class _User32(object):
        def __init__(self):
            self._dll = ctypes.WinDLL("user32", use_last_error=True)
            self._dll.GetWindow.restype = wintypes.HWND
            self._dll.GetWindow.argtypes = [wintypes.HWND, wintypes.UINT]
            self._dll.SendMessageW.restype = ctypes.c_ssize_t
            self._dll.SendMessageW.argtypes = [wintypes.HWND, wintypes.UINT, ctypes.c_size_t, ctypes.c_ssize_t]

        def GetWindow(self, window, command):
            return self._dll.GetWindow(window.ToInt64(), command) or 0

        def SendMessage(self, window, message, wparam, x, y):
            return self._dll.SendMessageW(window, message, wparam, (y << 16) | (x & 0xFFFF))
    return _User32()


USER32 = define_user32()


def grid(n, size):
    points = []
    for k in range(n + 1):
        for j in range(n + 1):
            for i in range(n + 1):
                points += [i * size / n, j * size / n, k * size / n]

    def index(i, j, k):
        return i + j * (n + 1) + k * (n + 1) * (n + 1)
    cells = []
    for k in range(n):
        for j in range(n):
            for i in range(n):
                cells += [8, index(i, j, k), index(i + 1, j, k), index(i + 1, j + 1, k), index(i, j + 1, k),
                          index(i, j, k + 1), index(i + 1, j, k + 1), index(i + 1, j + 1, k + 1), index(i, j + 1, k + 1)]
    return (System.Array[System.Double](points), System.Array[System.Int32](cells),
            System.Array[System.Byte]([12] * (n * n * n)))


def color_at(t):
    return System.Array[System.Double]([t, 0.3, 1 - t])


def main():
    state = {}
    double_clicks = []

    def setup():
        VTK.ShowNativeView(True)
        System.Windows.Forms.Application.DoEvents()
        host = VTK.NativeHost
        view = host.View
        points, cells, types = grid(6, 1.0)
        view.AddMeshActor("grid", points, cells, types, System.Array[System.Double]([0.6, 0.8, 0.6]), True)
        view.ResetCamera()
        bar = VtkNativeScalarBar(view)
        bar.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle())
        bar.SetPadding(15)
        bar.SetLabelFormat("E3")
        bar.SetNumberOfColors(9)
        bar.CreateLookupTable(System.Func[System.Double, System.Array[System.Double]](color_at), 0.0, 100.0)
        bar.SetText("DISP", "ALL", "mm", None, "Automatic")
        bar.SetFrame(True, System.Array[System.Double]([1, 1, 1]), True, System.Array[System.Double]([0, 0, 0]))
        bar.SetPosition(VtkNativeWidgetAnchor.TopLeft, 20, 20)
        view.Render()

        def on_double_click(sender, e):
            ok, edit_minimum, rectangle, text, low, high = bar.TryGetValueEditData(e.X, e.Y, False, EMPTY, "", 0.0,
                                                                                     0.0)
            double_clicks.append([bool(ok), bool(edit_minimum), text, low, high])
        bar.MouseDoubleClick += on_double_click
        state.update({"host": host, "view": view, "bar": bar,
                      "child": USER32.GetWindow(host.Handle, 5), "height": host.ClientSize.Height,
                      "width": host.ClientSize.Width})
    on_ui(setup)
    view = state["view"]
    bar = state["bar"]
    child = state["child"]
    height = state["height"]
    check(child != 0, "the VTK window is a child of the host")

    def color_pixels():
        # Saturated pixels in the column through the color boxes; nothing else in the view is saturated
        def read():
            r = bar.GetRectangle()
            pixels, width, h = view.ReadPixels(1)
            x = r.X + 15 + 8
            count = 0
            for y in range(h):
                i = 3 * (y * width + x)
                values = [pixels[i], pixels[i + 1], pixels[i + 2]]
                if max(values) - min(values) > 100:
                    count += 1
            return count
        return on_ui(read)
    saturated = color_pixels()
    check(saturated > 100, "the color boxes show over the frame before the mouse moves", "%d pixels" % saturated)

    def send(message, wparam, x, y):
        on_ui(lambda: USER32.SendMessage(child, message, wparam, x, y))

    def rectangle():
        r = on_ui(lambda: bar.GetRectangle())
        return [r.X, r.Y, r.Width, r.Height]

    def camera():
        return [round(v, 9) for v in on_ui(lambda: list(view.GetCamera()))]
    #
    start = rectangle()
    camera_start = camera()
    # Window coordinates: the origin top left; the bar is anchored top left at (20, 20)
    x, y = start[0] + 20, start[1] + 20
    send(WM_MOUSEMOVE, 0, x, y)
    send(WM_LBUTTONDOWN, MK_LBUTTON, x, y)
    for step in range(1, 5):
        send(WM_MOUSEMOVE, MK_LBUTTON, x + 10 * step, y + 8 * step)
    send(WM_LBUTTONUP, 0, x + 40, y + 32)
    moved = rectangle()
    check(moved == [start[0] + 40, start[1] + 32, start[2], start[3]], "a drag on the bar moves the bar",
          "%s -> %s" % (start, moved))
    check(camera() == camera_start, "a drag on the bar leaves the camera")
    time.sleep(0.7)
    # Beside the bar the style turns the camera with the middle button, as PrePoMax does (stream W ported the style)
    bx, by = state["width"] - 80, height - 80
    send(WM_MOUSEMOVE, 0, bx, by)
    send(WM_MBUTTONDOWN, MK_MBUTTON, bx, by)
    for step in range(1, 5):
        send(WM_MOUSEMOVE, MK_MBUTTON, bx - 15 * step, by - 5 * step)
    send(WM_MBUTTONUP, 0, bx - 60, by - 20)
    check(camera() != camera_start, "a drag beside the bar turns the camera")
    check(rectangle() == moved, "a drag beside the bar leaves the bar")
    time.sleep(0.7)
    # A double click on the maximum label: its box is in display coordinates, the origin bottom left
    ok, box, text, low, high = on_ui(lambda: bar.TryGetValueEditData(False, EMPTY, "", 0.0, 0.0))
    cx = box.X + box.Width // 2
    cy = height - 1 - (box.Y + box.Height // 2)
    for message in [WM_LBUTTONDOWN, WM_LBUTTONUP, WM_LBUTTONDOWN, WM_LBUTTONUP]:
        send(message, MK_LBUTTON if message == WM_LBUTTONDOWN else 0, cx, cy)
    check(len(double_clicks) == 1 and double_clicks[0][0] and not double_clicks[0][1],
          "a double click on the maximum label raises the double click with that label", str(double_clicks))
    check(rectangle() == moved, "a double click does not move the bar")
    #
    def cleanup():
        bar.Dispose()
        view.RemoveActor("grid")
        VTK.ShowNativeView(False)
        System.Windows.Forms.Application.DoEvents()
    on_ui(cleanup)
    for line in RESULTS:
        print(line)
    failed = [line for line in RESULTS if line.startswith("FAIL")]
    print("TEST PASSED" if not failed else "TEST FAILED")


main()
