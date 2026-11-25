#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native pilot, phase 1: checks the groundwork inside a running PrePoMax - the vtkNative library and VTK 9.7 load,
#the C API conventions hold (handles, status codes and the last error, arrays and strings in both directions,
#callbacks), a view renders off screen to a PNG, the view of vtkControl is the native view embedded in it, and the
#view renders the phase 0 reference scene again afterwards (Run_Groundwork.sh sets it beside the ActiViz picture of
#phase 0).
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Test_Groundwork.py --scriptArgs "<out dir>|<reference dir>" -g No
#
#Expected process exit code 0.

import clr
import os
import sys
import time

import System
from System import Action
from System.Reflection import BindingFlags

clr.AddReference("vtkControl")
from vtkControl.Native import VtkNativeEvent, VtkNativeException, VtkNativeLibrary, VtkNativeStatus, VtkNativeView

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
RESULTS = []
PROGRESS = [None]


def progress(text):
    # Written and flushed line by line, so a crash of the process still shows the last step
    if PROGRESS[0] is not None:
        with open(PROGRESS[0], "a", encoding="utf-8") as stream:
            stream.write(time.strftime("%H:%M:%S ") + text + "\n")


def check(condition, message):
    if not condition:
        progress("FAILED " + message)
        raise Exception("TEST FAILED: " + message)
    RESULTS.append("ok   " + message)
    progress("ok   " + message)


def get_field(instance, name):
    return instance.GetType().GetField(name, FLAGS).GetValue(instance)


def main_form():
    assembly = [a for a in System.AppDomain.CurrentDomain.GetAssemblies() if a.GetName().Name == "PrePoMax"][0]
    return assembly.GetType("PrePoMax.Program").GetField("_mainForm", FLAGS).GetValue(None)


FORM = main_form()
VTK = get_field(FORM, "_vtk")
CONTROLLER = get_field(FORM, "_controller")


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


def expect_native_error(function, status, text, message):
    try:
        on_ui(function)
    except VtkNativeException as ex:
        check(ex.Status == status and text in ex.Message, message + " (" + str(ex.Status) + ": " + ex.Message + ")")
        return
    raise Exception("TEST FAILED: " + message + " - no VtkNativeException")


def grid(n, size):
    # n x n x n hexahedra of the given size: points, legacy cells and cell types
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


def color(r, g, b):
    return System.Array[System.Double]([r, g, b])


def foreground_fraction(pixels, width, height):
    # The share of pixels that differ from the bottom left corner, which is background
    data = bytes(pixels)
    background = data[0:3]
    step = 7
    total = 0
    different = 0
    for offset in range(0, width * height * 3, 3 * step):
        total += 1
        if abs(data[offset] - background[0]) + abs(data[offset + 1] - background[1]) + \
                abs(data[offset + 2] - background[2]) > 30:
            different += 1
    return different / float(total)


def loaded_modules():
    names = []
    for module in System.Diagnostics.Process.GetCurrentProcess().Modules:
        names.append(module.ModuleName.lower())
    return names


def test_load_and_versions():
    started = time.time()
    on_ui(lambda: VtkNativeLibrary.EnsureLoaded())
    load_seconds = time.time() - started
    native_version = on_ui(lambda: VtkNativeLibrary.GetVtkVersion())
    check(native_version == "9.7.1", "vtkNative reports VTK 9.7.1, got " + native_version)
    modules = loaded_modules()
    check("vtkcommoncore-9.7.dll" in modules and "vtkrenderingopengl2-9.7.dll" in modules,
          "the VTK 9.7 native modules are loaded")
    RESULTS.append("info library loaded in %.2f s; VTK 9.7 modules in the process: %d" %
                   (load_seconds, len([m for m in modules if m.endswith("-9.7.dll")])))


def test_off_screen(out_dir):
    logs = []

    def on_log(level, message):
        logs.append((level, message))
    handler = System.Action[System.Int32, System.String](on_log)
    VtkNativeLibrary.LogMessage += handler
    try:
        def build():
            view = VtkNativeView.CreateOffScreen(640, 480)
            view.SetBackground(color(0.85, 0.85, 0.85), color(1.0, 1.0, 1.0))
            points, cells, types = grid(10, 1.0)
            view.AddMeshActor("Cube", points, cells, types, color(0.2, 0.4, 0.8), True)
            points2, cells2, types2 = grid(2, 0.5)
            view.AddMeshActor("Del-š", points2, cells2, types2, color(0.9, 0.3, 0.1), False)
            view.ResetCamera()
            return view
        started = time.time()
        view = on_ui(build)
        RESULTS.append("info off screen view with 1008 hexahedra built in %.3f s" % (time.time() - started))
        names = list(on_ui(lambda: view.GetActorNames()))
        check(sorted(names) == sorted(["Cube", "Del-š"]), "the actor names come back as UTF-8 strings: " + str(names))
        #
        camera = list(on_ui(lambda: view.GetCamera()))
        check(len(camera) == 10, "the camera is 10 values")
        moved = System.Array[System.Double]([camera[0] + 1, camera[1] + 2, camera[2] + 3] + camera[3:])
        on_ui(lambda: view.SetCamera(moved))
        check(abs(list(on_ui(lambda: view.GetCamera()))[1] - camera[1] - 2) < 1e-9, "the camera round trips")
        on_ui(lambda: view.ResetCamera())
        #
        started = time.time()
        result = on_ui(lambda: view.ReadPixels(1, 0, 0))
        pixels, width, height = result[0], result[1], result[2]
        RESULTS.append("info off screen read of %dx%d pixels in %.3f s" % (width, height, time.time() - started))
        check(width == 640 and height == 480 and len(pixels) == 640 * 480 * 3, "the pixels are 640x480 RGB")
        fraction = foreground_fraction(pixels, width, height)
        check(0.05 < fraction < 0.9, "the off screen picture shows the mesh (%.1f %% foreground)" % (100 * fraction))
        result = on_ui(lambda: view.ReadPixels(2, 0, 0))
        check(result[1] == 1280 and result[2] == 960, "a magnified read is 1280x960")
        #
        png = os.path.join(out_dir, "Groundwork_offscreen_š.png")
        on_ui(lambda: view.WritePng(png, 1))
        check(os.path.exists(png) and os.path.getsize(png) > 1000, "a PNG is written to a UTF-8 path: " + png)
        info = on_ui(lambda: view.GetRenderInfo())
        RESULTS.append("info " + " | ".join(line for line in info.splitlines()[:4] if line.strip()))
        #
        expect_native_error(lambda: view.AddMeshActor("Bad", System.Array[System.Double]([0.0, 0.0, 0.0]),
                                                      System.Array[System.Int32]([1, 5]),
                                                      System.Array[System.Byte]([1]), color(1, 0, 0), False),
                            VtkNativeStatus.InvalidArgument, "does not exist",
                            "a cell with a missing point is refused with the native message")
        missing = os.path.join(out_dir, "no_such_folder", "x.png")
        expect_native_error(lambda: view.WritePng(missing, 1), VtkNativeStatus.Vtk, "",
                            "a PNG to a missing folder fails with the VTK status")
        check(any(level == 2 for level, message in logs), "the VTK error reached the log callback: " +
              (logs[-1][1][:120].replace("\n", " ") if logs else "nothing"))
        #
        on_ui(lambda: view.Dispose())
        try:
            on_ui(lambda: view.Render())
            raise Exception("TEST FAILED: a disposed view must refuse a call")
        except System.ObjectDisposedException:
            check(True, "a disposed view refuses a call on the managed side")
        methods = clr.GetClrType(VtkNativeView).Assembly.GetType("vtkControl.Native.VtkNativeMethods")
        render = methods.GetMethod("vtkn_view_render")
        status = render.Invoke(None, System.Array[System.Object]([System.IntPtr(0x12345678)]))
        check(int(status) == int(VtkNativeStatus.InvalidHandle), "an unknown handle is refused by the native side")
        #
        thread_view = on_ui(lambda: VtkNativeView.CreateOffScreen(10, 10))
        try:
            thread_view.Render()
            raise Exception("TEST FAILED: a view was used from another thread")
        except System.InvalidOperationException:
            check(True, "a view refuses a call from another thread than its own")
        on_ui(lambda: thread_view.Dispose())
    finally:
        VtkNativeLibrary.LogMessage -= handler


def test_embedded(out_dir):
    host = VTK.NativeHost
    events = []

    def on_event(sender, args):
        events.append((str(args.Event), list(args.Values)))

    def show():
        System.Windows.Forms.Application.DoEvents()
        view = host.View
        view.ClearActors()
        view.NativeEvent += on_event
        view.SetBackground(color(0.85, 0.85, 0.85), color(1.0, 1.0, 1.0))
        points, cells, types = grid(6, 1.0)
        view.AddMeshActor("Cube", points, cells, types, color(0.2, 0.7, 0.3), True)
        view.ResetCamera()
        view.Render()
        return view
    view = on_ui(show)
    check(view is not None and not view.OffScreen and System.Object.ReferenceEquals(view, VTK.NativeMirror),
          "the view of vtkControl is an embedded native view")
    size = on_ui(lambda: [host.ClientSize.Width, host.ClientSize.Height])
    native_size = on_ui(lambda: view.GetSize(0, 0))
    check([native_size[0], native_size[1]] == size, "the embedded view has the host's size %s" % size)
    #
    child = on_ui(lambda: FindChild(host.Handle))
    check(child != System.IntPtr.Zero, "VTK created its OpenGL window as a child of the host")
    #
    result = on_ui(lambda: view.ReadPixels(1, 0, 0))
    fraction = foreground_fraction(result[0], result[1], result[2])
    check(0.05 < fraction < 0.9, "the embedded view renders the mesh (%.1f %% foreground)" % (100 * fraction))
    on_ui(lambda: view.WritePng(os.path.join(out_dir, "Groundwork_embedded.png"), 1))
    #
    renders = len([e for e in events if e[0] == "RenderEnd"])
    check(renders >= 1, "render end events reach C# (%d)" % renders)
    x, y = 120, 80
    lparam = System.IntPtr((y << 16) | x)
    on_ui(lambda: SendMessage(child, 0x0201, System.IntPtr(1), lparam))
    on_ui(lambda: SendMessage(child, 0x0202, System.IntPtr(0), lparam))
    downs = [e for e in events if e[0] == "LeftButtonDown"]
    ups = [e for e in events if e[0] == "LeftButtonUp"]
    check(len(downs) == 1 and len(ups) == 1, "mouse messages to the VTK window come back as events")
    expected_y = size[1] - 1 - y
    check(downs[0][1][0] == x and downs[0][1][1] == expected_y,
          "the event position is in display coordinates (%s, expected %d, %d)" % (downs[0][1][:2], x, expected_y))
    #
    def resize():
        FORM.Size = System.Drawing.Size(FORM.Width + 40, FORM.Height + 30)
        System.Windows.Forms.Application.DoEvents()
        return [host.ClientSize.Width, host.ClientSize.Height], list(view.GetSize(0, 0))[0:2]
    host_size, view_size = on_ui(resize)
    check(host_size == view_size, "a resized host resizes the native view %s %s" % (host_size, view_size))
    #
    def restore():
        view.NativeEvent -= on_event
        # The control gives the view its scene and its settings back
        VTK.EnableNativeMirror(True)
        CONTROLLER.Redraw(False)
        System.Windows.Forms.Application.DoEvents()
    on_ui(restore)
    RESULTS.append("info events received: %d (%s)" % (len(events), ", ".join(sorted(set(e[0] for e in events)))))


def FindChild(parent):
    return User32.GetWindow(parent, 5)


def SendMessage(window, message, wparam, lparam):
    return User32.SendMessage(window, message, wparam, lparam)


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
            result = self._dll.GetWindow(window.ToInt64(), command)
            return System.IntPtr(result or 0)

        def SendMessage(self, window, message, wparam, lparam):
            return self._dll.SendMessageW(window.ToInt64(), message, wparam.ToInt64(), lparam.ToInt64())
    return _User32()


User32 = define_user32()


def test_view_restored(out_dir, reference_dir):
    # The phase 0 hinge Isometric view, rendered by the view of vtkControl again after the test scene
    import json
    with open(os.path.join(reference_dir, "hinge", "hinge_meta.json")) as stream:
        meta = json.load(stream)
    image = [i for i in meta["images"] if i["file"] == "hinge_femodel_800x600_Isometric.png"][0]

    def size_viewport():
        for _ in range(4):
            delta_width = 800 - VTK.ClientSize.Width
            delta_height = 600 - VTK.ClientSize.Height
            if delta_width == 0 and delta_height == 0:
                break
            FORM.Size = System.Drawing.Size(FORM.Width + delta_width, FORM.Height + delta_height)
            System.Windows.Forms.Application.DoEvents()
    on_ui(size_viewport)
    pmx.view.set_parameters(meta["start_cameras"]["hinge_femodel"])
    pmx.view.set_orientation("Isometric")
    pmx.view.zoom_to_fit()
    path = os.path.join(out_dir, "hinge_femodel_800x600_Isometric.png")
    pmx.view.save_image(path)
    RESULTS.append("info picture of the view after the test scene: " + path)
    return path, os.path.join(reference_dir, "hinge", image["file"])


def main():
    # The arguments may contain spaces, so they are joined and split at '|': <out dir>|<phase 0 reference dir>
    out_dir, reference_dir = [os.path.abspath(part) for part in " ".join(sys.argv[1:]).split("|")]
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    PROGRESS[0] = os.path.join(out_dir, "progress.txt")
    if os.path.exists(PROGRESS[0]):
        os.remove(PROGRESS[0])
    pmx.view.current = "femodel"
    progress("start")
    test_load_and_versions()
    progress("off screen")
    test_off_screen(out_dir)
    progress("embedded")
    test_embedded(out_dir)
    progress("view restored")
    new_path, reference_path = test_view_restored(out_dir, reference_dir)
    with open(os.path.join(out_dir, "groundwork_results.txt"), "w", encoding="utf-8") as stream:
        stream.write("\n".join(RESULTS) + "\ncompare " + new_path + " " + reference_path + "\n")
    for line in RESULTS:
        print(line)
    print("TEST PASSED")


main()
