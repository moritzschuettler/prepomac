#PrePoMax Python Script
# Writes quad_dominant_seam_faces.brep for Test_Model_Meshing_Quad_Dominant_Seam.py: shell faces closed along a seam
# side by side, one part each, through GmshCommon (OCC kernel). The parts start at x = 150 i.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Quad_Dominant_Seam_Faces.py -g No -x Yes -w <work folder>
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


def revolve_z(dim, tags, cx, cy):
    out = OCC.Revolve(dimtags(dim, tags), cx, cy, 0.0, 0.0, 0.0, 1.0, 2 * math.pi, None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in out if t.Item1 == dim + 1]


def revolved_line(x0, z0, x1, z1, cx):
    """The face a line in the plane y = 0 sweeps about the vertical axis through (cx, 0)."""
    line = OCC.AddLine(OCC.AddPoint(x0, 0.0, z0), OCC.AddPoint(x1, 0.0, z1))
    return revolve_z(1, [line], cx, 0.0)[0]


def radial_hole(cx, radius, angle, z, hole_radius):
    """Cuts a round hole through the wall of the vertical cylinder about (cx, 0) of the radius: the existing faces are
    fragmented by a cylinder normal to the wall at the angle and z; the pieces inside it are removed."""
    existing = surfaces()
    c, s = math.cos(angle), math.sin(angle)
    centre = (cx + (radius - 10) * c, (radius - 10) * s, z)
    point = OCC.AddPoint(centre[0], centre[1], centre[2] + hole_radius)
    out = OCC.Revolve(dimtags(0, [point]), centre[0], centre[1], centre[2], c, s, 0.0, 2 * math.pi, None, no_ints(),
                      no_doubles(), False)
    curves = [t.Item2 for t in out if t.Item1 == 1]
    disc = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))
    tool = OCC.Extrude(dimtags(2, [disc]), 20 * c, 20 * s, 0.0, None, no_ints(), no_doubles(), False)
    volumes = [t.Item2 for t in tool if t.Item1 == 3]
    return existing, volumes


def keep_largest(existing, objects, volumes):
    OCC.Fragment(dimtags(2, objects), dimtags(3, volumes), None, None, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    new = [tag for tag in surfaces() if tag not in existing]
    areas = dict((tag, OCC.GetMass(2, tag, 0.0)) for tag in new)
    keep = max(areas, key=lambda tag: areas[tag])
    OCC.Remove(dimtags(2, [tag for tag in new if tag != keep]), True)
    OCC.Synchronize()
    return keep


def cylinder(dx):
    """r 20, 50 high, about the axis through (dx + 20, 0)."""
    revolved_line(dx + 40, 0.0, dx + 40, 50.0, dx + 20)


def pipe_with_hole(dx, angle):
    """r 20, 60 high, with a hole of r 6 through its wall at z 30 and the angle from the seam."""
    cx = dx + 20
    face = revolved_line(dx + 40, 0.0, dx + 40, 60.0, cx)
    existing, volumes = radial_hole(cx, 20.0, angle, 30.0, 6.0)
    existing.discard(face)
    return keep_largest(existing, [face], volumes)


def cone_frustum(dx):
    """r 20 at z 0 to r 10 at z 40."""
    revolved_line(dx + 40, 0.0, dx + 30, 40.0, dx + 20)


def tube_circle(cx):
    """The circle of r 8 about the point (cx + 25, 0, 0) in the plane y = 0, starting at its outer point."""
    point = OCC.AddPoint(cx + 25 + 8, 0.0, 0.0)
    out = OCC.Revolve(dimtags(0, [point]), cx + 25, 0.0, 0.0, 0.0, 1.0, 0.0, 2 * math.pi, None, no_ints(),
                      no_doubles(), False)
    return [t.Item2 for t in out if t.Item1 == 1]


def torus_with_hole(dx):
    """The tube of r 8 about the circle of r 25 about the axis through (dx + 33, 0), closed along both seams, with a
    hole of r 4 through its outer wall at 120 degrees from the seam the tube closes along, across the other seam."""
    cx = dx + 33
    face = revolve_z(1, tube_circle(cx), cx, 0.0)[0]
    existing, volumes = radial_hole(cx, 33.0, math.radians(120), 0.0, 4.0)
    existing.discard(face)
    return keep_largest(existing, [face], volumes)


def elbow(dx):
    """A quarter of the tube of r 8 about the circle of r 25, a pipe bend of 90 degrees closed along one seam."""
    cx = dx + 8
    OCC.Revolve(dimtags(1, tube_circle(cx)), cx, 0.0, 0.0, 0.0, 0.0, 1.0, 0.5 * math.pi, None, no_ints(),
                      no_doubles(), False)


def revolved_spline(dx):
    """A B-spline profile of radii between 14 and 26 over 50 in z, revolved about the axis through (dx + 26, 0)."""
    cx = dx + 26
    radii = [(20, 0), (26, 12), (14, 26), (22, 38), (18, 50)]
    points = [OCC.AddPoint(cx + r, 0.0, z) for r, z in radii]
    spline = OCC.AddBSpline(Array[Int32](points), -1, 3, no_doubles())
    revolve_z(1, [spline], cx, 0.0)


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


def pipe_tee(dx):
    """A pipe of r 15 along x from dx to dx + 100 and a branch of r 8 from its axis up to z 45 at x = dx + 50, fused."""
    existing = surfaces()
    main_line = OCC.AddLine(OCC.AddPoint(dx, -15.0, 15.0), OCC.AddPoint(dx + 100, -15.0, 15.0))
    out = OCC.Revolve(dimtags(1, [main_line]), dx, 0.0, 15.0, 1.0, 0.0, 0.0, 2 * math.pi, None, no_ints(),
                      no_doubles(), False)
    main = [t.Item2 for t in out if t.Item1 == 2]
    branch_line = OCC.AddLine(OCC.AddPoint(dx + 58, 0.0, 15.0), OCC.AddPoint(dx + 58, 0.0, 60.0))
    branch = revolve_z(1, [branch_line], dx + 50, 0.0)
    OCC.Fragment(dimtags(2, main), dimtags(2, branch), None, None, True, True)
    OCC.Synchronize()
    pieces = [tag for tag in surfaces() if tag not in existing]
    remove = []
    for tag in pieces:
        x, y, z = OCC.GetCenterOfMass(2, tag)
        area = OCC.GetMass(2, tag, 0.0)
        # The branch inside the pipe lies below its wall; the piece of the pipe the branch covers is small
        if area < 300 or (z < 28.0 and area < 1000):
            remove.append(tag)
        print("tee piece", tag, "centre", round(x, 2), round(y, 2), round(z, 2), "area", round(area, 1),
              "removed" if tag in remove else "kept")
    OCC.Remove(dimtags(2, remove), True)
    OCC.Synchronize()


Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
SHELLS = [("cylinder", cylinder),
          ("pipe with hole", lambda dx: pipe_with_hole(dx, math.radians(120))),
          ("hole on seam", lambda dx: pipe_with_hole(dx, math.radians(4))),
          ("cone frustum", cone_frustum),
          ("torus with hole", torus_with_hole),
          ("revolved spline", revolved_spline),
          ("sphere", sphere),
          ("extruded spline", extruded_spline),
          ("pipe tee", pipe_tee),
          ("elbow", elbow)]
for i, (name, make) in enumerate(SHELLS):
    make(SPACING * i)
    OCC.Synchronize()
    print("shell", name, "at x", SPACING * i)
OCC.Synchronize()
for t in OCC.GetEntities(2):
    print("surface", t.Item2, Gmsh.Model.GetType(2, t.Item2), "area", round(OCC.GetMass(2, t.Item2, 0.0), 2))
Gmsh.Write(os.path.join(OUT, "quad_dominant_seam_faces.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
