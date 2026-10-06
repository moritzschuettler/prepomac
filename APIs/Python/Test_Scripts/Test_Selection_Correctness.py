#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the selection algebra and the recorded selection options: the set operations a selection can
#carry, select all, the picking options and the area form. A meshed model must be open.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Selection_Correctness.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

# The box around the top face of the unit cube, as the six outward planes a rubber band records
TOP_FACE_PLANES = [[0, 0, 0.9, 0, 0, -1], [0, 0, 1.1, 0, 0, 1],
                   [-0.1, 0, 0, -1, 0, 0], [1.1, 0, 0, 1, 0, 0],
                   [0, -0.1, 0, 0, -1, 0], [0, 1.1, 0, 0, 1, 0]]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def ids(selection):
    return sorted(list(selection.get_ids()))


def test_set_operations():
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2])
    selection.add_by_ids('nodes', [3], operation='Add')
    check(ids(selection) == [1, 2, 3], "the added IDs are missing: " + str(ids(selection)))
    #
    selection.add_by_ids('nodes', [2], operation='Subtract')
    check(ids(selection) == [1, 3], "the subtracted ID is still selected: " + str(ids(selection)))
    #
    selection.add_by_ids('nodes', [3, 4], operation='Intersect')
    check(ids(selection) == [3], "the intersection is wrong: " + str(ids(selection)))
    # None replaces whatever the selection has collected so far, which is how a new pick starts
    selection.add_by_ids('nodes', [4], operation='None')
    check(ids(selection) == [4], "None did not replace the selection: " + str(ids(selection)))
    # A selection stores a description and resolves it against the mesh of the moment, so an ID the
    # mesh does not have is reported when the selection is resolved rather than when it is added
    selection.add_by_ids('nodes', [999999])
    check_raises(lambda: selection.get_ids(), "an unknown ID resolved without a report")


def test_select_all():
    selection = pmx.create.selection_all('femodel', 'nodes')
    check(len(ids(selection)) == pmx.model.nodes.count, "select all did not select every node")
    selection.add_all('Subtract')
    check(ids(selection) == [], "subtracting everything left " + str(ids(selection)))
    selection.add_all()
    check(len(ids(selection)) == pmx.model.nodes.count, "adding everything back failed")
    check_raises(lambda: selection.add_all('Invert'), "Invert is not a set operation and must be rejected")


def test_options():
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])
    options = selection.configure(max_geometry_ids=12, max_item_ids=7, limit_to='FirstPart',
                                  shell_edges_only=True, enable_shell_edge_faces=True).get_options()
    check(options['max_geometry_ids'] == 12, "max_geometry_ids did not read back")
    check(options['max_item_ids'] == 7, "max_item_ids did not read back")
    check(options['limit_to'] == 'FirstPart', "limit_to did not read back")
    check(options['shell_edges_only'], "shell_edges_only did not read back")
    check(options['enable_shell_edge_faces'], "enable_shell_edge_faces did not read back")
    check_raises(lambda: selection.configure(limit_to='NotAMode'), "an unknown limit_to was accepted")


def test_repeated_picks():
    selection = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface')
    selection.add_by_pick([0.5, 0.5, 0.0], 'Surface', operation='Add')
    check(len(ids(selection)) == 8, "two opposite face picks select 8 nodes, got " + str(ids(selection)))


def test_area():
    selection = pmx.create.selection_by_area('femodel', 'nodes', TOP_FACE_PLANES, 'Node')
    check(ids(selection) == [5, 6, 7, 8], "the area pick selected " + str(ids(selection)))
    check_raises(lambda: pmx.create.selection_by_area('femodel', 'nodes', TOP_FACE_PLANES[:2], 'Node'),
                 "two planes bound no volume and must be rejected")
    check_raises(lambda: pmx.create.selection_by_area('femodel', 'nodes', [[0, 0, 0, 0, 0, 0]], 'Node'),
                 "a plane without a normal must be rejected")


def test_multiple_part_pick():
    parts = pmx.model.parts
    first = parts.first().name
    parts.translate(first, [10, 0, 0], number_of_copies=1)
    second = parts.last()
    point = [10.5, 0.5, 1.0]
    # A pick that names its part and one that searches every part must find the same face
    explicit = pmx.create.selection_by_pick('femodel', 'nodes', point, 'GeometrySurface',
                                            direction=[0, 0, -1], part_ids=[second.id], precision=0.01,
                                            part_offsets=[[second.id, 0, 0, 0]])
    default = pmx.create.selection_by_pick('femodel', 'nodes', point, 'GeometrySurface',
                                           direction=[0, 0, -1])
    check(ids(explicit) == ids(default),
          "the default pick found " + str(ids(default)) + " and the explicit one " + str(ids(explicit)))
    check_raises(lambda: pmx.create.selection_by_pick('femodel', 'nodes', [0, 0, 0], 'Surface', precision=-1),
                 "a negative precision was accepted")
    parts.remove(second.name)


def test_geometry_source():
    # A geometry pick made in the Selection by ID mode is recorded as geometry IDs, so the API must be
    # able to take those IDs back unchanged and convert them to any target
    from_surfaces = pmx.create.selection_by_ids('femodel', 'geometry', 'surfaces', {1: [1]})
    geometry_ids = ids(from_surfaces)
    check(len(geometry_ids) > 0, "the surface source resolved to no geometry IDs")
    #
    from_geometry = pmx.create.selection_by_ids('femodel', 'geometry', 'geometry', geometry_ids)
    check(ids(from_geometry) == geometry_ids,
          "the geometry source did not reproduce the IDs: " + str(ids(from_geometry)))
    # The same IDs must convert to another target the way the surface source does
    nodes_from_geometry = pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', geometry_ids)
    nodes_from_surfaces = pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {1: [1]})
    check(ids(nodes_from_geometry) == ids(nodes_from_surfaces),
          "the geometry source and the surface source disagree on the nodes")


def test_case_insensitive_names():
    # The view, the target and the source are matched regardless of case, and the selection keeps the
    # lowercase spelling the rest of the API compares against, so it can still be used as a region
    selection = pmx.create.selection_by_ids('FeModel', 'Nodes', 'NODES', [1, 2])
    check(selection.view == 'femodel' and selection.target == 'nodes',
          "the view and the target were not stored canonically: " + selection.view + ", " + selection.target)
    selection.add_by_ids('Nodes', [3], operation='add')
    check(ids(selection) == [1, 2, 3], "the case insensitive names did not resolve: " + str(ids(selection)))
    from_surfaces = pmx.create.selection_by_ids('femodel', 'NODES', 'Surfaces', {1: [1]})
    check(ids(from_surfaces) == ids(pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {1: [1]})),
          "a capitalised source converted differently")
    everything = pmx.create.selection_all('FEMODEL', 'Nodes')
    check(len(ids(everything)) == pmx.model.nodes.count, "a capitalised select all did not select every node")
    name = "PyTest_Selection_Case"
    pmx.model.node_sets.add(name, selection)
    try:
        check(sorted(list(pmx.model.node_sets[name].get_node_ids())) == [1, 2, 3],
              "a capitalised selection did not work as a region")
    finally:
        pmx.model.node_sets.remove(name)
    check_raises(lambda: pmx.create.selection_by_ids('femodel', 'nodez', 'nodes', [1]),
                 "an unknown target was accepted")
    check_raises(lambda: pmx.create.selection_by_ids('femodel', 'nodes', 'nodez', [1]),
                 "an unknown source was accepted")


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_set_operations()
    test_select_all()
    test_options()
    test_repeated_picks()
    test_area()
    test_multiple_part_pick()
    test_geometry_source()
    test_case_insensitive_names()
    print("TEST PASSED")


main()
