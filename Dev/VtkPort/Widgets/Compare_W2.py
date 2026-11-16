"""Compares what Record_W2.py wrote: the cameras of vtkInteractorStyleControl (ActiViz 5.8) and of its native port
after the same input. Runs with the system Python (numpy).

    py -3 Compare_W2.py <out dir>

Both cameras must start alike and end alike to 1e-9 of the model size. The rotation center picked on the model is
the one exception: ActiViz rebuilds the picked point from the depth buffer and the native view intersects the ray
exactly (slice C), so the center differs by a fraction of a pixel and the cameras to 1e-3; that is reported. The
box selection must be drawn on both while the left button drags and be cancelled on both by escape. The results
of Test_Embedded_W2.py, the embedded view driven by Win32 messages, are counted too.
"""

import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Harness"))
from checks import compare_cameras, CAMERA_TOLERANCE  # noqa: E402

PICKED_TOLERANCE = 1e-3


def main():
    with open(os.path.join(sys.argv[1], "w2.json")) as stream:
        meta = json.load(stream)
    failed = 0
    total = 0
    for sequence in meta["sequences"]:
        total += 1
        start = compare_cameras(*sequence["start"])
        end = compare_cameras(sequence["old"], sequence["new"])
        moved = compare_cameras(sequence["start"][0], sequence["old"])
        tolerance = PICKED_TOLERANCE if sequence["kind"] == "picked" else CAMERA_TOLERANCE
        ok = start <= CAMERA_TOLERANCE and end <= tolerance and moved > 1e-6
        if sequence.get("native_pending_before_finish") is False:
            ok = False
        failed += 0 if ok else 1
        note = " (reported: picked rotation center)" if sequence["kind"] == "picked" and end > CAMERA_TOLERANCE else ""
        print("%s %-30s start %.1e, end %.1e, moved %.1e%s" % ("PASS" if ok else "FAIL", sequence["name"], start, end,
                                                              moved, note))
    embedded_file = os.path.join(sys.argv[1], "w2_embedded.json")
    embedded = json.load(open(embedded_file)) if os.path.isfile(embedded_file) else [["FAIL", "no embedded run", ""]]
    for status, text, detail in embedded:
        total += 1
        failed += 0 if status == "ok" else 1
        print("%s embedded: %s %s" % ("PASS" if status == "ok" else "FAIL", text, detail))
    box = meta["box_selection"]
    total += 1
    ok = box["old"] == box["new"] == [True, True]
    failed += 0 if ok else 1
    print("%s box selection drawn and cancelled: old %s new %s" % ("PASS" if ok else "FAIL", box["old"], box["new"]))
    print("%d of %d checks pass" % (total - failed, total))
    print("COMPARE PASSED" if failed == 0 and total > 0 else "COMPARE FAILED")
    return 0 if failed == 0 and total > 0 else 1


if __name__ == "__main__":
    sys.exit(main())
