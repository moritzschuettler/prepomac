#PrePoMax Python Script
# Writes quad_dominant_faces.brep (shell faces side by side, one part each) and quad_dominant_sweep.brep (extruded
# solids) for Test_Model_Meshing_Quad_Dominant.py, through GmshCommon (OCC kernel).
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Quad_Dominant_Faces.py -g No -x Yes -w <work folder>
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


def closed_circle(cx, cy, r):
    point = OCC.AddPoint(cx + r, cy, 0.0)
    out = OCC.Revolve(dimtags(0, [point]), cx, cy, 0.0, 0.0, 0.0, 1.0, 2 * math.pi, None, Array[Int32]([]),
                      Array[Double]([]), False)
    return [t.Item2 for t in out if t.Item1 == 1]


def loop(pieces, dx):
    if pieces[0][0] == "circle":
        (cx, cy), r = pieces[0][1], pieces[0][2]
        return OCC.AddCurveLoop(Array[Int32](closed_circle(cx + dx, cy, r))), []
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


def polygon(points):
    return [("line", p) for p in points]


def circle(cx, cy, r):
    return [("circle", (cx, cy), r)]


def rectangle(x0, y0, x1, y1):
    return polygon([(x0, y0), (x1, y0), (x1, y1), (x0, y1)])


def half_disc(cx, cy, r):
    return [("line", (cx - r, cy)), ("arc", (cx + r, cy), (cx, cy + r))]


def house():
    """A house of base 40 and walls of 25 whose roof meets the walls at 117 and 127 degrees inside."""
    right = math.radians(270 - 117)
    left = math.radians(127 - 90)
    # (40, 25) + t (cos right, sin right) = (0, 25) + s (cos left, sin left)
    a = [[math.cos(right), -math.cos(left)], [math.sin(right), -math.sin(left)]]
    b = [-40.0, 0.0]
    det = a[0][0] * a[1][1] - a[0][1] * a[1][0]
    t = (b[0] * a[1][1] - a[0][1] * b[1]) / det
    apex = (40 + t * math.cos(right), 25 + t * math.sin(right))
    return polygon([(0, 0), (40, 0), (40, 25), apex, (0, 25)])


HEPTAGON = [(0, 0), (34, -4), (52, 14), (46, 36), (24, 48), (4, 40), (-8, 18)]
L_THIN = [(0, 0), (30, 0), (30, 2), (2, 2), (2, 30), (0, 30)]
DIAMOND = polygon([(30, 30 - 4), (30 + 4, 30), (30, 30 + 4), (30 - 4, 30)])

PLANE_SHELLS = [("disc", circle(20, 20, 20), []),
                ("half disc", half_disc(20, 0, 20), []),
                ("triangle", polygon([(0, 0), (60, 0), (22, 45)]), []),
                ("house", house(), []),
                ("eccentric annulus", circle(20, 20, 20), [circle(31.8, 20, 8)]),
                ("plate with diamond", rectangle(0, 0, 60, 60), [DIAMOND]),
                ("three holes", rectangle(0, 0, 80, 50), [circle(15, 25, 4), circle(38, 20, 6), circle(62, 28, 9)]),
                ("heptagon", polygon(HEPTAGON), []),
                ("thin L", polygon(L_THIN), [])]


def cylinder_patch_with_hole(dx):
    """A quarter of a cylinder of r 40 about the z axis through (dx, 0), 50 high, with a round hole of r 6 cut through
    it radially at 45 degrees and z 25."""
    OCC.Synchronize()
    existing = set(t.Item2 for t in OCC.GetEntities(2))
    radius = 40.0
    bottom = OCC.AddPoint(dx + radius, 0.0, 0.0)
    top = OCC.AddPoint(dx + radius, 0.0, 50.0)
    line = OCC.AddLine(bottom, top)
    out = OCC.Revolve(dimtags(1, [line]), dx, 0.0, 0.0, 0.0, 0.0, 1.0, 0.5 * math.pi, None, Array[Int32]([]),
                      Array[Double]([]), False)
    patch = [t.Item2 for t in out if t.Item1 == 2][0]
    # The tool: a disc of r 6 normal to the radial direction at 45 degrees, extruded through the wall
    c, s = math.cos(math.radians(45)), math.sin(math.radians(45))
    centre = (dx + 30 * c, 30 * s, 25.0)
    point = OCC.AddPoint(centre[0], centre[1], centre[2] + 6.0)
    circle_out = OCC.Revolve(dimtags(0, [point]), centre[0], centre[1], centre[2], c, s, 0.0, 2 * math.pi, None,
                             Array[Int32]([]), Array[Double]([]), False)
    curves = [t.Item2 for t in circle_out if t.Item1 == 1]
    disc = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))
    tool = OCC.Extrude(dimtags(2, [disc]), 20 * c, 20 * s, 0.0, None, Array[Int32]([]), Array[Double]([]), False)
    volumes = [t.Item2 for t in tool if t.Item1 == 3]
    OCC.Fragment(dimtags(2, [patch]), dimtags(3, volumes), None, None, True, True)
    OCC.Synchronize()
    # Keep the patch with the hole, the largest surface; remove the volume and everything else
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    OCC.Synchronize()
    surfaces = [t.Item2 for t in OCC.GetEntities(2) if t.Item2 not in existing]
    areas = dict((tag, OCC.GetMass(2, tag, 0.0)) for tag in surfaces)
    keep = max(areas, key=lambda tag: areas[tag])
    OCC.Remove(dimtags(2, [tag for tag in surfaces if tag != keep]), True)
    OCC.Synchronize()
    print("cylinder patch with hole: surface", keep, "area", areas[keep])


start()
for i, (name, outer, holes) in enumerate(PLANE_SHELLS):
    face(outer, holes, SPACING * i)
    print("shell", name, "at x", SPACING * i)
cylinder_patch_with_hole(SPACING * len(PLANE_SHELLS))
print("shell cylinder patch at x", SPACING * len(PLANE_SHELLS))
finish("quad_dominant_faces.brep")

SOLIDS = PLANE_SHELLS[:8]
start()
for i, (name, outer, holes) in enumerate(SOLIDS):
    extrude(face(outer, holes, SPACING * i), 20.0)
    print("solid", name, "at x", SPACING * i)
finish("quad_dominant_sweep.brep")
print("TEST PASSED")
