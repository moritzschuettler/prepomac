#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks fuse_faces and repair. Fusing faces makes faces that lie on one surface one again across the
#edges they share, so a face a shell split divided comes back whole and the part keeps its name and
#its body. Faces on different surfaces cannot be fused, which is reported as nothing fused rather
#than refused. Repair runs on whole parts; with fuse_faces it gives back the faces a split divided,
#and a repair that has nothing to do leaves every face where it was. Both report the number of faces
#every part lost.
#
#blind_holes.brep is a 60 x 40 x 20 box with two blind holes drilled from the top face and one hole
#that goes all the way through.
#
#  PrePoMax.com -f blind_holes.brep -u MM_TON_S_C --script Test_Model_Fuse_Faces_Repair.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def refused(action, fragment):
    try:
        action()
    except Exception as ex:
        check(fragment in str(ex), "the refusal must mention '" + fragment + "': " + str(ex))
        return
    raise Exception("TEST FAILED: the call was not refused (" + fragment + ")")


def part():
    return pmx.model.geometry.parts.first()


def face_ids(part):
    return [int(i) for i in list(part.get_face_data().ids)]


def face_count(part):
    return part.get_face_data().count


def surfaces(part, ids):
    return pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {part.id: ids})


def top_face_id(part, high):
    data = part.get_face_data()
    best = None
    for i in range(data.count):
        z = float(list(data.centroids[i])[2])
        area = float(list(data.areas)[i])
        if list(data.types)[i] == "Plane" and abs(z - high) < 1e-6:
            if best is None or area > best[1]:
                best = (int(list(data.ids)[i]), area)
    check(best is not None, "the box must have a planar top face")
    return best[0]


def split_top_face(part):
    vertices = part.get_vertex_data()
    xyz = [[float(v) for v in list(vertices.coordinates[i])] for i in range(vertices.count)]
    low = [min(p[j] for p in xyz) for j in range(3)]
    high = [max(p[j] for p in xyz) for j in range(3)]
    y = low[1] + 2.0
    line = pmx.create.split_tool_line_by_two_points([[low[0] + 5.0, y, high[2]], [high[0] - 5.0, y, high[2]]])
    added = pmx.model.geometry.parts.split_shell(surfaces(part, [top_face_id(part, high[2])]), line)
    check(added[part.name] > 0, "the top face must be divided: " + str(dict(added)))
    return added[part.name]


def test_fuse_faces():
    parts = pmx.model.geometry.parts
    name = part().name
    before = face_count(part())
    added = split_top_face(part())
    check(face_count(part()) == before + added, "the split must add the faces it reports")
    # Every face of the part is selected: only the faces the split divided lie on one surface and share an
    # edge, so only they are fused, and the part comes back with the faces it had
    fused = parts.fuse_faces(surfaces(part(), face_ids(part())))
    check(name in fused, "the report must name the part: " + str(dict(fused)))
    check(fused[name] == added, "the faces the split added must be fused away: " + str(dict(fused)))
    check(part().name == name, "fusing faces must keep the part name")
    check(face_count(part()) == before, "the part must have the faces it had before the split: " +
          str(face_count(part())) + " against " + str(before))
    # Two faces that meet at a right angle lie on different surfaces, so nothing is fused
    data = part().get_face_data()
    planes = [int(list(data.ids)[i]) for i in range(data.count) if list(data.types)[i] == "Plane"]
    nothing = parts.fuse_faces(surfaces(part(), planes[:2]))
    check(nothing[name] == 0, "faces on different surfaces must not be fused: " + str(dict(nothing)))
    check(face_count(part()) == before, "a fuse that fused nothing must leave the faces alone")


def test_repair():
    parts = pmx.model.geometry.parts
    name = part().name
    before = face_count(part())
    # A clean part has nothing to repair
    removed = parts.repair(name)
    check(removed[name] == 0, "a clean part must lose no face: " + str(dict(removed)))
    check(face_count(part()) == before, "a repair with nothing to do must leave the faces alone")
    # Fusing faces gives back the faces a split divided
    added = split_top_face(part())
    removed = parts.repair(name, fuse_faces=True, join_edges=True, tolerance=0.001)
    check(removed[name] == added, "the repair must fuse the faces the split added: " + str(dict(removed)))
    check(part().name == name and face_count(part()) == before, "the repaired part must be the part it was")
    #
    refused(lambda: parts.repair(name, sew_faces=False, fix_shape=False, remove_small_faces=False,
                                 remove_small_edges=False), "one repair")
    refused(lambda: parts.repair(name, tolerance=-1), "tolerance")
    refused(lambda: parts.repair("No_such_part"), "No_such_part")


def main():
    test_fuse_faces()
    test_repair()
    print("TEST PASSED")


main()
