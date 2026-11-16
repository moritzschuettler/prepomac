#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the tube mapping: a shell face closed along exactly one seam and bounded by exactly two closed rims, the seam
#running from one rim to the other, is meshed as a structured tube - a ring that does not lie flat. Both rims carry N
#elements, the seam M, node i of one rim is joined to node i of the other, counted from the seam the same way round, and
#the face is N x M quads with no irregular node. The rims are balanced like the loops of a ring, edge by edge where their
#edges face each other and by their totals otherwise; the seam is free or takes the number set on it.
#
#  - tube_faces.brep: one part per shape side by side - a cylinder of r 20 and 50 high, a cone frustum of r 20 to 10 over
#    40, a B-spline profile of radii between 14 and 26 revolved, a closed periodic B-spline whose parameter runs from 3
#    to 11 extruded by 40 (its seam is not at the start of the parameter range), a pipe bend of 90 degrees (a quarter of
#    the torus of r 25 and 8, closed along one curved seam), a cylinder of r 20 cut at z 0 and by a plane tilted by 30
#    degrees through z 40, the cylinder of r 20 and 50 high with its bottom rim split at 120 and 240 degrees and its top
#    rim at 60, 150 and 270 degrees (not paired), the same split at 90, 180 and 270 degrees on both rims (paired), a
#    cylinder of r 20 and 3 high (one layer) and one of r 4 and 120 high; the faces that are no tubes - a pipe of r 20 and
#    60 high with a hole of r 6 through its wall, the torus of r 25 and 8 closed along both seams with a hole of r 4, a
#    cone of r 20 with its apex, and a sphere of r 20; and one part of three faces: a disc of r 20 closing a cylinder of
#    r 20 from z 0 to 40, which the cone frustum from r 20 at z 40 to r 10 at z 70 continues.
#    At the element sizes 5 and 3.5 and the recombinations None, Simple, Blossom, AllQuad, SimpleFullQuad and
#    BlossomFullQuad every tube is detected and meshed as a tube: quads only (two triangles per quad without
#    recombination), N on each rim, M + 1 nodes on the seam, N x M quads, every inner node in four quads and every rim
#    node in two, no fold, a worst scaled Jacobian of 0.2 at least measured in space against the normals of the mesh,
#    every node gmsh placed on the rims and the seam used once, no two nodes at one point, no element with a node twice
#    and no message, on the counts recorded;
#    the full-quad recombinations make both counts even (gmsh refuses a full-quad recombination of a periodic surface,
#    so it leaves the tube out of it and the tube is laid on the full counts). The faces that are no tubes are not
#    detected and are meshed exactly as with transfinite_submapped=False, the meshing before the tubes; the pipe with the
#    hole is converted to quads only under AllQuad as before.
#  - Table: per tube and size, gmsh's free mesh under AllQuad - the meshing before the tubes, with
#    transfinite_submapped=False - against the tube under AllQuad: quads, worst scaled Jacobian in space, irregular inner
#    nodes and time.
#  - Pinned counts on the cylinder: 30 elements on one rim are carried to the other; 20 and 24 on the two leave it free
#    with a message; 7 on the seam give 7 layers.
#  - Second order: the cylinder with Simple is quads of 8 nodes only, a tube.
#  - The part of three faces: the cylinder and the cone are tubes, their three circles carry the same number of elements,
#    and the disc is meshed free, quads only under AllQuad and the full-quad recombinations.
#  The models were made by gmsh's OCC kernel through GmshCommon: lines, circles and B-splines revolved or extruded, the
#  oblique end cut by fragmenting with a tilted box, the rims split by fragmenting with points, the holes cut by
#  fragmenting with a cylinder and removing the pieces inside it.
#  With the environment variable PMX_TUBE_PLOT_DIR set, the meshes are written into that folder as PNG images, the faces
#  unrolled about their axes with the seam drawn red.
#
#  PrePoMax.com -f tube_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_Tube.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys
import time
import clr
clr.AddReference('CaeMesh')
clr.AddReference('System.Drawing')
from CaeMesh import TubeFaceDetector
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

PLOT_DIR = os.environ.get("PMX_TUBE_PLOT_DIR")
SIZES = [5.0, 3.5]
RECOMBINATIONS = ["None", "Simple", "Blossom", "AllQuad", "SimpleFullQuad", "BlossomFullQuad"]
SPACING = 150.0
FLOOR = 0.2
TWO_PI = 2 * math.pi
SHELLS = ["cylinder", "cone frustum", "revolved spline", "extruded spline", "elbow", "oblique cylinder", "split rims",
          "split rims paired", "short cylinder", "long thin cylinder", "pipe with hole", "torus with hole",
          "cone with apex", "sphere", "capped cylinder and cone"]
TUBES = SHELLS[:10]
NOT_TUBES = ["pipe with hole", "torus with hole", "cone with apex", "sphere"]
MULTI = "capped cylinder and cone"
PAIRED = {"split rims": False}
# The unrolling of the shapes for the plots: the axis through (dx + x, 0), the radius the angle is scaled by, and the
# torus of the bend
AXIS_X = {"revolved spline": 26.0, "elbow": 8.0}
RADIUS = {"cone frustum": 15.0, "revolved spline": 20.0, "extruded spline": 16.0, "long thin cylinder": 4.0}
# By tube and size: the elements on each rim, N, and on the seam, M, under every recombination but the full-quad one,
# which makes both even
EXPECTED = {("cylinder", 5.0): (26, 10), ("cone frustum", 5.0): (26, 9), ("revolved spline", 5.0): (26, 11),
            ("extruded spline", 5.0): (20, 8), ("elbow", 5.0): (12, 11), ("oblique cylinder", 5.0): (28, 11),
            ("split rims", 5.0): (28, 10), ("split rims paired", 5.0): (28, 10), ("short cylinder", 5.0): (26, 1),
            ("long thin cylinder", 5.0): (12, 26),
            ("cylinder", 3.5): (36, 15), ("cone frustum", 3.5): (36, 12), ("revolved spline", 3.5): (36, 15),
            ("extruded spline", 3.5): (27, 12), ("elbow", 3.5): (15, 15), ("oblique cylinder", 3.5): (39, 15),
            ("split rims", 3.5): (36, 15), ("split rims paired", 3.5): (36, 15), ("short cylinder", 3.5): (36, 1),
            ("long thin cylinder", 3.5): (12, 35)}


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_x(names):
    """The geometry parts by the shape they were made from, told apart by where they lie along x."""
    parts = {}
    for part in pmx.model.geometry.parts:
        xs = [c[0] for c in part.get_vertex_data().coordinates]
        parts[names[int(round(min(xs) / SPACING))]] = part
    check(sorted(parts) == sorted(names), "the parts were not all found: " + str(sorted(parts)))
    return parts


def main_form():
    return [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]


def geometry_part(part):
    form = main_form()
    controller = form.GetType().GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form)
    return controller.Model.Geometry, controller.Model.Geometry.Parts[part.name]


def detected_tubes(part):
    geometry, geometry_part_ = geometry_part(part)
    return list(TubeFaceDetector.Detect(geometry, geometry_part_))


def output_messages(part_name):
    """The tube, ring, submapping and all-quad lines the last meshing of the part wrote to the output window."""
    form = main_form()
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    result = []
    for line in lines[starts[-1]:]:
        for marker in ("Tube mapping:", "Ring mapping:", "Submapping:", "All-quad meshing:"):
            if marker in line:
                result.append(line[line.index(marker):])
    return result


class Edges(object):
    """The edges of a geometry part by their 1-based ids: geometry ids, lengths and types."""

    def __init__(self, part):
        data = part.get_edge_data()
        self.ids = [int(i) for i in data.ids]
        self.geometry_ids = dict(zip(self.ids, [int(i) for i in data.geometry_ids]))
        self.lengths = dict(zip(self.ids, [float(l) for l in data.lengths]))


class Tube(object):
    """The edges of a tube the detection found: the seam and the two rims, by 1-based edge ids and geometry ids."""

    def __init__(self, part, face):
        edges = Edges(part)
        self.face_id = face.FaceId + 1
        self.seam = face.SeamEdgeId + 1
        self.rims = [[edge_id + 1 for edge_id in face.FirstRimEdgeIds], [edge_id + 1 for edge_id in face.SecondRimEdgeIds]]
        self.paired = face.IsPaired
        self.geometry_ids = edges.geometry_ids


def mesh(part, recombine, size, second_order=False, pins=(), submapped=True):
    """Meshes the part by a shell item; pins are (edge id, number of elements). Returns the time it took."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Tube_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                 [part.id]),
                                 relative_size=False, max_size=size, min_size=size / 10, second_order=second_order)
    names = ["PyTest_Tube_Size"]
    for edge_id, count in pins:
        name = "PyTest_Tube_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Tube", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine, transfinite_submapped=submapped)
    names.append("PyTest_Tube")
    start = time.time()
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(names)
    seconds = time.time() - start
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    return seconds


def nodes_on(geometry_id):
    """The mesh nodes on a geometry edge or the elements of a geometry face."""
    return set(int(i) for i in pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().ids)


def elements_on(geometry_id):
    return set(int(i) for i in pmx.create.selection_by_ids('femodel', 'elements', 'geometry',
                                                           [geometry_id]).preview().ids)


# Mesh access ----------------------------------------------------------------------------------------------------------
class FaceMesh(object):
    """The elements of a meshed part by their corner nodes, with the coordinates of the nodes; only the elements of the
    given ids when they are given."""

    def __init__(self, part_name, element_ids=None):
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
            if element_ids is not None and int(element_id) not in element_ids:
                continue
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


def boundary_edges(elements):
    uses = {}
    for element in elements:
        for k in range(len(element)):
            edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return [edge for edge, count in uses.items() if count == 1]


def boundary_nodes(elements):
    return set(node for edge in boundary_edges(elements) for node in edge)


def valences(elements):
    valence = {}
    for element in elements:
        for node in element:
            valence[node] = valence.get(node, 0) + 1
    return valence


def irregular_inner_nodes(elements):
    boundary = boundary_nodes(elements)
    return [node for node, count in valences(elements).items() if node not in boundary and count != 4]


def node_normals(face):
    """The normal of every node: the sum of the cross products at its corners in the elements around it."""
    sums = {}
    for element in face.elements:
        n = len(element)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            e1 = [a[k] - p[k] for k in range(3)]
            e2 = [b[k] - p[k] for k in range(3)]
            cross = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]
            length = math.sqrt(sum(v * v for v in cross))
            total = sums.setdefault(element[c], [0.0, 0.0, 0.0])
            for k in range(3):
                total[k] += cross[k] / length if length > 0 else 0.0
    normals = {}
    for node, n in sums.items():
        length = math.sqrt(sum(c * c for c in n))
        normals[node] = tuple(c / length for c in n) if length > 0 else (0.0, 0.0, 0.0)
    return normals


def min_scaled_jacobian(face):
    """The smallest sine of a corner in space against the normal of the mesh at its node, a triangle scaled so that 60
    degrees count as 1; negative where an element folds."""
    normals = node_normals(face)
    worst = 1.0
    for element in face.elements:
        n = len(element)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            e1 = [a[k] - p[k] for k in range(3)]
            e2 = [b[k] - p[k] for k in range(3)]
            cross = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]
            length = math.sqrt(sum(v * v for v in e1) * sum(v * v for v in e2))
            sine = sum(cross[k] * normals[element[c]][k] for k in range(3)) / length if length > 0 else -1.0
            worst = min(worst, sine * (2 / math.sqrt(3) if n == 3 else 1.0))
    return worst


def describe(face):
    quads, triangles = face.kinds()
    return quads, triangles, min_scaled_jacobian(face), len(irregular_inner_nodes(face.elements))


# Plots ----------------------------------------------------------------------------------------------------------------
def unrolled(name, p, dx):
    """The point in lengths about the axis of its shape from the seam: (around, along)."""
    cx = dx + AXIS_X.get(name, 20.0)
    if name == "elbow":
        rho = math.hypot(p[0] - cx, p[1])
        psi = math.atan2(p[2], rho - 25.0) % TWO_PI
        return 8 * psi, 25 * math.atan2(p[1], p[0] - cx)
    angle = math.atan2(p[1], p[0] - cx) % TWO_PI
    if name == "extruded spline":
        angle = (angle - SEAM_ANGLES.get(name, 0.0)) % TWO_PI
    return RADIUS.get(name, 20.0) * angle, p[2]


# The angle of the seam about the axis where it does not start at +x, set by main
SEAM_ANGLES = {}


def plot(file_name, title, name, face, dx):
    """The elements unrolled, quads blue, triangles orange, inner nodes without four quads red, the seam a red line at
    both ends of the unrolling; an element is drawn on the side of the seam its first corner lies on."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    period = (8.0 if name == "elbow" else RADIUS.get(name, 20.0)) * TWO_PI
    irregular = set(irregular_inner_nodes(face.elements))
    polygons = []
    marks = []
    for element in face.elements:
        points = [unrolled(name, face.xyz[node], dx) for node in element]
        us = [u for u, v in points]
        us = [u - round((u - us[0]) / period) * period for u in us]
        mean = sum(us) / len(us)
        us = [u - math.floor(mean / period) * period for u in us]
        polygons.append((list(zip(us, [v for u, v in points])), len(element)))
        for c, node in enumerate(element):
            if node in irregular:
                marks.append((us[c], points[c][1]))
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
    for value in (0.0, period):
        graphics.DrawLine(seam_pen, at(value, min(vs)), at(value, max(vs)))
    for u, v in marks:
        p = at(u, v)
        graphics.FillEllipse(Brushes.Firebrick, p.X - 3, p.Y - 3, 6.0, 6.0)
    graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, file_name + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


def seam_angle(part, dx):
    """The angle about the axis of the extruded spline of its seam, the one straight edge."""
    data = part.get_edge_data()
    lines = [k for k in range(data.count) if str(data.types[k]) == "Line"]
    check(len(lines) == 1, "the extruded spline has not one straight edge")
    centroid = data.centroids[lines[0]]
    return math.atan2(float(centroid[1]), float(centroid[0]) - dx - 20.0)


# Checks ---------------------------------------------------------------------------------------------------------------
def rim_nodes(tube, rim):
    nodes = set()
    for edge_id in tube.rims[rim]:
        nodes |= nodes_on(tube.geometry_ids[edge_id])
    return nodes


def check_tube_mesh(face, tube, title, quads=True):
    """The mesh of a tube: N x M quads (2 N x M triangles without quads) on the N nodes of each rim and the M + 1 of the
    seam, every rim node in two quads and every inner node in four, every node used once, no fold and no corner below
    the floor. Returns N, M, the worst scaled Jacobian and the irregular inner nodes."""
    first = rim_nodes(tube, 0)
    second = rim_nodes(tube, 1)
    seam = nodes_on(tube.geometry_ids[tube.seam])
    n = len(first)
    m = len(seam) - 1
    used = face.used_nodes()
    numbers, triangles = face.kinds()
    check(len(second) == n, title + " has " + str(n) + " and " + str(len(second)) + " nodes on its rims")
    check(len(first & seam) == 1 and len(second & seam) == 1, title + " has its seam not ending on its rims")
    if quads:
        check(triangles == 0 and numbers == n * m, title + " has " + str(numbers) + " quads and " + str(triangles) +
              " triangles, not " + str(n) + " x " + str(m))
    else:
        check(numbers == 0 and triangles == 2 * n * m, title + " has " + str(numbers) + " quads and " + str(triangles) +
              " triangles, not 2 x " + str(n) + " x " + str(m))
    check(first <= used and second <= used and seam <= used, title + " does not use every node of its rims and seam")
    check(len(used) == n * (m + 1), title + " has " + str(len(used)) + " nodes, not " + str(n) + " x " + str(m + 1))
    points = face.points(used)
    check(len(points) == len(set(points)), title + " has two nodes at one point")
    check(all(len(set(element)) == len(element) for element in face.elements), title + " has an element with a node twice")
    check(boundary_nodes(face.elements) == first | second, title + " has a boundary other than its two rims")
    valence = valences(face.elements)
    if quads:
        check(all(valence[node] == 2 for node in first | second), title + " has a rim node not in two quads")
        check(len(irregular_inner_nodes(face.elements)) == 0, title + " has irregular inner nodes")
    quality = min_scaled_jacobian(face)
    check(quality >= FLOOR, title + " has a corner of the scaled Jacobian " + str(quality))
    return n, m, quality


def check_tubes(parts, size):
    rows = []
    for name in TUBES:
        part = parts[name]
        dx = SPACING * SHELLS.index(name)
        faces = detected_tubes(part)
        check(len(faces) == 1, "the " + name + " is detected as " + str(len(faces)) + " tubes")
        tube = Tube(part, faces[0])
        check(tube.paired == PAIRED.get(name, True), "the " + name + " is paired " + str(tube.paired))
        counts = set()
        tube_row = None
        for recombine in RECOMBINATIONS:
            seconds = mesh(part, recombine, size)
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            title = name + " at size " + str(size) + " with " + recombine
            check(len(messages) == 0, title + " wrote: " + str(messages))
            n, m, quality = check_tube_mesh(face, tube, title, recombine != "None")
            print(title + ": a tube of", n, "x", m, face.kinds(), "worst scaled Jacobian", round(quality, 3), "in",
                  round(seconds, 3), "s")
            if recombine in ("SimpleFullQuad", "BlossomFullQuad"):
                check(n % 2 == 0 and m % 2 == 0, title + " has an odd count, " + str(n) + " x " + str(m))
            else:
                counts.add((n, m))
            if recombine == "AllQuad":
                tube_row = (face.kinds()[0], quality, len(irregular_inner_nodes(face.elements)), seconds)
                plot("tube_" + name.replace(" ", "_") + "_" + str(size).replace(".", "_"), title + ": " + str(n) +
                     " x " + str(m) + ", worst SJ " + str(round(quality, 3)), name, face, dx)
        # Simple, Blossom and AllQuad lay the tube on the same counts
        check(len(counts) == 1, "the " + name + " at size " + str(size) + " has different counts: " + str(counts))
        expected = EXPECTED.get((name, size))
        check(expected is None or counts == set([expected]), "the " + name + " at size " + str(size) + " is " +
              str(counts) + ", not " + str(expected))
        # The meshing before the tubes
        seconds = mesh(part, "AllQuad", size, submapped=False)
        face = FaceMesh(part.name)
        before = describe(face)
        plot("tube_" + name.replace(" ", "_") + "_" + str(size).replace(".", "_") + "_before", name + " at size " +
             str(size) + " before, AllQuad: " + str(before[0]) + " quads, worst SJ " + str(round(before[2], 3)) +
             ", irregular " + str(before[3]), name, face, dx)
        rows.append((name, size, before[0], before[1], round(before[2], 3), before[3], round(seconds, 2), tube_row[0],
                     round(tube_row[1], 3), tube_row[2], round(tube_row[3], 2)))
    return rows


def check_not_tubes(parts):
    """The faces that are no tubes are meshed as before the tubes."""
    for name in NOT_TUBES:
        part = parts[name]
        check(len(detected_tubes(part)) == 0, "the " + name + " is detected as a tube")
        for recombine in ("Simple", "AllQuad"):
            mesh(part, recombine, SIZES[0], submapped=False)
            before = FaceMesh(part.name)
            mesh(part, recombine, SIZES[0])
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            print(name + " with " + recombine + ":", face.kinds(), "worst scaled Jacobian",
                  round(min_scaled_jacobian(face), 3), messages)
            check(face.kinds() == before.kinds() and face.points(face.used_nodes()) == before.points(before.used_nodes()),
                  "the " + name + " with " + recombine + " is not meshed as before the tubes")
            check(not any("Tube mapping:" in message for message in messages), "the " + name + " wrote " + str(messages))
            if name == "pipe with hole" and recombine == "AllQuad":
                check(face.kinds()[1] == 0 and len(messages) == 0, "the pipe with the hole is not quads only: " +
                      str(face.kinds()) + " " + str(messages))


def check_pins(parts):
    part = parts["cylinder"]
    tube = Tube(part, detected_tubes(part)[0])
    first, second = tube.rims[0][0], tube.rims[1][0]
    # 30 elements on one rim are carried to the other
    mesh(part, "Simple", SIZES[0], pins=[(first, 30)])
    face = FaceMesh(part.name)
    n, m, quality = check_tube_mesh(face, tube, "the cylinder with 30 elements on a rim")
    check(n == 30 and len(output_messages(part.name)) == 0, "30 elements on one rim are not carried to the other: " + str(n))
    print("cylinder with 30 elements on a rim: a tube of", n, "x", m, "worst scaled Jacobian", round(quality, 3))
    # 20 and 24 leave it free with a message
    mesh(part, "Simple", SIZES[0], pins=[(first, 20), (second, 24)])
    face = FaceMesh(part.name)
    messages = output_messages(part.name)
    print("cylinder with 20 and 24 elements on its rims:", face.kinds(), "irregular", len(irregular_inner_nodes(
          [e for e in face.elements if len(e) == 4])), messages)
    check(len(messages) == 1 and messages[0].startswith("Tube mapping:") and "is meshed free" in messages[0] and
          "20" in messages[0] and "24" in messages[0], "20 and 24 elements must leave the cylinder free with a message: " +
          str(messages))
    check(face.kinds()[1] > 0 or len(irregular_inner_nodes(face.elements)) > 0, "the cylinder with 20 and 24 elements " +
          "is still a tube")
    # 7 on the seam give 7 layers
    mesh(part, "AllQuad", SIZES[0], pins=[(tube.seam, 7)])
    face = FaceMesh(part.name)
    n, m, quality = check_tube_mesh(face, tube, "the cylinder with 7 elements on its seam")
    check(m == 7 and len(output_messages(part.name)) == 0, "7 elements on the seam give " + str(m) + " layers")
    print("cylinder with 7 elements on its seam: a tube of", n, "x", m, "worst scaled Jacobian", round(quality, 3))
    plot("tube_cylinder_seam_7", "cylinder, 7 on the seam: " + str(n) + " x " + str(m), "cylinder", face, 0.0)


def check_second_order(parts):
    part = parts["cylinder"]
    tube = Tube(part, detected_tubes(part)[0])
    mesh(part, "Simple", SIZES[0], second_order=True)
    face = FaceMesh(part.name)
    check(all(count == 8 for count in face.node_counts), "the second order cylinder has elements of other than 8 " +
          "nodes: " + str(set(face.node_counts)))
    corners = set(node for element in face.elements for node in element)
    n = len(rim_nodes(tube, 0) & corners)
    m = len(nodes_on(tube.geometry_ids[tube.seam]) & corners) - 1
    check(len(face.elements) == n * m and len(irregular_inner_nodes(face.elements)) == 0,
          "the second order cylinder is not a tube: " + str(len(face.elements)) + " quads on " + str(n) + " x " + str(m))
    print("cylinder: second order tube of", n, "x", m, "quads of 8 nodes")


def check_multi_face(parts):
    part = parts[MULTI]
    tubes = [Tube(part, face) for face in detected_tubes(part)]
    check(len(tubes) == 2, "the " + MULTI + " has " + str(len(tubes)) + " tubes")
    faces = part.get_face_data()
    face_geometry_ids = dict(zip([int(i) for i in faces.ids], [int(i) for i in faces.geometry_ids]))
    face_types = dict(zip([int(i) for i in faces.ids], [str(t) for t in faces.types]))
    disc = [face_id for face_id, kind in face_types.items() if kind == "Plane"]
    check(len(disc) == 1, "the " + MULTI + " has no disc")
    for recombine in ("Simple", "AllQuad", "SimpleFullQuad", "BlossomFullQuad"):
        mesh(part, recombine, SIZES[0])
        messages = output_messages(part.name)
        circles = set()
        for tube in tubes:
            face = FaceMesh(part.name, elements_on(face_geometry_ids[tube.face_id]))
            n, m, quality = check_tube_mesh(face, tube, "the " + MULTI + " face " + str(tube.face_id) + " with " + recombine)
            circles.add(n)
            print(MULTI + " with " + recombine + ": face", tube.face_id, "a tube of", n, "x", m, "worst scaled Jacobian",
                  round(quality, 3))
        disc_face = FaceMesh(part.name, elements_on(face_geometry_ids[disc[0]]))
        disc_nodes = boundary_nodes(disc_face.elements)
        print(MULTI + " with " + recombine + ": the disc", disc_face.kinds(), "on", len(disc_nodes), "boundary nodes,",
              "worst scaled Jacobian", round(min_scaled_jacobian(disc_face), 3), messages)
        check(len(circles) == 1 and len(disc_nodes) in circles, "the circles of the " + MULTI + " carry " +
              str(sorted(circles)) + " and " + str(len(disc_nodes)) + " elements")
        check(len(messages) == 0, "the " + MULTI + " with " + recombine + " wrote " + str(messages))
        if recombine != "Simple":
            check(disc_face.kinds()[1] == 0, "the disc keeps triangles with AllQuad")


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    SEAM_ANGLES["extruded spline"] = seam_angle(parts["extruded spline"], SPACING * SHELLS.index("extruded spline"))
    rows = []
    for size in SIZES:
        rows.extend(check_tubes(parts, size))
    print("face | size | before quads | before triangles | before worst SJ | before irregular | before time s | " +
          "tube quads | tube worst SJ | tube irregular | tube time s")
    for row in rows:
        print(" | ".join(str(v) for v in row))
    check_not_tubes(parts)
    check_pins(parts)
    check_second_order(parts)
    check_multi_face(parts)
    print("TEST PASSED")


main()
