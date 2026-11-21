#PrePoMax Python Script
# Writes pole_faces.brep and wrapped_faces.brep for Test_Model_Meshing_Pole_Faces.py and
# Test_Model_Meshing_Wrapped_Range.py through GmshCommon (OCC kernel), one part per shape side by side at x = 150 i.
#  - pole_faces.brep: four-sided faces with a pole of their surface at a corner (dome and cone sectors cut by a plane),
#    faces with a reentrant pole (dome, cone and paraboloid sectors of more than 225 degrees), and a hemisphere cut
#    by a box round its pole.
#  - wrapped_faces.brep: faces whose parameter range wraps over the seam of a periodic surface without the face being
#    closed. Cylinder and torus: an arc made in the centre mode from 45 to -45 degrees runs over the seam of its
#    circle, and its extrusion or revolution keeps that parameter range. Sphere and cone: the sector revolved from the
#    axis is written to a BREP file, the frame of its surface turned by half its angle and its curves in the surface
#    parameters moved back by it, so that the face lies symmetric about the seam; it is then read again.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Pole_Faces.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
SRC = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Make_Cap_Faces.py")
source = open(SRC).read()
exec(source[:source.index("Gmsh.Initialize()")])
import shutil
import tempfile
from System import Tuple


def new_surfaces(existing):
    return [tag for tag in surfaces() if tag not in existing]


def prism_tool(polygon, vector):
    """The volume of the polygon (points in space) extruded along the vector."""
    corners = [OCC.AddPoint(x, y, z) for x, y, z in polygon]
    lines = [OCC.AddLine(corners[k], corners[(k + 1) % len(corners)]) for k in range(len(corners))]
    base = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](lines))]))
    tool = OCC.Extrude(dimtags(2, [base]), vector[0], vector[1], vector[2], None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in tool if t.Item1 == 3][0]


def holed_prism_tool(polygon, centre, radius, normal_axis, vector):
    """The volume of the polygon with a round hole of the radius about the centre, extruded along the vector; the
    polygon and the hole lie in a plane normal to the axis normal_axis (0 x, 1 y, 2 z)."""
    corners = [OCC.AddPoint(x, y, z) for x, y, z in polygon]
    lines = [OCC.AddLine(corners[k], corners[(k + 1) % len(corners)]) for k in range(len(corners))]
    outer = OCC.AddCurveLoop(Array[Int32](lines))
    a, b = [k for k in range(3) if k != normal_axis]
    def at(t):
        p = list(centre)
        p[a] += radius * math.cos(t)
        p[b] += radius * math.sin(t)
        return OCC.AddPoint(p[0], p[1], p[2])
    p0, p1, p2, p3 = at(0.0), at(0.5 * math.pi), at(math.pi), at(1.5 * math.pi)
    hole = OCC.AddCurveLoop(Array[Int32]([OCC.AddCircleArc(p0, p1, p2, False), OCC.AddCircleArc(p2, p3, p0, False)]))
    base = OCC.AddPlaneSurface(Array[Int32]([outer, hole]))
    tool = OCC.Extrude(dimtags(2, [base]), vector[0], vector[1], vector[2], None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in tool if t.Item1 == 3][0]


def keep_inside(faces, volume, existing):
    """The faces intersected with the volume; only the new faces of a curved surface are kept."""
    OCC.Intersect(dimtags(2, faces), dimtags(3, [volume]), None, None, -1, True, True)
    OCC.Synchronize()
    OCC.Remove(dimtags(3, [t.Item2 for t in OCC.GetEntities(3)]), True)
    OCC.Synchronize()
    new = new_surfaces(existing)
    OCC.Remove(dimtags(2, [tag for tag in new if Gmsh.Model.GetType(2, tag) == "Plane"]), True)
    OCC.Synchronize()


def box_x_below(cx, x_max):
    return prism_tool([(cx - 80, -80, -40), (x_max, -80, -40), (x_max, 80, -40), (cx - 80, 80, -40)], (0, 0, 120))


def box_x_above(cx, x_min):
    return prism_tool([(x_min, -80, -40), (cx + 80, -80, -40), (cx + 80, 80, -40), (x_min, 80, -40)], (0, 0, 120))


def box_y_below(cx, y_max):
    return prism_tool([(cx - 80, -80, -40), (cx + 80, -80, -40), (cx + 80, y_max, -40), (cx - 80, y_max, -40)],
                      (0, 0, 120))


# Faces with a pole ----------------------------------------------------------------------------------------------------
def dome_cut(dx, angle_deg, x_cut):
    """The hemisphere of r 20 about (dx + 20, 0, 0) revolved by the angle from +x, cut by the plane x = cx + x_cut."""
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx, math.radians(angle_deg))
    keep_inside(face, box_x_below(cx, cx + x_cut), existing)


def cone_cut(dx, half_angle_deg, radius, angle_deg, x_cut):
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [line(cx + radius, 0.0, cx, radius / math.tan(math.radians(half_angle_deg)))], cx,
                     math.radians(angle_deg))
    keep_inside(face, box_x_below(cx, cx + x_cut), existing)


def paraboloid_cut(dx, angle_deg, x_cut):
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [bspline([(cx, 15.0), (cx + 10.0, 15.0), (cx + 20.0, 0.0)], [1.0, 1.0, 1.0], 2, [0.0, 1.0],
                                 [3, 3])], cx, math.radians(angle_deg))
    keep_inside(face, box_x_below(cx, cx + x_cut), existing)


def dome_sector(dx, angle_deg):
    cx = dx + 20
    revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx, math.radians(angle_deg))


def cone_sector(dx, half_angle_deg, radius, angle_deg):
    cx = dx + 20
    revolve_z(1, [line(cx + radius, 0.0, cx, radius / math.tan(math.radians(half_angle_deg)))], cx,
              math.radians(angle_deg))


def paraboloid_sector(dx, angle_deg):
    cx = dx + 20
    revolve_z(1, [bspline([(cx, 15.0), (cx + 10.0, 15.0), (cx + 20.0, 0.0)], [1.0, 1.0, 1.0], 2, [0.0, 1.0],
                          [3, 3])], cx, math.radians(angle_deg))


def dome_270_cut(dx):
    """The dome revolved by 270 degrees with the side beyond x = cx - 14 cut off: two more corners on its rim."""
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx, math.radians(270))
    keep_inside(face, box_x_above(cx, cx - 14), existing)


def dome_box(dx):
    """The hemisphere of r 20 within the square |x - cx|, |y| < 12 round its pole."""
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx)
    tool = prism_tool([(cx - 12, -12, -40), (cx + 12, -12, -40), (cx + 12, 12, -40), (cx - 12, 12, -40)], (0, 0, 120))
    keep_inside(face, tool, existing)


def dome_cut_y(dx):
    """The dome revolved by 90 degrees from +x, cut by the plane y = 8: the dome 90 cut wrapped with its seam at an
    edge."""
    cx = dx + 20
    existing = surfaces()
    face = revolve_z(1, [arc(cx, 0.0, 20.0, 90.0, 0.0)], cx, 0.5 * math.pi)
    keep_inside(face, box_y_below(cx, 8.0), existing)


POLE_SHAPES = [("dome 90 cut", lambda dx: dome_cut(dx, 90, 14)),
               ("dome 60 cut", lambda dx: dome_cut(dx, 60, 14)),
               ("dome 120 cut", lambda dx: dome_cut(dx, 120, 14)),
               ("cone 45 quarter cut", lambda dx: cone_cut(dx, 45.0, 20.0, 90, 12)),
               ("cone 15 half cut", lambda dx: cone_cut(dx, 15.0, 10.0, 180, 6)),
               ("paraboloid quarter cut", lambda dx: paraboloid_cut(dx, 90, 14)),
               ("dome 270", lambda dx: dome_sector(dx, 270)),
               ("dome 240", lambda dx: dome_sector(dx, 240)),
               ("dome 300", lambda dx: dome_sector(dx, 300)),
               ("cone 60 270", lambda dx: cone_sector(dx, 60.0, 20.0, 270)),
               ("cone 45 330", lambda dx: cone_sector(dx, 45.0, 20.0, 330)),
               ("paraboloid 270", lambda dx: paraboloid_sector(dx, 270)),
               ("dome 270 cut", dome_270_cut),
               ("dome box", dome_box)]


# Faces across the seam ------------------------------------------------------------------------------------------------
def seam_arc(cx, r, z, plain=False):
    """The arc of r about (cx, 0, z) in the plane z from 45 to -45 degrees through +x, made in the centre mode, which
    lays it over the seam of its circle."""
    s = OCC.AddPoint(cx + r * math.cos(math.radians(45)), r * math.sin(math.radians(45)), z)
    c = OCC.AddPoint(cx, 0.0, z)
    e = OCC.AddPoint(cx + r * math.cos(math.radians(-45)), r * math.sin(math.radians(-45)), z)
    if plain:
        m = OCC.AddPoint(cx + r, 0.0, z)
        return OCC.AddCircleArc(s, m, e, False)
    return OCC.AddCircleArc(s, c, e, True)


def cylinder_patch(cx, plain=False):
    """r 20 about the axis (cx, 0), from -45 to 45 degrees about +x and from z 0 to 30."""
    out = OCC.Extrude(dimtags(1, [seam_arc(cx, 20.0, 0.0, plain)]), 0.0, 0.0, 30.0, None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in out if t.Item1 == 2]


def yz_tool(cx, polygon):
    """The polygon of (y, z) points extruded along x across the cylinder patch."""
    return prism_tool([(cx - 10, y, z) for y, z in polygon], (60, 0, 0))


def cylinder_shape(dx, polygon=None, hole=None, plain=False):
    cx = dx + 20
    existing = surfaces()
    faces = cylinder_patch(cx, plain)
    if polygon is not None:
        keep_inside(faces, yz_tool(cx, polygon), existing)
    if hole is not None:
        rectangle = [(cx - 10, -40, -10), (cx - 10, 40, -10), (cx - 10, 40, 40), (cx - 10, -40, 40)]
        keep_inside(faces, holed_prism_tool(rectangle, (cx - 10, hole[0], hole[1]), hole[2], 0, (60, 0, 0)), existing)


def circle_polygon(y0, z0, r, n=48, start_deg=0.0, end_deg=360.0):
    return [(y0 + r * math.cos(math.radians(start_deg + (end_deg - start_deg) * k / n)),
             z0 + r * math.sin(math.radians(start_deg + (end_deg - start_deg) * k / n))) for k in range(n)]


def cylinder_disc(dx):
    """The cylinder patch within the round tube of r 10 along x about (y 0, z 15)."""
    cx = dx + 20
    existing = surfaces()
    faces = cylinder_patch(cx)
    tool = round_tool(cx, 0.0, 15.0, 10.0, False)
    keep_inside(faces, tool, existing)


def cylinder_half_disc(dx):
    """The cylinder patch within the half tube of r 12 along x above z 5."""
    cx = dx + 20
    existing = surfaces()
    faces = cylinder_patch(cx)
    tool = round_tool(cx, 0.0, 5.0, 12.0, True)
    keep_inside(faces, tool, existing)


def round_tool(cx, y0, z0, r, half):
    x = cx - 10
    if half:
        p0 = OCC.AddPoint(x, y0 + r, z0)
        pm = OCC.AddPoint(x, y0, z0 + r)
        p1 = OCC.AddPoint(x, y0 - r, z0)
        curves = [OCC.AddCircleArc(p0, pm, p1, False), OCC.AddLine(p1, p0)]
    else:
        ps = [OCC.AddPoint(x, y0 + r * math.cos(t), z0 + r * math.sin(t)) for t in (0.0, 0.5 * math.pi, math.pi,
                                                                                    1.5 * math.pi)]
        curves = [OCC.AddCircleArc(ps[0], ps[1], ps[2], False), OCC.AddCircleArc(ps[2], ps[3], ps[0], False)]
    base = OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](curves))]))
    tool = OCC.Extrude(dimtags(2, [base]), 60.0, 0.0, 0.0, None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in tool if t.Item1 == 3][0]


def annulus_tool(cx, y0, z0, r_out, r_in):
    """The ring between the circles of the radii about (y0, z0) extruded along x across the cylinder patch."""
    x = cx - 10
    loops = []
    for r in (r_out, r_in):
        ps = [OCC.AddPoint(x, y0 + r * math.cos(t), z0 + r * math.sin(t)) for t in (0.0, 0.5 * math.pi, math.pi,
                                                                                    1.5 * math.pi)]
        loops.append(OCC.AddCurveLoop(Array[Int32]([OCC.AddCircleArc(ps[0], ps[1], ps[2], False),
                                                    OCC.AddCircleArc(ps[2], ps[3], ps[0], False)])))
    base = OCC.AddPlaneSurface(Array[Int32](loops))
    tool = OCC.Extrude(dimtags(2, [base]), 60.0, 0.0, 0.0, None, no_ints(), no_doubles(), False)
    return [t.Item2 for t in tool if t.Item1 == 3][0]


def cylinder_annulus(dx, plain=False):
    """The cylinder patch within the ring of radii 12 and 5 about (y 0, z 15) along x: a ring face."""
    cx = dx + 20
    existing = surfaces()
    faces = cylinder_patch(cx, plain)
    keep_inside(faces, annulus_tool(cx, 0.0, 15.0, 12.0, 5.0), existing)


def torus_patch(dx):
    """The torus of radii 40 and 10 about the axis (cx, 0): its tube from -45 to 45 degrees about the outer equator,
    which runs over the seam of the tube circle, revolved by 90 degrees."""
    cx = dx + 20
    s = point(cx + 40 + 10 * math.cos(math.radians(45)), 10 * math.sin(math.radians(45)))
    c = point(cx + 40.0, 0.0)
    e = point(cx + 40 + 10 * math.cos(math.radians(-45)), 10 * math.sin(math.radians(-45)))
    revolve_z(1, [OCC.AddCircleArc(s, c, e, True)], cx, 0.5 * math.pi)


# The surface records of the BREP format that carry a frame: plane, cylinder, cone, sphere, torus
FRAMED_SURFACES = ("1", "2", "3", "4", "5")


def wrap_brep(path_in, path_out, alpha):
    """Turns the frame of the one surface of the BREP file by alpha about its axis and moves the u of its curves in the
    surface parameters, lines all, back by alpha, so that every point stays where it is."""
    lines = open(path_in).read().split("\n")
    out = []
    i = 0
    while i < len(lines):
        row = lines[i]
        out.append(row)
        i += 1
        if row.startswith("Curve2ds "):
            for _ in range(int(row.split()[1])):
                fields = lines[i].split()
                if fields[0] != "1":
                    raise Exception("a curve in the surface parameters is no line: " + lines[i])
                fields[1] = repr(float(fields[1]) - alpha)
                out.append(" ".join(fields))
                i += 1
        elif row.startswith("Surfaces "):
            if int(row.split()[1]) != 1:
                raise Exception("the file has " + row)
            fields = lines[i].split()
            if fields[0] not in FRAMED_SURFACES:
                raise Exception("the surface has no frame: " + lines[i])
            x = [float(v) for v in fields[7:10]]
            y = [float(v) for v in fields[10:13]]
            c, s = math.cos(alpha), math.sin(alpha)
            new_x = [c * x[k] + s * y[k] for k in range(3)]
            new_y = [-s * x[k] + c * y[k] for k in range(3)]
            fields[7:13] = [repr(v) for v in new_x + new_y]
            out.append(" ".join(fields))
            i += 1
    open(path_out, "w").write("\n".join(out))


def wrapped_sector(name, make, angle_deg):
    """Makes the sector in a model of its own about the axis through the origin, writes it, wraps it over the seam
    and returns the path of the wrapped file."""
    Gmsh.Model.Add("sector")
    make(-20.0)
    OCC.Synchronize()
    plain = os.path.join(TEMP, "_" + name.replace(" ", "_") + ".brep")
    wrapped = os.path.join(TEMP, "_" + name.replace(" ", "_") + "_wrapped.brep")
    Gmsh.Write(plain)
    Gmsh.Clear()
    wrap_brep(plain, wrapped, math.radians(angle_deg) / 2)
    return wrapped


def import_at(path, dx):
    existing = surfaces()
    OCC.ImportShapes(path, None, True, "")
    OCC.Synchronize()
    new = new_surfaces(existing)
    OCC.Translate(dimtags(2, new), dx + 20, 0.0, 0.0)
    OCC.Synchronize()
    return new


def imported_cut(path, dx, tool_maker):
    existing = surfaces()
    faces = import_at(path, dx)
    if tool_maker is not None:
        keep_inside(faces, tool_maker(dx + 20), existing)


SECTORS = [("sphere band wrapped", lambda dx: revolve_z(1, [arc(dx + 20, 0.0, 20.0, 50.0, 0.0)], dx + 20,
                                                        0.5 * math.pi), 90),
           ("dome 90 wrapped", lambda dx: dome_sector(dx, 90), 90),
           ("dome 180 wrapped", lambda dx: dome_sector(dx, 180), 180),
           ("lune 90 wrapped", lambda dx: revolve_z(1, [arc(dx + 20, 20.0, 20.0, -90.0, 90.0)], dx + 20,
                                                    0.5 * math.pi), 90),
           ("cone 45 quarter wrapped", lambda dx: cone_sector(dx, 45.0, 20.0, 90), 90),
           ("cone frustum wrapped", lambda dx: revolve_z(1, [line(dx + 40, 0.0, dx + 30, 10.0)], dx + 20,
                                                         0.5 * math.pi), 90),
           ("dome 90 cut wrapped", lambda dx: dome_sector(dx, 90), 90),
           ("dome 270 wrapped", lambda dx: dome_sector(dx, 270), 270)]
WRAPPED_CUTS = {"dome 90 cut wrapped": lambda cx: box_y_below(cx, 8.0)}

TRIANGLE = [(-34.142, -10.0), (13.333, -10.0), (-34.142, 49.367)]
NOTCH = [(-40, -10), (40, -10), (40, 15), (3, 15), (3, 40), (-40, 40)]
WRAPPED_SHAPES = [("cylinder patch", lambda dx: cylinder_shape(dx)),
                  ("cylinder triangle", lambda dx: cylinder_shape(dx, TRIANGLE)),
                  ("cylinder notch", lambda dx: cylinder_shape(dx, NOTCH)),
                  ("cylinder half disc", cylinder_half_disc),
                  ("cylinder disc", cylinder_disc),
                  ("cylinder hole", lambda dx: cylinder_shape(dx, hole=(0.0, 15.0, 6.0))),
                  ("torus patch", torus_patch)]
WRAPPED_SHAPES += [(name, None) for name, make, angle in SECTORS]
WRAPPED_SHAPES += [("cylinder hole plain", lambda dx: cylinder_shape(dx, hole=(0.0, 15.0, 6.0), plain=True)),
                   ("cylinder patch plain", lambda dx: cylinder_shape(dx, plain=True)),
                   ("dome 90 cut plain", lambda dx: dome_cut_y(dx)),
                   ("cylinder triangle plain", lambda dx: cylinder_shape(dx, TRIANGLE, plain=True)),
                   ("cylinder notch plain", lambda dx: cylinder_shape(dx, NOTCH, plain=True)),
                   ("cylinder annulus", cylinder_annulus),
                   ("cylinder annulus plain", lambda dx: cylinder_annulus(dx, True))]


Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
TEMP = tempfile.mkdtemp()
# The sectors to wrap, each in a model of its own
wrapped_files = {}
for name, make, angle in SECTORS:
    wrapped_files[name] = wrapped_sector(name, make, angle)
# pole_faces.brep
Gmsh.Model.Add("pole")
for i, (name, make) in enumerate(POLE_SHAPES):
    make(SPACING * i)
    OCC.Synchronize()
    print("shell", name, "at x", SPACING * i)
for t in OCC.GetEntities(2):
    print("surface", t.Item2, Gmsh.Model.GetType(2, t.Item2), "area", round(OCC.GetMass(2, t.Item2, 0.0), 2))
Gmsh.Write(os.path.join(OUT, "pole_faces.brep"))
Gmsh.Clear()
# wrapped_faces.brep
Gmsh.Model.Add("wrapped")
for i, (name, make) in enumerate(WRAPPED_SHAPES):
    dx = SPACING * i
    if make is not None:
        make(dx)
    else:
        imported_cut(wrapped_files[name], dx, WRAPPED_CUTS.get(name))
    OCC.Synchronize()
    print("shell", name, "at x", dx)
for t in OCC.GetEntities(2):
    tag = t.Item2
    low, high = Gmsh.Model.GetParametrizationBounds(2, tag)
    print("surface", tag, Gmsh.Model.GetType(2, tag), "area", round(OCC.GetMass(2, tag, 0.0), 2), "bounds",
          [round(v, 4) for v in low], [round(v, 4) for v in high])
Gmsh.Write(os.path.join(OUT, "wrapped_faces.brep"))
Gmsh.FinalizeAll()
shutil.rmtree(TEMP)
print("TEST PASSED")
