"""The in-process side of the port checks: imported by the recorder scripts that run inside PrePoMax. It reaches the
live vtkControl and Controller, runs work on the user interface thread, sizes the viewport, switches the native
areas and their target, and frames and pictures a scene through both paths.

    import sys; sys.path.insert(0, <this folder>); import vtkport
"""

import clr
import os

import System
from System import Action
from System.Reflection import BindingFlags

clr.AddReference("vtkControl")
clr.AddReference("CaeGlobals")
from vtkControl.Native import VtkNativeArea, VtkNativeOrientation  # noqa: E402

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
PMX = None  # the pmx object of the recorder script, set by it
VIEWS = ["Isometric", "Front", "Top", "IsometricNegative", "Right", "Bottom"]
AREAS = {"geometry": VtkNativeArea.Geometry, "widgets": VtkNativeArea.Widgets, "selection": VtkNativeArea.Selection}


def get_field(instance, name):
    """A field of the instance or of one of its base types, public or not"""
    type_ = instance.GetType()
    while type_ is not None:
        field = type_.GetField(name, FLAGS)
        if field is not None:
            return field.GetValue(instance)
        type_ = type_.BaseType
    raise Exception("No field " + name)


def _to_net(value):
    if isinstance(value, bool):
        return System.Boolean(value)
    if isinstance(value, int):
        return System.Int32(value)
    return value


def invoke(instance, name, *args, **options):
    """Calls a method through reflection, so internal types and out parameters work; returns the result and the
    argument array, which holds the out values. count: the number of parameters when out ones are left out"""
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
            values = System.Array[System.Object]([_to_net(a) for a in args] + [None] * (count - len(args)))
            return method.Invoke(instance, values), list(values)
        type_ = type_.BaseType
    raise Exception("No method %s with %d parameters" % (name, count))


def _main_form():
    assembly = [a for a in System.AppDomain.CurrentDomain.GetAssemblies() if a.GetName().Name == "PrePoMax"][0]
    return assembly.GetType("PrePoMax.Program").GetField("_mainForm", FLAGS).GetValue(None)


FORM = _main_form()
VTK = get_field(FORM, "_vtk")
CONTROLLER = get_field(FORM, "_controller")


def on_ui(function):
    """Runs the function on the user interface thread and returns its value; its exception is raised here"""
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


# The checks compare the native view with the ActiViz one, which has no ambient occlusion; the settings of the build
# folder may have it on
on_ui(lambda: VTK.SetAmbientOcclusion(False, 0.05, 64, False))


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


def area_flags(areas):
    """The VtkNativeArea flags of area names; pythonnet does not combine .NET flags, so the value is built here"""
    value = 0
    for area in areas:
        value |= int(AREAS[area])
    return System.Enum.ToObject(clr.GetClrType(VtkNativeArea), value)


def enable_native(areas, in_window=True, redraw=True):
    """Switches the given native areas on ("geometry", "widgets", "selection") with the native view in the window
    as the target (the off screen mirror goes with ActiViz); the Controller redraws, so the native view receives the
    current scene"""
    flags = area_flags(areas)

    def run():
        VTK.SetNativeAreas(flags, in_window)
        if redraw:
            CONTROLLER.Redraw(False)
        if in_window:
            # The native view in the window starts from the camera the ActiViz view shows
            position, focal_point, view_up, parallel_scale = invoke(VTK, "GetViewParameters", count=4)[1]
            camera = list(position) + list(focal_point) + list(view_up) + [parallel_scale]
            VTK.NativeMirror.SetViewParameters(System.Array[System.Double]([float(v) for v in camera]))
        return list(VTK.GetNativeMirrorNames())
    return on_ui(run)


def disable_native():
    def run():
        VTK.SetNativeMirrorPartsOnly(False)
        VTK.SetNativeMirrorScalarBarOnly(False)
        VTK.SetNativeAreas(area_flags(["geometry", "widgets", "selection"]), True)
    on_ui(run)


def parts_only(on):
    on_ui(lambda: VTK.SetNativeMirrorPartsOnly(on))


def reference_value(document, *path):
    """A value of the reference run the checks compare with (PORT_REFERENCE, a suite folder of a run on a build
    with ActiViz), or None without one: the ActiViz camera a scene gives the native view, for example"""
    reference = os.environ.get("PORT_REFERENCE")
    if not reference:
        return None
    import json
    full = os.path.join(reference, document)
    if not os.path.isfile(full):
        return None
    with open(full) as stream:
        value = json.load(stream)
    for key in path:
        value = value[key]
    return value


def camera_list(values):
    return [float(v) for v in values]


def frame_both(start_camera, orientation):
    """Frames the view on both paths from the same start camera: ActiViz through pmx, the mirror through the
    native views; returns both cameras"""
    pmx = PMX
    pmx.view.set_parameters(start_camera)
    pmx.view.set_orientation(orientation)
    pmx.view.zoom_to_fit()
    old_camera = camera_list(pmx.view.get_parameters())
    start = System.Array[System.Double](start_camera)

    def native():
        mirror = VTK.NativeMirror
        mirror.SetViewParameters(start)
        mirror.SetStandardView(getattr(VtkNativeOrientation, orientation), True)
        mirror.ZoomToFit()
        return camera_list(mirror.GetCamera())
    return old_camera, on_ui(native)


def set_native_camera(camera):
    """Gives the native view a camera of the ActiViz one (a list of pmx.view.get_parameters) and returns its own"""
    def run():
        VTK.NativeMirror.SetViewParameters(System.Array[System.Double]([float(v) for v in camera]))
        return camera_list(VTK.NativeMirror.GetCamera())
    return on_ui(run)


def save_pair(out_dir, name):
    """Writes the ActiViz picture to old/<name> and the mirror's to new/<name>"""
    pmx = PMX
    for sub in ["old", "new"]:
        if not os.path.isdir(os.path.join(out_dir, sub)):
            os.makedirs(os.path.join(out_dir, sub))
    pmx.view.save_image(os.path.join(out_dir, "old", name))
    on_ui(lambda: VTK.NativeMirror.WritePng(os.path.join(out_dir, "new", name), 1))
