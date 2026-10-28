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
#    edges it cannot be kept, and the meshing is refused rather than overriding either of them. An element size is
#    kept the same way, by a number of elements within a quarter of the one it asks for: alone it is kept on its edge
#    and the edges parallel to it, and against a number of elements or an element size four times larger set on one
#    of those it is refused.
#  - The open quarter cylinder by a gmsh shell item with transfinite_three_sided="Polar": a quarter circle is the grid
#    gmsh collapses at its centre, so its triangles fan around that one node and everything else is quadrilaterals.
#  - The open quarter cylinder by a Netgen item with quad_dominated=True keeps every face and its area. Netgen meshing
#    its faces in parallel joined the quadrilaterals of every face but the first to the points of other faces, which
#    came out as one mesh face many times the area of the shell.
#  - shell_end_chain.brep, imported by the script: a tube with rounded ends, whose end faces are four-sided with one
#    side a chain of five curved edges, from a fraction of a millimetre to almost 10 mm long, that the curvature sizes
#    at 24 elements against 6 on the side across. Lowering the chain, the balance took every element but one from its
#    longest edges, which were the cheapest to take, and the mesh had elements of 11 mm against a maximum size of 3. No
#    element is longer than the maximum size by more than a tenth, and every edge keeps the elements that hold it within
#    the maximum size.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Meshing_Shell_Types.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")


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
    report = pmx.model.mesh.generate(part.name)
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
    report = pmx.model.mesh.generate(part.name)
    check(report[part.name] == "", "one pinned edge must mesh: " + report[part.name])
    items.add_local_mesh_size("PyTest_Edge_B", edge_selection(part, second), number_of_elements=9)
    message = None
    try:
        pmx.model.mesh.generate(part.name)
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
    added = pmx.model.geometry.parts.split_face(selection, tool)
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


def check_polar(part):
    items = pmx.model.mesh.setup_items
    items.add_shell_gmsh("PyTest_Polar", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine="Simple", transfinite_three_sided="Polar")
    kinds = mesh(part)
    items.remove(["PyTest_Polar"])
    check(kinds.get("triangle", 0) > 0 and kinds.get("quadrilateral", 0) > kinds.get("triangle", 0),
          "the polar open quarter cylinder must be quadrilaterals and a fan of triangles: " + str(kinds))
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
    # One centre for each of the two quarter circles
    centres = set(sorted(uses, key=lambda node: -uses[node])[:2])
    check(all(any(node in centres for node in triangle) for triangle in triangles),
          "the triangles of the polar open quarter cylinder do not fan around the centres of the quarter circles")


def check_netgen_quad_dominated(part):
    items = pmx.model.mesh.setup_items
    items.add_netgen_mesh("PyTest_Netgen_Quad", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                          quad_dominated=True)
    kinds = mesh(part)
    items.remove(["PyTest_Netgen_Quad"])
    check(kinds.get("quadrilateral", 0) > 0, "the quad-dominated Netgen mesh has no quadrilaterals: " + str(kinds))
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part.name][0]
    faces = meshed.get_face_data()
    area = sum(float(a) for a in faces.areas)
    print("Netgen quad-dominated:", faces.count, "of", part.get_face_data().count, "faces, area", area, "of",
          part.surface_area)
    check(faces.count == part.get_face_data().count,
          "the quad-dominated Netgen mesh has " + str(faces.count) + " faces instead of " +
          str(part.get_face_data().count))
    check(abs(area - part.surface_area) < 0.01 * part.surface_area,
          "the quad-dominated Netgen mesh has the area " + str(area) + " instead of " + str(part.surface_area))


def check_element_sizes(part):
    """An element size set on an edge of a structured face is kept, within a quarter of the number of elements it asks
    for, on it and on the edges the grid links to it; against a number of elements or an element size the grid cannot
    agree with, the meshing is refused."""
    items = pmx.model.mesh.setup_items
    first, second = parallel_edge_ids(part)
    data = part.get_edge_data()
    ids = [int(i) for i in data.ids]
    length = float(list(data.lengths)[ids.index(first)])
    geometry_ids = [int(list(data.geometry_ids)[ids.index(edge_id)]) for edge_id in (first, second)]
    items.add_meshing_parameters("PyTest_Sizes", pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
                                 relative_size=False, max_size=length / 4, min_size=length / 100, second_order=False)
    items.add_local_mesh_size("PyTest_Edge_A", edge_selection(part, first), element_size=length / 20)
    report = pmx.model.mesh.generate(part.name)
    check(report[part.name] == "", "one element size must mesh: " + report[part.name])
    counts = [pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().count - 1
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


def check_max_size_kept():
    """A chain of short curved edges lowered against the side across keeps every edge within the maximum size."""
    max_size = 3.0
    names = set(part.name for part in pmx.model.geometry.parts)
    pmx.import_file(os.path.join(MODELS, "shell_end_chain.brep"))
    part = [part for part in pmx.model.geometry.parts if part.name not in names][0]
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Chain", pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
                                 relative_size=False, max_size=max_size, min_size=0.26, grading=0.3,
                                 elements_per_edge=2.0, elements_per_curve=2.0, second_order=False)
    kinds = mesh(part)
    check(list(kinds.keys()) == ["quadrilateral"], "the tube must be meshed by quadrilaterals only: " + str(kinds))
    #
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part.name][0]
    coordinates = dict(zip([int(i) for i in meshed.get_node_ids()], [list(c) for c in meshed.get_node_coordinates()]))
    data = pmx.model.elements.get_data()
    offsets = [int(i) for i in data.offsets]
    connectivity = [int(i) for i in data.connectivity]
    index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
    longest = 0
    for element_id in meshed.get_element_ids():
        i = index[int(element_id)]
        corners = [coordinates[node] for node in connectivity[offsets[i]:offsets[i] + 4]]
        for k in range(4):
            longest = max(longest, math.sqrt(sum((corners[k][j] - corners[(k + 1) % 4][j]) ** 2 for j in range(3))))
    print("longest element edge:", longest, "of the maximum size", max_size)
    check(longest <= 1.1 * max_size, "an element edge is " + str(longest) + " long against the maximum size " +
          str(max_size))
    #
    edges = part.get_edge_data()
    short = []
    for geometry_id, length in zip([int(i) for i in edges.geometry_ids], [float(l) for l in edges.lengths]):
        count = pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().count - 1
        if count < math.ceil(length / max_size - 1E-6):
            short.append((geometry_id, round(length, 3), count))
    check(len(short) == 0, "edges given fewer elements than the maximum size needs (id, length, count): " + str(short))
    items.remove(["PyTest_Chain"])


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
    check_element_sizes(open_box)
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
    check_polar(open_cylinder)
    check_netgen_quad_dominated(open_cylinder)
    check_max_size_kept()
    print("TEST PASSED")


main()
