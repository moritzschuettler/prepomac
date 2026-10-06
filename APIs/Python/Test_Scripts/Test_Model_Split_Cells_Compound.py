#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks add_compound with a compound part made by split_solid among the parts. box_and_cylinder.brep holds a box
#and a cylinder. The box is copied three quarters of its width along x, so the copy overlaps its last quarter, and
#the box is then split in two halves across x.
#
#  - A compound of plain parts only is left as it was: every solid is a sub-part of its own.
#  - A compound of the split box and its copy gathers its solids under the parts they came from: the two box cells,
#    one of them cut by the copy, stay one sub-part, the copy outside the box is one, and the overlap, which lies in
#    both, is a sub-part of its own. The parts the compound is made of stay in the model, so the sub-parts take names
#    and part ids of their own.
#  - The overlap grouped under the box makes the box whole again, and regenerating the compound keeps the grouping.
#    Meshed, every sub-part is one mesh part.
#  - Regenerating the meshed compound keeps its name, the names and ids of its mesh parts and the model group of its
#    mesh: renamed and renamed back, the group follows it and does not hold its old name.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Split_Cells_Compound.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

TOLERANCE = 1e-3


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def parts():
    return pmx.model.geometry.parts


def bounding_box(name):
    return [float(value) for value in list(parts()[name].get_bounding_box())]


def volume(names):
    return sum(float(parts()[name].volume) for name in names)


def check_close(actual, expected, message):
    check(abs(actual - expected) <= TOLERANCE * max(1.0, abs(expected)),
          message + ": " + str(actual) + " instead of " + str(expected))


def added_part_names(before):
    return [name for name in parts().get_names() if name not in before]


def find_box_and_cylinder():
    names = [part.name for part in parts() if part.part_type != "Compound"]
    check(len(names) == 2, "box_and_cylinder.brep must hold two parts: " + str(names))
    # The box fills its bounding box
    def fill(name):
        box = bounding_box(name)
        return float(parts()[name].volume) / ((box[1] - box[0]) * (box[3] - box[2]) * (box[5] - box[4]))
    names.sort(key=fill, reverse=True)
    check(fill(names[0]) > 0.99, "one of the parts must be a box")
    return names[0], names[1]


def test_plain_compound(cylinder):
    before = parts().get_names()
    box = bounding_box(cylinder)
    parts().translate(cylinder, [(box[1] - box[0]) / 2, 0, 0], number_of_copies=1)
    copy = added_part_names(before)[0]
    before = parts().get_names()
    parts().add_compound([cylinder, copy])
    compound = [name for name in added_part_names(before) if parts()[name].part_type == "Compound"][0]
    check(list(parts()[compound].split_sub_part_names) == [],
          "a compound of plain parts must not gather its solids: " + str(list(parts()[compound].split_sub_part_names)))


def state(compound):
    part = parts()[compound]
    return dict((name, sorted(part.get_split_cell_names(name))) for name in part.split_sub_part_names)


def main():
    box, cylinder = find_box_and_cylinder()
    test_plain_compound(cylinder)
    #
    bounds = bounding_box(box)
    width = bounds[1] - bounds[0]
    box_volume = float(parts()[box].volume)
    before = parts().get_names()
    parts().translate(box, [0.75 * width, 0, 0], number_of_copies=1)
    copy = added_part_names(before)[0]
    #
    region = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [parts()[box].id])
    tool = pmx.create.split_tool_plane_by_point_and_normal([(bounds[0] + bounds[1]) / 2, 0, 0], [1, 0, 0])
    parts().split_solid(region, tool)
    split = [part.name for part in parts() if len(list(part.split_sub_part_names)) > 0][0]
    check(list(parts()[split].split_sub_part_names) == [box],
          "the split box must be one sub-part named after the box: " + str(state(split)))
    #
    before = parts().get_names()
    parts().add_compound([split, copy])
    compound = [name for name in added_part_names(before) if parts()[name].part_type == "Compound"][0]
    sub_parts = list(parts()[compound].split_sub_part_names)
    check(len(sub_parts) == 3, "the compound must have three sub-parts: " + str(state(compound)))
    box_part, copy_part, overlap = sub_parts
    check(box_part not in [box, split] and copy_part != copy,
          "the sub-parts must take names of their own: " + str(sub_parts))
    check(overlap.startswith("Overlap"), "the overlap must be a sub-part of its own: " + str(sub_parts))
    #
    cells = parts()[compound].get_split_cell_names
    check(len(cells(box_part)) == 2, "the box keeps the half the copy misses and the quarter it misses of the other: " +
          str(state(compound)))
    check(len(cells(copy_part)) == 1 and len(cells(overlap)) == 1, "the copy and the overlap are one cell each: " +
          str(state(compound)))
    check_close(volume(cells(box_part)), 0.75 * box_volume, "the box outside the copy")
    check_close(volume(cells(copy_part)), 0.75 * box_volume, "the copy outside the box")
    check_close(volume(cells(overlap)), 0.25 * box_volume, "the overlap")
    #
    parts().group_split_cells(compound, list(cells(overlap)), box_part)
    check(list(parts()[compound].split_sub_part_names) == [box_part, copy_part],
          "the overlap grouped under the box must leave two sub-parts: " + str(state(compound)))
    check_close(volume(cells(box_part)), box_volume, "the box with its overlap")
    #
    grouped = state(compound)
    parts().regenerate_compound([compound])
    check(state(compound) == grouped, "regenerating must keep the grouping: " + str(state(compound)) + " instead of " +
          str(grouped))
    #
    report = pmx.model.mesh.generate(compound)
    check(report[compound] == "", "meshing " + compound + " reported: " + report[compound])
    mesh_names = sorted(part.name for part in pmx.model.parts)
    check(mesh_names == sorted([box_part, copy_part]), "every sub-part must be one mesh part: " + str(mesh_names))
    mesh_ids = [part.id for part in pmx.model.parts]
    check(len(set(mesh_ids)) == len(mesh_ids), "every sub-part must be meshed with an id of its own: " + str(mesh_ids))
    #
    mesh_parts = dict((part.name, part.id) for part in pmx.model.parts)
    parts().regenerate_compound([compound])
    check(parts().contains(compound), "regenerating the meshed compound must keep its name: " + str(parts().get_names()))
    check(state(compound) == grouped, "regenerating the meshed compound must keep the grouping: " +
          str(state(compound)) + " instead of " + str(grouped))
    regenerated_mesh_parts = dict((part.name, part.id) for part in pmx.model.parts)
    check(regenerated_mesh_parts == mesh_parts, "regenerating the meshed compound must keep its mesh parts: " +
          str(regenerated_mesh_parts) + " instead of " + str(mesh_parts))
    parts().rename(compound, "Renamed_compound")
    parts().rename("Renamed_compound", compound)
    check(parts().contains(compound), "the regenerated compound must keep the model group of its mesh")
    print("TEST PASSED")


main()
