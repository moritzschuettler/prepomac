#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks deleting the faces of CAD parts. The faces are taken out and the gap is left open, so a solid
#that loses a face is no longer closed and becomes a shell part, and a shell part loses faces and
#stays a shell. Either way the part is replaced in place and keeps its name and part id. A part is
#left as it was when the faces it would keep fall apart into more pieces than it had, or when no face
#would be left, and the report says so with a zero.
#
#blind_holes.brep is a 60 x 40 x 20 box with two blind holes drilled from the top face and one hole
#that goes all the way through.
#
#  PrePoMax.com -f blind_holes.brep -u MM_TON_S_C --script Test_Model_Delete_Cad_Faces.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

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


def caps_of_blind_holes(rows):
    return [row for row in rows if row["type"] == "Plane" and row["area"] < CAP_AREA]


def part():
    return pmx.model.geometry.parts.first()


def main():
    solid = part()
    name = solid.name
    part_id = solid.id
    rows = faces(solid)
    caps = caps_of_blind_holes(rows)
    # A solid geometry part is visualized by its surface, which is what the part type says
    check(solid.part_type == "SolidAsShell" and len(caps) == 2,
          "the test part must be a solid with two blind holes: " + solid.part_type + " " + str(rows))
    # A solid that loses a face is open, so it comes back as a shell with the same name and id
    deleted = pmx.model.geometry.parts.delete_cad_faces(selection(solid, caps[:1]))
    check(deleted[name] == 1, "one face must be deleted from the solid: " + str(dict(deleted)))
    shell = part()
    check(shell.name == name and shell.id == part_id,
          "the part must keep its name and id: " + shell.name + " " + str(shell.id))
    check(shell.part_type == "Shell", "a solid that lost a face must be a shell: " + shell.part_type)
    check(len(faces(shell)) == len(rows) - 1, "the shell must have one face less than the solid")
    # A shell loses faces and stays a shell
    rows = faces(shell)
    deleted = pmx.model.geometry.parts.delete_cad_faces(selection(shell, caps_of_blind_holes(rows)))
    check(deleted[name] == 1, "the second cap must be deleted from the shell: " + str(dict(deleted)))
    check(part().part_type == "Shell" and len(faces(part())) == len(rows) - 1,
          "the shell must stay a shell with one face less")
    # Keeping only the hole walls would leave three pieces that do not touch, which is refused
    rows = faces(part())
    others = [row for row in rows if row["type"] != "Cylinder"]
    deleted = pmx.model.geometry.parts.delete_cad_faces(selection(part(), others))
    check(deleted[name] == 0, "a part that would fall apart must be left alone: " + str(dict(deleted)))
    check(len(faces(part())) == len(rows), "a refused deletion must leave the faces as they were")
    # Deleting every face would leave nothing, which is refused as well
    deleted = pmx.model.geometry.parts.delete_cad_faces(selection(part(), rows))
    check(deleted[name] == 0, "deleting every face must be refused: " + str(dict(deleted)))
    check(len(faces(part())) == len(rows), "a refused deletion must leave the faces as they were")
    # Only CAD parts are touched: an STL part in the selection is left alone and reports zero
    import os
    import sys
    models = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
    before = list(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(models, "cube.stl"))
    stl = [p for p in pmx.model.geometry.parts if p.name not in before][0]
    stl_faces = len(faces(stl))
    nothing = pmx.model.geometry.parts.delete_cad_faces(
        pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {stl.id: [1]}))
    check(stl.name not in nothing or nothing[stl.name] == 0,
          "an STL part has no CAD face to delete: " + str(dict(nothing)))
    check(len(faces(pmx.model.geometry.parts[stl.name])) == stl_faces, "the STL part must be left as it was")
    print("TEST PASSED")


main()
