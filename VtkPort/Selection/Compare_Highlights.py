"""Compares the highlight pictures Record_Selection_Highlights.py wrote. Runs with the system Python.

    py -3 Compare_Highlights.py <out dir>

On each path the highlight is what the picture with it changed against the picture of the same mode without it (more
than 24 levels).
A highlight passes when both paths highlight something or both nothing, the two highlights cover each other within
two pixels of their outline (at least COVERAGE_LIMIT of each inside the other one grown by two pixels; points and
lines of VTK 9 are drawn with multisampling and come out a pixel wider or narrower), and their mean colors differ by
at most COLOR_LIMIT levels. The pictures without a highlight are compared as checks.py compares a scene. Side by
side pictures (old, new, the two highlights in red and green) are written for review. A point highlight that differs
at a point the recorder marked as a border pick (a click there selects other ids on the two paths) is reported, not
failed.
"""

import glob
import json
import os
import sys

import numpy
from PIL import Image

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Harness"))
import checks  # noqa: E402

LEVELS = 24
COVERAGE_LIMIT = 0.95
COLOR_LIMIT = 40


def highlight_mask(picture, base):
    return numpy.abs(picture - base).max(axis=2) > LEVELS


def grow(mask, steps):
    for _ in range(steps):
        mask = checks.grow(mask)
    return mask


def main():
    out_dir = sys.argv[1]
    failed = 0
    total = 0
    for record_file in sorted(glob.glob(os.path.join(out_dir, "*_highlights.json"))):
        with open(record_file) as stream:
            record = json.load(stream)
        total += 1
        base_old_path = os.path.join(out_dir, "old", record["base"]["old"])
        base_new_path = os.path.join(out_dir, "new", record["base"]["new"])
        camera = checks.compare_cameras(record["old_camera"], record["new_camera"])
        base = checks.compare_pictures(base_old_path, base_new_path, os.path.join(out_dir, "side_by_side"))
        ok = (camera <= checks.CAMERA_TOLERANCE and base["coverage"] >= checks.COVERAGE_LIMIT and
              base["differing"] <= checks.DIFFERING_LIMIT)
        failed += 0 if ok else 1
        print("%s %-40s camera %.1e, coverage %.4f, differing %.4f" %
              ("PASS" if ok else "FAIL", record["base"]["old"], camera, base["coverage"], base["differing"]))
        for highlight in record["highlights"]:
            base_old = checks.load(os.path.join(out_dir, "old", highlight["base"]))
            base_new = checks.load(os.path.join(out_dir, "new", highlight["base"]))
            total += 1
            old = checks.load(os.path.join(out_dir, "old", highlight["file"]))
            new = checks.load(os.path.join(out_dir, "new", highlight["file"]))
            mask_old = highlight_mask(old, base_old)
            mask_new = highlight_mask(new, base_new)
            count_old = int(mask_old.sum())
            count_new = int(mask_new.sum())
            if count_old == 0 or count_new == 0:
                coverage = 1.0 if count_old == count_new else 0.0
                color = 0.0
            else:
                coverage = min(float((mask_old & grow(mask_new, 2)).sum()) / count_old,
                               float((mask_new & grow(mask_old, 2)).sum()) / count_new)
                color = float(numpy.abs(old[mask_old].mean(axis=0) - new[mask_new].mean(axis=0)).max())
            ok = coverage >= COVERAGE_LIMIT and color <= COLOR_LIMIT
            border = not ok and highlight.get("border", False)
            failed += 0 if ok or border else 1
            print("%s %-40s pixels old %6d new %6d, coverage %.4f, color %.1f" %
                  ("PASS" if ok else "BORDER" if border else "FAIL", highlight["file"], count_old, count_new,
                   coverage, color))
            sheet_dir = os.path.join(out_dir, "side_by_side")
            if not os.path.isdir(sheet_dir):
                os.makedirs(sheet_dir)
            marks = numpy.zeros(old.shape, dtype=numpy.uint8)
            marks[mask_old] = [255, 0, 0]
            marks[mask_new, 1] = 255
            sheet = numpy.concatenate([old.astype(numpy.uint8), new.astype(numpy.uint8), marks], axis=1)
            Image.fromarray(sheet).save(os.path.join(sheet_dir, highlight["file"]))
    print("%d of %d highlight pictures pass" % (total - failed, total))
    print("COMPARE PASSED" if failed == 0 and total > 0 else "COMPARE FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
