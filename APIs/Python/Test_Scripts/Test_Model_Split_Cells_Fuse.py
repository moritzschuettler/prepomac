#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks fuse_bodies, which makes one solid of cells and sub-parts of a compound part made by
#split_solid.
#sweep_side2.brep is two boxes standing side by side and sharing a wall; both are cut in two by a
#horizontal plane, so the split compound has two sub-parts, one per box, of a bottom and a top cell each.
#
#  - Cells of another part and bodies that hold only one cell are refused before anything is recorded,
#    and so are cells that meet only along an edge, which the modelling kernel refuses.
#  - The two sub-parts named, the fused box and the other box of two cells, give one block: the compound
#    becomes a plain part named after the larger sub-part, with six faces.
#  - The two cells of one box fused give back that box as one cell named after its sub-part, with its
#    own faces whole again and the wall still divided where the other box's cells meet it. The compound
#    keeps its name and id, meshing parameters on the box follow it onto the fused cell, and the mesh
#    stays conformal across the wall.
#  - blind_holes.brep cut through its holes and fused back is the part it was: a plain part with its
#    name, its id and its eleven faces.
#  - sweep_stack3.brep is an ordinary compound of three stacked boxes. Its two lower sub-parts fused leave
#    a compound of the same name and id, still not a split one, whose sub-parts keep their names and ids;
#    fusing the rest gives one plain part of six faces named after the bottom box.
#
#  PrePoMax.com -f sweep_side2.brep -u MM_TON_S_C --script Test_Model_Split_Cells_Fuse.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
MESHING_PARAMETERS_NAME = "Meshing_parameters-Fuse"


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


def split(part, z):
    parts = pmx.model.geometry.parts
    tool = pmx.create.split_tool_plane_by_point_and_normal([0, 0, z], [0, 0, 1])
    parts.split_solid(part_region([part]), tool)
    return [p for p in parts if len(list(p.split_sub_part_names)) > 0][-1]


def z_of(cell_name):
    return pmx.model.geometry.parts[cell_name].center_of_mass[2]


def bottom_and_top(compound, sub_part_name):
    cells = sorted(compound.get_split_cell_names(sub_part_name), key=z_of)
    check(len(cells) == 2, sub_part_name + " must have two cells: " + str(cells))
    return cells[0], cells[1]


def state(compound):
    return dict((name, list(compound.get_split_cell_names(name))) for name in compound.split_sub_part_names)


def face_count(name):
    return pmx.model.geometry.parts[name].get_face_data().count


def test_refusals(compound, box_1, box_2):
    parts = pmx.model.geometry.parts
    before = state(compound)
    bottom_1, top_1 = bottom_and_top(compound, box_1)
    bottom_2, top_2 = bottom_and_top(compound, box_2)
    other = [part.name for part in parts if part.part_type != "Compound"][0]
    refused(lambda: parts.fuse_bodies(other, [bottom_1, top_1]), "not a compound part")
    refused(lambda: parts.fuse_bodies(compound.name, [bottom_1]), "only one solid")
    refused(lambda: parts.fuse_bodies(compound.name, [bottom_1, 'Missing_cell']), "not a sub-part or a cell")
    refused(lambda: parts.fuse_bodies(compound.name, [bottom_1, bottom_1]), "more than once")
    check(state(compound) == before, "a refused fuse must change nothing: " + str(state(compound)))
    # A bottom cell of one box and the top cell of the other meet only along an edge
    parts.group_split_cells(compound.name, [bottom_1, top_2], 'Diagonal')
    diagonal = list(compound.get_split_cell_names('Diagonal'))
    refused(lambda: parts.fuse_bodies(compound.name, diagonal), "touch over faces")
    check(list(compound.get_split_cell_names('Diagonal')) == diagonal, "a refused fuse must change nothing")
    parts.group_split_cells(compound.name, [c for c in diagonal if z_of(c) < 3], box_1)
    parts.group_split_cells(compound.name, list(compound.get_split_cell_names('Diagonal')), box_2)
    check(sorted(compound.split_sub_part_names) == sorted([box_1, box_2]),
          "the cells must be back in their boxes: " + str(state(compound)))


def test_fuse_box(compound, box_1, box_2):
    parts = pmx.model.geometry.parts
    items = pmx.model.mesh.setup_items
    name, part_id = compound.name, compound.id
    cells = list(compound.get_split_cell_names(box_1))
    items.add_meshing_parameters(MESHING_PARAMETERS_NAME, part_region([parts[c] for c in cells]))
    #
    fused = parts.fuse_bodies(compound.name, cells)
    check(fused == box_1, "a sub-part left with one cell names it after itself: " + fused)
    compound = parts[name]
    check(compound.id == part_id, "the compound must keep its id")
    check(list(compound.split_sub_part_names) == [box_1, box_2], "the sub-parts must stay: " + str(state(compound)))
    check(len([p for p in parts if p.part_type != "Compound"]) == 3, "three cells must be left")
    # Five faces of its own, and the wall in two, where the two cells of the other box meet it
    check(face_count(fused) == 7, "the fused box must have 7 faces, got %d" % face_count(fused))
    check(abs(float(parts[fused].volume) - 300) < 1e-6, "the fused box must hold its volume")
    check(items[MESHING_PARAMETERS_NAME].valid and
          list(items[MESHING_PARAMETERS_NAME].get_geometry_ids()) == [parts[fused].id],
          "the meshing parameters must follow the box onto its fused cell")
    items.remove([MESHING_PARAMETERS_NAME])
    #
    report = pmx.model.mesh.generate(compound.name)
    check(report[compound.name] == "", "meshing reported: " + report[compound.name])
    names = sorted(part.name for part in pmx.model.parts)
    check(names == sorted([box_1, box_2]), "each box must be a mesh part: " + str(names))
    shared = set(pmx.model.parts[box_1].get_node_ids()) & set(pmx.model.parts[box_2].get_node_ids())
    check(len(shared) > 0, "the mesh must stay conformal across the wall")


def test_fuse_sub_parts(compound_name, box_1, box_2):
    parts = pmx.model.geometry.parts
    compound = parts[compound_name]
    cells = list(compound.get_split_cell_names(box_1)) + list(compound.get_split_cell_names(box_2))
    check(len(cells) == 3, "the fused box and the two cells of the other box must be left: " + str(cells))
    #
    fused = parts.fuse_bodies(compound_name, [box_1, box_2])
    check(fused == box_2, "the cells must end in the larger sub-part: " + fused)
    check(compound_name not in list(parts.get_names()), "the compound must be gone")
    check(face_count(fused) == 6, "the two boxes fused must be one block of 6 faces, got %d" % face_count(fused))
    check(abs(float(parts[fused].volume) - 600) < 1e-6, "the block must hold the volume of both boxes")


def test_undo_split():
    parts = pmx.model.geometry.parts
    before = list(parts.get_names())
    pmx.import_file(os.path.join(MODELS, "blind_holes.brep"))
    part = [p for p in parts if p.name not in before][0]
    name, part_id, part_type, faces = part.name, part.id, part.part_type, face_count(part.name)
    compound = split(part, 15)
    compound_name = compound.name
    cells = list(compound.get_split_cell_names(name))
    check(len(cells) == 2, "the plane must cut the part in two: " + str(cells))
    #
    fused = parts.fuse_bodies(compound_name, cells)
    check(fused == name, "the part must come back under its name: " + fused)
    check(compound_name not in list(parts.get_names()), "the compound must be gone")
    part = parts[name]
    check(part.part_type == part_type and part.id == part_id,
          "the part must be the %s it was, with its id %d: %s %d" % (part_type, part_id, part.part_type, part.id))
    check(face_count(name) == faces, "the part must have its %d faces, got %d" % (faces, face_count(name)))


def test_fuse_ordinary_compound():
    parts = pmx.model.geometry.parts
    before = list(parts.get_names())
    pmx.import_file(os.path.join(MODELS, "sweep_stack3.brep"))
    compound = [p for p in parts if p.name not in before and p.part_type == "Compound"][0]
    name, part_id = compound.name, compound.id
    sub_parts = sorted([p for p in parts if p.name not in before and p.part_type != "Compound"],
                       key=lambda p: p.center_of_mass[2])
    check(len(sub_parts) == 3, "sweep_stack3 must be three sub-parts: " + str([p.name for p in sub_parts]))
    bottom, middle, top = [(p.name, p.id) for p in sub_parts]
    #
    fused = parts.fuse_bodies(name, [bottom[0], middle[0]])
    check(fused == bottom[0], "the fused solid must take the name of the first sub-part: " + fused)
    compound = parts[name]
    check(compound.id == part_id and list(compound.split_sub_part_names) == [],
          "the compound must keep its id and stay an ordinary compound")
    check(parts[bottom[0]].id == bottom[1] and parts[top[0]].id == top[1],
          "the sub-parts must keep their ids: %d %d" % (parts[bottom[0]].id, parts[top[0]].id))
    check(middle[0] not in list(parts.get_names()), "the middle box must be gone")
    check(abs(float(parts[bottom[0]].volume) - 700) < 1e-6, "the fused solid must hold both boxes")
    #
    fused = parts.fuse_bodies(name, [bottom[0], top[0]])
    check(fused == bottom[0] and name not in list(parts.get_names()), "the whole compound must become one part")
    check(parts[fused].id == bottom[1] and face_count(fused) == 6,
          "the part must keep the id of the bottom box and be one block of 6 faces, got %d faces" % face_count(fused))


def main():
    compound = split([p for p in pmx.model.geometry.parts if p.part_type == "Compound"][0], 3)
    box_1, box_2 = list(compound.split_sub_part_names)
    test_refusals(compound, box_1, box_2)
    compound_name = compound.name
    test_fuse_box(compound, box_1, box_2)
    test_fuse_sub_parts(compound_name, box_1, box_2)
    test_undo_split()
    test_fuse_ordinary_compound()
    print("TEST PASSED")


main()
