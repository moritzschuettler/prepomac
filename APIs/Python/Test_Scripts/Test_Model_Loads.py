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
SCALAR_DISTRIBUTION = PREFIX + "Scalar"
VECTOR_DISTRIBUTION = PREFIX + "Vector"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def check_refused(function, fragment, message):
    # The refusal must be the API's own, which names what is wrong
    try:
        function()
    except Exception as ex:
        check(fragment in str(ex), message + ": " + str(ex)[0:200])
        return
    raise Exception("TEST FAILED: " + message)


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def clean():
    names = [n for n in pmx.model.steps.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.steps.remove(names)
    names = [n for n in pmx.model.surfaces.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.surfaces.remove(names)
    # A section references a material, so the sections go first
    names = [n for n in pmx.model.sections.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.sections.remove(names)
    names = [n for n in pmx.model.materials.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.materials.remove(names)
    names = [n for n in pmx.model.distributions.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.distributions.remove(names)


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
    pmx.model.distributions.add_from_equation(SCALAR_DISTRIBUTION, "1 + 0.01 * x")
    pmx.model.distributions.add_from_equation(VECTOR_DISTRIBUTION, ["x", "y", "z"])


def test_concentrated():
    loads = pmx.model.steps[STEP].loads
    check(loads.step_name == STEP, "the collection must carry the step name")
    check(len(loads) == 0, "a new step has no loads")
    #
    load = loads.add_concentrated_force(PREFIX + "F", node_selection([1, 2]), f3=-1000)
    check(load.item_type == "concentrated_force", "unexpected load type " + load.item_type)
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
    # A named region the load dialog does not offer is refused before anything is recorded: a concentrated
    # force acts on nodes or on a reference point, not on a surface
    message = raised_message(lambda: loads.add_concentrated_force(PREFIX + "Bad",
                                                                  pmx.create.region('SurfaceName', SURFACE), f3=-1))
    check(message is not None and "SurfaceName" in message and "NodeSetName" in message,
          "a surface region must be refused for a concentrated force: " + str(message))
    check(not loads.contains(PREFIX + "Bad"), "a load with a refused region must not be recorded")
    # The type cannot change on a replace
    check_raises(lambda: loads.replace_concentrated_force(PREFIX + "M"),
                 "replacing a moment by a concentrated force must be rejected")
    check_raises(lambda: loads.add_moment(PREFIX + "M", node_selection([1])),
                 "a duplicate name must be rejected")
    # The default names are matched without regard to case, and read back as the model spells them
    load = loads.add_moment(PREFIX + "M2", node_selection([3]), m3=1, amplitude_name="default",
                            coordinate_system_name="GLOBAL")
    check(load.amplitude_name == "Default", "unexpected amplitude name " + load.amplitude_name)
    check(load.coordinate_system_name == "Global", "unexpected coordinate system name " + load.coordinate_system_name)
    loads.remove([PREFIX + "M2"])
    # A static step is not complex, so a phase would be kept without effect
    check_refused(lambda: loads.add_concentrated_force(PREFIX + "Bad", node_selection([1]), f1=1, phase_deg=30),
                  "'phase_deg'", "a phase in a static step must be refused")
    check_refused(lambda: loads.replace_concentrated_force(PREFIX + "F", phase_deg=30), "'phase_deg'",
                  "a phase given to a load of a static step must be refused")
    check(not loads.contains(PREFIX + "Bad"), "a load with a refused phase was recorded")


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
    # A pressure is a scalar, which the model would only find out when the deck is written
    message = raised_message(lambda: loads.add_pressure(PREFIX + "Bad", surface_selection(1), 1,
                                                        distribution_name=VECTOR_DISTRIBUTION))
    check(message is not None and VECTOR_DISTRIBUTION in message and "Scalar" in message,
          "a vector distribution of a pressure must be refused: " + str(message))
    check(not loads.contains(PREFIX + "Bad"), "the refused pressure was recorded")
    message = raised_message(lambda: loads.replace_pressure(PREFIX + "P", distribution_name=VECTOR_DISTRIBUTION))
    check(message is not None and VECTOR_DISTRIBUTION in message and "Scalar" in message,
          "a vector distribution of a replaced pressure must be refused: " + str(message))
    check(loads[PREFIX + "P"].get_value("distribution_name") == "Constant",
          "the refused replacement changed the distribution")
    load = loads.replace_pressure(PREFIX + "P", distribution_name=SCALAR_DISTRIBUTION)
    check(load.get_value("distribution_name") == SCALAR_DISTRIBUTION, "the scalar distribution was not set")
    load = loads.replace_pressure(PREFIX + "P", distribution_name="constant")
    check(load.get_value("distribution_name") == "Constant",
          "the default distribution given in other case did not read back as the model spells it")
    load = loads.replace_pressure(PREFIX + "P", distribution_name=SCALAR_DISTRIBUTION.upper())
    check(load.get_value("distribution_name") == SCALAR_DISTRIBUTION,
          "a distribution given in other case did not read back as the model spells it")
    # Hydrostatic pressure
    load = loads.add_hydrostatic_pressure(PREFIX + "HP", surface_selection(1),
                                          first_point=[0, 0, 0], second_point=[0, 0, 1],
                                          direction=[0, 0, -1], first_point_pressure=0,
                                          second_point_pressure=10, cutoff="Positive")
    check(load.get_value("second_point_pressure") == 10, "the second point pressure was not set")
    check(type(load.get_value("second_point")) is list, "a vector value must read back as a list: " +
          type(load.get_value("second_point")).__name__)
    check(load.get_value("second_point") == [0, 0, 1], "the second point was not set")
    check(load.get_value("cutoff") == "Positive", "the cutoff was not set")
    # The values read back under the names of the arguments that set them, not of the stored properties
    names = list(load.get_value_names())
    check("first_point" in names and "x1" not in names and "hydrostatic_pressure_cutoff" not in names,
          "the hydrostatic values are not named after the arguments: " + str(names))
    check_raises(lambda: loads.add_hydrostatic_pressure(PREFIX + "Bad", surface_selection(1),
                                                        cutoff="NotACutoff"),
                 "an unknown cutoff must be rejected")
    check_raises(lambda: loads.add_hydrostatic_pressure(PREFIX + "Bad", surface_selection(1),
                                                        first_point=[0, 0]),
                 "a two value point must be rejected")
    # Surface traction: the force components and the force per area components are two independent
    # groups, and the model writes only the one that matches the distribution, so the other is refused
    load = loads.add_surface_traction(PREFIX + "ST", surface_selection(1), f1=100)
    check(load.get_value("f1") == 100, "f1 was not set")
    check(load.get_value("f_magnitude") == 100, "the force magnitude must follow the components")
    check(load.get_value("p1") == 0, "the force per area group must stay untouched")
    check_refused(lambda: loads.replace_surface_traction(PREFIX + "ST", p2=3.5), "'p2'",
                  "a force per area without a distribution must be refused")
    check_refused(lambda: loads.add_surface_traction(PREFIX + "Bad", surface_selection(1), f1=1,
                                                     distribution_name=SCALAR_DISTRIBUTION), "'f1'",
                  "a force together with a distribution must be refused")
    check(not loads.contains(PREFIX + "Bad"), "the refused surface traction was recorded")
    load = loads.replace_surface_traction(PREFIX + "ST", p2=3.5, distribution_name=VECTOR_DISTRIBUTION)
    check(load.get_value("p2") == 3.5, "p2 was not set")
    check(load.get_value("f1") == 100, "f1 must survive a replace")
    check(load.get_value("distribution_name") == VECTOR_DISTRIBUTION, "the vector distribution was not set")
    # The group is checked against the distribution the load keeps
    check_refused(lambda: loads.replace_surface_traction(PREFIX + "ST", f1=5), "'f1'",
                  "a force for a surface traction that keeps its distribution must be refused")
    check(loads[PREFIX + "ST"].get_value("f1") == 100, "the refused replace changed f1")
    # A surface traction scales its components by either kind of distribution
    load = loads.replace_surface_traction(PREFIX + "ST", distribution_name=SCALAR_DISTRIBUTION)
    check(load.get_value("distribution_name") == SCALAR_DISTRIBUTION, "the scalar distribution was not set")
    load = loads.replace_surface_traction(PREFIX + "ST", f2=50, distribution_name="Constant")
    check(load.get_value("f2") == 50 and load.get_value("distribution_name") == "Constant",
          "a force together with the return to no distribution was not accepted")
    # Shell edge load
    load = loads.add_shell_edge_load(PREFIX + "SE", pmx.create.region('SurfaceName', SURFACE), 7.5)
    check(load.get_value("magnitude") == 7.5, "the shell edge magnitude was not set")
    check(load.region_name == SURFACE, "unexpected region name " + str(load.region_name))
    # Pre-tension
    load = loads.add_pre_tension(PREFIX + "PT", surface_selection(1), 500)
    check(load.get_value("magnitude") == 500, "the pre-tension force was not set")
    check(load.get_value("auto_direction"), "the direction must be computed by default")
    load = loads.replace_pre_tension(PREFIX + "PT", direction=[0, 0, 1])
    check(not load.get_value("auto_direction"), "a given direction turns off the computed one")
    check(load.get_value("direction") == [0, 0, 1], "the pre-tension direction was not set")
    # auto_direction returns to the surface normal, and cannot be combined with a direction
    check_raises(lambda: loads.replace_pre_tension(PREFIX + "PT", direction=[0, 0, 1], auto_direction=True),
                 "a direction together with auto_direction must be rejected")
    load = loads.replace_pre_tension(PREFIX + "PT", auto_direction=True)
    check(load.get_value("auto_direction"), "auto_direction did not return to the surface normal")
    check(load.get_value("magnitude") == 500, "the force must survive the direction change")
    # Stopping the surface normal needs the direction that replaces it
    check_raises(lambda: loads.replace_pre_tension(PREFIX + "PT", auto_direction=False),
                 "auto_direction=False without a direction must be rejected")
    check(loads[PREFIX + "PT"].get_value("auto_direction"), "the refused replacement changed the load")
    # A type change without a magnitude would take the value stored for the other type, so it is refused
    check_raises(lambda: loads.replace_pre_tension(PREFIX + "PT", tension_type="Displacement"),
                 "a pre-tension type change without a magnitude must be rejected")
    check(loads[PREFIX + "PT"].get_value("tension_type") == "Force", "the refused replacement changed the type")
    load = loads.replace_pre_tension(PREFIX + "PT", tension_type="Displacement", magnitude=0.2)
    check(load.get_value("tension_type") == "Displacement", "the pre-tension type was not replaced")
    check(load.get_value("magnitude") == 0.2, "the pre-tension displacement was not replaced")
    # The type that is already set needs no magnitude
    load = loads.replace_pre_tension(PREFIX + "PT", tension_type="Displacement")
    check(load.get_value("magnitude") == 0.2, "the displacement must survive a replace")
    load = loads.add_pre_tension(PREFIX + "PT_D", surface_selection(1), 0.5, tension_type="Displacement")
    check(load.get_value("tension_type") == "Displacement", "the pre-tension type was not set")
    check(load.get_value("magnitude") == 0.5, "the pre-tension displacement was not set")
    check_raises(lambda: loads.add_pre_tension(PREFIX + "Bad", surface_selection(1), 1,
                                               tension_type="NotAType"),
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
    check(list(load.get_value("axis_direction")) == [0, 0, 1], "the axis direction was not set")
    # The load refuses a negative speed without naming the argument
    check_refused(lambda: loads.replace_centrifugal(PREFIX + "C", rotational_speed=-1), "'rotational_speed'",
                  "a negative rotational speed must be refused")
    check(loads[PREFIX + "C"].get_value("rotational_speed") == 100, "the refused replace changed the speed")
    check_raises(lambda: loads.add_centrifugal(PREFIX + "Bad", element_selection([1]),
                                               axis_direction=[0, 1]),
                 "a two value axis direction must be rejected")
    test_mass_section_region()


# A gravity and a centrifugal load may act on a mass section instead of a mesh region, which is a
# region type no other item accepts and which the CalculiX writer treats differently
def test_mass_section_region():
    loads = pmx.model.steps[STEP].loads
    sections = pmx.model.sections
    materials = pmx.model.materials
    if not materials.contains(PREFIX + "Mat"):
        materials.add(PREFIX + "Mat", pmx.create.elastic(210000, 0.3))
    if not sections.contains(PREFIX + "PointMass"):
        sections.add_point_mass(PREFIX + "PointMass",
                                pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1]), 0.5)
    if not sections.contains(PREFIX + "SolidSec"):
        sections.add_solid(PREFIX + "SolidSec", PREFIX + "Mat", element_selection([1]))
    mass_region = pmx.create.region('MassSection', PREFIX + "PointMass")
    #
    load = loads.add_gravity(PREFIX + "G_M", mass_region, f3=-9810)
    check(load.region_type == "MassSection", "unexpected region type " + load.region_type)
    check(load.region_name == PREFIX + "PointMass", "unexpected region name " + str(load.region_name))
    load = loads.add_centrifugal(PREFIX + "C_M", mass_region, axis_point=[0, 0, 0],
                                 axis_direction=[0, 0, 1], rotational_speed=50)
    check(load.region_type == "MassSection", "the centrifugal load lost the mass section region")
    # A section that is not a mass section, and a section that does not exist, are both refused
    check_raises(lambda: loads.add_gravity(PREFIX + "Bad",
                                           pmx.create.region('MassSection', PREFIX + "SolidSec"), f3=-1),
                 "a solid section must not be usable as a mass section region")
    check_raises(lambda: loads.add_gravity(PREFIX + "Bad",
                                           pmx.create.region('MassSection', 'NoSuchSection'), f3=-1),
                 "an unknown mass section must be rejected")
    check(not loads.contains(PREFIX + "Bad"), "a rejected load must not be recorded")
    # Only the two loads that act on a mass accept the region type
    check_raises(lambda: loads.add_pressure(PREFIX + "Bad", mass_region, 1),
                 "a pressure must not accept a mass section region")


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
    # A thermal load is never complex, so it takes no phase
    check_refused(lambda: loads.add_concentrated_flux(PREFIX + "Bad", node_selection([1]), 5, phase_deg=10),
                  "'phase_deg'", "a phase of a thermal load must be refused")
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
    # The solver limits a cavity name to three characters, and the model would cut a longer one
    check_refused(lambda: loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_radiation=True,
                                                                cavity_name="Cavity-1"),
                  "'cavity_name'", "a cavity name longer than three characters must be refused")
    check(not loads[PREFIX + "RH"].get_value("cavity_radiation"), "the refused replace changed the load")
    load = loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_radiation=True, cavity_name="Cav")
    check(load.get_value("cavity_radiation"), "the cavity radiation was not set")
    check(load.get_value("cavity_name") == "Cav",
          "unexpected cavity name " + str(load.get_value("cavity_name")))
    # The model would clamp an emissivity outside 0 to 1
    for emissivity in [1.5, -0.1]:
        check_refused(lambda: loads.replace_radiation_heat_transfer(PREFIX + "RH", emissivity=emissivity),
                      "'emissivity'", "the emissivity " + str(emissivity) + " must be refused")
    check(loads[PREFIX + "RH"].get_value("emissivity") == 0.8, "a refused replace changed the emissivity")


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
    # The thermal step is the last one, so a propagation from it would record a command that copies nothing
    last = pmx.model.steps[THERMAL_STEP].loads
    check(last.contains(PREFIX + "CF"), "the last step must carry the concentrated flux")
    check_refused(lambda: last.propagate(PREFIX + "CF"), "no following step",
                  "propagating from the last step must be refused")
    # The controller would skip a following step that does not take the load type, so the propagation is refused
    frequency = PREFIX + "Frequency"
    pmx.model.steps.add_frequency(frequency)
    try:
        check_refused(lambda: loads.propagate(PREFIX + "P"), frequency,
                      "propagating into a step that does not take the load type must be refused")
        check(not following.contains(PREFIX + "P"), "the refused propagation was recorded")
    finally:
        pmx.model.steps.remove([frequency])


def test_complex_phase():
    # A steady state dynamics step makes its loads complex, and only a complex load takes a phase
    steps = pmx.model.steps
    name = PREFIX + "SteadyState"
    steps.add_steady_state_dynamics(name)
    try:
        # The step is found without regard to case, and its loads are recorded under the name the model uses
        loads = steps[name.lower()].loads
        check(loads.step_name == name, "unexpected step name " + loads.step_name)
        load = loads.add_concentrated_force(PREFIX + "FC", node_selection([1]), f1=1, phase_deg=30)
        check(loads.contains(PREFIX + "FC"), "a load with a phase was not added to a complex step")
        # The phase reads back under its argument name, and goes back into it
        check(load.get_value("phase_deg") == 30, "unexpected phase " + str(load.get_value("phase_deg")))
        check("phase_deg" in list(load.get_value_names()), "phase_deg must be listed")
        load = loads.replace_concentrated_force(PREFIX + "FC", phase_deg="=15 * 2")
        check("phase_deg" in load.get_equations(), "the phase was not replaced: " + str(load.get_equations()))
        check(load.get_value("phase_deg") == 30, "the phase expression was not evaluated")
        equation = load.get_equations()["phase_deg"]
        load = loads.replace_concentrated_force(PREFIX + "FC", phase_deg=equation)
        check(load.get_equations()["phase_deg"] == equation and load.get_value("phase_deg") == 30,
              "the read back phase did not go back: " + str(load.get_equations()))
    finally:
        steps.remove([name])


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
    test_complex_phase()
    clean()
    if pmx.model.parameters.contains(PREFIX + "p"):
        pmx.model.parameters.remove([PREFIX + "p"])
    print("TEST PASSED")


main()
