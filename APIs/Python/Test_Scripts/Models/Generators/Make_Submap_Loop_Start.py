#PrePoMax Python Script
# Writes submap_loop_start.brep for Test_Model_Meshing_Submap_Loop_Start.py through GmshCommon (OCC kernel): planar
# shapes in the plane y = 0, each made five times, one part each, its curves listed from another start or the other way
# round: shape s, variant v at x = 150 (5 s + v). Variants: 0, 1 and 2 start the loop at its vertex 0, 1 and 2 and run
# it forward, 3 and 4 run it backward from vertex 0 and from vertex 1. Every curve is made in the direction the loop
# runs it, so the loop of the face starts and turns as listed. The helpers of Make_Cap_Faces.py beside this
# script are used.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Submap_Loop_Start.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
SRC = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Make_Cap_Faces.py")
source = open(SRC).read()
exec(source[:source.index("Gmsh.Initialize()")])
VARIANTS = [(0, False), (1, False), (2, False), (0, True), (1, True)]


def half_disc_one_fillet():
    """The half disc of r 20 whose corner at x = 34.1 is rounded by a fillet of r 5 (lens_planar.brep)."""
    cx = 20.0
    xc = cx + math.sqrt(200.0)
    on_arc = (cx + 20.0 * (xc - cx) / 15.0, 20.0 * 5.0 / 15.0)
    start = math.atan2(5.0, xc - cx)
    middle = 0.5 * (start + math.pi)
    return [((0.0, 0.0), None), ((xc, 0.0), (xc + 4.082, 5.0 - 2.887)),
            (on_arc, (cx + 20.0 * math.cos(middle), 20.0 * math.sin(middle)))]


def l_inner_fillet():
    """An L of legs 40 and 15 wide whose inner corner is rounded by a fillet of r 5."""
    c = 5.0 * (1.0 - math.sqrt(0.5))
    return [((0.0, 0.0), None), ((40.0, 0.0), None), ((40.0, 15.0), None), ((20.0, 15.0), (15.0 + c, 15.0 + c)),
            ((15.0, 20.0), None), ((15.0, 40.0), None), ((0.0, 40.0), None)]


def rounded_rectangle():
    """The rectangle 60 x 30 with its four corners rounded by r 5."""
    r = 5.0
    c = r * (1.0 - math.sqrt(0.5))
    return [((r, 0.0), None), ((60.0 - r, 0.0), (60.0 - c, c)), ((60.0, r), None),
            ((60.0, 30.0 - r), (60.0 - c, 30.0 - c)), ((60.0 - r, 30.0), None), ((r, 30.0), (c, 30.0 - c)),
            ((0.0, 30.0 - r), None), ((0.0, r), (c, c))]


def l_outer_fillet():
    """An L of legs 40 and 15 wide whose outer corner is rounded by a fillet of r 10."""
    r = 10.0
    c = r * (1.0 - math.sqrt(0.5))
    return [((r, 0.0), None), ((40.0, 0.0), None), ((40.0, 15.0), None), ((15.0, 15.0), None), ((15.0, 40.0), None),
            ((0.0, 40.0), None), ((0.0, r), (c, c))]


def tombstone():
    """The rectangle 20 x 60 closed by a half circle."""
    return [((0.0, 0.0), None), ((20.0, 0.0), None), ((20.0, 60.0), (10.0, 70.0)), ((0.0, 60.0), None)]


def chamfered_rectangle():
    """The rectangle 50 x 30 with two adjacent corners cut off by chamfers of 8."""
    return [((0.0, 0.0), None), ((42.0, 0.0), None), ((50.0, 8.0), None), ((50.0, 22.0), None), ((42.0, 30.0), None),
            ((0.0, 30.0), None)]


SHAPES = [("half disc one fillet", half_disc_one_fillet), ("L inner fillet", l_inner_fillet),
          ("rounded rectangle", rounded_rectangle), ("L outer fillet", l_outer_fillet), ("tombstone", tombstone),
          ("chamfered rectangle", chamfered_rectangle)]


def make(segments, dx, start, backward):
    """The face of the loop of segments, each its start vertex and the middle of its arc or None for a line, made from
    the vertex start of the loop, backward the other way round."""
    n = len(segments)
    points = [point(dx + x, z) for (x, z), _ in segments]
    curves = []
    for k in range(n):
        if not backward:
            i = (start + k) % n
            a, b, middle = i, (i + 1) % n, segments[i][1]
        else:
            i = (start - k) % n
            a, b, middle = i, (i - 1) % n, segments[(i - 1) % n][1]
        if middle is None: curves.append(OCC.AddLine(points[a], points[b]))
        else: curves.append(OCC.AddCircleArc(points[a], point(dx + middle[0], middle[1]), points[b], False))
    OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))


Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("loop_start")
for s, (name, shape) in enumerate(SHAPES):
    for v, (start, backward) in enumerate(VARIANTS):
        make(shape(), SPACING * (len(VARIANTS) * s + v), start, backward)
        OCC.Synchronize()
        print("shell", name, "variant", v, "at x", SPACING * (len(VARIANTS) * s + v))
Gmsh.Write(os.path.join(OUT, "submap_loop_start.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
