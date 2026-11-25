#PrePoMax Python Script
# Generates submap_thin.brep through GmshCommon (OCC kernel): planar faces in z = 0, 100 mm apart along x, one part
# each, and a U with a web of 2 mm whose bottom edge is shared with a flange of 20 mm standing up from it.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Submap_Thin.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
import clr
clr.AddReference('GmshCommon')
from GmshCommon import Gmsh
from System import Array, Int32, Tuple

OUT = os.environ.get("PMX_MODEL_DIR") or os.path.dirname(os.path.dirname(os.path.abspath(sys.argv[0])))
OCC = Gmsh.Model.OCC


def polygon(points, dx):
    ids = [OCC.AddPoint(x + dx, y, 0.0) for x, y in points]
    return [OCC.AddLine(ids[i], ids[(i + 1) % len(ids)]) for i in range(len(ids))]


def plane(lines):
    return OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](lines))]))


def bump(turn_deg=40, riser=20.0, joints=2):
    p = [(0.0, 0.0), (20.0, 0.0)]
    heading = 0.0
    steps = ([(turn_deg, 8.0)] * (joints - 1) + [(turn_deg, riser)] + [(-turn_deg, 8.0)] * (joints - 1) +
             [(-turn_deg, 0.0)])
    for turn, length in steps:
        heading += turn
        if length > 0:
            x, y = p[-1]
            p.append((x + length * math.cos(math.radians(heading)), y + length * math.sin(math.radians(heading))))
    y = p[-1][1]
    return p + [(80.0, y), (80.0, y + 20.0), (40.0, y + 20.0), (40.0, y + 60.0), (0.0, y + 60.0)]


SHAPES = [
    ("thin_web_U", [(0, 0), (60, 0), (60, 40), (45, 40), (45, 2), (15, 2), (15, 40), (0, 40)]),
    ("small_step", [(0, 0), (60, 0), (60, 20), (30, 20), (30, 21), (0, 21)]),
    ("thin_leg_L", [(0, 0), (60, 0), (60, 2), (2, 2), (2, 50), (0, 50)]),
    ("bump_rejected", bump(44, 20.0, 3)),
    ("bump_benign", bump(44, 10.0, 2)),
]

Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
for i, (name, points) in enumerate(SHAPES):
    print(name, [(round(x, 3), round(y, 3)) for x, y in points])
    plane(polygon([(float(x), float(y)) for x, y in points], 100.0 * i))
# The thin web U with a flange along its bottom edge, sharing that edge
dx = 100.0 * len(SHAPES)
u = [(0, 0), (60, 0), (60, 40), (45, 40), (45, 2), (15, 2), (15, 40), (0, 40)]
ids = [OCC.AddPoint(x + dx, float(y), 0.0) for x, y in u]
lines = [OCC.AddLine(ids[i], ids[(i + 1) % len(ids)]) for i in range(len(ids))]
plane(lines)
top0 = OCC.AddPoint(dx, 0.0, 20.0)
top1 = OCC.AddPoint(dx + 60.0, 0.0, 20.0)
plane([lines[0], OCC.AddLine(ids[1], top1), OCC.AddLine(top1, top0), OCC.AddLine(top0, ids[0])])
OCC.Synchronize()
OCC.Fragment(OCC.GetEntities(2), Array[Tuple[Int32, Int32]]([]), None, None, True, True)
OCC.Synchronize()
Gmsh.Write(os.path.join(OUT, "submap_thin.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
