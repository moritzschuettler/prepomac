#PrePoMax Python Script
# Writes submap_holes.brep (shell faces side by side, one part each) and submap_holes_sweep.brep (extruded solids) for
# Test_Model_Meshing_Submap_Holes.py, through GmshCommon (OCC kernel).
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Submap_Holes.py -g No -x Yes -w <work folder>
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
SPACING = 150.0


def dimtags(dim, tags):
    return Array[Tuple[Int32, Int32]]([Tuple[Int32, Int32](dim, t) for t in tags])


def start():
    Gmsh.Initialize()
    Gmsh.Option.SetNumber("General.Terminal", 0)
    Gmsh.Model.Add("m")


def finish(name):
    OCC.Synchronize()
    Gmsh.Write(os.path.join(OUT, name))
    Gmsh.FinalizeAll()
    print("written", name)


def loop(points, dx):
    """A curve loop through the points (x, y) or (x, y, cx, cy): the edge from a point of four values to the next is an
    arc about (cx, cy), every other edge a line."""
    ids = [OCC.AddPoint(float(p[0]) + dx, float(p[1]), 0.0) for p in points]
    centres = []
    curves = []
    for i, p in enumerate(points):
        j = (i + 1) % len(points)
        if len(p) == 4:
            centre = OCC.AddPoint(float(p[2]) + dx, float(p[3]), 0.0)
            centres.append(centre)
            curves.append(OCC.AddCircleArc(ids[i], centre, ids[j], True))
        else:
            curves.append(OCC.AddLine(ids[i], ids[j]))
    return OCC.AddCurveLoop(Array[Int32](curves)), centres


def face(outer, holes, dx):
    """A plane surface bounded by the outer outline with the holes cut out of it, shifted by dx along x."""
    loops = []
    centres = []
    for points in [outer] + holes:
        tag, c = loop(points, dx)
        loops.append(tag)
        centres.extend(c)
    surface = OCC.AddPlaneSurface(Array[Int32](loops))
    if centres:
        OCC.Remove(dimtags(0, centres), False)
    return surface


def extrude(surface, height):
    OCC.Extrude(dimtags(2, [surface]), 0.0, 0.0, height, None, Array[Int32]([]), Array[Double]([]), False)


def rectangle(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def rounded(x0, y0, x1, y1, r):
    return [(x0 + r, y0), (x1 - r, y0, x1 - r, y0 + r), (x1, y0 + r), (x1, y1 - r, x1 - r, y1 - r), (x1 - r, y1),
            (x0 + r, y1, x0 + r, y1 - r), (x0, y1 - r), (x0, y0 + r, x0 + r, y0 + r)]


def transformed(points, function):
    result = []
    for p in points:
        q = list(function(p[0], p[1]))
        if len(p) == 4:
            q.extend(function(p[2], p[3]))
        result.append(tuple(q))
    return result


SHEAR = math.tan(math.radians(15))
L_OUTER = [(0, 0), (60, 0), (60, 20), (20, 20), (20, 50), (0, 50)]
L_HOLE = rectangle(35, 5, 45, 15)
U_OUTER = [(0, 0), (60, 0), (60, 50), (40, 50), (40, 20), (20, 20), (20, 50), (0, 50)]
L_SHAPED_HOLE = [(15, 15), (45, 15), (45, 25), (25, 25), (25, 45), (15, 45)]
DIAMOND = [(30, 15), (45, 30), (30, 45), (15, 30)]
ROUND = [(20, 20, 30, 20), (40, 20, 30, 20)]
# The mirrored copy of the L with a square hole, x running the other way
MIRRORED_L = transformed(L_OUTER, lambda x, y: (60 - x, y))
MIRRORED_L_HOLE = transformed(L_HOLE, lambda x, y: (60 - x, y))

SHELLS = [("centred hole", rectangle(0, 0, 60, 40), [rectangle(15, 10, 45, 30)]),
          ("off-centre hole", rectangle(0, 0, 60, 40), [rectangle(8, 22, 28, 32)]),
          ("two holes", rectangle(0, 0, 80, 40), [rectangle(10, 10, 30, 30), rectangle(50, 10, 70, 30)]),
          ("L-shaped hole", rectangle(0, 0, 60, 60), [L_SHAPED_HOLE]),
          ("filleted hole", rectangle(0, 0, 60, 40), [rounded(15, 10, 45, 30, 4)]),
          ("L with hole", L_OUTER, [L_HOLE]),
          ("U with hole", U_OUTER, [rectangle(15, 6, 45, 14)]),
          ("sheared", transformed(rectangle(0, 0, 60, 40), lambda x, y: (x + SHEAR * y, y)),
           [transformed(rectangle(15, 10, 45, 30), lambda x, y: (x + SHEAR * y, y))]),
          ("mirrored L with hole", MIRRORED_L, [MIRRORED_L_HOLE]),
          ("diamond hole", rectangle(0, 0, 60, 60), [DIAMOND]),
          ("round hole", rectangle(0, 0, 60, 40), [ROUND]),
          ("thin border", rectangle(0, 0, 60, 40), [rectangle(1, 10, 21, 30)])]
start()
for i, (name, outer, holes) in enumerate(SHELLS):
    face(outer, holes, SPACING * i)
    print("shell", name, "at x", SPACING * i)
finish("submap_holes.brep")

SOLIDS = [("tube", rectangle(0, 0, 60, 40), [rectangle(10, 10, 50, 30)]),
          ("thin tube", rectangle(0, 0, 60, 40), [rectangle(2, 2, 58, 38)]),
          ("box section", rectangle(0, 0, 80, 40), [rectangle(5, 5, 37.5, 35), rectangle(42.5, 5, 75, 35)]),
          ("L with hole", L_OUTER, [L_HOLE]),
          ("filleted tube", rectangle(0, 0, 60, 40), [rounded(10, 10, 50, 30, 4)])]
start()
for i, (name, outer, holes) in enumerate(SOLIDS):
    extrude(face(outer, holes, SPACING * i), 30.0)
    print("solid", name, "at x", SPACING * i)
finish("submap_holes_sweep.brep")
print("TEST PASSED")
