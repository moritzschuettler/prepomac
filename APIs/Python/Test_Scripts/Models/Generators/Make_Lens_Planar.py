#PrePoMax Python Script
# Writes lens_planar.brep for Test_Model_Meshing_Lens_Planar.py through GmshCommon (OCC kernel), one part per shape at
# x = 150 i: 14 planar faces of two corners (the model of the stage of the thin lens faces), then 16 planar faces of no
# corner or one (the disc probe) and a cap of a sphere cut off by a plane, away from the seam and the poles of the
# sphere. The helpers of Make_Cap_Faces.py beside this script are used.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Lens_Planar.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
SRC = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Make_Cap_Faces.py")
source = open(SRC).read()
exec(source[:source.index("Gmsh.Initialize()")])
ROOT_HALF = 1.0 / math.sqrt(2.0)


def loop_face(curves):
    OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))


def arc3(p0, xm, zm, p1):
    return OCC.AddCircleArc(p0, point(xm, zm), p1, False)


def bezier2(p0, xm, zm, p1):
    return OCC.AddBSpline(Array[Int32]([p0, point(xm, zm), p1]), -1, 2, Array[Double]([1.0, 1.0, 1.0]),
                          Array[Double]([0.0, 1.0]), Array[Int32]([3, 3]))


def tombstone(dx, width, straight):
    p0, p1 = point(dx, 0.0), point(dx + width, 0.0)
    p2, p3 = point(dx + width, straight), point(dx, straight)
    loop_face([OCC.AddLine(p0, p1), OCC.AddLine(p1, p2), arc3(p2, dx + width / 2.0, straight + width / 2.0, p3),
               OCC.AddLine(p3, p0)])


def segment(dx, half_chord, sagitta):
    p0, p1 = point(dx, 0.0), point(dx + 2 * half_chord, 0.0)
    loop_face([OCC.AddLine(p0, p1), arc3(p1, dx + half_chord, sagitta, p0)])


def vesica(dx, half_chord, sagitta):
    p0, p1 = point(dx, 0.0), point(dx + 2 * half_chord, 0.0)
    loop_face([arc3(p0, dx + half_chord, -sagitta, p1), arc3(p1, dx + half_chord, sagitta, p0)])


def arch(dx, width, pole_height):
    p0, p1 = point(dx, 0.0), point(dx + width, 0.0)
    loop_face([OCC.AddLine(p0, p1), bezier2(p1, dx + width / 2.0, pole_height, p0)])


def obtuse_triangle(dx):
    p0, p1, p2 = point(dx, 0.0), point(dx + 40.0, 0.0), point(dx + 20.0, 5.36)
    loop_face([OCC.AddLine(p0, p1), OCC.AddLine(p1, p2), OCC.AddLine(p2, p0)])


def moon(dx, inner_sagitta):
    p0, p1 = point(dx, 0.0), point(dx + 40.0, 0.0)
    loop_face([arc3(p0, dx + 20.0, inner_sagitta, p1), arc3(p1, dx + 20.0, 20.0, p0)])


def kinked_half_disc(dx):
    p0, p1, pm = point(dx, 0.0), point(dx + 40.0, 0.0), point(dx + 20.0, -1.75)
    loop_face([OCC.AddLine(p0, pm), OCC.AddLine(pm, p1), arc3(p1, dx + 20.0, 20.0, p0)])


def conic(p0, xc, zc, p1):
    """The quarter of an ellipse from p0 to p1 with the corner of its box at (xc, zc)."""
    return OCC.AddBSpline(Array[Int32]([p0, point(xc, zc), p1]), -1, 2, Array[Double]([1.0, ROOT_HALF, 1.0]),
                          Array[Double]([0.0, 1.0]), Array[Int32]([3, 3]))


def closed_circle(dx, r):
    p = point(dx + 2 * r, r)
    out = OCC.Revolve(dimtags(0, [p]), dx + r, 0.0, r, 0.0, 1.0, 0.0, 2 * math.pi, None, no_ints(), no_doubles(),
                      False)
    loop_face([t.Item2 for t in out if t.Item1 == 1])


def disc_of_arcs(dx, r, angles):
    cx, cz = dx + r, r
    points = [point(cx + r * math.cos(math.radians(a)), cz + r * math.sin(math.radians(a))) for a in angles]
    curves = []
    for i, a in enumerate(angles):
        b = angles[(i + 1) % len(angles)]
        if b <= a:
            b += 360
        m = math.radians(0.5 * (a + b))
        curves.append(arc3(points[i], cx + r * math.cos(m), cz + r * math.sin(m), points[(i + 1) % len(angles)]))
    loop_face(curves)


def ellipse(dx, a, b, a_left=None):
    """The ellipse of the half axes a and b; with a_left its left half has that half axis instead: an egg."""
    left = a if a_left is None else a_left
    cx, cz = dx + left, b
    east, north = point(cx + a, cz), point(cx, cz + b)
    west, south = point(cx - left, cz), point(cx, cz - b)
    loop_face([conic(east, cx + a, cz + b, north), conic(north, cx - left, cz + b, west),
               conic(west, cx - left, cz - b, south), conic(south, cx + a, cz - b, east)])


def stadium(dx, straight, r):
    p0, p1 = point(dx + r, 0.0), point(dx + r + straight, 0.0)
    p2, p3 = point(dx + r + straight, 2 * r), point(dx + r, 2 * r)
    loop_face([OCC.AddLine(p0, p1), arc3(p1, dx + 2 * r + straight, r, p2), OCC.AddLine(p2, p3),
               arc3(p3, dx, r, p0)])


def rounded_polygon(dx, corners, radius):
    """The convex polygon of the given corners, counter-clockwise, each rounded by an arc of the radius."""
    n = len(corners)
    starts, ends, middles = [], [], []
    for i in range(n):
        p, a, b = corners[i], corners[i - 1], corners[(i + 1) % n]
        ua = [a[0] - p[0], a[1] - p[1]]
        ub = [b[0] - p[0], b[1] - p[1]]
        la, lb = math.hypot(ua[0], ua[1]), math.hypot(ub[0], ub[1])
        ua, ub = [ua[0] / la, ua[1] / la], [ub[0] / lb, ub[1] / lb]
        half = 0.5 * math.acos(ua[0] * ub[0] + ua[1] * ub[1])
        t = radius / math.tan(half)
        bis = [ua[0] + ub[0], ua[1] + ub[1]]
        lbis = math.hypot(bis[0], bis[1])
        bis = [bis[0] / lbis, bis[1] / lbis]
        centre_distance = radius / math.sin(half)
        starts.append(point(dx + p[0] + t * ua[0], p[1] + t * ua[1]))
        ends.append(point(dx + p[0] + t * ub[0], p[1] + t * ub[1]))
        middles.append((dx + p[0] + (centre_distance - radius) * bis[0], p[1] + (centre_distance - radius) * bis[1]))
    curves = []
    for i in range(n):
        curves.append(arc3(starts[i], middles[i][0], middles[i][1], ends[i]))
        curves.append(OCC.AddLine(ends[i], starts[(i + 1) % n]))
    loop_face(curves)


def teardrop(dx, r, tip_angle_deg):
    beta = math.radians(tip_angle_deg / 2.0)
    depth = r / math.sin(beta)
    cx, cz = dx + r, depth
    tip = point(cx, 0.0)
    right = point(cx + r * math.cos(beta), cz - r * math.sin(beta))
    left = point(cx - r * math.cos(beta), cz - r * math.sin(beta))
    loop_face([OCC.AddLine(tip, right), arc3(right, cx, cz + r, left), OCC.AddLine(left, tip)])


def half_disc_one_fillet(dx):
    """The half disc of r 20 whose corner at x = dx + 34.1 is rounded by a fillet of r 5. Its curves are made from the
    fillet on, which gives its loop the order and direction in which the submapping claims it, as it did in the disc
    probe (the submap detection of this face depends on where its loop starts)."""
    cx = dx + 20.0
    xc = cx + math.sqrt(200.0)
    corner = point(dx, 0.0)
    on_chord = point(xc, 0.0)
    on_arc = point(cx + 20.0 * (xc - cx) / 15.0, 20.0 * 5.0 / 15.0)
    start = math.atan2(5.0, xc - cx)
    middle = 0.5 * (start + math.pi)
    fillet = arc3(on_chord, xc + 4.082, 5.0 - 2.887, on_arc)
    rim = arc3(on_arc, cx + 20.0 * math.cos(middle), 20.0 * math.sin(middle), corner)
    loop_face([fillet, rim, OCC.AddLine(corner, on_chord)])


def sphere_cap(dx):
    """The cap of the sphere of r 20 about (dx + 20, 0, 20) beyond the plane x = dx + 6: its seam lies at the azimuth 0
    (x > dx + 20) and its poles at z 0 and 40, so the cap is bounded by one closed circle of r 14.3 and nothing else."""
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [arc(cx, 20.0, 20.0, -90.0, 90.0)], cx)[0]
    corners = [OCC.AddPoint(dx + 6.0, y, z) for y, z in [(-30.0, -10.0), (30.0, -10.0), (30.0, 50.0), (-30.0, 50.0)]]
    lines = [OCC.AddLine(corners[k], corners[(k + 1) % 4]) for k in range(4)]
    base = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](lines))]))
    tool = OCC.Extrude(dimtags(2, [base]), -20.0, 0.0, 0.0, None, no_ints(), no_doubles(), False)
    box = [t.Item2 for t in tool if t.Item1 == 3][0]
    OCC.Intersect(dimtags(2, [face]), dimtags(3, [box]), None, None, -1, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    OCC.Synchronize()
    new = [tag for tag in surfaces() if tag not in existing]
    print("cap pieces", [(tag, Gmsh.Model.GetType(2, tag), round(OCC.GetMass(2, tag, 0.0), 1)) for tag in new])
    OCC.Remove(dimtags(2, [tag for tag in new if Gmsh.Model.GetType(2, tag) == "Plane"]), True)
    OCC.Synchronize()


TRIANGLE = [(0.0, 0.0), (50.0, 0.0), (25.0, 43.3)]
SQUARE = [(0.0, 0.0), (40.0, 0.0), (40.0, 40.0), (0.0, 40.0)]
Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
SHELLS = [("tombstone 1:1", lambda dx: tombstone(dx, 40.0, 20.0)),
          ("tombstone 1:3", lambda dx: tombstone(dx, 20.0, 60.0)),
          ("tombstone 3:1", lambda dx: tombstone(dx, 60.0, 10.0)),
          ("segment 60", lambda dx: segment(dx, 17.32, 10.0)),
          ("segment 30", lambda dx: segment(dx, 20.0, 5.36)),
          ("vesica 120", lambda dx: vesica(dx, 17.32, 10.0)),
          ("vesica 40", lambda dx: vesica(dx, 20.0, 3.53)),
          ("arch 45", lambda dx: arch(dx, 60.0, 30.0)),
          ("arch tall", lambda dx: arch(dx, 30.0, 80.0)),
          ("obtuse triangle", obtuse_triangle),
          ("small half disc", lambda dx: segment(dx, 3.0, 3.0)),
          ("moon 8", lambda dx: moon(dx, 8.0)),
          ("moon 14", lambda dx: moon(dx, 14.0)),
          ("kinked half disc", kinked_half_disc),
          ("disc", lambda dx: closed_circle(dx, 20.0)),
          ("disc of 3 arcs", lambda dx: disc_of_arcs(dx, 20.0, [20.0, 140.0, 260.0])),
          ("disc of 4 arcs uneven", lambda dx: disc_of_arcs(dx, 20.0, [0.0, 30.0, 170.0, 250.0])),
          ("small disc", lambda dx: closed_circle(dx, 4.0)),
          ("ellipse 2:1", lambda dx: ellipse(dx, 30.0, 15.0)),
          ("ellipse 4:1", lambda dx: ellipse(dx, 40.0, 10.0)),
          ("egg", lambda dx: ellipse(dx, 35.0, 20.0, 20.0)),
          ("stadium 60", lambda dx: stadium(dx, 60.0, 10.0)),
          ("stadium 15", lambda dx: stadium(dx, 15.0, 15.0)),
          ("rounded triangle", lambda dx: rounded_polygon(dx, TRIANGLE, 8.0)),
          ("rounded square", lambda dx: rounded_polygon(dx, SQUARE, 15.0)),
          ("teardrop 30", lambda dx: teardrop(dx, 12.0, 30.0)),
          ("teardrop 60", lambda dx: teardrop(dx, 15.0, 60.0)),
          ("teardrop 90", lambda dx: teardrop(dx, 15.0, 90.0)),
          ("teardrop 120", lambda dx: teardrop(dx, 15.0, 120.0)),
          ("half disc one fillet", half_disc_one_fillet),
          ("sphere cap", sphere_cap)]
for i, (name, make) in enumerate(SHELLS):
    make(SPACING * i)
    OCC.Synchronize()
    print("shell", name, "at x", SPACING * i)
OCC.Synchronize()
for t in OCC.GetEntities(2):
    print("surface", t.Item2, Gmsh.Model.GetType(2, t.Item2), "area", round(OCC.GetMass(2, t.Item2, 0.0), 2))
Gmsh.Write(os.path.join(OUT, "lens_planar.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
