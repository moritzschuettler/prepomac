#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, stream S: the probe label of a query. With a query in progress, a hover through the native view
#(HoverWithNativeMirror) must leave the probe widget with the text, position and visibility the ActiViz path
#(Style_PointPickedOnMouseMoveEvt) left it with, for nodes, elements and parts; the native twin of the probe is shown
#and placed at the probe. A hover that misses hides the probe. The ActiViz side is the one of the reference run the
#checks compare with (PORT_REFERENCE); without a reference the probe is compared with no ActiViz state. Writes
#probe_label.json.
#
#  PrePoMax.com -f Joint_Hinge.pmx --script Test_Probe_Label.py --scriptArgs "<harness>|<out>" -g No -w <work>
#
#Expected process exit code 0 and "TEST PASSED".

import json
import os
import sys

HARNESS, OUT = " ".join(sys.argv[1:]).split("|")
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402
from CaeGlobals import vtkSelectBy, vtkSelectItem  # noqa: E402

VTK = vtkport.VTK
CONTROLLER = vtkport.CONTROLLER
on_ui = vtkport.on_ui
SIZE = (800, 600)
MODES = [("node", "Node", "Node"), ("element", "Element", "Element"), ("part", "Part", "Part")]
RESULTS = []


def check(condition, text, detail=""):
    RESULTS.append(["ok" if condition else "FAIL", text, str(detail)])


def ascii_digits(text):
    return text.replace(u"²", "2").replace(u"³", "3")


def probe_state():
    def run():
        probe = vtkport.get_field(VTK, "_probeWidget")
        twin = VTK.GetNativeMirrorWidget(VTK.NativeProbeName)
        twin_visible = None
        twin_position = None
        if twin is not None:
            values, visible = twin.GetPositionAndSize(False)
            twin_visible = bool(visible)
            twin_position = [round(float(v), 3) for v in values][:2]
        return {"text": str(probe.GetText()), "visible": int(probe.GetVisibility()),
                "position": [round(float(v), 3) for v in vtkport.get_field(probe, "_position")],
                "twin_visible": twin_visible, "twin_position": twin_position}
    return on_ui(run)


REFERENCE = vtkport.reference_value("probe_label.json")


def old_state(label, index):
    """The probe the ActiViz path left at the point of the reference, or None"""
    return None if REFERENCE is None else REFERENCE["modes"][label][index]["old"]


def hover(native, x, y):
    def run():
        CONTROLLER.ClearSelectionHistory()
        # Each path starts from a probe that shows nothing
        probe = vtkport.get_field(VTK, "_probeWidget")
        probe.SetText("")
        probe.SetPosition(0.0, 0.0)
        probe.VisibilityOff()
        VTK.HoverWithNativeMirror(x, y, False, 0, 0)
    on_ui(run)
    return probe_state()


def hit_points():
    width, height = SIZE
    points = []
    for j in range(6):
        for i in range(8):
            x = int(round((i + 0.5) * width / 8))
            y = int(round((j + 0.5) * height / 6))
            _, values = on_ui(lambda: vtkport.invoke(VTK, "NativeMirrorPickPoint", x, y, count=4))
            if values[2] is not None:
                points.append((x, y))
    return points


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    vtkport.PMX = pmx
    pmx.view.current = "femodel"
    vtkport.enable_native(["geometry", "widgets", "selection"])
    vtkport.set_viewport_size(*SIZE)
    vtkport.frame_both(vtkport.camera_list(pmx.view.get_parameters()), "Isometric")
    points = hit_points()
    check(len(points) >= 3, "points of the view hit the model", len(points))
    # The points of the reference, where its ActiViz path left the probe
    if REFERENCE is not None:
        points = [tuple(p) for p in REFERENCE["points"]]
    record = {"points": points, "modes": {}}
    on_ui(lambda: setattr(VTK, "QuerySelectionInProgress", True))
    for label, select_item, select_by in MODES:
        def set_mode():
            CONTROLLER.SelectItem = getattr(vtkSelectItem, select_item)
            CONTROLLER.SelectBy = getattr(vtkSelectBy, select_by)
        on_ui(set_mode)
        states = []
        for x, y in points[:4]:
            old = old_state(label, len(states))
            new = hover(True, x, y)
            states.append({"x": x, "y": y, "old": old, "new": new})
            if old is not None:
                # The reference read its text back through the ANSI text mapper of ActiViz, which turned the
                # superscripts of the units into digits, and its position is where ActiViz kept its own label inside
                # the view; the native twin of the reference is what the user saw
                same = (old["text"], old["visible"], old["twin_position"]) == (ascii_digits(new["text"]),
                                                                             new["visible"], new["twin_position"])
                check(same, "%s (%d, %d): the same probe on both paths" % (label, x, y), [old, new])
            check(new["visible"] == 1 and new["text"] != "", "%s (%d, %d): the probe shows a label" % (label, x, y),
                  new["text"])
            check(new["twin_visible"] is True, "%s (%d, %d): the native twin of the probe is shown" % (label, x, y),
                  new["twin_visible"])
        # A miss hides it
        new = hover(True, 2, 2)
        check(new["visible"] == 0, label + ": a miss hides the probe", new["visible"])
        record["modes"][label] = states
    def reset():
        VTK.QuerySelectionInProgress = False
        CONTROLLER.SelectBy = vtkSelectBy.Off
        CONTROLLER.ClearSelectionHistory()
    on_ui(reset)
    vtkport.disable_native()
    record["results"] = RESULTS
    with open(os.path.join(OUT, "probe_label.json"), "w") as f:
        json.dump(record, f, indent=1)
    for result in RESULTS:
        print("%s %s %s" % tuple(result))
    print("TEST PASSED" if all(r[0] == "ok" for r in RESULTS) else "TEST FAILED")


main()
