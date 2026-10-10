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
import shutil
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


def part_selection():
    # The gravity, centrifugal and body flux dialogs pick parts
    return pmx.create.selection_by_ids('femodel', 'parts', 'parts', [pmx.model.parts.first().id])


def setup():
    pmx.model.surfaces.add(SURFACE, surface_selection(2))
    pmx.model.steps.add_static(STEP)
    pmx.model.steps.add_static(NEXT_STEP)
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
    # A name with surrounding spaces is no name, here as in every collection
    check_refused(lambda: loads.add_moment(PREFIX + "Bad", node_selection([3]), m3=1, amplitude_name="Default "),
                  "does not exist", "a padded amplitude name must name no amplitude")
    check_refused(lambda: loads.add_concentrated_force(PREFIX + "Bad", node_selection([3]), f1=1,
                                                       coordinate_system_name=" Global"),
                  "does not exist", "a padded coordinate system name must name no coordinate system")
    check(not loads.contains(PREFIX + "Bad"), "a load with a padded name was recorded")
    # A static step is not complex, so a phase would be kept without effect
    check_refused(lambda: loads.add_concentrated_force(PREFIX + "Bad", node_selection([1]), f1=1, phase_deg=30),
                  "'phase_deg'", "a phase in a static step must be refused")
    check_refused(lambda: loads.replace_concentrated_force(PREFIX + "F", phase_deg=30), "'phase_deg'",
                  "a phase given to a load of a static step must be refused")
    check(not loads.contains(PREFIX + "Bad"), "a load with a refused phase was recorded")
    # What the phase_deg argument refuses is not read back either, so a read value always goes back
    check("phase_deg" not in list(loads[PREFIX + "F"].get_value_names()),
          "a load of a static step must not read back a phase")
    check("phase_deg" not in loads[PREFIX + "F"].get_equations(), "a load of a static step reported a phase")


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
    # A direction of zero length is no direction, which the dialog refuses as well
    count = len(loads)
    message = raised_message(lambda: loads.replace_hydrostatic_pressure(PREFIX + "HP", direction=[0, 0, 0]))
    check(message is not None and "'direction'" in message and "non-zero" in message,
          "a zero hydrostatic direction must be refused naming the argument: " + str(message))
    check(loads[PREFIX + "HP"].get_value("direction") == [0, 0, -1], "a refused replace changed the direction")
    message = raised_message(lambda: loads.add_pre_tension(PREFIX + "Bad", surface_selection(1), 1,
                                                           direction=[0, 0, 0]))
    check(message is not None and "'direction'" in message and "non-zero" in message,
          "a zero pre-tension direction must be refused naming the argument: " + str(message))
    check(len(loads) == count, "a refused load was recorded")
    # Surface traction: the force components and the force per area components are two independent
    # groups, and the model writes only the one that matches the distribution, so the other is refused
    load = loads.add_surface_traction(PREFIX + "ST", surface_selection(1), f1=100)
    check(load.get_value("f1") == 100, "f1 was not set")
    # Only what replace_surface_traction takes reads back: the derived magnitudes and the force per area group a
    # surface traction without a distribution refuses are left out
    names = list(load.get_value_names())
    check(names == ["f1", "f2", "f3", "distribution_name"],
          "a surface traction without a distribution read back other values: " + str(names))
    check_refused(lambda: loads.replace_surface_traction(PREFIX + "ST", p2=3.5), "'p2'",
                  "a force per area without a distribution must be refused")
    check_refused(lambda: loads.add_surface_traction(PREFIX + "Bad", surface_selection(1), f1=1,
                                                     distribution_name=SCALAR_DISTRIBUTION), "'f1'",
                  "a force together with a distribution must be refused")
    check(not loads.contains(PREFIX + "Bad"), "the refused surface traction was recorded")
    load = loads.replace_surface_traction(PREFIX + "ST", p2=3.5, distribution_name=VECTOR_DISTRIBUTION)
    check(load.get_value("p2") == 3.5, "p2 was not set")
    # The force a surface traction with a distribution does not use is kept, but not read back, as the
    # argument would refuse it
    check("f1" not in list(load.get_value_names()) and "p1" in list(load.get_value_names()),
          "a surface traction with a distribution must read back the force per area only")
    check(load.get_value("distribution_name") == VECTOR_DISTRIBUTION, "the vector distribution was not set")
    # The group is checked against the distribution the load keeps
    check_refused(lambda: loads.replace_surface_traction(PREFIX + "ST", f1=5), "'f1'",
                  "a force for a surface traction that keeps its distribution must be refused")
    check(loads[PREFIX + "ST"].get_value("p2") == 3.5, "the refused replace changed the load")
    # A surface traction scales its components by either kind of distribution
    load = loads.replace_surface_traction(PREFIX + "ST", distribution_name=SCALAR_DISTRIBUTION)
    check(load.get_value("distribution_name") == SCALAR_DISTRIBUTION, "the scalar distribution was not set")
    load = loads.replace_surface_traction(PREFIX + "ST", f2=50, distribution_name="Constant")
    check(load.get_value("f2") == 50 and load.get_value("distribution_name") == "Constant",
          "a force together with the return to no distribution was not accepted")
    check(load.get_value("f1") == 100, "f1 must survive the replaces with a distribution")
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
    # The add takes auto_direction, which get_value reads back, with the rules of the replace
    count = len(loads)
    load = loads.add_pre_tension(PREFIX + "PT_A", surface_selection(1), 100, auto_direction=True)
    check(load.get_value("auto_direction"), "auto_direction=True must compute the direction")
    load = loads.add_pre_tension(PREFIX + "PT_F", surface_selection(1), 100, direction=[0, 0, 1],
                                 auto_direction=False)
    check(not load.get_value("auto_direction") and load.get_value("direction") == [0, 0, 1],
          "auto_direction=False with a direction must keep the direction")
    for arguments, argument in [({"direction": [0, 0, 1], "auto_direction": True}, "'auto_direction'"),
                                ({"auto_direction": False}, "'direction'")]:
        message = raised_message(lambda: loads.add_pre_tension(PREFIX + "Bad", surface_selection(1), 1, **arguments))
        check(message is not None and argument in message,
              "an add with " + str(arguments) + " must be refused naming " + argument + ": " + str(message))
    check(len(loads) == count + 2, "a refused pre-tension was recorded")
    # A replace without a name keeps the load under its own spelling, whatever spelling old_name has
    for spelling in [(PREFIX + "PT_A").lower(), (PREFIX + "PT_A").upper()]:
        load = loads.replace_pre_tension(spelling, magnitude=200)
        check(load.name == PREFIX + "PT_A" and PREFIX + "PT_A" in list(loads.get_names()),
              "a replace of '" + spelling + "' changed the stored spelling: " + str(list(loads.get_names())))
    check(len(loads) == count + 2, "a mixed case replace changed the number of loads")
    loads.remove([PREFIX + "PT_A", PREFIX + "PT_F"])


def test_body_loads():
    loads = pmx.model.steps[STEP].loads
    load = loads.add_gravity(PREFIX + "G", part_selection(), f3=-9810)
    check(load.region_type == "ElementSetName", "unexpected region type " + load.region_type)
    # The picked part becomes an element set made of it, which covers every element of the part
    part_element_ids = sorted(pmx.model.parts.first().get_element_ids())
    element_ids = sorted(pmx.model.element_sets[load.region_name].get_element_ids())
    check(element_ids == part_element_ids, "the part selection resolved to the elements " + str(element_ids))
    check(load.get_value("f3") == -9810, "the gravity was not set")
    #
    load = loads.add_centrifugal(PREFIX + "C", part_selection(), axis_point=[0, 0, 0],
                                 axis_direction=[0, 0, 1], rotational_speed=100)
    check(load.get_value("rotational_speed") == 100, "the rotational speed was not set")
    check(list(load.get_value("axis_direction")) == [0, 0, 1], "the axis direction was not set")
    # The load refuses a negative speed without naming the argument
    check_refused(lambda: loads.replace_centrifugal(PREFIX + "C", rotational_speed=-1), "'rotational_speed'",
                  "a negative rotational speed must be refused")
    check(loads[PREFIX + "C"].get_value("rotational_speed") == 100, "the refused replace changed the speed")
    check_raises(lambda: loads.add_centrifugal(PREFIX + "Bad", part_selection(),
                                               axis_direction=[0, 1]),
                 "a two value axis direction must be rejected")
    # An axis of zero length is no axis, which the dialog refuses as well
    message = raised_message(lambda: loads.add_centrifugal(PREFIX + "Bad", part_selection(), axis_point=[0, 0, 0],
                                                           axis_direction=[0, 0, 0], rotational_speed=1))
    check(message is not None and "'axis_direction'" in message and "non-zero" in message,
          "a zero centrifugal axis must be refused naming the argument: " + str(message))
    check(not loads.contains(PREFIX + "Bad"), "a refused centrifugal load was recorded")
    test_body_load_regions()
    test_mass_section_region()


# The element set the controller makes from the selection of a gravity, a centrifugal or a body flux load is
# marked as made of parts, so an element selection would be read as part IDs; the API takes the part
# selection the dialog records, and a named element set or part
def test_body_load_regions():
    loads = pmx.model.steps[STEP].loads
    thermal = pmx.model.steps[THERMAL_STEP].loads
    part = pmx.model.parts.first()
    part_element_ids = sorted(part.get_element_ids())
    added = [(loads, loads.add_gravity(PREFIX + "G_Part", part_selection(), f3=-1)),
             (loads, loads.add_centrifugal(PREFIX + "C_Part", part_selection(), axis_point=[0, 0, 0],
                                           axis_direction=[0, 0, 1], rotational_speed=1)),
             (thermal, thermal.add_body_flux(PREFIX + "BF_Part", part_selection(), 1))]
    for collection, load in added:
        check(load.region_type == "ElementSetName", load.name + " has the region type " + load.region_type)
        element_ids = sorted(pmx.model.element_sets[load.region_name].get_element_ids())
        check(element_ids == part_element_ids, load.name + " resolved to the elements " + str(element_ids))
    load = loads.replace_gravity(PREFIX + "G_Part", region=part_selection())
    check(sorted(pmx.model.element_sets[load.region_name].get_element_ids()) == part_element_ids,
          "the replaced part selection did not resolve to the elements of the part")
    # A named part or element set is what the dialog offers besides the selection
    load = loads.replace_gravity(PREFIX + "G_Part", region=pmx.create.region('PartName', part.name))
    check(load.region_type == "PartName" and load.region_name == part.name, "the part region was not set")
    for function in [lambda: loads.add_gravity(PREFIX + "Bad", element_selection([1]), f3=-1),
                     lambda: loads.replace_centrifugal(PREFIX + "C_Part", region=element_selection([1])),
                     lambda: thermal.add_body_flux(PREFIX + "Bad", element_selection([1]), 1)]:
        check_refused(function, "requires target 'parts'", "an element selection must be refused naming the target")
    check_refused(lambda: loads.add_gravity(PREFIX + "Bad", pmx.create.region('NodeSetName', 'AnySet'), f3=-1),
                  "NodeSetName region was given", "a node set region must be refused")
    check(not loads.contains(PREFIX + "Bad") and not thermal.contains(PREFIX + "Bad"),
          "a refused load was recorded")
    for collection, load in added:
        collection.remove(load.name)


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


def write_open_foam_case(directory, with_field):
    """An OpenFOAM case with no mesh: the .foam file and, when asked, the scalar field p at time 1, which is
    all the dialog looks at before it accepts the load."""
    os.makedirs(directory)
    case_file = os.path.join(directory, "case.foam")
    open(case_file, "w").close()
    if with_field:
        time_directory = os.path.join(directory, "1")
        os.mkdir(time_directory)
        with open(os.path.join(time_directory, "p"), "w") as writer:
            writer.write("FoamFile\n{\n    version 2.0;\n    format ascii;\n    class volScalarField;\n"
                         "    object p;\n}\n")
    return case_file


def test_imported_pressure(directory):
    # Only the refusals are covered here. An imported pressure that reaches the model is read from its OpenFOAM
    # case on every model check, and the repository holds no OpenFOAM case with a mesh.
    loads = pmx.model.steps[STEP].loads
    empty_case = write_open_foam_case(os.path.join(directory, "empty"), False)
    case = write_open_foam_case(os.path.join(directory, "case"), True)
    check_raises(lambda: loads.add_imported_pressure(PREFIX + "Bad", surface_selection(1),
                                                     "no_such_file.foam", "1", "p"),
                 "a missing pressure file must be rejected")
    check_raises(lambda: loads.add_imported_pressure(PREFIX + "Bad", surface_selection(1), case,
                                                     "1", "p", interpolator_type="NotAnInterpolator"),
                 "an unknown interpolator type must be rejected")
    check_raises(lambda: loads.add_imported_pressure(PREFIX + "Bad", surface_selection(1), case,
                                                     None, "p"),
                 "an imported pressure without a time must be rejected")
    # The dialog refuses a case that does not hold the field, in these words, and so does the API
    for file_name, time, variable, fragment, argument in [
            (empty_case, "1", "p", "does not contain results.", "'file_name'"),
            (case, "2", "p", "does not contain results for time: 2.", "'pressure_time'"),
            (case, "1", "PRESSURE", "does not contain results for variable: PRESSURE.", "'pressure_variable_name'")]:
        message = raised_message(lambda: loads.add_imported_pressure(PREFIX + "Bad", surface_selection(1),
                                                                     file_name, time, variable))
        check(message is not None and fragment in message and argument in message,
              "an imported pressure the case does not hold must be refused naming " + argument + ": " + str(message))
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
    load = loads.add_body_flux(PREFIX + "BF", part_selection(), 1.5)
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
    # The solver input names the cavity of a cavity radiation, and a name without cavity radiation is unused
    check_refused(lambda: loads.add_radiation_heat_transfer(PREFIX + "Bad", surface_selection(1), 20, 0.8,
                                                            cavity_radiation=True),
                  "'cavity_name'", "a cavity radiation without a cavity name must be refused")
    check_refused(lambda: loads.add_radiation_heat_transfer(PREFIX + "Bad", surface_selection(1), 20, 0.8,
                                                            cavity_name="Cav"),
                  "cavity_radiation=True", "a cavity name without cavity radiation must be refused")
    check(not loads.contains(PREFIX + "Bad"), "a refused radiation load was recorded")
    # The dialog keeps the name when cavity radiation is turned off, and so does a replace, whose journal
    # leaves the kept name out
    load = loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_radiation=False)
    check(not load.get_value("cavity_radiation"), "the cavity radiation was not turned off")
    # The kept name is not read back, as cavity_name would refuse it without cavity radiation
    check("cavity_name" not in list(load.get_value_names()),
          "a radiation without cavity radiation read back its cavity name")
    check_refused(lambda: loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_name="Cv2"),
                  "cavity_radiation=True", "a cavity name for a load without cavity radiation must be refused")
    # The merged load is checked, so turning cavity radiation on again finds the kept name
    load = loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_radiation=True)
    check(load.get_value("cavity_name") == "Cav", "the kept cavity name was not used")
    # The model would clamp an emissivity outside 0 to 1
    for emissivity in [1.5, -0.1]:
        check_refused(lambda: loads.replace_radiation_heat_transfer(PREFIX + "RH", emissivity=emissivity),
                      "'emissivity'", "the emissivity " + str(emissivity) + " must be refused")
    check(loads[PREFIX + "RH"].get_value("emissivity") == 0.8, "a refused replace changed the emissivity")


# A replace keeps a value given as None, and the load factory follows it, so an add refused a None for a value it
# needs only when it reached the model - which took it as zero
# The equation setter of a container throws on an equation it cannot evaluate, with a message that names no
# argument, so the API refuses it naming the argument, a component of a vector by its index
def test_invalid_equations():
    loads = pmx.model.steps[STEP].loads
    count_before = len(loads)
    check_refused(lambda: loads.add_concentrated_force(PREFIX + "Bad", node_selection([1]), f1="=1+"), "'f1'",
                  "an equation that does not parse must be refused naming f1")
    check_refused(lambda: loads.add_pressure(PREFIX + "Bad", surface_selection(1), "=undefined_param"),
                  "'magnitude'", "an equation naming an undefined parameter must be refused naming magnitude")
    check_refused(lambda: loads.add_centrifugal(PREFIX + "Bad", part_selection(), axis_point=[0, "=1+", 0],
                                                axis_direction=[0, 0, 1], rotational_speed=1),
                  "'axis_point[1]'", "a vector component that cannot be evaluated must be refused by its index")
    check_refused(lambda: loads.replace_concentrated_force(PREFIX + "F", f2="=undefined_param"), "'f2'",
                  "a replace with an equation that cannot be evaluated must be refused naming f2")
    check(len(loads) == count_before and not loads.contains(PREFIX + "Bad"),
          "a load with an invalid equation was recorded")
    check(loads[PREFIX + "F"].get_value("f2") == 0, "the refused replace changed f2")


# The dialog refuses on OK a load that would do nothing, and the API refuses it in the dialog's words, naming the
# arguments, before anything is recorded; it judges the load an add or a replace ends with and the value an
# expression evaluates to
def test_values_the_dialog_refuses():
    loads = pmx.model.steps[STEP].loads
    count_before = len(loads)
    for function, fragments in [
            (lambda: loads.add_concentrated_force(PREFIX + "Bad", node_selection([1])),
             ["At least one force component must not be equal to 0", "'f1'"]),
            (lambda: loads.add_concentrated_force(PREFIX + "Bad", node_selection([1]), f2="=2 - 2"),
             ["At least one force component", "'f3'"]),
            (lambda: loads.add_moment(PREFIX + "Bad", node_selection([1]), m3=0),
             ["At least one moment component must not be equal to 0", "'m1'"]),
            (lambda: loads.add_pressure(PREFIX + "Bad", surface_selection(1), 0),
             ["The pressure load magnitude must not be equal to 0", "'magnitude'"]),
            (lambda: loads.add_pressure(PREFIX + "Bad", surface_selection(1), "=1 - 1"),
             ["The pressure load magnitude", "'magnitude'"]),
            (lambda: loads.add_hydrostatic_pressure(PREFIX + "Bad", surface_selection(1), second_point=[0, 0, 1]),
             ["The pressure direction is not properly defined", "'direction'"]),
            (lambda: loads.add_hydrostatic_pressure(PREFIX + "Bad", surface_selection(1), first_point=[0, 0, 0],
                                                    second_point=[1, 0, 0], direction=[0, 0, -1]),
             ["must not be on a plane perpendicular", "'first_point'"]),
            (lambda: loads.add_surface_traction(PREFIX + "Bad", surface_selection(1)),
             ["At least one surface traction load component must not be equal to 0", "'f1'"]),
            (lambda: loads.add_surface_traction(PREFIX + "Bad", surface_selection(1),
                                                distribution_name=SCALAR_DISTRIBUTION),
             ["At least one surface traction load component", "'p1'"]),
            (lambda: loads.add_shell_edge_load(PREFIX + "Bad", surface_selection(1), 0),
             ["The shell edge load magnitude must not be equal to 0", "'magnitude'"]),
            (lambda: loads.add_gravity(PREFIX + "Bad", part_selection()),
             ["At least one gravity load component must not be equal to 0", "'f1'"]),
            (lambda: loads.add_centrifugal(PREFIX + "Bad", part_selection(), rotational_speed=10),
             ["At least one axis direction component must not be equal to 0", "'axis_direction'"]),
            (lambda: loads.add_centrifugal(PREFIX + "Bad", part_selection(), axis_direction=[0, 0, 1]),
             ["Rotational speed must not be equal to 0", "'rotational_speed'"]),
            (lambda: loads.add_pre_tension(PREFIX + "Bad", surface_selection(1), 0),
             ["Pre-tension magnitude must not be equal to 0", "'magnitude'"]),
            # A replace is judged on the load it ends with
            (lambda: loads.replace_concentrated_force(PREFIX + "F", f1=0, f3="=0"),
             ["At least one force component", "'f1'"]),
            (lambda: loads.replace_centrifugal(PREFIX + "C", rotational_speed=0),
             ["Rotational speed must not be equal to 0", "'rotational_speed'"])]:
        message = raised_message(function)
        check(message is not None and all(fragment in message for fragment in fragments),
              "a load the dialog refuses must be refused with " + str(fragments) + ": " + str(message))
    check(len(loads) == count_before and not loads.contains(PREFIX + "Bad"), "a refused load was recorded")
    check(loads[PREFIX + "F"].get_value("f3") == -1000, "a refused replace changed the force")
    check(loads[PREFIX + "C"].get_value("rotational_speed") == 100, "a refused replace changed the speed")
    # The dialog takes a pre-tension displacement of 0, so the API does as well
    load = loads.add_pre_tension(PREFIX + "PT_Zero", surface_selection(1), 0, tension_type="Displacement")
    check(load.get_value("magnitude") == 0, "a pre-tension displacement of 0 must be accepted")
    loads.remove([PREFIX + "PT_Zero"])


def test_required_values():
    loads = pmx.model.steps[STEP].loads
    thermal = pmx.model.steps[THERMAL_STEP].loads
    count_before = len(loads)
    thermal_count_before = len(thermal)
    for function, argument in [
            (lambda: loads.add_pressure(PREFIX + "Bad", surface_selection(1), None), "'magnitude'"),
            (lambda: loads.add_shell_edge_load(PREFIX + "Bad", surface_selection(1), None), "'magnitude'"),
            (lambda: loads.add_pre_tension(PREFIX + "Bad", surface_selection(1), None), "'magnitude'"),
            (lambda: thermal.add_concentrated_flux(PREFIX + "Bad", node_selection([1]), None), "'magnitude'"),
            (lambda: thermal.add_surface_flux(PREFIX + "Bad", surface_selection(1), None), "'magnitude'"),
            (lambda: thermal.add_body_flux(PREFIX + "Bad", part_selection(), None), "'magnitude'"),
            (lambda: thermal.add_film_heat_transfer(PREFIX + "Bad", surface_selection(1), None, 0.05),
             "'sink_temperature'"),
            (lambda: thermal.add_film_heat_transfer(PREFIX + "Bad", surface_selection(1), 20, None),
             "'film_coefficient'"),
            (lambda: thermal.add_radiation_heat_transfer(PREFIX + "Bad", surface_selection(1), None, 0.8),
             "'sink_temperature'"),
            (lambda: thermal.add_radiation_heat_transfer(PREFIX + "Bad", surface_selection(1), 20, None),
             "'emissivity'")]:
        check_refused(function, argument, "a required value given as None must be refused naming " + argument)
    check(len(loads) == count_before and len(thermal) == thermal_count_before, "a refused load was recorded")
    # On a replace None keeps the value
    load = loads.add_pressure(PREFIX + "P_None", surface_selection(1), 3)
    load = loads.replace_pressure(PREFIX + "P_None", magnitude=None)
    check(load.get_value("magnitude") == 3, "a replace with magnitude=None must keep the magnitude")
    loads.remove([PREFIX + "P_None"])


# The solver input transforms only the nodes of a concentrated force and of a surface traction into a coordinate
# system, and the dialog offers one for those two only, so any other load would keep it without effect
def test_load_coordinate_system():
    loads = pmx.model.steps[STEP].loads
    system = PREFIX + "CS_Load"
    pmx.model.coordinate_systems.add(system, [0, 0, 0], [0, 1, 0], [-1, 0, 0])
    count_before = len(loads)
    check_refused(lambda: loads.add_pressure(PREFIX + "Bad", surface_selection(1), 1,
                                             coordinate_system_name=system),
                  "'coordinate_system_name'", "a coordinate system of a pressure must be refused")
    check_refused(lambda: loads.add_moment(PREFIX + "Bad", node_selection([1]), m1=1,
                                           coordinate_system_name=system),
                  "'coordinate_system_name'", "a coordinate system of a moment must be refused")
    check(len(loads) == count_before, "a load with a refused coordinate system was recorded")
    # The two loads it applies to take it, in the model's spelling
    load = loads.add_concentrated_force(PREFIX + "F_CS", node_selection([1]), f1=1,
                                        coordinate_system_name=system.lower())
    check(load.coordinate_system_name == system, "unexpected coordinate system " + load.coordinate_system_name)
    load = loads.add_pressure(PREFIX + "P_CS", surface_selection(1), 1)
    check_refused(lambda: loads.replace_pressure(PREFIX + "P_CS", coordinate_system_name=system),
                  "'coordinate_system_name'", "a replace must refuse a coordinate system of a pressure")
    # The default is what every load reads back, so it goes back into every load
    load = loads.replace_pressure(PREFIX + "P_CS", coordinate_system_name=load.coordinate_system_name)
    check(load.coordinate_system_name == "Global", "the default coordinate system was not kept")
    loads.remove([PREFIX + "F_CS", PREFIX + "P_CS"])
    pmx.model.coordinate_systems.remove([system])


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
    # A name given twice is refused: the controller would remove the removed load again
    for verb in [loads.remove, loads.duplicate, loads.deactivate, loads.hide]:
        check_refused(lambda: verb([name, name]), "'names'", "a load named twice must be refused")
    check(len(loads) == count_before and loads[name].active and loads[name].visible,
          "a refused call with a repeated name changed the loads")
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
    # A name is found regardless of case and passed on in the model's spelling
    loads.propagate(name.upper())
    check(name in list(following.get_names()), "the load was not propagated in the model's spelling")
    check_raises(lambda: loads.propagate("NoSuchLoad"), "propagating an unknown name must be rejected")
    # The thermal step is the last one: the dialog propagates from it too and copies nothing, so a journal of
    # that replays
    last = pmx.model.steps[THERMAL_STEP].loads
    check(last.contains(PREFIX + "CF"), "the last step must carry the concentrated flux")
    names = list(last.get_names())
    last.propagate(PREFIX + "CF")
    check(list(last.get_names()) == names, "propagating from the last step changed its loads")
    # A following step that does not take the load type is skipped, as the dialog does
    frequency = PREFIX + "Frequency"
    pmx.model.steps.add_frequency(frequency)
    try:
        check(not following.contains(PREFIX + "P"), "the following step must not carry the pressure yet")
        loads.propagate(PREFIX + "P")
        check(following.contains(PREFIX + "P"), "the pressure was not propagated to the static step")
        check(not pmx.model.steps[frequency].loads.contains(PREFIX + "P"), "the frequency step took a pressure")
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
        # The load would wrap a phase outside 0 to 360 degrees and store the number in place of the expression
        for phase in [360, -30, "=400", 720.5]:
            check_refused(lambda: loads.replace_concentrated_force(PREFIX + "FC", phase_deg=phase), "'phase_deg'",
                          "the phase " + str(phase) + " must be refused")
        check(loads[PREFIX + "FC"].get_equations()["phase_deg"] == equation, "a refused phase changed the load")
        load = loads.replace_concentrated_force(PREFIX + "FC", phase_deg=359.5)
        check(load.get_value("phase_deg") == 359.5, "a phase below 360 was changed: " + str(load.get_value("phase_deg")))
        # A pre-tension is complex here too, but the solver input carries no phase for it
        pre_tension = loads.add_pre_tension(PREFIX + "PT", surface_selection(1), 500)
        check("phase_deg" not in list(pre_tension.get_value_names()), "a pre-tension must not read back a phase")
        check_refused(lambda: loads.replace_pre_tension(PREFIX + "PT", phase_deg=30), "'phase_deg'",
                      "a phase of a pre-tension must be refused")
        loads.replace_pre_tension(PREFIX + "PT", magnitude=600)
    finally:
        steps.remove([name])


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    setup()
    test_concentrated()
    test_surface_loads()
    test_body_loads()
    directory = tempfile.mkdtemp(prefix="pytest_pressure_")
    try:
        test_imported_pressure(directory)
    finally:
        shutil.rmtree(directory)
    test_thermal_loads()
    test_invalid_equations()
    test_values_the_dialog_refuses()
    test_required_values()
    test_load_coordinate_system()
    test_collection_verbs()
    test_propagate()
    test_complex_phase()
    clean()
    if pmx.model.parameters.contains(PREFIX + "p"):
        pmx.model.parameters.remove([PREFIX + "p"])
    print("TEST PASSED")


main()
