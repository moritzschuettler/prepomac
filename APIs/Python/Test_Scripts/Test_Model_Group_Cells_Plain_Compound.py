#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks group_split_cells on a compound part that was imported, not made by split_solid. sweep_side2.brep
#is two boxes standing side by side and sharing a wall, imported as one compound of two sub-parts.
#
#  - The compound lists no split sub-parts until its sub-parts are grouped.
#  - A grouping of one sub-part alone would change nothing, or only its name, and is refused before
#    anything is recorded.
#  - The two boxes grouped without a name go under the first, whose name and part id the mesh part keeps;
#    the boxes become its cells, named after it.
#  - cell_names lists the sub-parts of the compound before the grouping and its cells after it, and is
#    empty for a part that is not a compound.
#
#  PrePoMax.com -f sweep_side2.brep -u MM_TON_S_C --script Test_Model_Group_Cells_Plain_Compound.py -g No -x Yes -w <dir>
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


def mesh(compound):
    report = pmx.model.mesh.generate(compound.name)
    check(report[compound.name] == "", "meshing " + compound.name + " reported: " + report[compound.name])
    return dict((part.name, part.id) for part in pmx.model.parts)


def main():
    parts = pmx.model.geometry.parts
    compound = [part for part in parts if part.part_type == "Compound"][0]
    check(type(compound.cell_names).__name__ == "StringListAPI",
          "the cell names must be a StringList: " + type(compound.cell_names).__name__)
    box_1, box_2 = list(compound.cell_names)
    check(sorted([box_1, box_2]) == sorted(part.name for part in parts if part.part_type != "Compound"),
          "the cell names must be the sub-parts of the compound: " + str([box_1, box_2]))
    check(list(parts[box_1].cell_names) == [], "a part that is not a compound must have no cell names: " +
          str(list(parts[box_1].cell_names)))
    check(list(compound.split_sub_part_names) == [],
          "an imported compound must list no split sub-parts: " + str(list(compound.split_sub_part_names)))
    mesh_ids = mesh(compound)
    check(sorted(mesh_ids) == sorted([box_1, box_2]), "every box must be a mesh part: " + str(mesh_ids))
    #
    refused(lambda: parts.group_split_cells(compound.name, [box_1]), "already belong")
    refused(lambda: parts.group_split_cells(compound.name, [box_1], 'Box'), "Rename the sub-part")
    refused(lambda: parts.group_split_cells(compound.name, [box_1, box_1]), "more than once")
    check(list(compound.split_sub_part_names) == [], "a refused grouping must change nothing: " +
          str(list(compound.split_sub_part_names)))
    #
    name = parts.group_split_cells(compound.name, [box_1, box_2])
    check(name == box_1, "the boxes must go under the first: " + name)
    check(list(compound.split_sub_part_names) == [box_1],
          "the compound must list one sub-part: " + str(list(compound.split_sub_part_names)))
    cells = list(compound.get_split_cell_names(box_1))
    check(sorted(compound.cell_names) == sorted(cells),
          "the cell names must follow the grouping: " + str(list(compound.cell_names)))
    check(sorted(cells) == [box_1 + "_cell-1", box_1 + "_cell-2"],
          "the boxes must be cells named after their sub-part: " + str(cells))
    #
    grouped_mesh_ids = mesh(compound)
    check(list(grouped_mesh_ids) == [box_1], "the sub-part must be one mesh part: " + str(grouped_mesh_ids))
    check(grouped_mesh_ids[box_1] == mesh_ids[box_1],
          "the mesh part must keep the id of the box: " + str(grouped_mesh_ids) + " before " + str(mesh_ids))
    print("TEST PASSED")


main()
