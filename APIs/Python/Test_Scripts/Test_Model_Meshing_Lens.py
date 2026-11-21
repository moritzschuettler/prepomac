#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the lens faces and the pole chart. A shell face of one boundary loop with exactly two corners (vertices at the
#transfinite angle or sharper) is a lens face: one inner line from a node of one side to a node of the other splits it
#into two three-sided pieces of three blocks of quads each, so its grid has two inner nodes of three quads; one with no
#corner and two vertices (a half sphere between two meridians, both vertices poles at 180 degrees) is first split by a
#line between its vertices into two lens faces whose lines meet it at one node, four inner nodes of three quads. A side
#with a pole of the surface inside it is split there. A piece with a pole at a corner, and a three-sided face with a pole
#at a corner, are mapped in the pole chart (conformal, regular at the pole) and in the parameters of the surface, the
#better grid kept. The angle at a vertex is the interior angle of the face where it differs from the angle of its edges
#by more than 30 degrees: the apex of a cone has its developed angle. A reentrant vertex leaves the face unstructured,
#but for a reentrant pole, at which the face is split into pieces of about a right angle (see also
#Test_Model_Meshing_Pole_Faces).
#
#  - lens_faces.brep: one part per shape side by side, r 20 unless said - the hemisphere revolved by 180, 90, 60, 120,
#    30 and 270 degrees (dome 180 ...: a pole inside a side of the half dome, a three-sided face for 90 to 30, a
#    reentrant pole of 270 degrees), the sphere between two meridians 90, 180 and 45 degrees apart (lunes: poles at
#    both corners, a zero-sided face for 180), the cone of 45 degrees revolved by 180, 90 and 270 degrees and the one of
#    15 degrees (r 10) by 180 (cone sectors: the apex a corner of 127, 60 and 30 degrees, inside a side at 191), the
#    paraboloid revolved by 180, the planar half disc (submapped, whose grid is refused, then a lens face), the full
#    sphere cut by planes through its axis to a quarter and to a half (two faces each, split at the seam of the sphere),
#    the 2:1 elliptical head revolved by 180; parts of several faces: the pressure vessel (two domes on a cylinder 40
#    long) revolved by 180 and by 90 degrees, and the cone of 45 degrees revolved by 180 closed by the half disc.
#  - At the element sizes 5 and 3.5 and the recombinations None, Simple, QuadDominant and SimpleFullQuad every shape is meshed
#    without a message: quads only (two triangles per quad without recombination), the number of inner nodes of three
#    quads of its kind (1 three-sided, 2 two-sided, 4 zero-sided, 3 for the dome of 270 degrees, split at its pole into
#    three pieces, their sum for the parts) and no other inner node without four, no fold, a worst scaled Jacobian of
#    the quads of 0.2 at least measured in space against the normals of the mesh - a corner at a pole against the
#    normal of its element; the triangles without recombination only unfolded; the full-quad recombination makes the
#    counts even. (The dome of 270 degrees was meshed free before, and a full-quad recombination of it failed in gmsh:
#    "Full-quad recombination not ready yet for periodic surfaces".)
#  - Table: per shape at size 5 under QuadDominant, the mesh with transfinite_submapped=False (gmsh's free mesh of a
#    lens face; the three-sided faces are structured either way) against the mesh with it.
#  - Pins on the half dome: its edges set to an odd total, and its rim to 3 elements, leave it free with a message.
#  - Second order: the half dome with Simple is quads of 8 nodes only, two inner nodes of three quads.
#  - The quasi-structured quad algorithm lays its own quads round a pole, so with it a lens face is left to gmsh: the
#    half dome is meshed, quads only, without a lens message.
#  The models were made by gmsh's OCC kernel through GmshCommon: arcs, lines and B-splines revolved, the sphere cut by
#  intersecting it with a wedge, the faces of a part fragmented with each other.
#  With the environment variable PMX_LENS_PLOT_DIR set, the meshes are written into that folder as PNG images, each
#  part seen along the axis of its smallest extent, the inner nodes without four quads marked red.
#
#  PrePoMax.com -f lens_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_Lens.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import time
import clr
clr.AddReference('CaeMesh')
clr.AddReference('System.Drawing')
from CaeMesh import LensFaceDetector, SubmapFaceDetector, TransfiniteFaceDetector, TransfiniteThreeSidedEnum
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

PLOT_DIR = os.environ.get("PMX_LENS_PLOT_DIR")
SIZES = [5.0, 3.5]
RECOMBINATIONS = ["None", "Simple", "QuadDominant", "SimpleFullQuad"]
SPACING = 150.0
FLOOR = 0.2
TRANSFINITE_ANGLE = 135.0
SHELLS = ["dome 180", "dome 90", "dome 60", "dome 120", "dome 30", "dome 270", "lune 90", "lune 180", "lune 45",
          "cone 45 half", "cone 15 half", "cone 45 quarter", "cone 45 270", "paraboloid half", "half disc",
          "quarter sphere", "half sphere", "elliptical head half", "half vessel", "quarter vessel", "closed half cone"]
# By shape: the three-sided, two-sided and zero-sided faces, and the submapped faces among the lens faces
KINDS = {"dome 180": (0, 1, 0), "dome 90": (1, 0, 0), "dome 60": (1, 0, 0), "dome 120": (1, 0, 0),
         "dome 30": (1, 0, 0), "lune 90": (0, 1, 0), "lune 180": (0, 0, 1), "lune 45": (0, 1, 0),
         "cone 45 half": (1, 0, 0), "cone 15 half": (1, 0, 0), "cone 45 quarter": (1, 0, 0), "cone 45 270": (0, 1, 0),
         "paraboloid half": (0, 1, 0), "half disc": (0, 1, 0), "quarter sphere": (0, 2, 0), "half sphere": (0, 2, 0),
         "elliptical head half": (0, 1, 0), "half vessel": (0, 2, 0), "quarter vessel": (2, 0, 0),
         "closed half cone": (1, 1, 0), "dome 270": (0, 1, 0)}
SUBMAPPED_LENSES = {"half disc": 1, "cone 45 270": 1, "closed half cone": 1}
# By shape: the inner nodes of three quads of a lens face split at a reentrant pole, one per piece
REENTRANT_IRREGULAR = {"dome 270": 3}


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
    """The three-sided faces, the lens faces and the submapped faces the detection finds on the part."""
    geometry, geometry_part_ = geometry_part(part)
    transfinite = list(TransfiniteFaceDetector.Detect(geometry, geometry_part_, TransfiniteThreeSidedEnum.Mapped, True,
                                                      TRANSFINITE_ANGLE))
    lenses = list(LensFaceDetector.Detect(geometry, geometry_part_, TRANSFINITE_ANGLE))
    submapped = list(SubmapFaceDetector.Detect(geometry, geometry_part_, TRANSFINITE_ANGLE))
    return transfinite, lenses, submapped


def poles(part, dx):
    """The vertices of the part on the axis of its shape, through (dx + 20, 0): its poles and apexes."""
    return [tuple(c) for c in part.get_vertex_data().coordinates if abs(c[0] - dx - 20) < 1E-6 and abs(c[1]) < 1E-6]


def output_messages(part_name):
    """The lens, transfinite, submapping, cap and quad-dominant lines the last meshing of the part wrote to the output
    window."""
    form = main_form()
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    result = []
    for line in lines[starts[-1]:]:
        for marker in ("Lens mapping:", "Transfinite:", "Submapping:", "Cap mapping:", "Quad-dominant meshing:"):
            if marker in line:
                result.append(line[line.index(marker):])
    return result


def mesh(part, recombine, size, second_order=False, pins=(), submapped=True, algorithm_2d=None):
    """Meshes the part by a shell item; pins are (edge id, number of elements). Returns the time it took."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Lens_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
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


def boundary_nodes(elements):
    uses = {}
    for element in elements:
        for k in range(len(element)):
            edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return set(node for edge, count in uses.items() if count == 1 for node in edge)


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
    p = [face.xyz[node] for node in element]
    if len(element) == 4:
        normal = cross([p[2][k] - p[0][k] for k in range(3)], [p[3][k] - p[1][k] for k in range(3)])
    else:
        normal = cross([p[1][k] - p[0][k] for k in range(3)], [p[2][k] - p[0][k] for k in range(3)])
    length = math.sqrt(sum(c * c for c in normal))
    return [c / length for c in normal] if length > 0 else [0.0, 0.0, 0.0]


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


def min_scaled_jacobian(face, pole_points=()):
    """The smallest sine of a corner in space against the normal of the mesh at its node, a triangle scaled so that 60
    degrees count as 1; negative where an element folds. A corner at a pole is measured against the normal of its
    element."""
    normals = node_normals(face)
    pole_nodes = face.nodes_at(pole_points)
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


def describe(face, pole_points):
    quads, triangles = face.kinds()
    shortest, longest = face.edge_lengths()
    return (quads, triangles, min_scaled_jacobian(face, pole_points), len(irregular_inner_nodes(face.elements)),
            shortest, longest)


# Plots ----------------------------------------------------------------------------------------------------------------
def plot(file_name, title, face):
    """The elements seen along the axis of the smallest extent of the part; quads blue, triangles orange, inner nodes
    without four quads red."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    points = list(face.xyz.values())
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
def expected_irregular(name):
    if name in REENTRANT_IRREGULAR:
        return REENTRANT_IRREGULAR[name]
    three, two, zero = KINDS[name]
    return three + 2 * two + 4 * zero


def check_detection(parts):
    """Every shape is detected as its kind: three-sided, two-sided or zero-sided lens faces, the submapped ones among
    them; the dome of 270 degrees as a two-sided lens face with its pole reentrant."""
    for name in SHELLS:
        transfinite, lenses, submapped = detected(parts[name])
        three = sum(1 for face in transfinite if face.NumberOfSides == 3)
        two = sum(1 for face in lenses if face.NumberOfSides == 2)
        zero = sum(1 for face in lenses if face.NumberOfSides == 0)
        expected = KINDS.get(name, (0, 0, 0))
        lens_ids = set(face.FaceId for face in lenses)
        submapped_lenses = sum(1 for face in submapped if face.FaceId in lens_ids)
        print(name + ": three-sided", three, "two-sided", two, "zero-sided", zero, "submapped lens faces",
              submapped_lenses, "corner angles", [[round(a, 1) for a in face.CornerAnglesDeg] for face in transfinite])
        check((three, two, zero) == expected, "the " + name + " is detected as " + str((three, two, zero)) + ", not " +
              str(expected))
        check(submapped_lenses == SUBMAPPED_LENSES.get(name, 0), "the " + name + " has " + str(submapped_lenses) +
              " submapped lens faces")


def check_shapes(parts, size):
    rows = []
    for name in SHELLS:
        part = parts[name]
        dx = SPACING * SHELLS.index(name)
        pole_points = poles(part, dx)
        irregular_count = expected_irregular(name)
        row = None
        for recombine in RECOMBINATIONS:
            seconds = mesh(part, recombine, size)
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            title = name + " at size " + str(size) + " with " + recombine
            check(len(messages) == 0, title + " wrote: " + str(messages))
            quads, triangles = face.kinds()
            quality = min_scaled_jacobian(face, pole_points)
            if recombine == "None":
                check(quads == 0 and triangles > 0 and triangles % 2 == 0 and quality > 0, title + " has " +
                      str(face.kinds()) + ", worst scaled Jacobian " + str(quality))
            else:
                irregular = irregular_inner_nodes(face.elements)
                valence = valences(face.elements)
                check(triangles == 0 and len(irregular) == irregular_count and
                      all(valence[node] == 3 for node in irregular),
                      title + " has " + str(face.kinds()) + " and the irregular inner nodes " +
                      str(sorted(valence[node] for node in irregular)) + ", not " + str(irregular_count) + " of 3")
                check(quality >= FLOOR, title + " has a corner of the scaled Jacobian " + str(quality))
            if recombine == "SimpleFullQuad":
                boundary = boundary_nodes(face.elements)
                check(len(boundary) % 2 == 0, title + " has an odd boundary of " + str(len(boundary)))
            print(title + ":", face.kinds(), "irregular", len(irregular_inner_nodes(face.elements)),
                  "worst scaled Jacobian", round(quality, 3), "in", round(seconds, 3), "s")
            if recombine == "QuadDominant":
                row = describe(face, pole_points)
                plot("lens_" + name.replace(" ", "_") + "_" + str(size).replace(".", "_"), title + ": " +
                     str(quads) + " quads, worst SJ " + str(round(quality, 3)), face)
        if size == SIZES[0]:
            mesh(part, "QuadDominant", size, submapped=False)
            face = FaceMesh(part.name)
            before = describe(face, pole_points)
            rows.append((name,) + tuple(round(v, 3) if isinstance(v, float) else v for v in before + row))
    return rows


def half_dome_edges(part):
    """The edges of the half dome by 1-based id: its rim, the side of one edge, and the two meridians."""
    transfinite, lenses, submapped = detected(part)
    sides = [list(side) for side in lenses[0].SideEdgeIds]
    rim = [side for side in sides if len(side) == 1][0][0] + 1
    meridians = [edge_id + 1 for side in sides if len(side) == 2 for edge_id in side]
    return rim, meridians


def check_pins(parts):
    part = parts["dome 180"]
    rim, meridians = half_dome_edges(part)
    for pins, words in (([(rim, 13), (meridians[0], 6), (meridians[1], 6)], "which must be even"),
                        ([(rim, 3)], "fewer than 4")):
        mesh(part, "Simple", SIZES[0], pins=pins)
        face = FaceMesh(part.name)
        messages = output_messages(part.name)
        print("half dome with the pins " + str(pins) + ":", face.kinds(), "irregular",
              len(irregular_inner_nodes(face.elements)), messages)
        check(len(messages) == 1 and messages[0].startswith("Lens mapping:") and "is meshed free" in messages[0] and
              words in messages[0], "the pins " + str(pins) + " must leave the half dome free with a message: " +
              str(messages))
        check(face.kinds()[1] > 0 or len(irregular_inner_nodes(face.elements)) != 2, "the half dome with the pins " +
              str(pins) + " is still a lens face")


def check_second_order(parts):
    part = parts["dome 180"]
    mesh(part, "Simple", SIZES[0], second_order=True)
    face = FaceMesh(part.name)
    irregular = irregular_inner_nodes(face.elements)
    check(all(count == 8 for count in face.node_counts) and len(irregular) == 2,
          "the second order half dome has elements of " + str(set(face.node_counts)) + " nodes and " +
          str(len(irregular)) + " irregular inner nodes")
    print("half dome: second order lens face of", len(face.elements), "quads of 8 nodes")


def check_quasi_structured(parts):
    part = parts["dome 180"]
    for recombine in ("Simple", "QuadDominant"):
        mesh(part, recombine, SIZES[0], algorithm_2d="QuasiStructuredQuad")
        face = FaceMesh(part.name)
        messages = output_messages(part.name)
        quads, triangles = face.kinds()
        print("half dome with QuasiStructuredQuad and " + recombine + ":", face.kinds(), "irregular",
              len(irregular_inner_nodes(face.elements)), messages)
        check(not any(message.startswith("Lens mapping:") for message in messages), "the half dome with " +
              "QuasiStructuredQuad and " + recombine + " wrote " + str(messages))
        check(quads > 0 and triangles == 0, "the half dome with QuasiStructuredQuad and " + recombine + " has " +
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
    check_pins(parts)
    check_second_order(parts)
    check_quasi_structured(parts)
    print("TEST PASSED")


main()
