#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the exploded view of the view API and that it leaves the mesh type recognition standing.
#sweep_plate_both.brep is a plate with a block above and one below, sharing their contact faces, which
#imports as a compound part of two transfinite blocks and a tetrahedral plate, beside the cube the model
#opens with. The plate does not sweep: both its ends are split by a footprint that nothing on the other end
#is swept onto. The compound is swept all the same, the sweep leaving the plate to tetrahedra, so it reads
#as a sweep. A compound moves as one body, so its sub-parts keep touching and every one of them is moved by the
#same offset. The sweep detection finds the faces two sub-parts share by their coordinates, which is why a
#compound must never be pulled apart: in the Tests/Sweep model sweep_19_1 a recognition made on separated
#sub-parts turned a sub-part the compound cannot sweep into a swept one. The mesh types must read the
#same with the exploded view on as off.
#
#  PrePoMax.com -f cube.stl -u MM_TON_S_C --script Test_Api_View_Exploded.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def import_compound():
    before = set(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "sweep_plate_both.brep"))
    added = [part for part in pmx.model.geometry.parts if part.name not in before]
    compounds = [part for part in added if part.part_type == "Compound"]
    sub_parts = [part for part in added if part.part_type in ("Solid", "SolidAsShell")]
    check(len(compounds) == 1 and len(sub_parts) == 3,
          "sweep_plate_both.brep must import as a compound of three solids: " + str([p.name for p in added]))
    return compounds[0], sub_parts


def cube_part():
    return pmx.model.geometry.parts[CUBE_NAME]


def mesh_types():
    return dict((part.name, part.mesh_type) for part in pmx.model.geometry.parts)


def boxes(parts):
    return dict((part.name, part.get_bounding_box()) for part in parts)


def shifts(first, second):
    """The translation of every part between two sets of boxes, from the minimum corner."""
    return dict((name, [second[name][2 * i] - first[name][2 * i] for i in range(3)]) for name in first)


def largest_shift(first, second):
    return max(max(abs(value) for value in shift) for shift in shifts(first, second).values())


def check_moved_as_one_body(unexploded, exploded, cube_moved):
    moves = list(shifts(unexploded, exploded).values())
    check(cube_moved or max(abs(value) for value in moves[0]) > 1E-6,
          "the exploded view must move the cube and the compound apart")
    for move in moves[1:]:
        check(max(abs(a - b) for a, b in zip(move, moves[0])) < 1E-6,
              "the sub-parts of a compound must move as one body: " + str(moves))


def test_mesh_types_do_not_follow_the_exploded_view(compound, sub_parts):
    pmx.view.current = "geometry"
    check(not pmx.view.exploded_view_active, "no exploded view is applied after the import")
    types = mesh_types()
    check(types[compound.name] == "Sweep",
          "the blocks sweep and leave the plate to tetrahedra, so the compound is swept: " + str(types))
    check(sorted(types[part.name] for part in sub_parts) == ["Tetrahedral", "Transfinite", "Transfinite"],
          "the plate is tetrahedral and the blocks are transfinite: " + str(types))
    unexploded = boxes(sub_parts)
    cube = boxes([cube_part()])
    #
    pmx.view.apply_exploded_view(1, "AssemblyCenter", 5)
    check(pmx.view.exploded_view_active, "the exploded view must be applied")
    check_moved_as_one_body(unexploded, boxes(sub_parts), largest_shift(cube, boxes([cube_part()])) > 1E-6)
    check(mesh_types() == types, "the exploded view must not change the mesh types: " + str(mesh_types()))
    #
    pmx.view.remove_exploded_view()
    check(not pmx.view.exploded_view_active, "the exploded view must be removed")
    check(largest_shift(unexploded, boxes(sub_parts)) < 1E-6, "removing it must put the sub-parts back")
    check(mesh_types() == types, "removing the exploded view must not change the mesh types")
    # Removing it again does nothing
    pmx.view.remove_exploded_view()


def test_the_other_methods(sub_parts):
    unexploded = boxes(sub_parts)
    cube = boxes([cube_part()])
    pmx.view.apply_exploded_view(0.5, "Disassembly", 2, "Z", True)
    check(pmx.view.exploded_view_active, "the disassembly method must apply")
    # The coordinate axes the movement is limited to are 'axes', not a direction vector
    pmx.view.apply_exploded_view(0.5, "AssemblyCenter", 2, axes="xy")
    check(pmx.view.exploded_view_active, "the axes must be taken regardless of case")
    pmx.view.apply_exploded_view(1, "centerpoint", 500, center=[0, 0, 0])
    check(pmx.view.exploded_view_active, "the method must be taken regardless of case")
    check_moved_as_one_body(unexploded, boxes(sub_parts), largest_shift(cube, boxes([cube_part()])) > 1E-6)
    pmx.view.remove_exploded_view()
    check(largest_shift(unexploded, boxes(sub_parts)) < 1E-6, "a replaced exploded view must not accumulate")


def test_refusals():
    check_raises(lambda: pmx.view.apply_exploded_view(1.5), "a scale factor above 1 must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(-0.1), "a negative scale factor must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "Disassembly", 11),
                 "a Disassembly magnification above 10 must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "AssemblyCenter", 26),
                 "an AssemblyCenter magnification above 25 must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "Disassembly", 0.5),
                 "a magnification below 1 must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "NoSuchMethod"), "an unknown method must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "CenterPoint"),
                 "the CenterPoint method without a center must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "CenterPoint", 2, center=[0, 0]),
                 "a center without 3 coordinates must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(1, axes="W"), "unknown axes must be refused")
    check_raises(lambda: pmx.view.apply_exploded_view(1, direction="XY"), "direction was renamed to axes")
    check_raises(lambda: pmx.view.apply_exploded_view(1, tolerance=-1), "a negative tolerance must be refused")
    # A value the method does not take is refused, never ignored
    check_raises(lambda: pmx.view.apply_exploded_view(1, "AssemblyCenter", 2, step_by_step=True),
                 "step_by_step must be refused by the AssemblyCenter method")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "AssemblyCenter", 2, step_by_step=False),
                 "an explicit step_by_step must be refused by the AssemblyCenter method")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "CenterPoint", 2, tolerance=0, center=[0, 0, 0]),
                 "tolerance must be refused by the CenterPoint method")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "Disassembly", 2, center=[0, 0, 0]),
                 "center must be refused by the Disassembly method")
    check_raises(lambda: pmx.view.apply_exploded_view(1, "AssemblyCenter", 2, center=[0, 0, 0]),
                 "center must be refused by the AssemblyCenter method")
    check(not pmx.view.exploded_view_active, "a refused call must apply nothing")


CUBE_NAME = list(pmx.model.geometry.parts.get_names())[0]
compound, sub_parts = import_compound()
test_mesh_types_do_not_follow_the_exploded_view(compound, sub_parts)
test_the_other_methods(sub_parts)
test_refusals()
print("TEST PASSED")
