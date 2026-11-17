#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the submapped faces with round holes and with edges that turn twice. A hole whose loop is a circle - one
#closed edge, two half circles or four quarters, every vertex tangent and the edges of the same length - is round: in
#the lattice it is a square of a quarter of its elements on each side, so it carries a multiple of four elements, as
#many on each of its edges, and SubmapHolePlacement starts it at the node nearest 45 degrees about its centre in the
#lattice. An edge that turns by about 180 degrees, a half circle, turns twice: three logical edges of a quarter, a
#half and a quarter of its elements, so it carries a multiple of four.
#
#  - submap_round.brep: plane faces side by side, one part each - a rectangle 60 x 40 with a centred round hole of
#    r 10 as one closed edge, the same hole of two half circles whose vertices lie on the axis of the plate, of two half
#    circles turned by 30 degrees and of four quarters, the rectangle with an off-centre hole of r 8, a plate 80 x 60
#    with four bolt holes of r 5, an L with a round hole in its long leg, a rectangle 80 x 40 with a slot along it (two
#    lines and two half circles of r 6), a rectangle 80 x 50 with a slot across it, a slot-shaped plate (a 40 x 20
#    rectangle with half circle ends of r 10), a rectangle 90 x 50 with a round and a rectangular hole, the rectangle
#    sheared by 15 degrees with a round hole, a disc, an annulus, a rectangle with an elliptic hole of one closed edge,
#    a rectangle with a round hole of r 13 that leaves walls of 7 mm, and a disc of two half circles.
#    At the element size 5 every face but the discs, the annulus and the elliptic hole is detected with its round holes
#    and its edges that turn twice, and every one but the slot-shaped plate is meshed as quadrilaterals only with four
#    quads around every inner node, no folded quad, a worst scaled Jacobian of 0.2 at least and as many quads as
#    recorded. The slot-shaped plate, a face with no corner, is a lens face as well; its submapped grid has a worst
#    corner of 0.383 and its lens grid a better one, so it is a lens grid on its own counts (four inner nodes of three
#    quads, 0.5 at least, as many quads as recorded, no message). Every round hole carries a
#    multiple of four elements, as many on each edge, and its four corner nodes, the boundary nodes of three quads,
#    lie within half a node spacing of the 45 degree directions about its centre. The two half circles turned by 30
#    degrees give the mesh of the two on the axis, up to the spacing of the nodes. The discs, the annulus and the
#    elliptic hole are not detected: a round outer loop is left free, and an ellipse is not round. The disc of one
#    closed edge and the disc of two half circles, faces with no corner, are meshed as lens faces
#    (Test_Model_Meshing_Lens_Planar: quads only, four inner nodes of three quads and no other inner node without four,
#    no message), and the annulus and the elliptic hole, faces of two loops, as rings (Test_Model_Meshing_Ring) of as
#    many quads as recorded.
#  - The slot-shaped plate at the element size 1.5: its half circles carry 20 elements or more, the quad at either end
#    of a half circle turns by 180 degrees less 180 degrees over that count, below the floor, so its submapped grid is
#    refused, while the detection, which leaves those corners out, still takes it; it is a lens grid, without a
#    message.
#  - The round hole that leaves walls of 7 mm at the element size 10 keeps a cell between it and the border.
#  - Pinned counts: 10 elements on the closed edge of the round hole and on a half circle of the slot are not a
#    multiple of four and leave the face free with a message naming the edge; 6 elements on one half circle of the
#    round hole are carried to the other, 3 on a quarter to the other three.
#  - A full-quad recombination meshes the round hole and the slot submapped, and second order elements keep the round
#    hole submapped.
#  - submap_round_sweep.brep: a plate with a round hole, an L bracket with a bolt hole, a plate with a slot, a plate
#    with four bolt holes and a block with a round and a square hole, extruded by 30 mm and swept: hexahedra only, the
#    source a grid with four quads around every inner node, the hexahedra a multiple of its quads.
#  The models were made by gmsh's OCC kernel through GmshCommon: plane surfaces of lines and circle arcs in z = 0 with
#  their holes as further curve loops, a closed circle by revolving a point and the ellipse a rational B-spline of one
#  closed edge, the solids extruded by 30 mm.
#  With the environment variable PMX_SUBMAP_PLOT_DIR set, the meshes are written into that folder as PNG images.
#
#  PrePoMax.com -f submap_round.brep -u MM_TON_S_C --script Test_Model_Meshing_Submap_Round.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys
import clr
clr.AddReference('CaeMesh')
clr.AddReference('System.Drawing')
from CaeMesh import SubmapFaceDetector
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
PLOT_DIR = os.environ.get("PMX_SUBMAP_PLOT_DIR")
MAX_SIZE = 5.0
SPACING = 150.0
SHEAR = math.tan(math.radians(15))
SHELLS = ["round hole", "two halves", "turned halves", "four quarters", "off-centre round", "four bolt holes",
          "L with round hole", "slot along", "slot across", "slot plate", "round and rectangle", "sheared round",
          "disc", "annulus", "elliptic hole", "coarse border", "disc of halves"]
FREE = []
LENSES = ["disc", "disc of halves"]
# The faces detected as submapped that are lens faces as well and whose lens grid is the better one, by their quads
SUBMAPPED_LENSES = {"slot plate": 74}
# The faces no lattice can take that are meshed as rings (Test_Model_Meshing_Ring), by their quads
RINGS = {"annulus": 78, "elliptic hole": 240}
SOLIDS = ["plate round", "L bracket", "plate slot", "plate four holes", "round and square"]
# The round holes of every shape by centre and radius in the coordinates of the shape
ROUND_HOLES = {"round hole": [(30, 20, 10)], "two halves": [(30, 20, 10)], "turned halves": [(30, 20, 10)],
               "four quarters": [(30, 20, 10)], "off-centre round": [(20, 25, 8)],
               "four bolt holes": [(15, 15, 5), (65, 15, 5), (65, 45, 5), (15, 45, 5)],
               "L with round hole": [(42, 10, 5)], "round and rectangle": [(25, 25, 10)],
               "sheared round": [(30 + SHEAR * 20, 20, 8)], "coarse border": [(30, 20, 13)]}
# By shape: the number of holes, the number of round ones, the turns of the edges that turn twice, and the quads at the
# element size 5 (10 for the coarse border)
EXPECTED = {"round hole": (1, 1, [], 80), "two halves": (1, 1, [], 80), "turned halves": (1, 1, [], 80),
            "four quarters": (1, 1, [], 80), "off-centre round": (1, 1, [], 87), "four bolt holes": (4, 4, [], 156),
            "L with round hole": (1, 1, [], 72), "slot along": (1, 0, [-2, -2], 110),
            "slot across": (1, 0, [-2, -2], 148),
            "round and rectangle": (2, 1, [], 148), "sheared round": (1, 1, [], 99), "coarse border": (1, 1, [], 36)}
# The hexahedra of the swept solids at the element size 5
EXPECTED_HEXAHEDRA = {"plate round": 480, "L bracket": 480, "plate slot": 588, "plate four holes": 936,
                      "round and square": 888}


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


def offset(part):
    """Where the shape of the part starts along x: its index times the spacing."""
    xs = [c[0] for c in part.get_vertex_data().coordinates]
    return SPACING * int(round(min(xs) / SPACING))


def controller():
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    return form.GetType().GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form)


def submapping_messages(part_name):
    """The submapping lines the last meshing of the part wrote to the output window, read off the main form."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    return [line[line.index("Submapping:"):] for line in lines[starts[-1]:] if "Submapping:" in line]


def detected_faces(part):
    """The faces of the part SubmapFaceDetector finds, read off the geometry of the model."""
    geometry = controller().Model.Geometry
    return list(SubmapFaceDetector.Detect(geometry, geometry.Parts[part.name], 135.0))


def edges_by_length(part):
    data = part.get_edge_data()
    result = {}
    for edge_id, geometry_id, length in zip([int(i) for i in data.ids], [int(i) for i in data.geometry_ids],
                                            [float(l) for l in data.lengths]):
        result.setdefault(round(length, 3), []).append((edge_id, geometry_id))
    return result


def edge_nodes(geometry_id):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().count


def mesh(part, recombine="Simple", second_order=False, max_size=MAX_SIZE, pins=()):
    """Meshes the part by a shell item; pins are (edge id, number of elements)."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Round_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                  [part.id]),
                                 relative_size=False, max_size=max_size, min_size=max_size / 10,
                                 second_order=second_order)
    names = ["PyTest_Round_Size"]
    for edge_id, count in pins:
        name = "PyTest_Round_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Round", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine)
    names.append("PyTest_Round")
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(names)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])


# Mesh access ----------------------------------------------------------------------------------------------------------
class FaceMesh(object):
    """The elements of a meshed part by their corner nodes, with the coordinates of the nodes in the plane z = 0."""

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
        self.uv = dict((node, (self.xyz[node][0], self.xyz[node][1])) for element in self.elements for node in element)

    def kinds(self):
        result = {}
        for element in self.elements:
            kind = "quadrilateral" if len(element) == 4 else "triangle"
            result[kind] = result.get(kind, 0) + 1
        return result


def boundary_nodes(quads):
    uses = {}
    for quad in quads:
        for k in range(len(quad)):
            edge = tuple(sorted((quad[k], quad[(k + 1) % len(quad)])))
            uses[edge] = uses.get(edge, 0) + 1
    return set(node for edge, count in uses.items() if count == 1 for node in edge)


def valences(quads):
    valence = {}
    for quad in quads:
        for node in quad:
            valence[node] = valence.get(node, 0) + 1
    return valence


def irregular_inner_nodes(quads):
    valence = valences(quads)
    boundary = boundary_nodes(quads)
    return [node for node, count in valence.items() if node not in boundary and count != 4]


def min_scaled_jacobian(quads, uv):
    """The smallest sine of a quad corner in the plane, the quads turned the way most of them turn; negative where a
    quad folds."""
    sines = []
    signs = 0
    for quad in quads:
        if len(quad) != 4:
            continue
        for c in range(4):
            p, a, b = uv[quad[c]], uv[quad[(c + 1) % 4]], uv[quad[(c + 3) % 4]]
            e1, e2 = (a[0] - p[0], a[1] - p[1]), (b[0] - p[0], b[1] - p[1])
            length = math.hypot(*e1) * math.hypot(*e2)
            sine = (e1[0] * e2[1] - e1[1] * e2[0]) / length if length > 0 else 0
            sines.append(sine)
            signs += 1 if sine > 0 else -1
    if not sines:
        return None
    return min(s if signs > 0 else -s for s in sines)


def plot(name, title, elements, uv, marks=()):
    """The elements as a PNG image, quads blue, triangles orange, inner nodes without four quads red, and the marked
    nodes green."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    us = [p[0] for p in uv.values()]
    vs = [p[1] for p in uv.values()]
    scale = 560.0 / max(max(us) - min(us), max(vs) - min(vs))
    width, height = int((max(us) - min(us)) * scale + 40), int((max(vs) - min(vs)) * scale + 70)
    bitmap = Bitmap(width, height)
    graphics = Graphics.FromImage(bitmap)
    graphics.Clear(Color.White)
    pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
    quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
    triangle_brush = SolidBrush(Color.FromArgb(246, 217, 200))

    def at(node):
        return PointF(float(20 + (uv[node][0] - min(us)) * scale), float(height - 20 - (uv[node][1] - min(vs)) * scale))

    for element in elements:
        points = Array[PointF]([at(n) for n in element])
        graphics.FillPolygon(quad_brush if len(element) == 4 else triangle_brush, points)
        graphics.DrawPolygon(pen, points)
    for node in irregular_inner_nodes([e for e in elements if len(e) == 4]):
        p = at(node)
        graphics.FillEllipse(Brushes.Firebrick, p.X - 3, p.Y - 3, 6.0, 6.0)
    for node in marks:
        p = at(node)
        graphics.FillEllipse(Brushes.ForestGreen, p.X - 4, p.Y - 4, 8.0, 8.0)
    graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, name + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


def check_submapped(face, name):
    """Quadrilaterals only, four quads around every inner node, and no quad corner below the floor or folded."""
    kinds = face.kinds()
    check(list(kinds.keys()) == ["quadrilateral"], name + " must be quadrilaterals only: " + str(kinds))
    irregular = irregular_inner_nodes(face.elements)
    check(len(irregular) == 0, name + " has " + str(len(irregular)) + " inner nodes without four quads")
    quality = min_scaled_jacobian(face.elements, face.uv)
    check(quality is not None and quality >= 0.2, name + " has a quad corner of the scaled Jacobian " + str(quality))
    return quality


def is_free(face):
    return "triangle" in face.kinds() or len(irregular_inner_nodes([e for e in face.elements if len(e) == 4])) > 0


def is_ring(face):
    """Quadrilaterals only, every inner node in four quads and every boundary node in two: the grid of a ring."""
    if is_free(face):
        return False
    valence = valences(face.elements)
    return all(valence[node] == 2 for node in boundary_nodes(face.elements))


# Checks ---------------------------------------------------------------------------------------------------------------
def describe(face):
    """The loops of a detected face: the number of holes, of round holes, and the turns of the edges that turn twice."""
    holes = [] if face.Holes is None else list(face.Holes)
    round_holes = sum(1 for hole in holes if hole.IsRound)
    turns = [int(t) for loop in [face] + holes if loop.EdgeTurns is not None for t in loop.EdgeTurns if abs(t) == 2]
    return len(holes), round_holes, turns


def round_hole_corners(face, centre, radius):
    """The nodes on a round hole, by their angles about its centre, and its corner nodes, those of three quads."""
    valence = valences(face.elements)
    on_hole = [node for node in valence if abs(math.hypot(face.xyz[node][0] - centre[0],
                                                          face.xyz[node][1] - centre[1]) - radius) < 1E-3 * radius]
    corners = [node for node in on_hole if valence[node] == 3]
    angles = dict((node, math.degrees(math.atan2(face.xyz[node][1] - centre[1], face.xyz[node][0] - centre[0])))
                  for node in on_hole)
    return on_hole, corners, angles


def check_round_holes(name, part, face):
    """Every round hole: a multiple of four nodes on it, and four corner nodes within half a node spacing of the 45
    degree directions. Returns the corner nodes and their angles."""
    dx = offset(part)
    marks = []
    found = []
    for cx, cy, r in ROUND_HOLES.get(name, []):
        on_hole, corners, angles = round_hole_corners(face, (cx + dx, cy), r)
        n = len(on_hole)
        check(n > 0 and n % 4 == 0,
              "a round hole of the " + name + " carries " + str(n) + " nodes, not a multiple of 4")
        spacing = 360.0 / n
        deviations = sorted(min(abs((angles[c] - d + 180) % 360 - 180) for d in (45, 135, -135, -45)) for c in corners)
        check(len(corners) == 4 and deviations[-1] <= 0.5 * spacing + 1E-6,
              "the corners of a round hole of the " + name + " are not near 45 degrees: " + str(len(corners)) +
              " corners off by " + str([round(d, 2) for d in deviations]) + " degrees, the spacing " +
              str(round(spacing, 2)))
        marks.extend(corners)
        found.append((n, sorted(round(angles[c], 1) for c in corners)))
    return marks, found


def check_shells(parts):
    results = {}
    for name in SHELLS:
        part = parts[name]
        faces = detected_faces(part)
        size = 10.0 if name == "coarse border" else MAX_SIZE
        if name in RINGS:
            check(len(faces) == 0, "the " + name + " must not be detected")
            mesh(part)
            face = FaceMesh(part.name)
            quality = min_scaled_jacobian(face.elements, face.uv)
            check(is_ring(face) and len(face.elements) == RINGS[name] and quality >= 0.2 and
                  len(submapping_messages(part.name)) == 0,
                  "the " + name + " must be meshed as a ring of " + str(RINGS[name]) + " quads without a message: " +
                  str(face.kinds()))
            print(name + ": not detected, a ring of", len(face.elements), "quads, worst scaled Jacobian",
                  round(quality, 3))
            plot("round_" + name.replace(" ", "_"), name + ": a ring of " + str(len(face.elements)) + " quads",
                 face.elements, face.uv)
            continue
        if name in FREE:
            check(len(faces) == 0, "the " + name + " must not be detected")
            mesh(part)
            face = FaceMesh(part.name)
            check(is_free(face) and len(submapping_messages(part.name)) == 0,
                  "the " + name + " must be meshed free without a message: " + str(face.kinds()))
            print(name + ": not detected, free", face.kinds())
            plot("round_" + name.replace(" ", "_"), name + ": free " + str(face.kinds()), face.elements, face.uv)
            continue
        if name in SUBMAPPED_LENSES:
            check(len(faces) == 1, "the " + name + " must be detected as submapped: " + str(len(faces)) + " faces")
            mesh(part)
            face = FaceMesh(part.name)
            irregular = irregular_inner_nodes(face.elements)
            valence = valences(face.elements)
            quality = min_scaled_jacobian(face.elements, face.uv)
            check("triangle" not in face.kinds() and len(irregular) == 4 and
                  all(valence[node] == 3 for node in irregular) and len(submapping_messages(part.name)) == 0 and
                  quality >= 0.5 and len(face.elements) == SUBMAPPED_LENSES[name],
                  "the " + name + " must be meshed as a lens grid of " + str(SUBMAPPED_LENSES[name]) + " quads " +
                  "without a message: " + str(face.kinds()) + ", " + str(len(irregular)) + " irregular inner " +
                  "nodes, worst scaled Jacobian " + str(quality))
            print(name + ": detected, a lens grid", face.kinds(), "worst scaled Jacobian", round(quality, 3))
            plot("round_" + name.replace(" ", "_"), name + ": a lens grid " + str(face.kinds()), face.elements,
                 face.uv)
            continue
        if name in LENSES:
            check(len(faces) == 0, "the " + name + " must not be detected")
            mesh(part)
            face = FaceMesh(part.name)
            irregular = irregular_inner_nodes(face.elements)
            valence = valences(face.elements)
            check("triangle" not in face.kinds() and len(irregular) == 4 and
                  all(valence[node] == 3 for node in irregular) and len(submapping_messages(part.name)) == 0,
                  "the " + name + " must be meshed as a lens face without a message: " + str(face.kinds()))
            print(name + ": not detected, a lens face", face.kinds())
            plot("round_" + name.replace(" ", "_"), name + ": a lens face " + str(face.kinds()), face.elements,
                 face.uv)
            continue
        check(len(faces) == 1, "the " + name + " must be detected as submapped: " + str(len(faces)) + " faces")
        holes, round_holes, turns = describe(faces[0])
        mesh(part, max_size=size)
        face = FaceMesh(part.name)
        messages = submapping_messages(part.name)
        print(name + ": holes", holes, "round", round_holes, "double turns", turns, "elements", face.kinds(),
              "messages", messages)
        quality = check_submapped(face, "the " + name)
        check(len(messages) == 0, "the submapped " + name + " wrote: " + str(messages))
        marks, found = check_round_holes(name, part, face)
        print("   quads", len(face.elements), "worst scaled Jacobian", round(quality, 3), "round holes (nodes, corner "
              "angles)", found)
        if name in EXPECTED:
            expected_holes, expected_round, expected_turns, expected_quads = EXPECTED[name]
            check(holes == expected_holes and round_holes == expected_round and sorted(turns) == sorted(expected_turns),
                  "the " + name + " is classified with " + str(holes) + " holes, " + str(round_holes) + " round, and " +
                  "the double turns " + str(turns) + " against " + str(EXPECTED[name][:3]))
            check(len(face.elements) == expected_quads, "the " + name + " has " + str(len(face.elements)) +
                  " quads instead of " + str(expected_quads))
        plot("round_" + name.replace(" ", "_"), name + ": " + str(len(face.elements)) + " quads, worst scaled " +
             "Jacobian " + str(round(quality, 3)), face.elements, face.uv, marks)
        results[name] = face
    return results


def check_turned(parts, faces):
    """The two half circles turned by 30 degrees give the mesh of the two on the axis up to the node spacing: as many
    quads, and every node of one within half an element of a node of the other."""
    a, b = faces["two halves"], faces["turned halves"]
    da, db = offset(parts["two halves"]), offset(parts["turned halves"])
    pa = [(p[0] - da, p[1]) for p in a.uv.values()]
    pb = [(p[0] - db, p[1]) for p in b.uv.values()]
    worst = max(min(math.hypot(p[0] - q[0], p[1] - q[1]) for q in pb) for p in pa)
    print("turned halves: quads", len(a.elements), "and", len(b.elements), "nodes off by", round(worst, 3), "at most")
    check(len(a.elements) == len(b.elements) and worst <= 0.5 * MAX_SIZE,
          "the turned half circles give another mesh: " + str((len(a.elements), len(b.elements), worst)))


def check_coarse_border(part, face):
    """The walls of 7 mm beside the hole of r 13 at the element size 10 keep a cell between the hole and the border."""
    dx = offset(part)
    hole = [n for n, p in face.xyz.items() if abs(math.hypot(p[0] - dx - 30, p[1] - 20) - 13) < 1E-3]
    border = [n for n, p in face.xyz.items() if abs(p[1]) < 1E-6 or abs(p[1] - 40) < 1E-6]
    shared = [q for q in face.elements if any(n in hole for n in q) and any(n in border for n in q)]
    check(len(shared) > 0, "the coarse border has no quad between the hole and the border")
    print("coarse border: quads", len(face.elements), ", quads on both the hole and the border", len(shared))


def check_slot_plate_fine(parts):
    part = parts["slot plate"]
    mesh(part, max_size=1.5)
    face = FaceMesh(part.name)
    messages = submapping_messages(part.name)
    irregular = irregular_inner_nodes(face.elements)
    valence = valences(face.elements)
    print("slot plate at the element size 1.5:", face.kinds(), "irregular", len(irregular), messages)
    check(len(detected_faces(part)) == 1, "the slot plate must be detected")
    check("triangle" not in face.kinds() and len(irregular) == 4 and all(valence[node] == 3 for node in irregular) and
          len(messages) == 0, "the fine slot plate must be a lens grid without a message: " +
          str(face.kinds()) + ", " + str(len(irregular)) + " irregular inner nodes, " + str(messages))
    plot("round_slot_plate_fine", "slot plate at 1.5: a lens face " + str(face.kinds()), face.elements, face.uv)


def check_pins(parts):
    # 10 elements on the closed edge of the round hole
    part = parts["round hole"]
    edges = edges_by_length(part)[round(2 * math.pi * 10, 3)]
    check(len(edges) == 1, "the round hole has not one closed edge: " + str(edges))
    mesh(part, pins=[(edges[0][0], 10)])
    face = FaceMesh(part.name)
    messages = submapping_messages(part.name)
    print("round hole with 10 elements on its edge:", face.kinds(), messages)
    check(is_free(face) and len(messages) == 1 and "round hole of Edge" in messages[0] and
          "not a multiple of four" in messages[0], "10 elements on the round hole must leave it free with a message: " +
          str(messages))
    # 10 elements on a half circle of the slot
    part = parts["slot along"]
    edges = edges_by_length(part)[round(math.pi * 6, 3)]
    check(len(edges) == 2, "the slot has not two half circles: " + str(edges))
    mesh(part, pins=[(edges[0][0], 10)])
    face = FaceMesh(part.name)
    messages = submapping_messages(part.name)
    print("slot with 10 elements on a half circle:", face.kinds(), messages)
    check(is_free(face) and len(messages) == 1 and "turns twice" in messages[0] and
          "not a multiple of four" in messages[0], "10 elements on a half circle must leave the slot free with a " +
          "message: " + str(messages))
    # 6 elements on one half circle of the round hole are carried to the other
    part = parts["two halves"]
    edges = edges_by_length(part)[round(math.pi * 10, 3)]
    check(len(edges) == 2, "the round hole has not two half circles: " + str(edges))
    mesh(part, pins=[(edges[0][0], 6)])
    face = FaceMesh(part.name)
    counts = [edge_nodes(g) - 1 for _, g in edges]
    quality = check_submapped(face, "the round hole of two halves with 6 elements on one")
    check(counts == [6, 6] and len(submapping_messages(part.name)) == 0,
          "6 elements on a half circle are not carried to the other: " + str(counts))
    print("two halves with 6 elements on one: the other carries", counts[1], "quads", len(face.elements),
          "worst scaled Jacobian", round(quality, 3))
    plot("round_two_halves_pinned_6", "Two halves, 6 elements on one: " + str(len(face.elements)) + " quads",
         face.elements, face.uv)
    # 3 elements on a quarter are carried to the other three
    part = parts["four quarters"]
    edges = edges_by_length(part)[round(math.pi * 5, 3)]
    check(len(edges) == 4, "the round hole has not four quarters: " + str(edges))
    mesh(part, pins=[(edges[0][0], 3)])
    face = FaceMesh(part.name)
    counts = [edge_nodes(g) - 1 for _, g in edges]
    check_submapped(face, "the round hole of four quarters with 3 elements on one")
    check(counts == [3, 3, 3, 3] and len(submapping_messages(part.name)) == 0,
          "3 elements on a quarter are not carried to the others: " + str(counts))
    print("four quarters with 3 elements on one: the others carry", counts[1:], "quads", len(face.elements))


def check_variants(parts):
    for name in ["round hole", "slot along"]:
        part = parts[name]
        mesh(part, recombine="SimpleFullQuad")
        face = FaceMesh(part.name)
        messages = submapping_messages(part.name)
        print(name + " with the full-quad recombination:", face.kinds(), messages)
        quality = check_submapped(face, "the " + name + " with the full-quad recombination")
        check(len(messages) == 0, "the full-quad " + name + " wrote: " + str(messages))
        check_round_holes(name, part, face)
        print("   submapped, worst scaled Jacobian", round(quality, 3))
        plot("round_" + name.replace(" ", "_") + "_full_quad", name + ", full-quad: " + str(len(face.elements)) +
             " quads", face.elements, face.uv)
    part = parts["round hole"]
    mesh(part, second_order=True)
    face = FaceMesh(part.name)
    check(all(count == 8 for count in face.node_counts), "the second order round hole has elements of other than 8 " +
          "nodes: " + str(set(face.node_counts)))
    check_submapped(face, "the second order round hole")
    print("round hole: second order submapped,", len(face.elements), "quads of 8 nodes")


# Sweep ----------------------------------------------------------------------------------------------------------------
class SweptMesh(object):
    """The elements of a meshed part by their corner nodes, with the node coordinates."""

    def __init__(self, part_name):
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name]
        check(len(meshed) == 1, "the part " + part_name + " was not meshed")
        self.xyz = dict(zip([int(i) for i in meshed[0].get_node_ids()],
                            [list(c) for c in meshed[0].get_node_coordinates()]))
        self.elements = []
        self.node_counts = []
        for element_id in meshed[0].get_element_ids():
            i = index[int(element_id)]
            corners = {8: 8, 20: 8, 6: 6, 15: 6, 4: 4, 10: 4, 5: 5, 13: 5}[node_counts[i]]
            self.elements.append(connectivity[offsets[i]:offsets[i] + corners])
            self.node_counts.append(node_counts[i])

    def kinds(self):
        names = {8: "hexahedron", 20: "hexahedron", 6: "wedge", 15: "wedge", 4: "tetrahedron", 10: "tetrahedron",
                 5: "pyramid", 13: "pyramid"}
        result = {}
        for count in self.node_counts:
            result[names[count]] = result.get(names[count], 0) + 1
        return result

    def source(self):
        """The quads of the hexahedra faces whose four nodes all lie in z = 0."""
        quads = []
        for element in self.elements:
            if len(element) != 8:
                continue
            for face in [(0, 1, 2, 3), (4, 5, 6, 7)]:
                nodes = [element[k] for k in face]
                if all(abs(self.xyz[node][2]) < 1E-6 for node in nodes):
                    quads.append(nodes)
        return quads


def check_sweeps():
    known = set(part.name for part in pmx.model.geometry.parts)
    pmx.import_file(os.path.join(MODELS, "submap_round_sweep.brep"))
    parts = parts_by_x(SOLIDS, known)
    items = pmx.model.mesh.setup_items
    for name in SOLIDS:
        part = parts[name]
        selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
        items.add_meshing_parameters("PyTest_Round_Size", selection, relative_size=False, max_size=MAX_SIZE,
                                     min_size=MAX_SIZE / 10)
        items.add_sweep_mesh("PyTest_Round_Sweep", pmx.create.selection_by_ids('geometry', 'geometry', 'parts',
                                                                               [part.id]))
        try:
            report = pmx.model.mesh.generate(part.name)
        finally:
            items.remove(["PyTest_Round_Sweep", "PyTest_Round_Size"])
        check(report[part.name] == "", "sweeping the " + name + " reported: " + report[part.name])
        mesh_ = SweptMesh(part.name)
        kinds = mesh_.kinds()
        messages = submapping_messages(part.name)
        source = mesh_.source()
        print("swept " + name + ":", kinds, messages, "source quads", len(source))
        check(list(kinds.keys()) == ["hexahedron"] and len(messages) == 0,
              "the swept " + name + " must be hexahedra only: " + str(kinds) + " " + str(messages))
        uv = dict((node, (mesh_.xyz[node][0], mesh_.xyz[node][1])) for quad in source for node in quad)
        irregular = irregular_inner_nodes(source)
        quality = min_scaled_jacobian(source, uv)
        check(len(irregular) == 0 and quality >= 0.2, "the source of the swept " + name + " has " +
              str(len(irregular)) + " inner nodes without four quads and a worst scaled Jacobian of " + str(quality))
        check(len(source) > 0 and len(mesh_.elements) % len(source) == 0,
              "the hexahedra of the " + name + " are not a multiple of its " + str(len(source)) + " source quads")
        if name in EXPECTED_HEXAHEDRA:
            check(len(mesh_.elements) == EXPECTED_HEXAHEDRA[name], "the swept " + name + " has " +
                  str(len(mesh_.elements)) + " hexahedra instead of " + str(EXPECTED_HEXAHEDRA[name]))
        print("   hexahedra", len(mesh_.elements), "source", len(source), "quads x",
              len(mesh_.elements) // len(source), "layers, worst scaled Jacobian of the source", round(quality, 3))
        plot("round_source_" + name.replace(" ", "_"), "Source of the swept " + name + ": " + str(len(source)) +
             " quads", source, uv)


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    faces = check_shells(parts)
    check_turned(parts, faces)
    check_coarse_border(parts["coarse border"], faces["coarse border"])
    check_slot_plate_fine(parts)
    check_pins(parts)
    check_variants(parts)
    check_sweeps()
    print("TEST PASSED")


main()
