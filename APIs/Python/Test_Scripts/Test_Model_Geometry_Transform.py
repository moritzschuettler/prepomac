#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks translating, rotating, mirroring and scaling geometry parts. A move replaces each part in
#place, so it keeps its name, its part id and its faces in the same order, and only its position
#changes; a copy or a pattern adds new parts and leaves the original where it was. A mirror must leave
#a solid the right way out, which the positive volume says. A pick moves with its part: with a moved mesh
#part, back to the geometry when the part is meshed again, and with a moved geometry part, which keeps the mesh
#setup items defined on it.
#
#box_and_cylinder.brep holds two CAD solids; cube.stl is imported for the STL parts.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Geometry_Transform.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os

MODELS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models")
TOLERANCE = 1e-6


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message, unknown_keyword=None):
    # Only the API's own refusal passes: a PythonAPIException, or for a call that deliberately passes the
    # keyword unknown_keyword, the TypeError of the keyword guard. Anything else, and a message carrying a
    # .NET stack trace, is a fault of the application that happened to stop the call
    try:
        function()
    except Exception as ex:
        kind, text = type(ex).__name__, str(ex)
        if unknown_keyword is None:
            expected, refusal = kind == "PythonAPIException", "a PythonAPIException"
        else:
            expected = kind == "TypeError" and "unexpected keyword argument '%s'" % unknown_keyword in text
            refusal = "the keyword guard's TypeError for '%s'" % unknown_keyword
        if not expected or "   at " in text:
            raise Exception("TEST FAILED: " + message + " - the refusal must be " + refusal +
                            " without a .NET stack trace, not " + kind + ": " + text[0:500])
        return
    raise Exception("TEST FAILED: " + message)


def parts():
    return pmx.model.geometry.parts


def center(name):
    return parts()[name].center_of_mass


# An STL part carries no center of mass, so its position is read from the middle of its bounding box
def box_center(name):
    box = parts()[name].get_bounding_box()
    return [(box[0] + box[1]) / 2, (box[2] + box[3]) / 2, (box[4] + box[5]) / 2]


def faces(name):
    data = parts()[name].get_face_data()
    return list(zip([int(value) for value in list(data.ids)], [round(float(value), 6) for value in list(data.areas)]))


def check_close(actual, expected, message):
    for a, e in zip(actual, expected):
        check(abs(a - e) <= TOLERANCE * max(1.0, abs(e)), message + ": " + str(actual) + " instead of " + str(expected))


def added_part_names(before):
    return [name for name in parts().get_names() if name not in before]


def test_translate_moves_in_place(name):
    part_id = parts()[name].id
    before_center = center(name)
    before_faces = faces(name)
    parts().translate(name, [10, 0, 0])
    check(name in parts().get_names(), "a moved part must keep its name")
    check(parts()[name].id == part_id, "a moved part must keep its part id")
    check(faces(name) == before_faces, "a moved part must keep its faces in the same order")
    check_close(center(name), [before_center[0] + 10, before_center[1], before_center[2]],
                "the moved part is not where it was sent")
    parts().translate(name, [-10, 0, 0])


def test_translate_pattern(name):
    before = list(parts().get_names())
    before_center = center(name)
    parts().translate(name, [0, 20, 0], number_of_copies=3)
    added = added_part_names(before)
    check(len(added) == 3, "a pattern of 3 copies added " + str(len(added)) + " parts")
    check_close(center(name), before_center, "the original of a pattern must not move")
    offsets = sorted(center(copy)[1] - before_center[1] for copy in added)
    check_close(offsets, [20, 40, 60], "copy k of a pattern must be translated k times")
    parts().remove(added)


def test_rotate(name):
    before_center = center(name)
    volume = parts()[name].volume
    parts().rotate(name, [0, 0, 0], [0, 0, 1], 90)
    x, y, z = before_center
    check_close(center(name), [-y, x, z], "a rotation of 90 degrees about z is not right handed")
    check(abs(parts()[name].volume - volume) <= TOLERANCE * volume, "a rotation must keep the volume")
    parts().rotate(name, [0, 0, 0], [0, 0, 1], -90)
    check_close(center(name), before_center, "rotating back must return the part")
    #
    before = list(parts().get_names())
    parts().rotate(name, [0, 0, 0], [0, 0, 1], 90, number_of_copies=2)
    added = added_part_names(before)
    check(len(added) == 2, "a rotated pattern of 2 copies added " + str(len(added)) + " parts")
    centers = sorted([round(value, 6) for value in center(copy)] for copy in added)
    expected = sorted([round(value, 6) for value in point] for point in ([-y, x, z], [-x, -y, z]))
    check(centers == expected, "copy k of a rotated pattern must be rotated k times: " + str(centers))
    parts().remove(added)


def test_mirror(name):
    before = list(parts().get_names())
    before_center = center(name)
    volume = parts()[name].volume
    parts().mirror(name, [0, 0, 0], [1, 0, 0], copy=True)
    added = added_part_names(before)
    check(len(added) == 1, "a mirrored copy added " + str(len(added)) + " parts")
    mirrored = added[0]
    check_close(center(mirrored), [-before_center[0], before_center[1], before_center[2]],
                "the mirrored copy is not on the other side of the plane")
    check(parts()[mirrored].volume > 0, "the mirrored solid is inside out")
    check(abs(parts()[mirrored].volume - volume) <= TOLERANCE * volume, "a mirror must keep the volume")
    check(len(faces(mirrored)) == len(faces(name)), "a mirror must keep the number of faces")
    check_close(center(name), before_center, "the original of a mirrored copy must not move")
    parts().remove(added)


def test_scale(name):
    volume = parts()[name].volume
    parts().scale(name, center(name), [2, 2, 2])
    check(abs(parts()[name].volume - 8 * volume) <= 1e-4 * volume, "a uniform scale of 2 must multiply the volume by 8")
    parts().scale(name, center(name), [0.5, 0.5, 0.5])


def test_stl():
    before = list(parts().get_names())
    pmx.import_file(os.path.join(MODELS, "cube.stl"))
    stl_name = added_part_names(before)[0]
    test_part_kind_refusals(before[0], stl_name)
    before_center = box_center(stl_name)
    parts().translate(stl_name, [0, 0, 5])
    check_close(box_center(stl_name), [before_center[0], before_center[1], before_center[2] + 5],
                "the STL part did not move")
    before = list(parts().get_names())
    parts().mirror(stl_name, [0, 0, 0], [0, 0, 1], copy=True)
    added = added_part_names(before)
    check(len(added) == 1, "a mirrored STL copy added " + str(len(added)) + " parts")
    check_close(box_center(added[0]),[before_center[0], before_center[1], -before_center[2] - 5],
                "the mirrored STL copy is not on the other side of the plane")
    parts().remove(added)
    parts().translate(stl_name, [0, 0, -5])
    #
    x, y, z = before_center
    parts().mirror(stl_name, [0, 0, 0], [1, 0, 0])
    check(stl_name in parts().get_names(), "a mirrored STL part must keep its name")
    check_close(box_center(stl_name), [-x, y, z], "the STL part was not mirrored in place")
    parts().mirror(stl_name, [0, 0, 0], [1, 0, 0])
    #
    parts().rotate(stl_name, [0, 0, 0], [0, 0, 1], 90)
    check_close(box_center(stl_name), [-y, x, z], "the STL part was not rotated right handed")
    parts().rotate(stl_name, [0, 0, 0], [0, 0, 1], -90)
    check_close(box_center(stl_name), before_center, "rotating the STL part back must return it")
    #
    before = list(parts().get_names())
    parts().rotate(stl_name, [0, 0, 0], [0, 0, 1], 90, number_of_copies=2)
    added = added_part_names(before)
    check(len(added) == 2, "a rotated STL pattern of 2 copies added " + str(len(added)) + " parts")
    centers = sorted([round(value, 6) for value in box_center(copy)] for copy in added)
    expected = sorted([round(value, 6) for value in point] for point in ([-y, x, z], [-x, -y, z]))
    check(centers == expected, "copy k of a rotated STL pattern must be rotated k times: " + str(centers))
    check_close(box_center(stl_name), before_center, "the original of an STL pattern must not move")
    parts().remove(added)
    #
    before = list(parts().get_names())
    parts().translate(stl_name, [0, 20, 0], number_of_copies=3)
    added = added_part_names(before)
    check(len(added) == 3, "a translated STL pattern of 3 copies added " + str(len(added)) + " parts")
    offsets = sorted(box_center(copy)[1] - y for copy in added)
    check_close(offsets, [20, 40, 60], "copy k of a translated STL pattern must be translated k times")
    parts().remove(added)
    #
    box = parts()[stl_name].get_bounding_box()
    parts().scale(stl_name, before_center, [2, 1, 1])
    scaled = parts()[stl_name].get_bounding_box()
    check_close([scaled[1] - scaled[0], scaled[3] - scaled[2]], [2 * (box[1] - box[0]), box[3] - box[2]],
                "the STL part was not scaled by 2 along x only")
    parts().remove([stl_name])


# The dialogs list only the parts an operation applies to, so any other part is refused naming the argument and
# nothing is recorded
def check_part_refused(function, fragment, message):
    names_before = list(parts().get_names())
    try:
        function()
    except Exception as ex:
        check("'part_names'" in str(ex) and fragment in str(ex), message + ": " + str(ex)[0:300])
        check(list(parts().get_names()) == names_before, message + ": the refused call changed the parts")
        return
    raise Exception("TEST FAILED: " + message)


def test_part_kind_refusals(cad_name, stl_name):
    check_part_refused(lambda: parts().add_compound([cad_name, stl_name]), "only CAD or only stl",
                       "a compound of a CAD and an STL part must be refused")
    check_part_refused(lambda: parts().add_compound([cad_name]), "at least two",
                       "a compound of one part must be refused")
    check_part_refused(lambda: parts().regenerate_compound([cad_name]), cad_name,
                       "regenerating a part that is not a compound must be refused")
    check_part_refused(lambda: parts().convert_shell_to_solid([cad_name]), cad_name,
                       "converting a CAD solid to a solid must be refused")
    check_part_refused(lambda: parts().convert_shell_to_solid([stl_name]), stl_name,
                       "converting an STL part to a solid must be refused")
    check_part_refused(lambda: parts().convert_solid_to_shell([stl_name]), stl_name,
                       "converting an STL part to a shell must be refused")
    check_part_refused(lambda: parts().flip_stl_normals([cad_name]), cad_name,
                       "flipping the normals of a CAD part must be refused")


def test_compound(names):
    before = list(parts().get_names())
    parts().add_compound(names)
    added = added_part_names(before)
    compounds = [name for name in added if parts()[name].part_type == "Compound"]
    check(len(compounds) == 1, "creating a compound added the parts " + str(added))
    compound = compounds[0]
    sub_part = [name for name in added if name != compound][0]
    before_center = center(sub_part)
    parts().translate(compound, [0, 0, 7])
    check(compound in parts().get_names(), "a moved compound must keep its name")
    check_close(center(sub_part), [before_center[0], before_center[1], before_center[2] + 7],
                "the sub-part did not move with its compound")
    check_raises(lambda: parts().translate(sub_part, [1, 0, 0]), "a compound sub-part must not be moved on its own")
    check_part_refused(lambda: parts().convert_solid_to_shell([sub_part]), sub_part,
                       "converting a compound sub-part to a shell must be refused")


def test_selections_follow_the_move(name):
    # A pick moves with its part. A mesh part moved in place takes the picks made on it along, and meshing its
    # geometry part again puts the mesh back on the geometry and the picks with it. A geometry part moved in place
    # keeps the mesh setup items defined on it, by ID and by pick, and its mesh takes the picks along once it is
    # meshed again
    setup_items = pmx.model.mesh.setup_items
    node_sets = pmx.model.node_sets
    part_id = parts()[name].id
    box = parts()[name].get_bounding_box()
    top = [(box[0] + box[1]) / 2, (box[2] + box[3]) / 2, box[5]]
    setup_items.add_meshing_parameters("PyTest_MP", pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part_id]),
                                       max_size=10, second_order=False)
    setup_items.add_local_mesh_size("PyTest_LS", pmx.create.selection_by_pick('geometry', 'geometry', top,
                                                                             'GeometrySurface'), element_size=5)
    face_ids = list(setup_items["PyTest_LS"].get_geometry_ids())
    set_names = ["PyTest_Top_Pick", "PyTest_Top_Box"]
    try:
        pmx.model.mesh.generate(name)
        node_count = pmx.model.nodes.count
        node_sets.add(set_names[0], pmx.create.selection_by_pick('femodel', 'nodes', top, 'GeometrySurface'))
        node_sets.add(set_names[1], pmx.create.selection_by_box('femodel', 'nodes', [box[0] - 1, box[2] - 1, box[5] - 0.1],
                                                                [box[1] + 1, box[3] + 1, box[5] + 0.1], 'Node'))
        top_count = node_sets[set_names[0]].count
        top_center = list(node_sets[set_names[0]].get_center_of_gravity())
        check(top_count > 0 and node_sets[set_names[1]].count == top_count, "the pick and the box must find the top face")

        def check_top(offset, message):
            for set_name in set_names:
                node_set = node_sets[set_name]
                check(node_set.valid and node_set.count == top_count,
                      message + ": " + set_name + " holds " + str(node_set.count) + " nodes instead of " + str(top_count))
                check_close(list(node_set.get_center_of_gravity()), [c + o for c, o in zip(top_center, offset)],
                            message + ": " + set_name + " is not on the top face")

        def check_setup_items(message):
            check(setup_items["PyTest_MP"].valid and list(setup_items["PyTest_MP"].get_geometry_ids()) == [part_id],
                  message + ": the meshing parameters lost their part")
            check(setup_items["PyTest_LS"].valid and list(setup_items["PyTest_LS"].get_geometry_ids()) == face_ids,
                  message + ": the local mesh size lost its face")
        # A mesh part moved and meshed again
        pmx.model.parts.translate(name, [0, 0, 5])
        check_top([0, 0, 5], "after the mesh part was moved")
        pmx.model.mesh.generate(name)
        check_top([0, 0, 0], "after the moved mesh part was meshed again")
        pmx.model.parts.rotate(name, [0, 0, 0], [0, 0, 1], 90)
        pmx.model.mesh.generate(name)
        check_top([0, 0, 0], "after the rotated mesh part was meshed again")
        # A geometry part moved and meshed again
        parts().translate(name, [10, 0, 0])
        check_setup_items("after the geometry part was moved")
        check_top([0, 0, 0], "after the geometry part was moved and before it was meshed")
        pmx.model.mesh.generate(name)
        check(pmx.model.nodes.count == node_count, "the moved geometry part was meshed without its mesh setup items")
        check_top([10, 0, 0], "after the moved geometry part was meshed again")
        # Both, the mesh first
        pmx.model.parts.translate(name, [0, 3, 0])
        parts().translate(name, [-10, 0, 0])
        check_setup_items("after the geometry part was moved back")
        check_top([10, 3, 0], "after the mesh part and the geometry part were moved")
        pmx.model.mesh.generate(name)
        check_top([0, 0, 0], "after the moved geometry part was meshed again")
    finally:
        node_sets.remove([set_name for set_name in set_names if node_sets.contains(set_name)])
        setup_items.remove(["PyTest_MP", "PyTest_LS"])
        if pmx.model.parts.contains(name): pmx.model.parts.remove(name)


def test_refusals(name):
    check_raises(lambda: parts().rotate(name, [0, 0, 0], [0, 0, 0], 45), "a zero rotation axis must be refused")
    check_raises(lambda: parts().mirror(name, [0, 0, 0], [0, 0, 0]), "a zero mirror normal must be refused")
    check_raises(lambda: parts().translate(name, [1, 0, 0], number_of_copies=-1),
                 "a negative number of copies must be refused")


def main():
    check(pmx.model.is_open(), "a model must be open")
    names = list(parts().get_names())
    check(len(names) >= 2, "box_and_cylinder.brep must hold two parts")
    name = names[0]
    test_refusals(name)
    test_translate_moves_in_place(name)
    test_translate_pattern(name)
    test_rotate(name)
    test_mirror(name)
    test_scale(name)
    test_selections_follow_the_move(name)
    test_stl()
    test_compound(names[:2])
    print("TEST PASSED")


main()
