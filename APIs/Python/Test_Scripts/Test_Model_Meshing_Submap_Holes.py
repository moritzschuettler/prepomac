#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the submapped faces with holes that have corners. Every boundary loop of a face is classified by the rules of
#SubmapFaceDetector, a hole by the turn -4 instead of +4, the directions of a hole are turned into the logical frame of
#the outer loop by the geometry, the counts leave room in the lattice for every hole and a cell on each side of it, and
#SubmapHolePlacement puts every hole where it lies once the counts are known.
#
#  - submap_holes.brep: plane faces side by side, one part each - a rectangle 60 x 40 with a centred hole 30 x 20, the
#    rectangle with an off-centre hole 20 x 10, a rectangle 80 x 40 with two holes 20 x 20 side by side, a square
#    60 x 60 with an L-shaped hole, the rectangle with a centred hole whose four corners are filleted (r 4), an L with
#    a square hole in its long leg, a U with a hole in its web, the rectangle with its hole sheared by 15 degrees, the
#    L with its hole mirrored, a square with a diamond hole (the square turned by 45 degrees), the rectangle with a
#    round hole of two half circles, and the rectangle with a hole 1 mm from its border.
#    At the element size 5 every face but the diamond and the round hole is detected with its holes, each hole turned
#    into the frame of the outer loop by a multiple of 90 degrees, and meshed as quadrilaterals only with four quads
#    around every inner node, no folded quad, a worst scaled Jacobian of 0.2 at least and as many quads as recorded.
#    The diamond hole is turned 45 degrees off the frame of the outer loop, and the visualization of the round hole
#    plate is triangulated only in part and shows only the hole, a disc of two half circles to the detection, which
#    leaves a round loop of one face free (round holes: Test_Model_Meshing_Submap_Round): both faces are not detected.
#    The round hole plate is meshed free without a submapping message (the disc the detection sees is a lens face,
#    left free with the message that gmsh bounds its surface by other edges), and the diamond hole, a face of two
#    loops, as a ring
#    (Test_Model_Meshing_Ring) of as many quads as recorded. The hole 1 mm from the border keeps one cell between it
#    and the border, so the wall of 1 mm is one element thick.
#  - 8 elements pinned on an edge of the centred hole are carried to the opposite edge of the hole.
#  - A full-quad recombination meshes the centred and the filleted hole submapped, and second order elements keep the
#    centred hole submapped.
#  - submap_holes_sweep.brep: a rectangular tube 60 x 40 with walls 10 and with walls 2, a box section with a web (two
#    holes), the L with its square hole and the tube with filleted inner corners, extruded by 30 mm and swept:
#    hexahedra only, the source a grid with four quads around every inner node, the hexahedra a multiple of its quads;
#    the thin tube at the element size 5 is one element through its wall.
#  The models were made by gmsh's OCC kernel through GmshCommon: plane surfaces of lines and circle arcs in z = 0 with
#  their holes as further curve loops, the solids extruded by 30 mm.
#  With the environment variable PMX_SUBMAP_PLOT_DIR set, the meshes are written into that folder as PNG images.
#
#  PrePoMax.com -f submap_holes.brep -u MM_TON_S_C --script Test_Model_Meshing_Submap_Holes.py -g No -x Yes -w <dir>
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
SHELLS = ["centred hole", "off-centre hole", "two holes", "L-shaped hole", "filleted hole", "L with hole",
          "U with hole", "sheared", "mirrored L with hole", "diamond hole", "round hole", "thin border"]
FREE = ["round hole"]
# The faces no lattice can take that are meshed as rings (Test_Model_Meshing_Ring), by their quads
RINGS = {"diamond hole": 336}
SOLIDS = ["tube", "thin tube", "box section", "L with hole", "filleted tube"]
# By shape: the number of holes, the direction in the frame of the outer loop the first edge of every hole runs in, a
# hole started at its vertex lowest in x, then y, then z, the turns of the edges of the holes that turn in their middle,
# and the quads at the element size 5
EXPECTED = {"centred hole": (1, [1], [], 72), "off-centre hole": (1, [1], [], 88), "two holes": (2, [1, 1], [], 96),
            "L-shaped hole": (1, [1], [], 124), "filleted hole": (1, [1], [-1, -1, -1, -1], 45),
            "L with hole": (1, [1], [], 68), "U with hole": (1, [1], [], 82), "sheared": (1, [1], [], 73),
            "mirrored L with hole": (1, [1], [], 68), "thin border": (1, [1], [], 80)}
# The hexahedra of the swept solids at the element size 5
EXPECTED_HEXAHEDRA = {"tube": 384, "thin tube": 240, "box section": 312, "L with hole": 408, "filleted tube": 352}


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


def edge_count(geometry_id):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().count - 1


def mesh(part, recombine="Simple", second_order=False, max_size=MAX_SIZE, pins=()):
    """Meshes the part by a shell item; pins are (edge id, number of elements)."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Hole_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                 [part.id]),
                                 relative_size=False, max_size=max_size, min_size=max_size / 10,
                                 second_order=second_order)
    names = ["PyTest_Hole_Size"]
    for edge_id, count in pins:
        name = "PyTest_Hole_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Hole", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine)
    names.append("PyTest_Hole")
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


def irregular_inner_nodes(quads):
    valence = {}
    for quad in quads:
        for node in quad:
            valence[node] = valence.get(node, 0) + 1
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


def plot(name, title, elements, uv):
    """The elements as a PNG image, quads blue, triangles orange, inner nodes without four quads red."""
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
    valence = {}
    for quad in face.elements:
        for node in quad:
            valence[node] = valence.get(node, 0) + 1
    return all(valence[node] == 2 for node in boundary_nodes(face.elements))


# Checks ---------------------------------------------------------------------------------------------------------------
def describe(face):
    """The loops of a detected face: the number of holes, the direction of the first edge of every hole in the frame of
    the outer loop, which is its turn as the hole starts along +i in its own, and the turns of the hole edges."""
    holes = [] if face.Holes is None else list(face.Holes)
    starts = [int(hole.EdgeDirections[0]) for hole in holes]
    turns = [int(t) for hole in holes if hole.EdgeTurns is not None for t in hole.EdgeTurns if t != 0]
    return len(holes), starts, turns


def check_shells(parts):
    results = {}
    for name in SHELLS:
        part = parts[name]
        faces = detected_faces(part)
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
            plot("holes_" + name.replace(" ", "_"), name + ": a ring of " + str(len(face.elements)) + " quads",
                 face.elements, face.uv)
            continue
        if name in FREE:
            check(len(faces) == 0, "the " + name + " must not be detected")
            mesh(part)
            face = FaceMesh(part.name)
            check(is_free(face) and len(submapping_messages(part.name)) == 0,
                  "the " + name + " must be meshed free without a message: " + str(face.kinds()))
            print(name + ": not detected, free", face.kinds())
            plot("holes_" + name.replace(" ", "_"), name + ": free " + str(face.kinds()), face.elements, face.uv)
            continue
        check(len(faces) == 1, "the " + name + " must be detected as submapped: " + str(len(faces)) + " faces")
        holes, starts, turns = describe(faces[0])
        expected_holes, expected_starts, expected_turns, expected_quads = EXPECTED[name]
        check(holes == expected_holes and starts == expected_starts and sorted(turns) == sorted(expected_turns),
              "the " + name + " is classified with " + str(holes) + " holes starting along " + str(starts) +
              " and the edge turns " + str(turns) + " against " + str(expected_holes) + ", " + str(expected_starts) +
              " and " + str(expected_turns))
        mesh(part)
        face = FaceMesh(part.name)
        quality = check_submapped(face, "the " + name)
        messages = submapping_messages(part.name)
        check(len(messages) == 0, "the submapped " + name + " wrote: " + str(messages))
        check(len(face.elements) == expected_quads, "the " + name + " has " + str(len(face.elements)) +
              " quads instead of " + str(expected_quads))
        print(name + ": holes", holes, "first hole edges along", starts, "hole edge turns", turns, "quads",
              len(face.elements), "worst scaled Jacobian", round(quality, 3))
        plot("holes_" + name.replace(" ", "_"), name + ": " + str(len(face.elements)) + " quads, worst scaled " +
             "Jacobian " + str(round(quality, 3)), face.elements, face.uv)
        results[name] = face
    return results


def check_thin_border(face):
    """The 1 mm wall between the hole and the left border is one element thick: the quads along the hole that reach
    the border end on the hole, 1 mm from it."""
    x0 = min(xyz[0] for xyz in face.xyz.values())
    wall = [quad for quad in face.elements if min(face.xyz[n][0] for n in quad) < x0 + 0.5 and
            10.0 - 1E-6 <= min(face.xyz[n][1] for n in quad) and max(face.xyz[n][1] for n in quad) <= 30.0 + 1E-6]
    check(len(wall) > 0 and all(max(face.xyz[n][0] for n in quad) <= x0 + 1.0 + 1E-6 for quad in wall),
          "the wall of 1 mm beside the hole is not one element thick")
    print("thin border: the wall of 1 mm beside the hole is one element thick,", len(wall), "quads along the hole")


def check_pinned_hole_edge(parts):
    part = parts["centred hole"]
    edges = edges_by_length(part)[30.0]
    check(len(edges) == 2, "the centred hole has not two edges of 30 mm: " + str(edges))
    (edge_id, geometry_id), (other_id, other_geometry_id) = edges
    mesh(part, pins=[(edge_id, 8)])
    face = FaceMesh(part.name)
    quality = check_submapped(face, "the centred hole with 8 elements pinned on a hole edge")
    check(edge_count(geometry_id) == 8 and edge_count(other_geometry_id) == 8 and
          len(submapping_messages(part.name)) == 0,
          "the 8 elements pinned on a hole edge did not carry to the opposite hole edge: " +
          str((edge_count(geometry_id), edge_count(other_geometry_id))))
    print("centred hole with 8 elements on a hole edge: the opposite hole edge carries",
          edge_count(other_geometry_id), "quads", len(face.elements), "worst scaled Jacobian", round(quality, 3))
    plot("holes_centred_hole_pinned_8", "Centred hole, 8 elements on a hole edge: " + str(len(face.elements)) +
         " quads", face.elements, face.uv)
    # As many elements on the hole edges as on the outer edges along them leave no room for the hole
    outer = edges_by_length(part)[60.0]
    mesh(part, pins=[(e, 12) for e, g in edges + outer])
    face = FaceMesh(part.name)
    messages = submapping_messages(part.name)
    print("centred hole with 12 elements on the hole edges and on the outer edges along them:", face.kinds(), messages)
    check(is_free(face) and len(messages) == 1 and "is meshed free" in messages[0] and
          "no room for the hole of Edge" in messages[0],
          "the centred hole without room for its hole must be meshed free with a message: " + str(messages))


def check_variants(parts):
    for name in ["centred hole", "filleted hole"]:
        part = parts[name]
        mesh(part, recombine="SimpleFullQuad")
        face = FaceMesh(part.name)
        quality = check_submapped(face, "the " + name + " with the full-quad recombination")
        messages = submapping_messages(part.name)
        check(len(messages) == 0, "the full-quad " + name + " wrote: " + str(messages))
        print(name + " with the full-quad recombination: submapped", face.kinds(), "worst scaled Jacobian",
              round(quality, 3))
        plot("holes_" + name.replace(" ", "_") + "_full_quad", name + ", full-quad: " + str(len(face.elements)) +
             " quads", face.elements, face.uv)
    part = parts["centred hole"]
    mesh(part, second_order=True)
    face = FaceMesh(part.name)
    check(all(count == 8 for count in face.node_counts), "the second order centred hole has elements of other than 8 " +
          "nodes: " + str(set(face.node_counts)))
    check_submapped(face, "the second order centred hole")
    print("centred hole: second order submapped,", len(face.elements), "quads of 8 nodes")


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
    pmx.import_file(os.path.join(MODELS, "submap_holes_sweep.brep"))
    parts = parts_by_x(SOLIDS, known)
    items = pmx.model.mesh.setup_items
    for name in SOLIDS:
        part = parts[name]
        selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
        items.add_meshing_parameters("PyTest_Hole_Size", selection, relative_size=False, max_size=MAX_SIZE,
                                     min_size=MAX_SIZE / 10)
        items.add_sweep_mesh("PyTest_Hole_Sweep", pmx.create.selection_by_ids('geometry', 'geometry', 'parts',
                                                                              [part.id]))
        try:
            report = pmx.model.mesh.generate(part.name)
        finally:
            items.remove(["PyTest_Hole_Sweep", "PyTest_Hole_Size"])
        check(report[part.name] == "", "sweeping the " + name + " reported: " + report[part.name])
        mesh_ = SweptMesh(part.name)
        kinds = mesh_.kinds()
        messages = submapping_messages(part.name)
        check(list(kinds.keys()) == ["hexahedron"] and len(messages) == 0,
              "the swept " + name + " must be hexahedra only: " + str(kinds) + " " + str(messages))
        source = mesh_.source()
        uv = dict((node, (mesh_.xyz[node][0], mesh_.xyz[node][1])) for quad in source for node in quad)
        irregular = irregular_inner_nodes(source)
        quality = min_scaled_jacobian(source, uv)
        check(len(irregular) == 0 and quality >= 0.2, "the source of the swept " + name + " has " +
              str(len(irregular)) + " inner nodes without four quads and a worst scaled Jacobian of " + str(quality))
        check(len(source) > 0 and len(mesh_.elements) % len(source) == 0,
              "the hexahedra of the " + name + " are not a multiple of its " + str(len(source)) + " source quads")
        if name == "thin tube":
            inner = set(node for quad in source for node in quad) - boundary_nodes(source)
            check(len(inner) == 0, "the source of the thin tube is not one element through its wall: " +
                  str(len(inner)) + " inner nodes")
        check(len(mesh_.elements) == EXPECTED_HEXAHEDRA[name], "the swept " + name + " has " +
              str(len(mesh_.elements)) + " hexahedra instead of " + str(EXPECTED_HEXAHEDRA[name]))
        print("swept " + name + ": hexahedra", len(mesh_.elements), "source", len(source), "quads x",
              len(mesh_.elements) // len(source), "layers, worst scaled Jacobian of the source", round(quality, 3))
        plot("holes_source_" + name.replace(" ", "_"), "Source of the swept " + name + ": " + str(len(source)) +
             " quads", source, uv)


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    faces = check_shells(parts)
    check_thin_border(faces["thin border"])
    check_pinned_hole_edge(parts)
    check_variants(parts)
    check_sweeps()
    print("TEST PASSED")


main()
