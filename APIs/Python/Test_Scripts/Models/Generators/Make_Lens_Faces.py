#PrePoMax Python Script
# Writes lens_faces.brep for Test_Model_Meshing_Lens.py: shell faces with a pole of their surface and no seam - domes,
# lunes and cone sectors - a half disc, faces cut from a full sphere, and parts of several faces, side by side, one part
# each, through GmshCommon (OCC kernel). The parts start at x = 150 i. Reuses the helpers of Make_Cap_Faces.py.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Lens_Faces.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
SRC = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Make_Cap_Faces.py")
source = open(SRC).read()
exec(source[:source.index("Gmsh.Initialize()")])


def dome_sector(dx, angle_deg):
    """The hemisphere of r 20 about (dx + 20, 0, 0) revolved by the angle from +x."""
    cx = dx + 20
    revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx, math.radians(angle_deg))


def lune(dx, angle_deg):
    """The sphere of r 20 about (dx + 20, 0, 20) between two meridians the angle apart."""
    cx = dx + 20
    revolve_z(1, [arc(cx, 20.0, 20.0, -90.0, 90.0)], cx, math.radians(angle_deg))


def cone_sector(dx, half_angle_deg, radius, angle_deg):
    cx = dx + 20
    revolve_z(1, [line(cx + radius, 0.0, cx, radius / math.tan(math.radians(half_angle_deg)))], cx,
              math.radians(angle_deg))


def paraboloid_sector(dx, angle_deg):
    cx = dx + 20
    revolve_z(1, [bspline([(cx, 15.0), (cx + 10.0, 15.0), (cx + 20.0, 0.0)], [1.0, 1.0, 1.0], 2, [0.0, 1.0],
                          [3, 3])], cx, math.radians(angle_deg))


def elliptical_head_sector(dx, angle_deg):
    cx = dx + 20
    revolve_z(1, [quarter_ellipse(cx, 0.0, 20.0, 10.0)], cx, math.radians(angle_deg))


def half_disc(dx):
    cx = dx + 20
    p0 = point(cx + 20.0, 0.0)
    pm = point(cx, 20.0)
    p1 = point(cx - 20.0, 0.0)
    a = OCC.AddCircleArc(p0, pm, p1, False)
    l = OCC.AddLine(p1, p0)
    OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32]([a, l]))]))


def sphere_cut(dx, half_width_deg):
    """The full sphere of r 20 about (dx + 20, 0, 20), its seam at the azimuth 0, intersected with the wedge within
    half_width of the azimuth 0 about its axis, so that the range of the azimuth of the face wraps the seam."""
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [arc(cx, 20.0, 20.0, -90.0, 90.0)], cx)[0]
    a = math.radians(half_width_deg)
    outline = [(cx, 0.0)] + [(cx + 60 * math.cos(t), 60 * math.sin(t)) for t in (-a, -a / 2, 0.0, a / 2, a)]
    corners = [OCC.AddPoint(x, y, -10.0) for x, y in outline]
    lines = [OCC.AddLine(corners[k], corners[(k + 1) % len(corners)]) for k in range(len(corners))]
    base = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](lines))]))
    tool = OCC.Extrude(dimtags(2, [base]), 0.0, 0.0, 60.0, None, no_ints(), no_doubles(), False)
    wedge = [t.Item2 for t in tool if t.Item1 == 3][0]
    OCC.Intersect(dimtags(2, [face]), dimtags(3, [wedge]), None, None, -1, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    OCC.Synchronize()
    new = [tag for tag in surfaces() if tag not in existing]
    print("pieces", [(tag, Gmsh.Model.GetType(2, tag), round(OCC.GetMass(2, tag, 0.0), 1)) for tag in new])
    OCC.Remove(dimtags(2, [tag for tag in new if Gmsh.Model.GetType(2, tag) == "Plane"]), True)
    OCC.Synchronize()


def half_vessel(dx, angle_deg):
    """Two domes of r 20 closing a cylinder of r 20 from z 20 to 60, revolved together by the angle."""
    cx = dx + 20
    glue(revolve_z(1, [arc(cx, 20.0, 20.0, -90.0, 0.0), line(cx + 20, 20.0, cx + 20, 60.0),
                       arc(cx, 60.0, 20.0, 0.0, 90.0)], cx, math.radians(angle_deg)))


def closed_half_cone(dx):
    """The cone of r 20 at z 0 to its apex at z 20 revolved by 180 degrees, closed by the half disc at z 0."""
    cx = dx + 20
    wall = revolve_z(1, [line(cx + 20, 0.0, cx, 20.0)], cx, math.pi)
    out = OCC.Revolve(dimtags(0, [OCC.AddPoint(cx + 20, 0.0, 0.0)]), cx, 0.0, 0.0, 0.0, 0.0, 1.0, math.pi, None,
                      no_ints(), no_doubles(), False)
    rim = [t.Item2 for t in out if t.Item1 == 1][0]
    diameter = OCC.AddLine(OCC.AddPoint(cx - 20, 0.0, 0.0), OCC.AddPoint(cx + 20, 0.0, 0.0))
    disc = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32]([rim, diameter]))]))
    OCC.Fragment(dimtags(2, wall), dimtags(2, [disc]), None, None, True, True)
    OCC.Synchronize()


Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
SHELLS = [("dome 180", lambda dx: dome_sector(dx, 180)),
          ("dome 90", lambda dx: dome_sector(dx, 90)),
          ("dome 60", lambda dx: dome_sector(dx, 60)),
          ("dome 120", lambda dx: dome_sector(dx, 120)),
          ("dome 30", lambda dx: dome_sector(dx, 30)),
          ("dome 270", lambda dx: dome_sector(dx, 270)),
          ("lune 90", lambda dx: lune(dx, 90)),
          ("lune 180", lambda dx: lune(dx, 180)),
          ("lune 45", lambda dx: lune(dx, 45)),
          ("cone 45 half", lambda dx: cone_sector(dx, 45.0, 20.0, 180)),
          ("cone 15 half", lambda dx: cone_sector(dx, 15.0, 10.0, 180)),
          ("cone 45 quarter", lambda dx: cone_sector(dx, 45.0, 20.0, 90)),
          ("cone 45 270", lambda dx: cone_sector(dx, 45.0, 20.0, 270)),
          ("paraboloid half", lambda dx: paraboloid_sector(dx, 180)),
          ("half disc", half_disc),
          ("quarter sphere", lambda dx: sphere_cut(dx, 45.0)),
          ("half sphere", lambda dx: sphere_cut(dx, 90.0)),
          ("elliptical head half", lambda dx: elliptical_head_sector(dx, 180)),
          ("half vessel", lambda dx: half_vessel(dx, 180)),
          ("quarter vessel", lambda dx: half_vessel(dx, 90)),
          ("closed half cone", closed_half_cone)]
for i, (name, make) in enumerate(SHELLS):
    make(SPACING * i)
    OCC.Synchronize()
    print("shell", name, "at x", SPACING * i)
OCC.Synchronize()
for t in OCC.GetEntities(2):
    print("surface", t.Item2, Gmsh.Model.GetType(2, t.Item2), "area", round(OCC.GetMass(2, t.Item2, 0.0), 2))
Gmsh.Write(os.path.join(OUT, "lens_faces.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
