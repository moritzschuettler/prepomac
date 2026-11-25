#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the faces with a pole of their surface at a corner of four sides, and the faces with a reentrant pole. A
#four-sided face with a pole at a corner (a sector of a dome, a cone or a paraboloid cut by a plane) is laid before
#gmsh meshes the surfaces as one block of quads, mapped both in the conformal chart of the pole and in the parameters of
#the surface, the better grid in space kept, its inner nodes then smoothed together in the chart; gmsh's transfinite
#mapping in the parameters, where the pole is a collapsed side, left corners of 0.27 to 0.6 and folded the dome cut off
#to one side of its middle meridian. A face whose pole is reentrant (a dome, a cone or a paraboloid revolved by more
#than 225 degrees about its apex) is a lens face split at the pole by lines of the surface into pieces of about a
#right angle, three for 225 to 315 degrees, so that the pole carries one quad per piece; the pieces are three-sided,
#three blocks of quads, or four-sided, one block.
#
#  - pole_faces.brep: one part per shape side by side at x = 150 i, r 20 about the axis (150 i + 20, 0) unless said:
#    the hemisphere revolved by 90, 60 and 120 degrees and cut by the plane x = 34 (dome 90 cut, dome 60 cut, dome 120
#    cut), the cone of 45 degrees revolved by 90 cut at x = 32, the cone of 15 degrees (r 10) revolved by 180 cut at
#    x = 26, the paraboloid revolved by 90 cut at x = 34 (four-sided, the pole a corner of 60 to 120 degrees, the apex
#    of the cones of 60 and 46.6); the hemisphere revolved by 270, 240 and 300 degrees, the cone of 60 degrees revolved
#    by 270 and the cone of 45 degrees by 330 (developed angles at the apex of 234 and 233 degrees), the paraboloid
#    revolved by 270, and the dome of 270 degrees cut by the plane x = 6, which adds two corners to its rim (reentrant
#    poles); the hemisphere within the square of 24 round its pole, a cap with a rim of four corners (gmsh's OCC keeps
#    the seam of the sphere in a face that holds the pole inside it, so a face with a pole inside it is always a cap).
#  - Detection: the four-sided faces are transfinite faces of four corners, the reentrant ones lens faces with one
#    reentrant vertex, the square cut a cap. The cone of 15 degrees cut is none: the transfinite interpolation of its
#    sides in space folds.
#  - At the element sizes 5 and 3.5 under None, Simple, QuadDominant and SimpleFullQuad without a message: quads only,
#    two triangles per quad without recombination; no fold; the four-sided faces no irregular inner node and a worst
#    scaled Jacobian of 0.5 at least, measured in space against the normals of the mesh, a corner at a pole against the
#    normal of its element; the reentrant faces three inner nodes of three quads, the pole in three quads, a worst
#    scaled Jacobian of 0.5 at least; the cap four inner nodes of three quads; the full-quad recombination makes the
#    boundary even. The dome of 270 cut is either a lens grid (worst 0.2 at least) or left free with the lens message;
#    its four-sided pieces carry lines of the counts of the sides they face. The cone of 15 degrees cut is meshed free
#    without a message under None, Simple and QuadDominant (gmsh refuses a full-quad recombination of a free face of a
#    cone).
#  - Pins: the dome of 270 degrees with its rim set to 13 and its meridians to 6 elements, an odd total, is left free
#    with the lens message.
#  - Second order: the dome of 270 degrees and the dome 90 cut with Simple are quads of 8 nodes only.
#  - The quasi-structured quad algorithm: the dome of 270 degrees is left to it (no lens message, quads only) and the
#    dome 90 cut is a transfinite surface of gmsh's, as before.
#  - Table: per shape at size 5 under QuadDominant, the mesh with transfinite_submapped=False against the mesh with it;
#    the four-sided faces are structured either way.
#  The model was made by gmsh's OCC kernel through GmshCommon: arcs, lines and B-splines revolved, intersected with
#  boxes (generator Models/Generators/Make_Pole_Faces.py). With the environment variable PMX_POLE_PLOT_DIR set, the
#  meshes are written into that folder as PNG images, each part seen along the axis of its smallest extent, the inner
#  nodes without four quads red.
#
#  PrePoMax.com -f pole_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_Pole_Faces.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import time
import clr
clr.AddReference('CaeMesh')
clr.AddReference('System.Drawing')
from CaeMesh import CapFaceDetector, LensFaceDetector, TransfiniteFaceDetector, TransfiniteThreeSidedEnum
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

PLOT_DIR = os.environ.get("PMX_POLE_PLOT_DIR")
SIZES = [5.0, 3.5]
RECOMBINATIONS = ["None", "Simple", "QuadDominant", "SimpleFullQuad"]
SPACING = 150.0
FLOOR = 0.2
GOOD = 0.5
TRANSFINITE_ANGLE = 135.0
SHAPES = ["dome 90 cut", "dome 60 cut", "dome 120 cut", "cone 45 quarter cut", "cone 15 half cut",
          "paraboloid quarter cut", "dome 270", "dome 240", "dome 300", "cone 60 270", "cone 45 330", "paraboloid 270",
          "dome 270 cut", "dome box"]
FOUR_SIDED = ["dome 90 cut", "dome 60 cut", "dome 120 cut", "cone 45 quarter cut", "paraboloid quarter cut"]
REENTRANT = ["dome 270", "dome 240", "dome 300", "cone 60 270", "cone 45 330", "paraboloid 270"]
PIECES = 3


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
    """The transfinite faces, the lens faces and the caps the detection finds on the part."""
    geometry, geometry_part_ = geometry_part(part)
    transfinite = list(TransfiniteFaceDetector.Detect(geometry, geometry_part_, TransfiniteThreeSidedEnum.Mapped, True,
                                                      TRANSFINITE_ANGLE))
    lenses = list(LensFaceDetector.Detect(geometry, geometry_part_, TRANSFINITE_ANGLE))
    caps = list(CapFaceDetector.Detect(geometry_part_))
    return transfinite, lenses, caps


def poles(part, dx):
    """The vertices of the part on the axis of its shape, through (dx + 20, 0): its poles and apexes."""
    return [tuple(c) for c in part.get_vertex_data().coordinates if abs(c[0] - dx - 20) < 1E-6 and abs(c[1]) < 1E-6]


def output_messages(part_name):
    """The lens, transfinite, cap and quad-dominant lines the last meshing of the part wrote to the output window."""
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
    items.add_meshing_parameters("PyTest_Pole_Size",
                                 pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
                                 relative_size=False, max_size=size, min_size=size / 10, second_order=second_order)
    names = ["PyTest_Pole_Size"]
    for edge_id, count in pins:
        name = "PyTest_Pole_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])
    if algorithm_2d is None:
        items.add_shell_gmsh("PyTest_Pole", selection, algorithm_recombine=recombine, transfinite_submapped=submapped)
    else:
        items.add_shell_gmsh("PyTest_Pole", selection, algorithm_2d=algorithm_2d, algorithm_recombine=recombine,
                             transfinite_submapped=submapped)
    names.append("PyTest_Pole")
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
def check_detection(parts):
    """The four-sided faces are transfinite faces of four corners, the reentrant faces lens faces with one reentrant
    vertex (also the dome of 270 cut, with four corners), the square cut a cap, the cone of 15 degrees cut none."""
    for name in SHAPES:
        transfinite, lenses, caps = detected(parts[name])
        four = sum(1 for face in transfinite if face.NumberOfSides == 4)
        reentrant = sum(1 for face in lenses if len(list(face.ReentrantIndices)) == 1)
        print(name + ": four-sided", four, "lens faces", [face.NumberOfSides for face in lenses], "reentrant",
              reentrant, "caps", len(caps))
        expected = (1 if name in FOUR_SIDED else 0, 1 if name in REENTRANT or name == "dome 270 cut" else 0,
                    1 if name == "dome box" else 0)
        check((four, reentrant, len(caps)) == expected and len(transfinite) == four and len(lenses) == reentrant,
              "the " + name + " is detected as " + str((len(transfinite), len(lenses), len(caps))))


def check_shape(part, name, size, recombine, pole_points):
    """Meshes one shape and checks it as its kind. Returns the face mesh and its worst scaled Jacobian."""
    mesh(part, recombine, size)
    face = FaceMesh(part.name)
    messages = output_messages(part.name)
    title = name + " at size " + str(size) + " with " + recombine
    quads, triangles = face.kinds()
    quality = min_scaled_jacobian(face, pole_points)
    irregular = irregular_inner_nodes(face.elements)
    valence = valences(face.elements)
    refused = len(messages) == 1 and messages[0].startswith("Lens mapping:") and "is meshed free" in messages[0]
    if name == "dome 270 cut" and refused:
        check(quality > 0, title + " has a folded element")
    else:
        check(len(messages) == 0, title + " wrote: " + str(messages))
        if name == "cone 15 half cut":
            check(quality > 0, title + " has a folded element")
        elif recombine == "None":
            check(quads == 0 and triangles > 0 and triangles % 2 == 0 and quality > 0, title + " has " +
                  str(face.kinds()) + ", worst scaled Jacobian " + str(quality))
        else:
            expected = 0 if name in FOUR_SIDED else 4 if name == "dome box" else None
            check(triangles == 0 and (expected is None or len(irregular) == expected) and
                  all(valence[node] == 3 for node in irregular), title + " has " + str(face.kinds()) +
                  " and the irregular inner nodes " + str(sorted(valence[node] for node in irregular)))
            check(quality >= (GOOD if name in FOUR_SIDED or name in REENTRANT else FLOOR), title + " has a corner " +
                  "of the scaled Jacobian " + str(quality))
            if name in REENTRANT:
                pole_nodes = face.nodes_at(pole_points)
                check(len(irregular) == PIECES and len(pole_nodes) == 1 and
                      valence[list(pole_nodes)[0]] == PIECES, title + " has " + str(len(irregular)) +
                      " irregular inner nodes and its pole in " + str([valence[node] for node in pole_nodes]) +
                      " quads, not " + str(PIECES))
        if recombine == "SimpleFullQuad":
            check(len(boundary_nodes(face.elements)) % 2 == 0, title + " has an odd boundary")
    print(title + ":", face.kinds(), "irregular", len(irregular), "worst scaled Jacobian", round(quality, 3),
          messages)
    return face, quality


def check_shapes(parts, size):
    rows = []
    for name in SHAPES:
        part = parts[name]
        pole_points = poles(part, SPACING * SHAPES.index(name))
        row = None
        for recombine in RECOMBINATIONS:
            if name == "cone 15 half cut" and recombine == "SimpleFullQuad":
                continue
            face, quality = check_shape(part, name, size, recombine, pole_points)
            if recombine == "QuadDominant":
                row = describe(face, pole_points)
                plot("pole_" + name.replace(" ", "_") + "_" + str(size).replace(".", "_"), name + " at size " +
                     str(size) + " with QuadDominant: " + str(face.kinds()[0]) + " quads, worst SJ " +
                     str(round(quality, 3)), face)
        if size == SIZES[0]:
            mesh(part, "QuadDominant", size, submapped=False)
            before = describe(FaceMesh(part.name), pole_points)
            rows.append((name,) + tuple(round(v, 3) if isinstance(v, float) else v for v in before + row))
    return rows


def lens_sides(part):
    """The edges of the lens face of the part by 1-based id: its side of one edge and its side of the others."""
    transfinite, lenses, caps = detected(part)
    sides = [list(side) for side in lenses[0].SideEdgeIds]
    one = [side for side in sides if len(side) == 1][0][0] + 1
    others = [edge_id + 1 for side in sides if len(side) > 1 for edge_id in side]
    return one, others


def check_pins(parts):
    part = parts["dome 270"]
    rim, meridians = lens_sides(part)
    pins = [(rim, 13)] + [(edge_id, 6) for edge_id in meridians]
    mesh(part, "Simple", SIZES[0], pins=pins)
    face = FaceMesh(part.name)
    messages = output_messages(part.name)
    print("dome 270 with the pins " + str(pins) + ":", face.kinds(), messages)
    check(len(messages) == 1 and messages[0].startswith("Lens mapping:") and "is meshed free" in messages[0] and
          "which must be even" in messages[0], "the pins must leave the dome 270 free with a message: " + str(messages))


def check_second_order(parts):
    for name, irregular_count in (("dome 270", PIECES), ("dome 90 cut", 0)):
        mesh(parts[name], "Simple", SIZES[0], second_order=True)
        face = FaceMesh(parts[name].name)
        irregular = irregular_inner_nodes(face.elements)
        check(all(count == 8 for count in face.node_counts) and len(irregular) == irregular_count,
              "the second order " + name + " has elements of " + str(set(face.node_counts)) + " nodes and " +
              str(len(irregular)) + " irregular inner nodes")
        print(name + ": second order grid of", len(face.elements), "quads of 8 nodes")


def check_quasi_structured(parts):
    for name in ("dome 270", "dome 90 cut"):
        mesh(parts[name], "QuadDominant", SIZES[0], algorithm_2d="QuasiStructuredQuad")
        face = FaceMesh(parts[name].name)
        messages = output_messages(parts[name].name)
        quads, triangles = face.kinds()
        print(name + " with QuasiStructuredQuad:", face.kinds(), "irregular",
              len(irregular_inner_nodes(face.elements)), messages)
        check(len(messages) == 0 and quads > 0 and triangles == 0, "the " + name + " with QuasiStructuredQuad has " +
              str(face.kinds()) + " and wrote " + str(messages))


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHAPES)
    check_detection(parts)
    rows = []
    for size in SIZES:
        rows.extend(check_shapes(parts, size))
    print("shape | off quads | off triangles | off worst SJ | off irregular | off shortest | off longest | " +
          "quads | triangles | worst SJ | irregular | shortest | longest")
    for row in rows:
        print(" | ".join(str(v) for v in row))
    check_pins(parts)
    check_second_order(parts)
    check_quasi_structured(parts)
    print("TEST PASSED")


main()
