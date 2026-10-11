#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the picked selection: a point in space rather than a list of IDs, resolved by the
#application the same way a mouse click is. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Selection_Pick.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"


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
    for collection in [pmx.model.surfaces, pmx.model.node_sets, pmx.model.element_sets]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def test_pick_resolves():
    # The cube spans 0 to 1, so a point on its top face picks that face
    selection = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface')
    node_set = pmx.model.node_sets.add(PREFIX + "Top", selection)
    check(node_set.count == 4, "the top face has 4 nodes, the pick found " + str(node_set.count))
    ids = sorted(list(node_set.get_node_ids()))
    check(ids == [5, 6, 7, 8], "unexpected picked nodes " + str(ids))
    # The bottom face is the opposite pick
    selection = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 0.0], 'Surface')
    node_set = pmx.model.node_sets.add(PREFIX + "Bottom", selection)
    check(sorted(list(node_set.get_node_ids())) == [1, 2, 3, 4],
          "unexpected bottom nodes " + str(sorted(list(node_set.get_node_ids()))))
    # A surface target picks the face itself
    selection = pmx.create.selection_by_pick('femodel', 'surfaces', [0.5, 0.5, 1.0], 'Surface')
    surface = pmx.model.surfaces.add(PREFIX + "Top_S", selection)
    check(surface.area > 0, "the picked surface has no area")
    check(len(surface.get_face_ids()) > 0, "the picked surface has no faces")


def test_pick_arguments():
    # A pick names the parts it searches; without them the whole mesh is searched
    part = pmx.model.parts.first()
    selection = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface',
                                             part_ids=[part.id], direction=[0, 0, -1])
    node_set = pmx.model.node_sets.add(PREFIX + "Explicit", selection)
    check(node_set.count == 4, "the explicit pick found " + str(node_set.count) + " nodes")
    # A pick always finds something: the application picks the nearest entity rather than requiring
    # the point to lie on the model, so a point far outside it still resolves, to the nearest face
    far = pmx.model.node_sets.add(PREFIX + "Far",
                                  pmx.create.selection_by_pick('femodel', 'nodes', [9.0, 9.0, 9.0],
                                                               'Surface'))
    check(far.count == 4, "a far point picks the nearest face, found " + str(far.count) + " nodes")
    # The select_by value is checked
    check_raises(lambda: pmx.create.selection_by_pick('femodel', 'nodes', [0, 0, 0], 'NotAMode'),
                 "an unknown select_by must be rejected")
    check_raises(lambda: pmx.create.selection_by_pick('femodel', 'nodes', [0, 0, 0], 'Widget'),
                 "a select_by that cannot be picked must be rejected")
    # An angle based mode cannot do without an angle
    check_raises(lambda: pmx.create.selection_by_pick('femodel', 'nodes', [0, 0, 0], 'SurfaceAngle'),
                 "an angle based pick without an angle must be rejected")
    # The angle between two faces lies from 0 to 180 degrees, which the dialog keeps the angle to as well
    pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'SurfaceAngle', angle_deg=180)
    for angle in [180.5, -1, float('nan'), float('inf')]:
        try:
            pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'SurfaceAngle', angle_deg=angle)
        except Exception as exception:
            check("angle_deg" in str(exception), "the refusal must name angle_deg: " + str(exception))
        else:
            raise Exception("TEST FAILED: the pick angle " + str(angle) + " must be rejected")
    check_raises(lambda: pmx.create.selection_by_pick('femodel', 'nodes', [0, 0], 'Surface'),
                 "a two value point must be rejected")


def test_pick_combines_with_ids():
    # A selection holds picks and ID items together
    selection = pmx.create.selection_by_pick('femodel', 'nodes', [0.5, 0.5, 1.0], 'Surface')
    selection.add_by_ids('nodes', [1])
    node_set = pmx.model.node_sets.add(PREFIX + "Mixed", selection)
    check(node_set.count == 5, "the mixed selection has 5 nodes, found " + str(node_set.count))
    sources = list(selection.get_sources())
    check("pick" in sources, "the pick is missing from the sources " + str(sources))


def test_area_pick():
    # A box is the six plane form of an area pick, and its normals point out of the volume
    selection = pmx.create.selection_by_box('femodel', 'nodes', [-0.1, -0.1, 0.9], [1.1, 1.1, 1.1], 'Node')
    node_set = pmx.model.node_sets.add(PREFIX + "Box", selection)
    check(sorted(list(node_set.get_node_ids())) == [5, 6, 7, 8],
          "the box around the top face selected " + str(sorted(list(node_set.get_node_ids()))))
    #
    selection = pmx.create.selection_by_box('femodel', 'nodes', [-0.1, -0.1, -0.1], [1.1, 1.1, 1.1], 'Node')
    node_set = pmx.model.node_sets.add(PREFIX + "Box_All", selection)
    check(node_set.count == 8, "the box around the cube selected " + str(node_set.count) + " nodes")
    # Unlike a point pick, an area pick selects only what lies inside it, so an empty box finds nothing
    check_raises(lambda: pmx.model.node_sets.add(
                     PREFIX + "Bad",
                     pmx.create.selection_by_box('femodel', 'nodes', [9, 9, 9], [10, 10, 10], 'Node')),
                 "a box that contains nothing must not resolve")
    # The explicit plane form is what a rubber band records
    planes = [[0, 0, 0.9, 0, 0, -1], [0, 0, 1.1, 0, 0, 1],
              [-0.1, 0, 0, -1, 0, 0], [1.1, 0, 0, 1, 0, 0],
              [0, -0.1, 0, 0, -1, 0], [0, 1.1, 0, 0, 1, 0]]
    selection = pmx.create.selection_by_area('femodel', 'nodes', planes, 'Node')
    node_set = pmx.model.node_sets.add(PREFIX + "Planes", selection)
    check(sorted(list(node_set.get_node_ids())) == [5, 6, 7, 8],
          "the plane form selected " + str(sorted(list(node_set.get_node_ids()))))
    check_raises(lambda: pmx.create.selection_by_area('femodel', 'nodes', planes[:2], 'Node'),
                 "fewer than four planes bound no volume and must be rejected")
    check_raises(lambda: pmx.create.selection_by_area('femodel', 'nodes', [[0, 0, 0, 0, 0]], 'Node'),
                 "a plane row of five values must be rejected")
    check_raises(lambda: pmx.create.selection_by_box('femodel', 'nodes', [0, 0, 0], [0, 1, 1], 'Node'),
                 "a box with no extent along an axis must be rejected")


def test_invert():
    # An invert flips what the selection has selected so far
    selection = pmx.create.selection_by_box('femodel', 'nodes', [-0.1, -0.1, 0.9], [1.1, 1.1, 1.1], 'Node')
    node_set = pmx.model.node_sets.add(PREFIX + "Inverted", selection.invert())
    check(sorted(list(node_set.get_node_ids())) == [1, 2, 3, 4],
          "the inverted top face is the bottom one, got " + str(sorted(list(node_set.get_node_ids()))))
    # It inverts an ID based selection just as well
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2]).invert()
    node_set = pmx.model.node_sets.add(PREFIX + "Inverted_Ids", selection)
    check(sorted(list(node_set.get_node_ids())) == [3, 4, 5, 6, 7, 8],
          "unexpected inverted nodes " + str(sorted(list(node_set.get_node_ids()))))
    # There is nothing to invert in an empty selection
    check_raises(lambda: pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', []).invert(),
                 "inverting an empty selection must be rejected")
    sources = list(selection.get_sources())
    check("invert" in sources, "the invert is missing from the sources " + str(sources))


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    test_pick_resolves()
    test_pick_arguments()
    test_pick_combines_with_ids()
    test_area_pick()
    test_invert()
    clean()
    print("TEST PASSED")


main()
