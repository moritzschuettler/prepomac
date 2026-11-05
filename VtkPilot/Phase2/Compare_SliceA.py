"""Compares the slice A pictures and cameras Record_SliceA.py wrote: old/ is ActiViz 5.8, new/ is the native VTK 9.7
port, both of the same scene in one process. Runs with the system Python (numpy, Pillow).

    py -3 Compare_SliceA.py <out dir> [<side by side dir>]

A view passes when the cameras agree to 1e-9 of the model size, the shapes cover the same pixels within one pixel of
their outline (>= 0.999 of each shape inside the other one grown by a pixel; the two versions antialias the
silhouette differently) and at most 2 % of the pixels differ by more than 24 levels in any channel. VTK 5.8 and
9.7 do not render identically, so the pixel limit is a tolerance, and the side by side pictures are for review.
"""

import glob
import json
import os
import sys

import numpy

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Phase0"))
from Compare_Reference import compare_images, foreground, load  # noqa: E402

CAMERA_TOLERANCE = 1e-9
COVERAGE_LIMIT = 0.999
DIFFERING_LIMIT = 0.02


def grow(mask):
    grown = mask.copy()
    padded = numpy.pad(mask, 1)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            grown |= padded[1 + dy:1 + dy + mask.shape[0], 1 + dx:1 + dx + mask.shape[1]]
    return grown


def tolerant_coverage(old_path, new_path):
    a = foreground(load(old_path))
    b = foreground(load(new_path))
    if a.sum() == 0 or b.sum() == 0:
        return 1.0 if a.sum() == b.sum() else 0.0
    return min(float((a & grow(b)).sum()) / a.sum(), float((b & grow(a)).sum()) / b.sum())


def main():
    out_dir = sys.argv[1]
    side_by_side_dir = sys.argv[2] if len(sys.argv) > 2 else os.path.join(out_dir, "side_by_side")
    if not os.path.isdir(side_by_side_dir):
        os.makedirs(side_by_side_dir)
    failed = 0
    total = 0
    worst = {"differing": 0.0, "iou": 1.0, "camera": 0.0}
    for meta_file in sorted(glob.glob(os.path.join(out_dir, "*_slice_a.json"))):
        with open(meta_file) as stream:
            meta = json.load(stream)
        for image in meta["images"]:
            total += 1
            old = numpy.array(image["old_camera"])
            new = numpy.array(image["new_camera"])
            size = max(1.0, float(numpy.abs(old[:6]).max()))
            camera = float(numpy.abs(old - new).max()) / size
            result = compare_images(os.path.join(out_dir, "old", image["file"]),
                                    os.path.join(out_dir, "new", image["file"]), side_by_side_dir)
            coverage = tolerant_coverage(os.path.join(out_dir, "old", image["file"]),
                                         os.path.join(out_dir, "new", image["file"]))
            mean_abs = float(numpy.abs(load(os.path.join(out_dir, "old", image["file"])) -
                                       load(os.path.join(out_dir, "new", image["file"]))).mean())
            ok = (camera <= CAMERA_TOLERANCE and coverage >= COVERAGE_LIMIT and
                  result["differing_pixels"] <= DIFFERING_LIMIT)
            failed += 0 if ok else 1
            worst["differing"] = max(worst["differing"], result["differing_pixels"])
            worst["iou"] = min(worst["iou"], result["coverage_iou"])
            worst["coverage"] = min(worst.get("coverage", 1.0), coverage)
            worst["camera"] = max(worst["camera"], camera)
            worst["mean_abs"] = worst.get("mean_abs", 0.0) + mean_abs
            print("%s %-50s camera %.1e, differing %.4f, coverage %.4f (IoU %.4f), max %3d" %
                  ("PASS" if ok else "FAIL", image["file"], camera, result["differing_pixels"], coverage,
                   result["coverage_iou"], result["max_difference"]))
    print("worst: camera %.1e, differing %.4f, coverage %.4f, IoU %.4f" %
          (worst["camera"], worst["differing"], worst.get("coverage", 1.0), worst["iou"]))
    print("mean absolute difference over all views %.3f levels (line quality; review crops too)" %
          (worst.get("mean_abs", 0.0) / max(1, total)))
    print("%d of %d views pass" % (total - failed, total))
    print("COMPARE PASSED" if failed == 0 and total > 0 else "COMPARE FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
