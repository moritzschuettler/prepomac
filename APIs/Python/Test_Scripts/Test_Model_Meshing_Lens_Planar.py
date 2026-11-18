#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the lens faces of planar shapes and of a cap of a sphere. Every face of one boundary loop with two corners, one
#corner or none is a lens face on a shell with transfinite_submapped, the default. With two corners one inner line
#splits it into two three-sided pieces of three blocks of quads, two inner nodes of three quads; with no corner a first
#line across it splits it into two such faces, four inner nodes of three quads; with one corner a line across the
#corner leaves a three-sided piece at the corner and a two-sided rest, three inner nodes of three quads. A lens face
#with no straight edge is not submapped at all. A face the submapping keeps as well is meshed as the kind whose grid,
#laid on the face alone on the counts that kind asks for, is better: submapped when its worst corner is 0.5 at least,
#otherwise the better of the two. Once the pieces are laid, the nodes inside the face are smoothed
#together, so that the grid lines run on across the inner lines. A thin lens face, whose line would have to carry far
#more elements than its length asks for because its sides carry different counts (an arc is given more elements than
#its chord), asks the balance for equal counts on its two sides.
#
#  - lens_planar.brep: one part per shape side by side, at x = 150 i, in the plane y = 0: the tombstones 40 x 20,
#    20 x 60 and 60 x 10 (a rectangle closed by a half circle), the circular segments with corners of 60 and of 30
#    degrees (chord 34.64 and 40), the lenses of two arcs with corners of 120 and 40 degrees, the parabolic arches
#    60 x 30 and 30 x 80 (chord and a B-spline of degree 2), the triangle of 15, 15 and 150 degrees, the half disc of
#    r 3, the crescents 12 and 6 thick between two arcs over a chord of 40 (moon 8 and moon 14 by the sagitta of the
#    inner arc), the half disc with a kink of 170 degrees in its chord; then the faces of no corner - the disc of r 20
#    (one closed edge), the discs of r 20 of three arcs and of four uneven arcs (30, 140, 80 and 110 degrees), the disc
#    of r 4, the ellipses 60 x 30 and 80 x 20 and the egg of two half ellipses (four rational B-splines each), the
#    stadiums 80 x 20 and 45 x 30, the triangle of side 50 and the square of 40 with corners rounded by r 8 and r 15 -
#    and of one corner - the teardrops of tip angles 30, 60, 90 and 120 degrees (an arc of r 12 or 15 between two
#    tangent lines) and the half disc of r 20 with one corner rounded by r 5 -, and the cap of the sphere of r 20 cut
#    off by a plane 6 from its rim, bounded by one closed circle away from the seam and the poles of the sphere.
#  - Detection: every shape is one lens face of its number of corners; the tombstones, the lens of 120, the small half
#    disc, moon 8, the kinked half disc, the disc of four arcs, the ellipses, the egg, the stadiums, the rounded square
#    and the half disc with one fillet are submapped faces as well.
#  - At the element sizes 5 and 3.5 under Simple, QuadDominant, None and SimpleFullQuad every shape is meshed without a
#    message: quads only, two triangles per quad of Simple under None; no fold, a worst scaled Jacobian of 0.2 at least
#    measured in space against the normals of the mesh, and the inner nodes of three quads of its kind (2 with two
#    corners, 3 with one, 4 with none) and no other inner node without four. The full-quad recombination makes the
#    boundary even. The thin crescent (moon 14), whose grid folded and was meshed free with the lens message before the
#    equal counts, is a lens grid. The lens of 120, moon 8, the disc of four arcs, the ellipses and the egg have no
#    straight edge and are lens faces only, on their own counts (the disc of four arcs: 76 quads at size 5, not the 292
#    it took on the counts of the submapping). The half disc with one fillet is a submapped face its counts cannot
#    balance; it is balanced as a lens face instead, without a submapping message.
#  - Submapped or lens: no submapped grid of a submapped shape here reaches a worst corner of 0.5 (the tombstone
#    20 x 60, the stadium 80 x 20 and the small half disc kept one of 0.383 at size 5, the stadium 45 x 30 and the
#    rounded square 0.259), and its lens grid is better, so every shape is a lens grid on its own counts. The
#    tombstone 20 x 60, the small half disc, the stadiums and the rounded square are checked under QuadDominant at the
#    sizes 5, 3.5 and 2.5: a lens grid of their kind at every size, no message, a worst corner of 0.5 at least.
#  - Thin faces: the segment of 30, the obtuse triangle and the lens of 40 carry equal counts on their two sides under
#    Simple and QuadDominant, and their shortest element edge is 0.2 of the size at least (before the equal counts: 0.77
#    and 0.42 at size 5).
#  - Smoothing: on the tombstones 40 x 20 and 60 x 10 a grid line crossing the inner line (the axis of the shape) bends
#    there by 9 degrees at most under Simple and QuadDominant (each piece smoothed on its own: 13.7 to 16.1 degrees).
#  - Pins: the chord of the segment of 30 set to 12 elements keeps 12, the arc follows it to 12, and the face is a lens
#    grid without a message; the arcs of the thin crescent set to 13 and 11 elements, counts its grid folds on, keep
#    them and leave it free with the lens message and no fold. The circle of the disc set to 13 elements, an odd total,
#    and to 6, fewer than 8, leaves it free with the lens message.
#  - Second order: the disc with Simple is quads of 8 nodes only, four inner nodes of three quads.
#  - The quasi-structured quad algorithm lays its own quads, so with it the disc is left to gmsh: quads only, without a
#    lens message.
#  - Table: per shape at size 5 under QuadDominant, the mesh with transfinite_submapped=False (gmsh's free mesh) against
#    the mesh with it.
#  The model was made by gmsh's OCC kernel through GmshCommon: lines, circle arcs and B-splines, one plane surface each,
#  and the sphere intersected with a box (generator Models/Generators/Make_Lens_Planar.py).
#  With the environment variable PMX_LENS_PLOT_DIR set, the meshes are written into that folder as PNG images, the
#  inner nodes without four quads marked red.
#
#  PrePoMax.com -f lens_planar.brep -u MM_TON_S_C --script Test_Model_Meshing_Lens_Planar.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import time
import clr
clr.AddReference('CaeMesh')
clr.AddReference('System.Drawing')
from CaeMesh import LensFaceDetector, SubmapFaceDetector
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

PLOT_DIR = os.environ.get("PMX_LENS_PLOT_DIR")
SIZES = [5.0, 3.5]
RECOMBINATIONS = ["Simple", "QuadDominant", "None", "SimpleFullQuad"]
SPACING = 150.0
FLOOR = 0.2
TRANSFINITE_ANGLE = 135.0
SHELLS = ["tombstone 40 x 20", "tombstone 20 x 60", "tombstone 60 x 10", "segment 60", "segment 30", "vesica 120",
          "vesica 40", "arch 60 x 30", "arch 30 x 80", "obtuse triangle", "small half disc", "moon 8", "moon 14",
          "kinked half disc", "disc", "disc of 3 arcs", "disc of 4 arcs", "small disc", "ellipse 60 x 30",
          "ellipse 80 x 20", "egg", "stadium 80 x 20", "stadium 45 x 30", "rounded triangle", "rounded square",
          "teardrop 30", "teardrop 60", "teardrop 90", "teardrop 120", "half disc one fillet", "sphere cap"]
NO_CORNER = ["disc", "disc of 3 arcs", "disc of 4 arcs", "small disc", "ellipse 60 x 30", "ellipse 80 x 20", "egg",
             "stadium 80 x 20", "stadium 45 x 30", "rounded triangle", "rounded square", "sphere cap"]
ONE_CORNER = ["teardrop 30", "teardrop 60", "teardrop 90", "teardrop 120", "half disc one fillet"]
# The inner nodes of three quads of a lens grid by its number of corners
IRREGULAR = {0: 4, 1: 3, 2: 2}
SUBMAPPED_LENSES = ["tombstone 40 x 20", "tombstone 20 x 60", "tombstone 60 x 10", "vesica 120", "small half disc",
                    "moon 8", "kinked half disc", "disc of 4 arcs", "ellipse 60 x 30", "ellipse 80 x 20", "egg",
                    "stadium 80 x 20", "stadium 45 x 30", "rounded square", "half disc one fillet"]
# The submapped shapes whose submapped grid was kept at some size before the kind was chosen by the better grid, the
# sizes they keep one kind at, and the worst corner their lens grid reaches at least
SAME_KIND = ["tombstone 20 x 60", "small half disc", "stadium 80 x 20", "stadium 45 x 30", "rounded square"]
SAME_KIND_SIZES = [5.0, 3.5, 2.5]
MIN_CHOSEN_LENS = 0.5
THIN = ["segment 30", "obtuse triangle", "vesica 40"]
SHORTEST_SHARE = 0.2
# The x of the inner line of the tombstones, from the start of the part
AXES = {"tombstone 40 x 20": 20.0, "tombstone 60 x 10": 30.0}
MAX_AXIS_BEND = 9.0
# The inner arc of the crescents, through (0, 0), (40, 0) and (20, sagitta): centre (20, c), radius r
MOON_14_CENTRE_Y = (14.0 ** 2 - 20.0 ** 2) / (2 * 14.0)
MOON_14_RADIUS = 14.0 - MOON_14_CENTRE_Y


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


def detected(part):
    """The lens faces and the submapped faces the detection finds on the part."""
    geometry, geometry_part_ = geometry_part(part)
    lenses = list(LensFaceDetector.Detect(geometry, geometry_part_, TRANSFINITE_ANGLE))
    submapped = list(SubmapFaceDetector.Detect(geometry, geometry_part_, TRANSFINITE_ANGLE))
    return lenses, submapped


def output_messages(part_name):
    """The lens, transfinite, submapping and quad-dominant lines the last meshing of the part wrote to the output
    window."""
    form = main_form()
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    result = []
    for line in lines[starts[-1]:]:
        for marker in ("Lens mapping:", "Transfinite:", "Submapping:", "Quad-dominant meshing:"):
            if marker in line:
                result.append(line[line.index(marker):])
    return result


def edge_ids(part):
    """The 1-based edge ids of the part with their lengths and centroids."""
    data = part.get_edge_data()
    return list(zip([int(i) for i in data.ids], [float(v) for v in data.lengths], [list(c) for c in data.centroids]))


def mesh(part, recombine, size, pins=(), submapped=True, second_order=False, algorithm_2d=None):
    """Meshes the part by a shell item; pins are (edge id, number of elements). Returns the time it took."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Lens_Size",
                                 pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
                                 relative_size=False, max_size=size, min_size=size / 10, second_order=second_order)
    names = ["PyTest_Lens_Size"]
    for edge_id, count in pins:
        name = "PyTest_Lens_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])
    if algorithm_2d is None:
        items.add_shell_gmsh("PyTest_Lens", selection, algorithm_recombine=recombine, transfinite_submapped=submapped)
    else:
        items.add_shell_gmsh("PyTest_Lens", selection, algorithm_2d=algorithm_2d, algorithm_recombine=recombine,
                             transfinite_submapped=submapped)
    names.append("PyTest_Lens")
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

    def edge_lengths(self):
        lengths = []
        for element in self.elements:
            for k in range(len(element)):
                p, q = self.xyz[element[k]], self.xyz[element[(k + 1) % len(element)]]
                lengths.append(math.sqrt(sum((p[c] - q[c]) ** 2 for c in range(3))))
        return min(lengths), max(lengths)

    def boundary_segments(self):
        return [edge for edge, count in edge_uses(self.elements).items() if count == 1]

    def side_counts(self, on_side_a):
        """The number of boundary segments with both nodes on side A, by the predicate on node coordinates, and the
        number of the others."""
        segments = self.boundary_segments()
        a = sum(1 for p, q in segments if on_side_a(self.xyz[p]) and on_side_a(self.xyz[q]))
        return a, len(segments) - a


def edge_uses(elements):
    uses = {}
    for element in elements:
        for k in range(len(element)):
            edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return uses


def boundary_nodes(elements):
    return set(node for edge, count in edge_uses(elements).items() if count == 1 for node in edge)


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


def node_normals(face):
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
            product = cross(e1, e2)
            length = math.sqrt(sum(v * v for v in e1) * sum(v * v for v in e2))
            sine = sum(product[k] * normals[element[c]][k] for k in range(3)) / length if length > 0 else -1.0
            worst = min(worst, sine * (2 / math.sqrt(3) if n == 3 else 1.0))
    return worst


def axis_bend(face, x_axis):
    """The largest bend in degrees, at an inner node of four quads within 1 of the axis x = x_axis, of the grid line
    that crosses the axis there: the line through its neighbours furthest apart along x."""
    boundary = boundary_nodes(face.elements)
    neighbours = {}
    for element in face.elements:
        for k in range(len(element)):
            neighbours.setdefault(element[k], set()).update([element[(k + 1) % len(element)], element[k - 1]])
    worst = 0.0
    for node, count in valences(face.elements).items():
        if node in boundary or count != 4 or abs(face.xyz[node][0] - x_axis) > 1.0:
            continue
        around = sorted(neighbours[node], key=lambda other: face.xyz[other][0])
        p, q, r = face.xyz[node], face.xyz[around[0]], face.xyz[around[-1]]
        u = [q[k] - p[k] for k in range(3)]
        v = [r[k] - p[k] for k in range(3)]
        cosine = -sum(u[k] * v[k] for k in range(3)) / math.sqrt(sum(x * x for x in u) * sum(x * x for x in v))
        worst = max(worst, math.degrees(math.acos(max(-1.0, min(1.0, cosine)))))
    return worst


def describe(face):
    quads, triangles = face.kinds()
    shortest, longest = face.edge_lengths()
    return (quads, triangles, min_scaled_jacobian(face), len(irregular_inner_nodes(face.elements)), shortest, longest)


# Plots ----------------------------------------------------------------------------------------------------------------
def plot(file_name, title, face):
    """The elements seen along the axis of the smallest extent of the part, the x-z plane of the planar shapes; quads
    blue, triangles orange, inner nodes without four quads red."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    points = [face.xyz[node] for node in face.used_nodes()]
    extents = [max(p[k] for p in points) - min(p[k] for p in points) for k in range(3)]
    axis = extents.index(min(extents))
    i0, i1 = [k for k in range(3) if k != axis]
    xs = [face.xyz[node][i0] for node in face.used_nodes()]
    ys = [face.xyz[node][i1] for node in face.used_nodes()]
    scale = 700.0 / max(max(xs) - min(xs), max(ys) - min(ys))
    width, height = int((max(xs) - min(xs)) * scale + 40), int((max(ys) - min(ys)) * scale + 70)
    bitmap = Bitmap(width, height)
    graphics = Graphics.FromImage(bitmap)
    graphics.Clear(Color.White)
    pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
    quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
    triangle_brush = SolidBrush(Color.FromArgb(246, 190, 160))

    def to_image(node):
        p = face.xyz[node]
        return PointF(float(20 + (p[i0] - min(xs)) * scale), float(height - 20 - (p[i1] - min(ys)) * scale))

    for element in face.elements:
        polygon = Array[PointF]([to_image(node) for node in element])
        graphics.FillPolygon(quad_brush if len(element) == 4 else triangle_brush, polygon)
        graphics.DrawPolygon(pen, polygon)
    for node in irregular_inner_nodes(face.elements):
        p = to_image(node)
        graphics.FillEllipse(Brushes.Firebrick, p.X - 3, p.Y - 3, 6.0, 6.0)
    graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, file_name + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


# Checks ---------------------------------------------------------------------------------------------------------------
def number_of_corners(name):
    return 0 if name in NO_CORNER else 1 if name in ONE_CORNER else 2


def check_detection(parts):
    """Every shape is one lens face of its number of corners, and the submapped ones are submapped faces as well."""
    for name in SHELLS:
        lenses, submapped = detected(parts[name])
        corners = number_of_corners(name)
        kind = sum(1 for face in lenses if face.NumberOfSides == corners)
        lens_ids = set(face.FaceId for face in lenses)
        submapped_lenses = sum(1 for face in submapped if face.FaceId in lens_ids)
        if corners == 2:
            print(name + ": two-sided lens faces", kind, "submapped lens faces", submapped_lenses)
        else:
            print(name + ": lens faces of " + str(corners) + " corners", kind, "submapped lens faces", submapped_lenses)
        check(len(lenses) == 1 and kind == 1, "the " + name + " is detected as " + str(len(lenses)) + " lens faces, " +
              str(kind) + " of " + str(corners) + " corners")
        expected = 1 if name in SUBMAPPED_LENSES else 0
        check(submapped_lenses == expected, "the " + name + " has " + str(submapped_lenses) + " submapped lens faces")


def on_chord(p):
    """Whether a node lies on the side along z = 0 of a segment or the triangle, or on the lower arc of a lens."""
    return p[2] < 1E-6


def check_shapes(parts, size):
    rows = []
    for name in SHELLS:
        part = parts[name]
        dx = SPACING * SHELLS.index(name)
        expected_irregular = IRREGULAR[number_of_corners(name)]
        row = None
        simple_quads = None
        for recombine in RECOMBINATIONS:
            seconds = mesh(part, recombine, size)
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            title = name + " at size " + str(size) + " with " + recombine
            check(len(messages) == 0, title + " wrote: " + str(messages))
            quads, triangles = face.kinds()
            quality = min_scaled_jacobian(face)
            shortest, longest = face.edge_lengths()
            if recombine == "None":
                check(quads == 0 and triangles == 2 * simple_quads and quality > 0, title + " has " +
                      str(face.kinds()) + ", not two triangles per quad of Simple (" + str(simple_quads) + "), worst " +
                      "scaled Jacobian " + str(quality))
                print(title + ":", face.kinds(), "worst scaled Jacobian", round(quality, 3), "edges",
                      round(shortest, 2), "-", round(longest, 2), "in", round(seconds, 3), "s")
                continue
            irregular = irregular_inner_nodes(face.elements)
            valence = valences(face.elements)
            check(triangles == 0 and len(irregular) == expected_irregular and
                  all(valence[node] == 3 for node in irregular),
                  title + " has " + str(face.kinds()) + " and the irregular inner nodes " +
                  str(sorted(valence[node] for node in irregular)) + ", not " + str(expected_irregular) + " of 3")
            check(quality >= FLOOR, title + " has a corner of the scaled Jacobian " + str(quality))
            if recombine == "Simple":
                simple_quads = quads
            if recombine == "SimpleFullQuad":
                boundary = boundary_nodes(face.elements)
                check(len(boundary) % 2 == 0, title + " has an odd boundary of " + str(len(boundary)))
                print(title + ":", face.kinds(), "irregular", len(irregular), "worst scaled Jacobian",
                      round(quality, 3), "edges", round(shortest, 2), "-", round(longest, 2), "in", round(seconds, 3),
                      "s")
                continue
            words = ""
            if name in THIN:
                sides = face.side_counts(on_chord)
                check(sides[0] == sides[1], title + " carries " + str(sides) + " elements on its sides, not equal")
                check(shortest >= SHORTEST_SHARE * size, title + " has an element edge of " + str(shortest) +
                      ", shorter than " + str(SHORTEST_SHARE * size))
                words += " sides " + str(sides)
            if name in AXES:
                bend = axis_bend(face, dx + AXES[name])
                check(bend <= MAX_AXIS_BEND, title + ": a grid line bends by " + str(bend) + " degrees at its axis")
                words += " axis bend " + str(round(bend, 2))
            print(title + ":", face.kinds(), "irregular", len(irregular), "worst scaled Jacobian", round(quality, 3),
                  "edges", round(shortest, 2), "-", str(round(longest, 2)) + words, "in", round(seconds, 3), "s")
            if recombine == "QuadDominant":
                row = describe(face)
                plot("lens_planar_" + name.replace(" ", "_") + "_" + str(size).replace(".", "_"), title + ": " +
                     str(quads) + " quads, worst SJ " + str(round(quality, 3)), face)
        if size == SIZES[0]:
            mesh(part, "QuadDominant", size, submapped=False)
            before = describe(FaceMesh(part.name))
            rows.append((name,) + tuple(round(v, 3) if isinstance(v, float) else v for v in before + row))
    return rows


def check_same_kind(parts):
    """The submapped shapes whose submapped grid was kept at some sizes and refused at others are lens grids at every
    size, better than their submapped grids."""
    for name in SAME_KIND:
        part = parts[name]
        expected_irregular = IRREGULAR[number_of_corners(name)]
        for size in SAME_KIND_SIZES:
            seconds = mesh(part, "QuadDominant", size)
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            irregular = irregular_inner_nodes(face.elements)
            valence = valences(face.elements)
            quality = min_scaled_jacobian(face)
            title = name + " at size " + str(size) + " with QuadDominant"
            print(title + ":", face.kinds(), "irregular", len(irregular), "worst scaled Jacobian", round(quality, 3),
                  "in", round(seconds, 3), "s")
            check(len(messages) == 0, title + " wrote: " + str(messages))
            check(face.kinds()[1] == 0 and len(irregular) == expected_irregular and
                  all(valence[node] == 3 for node in irregular),
                  title + " is not a lens grid: " + str(face.kinds()) + " and the irregular inner nodes " +
                  str(sorted(valence[node] for node in irregular)))
            check(quality >= MIN_CHOSEN_LENS, title + " has a corner of the scaled Jacobian " + str(quality) +
                  ", below " + str(MIN_CHOSEN_LENS))


def check_pins(parts):
    """A pinned edge of a thin lens face keeps its count: the other side follows it where it can, and a face its
    counts leave no grid for is meshed free with the lens message."""
    name = "segment 30"
    part = parts[name]
    chord = min(edge_ids(part), key=lambda edge: abs(edge[2][2]))[0]
    mesh(part, "QuadDominant", SIZES[0], pins=[(chord, 12)])
    face = FaceMesh(part.name)
    messages = output_messages(part.name)
    sides = face.side_counts(on_chord)
    irregular = irregular_inner_nodes(face.elements)
    print(name + " with its chord set to 12 elements:", face.kinds(), "sides", sides, "irregular", len(irregular),
          messages)
    check(sides == (12, 12) and len(irregular) == 2 and face.kinds()[1] == 0 and len(messages) == 0,
          "the " + name + " with its chord set to 12 has the sides " + str(sides) + ", " + str(face.kinds()) + ", " +
          str(len(irregular)) + " irregular inner nodes and the messages " + str(messages))
    #
    name = "moon 14"
    part = parts[name]
    dx = SPACING * SHELLS.index(name)
    arcs = sorted(edge_ids(part), key=lambda edge: edge[1])
    mesh(part, "QuadDominant", SIZES[0], pins=[(arcs[1][0], 13), (arcs[0][0], 11)])
    face = FaceMesh(part.name)
    messages = output_messages(part.name)

    def on_inner_arc(p):
        return abs(math.sqrt((p[0] - dx - 20.0) ** 2 + (p[2] - MOON_14_CENTRE_Y) ** 2) - MOON_14_RADIUS) < 1E-3

    inner, outer = face.side_counts(on_inner_arc)
    quality = min_scaled_jacobian(face)
    print(name + " with its arcs set to 13 and 11 elements:", face.kinds(), "arcs", (outer, inner), "worst scaled " +
          "Jacobian", round(quality, 3), messages)
    check((outer, inner) == (13, 11), "the " + name + " with its arcs set to 13 and 11 carries " + str((outer, inner)))
    check(len(messages) == 1 and messages[0].startswith("Lens mapping:") and "is meshed free" in messages[0] and
          quality > 0, "the " + name + " with its arcs set to 13 and 11 must be meshed free with the lens message " +
          "and no fold: " + str(messages) + ", " + str(quality))
    # The disc of one closed edge set to an odd total and to fewer than 8 elements
    name = "disc"
    part = parts[name]
    circle = edge_ids(part)[0][0]
    for count, words in ((13, "which must be even"), (6, "fewer than 8")):
        mesh(part, "Simple", SIZES[0], pins=[(circle, count)])
        face = FaceMesh(part.name)
        messages = output_messages(part.name)
        quality = min_scaled_jacobian(face)
        irregular = irregular_inner_nodes(face.elements)
        print(name + " with its circle set to " + str(count) + " elements:", face.kinds(), "irregular",
              len(irregular), "boundary", len(face.boundary_segments()), messages)
        check(len(face.boundary_segments()) == count and len(messages) == 1 and
              messages[0].startswith("Lens mapping:") and "is meshed free" in messages[0] and words in messages[0] and
              quality > 0, "the " + name + " with its circle set to " + str(count) + " must be meshed free with the " +
              "lens message and no fold: " + str(messages) + ", " + str(quality))
        check(face.kinds()[1] > 0 or len(irregular) != 4, "the " + name + " with its circle set to " + str(count) +
              " is still a lens grid")


def check_second_order(parts):
    part = parts["disc"]
    mesh(part, "Simple", SIZES[0], second_order=True)
    face = FaceMesh(part.name)
    irregular = irregular_inner_nodes(face.elements)
    check(all(count == 8 for count in face.node_counts) and len(irregular) == 4,
          "the second order disc has elements of " + str(set(face.node_counts)) + " nodes and " + str(len(irregular)) +
          " irregular inner nodes")
    print("disc: second order lens face of", len(face.elements), "quads of 8 nodes")


def check_quasi_structured(parts):
    part = parts["disc"]
    for recombine in ("Simple", "QuadDominant"):
        mesh(part, recombine, SIZES[0], algorithm_2d="QuasiStructuredQuad")
        face = FaceMesh(part.name)
        messages = output_messages(part.name)
        quads, triangles = face.kinds()
        print("disc with QuasiStructuredQuad and " + recombine + ":", face.kinds(), "irregular",
              len(irregular_inner_nodes(face.elements)), messages)
        check(not any(message.startswith("Lens mapping:") for message in messages), "the disc with " +
              "QuasiStructuredQuad and " + recombine + " wrote " + str(messages))
        check(quads > 0 and triangles == 0, "the disc with QuasiStructuredQuad and " + recombine + " has " +
              str(face.kinds()))


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    check_detection(parts)
    rows = []
    for size in SIZES:
        rows.extend(check_shapes(parts, size))
    print("shape | free quads | free triangles | free worst SJ | free irregular | free shortest | free longest | " +
          "quads | triangles | worst SJ | irregular | shortest | longest")
    for row in rows:
        print(" | ".join(str(v) for v in row))
    check_same_kind(parts)
    check_pins(parts)
    check_second_order(parts)
    check_quasi_structured(parts)
    print("TEST PASSED")


main()
