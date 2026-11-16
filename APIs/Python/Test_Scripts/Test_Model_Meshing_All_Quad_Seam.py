#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the all-quad recombination of a Gmsh shell on faces closed along a seam: GmshQuadMesher cuts such a face open
#along its seams - a node on a seam appears once for every side of it, with the parameters of that side - converts the
#cut face like any other free face and writes every node gmsh placed back once. The even total around such a face
#counts every edge once but the seam, which the face meets on both sides. A face of one seam between two closed rims is
#meshed as a structured tube while transfinite_submapped is on, so the meshings below turn it off
#(transfinite_submapped=False) and the conversion meets the cylinder, the cone, the splines and the bend as before the
#tubes; the tubes themselves are checked once with it on.
#
#  - all_quad_seam_faces.brep: one part per shape side by side - a cylinder of r 20 and 50 high, a pipe of r 20 and 60
#    high with a hole of r 6 through its wall at 120 degrees from its seam, the same pipe with the hole across its
#    seam, a cone frustum of r 20 to 10 over 40, a torus of r 25 and 8 closed along both seams with a hole of r 4
#    across one of them, a B-spline profile of radii between 14 and 26 revolved, a sphere of r 20, a closed periodic
#    B-spline whose parameter runs from 3 to 11 (so the seam is not at the start 0) extruded by 40, a tee of two pipes
#    of r 15 and 8 fused (one part of two faces) and a pipe bend of 90 degrees (a quarter of the torus, closed along
#    one curved seam). A full torus is not in it: gmsh cannot mesh a face closed along two seams without any other
#    edge ("Impossible to mesh periodic surface").
#    At the element sizes 5 and 3.5, against gmsh's own meshes under Simple and Blossom (the base of AllQuad): every
#    face but the sphere ends with quads only, an even number of elements around it, no fold, a worst scaled Jacobian
#    of 0.2 at least measured in space against the normal of its surface, every boundary node on an edge around the
#    face, as many nodes on the seams as Blossom has where the totals agree, every node once (no two nodes at one point,
#    so the seam is not torn open), no element with a node twice and no all-quad message. The boundary nodes are not
#    compared with Blossom's one by one: the balance meshes the curves once before the mesh, and gmsh then lays some of
#    them a little differently (the hole across the seam by up to 0.6 mm at the same counts). The sphere, whose poles
#    are degenerate edges, keeps the mesh gmsh gave it under Blossom (and its message, should that mesh have
#    triangles). The table of the three and the time are printed; Blossom is a run of its own, so its worst corner is
#    not always the one of the mesh AllQuad starts from.
#  A sweep source closed along a seam does not occur: the seam has no side surface, and the sweep refuses such a source
#  ("The sweep side surfaces around the source surface/s ... do not form a closed loop").
#  - Pinned counts on the cylinder: 25 and 24 elements on its rims leave exactly one triangle and the all-quad message
#    of the odd total 49; 25 on one rim alone is balanced to an even total; 7 elements on the seam change neither
#    verdict, since the seam does not count.
#  - Second order: the pipe with the hole with AllQuad is quads of 8 nodes only.
#  - Tubes: with transfinite_submapped on, at the element size 5 with AllQuad, the cylinder, the cone, the revolved and
#    the extruded spline, the bend and the branch of the tee (closed along its seam between its top rim and the curve
#    it meets the pipe along) are structured tubes - quads only, every rim node in two quads and every inner node in
#    four, a worst scaled Jacobian of 0.2 at least, no message, the pipe of the tee quads only as well - the sphere,
#    closed along its seam from pole to pole, is a structured cap (quads only, the four corners of the block at each
#    pole its only irregular inner nodes, no message), and every other face is meshed exactly as with it off.
#  The models were made by gmsh's OCC kernel through GmshCommon: lines, arcs, circles and B-splines revolved or
#  extruded, the holes cut by fragmenting with a cylinder and removing the pieces inside it.
#  With the environment variable PMX_QUAD_PLOT_DIR set, the meshes are written into that folder as PNG images, the
#  faces unrolled in their parameters with the seams drawn red, Simple, Blossom and AllQuad.
#
#  PrePoMax.com -f all_quad_seam_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_All_Quad_Seam.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys
import time
import clr
clr.AddReference('System.Drawing')
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
PLOT_DIR = os.environ.get("PMX_QUAD_PLOT_DIR")
SIZES = [5.0, 3.5]
SPACING = 150.0
FLOOR = 0.2
RIM_TOLERANCE = 1E-4
SHELLS = ["cylinder", "pipe with hole", "hole on seam", "cone frustum", "torus with hole", "revolved spline", "sphere",
          "extruded spline", "pipe tee", "elbow"]
TUBES = ["cylinder", "cone frustum", "revolved spline", "extruded spline", "elbow"]
TWO_PI = 2 * math.pi
# The axis of every shape: the x of the vertical axis through (x, 0) from the start of the part
AXIS_X = {"cylinder": 20.0, "pipe with hole": 20.0, "hole on seam": 20.0, "cone frustum": 20.0,
          "torus with hole": 33.0, "revolved spline": 26.0, "sphere": 20.0, "extruded spline": 20.0, "elbow": 8.0}
TORUS_RADIUS = 25.0
# The heights of the vertical shapes, and the holes cut radially: (angle from the seam, z of the axis, radius)
HEIGHTS = {"cylinder": 50.0, "pipe with hole": 60.0, "hole on seam": 60.0, "cone frustum": 40.0, "revolved spline": 50.0,
           "extruded spline": 40.0}
HOLES = {"pipe with hole": (math.radians(120), 30.0, 6.0), "hole on seam": (math.radians(4), 30.0, 6.0),
         "torus with hole": (math.radians(120), 0.0, 4.0)}


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_x(names, known=()):
    """The geometry parts not in known by the shape they were made from, told apart by where they lie along x."""
    parts = {}
    for part in pmx.model.geometry.parts:
        if part.name in known:
            continue
        xs = [c[0] for c in part.get_vertex_data().coordinates]
        parts[names[int(round(min(xs) / SPACING))]] = part
    check(sorted(parts) == sorted(names), "the parts were not all found: " + str(sorted(parts)))
    return parts


def output_messages(part_name):
    """The all-quad lines the last meshing of the part wrote to the output window."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    return [line[line.index("All-quad meshing:"):] for line in lines[starts[-1]:] if "All-quad meshing:" in line]


def edges_by_length(part):
    data = part.get_edge_data()
    result = {}
    for edge_id, length in zip([int(i) for i in data.ids], [float(l) for l in data.lengths]):
        result.setdefault(round(length, 3), []).append(edge_id)
    return result


def mesh(part, recombine, size, second_order=False, pins=(), submapped=False):
    """Meshes the part by a shell item; pins are (edge id, number of elements). Returns the time the meshing took. The
    structured faces, tubes among them, are off unless submapped is set."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Seam_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                 [part.id]),
                                 relative_size=False, max_size=size, min_size=size / 10, second_order=second_order)
    names = ["PyTest_Seam_Size"]
    for edge_id, count in pins:
        name = "PyTest_Seam_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Seam", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine, transfinite_submapped=submapped)
    names.append("PyTest_Seam")
    start = time.time()
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(names)
    seconds = time.time() - start
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    return seconds


# Mesh access ----------------------------------------------------------------------------------------------------------
class FaceMesh(object):
    """The elements of a meshed part by their corner nodes, with the coordinates of the nodes."""

    def __init__(self, part_name):
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name]
        check(len(meshed) == 1, "the part " + part_name + " was not meshed")
        self.xyz = dict(zip([int(i) for i in meshed[0].get_node_ids()],
                            [list(c) for c in meshed[0].get_node_coordinates()]))
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        self.elements = []
        self.node_counts = []
        for element_id in meshed[0].get_element_ids():
            i = index[int(element_id)]
            corners = 4 if node_counts[i] in (4, 8) else 3
            self.elements.append(connectivity[offsets[i]:offsets[i] + corners])
            self.node_counts.append(node_counts[i])

    def kinds(self):
        quads = sum(1 for e in self.elements if len(e) == 4)
        return quads, len(self.elements) - quads

    def used_nodes(self):
        return set(node for element in self.elements for node in element)

    def points(self, nodes):
        return sorted(tuple(round(c, 6) for c in self.xyz[node]) for node in nodes)

    def boundary_points(self):
        return self.points(boundary_nodes(self.elements))


def boundary_edges(elements):
    uses = {}
    for element in elements:
        for k in range(len(element)):
            edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return [edge for edge, count in uses.items() if count == 1]


def boundary_nodes(elements):
    return set(node for edge in boundary_edges(elements) for node in edge)


def distance_to_axis(p, origin, direction):
    d = [p[k] - origin[k] for k in range(3)]
    along = sum(d[k] * direction[k] for k in range(3))
    return math.sqrt(max(0.0, sum(v * v for v in d) - along * along))


def on_rim(name, p, dx):
    """Whether the point lies on an edge around the face: a rim, the end of a pipe or the edge of a hole."""
    x, y, z = p[0] - dx, p[1], p[2]
    if name in HOLES:
        angle, z0, radius = HOLES[name]
        cx = dx + AXIS_X[name]
        if abs(distance_to_axis(p, (cx, 0.0, z0), (math.cos(angle), math.sin(angle), 0.0)) - radius) < RIM_TOLERANCE:
            return True
    if name == "pipe tee":
        return abs(x) < RIM_TOLERANCE or abs(x - 100) < RIM_TOLERANCE or abs(z - 60) < RIM_TOLERANCE
    if name == "elbow":
        return abs(y) < RIM_TOLERANCE or abs(x - AXIS_X[name]) < RIM_TOLERANCE
    return name in HEIGHTS and (abs(z) < RIM_TOLERANCE or abs(z - HEIGHTS[name]) < RIM_TOLERANCE)


def irregular_inner_nodes(elements):
    boundary = boundary_nodes(elements)
    valence = {}
    for element in elements:
        for node in element:
            valence[node] = valence.get(node, 0) + 1
    return [node for node, count in valence.items() if node not in boundary and count != 4]


# Geometry of the shapes -----------------------------------------------------------------------------------------------
def wrap(angle):
    angle = math.fmod(angle, TWO_PI)
    return angle + TWO_PI if angle < 0 else angle


def on_branch(name, points, dx):
    """Whether the points lie on the branch of the tee, the cylinder of r 8 about the vertical axis through dx + 50."""
    return name == "pipe tee" and all(abs(math.hypot(p[0] - dx - 50, p[1]) - 8) < 1E-6 and p[2] > 15 for p in points)


def analytic_normal(name, p, dx, branch):
    if name == "pipe tee":
        if branch:
            x, y = p[0] - dx - 50, p[1]
            r = math.hypot(x, y)
            return (x / r, y / r, 0.0)
        y, z = p[1], p[2] - 15
        r = math.hypot(y, z)
        return (0.0, y / r, z / r)
    cx = dx + AXIS_X[name]
    x, y, z = p[0] - cx, p[1], p[2]
    rho = math.hypot(x, y)
    if name in ("torus with hole", "elbow"):
        n = (x - TORUS_RADIUS * x / rho, y - TORUS_RADIUS * y / rho, z)
    elif name == "sphere":
        n = (x, y, z - 20)
    elif name == "cone frustum":
        n = (x / rho, y / rho, 0.25)
    else:
        n = (x / rho, y / rho, 0.0)
    length = math.sqrt(sum(c * c for c in n))
    return tuple(c / length for c in n)


def mesh_normals(name, face, dx):
    """The normals of the nodes of the splines from the elements around them: of the revolved spline in its meridian
    plane, of the extruded spline horizontal."""
    sums = {}
    for element in face.elements:
        n = len(element)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            e1 = [a[k] - p[k] for k in range(3)]
            e2 = [b[k] - p[k] for k in range(3)]
            cross = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]
            total = sums.setdefault(element[c], [0.0, 0.0, 0.0])
            for k in range(3):
                total[k] += cross[k]
    normals = {}
    cx = dx + AXIS_X[name]
    for node, n in sums.items():
        p = face.xyz[node]
        if name == "extruded spline":
            n[2] = 0.0
        else:
            rho = math.hypot(p[0] - cx, p[1])
            t = (-p[1] / rho, (p[0] - cx) / rho, 0.0)
            along = sum(n[k] * t[k] for k in range(3))
            n = [n[k] - along * t[k] for k in range(3)]
        length = math.sqrt(sum(c * c for c in n))
        normals[node] = tuple(c / length for c in n)
    return normals


def min_scaled_jacobian(name, face, dx):
    """The smallest sine of a corner in space against the normal of the surface, a triangle scaled so that 60 degrees
    count as 1, the elements turned the way most corners turn; negative where an element folds."""
    normals = mesh_normals(name, face, dx) if name in ("revolved spline", "extruded spline") else None
    sines = []
    signs = 0
    for element in face.elements:
        n = len(element)
        branch = on_branch(name, [face.xyz[node] for node in element], dx)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            e1 = [a[k] - p[k] for k in range(3)]
            e2 = [b[k] - p[k] for k in range(3)]
            cross = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]
            nrm = normals[element[c]] if normals else analytic_normal(name, p, dx, branch)
            length = math.sqrt(sum(v * v for v in e1) * sum(v * v for v in e2))
            sine = sum(cross[k] * nrm[k] for k in range(3)) / length if length > 0 else 0.0
            sines.append(sine * (2 / math.sqrt(3) if n == 3 else 1.0))
            signs += 1 if sine > 0 else -1
    return min(s if signs > 0 else -s for s in sines)


def unrolled(name, p, dx, branch, seam_angle):
    """The point in the parameters of its face, scaled to lengths: (u, v, period of u, period of v), a period None
    where the face is not closed."""
    if name == "pipe tee":
        if branch:
            return (8 * wrap(math.atan2(p[1], p[0] - dx - 50)), p[2] + 95, 8 * TWO_PI, None)
        return (15 * wrap(math.atan2(p[2] - 15, p[1]) - math.pi), p[0] - dx, 15 * TWO_PI, None)
    cx = dx + AXIS_X[name]
    phi = wrap(math.atan2(p[1], p[0] - cx) - seam_angle)
    if name in ("torus with hole", "elbow"):
        rho = math.hypot(p[0] - cx, p[1])
        psi = wrap(math.atan2(p[2], rho - TORUS_RADIUS))
        return (TORUS_RADIUS * phi, 8 * psi, TORUS_RADIUS * TWO_PI if name == "torus with hole" else None, 8 * TWO_PI)
    return (20 * phi, p[2], 20 * TWO_PI, None)


def seam_angle_of(name, part, dx):
    """The angle of the seam about the axis: the seams of the revolved shapes start at +x; the extruded spline closes
    where its straight edge is."""
    if name != "extruded spline":
        return 0.0
    data = part.get_edge_data()
    lines = [k for k in range(data.count) if str(data.types[k]) == "Line"]
    check(len(lines) == 1, "the extruded spline has not one straight edge")
    centroid = data.centroids[lines[0]]
    return math.atan2(float(centroid[1]), float(centroid[0]) - dx - AXIS_X[name])


def seam_nodes(name, face, dx, seam_angle):
    """The nodes on a seam: a closed parameter at its start."""
    result = set()
    for node in face.used_nodes():
        p = face.xyz[node]
        branch = on_branch(name, [p], dx)
        u, v, pu, pv = unrolled(name, p, dx, branch, seam_angle)
        for value, period in ((u, pu), (v, pv)):
            if period is not None and min(value, period - value) < 1E-6 * period:
                result.add(node)
    return result


# Plots ----------------------------------------------------------------------------------------------------------------
def plot(file_name, title, name, face, dx, seam_angle):
    """The elements unrolled, quads blue, triangles orange, inner nodes without four quads red, the seams red lines.
    Every element is drawn on the side of the seam its middle lies on."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    polygons = []
    seams = set()
    irregular = set(irregular_inner_nodes(face.elements))
    marks = []
    for element in face.elements:
        branch = on_branch(name, [face.xyz[node] for node in element], dx)
        points = [unrolled(name, face.xyz[node], dx, branch, seam_angle) for node in element]
        corners = []
        for k in range(2):
            period = points[0][2 + k]
            values = [point[k] for point in points]
            if period is not None:
                values = [v - round((v - values[0]) / period) * period for v in values]
                mean = sum(values) / len(values)
                values = [v - math.floor(mean / period) * period for v in values]
                seams.add((k, period, branch))
            corners.append(values)
        polygons.append((list(zip(corners[0], corners[1])), len(element)))
        for c, node in enumerate(element):
            if node in irregular:
                marks.append((corners[0][c], corners[1][c]))
    us = [p[0] for polygon, n in polygons for p in polygon]
    vs = [p[1] for polygon, n in polygons for p in polygon]
    scale = 760.0 / max(max(us) - min(us), max(vs) - min(vs))
    width, height = int((max(us) - min(us)) * scale + 40), int((max(vs) - min(vs)) * scale + 70)
    bitmap = Bitmap(width, height)
    graphics = Graphics.FromImage(bitmap)
    graphics.Clear(Color.White)
    pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
    seam_pen = Pen(Color.Firebrick, 2.0)
    quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
    triangle_brush = SolidBrush(Color.FromArgb(246, 190, 160))

    def at(u, v):
        return PointF(float(20 + (u - min(us)) * scale), float(height - 20 - (v - min(vs)) * scale))

    for polygon, n in polygons:
        points = Array[PointF]([at(u, v) for u, v in polygon])
        graphics.FillPolygon(quad_brush if n == 4 else triangle_brush, points)
        graphics.DrawPolygon(pen, points)
    for k, period, branch in seams:
        low = min(vs) if k == 0 else min(us)
        high = max(vs) if k == 0 else max(us)
        if name == "pipe tee":
            low, high = (107.5, max(vs)) if branch else (min(vs), 100.0)
        for value in (0.0, period):
            graphics.DrawLine(seam_pen, at(value, low) if k == 0 else at(low, value),
                              at(value, high) if k == 0 else at(high, value))
    for u, v in marks:
        p = at(u, v)
        graphics.FillEllipse(Brushes.Firebrick, p.X - 3, p.Y - 3, 6.0, 6.0)
    graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, file_name + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


def describe(face, name, dx):
    quads, triangles = face.kinds()
    return quads, triangles, min_scaled_jacobian(name, face, dx), len(irregular_inner_nodes(face.elements))


# Checks ---------------------------------------------------------------------------------------------------------------
def check_shells(parts, size):
    rows = []
    for i, name in enumerate(SHELLS):
        part = parts[name]
        dx = SPACING * i
        seam_angle = seam_angle_of(name, part, dx)
        tag = name.replace(" ", "_") + "_" + str(size).replace(".", "_")
        meshes = {}
        for recombine in ("Simple", "Blossom", "AllQuad"):
            seconds = mesh(part, recombine, size)
            face = FaceMesh(part.name)
            quads, triangles, quality, irregular = describe(face, name, dx)
            messages = output_messages(part.name)
            meshes[recombine] = (face, quads, triangles, quality, irregular, seconds, messages)
            print(name + " at size " + str(size) + ": " + recombine, quads, "quads", triangles, "triangles, boundary total",
                  len(boundary_edges(face.elements)), "worst scaled Jacobian", round(quality, 3), "irregular", irregular,
                  "in", round(seconds, 3), "s, messages", messages)
            plot("all_quad_seam_" + tag + "_" + recombine.lower(), name + ", size " + str(size) + ", " + recombine +
                 ": " + str(quads) + " quads, " + str(triangles) + " triangles, worst SJ " + str(round(quality, 3)) +
                 ", irregular " + str(irregular), name, face, dx, seam_angle)
        face, quads, triangles, quality, irregular, seconds, messages = meshes["AllQuad"]
        blossom = meshes["Blossom"][0]
        total = len(boundary_edges(face.elements))
        points = face.points(face.used_nodes())
        check(len(points) == len(set(points)), "the " + name + " has two nodes at one point with AllQuad")
        check(all(len(set(element)) == len(element) for element in face.elements),
              "the " + name + " has an element with a node twice with AllQuad")
        if name == "sphere":
            check(face.kinds() == blossom.kinds() and points == blossom.points(blossom.used_nodes()),
                  "the sphere does not keep the mesh gmsh gave it")
            check(triangles == 0 or (len(messages) == 1 and "degenerate" in messages[0]),
                  "the sphere wrote: " + str(messages))
        else:
            check(total % 2 == 0, "the " + name + " has an odd number of elements around it with AllQuad, " + str(total))
            check(triangles == 0, "the " + name + " keeps " + str(triangles) + " triangles with AllQuad")
            check(quality >= FLOOR, "the " + name + " has a corner of the scaled Jacobian " + str(quality))
            check(len(messages) == 0, "the " + name + " wrote: " + str(messages))
            off = [face.xyz[node] for node in boundary_nodes(face.elements) if not on_rim(name, face.xyz[node], dx)]
            check(len(off) == 0, "the " + name + " has boundary nodes off its edges with AllQuad: " + str(off[:3]))
            if total == len(boundary_edges(blossom.elements)):
                seams = len(seam_nodes(name, face, dx, seam_angle))
                blossom_seams = len(seam_nodes(name, blossom, dx, seam_angle))
                check(seams == blossom_seams, "the " + name + " has " + str(seams) + " nodes on its seams with AllQuad " +
                      "and " + str(blossom_seams) + " with Blossom")
        simple = meshes["Simple"]
        rows.append((name, size, simple[1], simple[2], round(simple[3], 3), simple[4], meshes["Blossom"][1],
                     meshes["Blossom"][2], round(meshes["Blossom"][3], 3), meshes["Blossom"][4], quads, triangles,
                     round(quality, 3), irregular, round(seconds, 2)))
    return rows


def check_pins(parts):
    part = parts["cylinder"]
    edges = edges_by_length(part)
    rims = edges[round(TWO_PI * 20, 3)]
    seam = edges[50.0][0]
    check(len(rims) == 2, "the cylinder has not two rims: " + str(edges))
    cases = [("25 and 24 on the rims", [(rims[0], 25), (rims[1], 24)], 49),
             ("7 on the seam, 25 and 24 on the rims", [(seam, 7), (rims[0], 25), (rims[1], 24)], 49),
             ("25 on one rim", [(rims[0], 25)], None),
             ("7 on the seam", [(seam, 7)], None)]
    for title, pins, odd_total in cases:
        mesh(part, "AllQuad", SIZES[0], pins=pins)
        face = FaceMesh(part.name)
        messages = output_messages(part.name)
        quads, triangles = face.kinds()
        total = len(boundary_edges(face.elements))
        quality = min_scaled_jacobian("cylinder", face, 0.0)
        print("cylinder with " + title + ": boundary total", total, quads, "quads", triangles, "triangles, worst scaled "
              "Jacobian", round(quality, 3), messages)
        check(quality >= FLOOR, "the cylinder with " + title + " has a corner of the scaled Jacobian " + str(quality))
        if odd_total is None:
            check(total % 2 == 0 and triangles == 0 and len(messages) == 0,
                  "the cylinder with " + title + " is not quads only")
        else:
            check(total == odd_total and triangles == 1, "the cylinder with " + title + " keeps " + str(triangles) +
                  " triangles around " + str(total) + " elements")
            check(len(messages) == 1 and "keeps 1 triangle - " in messages[0] and "odd number, " + str(total) in
                  messages[0], "the cylinder with " + title + " wrote " + str(messages))
        plot("all_quad_seam_cylinder_pins_" + str(cases.index((title, pins, odd_total))), "cylinder, " + title + ": " +
             str(quads) + " quads, " + str(triangles) + " triangles", "cylinder", face, 0.0, 0.0)


def check_second_order(parts):
    part = parts["pipe with hole"]
    mesh(part, "AllQuad", SIZES[0], second_order=True)
    face = FaceMesh(part.name)
    check(all(count == 8 for count in face.node_counts), "the second order pipe has elements of other than 8 nodes: " +
          str(set(face.node_counts)))
    print("pipe with hole: AllQuad of second order,", len(face.elements), "quads of 8 nodes")


def check_tubes(parts):
    """With the structured faces on, a face of one seam between two rims is a tube, every other face is converted as
    with them off."""
    for i, name in enumerate(SHELLS):
        part = parts[name]
        dx = SPACING * i
        mesh(part, "AllQuad", SIZES[0])
        before = FaceMesh(part.name)
        mesh(part, "AllQuad", SIZES[0], submapped=True)
        face = FaceMesh(part.name)
        quads, triangles, quality, irregular = describe(face, name, dx)
        messages = output_messages(part.name)
        # The branch of the tee, closed along its seam between its top rim and the curve it meets the pipe along, is a
        # tube of its own
        elements = face.elements
        if name == "pipe tee":
            elements = [e for e in face.elements if on_branch(name, [face.xyz[node] for node in e], dx)]
        valence = {}
        for element in elements:
            for node in element:
                valence[node] = valence.get(node, 0) + 1
        tube = all(len(e) == 4 for e in elements) and len(irregular_inner_nodes(elements)) == 0 and \
            all(valence[node] == 2 for node in boundary_nodes(elements))
        print(name + " with the structured faces on: tube", tube, "(" + str(len(elements)) + " elements)", quads, "quads",
              triangles, "triangles, worst scaled Jacobian", round(quality, 3), "irregular", irregular, messages)
        if name in TUBES or name == "pipe tee":
            check(tube and triangles == 0 and quality >= FLOOR and len(messages) == 0,
                  "the " + name + " is not meshed with a tube")
        elif name == "sphere":
            # A structured cap: a block of quads at each pole, whose four corners are the only irregular inner nodes
            irregular_valences = [valence[node] for node in irregular_inner_nodes(elements)]
            check(triangles == 0 and irregular_valences == [3] * 8 and len(messages) == 0,
                  "the sphere is not meshed with a cap: " + str(face.kinds()) + " " + str(irregular_valences) + " " +
                  str(messages))
        else:
            check(face.kinds() == before.kinds() and face.points(face.used_nodes()) == before.points(before.used_nodes()),
                  "the " + name + " is not meshed as with the structured faces off")
        plot("all_quad_seam_tube_" + name.replace(" ", "_"), name + ", structured faces on: " + str(quads) + " quads, " +
             str(triangles) + " triangles, worst SJ " + str(round(quality, 3)), name, face, dx,
             seam_angle_of(name, part, dx))


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    rows = []
    for size in SIZES:
        rows.extend(check_shells(parts, size))
    print("face | size | Simple quads | Simple triangles | Simple worst SJ | Simple irregular | Blossom quads | "
          "Blossom triangles | Blossom worst SJ | Blossom irregular | AllQuad quads | AllQuad triangles | "
          "AllQuad worst SJ | AllQuad irregular | AllQuad time s")
    for row in rows:
        print(" | ".join(str(v) for v in row))
    check_pins(parts)
    check_second_order(parts)
    check_tubes(parts)
    print("TEST PASSED")


main()
