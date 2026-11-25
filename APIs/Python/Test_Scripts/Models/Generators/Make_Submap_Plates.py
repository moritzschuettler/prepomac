#PrePoMax Python Script
# Generates submap_plates.brep, submap_folded.brep and submap_curved.brep through GmshCommon (OCC kernel).
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Submap_Plates.py -g No -x Yes -w <work folder>
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


def polygon(points, dx=0.0, dy=0.0):
    ids = [OCC.AddPoint(x + dx, y + dy, 0.0) for x, y in points]
    lines = [OCC.AddLine(ids[i], ids[(i + 1) % len(ids)]) for i in range(len(ids))]
    return lines


def plane(lines, holes=()):
    loops = [OCC.AddCurveLoop(Array[Int32](lines))] + [OCC.AddCurveLoop(Array[Int32](h)) for h in holes]
    return OCC.AddPlaneSurface(Array[Int32](loops))


def start():
    Gmsh.Initialize()
    Gmsh.Option.SetNumber("General.Terminal", 0)
    Gmsh.Model.Add("m")


def finish(name):
    OCC.Synchronize()
    Gmsh.Write(os.path.join(OUT, name))
    Gmsh.FinalizeAll()
    print("written", name)


SHAPES = [
    ("L", [(0, 0), (60, 0), (60, 20), (20, 20), (20, 50), (0, 50)]),
    ("T", [(20, 0), (40, 0), (40, 30), (60, 30), (60, 45), (0, 45), (0, 30), (20, 30)]),
    ("U", [(0, 0), (60, 0), (60, 50), (45, 50), (45, 15), (15, 15), (15, 50), (0, 50)]),
    ("plus", [(20, 0), (40, 0), (40, 20), (60, 20), (60, 40), (40, 40), (40, 60), (20, 60), (20, 40), (0, 40),
              (0, 20), (20, 20)]),
    ("stairs", [(0, 0), (60, 0), (60, 20), (40, 20), (40, 40), (20, 40), (20, 60), (0, 60)]),
    ("rectangle", [(0, 0), (60, 0), (60, 40), (0, 40)]),
]

# Plates: every shape apart from the others in z = 0, 100 mm apart along x, and a plate with a round hole
start()
for i, (name, points) in enumerate(SHAPES):
    plane(polygon(points, dx=100.0 * i))
outer = polygon([(0, 0), (60, 0), (60, 40), (0, 40)], dx=600.0)
c = OCC.AddPoint(630.0, 20.0, 0.0)
p1 = OCC.AddPoint(638.0, 20.0, 0.0)
p2 = OCC.AddPoint(622.0, 20.0, 0.0)
hole = [OCC.AddCircleArc(p1, c, p2, True), OCC.AddCircleArc(p2, c, p1, True)]
plane(outer, [hole])
finish("submap_plates.brep")

# Folded sheet: an L-shaped base in z = 0 with a flange of 20 mm up along its edge y = 0 and one along its edge x = 0;
# the three faces share their edges
start()
P = {}
for key, xyz in {"a": (0, 0, 0), "b": (60, 0, 0), "c": (60, 20, 0), "d": (20, 20, 0), "e": (20, 50, 0),
                 "f": (0, 50, 0), "g": (0, 0, 20), "h": (60, 0, 20), "i": (0, 50, 20)}.items():
    P[key] = OCC.AddPoint(float(xyz[0]), float(xyz[1]), float(xyz[2]))
L = {}
for key in ["ab", "bc", "cd", "de", "ef", "fa", "bh", "hg", "ga", "fi", "ig"]:
    L[key] = OCC.AddLine(P[key[0]], P[key[1]])
plane([L["ab"], L["bc"], L["cd"], L["de"], L["ef"], L["fa"]])
plane([L["ab"], L["bh"], L["hg"], L["ga"]])
plane([L["fa"], L["ga"], L["ig"], L["fi"]])
OCC.Synchronize()
surfaces = OCC.GetEntities(2)
OCC.Fragment(surfaces, Array[Tuple[Int32, Int32]]([]), None, None, True, True)
finish("submap_folded.brep")

# Curved: an L cut from the wall of a cylinder of radius 50 - a quarter of it 60 mm high, without the corner from 45 to
# 90 degrees and from 30 to 60 mm
start()
R = 50.0
centre = OCC.AddPoint(0.0, 0.0, 0.0)
q0 = OCC.AddPoint(R, 0.0, 0.0)
q1 = OCC.AddPoint(0.0, R, 0.0)
arc = OCC.AddCircleArc(q0, centre, q1, True)
extruded = OCC.Extrude(dimtags(1, [arc]), 0.0, 0.0, 60.0, None, Array[Int32]([]), Array[Double]([]), False)
wall = [t.Item2 for t in extruded if t.Item1 == 2]
s = math.sqrt(0.5) * R
m1 = OCC.AddPoint(s, s, 30.0)
m2 = OCC.AddPoint(s, s, 60.0)
m3 = OCC.AddPoint(0.0, R, 30.0)
cz = OCC.AddPoint(0.0, 0.0, 30.0)
cut_line = OCC.AddLine(m1, m2)
cut_arc = OCC.AddCircleArc(m1, cz, m3, True)
OCC.Remove(dimtags(0, [centre, cz]), False)
out = OCC.Fragment(dimtags(2, wall), dimtags(1, [cut_line, cut_arc]), None, None, True, True)
OCC.Synchronize()
faces = [t.Item2 for t in OCC.GetEntities(2)]
corner = None
for f in faces:
    x, y, z = OCC.GetCenterOfMass(2, f, 0.0, 0.0, 0.0)
    print("face", f, x, y, z)
    if z > 30.0 and y > x:
        corner = f
OCC.Remove(dimtags(2, [corner]), True)
finish("submap_curved.brep")
print("TEST PASSED")
