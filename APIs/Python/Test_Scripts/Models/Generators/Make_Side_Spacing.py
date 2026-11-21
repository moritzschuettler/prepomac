#PrePoMax Python Script
# Writes side_spacing.brep for Test_Model_Meshing_Side_Spacing.py through GmshCommon (OCC kernel): the shell parts
# the header of that test describes, side by side along x. Two are the boundary shells of gmsh's examples
# boolean/fillet3.geo and boolean/twist.geo, read from the gmsh SDK in ThirdParty.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Side_Spacing.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
import tempfile
import clr
clr.AddReference('GmshCommon')
from GmshCommon import Gmsh
from System import Array, Int32, Double, Tuple

OUT = os.environ.get("PMX_MODEL_DIR") or os.path.dirname(os.path.dirname(os.path.abspath(sys.argv[0])))

Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("side_spacing")
occ = Gmsh.Model.OCC
DimTag = Tuple[Int32, Int32]


def plates(x, outlines, z=0.0, arcs=None, holes=None):
    points = {}
    edges = {}
    for face_index, outline in enumerate(outlines):
        corners = []
        for u, v in outline:
            if (u, v) not in points:
                points[u, v] = occ.AddPoint(float(x + u), float(v), z)
            corners.append(points[u, v])
        curves = []
        for i, a in enumerate(corners):
            b = corners[(i + 1) % len(corners)]
            if (b, a) in edges:
                curves.append(-edges[b, a])
            else:
                edge = (outline[i], outline[(i + 1) % len(corners)])
                if arcs and edge in arcs:
                    u, v = arcs[edge]
                    mid = occ.AddPoint(float(x + u), float(v), z)
                    edges[a, b] = occ.AddCircleArc(a, mid, b, True)
                else:
                    edges[a, b] = occ.AddLine(a, b)
                curves.append(edges[a, b])
        loops = [occ.AddCurveLoop(Array[Int32](curves))]
        if holes and face_index in holes:
            loops.extend(holes[face_index]())
        surface = occ.AddPlaneSurface(Array[Int32](loops))
    return surface


def plate(x, height, bottom, top):
    plates(x, [[(0, 0)] + [(u, 0) for u in bottom] + [(100, 0), (100, height)] +
               [(u, height) for u in top] + [(0, height)]])


def strip(x, top):
    plates(x, [[(0, 0), (30, 0), (100, 0), (100, 40), (0, 40)],
               [(0, 40), (100, 40), (100, 80)] + [(u, 80) for u in top] + [(0, 80)]])


def cylinder():
    def point(angle):
        return occ.AddPoint(600.0 + 50.0 * math.cos(angle), 50.0 * math.sin(angle), 0.0)

    start, end = math.radians(20.0), math.radians(140.0)
    center = occ.AddPoint(600.0, 0.0, 0.0)
    arc = occ.AddCircleArc(point(start), center, point(end), True)
    extruded = occ.Extrude(Array[DimTag]([DimTag(1, arc)]), 0.0, 0.0, 40.0, None, Array[Int32]([]),
                           Array[Double]([]), False)
    face = [dt for dt in extruded if dt.Item1 == 2][0]
    split = DimTag(0, point(start + 0.3 * (end - start)))
    occ.Fragment(Array[DimTag]([face]), Array[DimTag]([split]), None, None, -1, True, True)


def reversed_plate(x, z):
    # The two-split plate as the far end, in z = 0, of a solid extruded from z upwards or downwards, of which only it
    # is kept: the one extruded downwards is a reversed face, whose normal runs against its parameters.
    before = set(t.Item2 for t in occ.GetEntities(2))
    base = plates(x, [[(0, 0), (30, 0), (100, 0), (100, 40), (60, 40), (0, 40)]], z)
    occ.Extrude(Array[DimTag]([DimTag(2, base)]), 0.0, 0.0, -z, None, Array[Int32]([]), Array[Double]([]), False)
    occ.Synchronize()
    occ.Remove(Array[DimTag]([t for t in occ.GetEntities(3)]), False)
    faces = [t.Item2 for t in occ.GetEntities(2) if t.Item2 not in before]
    drop = [tag for tag in faces if abs(occ.GetCenterOfMass(2, tag)[2]) > 1e-6 or
            abs(occ.GetMass(2, tag, 0.0) - 4000.0) > 1e-6]
    occ.Remove(Array[DimTag]([DimTag(2, tag) for tag in drop]), True)


plate(0, 40, [30], [])
plate(150, 40, [30], [60])
strip(300, [])
strip(450, [60])
cylinder()
plate(750, 12, [30], [60])
plate(900, 40, [], [])
plates(1050, [[(0, 0), (30, 0), (100, 0), (90, 40), (10, 40)]])
plates(1200, [[(0, 0), (30, 0), (100, 0), (100, 40), (0, 40)],
              [(0, 40), (100, 40), (100, 66), (47, 91), (0, 65)]])
plates(1350, [[(0, 0), (30, 0), (200, 0), (200, 40), (170, 40), (0, 40)]])
reversed_plate(1500, -20.0)
reversed_plate(1650, 20.0)


def sector(x, height):
    def point(r, angle, z):
        return occ.AddPoint(x + r * math.cos(angle), r * math.sin(angle), z)

    if height:
        meridian = occ.AddLine(point(40, 0, 0), point(60, 0, height))
        entities = occ.Revolve(Array[DimTag]([DimTag(1, meridian)]), x, 0.0, 0.0, 0.0, 0.0, 1.0, math.pi / 2,
                               None, Array[Int32]([]), Array[Double]([]), False)
        face = next(dt for dt in entities if dt.Item1 == 2)
        split = DimTag(0, point(40, math.pi / 6, 0))
        occ.Fragment(Array[DimTag]([face]), Array[DimTag]([split]), None, None, -1, True, True)
    else:
        center = occ.AddPoint(x, 0.0, 0.0)
        inner = [point(40, angle, 0) for angle in (0, math.pi / 6, math.pi / 2)]
        outer = [point(60, angle, 0) for angle in (0, math.pi / 2)]
        curves = [occ.AddLine(inner[0], outer[0]), occ.AddCircleArc(outer[0], center, outer[1], True),
                  occ.AddLine(outer[1], inner[2]), occ.AddCircleArc(inner[2], center, inner[1], True),
                  occ.AddCircleArc(inner[1], center, inner[0], True)]
        occ.AddPlaneSurface(Array[Int32]([occ.AddCurveLoop(Array[Int32](curves))]))


def hole_plate(x):
    loops = []
    for path in ([(0, 0), (60, 0), (60, 40), (0, 40)], [(15, 10), (45, 10), (45, 30), (15, 30)]):
        corners = [occ.AddPoint(float(x + u), float(v), 0.0) for u, v in path]
        edges = [occ.AddLine(corner, corners[(i + 1) % len(corners)]) for i, corner in enumerate(corners)]
        loops.append(occ.AddCurveLoop(Array[Int32](edges)))
    occ.AddPlaneSurface(Array[Int32](loops))


plates(1800, [[(0, 0), (30, 0), (100, 0), (97.5, 40), (2.5, 40)]])
sector(1950, 0)
sector(2100, 40.0)
plates(2250, [[(0, 0), (28, 0), (45, 13), (55, 13), (72, 0), (100, 0), (100, 55), (0, 55)]])
l_shape = [(0, 0), (100, 0), (100, 40), (30, 40), (30, 80), (0, 80)]
plates(2400, [l_shape])
plates(2550, [[(0, 0), (100, 0), (100, 80), (70, 80), (70, 40), (30, 40), (30, 80), (0, 80)]])
plates(2700, [l_shape, [(0, -40), (100, -40), (100, 0), (0, 0)]])
plates(2850, [l_shape[:-1] + [(5, 80), (0, 75)]], arcs={((5, 80), (0, 75)): (5, 75)})
hole_plate(3000)
plates(3150, [[(0, 0), (30, 0), (100, 0), (97.5, 60), (2.5, 60)]])
plates(3300, [[(0, 0), (30, 0), (100, 0), (97.5, 100), (2.5, 100)],
              [(2.5, 100), (97.5, 100), (97.5, 195), (2.5, 195)],
              [(2.5, 195), (97.5, 195), (50, 197)]])
plates(3450, [[(0, 0), (30, 0), (100, 0), (97.5, 40), (2.5, 40)],
              [(30, 0), (0, 0), (15, -2)],
              [(100, 0), (30, 0), (65, -2)],
              [(2.5, 40), (97.5, 40), (50, 42)]])

def rectangular_frame(x, width, height, wall):
    bounds = [(0, 0, width, height), (wall, wall, width - wall, height - wall)]
    loops = []
    for x0, y0, x1, y1 in bounds:
        corners = [occ.AddPoint(float(x + u), float(v), 0.0)
                   for u, v in ((x0, y0), (x1, y0), (x1, y1), (x0, y1))]
        edges = [occ.AddLine(a, b) for a, b in zip(corners, corners[1:] + corners[:1])]
        loops.append(occ.AddCurveLoop(Array[Int32](edges)))
    occ.AddPlaneSurface(Array[Int32](loops))


rectangular_frame(3600, 644 / 11, 1149 / 11, 62 / 11)
rectangular_frame(3750, 40 + 10 / 3, 80 + 10 / 3, 5 / 3)
plate(3900, 60, [30], [33])
plates(4050, [[(0, 0), (30, 0), (100, 0), (97.5, 60), (2.5, 60)],
              [(2.5, 60), (97.5, 60), (127, 150), (50, 206), (-27, 150)]])


def add_example_shell(name, x):
    script_dir = os.path.dirname(os.path.abspath(sys.argv[0]))
    root = os.path.abspath(os.path.join(script_dir, "..", "..", "..", "..", ".."))
    examples = os.path.join(root, "ThirdParty", "gmsh-4.13.0-Windows64-sdk", "share", "doc", "gmsh", "examples")
    descriptor, shell_file = tempfile.mkstemp(suffix=".brep", dir=OUT)
    os.close(descriptor)
    try:
        Gmsh.Model.Add(name)
        Gmsh.Merge(os.path.join(examples, "boolean", name + ".geo"))
        occ.Synchronize()
        occ.Remove(Array[DimTag](occ.GetEntities(3)), False)
        occ.Synchronize()
        Gmsh.Write(shell_file)
        Gmsh.Model.SetCurrent("side_spacing")
        imported = occ.ImportShapes(shell_file, None, True, "")
        occ.Translate(Array[DimTag](imported), float(x), 0.0, 0.0)
        if name == "fillet3":
            point = DimTag(0, occ.AddPoint(x - 0.2, -0.05, -0.3))
            occ.Fragment(Array[DimTag](imported), Array[DimTag]([point]), None, None, -1, True, True)
    finally:
        os.remove(shell_file)


add_example_shell("fillet3", 4200)
add_example_shell("twist", 4350)


def round_loop(x, y, radius):
    point = occ.AddPoint(x + radius, y, 0.0)
    entities = occ.Revolve(Array[DimTag]([DimTag(0, point)]), x, y, 0.0, 0.0, 0.0, 1.0, 2 * math.pi,
                           None, Array[Int32]([]), Array[Double]([]), False)
    return occ.AddCurveLoop(Array[Int32]([tag.Item2 for tag in entities if tag.Item1 == 1]))


def box_loop(x0, y0, x1, y1):
    points = [occ.AddPoint(x, y, 0.0) for x, y in ((x0, y0), (x1, y0), (x1, y1), (x0, y1))]
    return occ.AddCurveLoop(Array[Int32]([occ.AddLine(a, b) for a, b in zip(points, points[1:] + points[:1])]))


plates(4500, [[(0, 0), (30, 0), (100, 0), (100, 40), (0, 40)],
              [(0, 40), (100, 40), (100, 100), (0, 100)]],
       holes={1: lambda: [round_loop(4550.0, 70.0, 12.0)]})
plates(4650, [[(0, 0), (100, 0), (100, 80), (0, 80)]],
       holes={0: lambda: [round_loop(4675.0, 40.0, 10.0), box_loop(4703.0, 23.0, 4728.0, 57.0)]})
occ.Synchronize()
Gmsh.Write(os.path.join(OUT, "side_spacing.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
