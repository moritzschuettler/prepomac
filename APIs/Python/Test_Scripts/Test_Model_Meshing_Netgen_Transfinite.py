#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the structured faces Netgen meshes. PrePoMax finds them by the one definition all its meshers follow and
#hands them to Netgen, which meshes them as given: a side is a chain of edges, opposite sides are balanced to the
#coarser count, a number of elements set on an edge is never changed, and when the set numbers cannot agree the
#meshing is refused. A quad-dominated shell gets quads on its structured faces, otherwise every grid cell is split
#into two triangles.
#
#  - box_and_cylinder.brep: the box that loses its top face is an open box of five rectangles. Meshed by Netgen
#    quad-dominated with transfinite faces it is quadrilaterals only, and no node away from the corners of the box
#    has other than four quads around it (two on the open rim). Without quad-dominated it is triangles only.
#  - A side face of the open box split down the middle leaves its neighbours with sides of two edges, and the box
#    is still meshed as structured grids.
#  - A number of elements set on one edge carries over to the edges parallel to it; set to a different number on a
#    second of them it cannot be kept, and the meshing is refused.
#  - The quarter cylinder that loses a flat radial face keeps two quarter circles, which have three sides and are
#    meshed as three blocks of quads around one inner node by the mapping all meshers share: the shell is
#    quadrilaterals only, and only the two inner nodes and the vertices have other than four quads around them.
#  - blind_holes.brep with one cap deleted: the face with the holes is meshed free, the others structured. It is
#    meshed with triangles: Netgen's parallel quad-dominated meshing of a free face fails now and then.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Meshing_Netgen_Transfinite.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
NETGEN = "PyTest_Netgen"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def part_selection(part):
    return pmx.create.selection_by_ids("geometry", "geometry", "parts", [part.id])


def edge_selection(part, edge_id):
    return pmx.create.selection_by_ids("geometry", "geometry", "edges", {part.id: [edge_id]})


def geometry_part(name):
    return [part for part in pmx.model.geometry.parts if part.name == name][0]


def delete_face(part, face_id):
    selection = pmx.create.selection_by_ids("geometry", "geometry", "surfaces", {part.id: [face_id]})
    deleted = pmx.model.geometry.parts.delete_cad_faces(selection)
    check(deleted[part.name] == 1, "one face must be deleted from " + part.name + ": " + str(dict(deleted)))
    shell = geometry_part(part.name)
    check(shell.part_type == "Shell", "the part " + part.name + " must be a shell: " + shell.part_type)
    return shell


def element_kinds(part_name):
    data = pmx.model.elements.get_data()
    node_counts = dict(zip([int(i) for i in data.ids], [int(n) for n in data.node_counts]))
    names = {3: "triangle", 6: "triangle", 4: "quadrilateral", 8: "quadrilateral"}
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name][0]
    kinds = {}
    for element_id in meshed.get_element_ids():
        kind = names.get(node_counts[int(element_id)], str(node_counts[int(element_id)]))
        kinds[kind] = kinds.get(kind, 0) + 1
    return kinds


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


def netgen(part, quad_dominated):
    items = pmx.model.mesh.setup_items
    if NETGEN in list(items.get_names()):
        items.remove([NETGEN])
    items.add_netgen_mesh(NETGEN, part_selection(part), quad_dominated=quad_dominated,
                          transfinite_three_sided=True, transfinite_four_sided=True)


def mesh(part):
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    kinds = element_kinds(part.name)
    print("meshed:", part.name, kinds)
    return kinds


def check_structured_quads(part, what):
    kinds = mesh(part)
    check(list(kinds.keys()) == ["quadrilateral"], what + " must be meshed by quadrilaterals only: " + str(kinds))
    irregular = irregular_node_count(part.name)
    corners = part.get_vertex_data().count
    print("irregular nodes:", irregular, "of at most", corners, "vertices")
    check(irregular <= corners, what + " is not meshed as structured grids: " + str(irregular) + " irregular nodes")


def parallel_edge_ids(part):
    data = part.get_edge_data()
    by_length = {}
    for edge_id, length in zip([int(i) for i in data.ids], [float(l) for l in data.lengths]):
        by_length.setdefault(round(length, 6), []).append(edge_id)
    pairs = [edge_ids for edge_ids in by_length.values() if len(edge_ids) >= 2]
    check(len(pairs) > 0, "the open box has no two edges of the same length")
    return pairs[0][0], pairs[0][1]


def check_pinned_edges(part):
    items = pmx.model.mesh.setup_items
    first, second = parallel_edge_ids(part)
    items.add_local_mesh_size("PyTest_Edge_A", edge_selection(part, first), number_of_elements=7)
    mesh(part)
    items.add_local_mesh_size("PyTest_Edge_B", edge_selection(part, second), number_of_elements=9)
    refused = False
    try:
        pmx.model.mesh.create(part.name)
    except Exception as ex:
        refused = True
        print("refused:", str(ex).splitlines()[0])
    check(refused, "edges pinned to different numbers of elements must be refused")
    items.remove(["PyTest_Edge_A", "PyTest_Edge_B"])


def split_side_face(part):
    vertices = [list(c) for c in part.get_vertex_data().coordinates]
    low = [min(c[i] for c in vertices) for i in range(3)]
    high = [max(c[i] for c in vertices) for i in range(3)]
    data = part.get_face_data()
    normals = [list(n) for n in data.normals]
    face = [int(list(data.ids)[i]) for i in range(data.count) if normals[i][0] > 0.99][0]
    y = 0.5 * (low[1] + high[1])
    tool = pmx.create.split_tool_line_by_two_points(points=[[high[0], y, low[2]], [high[0], y, high[2]]])
    selection = pmx.create.selection_by_ids("geometry", "geometry_surfaces", "surfaces", {part.id: [face]})
    added = pmx.model.geometry.parts.split_shell(selection, tool)
    check(added[part.name] == 1, "the side face of " + part.name + " must be split in two: " + str(dict(added)))
    return geometry_part(part.name)


def main():
    check(pmx.model.is_open(), "a model must be open")
    box = [part for part in pmx.model.geometry.parts if part.get_face_data().count == 6][0]
    open_box = delete_face(box, int(list(box.get_face_data().ids)[0]))
    #
    netgen(open_box, True)
    check_structured_quads(open_box, "the open box")
    netgen(open_box, False)
    kinds = mesh(open_box)
    check(list(kinds.keys()) == ["triangle"], "without quad-dominated the open box must be triangles: " + str(kinds))
    netgen(open_box, True)
    check_pinned_edges(open_box)
    #
    open_box = split_side_face(open_box)
    netgen(open_box, True)
    check_structured_quads(open_box, "the split open box")
    #
    cylinder = [part for part in pmx.model.geometry.parts if part.get_face_data().count == 5][0]
    open_cylinder = delete_face(cylinder, 4)
    netgen(open_cylinder, True)
    kinds = mesh(open_cylinder)
    check(list(kinds.keys()) == ["quadrilateral"], "the open quarter cylinder must be quadrilaterals: " + str(kinds))
    irregular = irregular_node_count(open_cylinder.name)
    allowed = open_cylinder.get_vertex_data().count + 2
    print("irregular nodes:", irregular, "of at most", allowed, "vertices and inner nodes")
    check(irregular <= allowed, "the quarter circles are not meshed as three blocks: " + str(irregular))
    #
    before = set(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "blind_holes.brep"))
    holes = [part for part in pmx.model.geometry.parts if part.name not in before][0]
    data = holes.get_face_data()
    caps = [int(list(data.ids)[i]) for i in range(data.count)
            if list(data.types)[i] == "Plane" and float(list(data.areas)[i]) < 100.0]
    holes = delete_face(holes, caps[0])
    netgen(holes, False)
    kinds = mesh(holes)
    check(list(kinds.keys()) == ["triangle"], "the shell with holes must be meshed by triangles: " + str(kinds))
    print("TEST PASSED")


main()
