#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks what defeaturing reports. The modelling kernel returns a shape whether or not it closed
#anything: a blind hole whose bottom face was not selected survives, gmsh says so only in a warning
#written to the output window, and the call used to return as if it had worked. It now reports the
#number of faces every selected part actually lost, so a miss is visible to the caller.
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
    print("TEST PASSED")


main()
