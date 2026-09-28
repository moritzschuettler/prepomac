#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks what defeaturing reports. The modelling kernel returns a shape whether or not it closed
#anything: a blind hole whose bottom face was not selected survives, gmsh says so only in a warning
#written to the output window, and the call used to return as if it had worked. It now reports the
#number of faces every selected part actually lost, so a miss is visible to the caller.
#
#Its two siblings - flipping a face orientation and deleting an STL face - each act on one kind of
#part only and leave every other kind alone, so they report the same way.
#
#blind_holes.brep is a 60 x 40 x 20 box with two blind holes drilled from the top face and one hole
#that goes all the way through.
#
#  PrePoMax.com -f blind_holes.brep -u MM_TON_S_C --script Test_Model_Defeature.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

# A blind hole wall stops at its cap, so it covers less area than one that spans the whole thickness
THROUGH_WALL_AREA = 200.0
# The cap of a blind hole is the only plane face smaller than this
CAP_AREA = 100.0


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def faces(part):
    data = part.get_face_data()
    rows = []
    for i in range(data.count):
        rows.append({"id": int(list(data.ids)[i]),
                     "type": list(data.types)[i],
                     "area": float(list(data.areas)[i])})
    return rows


def selection(part, rows):
    return pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                       {part.id: [row["id"] for row in rows]})


def walls_of_blind_holes(rows):
    return [row for row in rows if row["type"] == "Cylinder" and row["area"] < THROUGH_WALL_AREA]


def walls_of_through_holes(rows):
    return [row for row in rows if row["type"] == "Cylinder" and row["area"] >= THROUGH_WALL_AREA]


def caps_of_blind_holes(rows):
    return [row for row in rows if row["type"] == "Plane" and row["area"] < CAP_AREA]


# Flipping a face orientation works on CAD shell parts only, and deleting a face on STL parts only.
# A selection that names any other kind of part is not refused - it simply leaves that part alone, so
# the report is the only thing that says so
def test_siblings():
    import os
    import sys
    models = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
    box = pmx.model.geometry.parts.first()
    face = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {box.id: [1]})
    flipped = pmx.model.geometry.parts.flip_face_orientations(face)
    check(box.name in flipped, "the flip report must name the part: " + str(dict(flipped)))
    check(flipped[box.name] == 0, "a CAD solid has no face orientation to flip: " + str(dict(flipped)))
    #
    before = list(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(models, "cube.stl"))
    stl = [part for part in pmx.model.geometry.parts if part.name not in before][0]
    stl_face = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {stl.id: [1]})
    deleted = pmx.model.geometry.parts.delete_stl_faces(stl_face)
    check(deleted[stl.name] > 0, "deleting a face of an STL part must remove elements: " + str(dict(deleted)))
    #
    cad_face = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {box.id: [1]})
    nothing = pmx.model.geometry.parts.delete_stl_faces(cad_face)
    # A .NET dictionary reaches Python without get(), so the key is asked for and then read
    check(box.name not in nothing or nothing[box.name] == 0,
          "a CAD part has no STL face to delete: " + str(dict(nothing)))


def main():
    part = pmx.model.geometry.parts.first()
    rows = faces(part)
    check(len(walls_of_blind_holes(rows)) > 0 and len(walls_of_through_holes(rows)) > 0 and
          len(caps_of_blind_holes(rows)) > 0,
          "the test part must carry blind holes and a through hole: " + str(rows))
    # The reported mistake: the wall of a blind hole without its bottom face. The kernel cannot close
    # the feature, and the call must say so instead of reporting success
    removed = pmx.model.geometry.parts.defeature(selection(part, walls_of_blind_holes(rows)))
    check(part.name in removed, "the report must name the part that was worked on: " + str(removed))
    check(removed[part.name] == 0,
          "a feature that was not closed must report no removed face: " + str(removed))
    check(len(faces(pmx.model.geometry.parts.first())) == len(rows),
          "a defeaturing that removed nothing must leave the face count alone")
    # A through hole has no bottom face to miss, so its wall alone is enough
    part = pmx.model.geometry.parts.first()
    rows = faces(part)
    removed = pmx.model.geometry.parts.defeature(selection(part, walls_of_through_holes(rows)))
    check(removed[part.name] > 0, "a through hole must be removed: " + str(removed))
    # A blind hole closes once its cap is selected together with its wall. Every cylinder left is a
    # blind hole wall, and the kernel merges the halves it split, so the areas of the first pass no
    # longer separate them
    part = pmx.model.geometry.parts.first()
    rows = faces(part)
    before = len(rows)
    walls = [row for row in rows if row["type"] == "Cylinder"]
    removed = pmx.model.geometry.parts.defeature(selection(part, walls + caps_of_blind_holes(rows)))
    check(removed[part.name] > 0, "a blind hole selected with its cap must be removed: " + str(removed))
    after = len(faces(pmx.model.geometry.parts.first()))
    check(before - after == removed[part.name],
          "the reported count must be the number of faces the part actually lost: " + str(before) +
          " - " + str(after) + " against " + str(removed))
    check(after == 6, "the box must be left with its six faces: " + str(after))
    test_siblings()
    print("TEST PASSED")


main()
