#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the structured faces QuadWild meshes. PrePoMax finds them by the one definition all its meshers follow and
#hands them to quad_from_patches in <input>.tfaces, which replaces QuadWild's own detection: every given face is one
#patch with the given corners, its opposite sides get the lower of their counts, a 3-sided face is placed as three
#blocks of quads around one inner node by the mapping all meshers share, and none is left out for the quality of the
#mesh.
#
#  - box_and_cylinder.brep: the open box (its top face deleted) is quadrilaterals only and structured - no node away
#    from the corners of the box has other than four quads around it (two on the open rim).
#  - A side face of the open box split down the middle leaves its neighbours with sides of two edges; the box is still
#    structured.
#  - The open quarter cylinder keeps two quarter circles, each three blocks of quads around one inner node: only the
#    two inner nodes and the vertices are irregular.
#  - The open quarter cylinder with transfinite_three_sided="Polar": each quarter circle is the grid collapsed at its
#    centre, quadrilaterals and a fan of triangles around the centre.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Meshing_QuadWild_Transfinite.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
QUADWILD = "PyTest_QuadWild"


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


def quad_wild(part, three_sided="Mapped"):
    items = pmx.model.mesh.setup_items
    if QUADWILD in list(items.get_names()):
        items.remove([QUADWILD])
    items.add_shell_quad_wild(QUADWILD, part_selection(part), transfinite_three_sided=three_sided,
                              transfinite_four_sided=True)


def mesh(part):
    report = pmx.model.mesh.generate(part.name)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    kinds = element_kinds(part.name)
    print("meshed:", part.name, kinds)
    return kinds


def check_structured_quads(part, what, extra=0):
    kinds = mesh(part)
    check(list(kinds.keys()) == ["quadrilateral"], what + " must be meshed by quadrilaterals only: " + str(kinds))
    irregular = irregular_node_count(part.name)
    allowed = part.get_vertex_data().count + extra
    print("irregular nodes:", irregular, "of at most", allowed)
    check(irregular <= allowed, what + " is not meshed as structured grids: " + str(irregular) + " irregular nodes")


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
    cylinder = [part for part in pmx.model.geometry.parts if part.get_face_data().count == 5][0]
    open_box = delete_face(box, int(list(box.get_face_data().ids)[0]))
    quad_wild(open_box)
    check_structured_quads(open_box, "the open box")
    open_box = split_side_face(open_box)
    quad_wild(open_box)
    check_structured_quads(open_box, "the split open box")
    open_cylinder = delete_face(cylinder, 4)
    quad_wild(open_cylinder)
    check_structured_quads(open_cylinder, "the open quarter cylinder", 2)
    quad_wild(open_cylinder, "Polar")
    check_polar_fan(open_cylinder, "the polar open quarter cylinder")
    print("TEST PASSED")


main()
