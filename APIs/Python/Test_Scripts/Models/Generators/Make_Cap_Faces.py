#PrePoMax Python Script
# Writes cap_faces.brep for Test_Model_Meshing_Cap.py: shell faces closed along a seam that ends in a pole, side by side,
# one part each, and parts of several faces, through GmshCommon (OCC kernel). The parts start at x = 150 i.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Cap_Faces.py -g No -x Yes -w <work folder>
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


def no_ints():
    return Array[Int32]([])


def no_doubles():
    return Array[Double]([])


def surfaces():
    OCC.Synchronize()
    return set(t.Item2 for t in OCC.GetEntities(2))


def revolve_z(dim, tags, cx, angle=2 * math.pi):
    out = OCC.Revolve(dimtags(dim, tags), cx, 0.0, 0.0, 0.0, 0.0, 1.0, angle, None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in out if t.Item1 == dim + 1]


def point(x, z):
    return OCC.AddPoint(x, 0.0, z)


def arc(cx, cz, radius, start_deg, end_deg):
    """The arc of the circle about (cx, cz) in the plane y = 0 from the angle start to end, measured from +x towards
    +z."""
    a0, a1 = math.radians(start_deg), math.radians(end_deg)
    am = 0.5 * (a0 + a1)
    return OCC.AddCircleArc(point(cx + radius * math.cos(a0), cz + radius * math.sin(a0)),
                            point(cx + radius * math.cos(am), cz + radius * math.sin(am)),
                            point(cx + radius * math.cos(a1), cz + radius * math.sin(a1)), False)


def line(x0, z0, x1, z1):
    return OCC.AddLine(point(x0, z0), point(x1, z1))


def bspline(poles, weights, degree, knots, multiplicities):
    tags = [point(x, z) for x, z in poles]
    return OCC.AddBSpline(Array[Int32](tags), -1, degree, Array[Double](weights), Array[Double](knots),
                          Array[Int32](multiplicities))


def quarter_ellipse(cx, cz, a, b):
    """From the pole (cx, cz + b) to the equator (cx + a, cz), exact as a rational quadratic B-spline."""
    w = math.sqrt(0.5)
    return bspline([(cx, cz + b), (cx + a, cz + b), (cx + a, cz)], [1.0, w, 1.0], 2, [0.0, 1.0], [3, 3])


def glue(faces):
    """The faces fragmented with each other, so that they share the curves they meet along."""
    OCC.Fragment(dimtags(2, faces[:1]), dimtags(2, faces[1:]), None, None, True, True)
    OCC.Synchronize()


def keep_new(existing, pick):
    """Of the surfaces made since existing, keeps the one pick chooses by tag."""
    new = [tag for tag in surfaces() if tag not in existing]
    keep = pick(new)
    OCC.Remove(dimtags(2, [tag for tag in new if tag != keep]), True)
    OCC.Synchronize()


def hemisphere(dx):
    """r 20 about (dx + 20, 0, 0), the pole at z 20."""
    cx = dx + 20
    revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx)


def shallow_cap(dx):
    """The cap of the sphere of r 40 within 30 degrees of its pole: rim r 20, 5.36 high."""
    cx = dx + 20
    revolve_z(1, [arc(cx, -40.0 * math.cos(math.radians(30)), 40.0, 90.0, 60.0)], cx)


def cone(dx, half_angle_deg, radius):
    """A cone of the base radius at z 0 to its apex on the axis through (dx + 20, 0)."""
    cx = dx + 20
    revolve_z(1, [line(cx + radius, 0.0, cx, radius / math.tan(math.radians(half_angle_deg)))], cx)


def paraboloid(dx):
    """z = 15 (1 - (r / 20)^2), a quadratic B-spline from the pole revolved."""
    cx = dx + 20
    revolve_z(1, [bspline([(cx, 15.0), (cx + 10.0, 15.0), (cx + 20.0, 0.0)], [1.0, 1.0, 1.0], 2, [0.0, 1.0],
                          [3, 3])], cx)


def elliptical_head(dx):
    """A 2:1 elliptical head of r 20 and 10 deep."""
    cx = dx + 20
    revolve_z(1, [quarter_ellipse(cx, 0.0, 20.0, 10.0)], cx)


def sphere(dx):
    """r 20 about (dx + 20, 0, 20), a half circle revolved."""
    cx = dx + 20
    revolve_z(1, [arc(cx, 20.0, 20.0, -90.0, 90.0)], cx)


def spheroid(dx):
    """The prolate spheroid of r 12 and 24 half high about (dx + 20, 0, 24), a half ellipse revolved."""
    cx = dx + 20
    w = math.sqrt(0.5)
    revolve_z(1, [bspline([(cx, 0.0), (cx + 12.0, 0.0), (cx + 12.0, 24.0), (cx + 12.0, 48.0), (cx, 48.0)],
                          [1.0, w, 1.0, w, 1.0], 2, [0.0, 1.0, 2.0], [3, 2, 3])], cx)


def split_dome(dx, angles):
    """The hemisphere of r 20 with its rim split at the angles (degrees from the seam) by points fragmented into it."""
    cx = dx + 20
    face = revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx)[0]
    points = [OCC.AddPoint(cx + 20 * math.cos(math.radians(a)), 20 * math.sin(math.radians(a)), 0.0) for a in angles]
    OCC.Fragment(dimtags(2, [face]), dimtags(0, points), None, None, True, True)
    OCC.Synchronize()


def oblique_cone(dx):
    """The cone of r 20 at z 0 to its apex at z 40 cut by the plane through (dx + 20, 0, 12) tilted by 20 degrees about
    y; the piece with the apex is kept."""
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [line(cx + 20, 0.0, cx, 40.0)], cx)[0]
    slope = math.tan(math.radians(20))
    corners = [OCC.AddPoint(cx + x, y, 12.0 + slope * x) for x, y in [(-60, -60), (60, -60), (60, 60), (-60, 60)]]
    lines = [OCC.AddLine(corners[k], corners[(k + 1) % 4]) for k in range(4)]
    plane = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](lines))]))
    tool = OCC.Extrude(dimtags(2, [plane]), 0.0, 0.0, -100.0, None, no_ints(), no_doubles(), False)
    box = [t.Item2 for t in tool if t.Item1 == 3][0]
    OCC.Fragment(dimtags(2, [face]), dimtags(3, [box]), None, None, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    keep_new(existing, lambda new: max(new, key=lambda tag: OCC.GetMass(2, tag, 0.0)))


def with_hole(dx, make, cz, azimuth_deg, elevation_deg, radius):
    """The face make lays with a hole of the radius cut through it along the direction of the azimuth and elevation
    from the point (dx + 20, 0, cz) on its axis."""
    existing = surfaces()
    make(dx)
    centre = (dx + 20, 0.0, cz)
    az, el = math.radians(azimuth_deg), math.radians(elevation_deg)
    d = [math.cos(el) * math.cos(az), math.cos(el) * math.sin(az), math.sin(el)]
    other = [0.0, 0.0, 1.0] if abs(d[2]) < 0.9 else [1.0, 0.0, 0.0]
    start = [centre[k] + 10 * d[k] for k in range(3)]
    radial = [d[1] * other[2] - d[2] * other[1], d[2] * other[0] - d[0] * other[2], d[0] * other[1] - d[1] * other[0]]
    rl = math.sqrt(sum(c * c for c in radial))
    rim = OCC.AddPoint(start[0] + radius * radial[0] / rl, start[1] + radius * radial[1] / rl,
                       start[2] + radius * radial[2] / rl)
    out = OCC.Revolve(dimtags(0, [rim]), start[0], start[1], start[2], d[0], d[1], d[2], 2 * math.pi, None,
                      no_ints(), no_doubles(), False)
    curves = [t.Item2 for t in out if t.Item1 == 1]
    disc = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))
    tool = OCC.Extrude(dimtags(2, [disc]), 60 * d[0], 60 * d[1], 60 * d[2], None, no_ints(), no_doubles(), False)
    volumes = [t.Item2 for t in tool if t.Item1 == 3]
    faces = [tag for tag in surfaces() if tag not in existing and tag != disc]
    OCC.Fragment(dimtags(2, faces), dimtags(3, volumes), None, None, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    keep_new(existing, lambda new: max(new, key=lambda tag: OCC.GetMass(2, tag, 0.0)))


def pressure_vessel(dx):
    """Two hemispheres of r 20 closing a cylinder of r 20 from z 20 to 60, revolved together: poles at z 0 and 80."""
    cx = dx + 20
    glue(revolve_z(1, [arc(cx, 20.0, 20.0, -90.0, 0.0), line(cx + 20, 20.0, cx + 20, 60.0),
                       arc(cx, 60.0, 20.0, 0.0, 90.0)], cx))


def closed_cone(dx):
    """The cone of r 20 at z 0 to its apex at z 40, closed by the disc of r 20 at z 0."""
    cx = dx + 20
    wall = revolve_z(1, [line(cx + 20, 0.0, cx, 40.0)], cx)
    out = OCC.Revolve(dimtags(0, [OCC.AddPoint(cx + 20, 0.0, 0.0)]), cx, 0.0, 0.0, 0.0, 0.0, 1.0, 2 * math.pi, None,
                      no_ints(), no_doubles(), False)
    circle = [t.Item2 for t in out if t.Item1 == 1][0]
    disc = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32]([circle]))]))
    OCC.Fragment(dimtags(2, wall), dimtags(2, [disc]), None, None, True, True)
    OCC.Synchronize()


def dome_on_frustum(dx):
    """The cone frustum from r 10 at z 0 to r 20 at z 20 and the hemisphere of r 20 on it, revolved together."""
    cx = dx + 20
    glue(revolve_z(1, [line(cx + 10, 0.0, cx + 20, 20.0), arc(cx, 20.0, 20.0, 0.0, 90.0)], cx))


def torispherical_head(dx):
    """A torispherical head of r 20: the knuckle of r 8 about the circle of r 12 at z 0 from the rim at z 0 to where
    it meets the crown of r 40 tangentially, and the crown to its pole at z 10.34, revolved together."""
    cx = dx + 20
    zc = -math.sqrt(32.0 ** 2 - 12.0 ** 2)
    tangent_deg = math.degrees(math.atan2(-zc, 12.0))
    knuckle = arc(cx + 12.0, 0.0, 8.0, 0.0, tangent_deg)
    crown = arc(cx, zc, 40.0, tangent_deg, 90.0)
    glue(revolve_z(1, [knuckle, crown], cx))


def half_dome(dx):
    """The hemisphere of r 20 revolved by 180 degrees only: a face with a degenerate edge and no seam."""
    cx = dx + 20
    revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx, math.pi)


Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
SHELLS = [("hemisphere", hemisphere),
          ("shallow cap", shallow_cap),
          ("cone 45", lambda dx: cone(dx, 45.0, 20.0)),
          ("sharp cone", lambda dx: cone(dx, 15.0, 10.0)),
          ("paraboloid", paraboloid),
          ("elliptical head", elliptical_head),
          ("sphere", sphere),
          ("spheroid", spheroid),
          ("split dome", lambda dx: split_dome(dx, [100, 200, 290])),
          ("oblique cone", oblique_cone),
          ("dome with hole", lambda dx: with_hole(dx, hemisphere, 0.0, 120.0, 40.0, 5.0)),
          ("sphere with hole", lambda dx: with_hole(dx, sphere, 20.0, 120.0, 30.0, 5.0)),
          ("pressure vessel", pressure_vessel),
          ("closed cone", closed_cone),
          ("dome on frustum", dome_on_frustum),
          ("torispherical head", torispherical_head),
          ("half dome", half_dome),
          ("needle cone", lambda dx: cone(dx, 11.0, 10.0))]
for i, (name, make) in enumerate(SHELLS):
    make(SPACING * i)
    OCC.Synchronize()
    print("shell", name, "at x", SPACING * i)
OCC.Synchronize()
for t in OCC.GetEntities(2):
    print("surface", t.Item2, Gmsh.Model.GetType(2, t.Item2), "area", round(OCC.GetMass(2, t.Item2, 0.0), 2))
for t in OCC.GetEntities(1):
    print("curve", t.Item2, Gmsh.Model.GetType(1, t.Item2), "length", round(OCC.GetMass(1, t.Item2, 0.0), 3))
Gmsh.Write(os.path.join(OUT, "cap_faces.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
