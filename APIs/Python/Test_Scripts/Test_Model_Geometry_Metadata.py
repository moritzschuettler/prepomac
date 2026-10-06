#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the CAD geometry metadata and the CAD part measurements. It needs a CAD import: an imported
#mesh and an imported STL carry no metadata at all, so the other part tests never reach a property.
#The L bracket has nine faces, eight of them planes and one a cylinder, so the property set really
#does vary from face to face and get_property has to answer for a face that does not carry the name.
#
#  PrePoMax.com -f L_bracket_r.step -u MM_TON_S_C --script Test_Model_Geometry_Metadata.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def get_part():
    parts = pmx.model.geometry.parts
    check(len(parts) == 1, "the CAD import must produce exactly one geometry part")
    part = parts.first()
    check(part.is_cad_part, "the imported part must be a CAD part")
    return part


def test_measurements(part):
    check(part.volume > 0, "a CAD solid must have a volume")
    check(part.surface_area > 0, "a CAD solid must have a surface area")
    check(len(part.center_of_mass) == 3, "the center of mass must have 3 values")
    # A volume inertia computed with a density of 1, so it is symmetric and its trace is positive
    inertia = part.get_inertia_at_center_of_mass()
    check(len(inertia) == 3 and len(inertia[0]) == 3, "the inertia matrix must be 3 by 3")
    check(inertia[0][0] + inertia[1][1] + inertia[2][2] > 0, "the inertia trace must be positive")
    for row in range(3):
        for column in range(3):
            check(abs(inertia[row][column] - inertia[column][row]) < 1e-6 * (1 + abs(inertia[row][column])),
                  "the inertia matrix must be symmetric")
    check(len(part.get_inertia_at_origin()) == 3, "the origin inertia matrix must be 3 by 3")
    # Fixed size values are plain lists: the center as [x, y, z], each matrix as three rows
    check(type(part.center_of_mass) is list and all(type(value) is float for value in part.center_of_mass),
          "the center of mass must be a list[float]: " + type(part.center_of_mass).__name__)
    for matrix in (inertia, part.get_inertia_at_origin()):
        check(type(matrix) is list and all(type(row) is list and len(row) == 3 for row in matrix),
              "an inertia matrix must be a list of three-value lists: " + type(matrix).__name__)


def test_node_coordinates(part):
    ids = part.get_node_ids()
    coordinates = part.get_node_coordinates()
    check(len(ids) == part.node_count, "the node ID count does not match")
    check(len(coordinates) == part.node_count, "the node coordinate count does not match")
    check(len(coordinates[0]) == 3, "a node coordinate must have 3 values")
    # The rows are the part nodes, so they lie inside the part bounding box
    box = part.get_bounding_box()
    check(type(box) is list and len(box) == 6 and all(type(value) is float for value in box),
          "the bounding box must be a list[float] of 6 values: " + str(box))
    for axis in range(3):
        check(box[2 * axis] < box[2 * axis + 1], "the bounding box " + str(box) + " is empty along an axis")
    center = part.center_of_mass
    for axis in range(3):
        check(box[2 * axis] <= center[axis] <= box[2 * axis + 1],
              "the center of mass " + str(center) + " lies outside the bounding box " + str(box))
    for row in coordinates:
        for axis in range(3):
            check(box[2 * axis] - 1e-6 <= row[axis] <= box[2 * axis + 1] + 1e-6,
                  "a node coordinate lies outside the part bounding box")
    # The vertices are part nodes, so every vertex coordinate is one of the rows
    vertices = part.get_vertex_data()
    rows = set(tuple(row) for row in coordinates)
    for i in range(vertices.count):
        check(tuple(vertices.coordinates[i]) in rows, "a vertex coordinate is not a part node coordinate")


def test_metadata(part):
    metadata = part.get_geometry_metadata()
    check(metadata.available, "a CAD import must carry geometry metadata")
    kinds = list(metadata.entity_kinds)
    for kind in ("solid", "shell", "face", "edge"):
        check(kind in kinds, "the metadata does not report " + kind + " entities")
    faces = metadata.get_entities("face")
    check(faces.count == part.get_face_data().count, "the metadata face count does not match the topology")
    # The property set varies by surface type: only the cylinder carries a radius, and asking the
    # planes for one has to answer rather than raise
    with_radius = 0
    for face in faces:
        radius = face.get_property("radius")
        if radius is None:
            check("radius" not in list(face.property_names), "a face hid a radius it carries")
        else:
            check(radius > 0, "a cylinder radius must be positive")
            check(face.get_property("convexity") in ("Convex", "Concave"),
                  "a solid cylinder face must state which radial side contains material")
            with_radius += 1
    check(with_radius == 1, "exactly one face of the L bracket is a cylinder, found " + str(with_radius))
    # A numeric vector property - an axis, a direction, a bounding box - is a plain list[float]
    vectors = 0
    for face in faces:
        for property_name in list(face.property_names):
            value = face.get_property(property_name)
            if property_name.endswith("_ids") or isinstance(value, (float, str)):
                continue
            if type(value).__name__ == "StringListAPI":
                continue
            check(type(value) is list and all(type(item) is float for item in value),
                  "the vector property " + property_name + " must be a list[float]: " + type(value).__name__)
            vectors += 1
    check(vectors > 0, "no face carries a numeric vector property")
    # Every circular edge states its angular span directly; closed is consistent with a full turn.
    topology_edges = part.get_edge_data()
    metadata_edges = metadata.get_entities("edge")
    circle_count = 0
    for i in range(topology_edges.count):
        if topology_edges.types[i] == "Circle":
            edge = metadata_edges.get(int(topology_edges.ids[i]))
            sweep = edge.get_property("sweep_angle_rad")
            closed = int(edge.get_property("closed"))
            check(sweep is not None and 0 < sweep <= 2 * 3.141592653589793 + 1e-12,
                  "a circular edge must carry a valid swept angle")
            check(closed == int(abs(sweep - 2 * 3.141592653589793) < 1e-10),
                  "a circular edge closure flag must agree with its swept angle")
            circle_count += 1
    check(circle_count > 0, "the CAD test part must contain at least one circular edge")
    # A name no entity carries, with and without a default
    face = faces[1]
    check(face.get_property("PyTest_no_such_property") is None,
          "a missing property must return None")
    check(face.get_property("PyTest_no_such_property", -1.0) == -1.0,
          "a missing property must return the given default")
    check(face.get_property("PyTest_no_such_property", "fallback") == "fallback",
          "a missing property must return a text default unchanged")
    # A canonical measurement belongs to the part, face and edge data and is refused here
    check("area" not in list(face.property_names), "a canonical measurement must not be listed")
    check_raises(lambda: face.get_property("area"), "a canonical measurement must be refused")
    check_raises(lambda: face.get_property("area", 0.0),
                 "a canonical measurement must be refused even with a default")
    check_raises(lambda: face.get_property("   "), "an empty property name must be refused")


def main():
    check(pmx.model.is_open(), "a model must be open")
    part = get_part()
    test_measurements(part)
    test_node_coordinates(part)
    test_metadata(part)
    print("TEST PASSED")


main()
