#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, slice W1: the status block, scale bar, color bar, text with arrow widgets, the probe and the
#triad. With the widgets area on, every vtkMax widget forwards its calls to a native twin in the off screen mirror.
#Both paths show only the widgets; the script sets them up through the vtkControl methods the Controller calls,
#moves both cameras the same way, resizes the window, and sends the same mouse events to both (hover, drags, the
#scale factor press, double clicks, the triad targets and clicks). It writes pictures, layouts, highlights, labels,
#leader lines, double click results and cameras for Compare_W1.py.
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Record_W1.py --scriptArgs "<harness>|<out>" -g No -w <work>
#
#Expected process exit code 0.

import clr
import json
import os
import sys
import time

import System
from System import Action

HARNESS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402

clr.AddReference("System.Drawing")
clr.AddReference("System.Windows.Forms")
from System.Drawing import Color, Rectangle  # noqa: E402
from System.Windows.Forms import Keys, MouseEventArgs  # noqa: E402
from vtkControl.Native import VtkNativeEvent  # noqa: E402

VTK = vtkport.VTK
STYLE = vtkport.get_field(VTK, "_style")
RWI = vtkport.get_field(VTK, "_renderWindowInteractor")
on_ui = vtkport.on_ui
invoke = vtkport.invoke
get_field = vtkport.get_field
WIDTH, HEIGHT = 800, 600
PAUSE = 0.7
FIXED = {"ScaleBar": "_scaleWidget", "ColorBar": "_colorBarWidget", "StatusBlock": "_statusBlockWidget",
         "Probe": "_probeWidget", "Triad": "_coorSysWidget"}
ARROWS = ["Arrow_A", "Arrow_B", "Arrow_C"]
NATIVE_EVENTS = {"LeftButtonPress": VtkNativeEvent.LeftButtonDown, "LeftButtonRelease": VtkNativeEvent.LeftButtonUp,
                 "MouseMove": VtkNativeEvent.MouseMove}


def old_widget(name):
    if name in FIXED:
        return get_field(VTK, FIXED[name])
    return get_field(VTK, "_arrowWidgets")[name]


def rect_list(rectangle):
    return [rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height]


def corners_list(corners):
    if corners is None:
        return None
    return [[round(float(c[0]), 6), round(float(c[1]), 6)] for c in corners]


def old_state(name):
    widget = old_widget(name)
    position = get_field(widget, "_position")
    size = get_field(widget, "_size")
    state = {"rectangle": rect_list(invoke(widget, "GetRectangle")[0]),
             "box": [round(float(v), 6) for v in [position[0], position[1], size[0], size[1]]],
             "visible": int(invoke(widget, "GetVisibility")[0]) == 1,
             "highlight": corners_list(get_field(get_field(widget, "_highlight"), "_corners"))}
    if name == "ScaleBar":
        state["labels"] = [m.GetInput() for m in get_field(widget, "_scaleTextMappers")]
    elif name == "StatusBlock":
        state["scale_factor"] = [bool(get_field(widget, "_deformationScaleFactorTextClicked")),
                                 rect_list(get_field(widget, "_deformationScaleFactorRect"))]
    elif name.startswith("Arrow"):
        line = invoke(widget, "GetLeaderLine")[0]
        state["leader"] = [round(float(line[i][j]), 6) for i in range(2) for j in range(2)]
    return state


def new_state(name):
    widget = VTK.GetNativeMirrorWidget(name)
    box, values = invoke(widget, "GetPositionAndSize", count=1)
    state = {"rectangle": rect_list(widget.GetRectangle()), "box": [round(float(v), 6) for v in box],
             "visible": bool(values[0]), "highlight": corners_list(widget.GetHighlightCorners())}
    if name == "ScaleBar":
        state["labels"] = list(widget.GetScaleBarLabels())
    elif name == "StatusBlock":
        clicked, values = invoke(widget, "GetDeformationScaleFactorHit", count=1)
        state["scale_factor"] = [bool(clicked), rect_list(values[0])]
    elif name.startswith("Arrow"):
        line = widget.GetLeaderLine()
        state["leader"] = [round(float(line[i][j]), 6) for i in range(2) for j in range(2)]
    return state


def names():
    return list(FIXED) + [n for n in ARROWS if n in on_ui(lambda: list(get_field(VTK, "_arrowWidgets").Keys))]


def state():
    def run():
        return {name: {"old": old_state(name), "new": new_state(name)} for name in names()}
    return on_ui(run)


def capture(meta, name, extra=None):
    out_dir = os.path.abspath(OUT)
    pmx.view.save_image(os.path.join(out_dir, "old", name + ".png"))
    on_ui(lambda: VTK.NativeMirror.WritePng(os.path.join(out_dir, "new", name + ".png"), 1))
    entry = {"name": name, "file": name + ".png", "widgets": state()}
    if extra:
        entry.update(extra)
    meta["states"].append(entry)
    return entry


def old_event(kind, x, y):
    RWI.SetEventPosition(x, y)
    invoke(STYLE, "FindPokedRenderer", x, y)
    invoke(STYLE, "vtkInteractorStyleControl_" + kind + "Evt", None, None)


def send(events):
    """Sends (kind, x, y) events to the old path, then the same ones to the native path"""
    def old():
        for kind, x, y in events:
            old_event(kind, x, y)

    def new():
        return [bool(VTK.NativeMirror.HandleMouseEvent(NATIVE_EVENTS[kind], x, y)) for kind, x, y in events]
    on_ui(old)
    return on_ui(new)


def wait_for_animation():
    for _ in range(100):
        if not on_ui(lambda: bool(get_field(VTK, "_animating"))):
            break
        time.sleep(0.05)
    time.sleep(0.2)


def cameras():
    old = vtkport.camera_list(pmx.view.get_parameters())
    new = on_ui(lambda: vtkport.camera_list(VTK.NativeMirror.GetCamera()))
    return old, new


def set_cameras(camera):
    pmx.view.set_parameters(camera)
    on_ui(lambda: VTK.NativeMirror.SetViewParameters(System.Array[System.Double](camera)))


def display_rectangle(entry, name):
    """The widget rectangle in display coordinates (origin bottom left) from the old layout"""
    widget = entry["widgets"][name]["old"]
    x, y, w, h = widget["box"]
    if name in ("StatusBlock", "ColorBar"):
        y = HEIGHT - h - y
    return x, y, w, h


def center(rectangle):
    x, y, w, h = rectangle
    return int(x + w / 2), int(y + h / 2)


def setup_widgets(meta, views):
    set_cameras(views["Isometric"])

    def visible():
        VTK.SetWidgetHighlightEnabled(True)
        VTK.SetWidgetInteractionEnabled(True)
        VTK.SetCoorSysVisibility(True)
        VTK.SetScaleWidgetVisibility(True)
        VTK.SetScaleWidgetUnit("mm")
        VTK.ResetScaleBarPosition()
    on_ui(visible)
    capture(meta, "initial")

    def status_block():
        data_type = [a for a in System.AppDomain.CurrentDomain.GetAssemblies()
                     if a.GetName().Name == "vtkControl"][0].GetType("vtkControl.vtkMaxFieldDataType")
        VTK.SetStatusBlockVisibility(True)
        VTK.SetStatusBlockNumberFormat("G4")
        VTK.DrawStatusBlockBackground(True)
        VTK.DrawStatusBlockBorder(True)
        VTK.SetStatusBlock("Joint_Hinge.frd", System.DateTime(2026, 11, 5, 12, 0, 0), 1.0, "s", "DISP", 12.5,
                           System.Enum.ToObject(data_type, 0), 1, 3, -1)
        VTK.InitializeResultWidgetPositions()
    on_ui(status_block)
    capture(meta, "status_block")

    def color_bar():
        colors = System.Array[Color]([Color.Red, Color.FromArgb(40, 160, 90), Color.Blue, Color.Orange])
        labels = System.Array[System.String](["Base", "Lever_arm", "Pin-1", "Steel S235"])
        VTK.InitializeColorBarWidgetPosition()
        VTK.SetColorBarColorsAndLabels(colors, labels)
        VTK.DrawColorBarBackground(True)
        VTK.DrawColorBarBorder(True)
    on_ui(color_bar)
    capture(meta, "color_bar")
    # The arrows point at points around the focal point, as the min/max annotations and the probe do
    camera = views["Isometric"]
    focal, scale = camera[3:6], camera[9]
    offsets = [(0.3, 0.2, 0.1), (-0.4, -0.1, 0.2), (0.1, -0.35, -0.2)]
    texts = ["Node id: 1234" + System.Environment.NewLine + "U: 1.234E-02 mm", "Max: 5.67E+01 MPa",
             "Element id: 42" + System.Environment.NewLine + "Part: Lever" + System.Environment.NewLine + "x: 1.5"]

    def arrows():
        for name, offset, text in zip(ARROWS, offsets, texts):
            anchor = System.Array[System.Double]([focal[i] + scale * offset[i] for i in range(3)])
            VTK.AddArrowWidget(name, text, "G4", anchor, True, True, True)
    on_ui(arrows)
    capture(meta, "arrows")
    on_ui(lambda: VTK.ArrangeVisibleArrowWidgets())
    capture(meta, "arrows_arranged")

    def probe():
        widget = get_field(VTK, "_probeWidget")
        invoke(widget, "SetPosition", 330.0, 260.0)
        invoke(widget, "SetText", "Element id: 7" + System.Environment.NewLine + "Base")
        invoke(widget, "VisibilityOn")
        invoke(VTK, "RenderScene")
    on_ui(probe)
    capture(meta, "probe")
    for view in ["Front", "Top", "IsometricNegative", "Isometric"]:
        set_cameras(views[view])
        old, new = cameras()
        capture(meta, "camera_" + view, {"old_camera": old, "new_camera": new})
    vtkport.set_viewport_size(960, 540)
    capture(meta, "resized_960x540")
    vtkport.set_viewport_size(WIDTH, HEIGHT)
    capture(meta, "resized_back")


def record_mouse(meta):
    clicks = []
    saved = on_ui(lambda: [VTK.Form_ShowColorBarSettings, VTK.Form_ShowStatusBlockSettings, VTK.Form_WidgetPicked])

    def on_picked(e, keys, name, rectangle):
        clicks.append(["picked", name, rect_list(rectangle)])
    color_bar_handler = Action(lambda: clicks.append(["color bar settings"]))
    status_handler = Action(lambda: clicks.append(["status block settings"]))
    picked_handler = System.Action[MouseEventArgs, Keys, System.String, Rectangle](on_picked)

    def install():
        VTK.Form_ShowColorBarSettings = color_bar_handler
        VTK.Form_ShowStatusBlockSettings = status_handler
        VTK.Form_WidgetPicked = picked_handler
    on_ui(install)
    try:
        start = meta["states"][-1]
        targets = {name: center(display_rectangle(start, name)) for name in ["StatusBlock", "ColorBar", "ScaleBar",
                                                                            "Probe", "Arrow_B"]}
        for name, point in sorted(targets.items()):
            handled = send([("MouseMove", 5, 5), ("MouseMove", point[0], point[1])])
            capture(meta, "hover_" + name, {"handled": handled})
        # Drags of a free widget and of an arrow box, which keeps its corner in the plane of its anchor
        for name in ["StatusBlock", "Arrow_B"]:
            x, y = targets[name]
            steps = [(x - 25 * i, y - 15 * i) for i in range(1, 5)]
            events = [("LeftButtonPress", x, y)] + [("MouseMove", sx, sy) for sx, sy in steps] + \
                     [("LeftButtonRelease", steps[-1][0], steps[-1][1])]
            handled = send(events)
            capture(meta, "drag_" + name, {"handled": handled})
            time.sleep(PAUSE)
        # A press on the deformation scale factor of the status block
        block = meta["states"][-1]["widgets"]["StatusBlock"]["old"]
        bx, by, bw, bh = block["box"]
        sx, sy, sw, sh = block["scale_factor"][1]
        point = (int(bx + sx + sw / 2), int(HEIGHT - by - sy - sh / 2))
        handled = send([("MouseMove", point[0], point[1]), ("LeftButtonPress", point[0], point[1]),
                        ("LeftButtonRelease", point[0], point[1])])
        capture(meta, "press_scale_factor", {"handled": handled})
        time.sleep(PAUSE)
        # Double clicks
        start = meta["states"][-1]
        for name in ["ColorBar", "StatusBlock", "Arrow_B"]:
            point = center(display_rectangle(start, name))
            del clicks[:]
            on_ui(lambda: [old_event(k, point[0], point[1]) for k in ["LeftButtonPress", "LeftButtonRelease",
                                                                      "LeftButtonPress", "LeftButtonRelease"]])
            old_result = list(clicks)
            del clicks[:]
            time.sleep(PAUSE)
            on_ui(lambda: [VTK.NativeMirror.HandleMouseEvent(NATIVE_EVENTS[k], point[0], point[1])
                           for k in ["LeftButtonPress", "LeftButtonRelease", "LeftButtonPress", "LeftButtonRelease"]])
            new_result = list(clicks)
            meta["double_clicks"].append({"name": name, "point": list(point), "old": old_result, "new": new_result})
            time.sleep(PAUSE)
    finally:
        def restore():
            VTK.Form_ShowColorBarSettings = saved[0]
            VTK.Form_ShowStatusBlockSettings = saved[1]
            VTK.Form_WidgetPicked = saved[2]
        on_ui(restore)


def record_triad(meta, views):
    set_cameras(views["Isometric"])
    triad = meta["states"][-1]["widgets"]["Triad"]["old"]
    x0, y0, w, h = triad["box"]
    # The negative axes show while the mouse is near the triad
    handled = send([("MouseMove", int(x0 + w / 2) + 3, int(y0 + h / 2) + 2)])
    capture(meta, "triad_near", {"handled": handled})

    def grid():
        old = on_ui(lambda: get_field(VTK, "_coorSysWidget"))
        new = VTK.GetNativeMirrorWidget("Triad")
        points = {}
        for x in range(int(x0) - 20, int(x0 + w) + 20, 2):
            for y in range(int(y0) - 20, int(y0 + h) + 20, 2):
                points[(x, y)] = [int(invoke(old, "GetTargetAtPosition", x, y)[0]), int(new.GetTriadTarget(x, y))]
        return points
    points = on_ui(grid)
    meta["triad_grid"] = {"points": len(points),
                          "differing": [[k[0], k[1], v[0], v[1]] for k, v in points.items() if v[0] != v[1]]}
    found = {}
    for (x, y), (old, new) in sorted(points.items()):
        if old == new and old != 0:
            found.setdefault(old, (x, y))
    # Clicks on a cone, a negative end, a shaft and the origin; each turns both cameras the same way
    for target in [2, 7, 12, 5, 1]:
        if target not in found:
            meta["triad_clicks"].append({"target": target, "missing": True})
            continue
        set_cameras(views["Isometric"])
        time.sleep(PAUSE)
        x, y = found[target]
        handled = send([("MouseMove", x, y), ("LeftButtonPress", x, y), ("LeftButtonRelease", x, y)])
        wait_for_animation()
        old, new = cameras()
        meta["triad_clicks"].append({"target": target, "point": [x, y], "handled": handled, "old_camera": old,
                                     "new_camera": new})
        capture(meta, "triad_click_%d" % target, {"old_camera": old, "new_camera": new})


def main():
    out_dir = os.path.abspath(OUT)
    for sub in ["old", "new"]:
        if not os.path.isdir(os.path.join(out_dir, sub)):
            os.makedirs(os.path.join(out_dir, sub))
    started = time.time()
    vtkport.PMX = pmx
    meta = {"states": [], "double_clicks": [], "triad_clicks": []}
    pmx.view.current = "femodel"
    vtkport.set_viewport_size(WIDTH, HEIGHT)
    views = {}
    start = vtkport.camera_list(pmx.view.get_parameters())
    for view in ["Isometric", "Front", "Top", "IsometricNegative"]:
        pmx.view.set_parameters(start)
        pmx.view.set_orientation(view)
        pmx.view.zoom_to_fit()
        views[view] = vtkport.camera_list(pmx.view.get_parameters())
    vtkport.enable_native(["widgets"])
    on_ui(lambda: VTK.SetNativeMirrorWidgetsOnly(True))
    try:
        setup_widgets(meta, views)
        record_mouse(meta)
        record_triad(meta, views)
    finally:
        on_ui(lambda: VTK.SetNativeMirrorWidgetsOnly(False))
        vtkport.disable_native()
    meta["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, "w1.json"), "w") as stream:
        json.dump(meta, stream, indent=1)
    print("States: %d, double clicks: %d, triad clicks: %d, %.1f s" %
          (len(meta["states"]), len(meta["double_clicks"]), len(meta["triad_clicks"]), meta["seconds"]))
    print("W1 RECORDED")


main()
