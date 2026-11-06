#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, stream S: pictures of the mouse selection highlights, as phase 0 recorded them per mode. Both paths
#show the mirrored parts and their selection layer; for each select mode the mouse moves to three points on the model
#and, where the mode highlights an area, drags a box: through Style_PointPickedOnMouseMoveEvt (ActiViz) and through
#HoverWithNativeMirror (native pick, native ids, native selection layer). A picture without a highlight is taken on
#each path after the mode is set and a first move (a mode may draw selection layer items of its own, ActiViz on the
#first move); Compare_Highlights.py compares what the highlight changed. A point where a click selects other ids on
#the two paths, in the mode or in the element mode (the hover of several modes shows the element face under the
#point), is marked: a border pick of slice C, where the native point is the exact one on the ray.
#
#  PrePoMax.com <scene arguments> --script Record_Selection_Highlights.py --scriptArgs "<harness>|<scene>|<modes>|<out>" -g No
#
#<modes>: femodel or geometry. Expected process exit code 0.

import json
import os
import sys
import time

HARNESS, SCENE, MODES, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import scenes  # noqa: E402
import vtkport  # noqa: E402

from CaeGlobals import vtkSelectBy, vtkSelectItem  # noqa: E402

VTK = vtkport.VTK
CONTROLLER = vtkport.CONTROLLER
SIZE = (800, 600)
GRID = (16, 12)
MODE_SETS = {
    "femodel": [("node", "Node", "Node", True), ("element", "Element", "Element", True),
                ("edge", "Node", "Edge", False), ("surface", "Surface", "Surface", False),
                ("edge_angle", "Node", "EdgeAngle", False), ("surface_angle", "Surface", "SurfaceAngle", False),
                ("part", "Part", "Part", True)],
    "geometry": [("geometry", "Geometry", "Geometry", True), ("vertex", "GeometryVertex", "GeometryVertex", False),
                 ("geometry_edge", "GeometryEdge", "GeometryEdge", False),
                 ("geometry_surface", "GeometrySurface", "GeometrySurface", False),
                 ("edge_angle", "GeometryEdge", "GeometryEdgeAngle", False),
                 ("surface_angle", "GeometrySurface", "GeometrySurfaceAngle", False),
                 ("geometry_part", "Part", "GeometryPart", True)],
}


def set_select_mode(select_item, select_by):
    def run():
        CONTROLLER.ClearSelectionHistory()
        CONTROLLER.SelectItem = getattr(vtkSelectItem, select_item)
        CONTROLLER.SelectBy = getattr(vtkSelectBy, select_by)
    vtkport.on_ui(run)


def hover(native, x1, y1, box, x2, y2):
    def run():
        if native:
            VTK.HoverWithNativeMirror(x1, y1, box, x2, y2)
        else:
            vtkport.invoke(VTK, "Style_PointPickedOnMouseMoveEvt", x1, y1, box, x2, y2)
    vtkport.on_ui(run)


def save(out_dir, native, name):
    sub = "new" if native else "old"
    if not os.path.isdir(os.path.join(out_dir, sub)):
        os.makedirs(os.path.join(out_dir, sub))
    path = os.path.join(out_dir, sub, name)
    if native:
        vtkport.on_ui(lambda: VTK.NativeMirror.WritePng(path, 1))
    else:
        pmx.view.save_image(path)


def clicked_ids(native, x, y):
    def run():
        CONTROLLER.ClearSelectionHistory()
        VTK.NativeIdResolution = native
        try:
            if native:
                VTK.PickWithNativeMirror(x, y, False, x, y)
            else:
                VTK.Pick(x, y, False, x, y)
            return sorted(int(i) for i in CONTROLLER.GetSelectionIds())
        finally:
            VTK.NativeIdResolution = True
            CONTROLLER.ClearSelectionHistory()
    return vtkport.on_ui(run)


def hit_points():
    """The grid points the native pick hits, three of them spread over the model"""
    width, height = SIZE
    columns, rows = GRID
    hits = []
    for j in range(rows):
        for i in range(columns):
            x = int(round((i + 0.5) * width / columns))
            y = int(round((j + 0.5) * height / rows))
            _, values = vtkport.on_ui(lambda: vtkport.invoke(VTK, "NativeMirrorPickPoint", x, y, count=4))
            if values[2] is not None:
                hits.append((x, y))
    return [hits[0], hits[len(hits) // 2], hits[-1]] if len(hits) >= 3 else hits


def main():
    out_dir = os.path.abspath(OUT)
    started = time.time()
    vtkport.PMX = pmx
    scenes.setup(SCENE, pmx)
    names = vtkport.enable_native(["geometry", "selection"])
    vtkport.parts_only(True)

    def draw_selection_layers():
        vtkport.get_field(VTK, "_selectionRenderer").SetDraw(1)
        vtkport.get_field(VTK, "_overlayRenderer").SetDraw(1)
    vtkport.on_ui(draw_selection_layers)
    width, height = SIZE
    vtkport.set_viewport_size(width, height)
    start = vtkport.camera_list(pmx.view.get_parameters())
    old_camera, new_camera = vtkport.frame_both(start, "Isometric")
    record = {"scene": SCENE, "mirrored": names, "old_camera": old_camera, "new_camera": new_camera, "base": {},
              "highlights": []}
    for native in (False, True):
        name = "%s_base.png" % SCENE
        save(out_dir, native, name)
        record["base"]["new" if native else "old"] = name
    points = hit_points()
    record["points"] = points
    set_select_mode("Element", "Element")
    element_border = [p for p in points if clicked_ids(False, p[0], p[1]) != clicked_ids(True, p[0], p[1])]
    box = (width // 8, height // 8, width * 7 // 8, height * 7 // 8)
    for mode, select_item, select_by, area in MODE_SETS[MODES]:
        set_select_mode(select_item, select_by)
        base = "%s_%s_base.png" % (SCENE, mode)
        for native in (False, True):
            hover(native, points[0][0], points[0][1], False, points[0][0], points[0][1])
            vtkport.on_ui(lambda: VTK.ClearCurrentMouseSelection())
            save(out_dir, native, base)
        moves = [("point%d" % i, x, y, False, x, y) for i, (x, y) in enumerate(points)]
        if area:
            moves.append(("box", box[0], box[1], True, box[2], box[3]))
        for label, x1, y1, is_box, x2, y2 in moves:
            name = "%s_%s_%s.png" % (SCENE, mode, label)
            for native in (False, True):
                hover(native, x1, y1, is_box, x2, y2)
                save(out_dir, native, name)
                vtkport.on_ui(lambda: VTK.ClearCurrentMouseSelection())
            border = not is_box and ((x1, y1) in element_border or
                                     clicked_ids(False, x1, y1) != clicked_ids(True, x1, y1))
            record["highlights"].append({"file": name, "base": base, "mode": mode, "move": label, "x1": x1, "y1": y1,
                                         "x2": x2, "y2": y2, "box": is_box, "border": border})
        vtkport.on_ui(lambda: setattr(CONTROLLER, "SelectBy", vtkSelectBy.Off))
    vtkport.disable_native()
    record["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, SCENE + "_highlights.json"), "w") as stream:
        json.dump(record, stream, indent=1)
    print("Highlights %s: %d pictures per path, %.1f s" % (SCENE, len(record["highlights"]), record["seconds"]))
    print("SELECTION HIGHLIGHTS RECORDED")


main()
