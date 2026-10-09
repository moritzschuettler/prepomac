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


def construction_modes(system):
    construction = system.get_construction()
    return [construction["origin_created_from"], construction["point_on_x_created_from"],
            construction["point_in_xy_created_from"]]


def test_reference_point_items(points):
    # A result reference point is an item like a model one: add returns it, the collection is indexed
    # by name and iteration yields the items rather than their names
    point = points.add(PREFIX + "Item", [1, 2, 3], name_visible=False, color=[10, 20, 30])
    check(point is not None and point.name == PREFIX + "Item", "add did not return the reference point")
    check(points[PREFIX + "Item"].name == PREFIX + "Item", "the name indexer did not return the item")
    check(points.get(PREFIX + "Item").name == PREFIX + "Item", "get did not return the item")
    check(points.last().name == PREFIX + "Item", "last did not return the newest reference point")
    check(points.first().name == list(points.get_names())[0], "first did not return the first item")
    items = list(points)
    check(len(items) == len(points), "iteration and len disagree")
    check(all(hasattr(item, "created_from") for item in items), "iteration did not yield reference points")
    check(PREFIX + "Item" in [item.name for item in items], "iteration missed the added reference point")
    check(point.created_from == "Coordinates", "the construction mode did not read back")
    check(not point.name_visible, "the name visibility did not read back")
    check(list(point.color)[:3] == [10, 20, 30], "the colour did not read back: " + str(list(point.color)))
    check(type(point.color) is list and point.color == [10, 20, 30, 255],
          "the colour must read back as a plain list: " + repr(point.color))
    check(point.visible, "a new result reference point must be visible")
    check(type(point.get_coordinates()) is list and point.get_coordinates() == [1.0, 2.0, 3.0],
          "the coordinates did not read back as a list: " + repr(point.get_coordinates()))
    check(len(point.get_equations()) == 0,
          "plain coordinates were read back as expressions: " + str(dict(point.get_equations())))
    check(point.get_construction() == {"created_from": "Coordinates", "region_type": None, "region_name": None},
          "get_construction did not read back: " + str(point.get_construction()))
    check("ReferencePoint(" in str(point), "the item has no readable string form: " + str(point))
    points.hide(PREFIX + "Item")
    check(not point.visible, "hiding the reference point did not read back")
    points.show(PREFIX + "Item")
    replaced = points.replace(PREFIX + "Item", name=PREFIX + "Item_R", coordinates=[4, 5, 6])
    check(replaced.name == PREFIX + "Item_R", "replace did not return the renamed reference point")
    check(replaced.get_coordinates() == [4.0, 5.0, 6.0], "the replaced coordinates did not read back")
    check_raises(lambda: points["__missing_result_point__"], "a missing reference point was indexed")
    clean(points)


def test_reference_points(points):
    added = points.add(PREFIX + "Point", [0, 0, 0])
    check(added.name == PREFIX + "Point", "add did not return the reference point")
    check(PREFIX + "Point" in list(points.get_names()), "the reference point was not added")
    check(points[PREFIX + "Point"].get_coordinates() == [0.0, 0.0, 0.0], "the stored coordinates did not read back")
    points.hide(PREFIX + "Point")
    points.show(PREFIX + "Point")
    points.duplicate(PREFIX + "Point")
    points.replace(PREFIX + "Point", name=PREFIX + "Point_R", coordinates=["0.5 * 2", 2, 3])
    check(PREFIX + "Point_R" in list(points.get_names()), "the reference point was not renamed")
    # The expressions read back under the argument that sets them, a list with the plain values as numbers,
    # which goes back into replace
    definition = points[PREFIX + "Point_R"].get_equations()
    check(sorted(definition) == ["coordinates"] and type(definition["coordinates"]) is list and
          definition["coordinates"][0].replace(" ", "") == "=0.5*2" and definition["coordinates"][1:] == [2.0, 3.0],
          "the coordinate expressions must read back under 'coordinates': " + str(dict(definition)))
    points.replace(PREFIX + "Point_R", coordinates=definition["coordinates"])
    check(points[PREFIX + "Point_R"].get_equations() == definition, "the expressions did not survive a round trip")
    check(points[PREFIX + "Point_R"].get_coordinates() == [1.0, 2.0, 3.0], "the replaced coordinates did not read back")
    check_raises(lambda: points.remove("__missing_result_point__"), "a missing reference point was accepted")
    check_raises(lambda: points.add(PREFIX + "Point_R", [0, 0, 0]), "a duplicate name was accepted")
    # The name visibility and the symbol colour are part of the stored definition
    check(points[PREFIX + "Point_R"].name_visible, "a new result point must have a visible name")
    points.add(PREFIX + "Point_D", [0, 0, 0], name_visible=False, color=[40, 50, 60])
    check(not points[PREFIX + "Point_D"].name_visible, "the name visibility was not stored")
    check(list(points[PREFIX + "Point_D"].color)[:3] == [40, 50, 60],
          "the colour was not stored, it is " + str(list(points[PREFIX + "Point_D"].color)))
    points.replace(PREFIX + "Point_D", coordinates=[1, 1, 1], name_visible=True, color=[70, 80, 90])
    check(points[PREFIX + "Point_D"].name_visible, "the replacement did not set the name visibility")
    check(list(points[PREFIX + "Point_D"].color)[:3] == [70, 80, 90],
          "the replacement did not set the colour")
    # The rename is the one the results view records, and names are compared without regard to case
    check(points.contains(PREFIX + "point_r") and PREFIX + "POINT_R" in points,
          "contains must compare names without regard to case")
    check(points[PREFIX + "point_r"].name == PREFIX + "Point_R", "the name must read back as it is stored")
    points.hide(PREFIX + "POINT_R")
    check(not points[PREFIX + "Point_R"].visible, "hide did not find the reference point in another case")
    points.show([PREFIX + "point_r"])
    replaced = points.replace(PREFIX + "point_d", coordinates=[1, 1, 1])
    check(replaced.name == PREFIX + "Point_D", "a replace without a name renamed the point to the lookup spelling")
    check(not points.contains("__missing_result_point__"), "a missing reference point was found")
    points.rename(PREFIX + "Point_R", PREFIX + "Point_N")
    check(points.contains(PREFIX + "Point_N") and not points.contains(PREFIX + "Point_R"),
          "the reference point was not renamed: " + str(list(points.get_names())))
    check_raises(lambda: points.rename(PREFIX + "Point_N", PREFIX + "point_d"),
                 "a rename onto an existing name in another case was accepted")
    check_raises(lambda: points.rename(PREFIX + "Point_N", ""), "an empty new name was accepted")
    check_raises(lambda: points.rename("__missing_result_point__", PREFIX + "Point_X"),
                 "renaming a missing reference point was accepted")
    check(not points.contains(PREFIX + "Point_X"), "a refused rename changed the reference points")
    clean(points)
    check(len(points) == 0, "the reference points left behind " + str(list(points.get_names())))


def test_reference_point_construction(points):
    # A result replacement re-specifies the whole definition, so a point created from a node must
    # forget that node: otherwise the next result mesh update resolves the node and undoes the change
    one = pmx.create.selection_by_ids("results", "nodes", "nodes", [1])
    two = pmx.create.selection_by_ids("results", "nodes", "nodes", [1, 2])
    on_point = points.add_from_selection(PREFIX + "OnPoint", "OnPoint", one)
    check(on_point.name == PREFIX + "OnPoint", "add_from_selection did not return the reference point")
    check(points[PREFIX + "OnPoint"].created_from == "OnPoint",
          "the construction mode was not stored")
    points.replace(PREFIX + "OnPoint", coordinates=[10, 20, 30])
    check(points[PREFIX + "OnPoint"].created_from == "Coordinates",
          "the coordinate replacement kept the obsolete construction mode")
    check(points[PREFIX + "OnPoint"].get_coordinates() == [10.0, 20.0, 30.0],
          "the replaced coordinates did not read back")
    #
    replaced = points.replace_from_selection(PREFIX + "OnPoint", created_from="BetweenTwoPoints", selection=two)
    check(replaced.name == PREFIX + "OnPoint", "replace_from_selection did not return the reference point")
    check(points[PREFIX + "OnPoint"].created_from == "BetweenTwoPoints",
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
        region_point = points.add_from_region(PREFIX + "Region", "CenterOfGravity", "NodeSetName", node_sets[0])
        check(region_point.created_from == "CenterOfGravity", "the region construction mode was not stored")
        construction = region_point.get_construction()
        check(construction["region_type"] == "NodeSetName" and construction["region_name"] == node_sets[0],
              "the region construction did not read back: " + str(dict(construction)))
    check_raises(lambda: points.add_from_region(PREFIX + "Bad", "CenterOfGravity", "NodeSetName",
                                                 "__missing_result_node_set__"),
                 "a missing result region was accepted")
    check_raises(lambda: points.add_from_region(PREFIX + "Bad", "OnPoint", "NodeSetName", "any"),
                 "a selection mode with a region was accepted")
    clean(points)


def test_coordinate_system_construction(systems):
    one = pmx.create.selection_by_ids("results", "nodes", "nodes", [1])
    cylindrical = systems.add(PREFIX + "Cyl", [0, 0, 0], [1, 0, 0], [0, 1, 0],
                              coordinate_system_type="Cylindrical")
    check(cylindrical.coordinate_system_type == "Cylindrical",
          "the coordinate system type was not stored")
    systems.replace(PREFIX + "Cyl", origin=[0, 0, 0], point_on_x=[2, 0, 0], point_in_xy=[0, 2, 0])
    check(systems[PREFIX + "Cyl"].coordinate_system_type == "Cylindrical",
          "the replacement reset the coordinate system type")
    systems.replace(PREFIX + "Cyl", origin=[0, 0, 0], point_on_x=[2, 0, 0], point_in_xy=[0, 2, 0],
                    coordinate_system_type="Rectangular")
    check(systems[PREFIX + "Cyl"].coordinate_system_type == "Rectangular",
          "the coordinate system type was not replaced")
    check_raises(lambda: systems.add(PREFIX + "Bad", [0, 0, 0], [1, 0, 0], [0, 1, 0],
                                      coordinate_system_type="NotAType"),
                 "an unknown coordinate system type was accepted")
    #
    # A point built from a selection must not read back as a typed coordinate point
    node = node_coordinates(1)
    origin = [node[0] - 1.0, node[1], node[2]]
    in_xy = [node[0] - 1.0, node[1] + 1.0, node[2]]
    mixed = systems.add_from_selections(PREFIX + "Mixed", origin=origin,
                                        point_on_x_created_from="OnPoint", point_on_x_selection=one,
                                        point_in_xy=in_xy)
    check(mixed.name == PREFIX + "Mixed", "add_from_selections did not return the coordinate system")
    modes = construction_modes(mixed)
    check(modes[0] is None, "the typed origin did not read back as a coordinate point")
    check(modes[1] == "OnPoint", "the selected X point did not read back as a selected point")
    replaced = systems.replace_from_selections(PREFIX + "Mixed", origin=[0, 0, 0], point_on_x=[1, 0, 0],
                                               point_in_xy=[0, 1, 0])
    check(replaced.name == PREFIX + "Mixed", "replace_from_selections did not return the coordinate system")
    check(construction_modes(systems[PREFIX + "Mixed"]) == [None] * 3,
          "the replacement kept the obsolete point construction modes")
    # A typed origin with a selected XY point: the XY point is reevaluated from its own selection,
    # not from the one of the origin, which a typed origin does not have
    systems.add_from_selections(PREFIX + "MixedXY", origin=origin,
                                 point_on_x=[node[0], node[1] + 1.0, node[2]],
                                 point_in_xy_created_from="OnPoint", point_in_xy_selection=one)
    modes = construction_modes(systems[PREFIX + "MixedXY"])
    check(modes[0] is None and modes[2] == "OnPoint",
          "a typed origin with a selected XY point was not stored")
    check(abs(systems[PREFIX + "MixedXY"].get_point_in_xy()[0] - node[0]) < 1E-6,
          "the selected XY point was not resolved on the result mesh")
    # A creation keeps the default of a point it is not told about, the way the dialog does
    systems.add_from_selections(PREFIX + "Default", origin=[0, 0, 0])
    check(construction_modes(systems[PREFIX + "Default"]) == [None] * 3,
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
    check(construction_modes(systems[PREFIX + "Mixed"]) == [None] * 3,
          "a refused mixed replacement changed the definition")
    check(PREFIX + "Bad" not in list(systems.get_names()), "a refused system changed the result")
    # What get_construction reads back goes back into the argument that sets it: None beside the coordinates
    # of the point, a mode with a selection
    modes = systems[PREFIX + "MixedXY"].get_construction()
    systems.replace_from_selections(PREFIX + "MixedXY", origin=origin,
                                    origin_created_from=modes["origin_created_from"],
                                    point_on_x=[node[0], node[1] + 1.0, node[2]],
                                    point_on_x_created_from=modes["point_on_x_created_from"],
                                    point_in_xy_created_from=modes["point_in_xy_created_from"],
                                    point_in_xy_selection=one)
    check(systems[PREFIX + "MixedXY"].get_construction() == modes, "the construction did not survive a round trip")
    check_raises(lambda: systems.add_from_selections(PREFIX + "Bad", origin_created_from="Coordinates",
                                                      origin_selection=one),
                 "the mode Coordinates was accepted with a selection")
    clean(systems)


def test_coordinate_systems(systems):
    system = systems.add(PREFIX + "CS", [0, 0, 0], [1, 0, 0], [0, 1, 0])
    check(system.name == PREFIX + "CS", "add did not return the coordinate system")
    check(PREFIX + "CS" in list(systems.get_names()), "the coordinate system was not added")
    check(len(system.get_equations()) == 0, "plain coordinates were read back as expressions")
    expression = systems.add(PREFIX + "CS_E", [0, 0, 0], ["=2*1", 0, 0], [0, 1, 0])
    equations = expression.get_equations()
    check(sorted(equations) == ["point_on_x"] and equations["point_on_x"][0].replace(" ", "") == "=2*1" and
          equations["point_on_x"][1:] == [0.0, 0.0],
          "the expressions must read back under the argument names: " + str(dict(equations)))
    systems.replace(PREFIX + "CS_E", origin=[0, 0, 0], point_on_x=equations["point_on_x"], point_in_xy=[0, 1, 0])
    check(systems[PREFIX + "CS_E"].get_equations() == equations, "the expressions did not survive a round trip")
    systems.remove(PREFIX + "cs_e")
    check(not systems.contains(PREFIX + "CS_E"), "remove did not find the coordinate system in another case")
    # The collection is indexed by name and iteration yields the items rather than their names
    check(systems[PREFIX + "CS"].name == PREFIX + "CS", "the name indexer did not return the item")
    check(systems.get(PREFIX + "CS").name == PREFIX + "CS", "get did not return the item")
    check(systems.last().name == PREFIX + "CS", "last did not return the newest coordinate system")
    check(systems.first().name == list(systems.get_names())[0], "first did not return the first item")
    items = list(systems)
    check(len(items) == len(systems), "iteration and len disagree")
    check(PREFIX + "CS" in [item.name for item in items], "iteration did not yield coordinate systems")
    check(system.coordinate_system_type == "Rectangular", "the default type did not read back")
    check(system.visible, "a new result coordinate system must be visible")
    check(system.get_origin() == [0.0, 0.0, 0.0], "the origin did not read back")
    check(system.get_point_on_x() == [1.0, 0.0, 0.0], "the point on X did not read back")
    check(system.get_point_in_xy() == [0.0, 1.0, 0.0], "the point in XY did not read back")
    for defining_point in (system.get_origin(), system.get_point_on_x(), system.get_point_in_xy()):
        check(type(defining_point) is list, "a result coordinate system point must be a list: " +
              type(defining_point).__name__)
    check(construction_modes(system) == [None] * 3, "the construction did not read back")
    check("CoordinateSystem(" in str(system), "the item has no readable string form: " + str(system))
    check_raises(lambda: systems["__missing_result_cs__"], "a missing coordinate system was indexed")
    systems.hide(PREFIX + "CS")
    systems.show(PREFIX + "CS")
    systems.duplicate(PREFIX + "CS")
    replaced = systems.replace(PREFIX + "CS", name=PREFIX + "CS_R", origin=[0, 0, 0],
                               point_on_x=[2, 0, 0], point_in_xy=[0, 1, 0])
    check(replaced.name == PREFIX + "CS_R", "replace did not return the renamed coordinate system")
    check(replaced.get_point_on_x() == [2.0, 0.0, 0.0], "the replaced point on X did not read back")
    check(PREFIX + "CS_R" in list(systems.get_names()), "the coordinate system was not renamed")
    check_raises(lambda: systems.add(PREFIX + "Bad", [0, 0, 0], [0, 0, 0], [0, 1, 0]),
                 "a degenerate coordinate system was accepted")
    check_raises(lambda: systems.remove("__missing_result_cs__"), "a missing coordinate system was accepted")
    # The name visibility is part of the stored definition
    check(systems[PREFIX + "CS_R"].name_visible, "a new result system must have a visible name")
    systems.add(PREFIX + "CS_N", [0, 0, 0], [1, 0, 0], [0, 1, 0], name_visible=False)
    check(not systems[PREFIX + "CS_N"].name_visible, "the name visibility was not stored")
    systems.replace(PREFIX + "CS_N", origin=[0, 0, 0], point_on_x=[3, 0, 0], point_in_xy=[0, 1, 0],
                    name_visible=True)
    check(systems[PREFIX + "CS_N"].name_visible, "the replacement did not set the name visibility")
    # The rename is the one the results view records, and names are compared without regard to case
    check(systems.contains(PREFIX + "cs_r") and PREFIX + "CS_R".upper() in systems,
          "contains must compare names without regard to case")
    check(not systems.contains("__missing_result_cs__"), "a missing coordinate system was found")
    systems.rename(PREFIX + "CS_R", PREFIX + "CS_M")
    check(systems.contains(PREFIX + "CS_M") and not systems.contains(PREFIX + "CS_R"),
          "the coordinate system was not renamed: " + str(list(systems.get_names())))
    check_raises(lambda: systems.rename(PREFIX + "CS_M", PREFIX + "cs_n"),
                 "a rename onto an existing name in another case was accepted")
    check_raises(lambda: systems.rename(PREFIX + "CS_M", ""), "an empty new name was accepted")
    check_raises(lambda: systems.rename("__missing_result_cs__", PREFIX + "CS_X"),
                 "renaming a missing coordinate system was accepted")
    check(not systems.contains(PREFIX + "CS_X"), "a refused rename changed the coordinate systems")
    clean(systems)
    check(len(systems) == 0, "the coordinate systems left behind " + str(list(systems.get_names())))


def main():
    check(len(pmx.results.get_names().to_array()) > 0, "no result is loaded")
    mesh = pmx.results.current.mesh
    clean(mesh.reference_points)
    clean(mesh.coordinate_systems)
    test_reference_point_items(mesh.reference_points)
    test_reference_points(mesh.reference_points)
    test_reference_point_construction(mesh.reference_points)
    test_coordinate_systems(mesh.coordinate_systems)
    test_coordinate_system_construction(mesh.coordinate_systems)
    print("TEST PASSED")


main()
