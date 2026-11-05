#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native pilot, slice A: renders one case through both paths in one process. vtkControl mirrors every part it
#adds into an off screen VTK 9.7 view; both paths start from the same camera, run the same view operations, and
#write their pictures and cameras. The ActiViz path draws only what the mirror holds (no symbols, widgets or
#overlay), so the pictures show the same scene. Compare_SliceA.py compares the two folders.
#
#  solid:  PrePoMax.com -f L_bracket_r.step -u MM_TON_S_C --script Record_SliceA.py --scriptArgs "solid|<out>" -g No
#  hinge:  PrePoMax.com -f Joint_Hinge.pmx --script Record_SliceA.py --scriptArgs "hinge|<out>" -g No
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
from vtkControl.Native import VtkNativeOrientation

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
VIEWPORT_SIZES = [(800, 600), (960, 540)]
VIEWS = ["Isometric", "Front", "Top", "IsometricNegative", "Right", "Bottom"]


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


def camera_list(values):
    return [float(v) for v in values]


def record(out_dir, prefix, meta):
    mirror = VTK.NativeMirror
    start_camera = camera_list(pmx.view.get_parameters())
    meta["start_cameras"][prefix] = start_camera
    start = System.Array[System.Double](start_camera)
    for width, height in VIEWPORT_SIZES:
        set_viewport_size(width, height)
        for view in VIEWS:
            name = "%s_%dx%d_%s.png" % (prefix, width, height, view)
            # ActiViz
            pmx.view.set_parameters(start_camera)
            pmx.view.set_orientation(view)
            pmx.view.zoom_to_fit()
            old_camera = camera_list(pmx.view.get_parameters())
            pmx.view.save_image(os.path.join(out_dir, "old", name))
            # VTK 9.7
            orientation = getattr(VtkNativeOrientation, view)

            def native():
                started = time.time()
                mirror.SetViewParameters(start)
                mirror.SetStandardView(orientation, True)
                mirror.ZoomToFit()
                seconds = time.time() - started
                mirror.WritePng(os.path.join(out_dir, "new", name), 1)
                return camera_list(mirror.GetCamera()), seconds
            new_camera, seconds = on_ui(native)
            meta["images"].append({"file": name, "orientation": view, "viewport": [width, height],
                                   "old_camera": old_camera, "new_camera": new_camera,
                                   "native_view_seconds": round(seconds, 4)})


def mirror_scene():
    def run():
        VTK.EnableNativeMirror(True)
        started = time.time()
        CONTROLLER.Redraw(False)
        seconds = time.time() - started
        VTK.SetNativeMirrorPartsOnly(True)
        return seconds, list(VTK.GetNativeMirrorNames())
    return on_ui(run)


def unmirror_scene():
    def run():
        VTK.SetNativeMirrorPartsOnly(False)
        VTK.EnableNativeMirror(False)
    on_ui(run)


def main():
    case, out_dir = " ".join(sys.argv[1:]).split("|")
    out_dir = os.path.abspath(out_dir)
    for sub in ["old", "new"]:
        if not os.path.isdir(os.path.join(out_dir, sub)):
            os.makedirs(os.path.join(out_dir, sub))
    meta = {"case": case, "images": [], "start_cameras": {}, "mirror": {}}
    #
    prefixes = []
    if case == "solid":
        pmx.view.current = "geometry"
        prefixes.append("solid_geometry")
    elif case == "hinge":
        pmx.view.current = "femodel"
        prefixes.append("hinge_femodel")
    else:
        raise Exception("Unknown case: " + case)
    #
    for index, prefix in enumerate(prefixes + (["solid_femodel"] if case == "solid" else [])):
        if prefix == "solid_femodel":
            pmx.model.mesh.generate(list(pmx.model.geometry.parts)[0].name)
            pmx.view.current = "femodel"
        seconds, names = mirror_scene()
        meta["mirror"][prefix] = {"redraw_seconds_with_mirror": round(seconds, 3), "parts": names}
        record(out_dir, prefix, meta)
        unmirror_scene()
    #
    with open(os.path.join(out_dir, case + "_slice_a.json"), "w") as stream:
        json.dump(meta, stream, indent=1)
    print("Mirrored: %s" % json.dumps(meta["mirror"]))
    print("Images: %d" % (2 * len(meta["images"])))
    print("SLICE A RECORDED")


main()
