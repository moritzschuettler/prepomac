#PrePoMax Python Script
# Writes ring_faces.brep (shell faces side by side, one part each) and ring_sweep.brep (extruded solids) for
# Test_Model_Meshing_Ring.py, through GmshCommon (OCC kernel).
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Ring_Faces.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
#
# A loop is a list of pieces, each starting at its first point:
#   ("line", (x, y))                    a line to the start of the next piece
#   ("arc", (x, y), (mx, my))           a circle arc through (mx, my) to the start of the next piece
#   ("circle", (cx, cy), r, angle)      a whole circle of one closed edge, its vertex at the angle in degrees
#   ("ellipse", (cx, cy), a, b)         a whole ellipse of one closed edge, a rational B-spline
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


def closed_circle(cx, cy, r, angle_deg):
    a = math.radians(angle_deg)
    point = OCC.AddPoint(cx + r * math.cos(a), cy + r * math.sin(a), 0.0)
    out = OCC.Revolve(dimtags(0, [point]), cx, cy, 0.0, 0.0, 0.0, 1.0, 2 * math.pi, None, Array[Int32]([]),
                      Array[Double]([]), False)
    curves = [t.Item2 for t in out if t.Item1 == 1]
    return curves


def closed_ellipse(cx, cy, a, b):
    w = math.sqrt(0.5)
    unit = [(1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1)]
    tags = [OCC.AddPoint(cx + a * x, cy + b * y, 0.0) for x, y in unit]
    tags.append(tags[0])
    weights = [1, w, 1, w, 1, w, 1, w, 1]
    return [OCC.AddBSpline(Array[Int32](tags), -1, 2, Array[Double](weights),
                           Array[Double]([0.0, 0.25, 0.5, 0.75, 1.0]), Array[Int32]([2, 2, 2, 2, 2]))]


def loop(pieces, dx):
    if pieces[0][0] == "circle":
        (cx, cy), r, angle = pieces[0][1], pieces[0][2], pieces[0][3]
        return OCC.AddCurveLoop(Array[Int32](closed_circle(cx + dx, cy, r, angle))), []
    if pieces[0][0] == "ellipse":
        (cx, cy), a, b = pieces[0][1], pieces[0][2], pieces[0][3]
        return OCC.AddCurveLoop(Array[Int32](closed_ellipse(cx + dx, cy, a, b))), []
    ids = [OCC.AddPoint(float(p[1][0]) + dx, float(p[1][1]), 0.0) for p in pieces]
    middles = []
    curves = []
    for i, p in enumerate(pieces):
        j = (i + 1) % len(pieces)
        if p[0] == "arc":
            middle = OCC.AddPoint(float(p[2][0]) + dx, float(p[2][1]), 0.0)
            middles.append(middle)
            curves.append(OCC.AddCircleArc(ids[i], middle, ids[j], False))
        else:
            curves.append(OCC.AddLine(ids[i], ids[j]))
    return OCC.AddCurveLoop(Array[Int32](curves)), middles


def face(outer, holes, dx):
    loops = []
    middles = []
    for pieces in [outer] + holes:
        tag, m = loop(pieces, dx)
        loops.append(tag)
        middles.extend(m)
    surface = OCC.AddPlaneSurface(Array[Int32](loops))
    if middles:
        OCC.Remove(dimtags(0, middles), False)
    return surface


def extrude(surface, height):
    OCC.Extrude(dimtags(2, [surface]), 0.0, 0.0, height, None, Array[Int32]([]), Array[Double]([]), False)


def rectangle(x0, y0, x1, y1):
    return [("line", (x0, y0)), ("line", (x1, y0)), ("line", (x1, y1)), ("line", (x0, y1))]


def polygon(points):
    return [("line", p) for p in points]


def at(cx, cy, r, angle_deg):
    a = math.radians(angle_deg)
    return (cx + r * math.cos(a), cy + r * math.sin(a))


def circle(cx, cy, r, angle=0.0):
    return [("circle", (cx, cy), r, angle)]


def arcs(cx, cy, r, angles):
    """A circle of arcs between the given angles, counter-clockwise."""
    pieces = []
    for i, a in enumerate(angles):
        b = angles[(i + 1) % len(angles)]
        if b <= a:
            b += 360
        pieces.append(("arc", at(cx, cy, r, a), at(cx, cy, r, 0.5 * (a + b))))
    return pieces


def slot(x0, x1, y, r):
    """A slot along x: two lines and two half circles, counter-clockwise."""
    return [("line", (x0, y - r)), ("arc", (x1, y - r), (x1 + r, y)), ("line", (x1, y + r)),
            ("arc", (x0, y + r), (x0 - r, y))]


def slot_across(x, y0, y1, r):
    """A slot along y: two lines and two half circles, counter-clockwise."""
    return [("line", (x + r, y0)), ("arc", (x + r, y1), (x, y1 + r)), ("line", (x - r, y1)),
            ("arc", (x - r, y0), (x, y0 - r))]


def rounded(x0, y0, x1, y1, r):
    """A rectangle with corners rounded by r: four lines and four quarter arcs, counter-clockwise."""
    return [("line", (x0 + r, y0)), ("arc", (x1 - r, y0), at(x1 - r, y0 + r, r, -45)),
            ("line", (x1, y0 + r)), ("arc", (x1, y1 - r), at(x1 - r, y1 - r, r, 45)),
            ("line", (x1 - r, y1)), ("arc", (x0 + r, y1), at(x0 + r, y1 - r, r, 135)),
            ("line", (x0, y1 - r)), ("arc", (x0, y0 + r), at(x0 + r, y0 + r, r, 225))]


def ellipse(cx, cy, a, b):
    return [("ellipse", (cx, cy), a, b)]


L_OUTER = [(0, 0), (60, 0), (60, 20), (20, 20), (20, 50), (0, 50)]

SHELLS = [("annulus", circle(30, 30, 20), [circle(30, 30, 10)]),
          ("annulus of halves", arcs(30, 30, 20, [0, 180]), [arcs(30, 30, 10, [0, 180])]),
          ("turned seam", circle(30, 30, 20, 0.0), [circle(30, 30, 10, 40.0)]),
          ("thin washer", circle(30, 30, 20), [circle(30, 30, 17)]),
          ("eccentric", circle(30, 30, 20), [circle(34, 30, 8)]),
          ("very eccentric", circle(30, 30, 20), [circle(41.8, 30, 8)]),
          ("tube with radii", rounded(0, 0, 60, 40, 6), [rounded(2, 2, 58, 38, 4)]),
          ("thin tube", rectangle(0, 0, 60, 40), [rectangle(2, 2, 58, 38)]),
          ("thick tube", rectangle(0, 0, 60, 40), [rectangle(10, 10, 50, 30)]),
          ("plate large bore", rectangle(0, 0, 60, 60), [circle(30, 30, 28)]),
          ("plate small bore", rectangle(0, 0, 60, 60), [circle(30, 30, 8)]),
          ("elliptic ring", ellipse(30, 30, 30, 20), [ellipse(30, 30, 24, 14)]),
          ("L with small hole", polygon(L_OUTER), [circle(10, 40, 3)]),
          ("two holes", rectangle(0, 0, 80, 40), [circle(20, 20, 8), circle(60, 20, 8)]),
          ("small diamond", rectangle(0, 0, 60, 60), [polygon([(30, 26), (34, 30), (30, 34), (26, 30)])])]
start()
for i, (name, outer, holes) in enumerate(SHELLS):
    face(outer, holes, SPACING * i)
    print("shell", name, "at x", SPACING * i)
finish("ring_faces.brep")

SOLIDS = [("pipe", circle(30, 30, 20), [circle(30, 30, 12)]),
          ("thin pipe", circle(30, 30, 20), [circle(30, 30, 18)]),
          ("tube with radii", rounded(0, 0, 60, 40, 6), [rounded(2, 2, 58, 38, 4)]),
          ("flange", rectangle(0, 0, 60, 60), [circle(30, 30, 28)])]
start()
for i, (name, outer, holes) in enumerate(SOLIDS):
    extrude(face(outer, holes, SPACING * i), 30.0)
    print("solid", name, "at x", SPACING * i)
finish("ring_sweep.brep")
print("TEST PASSED")
