#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the default mesh of CAD shells. A CAD shell without a mesh setup item is meshed by gmsh the way a
#sweep meshes its source: a structured grid on the three and four sided faces, a free mesh on the others,
#and all of it recombined into quadrilaterals. Its recognized mesh type is Free, since which of its faces
#are structured is told face by face.
#
#  - box_and_cylinder.brep: the box that loses its top face is an open box of five rectangles, all of which
#    carry a grid, so its mesh is quadrilaterals only. The quarter cylinder that loses a flat radial face
#    keeps its curved face, its other radial face and its two quarter circles. A quarter circle has three
#    sides, and is meshed as three blocks of quads around one inner node, so the whole shell is quadrilaterals,
#    and only the two inner nodes and the vertices have other than four quads around them.
#  - A side of a structured face is a chain of edges. A side face of the open box split down the middle leaves
#    a vertex part way along the edges of its neighbours, whose sides are then two edges each, and the box is
#    still meshed as structured grids: no node away from the corners of the box has other than four quads
#    around it (two on the open rim).
#  - A structured grid divides the opposite sides of a face equally, so the number of elements set on one edge
#    of the open box carries over to the edges parallel to it. Set to a different number on a second of those
#    edges it cannot be kept, and the meshing is refused rather than overriding either of them.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Meshing_Shell_Types.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def face_ids(part):
    return [int(face_id) for face_id in part.get_face_data().ids]


def open_shell(part, face_id):
    selection = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {part.id: [face_id]})
    deleted = pmx.model.geometry.parts.delete_cad_faces(selection)
    check(deleted[part.name] == 1, "one face must be deleted from " + part.name + ": " + str(dict(deleted)))
    shell = [geometry_part for geometry_part in pmx.model.geometry.parts if geometry_part.name == part.name][0]
    check(shell.part_type == "Shell", "the part " + part.name + " must be a shell: " + shell.part_type)
    return shell


def element_kinds(part_name):
    data = pmx.model.elements.get_data()
    node_counts = dict(zip([int(i) for i in data.ids], [int(n) for n in data.node_counts]))
    names = {3: "triangle", 6: "triangle", 4: "quadrilateral", 8: "quadrilateral"}
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name]
    check(len(meshed) == 1, "the part " + part_name + " was not meshed")
    kinds = {}
    for element_id in meshed[0].get_element_ids():
        kind = names.get(node_counts[int(element_id)], str(node_counts[int(element_id)]))
        kinds[kind] = kinds.get(kind, 0) + 1
    return kinds


def mesh(part):
    check(part.mesh_type == "Free", "the CAD shell " + part.name + " is recognized as " + part.mesh_type)
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    kinds = element_kinds(part.name)
    print("meshed:", part.name, kinds)
    return kinds


def parallel_edge_ids(part):
    """Two edges of the open box that run the same way, which the grid of its faces links."""
    data = part.get_edge_data()
    ids = [int(i) for i in data.ids]
    lengths = [float(length) for length in data.lengths]
    by_length = {}
    for edge_id, length in zip(ids, lengths):
        by_length.setdefault(round(length, 6), []).append(edge_id)
    pairs = [edge_ids for edge_ids in by_length.values() if len(edge_ids) >= 2]
    check(len(pairs) > 0, "the open box has no two edges of the same length: " + str(by_length))
    return pairs[0][0], pairs[0][1]


def edge_selection(part, edge_id):
    return pmx.create.selection_by_ids('geometry', 'geometry', 'edges', {part.id: [edge_id]})


def check_pinned_edges(part):
    items = pmx.model.mesh.setup_items
    first, second = parallel_edge_ids(part)
    items.add_local_mesh_size("PyTest_Edge_A", edge_selection(part, first), number_of_elements=7)
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "one pinned edge must mesh: " + report[part.name])
    items.add_local_mesh_size("PyTest_Edge_B", edge_selection(part, second), number_of_elements=9)
    message = None
    try:
        pmx.model.mesh.create(part.name)
    except Exception as ex:
        message = str(ex)
    check(message is not None, "edges pinned to different numbers of elements must be refused")
    print("refused:", [line for line in message.splitlines() if "cannot be given the same" in line])
    check("cannot be given the same" in message and "Edge " in message and "Surface " in message,
          "the refusal must name the edges and the surfaces: " + message)
    items.remove(["PyTest_Edge_A", "PyTest_Edge_B"])


def irregular_node_count(part_name):
    """The nodes of a quad mesh that do not have four quads around them, or two on the free boundary."""
    data = pmx.model.elements.get_data()
    offsets = [int(i) for i in data.offsets]
    connectivity = [int(i) for i in data.connectivity]
    index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name][0]
    valence = {}
    edge_uses = {}
    for element_id in meshed.get_element_ids():
        i = index[int(element_id)]
        corners = connectivity[offsets[i]:offsets[i] + 4]
        for k in range(4):
            valence[corners[k]] = valence.get(corners[k], 0) + 1
            edge = tuple(sorted((corners[k], corners[(k + 1) % 4])))
            edge_uses[edge] = edge_uses.get(edge, 0) + 1
    boundary = set(node for edge, uses in edge_uses.items() if uses == 1 for node in edge)
    return sum(1 for node, count in valence.items() if count != (2 if node in boundary else 4))


def split_side_face(part):
    vertices = [list(c) for c in part.get_vertex_data().coordinates]
    low = [min(c[i] for c in vertices) for i in range(3)]
    high = [max(c[i] for c in vertices) for i in range(3)]
    data = part.get_face_data()
    normals = [list(n) for n in data.normals]
    face = [int(list(data.ids)[i]) for i in range(data.count) if normals[i][0] > 0.99][0]
    y = 0.5 * (low[1] + high[1])
    tool = pmx.create.split_tool_line_by_two_points(points=[[high[0], y, low[2]], [high[0], y, high[2]]])
    selection = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {part.id: [face]})
    added = pmx.model.geometry.parts.split_shell(selection, tool)
    check(added[part.name] == 1, "the side face of " + part.name + " must be split in two: " + str(dict(added)))
    return [geometry_part for geometry_part in pmx.model.geometry.parts if geometry_part.name == part.name][0]


def check_chained_sides(part):
    shell = split_side_face(part)
    kinds = mesh(shell)
    check(list(kinds.keys()) == ["quadrilateral"], "the split open box must be meshed by quadrilaterals only: " +
          str(kinds))
    irregular = irregular_node_count(shell.name)
    corners = shell.get_vertex_data().count
    print("irregular nodes:", irregular, "of at most", corners, "box vertices")
    check(irregular <= corners, "the split open box is not meshed as structured grids: " + str(irregular) +
          " irregular nodes")


def main():
    check(pmx.model.is_open(), "a model must be open")
    cad = list(pmx.model.geometry.parts)
    box = [part for part in cad if part.get_face_data().count == 6][0]
    cylinder = [part for part in cad if part.get_face_data().count != 6][0]
    check(box.mesh_type == "Transfinite", "the CAD box is recognized as " + box.mesh_type)
    #
    open_box = open_shell(box, face_ids(box)[0])
    kinds = mesh(open_box)
    check(list(kinds.keys()) == ["quadrilateral"], "the open box must be meshed by quadrilaterals only: " + str(kinds))
    check_pinned_edges(open_box)
    check_chained_sides(open_box)
    #
    open_cylinder = open_shell(cylinder, 4)
    kinds = mesh(open_cylinder)
    check(list(kinds.keys()) == ["quadrilateral"],
          "the open quarter cylinder must be meshed by quadrilaterals only: " + str(kinds))
    irregular = irregular_node_count(open_cylinder.name)
    allowed = open_cylinder.get_vertex_data().count + 2
    print("irregular nodes:", irregular, "of at most", allowed, "vertices and inner nodes")
    check(irregular <= allowed, "the quarter circles of the open quarter cylinder are not meshed as three blocks: " +
          str(irregular) + " irregular nodes")
    print("PASSED")


main()
