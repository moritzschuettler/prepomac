#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the revolve mesh item that finds its own source surfaces. A revolve given a part rather than its
#source surfaces looks for a group of coplanar surfaces the part is the revolution of, and accepts it only
#when every vertex of the part lies on a circle a source vertex turns on and every other surface is the
#source turned through the angle or a surface turning about the axis.
#
#  - box_and_cylinder.brep: the quarter cylinder is revolved from either of its flat radial faces, which
#    touch the axis. The box has no surface whose edges turn about an axis, and is refused.
#  - revolve_stepped_axis.brep: a stepped profile touching the axis, turned through 60 degrees.
#  - revolve_ring.brep: a rectangular section away from the axis, turned through 120 degrees.
#  - revolve_ring_hole.brep: the same ring with a radial bore through its wall. The revolution does not make
#    the bore, so the part is refused, and the refusal names the surface of the bore.
#
#The source surfaces may still be selected by hand, and then the item keeps them and meshes the quarter
#cylinder exactly as the automatic item does. Where the source touches the axis the layers collapse onto it,
#so the elements there are tetrahedra and pyramids; a part clear of the axis is only wedges and hexahedra.
#
#A part the sweep between two ends cannot mesh and that is revolved about an axis its source touches is
#recognized as revolved, and a part with no mesh setup item is meshed by a revolved block of the default
#sweep. A revolved part clear of the axis is left to the sweep.
#
#  - revolve_stepped_axis.brep and revolve_sphere_top.brep, a stepped shaft under a spherical end, are
#    recognized as revolved and meshed without a setup item, as an automatic sweep item meshes them.
#  - revolve_ring.brep is not recognized as revolved, and revolve_ring_hole.brep is swept.
#  - revolve_capsule_split.brep is a capsule split across its axis into two cells, each revolved. The
#    wall between them is meshed once, so the two mesh parts share every node on it.
#  - revolve_split_source.brep is turned through 90 degrees from a source split into five faces onto a target of
#    one face. The vertices inside the source and those that only split an edge of its boundary have no vertex on
#    the target, and turn into it and onto points along its edges.
#  - revolve_split_cells.brep is that part split into two cells, in a compound with a swept part standing on it. An
#    edge of the target of one cell is an edge of a wall of the swept part, which settles its count, so the cell
#    takes it on the source edge turned onto it. The swept part splits the outer wall of the other cell along the
#    revolution, so that cell is turned through layers ending at the angles of the split, and the faces it shares
#    with the swept part are walls of both. Both cells are revolved, nothing is left to tetrahedra, and the three
#    parts share every node on the faces and edges between them.
#  - revolve_sectors.brep is the part split into three sectors, each found revolved on its own, some of them from the
#    other end. In the compound they are turned the same way round, so the target of one is the source of the
#    next, and the compound is meshed by layers alone, sharing every node.
#  - revolve_bracket.brep is the part whole with the bracket. Its source edge under the bracket is turned onto three
#    edges of the bracket, which its sweep settles, and onto a free edge of the target, so the source edge takes the
#    count of the bracket before the target edge follows.
#  - revolve_elbows.brep is a pipe of two straight pieces on either side of each of two elbows. Every straight piece
#    is found swept from its free end towards its elbow, so an elbow is revolved from the target of one piece and
#    is built after it, and the piece ending on the target of the elbow is turned round to start from it. The
#    source edges of an elbow tie the counts of the pieces on its two sides.
#  - revolve_frame.brep is a hexagonal frame of an L shaped section, whose six straight sides are swept and whose six
#    corners are revolved about the edge of their inner corner. The cells form a ring, closed by a swept side ending
#    on the meshed source of the corner after it, through the nodes every revolved and swept cell copied from the
#    one before. The corners collapse into wedges, pyramids and tetrahedra beside their axis, and the sides are
#    meshed by layers alone.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Meshing_Revolve.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
NAME = "PyTest_Revolve"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def raised_message(function):
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def import_parts(file_name):
    before = set(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, file_name))
    return [part for part in pmx.model.geometry.parts if part.name not in before]


def part_selection(part):
    return pmx.create.selection_by_ids("geometry", "geometry", "parts", [part.id])


def face_selection(part, face_id):
    return pmx.create.selection_by_ids("geometry", "geometry_surfaces", "surfaces", {part.id: [face_id]})


def element_kinds(part):
    data = pmx.model.elements.get_data()
    node_counts = dict(zip([int(i) for i in data.ids], [int(n) for n in data.node_counts]))
    names = {4: "tetrahedron", 10: "tetrahedron", 5: "pyramid", 13: "pyramid", 6: "wedge", 15: "wedge",
             8: "hexahedron", 20: "hexahedron"}
    kinds = {}
    for element_id in part.get_element_ids():
        kind = names.get(node_counts[int(element_id)], str(node_counts[int(element_id)]))
        kinds[kind] = kinds.get(kind, 0) + 1
    return kinds


def revolve(part, selection, source_type, angle, off_axis):
    items = pmx.model.mesh.setup_items
    items.add_revolve_mesh(NAME, selection, number_of_elements=4)
    values = items[NAME].get_settings()
    print(source_type + ":", part.name, "angle", values["angle_deg"], "sources", list(values["source_surface_ids"]))
    check(values["source_type"] == source_type, "the selection gave the source type " + str(values["source_type"]))
    check(abs(values["angle_deg"] - angle) < 1E-3, "the revolution angle of " + part.name + " is " +
          str(values["angle_deg"]) + ", expected " + str(angle))
    check(len(list(values["source_surface_ids"])) > 0, "no source surface was found on " + part.name)
    #
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part.name]
    check(len(meshed) == 1, "the part " + part.name + " was not meshed")
    kinds = element_kinds(meshed[0])
    print("meshed:", part.name, kinds)
    if off_axis:
        check("tetrahedron" not in kinds and "pyramid" not in kinds,
              "the revolved part " + part.name + " is not meshed by layers: " + str(kinds))
    check(kinds.get("hexahedron", 0) + kinds.get("wedge", 0) > 0, "the part " + part.name + " has no layers")
    items.remove([NAME])
    return kinds


def refused(part, what):
    items = pmx.model.mesh.setup_items
    message = raised_message(lambda: items.add_revolve_mesh(NAME, part_selection(part)))
    check(message is not None, "the " + what + " " + part.name + " was accepted as a revolution")
    check(NAME not in list(items.get_names()), "the refused item was kept")
    print("refused:", part.name, "-", message.splitlines()[0])
    return message


def mesh_by_default(part):
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "meshing " + part.name + " without a setup item reported: " + report[part.name])
    kinds = element_kinds([p for p in pmx.model.parts if p.name == part.name][0])
    print("default:", part.name, kinds)
    check(kinds.get("hexahedron", 0) + kinds.get("wedge", 0) > kinds.get("tetrahedron", 0) + kinds.get("pyramid", 0),
          "the revolved part " + part.name + " is not meshed by layers: " + str(kinds))
    return kinds


def test_split_capsule():
    added = import_parts("revolve_capsule_split.brep")
    compound = [p for p in added if p.part_type == "Compound"][0]
    cells = [p for p in added if p.part_type != "Compound"]
    check(all(cell.mesh_type == "Revolve" for cell in cells),
          "the cells of the split capsule are recognized as " + str([cell.mesh_type for cell in cells]))
    report = pmx.model.mesh.create(compound.name)
    check(report[compound.name] == "", "meshing the split capsule reported: " + report[compound.name])
    meshed = [p for p in pmx.model.parts if p.name in [cell.name for cell in cells]]
    check(len(meshed) == 2, "the split capsule was not meshed into two parts")
    # Every node on the wall between the cells belongs to both of them
    points = {}
    for part in meshed:
        for node_id, xyz in zip(part.get_node_ids(), part.get_node_coordinates()):
            if abs(float(xyz[2]) - 10) < 1E-6:
                points.setdefault((round(float(xyz[0]), 5), round(float(xyz[1]), 5)), set()).add(int(node_id))
    doubled = [point for point, ids in points.items() if len(ids) > 1]
    print("split capsule:", len(points), "points on the shared wall,", len(doubled), "with two nodes")
    check(len(points) > 0 and len(doubled) == 0, "the cells of the split capsule do not share the nodes of their wall")


def test_split_cells(file_name, expected_types, collapsed_at_axis=False):
    added = import_parts(file_name)
    compound = [p for p in added if p.part_type == "Compound"][0]
    cells = [p for p in added if p.part_type != "Compound"]
    types = sorted(cell.mesh_type for cell in cells)
    check(types == expected_types, "the cells of " + file_name + " are recognized as " + str(types))
    report = pmx.model.mesh.create(compound.name)
    check(report[compound.name] == "", "meshing the split part reported: " + report[compound.name])
    meshed = [p for p in pmx.model.parts if p.name in [cell.name for cell in cells]]
    check(len(meshed) == len(cells) and all(p.element_count > 0 for p in meshed),
          file_name + " was not meshed into " + str(len(cells)) + " parts")
    # A revolved cell whose source touches the axis by a vertex collapses beside it into pyramids and tetrahedra
    cell_types = dict((cell.name, cell.mesh_type) for cell in cells)
    kinds = [element_kinds(p) for p in meshed if not (collapsed_at_axis and cell_types[p.name] == "Revolve")]
    check(all("tetrahedron" not in k and "pyramid" not in k for k in kinds),
          "the split part is not meshed by layers alone: " + str(kinds))
    # Two parts never put two nodes at one point
    points = {}
    for part in meshed:
        for node_id, xyz in zip(part.get_node_ids(), part.get_node_coordinates()):
            points.setdefault(tuple(round(float(c), 5) for c in xyz), set()).add(int(node_id))
    doubled = [point for point, ids in points.items() if len(ids) > 1]
    print("split cells:", file_name, [(p.name, p.element_count) for p in meshed], len(doubled), "points with two nodes")
    check(len(doubled) == 0, "the cells of the split part do not share the nodes between them")


def main():
    check(pmx.model.is_open(), "a model must be open")
    cad = list(pmx.model.geometry.parts)
    box = [part for part in cad if part.get_face_data().count == 6][0]
    cylinder = [part for part in cad if part.get_face_data().count != 6][0]
    #
    automatic = revolve(cylinder, part_selection(cylinder), "Automatic", 90.0, False)
    selected = revolve(cylinder, face_selection(cylinder, 4), "SelectedSurfaces", 90.0, False)
    check(automatic == selected, "the found source meshes differently from the selected one: " +
          str(automatic) + " against " + str(selected))
    refused(box, "box")
    #
    stepped = import_parts("revolve_stepped_axis.brep")[0]
    revolve(stepped, part_selection(stepped), "Automatic", 60.0, False)
    ring = import_parts("revolve_ring.brep")[0]
    revolve(ring, part_selection(ring), "Automatic", 120.0, True)
    bored = import_parts("revolve_ring_hole.brep")[0]
    message = refused(bored, "bored ring")
    check("surface " in message, "the refusal does not name the surface in the way: " + message)
    # The recognition and the default mesh
    check(ring.mesh_type != "Revolve", "the ring clear of the axis is recognized as " + ring.mesh_type)
    check(bored.mesh_type != "Revolve", "the bored ring is recognized as " + bored.mesh_type)
    check(stepped.mesh_type == "Revolve", "the stepped shaft is recognized as " + stepped.mesh_type)
    default_kinds = mesh_by_default(stepped)
    items = pmx.model.mesh.setup_items
    items.add_sweep_mesh(NAME, part_selection(stepped))
    report = pmx.model.mesh.create(stepped.name)
    check(report[stepped.name] == "", "sweeping the stepped shaft reported: " + report[stepped.name])
    swept_kinds = element_kinds([p for p in pmx.model.parts if p.name == stepped.name][0])
    items.remove([NAME])
    check(swept_kinds == default_kinds, "the automatic sweep item meshes the stepped shaft differently from the " +
          "default: " + str(swept_kinds) + " against " + str(default_kinds))
    sphere = import_parts("revolve_sphere_top.brep")[0]
    check(sphere.mesh_type == "Revolve", "the shaft with a spherical end is recognized as " + sphere.mesh_type)
    mesh_by_default(sphere)
    split_source = import_parts("revolve_split_source.brep")[0]
    check(split_source.mesh_type == "Revolve", "the part revolved from a split source is recognized as " +
          split_source.mesh_type)
    kinds = mesh_by_default(split_source)
    check("tetrahedron" not in kinds and "pyramid" not in kinds,
          "the part revolved from a split source is not meshed by layers alone: " + str(kinds))
    test_split_capsule()
    test_split_cells("revolve_split_cells.brep", ["Revolve", "Revolve", "Sweep"])
    test_split_cells("revolve_sectors.brep", ["Revolve", "Revolve", "Revolve", "Sweep"])
    test_split_cells("revolve_bracket.brep", ["Revolve", "Sweep"])
    test_split_cells("revolve_elbows.brep", ["Revolve", "Revolve", "Transfinite", "Transfinite", "Transfinite",
                                             "Transfinite"])
    test_split_cells("revolve_frame.brep", ["Revolve"] * 6 + ["Sweep"] * 6, True)
    print("TEST PASSED")


main()
