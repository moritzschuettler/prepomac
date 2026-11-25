#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native pilot, phase 0: records the reference output of the current vtkControl (ActiViz 5.8) for one
#case - images of the standard views at fixed viewport sizes, the cameras that produced them, and the raw
#and resolved output of pixel picks for slice C. Reaches the live vtkControl and Controller by reflection,
#so it needs no change to the application.
#
#  solid:  PrePoMax.com -f L_bracket_r.step -u MM_TON_S_C --script Record_Reference.py --scriptArgs "solid <out>" -g No
#  hinge:  PrePoMax.com -f Joint_Hinge.pmx --script Record_Reference.py --scriptArgs "hinge <out>" -g No
#  result: PrePoMax.com -f Joint_Hinge.pmx --script Record_Reference.py --scriptArgs "result <out>" -g No -w <work>
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

clr.AddReference("CaeGlobals")
from CaeGlobals import vtkSelectBy, vtkSelectItem

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
VIEWPORT_SIZES = [(800, 600), (960, 540)]
VIEWS = ["Isometric", "Front", "Top", "IsometricNegative"]
PICK_SIZE = (800, 600)
PICK_GRID = (16, 12)
PICK_MODES = [("node", "Node", "Node"),
              ("element", "Element", "Element"),
              ("edge", "Node", "Edge"),
              ("surface", "Surface", "Surface"),
              ("part", "Part", "Part")]


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


def render_window_size():
    render_window = get_field(VTK, "_renderWindow")
    size = render_window.GetSize()
    return [int(size[0]), int(size[1])]


def set_viewport_size(width, height):
    def resize():
        for _ in range(4):
            client = VTK.ClientSize
            delta_width = width - client.Width
            delta_height = height - client.Height
            if delta_width == 0 and delta_height == 0:
                break
            FORM.Size = System.Drawing.Size(FORM.Width + delta_width, FORM.Height + delta_height)
            System.Windows.Forms.Application.DoEvents()
        return [VTK.ClientSize.Width, VTK.ClientSize.Height]
    actual = on_ui(resize)
    if actual != [width, height]:
        raise Exception("The viewport could not be sized to %dx%d, it is %dx%d" % (width, height, actual[0], actual[1]))
    return render_window_size()


# A standard view turns the camera to the closest iso quadrant and up axis of the camera it starts from,
# so every view starts from the same recorded camera
def save_views(out_dir, prefix, meta):
    start_camera = [float(v) for v in pmx.view.get_parameters()]
    meta.setdefault("start_cameras", {})[prefix] = start_camera
    for width, height in VIEWPORT_SIZES:
        render_size = set_viewport_size(width, height)
        for view in VIEWS:
            pmx.view.set_parameters(start_camera)
            pmx.view.set_orientation(view)
            pmx.view.zoom_to_fit()
            name = "%s_%dx%d_%s.png" % (prefix, width, height, view)
            pmx.view.save_image(os.path.join(out_dir, name))
            meta["images"].append({"file": name, "view": pmx.view.current, "orientation": view,
                                   "viewport": [width, height], "render_window": render_size,
                                   "camera": [float(v) for v in pmx.view.get_parameters()]})


def ids_to_list(ids):
    return sorted(int(i) for i in ids) if ids is not None else []


def array_to_list(values):
    if values is None:
        return None
    return [float(v) for v in values]


def pick(x1, y1, box=False, x2=0, y2=0):
    raw = {}

    def handler(point, direction, planes, completely_inside, operation, part_names):
        raw["point"] = array_to_list(point)
        raw["direction"] = array_to_list(direction)
        raw["planes"] = None if planes is None else [array_to_list(p) for p in planes]
        raw["completely_inside"] = bool(completely_inside)
        raw["part_names"] = [str(n) for n in part_names] if part_names is not None else []

    def run():
        CONTROLLER.ClearSelectionHistory()
        VTK.OnMouseLeftButtonUpSelection += handler
        try:
            VTK.Pick(x1, y1, box, x2, y2)
        finally:
            VTK.OnMouseLeftButtonUpSelection -= handler
        return CONTROLLER.GetSelectionIds()
    ids = on_ui(run)
    record = {"x1": x1, "y1": y1, "box": box, "x2": x2, "y2": y2, "ids": ids_to_list(ids)}
    record.update(raw)
    return record


def hover(x, y):
    method = VTK.GetType().GetMethod("Style_PointPickedOnMouseMoveEvt", FLAGS)
    on_ui(lambda: method.Invoke(VTK, System.Array[System.Object]([System.Int32(x), System.Int32(y), System.Boolean(False), System.Int32(0), System.Int32(0)])))


def set_select_mode(select_item, select_by):
    def run():
        CONTROLLER.ClearSelectionHistory()
        CONTROLLER.SelectItem = getattr(vtkSelectItem, select_item)
        CONTROLLER.SelectBy = getattr(vtkSelectBy, select_by)
    on_ui(run)


def clear_select_mode():
    def run():
        CONTROLLER.ClearSelectionHistory()
        CONTROLLER.SelectBy = vtkSelectBy.Off
    on_ui(run)


def record_picks(out_dir, prefix, meta):
    width, height = PICK_SIZE
    set_viewport_size(width, height)
    pmx.view.set_parameters(meta["start_cameras"][prefix])
    pmx.view.set_orientation("Isometric")
    pmx.view.zoom_to_fit()
    camera = [float(v) for v in pmx.view.get_parameters()]
    columns, rows = PICK_GRID
    points = [(int(round((i + 0.5) * width / columns)), int(round((j + 0.5) * height / rows)))
              for j in range(rows) for i in range(columns)]
    boxes = [("inside", width * 3 // 8, height * 3 // 8, width * 5 // 8, height * 5 // 8),
             ("crossing", width * 5 // 8, height * 5 // 8, width * 3 // 8, height * 3 // 8),
             ("inside_large", width // 8, height // 8, width * 7 // 8, height * 7 // 8),
             ("crossing_large", width * 7 // 8, height * 7 // 8, width // 8, height // 8)]
    picks = {"case": prefix, "viewport": [width, height], "camera": camera, "grid": [columns, rows],
             "coordinates": "VTK display coordinates, origin at the bottom left", "modes": {}}
    for mode, select_item, select_by in PICK_MODES:
        set_select_mode(select_item, select_by)
        started = time.time()
        point_picks = [pick(x, y) for x, y in points]
        box_picks = []
        for box_name, x1, y1, x2, y2 in boxes:
            record = pick(x1, y1, True, x2, y2)
            record["name"] = box_name
            box_picks.append(record)
        picks["modes"][mode] = {"select_item": select_item, "select_by": select_by, "points": point_picks,
                                "boxes": box_picks, "seconds": round(time.time() - started, 2)}
        # The highlight of the last hit point pick and the hover highlight are compared as images
        hits = [p for p in point_picks if p["ids"]]
        if hits:
            middle = hits[len(hits) // 2]
            pick(middle["x1"], middle["y1"])
            name = "%s_pick_%s_highlight.png" % (prefix, mode)
            pmx.view.save_image(os.path.join(out_dir, name))
            meta["images"].append({"file": name, "pick": [middle["x1"], middle["y1"]], "mode": mode,
                                   "viewport": [width, height], "camera": camera})
            on_ui(lambda: CONTROLLER.ClearSelectionHistory())
            hover(middle["x1"], middle["y1"])
            name = "%s_pick_%s_hover.png" % (prefix, mode)
            pmx.view.save_image(os.path.join(out_dir, name))
            meta["images"].append({"file": name, "hover": [middle["x1"], middle["y1"]], "mode": mode,
                                   "viewport": [width, height], "camera": camera})
        clear_select_mode()
    with open(os.path.join(out_dir, prefix + "_picks.json"), "w") as stream:
        json.dump(picks, stream, indent=1)
    return picks


def summarize_picks(picks):
    lines = []
    for mode, data in picks["modes"].items():
        hits = sum(1 for p in data["points"] if p["ids"])
        lines.append("%s: %d/%d point picks hit, boxes %s, %.1f s" %
                     (mode, hits, len(data["points"]), [len(b["ids"]) for b in data["boxes"]], data["seconds"]))
    return lines


def main():
    case = sys.argv[1]
    out_dir = os.path.abspath(" ".join(sys.argv[2:]))
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    meta = {"case": case, "activiz": str(VTK.GetType().Assembly.GetReferencedAssemblies()), "images": [],
            "started": time.strftime("%Y-%m-%d %H:%M:%S")}
    started = time.time()
    #
    if case == "solid":
        pmx.view.current = "geometry"
        save_views(out_dir, "solid_geometry", meta)
        part_name = list(pmx.model.geometry.parts)[0].name
        pmx.model.mesh.generate(part_name)
        pmx.view.current = "femodel"
        save_views(out_dir, "solid_femodel", meta)
        meta["picks"] = summarize_picks(record_picks(out_dir, "solid_femodel", meta))
    elif case == "hinge":
        pmx.view.current = "femodel"
        save_views(out_dir, "hinge_femodel", meta)
        meta["picks"] = summarize_picks(record_picks(out_dir, "hinge_femodel", meta))
    elif case == "result":
        job_name = list(pmx.jobs)[0].name
        status = pmx.jobs[job_name].run(timeout=600)
        meta["job_status"] = str(status)
        pmx.jobs.open_results(job_name)
        pmx.view.current = "results"
        pmx.view.set_result_field("DISP", "ALL")
        save_views(out_dir, "result_disp", meta)
        pmx.view.set_result_field("STRESS", "MISES")
        save_views(out_dir, "result_mises", meta)
    else:
        raise Exception("Unknown case: " + case)
    #
    meta["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, case + "_meta.json"), "w") as stream:
        json.dump(meta, stream, indent=1)
    for line in meta.get("picks", []):
        print(line)
    print("Images: %d, %.1f s" % (len(meta["images"]), meta["seconds"]))
    print("REFERENCE RECORDED")


main()
