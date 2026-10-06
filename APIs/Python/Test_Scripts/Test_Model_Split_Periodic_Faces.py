#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks split_periodic_faces. A CAD file is imported with its periodic faces as they are - the wall of a bore is one
#face closing on itself along a single seam - and only Split Periodic Faces divides them, into the number of faces
#asked for. The part keeps its name, its id and its body, and a part with nothing left to split reports zero. A
#sub-part is split with its compound, under whose name it is reported, and the sub-parts keep their names. Faces
#fused back into a closed face stay one face, which they did not while every geometry operation split them again.
#A defeaturing keeps the splits of every face it does not remove, although the modelling kernel joins the faces that
#lie on one surface anywhere in the solid it returns.
#
#A part whose CAD system wrote the vertices of its bores a hair past the seam is split as well: the split used to cut
#them off as edges shorter than any tolerance and the part was left as it was.
#
#blind_holes.brep is a 60 x 40 x 20 box with two blind holes drilled from the top face and one hole that goes all
#the way through. sweep_holes_side.brep is a compound of two boxes side by side with a bore through each.
#split_periodic_mirrored.brep is the mirrored copy of a part with six periodic faces, as a CAD system wrote it, with
#the vertices of its small bores 2e-9 rad past the seam.
#
#  PrePoMax.com -f blind_holes.brep -u MM_TON_S_C --script Test_Model_Split_Periodic_Faces.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")


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


def faces(part):
    data = part.get_face_data()
    rows = []
    for i in range(data.count):
        rows.append({"id": int(list(data.ids)[i]),
                     "type": list(data.types)[i],
                     "area": float(list(data.areas)[i]),
                     "x": float(list(data.centroids[i])[0])})
    return rows


def cylinders(part):
    return [row for row in faces(part) if row["type"] == "Cylinder"]


def import_parts(file_name):
    before = set(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, file_name))
    return [part for part in pmx.model.geometry.parts if part.name not in before]


def test_part():
    parts = pmx.model.geometry.parts
    part = parts.first()
    name = part.name
    part_id = part.id
    check(len(faces(part)) == 11 and len(cylinders(part)) == 3,
          "the import must not split the three bores: " + str(faces(part)))
    #
    refused(lambda: parts.split_periodic_faces(name, number_of_faces=1), "number_of_faces")
    #
    added = parts.split_periodic_faces(name)
    check(dict(added) == {name: 3}, "each of the three bores must gain one face: " + str(dict(added)))
    part = parts[name]
    check(part.id == part_id and part.part_type == "SolidAsShell",
          "the part must keep its id and stay a solid: " + str(part.id) + " " + part.part_type)
    check(len(faces(part)) == 14 and len(cylinders(part)) == 6,
          "the part must hold the six halves of the bores: " + str(faces(part)))
    # The halves of a bore no longer close on themselves
    added = parts.split_periodic_faces(name)
    check(dict(added) == {name: 0}, "a part without periodic faces must report zero: " + str(dict(added)))
    # The two halves of the through hole are the only cylinders of their area, and fused they stay one face
    through_area = max(row["area"] for row in cylinders(part))
    halves = [row["id"] for row in cylinders(part) if abs(row["area"] - through_area) < 1e-6]
    check(len(halves) == 2, "the through hole must be split into two halves: " + str(cylinders(part)))
    fused = parts.fuse_faces(pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces',
                                                         {part.id: halves}))
    check(dict(fused) == {name: 1}, "the halves of the through hole must fuse into one face: " + str(dict(fused)))
    check(len(cylinders(parts[name])) == 5, "the fused wall must stay one face: " + str(cylinders(parts[name])))


def test_more_faces():
    part = import_parts("blind_holes.brep")[0]
    added = pmx.model.geometry.parts.split_periodic_faces([part.name], number_of_faces=3)
    check(dict(added) == {part.name: 6}, "each bore split into three must gain two faces: " + str(dict(added)))


def test_defeature_keeps_splits():
    parts = pmx.model.geometry.parts
    part = import_parts("blind_holes.brep")[0]
    parts.split_periodic_faces(part.name)
    part = parts[part.name]
    # The blind hole nearest the end of the box: its two halves and its cap
    first_x = min(row["x"] for row in cylinders(part))
    hole = [row for row in faces(part) if abs(row["x"] - first_x) < 1 and
            (row["type"] == "Cylinder" or row["area"] < 100)]
    check(len(hole) == 3, "the blind hole must be two halves and a cap: " + str(hole))
    removed = parts.defeature(pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                                          {part.id: [row["id"] for row in hole]}))
    check(dict(removed) == {part.name: 3}, "only the faces of the blind hole must be removed: " + str(dict(removed)))
    check(len(cylinders(parts[part.name])) == 4,
          "the other two bores must keep their halves: " + str(cylinders(parts[part.name])))


def test_compound():
    parts = pmx.model.geometry.parts
    added_parts = import_parts("sweep_holes_side.brep")
    compound = [part for part in added_parts if part.part_type == "Compound"]
    solids = sorted(part.name for part in added_parts if part.part_type != "Compound")
    check(len(compound) == 1 and len(solids) == 2, "the model must import as a compound of two solids: " +
          str([(part.name, part.part_type) for part in added_parts]))
    compound = compound[0]
    added = parts.split_periodic_faces(solids[0])
    check(dict(added) == {compound.name: 2},
          "a sub-part must be split with its compound, the bore of each solid gaining a face: " + str(dict(added)))
    names = list(parts.get_names())
    check(compound.name in names and all(name in names for name in solids),
          "the compound and its sub-parts must keep their names: " + str(names))
    check(sum(len(cylinders(parts[name])) for name in solids) == 4, "each bore must be split into two halves")


def test_vertices_past_seam():
    parts = pmx.model.geometry.parts
    part = import_parts("split_periodic_mirrored.brep")[0]
    check(len(faces(part)) == 31, "the mirrored part must import with 31 faces: " + str(len(faces(part))))
    added = parts.split_periodic_faces(part.name)
    check(dict(added) == {part.name: 6}, "each of the six periodic faces must gain one face: " + str(dict(added)))
    check(len(faces(parts[part.name])) == 37, "the split part must hold 37 faces: " + str(len(faces(parts[part.name]))))


def test_not_cad():
    stl = import_parts("cube.stl")[0]
    refused(lambda: pmx.model.geometry.parts.split_periodic_faces(stl.name), "CAD part")


def main():
    test_part()
    test_more_faces()
    test_defeature_keeps_splits()
    test_compound()
    test_vertices_past_seam()
    test_not_cad()
    print("TEST PASSED")


main()
