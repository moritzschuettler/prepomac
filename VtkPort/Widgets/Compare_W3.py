"""Compares what Record_W3.py wrote: the cameras of the ActiViz and the native style after the same touchpad
gestures, which must start and end alike to 1e-9 of the model size, and the menu a right click on an annotation
opens on both. Runs with the system Python (numpy).

    py -3 Compare_W3.py <out dir>
"""

import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Harness"))
from checks import compare_cameras, CAMERA_TOLERANCE  # noqa: E402


def main():
    with open(os.path.join(sys.argv[1], "w3.json")) as stream:
        meta = json.load(stream)
    failed = 0
    total = 0
    for sequence in meta["sequences"]:
        total += 1
        start = compare_cameras(*sequence["start"])
        end = compare_cameras(sequence["old"], sequence["new"])
        moved = compare_cameras(sequence["start"][0], sequence["old"])
        ok = start <= CAMERA_TOLERANCE and end <= CAMERA_TOLERANCE and moved > 1e-6
        failed += 0 if ok else 1
        print("%s %-20s start %.1e, end %.1e, moved %.1e" % ("PASS" if ok else "FAIL", sequence["name"], start, end,
                                                            moved))
    menu = meta["context_menu"]
    total += 1
    ok = menu["old"] == menu["new"] and len(menu["old"]) == 1
    failed += 0 if ok else 1
    print("%s right click on an annotation at %s: old %s new %s" % ("PASS" if ok else "FAIL", menu["point"],
                                                                    menu["old"], menu["new"]))
    print("%d of %d checks pass" % (total - failed, total))
    print("COMPARE PASSED" if failed == 0 and total > 0 else "COMPARE FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
