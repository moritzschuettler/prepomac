#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks deleting the faces of CAD parts. The faces are taken out and the gap is left open, so a solid
#that loses a face is no longer closed and becomes a shell part, and a shell part loses faces and
#stays a shell. Either way the part is replaced in place and keeps its name and part id. When the faces
#it keeps fall apart into more pieces than it had, the largest piece stays the part and every other one
#becomes a shell part of its own. A body whose last face is deleted is deleted.
#
#A compound holds only solids, so a sub-part or a split cell that loses a face leaves it as a shell part
#with its own name and id, the solids that lost nothing stay the compound, and one solid left gives way
#to a plain part. A face two sub-parts share is deleted from both. sweep_stack3.brep is an ordinary
#compound of three stacked boxes; blind_holes.brep cut by a plane gives a split compound of two cells.
#
#blind_holes.brep is a 60 x 40 x 20 box with two blind holes drilled from the top face and one hole
#that goes all the way through.
#
#  PrePoMax.com -f blind_holes.brep -u MM_TON_S_C --script Test_Model_Delete_Cad_Faces.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
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
    # Keeping only the hole walls leaves three pieces that do not touch. The largest, the wall of the
    # through hole, stays the part with its name and id, and each of the others becomes a part of its own
    parts = pmx.model.geometry.parts
    rows = faces(part())
    walls = [row for row in rows if row["type"] == "Cylinder"]
    others = [row for row in rows if row["type"] != "Cylinder"]
    deleted = parts.delete_cad_faces(selection(part(), others))
    check(deleted[name] == len(others), "every face but the walls must be deleted: " + str(dict(deleted)))
    names = list(parts.get_names())
    check(parts[name].id == part_id and parts[name].part_type == "Shell",
          "the largest piece must keep the name and id of the part")
    pieces = [n for n in names if n.startswith(name + "-")]
    check(len(pieces) == 2, "the two other pieces must be parts of their own: " + str(names))
    through_wall_area = max(row["area"] for row in walls)
    check(abs(sum(row["area"] for row in faces(parts[name])) - through_wall_area) < 1e-6,
          "the part must keep the largest piece, the wall of the through hole")
    check(sum(len(faces(parts[n])) for n in pieces + [name]) == len(walls), "the pieces must hold every wall")
    # A part whose every face is deleted is deleted with them
    rows = faces(parts[name])
    deleted = pmx.model.geometry.parts.delete_cad_faces(selection(parts[name], rows))
    check(deleted[name] == len(rows), "every face must be reported deleted: " + str(dict(deleted)))
    check(name not in list(pmx.model.geometry.parts.get_names()), "a part without faces must be deleted")
    # Only CAD parts are touched: an STL part in the selection is left alone and reports zero
    before = list(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "cube.stl"))
    stl = [p for p in pmx.model.geometry.parts if p.name not in before][0]
    stl_faces = len(faces(stl))
    nothing = pmx.model.geometry.parts.delete_cad_faces(
        pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {stl.id: [1]}))
    check(stl.name not in nothing or nothing[stl.name] == 0,
          "an STL part has no CAD face to delete: " + str(dict(nothing)))
    check(len(faces(pmx.model.geometry.parts[stl.name])) == stl_faces, "the STL part must be left as it was")
    test_ordinary_compound()
    test_shared_face()
    test_last_face()
    test_compound_body_falls_apart()
    test_split_compound()
    print("TEST PASSED")


def z_faces(p):
    # The face ids of a part ordered by the height of their centroids, lowest first
    data = p.get_face_data()
    ids = [int(i) for i in list(data.ids)]
    zs = [float(list(c)[2]) for c in list(data.centroids)]
    return [i for _, i in sorted(zip(zs, ids))]


def import_stack():
    parts = pmx.model.geometry.parts
    before = list(parts.get_names())
    pmx.import_file(os.path.join(MODELS, "sweep_stack3.brep"))
    compound = [p for p in parts if p.name not in before and p.part_type == "Compound"][0]
    sub_parts = sorted([p for p in parts if p.name not in before and p.part_type != "Compound"],
                       key=lambda p: float(list(p.center_of_mass)[2]))
    check(len(sub_parts) == 3, "sweep_stack3 must be three sub-parts: " + str([p.name for p in sub_parts]))
    return compound.name, compound.id, [(p.name, p.id) for p in sub_parts]


def delete(p, face_ids):
    return pmx.model.geometry.parts.delete_cad_faces(
        pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {p.id: face_ids}))


# sweep_stack3.brep is an ordinary compound of three stacked boxes. A sub-part that loses a face leaves the
# compound as a shell part, keeping its name and id, and the others stay the compound; one solid left gives
# way to a plain part named after it
def test_ordinary_compound():
    parts = pmx.model.geometry.parts
    name, part_id, (bottom, middle, top) = import_stack()
    deleted = delete(parts[top[0]], z_faces(parts[top[0]])[-1:])
    check(deleted[top[0]] == 1, "the top face must be deleted from the top box: " + str(dict(deleted)))
    check(parts[top[0]].part_type == "Shell" and parts[top[0]].id == top[1],
          "the top box must be a shell part with its id: %s %d" % (parts[top[0]].part_type, parts[top[0]].id))
    compound = parts[name]
    check(compound.part_type == "Compound" and compound.id == part_id, "the compound must keep its name and id")
    check(parts[bottom[0]].id == bottom[1] and parts[middle[0]].id == middle[1],
          "the sub-parts that stayed must keep their ids")
    check(parts[bottom[0]].part_type == "SolidAsShell" and parts[middle[0]].part_type == "SolidAsShell",
          "the sub-parts that stayed must stay solids")
    # The middle box opened leaves the bottom box alone, which is no compound any more
    deleted = delete(parts[middle[0]], z_faces(parts[middle[0]])[-1:])
    check(deleted[middle[0]] == 1, "one face must be deleted from the middle box: " + str(dict(deleted)))
    check(name not in list(parts.get_names()), "a compound of one solid must give way to a plain part")
    check(parts[bottom[0]].id == bottom[1] and parts[bottom[0]].part_type == "SolidAsShell",
          "the bottom box must be a plain solid part with its id")
    check(parts[middle[0]].part_type == "Shell" and parts[middle[0]].id == middle[1],
          "the middle box must be a shell part with its id")


# A face two sub-parts share is gone from both, so both open and leave the compound
def test_shared_face():
    parts = pmx.model.geometry.parts
    name, part_id, (bottom, middle, top) = import_stack()
    deleted = delete(parts[bottom[0]], z_faces(parts[bottom[0]])[-1:])
    check(deleted[bottom[0]] == 1 and deleted[middle[0]] == 1,
          "the shared face must be deleted from both boxes: " + str(dict(deleted)))
    for sub_part in (bottom, middle):
        check(parts[sub_part[0]].part_type == "Shell" and parts[sub_part[0]].id == sub_part[1],
              sub_part[0] + " must be a shell part with its id")
    check(name not in list(parts.get_names()) and parts[top[0]].id == top[1] and
          parts[top[0]].part_type == "SolidAsShell", "the top box must be left as a plain solid part")


# A box that loses every face is gone, and the boxes it shared a face with are opened by losing it
def test_last_face():
    parts = pmx.model.geometry.parts
    name, part_id, (bottom, middle, top) = import_stack()
    middle_faces = len(faces(parts[middle[0]]))
    deleted = delete(parts[middle[0]], z_faces(parts[middle[0]]))
    check(deleted[middle[0]] == middle_faces, "every face of the middle box must be deleted: " + str(dict(deleted)))
    names = list(parts.get_names())
    check(middle[0] not in names and name not in names, "the middle box and the compound must be gone")
    for sub_part in (bottom, top):
        check(parts[sub_part[0]].part_type == "Shell" and parts[sub_part[0]].id == sub_part[1] and
              deleted[sub_part[0]] == 1, sub_part[0] + " must be a shell part with its id, one face less")


# The middle box losing its four sides falls into its bottom and its top, which no longer touch: the larger
# keeps its name and id - they are the same size, so either - and the other becomes a part of its own. The
# boxes above and below keep the faces they shared with it and stay the compound
def test_compound_body_falls_apart():
    parts = pmx.model.geometry.parts
    name, part_id, (bottom, middle, top) = import_stack()
    sides = z_faces(parts[middle[0]])[1:-1]
    check(len(sides) == 4, "the middle box must have four sides: " + str(sides))
    deleted = delete(parts[middle[0]], sides)
    check(deleted[middle[0]] == 4, "the four sides must be deleted: " + str(dict(deleted)))
    names = list(parts.get_names())
    check(parts[middle[0]].part_type == "Shell" and parts[middle[0]].id == middle[1],
          "the middle box must be a shell part with its id")
    check(middle[0] + "-2" in names and len(faces(parts[middle[0] + "-2"])) == 1 and
          len(faces(parts[middle[0]])) == 1, "the other piece must be a part of its own: " + str(names))
    check(parts[name].part_type == "Compound" and parts[name].id == part_id,
          "the compound of the two other boxes must keep its name and id")


# A cell of a split compound that loses a face leaves it the same way, under its own name and id
def test_split_compound():
    parts = pmx.model.geometry.parts
    before = list(parts.get_names())
    pmx.import_file(os.path.join(MODELS, "blind_holes.brep"))
    part = [p for p in parts if p.name not in before][0]
    name, part_id = part.name, part.id
    tool = pmx.create.split_tool_plane_by_point_and_normal([0, 0, 15], [0, 0, 1])
    parts.split_solid(pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]), tool)
    compound = [p for p in parts if len(list(p.split_sub_part_names)) > 0][-1]
    cells = sorted(compound.get_split_cell_names(name), key=lambda c: float(list(parts[c].center_of_mass)[2]))
    check(len(cells) == 2, "the plane must cut the part in two: " + str(cells))
    compound_name = compound.name
    top_name, top_id = cells[1], parts[cells[1]].id
    top_faces = len(faces(parts[top_name]))
    #
    deleted = delete(parts[top_name], z_faces(parts[top_name])[-1:])
    check(deleted[top_name] == 1, "the top face must be deleted from the top cell: " + str(dict(deleted)))
    check(parts[top_name].part_type == "Shell" and parts[top_name].id == top_id and
          len(faces(parts[top_name])) == top_faces - 1, "the top cell must be a shell part with its name and id")
    check(compound_name not in list(parts.get_names()), "a split compound of one cell must give way to a part")
    check(parts[name].part_type == "SolidAsShell" and parts[name].id == part_id,
          "the bottom cell must be a plain solid part with the name and id of its sub-part")


main()
