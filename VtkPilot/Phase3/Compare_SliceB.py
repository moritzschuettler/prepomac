"""Compares what Record_SliceB.py wrote: the ActiViz 5.8 scalar bar (old/) and its native VTK 9.7 port (new/), both
alone in the same window, through the same legend configurations and mouse events. Runs with the system Python
(numpy, Pillow).

    py -3 Compare_SliceB.py <out dir> [<side by side dir>]

The layout must agree exactly: the widget rectangle, the hover highlight, the labels, and the value edit data (the
label boxes a double click edits, their texts and limits), and a double click must open the same thing. The
pictures are compared inside the widget: the shapes must cover each other within a pixel (>= 0.98 of each one
inside the other one grown by a pixel) and the pixels differing by more than 24 levels are reported; the two
versions rasterize text differently, so that is a tolerance and the side by side pictures are for review.
"""

import json
import os
import sys

import numpy

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Phase0"))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Phase2"))
from Compare_Reference import compare_images, foreground, load  # noqa: E402
from Compare_SliceA import grow  # noqa: E402

COVERAGE_LIMIT = 0.98
# The lines of the native widgets are drawn into one pixel row, where ActiViz spread them over two gray ones
DIFFERING_LIMIT = 0.16
# The band of the hover highlight: its translucent blue lies over the black border line, which is one dark row now
HIGHLIGHT_BAND = 3


def widget_box(a, b):
    mask = foreground(a) | foreground(b)
    rows = numpy.where(mask.any(axis=1))[0]
    columns = numpy.where(mask.any(axis=0))[0]
    if len(rows) == 0:
        return None
    return rows[0], rows[-1] + 1, columns[0], columns[-1] + 1


def compare_widget(old_path, new_path):
    a = load(old_path)
    b = load(new_path)
    box = widget_box(a, b)
    if box is None:
        return {"coverage": 1.0, "differing": 0.0, "mean_abs": 0.0}
    top, bottom, left, right = box
    a = a[top:bottom, left:right]
    b = b[top:bottom, left:right]
    band = numpy.zeros(a.shape[:2], dtype=bool)
    band[HIGHLIGHT_BAND:-HIGHLIGHT_BAND, HIGHLIGHT_BAND:-HIGHLIGHT_BAND] = True
    # Inside the widget the shapes are the dark text and lines; a dark pixel is covered by a pixel of the other
    # picture within one pixel that is dark up to the same 24 levels the pixel count allows
    dark_a = (a.max(axis=2) < 128) & band
    dark_b = (b.max(axis=2) < 128) & band
    near_a = a.max(axis=2) < 128 + 24
    near_b = b.max(axis=2) < 128 + 24
    if dark_a.sum() == 0 or dark_b.sum() == 0:
        coverage = 1.0 if dark_a.sum() == dark_b.sum() else 0.0
    else:
        coverage = min(float((dark_a & grow(near_b)).sum()) / dark_a.sum(),
                       float((dark_b & grow(near_a)).sum()) / dark_b.sum())
    difference = numpy.abs(a - b).max(axis=2)
    return {"coverage": coverage, "differing": float((difference > 24).mean()),
            "mean_abs": float(numpy.abs(a - b).mean()), "box": [int(v) for v in box]}


def main():
    out_dir = sys.argv[1]
    side_by_side_dir = sys.argv[2] if len(sys.argv) > 2 else os.path.join(out_dir, "side_by_side")
    if not os.path.isdir(side_by_side_dir):
        os.makedirs(side_by_side_dir)
    with open(os.path.join(out_dir, "slice_b.json")) as stream:
        meta = json.load(stream)
    failed = 0
    for entry in meta["states"]:
        problems = []
        for key in ["rectangle", "highlight", "labels", "edit"]:
            if entry["old_" + key] != entry["new_" + key]:
                problems.append("%s old %s new %s" % (key, entry["old_" + key], entry["new_" + key]))
        old_path = os.path.join(out_dir, "old", entry["file"])
        new_path = os.path.join(out_dir, "new", entry["file"])
        compare_images(old_path, new_path, side_by_side_dir)
        picture = compare_widget(old_path, new_path)
        if picture["coverage"] < COVERAGE_LIMIT:
            problems.append("coverage %.4f" % picture["coverage"])
        if picture["differing"] > DIFFERING_LIMIT:
            problems.append("differing %.4f" % picture["differing"])
        failed += 1 if problems else 0
        print("%s %-26s rect %-20s highlight %-20s coverage %.4f differing %.4f mean %.2f %s" %
              ("PASS" if not problems else "FAIL", entry["name"], entry["new_rectangle"], entry["new_highlight"],
               picture["coverage"], picture["differing"], picture["mean_abs"], "; ".join(problems)))
    for click in meta["double_clicks"]:
        ok = click["old"] == click["new"]
        failed += 0 if ok else 1
        print("%s double click on %-10s old %s new %s" % ("PASS" if ok else "FAIL", click["name"], click["old"],
                                                         click["new"]))
    total = len(meta["states"]) + len(meta["double_clicks"])
    print("%d of %d checks pass" % (total - failed, total))
    print("COMPARE PASSED" if failed == 0 and total > 0 else "COMPARE FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
