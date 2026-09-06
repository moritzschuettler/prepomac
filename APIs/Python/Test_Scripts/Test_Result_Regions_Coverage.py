#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the result reference points and result coordinate systems, which a result field
#transformation needs. A result must be open before the script runs.
#
#  PrePoMax.com -f PyTest_Job.frd --script Test_Result_Regions_Coverage.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_Result_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def magnitude(equation):
    # A stored value carries its unit, exactly the way the property grid shows it
    return float(equation.split(" ")[0])


def node_coordinates(node_id):
    # The typed points of a mixed definition are placed around the selected node, so the check does
    # not depend on where the result mesh happens to put that node
    data = pmx.results.current.mesh.nodes.get_data()
    index = list(data.ids).index(node_id)
    row = data.coordinates[index]
    return [float(row[0]), float(row[1]), float(row[2])]


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def clean(collection):
    names = [n for n in collection.get_names() if n.startswith(PREFIX)]
    if names:
        collection.remove(names)


def test_reference_points(points):
    points.add(PREFIX + "Point", [0, 0, 0])
    check(PREFIX + "Point" in list(points.get_names()), "the reference point was not added")
    definition = points.get_definition(PREFIX + "Point")
    check(magnitude(definition[0]) == 0, "the stored coordinate did not read back: " + str(definition))
    points.hide(PREFIX + "Point")
    points.show(PREFIX + "Point")
    points.duplicate(PREFIX + "Point")
    points.replace(PREFIX + "Point", name=PREFIX + "Point_R", coordinates=[1, 2, 3])
    check(PREFIX + "Point_R" in list(points.get_names()), "the reference point was not renamed")
    check(magnitude(points.get_definition(PREFIX + "Point_R")[0]) == 1,
          "the replaced coordinate did not read back")
    check_raises(lambda: points.remove("__missing_result_point__"), "a missing reference point was accepted")
    check_raises(lambda: points.add(PREFIX + "Point_R", [0, 0, 0]), "a duplicate name was accepted")
    clean(points)
    check(len(points) == 0, "the reference points left behind " + str(list(points.get_names())))


def test_reference_point_construction(points):
    # A result replacement re-specifies the whole definition, so a point created from a node must
    # forget that node: otherwise the next result mesh update resolves the node and undoes the change
    one = pmx.create.selection_by_ids("results", "nodes", "nodes", [1])
    two = pmx.create.selection_by_ids("results", "nodes", "nodes", [1, 2])
    points.add_from_selection(PREFIX + "OnPoint", "OnPoint", one)
    check(points.get_created_from(PREFIX + "OnPoint") == "OnPoint",
          "the construction mode was not stored")
    points.replace(PREFIX + "OnPoint", coordinates=[10, 20, 30])
    check(points.get_created_from(PREFIX + "OnPoint") == "Coordinates",
          "the coordinate replacement kept the obsolete construction mode")
    check(magnitude(points.get_definition(PREFIX + "OnPoint")[0]) == 10,
          "the replaced coordinate did not read back")
    #
    points.replace_from_selection(PREFIX + "OnPoint", created_from="BetweenTwoPoints", selection=two)
    check(points.get_created_from(PREFIX + "OnPoint") == "BetweenTwoPoints",
          "the selection replacement did not change the construction mode")
    check_raises(lambda: points.add_from_selection(PREFIX + "Bad", "OnPoint", two),
                 "a mode with the wrong number of selected points was accepted")
    check_raises(lambda: points.add_from_selection(PREFIX + "Bad", "Coordinates", one),
                 "a coordinate mode with a selection was accepted")
    check_raises(lambda: points.replace_from_selection(PREFIX + "OnPoint", created_from="OnPoint"),
                 "a result replacement without its selection was accepted")
    check(PREFIX + "Bad" not in list(points.get_names()), "a refused point changed the result")
    #
    node_sets = list(pmx.results.current.mesh.node_sets.get_names())
    if len(node_sets) > 0:
        points.add_from_region(PREFIX + "Region", "CenterOfGravity", "NodeSetName", node_sets[0])
        check(points.get_created_from(PREFIX + "Region") == "CenterOfGravity",
              "the region construction mode was not stored")
    check_raises(lambda: points.add_from_region(PREFIX + "Bad", "CenterOfGravity", "NodeSetName",
                                                 "__missing_result_node_set__"),
                 "a missing result region was accepted")
    check_raises(lambda: points.add_from_region(PREFIX + "Bad", "OnPoint", "NodeSetName", "any"),
                 "a selection mode with a region was accepted")
    clean(points)


def test_coordinate_system_construction(systems):
    one = pmx.create.selection_by_ids("results", "nodes", "nodes", [1])
    systems.add(PREFIX + "Cyl", [0, 0, 0], [1, 0, 0], [0, 1, 0], coordinate_system_type="Cylindrical")
    check(systems.get_coordinate_system_type(PREFIX + "Cyl") == "Cylindrical",
          "the coordinate system type was not stored")
    systems.replace(PREFIX + "Cyl", origin=[0, 0, 0], point_on_x=[2, 0, 0], point_in_xy=[0, 2, 0])
    check(systems.get_coordinate_system_type(PREFIX + "Cyl") == "Cylindrical",
          "the replacement reset the coordinate system type")
    systems.replace(PREFIX + "Cyl", origin=[0, 0, 0], point_on_x=[2, 0, 0], point_in_xy=[0, 2, 0],
                    coordinate_system_type="Rectangular")
    check(systems.get_coordinate_system_type(PREFIX + "Cyl") == "Rectangular",
          "the coordinate system type was not replaced")
    check_raises(lambda: systems.add(PREFIX + "Bad", [0, 0, 0], [1, 0, 0], [0, 1, 0],
                                      coordinate_system_type="NotAType"),
                 "an unknown coordinate system type was accepted")
    #
    # A point built from a selection must not read back as a typed coordinate point
    node = node_coordinates(1)
    origin = [node[0] - 1.0, node[1], node[2]]
    in_xy = [node[0] - 1.0, node[1] + 1.0, node[2]]
    systems.add_from_selections(PREFIX + "Mixed", origin=origin,
                                 point_on_x_created_from="OnPoint", point_on_x_selection=one,
                                 point_in_xy=in_xy)
    modes = list(systems.get_created_from(PREFIX + "Mixed"))
    check(modes[0] == "Coordinates", "the typed origin did not read back as a coordinate point")
    check(modes[1] == "OnPoint", "the selected X point did not read back as a selected point")
    systems.replace_from_selections(PREFIX + "Mixed", origin=[0, 0, 0], point_on_x=[1, 0, 0],
                                     point_in_xy=[0, 1, 0])
    check(list(systems.get_created_from(PREFIX + "Mixed")) == ["Coordinates"] * 3,
          "the replacement kept the obsolete point construction modes")
    # A typed origin with a selected XY point: the XY point is reevaluated from its own selection,
    # not from the one of the origin, which a typed origin does not have
    systems.add_from_selections(PREFIX + "MixedXY", origin=origin,
                                 point_on_x=[node[0], node[1] + 1.0, node[2]],
                                 point_in_xy_created_from="OnPoint", point_in_xy_selection=one)
    modes = list(systems.get_created_from(PREFIX + "MixedXY"))
    check(modes[0] == "Coordinates" and modes[2] == "OnPoint",
          "a typed origin with a selected XY point was not stored")
    check(abs(magnitude(systems.get_definition(PREFIX + "MixedXY")[6]) - node[0]) < 1E-6,
          "the selected XY point was not resolved on the result mesh")
    # A creation keeps the default of a point it is not told about, the way the dialog does
    systems.add_from_selections(PREFIX + "Default", origin=[0, 0, 0])
    check(list(systems.get_created_from(PREFIX + "Default")) == ["Coordinates"] * 3,
          "an omitted point did not keep its default")
    # A replacement re-specifies the whole definition, so every point is required
    check_raises(lambda: systems.replace_from_selections(PREFIX + "Default", origin=[0, 0, 0],
                                                         point_on_x=[1, 0, 0]),
                 "a result replacement with an undefined point was accepted")
    check_raises(lambda: systems.add_from_selections(PREFIX + "Bad", origin=[0, 0, 0],
                                                      origin_created_from="OnPoint",
                                                      origin_selection=one, point_on_x=[1, 0, 0],
                                                      point_in_xy=[0, 1, 0]),
                 "a point given as both coordinates and a selection was accepted")
    # A selected point is resolved on the result mesh before the axes are checked, so a typed origin
    # that lands on the selected node is refused before anything is recorded
    check_raises(lambda: systems.add_from_selections(PREFIX + "Bad", origin=node,
                                                      point_on_x_created_from="OnPoint",
                                                      point_on_x_selection=one, point_in_xy=in_xy),
                 "a mixed definition whose selected X point sits on the origin was accepted")
    check_raises(lambda: systems.replace_from_selections(PREFIX + "Mixed", origin=node,
                                                          point_on_x_created_from="OnPoint",
                                                          point_on_x_selection=one, point_in_xy=in_xy),
                 "a replacement that collapses the X axis onto a selected point was accepted")
    check(list(systems.get_created_from(PREFIX + "Mixed")) == ["Coordinates"] * 3,
          "a refused mixed replacement changed the definition")
    check(PREFIX + "Bad" not in list(systems.get_names()), "a refused system changed the result")
    clean(systems)


def test_coordinate_systems(systems):
    systems.add(PREFIX + "CS", [0, 0, 0], [1, 0, 0], [0, 1, 0])
    check(PREFIX + "CS" in list(systems.get_names()), "the coordinate system was not added")
    check(len(systems.get_definition(PREFIX + "CS")) == 9, "the definition must carry nine coordinates")
    systems.hide(PREFIX + "CS")
    systems.show(PREFIX + "CS")
    systems.duplicate(PREFIX + "CS")
    systems.replace(PREFIX + "CS", name=PREFIX + "CS_R", origin=[0, 0, 0],
                    point_on_x=[2, 0, 0], point_in_xy=[0, 1, 0])
    check(PREFIX + "CS_R" in list(systems.get_names()), "the coordinate system was not renamed")
    check_raises(lambda: systems.add(PREFIX + "Bad", [0, 0, 0], [0, 0, 0], [0, 1, 0]),
                 "a degenerate coordinate system was accepted")
    check_raises(lambda: systems.remove("__missing_result_cs__"), "a missing coordinate system was accepted")
    clean(systems)
    check(len(systems) == 0, "the coordinate systems left behind " + str(list(systems.get_names())))


def main():
    check(len(pmx.results.get_names().to_array()) > 0, "no result is loaded")
    mesh = pmx.results.current.mesh
    clean(mesh.reference_points)
    clean(mesh.coordinate_systems)
    test_reference_points(mesh.reference_points)
    test_reference_point_construction(mesh.reference_points)
    test_coordinate_systems(mesh.coordinate_systems)
    test_coordinate_system_construction(mesh.coordinate_systems)
    print("TEST PASSED")


main()
