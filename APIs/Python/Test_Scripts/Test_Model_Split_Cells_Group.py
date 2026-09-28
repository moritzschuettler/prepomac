#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks group_split_cells, which puts cells of a compound part made by split_solid under one sub-part
#without changing the geometry. sweep_side2.brep is two boxes standing side by side and sharing a wall;
#both are cut in two by a horizontal plane, so the split compound has two sub-parts, one per box, of a
#bottom and a top cell each.
#
#  - A grouping that would change nothing, or only the name of a sub-part, is refused before anything is
#    recorded, as are cells of another part and a name another part uses.
#  - The two bottom cells grouped under a new name become a sub-part of their own, and each box keeps its
#    top cell. Meshed with the cells merged, every sub-part is one mesh part, with a part id of its own.
#  - Without a name, a sub-part picked whole together with further cells takes them in, and the sub-part
#    left without cells is gone.
#  - Without a name and without a whole sub-part, a new sub-part is created.
#  - A new sub-part is listed after the sub-parts the compound already has.
#
#  PrePoMax.com -f sweep_side2.brep -u MM_TON_S_C --script Test_Model_Split_Cells_Group.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def refused(call, words):
    try:
        call()
    except Exception as ex:
        message = str(ex)
        check(words in message, "the refusal must say '" + words + "': " + message)
        return
    raise Exception("TEST FAILED: the call was not refused, expected '" + words + "'")


def part_region(parts):
    return pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id for part in parts])


def split_boxes():
    parts = pmx.model.geometry.parts
    compound = [part for part in parts if part.part_type == "Compound"][0]
    tool = pmx.create.split_tool_plane_by_point_and_normal(normal=[0, 0, 1])
    parts.split_solid(part_region([compound]), tool)
    compound = [part for part in parts if len(list(part.split_sub_part_names)) > 0][0]
    check(len(list(compound.split_sub_part_names)) == 2,
          "the split compound must have two sub-parts: " + str(list(compound.split_sub_part_names)))
    return compound


def z_of(cell_name):
    return float(list(pmx.model.geometry.parts[cell_name].center_of_mass)[2])


def bottom_and_top(compound, sub_part_name):
    cells = sorted(compound.get_split_cell_names(sub_part_name), key=z_of)
    check(len(cells) == 2, sub_part_name + " must have two cells: " + str(cells))
    return cells[0], cells[1]


def state(compound):
    return dict((name, list(compound.get_split_cell_names(name))) for name in compound.split_sub_part_names)


def test_refusals(compound, box_1, box_2):
    parts = pmx.model.geometry.parts
    before = state(compound)
    other = [part.name for part in parts if part.part_type != "Compound"][0]
    bottom_1, top_1 = bottom_and_top(compound, box_1)
    refused(lambda: parts.group_split_cells(other, [bottom_1]), "not a compound part")
    refused(lambda: parts.group_split_cells(compound.name, []), "cell_names")
    refused(lambda: parts.group_split_cells(compound.name, ['Missing_cell']), "not a cell")
    refused(lambda: parts.group_split_cells(compound.name, [bottom_1, bottom_1]), "more than once")
    refused(lambda: parts.group_split_cells(compound.name, [bottom_1], box_1), "already belong")
    refused(lambda: parts.group_split_cells(compound.name, [bottom_1, top_1]), "already belong")
    refused(lambda: parts.group_split_cells(compound.name, [bottom_1, top_1], 'Box'), "Rename the sub-part")
    refused(lambda: parts.group_split_cells(compound.name, [bottom_1], 'Bottom part'), "sub_part_name")
    refused(lambda: parts.group_split_cells(compound.name, [bottom_1], compound.name.upper()), "already used")
    check(state(compound) == before, "a refused grouping must change nothing: " + str(state(compound)))


def mesh(compound, expected_names):
    report = pmx.model.mesh.create(compound.name)
    check(report[compound.name] == "", "meshing " + compound.name + " reported: " + report[compound.name])
    names = sorted(part.name for part in pmx.model.parts)
    check(names == sorted(expected_names), "expected the mesh parts " + str(sorted(expected_names)) +
          ", got " + str(names))
    ids = [part.id for part in pmx.model.parts]
    check(len(set(ids)) == len(ids), "every sub-part must be meshed with an id of its own: " + str(ids))


def main():
    compound = split_boxes()
    parts = pmx.model.geometry.parts
    box_1, box_2 = list(compound.split_sub_part_names)
    test_refusals(compound, box_1, box_2)
    #
    bottom_1, top_1 = bottom_and_top(compound, box_1)
    bottom_2, top_2 = bottom_and_top(compound, box_2)
    name = parts.group_split_cells(compound.name, [bottom_1, bottom_2], 'Bottom')
    check(name == 'Bottom', "the grouping must return the name it was given: " + name)
    check(list(compound.split_sub_part_names) == [box_1, box_2, 'Bottom'],
          "the bottom cells must form a sub-part of their own: " + str(list(compound.split_sub_part_names)))
    check(list(compound.get_split_cell_names(box_1)) == [box_1],
          "a sub-part left with one cell names the cell after itself: " + str(state(compound)))
    check(len(compound.get_split_cell_names('Bottom')) == 2, "Bottom must hold two cells: " + str(state(compound)))
    mesh(compound, [box_1, box_2, 'Bottom'])
    #
    bottom_cells = list(compound.get_split_cell_names('Bottom'))
    name = parts.group_split_cells(compound.name, bottom_cells + [box_1])
    check(name == 'Bottom', "a whole sub-part picked with further cells must take them in: " + name)
    check(list(compound.split_sub_part_names) == [box_2, 'Bottom'],
          "the sub-part left without cells must be gone: " + str(list(compound.split_sub_part_names)))
    check(len(compound.get_split_cell_names('Bottom')) == 3, "Bottom must hold three cells: " + str(state(compound)))
    mesh(compound, [box_2, 'Bottom'])
    #
    first_cell = list(compound.get_split_cell_names('Bottom'))[0]
    name = parts.group_split_cells(compound.name, [first_cell])
    check(name not in [box_2, 'Bottom'], "a cell taken out alone must get a new sub-part: " + name)
    check(list(compound.split_sub_part_names) == [box_2, 'Bottom', name],
          "the new sub-part must be listed last: " + str(list(compound.split_sub_part_names)))
    check(list(compound.get_split_cell_names(name)) == [name], "its only cell is named after it: " +
          str(state(compound)))
    mesh(compound, [box_2, 'Bottom', name])
    print("TEST PASSED")


main()
