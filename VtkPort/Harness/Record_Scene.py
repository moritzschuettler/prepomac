#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port: renders a scene of scenes.py through both paths. The given native areas are switched on with the
#mirror as the target, both paths show only what the mirror holds, and for each viewport size and standard view
#both frame the view from the same start camera and write their pictures and cameras. checks.py compares them.
#
#  PrePoMax.com <scene arguments> --script Record_Scene.py --scriptArgs "<harness>|<scene>|<areas>|<out>" -g No
#
#<areas> is a comma separated list of geometry, widgets, selection. Expected process exit code 0.

import json
import os
import sys
import time

HARNESS, SCENE, AREAS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import scenes  # noqa: E402
import vtkport  # noqa: E402

VIEWPORT_SIZES = [(800, 600), (960, 540)]


def main():
    out_dir = os.path.abspath(OUT)
    started = time.time()
    vtkport.PMX = pmx
    scenes.setup(SCENE, pmx)
    names = vtkport.enable_native([a for a in AREAS.split(",") if a])
    scenes.after_enable(SCENE, pmx)
    vtkport.on_ui(lambda: vtkport.VTK.SetNativeMirrorAllGeometry("geometry" in AREAS.split(",")))
    vtkport.parts_only(True)
    start_camera = vtkport.camera_list(pmx.view.get_parameters())
    record = {"scene": SCENE, "areas": AREAS, "mirrored": names, "start_camera": start_camera, "images": []}
    for width, height in VIEWPORT_SIZES:
        vtkport.set_viewport_size(width, height)
        for view in vtkport.VIEWS:
            name = "%s_%dx%d_%s.png" % (SCENE, width, height, view)
            old_camera, new_camera = vtkport.frame_both(start_camera, view)
            vtkport.save_pair(out_dir, name)
            record["images"].append({"file": name, "view": view, "viewport": [width, height],
                                     "old_camera": old_camera, "new_camera": new_camera})
    record["camera_tolerance"] = scenes.CAMERA_TOLERANCES.get(SCENE)
    record["differing_limit"] = scenes.DIFFERING_LIMITS.get(SCENE)
    record["mirror_errors"] = list(vtkport.on_ui(lambda: list(vtkport.VTK.GetNativeMirrorErrors())))
    vtkport.disable_native()
    record["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, SCENE + "_scene.json"), "w") as stream:
        json.dump(record, stream, indent=1)
    print("Scene %s: %d pictures per path, mirrored %s, %.1f s" % (SCENE, len(record["images"]), names,
                                                                  record["seconds"]))
    print("SCENE RECORDED")


main()
