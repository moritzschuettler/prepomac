#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that the structured meshing of a shell face does not depend on where the boundary loop of the face starts or
#which way it runs. The submap detection starts the loop of a face of one loop at its vertex lowest in x, then y, then
#z, and runs it along the lower of the two edges there, so that the alternatives of the classification by turns come in
#one order; it judges the grid it lays over the face from there, and when that grid is poor from every vertex of the
#loop, the best kept. A face with holes starts every loop at its lowest vertex. A lens face starts its loop at a
#corner, of two at the lower one, and with no corner at its lowest vertex, and runs it the same way.
#
#  - submap_loop_start.brep: six planar shapes in the plane y = 0, each made five times, one part each, its curves
#    listed from vertex 0, 1 and 2 of its loop forward and from vertex 0 and 1 backward: shape s, variant v at
#    x = 150 (5 s + v). The shapes: the half disc of r 20 with one corner rounded by r 5 (submapped and a lens face of
#    one corner), an L of legs 40 and 15 wide with its inner corner rounded by r 5 (submapped), the rectangle 60 x 30
#    with its corners rounded by r 5 (submapped and a lens face of no corner), the L with its outer corner rounded by
#    r 10 (submapped), the tombstone 20 x 60 (submapped and a lens face of two corners) and the rectangle
#    50 x 30 with two adjacent corners chamfered by 8 (submapped, its two chamfers tied among six alternatives).
#  - Detection: the five variants of a shape are all submapped or all not, with the same logical directions from the
#    same vertex, and the same lens faces, from the same vertex.
#  - Meshing at the element sizes 5 and 3.5 under Simple and QuadDominant: the five variants of a shape give the same
#    numbers of quads and triangles, the same inner nodes without four quads, the same worst scaled Jacobian, measured
#    in space against the normals of the mesh, within 1E-6, and the same messages. Before this check the detection
#    of the half disc differed between its variants, the L with the inner fillet and the half disc (a lens grid) were
#    meshed with other worst corners and the chamfered rectangle at size 3.5 with 144 or 153 quads: the start of the
#    loop moved the lattice the proportional counts of the detection were snapped to, decided between tied
#    alternatives and laid the lens grid from another vertex.
#  The model was made by gmsh's OCC kernel through GmshCommon: lines and circle arcs, one plane surface each (generator
#  scratchpad/submap/stage23/make_loop_start.py, run with PMX_CAP_GENERATOR = scratchpad/submap/stage14/
#  make_cap_models.py for its helpers).
#
#  PrePoMax.com -f submap_loop_start.brep -u MM_TON_S_C --script Test_Model_Meshing_Submap_Loop_Start.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import clr
clr.AddReference('CaeMesh')
from CaeMesh import LensFaceDetector, SubmapFaceDetector
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

SIZES = [5.0, 3.5]
RECOMBINATIONS = ["Simple", "QuadDominant"]
SPACING = 150.0
TRANSFINITE_ANGLE = 135.0
SHAPES = ["half disc one fillet", "L inner fillet", "rounded rectangle", "L outer fillet", "tombstone",
          "chamfered rectangle"]
NUMBER_OF_VARIANTS = 5
SUBMAPPED = SHAPES
LENSES = {"half disc one fillet": 1, "rounded rectangle": 0, "tombstone": 2}
TOLERANCE = 1E-6


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_variant():
    """The geometry parts by (shape, variant), told apart by where they lie along x."""
    parts = {}
    for part in pmx.model.geometry.parts:
        xs = [c[0] for c in part.get_vertex_data().coordinates]
        index = int(round(min(xs) / SPACING))
        parts[(SHAPES[index // NUMBER_OF_VARIANTS], index % NUMBER_OF_VARIANTS)] = part
    check(len(parts) == len(SHAPES) * NUMBER_OF_VARIANTS, "the parts were not all found: " + str(len(parts)))
    return parts


def main_form():
    return [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]


def geometry_part(part):
    form = main_form()
    controller = form.GetType().GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form)
    return controller.Model.Geometry, controller.Model.Geometry.Parts[part.name]


def offset_of(variant_index):
    return SPACING * variant_index


def vertex_key(geometry, node_id, dx):
    """The coordinates of a vertex node, the part moved back to x = 0, rounded so that the variants compare."""
    coor = geometry.Nodes[node_id].Coor
    return (round(coor[0] - dx, 6), round(coor[1], 6), round(coor[2], 6))


def detection(part, dx):
    """What the detection finds on the part: per submapped face its logical directions and the place of its first
    vertex, per lens face its number of corners and the place of its first vertex."""
    geometry, gpart = geometry_part(part)
    submapped = [(tuple(face.LogicalEdgeDirections), vertex_key(geometry, list(face.VertexNodeIds)[0], dx))
                 for face in SubmapFaceDetector.Detect(geometry, gpart, TRANSFINITE_ANGLE)]
    lenses = [(len(list(face.CornerIndices)), vertex_key(geometry, list(face.VertexNodeIds)[0], dx))
              for face in LensFaceDetector.Detect(geometry, gpart, TRANSFINITE_ANGLE)]
    return submapped, lenses


def output_messages(part_name):
    """The structured and quad-dominant lines the last meshing of the part wrote to the output window, without the
    part."""
    form = main_form()
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    result = []
    for line in lines[starts[-1]:]:
        for marker in ("Lens mapping:", "Transfinite:", "Submapping:", "Quad-dominant meshing:"):
            if marker in line:
                result.append(line[line.index(marker):].replace(" of " + part_name, ""))
    return result


def mesh(part, recombine, size):
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Loop_Size",
                                 pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
                                 relative_size=False, max_size=size, min_size=size / 10)
    items.add_shell_gmsh("PyTest_Loop", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine)
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(["PyTest_Loop_Size", "PyTest_Loop"])
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])


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
        for element_id in meshed[0].get_element_ids():
            i = index[int(element_id)]
            corners = 4 if node_counts[i] in (4, 8) else 3
            self.elements.append(connectivity[offsets[i]:offsets[i] + corners])


def edge_uses(elements):
    uses = {}
    for element in elements:
        for k in range(len(element)):
            edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return uses


def irregular_inner_valences(elements):
    """The numbers of elements round the inner nodes that are not in four, in increasing order."""
    boundary = set(node for edge, count in edge_uses(elements).items() if count == 1 for node in edge)
    valence = {}
    for element in elements:
        for node in element:
            valence[node] = valence.get(node, 0) + 1
    return sorted(count for node, count in valence.items() if node not in boundary and count != 4)


def cross(e1, e2):
    return [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]


def min_scaled_jacobian(face):
    """The smallest sine of a corner in space against the normal of the mesh at its node, a triangle scaled so that 60
    degrees count as 1; negative where an element folds."""
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
        normals[node] = [c / length for c in n] if length > 0 else [0.0, 0.0, 0.0]
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


# Checks ---------------------------------------------------------------------------------------------------------------
def check_detection(parts):
    """The variants of a shape are detected alike: the same submapped faces with the same logical directions from the
    same vertex, and the same lens faces from the same vertex."""
    for shape in SHAPES:
        found = []
        for v in range(NUMBER_OF_VARIANTS):
            found.append(detection(parts[(shape, v)], offset_of(SHAPES.index(shape) * NUMBER_OF_VARIANTS + v)))
        submapped, lenses = found[0]
        print(shape + ": submapped " + str(submapped) + ", lens faces " + str(lenses))
        for v in range(1, NUMBER_OF_VARIANTS):
            check(found[v] == found[0], "variant " + str(v) + " of the " + shape + " is detected as " + str(found[v]) +
                  ", variant 0 as " + str(found[0]))
        check(len(submapped) == (1 if shape in SUBMAPPED else 0), "the " + shape + " has " + str(len(submapped)) +
              " submapped faces")
        expected_lenses = [LENSES[shape]] if shape in LENSES else []
        check([corners for corners, _ in lenses] == expected_lenses,
              "the " + shape + " has the lens faces " + str(lenses))


def check_meshes(parts):
    """The variants of a shape are meshed alike at every size and recombination."""
    rows = []
    for size in SIZES:
        for recombine in RECOMBINATIONS:
            for shape in SHAPES:
                results = []
                for v in range(NUMBER_OF_VARIANTS):
                    part = parts[(shape, v)]
                    mesh(part, recombine, size)
                    face = FaceMesh(part.name)
                    quads = sum(1 for element in face.elements if len(element) == 4)
                    results.append((quads, len(face.elements) - quads, irregular_inner_valences(face.elements),
                                    min_scaled_jacobian(face), output_messages(part.name)))
                quads, triangles, irregular, worst, messages = results[0]
                rows.append((shape, size, recombine, quads, triangles, len(irregular), round(worst, 4), len(messages)))
                for v in range(1, NUMBER_OF_VARIANTS):
                    other = results[v]
                    same = other[0] == quads and other[1] == triangles and other[2] == irregular and \
                           abs(other[3] - worst) <= TOLERANCE and other[4] == messages
                    check(same, "variant " + str(v) + " of the " + shape + " at size " + str(size) + " with " +
                          recombine + " is meshed as " + str(other[:4]) + " " + str(other[4]) + ", variant 0 as " +
                          str(results[0][:4]) + " " + str(messages))
                check(worst > 0, "the " + shape + " at size " + str(size) + " with " + recombine + " folds")
    print("shape | size | recombination | quads | triangles | irregular inner nodes | worst SJ | messages")
    for row in rows:
        print(" | ".join(str(value) for value in row))


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_variant()
    check_detection(parts)
    check_meshes(parts)
    print("TEST PASSED")


main()
