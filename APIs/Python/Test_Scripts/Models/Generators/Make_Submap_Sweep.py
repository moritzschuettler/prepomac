#PrePoMax Python Script
# Generates submap_sweep.brep through GmshCommon (OCC kernel): solids 150 mm apart along x, one part each (the last
# two compounds of two cells), every profile drawn in z = 0 and extruded by 40 mm along z unless said otherwise.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Submap_Sweep.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
import clr
clr.AddReference('GmshCommon')
from GmshCommon import Gmsh
from System import Array, Int32, Double, Tuple

OUT = os.environ.get("PMX_MODEL_DIR") or os.path.dirname(os.path.dirname(os.path.abspath(sys.argv[0])))
OCC = Gmsh.Model.OCC


def dimtags(dim, tags):
    return Array[Tuple[Int32, Int32]]([Tuple[Int32, Int32](dim, t) for t in tags])


def profile(points, dx=0.0, holes=()):
    ids = [OCC.AddPoint(float(x) + dx, float(y), 0.0) for x, y in points]
    lines = [OCC.AddLine(ids[i], ids[(i + 1) % len(ids)]) for i in range(len(ids))]
    loops = [OCC.AddCurveLoop(Array[Int32](lines))]
    for cx, cy, r in holes:
        c = OCC.AddPoint(cx + dx, cy, 0.0)
        p1 = OCC.AddPoint(cx + dx + r, cy, 0.0)
        p2 = OCC.AddPoint(cx + dx - r, cy, 0.0)
        arcs = [OCC.AddCircleArc(p1, c, p2, True), OCC.AddCircleArc(p2, c, p1, True)]
        loops.append(OCC.AddCurveLoop(Array[Int32](arcs)))
        OCC.Remove(dimtags(0, [c]), False)
    return OCC.AddPlaneSurface(Array[Int32](loops))


def extrude(surface, height=40.0):
    out = OCC.Extrude(dimtags(2, [surface]), 0.0, 0.0, height, None, Array[Int32]([]), Array[Double]([]), False)
    return [t.Item2 for t in out if t.Item1 == 3][0]


L = [(0, 0), (60, 0), (60, 20), (20, 20), (20, 50), (0, 50)]
SHAPES = [
    ("L", L),
    ("U", [(0, 0), (60, 0), (60, 50), (45, 50), (45, 15), (15, 15), (15, 50), (0, 50)]),
    ("T", [(20, 0), (40, 0), (40, 30), (60, 30), (60, 45), (0, 45), (0, 30), (20, 30)]),
    ("plus", [(20, 0), (40, 0), (40, 20), (60, 20), (60, 40), (40, 40), (40, 60), (20, 60), (20, 40), (0, 40),
              (0, 20), (20, 20)]),
    ("staircase", [(0, 0), (60, 0), (60, 20), (40, 20), (40, 40), (20, 40), (20, 60), (0, 60)]),
    ("thin_leg_L", [(0, 0), (60, 0), (60, 2), (2, 2), (2, 50), (0, 50)]),
]

Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
for i, (name, points) in enumerate(SHAPES):
    extrude(profile(points, 150.0 * i))
    print(name, "at x", 150.0 * i)
# An L profile in the plane y = 0, 30 to 90 mm from the z axis, swept a quarter turn about that axis
dx = 150.0 * len(SHAPES)
ids = [OCC.AddPoint(dx + 30.0 + x * 1.0, 0.0, y * 1.0) for x, y in L]
lines = [OCC.AddLine(ids[k], ids[(k + 1) % len(ids)]) for k in range(len(ids))]
bent = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](lines))]))
OCC.Revolve(dimtags(2, [bent]), dx, 0.0, 0.0, 0.0, 0.0, 1.0, math.pi / 2, None, Array[Int32]([]), Array[Double]([]),
            False)
print("bent L at x", dx)
# Two L cells stacked, the compound swept as two blocks
dx += 150.0
lower = extrude(profile(L, dx), 20.0)
upper_face = profile([(x, y) for x, y in L], dx)
OCC.Translate(dimtags(2, [upper_face]), 0.0, 0.0, 20.0)
upper = extrude(upper_face, 20.0)
print("stacked L at x", dx)
stack = [lower, upper]
# A house of five sides, a source neither structured nor submapped, swept with an L by one item
dx += 150.0
extrude(profile([(0, 0), (60, 0), (60, 30), (30, 45), (0, 30)], dx))
print("house at x", dx)
# Two L cells of 15 and 25 mm stacked, which no mirror maps onto each other, swept as two blocks
dx += 150.0
lower2 = extrude(profile(L, dx), 15.0)
upper2_face = profile(L, dx)
OCC.Translate(dimtags(2, [upper2_face]), 0.0, 0.0, 15.0)
upper2 = extrude(upper2_face, 25.0)
print("uneven stack at x", dx)
OCC.Synchronize()
OCC.Fragment(dimtags(3, stack), dimtags(3, []), None, None, True, True)
OCC.Fragment(dimtags(3, [lower2, upper2]), dimtags(3, []), None, None, True, True)
OCC.Synchronize()
Gmsh.Write(os.path.join(OUT, "submap_sweep.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
