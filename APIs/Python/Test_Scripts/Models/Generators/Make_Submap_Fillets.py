#PrePoMax Python Script
# Writes submap_fillets.brep (shell faces side by side, one part each) and submap_fillets_sweep.brep (extruded solids)
# for Test_Model_Meshing_Submap_Fillets.py, through GmshCommon (OCC kernel).
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Submap_Fillets.py -g No -x Yes -w <work folder>
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


def outline(points, dx, y0=0.0):
    """A plane surface bounded by the points (x, y), (x, y, cx, cy) or (x, y, mx, my, "through"): the edge from a
    point of four values to the next is an arc about (cx, cy), from one of five an arc through (mx, my), every other
    edge a line."""
    ids = [OCC.AddPoint(float(p[0]) + dx, float(p[1]) + y0, 0.0) for p in points]
    centres = []
    curves = []
    for i, p in enumerate(points):
        j = (i + 1) % len(points)
        if len(p) >= 4:
            centre = OCC.AddPoint(float(p[2]) + dx, float(p[3]) + y0, 0.0)
            centres.append(centre)
            curves.append(OCC.AddCircleArc(ids[i], centre, ids[j], len(p) == 4))
        else:
            curves.append(OCC.AddLine(ids[i], ids[j]))
    surface = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))
    if centres: OCC.Remove(dimtags(0, centres), False)
    return surface


def out_tags(result):
    return result[1] if isinstance(result, tuple) else result


def extrude(surface, height):
    OCC.Extrude(dimtags(2, [surface]), 0.0, 0.0, height, None, Array[Int32]([]), Array[Double]([]), False)


R = 5.0
# The L of the submapping tests: (0, 0), (60, 0), (60, 20), (20, 20), (20, 50), (0, 50)
FILLETED_L = [(0, 0), (60, 0), (60, 20), (20 + R, 20, 20 + R, 20 + R), (20, 20 + R), (20, 50), (0, 50)]
CHAMFERED_L = [(10, 0), (60, 0), (60, 20), (20, 20), (20, 50), (0, 50), (0, 10)]
BOTH_L = [(10, 0), (60, 0), (60, 20), (20 + R, 20, 20 + R, 20 + R), (20, 20 + R), (20, 50), (0, 50), (0, 10)]
# The U: outer 60 x 50, notch 20 wide and 30 deep, both re-entrant corners rounded
FILLETED_U = [(0, 0), (60, 0), (60, 50), (40, 50), (40, 20 + R, 40 - R, 20 + R), (40 - R, 20),
              (20 + R, 20, 20 + R, 20 + R), (20, 20 + R), (20, 50), (0, 50)]
ONE_ROUND = [(0, 0), (60 - 10, 0, 60 - 10, 10), (60, 10), (60, 40), (0, 40)]
ROUNDED = [(R, 0), (60 - R, 0, 60 - R, R), (60, R), (60, 40 - R, 60 - R, 40 - R), (60 - R, 40), (R, 40, R, 40 - R),
           (0, 40 - R), (0, R, R, R)]
HOUSE = [(0, 0), (40, 0), (40, 30), (20, 50), (0, 30)]
SLOT = [(10, 0), (50, 0, 60, 10, "through"), (50, 20), (10, 20, 0, 10, "through")]
# An L whose outer corner at the origin is rounded by a radius of 15
ROUND_L = [(15, 0), (60, 0), (60, 20), (20, 20), (20, 50), (0, 50), (0, 15, 15, 15)]
# An I section 60 wide and 80 high, flanges 10 and web 10 thick, the four roots of the web rounded
I_SECTION = [(0, 0), (60, 0), (60, 10), (35 + R, 10, 35 + R, 10 + R), (35, 10 + R), (35, 70 - R, 35 + R, 70 - R),
             (35 + R, 70), (60, 70), (60, 80), (0, 80), (0, 70), (25 - R, 70, 25 - R, 70 - R), (25, 70 - R),
             (25, 10 + R, 25 - R, 10 + R), (25 - R, 10), (0, 10)]

SHELLS = [("filleted L", FILLETED_L), ("chamfered L", CHAMFERED_L), ("filleted chamfered L", BOTH_L),
          ("filleted U", FILLETED_U), ("one round", ONE_ROUND), ("rounded rectangle", ROUNDED), ("house", HOUSE),
          ("slot", SLOT), ("round L", ROUND_L)]
start()
for i, (name, points) in enumerate(SHELLS):
    outline(points, SPACING * i)
    print("shell", name, "at x", SPACING * i)
# The filleted L wrapped onto the wall of a cylinder of radius 50 about the z axis: the profile in the plane x = 0,
# its y measured from 5 and its z the y of the profile, cut by the prism it sweeps along x
dx = SPACING * len(SHELLS)
centre = OCC.AddPoint(dx, 0.0, 0.0)
start_point = OCC.AddPoint(dx + 50.0, 0.0, 0.0)
end_point = OCC.AddPoint(dx, 50.0, 0.0)
arc = OCC.AddCircleArc(start_point, centre, end_point, True)
OCC.Remove(dimtags(0, [centre]), False)
wall = out_tags(OCC.Extrude(dimtags(1, [arc]), 0.0, 0.0, 60.0, None, Array[Int32]([]), Array[Double]([]), False))
wall_surface = [t.Item2 for t in wall if t.Item1 == 2][0]
ids = []
curves = []
cyl_points = [tuple(5.0 + 0.6 * c for c in p) for p in FILLETED_L]
point_ids = [OCC.AddPoint(dx - 10.0, p[0], p[1]) for p in cyl_points]
centres = []
for i, p in enumerate(cyl_points):
    j = (i + 1) % len(cyl_points)
    if len(p) == 4:
        c = OCC.AddPoint(dx - 10.0, p[2], p[3])
        centres.append(c)
        curves.append(OCC.AddCircleArc(point_ids[i], c, point_ids[j], True))
    else:
        curves.append(OCC.AddLine(point_ids[i], point_ids[j]))
profile_surface = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))
OCC.Remove(dimtags(0, centres), False)
prism = out_tags(OCC.Extrude(dimtags(2, [profile_surface]), 80.0, 0.0, 0.0, None, Array[Int32]([]),
                             Array[Double]([]), False))
prism_volume = [t.Item2 for t in prism if t.Item1 == 3][0]
OCC.Intersect(dimtags(2, [wall_surface]), dimtags(3, [prism_volume]), None, None, -1, True, True)
print("cylinder L at x", dx)
finish("submap_fillets.brep")

SOLIDS = [("filleted L", FILLETED_L), ("chamfered L", CHAMFERED_L), ("I section", I_SECTION), ("house", HOUSE)]
start()
for i, (name, points) in enumerate(SOLIDS):
    extrude(outline(points, SPACING * i), 30.0)
    print("solid", name, "at x", SPACING * i)
finish("submap_fillets_sweep.brep")
print("TEST PASSED")
