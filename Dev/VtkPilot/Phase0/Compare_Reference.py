"""Compares two reference folders written by Record_Reference.py: the images by pixel difference and shape coverage,
the picks by exact ids and by point distance. Runs with the system Python (numpy, Pillow).

    py -3 Compare_Reference.py <reference dir> <candidate dir> [<side by side output dir>]

A pick whose ids differ is reported as a border pick when the reference point picks of a neighbouring grid
cell hit a different id set, since a pixel on the border between two elements may go either way.
"""

import json
import os
import sys

import numpy
from PIL import Image

BACKGROUND_TOLERANCE = 12
PIXEL_TOLERANCE = 24


def load(path):
    return numpy.asarray(Image.open(path).convert("RGB")).astype(numpy.int16)


def foreground(image):
    # The background is a vertical gradient; the rightmost column is background in every view
    background = image[:, -1:, :]
    return numpy.abs(image - background).max(axis=2) > BACKGROUND_TOLERANCE


def compare_images(reference_path, candidate_path, side_by_side_dir):
    a = load(reference_path)
    b = load(candidate_path)
    if a.shape != b.shape:
        return {"size_mismatch": [list(a.shape), list(b.shape)]}
    difference = numpy.abs(a - b).max(axis=2)
    mask_a = foreground(a)
    mask_b = foreground(b)
    union = numpy.logical_or(mask_a, mask_b).sum()
    result = {"identical": bool(difference.max() == 0),
              "max_difference": int(difference.max()),
              "differing_pixels": float((difference > PIXEL_TOLERANCE).mean()),
              "coverage_iou": float(numpy.logical_and(mask_a, mask_b).sum() / union) if union else 1.0}
    if side_by_side_dir is not None and not result["identical"]:
        heat = numpy.zeros_like(a)
        heat[..., 0] = numpy.clip(difference * 4, 0, 255)
        row = numpy.concatenate([a, b, heat], axis=1).astype(numpy.uint8)
        Image.fromarray(row).save(os.path.join(side_by_side_dir, os.path.basename(reference_path)))
    return result


def compare_picks(reference, candidate):
    report = {}
    columns, rows = reference["grid"]
    for mode, data in reference["modes"].items():
        other = candidate["modes"][mode]
        span = 0.0
        points = [p["point"] for p in data["points"] if p.get("point")]
        if points:
            array = numpy.array(points)
            span = float(numpy.linalg.norm(array.max(axis=0) - array.min(axis=0)))
        mismatched, border, worst_distance = [], [], 0.0
        for index, (p, q) in enumerate(zip(data["points"], other["points"])):
            if p.get("point") and q.get("point"):
                worst_distance = max(worst_distance, float(numpy.linalg.norm(numpy.subtract(p["point"], q["point"]))))
            if p["ids"] == q["ids"]:
                continue
            i, j = index % columns, index // columns
            neighbours = [data["points"][jj * columns + ii]["ids"] for ii, jj in
                          [(i - 1, j), (i + 1, j), (i, j - 1), (i, j + 1)] if 0 <= ii < columns and 0 <= jj < rows]
            (border if any(n != p["ids"] for n in neighbours) else mismatched).append([p["x1"], p["y1"]])
        boxes = [b["name"] for b, c in zip(data["boxes"], other["boxes"]) if b["ids"] != c["ids"]]
        report[mode] = {"point_mismatches": mismatched, "border_mismatches": border, "box_mismatches": boxes,
                        "max_point_distance": worst_distance, "point_span": span}
    return report


def main():
    reference_dir, candidate_dir = sys.argv[1], sys.argv[2]
    side_by_side_dir = sys.argv[3] if len(sys.argv) > 3 else None
    if side_by_side_dir and not os.path.isdir(side_by_side_dir):
        os.makedirs(side_by_side_dir)
    failed = False
    for name in sorted(os.listdir(reference_dir)):
        reference_path = os.path.join(reference_dir, name)
        candidate_path = os.path.join(candidate_dir, name)
        if not os.path.exists(candidate_path):
            print("MISSING  " + name)
            failed = True
        elif name.endswith(".png"):
            r = compare_images(reference_path, candidate_path, side_by_side_dir)
            if "size_mismatch" in r:
                print("SIZE     %s %s" % (name, r["size_mismatch"]))
                failed = True
            else:
                print("%s %-55s max %3d, differing %.4f, coverage IoU %.4f" %
                      ("SAME    " if r["identical"] else "DIFFERS ", name, r["max_difference"],
                       r["differing_pixels"], r["coverage_iou"]))
        elif name.endswith("_picks.json"):
            with open(reference_path) as a, open(candidate_path) as b:
                report = compare_picks(json.load(a), json.load(b))
            for mode, r in report.items():
                bad = r["point_mismatches"] or r["box_mismatches"]
                failed = failed or bool(bad)
                print("%s %-10s point mismatches %d, border %d, box mismatches %s, max point distance %.3g "
                      "(span %.3g)" % ("PICKS-BAD" if bad else "PICKS-OK ", mode, len(r["point_mismatches"]),
                                        len(r["border_mismatches"]), r["box_mismatches"], r["max_point_distance"],
                                        r["point_span"]))
    print("COMPARE FAILED" if failed else "COMPARE PASSED")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
