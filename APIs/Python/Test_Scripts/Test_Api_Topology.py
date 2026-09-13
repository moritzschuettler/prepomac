#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the part topology query, the selection preview and the view API - the three things a caller
#that cannot see the viewport needs. The cube is a single hexahedron, so its faces are known: six of
#them, each of unit area, each normal along an axis, and the top one is the face whose normal is
#[0, 0, 1]. Finding that face by its reported normal, selecting it by the reported ID and having the
#preview confirm the four nodes it covers is the whole point of the query.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Api_Topology.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_Topo_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def clean():
    for name in list(pmx.model.node_sets.get_names()):
        if name.startswith(PREFIX):
            pmx.model.node_sets.remove(name)


def test_face_data(part):
    faces = part.get_face_data()
    check(faces.count == 6, "the cube must have 6 faces, got " + str(faces.count))
    check(len(list(faces.ids)) == 6, "the face IDs must match the count")
    check(sorted(int(i) for i in faces.ids) == [1, 2, 3, 4, 5, 6],
          "the face IDs must be 1-based and contiguous: " + str(list(faces.ids)))
    check(len(list(faces.types)) == 6, "every face must carry a type")
    # The geometry ID carries the item, the type and the part, so it must decode to the part
    for geometry_id in faces.geometry_ids:
        check(int(geometry_id) % 10000 == part.id,
              "a face geometry ID must name the part: " + str(geometry_id))
    # Every area is positive and the six of them sum to the cube surface
    areas = [float(a) for a in faces.areas]
    check(min(areas) > 0, "every face must have a positive area: " + str(areas))
    check(abs(sum(areas) - 6.0) < 1e-6, "the six unit faces must sum to 6: " + str(sum(areas)))
    # Every normal is a unit vector along an axis, and each axis direction appears once
    directions = []
    for i in range(faces.count):
        normal = [round(float(v), 6) for v in list(faces.normals[i])]
        length = sum(v * v for v in normal) ** 0.5
        check(abs(length - 1.0) < 1e-6, "a face normal must be a unit vector: " + str(normal))
        directions.append(tuple(normal))
    for expected in [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)]:
        check(tuple(float(v) for v in expected) in directions,
              "the cube must have a face pointing " + str(expected) + ": " + str(directions))
    return faces


def find_top_face(faces):
    """The face an agent is looking for, found the way it would find it."""
    best, best_z = None, -2.0
    for i in range(faces.count):
        z = float(list(faces.normals[i])[2])
        if z > best_z:
            best, best_z = i, z
    check(best is not None, "no face was found")
    centroid = [round(float(v), 6) for v in list(faces.centroids[best])]
    check(abs(centroid[2] - 1.0) < 1e-6,
          "the centroid of the top face must lie at z = 1: " + str(centroid))
    return int(faces.ids[best])


def test_edge_and_vertex_data(part):
    edges = part.get_edge_data()
    check(edges.count == 12, "the cube must have 12 edges, got " + str(edges.count))
    lengths = [float(v) for v in edges.lengths]
    check(abs(sum(lengths) - 12.0) < 1e-6, "the twelve unit edges must sum to 12: " + str(sum(lengths)))
    check(sorted(int(i) for i in edges.ids) == list(range(1, 13)),
          "the edge IDs must be 1-based and contiguous: " + str(list(edges.ids)))
    #
    vertices = part.get_vertex_data()
    check(vertices.count == 8, "the cube must have 8 vertices, got " + str(vertices.count))
    node_ids = sorted(int(i) for i in vertices.node_ids)
    check(node_ids == sorted(int(i) for i in part.get_node_ids()),
          "every node of a single hexahedron is a vertex: " + str(node_ids))
    for i in range(vertices.count):
        coordinates = [float(v) for v in list(vertices.coordinates[i])]
        check(len(coordinates) == 3, "a vertex must have 3 coordinates")
        for value in coordinates:
            check(abs(value) < 1e-6 or abs(value - 1.0) < 1e-6,
                  "a cube vertex coordinate must be 0 or 1: " + str(coordinates))


def test_preview(part, top_face_id):
    # The reported face ID is the one a selection takes, grouped by part ID
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {part.id: [top_face_id]})
    preview = selection.preview()
    check(preview.target == "nodes", "the preview must report the target: " + preview.target)
    check(preview.count == 4, "the top face of a hexahedron covers 4 nodes, got " + str(preview.count))
    check(list(preview.part_names) == [part.name],
          "the preview must name the part: " + str(list(preview.part_names)))
    box = [float(v) for v in preview.bounding_box]
    check(len(box) == 6, "the preview bounding box must have 6 values: " + str(box))
    check(abs(box[4] - 1.0) < 1e-6 and abs(box[5] - 1.0) < 1e-6,
          "the top face nodes must all lie at z = 1: " + str(box))
    centroid = [float(v) for v in preview.centroid]
    check(abs(centroid[2] - 1.0) < 1e-6, "the preview centroid must lie at z = 1: " + str(centroid))
    # A surface target resolves to element faces, and the preview reduces them to the nodes of the face
    faces = pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {part.id: [top_face_id]}).preview()
    check(faces.target == "surfaces", "the face preview must report the target: " + faces.target)
    check(faces.count == 1, "the top face of one hexahedron is one element face, got " + str(faces.count))
    check(list(faces.part_names) == [part.name],
          "the face preview must name the part: " + str(list(faces.part_names)))
    box = [float(v) for v in faces.bounding_box]
    check(len(box) == 6 and abs(box[4] - 1.0) < 1e-6 and abs(box[5] - 1.0) < 1e-6,
          "the face preview must lie at z = 1: " + str(box))
    # Nothing was recorded: a preview is a dry run
    before = len(list(pmx.model.node_sets.get_names()))
    selection.preview()
    check(len(list(pmx.model.node_sets.get_names())) == before,
          "a preview must not create anything")
    # And the same selection does build the set it previewed
    pmx.model.node_sets.add(PREFIX + "Top", selection)
    check(len(pmx.model.node_sets[PREFIX + "Top"].get_node_ids()) == 4,
          "the previewed selection must build the same set")


def test_empty_preview():
    # An area pick selects only what lies inside it, so an empty box resolves to nothing and says so
    selection = pmx.create.selection_by_box('femodel', 'nodes', [10, 10, 10], [11, 11, 11], 'Node')
    preview = selection.preview()
    check(preview.count == 0, "a box outside the model must resolve to nothing: " + str(preview.count))
    check(len(list(preview.part_names)) == 0, "an empty preview must name no parts")
    check(len(preview.bounding_box) == 0, "an empty preview must have no bounding box")


def test_view():
    check(pmx.view.current == "femodel", "the femodel view must be current: " + pmx.view.current)
    names = list(pmx.view.get_orientation_names())
    check("Isometric" in names, "the orientations must include Isometric: " + str(names))
    camera = list(pmx.view.get_parameters())
    check(len(camera) == 10, "the camera must be 10 values: " + str(camera))
    for orientation in names:
        pmx.view.set_orientation(orientation)
    pmx.view.zoom_to_fit()
    pmx.view.set_parameters(camera)
    restored = list(pmx.view.get_parameters())
    for i in range(10):
        check(abs(restored[i] - camera[i]) < 1e-6,
              "the camera must be restored: " + str(restored) + " against " + str(camera))
    # A view change records nothing, because it changes nothing the model keeps
    check_raises(lambda: pmx.view.set_orientation("Sideways"), "an unknown orientation must be rejected")
    check_raises(lambda: pmx.view.set_result_field("DISP"), "a plot without an open result must be rejected")
    check_raises(lambda: pmx.view.set_parameters([1, 2, 3]), "a short camera must be rejected")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    part = pmx.model.parts[pmx.model.parts.get_names()[0]]
    faces = test_face_data(part)
    top_face_id = find_top_face(faces)
    test_edge_and_vertex_data(part)
    test_preview(part, top_face_id)
    test_empty_preview()
    test_view()
    clean()
    print("TEST PASSED")


main()
