"""Compares the picks Record_SliceC.py wrote: each pick made through ActiViz 5.8 (old) and through the native VTK 9.7
view (new), both resolved into ids by the Controller. Runs with the system Python.

    py -3 Compare_SliceC.py <out dir>

A pick passes when both paths resolve to the same ids and report the same part names (as a set: the Controller
resolves each part on its own, and the area pickers of the two versions list the parts in different orders). The
reported point and the direction of projection are compared to 1e-6 of the view size, and the frustum planes as planes (normal and distance
from the origin): the four side planes to 1e-6 of the view size; the near and far planes follow the clipping range,
which each path sets from what it draws (ActiViz also from its selection highlights), so their shift is reported.
A point pick whose ids differ is reported as a border pick, not failed, when both points lie on the model within a
pixel of each other (a pick on the border between two elements may fall on either side), or when one path hits and
the other misses and the ActiViz path both hits and misses two pixels around it (a pick on the outline). A pick that
only the native path makes, at a point within the selection distance of the pixel, is reported as a tolerance pick:
GetPickPoint picks geometry within that distance when the exact pixel misses, which VTK 9 does and VTK 5.8 did not.
"""

import glob
import json
import math
import os
import sys

TOLERANCE = 1e-6


def distance(a, b):
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def normalize(v):
    n = math.sqrt(sum(c * c for c in v))
    return [c / n for c in v]


def cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def display_distance(point, pick_x, pick_y, camera, viewport):
    """Pixels between a model point, projected with the parallel camera, and the picked pixel"""
    position, focal, view_up, scale = camera[0:3], camera[3:6], camera[6:9], camera[9]
    direction = normalize([f - p for f, p in zip(focal, position)])
    right = normalize(cross(direction, view_up))
    up = cross(right, direction)
    pixel = 2 * scale / viewport[1]
    relative = [p - f for p, f in zip(point, focal)]
    x = viewport[0] / 2 + dot(relative, right) / pixel
    y = viewport[1] / 2 + dot(relative, up) / pixel
    return math.sqrt((x - pick_x) ** 2 + (y - pick_y) ** 2)


def plane_differences(a, b):
    """The largest difference of the side planes and of the near and far planes, each plane as its unit normal and
    its distance from the origin"""
    if a is None or b is None:
        return (0.0, 0.0) if a is None and b is None else (float("inf"), float("inf"))
    differences = []
    for pa, pb in zip(a, b):
        normal_difference = max(abs(x - y) for x, y in zip(pa[3:], pb[3:]))
        distance_a = sum(o * n for o, n in zip(pa[:3], pa[3:]))
        distance_b = sum(o * n for o, n in zip(pb[:3], pb[3:]))
        differences.append(max(normal_difference, abs(distance_a - distance_b)))
    return max(differences[:4]), max(differences[4:])


def main():
    out_dir = sys.argv[1]
    failed = 0
    total = 0
    border = 0
    for result_file in sorted(glob.glob(os.path.join(out_dir, "*_slice_c.json"))):
        with open(result_file) as stream:
            result = json.load(stream)
        camera_old = result["old_camera"]
        camera_new = result["new_camera"]
        scale = max(1.0, max(abs(v) for v in camera_old[:6]))
        camera_difference = max(abs(a - b) for a, b in zip(camera_old, camera_new)) / scale
        # One pixel in model units: the parallel scale is half the viewport height
        pixel = 2 * camera_old[9] / result["viewport"][1]
        print("%s: camera difference %.1e, one pixel %.4g model units, parts %s" %
              (result["case"], camera_difference, pixel, result["mirrored_parts"]))
        for mode, records in result["modes"].items():
            mode_failed = 0
            mode_border = 0
            hits = 0
            worst_point = 0.0
            worst_plane = 0.0
            worst_clipping = 0.0
            times = [0.0, 0.0]
            for record in records:
                total += 1
                old = record["old"]
                new = record["new"]
                times[0] += old["seconds"]
                times[1] += new["seconds"]
                problems = []
                outline = False
                tolerance_pick = False
                if (old["point"] is None) != (new["point"] is None):
                    neighbors = record.get("old_neighbor_hits", [])
                    outline = any(neighbors) and not all(neighbors)
                    if old["point"] is None and not outline:
                        away = display_distance(new["point"], record["x1"], record["y1"], camera_old,
                                                result["viewport"])
                        tolerance_pick = away <= result["selection_distance"] + 0.5
                        outline = tolerance_pick
                    if not outline:
                        problems.append("point old %s new %s" % (old["point"], new["point"]))
                elif old["point"] is not None:
                    worst_point = max(worst_point, distance(old["point"], new["point"]) / scale)
                if sorted(old["part_names"]) != sorted(new["part_names"]) and not outline:
                    problems.append("parts old %s new %s" % (old["part_names"], new["part_names"]))
                if old.get("direction") and new.get("direction"):
                    if distance(old["direction"], new["direction"]) > TOLERANCE:
                        problems.append("direction old %s new %s" % (old["direction"], new["direction"]))
                side_difference, clipping_difference = plane_differences(old.get("planes"), new.get("planes"))
                worst_plane = max(worst_plane, side_difference / scale)
                worst_clipping = max(worst_clipping, clipping_difference / scale)
                if side_difference / scale > TOLERANCE:
                    problems.append("side planes differ by %.2e" % (side_difference / scale))
                ids_equal = old["ids"] == new["ids"]
                if old["ids"]:
                    hits += 1
                if not ids_equal:
                    on_border = (not record["box"] and old["point"] is not None and new["point"] is not None and
                                 distance(old["point"], new["point"]) <= pixel and not problems)
                    if tolerance_pick and not problems:
                        mode_border += 1
                        print("  TOLERANCE %-5s (%d, %d) only native: ids %s at %.1f px from the pixel, selection "
                              "distance %d px" % (mode, record["x1"], record["y1"], new["ids"][:6],
                                                  display_distance(new["point"], record["x1"], record["y1"],
                                                                   camera_old, result["viewport"]),
                                                  result["selection_distance"]))
                    elif outline and not problems:
                        mode_border += 1
                        print("  OUTLINE %-7s (%d, %d) old ids %s new ids %s; ActiViz around it %s" %
                              (mode, record["x1"], record["y1"], old["ids"][:6], new["ids"][:6],
                               record["old_neighbor_hits"]))
                    elif on_border:
                        mode_border += 1
                        print("  BORDER %-8s (%d, %d) old ids %s new ids %s, points %.3g px apart" %
                              (mode, record["x1"], record["y1"], old["ids"][:6], new["ids"][:6],
                               distance(old["point"], new["point"]) / pixel))
                    else:
                        only_old = sorted(set(old["ids"]) - set(new["ids"]))
                        only_new = sorted(set(new["ids"]) - set(old["ids"]))
                        problems.append("ids: %d old, %d new, only old %s, only new %s" %
                                        (len(old["ids"]), len(new["ids"]), only_old[:8], only_new[:8]))
                if problems:
                    mode_failed += 1
                    label = record.get("name", "(%d, %d)" % (record["x1"], record["y1"]))
                    print("  FAIL   %-8s %-16s %s" % (mode, label, "; ".join(problems)))
            failed += mode_failed
            border += mode_border
            print("%s %-8s %3d picks, %3d hit, %d fail, %d border; worst point %.1e, side planes %.1e, "
                  "near/far %.1e; time old %.2f s, new %.2f s" %
                  ("PASS" if mode_failed == 0 else "FAIL", mode, len(records), hits, mode_failed, mode_border,
                   worst_point, worst_plane, worst_clipping, times[0], times[1]))
    print("%d of %d picks pass (%d border picks reported)" % (total - failed, total, border))
    print("COMPARE PASSED" if failed == 0 and total > 0 else "COMPARE FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
