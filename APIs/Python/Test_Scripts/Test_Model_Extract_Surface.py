#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the surface extraction: the picked surfaces are lifted out of the part that carries them and
#added to the model as a new shell part, while that part is left exactly as it was. A non zero offset
#moves the extracted surfaces along their own normals, outwards when it is positive, which is what a
#mid-surface is made of.
#
#blind_holes.brep is a 60 x 40 x 20 box with two blind holes drilled from the top face and one hole
#that goes all the way through. sweep_stack_small.brep is a small box on a large one, fragmented, so
#the two share their contact face and import as a compound part - a sub-part of which carries no
#geometry of its own and is extracted from the geometry of its compound.
#
#  PrePoMax.com -f blind_holes.brep -u MM_TON_S_C --script Test_Model_Extract_Surface.py -g No -x Yes -w <dir>
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


def selection(part, rows):
    return pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                       {part.id: [row["id"] for row in rows]})


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
    return compounds[0], solids


# A sub-part of a compound carries no geometry of its own: the compound is written to the modelling
# kernel and the faces of the sub-part are numbered inside the solid it was written out of. Each
# sub-part still gives a part of its own, the way every other part does.
def test_compound():
    compound, solids = import_compound()
    first, second = solids[0], solids[1]
    first_top = top_face(faces(first))
    second_top = top_face(faces(second))
    #
    names = list(pmx.model.geometry.parts.extract_surface(selection(first, [first_top])))
    check(len(names) == 1, "one part must be created from a sub-part: " + str(names))
    check(names[0] == first.name + "-Surface",
          "the part must be named after the sub-part it was picked on: " + names[0])
    extracted = part_named(names[0])
    check(extracted.part_type == "Shell", "the extracted part must be a shell part: " + extracted.part_type)
    check(len(faces(extracted)) == 1, "the extracted part must carry one face")
    close(extracted.surface_area, first_top["area"],
          "the surface extracted from a sub-part must keep its area")
    limits = list(extracted.get_bounding_box())
    close(limits[4], first_top["z"], "the surface must stay where it was taken from")
    close(limits[5], first_top["z"], "the surface must stay where it was taken from")
    # The face ids of a sub-part mean nothing in the compound as a whole, so a wrong solid number
    # would quietly take the face of the other box - this is what says it did not
    other = list(pmx.model.geometry.parts.extract_surface(selection(second, [second_top])))
    check(len(other) == 1, "one part must be created from the second sub-part: " + str(other))
    check(other[0] == second.name + "-Surface", "the second part must be named after its sub-part: " + other[0])
    second_extracted = part_named(other[0])
    close(second_extracted.surface_area, second_top["area"],
          "the surface extracted from the second sub-part must keep its area")
    limits = list(second_extracted.get_bounding_box())
    close(limits[4], second_top["z"], "the second surface must stay where it was taken from")
    # One selection over both sub-parts gives one part each, and the offset follows each surface
    both = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                       {first.id: [first_top["id"]], second.id: [second_top["id"]]})
    names = list(pmx.model.geometry.parts.extract_surface(both, -1.0))
    check(len(names) == 2, "each sub-part must give a part of its own: " + str(names))
    for source, row in ((first, first_top), (second, second_top)):
        named = [name for name in names if name.startswith(source.name + "-Surface")]
        check(len(named) == 1,
              "one of the parts must be named after " + source.name + ": " + str(names))
        limits = list(part_named(named[0]).get_bounding_box())
        close(limits[4], row["z"] - 1.0, "the offset surface must lie one below the face it was taken from")
    # The compound itself is left as it was
    check(len(faces(part_named(first.name))) > 0 and len(faces(part_named(second.name))) > 0,
          "the sub-parts must still be in the model")


def main():
    box = pmx.model.geometry.parts.first()
    rows = faces(box)
    box_face_count = len(rows)
    box_area = box.surface_area
    top = top_face(rows)
    close(top["z"], 20.0, "the top face of the box must lie at z = 20")
    # The plain extraction: one new shell part carrying the one picked surface
    names = list(pmx.model.geometry.parts.extract_surface(selection(box, [top])))
    check(len(names) == 1, "one part must be created: " + str(names))
    check(names[0] == box.name + "-Surface", "the part must be named after its source: " + names[0])
    extracted = part_named(names[0])
    check(extracted.part_type == "Shell", "the extracted part must be a shell part: " + extracted.part_type)
    extracted_rows = faces(extracted)
    check(len(extracted_rows) == 1, "the extracted part must carry one face: " + str(extracted_rows))
    close(extracted.surface_area, top["area"], "the extracted surface must keep its area")
    box_limits = list(box.get_bounding_box())
    limits = list(extracted.get_bounding_box())
    close(limits[4], 20.0, "the extracted surface must stay where it was taken from")
    close(limits[5], 20.0, "the extracted surface must stay where it was taken from")
    # The part the surface was taken from is left as it was
    box = part_named(box.name)
    check(len(faces(box)) == box_face_count, "the source part must keep its faces")
    close(box.surface_area, box_area, "the source part must keep its area")
    # The mid-surface: half the wall thickness entered as a negative value on an outer surface
    names = list(pmx.model.geometry.parts.extract_surface(selection(box, [top]), -10.0))
    check(len(names) == 1, "one part must be created: " + str(names))
    check(names[0] == box.name + "-Surface-2", "the second part must be numbered: " + names[0])
    middle = part_named(names[0])
    limits = list(middle.get_bounding_box())
    close(limits[4], 10.0, "the offset surface must lie halfway through the box")
    close(limits[5], 10.0, "the offset surface must lie halfway through the box")
    close(middle.surface_area, top["area"], "an offset plane must keep its area")
    # A positive offset moves the surface out of the body it was taken from
    names = list(pmx.model.geometry.parts.extract_surface(selection(box, [top]), 5.0))
    limits = list(part_named(names[0]).get_bounding_box())
    close(limits[4], 25.0, "a positive offset must move the surface outwards")
    # Several surfaces of one part become one part, and a connected patch is offset as a whole: the
    # faces that meet are trimmed against each other instead of coming apart at the edge they share
    sides = side_faces(rows, top)
    check(len(sides) >= 2, "the box must carry side faces: " + str(sides))
    picked = [top] + sides[:2]
    names = list(pmx.model.geometry.parts.extract_surface(selection(box, picked)))
    check(len(names) == 1, "the surfaces of one part must become one part: " + str(names))
    patch = part_named(names[0])
    check(len(faces(patch)) == len(picked),
          "the extracted part must carry every picked surface: " + str(faces(patch)))
    #
    names = list(pmx.model.geometry.parts.extract_surface(selection(box, picked), -2.0))
    offset_patch = part_named(names[0])
    check(len(faces(offset_patch)) == len(picked),
          "the offset patch must carry every picked surface: " + str(faces(offset_patch)))
    limits = list(offset_patch.get_bounding_box())
    check(limits[1] - limits[0] < box_limits[1] - box_limits[0] - TOLERANCE,
          "a patch offset inwards must be smaller than the body it was taken from: " + str(limits))
    test_compound()
    print("TEST PASSED")


main()
