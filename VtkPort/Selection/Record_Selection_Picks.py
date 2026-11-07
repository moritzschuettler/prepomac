#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, stream S: the pick plan of slice C with the id resolution of each path. Every pick of the plan (a
#16 x 12 grid of point picks and four boxes per select mode) is made twice: through vtkControl.Pick with the ActiViz
#helpers resolving the ids, and through PickWithNativeMirror with the native helpers (NativeIdResolution), so the
#Controller resolves the native picks with no ActiViz call. The output is the one of Record_SliceC.py, which
#Compare_SliceC.py compares; the closest cells on the parts are compared at every hit point as well.
#
#  PrePoMax.com <scene arguments> --script Record_Selection_Picks.py --scriptArgs "<harness>|<scene>|<modes>|<out>" -g No
#
#<modes>: femodel (node, element, edge, surface, part) or geometry (the geometry selection modes of S3).
#Expected process exit code 0.

import json
import os
import sys
import time

HARNESS, SCENE, MODES, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import scenes  # noqa: E402
import vtkport  # noqa: E402

import System  # noqa: E402
from CaeGlobals import vtkSelectBy, vtkSelectItem  # noqa: E402

VTK = vtkport.VTK
CONTROLLER = vtkport.CONTROLLER
PICK_SIZE = (800, 600)
PICK_GRID = (16, 12)
MODE_SETS = {
    "femodel": [("node", "Node", "Node"), ("element", "Element", "Element"), ("edge", "Node", "Edge"),
                ("surface", "Surface", "Surface"), ("part", "Part", "Part")],
    "geometry": [("geometry", "Geometry", "Geometry"), ("vertex", "GeometryVertex", "GeometryVertex"),
                 ("geometry_edge", "GeometryEdge", "GeometryEdge"),
                 ("geometry_surface", "GeometrySurface", "GeometrySurface"),
                 ("edge_angle", "GeometryEdge", "GeometryEdgeAngle"),
                 ("surface_angle", "GeometrySurface", "GeometrySurfaceAngle"),
                 ("geometry_part", "Part", "GeometryPart")],
}


def array_to_list(values):
    return None if values is None else [float(v) for v in values]


def pick(native, x1, y1, box=False, x2=0, y2=0):
    raw = {}

    def handler(point, direction, planes, completely_inside, operation, part_names):
        raw["point"] = array_to_list(point)
        raw["direction"] = array_to_list(direction)
        raw["planes"] = None if planes is None else [array_to_list(p) for p in planes]
        raw["completely_inside"] = bool(completely_inside)
        raw["part_names"] = [str(n) for n in part_names] if part_names is not None else []

    def run():
        CONTROLLER.ClearSelectionHistory()
        VTK.NativeIdResolution = native
        VTK.OnMouseLeftButtonUpSelection += handler
        started = time.time()
        try:
            if native:
                VTK.PickWithNativeMirror(x1, y1, box, x2, y2)
            else:
                VTK.Pick(x1, y1, box, x2, y2)
            ids = sorted(int(i) for i in CONTROLLER.GetSelectionIds())
        finally:
            VTK.OnMouseLeftButtonUpSelection -= handler
            VTK.NativeIdResolution = True
        return ids, time.time() - started
    ids, seconds = vtkport.on_ui(run)
    raw["ids"] = ids
    raw["seconds"] = round(seconds, 4)
    return raw


def closest_cells(point, names, scale):
    """GetClosestCellsOnActors through both paths: the largest difference of the distances, as a part of the view
    size, and of the normals"""
    def run(native):
        VTK.NativeIdResolution = native
        try:
            _, values = vtkport.invoke(VTK, "GetClosestCellsOnActors", System.Array[System.Double](point),
                                       System.Array[System.String](names), count=4)
        finally:
            VTK.NativeIdResolution = True
        distances = [float(d) for d in values[2]]
        normals = [None if n is None else [float(c) for c in n] for n in values[3]]
        return distances, normals
    old = vtkport.on_ui(lambda: run(False))
    new = vtkport.on_ui(lambda: run(True))
    worst = max([abs(a - b) / scale for a, b in zip(old[0], new[0])] + [0.0])
    for a, b in zip(old[1], new[1]):
        if (a is None) != (b is None):
            worst = float("inf")
        elif a is not None:
            worst = max([worst] + [abs(x - y) for x, y in zip(a, b)])
    return worst


def set_select_mode(select_item, select_by):
    def run():
        CONTROLLER.ClearSelectionHistory()
        CONTROLLER.SelectItem = getattr(vtkSelectItem, select_item)
        CONTROLLER.SelectBy = getattr(vtkSelectBy, select_by)
    vtkport.on_ui(run)


def clear_select_mode():
    def run():
        CONTROLLER.ClearSelectionHistory()
        CONTROLLER.SelectBy = vtkSelectBy.Off
    vtkport.on_ui(run)


def record_picks(names):
    width, height = PICK_SIZE
    vtkport.set_viewport_size(width, height)
    start = vtkport.camera_list(pmx.view.get_parameters())
    old_camera, new_camera = vtkport.frame_both(start, "Isometric")
    if SCENE in scenes.SAME_CAMERA:
        new_camera = vtkport.set_native_camera(old_camera)
    scale = max(1.0, max(abs(v) for v in old_camera[:6]))
    columns, rows = PICK_GRID
    points = [(int(round((i + 0.5) * width / columns)), int(round((j + 0.5) * height / rows)))
              for j in range(rows) for i in range(columns)]
    boxes = [("inside", width * 3 // 8, height * 3 // 8, width * 5 // 8, height * 5 // 8),
             ("crossing", width * 5 // 8, height * 5 // 8, width * 3 // 8, height * 3 // 8),
             ("inside_large", width // 8, height // 8, width * 7 // 8, height * 7 // 8),
             ("crossing_large", width * 7 // 8, height * 7 // 8, width // 8, height // 8)]
    result = {"case": SCENE, "viewport": [width, height], "old_camera": old_camera, "new_camera": new_camera,
              "selection_distance": int(vtkport.on_ui(lambda: vtkport.get_field(VTK, "_selectionDistance"))),
              "modes": {}, "closest_cells_worst": 0.0}
    for mode, select_item, select_by in MODE_SETS[MODES]:
        set_select_mode(select_item, select_by)
        records = []
        for x, y in points:
            record = {"x1": x, "y1": y, "box": False, "old": pick(False, x, y), "new": pick(True, x, y)}
            if (record["old"]["point"] is None) != (record["new"]["point"] is None):
                record["old_neighbor_hits"] = [pick(False, x + dx, y + dy)["point"] is not None
                                               for dx, dy in [(-2, 0), (2, 0), (0, -2), (0, 2)]]
            elif record["old"]["point"] is not None and mode == MODE_SETS[MODES][0][0]:
                result["closest_cells_worst"] = max(result["closest_cells_worst"],
                                                    closest_cells(record["old"]["point"], names, scale))
            records.append(record)
        for name, x1, y1, x2, y2 in boxes:
            records.append({"name": name, "x1": x1, "y1": y1, "x2": x2, "y2": y2, "box": True,
                            "old": pick(False, x1, y1, True, x2, y2), "new": pick(True, x1, y1, True, x2, y2)})
        result["modes"][mode] = records
        clear_select_mode()
    return result


def main():
    out_dir = os.path.abspath(OUT)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    started = time.time()
    vtkport.PMX = pmx
    scenes.setup(SCENE, pmx)
    names = vtkport.enable_native(["geometry", "selection"])
    # A scene with a step after the switch (a section view, transformed copies) keeps those actors drawn on both
    # paths, as the geometry checks do
    if SCENE in scenes.AFTER_ENABLE:
        scenes.after_enable(SCENE, pmx)
        vtkport.on_ui(lambda: VTK.SetNativeMirrorAllGeometry(True))
    vtkport.parts_only(True)
    result = record_picks(names)
    result["mirrored_parts"] = names
    vtkport.disable_native()
    result["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, SCENE + "_slice_c.json"), "w") as stream:
        json.dump(result, stream, indent=1)
    picks = sum(len(records) for records in result["modes"].values())
    print("Picks: %d per path, closest cells differ by %.1e, %.1f s" % (picks, result["closest_cells_worst"],
                                                                        result["seconds"]))
    print("SELECTION PICKS RECORDED")


main()
