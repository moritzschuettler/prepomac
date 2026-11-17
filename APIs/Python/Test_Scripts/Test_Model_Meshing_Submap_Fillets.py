#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the submapped faces with filleted and chamfered corners. A face the vertex rule of SubmapFaceDetector leaves
#out is classified again by the turns of its vertices and its edges: a fillet, tangent at both ends, turns a corner in
#its middle, and of the two vertices of a chamfer, each half a corner, one is taken as a corner and the other as a side.
#An edge that turns in its middle is two logical edges of half its elements each, so it carries an even number.
#
#  - submap_fillets.brep: plane faces side by side, one part each - an L with its re-entrant corner filleted (r 5), an
#    L with an outer corner chamfered 10 x 10, an L with both, a U with both re-entrant corners filleted, a rectangle
#    with one rounded corner, a rectangle with four rounded corners (r 5), a five-sided house, a slot with half circle
#    ends, an L with its outer corner rounded by r 15, and the filleted L cut from the wall of a cylinder of radius 50.
#    At the element size 5 every shape is classified as expected, quadrilaterals only with four quads around every
#    inner node, no folded quad and a worst scaled Jacobian of 0.2 at least, as many quads as recorded and an even
#    number of elements on every fillet. The rectangle with one rounded corner is a 3-sided face, which the transfinite
#    3-sided mapping takes first, and is submapped when that is off. The half circles of the slot turn twice, each a
#    multiple of four elements (Test_Model_Meshing_Submap_Round). The slot and the rounded rectangle are lens faces as
#    well (one loop, no corner), meshed as the kind whose grid laid on the face alone is better: the slot is a lens
#    grid, four inner nodes of three quads and a worst scaled Jacobian of 0.5 at least (its submapped grid had 0.383);
#    the rounded rectangle keeps its submapped grid, also under AllQuad at the sizes 3.5 and 2.5, where that grid has
#    0.383 and its lens grid, better in its worst corner, stretches elements to twice the size across it.
#  - The L with its outer corner rounded by r 15 at the element size 2: the quad at the middle of its round turns by
#    180 degrees less 90 degrees over the elements along the round, below the floor from 8 elements, so the mesher
#    leaves it free and says so, while the detection, which leaves that corner out, still takes it.
#  - An odd number of elements set on the fillet of the L leaves it free with a message naming the edge; an even one is
#    kept and the L stays submapped.
#  - A full-quad recombination meshes the filleted L and the rounded rectangle submapped with even fillets; gmsh
#    subdivides its full-quad mesh before SubmapMapping replaces it, so the mesher splits the fillets of the full
#    counts. The rounded rectangle keeps fillets of 2 elements, their odd halves balancing each other along both axes.
#    6 elements set on the fillet of the L leave halves of 3 that every other edge, even under the full-quad
#    recombination, cannot balance: the L is meshed free and the message names the fillet. Second order elements keep
#    the L submapped.
#  - submap_fillets_sweep.brep: the filleted L, the chamfered L, an I section with four root fillets and the house,
#    extruded by 30 mm and swept: hexahedra only, the source a grid with four quads around every inner node, the
#    hexahedra a multiple of its quads.
#  The models were made by gmsh's OCC kernel through GmshCommon: plane surfaces of lines and circle arcs in z = 0, the
#  solids extruded by 30 mm, and the cylinder L the wall of a quarter cylinder intersected with the prism the filleted
#  L, scaled by 0.6, sweeps along x.
#  With the environment variable PMX_SUBMAP_PLOT_DIR set, the meshes are written into that folder as SVG drawings.
#
#  PrePoMax.com -f submap_fillets.brep -u MM_TON_S_C --script Test_Model_Meshing_Submap_Fillets.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys
import clr
clr.AddReference('CaeMesh')
from CaeMesh import SubmapFaceDetector, TransfiniteThreeSidedEnum
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
PLOT_DIR = os.environ.get("PMX_SUBMAP_PLOT_DIR")
MAX_SIZE = 5.0
RADIUS = 50.0
CYLINDER_AXIS_X = 1350.0
SPACING = 150.0
SHELLS = ["filleted L", "chamfered L", "filleted chamfered L", "filleted U", "one round", "rounded rectangle", "house",
          "slot", "round L", "cylinder L"]
SOLIDS = ["filleted L", "chamfered L", "I section", "house"]
FILLET_LENGTH = round(math.pi * 5.0 / 2, 3)
# By shape: the turns of the edges that turn in their middle, how many vertices of 135 degrees are sides, and the quads
# at the element size 5
EXPECTED = {"filleted L": ([-1], 0, 80), "chamfered L": ([], 1, 76), "filleted chamfered L": ([-1], 1, 76),
            "filleted U": ([-1, -1], 0, 98), "rounded rectangle": ([1, 1, 1, 1], 0, 140), "house": ([], 1, 96),
            "round L": ([1], 0, 72), "cylinder L": ([-1], 0, 42), "slot": ([2, 2], 0, 74)}
# The shapes meshed as lens faces, a lens grid of four inner nodes of three quads, and the worst corner it reaches
LENS_GRIDS = ["slot"]
MIN_LENS_SCALED_JACOBIAN = 0.5
# The sizes under AllQuad at which the rounded rectangle keeps its submapped grid as well
ROUNDED_RECTANGLE_SIZES = [3.5, 2.5]
# The hexahedra of the swept solids at the element size 5
EXPECTED_HEXAHEDRA = {"filleted L": 616, "chamfered L": 456, "I section": 532, "house": 576}


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
    """The submapping lines the last meshing of the part wrote to the output window. The API returns no mesher
    output, so the lines are read off the main form, as Test_Rpc_Live_Session reaches it."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    return [line[line.index("Submapping:"):] for line in lines[starts[-1]:] if "Submapping:" in line]


def detected_faces(part):
    """The faces of the part SubmapFaceDetector finds, read off the geometry of the model, which the API does not
    reach."""
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


def mesh(part, recombine="Simple", second_order=False, max_size=MAX_SIZE, pins=(), **shell):
    """Meshes the part by a shell item with the given arguments; pins are (edge id, number of elements)."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Fillet_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                   [part.id]),
                                 relative_size=False, max_size=max_size, min_size=max_size / 10,
                                 second_order=second_order)
    names = ["PyTest_Fillet_Size"]
    for edge_id, count in pins:
        name = "PyTest_Fillet_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Fillet", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine, **shell)
    names.append("PyTest_Fillet")
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(names)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])


# Mesh access ----------------------------------------------------------------------------------------------------------
class FaceMesh(object):
    """The elements of a meshed part by their corner nodes, with the coordinates of the nodes and their projection
    into the plane of the face."""

    def __init__(self, part_name, project):
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
        self.uv = dict((node, project(self.xyz[node])) for element in self.elements for node in element)

    def kinds(self):
        result = {}
        for element in self.elements:
            kind = "quadrilateral" if len(element) == 4 else "triangle"
            result[kind] = result.get(kind, 0) + 1
        return result

    def irregular_inner_nodes(self):
        return irregular_inner_nodes(self.elements)

    def min_scaled_jacobian(self):
        return min_scaled_jacobian(self.elements, self.uv)

    def plot(self, name, title):
        plot(name, title, self.elements, self.uv)


def irregular_inner_nodes(quads):
    uses = {}
    valence = {}
    for quad in quads:
        for k in range(len(quad)):
            edge = tuple(sorted((quad[k], quad[(k + 1) % len(quad)])))
            uses[edge] = uses.get(edge, 0) + 1
            valence[quad[k]] = valence.get(quad[k], 0) + 1
    boundary = set(node for edge, count in uses.items() if count == 1 for node in edge)
    return [node for node, count in valence.items() if node not in boundary and count != 4]


def min_scaled_jacobian(quads, uv):
    """The smallest sine of a quad corner in the projection, the quads turned the way most of them turn; negative
    where a quad folds."""
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
    if not PLOT_DIR:
        return
    us = [p[0] for p in uv.values()]
    vs = [p[1] for p in uv.values()]
    scale = 560.0 / max(max(us) - min(us), max(vs) - min(vs))
    width, height = (max(us) - min(us)) * scale + 40, (max(vs) - min(vs)) * scale + 70
    lines = ['<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d">' % (width, height),
             '<rect width="100%" height="100%" fill="white"/>',
             '<text x="20" y="24" font-family="sans-serif" font-size="15">%s</text>' % title]
    irregular = set(irregular_inner_nodes(elements))
    for element in elements:
        points = " ".join("%.2f,%.2f" % (20 + (uv[n][0] - min(us)) * scale, height - 20 - (uv[n][1] - min(vs)) * scale)
                          for n in element)
        lines.append('<polygon points="%s" fill="%s" stroke="#1d3c5a" stroke-width="0.8"/>' %
                     (points, "#dbe9f6" if len(element) == 4 else "#f6d9c8"))
    for node in irregular:
        lines.append('<circle cx="%.2f" cy="%.2f" r="3" fill="#c0392b"/>' %
                     (20 + (uv[node][0] - min(us)) * scale, height - 20 - (uv[node][1] - min(vs)) * scale))
    lines.append("</svg>")
    with open(os.path.join(PLOT_DIR, name + ".svg"), "w") as file:
        file.write("\n".join(lines))


def plane_xy(xyz):
    return (xyz[0], xyz[1])


def unrolled(xyz):
    return (RADIUS * math.atan2(xyz[1], xyz[0] - CYLINDER_AXIS_X), xyz[2])


def check_submapped(face, name):
    """Quadrilaterals only, four quads around every inner node, and no quad corner below the floor or folded."""
    kinds = face.kinds()
    check(list(kinds.keys()) == ["quadrilateral"], name + " must be quadrilaterals only: " + str(kinds))
    irregular = face.irregular_inner_nodes()
    check(len(irregular) == 0, name + " has " + str(len(irregular)) + " inner nodes without four quads")
    quality = face.min_scaled_jacobian()
    check(quality is not None and quality >= 0.2, name + " has a quad corner of the scaled Jacobian " + str(quality))
    return quality


def check_lens_grid(face, name):
    """Quadrilaterals only, four inner nodes of three quads and every other one of four, no quad corner below the worst
    corner of a chosen lens grid."""
    kinds = face.kinds()
    check(list(kinds.keys()) == ["quadrilateral"], name + " must be quadrilaterals only: " + str(kinds))
    valence = {}
    for quad in face.elements:
        for node in quad:
            valence[node] = valence.get(node, 0) + 1
    irregular = face.irregular_inner_nodes()
    check(len(irregular) == 4 and all(valence[node] == 3 for node in irregular),
          name + " is not a lens grid: inner nodes without four quads of " + str([valence[n] for n in irregular]))
    quality = face.min_scaled_jacobian()
    check(quality is not None and quality >= MIN_LENS_SCALED_JACOBIAN,
          name + " has a quad corner of the scaled Jacobian " + str(quality))
    return quality


def is_free(face):
    return "triangle" in face.kinds() or len(face.irregular_inner_nodes()) > 0


# Checks ---------------------------------------------------------------------------------------------------------------
def check_classification(name, part):
    """The turns the detection assigns to the edges and the vertices of 135 degrees it takes as sides."""
    faces = detected_faces(part)
    check(len(faces) == 1, "the " + name + " must be detected as submapped: " + str(len(faces)) + " faces")
    face = faces[0]
    turns = [] if face.EdgeTurns is None else [int(t) for t in face.EdgeTurns if t != 0]
    directions = [int(d) for d in face.EdgeDirections]
    angles = [float(a) for a in face.VertexAnglesDeg]
    n = len(directions)
    sides = 0
    for k in range(n):
        previous = (directions[k - 1] + (0 if face.EdgeTurns is None else int(face.EdgeTurns[k - 1]))) % 4
        if abs(angles[k] - 135.0) < 1 and directions[k] == previous:
            sides += 1
    expected_turns, expected_sides, quads = EXPECTED[name]
    check(sorted(turns) == sorted(expected_turns) and sides == expected_sides,
          "the " + name + " is classified with the edge turns " + str(turns) + " and " + str(sides) +
          " vertices of 135 degrees as sides, against " + str(expected_turns) + " and " + str(expected_sides))
    return turns, sides


def check_shells(parts):
    for name in SHELLS:
        part = parts[name]
        if name == "one round":
            check(len(detected_faces(part)) == 1, "the rectangle with one rounded corner must be detected")
            mesh(part)
            face = FaceMesh(part.name, plane_xy)
            check(list(face.kinds().keys()) == ["quadrilateral"] and len(face.irregular_inner_nodes()) == 1,
                  "the rectangle with one rounded corner must be the three blocks of the 3-sided mapping: " +
                  str(face.kinds()) + ", " + str(len(face.irregular_inner_nodes())) + " irregular inner nodes")
            print("one round: 3-sided mapping", face.kinds())
            mesh(part, transfinite_three_sided="No")
            face = FaceMesh(part.name, plane_xy)
            quality = check_submapped(face, "the rectangle with one rounded corner without the 3-sided mapping")
            check(len(submapping_messages(part.name)) == 0, "the submapped one round wrote a message")
            print("one round without the 3-sided mapping: submapped", face.kinds(), "worst scaled Jacobian",
                  round(quality, 3))
            face.plot("fillets_one_round", "Rectangle with one rounded corner, no 3-sided mapping: " +
                      str(len(face.elements)) + " quads")
            continue
        turns, sides = check_classification(name, part)
        mesh(part)
        face = FaceMesh(part.name, unrolled if name == "cylinder L" else plane_xy)
        kind = "lens grid" if name in LENS_GRIDS else "submapped"
        if name in LENS_GRIDS:
            quality = check_lens_grid(face, "the " + name)
        else:
            quality = check_submapped(face, "the " + name)
        messages = submapping_messages(part.name)
        check(len(messages) == 0, "the " + kind + " " + name + " wrote: " + str(messages))
        check(len(face.elements) == EXPECTED[name][2], "the " + name + " has " + str(len(face.elements)) +
              " quads instead of " + str(EXPECTED[name][2]))
        fillets = [edge_count(g) for length, edges in edges_by_length(part).items()
                   if abs(length - FILLET_LENGTH) < 1E-2 for e, g in edges]
        check(all(count % 2 == 0 for count in fillets), "a fillet of the " + name + " carries an odd count: " +
              str(fillets))
        print(name + ": edge turns", turns, "sides at 135 degrees", sides, kind, face.kinds(), "fillets",
              fillets, "worst scaled Jacobian", round(quality, 3))
        face.plot("fillets_" + name.replace(" ", "_"), name + ": " + str(len(face.elements)) + " quads, worst scaled " +
                  "Jacobian " + str(round(quality, 3)))


def check_rounded_rectangle_sizes(parts):
    """The rectangle with small fillets keeps its submapped grid at finer sizes, where its lens grid stretches."""
    name = "rounded rectangle"
    part = parts[name]
    for size in ROUNDED_RECTANGLE_SIZES:
        mesh(part, recombine="AllQuad", max_size=size)
        face = FaceMesh(part.name, plane_xy)
        title = "the " + name + " under AllQuad at the size " + str(size)
        quality = check_submapped(face, title)
        messages = submapping_messages(part.name)
        check(len(messages) == 0, title + " wrote: " + str(messages))
        print(name + " under AllQuad at the size " + str(size) + ": submapped", face.kinds(), "worst scaled Jacobian",
              round(quality, 3))


def check_round_fallback(parts):
    part = parts["round L"]
    mesh(part, max_size=2.0)
    face = FaceMesh(part.name, plane_xy)
    messages = submapping_messages(part.name)
    print("round L at the element size 2:", face.kinds(), messages)
    check(len(detected_faces(part)) == 1, "the round L must still be detected")
    check(len(messages) == 1 and "is meshed free" in messages[0] and "scaled Jacobian" in messages[0],
          "the round L at the element size 2 must be meshed free by the mesher for its scaled Jacobian: " +
          str(messages))
    check(is_free(face), "the round L at the element size 2 was not left as gmsh meshed it: " + str(face.kinds()))
    face.plot("fillets_round_L_fallback", "L with a round of r 15 at the element size 2: meshed free")


def fillet_edge(part):
    edges = [edge for length, edges in edges_by_length(part).items() if abs(length - FILLET_LENGTH) < 1E-2
             for edge in edges]
    check(len(edges) == 1, "the part has not one fillet: " + str(edges))
    return edges[0]


def check_pinned_fillet(parts):
    part = parts["filleted L"]
    edge_id, geometry_id = fillet_edge(part)
    mesh(part, pins=[(edge_id, 5)])
    face = FaceMesh(part.name, plane_xy)
    messages = submapping_messages(part.name)
    print("filleted L with 5 elements on its fillet:", face.kinds(), messages)
    check(len(messages) == 1 and "is meshed free" in messages[0] and
          "turns a corner in its middle and carries an odd number of elements" in messages[0],
          "an odd count on the fillet must leave the L free with a message naming the edge: " + str(messages))
    check(edge_count(geometry_id) == 5 and is_free(face), "the L with 5 elements on its fillet is not free with 5")
    #
    mesh(part, pins=[(edge_id, 6)])
    face = FaceMesh(part.name, plane_xy)
    quality = check_submapped(face, "the L with 6 elements on its fillet")
    check(edge_count(geometry_id) == 6 and len(submapping_messages(part.name)) == 0,
          "the L with 6 elements on its fillet must keep them, submapped")
    print("filleted L with 6 elements on its fillet: submapped", face.kinds(), "worst scaled Jacobian",
          round(quality, 3))
    face.plot("fillets_L_pinned_6", "Filleted L, 6 elements on the fillet: " + str(len(face.elements)) + " quads")


def check_variants(parts):
    for name in ["filleted L", "rounded rectangle"]:
        part = parts[name]
        mesh(part, recombine="SimpleFullQuad")
        face = FaceMesh(part.name, plane_xy)
        quality = check_submapped(face, "the " + name + " with the full-quad recombination")
        fillets = [edge_count(g) for length, edges in edges_by_length(part).items()
                   if abs(length - FILLET_LENGTH) < 1E-2 for e, g in edges]
        check(len(fillets) > 0 and all(count % 2 == 0 for count in fillets) and
              len(submapping_messages(part.name)) == 0,
              "the " + name + " with the full-quad recombination has fillets " + str(fillets) + ", messages " +
              str(submapping_messages(part.name)))
        print(name + " with the full-quad recombination: submapped", face.kinds(), "fillets", fillets,
              "worst scaled Jacobian", round(quality, 3))
        face.plot("fillets_" + name.replace(" ", "_") + "_full_quad", name + ", full-quad: " +
                  str(len(face.elements)) + " quads")
    part = parts["filleted L"]
    edge_id, geometry_id = fillet_edge(part)
    mesh(part, recombine="SimpleFullQuad", pins=[(edge_id, 6)])
    face = FaceMesh(part.name, plane_xy)
    messages = submapping_messages(part.name)
    print("filleted L with the full-quad recombination and 6 elements on its fillet:", face.kinds(), messages)
    check(is_free(face) and len(messages) == 1 and "full-quad" in messages[0] and
          "odd halves of Edge " + str(edge_id) in messages[0],
          "the full-quad L with 6 elements on its fillet must be free with a message naming the odd halves of the " +
          "fillet: " + str(messages))
    #
    mesh(part, second_order=True)
    face = FaceMesh(part.name, plane_xy)
    check(all(count == 8 for count in face.node_counts), "the second order filleted L has elements of other than 8 " +
          "nodes: " + str(set(face.node_counts)))
    check_submapped(face, "the second order filleted L")
    print("filleted L: second order submapped,", len(face.elements), "quads of 8 nodes")


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
    pmx.import_file(os.path.join(MODELS, "submap_fillets_sweep.brep"))
    parts = parts_by_x(SOLIDS, known)
    items = pmx.model.mesh.setup_items
    for name in SOLIDS:
        part = parts[name]
        selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
        items.add_meshing_parameters("PyTest_Fillet_Size", selection, relative_size=False, max_size=MAX_SIZE,
                                     min_size=MAX_SIZE / 10)
        items.add_sweep_mesh("PyTest_Fillet_Sweep", pmx.create.selection_by_ids('geometry', 'geometry', 'parts',
                                                                                [part.id]))
        try:
            report = pmx.model.mesh.generate(part.name)
        finally:
            items.remove(["PyTest_Fillet_Sweep", "PyTest_Fillet_Size"])
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
        check(len(mesh_.elements) == EXPECTED_HEXAHEDRA[name], "the swept " + name + " has " +
              str(len(mesh_.elements)) + " hexahedra instead of " + str(EXPECTED_HEXAHEDRA[name]))
        print("swept " + name + ": hexahedra", len(mesh_.elements), "source", len(source), "quads x",
              len(mesh_.elements) // len(source), "layers, worst scaled Jacobian of the source", round(quality, 3))
        plot("fillets_source_" + name.replace(" ", "_"), "Source of the swept " + name + ": " + str(len(source)) +
             " quads", source, uv)


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    check_shells(parts)
    check_rounded_rectangle_sizes(parts)
    check_round_fallback(parts)
    check_pinned_fillet(parts)
    check_variants(parts)
    check_sweeps()
    print("TEST PASSED")


main()
