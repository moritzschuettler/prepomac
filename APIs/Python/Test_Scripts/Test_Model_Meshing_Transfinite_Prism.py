#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the transfinite mesh item on a five-faced solid. Its two 3-sided caps are meshed as three blocks of quads
#around one inner node, as the 3-sided faces of a shell are, instead of the triangle gmsh collapses to one corner,
#and the volume is filled layer by layer with hexahedra between them.
#
#  - box_and_cylinder.brep: the quarter cylinder, whose caps are quarter discs. The mesh is hexahedra only, none of
#    them inverted, and the only irregular nodes lie on one line along the axis, through the inner node of the
#    three blocks of both caps.
#  - The quarter cylinder again with numbers of elements set on the three edges of one cap that three blocks of
#    quads cannot fill: refused.
#  - The quarter cylinder again without recombination, which gmsh meshes as before.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Meshing_Transfinite_Prism.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

TRANSFINITE = "PyTest_Transfinite"
SIZE = "PyTest_Size"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def part_selection(part):
    return pmx.create.selection_by_ids("geometry", "geometry", "parts", [part.id])


def edge_selection(part, edge_ids):
    return pmx.create.selection_by_ids("geometry", "geometry", "edges", {part.id: edge_ids})


def mesh_part(part_name):
    return [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name][0]


def mesh(part, recombine=True):
    items = pmx.model.mesh.setup_items
    if TRANSFINITE in list(items.get_names()):
        items.remove([TRANSFINITE])
    items.add_transfinite_mesh(TRANSFINITE, part_selection(part), recombine=recombine)
    check(items[TRANSFINITE].valid, "the transfinite mesh item on " + part.name + " is not valid")
    return pmx.model.mesh.create(part.name)[part.name]


def hexahedron_valences(part_name):
    """The number of hexahedra around every node of the mesh part, and the coordinates of the nodes."""
    data = pmx.model.elements.get_data()
    offsets = [int(i) for i in data.offsets]
    connectivity = [int(i) for i in data.connectivity]
    node_counts = [int(n) for n in data.node_counts]
    index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
    meshed = mesh_part(part_name)
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


def caps(cylinder):
    data = cylinder.get_face_data()
    ids = [int(i) for i in data.ids]
    normals = [list(n) for n in data.normals]
    # A quarter disc is flat: its normal is the axis, which is the only direction two faces share up to the sign
    flat = [ids[i] for i in range(data.count)
            if any(abs(sum(a * b for a, b in zip(normals[i], normals[j]))) > 0.99 for j in range(data.count) if j != i)]
    check(len(flat) == 2, "the quarter cylinder must have two quarter discs: " + str(flat))
    return flat


def set_size(cylinder):
    # Fine enough that a free mesh of the quarter disc would not be three quads
    vertices = [list(c) for c in cylinder.get_vertex_data().coordinates]
    size = min(max(c[k] for c in vertices) - min(c[k] for c in vertices) for k in range(3)) / 8
    pmx.model.mesh.setup_items.add_meshing_parameters(SIZE, part_selection(cylinder), relative_size=False,
                                                      max_size=size, min_size=size / 4)


def test_three_block_caps(cylinder):
    set_size(cylinder)
    report = mesh(cylinder)
    pmx.model.mesh.setup_items.remove([SIZE])
    check(report == "", "meshing the quarter cylinder reported: " + report)
    valence, coordinates = hexahedron_valences(cylinder.name)
    inverted = [int(i) for i in mesh_part(cylinder.name).get_inverted_element_ids()]
    check(len(inverted) == 0, "the quarter cylinder has " + str(len(inverted)) + " inverted hexahedra")
    irregular = [node for node, count in valence.items() if count not in (1, 2, 4, 8)]
    print("the quarter cylinder:", len(valence), "nodes,", len(irregular), "irregular")
    check(len(irregular) > 0, "the quarter cylinder must have the inner node of its three blocks")
    check(len(valence) > 8 * len(irregular), "the quarter cylinder is meshed too coarsely to tell three blocks " +
          "from a free mesh: " + str(len(valence)) + " nodes")
    check(all(valence[node] in (3, 6) for node in irregular),
          "the irregular nodes must have the three quads of the inner node around them: " +
          str(sorted(set(valence[node] for node in irregular))))
    # On one line along the axis: the irregular nodes differ in one coordinate only
    spread = [max(coordinates[n][k] for n in irregular) - min(coordinates[n][k] for n in irregular) for k in range(3)]
    check(sorted(spread)[1] < 1e-6 * max(spread), "the irregular nodes of the quarter cylinder do not lie on one " +
          "line along the axis: " + str(spread))


def test_invalid_cap_counts(cylinder):
    cap = caps(cylinder)[0]
    face_data = cylinder.get_face_data()
    i = [int(face_id) for face_id in face_data.ids].index(cap)
    normal = list(face_data.normals[i])
    centroid = list(face_data.centroids[i])
    data = cylinder.get_edge_data()
    edge_ids = [int(edge_id) for edge_id in data.ids]
    edge_centroids = [list(c) for c in data.centroids]
    edge_lengths = [float(length) for length in data.lengths]
    # The edges of the flat cap have their centroids in its plane
    tolerance = 1e-6 * max(edge_lengths)
    cap_edges = [edge_ids[j] for j in range(data.count)
                 if abs(sum((edge_centroids[j][k] - centroid[k]) * normal[k] for k in range(3))) < tolerance]
    check(len(cap_edges) == 3, "the cap must have three edges: " + str(cap_edges))
    lengths = [edge_lengths[edge_ids.index(e)] for e in cap_edges]
    longest = cap_edges[lengths.index(max(lengths))]
    items = pmx.model.mesh.setup_items
    # The arc carries as many elements as the two straight edges together, which leaves no inner line to it
    for k, edge_id in enumerate(cap_edges):
        items.add_local_mesh_size(SIZE + str(k), edge_selection(cylinder, [edge_id]),
                                  number_of_elements=8 if edge_id == longest else 4)
    try:
        report = mesh(cylinder)
    except Exception as ex:
        report = str(ex)
    print("invalid cap counts:", report)
    check("three blocks of quads fill" in report,
          "numbers of elements three blocks of quads cannot fill must be refused: " + report)
    items.remove([SIZE + str(k) for k in range(3)])


def test_without_recombination(cylinder):
    report = mesh(cylinder, recombine=False)
    check(report == "", "meshing the quarter cylinder without recombination reported: " + report)
    check(len(mesh_part(cylinder.name).get_element_ids()) > 0, "the quarter cylinder without recombination is empty")


def main():
    check(pmx.model.is_open(), "a model must be open")
    cylinder = [part for part in pmx.model.geometry.parts if part.get_face_data().count == 5][0]
    test_three_block_caps(cylinder)
    test_invalid_cap_counts(cylinder)
    test_without_recombination(cylinder)
    print("TEST PASSED")


main()
