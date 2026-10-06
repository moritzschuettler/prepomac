#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the face offset: the picked surfaces are lifted out of the part that carries them and
#added to the model as a new shell part, while that part is left exactly as it was. A non zero offset
#moves the new surfaces along their own normals, outwards when it is positive, which is what a
#mid-surface is made of. Surfaces that touch are sewn together, so what is picked in one operation
#comes back as one body rather than as loose faces - and the surfaces of several sub-parts of one
#compound are sewn the same way, because the compound is the geometry all of them are written out of.
#
#blind_holes.brep is a 60 x 40 x 20 box with two blind holes drilled from the top face and one hole
#that goes all the way through. sweep_stack_small.brep is a 10 x 10 x 5 box with a 6 x 6 x 5 box on
#top of it, fragmented, so the two share their contact face and import as a compound part: the top of
#the large box is split into that shared face and the rim around it, and the walls of the small box
#stand on the boundary between the two.
#
#  PrePoMax.com -f blind_holes.brep -u MM_TON_S_C --script Test_Model_Offset_Face.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
TOLERANCE = 1.0E-6


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def close(a, b, message):
    check(abs(a - b) < TOLERANCE * max(1.0, abs(b)), message + ": " + str(a) + " against " + str(b))


def faces(part):
    data = part.get_face_data()
    centroids = list(data.centroids)
    rows = []
    for i in range(data.count):
        centroid = centroids[i]
        rows.append({"id": int(list(data.ids)[i]),
                     "type": list(data.types)[i],
                     "area": float(list(data.areas)[i]),
                     "z": float(list(centroid)[2])})
    return rows


def edge_count(part):
    return part.get_edge_data().count


def selection(part, rows):
    return pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                       {part.id: [row["id"] for row in rows]})


def offset_faces(selection_to_use, offset=None):
    if offset is None:
        names = pmx.model.geometry.parts.offset_face(selection_to_use)
    else:
        names = pmx.model.geometry.parts.offset_face(selection_to_use, offset)
    check(type(names).__name__ == "StringListAPI", "offset_face must return a StringList: " + type(names).__name__)
    return list(names)


def part_named(name):
    for part in pmx.model.geometry.parts:
        if part.name == name:
            return part
    raise Exception("TEST FAILED: the part " + name + " is not in the model.")


def top_face(rows):
    planes = [row for row in rows if row["type"] == "Plane"]
    return max(planes, key=lambda row: (round(row["z"], 6), row["area"]))


def side_faces(rows, top):
    return [row for row in rows if row["type"] == "Plane" and row["z"] < top["z"] - TOLERANCE and
            row["area"] > 100.0]


def import_compound():
    """The two fragmented boxes of sweep_stack_small.brep, which import as a compound part."""
    before = set(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "sweep_stack_small.brep"))
    added = [part for part in pmx.model.geometry.parts if part.name not in before]
    solids = [part for part in added if part.part_type in ("Solid", "SolidAsShell") and part.is_cad_part]
    compounds = [part for part in added if part.part_type == "Compound"]
    check(len(compounds) == 1 and len(solids) == 2,
          "the two fragmented boxes must import as a compound part: " + str([p.name for p in added]))
    large = [part for part in solids if part.volume > 300.0]
    small = [part for part in solids if part.volume <= 300.0]
    check(len(large) == 1 and len(small) == 1, "the compound must hold the large box and the small one")
    return compounds[0], large[0], small[0]


# Two faces that meet along an edge lose that edge to the sewing when they are offset together, so
# the edge count is what says whether the face offset glued them or left them loose
def check_glued(first_name, second_name, both_name, message):
    separate = edge_count(part_named(first_name)) + edge_count(part_named(second_name))
    together = edge_count(part_named(both_name))
    check(together < separate,
          message + ": " + str(together) + " edges together against " + str(separate) + " apart")


# A sub-part of a compound carries no geometry of its own: the compound is written to the modelling
# kernel and the faces of the sub-part are numbered inside the solid it was written out of. Picking
# the surfaces of several sub-parts is still one face offset, so they are sewn together.
def test_compound():
    compound, large, small = import_compound()
    # The top of the large box is split by the contact face, and the rim around it is what the walls of
    # the small box stand on
    rim = top_face(faces(large))
    walls = [row for row in faces(small) if row["z"] > 5.0 + TOLERANCE and row["z"] < 10.0 - TOLERANCE]
    check(len(walls) == 4, "the small box must stand on the rim with four walls: " + str(faces(small)))
    wall = walls[0]
    #
    names = offset_faces(selection(large, [rim]))
    check(len(names) == 1, "one part must be created from a sub-part: " + str(names))
    check(names[0] == large.name + "_Offset-1",
          "the part must be named after the sub-part it was picked on: " + names[0])
    rim_part = part_named(names[0])
    check(rim_part.part_type == "Shell", "the offset part must be a shell part: " + rim_part.part_type)
    close(rim_part.surface_area, rim["area"], "the surface offset from a sub-part must keep its area")
    limits = rim_part.get_bounding_box()
    close(limits[4], rim["z"], "the surface must stay where it was taken from")
    close(limits[5], rim["z"], "the surface must stay where it was taken from")
    # The face ids of a sub-part mean nothing in the compound as a whole, so a wrong solid number would
    # quietly take the face of the other box - this is what says it did not
    names = offset_faces(selection(small, [wall]))
    check(len(names) == 1, "one part must be created from the second sub-part: " + str(names))
    check(names[0] == small.name + "_Offset-1", "the second part must be named after its sub-part: " + names[0])
    wall_part = part_named(names[0])
    close(wall_part.surface_area, wall["area"],
          "the surface offset from the second sub-part must keep its area")
    # One selection over both sub-parts is one face offset: one part, and the rim and the wall standing on
    # it are sewn along the edge they share
    both = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                       {large.id: [rim["id"]], small.id: [wall["id"]]})
    names = offset_faces(both)
    check(len(names) == 1, "the surfaces of one compound must become one part: " + str(names))
    check(names[0] == compound.name + "_Offset-1",
          "a part built from several sub-parts must be named after the compound: " + names[0])
    glued = part_named(names[0])
    check(len(faces(glued)) == 2, "the offset part must carry both picked surfaces: " + str(faces(glued)))
    close(glued.surface_area, rim["area"] + wall["area"], "the offset part must keep the area of both")
    check_glued(rim_part.name, wall_part.name, glued.name,
                "the surfaces of two sub-parts must be sewn together")
    # The sub-parts are left as they were
    check(len(faces(part_named(large.name))) > 0 and len(faces(part_named(small.name))) > 0,
          "the sub-parts must still be in the model")


# Surfaces of one part that share no edge become a part each, and each of them carries only its own
# geometry: the part is meshed from that geometry, so a part carrying the others as well is meshed with
# them and its mesh covers surfaces that belong to other parts
def test_apart(box, sides):
    first = sides[0]
    opposite = [row for row in sides[1:] if abs(row["area"] - first["area"]) < TOLERANCE * first["area"]]
    check(len(opposite) == 1, "the box must carry one side face opposite the first one: " + str(sides))
    names = offset_faces(selection(box, [first, opposite[0]]))
    check(len(names) == 2, "two surfaces that share no edge must become two parts: " + str(names))
    for name in names:
        part = part_named(name)
        check(part.is_cad_part, "every part offset apart must carry its own CAD geometry: " + name)
        check(len(faces(part)) == 1, "every part offset apart must carry one face: " + name)
        close(part.surface_area, first["area"], "every part offset apart must keep the area of its face")
    pmx.model.mesh.generate(names)
    mesh_parts = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name in names]
    check(len(mesh_parts) == 2, "both parts offset apart must be meshed: " + str([p.name for p in mesh_parts]))
    for mesh_part in mesh_parts:
        close(mesh_part.surface_area, first["area"],
              "the mesh of a part offset apart must cover its own face only: " + mesh_part.name)


def main():
    box = pmx.model.geometry.parts.first()
    rows = faces(box)
    box_face_count = len(rows)
    box_area = box.surface_area
    top = top_face(rows)
    close(top["z"], 20.0, "the top face of the box must lie at z = 20")
    # The plain face offset: one new shell part carrying the one picked surface
    names = offset_faces(selection(box, [top]))
    check(len(names) == 1, "one part must be created: " + str(names))
    check(names[0] == box.name + "_Offset-1", "the part must be named after its source: " + names[0])
    offset_part = part_named(names[0])
    check(offset_part.part_type == "Shell", "the offset part must be a shell part: " + offset_part.part_type)
    offset_rows = faces(offset_part)
    check(len(offset_rows) == 1, "the offset part must carry one face: " + str(offset_rows))
    close(offset_part.surface_area, top["area"], "the offset surface must keep its area")
    box_limits = box.get_bounding_box()
    limits = offset_part.get_bounding_box()
    close(limits[4], 20.0, "the offset surface must stay where it was taken from")
    close(limits[5], 20.0, "the offset surface must stay where it was taken from")
    # The part the surface was taken from is left as it was
    box = part_named(box.name)
    check(len(faces(box)) == box_face_count, "the source part must keep its faces")
    close(box.surface_area, box_area, "the source part must keep its area")
    # The mid-surface: half the wall thickness entered as a negative value on an outer surface
    names = offset_faces(selection(box, [top]), -10.0)
    check(len(names) == 1, "one part must be created: " + str(names))
    check(names[0] == box.name + "_Offset-2", "the second part must be numbered: " + names[0])
    middle = part_named(names[0])
    limits = middle.get_bounding_box()
    close(limits[4], 10.0, "the offset surface must lie halfway through the box")
    close(limits[5], 10.0, "the offset surface must lie halfway through the box")
    close(middle.surface_area, top["area"], "an offset plane must keep its area")
    # An offset of an offset part is named after that part, so its own offsets are numbered apart
    names = offset_faces(selection(middle, faces(middle)))
    check(names[0] == middle.name + "_Offset-1", "an offset of an offset part must be named after it: " + names[0])
    # A positive offset moves the surface out of the body it was taken from
    names = offset_faces(selection(box, [top]), 5.0)
    limits = part_named(names[0]).get_bounding_box()
    close(limits[4], 25.0, "a positive offset must move the surface outwards")
    # Several surfaces of one part become one part, sewn along the edges they share
    sides = side_faces(rows, top)
    check(len(sides) >= 2, "the box must carry side faces: " + str(sides))
    side_names = offset_faces(selection(box, [sides[0]]))
    pair_names = offset_faces(selection(box, [top, sides[0]]))
    check_glued(offset_part.name, side_names[0], pair_names[0],
                "the surfaces of one part must be sewn together")
    picked = [top] + sides[:2]
    names = offset_faces(selection(box, picked))
    check(len(names) == 1, "the surfaces of one part must become one part: " + str(names))
    patch = part_named(names[0])
    check(len(faces(patch)) == len(picked),
          "the offset part must carry every picked surface: " + str(faces(patch)))
    # A connected patch is offset as a whole, so the faces that meet are trimmed against each other
    names = offset_faces(selection(box, picked), -2.0)
    offset_patch = part_named(names[0])
    check(len(faces(offset_patch)) == len(picked),
          "the offset patch must carry every picked surface: " + str(faces(offset_patch)))
    limits = offset_patch.get_bounding_box()
    check(limits[1] - limits[0] < box_limits[1] - box_limits[0] - TOLERANCE,
          "a patch offset inwards must be smaller than the body it was taken from: " + str(limits))
    test_apart(box, sides)
    test_compound()
    print("TEST PASSED")


main()
