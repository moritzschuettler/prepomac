#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Behavioral coverage for bounded region definitions. A meshed model must be open before the script runs.
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Region_Coverage.py -g No -x Yes -w <dir>
#Expected process exit code 0.

PREFIX = "PyTest_Cov_"

def check(value, message):
    if not value:
        raise Exception("TEST FAILED: " + message)

def raises(call, message):
    try:
        call()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)

def clean(collection):
    names = [n for n in collection.get_names() if n.startswith(PREFIX)]
    if names:
        collection.remove(names)

def main():
    check(pmx.model.is_open(), "a model must be open")
    for collection in (pmx.model.coordinate_systems, pmx.model.reference_points, pmx.model.surfaces,
                       pmx.model.node_sets):
        clean(collection)

    ns = pmx.model.node_sets.add(PREFIX + "NS",
        pmx.create.selection_by_ids("femodel", "nodes", "nodes", [1, 2, 3, 4]))
    check(ns.count == 4, "node set selection was not stored")

    # Equation strings are retained as model inputs and evaluate component-wise.
    rp = pmx.model.reference_points.add(PREFIX + "RP", ["=1", 2, "=3"])
    check(list(rp.get_coordinates()) == [1.0, 2.0, 3.0], "reference point equations did not evaluate")
    pmx.model.reference_points.hide(PREFIX + "RP")
    pmx.model.reference_points.replace(PREFIX + "RP", PREFIX + "RP2", ["=4", "=5", "=6"])
    check(not pmx.model.reference_points[PREFIX + "RP2"].visible, "reference point replace lost visibility")
    check(list(pmx.model.reference_points[PREFIX + "RP2"].get_coordinates()) == [4.0, 5.0, 6.0],
          "reference point replacement coordinates are wrong")

    one = pmx.create.selection_by_ids("femodel", "nodes", "nodes", [1])
    two = pmx.create.selection_by_ids("femodel", "nodes", "nodes", [1, 7])
    rp_sel = pmx.model.reference_points.add_from_selection(PREFIX + "RPS", "BetweenTwoPoints", two)
    check(rp_sel.created_from == "BetweenTwoPoints", "reference point selection mode was not stored")
    pmx.model.reference_points.replace_from_selection(PREFIX + "RPS", PREFIX + "RPS2", "OnPoint", one)
    check(pmx.model.reference_points[PREFIX + "RPS2"].created_from == "OnPoint",
          "reference point selection replacement failed")
    pmx.model.reference_points.replace_from_region(PREFIX + "RPS2", PREFIX + "RPR", "CenterOfGravity",
                                                    "NodeSetName", PREFIX + "NS")
    check(pmx.model.reference_points[PREFIX + "RPR"].created_from == "CenterOfGravity",
          "reference point region replacement failed")
    # A model side replacement keeps what it is not given, so the mode alone is a valid change
    pmx.model.reference_points.replace_from_region(PREFIX + "RPR", created_from="BoundingBoxCenter")
    check(pmx.model.reference_points[PREFIX + "RPR"].created_from == "BoundingBoxCenter",
          "the region mode alone was not applied")
    rp_keep = pmx.model.reference_points.add_from_selection(PREFIX + "RPKeep", "OnPoint", one)
    pmx.model.reference_points.replace_from_selection(PREFIX + "RPKeep", selection=one)
    check(pmx.model.reference_points[PREFIX + "RPKeep"].created_from == "OnPoint",
          "the omitted construction mode was not kept")
    pmx.model.reference_points.replace_from_selection(PREFIX + "RPKeep", created_from="BetweenTwoPoints",
                                                       selection=two)
    check(pmx.model.reference_points[PREFIX + "RPKeep"].created_from == "BetweenTwoPoints",
          "the selection replacement did not change the mode")
    pmx.model.reference_points.replace_from_selection(PREFIX + "RPKeep", created_from="BetweenTwoPoints")
    check(pmx.model.reference_points[PREFIX + "RPKeep"].created_from == "BetweenTwoPoints",
          "the kept selection was not reused")
    raises(lambda: pmx.model.reference_points.replace_from_selection(PREFIX + "RPKeep",
                                                                     created_from="OnPoint"),
           "a kept selection with the wrong number of points was accepted")
    raises(lambda: pmx.model.reference_points.replace_from_selection(PREFIX + "RP2", selection=one),
           "a coordinate point without a construction mode was accepted")
    raises(lambda: pmx.model.reference_points.add_from_region(PREFIX + "BadRP", "CenterOfGravity",
                                                               "NodeSetName", "Missing"),
           "missing reference point region was accepted")
    check(PREFIX + "BadRP" not in pmx.model.reference_points.get_names(),
          "invalid reference point changed the model")

    cs = pmx.model.coordinate_systems.add(PREFIX + "CS", ["=0", 0, 0], [1, 0, 0], [0, 1, 0],
                                           coordinate_system_type="Cylindrical")
    pmx.model.coordinate_systems.hide(PREFIX + "CS")
    pmx.model.coordinate_systems.replace(PREFIX + "CS", PREFIX + "CS2", origin=["=2", "=3", "=4"])
    check(pmx.model.coordinate_systems[PREFIX + "CS2"].coordinate_system_type == "Cylindrical",
          "coordinate system replacement reset cylindrical type")
    check(not pmx.model.coordinate_systems[PREFIX + "CS2"].visible,
          "coordinate system replacement lost visibility")
    check(list(pmx.model.coordinate_systems[PREFIX + "CS2"].get_origin()) == [2.0, 3.0, 4.0],
          "coordinate system equation origin is wrong")
    # A replacement is checked the way a creation is, so a degenerate axis is refused either way
    raises(lambda: pmx.model.coordinate_systems.replace(PREFIX + "CS2", point_on_x=[2, 3, 4]),
           "a replacement that collapses the X axis was accepted")
    check(list(pmx.model.coordinate_systems[PREFIX + "CS2"].get_point_on_x()) == [1.0, 0.0, 0.0],
          "a refused coordinate system replacement changed the model")

    # Mixed explicit and selected point definitions: explicit origin, selected X point. Node 2 of the
    # cube lies on the X axis, so the selected point completes a proper system
    on_x = pmx.create.selection_by_ids("femodel", "nodes", "nodes", [2])
    cs_mix = pmx.model.coordinate_systems.add_from_selections(
        PREFIX + "CSMix", point_on_x_created_from="OnPoint", point_on_x_selection=on_x,
        origin=["=0", "=0", "=0"], point_in_xy=["=0", "=1", "=0"])
    check(len(cs_mix.get_origin()) == 3 and len(cs_mix.get_point_on_x()) == 3,
          "mixed coordinate system definition was not created")
    check(list(cs_mix.get_point_on_x()) == [1.0, 0.0, 0.0],
          "the selected X point was not resolved on the mesh")
    # A selected point is resolved before the axes are checked, so node 1 sitting on the typed origin
    # is refused the way the dialog refuses it
    raises(lambda: pmx.model.coordinate_systems.add_from_selections(
                       PREFIX + "CSBad", point_on_x_created_from="OnPoint", point_on_x_selection=one,
                       origin=[0, 0, 0], point_in_xy=[0, 1, 0]),
           "a mixed definition whose selected X point sits on the origin was accepted")
    check(PREFIX + "CSBad" not in pmx.model.coordinate_systems.get_names(),
          "a refused mixed coordinate system was recorded")
    raises(lambda: pmx.model.coordinate_systems.replace_from_selections(
                       PREFIX + "CSMix", point_on_x_created_from="OnPoint", point_on_x_selection=one,
                       origin=[0, 0, 0], point_in_xy=[0, 1, 0]),
           "a replacement that collapses the X axis onto a selected point was accepted")
    check(list(pmx.model.coordinate_systems[PREFIX + "CSMix"].get_point_on_x()) == [1.0, 0.0, 0.0],
          "a refused mixed replacement changed the definition")
    # The XY point is reevaluated from its own selection, not from the one of the origin
    cs_xy = pmx.model.coordinate_systems.add_from_selections(
        PREFIX + "CSXY", origin=[0, 0, 0], point_on_x=[0, 1, 0],
        point_in_xy_created_from="OnPoint", point_in_xy_selection=on_x)
    check(list(cs_xy.get_point_in_xy()) == [1.0, 0.0, 0.0],
          "the selected XY point was not resolved on the mesh")

    surface = pmx.model.surfaces.add(PREFIX + "SU",
        pmx.create.selection_by_ids("femodel", "surfaces", "surfaces", {1: [1]}))
    pmx.model.surfaces.add_from_node_set(PREFIX + "SU_NS", PREFIX + "NS")
    pmx.model.surfaces.replace_from_node_set(PREFIX + "SU_NS", PREFIX + "SU_NS2", PREFIX + "NS")
    check(pmx.model.surfaces[PREFIX + "SU_NS2"].created_from == "NodeSet",
          "surface node set replacement failed")
    pmx.model.surfaces.replace_from_node_set(PREFIX + "SU_NS2", PREFIX + "SU_NS3")
    check(pmx.model.surfaces[PREFIX + "SU_NS3"].node_set_name == PREFIX + "NS",
          "the omitted node set was not kept")
    pmx.model.surfaces.replace(PREFIX + "SU", PREFIX + "SU2")
    check(pmx.model.surfaces[PREFIX + "SU2"].created_from == "Selection",
          "the omitted surface selection was not kept")
    raises(lambda: pmx.model.surfaces.replace(PREFIX + "SU_NS3"),
           "a node set surface replaced without a selection was accepted")
    raises(lambda: pmx.model.surfaces.replace_from_node_set(PREFIX + "SU2"),
           "a selection surface replaced without a node set was accepted")
    raises(lambda: pmx.model.surfaces.replace_from_node_set(PREFIX + "SU_NS3", PREFIX + "BadSU", "Missing"),
           "missing surface node set was accepted")
    check(PREFIX + "BadSU" not in pmx.model.surfaces.get_names(), "invalid surface changed the model")

    clean(pmx.model.coordinate_systems)
    clean(pmx.model.reference_points)
    clean(pmx.model.surfaces)
    clean(pmx.model.node_sets)
    print("TEST PASSED")

main()
