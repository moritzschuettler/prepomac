#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the lens faces as sources of the sweep. A plane source of one boundary loop with two corners, one corner or
#none is laid by the lens mesher when transfinite_submapped is on, the default: its edges ask the interval assignment
#of the sweep for an even total, 4 elements at least on each side of a two-cornered face and 8 round the loop of
#another, and equal sides of a thin two-cornered face at the weakest tier; gmsh meshes it free first and the lens grid
#replaces that mesh, which the sweep carries to the target. A source given up - counts the walls leave odd or too few,
#a grid that folds or has a quad corner below 0.2 - keeps the free mesh, cleaned to quads, with a "Lens mapping:"
#message. A source with no straight edge is never submapped; one the submapping keeps as well takes the kind whose
#grid is the better on the face alone, as on a shell.
#
#  - lens_sweep.brep: prisms 40 long along +y from plane faces in y = 0, one part each at x = 150 i: a cylinder of r 20
#    (a disc of one closed edge), an elliptic cylinder 60 x 30 (four rational B-splines), a half cylinder of r 20, a
#    prism of a lens of two arcs over a chord of 40 with a sagitta of 8 each, a teardrop prism (tip of 60 degrees, an
#    arc of r 15), a stadium prism 45 x 30, the prism of a crescent between two arcs over a chord of 40 (the inner one
#    of the sagitta 14), then two compounds: three cylinders of r 15 stacked along y, 15, 20 and 25 long, and a plate
#    of 60 x 60 x 30 with a cylinder of r 12 through its middle that fills its bore.
#  - The prisms at the element size 5: hexahedra only, the source the grid of its kind - inner nodes of three quads, 4
#    with no corner, 3 with one, 2 with two, and no other inner node without four quads -, the target the same number
#    of quads, a worst scaled Jacobian of the hexahedra of 0.5 at least (the crescent 0.3), no lens or submapping
#    message. The half cylinder and the stadium are submapped sources as well, whose submapped grids are refused or
#    poor (0.195 and 0.259): both are lens sources.
#  - Multi-domain: the stack is swept as three blocks, hexahedra only, its first source a lens grid; the plate and
#    the cylinder are swept side by side, hexahedra only, the source of the cylinder a lens grid.
#  - Refused: the circle of the cylinder pinned to 25 elements is meshed free with "Lens mapping: ... its edges carry
#    25 elements in all, which must be even"; the arcs of the crescent pinned to 13 and 11 make its grid fold, and it
#    is meshed free with "Lens mapping: ... quads of its grid fold". Both are swept all the same.
#  - Second order (hexahedra of 20 nodes, the same counts) on the cylinder and the half cylinder; the full-quad
#    recombination on the elliptic cylinder, the half cylinder and the teardrop (lens grids of their kind); the
#    quasi-structured quad algorithm on the cylinder (no lens source, hexahedra only, no lens message) and
#    transfinite_submapped=False on the cylinder (meshed free, more irregular inner nodes).
#  The model was made by gmsh's OCC kernel through GmshCommon (generator make_lens_sweep.py in the scratch folder of
#  the stage of the lens sweep sources).
#
#  PrePoMax.com -f lens_sweep.brep -u MM_TON_S_C --script Test_Model_Meshing_Lens_Sweep.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import time
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MAX_SIZE = 5.0
NAMES = ["cylinder", "elliptic cylinder", "half cylinder", "lens prism", "teardrop prism", "stadium prism",
         "moon prism", "cylinder stack", "plate with cylinder"]
# The inner nodes of three quads of the source grid of each prism, by its corners
IRREGULAR = {"cylinder": 4, "elliptic cylinder": 4, "half cylinder": 2, "lens prism": 2, "teardrop prism": 3,
             "stadium prism": 4, "moon prism": 2}
MIN_SCALED_JACOBIAN = 0.5
MOON_MIN_SCALED_JACOBIAN = 0.3


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_x():
    """The geometry parts by the shape they were made from, told apart by where they lie along x, and the cells of the
    compounds."""
    parts = {}
    cells = {}
    last_name = None
    for part in pmx.model.geometry.parts:
        if part.part_type == "Compound":
            parts[last_name] = part
            continue
        xs = [c[0] for c in part.get_vertex_data().coordinates]
        last_name = NAMES[int(round(min(xs) / 150.0))]
        if last_name in ("cylinder stack", "plate with cylinder"): cells.setdefault(last_name, []).append(part)
        else: parts[last_name] = part
    check(sorted(parts) == sorted(NAMES), "the parts of lens_sweep.brep were not all found: " + str(sorted(parts)))
    return parts, cells


def messages(part_names, keys=("Lens mapping:", "Submapping:")):
    """The lines of the given kinds the last meshing of the parts wrote to the output window, read off the main form:
    the API returns no mesher output."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    result = []
    for part_name in part_names:
        starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
        if not starts: continue
        for line in lines[starts[-1]:]:
            for key in keys:
                if key in line and line[line.index(key):].strip() not in result:
                    result.append(line[line.index(key):].strip())
    return result


def source_edges(part):
    """The edges of the part in y = 0 by edge id, with their lengths."""
    data = part.get_edge_data()
    return [(int(edge_id), float(length)) for edge_id, length, centroid in
            zip(data.ids, data.lengths, [list(c) for c in data.centroids]) if abs(centroid[1]) < 1E-6]


def sweep(part, second_order=False, pins=(), **arguments):
    """Sweeps the part by one item with the given arguments; pins are (edge id, number of elements). Returns the
    seconds the meshing took."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Lens_Sweep_Size",
                                 pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
                                 relative_size=False, max_size=MAX_SIZE, min_size=MAX_SIZE / 10,
                                 second_order=second_order)
    names = ["PyTest_Lens_Sweep_Size"]
    for edge_id, count in pins:
        name = "PyTest_Lens_Sweep_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_sweep_mesh("PyTest_Lens_Sweep", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         **arguments)
    names.append("PyTest_Lens_Sweep")
    start = time.time()
    try:
        report = pmx.model.mesh.generate([part.name])
    finally:
        items.remove(names)
    check(report[part.name] == "", "sweeping " + part.name + " reported: " + report[part.name])
    return time.time() - start


class SweptMesh(object):
    """The elements of mesh parts by their corner nodes, with the node coordinates."""

    def __init__(self, part_names):
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        self.xyz = {}
        self.elements = []
        self.node_counts = []
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name in part_names]
        check(len(meshed) == len(part_names), "not every part of " + str(part_names) + " was meshed")
        for mesh_part in meshed:
            self.xyz.update(zip([int(i) for i in mesh_part.get_node_ids()],
                                [list(c) for c in mesh_part.get_node_coordinates()]))
            for element_id in mesh_part.get_element_ids():
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

    def cap(self, y):
        """The faces of the elements whose nodes all lie in the plane at y."""
        faces = {8: [(0, 1, 2, 3), (4, 5, 6, 7)], 6: [(0, 1, 2), (3, 4, 5)]}
        result = []
        for element in self.elements:
            for face in faces.get(len(element), []):
                nodes = [element[k] for k in face]
                if all(abs(self.xyz[node][1] - y) < 1E-6 for node in nodes): result.append(nodes)
        return result

    def worst_scaled_jacobian(self):
        """The worst scaled Jacobian of a corner of the hexahedra, the sign taken from most corners."""
        values = []
        for element in self.elements:
            if len(element) != 8: continue
            p = [self.xyz[n] for n in element]
            for c in range(8):
                k = c % 4
                if c < 4: ends = (p[(k + 1) % 4], p[(k + 3) % 4], p[c + 4])
                else: ends = (p[4 + (k + 3) % 4], p[4 + (k + 1) % 4], p[k])
                e = [[end[i] - p[c][i] for i in range(3)] for end in ends]
                n = [e[0][1] * e[1][2] - e[0][2] * e[1][1], e[0][2] * e[1][0] - e[0][0] * e[1][2],
                     e[0][0] * e[1][1] - e[0][1] * e[1][0]]
                lengths = math.sqrt(sum(x * x for x in e[0]) * sum(x * x for x in e[1]) * sum(x * x for x in e[2]))
                values.append(sum(n[i] * e[2][i] for i in range(3)) / lengths if lengths > 0 else -1)
        sign = 1 if sum(1 for v in values if v > 0) * 2 >= len(values) else -1
        return min(sign * v for v in values)


def inner_valences(faces):
    """The numbers of faces round every inner node of a cap without four of them."""
    uses = {}
    valence = {}
    for face in faces:
        for k in range(len(face)):
            edge = tuple(sorted((face[k], face[(k + 1) % len(face)])))
            uses[edge] = uses.get(edge, 0) + 1
            valence[face[k]] = valence.get(face[k], 0) + 1
    boundary = set(node for edge, count in uses.items() if count == 1 for node in edge)
    return sorted(count for node, count in valence.items() if node not in boundary and count != 4)


def check_lens_source(name, mesh, faces, irregular_count, min_scaled_jacobian, target_y=None):
    """The source faces are quads, the grid of a lens face of its kind; the target, when given, as many quads; the
    hexahedra no worse than the floor. Returns the worst scaled Jacobian."""
    check(all(len(face) == 4 for face in faces), "the source of the " + name + " is not quads only")
    valences = inner_valences(faces)
    check(valences == [3] * irregular_count, "the source of the " + name + " has the inner nodes without four quads " +
          str(valences) + ", not " + str(irregular_count) + " of three quads")
    if target_y is not None:
        target = mesh.cap(target_y)
        check(len(target) == len(faces), "the target of the " + name + " has " + str(len(target)) + " faces against " +
              str(len(faces)) + " of its source")
    worst = mesh.worst_scaled_jacobian()
    check(worst >= min_scaled_jacobian, "the hexahedra of the " + name + " have a worst scaled Jacobian of " +
          str(round(worst, 3)) + ", below " + str(min_scaled_jacobian))
    return worst


# Checks ---------------------------------------------------------------------------------------------------------------
def check_prisms(parts):
    rows = []
    for name in ["cylinder", "elliptic cylinder", "half cylinder", "lens prism", "teardrop prism", "stadium prism",
                 "moon prism"]:
        part = parts[name]
        seconds = sweep(part)
        mesh = SweptMesh([part.name])
        kinds = mesh.kinds()
        check(list(kinds.keys()) == ["hexahedron"], "the swept " + name + " must be hexahedra only: " + str(kinds))
        found = messages([part.name])
        check(len(found) == 0, "the swept " + name + " wrote: " + str(found))
        source = mesh.cap(0.0)
        floor = MOON_MIN_SCALED_JACOBIAN if name == "moon prism" else MIN_SCALED_JACOBIAN
        worst = check_lens_source(name, mesh, source, IRREGULAR[name], floor, 40.0)
        rows.append((name, len(mesh.elements), len(source), len(mesh.elements) // len(source), round(worst, 3),
                     round(seconds, 1)))
    print("Prisms at the size 5 (hexahedra, source quads, layers, worst scaled Jacobian, seconds):")
    for row in rows:
        print("  " + row[0] + ": " + ", ".join(str(value) for value in row[1:]))


def check_multi_domain(parts, cells):
    compound = parts["cylinder stack"]
    sweep(compound)
    names = [cell.name for cell in cells["cylinder stack"]]
    mesh = SweptMesh(names)
    kinds = mesh.kinds()
    check(list(kinds.keys()) == ["hexahedron"], "the stack of cylinders must be hexahedra only: " + str(kinds))
    check(len(messages(names + [compound.name])) == 0, "the stack of cylinders wrote: " +
          str(messages(names + [compound.name])))
    source = mesh.cap(0.0)
    worst = check_lens_source("stack of cylinders", mesh, source, 4, MIN_SCALED_JACOBIAN, 60.0)
    print("Stack of three cylinders:", kinds, "first source", len(source), "quads, worst scaled Jacobian",
          round(worst, 3))
    #
    compound = parts["plate with cylinder"]
    sweep(compound)
    names = [cell.name for cell in cells["plate with cylinder"]]
    mesh = SweptMesh(names)
    kinds = mesh.kinds()
    check(list(kinds.keys()) == ["hexahedron"], "the plate with the cylinder must be hexahedra only: " + str(kinds))
    cylinder = [cell for cell in cells["plate with cylinder"] if cell.get_face_data().count == 3]
    check(len(cylinder) == 1, "the plate with the cylinder has no cell of three faces")
    cylinder_mesh = SweptMesh([cylinder[0].name])
    source = cylinder_mesh.cap(0.0)
    worst = check_lens_source("cylinder in the plate", cylinder_mesh, source, 4, MIN_SCALED_JACOBIAN, 30.0)
    print("Plate with a cylinder:", kinds, "source of the cylinder", len(source), "quads, worst scaled Jacobian",
          round(worst, 3), "; of the whole", round(mesh.worst_scaled_jacobian(), 3))


def check_refused(parts):
    part = parts["cylinder"]
    edges = source_edges(part)
    check(len(edges) == 1, "the cylinder has not one edge in y = 0: " + str(edges))
    sweep(part, pins=[(edges[0][0], 25)])
    mesh = SweptMesh([part.name])
    found = messages([part.name])
    print("Cylinder with its circle pinned to 25:", mesh.kinds(), found)
    check(len(found) == 1 and found[0].startswith("Lens mapping:") and "is meshed free" in found[0] and
          "25 elements in all, which must be even" in found[0], "the cylinder pinned to 25 must be meshed free with " +
          "the lens message: " + str(found))
    check(set(mesh.kinds()) <= {"hexahedron", "wedge"}, "the pinned cylinder must be swept: " + str(mesh.kinds()))
    #
    part = parts["moon prism"]
    edges = sorted(source_edges(part), key=lambda edge: -edge[1])
    check(len(edges) == 2, "the crescent has not two edges in y = 0: " + str(edges))
    sweep(part, pins=[(edges[0][0], 13), (edges[1][0], 11)])
    mesh = SweptMesh([part.name])
    found = messages([part.name])
    print("Crescent with its arcs pinned to 13 and 11:", mesh.kinds(), found)
    check(len(found) == 1 and found[0].startswith("Lens mapping:") and "is meshed free" in found[0] and
          "quads of its grid fold" in found[0], "the pinned crescent must be meshed free with the lens message: " +
          str(found))
    check(set(mesh.kinds()) <= {"hexahedron", "wedge"}, "the pinned crescent must be swept: " + str(mesh.kinds()))


def check_variants(parts):
    for name in ["cylinder", "half cylinder"]:
        part = parts[name]
        sweep(part, second_order=True)
        mesh = SweptMesh([part.name])
        check(all(count == 20 for count in mesh.node_counts), "the second order " + name + " has elements of other " +
              "than 20 nodes")
        check_lens_source(name + " (second order)", mesh, mesh.cap(0.0), IRREGULAR[name], MIN_SCALED_JACOBIAN, 40.0)
        print(name + ": second order,", len(mesh.elements), "hexahedra of 20 nodes")
    for name in ["elliptic cylinder", "half cylinder", "teardrop prism"]:
        part = parts[name]
        sweep(part, algorithm_recombine="SimpleFullQuad")
        mesh = SweptMesh([part.name])
        check(list(mesh.kinds().keys()) == ["hexahedron"] and len(messages([part.name])) == 0,
              "the " + name + " under the full-quad recombination: " + str(mesh.kinds()) + " " +
              str(messages([part.name])))
        worst = check_lens_source(name + " (full-quad)", mesh, mesh.cap(0.0), IRREGULAR[name], MIN_SCALED_JACOBIAN,
                                  40.0)
        print(name + ": full-quad recombination,", len(mesh.elements), "hexahedra, worst scaled Jacobian",
              round(worst, 3))
    part = parts["cylinder"]
    sweep(part, algorithm_2d="QuasiStructuredQuad")
    mesh = SweptMesh([part.name])
    found = messages([part.name])
    check(list(mesh.kinds().keys()) == ["hexahedron"] and len(found) == 0,
          "the cylinder under the quasi-structured quad algorithm: " + str(mesh.kinds()) + " " + str(found))
    print("cylinder: quasi-structured quad algorithm,", len(mesh.elements), "hexahedra, inner source nodes without " +
          "four quads", inner_valences(mesh.cap(0.0)))
    sweep(part, transfinite_submapped=False)
    mesh = SweptMesh([part.name])
    valences = inner_valences(mesh.cap(0.0))
    check(list(mesh.kinds().keys()) == ["hexahedron"] and valences != [3] * 4,
          "the cylinder with transfinite_submapped=False must be swept from a free source: " + str(mesh.kinds()) +
          " " + str(valences))
    print("cylinder: transfinite_submapped=False,", len(mesh.elements), "hexahedra, inner source nodes without four " +
          "quads", valences, "worst scaled Jacobian", round(mesh.worst_scaled_jacobian(), 3))


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts, cells = parts_by_x()
    check_prisms(parts)
    check_multi_domain(parts, cells)
    check_refused(parts)
    check_variants(parts)
    print("TEST PASSED")


main()
