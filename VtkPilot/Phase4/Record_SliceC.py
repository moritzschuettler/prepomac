#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native pilot, slice C: picking. Every pick of the Phase 0 plan (a 16 x 12 grid of point picks and four boxes,
#for the node, element, edge, surface and part modes) is made through PickWithNativeMirror, which takes the screen to
#model step from the native view and raises the events the Controller resolves into ids; the reported points,
#directions, frustum planes, part names and the resolved ids are written for Compare_SliceC.py. The ActiViz side
#(old) is the one of the reference run the checks compare with (PORT_REFERENCE): it is taken from there, the probes
#around a pick where one side misses included. Without a reference the old side is picked through vtkControl.Pick,
#the native view as well.
#
#  solid:  PrePoMax.com -f L_bracket_r.step -u MM_TON_S_C --script Record_SliceC.py --scriptArgs "solid|<out>" -g No
#  hinge:  PrePoMax.com -f Joint_Hinge.pmx --script Record_SliceC.py --scriptArgs "hinge|<out>" -g No
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
clr.AddReference("CaeGlobals")
from CaeGlobals import vtkSelectBy, vtkSelectItem
from vtkControl.Native import VtkNativeOrientation

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
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


def reference_document(name):
    """The document of the reference run (PORT_REFERENCE), or None"""
    reference = os.environ.get("PORT_REFERENCE")
    if not reference or not os.path.isfile(os.path.join(reference, name)):
        return None
    with open(os.path.join(reference, name)) as stream:
        return json.load(stream)


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
        VTK.OnMouseLeftButtonUpSelection += handler
        started = time.time()
        try:
            if native:
                VTK.PickWithNativeMirror(x1, y1, box, x2, y2)
            else:
                VTK.Pick(x1, y1, box, x2, y2)
        finally:
            VTK.OnMouseLeftButtonUpSelection -= handler
        seconds = time.time() - started
        return sorted(int(i) for i in CONTROLLER.GetSelectionIds()), seconds
    ids, seconds = on_ui(run)
    raw["ids"] = ids
    raw["seconds"] = round(seconds, 4)
    return raw


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


def record_picks(prefix):
    width, height = PICK_SIZE
    set_viewport_size(width, height)
    start = [float(v) for v in pmx.view.get_parameters()]
    pmx.view.set_parameters(start)
    pmx.view.set_orientation("Isometric")
    pmx.view.zoom_to_fit()
    old_camera = [float(v) for v in pmx.view.get_parameters()]

    def native_camera():
        mirror = VTK.NativeMirror
        mirror.SetViewParameters(System.Array[System.Double](start))
        mirror.SetStandardView(VtkNativeOrientation.Isometric, True)
        mirror.ZoomToFit()
        return [float(v) for v in mirror.GetCamera()]
    new_camera = on_ui(native_camera)
    columns, rows = PICK_GRID
    points = [(int(round((i + 0.5) * width / columns)), int(round((j + 0.5) * height / rows)))
              for j in range(rows) for i in range(columns)]
    boxes = [("inside", width * 3 // 8, height * 3 // 8, width * 5 // 8, height * 5 // 8),
             ("crossing", width * 5 // 8, height * 5 // 8, width * 3 // 8, height * 3 // 8),
             ("inside_large", width // 8, height // 8, width * 7 // 8, height * 7 // 8),
             ("crossing_large", width * 7 // 8, height * 7 // 8, width // 8, height // 8)]
    result = {"case": prefix, "viewport": [width, height], "old_camera": old_camera, "new_camera": new_camera,
              "selection_distance": int(on_ui(lambda: get_field(VTK, "_selectionDistance"))), "modes": {}}
    reference = reference_document(prefix + "_slice_c.json")
    for mode, select_item, select_by in PICK_MODES:
        set_select_mode(select_item, select_by)
        old_records = reference["modes"][mode] if reference is not None else None
        records = []
        for x, y in points:
            if old_records is not None:
                old_record = old_records[len(records)]
                record = {"x1": x, "y1": y, "box": False, "old": old_record["old"], "new": pick(True, x, y)}
                if "old_neighbor_hits" in old_record: record["old_neighbor_hits"] = old_record["old_neighbor_hits"]
                records.append(record)
                continue
            record = {"x1": x, "y1": y, "box": False, "old": pick(False, x, y), "new": pick(True, x, y)}
            # Where one path hits and the other misses, the old path is probed two pixels around the pick: a mix of
            # hits and misses there puts the pick on the outline of the model
            if (record["old"]["point"] is None) != (record["new"]["point"] is None):
                record["old_neighbor_hits"] = [pick(False, x + dx, y + dy)["point"] is not None
                                               for dx, dy in [(-2, 0), (2, 0), (0, -2), (0, 2)]]
            records.append(record)
        for name, x1, y1, x2, y2 in boxes:
            old = old_records[len(records)]["old"] if old_records is not None else pick(False, x1, y1, True, x2, y2)
            records.append({"name": name, "x1": x1, "y1": y1, "x2": x2, "y2": y2, "box": True,
                            "old": old, "new": pick(True, x1, y1, True, x2, y2)})
        result["modes"][mode] = records
        clear_select_mode()
    return result


def mirror_scene():
    def run():
        # The ActiViz view of the reference has no ambient occlusion; the settings of the build folder may have it on
        VTK.SetAmbientOcclusion(False, 0.05, 64, False)
        VTK.EnableNativeMirror(True)
        CONTROLLER.Redraw(False)
        # Only the parts are shown, so the symbols take no part in the framing of the view
        VTK.SetNativeMirrorPartsOnly(True)
        return list(VTK.GetNativeMirrorNames())
    return on_ui(run)


def main():
    case, out_dir = " ".join(sys.argv[1:]).split("|")
    out_dir = os.path.abspath(out_dir)
    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    started = time.time()
    if case == "solid":
        pmx.model.mesh.generate(list(pmx.model.geometry.parts)[0].name)
        prefix = "solid_femodel"
    elif case == "hinge":
        prefix = "hinge_femodel"
    else:
        raise Exception("Unknown case: " + case)
    pmx.view.current = "femodel"
    names = mirror_scene()
    result = record_picks(prefix)
    result["mirrored_parts"] = names
    def unmirror():
        VTK.SetNativeMirrorPartsOnly(False)
        VTK.EnableNativeMirror(False)
    on_ui(unmirror)
    result["seconds"] = round(time.time() - started, 1)
    with open(os.path.join(out_dir, prefix + "_slice_c.json"), "w") as stream:
        json.dump(result, stream, indent=1)
    picks = sum(len(records) for records in result["modes"].values())
    print("Picks: %d per path, %.1f s" % (picks, result["seconds"]))
    print("SLICE C RECORDED")


main()
