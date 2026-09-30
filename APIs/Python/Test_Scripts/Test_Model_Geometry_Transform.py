#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks translating, rotating, mirroring and scaling geometry parts. A move replaces each part in
#place, so it keeps its name, its part id and its faces in the same order, and only its position
#changes; a copy or a pattern adds new parts and leaves the original where it was. A mirror must leave
#a solid the right way out, which the positive volume says.
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


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def parts():
    return pmx.model.geometry.parts


def center(name):
    return [float(value) for value in list(parts()[name].center_of_mass)]


# An STL part carries no center of mass, so its position is read from the middle of its bounding box
def box_center(name):
    box = [float(value) for value in list(parts()[name].get_bounding_box())]
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
    parts().remove(added + [stl_name])


def test_compound(names):
    before = list(parts().get_names())
    parts().create_compound(names)
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
    test_stl()
    test_compound(names[:2])
    print("TEST PASSED")


main()
