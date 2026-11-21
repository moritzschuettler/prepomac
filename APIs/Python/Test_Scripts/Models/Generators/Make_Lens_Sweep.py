#PrePoMax Python Script
# Writes lens_sweep.brep for Test_Model_Meshing_Lens_Sweep.py through GmshCommon (OCC kernel): prisms extruded along +y
# from plane faces in y = 0 whose source is a lens face, one part per shape at x = 150 i, and two compounds. The helpers
# of Make_Cap_Faces.py beside this script are used.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Lens_Sweep.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
SRC = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Make_Cap_Faces.py")
source = open(SRC).read()
exec(source[:source.index("Gmsh.Initialize()")])
ROOT_HALF = 1.0 / math.sqrt(2.0)
HEIGHT = 40.0


def point_at(x, y, z):
    return OCC.AddPoint(x, y, z)


def loop_face(curves):
    return OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))


def arc3(p0, xm, zm, p1):
    return OCC.AddCircleArc(p0, point(xm, zm), p1, False)


def conic(p0, xc, zc, p1):
    """The quarter of an ellipse from p0 to p1 with the corner of its box at (xc, zc)."""
    return OCC.AddBSpline(Array[Int32]([p0, point(xc, zc), p1]), -1, 2, Array[Double]([1.0, ROOT_HALF, 1.0]),
                          Array[Double]([0.0, 1.0]), Array[Int32]([3, 3]))


def closed_circle_at(cx, y, cz, r):
    p = point_at(cx + r, y, cz)
    out = OCC.Revolve(dimtags(0, [p]), cx, y, cz, 0.0, 1.0, 0.0, 2 * math.pi, None, no_ints(), no_doubles(), False)
    return loop_face([t.Item2 for t in out if t.Item1 == 1])


def disc(dx):
    return closed_circle_at(dx + 20.0, 0.0, 20.0, 20.0)


def ellipse(dx, a, b):
    cx, cz = dx + a, b
    east, north = point(cx + a, cz), point(cx, cz + b)
    west, south = point(cx - a, cz), point(cx, cz - b)
    return loop_face([conic(east, cx + a, cz + b, north), conic(north, cx - a, cz + b, west),
                      conic(west, cx - a, cz - b, south), conic(south, cx + a, cz - b, east)])


def half_disc(dx):
    p0, p1 = point(dx, 0.0), point(dx + 40.0, 0.0)
    return loop_face([OCC.AddLine(p0, p1), arc3(p1, dx + 20.0, 20.0, p0)])


def vesica(dx, half_chord, sagitta):
    p0, p1 = point(dx, 0.0), point(dx + 2 * half_chord, 0.0)
    return loop_face([arc3(p0, dx + half_chord, -sagitta, p1), arc3(p1, dx + half_chord, sagitta, p0)])


def teardrop(dx, r, tip_angle_deg):
    beta = math.radians(tip_angle_deg / 2.0)
    depth = r / math.sin(beta)
    cx, cz = dx + r, depth
    tip = point(cx, 0.0)
    right = point(cx + r * math.cos(beta), cz - r * math.sin(beta))
    left = point(cx - r * math.cos(beta), cz - r * math.sin(beta))
    return loop_face([OCC.AddLine(tip, right), arc3(right, cx, cz + r, left), OCC.AddLine(left, tip)])


def stadium(dx, straight, r):
    p0, p1 = point(dx + r, 0.0), point(dx + r + straight, 0.0)
    p2, p3 = point(dx + r + straight, 2 * r), point(dx + r, 2 * r)
    return loop_face([OCC.AddLine(p0, p1), arc3(p1, dx + 2 * r + straight, r, p2), OCC.AddLine(p2, p3),
                      arc3(p3, dx, r, p0)])


def moon(dx, inner_sagitta):
    p0, p1 = point(dx, 0.0), point(dx + 40.0, 0.0)
    return loop_face([arc3(p0, dx + 20.0, inner_sagitta, p1), arc3(p1, dx + 20.0, 20.0, p0)])


def extrude(face, height=HEIGHT):
    out = OCC.Extrude(dimtags(2, [face]), 0.0, height, 0.0, None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in out if t.Item1 == 3][0]


def prism(make):
    return lambda dx: extrude(make(dx))


def cylinder_stack(dx):
    """Three cylinders of r 15 stacked along y, 15, 20 and 25 high, fragmented into one compound."""
    volumes = []
    y = 0.0
    for height in (15.0, 20.0, 25.0):
        volumes.append(extrude(closed_circle_at(dx + 15.0, y, 15.0, 15.0), height))
        y += height
    OCC.Fragment(dimtags(3, volumes[:1]), dimtags(3, volumes[1:]), None, None, True, True)


def plate_with_cylinder(dx):
    """A plate of 60 x 60 x 30 and a cylinder of r 12 through it about its middle, fragmented into one compound: the
    plate with a bore beside the cylinder that fills it."""
    corners = [point(dx + x, z) for x, z in [(0.0, 0.0), (60.0, 0.0), (60.0, 60.0), (0.0, 60.0)]]
    plate = extrude(loop_face([OCC.AddLine(corners[k], corners[(k + 1) % 4]) for k in range(4)]), 30.0)
    cylinder = extrude(closed_circle_at(dx + 30.0, 0.0, 30.0, 12.0), 30.0)
    OCC.Fragment(dimtags(3, [plate]), dimtags(3, [cylinder]), None, None, True, True)


Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
SOLIDS = [("cylinder", prism(disc)),
          ("elliptic cylinder", prism(lambda dx: ellipse(dx, 30.0, 15.0))),
          ("half cylinder", prism(half_disc)),
          ("lens prism", prism(lambda dx: vesica(dx, 20.0, 8.0))),
          ("teardrop prism", prism(lambda dx: teardrop(dx, 15.0, 60.0))),
          ("stadium prism", prism(lambda dx: stadium(dx, 15.0, 15.0))),
          ("moon prism", prism(lambda dx: moon(dx, 14.0))),
          ("cylinder stack", cylinder_stack),
          ("plate with cylinder", plate_with_cylinder)]
for i, (name, make) in enumerate(SOLIDS):
    make(SPACING * i)
    OCC.Synchronize()
    print("solid", name, "at x", SPACING * i)
OCC.Synchronize()
for t in OCC.GetEntities(3):
    print("volume", t.Item2, "volume", round(OCC.GetMass(3, t.Item2, 0.0), 2))
Gmsh.Write(os.path.join(OUT, "lens_sweep.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
