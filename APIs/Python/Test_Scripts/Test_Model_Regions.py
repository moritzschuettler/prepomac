#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the region collections: node sets, element sets, surfaces, reference points and coordinate
#systems. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Regions.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def clean(collection):
    names = [n for n in collection.get_names() if n.startswith(PREFIX)]
    if names:
        collection.remove(names)


def check_collection_verbs(collection, name, label):
    check(collection.contains(name), label + " was not added")
    check(collection[name].name == name, label + " indexing by name failed")
    check(collection.get(name).name == name, label + " get failed")
    check(name in list(collection.get_names()), label + " is missing from get_names")
    check(len([i for i in collection]) == len(collection), label + " iteration failed")
    collection.rename(name, name + "_R")
    check(collection.contains(name + "_R"), label + " was not renamed")
    collection.rename(name + "_R", name)
    check_raises(lambda: collection["NoSuchItem"], label + " must reject an unknown name")


def test_node_sets():
    clean(pmx.model.node_sets)
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4])
    node_set = pmx.model.node_sets.add(PREFIX + "NS", selection)
    check(node_set.count == 4, "4 nodes expected")
    # The center of gravity is a plain [x, y, z] list: the mean of the node coordinates
    center = node_set.get_center_of_gravity()
    check(type(center) is list and len(center) == 3, "the center of gravity must be a list of 3: " + repr(center))
    data = pmx.model.nodes.get_data()
    rows = dict(zip(list(data.ids), [list(row) for row in data.coordinates]))
    for axis in range(3):
        mean = sum(rows[node_id][axis] for node_id in (1, 2, 3, 4)) / 4.0
        check(abs(center[axis] - mean) < 1e-9, "the center of gravity " + str(center) + " is not the node mean")
    check_collection_verbs(pmx.model.node_sets, PREFIX + "NS", "node set")
    return selection


def test_element_sets():
    clean(pmx.model.element_sets)
    selection = pmx.create.selection_by_ids('femodel', 'elements', 'elements', [1])
    element_set = pmx.model.element_sets.add(PREFIX + "ES", selection)
    check(element_set.count == 1, "1 element expected, found " + str(element_set.count))
    check(list(element_set.get_element_ids()) == [1], "unexpected element IDs")
    check_collection_verbs(pmx.model.element_sets, PREFIX + "ES", "element set")
    # A name is unique across all the mesh entities
    check_raises(lambda: pmx.model.element_sets.add(PREFIX + "NS", selection),
                 "a name used by a node set must be rejected")


def test_surfaces():
    clean(pmx.model.surfaces)
    selection = pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [1]})
    surface = pmx.model.surfaces.add(PREFIX + "SU", selection)
    check(surface.created_from == "Selection", "unexpected created_from " + surface.created_from)
    check(surface.area > 0, "the surface area must be positive")
    check(len(surface.get_face_ids()) > 0, "the surface has no face IDs")
    check_collection_verbs(pmx.model.surfaces, PREFIX + "SU", "surface")
    # A surface can also be created from an existing node set
    pmx.model.surfaces.add_from_node_set(PREFIX + "SU_NS", (PREFIX + "NS").lower())
    check(pmx.model.surfaces[PREFIX + "SU_NS"].created_from == "NodeSet",
          "the surface was not created from the node set")
    # The node set is stored in the model's own spelling, which the controller compares exactly
    check(pmx.model.surfaces[PREFIX + "SU_NS"].node_set_name == PREFIX + "NS",
          "the node set name was stored in the caller's spelling")
    check_raises(lambda: pmx.model.surfaces.add_from_node_set(PREFIX + "Bad", "NoSuchNodeSet"),
                 "an unknown node set must be rejected")


def test_reference_points():
    clean(pmx.model.reference_points)
    point = pmx.model.reference_points.add(PREFIX + "RP", [1.0, 2.0, 3.0])
    check(point.get_coordinates() == [1.0, 2.0, 3.0], "unexpected reference point coordinates")
    check(type(point.get_coordinates()) is list, "the coordinates must be a list: " + type(point.get_coordinates()).__name__)
    check_collection_verbs(pmx.model.reference_points, PREFIX + "RP", "reference point")
    pmx.model.reference_points.hide(PREFIX + "RP")
    check(not pmx.model.reference_points[PREFIX + "RP"].visible, "the reference point was not hidden")
    pmx.model.reference_points.show(PREFIX + "RP")
    check(pmx.model.reference_points[PREFIX + "RP"].visible, "the reference point was not shown")
    check_raises(lambda: pmx.model.reference_points.add(PREFIX + "Bad", [1, 2]),
                 "a 2 value coordinate list must be rejected")
    # The name visibility and the symbol colour are part of the stored definition
    check(point.name_visible, "a new reference point must have a visible name")
    point = pmx.model.reference_points.add(PREFIX + "RP_D", [0, 0, 1], name_visible=False,
                                           color=[10, 20, 30])
    check(not point.name_visible, "the name visibility was not stored")
    check(list(point.color)[:3] == [10, 20, 30],
          "the colour was not stored, it is " + str(list(point.color)))
    # A replacement keeps what it is not given and changes what it is
    point = pmx.model.reference_points.replace(PREFIX + "RP_D", coordinates=[0, 0, 2])
    check(not point.name_visible, "the replacement dropped the name visibility")
    check(list(point.color)[:3] == [10, 20, 30], "the replacement dropped the colour")
    point = pmx.model.reference_points.replace(PREFIX + "RP_D", name_visible=True)
    check(point.name_visible, "the replacement did not change the name visibility")
    check_raises(lambda: pmx.model.reference_points.add(PREFIX + "Bad", [0, 0, 0], color=[1, 2]),
                 "a 2 channel colour must be rejected")


def test_coordinate_systems():
    clean(pmx.model.coordinate_systems)
    system = pmx.model.coordinate_systems.add(PREFIX + "CS", [0, 0, 0], [1, 0, 0], [0, 1, 0])
    check(system.coordinate_system_type == "Rectangular", "unexpected default type")
    check(system.get_origin() == [0.0, 0.0, 0.0], "unexpected origin")
    check(system.get_point_on_x() == [1.0, 0.0, 0.0], "unexpected point on X")
    check(system.get_point_in_xy() == [0.0, 1.0, 0.0], "unexpected point in XY")
    for point in (system.get_origin(), system.get_point_on_x(), system.get_point_in_xy()):
        check(type(point) is list, "a coordinate system point must be a list: " + type(point).__name__)
    check_collection_verbs(pmx.model.coordinate_systems, PREFIX + "CS", "coordinate system")
    #
    pmx.model.coordinate_systems.add(PREFIX + "CS_C", [0, 0, 0], [1, 0, 0], [0, 1, 0],
                                     coordinate_system_type="Cylindrical")
    check(pmx.model.coordinate_systems[PREFIX + "CS_C"].coordinate_system_type == "Cylindrical",
          "the cylindrical type was not set")
    check_raises(lambda: pmx.model.coordinate_systems.add(PREFIX + "Bad", [0, 0, 0], [1, 0, 0], [0, 1, 0],
                                                          coordinate_system_type="NotAType"),
                 "an unknown coordinate system type must be rejected")
    # The name visibility is part of the stored definition
    check(system.name_visible, "a new coordinate system must have a visible name")
    system = pmx.model.coordinate_systems.add(PREFIX + "CS_N", [0, 0, 0], [1, 0, 0], [0, 1, 0],
                                               name_visible=False)
    check(not system.name_visible, "the name visibility was not stored")
    system = pmx.model.coordinate_systems.replace(PREFIX + "CS_N", origin=[0, 0, 1])
    check(not system.name_visible, "the replacement dropped the name visibility")
    system = pmx.model.coordinate_systems.replace(PREFIX + "CS_N", name_visible=True)
    check(system.name_visible, "the replacement did not change the name visibility")
    # Coordinate system names are checked against the other coordinate systems only
    message = raised_message(lambda: pmx.model.coordinate_systems.add(PREFIX + "CS_C", [0, 0, 0], [1, 0, 0],
                                                                      [0, 1, 0]))
    check(message is not None and "already exists" in message,
          "a second coordinate system of the same name must be refused: " + str(message))
    check(pmx.model.node_sets.contains(PREFIX + "NS"), "the node set the next check shares a name with is missing")
    pmx.model.coordinate_systems.add(PREFIX + "NS", [0, 0, 0], [1, 0, 0], [0, 1, 0])
    check(pmx.model.coordinate_systems.contains(PREFIX + "NS"),
          "a coordinate system must be allowed to share its name with a node set")


def test_derived_reference_points():
    clean(pmx.model.reference_points)
    # The mesh computes the coordinates from the selected points
    one = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])
    two = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 7])
    point = pmx.model.reference_points.add_from_selection(PREFIX + "RP_Mid", "BetweenTwoPoints", two)
    check(point.created_from == "BetweenTwoPoints", "unexpected created_from " + point.created_from)
    check(point.get_coordinates() == [0.5, 0.5, 0.5],
          "the midpoint was not computed, got " + str(point.get_coordinates()))
    # A mode needs exactly as many points as it consumes
    check_raises(lambda: pmx.model.reference_points.add_from_selection(PREFIX + "Bad", "OnPoint", two),
                 "OnPoint with two points must be rejected")
    check_raises(lambda: pmx.model.reference_points.add_from_selection(PREFIX + "Bad", "CenterOfGravity", one),
                 "a region mode with a selection must be rejected")
    # The region modes take a named region instead
    point = pmx.model.reference_points.add_from_region(PREFIX + "RP_Cog", "CenterOfGravity",
                                                       "NodeSetName", PREFIX + "NS")
    check(len(point.get_coordinates()) == 3, "the center of gravity was not computed")
    check_raises(lambda: pmx.model.reference_points.add_from_region(PREFIX + "Bad", "OnPoint",
                                                                    "NodeSetName", PREFIX + "NS"),
                 "a selection mode with a region must be rejected")
    # A replace that switches the construction kind drops what belonged to the old one, so a later
    # replace that keeps a value cannot bring back a selection or a region the point no longer uses
    points = pmx.model.reference_points
    name = PREFIX + "RP_Kind"
    points.add_from_selection(name, "OnPoint", one)
    point = points.replace(name, coordinates=[0.25, 0.25, 0.25])
    check(point.created_from == "Coordinates", "the coordinates did not switch the construction kind")
    message = raised_message(lambda: points.replace_from_selection(name, created_from="OnPoint"))
    check(message is not None and "'selection'" in message,
          "a selection replaced by coordinates must not come back: " + str(message))
    check(points[name].created_from == "Coordinates", "the refused replace changed the construction kind")
    check(points[name].get_coordinates() == [0.25, 0.25, 0.25], "the refused replace moved the point")
    point = points.replace_from_region(name, created_from="CenterOfGravity", region_type="NodeSetName",
                                       region_name=PREFIX + "NS")
    check(point.get_construction()["region_name"] == PREFIX + "NS", "the region was not set")
    message = raised_message(lambda: points.replace_from_selection(name, created_from="OnPoint"))
    check(message is not None and "'selection'" in message,
          "a selection replaced by a region must not come back: " + str(message))
    point = points.replace(name, coordinates=[0.5, 0.5, 0.5])
    check(point.get_construction()["region_name"] is None, "the coordinates kept the region of the old kind")
    message = raised_message(lambda: points.replace_from_region(name, created_from="CenterOfGravity"))
    check(message is not None and "'region_type'" in message,
          "a region replaced by coordinates must not come back: " + str(message))
    message = raised_message(lambda: points.replace_from_region(name, created_from="CenterOfGravity",
                                                                region_type="NodeSetName"))
    check(message is not None and "'region_name'" in message,
          "a region name replaced by coordinates must not come back: " + str(message))
    check(points[name].created_from == "Coordinates", "the refused replace changed the construction kind")


def test_derived_coordinate_systems():
    clean(pmx.model.coordinate_systems)
    system = pmx.model.coordinate_systems.add_from_selections(
        PREFIX + "CS_Sel",
        origin_created_from="OnPoint",
        origin_selection=pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1]),
        point_on_x_created_from="OnPoint",
        point_on_x_selection=pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [2]))
    check(len(system.get_origin()) == 3, "the origin was not computed")
    check(len(system.get_point_on_x()) == 3, "the point on X was not computed")
    # A mode and its selection are given together
    check_raises(lambda: pmx.model.coordinate_systems.add_from_selections(PREFIX + "Bad",
                                                                          origin_created_from="OnPoint"),
                 "a mode without its selection must be rejected")


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_node_sets()
    test_element_sets()
    test_surfaces()
    test_reference_points()
    test_coordinate_systems()
    test_derived_reference_points()
    test_derived_coordinate_systems()
    # Clean up in reverse order, so that a surface never outlives the node set it uses
    clean(pmx.model.coordinate_systems)
    clean(pmx.model.reference_points)
    clean(pmx.model.surfaces)
    clean(pmx.model.element_sets)
    clean(pmx.model.node_sets)
    print("TEST PASSED")


main()
