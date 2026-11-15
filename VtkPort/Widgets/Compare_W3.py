"""Compares what Record_W3.py wrote: the cameras of the ActiViz and the native style after the same touchpad
gestures, which must start and end alike to 1e-9 of the model size, and the menu a right click on an annotation
opens on both. Runs with the system Python (numpy).

A pinch zooms about the mouse pointer, which the recorder does not place, so of a pinch only the zoom and the
orientation are compared: where the camera ends depends on where the pointer was during the run. The rectangle of
the annotation menu is the native label's since the ActiViz label went, which may differ from the ActiViz one by a
pixel.

    py -3 Compare_W3.py <out dir>
"""

import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Harness"))
from checks import compare_cameras, CAMERA_TOLERANCE  # noqa: E402

POINTER_SEQUENCES = {"pinch_zoom"}


def compare_zoom_and_orientation(old, new):
    """The largest difference of the direction of view, the view up and the parallel scale (relative)"""
    def direction(camera):
        d = [camera[3 + i] - camera[i] for i in range(3)]
        length = sum(v * v for v in d) ** 0.5
        return [v / length for v in d]
    worst = max(abs(a - b) for a, b in zip(direction(old), direction(new)))
    worst = max(worst, max(abs(old[6 + i] - new[6 + i]) for i in range(3)))
    return max(worst, abs(old[9] - new[9]) / max(1.0, abs(old[9])))


def same_menu(old, new):
    if len(old) != len(new):
        return False
    for a, b in zip(old, new):
        if a[0] != b[0] or a[2] != b[2] or max(abs(x - y) for x, y in zip(a[1], b[1])) > 1:
            return False
    return True


def main():
    with open(os.path.join(sys.argv[1], "w3.json")) as stream:
        meta = json.load(stream)
    failed = 0
    total = 0
    for sequence in meta["sequences"]:
        total += 1
        start = compare_cameras(*sequence["start"])
        if sequence["name"] in POINTER_SEQUENCES:
            end = compare_zoom_and_orientation(sequence["old"], sequence["new"])
        else:
            end = compare_cameras(sequence["old"], sequence["new"])
        moved = compare_cameras(sequence["start"][0], sequence["old"])
        ok = start <= CAMERA_TOLERANCE and end <= CAMERA_TOLERANCE and moved > 1e-6
        failed += 0 if ok else 1
        print("%s %-20s start %.1e, end %.1e, moved %.1e" % ("PASS" if ok else "FAIL", sequence["name"], start, end,
                                                            moved))
    menu = meta["context_menu"]
    total += 1
    ok = same_menu(menu["old"], menu["new"]) and len(menu["old"]) == 1
    failed += 0 if ok else 1
    print("%s right click on an annotation at %s: old %s new %s" % ("PASS" if ok else "FAIL", menu["point"],
                                                                    menu["old"], menu["new"]))
    print("%d of %d checks pass" % (total - failed, total))
    print("COMPARE PASSED" if failed == 0 and total > 0 else "COMPARE FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
