#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the cap mapping: a shell face closed along exactly one seam that ends in a pole - a degenerate edge, the apex
#of a cone or the pole of a sphere - at one end and on a closed rim at the other, or in a pole at both ends, is meshed as
#a structured cap, an O-grid: N elements round, from the rim inward L rings of N quads each, and around each pole a
#block of a x b quads, 2 (a + b) = N, a and b as equal as N allows, whose boundary is the innermost ring. The seam is a
#line of the grid through the rings and into the block to the pole, which is a node of the block d = b / 2 (rounded
#down) deep, so the seam carries L + d elements, or L + 2 d between two poles. The rim total is balanced even and 8 at
#least with the structured faces; the seam keeps the number the mesh size asks for unless the blocks would take more
#than 0.45 of it, or takes the number set on it.
#
#  - cap_faces.brep: one part per shape side by side - a hemisphere of r 20, the cap of a sphere of r 40 within 30
#    degrees of its pole, a cone of r 20 with its apex at 45 degrees and one of r 10 with a half angle of 15 degrees, a
#    paraboloid of r 20 and 15 high (a quadratic B-spline from the pole revolved), a 2:1 elliptical head of r 20 (a
#    rational B-spline), a sphere of r 20, a prolate spheroid of r 12 and 48 long, the hemisphere with its rim split
#    into four edges, the cone of r 20 and 40 high cut by a plane tilted by 20 degrees (its apex kept); the faces that
#    are no caps - the hemisphere with a hole of r 5 through its wall, the sphere with one, and the hemisphere revolved
#    by 180 degrees only (a face with a degenerate edge and no seam); parts of several faces: a pressure vessel (two
#    hemispheres of r 20 closing a cylinder 40 long), the cone of r 20 and 40 high closed by a disc, the cone frustum
#    from r 10 to r 20 with the hemisphere of r 20 on it, and a torispherical head (a crown of r 40 on a knuckle of r 8);
#    and a needle, a cone of r 10 with a half angle of 11 degrees.
#    At the element sizes 5 and 3.5 and the recombinations None, Simple, Blossom, AllQuad, SimpleFullQuad and
#    BlossomFullQuad every cap is detected and meshed as a cap: quads only (two triangles per quad without
#    recombination), exactly N x L + a x b per pole on the recorded counts, exactly four inner nodes of three quads per
#    pole and no other inner node without four, every rim node in two quads, every node gmsh placed on the rim and the
#    seam used and the pole node used, no two nodes at one point, no element with a node twice, no fold, a worst scaled
#    Jacobian of 0.2 at least of the quads measured in space against the normals of the mesh - a corner at a pole, where
#    the surface has no normal, against the normal of its own element; the two triangles a quad is split into without
#    recombination only unfolded, since on the sharp cone they are as narrow as 0.12 - and no message; the full-quad
#    recombinations make the counts even (the cap is meshed on the nodes of its edges before gmsh meshes the other
#    surfaces, so no recombination reaches it, and is laid on the full counts).
#  - Table: per cap and size, gmsh's free mesh under AllQuad - the meshing before the caps, with
#    transfinite_submapped=False - against the cap under AllQuad: quads, triangles, worst scaled Jacobian, irregular
#    inner nodes, the smallest and the largest element edge and the time.
#  - Pins on the hemisphere: 30 elements on the rim give 30 round; 25 leave it free with a message (odd), and so do 6
#    (fewer than eight); 9 on the seam give 9 on the seam; 3 leave it free with a message (no ring).
#  - Second order: the hemisphere with Simple is quads of 8 nodes only, a cap.
#  - The parts of several faces: every cap and every tube in them structured with one N round on the circles they
#    share, the disc of the closed cone a lens face of no corner (Test_Model_Meshing_Lens_Planar), quads only under
#    every recombination (meshed free before the lens faces of no corner, with triangles under Simple); the
#    pressure vessel has no irregular inner node but the eight block corners.
#  - The faces that are no caps are not detected and are meshed exactly as with transfinite_submapped=False, but for
#    the half dome, which is a lens face (Test_Model_Meshing_Lens): quads only, two inner nodes of three quads and no
#    other inner node without four, no message.
#  - The needle: gmsh itself cannot be relied on at the apex of a sharp cone (meshed free it has folded elements, splits
#    the edges of the face or fails), which is why a cap is meshed before gmsh meshes the surfaces and is kept down to a
#    worst scaled Jacobian of 0.1, half the floor of the other structured faces. At both sizes with Simple, AllQuad and
#    SimpleFullQuad the needle is a cap, quads only, of 0.1 at least and without a message.
#  - The quasi-structured quad algorithm lays its own quads round a pole, on edges it meshes coarser and refines, so
#    with it a cap is left to gmsh: the hemisphere is meshed, quads only, without a cap message.
#  The models were made by gmsh's OCC kernel through GmshCommon: arcs, lines and B-splines revolved, the oblique end
#  cut by fragmenting with a tilted box, the rim split by fragmenting with points, the holes cut by fragmenting with a
#  cylinder and removing the pieces inside it, the faces of a part fragmented with each other.
#  With the environment variable PMX_CAP_PLOT_DIR set, the meshes are written into that folder as PNG images, each
#  cap seen from its pole (azimuth about the axis, radius the distance from the pole) with its block visible, the
#  seam drawn red and the inner nodes without four quads marked red.
#
#  PrePoMax.com -f cap_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_Cap.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import time
import clr
clr.AddReference('CaeMesh')
clr.AddReference('System.Drawing')
from CaeMesh import CapFaceDetector, TubeFaceDetector
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

PLOT_DIR = os.environ.get("PMX_CAP_PLOT_DIR")
SIZES = [5.0, 3.5]
RECOMBINATIONS = ["None", "Simple", "Blossom", "AllQuad", "SimpleFullQuad", "BlossomFullQuad"]
SPACING = 150.0
FLOOR = 0.2
CAP_FLOOR = 0.1
SHELLS = ["hemisphere", "shallow cap", "cone 45", "sharp cone", "paraboloid", "elliptical head", "sphere", "spheroid",
          "split dome", "oblique cone", "dome with hole", "sphere with hole", "pressure vessel", "closed cone",
          "dome on frustum", "torispherical head", "half dome", "needle cone"]
CAPS = SHELLS[:10]
NOT_CAPS = ["dome with hole", "sphere with hole", "half dome"]
# The faces that are no caps but lens faces, meshed otherwise than with transfinite_submapped=False
LENSES = ["half dome"]
MULTI = {"pressure vessel": (2, 1), "closed cone": (1, 0), "dome on frustum": (1, 1), "torispherical head": (1, 1)}
# By cap and size: the elements round, N, and on the seam, M, under every recombination but the full-quad ones, which
# make both even
EXPECTED = {("hemisphere", 5.0): (26, 7), ("shallow cap", 5.0): (26, 7), ("cone 45", 5.0): (26, 7),
            ("sharp cone", 5.0): (14, 9), ("paraboloid", 5.0): (26, 7), ("elliptical head", 5.0): (26, 7),
            ("sphere", 5.0): (26, 14), ("spheroid", 5.0): (16, 13), ("split dome", 5.0): (26, 7),
            ("oblique cone", 5.0): (20, 6),
            ("hemisphere", 3.5): (36, 9), ("shallow cap", 3.5): (36, 9), ("cone 45", 3.5): (36, 9),
            ("sharp cone", 3.5): (18, 12), ("paraboloid", 3.5): (36, 9), ("elliptical head", 3.5): (36, 9),
            ("sphere", 3.5): (36, 18), ("spheroid", 3.5): (22, 17), ("split dome", 3.5): (36, 9),
            ("oblique cone", 3.5): (28, 8)}


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


def detected_caps(part):
    geometry, geometry_part_ = geometry_part(part)
    return list(CapFaceDetector.Detect(geometry_part_))


def detected_tubes(part):
    geometry, geometry_part_ = geometry_part(part)
    return list(TubeFaceDetector.Detect(geometry, geometry_part_))


def output_messages(part_name):
    """The cap, tube, ring, submapping and all-quad lines the last meshing of the part wrote to the output window."""
    form = main_form()
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    result = []
    for line in lines[starts[-1]:]:
        for marker in ("Cap mapping:", "Tube mapping:", "Ring mapping:", "Submapping:", "All-quad meshing:"):
            if marker in line:
                result.append(line[line.index(marker):])
    return result


class Cap(object):
    """The edges of a cap the detection found by 1-based edge ids and geometry ids, and its poles as points."""

    def __init__(self, part, face):
        data = part.get_edge_data()
        ids = [int(i) for i in data.ids]
        self.geometry_ids = dict(zip(ids, [int(i) for i in data.geometry_ids]))
        self.face_id = face.FaceId + 1
        self.seam = face.SeamEdgeId + 1
        self.rim = [edge_id + 1 for edge_id in face.RimEdgeIds]
        geometry, geometry_part_ = geometry_part(part)
        self.poles = [tuple(round(c, 6) for c in geometry.Nodes[node_id].Coor) for node_id in face.PoleVertexNodeIds]


class Tube(object):
    """The edges of a tube the detection found: the seam and the two rims, by 1-based edge ids and geometry ids."""

    def __init__(self, part, face):
        data = part.get_edge_data()
        ids = [int(i) for i in data.ids]
        self.geometry_ids = dict(zip(ids, [int(i) for i in data.geometry_ids]))
        self.face_id = face.FaceId + 1
        self.seam = face.SeamEdgeId + 1
        self.rims = [[edge_id + 1 for edge_id in face.FirstRimEdgeIds], [edge_id + 1 for edge_id in face.SecondRimEdgeIds]]


def mesh(part, recombine, size, second_order=False, pins=(), submapped=True, algorithm_2d=None):
    """Meshes the part by a shell item; pins are (edge id, number of elements). Returns the time it took."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Cap_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
                                 relative_size=False, max_size=size, min_size=size / 10, second_order=second_order)
    names = ["PyTest_Cap_Size"]
    for edge_id, count in pins:
        name = "PyTest_Cap_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])
    if algorithm_2d is None:
        items.add_shell_gmsh("PyTest_Cap", selection, algorithm_recombine=recombine, transfinite_submapped=submapped)
    else:
        items.add_shell_gmsh("PyTest_Cap", selection, algorithm_2d=algorithm_2d, algorithm_recombine=recombine,
                             transfinite_submapped=submapped)
    names.append("PyTest_Cap")
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


def face_geometry_ids(part):
    faces = part.get_face_data()
    return dict(zip([int(i) for i in faces.ids], [int(i) for i in faces.geometry_ids]))


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

    def nodes_at(self, points):
        """The nodes of the mesh within 1E-3 of the points."""
        found = set()
        for point in points:
            for node in self.used_nodes():
                if sum((self.xyz[node][c] - point[c]) ** 2 for c in range(3)) < 1E-6:
                    found.add(node)
        return found

    def edge_lengths(self):
        lengths = []
        for element in self.elements:
            for k in range(len(element)):
                p, q = self.xyz[element[k]], self.xyz[element[(k + 1) % len(element)]]
                lengths.append(math.sqrt(sum((p[c] - q[c]) ** 2 for c in range(3))))
        return min(lengths), max(lengths)


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


def cross(e1, e2):
    return [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]


def own_normal(face, element):
    """The normal of an element by its diagonals, or of a triangle by two of its sides."""
    p = [face.xyz[node] for node in element]
    if len(element) == 4:
        normal = cross([p[2][k] - p[0][k] for k in range(3)], [p[3][k] - p[1][k] for k in range(3)])
    else:
        normal = cross([p[1][k] - p[0][k] for k in range(3)], [p[2][k] - p[0][k] for k in range(3)])
    length = math.sqrt(sum(c * c for c in normal))
    return [c / length for c in normal] if length > 0 else [0.0, 0.0, 0.0]


def node_normals(face):
    """The normal of every node: the sum of the cross products at its corners in the elements around it."""
    sums = {}
    for element in face.elements:
        n = len(element)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            product = cross([a[k] - p[k] for k in range(3)], [b[k] - p[k] for k in range(3)])
            length = math.sqrt(sum(v * v for v in product))
            total = sums.setdefault(element[c], [0.0, 0.0, 0.0])
            for k in range(3):
                total[k] += product[k] / length if length > 0 else 0.0
    normals = {}
    for node, n in sums.items():
        length = math.sqrt(sum(c * c for c in n))
        normals[node] = tuple(c / length for c in n) if length > 0 else (0.0, 0.0, 0.0)
    return normals


def min_scaled_jacobian(face, poles=()):
    """The smallest sine of a corner in space against the normal of the mesh at its node, a triangle scaled so that 60
    degrees count as 1; negative where an element folds. A corner at a pole, where the surface has no normal and the
    corners round the apex of a cone cancel each other's normal out, is measured against the normal of its element."""
    normals = node_normals(face)
    pole_nodes = face.nodes_at(poles)
    worst = 1.0
    for element in face.elements:
        n = len(element)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            e1 = [a[k] - p[k] for k in range(3)]
            e2 = [b[k] - p[k] for k in range(3)]
            product = cross(e1, e2)
            normal = own_normal(face, element) if element[c] in pole_nodes else normals[element[c]]
            length = math.sqrt(sum(v * v for v in e1) * sum(v * v for v in e2))
            sine = sum(product[k] * normal[k] for k in range(3)) / length if length > 0 else -1.0
            worst = min(worst, sine * (2 / math.sqrt(3) if n == 3 else 1.0))
    return worst


def describe(face, poles):
    quads, triangles = face.kinds()
    shortest, longest = face.edge_lengths()
    return quads, triangles, min_scaled_jacobian(face, poles), len(irregular_inner_nodes(face.elements)), shortest, longest


# Plots ----------------------------------------------------------------------------------------------------------------
def plot(file_name, title, face, cap, dx):
    """The elements seen from each pole: the angle about the vertical axis of the shape, through (dx + 20, 0), and the
    distance from the pole; quads blue, triangles orange, inner nodes without four quads red, the seam red. A face with
    two poles is drawn twice, each half from its pole."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    irregular = set(irregular_inner_nodes(face.elements))
    seam = nodes_on(cap.geometry_ids[cap.seam])
    for k, pole in enumerate(cap.poles):
        others = [other for other in cap.poles if other != pole]
        polygons = []
        marks = []
        seam_points = []

        def at_pole(node):
            p = face.xyz[node]
            angle = math.atan2(p[1], p[0] - dx - 20.0)
            distance = math.sqrt(sum((p[c] - pole[c]) ** 2 for c in range(3)))
            return distance * math.cos(angle), distance * math.sin(angle)

        def nearer(node):
            p = face.xyz[node]
            return all(sum((p[c] - pole[c]) ** 2 for c in range(3)) <= sum((p[c] - other[c]) ** 2 for c in range(3)) + 1E-9
                       for other in others)

        for element in face.elements:
            if not all(nearer(node) for node in element):
                continue
            polygons.append(([at_pole(node) for node in element], len(element)))
            marks.extend(at_pole(node) for node in element if node in irregular)
        seam_points = sorted((at_pole(node) for node in seam if nearer(node)), key=lambda p: p[0] ** 2 + p[1] ** 2)
        xs = [p[0] for polygon, n in polygons for p in polygon]
        ys = [p[1] for polygon, n in polygons for p in polygon]
        extent = max(max(xs) - min(xs), max(ys) - min(ys))
        scale = 700.0 / extent
        width, height = int((max(xs) - min(xs)) * scale + 40), int((max(ys) - min(ys)) * scale + 70)
        bitmap = Bitmap(width, height)
        graphics = Graphics.FromImage(bitmap)
        graphics.Clear(Color.White)
        pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
        seam_pen = Pen(Color.Firebrick, 2.0)
        quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
        triangle_brush = SolidBrush(Color.FromArgb(246, 190, 160))

        def to_image(x, y):
            return PointF(float(20 + (x - min(xs)) * scale), float(height - 20 - (y - min(ys)) * scale))

        for polygon, n in polygons:
            points = Array[PointF]([to_image(x, y) for x, y in polygon])
            graphics.FillPolygon(quad_brush if n == 4 else triangle_brush, points)
            graphics.DrawPolygon(pen, points)
        for i in range(len(seam_points) - 1):
            graphics.DrawLine(seam_pen, to_image(*seam_points[i]), to_image(*seam_points[i + 1]))
        for x, y in marks:
            p = to_image(x, y)
            graphics.FillEllipse(Brushes.Firebrick, p.X - 3, p.Y - 3, 6.0, 6.0)
        graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
        bitmap.Save(os.path.join(PLOT_DIR, file_name + ("" if len(cap.poles) == 1 else "_pole" + str(k + 1)) + ".png"),
                    ImageFormat.Png)
        graphics.Dispose()
        bitmap.Dispose()


# Checks ---------------------------------------------------------------------------------------------------------------
def block(n):
    """The block of a cap of n elements round: a across the seam, b along it, and the depth of the pole in it."""
    a = n // 4
    b = n // 2 - a
    return a, b, b // 2


def rim_nodes(cap):
    nodes = set()
    for edge_id in cap.rim:
        nodes |= nodes_on(cap.geometry_ids[edge_id])
    return nodes


def round_count(cap, m, numbers):
    """The number round a cap with no rim, the even one whose grid on the seam of m elements has that many quads."""
    for n in range(8, 1000, 2):
        a, b, depth = block(n)
        rings = m - 2 * depth
        if rings >= 1 and n * rings + 2 * a * b == numbers:
            return n
    return None


def check_cap_mesh(face, cap, title, quads=True, floor=FLOOR):
    """The mesh of a cap: N x L + a x b per pole quads (twice as many triangles without quads) on the N nodes of the
    rim and the M + 1 of the seam, every rim node in two quads, four inner nodes of three quads per pole and every other
    inner node in four, every node used once, no fold and no corner below the floor. Returns N, M and the worst scaled
    Jacobian."""
    rim = rim_nodes(cap)
    seam = nodes_on(cap.geometry_ids[cap.seam])
    m = len(seam) - 1
    used = face.used_nodes()
    numbers, triangles = face.kinds()
    count = numbers if quads else triangles // 2
    n = len(rim) if cap.rim else round_count(cap, m, count)
    check(n is not None and n % 2 == 0 and n >= 8, title + " has " + str(n) + " elements round")
    a, b, depth = block(n)
    poles = len(cap.poles)
    rings = m - poles * depth
    check(rings >= 1, title + " has no ring: " + str(m) + " on the seam, " + str(n) + " round")
    expected = n * rings + poles * a * b
    if quads:
        check(triangles == 0 and numbers == expected, title + " has " + str(numbers) + " quads and " + str(triangles) +
              " triangles, not " + str(n) + " x " + str(rings) + " + " + str(poles) + " x " + str(a) + " x " + str(b))
    else:
        check(numbers == 0 and triangles == 2 * expected, title + " has " + str(numbers) + " quads and " + str(triangles) +
              " triangles, not twice " + str(expected))
    check(len(rim & seam) == (1 if cap.rim else 0), title + " has its seam not ending on its rim")
    check(rim <= used and seam <= used, title + " does not use every node of its rim and seam")
    pole_nodes = face.nodes_at(cap.poles)
    check(len(pole_nodes) == poles and pole_nodes <= seam, title + " does not use its pole nodes")
    nodes = n * (rings + 1) + poles * (a - 1) * (b - 1)
    check(len(used) == nodes, title + " has " + str(len(used)) + " nodes, not " + str(nodes))
    points = face.points(used)
    check(len(points) == len(set(points)), title + " has two nodes at one point")
    check(all(len(set(element)) == len(element) for element in face.elements), title + " has an element with a node twice")
    check(boundary_nodes(face.elements) == rim, title + " has a boundary other than its rim")
    if quads:
        valence = valences(face.elements)
        check(all(valence[node] == 2 for node in rim), title + " has a rim node not in two quads")
        irregular = irregular_inner_nodes(face.elements)
        check(len(irregular) == 4 * poles and all(valence[node] == 3 for node in irregular),
              title + " has the irregular inner nodes " + str(sorted(valence[node] for node in irregular)))
    quality = min_scaled_jacobian(face, cap.poles)
    check(quality >= floor if quads else quality > 0, title + " has a corner of the scaled Jacobian " + str(quality))
    return n, m, quality


def check_caps(parts, size):
    rows = []
    for name in CAPS:
        part = parts[name]
        dx = SPACING * SHELLS.index(name)
        faces = detected_caps(part)
        check(len(faces) == 1, "the " + name + " is detected as " + str(len(faces)) + " caps")
        check(len(detected_tubes(part)) == 0, "the " + name + " is detected as a tube")
        cap = Cap(part, faces[0])
        counts = set()
        cap_row = None
        for recombine in RECOMBINATIONS:
            seconds = mesh(part, recombine, size)
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            title = name + " at size " + str(size) + " with " + recombine
            check(len(messages) == 0, title + " wrote: " + str(messages))
            n, m, quality = check_cap_mesh(face, cap, title, recombine != "None")
            a, b, depth = block(n)
            print(title + ": a cap of", n, "round,", m, "on the seam, blocks of", a, "x", b, face.kinds(),
                  "worst scaled Jacobian", round(quality, 3), "in", round(seconds, 3), "s")
            if recombine in ("SimpleFullQuad", "BlossomFullQuad"):
                check(n % 2 == 0 and m % 2 == 0, title + " has an odd count, " + str(n) + " and " + str(m))
            else:
                counts.add((n, m))
            if recombine == "AllQuad":
                cap_row = describe(face, cap.poles) + (seconds,)
                plot("cap_" + name.replace(" ", "_") + "_" + str(size).replace(".", "_"), title + ": " + str(n) +
                     " round, " + str(m) + " on the seam, worst SJ " + str(round(quality, 3)), face, cap, dx)
        check(len(counts) == 1, "the " + name + " at size " + str(size) + " has different counts: " + str(counts))
        expected = EXPECTED.get((name, size))
        check(expected is None or counts == set([expected]), "the " + name + " at size " + str(size) + " is " +
              str(counts) + ", not " + str(expected))
        # The meshing before the caps
        seconds = mesh(part, "AllQuad", size, submapped=False)
        face = FaceMesh(part.name)
        before = describe(face, cap.poles)
        plot("cap_" + name.replace(" ", "_") + "_" + str(size).replace(".", "_") + "_before", name + " at size " +
             str(size) + " before, AllQuad: " + str(before[0]) + " quads, " + str(before[1]) + " triangles, worst SJ " +
             str(round(before[2], 3)) + ", irregular " + str(before[3]), face, cap, dx)
        rows.append((name, size) + tuple(round(v, 3) if isinstance(v, float) else v for v in before + (seconds,)) +
                    tuple(round(v, 3) if isinstance(v, float) else v for v in cap_row))
    return rows


def check_not_caps(parts):
    """The faces that are no caps are meshed as before the caps."""
    for name in NOT_CAPS:
        part = parts[name]
        check(len(detected_caps(part)) == 0, "the " + name + " is detected as a cap")
        for recombine in ("Simple", "AllQuad"):
            mesh(part, recombine, SIZES[0], submapped=False)
            before = FaceMesh(part.name)
            before_messages = output_messages(part.name)
            mesh(part, recombine, SIZES[0])
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            print(name + " with " + recombine + ":", face.kinds(), "irregular", len(irregular_inner_nodes(face.elements)),
                  "worst scaled Jacobian", round(min_scaled_jacobian(face), 3), messages)
            if name in LENSES:
                irregular = irregular_inner_nodes(face.elements)
                valence = valences(face.elements)
                check(face.kinds()[1] == 0 and len(irregular) == 2 and all(valence[node] == 3 for node in irregular)
                      and len(messages) == 0, "the " + name + " with " + recombine + " is not meshed as a lens face")
                continue
            check(face.kinds() == before.kinds() and face.points(face.used_nodes()) == before.points(before.used_nodes())
                  and messages == before_messages, "the " + name + " with " + recombine + " is not meshed as before the caps")
            check(not any("Cap mapping:" in message for message in messages), "the " + name + " wrote " + str(messages))


def check_pins(parts):
    part = parts["hemisphere"]
    cap = Cap(part, detected_caps(part)[0])
    rim = cap.rim[0]
    # 30 elements on the rim give 30 round
    mesh(part, "Simple", SIZES[0], pins=[(rim, 30)])
    n, m, quality = check_cap_mesh(FaceMesh(part.name), cap, "the hemisphere with 30 elements on its rim")
    check(n == 30 and len(output_messages(part.name)) == 0, "30 elements on the rim give " + str(n))
    print("hemisphere with 30 elements on its rim: a cap of", n, "round,", m, "on the seam, worst scaled Jacobian",
          round(quality, 3))
    # 25 and 6 leave it free with a message
    for count, words in ((25, "which must be even"), (6, "fewer than 8")):
        mesh(part, "Simple", SIZES[0], pins=[(rim, count)])
        face = FaceMesh(part.name)
        messages = output_messages(part.name)
        print("hemisphere with " + str(count) + " elements on its rim:", face.kinds(), messages)
        check(len(messages) == 1 and messages[0].startswith("Cap mapping:") and "is meshed free" in messages[0] and
              words in messages[0], str(count) + " elements on the rim must leave the hemisphere free with a message: " +
              str(messages))
        check(face.kinds()[1] > 0 or len(irregular_inner_nodes(face.elements)) != 4, "the hemisphere with " + str(count) +
              " elements on its rim is still a cap")
    # 9 on the seam give 9; 3 leave no ring
    mesh(part, "AllQuad", SIZES[0], pins=[(cap.seam, 9)])
    face = FaceMesh(part.name)
    n, m, quality = check_cap_mesh(face, cap, "the hemisphere with 9 elements on its seam")
    check(m == 9 and len(output_messages(part.name)) == 0, "9 elements on the seam give " + str(m))
    print("hemisphere with 9 elements on its seam: a cap of", n, "round,", m, "on the seam, worst scaled Jacobian",
          round(quality, 3))
    plot("cap_hemisphere_seam_9", "hemisphere, 9 on the seam: " + str(n) + " round, " + str(m) + " on the seam", face,
         cap, 0.0)
    mesh(part, "Simple", SIZES[0], pins=[(cap.seam, 3)])
    face = FaceMesh(part.name)
    messages = output_messages(part.name)
    print("hemisphere with 3 elements on its seam:", face.kinds(), messages)
    check(len(messages) == 1 and messages[0].startswith("Cap mapping:") and "leaves no ring" in messages[0],
          "3 elements on the seam must leave the hemisphere free with a message: " + str(messages))


def check_second_order(parts):
    part = parts["hemisphere"]
    cap = Cap(part, detected_caps(part)[0])
    mesh(part, "Simple", SIZES[0], second_order=True)
    face = FaceMesh(part.name)
    check(all(count == 8 for count in face.node_counts), "the second order hemisphere has elements of other than 8 " +
          "nodes: " + str(set(face.node_counts)))
    corners = face.used_nodes()
    n = len(rim_nodes(cap) & corners)
    m = len(nodes_on(cap.geometry_ids[cap.seam]) & corners) - 1
    a, b, depth = block(n)
    irregular = irregular_inner_nodes(face.elements)
    check(len(face.elements) == n * (m - depth) + a * b and len(irregular) == 4,
          "the second order hemisphere is not a cap: " + str(len(face.elements)) + " quads on " + str(n) + " and " + str(m))
    print("hemisphere: second order cap of", n, "round,", m, "on the seam, quads of 8 nodes")


def check_multi_face(parts):
    for name, (number_of_caps, number_of_tubes) in sorted(MULTI.items()):
        part = parts[name]
        caps = [Cap(part, face) for face in detected_caps(part)]
        tubes = [Tube(part, face) for face in detected_tubes(part)]
        check(len(caps) == number_of_caps and len(tubes) == number_of_tubes, "the " + name + " has " + str(len(caps)) +
              " caps and " + str(len(tubes)) + " tubes")
        geometry_ids = face_geometry_ids(part)
        structured = set([cap.face_id for cap in caps] + [tube.face_id for tube in tubes])
        for recombine in ("Simple", "AllQuad", "SimpleFullQuad", "BlossomFullQuad"):
            mesh(part, recombine, SIZES[0])
            messages = output_messages(part.name)
            check(len(messages) == 0, "the " + name + " with " + recombine + " wrote " + str(messages))
            rounds = set()
            for cap in caps:
                face = FaceMesh(part.name, elements_on(geometry_ids[cap.face_id]))
                n, m, quality = check_cap_mesh(face, cap, "the " + name + " face " + str(cap.face_id) + " with " + recombine)
                rounds.add(n)
                print(name + " with " + recombine + ": face", cap.face_id, "a cap of", n, "round,", m, "on the seam,",
                      "worst scaled Jacobian", round(quality, 3))
            for tube in tubes:
                face = FaceMesh(part.name, elements_on(geometry_ids[tube.face_id]))
                rims = [set().union(*[nodes_on(tube.geometry_ids[edge_id]) for edge_id in rim]) for rim in tube.rims]
                m = len(nodes_on(tube.geometry_ids[tube.seam])) - 1
                quads, triangles = face.kinds()
                quality = min_scaled_jacobian(face)
                check(triangles == 0 and len(rims[0]) == len(rims[1]) and quads == len(rims[0]) * m and
                      len(irregular_inner_nodes(face.elements)) == 0 and quality >= FLOOR,
                      "the " + name + " face " + str(tube.face_id) + " with " + recombine + " is no tube: " +
                      str(face.kinds()) + " on " + str(len(rims[0])) + " and " + str(len(rims[1])))
                rounds.add(len(rims[0]))
                print(name + " with " + recombine + ": face", tube.face_id, "a tube of", len(rims[0]), "x", m,
                      "worst scaled Jacobian", round(quality, 3))
            check(len(rounds) == 1, "the " + name + " with " + recombine + " has the counts round " + str(sorted(rounds)))
            whole = FaceMesh(part.name)
            irregular = irregular_inner_nodes(whole.elements)
            for face_id in sorted(set(geometry_ids) - structured):
                free = FaceMesh(part.name, elements_on(geometry_ids[face_id]))
                print(name + " with " + recombine + ": free face", face_id, free.kinds(), "on",
                      len(boundary_nodes(free.elements)), "boundary nodes, worst scaled Jacobian",
                      round(min_scaled_jacobian(free), 3))
                check(len(boundary_nodes(free.elements)) in rounds, "the free face of the " + name + " does not carry " +
                      str(sorted(rounds)) + " round")
                if recombine != "Simple":
                    check(free.kinds()[1] == 0, "the free face of the " + name + " keeps triangles with " + recombine)
            if name == "pressure vessel":
                check(whole.kinds()[1] == 0 and len(irregular) == 8, "the pressure vessel with " + recombine + " has " +
                      str(whole.kinds()) + " and " + str(len(irregular)) + " irregular inner nodes, not 8")


def check_needle(parts):
    """The needle is a cap although gmsh cannot mesh it well, or at all, and its apex leaves the grid below 0.2."""
    part = parts["needle cone"]
    dx = SPACING * SHELLS.index("needle cone")
    faces = detected_caps(part)
    check(len(faces) == 1, "the needle cone is detected as " + str(len(faces)) + " caps")
    cap = Cap(part, faces[0])
    for size in SIZES:
        for recombine in ("Simple", "AllQuad", "SimpleFullQuad"):
            mesh(part, recombine, size)
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            title = "needle cone at size " + str(size) + " with " + recombine
            check(len(messages) == 0, title + " wrote: " + str(messages))
            n, m, quality = check_cap_mesh(face, cap, title, floor=CAP_FLOOR)
            print(title + ": a cap of", n, "round,", m, "on the seam,", face.kinds(), "worst scaled Jacobian",
                  round(quality, 3))
            if recombine == "AllQuad":
                plot("cap_needle_cone_" + str(size).replace(".", "_"), title + ": " + str(n) + " round, " + str(m) +
                     " on the seam, worst SJ " + str(round(quality, 3)), face, cap, dx)


def check_quasi_structured(parts):
    """With the quasi-structured quad algorithm a cap is left to gmsh."""
    part = parts["hemisphere"]
    cap = Cap(part, detected_caps(part)[0])
    for recombine in ("Simple", "AllQuad"):
        mesh(part, recombine, SIZES[0], algorithm_2d="QuasiStructuredQuad")
        face = FaceMesh(part.name)
        messages = output_messages(part.name)
        quads, triangles = face.kinds()
        print("hemisphere with QuasiStructuredQuad and " + recombine + ":", face.kinds(), "irregular",
              len(irregular_inner_nodes(face.elements)), "worst scaled Jacobian",
              round(min_scaled_jacobian(face, cap.poles), 3), messages)
        check(not any("Cap mapping:" in message for message in messages), "the hemisphere with QuasiStructuredQuad " +
              "and " + recombine + " wrote " + str(messages))
        check(quads > 0 and triangles == 0, "the hemisphere with QuasiStructuredQuad and " + recombine + " has " +
              str(face.kinds()))


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    rows = []
    for size in SIZES:
        rows.extend(check_caps(parts, size))
    print("face | size | before quads | before triangles | before worst SJ | before irregular | before shortest | " +
          "before longest | before time s | cap quads | cap triangles | cap worst SJ | cap irregular | cap shortest | " +
          "cap longest | cap time s")
    for row in rows:
        print(" | ".join(str(v) for v in row))
    check_not_caps(parts)
    check_pins(parts)
    check_second_order(parts)
    check_multi_face(parts)
    check_needle(parts)
    check_quasi_structured(parts)
    print("TEST PASSED")


main()
