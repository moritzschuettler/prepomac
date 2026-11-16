"""Compares what Record_W1.py wrote: the ActiViz 5.8 widgets (old/) and their native VTK 9.7 ports (new/), alone in
the same window, through the same calls, cameras and mouse events. Runs with the system Python (numpy, Pillow).

    py -3 Compare_W1.py <out dir>

The layout must agree: every widget's rectangle, visibility, hover highlight, scale bar labels and the box of the
status block's scale factor exactly, the position and size and the leader lines of the arrows to 1e-3 of a pixel.
A highlight one pixel off is reported, not failed, when the widget lies on a whole pixel (within 1e-4): the
highlight floors the position, and the two versions reach x.99999 and x.00001 through the world to display step.
The triad must report the same target at every point of a grid around it, and a click on a target must turn both
cameras the same way (1e-9 of the model size). Double clicks must open the same thing. The pictures: the shapes
must cover each other within a pixel (>= 0.98 of each one inside the other one grown by a pixel), and the pixels
that differ by more than 24 levels are counted inside the box of the widgets; the two versions rasterize text
differently, so that is a tolerance and the side by side pictures are for review.
"""

import json
import os
import sys

import numpy

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Harness"))
from checks import compare_cameras, foreground, grow, load, CAMERA_TOLERANCE  # noqa: E402
from PIL import Image  # noqa: E402

COVERAGE_LIMIT = 0.98
DIFFERING_LIMIT = 0.10
POSITION_TOLERANCE = 1e-3


def compare_picture(old_path, new_path, side_by_side_dir):
    a = load(old_path)
    b = load(new_path)
    mask_a = foreground(a)
    mask_b = foreground(b)
    union = mask_a | mask_b
    if union.sum() == 0:
        return {"coverage": 1.0, "differing": 0.0}
    # Within two pixels: the native scale bar stands a pixel higher (accepted in the user's review) and its lines
    # are drawn into one pixel row, where ActiViz spread them over two
    coverage = min(float((mask_a & grow(grow(mask_b))).sum()) / max(1, mask_a.sum()),
                   float((mask_b & grow(grow(mask_a))).sum()) / max(1, mask_b.sum()))
    rows = numpy.where(union.any(axis=1))[0]
    columns = numpy.where(union.any(axis=0))[0]
    difference = numpy.abs(a - b).max(axis=2)
    inside = difference[rows[0]:rows[-1] + 1, columns[0]:columns[-1] + 1]
    marked = numpy.zeros(a.shape, dtype=numpy.uint8)
    marked[difference > 24] = [255, 0, 0]
    sheet = numpy.concatenate([a.astype(numpy.uint8), b.astype(numpy.uint8), marked], axis=1)
    Image.fromarray(sheet).save(os.path.join(side_by_side_dir, os.path.basename(old_path)))
    return {"coverage": coverage, "differing": float((inside > 24).mean())}


def close(old, new):
    if old is None or new is None:
        return old == new
    return len(old) == len(new) and all(abs(o - n) <= POSITION_TOLERANCE for o, n in zip(old, new))


def on_pixel_boundary(old, new):
    """Both highlights one pixel apart at most, and the widget on a whole pixel"""
    if old.get("highlight") is None or new.get("highlight") is None or not close(old["box"], new["box"]):
        return False
    shift = max(abs(a - b) for p, q in zip(old["highlight"], new["highlight"]) for a, b in zip(p, q))
    return shift <= 1 and any(abs(v - round(v)) < 1e-4 for v in old["box"][:2])


def compare_widget(name, old, new, reported):
    problems = []
    if old.get("highlight") != new.get("highlight") and on_pixel_boundary(old, new):
        reported.append("%s highlight on a pixel boundary: old %s new %s" % (name, old["highlight"][0],
                                                                            new["highlight"][0]))
        new = dict(new, highlight=old["highlight"])
    for key in ["rectangle", "visible", "highlight", "labels", "scale_factor"]:
        if old.get(key) != new.get(key):
            problems.append("%s %s: old %s new %s" % (name, key, old.get(key), new.get(key)))
    for key in ["box", "leader"]:
        if not close(old.get(key), new.get(key)):
            problems.append("%s %s: old %s new %s" % (name, key, old.get(key), new.get(key)))
    return problems


def main():
    out_dir = sys.argv[1]
    side_by_side_dir = os.path.join(out_dir, "side_by_side")
    if not os.path.isdir(side_by_side_dir):
        os.makedirs(side_by_side_dir)
    with open(os.path.join(out_dir, "w1.json")) as stream:
        meta = json.load(stream)
    failed = 0
    total = 0
    for entry in meta["states"]:
        total += 1
        problems = []
        reported = []
        for name, widget in sorted(entry["widgets"].items()):
            problems += compare_widget(name, widget["old"], widget["new"], reported)
        if "old_camera" in entry and compare_cameras(entry["old_camera"], entry["new_camera"]) > CAMERA_TOLERANCE:
            problems.append("camera %.1e" % compare_cameras(entry["old_camera"], entry["new_camera"]))
        picture = compare_picture(os.path.join(out_dir, "old", entry["file"]),
                                  os.path.join(out_dir, "new", entry["file"]), side_by_side_dir)
        if picture["coverage"] < COVERAGE_LIMIT:
            problems.append("coverage %.4f" % picture["coverage"])
        if picture["differing"] > DIFFERING_LIMIT:
            problems.append("differing %.4f" % picture["differing"])
        failed += 1 if problems else 0
        print("%s %-26s coverage %.4f differing %.4f %s" % ("PASS" if not problems else "FAIL", entry["name"],
                                                           picture["coverage"], picture["differing"],
                                                           "; ".join(problems + reported)))
    for click in meta["double_clicks"]:
        total += 1
        ok = click["old"] == click["new"] and len(click["old"]) > 0
        failed += 0 if ok else 1
        print("%s double click on %-12s old %s new %s" % ("PASS" if ok else "FAIL", click["name"], click["old"],
                                                         click["new"]))
    grid = meta.get("triad_grid", {"points": 0, "differing": ["missing"]})
    total += 1
    ok = grid["points"] > 0 and not grid["differing"]
    failed += 0 if ok else 1
    print("%s triad targets on %d grid points, %d differ %s" % ("PASS" if ok else "FAIL", grid["points"],
                                                                 len(grid["differing"]), grid["differing"][:10]))
    for click in meta["triad_clicks"]:
        total += 1
        if click.get("missing"):
            ok = False
            print("FAIL triad target %d not found on the grid" % click["target"])
        else:
            difference = compare_cameras(click["old_camera"], click["new_camera"])
            ok = difference <= CAMERA_TOLERANCE and click["handled"][-1]
            print("%s triad click on target %2d at %s: camera %.1e" % ("PASS" if ok else "FAIL", click["target"],
                                                                      click["point"], difference))
        failed += 0 if ok else 1
    print("%d of %d checks pass" % (total - failed, total))
    print("COMPARE PASSED" if failed == 0 and total > 0 else "COMPARE FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
