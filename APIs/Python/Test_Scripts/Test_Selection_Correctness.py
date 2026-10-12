#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the selection algebra and the recorded selection options: the set operations a selection can
#carry, select all, that hiding a part leaves what a stored selection resolves to unchanged, the picking
#options and that their limits apply to the picks, that a pick keeps the part_ids it is given, also where two
#parts share nodes (sweep_side2.brep is imported for that), and the area form. A meshed model must be open.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Selection_Correctness.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os

MODELS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models")

# The box around the top face of the unit cube, as the six outward planes a rubber band records
TOP_FACE_PLANES = [[0, 0, 0.9, 0, 0, -1], [0, 0, 1.1, 0, 0, 1],
                   [-0.1, 0, 0, -1, 0, 0], [1.1, 0, 0, 1, 0, 0],
                   [0, -0.1, 0, 0, -1, 0], [0, 1.1, 0, 0, 1, 0]]


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


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


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


def test_hidden_part_stays_selected():
    # Hiding a part changes the view and not the model, so a stored selection resolves to the same items
    # whether the part is shown or hidden, and a selection made while it is hidden covers it as well
    part_name = pmx.model.parts.first().name
    count = pmx.model.nodes.count
    expected = {"PyTest_All_Shown": count, "PyTest_Ids_Shown": 2, "PyTest_Inverted_Shown": count - 2,
                "PyTest_All_Hidden": count, "PyTest_Ids_Hidden": 2}
    pmx.model.node_sets.add("PyTest_All_Shown", pmx.create.selection_all('femodel', 'nodes'))
    pmx.model.node_sets.add("PyTest_Ids_Shown", pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2]))
    pmx.model.node_sets.add("PyTest_Inverted_Shown",
                            pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2]).invert())
    pmx.model.parts.hide([part_name])
    try:
        check(len(ids(pmx.create.selection_all('femodel', 'nodes'))) == count,
              "select all must cover the hidden part")
        pmx.model.node_sets.add("PyTest_All_Hidden", pmx.create.selection_all('femodel', 'nodes'))
        pmx.model.node_sets.add("PyTest_Ids_Hidden",
                                pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2]))
        # Deactivating and activating the part resolves every stored selection again while it is hidden
        pmx.model.parts.deactivate([part_name])
        pmx.model.parts.activate([part_name])
        for name in expected:
            node_set = pmx.model.node_sets[name]
            check(node_set.valid and node_set.count == expected[name],
                  name + " resolved to " + str(node_set.count) + " nodes with the part hidden, expected " +
                  str(expected[name]))
        pmx.model.parts.show([part_name])
        pmx.model.parts.deactivate([part_name])
        pmx.model.parts.activate([part_name])
        for name in expected:
            check(pmx.model.node_sets[name].count == expected[name],
                  name + " changed when the part was shown again")
    finally:
        pmx.model.parts.show([part_name])
        pmx.model.node_sets.remove([name for name in expected if pmx.model.node_sets.contains(name)])


def test_options():
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])
    defaults = selection.get_options()
    check(defaults['max_geometry_ids'] == -1 and defaults['max_item_ids'] == -1 and defaults['limit_to'] == 'None',
          "a new selection must be unlimited")
    # A refused limit names the argument it came from
    for argument in ['max_geometry_ids', 'max_item_ids']:
        try:
            selection.configure(**{argument: 0})
        except Exception as ex:
            check("'" + argument + "'" in str(ex), "the refusal must name " + argument + ": " + str(ex))
        else:
            raise Exception("TEST FAILED: a zero " + argument + " was accepted")
    check(selection.get_options()['max_geometry_ids'] == -1 and selection.get_options()['max_item_ids'] == -1,
          "a refused limit changed the options")
    options = selection.configure(max_geometry_ids=12, max_item_ids=7, limit_to='FirstPart',
                                  shell_edges_only=True, enable_shell_edge_faces=True).get_options()
    check(options['max_geometry_ids'] == 12, "max_geometry_ids did not read back")
    check(options['max_item_ids'] == 7, "max_item_ids did not read back")
    check(options['limit_to'] == 'FirstPart', "limit_to did not read back")
    check(options['shell_edges_only'], "shell_edges_only did not read back")
    check(options['enable_shell_edge_faces'], "enable_shell_edge_faces did not read back")
    check_raises(lambda: selection.configure(limit_to='NotAMode'), "an unknown limit_to was accepted")
    # An option string is matched regardless of case and reads back in the spelling of its enumeration
    options = selection.configure(limit_to='firstgeometrytype').get_options()
    check(options['limit_to'] == 'FirstGeometryType', "limit_to must be taken regardless of case: " +
          str(options['limit_to']))


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


def part_node_ids(part_id):
    return set(pmx.create.selection_by_ids('femodel', 'nodes', 'parts', [part_id]).get_ids())


def test_given_part_ids_are_kept():
    # A pick restricted to a part stays on it, even where another part lies nearer the point, and the item it
    # creates records that part
    parts = pmx.model.parts
    first = parts.first()
    parts.translate(first.name, [10, 0, 0], number_of_copies=1)
    second = parts.last()
    try:
        on_first = [0.5, 0.5, 1.0]
        selection = pmx.create.selection_by_pick('femodel', 'nodes', on_first, 'Surface', part_ids=[second.id],
                                                 precision=0.01)
        picked = set(ids(selection))
        check(len(picked) > 0 and picked <= part_node_ids(second.id),
              "a pick restricted to part " + str(second.id) + " selected " + str(sorted(picked)))
        node_set = pmx.model.node_sets.add("PyTest_Given_Part", selection)
        check(set(node_set.get_node_ids()) == picked, "the stored pick moved to another part")
        # The part stays given when the stored pick is resolved again
        parts.deactivate([first.name])
        parts.activate([first.name])
        check(set(pmx.model.node_sets["PyTest_Given_Part"].get_node_ids()) == picked,
              "the stored pick moved to another part when it was resolved again")
        # Without part_ids the pick takes the part under the point
        check(set(ids(pmx.create.selection_by_pick('femodel', 'nodes', on_first, 'Surface', precision=0.01))) <=
              part_node_ids(first.id), "a pick without part_ids must take the part under the point")
        pmx.model.node_sets.remove("PyTest_Given_Part")
    finally:
        parts.remove(second.name)


def test_limits_apply():
    # The limits a selection is configured with apply to its picks one by one, as to the clicks of a dialog
    parts = pmx.model.parts
    first = parts.first()
    parts.translate(first.name, [10, 0, 0], number_of_copies=1)
    second = parts.last()
    try:
        first_top = set([5, 6, 7, 8])
        second_top = set(ids(pmx.create.selection_by_pick('femodel', 'nodes', [10.5, 0.5, 1.0], 'Surface',
                                                          part_ids=[second.id])))

        def two_picks(**options):
            selection = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface')
            return selection.configure(**options).add_by_pick([10.5, 0.5, 1.0], 'Surface')
        check(set(ids(two_picks())) == first_top | second_top, "two picks without limits must take both faces")
        check(set(ids(two_picks(limit_to='FirstPart'))) == first_top,
              "limit_to='FirstPart' must leave out the pick on the other part")
        check(set(ids(two_picks(max_geometry_ids=1))) == second_top,
              "max_geometry_ids=1 must let the last pick replace the first")
        check(set(ids(two_picks(max_item_ids=4))) == first_top,
              "max_item_ids=4 must leave out the pick that would exceed it")
        check(set(ids(pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3])
                      .configure(max_item_ids=2).add_by_ids('nodes', [4]))) == set([4]),
              "a cap must apply to ID items as well")
        # The item created keeps only what the limits kept, and a selection they empty is refused saying so
        node_set = pmx.model.node_sets.add("PyTest_Limited", two_picks(limit_to='FirstPart'))
        check(set(node_set.get_node_ids()) == first_top, "the stored selection did not keep its limits")
        pmx.model.node_sets.remove("PyTest_Limited")
        names_before = list(pmx.model.node_sets.get_names())
        message = raised_message(lambda: pmx.model.node_sets.add("PyTest_Limited", two_picks(max_item_ids=1)))
        check(message is not None and "limits" in message,
              "a selection its limits empty must be refused naming them: " + str(message))
        check(list(pmx.model.node_sets.get_names()) == names_before, "a refused node set was recorded")
    finally:
        parts.remove(second.name)


def test_given_part_ids_on_shared_nodes():
    # Two boxes meshed as one compound share the nodes of their common wall, so a pick restricted to one of
    # them must take the faces and elements of that box only
    names_before = list(pmx.model.geometry.parts.get_names())
    mesh_parts_before = list(pmx.model.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "sweep_side2.brep"))
    added = [name for name in pmx.model.geometry.parts.get_names() if name not in names_before]
    compounds = [name for name in added if pmx.model.geometry.parts[name].part_type == "Compound"]
    try:
        check(len(compounds) == 1, "sweep_side2.brep must be imported as one compound, not " + str(added))
        pmx.model.mesh.generate(compounds)
        boxes = [part for part in pmx.model.parts if part.name not in mesh_parts_before]
        check(len(boxes) == 2, "the compound must give two mesh parts, not " + str(len(boxes)))
        wall = [5.0, 5.0, 3.0]
        for part in boxes:
            for target, mode in [('surfaces', 'Surface'), ('elements', 'Surface'), ('surfaces', 'Node'),
                                 ('elements', 'SurfaceAngle')]:
                angle = 30 if mode == 'SurfaceAngle' else None
                selection = pmx.create.selection_by_pick('femodel', target, wall, mode, angle_deg=angle,
                                                         part_ids=[part.id], precision=0.01)
                if target == 'surfaces':
                    element_ids = set(face_id // 10 for face_id in ids(selection))
                else:
                    element_ids = set(ids(selection))
                part_elements = set(pmx.create.selection_by_ids('femodel', 'elements', 'parts', [part.id]).get_ids())
                check(len(element_ids) > 0 and element_ids <= part_elements,
                      "a " + mode + " pick of " + target + " restricted to part " + str(part.id) +
                      " took elements of the other box")
    finally:
        pmx.model.parts.remove([name for name in pmx.model.parts.get_names() if name not in mesh_parts_before])
        pmx.model.geometry.parts.remove(compounds if len(compounds) == 1 else added)


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
    # An edge or a surface counts from 1 on its part, so 0 or a negative ID is refused rather than turned
    # into the geometry ID of another item
    for source in ['surfaces', 'edges']:
        for item_id in [0, -1]:
            check_raises(lambda: ids(pmx.create.selection_by_ids('femodel', 'geometry', source, {1: [item_id]})),
                         "the " + source + " ID " + str(item_id) + " was accepted")
    # A geometry ID is item_index * 100000 + type * 10000 + part_id. One naming a missing part, a type digit that
    # is no geometry type, or an item its part does not have is refused naming 'ids', for every target and for a
    # preview, rather than failing inside the mesh, and a model item given it is not recorded
    part_id = geometry_ids[0] % 10000
    node_sets_before = list(pmx.model.node_sets.get_names())
    for geometry_id, words in [(geometry_ids[0] - part_id + 9999, "does not exist"),
                               (7 * 10000 + part_id, "type digit 7"),
                               (part_id, "type digit 0"),
                               (999 * 100000 + 3 * 10000 + part_id, "has"),
                               (999 * 100000 + 2 * 10000 + part_id, "has"),
                               (999 * 100000 + 1 * 10000 + part_id, "has"),
                               (1 * 100000 + 9 * 10000 + part_id, "has")]:
        for target in ['nodes', 'elements', 'surfaces', 'edges', 'parts', 'geometry']:
            selection = pmx.create.selection_by_ids('femodel', target, 'geometry', [geometry_id])
            for function in [lambda: selection.get_ids(), lambda: selection.preview()]:
                message = raised_message(function)
                check(message is not None and "'ids'" in message and words in message,
                      "the geometry ID " + str(geometry_id) + " must be refused naming ids for target " + target +
                      ": " + str(message))
        selection = pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id])
        message = raised_message(lambda: pmx.model.node_sets.add("PyTest_Bad_Geometry_Id", selection))
        check(message is not None and "'ids'" in message, "a node set from the geometry ID " + str(geometry_id) +
              " must be refused naming ids: " + str(message))
    check(list(pmx.model.node_sets.get_names()) == node_sets_before, "a refused node set was recorded")


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
    test_hidden_part_stays_selected()
    test_options()
    test_repeated_picks()
    test_area()
    test_multiple_part_pick()
    test_given_part_ids_are_kept()
    test_limits_apply()
    test_geometry_source()
    test_case_insensitive_names()
    test_given_part_ids_on_shared_nodes()
    print("TEST PASSED")


main()
