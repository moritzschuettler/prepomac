#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native pilot, slice B: the scalar bar of the native view, shown alone. The script sets legend configurations
#through the vtkControl methods the Controller calls, then sends mouse events to it (hover, double clicks, drags, a
#window resize) and writes pictures, rectangles, highlights and double click results for Compare_SliceB.py. ActiViz
#is gone: the old side is written as the new one and replaced by the ActiViz run of the reference
#(graft_reference.py, PORT_REFERENCE).
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Record_SliceB.py --scriptArgs "<out>" -g No -w <work>
#
#Expected process exit code 0.

import clr
import json
import os
import sys
import time

import System
from System import Action
from System.Reflection import BindingFlags

clr.AddReference("vtkControl")
clr.AddReference("System.Drawing")
from vtkControl import vtkMaxColorSpectrum, vtkColorSpectrumMode, vtkColorSpectrumMinMaxType, vtkColorSpectrumType
from vtkControl.Native import VtkNativeEvent

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
WIDTH, HEIGHT = 800, 600
PAUSE = 0.7


def get_field(instance, name):
    field = None
    type_ = instance.GetType()
    while field is None and type_ is not None:
        field = type_.GetField(name, FLAGS)
        type_ = type_.BaseType
    return field.GetValue(instance)


def to_net(value):
    if isinstance(value, bool):
        return System.Boolean(value)
    if isinstance(value, int):
        return System.Int32(value)
    return value


def invoke(instance, name, *args, **options):
    """Calls a method through reflection, so internal types and out parameters work; returns the result and the
    argument array, which holds the out values"""
    count = options.get("count", len(args))
    first_type = options.get("first_type")
    type_ = instance.GetType()
    while type_ is not None:
        for method in type_.GetMethods(FLAGS):
            parameters = method.GetParameters()
            if method.Name != name or parameters.Length != count:
                continue
            if first_type is not None and parameters[0].ParameterType != first_type:
                continue
            values = System.Array[System.Object]([to_net(a) for a in args] + [None] * (count - len(args)))
            result = method.Invoke(instance, values)
            return result, list(values)
        type_ = type_.BaseType
    raise Exception("No method %s with %d parameters" % (name, count))


def main_form():
    assembly = [a for a in System.AppDomain.CurrentDomain.GetAssemblies() if a.GetName().Name == "PrePoMax"][0]
    return assembly.GetType("PrePoMax.Program").GetField("_mainForm", FLAGS).GetValue(None)


FORM = main_form()
VTK = get_field(FORM, "_vtk")


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


def set_viewport_size(width, height):
    def resize():
        for _ in range(4):
            client = VTK.ClientSize
            if client.Width == width and client.Height == height:
                break
            FORM.Size = System.Drawing.Size(FORM.Width + width - client.Width, FORM.Height + height - client.Height)
            System.Windows.Forms.Application.DoEvents()
        VTK.SyncNativeMirror()
        return [VTK.ClientSize.Width, VTK.ClientSize.Height]
    actual = on_ui(resize)
    if actual != [width, height]:
        raise Exception("The viewport could not be sized to %dx%d" % (width, height))


def rect_list(rectangle):
    return [rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height]


def new_highlight():
    ok, values = invoke(VTK.NativeMirrorScalarBar, "GetHighlightRectangle", count=1)
    return rect_list(values[0]) if ok else None


def edit_data(bar, minimum):
    ok, values = invoke(bar, "TryGetValueEditData", minimum, count=5, first_type=System.Boolean)
    return [bool(ok), rect_list(values[1]), values[2], values[3], values[4]]


NATIVE_EVENTS = {"LeftButtonPress": VtkNativeEvent.LeftButtonDown, "LeftButtonRelease": VtkNativeEvent.LeftButtonUp,
                 "MouseMove": VtkNativeEvent.MouseMove}


def send(events):
    """Sends (kind, x, y) events to the native view"""
    def new():
        handled = []
        for kind, x, y in events:
            handled.append(bool(VTK.NativeMirror.HandleMouseEvent(NATIVE_EVENTS[kind], x, y)))
        return handled
    return on_ui(new)


def state():
    def run():
        bar = VTK.NativeMirrorScalarBar
        new_edit = {}
        for minimum in [True, False]:
            new_edit["min" if minimum else "max"] = edit_data(bar, minimum)
        new = {"rectangle": rect_list(bar.GetRectangle()), "highlight": new_highlight(), "labels": list(bar.Labels),
               "edit": new_edit}
        result = {}
        for key, value in new.items():
            result["old_" + key] = value
            result["new_" + key] = value
        return result
    return on_ui(run)


def capture(out_dir, name, meta, extra=None):
    pmx.view.save_image(os.path.join(out_dir, "old", name + ".png"))
    on_ui(lambda: VTK.NativeMirror.WritePng(os.path.join(out_dir, "new", name + ".png"), 1))
    entry = {"name": name, "file": name + ".png"}
    entry.update(state())
    if extra:
        entry.update(extra)
    meta["states"].append(entry)
    return entry


def rehide():
    def run():
        VTK.SetNativeMirrorScalarBarOnly(False)
        VTK.SetNativeMirrorScalarBarOnly(True)
    on_ui(run)


def current_spectrum():
    spectrum = vtkMaxColorSpectrum()
    spectrum.DeepCopy(get_field(VTK, "_colorSpectrum"))
    return spectrum


def apply_legend(spectrum, field, component, number_format="E3"):
    def run():
        VTK.SetScalarBarColorSpectrum(spectrum)
        VTK.SetScalarBarNumberFormat(number_format)
        VTK.SetScalarBarText(field, component, "mm", None, str(spectrum.MinMaxType))
    on_ui(run)
    rehide()


def manual_limits():
    """The limits of the manual legend: at 20 and 80 % of the table range. The reference took the range from the
    lookup table of the ActiViz bar, which kept its default of 0 to 1, so with a reference its limits are taken"""
    reference = os.environ.get("PORT_REFERENCE")
    if reference:
        with open(os.path.join(reference, "slice_b.json")) as stream:
            states = json.load(stream)["states"]
        edit = [s for s in states if s["name"] == "config_manual"][0]["old_edit"]["min"]
        return edit[3], edit[4]
    bar = VTK.NativeMirrorScalarBar
    low, high = float(get_field(bar, "_tableRangeMin")), float(get_field(bar, "_tableRangeMax"))
    return low + 0.2 * (high - low), low + 0.8 * (high - low)


def record_configurations(out_dir, meta):
    base = current_spectrum()
    base.MinMaxType = vtkColorSpectrumMinMaxType.Automatic
    apply_legend(base, "DISP", "ALL")
    capture(out_dir, "config_disp_all", meta)
    #
    pmx.view.set_result_field("STRESS", "MISES")
    on_ui(lambda: VTK.SetNativeMirrorScalarBarOnly(True))
    apply_legend(base, "STRESS", "MISES")
    capture(out_dir, "config_mises", meta)
    minimum, maximum = manual_limits()
    #
    spectrum = current_spectrum()
    spectrum.Mode = vtkColorSpectrumMode.Smooth
    apply_legend(spectrum, "STRESS", "MISES")
    capture(out_dir, "config_smooth", meta)
    #
    spectrum = current_spectrum()
    spectrum.Mode = vtkColorSpectrumMode.Discrete
    spectrum.NumberOfColors = 5
    spectrum.Type = vtkColorSpectrumType.CoolWarm
    apply_legend(spectrum, "STRESS", "MISES", "F2")
    capture(out_dir, "config_coolwarm_5_f2", meta)
    #
    spectrum = current_spectrum()
    spectrum.NumberOfColors = 9
    spectrum.Type = vtkColorSpectrumType.Rainbow
    spectrum.MinMaxType = vtkColorSpectrumMinMaxType.Manual
    spectrum.MaxUserValue = 1e300
    spectrum.MinUserValue = minimum
    spectrum.MaxUserValue = maximum
    apply_legend(spectrum, "STRESS", "MISES", "G4")
    capture(out_dir, "config_manual", meta)
    #
    spectrum.MaxColorVisible = False
    apply_legend(spectrum, "STRESS", "MISES", "G4")
    capture(out_dir, "config_manual_max_hidden", meta)
    #
    spectrum.MaxColorVisible = True
    spectrum.Mode = vtkColorSpectrumMode.Smooth
    apply_legend(spectrum, "STRESS", "MISES", "E3")
    capture(out_dir, "config_manual_smooth", meta)
    #
    def no_frame():
        VTK.DrawScalarBarBackground(False)
        VTK.DrawScalarBarBorder(False)
    on_ui(no_frame)
    capture(out_dir, "config_no_frame", meta)

    def frame():
        VTK.DrawScalarBarBackground(True)
        VTK.DrawScalarBarBorder(True)
    on_ui(frame)
    # The interaction runs on the manual legend, which has both added colors
    spectrum.Mode = vtkColorSpectrumMode.Discrete
    apply_legend(spectrum, "STRESS", "MISES", "E3")


def center(rectangle):
    return rectangle[0] + rectangle[2] // 2, rectangle[1] + rectangle[3] // 2


def record_interaction(out_dir, meta):
    double_clicks = []

    def on_edit(edit_minimum, rectangle, text, low, high):
        double_clicks.append(["edit", bool(edit_minimum), rect_list(rectangle), text, low, high])

    def on_settings():
        double_clicks.append(["settings"])

    edit_handler = System.Action[System.Boolean, System.Drawing.Rectangle, System.String, System.Double,
                                 System.Double](on_edit)
    settings_handler = Action(on_settings)
    saved = on_ui(lambda: [VTK.Form_EditLegendValue, VTK.Form_ShowLegendSettings])

    def install():
        VTK.Form_EditLegendValue = edit_handler
        # A double click on either path ends in these callbacks of vtkControl
        VTK.Form_ShowLegendSettings = settings_handler
    on_ui(install)
    try:
        start = state()
        widget = start["new_rectangle"]
        height = HEIGHT
        # The widget is anchored top left: its display rectangle has the bottom at height - y - h
        bottom = height - widget[1] - widget[3]
        title = (widget[0] + 20, bottom + widget[3] - 22)
        maximum = center(start["new_edit"]["max"][1])
        minimum = center(start["new_edit"]["min"][1])
        far = (WIDTH - 50, 50)
        #
        handled = send([("MouseMove", far[0], far[1]), ("MouseMove", title[0], title[1])])
        capture(out_dir, "hover_title", meta, {"handled": handled})
        handled = send([("MouseMove", maximum[0], maximum[1])])
        capture(out_dir, "hover_max_label", meta, {"handled": handled})
        handled = send([("MouseMove", minimum[0], minimum[1])])
        capture(out_dir, "hover_min_label", meta, {"handled": handled})
        #
        for name, point in [("min_label", minimum), ("max_label", maximum), ("title", title)]:
            time.sleep(PAUSE)
            del double_clicks[:]

            def new_double():
                for kind in ["LeftButtonPress", "LeftButtonRelease", "LeftButtonPress", "LeftButtonRelease"]:
                    VTK.NativeMirror.HandleMouseEvent(NATIVE_EVENTS[kind], point[0], point[1])
            on_ui(new_double)
            new_result = list(double_clicks)
            meta["double_clicks"].append({"name": name, "point": list(point), "old": new_result, "new": new_result})
            time.sleep(PAUSE)
        #
        steps = [(title[0] + 30 * i, title[1] - 20 * i) for i in range(1, 6)]
        events = [("LeftButtonPress", title[0], title[1])] + [("MouseMove", x, y) for x, y in steps]
        handled = send(events)
        capture(out_dir, "drag_pressed", meta, {"handled": handled})
        handled = send([("LeftButtonRelease", steps[-1][0], steps[-1][1])])
        capture(out_dir, "drag_released", meta, {"handled": handled})
        time.sleep(PAUSE)
        # Dragged beyond the bottom right corner the widget stays inside the window
        x, y = steps[-1]
        events = [("LeftButtonPress", x, y), ("MouseMove", x + 900, y - 900), ("LeftButtonRelease", x + 900, y - 900)]
        handled = send(events)
        capture(out_dir, "drag_clamped", meta, {"handled": handled})
        time.sleep(PAUSE)
        # A press outside the widget is not taken; the parts are hidden once more, as on the ActiViz path of the
        # reference, where the press went on to pick the model
        handled = send([("MouseMove", 5, height - 5), ("LeftButtonPress", 5, height - 5),
                        ("LeftButtonRelease", 5, height - 5)])
        rehide()
        capture(out_dir, "press_outside", meta, {"handled": handled})
        # The widget in the bottom right quadrant follows that corner when the window is resized
        set_viewport_size(960, 540)
        capture(out_dir, "resized_960x540", meta)
        set_viewport_size(WIDTH, HEIGHT)
        capture(out_dir, "resized_back", meta)
    finally:
        def restore():
            VTK.Form_EditLegendValue = saved[0]
            VTK.Form_ShowLegendSettings = saved[1]
        on_ui(restore)


def main():
    out_dir = os.path.abspath(" ".join(sys.argv[1:]))
    for sub in ["old", "new"]:
        if not os.path.isdir(os.path.join(out_dir, sub)):
            os.makedirs(os.path.join(out_dir, sub))
    meta = {"states": [], "double_clicks": []}
    started = time.time()
    #
    job_name = list(pmx.jobs)[0].name
    meta["job_status"] = str(pmx.jobs[job_name].run(timeout=600))
    pmx.jobs.open_results(job_name)
    pmx.view.current = "results"
    pmx.view.set_result_field("DISP", "ALL")
    set_viewport_size(WIDTH, HEIGHT)

    def mirror():
        # The ActiViz view has no ambient occlusion; the settings of the build folder may have it on
        VTK.SetAmbientOcclusion(False, 0.05, 64, False)
        VTK.EnableNativeMirror(True)
        FORM.GetType().GetField("_controller", FLAGS).GetValue(FORM).Redraw(False)
        VTK.InitializeResultWidgetPositions()
        VTK.SetNativeMirrorScalarBarOnly(True)
    on_ui(mirror)
    #
    record_configurations(out_dir, meta)
    record_interaction(out_dir, meta)
    #
    def unmirror():
        VTK.SetNativeMirrorScalarBarOnly(False)
        VTK.EnableNativeMirror(False)
    on_ui(unmirror)
    meta["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, "slice_b.json"), "w") as stream:
        json.dump(meta, stream, indent=1)
    print("States: %d, double clicks: %d, %.1f s" % (len(meta["states"]), len(meta["double_clicks"]), meta["seconds"]))
    print("SLICE B RECORDED")


main()
