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
#    second of them it cannot be kept, and the meshing is refused. An element size is kept the same way, by a number
#    of elements within a quarter of the one it asks for, which PrePoMax settles among the numbers set and hands to
#    Netgen as set: alone it is kept on its edge and the edges parallel to it, where Netgen's balance took them to the
#    maximum element size, and against a number of elements or a four times larger element size it is refused.
#  - The quarter cylinder that loses a flat radial face keeps two quarter circles, which have three sides and are
#    meshed as three blocks of quads around one inner node by the mapping all meshers share: the shell is
#    quadrilaterals only, and only the two inner nodes and the vertices have other than four quads around them.
#    With transfinite_three_sided="Polar" a quarter circle is the grid collapsed at its centre instead: quadrilaterals
#    and a fan of triangles around the centre.
#  - blind_holes.brep with one cap deleted: the face with the holes is meshed free, the others structured. It is
#    meshed with triangles: Netgen's parallel quad-dominated meshing of a free face fails now and then.
#  - l_profile_fillet.brep: a solid L profile with a filleted inner corner, whose end faces are 3-sided with one side
#    turning through the fillet. Three blocks of quads would fold on them, so with the Mapped layout they are meshed
#    free and the solid meshes, where Netgen refused it. With no structured faces at all, a number of elements set on
#    an edge five times the maximum element size long is still kept.
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


def netgen(part, quad_dominated, three_sided="Mapped"):
    items = pmx.model.mesh.setup_items
    if NETGEN in list(items.get_names()):
        items.remove([NETGEN])
    items.add_netgen_mesh(NETGEN, part_selection(part), quad_dominated=quad_dominated,
                          transfinite_three_sided=three_sided, transfinite_four_sided=True)


def mesh(part):
    report = pmx.model.mesh.generate(part.name)
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


def check_polar_fan(part, what):
    """The 3-sided faces of the part collapsed at their centres: quadrilaterals, and triangles that fan around the
    centre of each of the two quarter circles."""
    kinds = mesh(part)
    check(kinds.get("triangle", 0) > 0 and kinds.get("quadrilateral", 0) > kinds.get("triangle", 0),
          what + " must be quadrilaterals and a fan of triangles: " + str(kinds))
    data = pmx.model.elements.get_data()
    offsets = [int(i) for i in data.offsets]
    connectivity = [int(i) for i in data.connectivity]
    node_counts = [int(n) for n in data.node_counts]
    index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part.name][0]
    triangles = [connectivity[offsets[i]:offsets[i] + 3] for i in (index[int(e)] for e in meshed.get_element_ids())
                 if node_counts[i] in (3, 6)]
    uses = {}
    for triangle in triangles:
        for node in triangle:
            uses[node] = uses.get(node, 0) + 1
    centres = set(sorted(uses, key=lambda node: -uses[node])[:2])
    check(all(any(node in centres for node in triangle) for triangle in triangles),
          "the triangles of " + what + " do not fan around the centres of the quarter circles")


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
        pmx.model.mesh.generate(part.name)
    except Exception as ex:
        refused = True
        print("refused:", str(ex).splitlines()[0])
    check(refused, "edges pinned to different numbers of elements must be refused")
    items.remove(["PyTest_Edge_A", "PyTest_Edge_B"])


def check_l_profile():
    """The L profile with a filleted inner corner: its end faces are 3-sided, a side turning through the fillet, and the
    three blocks of quads would fold on them, so with the Mapped layout they are meshed free and the solid meshes. A
    number of elements set on an edge is kept with no structured faces as well, however much coarser than the
    maximum element size."""
    before = set(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "l_profile_fillet.brep"))
    profile = [part for part in pmx.model.geometry.parts if part.name not in before][0]
    items = pmx.model.mesh.setup_items
    items.add_netgen_mesh(NETGEN + "_L", part_selection(profile), transfinite_three_sided="Mapped",
                          transfinite_four_sided=True)
    mesh(profile)
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == profile.name][0]
    inverted = list(meshed.get_inverted_element_ids())
    check(len(inverted) == 0, "the L profile has inverted elements: " + str(inverted[:10]))
    items.remove([NETGEN + "_L"])
    #
    data = profile.get_edge_data()
    longest = max(range(data.count), key=lambda i: float(data.lengths[i]))
    edge_id = int(list(data.ids)[longest])
    length = float(data.lengths[longest])
    items.add_meshing_parameters(NETGEN + "_Size", part_selection(profile), relative_size=False, max_size=length / 20,
                                 min_size=length / 200, second_order=False)
    items.add_netgen_mesh(NETGEN + "_L", part_selection(profile))
    items.add_local_mesh_size("PyTest_Edge_L", edge_selection(profile, edge_id), number_of_elements=4)
    mesh(profile)
    geometry_id = int(list(data.geometry_ids)[longest])
    count = pmx.create.selection_by_ids("femodel", "nodes", "geometry", [geometry_id]).preview().count - 1
    check(count == 4, "the edge set to 4 elements five times the maximum element size long has " + str(count))
    items.remove([NETGEN + "_L", NETGEN + "_Size", "PyTest_Edge_L"])


def check_element_sizes(part):
    """An element size set on an edge of a structured face is kept on it and on the edges the grid links to it, rather
    than Netgen's balance taking the edges to the maximum element size; against a number of elements or an element size
    the grid cannot agree with, the meshing is refused."""
    items = pmx.model.mesh.setup_items
    first, second = parallel_edge_ids(part)
    data = part.get_edge_data()
    ids = [int(i) for i in data.ids]
    length = float(list(data.lengths)[ids.index(first)])
    geometry_ids = [int(list(data.geometry_ids)[ids.index(edge_id)]) for edge_id in (first, second)]
    items.add_meshing_parameters("PyTest_Sizes", part_selection(part), relative_size=False, max_size=length / 4,
                                 min_size=length / 100, second_order=False)
    items.add_local_mesh_size("PyTest_Edge_A", edge_selection(part, first), element_size=length / 20)
    mesh(part)
    counts = [pmx.create.selection_by_ids("femodel", "nodes", "geometry", [geometry_id]).preview().count - 1
              for geometry_id in geometry_ids]
    check(counts == [20, 20], "the element size asking for 20 elements must be kept on both edges: " + str(counts))
    for name, values in (("a number of elements", {"number_of_elements": 3}),
                         ("an element size four times larger", {"element_size": length / 5})):
        items.add_local_mesh_size("PyTest_Edge_B", edge_selection(part, second), **values)
        message = None
        try:
            pmx.model.mesh.generate(part.name)
        except Exception as ex:
            message = str(ex)
        check(message is not None and "by its element size" in message and "   at " not in message,
              "an element size against " + name + " on linked edges must be refused: " + str(message))
        items.remove(["PyTest_Edge_B"])
    items.remove(["PyTest_Edge_A", "PyTest_Sizes"])


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
    added = pmx.model.geometry.parts.split_face(selection, tool)
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
    check_element_sizes(open_box)
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
    netgen(open_cylinder, True, "Polar")
    check_polar_fan(open_cylinder, "the polar open quarter cylinder")
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
    check_l_profile()
    print("TEST PASSED")


main()
