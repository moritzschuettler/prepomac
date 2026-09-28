#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the two splitting operations, split_shell and split_solid, and the splitting tools
#pmx.create makes for them. A shell split divides the selected surfaces and leaves the part the
#kind of body it was, under its own name; a solid split that cuts a part in two replaces it by a
#compound part of the pieces, whose sub-part can then be renamed. Both report the number of faces
#every part gained.
#
#A tool whose arguments contradict each other, or a split the tool cannot apply, is refused before
#anything is recorded, so the geometry must be left exactly as it was.
#
#blind_holes.brep is a 60 x 40 x 20 box with two blind holes drilled from the top face and one hole
#that goes all the way through.
#
#  PrePoMax.com -f blind_holes.brep -u MM_TON_S_C --script Test_Model_Split_Geometry.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
TOLERANCE = 1.0E-6


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def face_count(part):
    return part.get_face_data().count


def corners(part):
    vertices = part.get_vertex_data()
    rows = []
    for i in range(vertices.count):
        rows.append({"node_id": int(list(vertices.node_ids)[i]),
                     "xyz": [float(v) for v in list(vertices.coordinates[i])]})
    low = [min(row["xyz"][j] for row in rows) for j in range(3)]
    high = [max(row["xyz"][j] for row in rows) for j in range(3)]
    return rows, low, high


def corner(rows, xyz):
    for row in rows:
        if all(abs(row["xyz"][j] - xyz[j]) < TOLERANCE for j in range(3)):
            return row["node_id"]
    raise Exception("TEST FAILED: no vertex at " + str(xyz))


def top_face_id(part, high):
    data = part.get_face_data()
    best = None
    for i in range(data.count):
        z = float(list(data.centroids[i])[2])
        area = float(list(data.areas)[i])
        if list(data.types)[i] == "Plane" and abs(z - high[2]) < TOLERANCE:
            if best is None or area > best[1]:
                best = (int(list(data.ids)[i]), area)
    check(best is not None, "the box must have a planar top face")
    return best[0]


def refused(call, words):
    try:
        call()
    except Exception as ex:
        message = str(ex)
        check(words in message, "the refusal must say '" + words + "': " + message)
        return
    raise Exception("TEST FAILED: the call was not refused, expected '" + words + "'")


def part_region(part):
    return pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])


def test_refusals(part):
    parts = pmx.model.geometry.parts
    names = list(parts.get_names())
    faces = face_count(part)
    rows, low, high = corners(part)
    vertex = pmx.create.selection_by_ids('geometry', 'geometry', 'vertices', [rows[0]["node_id"]])
    #
    refused(lambda: pmx.create.split_tool_plane_by_three_points(), "points")
    refused(lambda: pmx.create.split_tool_plane_by_three_points([[0, 0, 0], [1, 0, 0], [0, 1, 0]], vertex),
            "either")
    refused(lambda: pmx.create.split_tool_plane_by_three_points([[0, 0, 0], [1, 0, 0]]), "3 points")
    refused(lambda: pmx.create.split_tool_plane_by_point_and_normal([0, 0, 0]), "normal")
    refused(lambda: pmx.create.split_tool_plane_by_point_and_normal(normal=[0, 0, 0]), "zero")
    refused(lambda: pmx.create.split_tool_extrude_edges(vertex), "direction")
    refused(lambda: pmx.create.split_tool_extrude_edges(vertex, [0, 0, 1], vertex), "either")
    refused(lambda: pmx.create.split_tool_extrude_edges(vertex, [0, 0, 1], length=0), "length")
    # A line is drawn on a surface, so it only splits a shell
    line = pmx.create.split_tool_line_by_two_points([[0, 0, 0], [1, 0, 0]])
    check(line.tool_type == "Line by two points", "the tool must report its type: " + line.tool_type)
    refused(lambda: parts.split_solid(part_region(part), line), "split_shell")
    # A solid split names parts and a shell split names surfaces, as the two dialogs pick
    plane = pmx.create.split_tool_plane_by_point_and_normal(normal=[0, 0, 1])
    surfaces = pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces', {part.id: [1]})
    refused(lambda: parts.split_solid(surfaces, plane), "parts")
    refused(lambda: parts.split_shell(part_region(part), plane), "geometry_surfaces")
    refused(lambda: parts.split_solid(part_region(part), plane, -1), "fuzzy_tolerance")
    # Three points on one line define no plane, which is known only once the tool is applied
    collinear = pmx.create.split_tool_plane_by_three_points([[0, 0, 0], [1, 0, 0], [2, 0, 0]])
    refused(lambda: parts.split_solid(part_region(part), collinear), "one line")
    # A typed point the picked vertex would replace with another one is refused, not dropped
    far = [low[0] - 100, low[1] - 100, low[2] - 100]
    conflict = pmx.create.split_tool_plane_by_point_and_normal(far, [0, 0, 1], vertex)
    refused(lambda: parts.split_solid(part_region(part), conflict), "'point'")
    #
    check(list(parts.get_names()) == names, "a refused split must not change the parts")
    check(face_count(parts[part.name]) == faces, "a refused split must not change the faces")


def test_split_shell(part):
    parts = pmx.model.geometry.parts
    rows, low, high = corners(part)
    before = face_count(part)
    # A line across the top face near its front edge, clear of the holes
    y = low[1] + 2.0
    line = pmx.create.split_tool_line_by_two_points([[low[0] + 5.0, y, high[2]], [high[0] - 5.0, y, high[2]]])
    top = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces',
                                      {part.id: [top_face_id(part, high)]})
    added = parts.split_shell(top, line)
    check(part.name in added, "the report must name the part: " + str(dict(added)))
    check(added[part.name] > 0, "the top face must be divided: " + str(dict(added)))
    check(part.name in list(parts.get_names()), "a shell split must keep the part name")
    after = face_count(parts[part.name])
    check(after - before == added[part.name],
          "the reported count must be the faces the part gained: " + str(before) + " -> " + str(after) +
          " against " + str(dict(added)))


def test_split_solid_by_vertices(part):
    parts = pmx.model.geometry.parts
    rows, low, high = corners(part)
    # A diagonal plane through three corners of the box cuts it in two
    ids = [corner(rows, [low[0], low[1], low[2]]), corner(rows, [low[0], high[1], low[2]]),
           corner(rows, [high[0], low[1], high[2]])]
    vertices = pmx.create.selection_by_ids('geometry', 'geometry', 'vertices', ids)
    tool = pmx.create.split_tool_plane_by_three_points(splitter=vertices)
    check(tool.tool_type == "Plane by three points", "the tool must report its type: " + tool.tool_type)
    # The part is gone once it is cut, so its name is read before
    name = part.name
    before = list(parts.get_names())
    added = parts.split_solid(part_region(part), tool)
    check(added[name] > 0, "the diagonal plane must cut the box: " + str(dict(added)))
    created = [item for item in parts.get_names() if item not in before]
    check(name not in list(parts.get_names()), "a part cut in two must be replaced")
    compounds = [item for item in created if parts[item].part_type == "Compound"]
    check(len(compounds) == 1, "the pieces must be gathered into one compound part: " + str(created))
    test_rename_split_sub_part(parts[compounds[0]])


def test_rename_split_sub_part(compound):
    parts = pmx.model.geometry.parts
    sub_parts = list(compound.split_sub_part_names)
    check(len(sub_parts) == 1, "a single solid cut in two is one sub-part: " + str(sub_parts))
    other = [name for name in parts.get_names() if name != compound.name][0]
    refused(lambda: parts.rename_split_sub_part(compound.name, 'Missing_part', 'Web'), "no sub-part")
    refused(lambda: parts.rename_split_sub_part(compound.name, sub_parts[0], 'Web part'), "new_name")
    refused(lambda: parts.rename_split_sub_part(compound.name, sub_parts[0], other.upper()), "already used")
    refused(lambda: parts.rename_split_sub_part(other, sub_parts[0], 'Web'), "not a compound part")
    check(list(compound.split_sub_part_names) == sub_parts, "a refused rename must change nothing")
    #
    parts.rename_split_sub_part(compound.name, sub_parts[0], 'Web')
    check(list(compound.split_sub_part_names) == ['Web'],
          "the sub-part must carry its new name: " + str(list(compound.split_sub_part_names)))
    check(sub_parts[0] not in list(parts.get_names()), "the old name must be gone")


def test_split_solid_by_point_and_normal(part):
    parts = pmx.model.geometry.parts
    # A point that is not given is put at the center of the region, as the dialog puts it
    tool = pmx.create.split_tool_plane_by_point_and_normal(normal=[0, 0, 1])
    name = part.name
    before = list(parts.get_names())
    added = parts.split_solid(part_region(part), tool)
    check(added[name] > 0, "the plane through the center must cut the box: " + str(dict(added)))
    check(name not in list(parts.get_names()), "a part cut in two must be replaced")
    check(len([item for item in parts.get_names() if item not in before]) > 0, "the pieces must be added")


def import_box():
    before = list(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "blind_holes.brep"))
    return [part for part in pmx.model.geometry.parts if part.name not in before][0]


def main():
    part = pmx.model.geometry.parts.first()
    test_refusals(part)
    test_split_shell(part)
    test_split_solid_by_point_and_normal(pmx.model.geometry.parts[part.name])
    test_split_solid_by_vertices(import_box())
    print("TEST PASSED")


main()
