#PrePoMax Python Script
# Writes tube_faces.brep for Test_Model_Meshing_Tube.py: shell faces closed along a seam side by side, one part each,
# and one part of three faces, through GmshCommon (OCC kernel). The parts start at x = 150 i.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Tube_Faces.py -g No -x Yes -w <work folder>
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


def revolve_z(dim, tags, cx, cy, angle=2 * math.pi):
    out = OCC.Revolve(dimtags(dim, tags), cx, cy, 0.0, 0.0, 0.0, 1.0, angle, None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in out if t.Item1 == dim + 1]


def revolved_line(x0, z0, x1, z1, cx):
    """The face a line in the plane y = 0 sweeps about the vertical axis through (cx, 0)."""
    line = OCC.AddLine(OCC.AddPoint(x0, 0.0, z0), OCC.AddPoint(x1, 0.0, z1))
    return revolve_z(1, [line], cx, 0.0)[0]


def cylinder(dx, radius=20.0, height=50.0):
    """About the axis through (dx + 20, 0) from z 0."""
    revolved_line(dx + 20 + radius, 0.0, dx + 20 + radius, height, dx + 20)


def cone_frustum(dx):
    """r 20 at z 0 to r 10 at z 40."""
    revolved_line(dx + 40, 0.0, dx + 30, 40.0, dx + 20)


def revolved_spline(dx):
    """A B-spline profile of radii between 14 and 26 over 50 in z, revolved about the axis through (dx + 26, 0)."""
    cx = dx + 26
    radii = [(20, 0), (26, 12), (14, 26), (22, 38), (18, 50)]
    points = [OCC.AddPoint(cx + r, 0.0, z) for r, z in radii]
    spline = OCC.AddBSpline(Array[Int32](points), -1, 3, no_doubles())
    revolve_z(1, [spline], cx, 0.0)


def extruded_spline(dx):
    """A closed periodic B-spline of eight poles round an oval of 40 x 24, its parameter from 3 to 11, extruded by 40."""
    cx, cy = dx + 20, 0.0
    poles = [(cx + 20 * math.cos(a), cy + 12 * math.sin(a)) for a in [2 * math.pi * k / 8 for k in range(8)]]
    tags = [OCC.AddPoint(x, y, 0.0) for x, y in poles]
    tags.append(tags[0])
    knots = Array[Double]([3.0 + k for k in range(9)])
    multiplicities = Array[Int32]([1] * 9)
    weights = Array[Double]([1.0] * 9)
    spline = OCC.AddBSpline(Array[Int32](tags), -1, 3, weights, knots, multiplicities)
    OCC.Extrude(dimtags(1, [spline]), 0.0, 0.0, 40.0, None, no_ints(), no_doubles(), False)


def tube_circle(cx, radius=8.0):
    """The circle of the radius about the point (cx + 25, 0, 0) in the plane y = 0, starting at its outer point."""
    point = OCC.AddPoint(cx + 25 + radius, 0.0, 0.0)
    out = OCC.Revolve(dimtags(0, [point]), cx + 25, 0.0, 0.0, 0.0, 1.0, 0.0, 2 * math.pi, None, no_ints(),
                      no_doubles(), False)
    return [t.Item2 for t in out if t.Item1 == 1]


def elbow(dx):
    """A quarter of the tube of r 8 about the circle of r 25, a pipe bend of 90 degrees closed along one curved seam."""
    cx = dx + 8
    revolve_z(1, tube_circle(cx), cx, 0.0, 0.5 * math.pi)


def oblique_cylinder(dx):
    """The cylinder of r 20 about the axis through (dx + 20, 0), cut at z 0 and by the plane through (dx + 20, 0, 40)
    tilted by 30 degrees about y: the box above that plane cuts it and the piece below is kept."""
    cx = dx + 20
    existing = surfaces()
    face = revolved_line(cx + 20, 0.0, cx + 20, 80.0, cx)
    slope = math.tan(math.radians(30))
    corners = [OCC.AddPoint(cx + x, y, 40.0 + slope * x) for x, y in [(-60, -60), (60, -60), (60, 60), (-60, 60)]]
    lines = [OCC.AddLine(corners[k], corners[(k + 1) % 4]) for k in range(4)]
    plane = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](lines))]))
    tool = OCC.Extrude(dimtags(2, [plane]), 0.0, 0.0, 100.0, None, no_ints(), no_doubles(), False)
    box = [t.Item2 for t in tool if t.Item1 == 3][0]
    OCC.Fragment(dimtags(2, [face]), dimtags(3, [box]), None, None, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    new = [tag for tag in surfaces() if tag not in existing]
    centres = dict((tag, OCC.GetCenterOfMass(2, tag)[2]) for tag in new)
    keep = min(centres, key=lambda tag: centres[tag])
    OCC.Remove(dimtags(2, [tag for tag in new if tag != keep]), True)
    OCC.Synchronize()


def split_rims(dx, bottom_angles, top_angles):
    """The cylinder of r 20 and 50 high whose bottom rim is split at the bottom angles and top rim at the top angles
    (degrees from the seam), by points fragmented into it."""
    cx = dx + 20
    face = revolved_line(cx + 20, 0.0, cx + 20, 50.0, cx)
    points = [OCC.AddPoint(cx + 20 * math.cos(math.radians(a)), 20 * math.sin(math.radians(a)), 0.0)
              for a in bottom_angles]
    points += [OCC.AddPoint(cx + 20 * math.cos(math.radians(a)), 20 * math.sin(math.radians(a)), 50.0)
               for a in top_angles]
    OCC.Fragment(dimtags(2, [face]), dimtags(0, points), None, None, True, True)
    OCC.Synchronize()


def pipe_with_hole(dx):
    """r 20, 60 high, with a hole of r 6 through its wall at z 30 and 120 degrees from the seam."""
    cx = dx + 20
    angle = math.radians(120)
    face = revolved_line(dx + 40, 0.0, dx + 40, 60.0, cx)
    existing = surfaces()
    existing.discard(face)
    c, s = math.cos(angle), math.sin(angle)
    centre = (cx + 10 * c, 10 * s, 30.0)
    point = OCC.AddPoint(centre[0], centre[1], centre[2] + 6.0)
    out = OCC.Revolve(dimtags(0, [point]), centre[0], centre[1], centre[2], c, s, 0.0, 2 * math.pi, None, no_ints(),
                      no_doubles(), False)
    curves = [t.Item2 for t in out if t.Item1 == 1]
    disc = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))
    tool = OCC.Extrude(dimtags(2, [disc]), 20 * c, 20 * s, 0.0, None, no_ints(), no_doubles(), False)
    volumes = [t.Item2 for t in tool if t.Item1 == 3]
    OCC.Fragment(dimtags(2, [face]), dimtags(3, volumes), None, None, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    new = [tag for tag in surfaces() if tag not in existing]
    areas = dict((tag, OCC.GetMass(2, tag, 0.0)) for tag in new)
    keep = max(areas, key=lambda tag: areas[tag])
    OCC.Remove(dimtags(2, [tag for tag in new if tag != keep]), True)
    OCC.Synchronize()


def torus_section(dx):
    """Half of the tube of r 8 about the circle of r 25 cut across by a plane through its axis, then closed again: a
    full torus is not meshed by gmsh, so the face closed along two seams is the torus of r 25 and 8 with both seams
    and a hole of r 4 through its outer wall, as in quad_dominant_seam_faces.brep."""
    cx = dx + 33
    face = revolve_z(1, tube_circle(cx), cx, 0.0)[0]
    existing = surfaces()
    existing.discard(face)
    angle = math.radians(120)
    c, s = math.cos(angle), math.sin(angle)
    centre = (cx + 23 * c, 23 * s, 0.0)
    point = OCC.AddPoint(centre[0], centre[1], centre[2] + 4.0)
    out = OCC.Revolve(dimtags(0, [point]), centre[0], centre[1], centre[2], c, s, 0.0, 2 * math.pi, None, no_ints(),
                      no_doubles(), False)
    curves = [t.Item2 for t in out if t.Item1 == 1]
    disc = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))
    tool = OCC.Extrude(dimtags(2, [disc]), 20 * c, 20 * s, 0.0, None, no_ints(), no_doubles(), False)
    volumes = [t.Item2 for t in tool if t.Item1 == 3]
    OCC.Fragment(dimtags(2, [face]), dimtags(3, volumes), None, None, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    new = [tag for tag in surfaces() if tag not in existing]
    areas = dict((tag, OCC.GetMass(2, tag, 0.0)) for tag in new)
    keep = max(areas, key=lambda tag: areas[tag])
    OCC.Remove(dimtags(2, [tag for tag in new if tag != keep]), True)
    OCC.Synchronize()


def cone_apex(dx):
    """A cone of r 20 at z 0 to its apex at z 40: a line from the base to the axis revolved."""
    revolved_line(dx + 40, 0.0, dx + 20, 40.0, dx + 20)


def sphere(dx):
    """r 20 about (dx + 20, 0, 20), a half circle revolved."""
    cx = dx + 20
    bottom = OCC.AddPoint(cx, 0.0, 0.0)
    centre = OCC.AddPoint(cx, 0.0, 20.0)
    top = OCC.AddPoint(cx, 0.0, 40.0)
    side = OCC.AddPoint(cx + 20, 0.0, 20.0)
    arc = OCC.AddCircleArc(bottom, side, top, False)
    revolve_z(1, [arc], cx, 0.0)
    OCC.Remove(dimtags(0, [centre]), False)


def capped_cylinder_and_cone(dx):
    """One part of three faces sharing their circles: a disc of r 20 at z 0, the cylinder of r 20 from z 0 to 40 and the
    cone frustum from r 20 at z 40 to r 10 at z 70, the cylinder and the cone revolved together."""
    cx = dx + 20
    existing = surfaces()
    p0 = OCC.AddPoint(cx + 20, 0.0, 0.0)
    p1 = OCC.AddPoint(cx + 20, 0.0, 40.0)
    p2 = OCC.AddPoint(cx + 10, 0.0, 70.0)
    walls = revolve_z(1, [OCC.AddLine(p0, p1), OCC.AddLine(p1, p2)], cx, 0.0)
    out = OCC.Revolve(dimtags(0, [OCC.AddPoint(cx + 20, 0.0, 0.0)]), cx, 0.0, 0.0, 0.0, 0.0, 1.0, 2 * math.pi, None,
                      no_ints(), no_doubles(), False)
    circle = [t.Item2 for t in out if t.Item1 == 1][0]
    disc = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32]([circle]))]))
    OCC.Fragment(dimtags(2, walls), dimtags(2, [disc]), None, None, True, True)
    OCC.Synchronize()
    print("capped cylinder and cone faces", sorted(tag for tag in surfaces() if tag not in existing))


Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
SHELLS = [("cylinder", cylinder),
          ("cone frustum", cone_frustum),
          ("revolved spline", revolved_spline),
          ("extruded spline", extruded_spline),
          ("elbow", elbow),
          ("oblique cylinder", oblique_cylinder),
          ("split rims", lambda dx: split_rims(dx, [120, 240], [60, 150, 270])),
          ("split rims paired", lambda dx: split_rims(dx, [90, 180, 270], [90, 180, 270])),
          ("short cylinder", lambda dx: cylinder(dx, 20.0, 3.0)),
          ("long thin cylinder", lambda dx: cylinder(dx, 4.0, 120.0)),
          ("pipe with hole", pipe_with_hole),
          ("torus with hole", torus_section),
          ("cone with apex", cone_apex),
          ("sphere", sphere),
          ("capped cylinder and cone", capped_cylinder_and_cone)]
for i, (name, make) in enumerate(SHELLS):
    make(SPACING * i)
    OCC.Synchronize()
    print("shell", name, "at x", SPACING * i)
OCC.Synchronize()
for t in OCC.GetEntities(2):
    print("surface", t.Item2, Gmsh.Model.GetType(2, t.Item2), "area", round(OCC.GetMass(2, t.Item2, 0.0), 2))
for t in OCC.GetEntities(1):
    print("curve", t.Item2, Gmsh.Model.GetType(1, t.Item2), "length", round(OCC.GetMass(1, t.Item2, 0.0), 3))
Gmsh.Write(os.path.join(OUT, "tube_faces.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
