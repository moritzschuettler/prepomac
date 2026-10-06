#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks how the mesh of a compound part made by split_solid is divided into mesh parts. sweep_side2.brep
#is two boxes standing side by side and sharing a wall; both are cut in two by a horizontal plane, so the
#split compound has two sub-parts of two cells each.
#
#  - By default (merge_cells=True) each sub-part becomes one mesh part, and the two share the nodes of
#    their wall.
#  - With merge_cells=False every cell is a mesh part of its own, still sharing its nodes with the cells
#    beside it, so the cells can take different sections.
#  - split_compound_mesh separates only the sub-parts when the cells are merged: the wall between the boxes
#    is split, the cut inside each box is not.
#  - split_compound_mesh with merge_cells=False separates every cell.
#  - Splitting the compound again replaces its cells only: the compound keeps its name and id, and the mesh
#    parts of the sub-parts and of the merged compound come back with the ids they had. Meshing parameters
#    and an automatic sweep defined on one sub-part follow it onto its new cells.
#
#  PrePoMax.com -f sweep_side2.brep -u MM_TON_S_C --script Test_Model_Meshing_Split_Cells.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import itertools

MESHING_PARAMETERS_NAME = "Meshing_parameters-Split_cells"
SUB_PART_MESHING_PARAMETERS_NAME = "Meshing_parameters-Sub_part"
SUB_PART_SWEEP_NAME = "Sweep_mesh-Sub_part"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def part_region(parts):
    return pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id for part in parts])


def split_boxes():
    parts = pmx.model.geometry.parts
    compound = [part for part in parts if part.part_type == "Compound"][0]
    tool = pmx.create.split_tool_plane_by_point_and_normal(normal=[0, 0, 1])
    parts.split_solid(part_region([compound]), tool)
    compounds = [part for part in parts if len(list(part.split_sub_part_names)) > 0]
    check(len(compounds) == 1, "the split must leave one split compound, got %d" % len(compounds))
    compound = compounds[0]
    sub_part_names = list(compound.split_sub_part_names)
    check(len(sub_part_names) == 2, "the split compound must have two sub-parts: " + str(sub_part_names))
    cell_names = [part.name for part in parts if part.part_type != "Compound"]
    check(len(cell_names) == 4, "each box must be cut into two cells: " + str(cell_names))
    return compound, sub_part_names, cell_names


def mesh(compound, expected_names, **values):
    items = pmx.model.mesh.setup_items
    items.replace_meshing_parameters(MESHING_PARAMETERS_NAME, **values)
    report = pmx.model.mesh.generate(compound.name)
    check(report[compound.name] == "", "meshing " + compound.name + " reported: " + report[compound.name])
    names = sorted(part.name for part in pmx.model.parts)
    check(names == sorted(expected_names),
          str(values) + ": expected the mesh parts " + str(sorted(expected_names)) + ", got " + str(names))
    node_ids = dict((name, set(pmx.model.parts[name].get_node_ids())) for name in names)
    for name in names:
        print(values, name, len(node_ids[name]), "nodes")
    return node_ids


def shared(node_ids, name_1, name_2):
    return len(node_ids[name_1] & node_ids[name_2])


def main():
    compound, sub_part_names, cell_names = split_boxes()
    cells = [pmx.model.geometry.parts[name] for name in cell_names]
    pmx.model.mesh.setup_items.add_meshing_parameters(MESHING_PARAMETERS_NAME, part_region(cells))
    check(pmx.model.mesh.setup_items[MESHING_PARAMETERS_NAME].valid, "the meshing parameters must be valid")
    #
    merged = mesh(compound, sub_part_names)
    check(shared(merged, *sub_part_names) > 0, "the merged sub-parts must share the nodes of their wall")
    #
    cells = mesh(compound, cell_names, merge_cells=False)
    connected = [pair for pair in itertools.combinations(cell_names, 2) if shared(cells, *pair) > 0]
    check(len(connected) >= 4, "the cells must share their nodes with the cells beside them: " + str(connected))
    #
    split = mesh(compound, sub_part_names, merge_cells=True, split_compound_mesh=True)
    check(shared(split, *sub_part_names) == 0, "the split sub-parts must not share nodes")
    for name in sub_part_names:
        check(len(split[name]) == len(merged[name]),
              "the cut inside " + name + " must not be split: %d nodes instead of %d" %
              (len(split[name]), len(merged[name])))
    #
    split_cells = mesh(compound, cell_names, merge_cells=False, split_compound_mesh=True)
    for pair in itertools.combinations(cell_names, 2):
        check(shared(split_cells, *pair) == 0, "the split cells " + str(pair) + " must not share nodes")
    #
    check_split_again(compound, sub_part_names)
    print("TEST PASSED")


def cells_of(sub_part_name):
    return [part for part in pmx.model.geometry.parts
            if part.part_type != "Compound" and part.name.startswith(sub_part_name + "_cell-")]


def mesh_part_ids():
    return dict((part.name, part.id) for part in pmx.model.parts)


def check_split_again(compound, sub_part_names):
    mesh(compound, sub_part_names, merge_cells=True, split_compound_mesh=False)
    sub_part_ids = mesh_part_ids()
    mesh(compound, [compound.name], merge_compound_parts=True)
    compound_ids = mesh_part_ids()
    check(compound_ids[compound.name] == compound.id,
          "the merged compound mesh must take the id of the compound: " + str(compound_ids))
    #
    parts = pmx.model.geometry.parts
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters(SUB_PART_MESHING_PARAMETERS_NAME, part_region(cells_of(sub_part_names[0])))
    items.add_sweep_mesh(SUB_PART_SWEEP_NAME, part_region(cells_of(sub_part_names[0])))
    parts.split_solid(part_region([compound]), pmx.create.split_tool_plane_by_point_and_normal(normal=[0, 1, 0]))
    new_cell_ids = sorted(part.id for part in cells_of(sub_part_names[0]))
    for name in [SUB_PART_MESHING_PARAMETERS_NAME, SUB_PART_SWEEP_NAME]:
        check(items[name].valid and sorted(items[name].get_geometry_ids()) == new_cell_ids,
              "%s on %s must follow it onto its new cells %s: valid %s, ids %s" %
              (name, sub_part_names[0], new_cell_ids, items[name].valid, sorted(items[name].get_geometry_ids())))
    items.remove([SUB_PART_MESHING_PARAMETERS_NAME, SUB_PART_SWEEP_NAME])
    compounds = [part for part in parts if part.part_type == "Compound"]
    check(len(compounds) == 1 and compounds[0].name == compound.name and compounds[0].id == compound.id,
          "a compound split again must keep its name and id: " + str([(c.name, c.id) for c in compounds]))
    compound = compounds[0]
    check(sorted(compound.split_sub_part_names) == sorted(sub_part_names),
          "a compound split again must keep its sub-parts: " + str(list(compound.split_sub_part_names)))
    cell_ids = [part.id for part in parts if part.part_type != "Compound"]
    check(len(cell_ids) == 8, "each box must now be cut into four cells: %d cells" % len(cell_ids))
    check(not set(cell_ids) & set(sub_part_ids.values()) and compound.id not in cell_ids,
          "no cell may take the id of a sub-part or of the compound: " + str(cell_ids))
    #
    mesh(compound, [compound.name], merge_compound_parts=True)
    check(mesh_part_ids() == compound_ids,
          "the compound meshed again must keep its mesh part and id: " + str(mesh_part_ids()))
    mesh(compound, sub_part_names, merge_compound_parts=False, merge_cells=True)
    check(mesh_part_ids() == sub_part_ids,
          "the sub-parts meshed again must keep their ids: %s instead of %s" % (mesh_part_ids(), sub_part_ids))


main()
