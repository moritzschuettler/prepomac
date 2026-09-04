#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the step scoped load collection: the 15 load types, the mechanical and the thermal step
#scoping, and the step scoped collection verbs. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Loads.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile

PREFIX = "PyTest_"
STEP = PREFIX + "Static"
NEXT_STEP = PREFIX + "Static_2"
THERMAL_STEP = PREFIX + "Thermal"
SURFACE = PREFIX + "Top"


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
    names = [n for n in pmx.model.steps.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.steps.remove(names)
    names = [n for n in pmx.model.surfaces.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.surfaces.remove(names)


def node_selection(ids):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', ids)


def surface_selection(face):
    return pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [face]})


def element_selection(ids):
    return pmx.create.selection_by_ids('femodel', 'elements', 'elements', ids)


def setup():
    pmx.model.surfaces.add(SURFACE, surface_selection(2))
    pmx.model.steps.add_static(STEP, time_period=1.0)
    pmx.model.steps.add_static(NEXT_STEP, time_period=1.0)
    pmx.model.steps.add_uncoupled_temp_disp(THERMAL_STEP)


def test_concentrated():
    loads = pmx.model.steps[STEP].loads
    check(loads.step_name == STEP, "the collection must carry the step name")
    check(len(loads) == 0, "a new step has no loads")
    #
    load = loads.add_concentrated_force(PREFIX + "F", node_selection([1, 2]), f3=-1000)
    check(load.load_type == "CLoad", "unexpected load type " + load.load_type)
    check(load.region_type == "NodeSetName", "unexpected region type " + load.region_type)
    check(load.get_value("f3") == -1000, "f3 was not set, got " + str(load.get_value("f3")))
    check(load.get_value("f1") == 0, "f1 must stay zero")
    check(load.amplitude_name == "Default", "unexpected amplitude name " + load.amplitude_name)
    check(load.active and load.visible, "a new load must be active and visible")
    # A value that is not given survives the replace
    load = loads.replace_concentrated_force(PREFIX + "F", f1=250)
    check(load.get_value("f1") == 250, "f1 was not replaced")
    check(load.get_value("f3") == -1000, "f3 must survive a replace")
    # A named region is accepted where a selection is
    load = loads.add_moment(PREFIX + "M", pmx.create.region('NodeSetName', load.region_name), m2=5.5)
    check(load.get_value("m2") == 5.5, "m2 was not set")
    check(load.region_type == "NodeSetName", "unexpected region type " + load.region_type)
    # The type cannot change on a replace
    check_raises(lambda: loads.replace_concentrated_force(PREFIX + "M"),
                 "replacing a moment by a concentrated force must be rejected")
    check_raises(lambda: loads.add_moment(PREFIX + "M", node_selection([1])),
                 "a duplicate name must be rejected")


def test_surface_loads():
    loads = pmx.model.steps[STEP].loads
    # Pressure
    load = loads.add_pressure(PREFIX + "P", surface_selection(1), 2.5)
    check(load.region_type == "SurfaceName", "unexpected region type " + load.region_type)
    check(load.get_value("magnitude") == 2.5, "the pressure was not set")
    check(load.get_value("distribution_name") == "Constant",
          "unexpected distribution name " + str(load.get_value("distribution_name")))
    check_raises(lambda: loads.add_pressure(PREFIX + "Bad", surface_selection(1), 1,
                                            distribution_name="NoSuchDistribution"),
                 "an unknown distribution must be rejected")
    # An equation is accepted where a value is
    pmx.model.parameters.add(PREFIX + "p", 1.25)
    load = loads.replace_pressure(PREFIX + "P", magnitude=PREFIX + "p * 2")
    check(load.get_value("magnitude") == 2.5, "the equation did not evaluate")
    # Hydrostatic pressure
    load = loads.add_hydrostatic_pressure(PREFIX + "HP", surface_selection(1),
                                          first_point=[0, 0, 0], second_point=[0, 0, 1],
                                          direction=[0, 0, -1], first_point_pressure=0,
                                          second_point_pressure=10, cutoff="Positive")
    check(load.get_value("second_point_pressure") == 10, "the second point pressure was not set")
    check(load.get_value("z2") == 1, "the second point was not set")
    check(load.get_value("hydrostatic_pressure_cutoff") == "Positive", "the cutoff was not set")
    check_raises(lambda: loads.add_hydrostatic_pressure(PREFIX + "Bad", surface_selection(1),
                                                        cutoff="NotACutoff"),
                 "an unknown cutoff must be rejected")
    check_raises(lambda: loads.add_hydrostatic_pressure(PREFIX + "Bad", surface_selection(1),
                                                        first_point=[0, 0]),
                 "a two value point must be rejected")
    # Surface traction: the force components and the force per area components are two independent
    # groups, and the model writes the one that matches the distribution
    load = loads.add_surface_traction(PREFIX + "ST", surface_selection(1), f1=100)
    check(load.get_value("f1") == 100, "f1 was not set")
    check(load.get_value("f_magnitude") == 100, "the force magnitude must follow the components")
    check(load.get_value("p1") == 0, "the force per area group must stay untouched")
    load = loads.replace_surface_traction(PREFIX + "ST", p2=3.5)
    check(load.get_value("p2") == 3.5, "p2 was not set")
    check(load.get_value("f1") == 100, "f1 must survive a replace")
    # Shell edge load
    load = loads.add_shell_edge_load(PREFIX + "SE", pmx.create.region('SurfaceName', SURFACE), 7.5)
    check(load.get_value("magnitude") == 7.5, "the shell edge magnitude was not set")
    check(load.region_name == SURFACE, "unexpected region name " + str(load.region_name))
    # Pre-tension
    load = loads.add_pre_tension(PREFIX + "PT", surface_selection(1), 500)
    check(load.get_value("force_magnitude") == 500, "the pre-tension force was not set")
    check(load.get_value("auto_compute_direction"), "the direction must be computed by default")
    load = loads.replace_pre_tension(PREFIX + "PT", direction=[0, 0, 1])
    check(not load.get_value("auto_compute_direction"), "a given direction turns off the computed one")
    load = loads.add_pre_tension(PREFIX + "PT_D", surface_selection(1), 0.5, load_type="Displacement")
    check(load.get_value("type") == "Displacement", "the pre-tension type was not set")
    check(load.get_value("displacement_magnitude") == 0.5, "the pre-tension displacement was not set")
    check_raises(lambda: loads.add_pre_tension(PREFIX + "Bad", surface_selection(1), 1,
                                               load_type="NotAType"),
                 "an unknown pre-tension type must be rejected")


def test_body_loads():
    loads = pmx.model.steps[STEP].loads
    load = loads.add_gravity(PREFIX + "G", element_selection([1]), f3=-9810)
    check(load.region_type == "ElementSetName", "unexpected region type " + load.region_type)
    check(load.get_value("f3") == -9810, "the gravity was not set")
    #
    load = loads.add_centrifugal(PREFIX + "C", element_selection([1]), axis_point=[0, 0, 0],
                                 axis_direction=[0, 0, 1], rotational_speed=100)
    check(load.get_value("rotational_speed") == 100, "the rotational speed was not set")
    check(load.get_value("n3") == 1, "the axis direction was not set")
    check_raises(lambda: loads.add_centrifugal(PREFIX + "Bad", element_selection([1]),
                                               axis_direction=[0, 1]),
                 "a two value axis direction must be rejected")


def test_imported_pressure(file_name):
    # Only the argument checks are covered here. An imported pressure that reaches the model is read
    # from its results file on every model check, so the accepting path needs a real .frd file.
    loads = pmx.model.steps[STEP].loads
    check_raises(lambda: loads.add_imported_pressure(PREFIX + "Bad", surface_selection(1),
                                                     "no_such_file.frd", "1", "PRESSURE"),
                 "a missing pressure file must be rejected")
    check_raises(lambda: loads.add_imported_pressure(PREFIX + "Bad", surface_selection(1), file_name,
                                                     "1", "PRESSURE",
                                                     interpolator_type="NotAnInterpolator"),
                 "an unknown interpolator type must be rejected")
    check_raises(lambda: loads.add_imported_pressure(PREFIX + "Bad", surface_selection(1), file_name,
                                                     None, "PRESSURE"),
                 "an imported pressure without a time must be rejected")
    check(not loads.contains(PREFIX + "Bad"), "a refused load must not reach the model")


def test_thermal_loads():
    mechanical = pmx.model.steps[STEP].loads
    # A static step carries no thermal load
    check_raises(lambda: mechanical.add_concentrated_flux(PREFIX + "Bad", node_selection([1]), 5),
                 "a concentrated flux in a static step must be rejected")
    #
    loads = pmx.model.steps[THERMAL_STEP].loads
    load = loads.add_concentrated_flux(PREFIX + "CF", node_selection([1]), 5, add_flux=True)
    check(load.get_value("magnitude") == 5, "the flux was not set")
    check(load.get_value("add_flux"), "add_flux was not set")
    #
    load = loads.add_surface_flux(PREFIX + "DF", surface_selection(1), 2.5)
    check(load.get_value("magnitude") == 2.5, "the surface flux was not set")
    #
    load = loads.add_body_flux(PREFIX + "BF", element_selection([1]), 1.5)
    check(load.get_value("magnitude") == 1.5, "the body flux was not set")
    #
    load = loads.add_film_heat_transfer(PREFIX + "FH", surface_selection(1), 20, 0.05)
    check(load.get_value("sink_temperature") == 20, "the sink temperature was not set")
    check(load.get_value("film_coefficient") == 0.05, "the film coefficient was not set")
    check_raises(lambda: loads.add_film_heat_transfer(PREFIX + "Bad", surface_selection(1), 20, 0.05,
                                                      coefficient_amplitude_name="NoSuchAmplitude"),
                 "an unknown coefficient amplitude must be rejected")
    #
    load = loads.add_radiation_heat_transfer(PREFIX + "RH", surface_selection(1), 20, 0.8)
    check(load.get_value("emissivity") == 0.8, "the emissivity was not set")
    check(not load.get_value("cavity_radiation"), "the cavity radiation must be off by default")
    load = loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_radiation=True,
                                                 cavity_name="Cavity-1")
    check(load.get_value("cavity_radiation"), "the cavity radiation was not set")
    # The solver limits a cavity name to three characters, and the model truncates it
    check(load.get_value("cavity_name") == "Cav",
          "unexpected cavity name " + str(load.get_value("cavity_name")))


def test_collection_verbs():
    loads = pmx.model.steps[STEP].loads
    name = PREFIX + "F"
    check(loads.contains(name), "the load is missing")
    check(loads[name].name == name, "indexing by name failed")
    check(loads.get(name).name == name, "get failed")
    check(len([l for l in loads]) == len(loads), "iteration failed")
    check_raises(lambda: loads["NoSuchLoad"], "an unknown name must be rejected")
    # The name is scoped to the step
    other = pmx.model.steps[NEXT_STEP].loads
    other.add_concentrated_force(name, node_selection([1]), f1=1)
    check(other.contains(name), "the name is scoped to the step")
    other.remove([name])
    # Rename, duplicate, remove
    loads.rename(name, name + "_R")
    check(loads.contains(name + "_R"), "the load was not renamed")
    loads.rename(name + "_R", name)
    count_before = len(loads)
    loads.duplicate([name])
    check(len(loads) == count_before + 1, "the load was not duplicated")
    duplicated = [n for n in loads.get_names() if n.startswith(name) and n != name]
    loads.remove(duplicated)
    check(len(loads) == count_before, "the duplicate was not removed")
    # Activate, deactivate, hide, show
    loads.deactivate([name])
    check(not loads[name].active, "the load was not deactivated")
    loads.activate([name])
    check(loads[name].active, "the load was not activated")
    loads.hide([name])
    check(not loads[name].visible, "the load was not hidden")
    loads.show([name])
    check(loads[name].visible, "the load was not shown")


def test_propagate():
    loads = pmx.model.steps[STEP].loads
    name = PREFIX + "F"
    following = pmx.model.steps[NEXT_STEP].loads
    check(not following.contains(name), "the following step must not carry the load yet")
    loads.propagate(name)
    check(following.contains(name), "the load was not propagated")
    check_raises(lambda: loads.propagate("NoSuchLoad"), "propagating an unknown name must be rejected")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    setup()
    test_concentrated()
    test_surface_loads()
    test_body_loads()
    # Any existing file proves the path check; the file is only read when the load is previewed
    file_name = os.path.join(tempfile.gettempdir(), "pytest_pressure.frd")
    open(file_name, "w").close()
    try:
        test_imported_pressure(file_name)
    finally:
        os.remove(file_name)
    test_thermal_loads()
    test_collection_verbs()
    test_propagate()
    clean()
    if pmx.model.parameters.contains(PREFIX + "p"):
        pmx.model.parameters.remove([PREFIX + "p"])
    print("TEST PASSED")


main()
