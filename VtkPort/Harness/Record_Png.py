#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, stream G: RenderToPNG through both paths. The geometry area is switched on with the mirror as the
#target and both paths show only what the mirror holds; for each case of a transparent background and a
#magnification both frame the isometric view from the same start camera and write their picture with
#vtkControl.RenderToPNG and VtkNativeView.RenderToPng. checks.py scenes compares them, the alpha included.
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Record_Png.py --scriptArgs "<harness>|<out>" -g No
#
#Expected process exit code 0.

import json
import os
import sys

HARNESS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import scenes  # noqa: E402
import vtkport  # noqa: E402

CASES = [(False, 1), (True, 1), (False, 2), (True, 2)]


def main():
    out_dir = os.path.abspath(OUT)
    vtkport.PMX = pmx
    pmx.view.current = "femodel"
    names = vtkport.enable_native(["geometry"])
    vtkport.on_ui(lambda: vtkport.VTK.SetNativeMirrorAllGeometry(True))
    vtkport.parts_only(True)
    start_camera = vtkport.camera_list(pmx.view.get_parameters())
    vtkport.set_viewport_size(640, 480)
    record = {"scene": "hinge_png", "areas": "geometry", "mirrored": names, "start_camera": start_camera,
              "images": []}
    for sub in ["old", "new"]:
        if not os.path.isdir(os.path.join(out_dir, sub)):
            os.makedirs(os.path.join(out_dir, sub))
    for transparent, magnification in CASES:
        name = "hinge_png_%s_x%d.png" % ("transparent" if transparent else "opaque", magnification)
        old_camera, new_camera = vtkport.frame_both(start_camera, "Isometric")

        def write():
            vtkport.VTK.RenderToPNG(os.path.join(out_dir, "old", name), transparent, magnification)
            vtkport.VTK.NativeMirror.RenderToPng(os.path.join(out_dir, "new", name), transparent, magnification)
        vtkport.on_ui(write)
        record["images"].append({"file": name, "view": "Isometric", "viewport": [640, 480],
                                 "old_camera": old_camera, "new_camera": new_camera})
    record["differing_limit"] = scenes.differing_limit("hinge_png", "femodel")
    record["mirror_errors"] = list(vtkport.on_ui(lambda: list(vtkport.VTK.GetNativeMirrorErrors())))
    vtkport.disable_native()
    with open(os.path.join(out_dir, "hinge_png_scene.json"), "w") as stream:
        json.dump(record, stream, indent=1)
    print("PNG RECORDED")


main()
