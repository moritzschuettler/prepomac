"""The comparisons of the port checks, with the system Python (numpy, Pillow). Each takes the old (ActiViz) and the
new (native) output of a recorder.

    py -3 checks.py scenes <out dir>     compares every <scene>_scene.json of Record_Scene.py

Cameras must agree to 1e-9 of the model size. Pictures: the shapes must cover each other within one pixel of their
outline (>= 0.999 of each shape inside the other one grown by a pixel) and at most 2 % of the pixels may differ by
more than 24 levels; the mean absolute difference is reported for the line quality. Side by side pictures are
written for review. The thresholds are those slice A of the pilot settled.
"""

import glob
import json
import os
import sys

import numpy
from PIL import Image

CAMERA_TOLERANCE = 1e-9
COVERAGE_LIMIT = 0.999
DIFFERING_LIMIT = 0.02
LEVELS = 24
BACKGROUND_TOLERANCE = 12


def load(path):
    return numpy.asarray(Image.open(path).convert("RGB")).astype(numpy.int16)


def foreground(image):
    """What is not background: the background is a vertical gradient whose rightmost column is free in every view"""
    return numpy.abs(image - image[:, -1:, :]).max(axis=2) > BACKGROUND_TOLERANCE


def grow(mask):
    grown = mask.copy()
    padded = numpy.pad(mask, 1)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            grown |= padded[1 + dy:1 + dy + mask.shape[0], 1 + dx:1 + dx + mask.shape[1]]
    return grown


def compare_cameras(old, new):
    old = numpy.array(old)
    new = numpy.array(new)
    size = max(1.0, float(numpy.abs(old[:6]).max()))
    return float(numpy.abs(old - new).max()) / size


def compare_pictures(old_path, new_path, side_by_side_dir=None):
    a = load(old_path)
    b = load(new_path)
    if a.shape != b.shape:
        return {"size_mismatch": [list(a.shape), list(b.shape)], "coverage": 0.0, "differing": 1.0, "mean_abs": 255}
    mask_a = foreground(a)
    mask_b = foreground(b)
    if mask_a.sum() == 0 or mask_b.sum() == 0:
        coverage = 1.0 if mask_a.sum() == mask_b.sum() else 0.0
    else:
        coverage = min(float((mask_a & grow(mask_b)).sum()) / mask_a.sum(),
                       float((mask_b & grow(mask_a)).sum()) / mask_b.sum())
    difference = numpy.abs(a - b).max(axis=2)
    result = {"coverage": coverage, "differing": float((difference > LEVELS).mean()),
              "mean_abs": float(numpy.abs(a - b).mean()), "max": int(difference.max())}
    if side_by_side_dir:
        if not os.path.isdir(side_by_side_dir):
            os.makedirs(side_by_side_dir)
        marked = numpy.zeros(a.shape, dtype=numpy.uint8)
        marked[difference > LEVELS] = [255, 0, 0]
        sheet = numpy.concatenate([a.astype(numpy.uint8), b.astype(numpy.uint8), marked], axis=1)
        Image.fromarray(sheet).save(os.path.join(side_by_side_dir, os.path.basename(old_path)))
    return result


def check_scenes(out_dir):
    failed = 0
    total = 0
    mean_abs = []
    for record_file in sorted(glob.glob(os.path.join(out_dir, "*_scene.json"))):
        with open(record_file) as stream:
            record = json.load(stream)
        for error in record.get("mirror_errors", []):
            failed += 1
            print("FAIL mirror error in %s: %s" % (record["scene"], error))
        for image in record["images"]:
            total += 1
            camera = compare_cameras(image["old_camera"], image["new_camera"])
            picture = compare_pictures(os.path.join(out_dir, "old", image["file"]),
                                       os.path.join(out_dir, "new", image["file"]),
                                       os.path.join(out_dir, "side_by_side"))
            camera_tolerance = record.get("camera_tolerance") or CAMERA_TOLERANCE
            ok = (camera <= camera_tolerance and picture["coverage"] >= COVERAGE_LIMIT and
                  picture["differing"] <= DIFFERING_LIMIT)
            failed += 0 if ok else 1
            mean_abs.append(picture["mean_abs"])
            print("%s %-45s camera %.1e, coverage %.4f, differing %.4f, mean %.2f" %
                  ("PASS" if ok else "FAIL", image["file"], camera, picture["coverage"], picture["differing"],
                   picture["mean_abs"]))
    if mean_abs:
        print("mean absolute difference over all pictures %.3f levels" % (sum(mean_abs) / len(mean_abs)))
    print("%d of %d pictures pass" % (total - failed, total))
    print("CHECK PASSED" if failed == 0 and total > 0 else "CHECK FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    if len(sys.argv) != 3 or sys.argv[1] != "scenes":
        sys.stderr.write(__doc__)
        sys.exit(2)
    sys.exit(check_scenes(sys.argv[2]))
