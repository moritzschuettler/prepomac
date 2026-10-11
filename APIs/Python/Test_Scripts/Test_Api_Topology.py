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


def check_raises(function, message, unknown_keyword=None):
    # Only the API's own refusal passes: a PythonAPIException, or for a call that deliberately passes the
    # keyword unknown_keyword, the TypeError of the keyword guard. Anything else, and a message carrying a
    # .NET stack trace, is a fault of the application that happened to stop the call
    try:
        function()
    except Exception as ex:
        kind, text = type(ex).__name__, str(ex)
        if unknown_keyword is None:
            expected, refusal = kind == "PythonAPIException", "a PythonAPIException"
        else:
            expected = kind == "TypeError" and "unexpected keyword argument '%s'" % unknown_keyword in text
            refusal = "the keyword guard's TypeError for '%s'" % unknown_keyword
        if not expected or "   at " in text:
            raise Exception("TEST FAILED: " + message + " - the refusal must be " + refusal +
                            " without a .NET stack trace, not " + kind + ": " + text[0:500])
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
    # A list of IDs is an IntList wherever it comes from, the data columns of the topology included
    for ids in (faces.ids, faces.geometry_ids):
        check(type(ids).__name__ == "IntListAPI", "a face ID list must be an IntList: " + type(ids).__name__)
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
    for ids in (edges.ids, edges.geometry_ids, vertices.node_ids, vertices.geometry_ids):
        check(type(ids).__name__ == "IntListAPI", "an edge or vertex ID list must be an IntList: " +
              type(ids).__name__)
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
    check(type(preview.ids).__name__ == "IntListAPI" and len(preview.ids) == preview.count,
          "the preview IDs must be an IntList of count IDs: " + type(preview.ids).__name__)
    check(preview.count == 4, "the top face of a hexahedron covers 4 nodes, got " + str(preview.count))
    check(list(preview.part_names) == [part.name],
          "the preview must name the part: " + str(list(preview.part_names)))
    # A fixed size value is a plain list, so it compares and prints like one
    box = preview.bounding_box
    check(type(box) is list and type(preview.centroid) is list,
          "the preview bounding box and centroid must be lists: " + type(box).__name__)
    check(len(box) == 6, "the preview bounding box must have 6 values: " + str(box))
    check(abs(box[4] - 1.0) < 1e-6 and abs(box[5] - 1.0) < 1e-6,
          "the top face nodes must all lie at z = 1: " + str(box))
    centroid = preview.centroid
    check(abs(centroid[2] - 1.0) < 1e-6, "the preview centroid must lie at z = 1: " + str(centroid))
    check(all(box[2 * axis] - 1e-6 <= centroid[axis] <= box[2 * axis + 1] + 1e-6 for axis in range(3)),
          "the preview centroid must lie inside its bounding box: " + str(centroid))
    # A surface target resolves to element faces, and the preview reduces them to the nodes of the face
    faces = pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {part.id: [top_face_id]}).preview()
    check(faces.target == "surfaces", "the face preview must report the target: " + faces.target)
    check(faces.count == 1, "the top face of one hexahedron is one element face, got " + str(faces.count))
    check(list(faces.part_names) == [part.name],
          "the face preview must name the part: " + str(list(faces.part_names)))
    box = faces.bounding_box
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
    check(preview.bounding_box == [] and preview.centroid == [],
          "an empty preview must have an empty bounding box and centroid: " + str(preview.bounding_box))


def test_view():
    check(pmx.view.current == "femodel", "the femodel view must be current: " + pmx.view.current)
    names = list(pmx.view.get_orientation_names())
    check("Isometric" in names, "the orientations must include Isometric: " + str(names))
    camera = pmx.view.get_parameters()
    check(type(camera) is list and all(type(value) is float for value in camera),
          "the camera must be a list[float]: " + type(camera).__name__)
    check(len(camera) == 10, "the camera must be 10 values: " + str(camera))
    for orientation in names:
        pmx.view.set_orientation(orientation)
    pmx.view.zoom_to_fit()
    pmx.view.set_parameters(camera)
    restored = pmx.view.get_parameters()
    for i in range(10):
        check(abs(restored[i] - camera[i]) < 1e-6,
              "the camera must be restored: " + str(restored) + " against " + str(camera))
    # A view change records nothing, because it changes nothing the model keeps
    check_raises(lambda: pmx.view.set_orientation("Sideways"), "an unknown orientation must be rejected")
    check_raises(lambda: pmx.view.set_result_field("DISP"), "a plot without an open result must be rejected")
    check_raises(lambda: pmx.view.set_parameters([1, 2, 3]), "a short camera must be rejected")
    for value in [float("nan"), float("-inf")]:
        try:
            pmx.view.set_parameters(camera[:9] + [value])
            raise Exception("TEST FAILED: a camera with " + str(value) + " must be refused")
        except Exception as ex:
            check("'parameters'" in str(ex), "a non-finite camera must be refused naming the argument: " + str(ex))
    restored = pmx.view.get_parameters()
    check(all(abs(restored[i] - camera[i]) < 1e-6 for i in range(10)), "a refused camera changed the view")


def centre_offset(camera, target):
    """How far the target is from the centre of the picture, across the viewing direction.

    The application keeps the camera at the depth of the model after every camera change, which moves
    the focal point along the viewing direction and changes nothing on screen, so a point looked at is
    checked in the picture rather than against the focal point itself.
    """
    direction = [camera[i] - camera[3 + i] for i in range(3)]
    length = sum(d * d for d in direction) ** 0.5
    direction = [d / length for d in direction]
    offset = [camera[3 + i] - target[i] for i in range(3)]
    along = sum(offset[i] * direction[i] for i in range(3))
    return sum((offset[i] - along * direction[i]) ** 2 for i in range(3)) ** 0.5


def test_view_without_a_result():
    # Everything about a result plot is refused while there is none; reading the state is not
    check_raises(lambda: setattr(pmx.view, "deformation_type", "TrueScale"), "the deformation type needs a result")
    check_raises(lambda: setattr(pmx.view, "deformation_factor", 2.0), "the deformation factor needs a result")
    check_raises(lambda: setattr(pmx.view, "undeformed_model", "SolidBody"), "the undeformed model needs a result")
    check_raises(lambda: pmx.view.set_legend_range(0.0, 1.0), "the legend range needs a result")
    check_raises(lambda: pmx.view.reset_legend_range(), "resetting the legend range needs a result")
    check_raises(lambda: pmx.view.annotations.show_min_max(True, True), "the min/max labels need a result")
    check(pmx.view.deformation_type in ["Undeformed", "TrueScale", "Automatic", "Automatic025", "Automatic05",
                                        "Automatic2", "Automatic5", "UserDefined"],
          "the deformation type must be readable: " + pmx.view.deformation_type)
    check(pmx.view.undeformed_model in ["None", "WireframeBody", "SolidBody"],
          "the undeformed model must be readable: " + pmx.view.undeformed_model)
    shown = pmx.view.annotations.get_min_max()
    check(type(shown) is list and len(shown) == 2 and all(type(value) is bool for value in shown),
          "the min/max setting must be a list of two bools: " + repr(shown))
    # The camera needs no result
    camera = pmx.view.get_parameters()
    pmx.view.set_orientation("Top")
    before = pmx.view.get_parameters()
    pmx.view.rotate(45.0)
    after = pmx.view.get_parameters()
    check(any(abs(after[i] - before[i]) > 1e-6 for i in range(3)), "a rotation must move the camera: " + str(after))
    pmx.view.look_at([1.0, 1.0, 1.0], 0.5)
    framed = pmx.view.get_parameters()
    check(centre_offset(framed, [1.0, 1.0, 1.0]) < 1e-6, "look_at must put the point in the centre: " + str(framed))
    check(abs(framed[9] - 0.5) < 1e-6, "look_at must set the parallel scale: " + str(framed))
    pmx.view.set_parameters(camera)
    # The femodel view keeps annotations of its own, on the mesh it displays
    annotations = pmx.view.annotations
    check(len(list(annotations.get_names())) == 0, "the femodel view starts without annotations")
    annotations.add_text("PyTest_Label", "The cube", [0.5, 0.5, 1.0])
    node_name = annotations.add_node(1)
    element_name = annotations.add_element(1)
    check(sorted(annotations.get_names()) == sorted(["PyTest_Label", node_name, element_name]),
          "the femodel annotations must be listed: " + str(list(annotations.get_names())))
    check_raises(lambda: annotations.add_node(99), "an unknown node must be rejected")
    check_raises(lambda: annotations.add_text("Measure_Annotation", "mine", [0.0, 0.0, 0.0]),
                 "a reserved name must be rejected")
    annotations.remove(["PyTest_Label", node_name, element_name])
    check(len(list(annotations.get_names())) == 0, "the removed annotations must be gone")
    # A generated name is free regardless of case, so it never twins a name the script chose
    annotations.add_text(node_name.swapcase(), "Taken", [0.0, 0.0, 0.0])
    twin_free_name = annotations.add_node(1)
    check(twin_free_name.lower() != node_name.lower(), "the generated name twins a name of another case: " +
          twin_free_name)
    annotations.remove([node_name.swapcase(), twin_free_name])
    check(len(list(annotations.get_names())) == 0, "the removed annotations must be gone")


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
    test_view_without_a_result()
    clean()
    print("TEST PASSED")


main()
