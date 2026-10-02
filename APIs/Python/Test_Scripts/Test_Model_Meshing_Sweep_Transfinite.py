#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the structured sources of a gmsh sweep. They are the faces the one definition all PrePoMax meshers follow
#finds, by the transfinite settings of the sweep item, as for a shell: a side may be a chain of edges, the opposite
#sides get one count, a 3-sided source is three blocks of quads around one inner node, and counts the user set that
#make this impossible refuse the meshing instead of a free mesh.
#
#  - box_and_cylinder.brep: the box with one side face split down the middle, swept from its bottom, whose side along
#    the split face is two edges. The mesh is hexahedra only and a structured grid: every node has 1, 2, 4 or 8
#    hexahedra around it.
#  - The quarter cylinder swept along its axis from a quarter disc, a 3-sided source: hexahedra only, and the only
#    irregular nodes lie on one line along the sweep, through the inner node of the three blocks.
#  - The box again with different numbers of elements set on the two opposite sides of its source: refused.
#  - The split box and the quarter cylinder again with QuadWild meshing the sources, which are given the same faces.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Meshing_Sweep_Transfinite.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

SWEEP = "PyTest_Sweep"
SIZE = "PyTest_Size"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def geometry_part(name):
    return [part for part in pmx.model.geometry.parts if part.name == name][0]


def face_selection(part, face_id):
    return pmx.create.selection_by_ids("geometry", "geometry", "surfaces", {part.id: [face_id]})


def edge_selection(part, edge_ids):
    return pmx.create.selection_by_ids("geometry", "geometry", "edges", {part.id: edge_ids})


def face_with_normal(part, axis, sign):
    data = part.get_face_data()
    normals = [list(n) for n in data.normals]
    ids = [int(i) for i in data.ids]
    return [ids[i] for i in range(data.count) if sign * normals[i][axis] > 0.99][0]


def split_side_face(part):
    vertices = [list(c) for c in part.get_vertex_data().coordinates]
    low = [min(c[i] for c in vertices) for i in range(3)]
    high = [max(c[i] for c in vertices) for i in range(3)]
    face = face_with_normal(part, 0, 1)
    y = 0.5 * (low[1] + high[1])
    tool = pmx.create.split_tool_line_by_two_points(points=[[high[0], y, low[2]], [high[0], y, high[2]]])
    selection = pmx.create.selection_by_ids("geometry", "geometry_surfaces", "surfaces", {part.id: [face]})
    added = pmx.model.geometry.parts.split_shell(selection, tool)
    check(added[part.name] == 1, "the side face of " + part.name + " must be split in two: " + str(dict(added)))
    return geometry_part(part.name)


def hexahedron_valences(part_name):
    """The number of hexahedra around every node of the mesh part, and the coordinates of the nodes."""
    data = pmx.model.elements.get_data()
    offsets = [int(i) for i in data.offsets]
    connectivity = [int(i) for i in data.connectivity]
    node_counts = [int(n) for n in data.node_counts]
    index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name][0]
    valence = {}
    for element_id in meshed.get_element_ids():
        i = index[int(element_id)]
        check(node_counts[i] in (8, 20), part_name + " must be meshed by hexahedra only: an element has " +
              str(node_counts[i]) + " nodes")
        for node in connectivity[offsets[i]:offsets[i] + 8]:
            valence[node] = valence.get(node, 0) + 1
    coordinates = dict((int(node_id), list(c)) for node_id, c in zip(meshed.get_node_ids(),
                                                                      meshed.get_node_coordinates()))
    return valence, coordinates


def sweep(part, face_id, algorithm_2d=None):
    items = pmx.model.mesh.setup_items
    if SWEEP in list(items.get_names()):
        items.remove([SWEEP])
    if algorithm_2d is None:
        items.add_sweep_mesh(SWEEP, face_selection(part, face_id))
    else:
        items.add_sweep_mesh(SWEEP, face_selection(part, face_id), algorithm_2d=algorithm_2d)
    check(items[SWEEP].valid, "the sweep mesh item on " + part.name + " is not valid")
    return pmx.model.mesh.create(part.name)[part.name]


def test_split_box(box, algorithm_2d=None):
    report = sweep(box, face_with_normal(box, 2, -1), algorithm_2d)
    what = "the split box" + ("" if algorithm_2d is None else " (" + algorithm_2d + ")")
    check(report == "", "sweeping " + what + " reported: " + report)
    valence, _ = hexahedron_valences(box.name)
    irregular = [node for node, count in valence.items() if count not in (1, 2, 4, 8)]
    print(what + ":", len(valence), "nodes,", len(irregular), "irregular")
    check(len(irregular) == 0, what + " is not swept from a structured source: " + str(len(irregular)) +
          " irregular nodes")


def test_quarter_cylinder(cylinder, algorithm_2d=None):
    data = cylinder.get_face_data()
    ids = [int(i) for i in data.ids]
    normals = [list(n) for n in data.normals]
    # A quarter disc is flat: its normal is the axis, which is the only direction two faces share up to the sign
    flat = [ids[i] for i in range(data.count)
            if any(abs(sum(a * b for a, b in zip(normals[i], normals[j]))) > 0.99 for j in range(data.count) if j != i)]
    check(len(flat) == 2, "the quarter cylinder must have two quarter discs: " + str(flat))
    source = flat[0]
    # Fine enough that a free mesh of the quarter disc would not be three quads
    vertices = [list(c) for c in cylinder.get_vertex_data().coordinates]
    size = min(max(c[k] for c in vertices) - min(c[k] for c in vertices) for k in range(3)) / 8
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters(SIZE, pmx.create.selection_by_ids("geometry", "geometry", "parts", [cylinder.id]),
                                 relative_size=False, max_size=size, min_size=size / 4)
    report = sweep(cylinder, source, algorithm_2d)
    items.remove([SIZE])
    what = "the quarter cylinder" + ("" if algorithm_2d is None else " (" + algorithm_2d + ")")
    check(report == "", "sweeping " + what + " reported: " + report)
    valence, coordinates = hexahedron_valences(cylinder.name)
    irregular = [node for node, count in valence.items() if count not in (1, 2, 4, 8)]
    print(what + ":", len(valence), "nodes,", len(irregular), "irregular")
    check(len(irregular) > 0, what + " must have the inner node of its three blocks")
    check(len(valence) > 8 * len(irregular), what + " is meshed too coarsely to tell three blocks from a free mesh: " +
          str(len(valence)) + " nodes")
    # On one line along the sweep: the irregular nodes differ in one coordinate only
    spread = [max(coordinates[n][k] for n in irregular) - min(coordinates[n][k] for n in irregular) for k in range(3)]
    # QuadWild smooths the nodes, so a little off the line
    check(sorted(spread)[1] < 1e-4 * max(spread), "the irregular nodes of " + what + " do not lie on one line along the sweep: " +
          str(spread))


def test_conflicting_counts(box):
    source = face_with_normal(box, 2, -1)
    # The two edges of the bottom along x, at the lowest and the highest y
    vertices = [list(c) for c in box.get_vertex_data().coordinates]
    low = [min(c[i] for c in vertices) for i in range(3)]
    high = [max(c[i] for c in vertices) for i in range(3)]
    data = box.get_edge_data()
    ids = [int(i) for i in data.ids]
    centroids = [list(c) for c in data.centroids]
    tolerance = 1e-6 * (high[0] - low[0])
    along_x = [ids[i] for i in range(data.count) if abs(centroids[i][2] - low[2]) < tolerance and
               abs(centroids[i][1] - low[1]) < tolerance or abs(centroids[i][2] - low[2]) < tolerance and
               abs(centroids[i][1] - high[1]) < tolerance]
    check(len(along_x) == 2, "the bottom of the box must have two edges along x: " + str(along_x))
    items = pmx.model.mesh.setup_items
    items.add_local_mesh_size(SIZE + "_A", edge_selection(box, [along_x[0]]), number_of_elements=4)
    items.add_local_mesh_size(SIZE + "_B", edge_selection(box, [along_x[1]]), number_of_elements=7)
    try:
        report = sweep(box, source)
    except Exception as ex:
        report = str(ex)
    print("conflicting counts:", report)
    check("cannot all carry one number of elements" in report,
          "the sweep with different counts on opposite sides of its source must be refused: " + report)
    items.remove([SIZE + "_A", SIZE + "_B"])


def main():
    check(pmx.model.is_open(), "a model must be open")
    box = [part for part in pmx.model.geometry.parts if part.get_face_data().count == 6][0]
    cylinder = [part for part in pmx.model.geometry.parts if part.get_face_data().count == 5][0]
    test_conflicting_counts(box)
    box = split_side_face(box)
    test_split_box(box)
    test_quarter_cylinder(cylinder)
    test_split_box(box, "QuadWild")
    test_quarter_cylinder(cylinder, "QuadWild")
    print("TEST PASSED")


main()
