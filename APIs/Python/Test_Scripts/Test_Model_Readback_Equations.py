#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the bulk model readback and the equation preservation: a value the user could type as an
#expression must read back as that expression, not as the number it evaluated to. Every load, boundary
#condition, defined field, initial condition and constraint type the cube can carry is given back to its
#replace_* method as get_value reads it, and the replace must change nothing.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Readback_Equations.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import os
import sys
import tempfile

PREFIX = "PyTest_Readback_"
SURFACE = PREFIX + "Top"
RP = PREFIX + "RP"
SCALAR = PREFIX + "Scalar"
VECTOR = PREFIX + "Vector"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def clean():
    # A constraint references a reference point and a surface, and a distribution is referenced by the items
    for collection in [pmx.model.constraints, pmx.model.initial_conditions, pmx.model.steps,
                       pmx.model.reference_points, pmx.model.surfaces, pmx.model.distributions,
                       pmx.model.materials, pmx.model.parameters]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def test_bulk_mesh_readback():
    node_ids = list(pmx.model.nodes.get_ids())
    check(len(node_ids) == pmx.model.nodes.count, "the node IDs do not match the node count")
    check(len(pmx.model.nodes.get_coordinates()) == pmx.model.nodes.count, "the coordinates are incomplete")
    check(list(pmx.model.nodes.get_data().ids) == node_ids, "the node snapshot differs from the IDs")
    check(len(pmx.model.elements.get_data().ids) == pmx.model.elements.count,
          "the element snapshot is incomplete")


def test_material_readback():
    material = pmx.model.materials.add(PREFIX + "Steel", pmx.create.elastic(210000, 0.3))
    check(material.property_count == 1, "the material must carry one property")
    check(len(material.get_properties()) > 0, "the material properties did not read back")


def test_equation_readback():
    pmx.model.parameters.add(PREFIX + "P", 2.0)
    step = pmx.model.steps.add_static(PREFIX + "Step")
    load = step.loads.add_concentrated_force(
        PREFIX + "Load", pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1]),
        f1=PREFIX + "P", f2='=3', f3=4)
    equations = load.get_equations()
    # An expression reads back as the expression; a plain value is left out, and get_value returns it
    check(equations['f1'] == '=' + PREFIX + "P",
          "the parameter expression did not read back: " + str(equations['f1']))
    check(equations['f2'] == '=3', "the equation did not read back: " + str(equations['f2']))
    check('f3' not in equations, "the plain value was read back as an equation: " + str(equations))
    check(load.get_value('f3') == 4, "the plain value did not read back: " + str(load.get_value('f3')))
    # Every name the equations use is one get_value reads; a static step is not complex, so its loads take no
    # phase and read none back
    value_names = list(load.get_value_names())
    missing = [name for name in list(equations) if name not in value_names]
    check(not missing, "these names are not read by get_value: " + str(missing))
    check("phase_deg" not in value_names, "a load of a static step read back a phase: " + str(value_names))


def node_selection(ids):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', ids)


def surface_selection(face):
    return pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [face]})


def element_selection(ids):
    return pmx.create.selection_by_ids('femodel', 'elements', 'elements', ids)


def plain(value):
    """A read-back value in a form two snapshots compare by."""
    if value is None or isinstance(value, (bool, int, float, str)):
        return value
    if isinstance(value, (list, tuple)):
        return [plain(item) for item in value]
    return type(value).__name__


def read_back(item):
    return {key: item.get_value(key) for key in item.get_value_names()}


def snapshot(item, with_equations):
    values = {key: plain(value) for key, value in read_back(item).items()}
    if with_equations:
        values["<equations>"] = {key: plain(value) for key, value in dict(item.get_equations()).items()}
    return values


def check_round_trip(collection, name):
    """What get_value reads goes back into the replace of the item type, every name an argument it takes, and
    the replace changes nothing: first with the expressions, as a journal gives them back, then as plain values."""
    item = collection[name]
    replace = getattr(collection, "replace_" + item.item_type)
    before = snapshot(item, True)
    arguments = read_back(item)
    arguments.update(dict(item.get_equations()))
    replace(name, **arguments)
    after = snapshot(collection[name], True)
    check(after == before, "the read-back of " + name + " did not round trip: " + str(before) + " became " +
          str(after))
    before = snapshot(collection[name], False)
    replace(name, **read_back(collection[name]))
    after = snapshot(collection[name], False)
    check(after == before, "the plain read-back of " + name + " did not round trip: " + str(before) +
          " became " + str(after))


def check_collection_round_trips(collection, expected_types):
    names = [n for n in collection.get_names() if n.startswith(PREFIX)]
    item_types = set(collection[name].item_type for name in names)
    check(item_types == set(expected_types), "unexpected item types " + str(sorted(item_types)) + ", expected " +
          str(sorted(expected_types)))
    for name in names:
        check_round_trip(collection, name)


def test_load_round_trips():
    static = pmx.model.steps.add_static(PREFIX + "RT_Static")
    loads = static.loads
    loads.add_concentrated_force(PREFIX + "F", node_selection([1, 2]), f1=PREFIX + "P", f3=4)
    loads.add_moment(PREFIX + "M", node_selection([3]), m2="=5.5")
    loads.add_pressure(PREFIX + "Pressure", surface_selection(1), 2.5, distribution_name=SCALAR)
    loads.add_hydrostatic_pressure(PREFIX + "HP", surface_selection(1), first_point=[0, 0, "=0"],
                                   second_point=[0, 0, 1], direction=[0, 0, -1], first_point_pressure=0,
                                   second_point_pressure=10, cutoff="Positive")
    loads.add_surface_traction(PREFIX + "ST_F", surface_selection(1), f1=100)
    loads.add_surface_traction(PREFIX + "ST_P", surface_selection(3), f1=100)
    loads.replace_surface_traction(PREFIX + "ST_P", p2=3.5, distribution_name=VECTOR)
    loads.add_shell_edge_load(PREFIX + "SE", pmx.create.region('SurfaceName', SURFACE), 7.5)
    loads.add_gravity(PREFIX + "G", element_selection([1]), f3=-9810)
    loads.add_centrifugal(PREFIX + "C", element_selection([1]), axis_point=[0, 0, 0], axis_direction=[0, 0, 1],
                          rotational_speed=100)
    loads.add_pre_tension(PREFIX + "PT_Auto", surface_selection(1), 500)
    loads.add_pre_tension(PREFIX + "PT_Dir", surface_selection(3), 0.5, tension_type="Displacement",
                          direction=[0, 0, 1])
    check_collection_round_trips(loads, ["concentrated_force", "moment", "pressure", "hydrostatic_pressure",
                                         "surface_traction", "shell_edge_load", "gravity", "centrifugal",
                                         "pre_tension"])
    #
    thermal = pmx.model.steps.add_uncoupled_temp_disp(PREFIX + "RT_Thermal")
    loads = thermal.loads
    loads.add_concentrated_flux(PREFIX + "CF", node_selection([1]), 5, add_flux=True)
    loads.add_surface_flux(PREFIX + "DF", surface_selection(1), "=2.5")
    loads.add_body_flux(PREFIX + "BF", element_selection([1]), 1.5)
    loads.add_film_heat_transfer(PREFIX + "FH", surface_selection(1), 20, 0.05)
    loads.add_radiation_heat_transfer(PREFIX + "RH", surface_selection(1), 20, 0.8)
    loads.add_radiation_heat_transfer(PREFIX + "RH_Cavity", surface_selection(2), 20, 0.8, cavity_radiation=True,
                                      cavity_name="Cav")
    loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_radiation=True, cavity_name="Cv")
    loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_radiation=False)
    check_collection_round_trips(loads, ["concentrated_flux", "surface_flux", "body_flux", "film_heat_transfer",
                                         "radiation_heat_transfer"])
    # The cavity name kept without cavity radiation is not read back, yet a round trip keeps it
    loads.replace_radiation_heat_transfer(PREFIX + "RH", cavity_radiation=True)
    check(loads[PREFIX + "RH"].get_value("cavity_name") == "Cv", "the round trip lost the kept cavity name")
    #
    steady = pmx.model.steps.add_steady_state_dynamics(PREFIX + "RT_Steady")
    loads = steady.loads
    loads.add_concentrated_force(PREFIX + "F_Phase", node_selection([1]), f1=1, phase_deg="=15 * 2")
    loads.add_pre_tension(PREFIX + "PT_Complex", surface_selection(1), 500)
    check("phase_deg" in list(loads[PREFIX + "F_Phase"].get_value_names()), "a complex load must read its phase")
    check_collection_round_trips(loads, ["concentrated_force", "pre_tension"])


def test_boundary_condition_round_trips():
    bcs = pmx.model.steps[PREFIX + "RT_Static"].bcs
    bcs.add_fixed(PREFIX + "Fix", node_selection([1]))
    bcs.add_displacement_rotation(PREFIX + "DR", node_selection([2, 3]), u1=0, u2="=0", u3=1.5,
                                  distribution_name=SCALAR)
    bcs.add_displacement_rotation(PREFIX + "DR_Fixed", node_selection([4]), fixed_dofs=["U1", "U2"])
    bcs.add_submodel(PREFIX + "SM", node_selection([5]), step_id=2, u1=True, u3=True)
    dr = bcs[PREFIX + "DR_Fixed"]
    check(list(dr.get_value("fixed_dofs")) == ["U1", "U2"], "unexpected fixed_dofs " + str(dr.get_value("fixed_dofs")))
    check(list(dr.get_value("free_dofs")) == ["U3", "UR1", "UR2", "UR3"],
          "unexpected free_dofs " + str(dr.get_value("free_dofs")))
    check_collection_round_trips(bcs, ["fixed", "displacement_rotation", "submodel"])
    #
    bcs = pmx.model.steps[PREFIX + "RT_Thermal"].bcs
    bcs.add_temperature(PREFIX + "T", node_selection([1, 2]), "=100", distribution_name=SCALAR)
    check_collection_round_trips(bcs, ["temperature"])
    #
    bcs = pmx.model.steps[PREFIX + "RT_Steady"].bcs
    bcs.add_displacement_rotation(PREFIX + "DR_Phase", node_selection([1, 2]), u1=0.1, phase_deg=45)
    check_collection_round_trips(bcs, ["displacement_rotation"])


def test_defined_field_round_trips(file_name):
    fields = pmx.model.steps[PREFIX + "RT_Static"].defined_fields
    fields.add_temperature(PREFIX + "DT", node_selection([1, 2]), "=100", distribution_name=SCALAR)
    fields.add_temperature_from_file(PREFIX + "DT_File", file_name, step_id=2)
    check_collection_round_trips(fields, ["temperature", "temperature_from_file"])


def test_initial_condition_round_trips():
    conditions = pmx.model.initial_conditions
    conditions.add_temperature(PREFIX + "IT", node_selection([1, 2]), "=20", distribution_name=SCALAR)
    conditions.add_translational_velocity(PREFIX + "IV", node_selection([1, 2]), 1, 0, "=0")
    conditions.add_angular_velocity(PREFIX + "IA", node_selection([1, 2]), [0, 0, 0], [0, 0, "=1"], 10)
    check_collection_round_trips(conditions, ["temperature", "translational_velocity", "angular_velocity"])


def test_constraint_round_trips():
    constraints = pmx.model.constraints
    constraints.add_tie(PREFIX + "Tie", surface_selection(1), surface_selection(2), position_tolerance=0.01)
    constraints.add_cyclic_symmetry(PREFIX + "CS", surface_selection(3), surface_selection(4),
                                    region=element_selection([1]), number_of_graph_sectors=2, axis_point=[0, 0, 0],
                                    axis_direction=[0, 0, "1"], number_of_sectors=6)
    constraints.add_joint(PREFIX + "Joint", surface_selection(5), surface_selection(6), joint_type="Slider",
                          center=[0.5, 0.5, 0.5], axis_direction=[0, 0, 1], formulation="Springs", stiffness=5e6)
    constraints.add_joint(PREFIX + "Joint_Ball", surface_selection(5), surface_selection(6), joint_type="Ball")
    constraints.add_rigid_body(PREFIX + "RB", RP, node_selection([1, 2, 3, 4]))
    constraints.add_kinematic_coupling(PREFIX + "KC", RP, surface_selection(1), u1=True, u2=False)
    constraints.add_distributing_coupling(PREFIX + "DC", RP, surface_selection(2), ur1=False)
    constraints.add_compression_only(PREFIX + "CO", surface_selection(2), clearance=0.001, non_linear=True)
    constraints.add_point_spring(PREFIX + "PS", node_selection([5, 6]), k1=100, k2=PREFIX + "P", k3=300)
    constraints.add_surface_spring(PREFIX + "SS", surface_selection(1), k3=50, stiffness_per_area=True)
    cs = constraints[PREFIX + "CS"]
    check(abs(cs.sector_angle_deg - 60.0) < 1e-9 and cs.region_type == "ElementSetName",
          "the cyclic symmetry region and sector angle did not read back")
    check(constraints[PREFIX + "PS"].region_type == "NodeSetName", "the point spring region did not read back")
    check(constraints[PREFIX + "RB"].region_type == "NodeSetName", "the rigid body region did not read back")
    for name in [PREFIX + "CS", PREFIX + "PS", PREFIX + "Joint"]:
        names = list(constraints[name].get_value_names())
        check(not any(key in names for key in ["region_name", "region_type", "creation_ids", "sector_angle_deg",
                                                "node_id", "has_axis", "is_center_from_selection"]),
              "a value no argument takes was read back by " + name + ": " + str(names))
    check_collection_round_trips(constraints, ["tie", "cyclic_symmetry", "joint", "rigid_body", "kinematic_coupling",
                                               "distributing_coupling", "compression_only", "point_spring",
                                               "surface_spring"])


def test_read_back_round_trips():
    pmx.model.surfaces.add(SURFACE, surface_selection(2))
    pmx.model.reference_points.add(RP, [0.5, 0.5, 2.0])
    pmx.model.distributions.add_from_equation(SCALAR, "1 + 0.01 * x")
    pmx.model.distributions.add_from_equation(VECTOR, ["x", "y", "z"])
    test_load_round_trips()
    test_boundary_condition_round_trips()
    # Any existing file proves the path check; the file is only read when the analysis is written
    file_name = os.path.join(tempfile.gettempdir(), "pytest_readback_temperature.frd")
    open(file_name, "w").close()
    try:
        test_defined_field_round_trips(file_name)
    finally:
        pmx.model.steps.remove([n for n in pmx.model.steps.get_names() if n.startswith(PREFIX)])
        os.remove(file_name)
    test_initial_condition_round_trips()
    test_constraint_round_trips()


def test_script_context():
    # A script run from a file knows its own path, so a journal can resolve the assets next to it
    check(os.path.abspath(__file__) == os.path.abspath(sys.argv[0]),
          "__file__ and argv[0] disagree: " + __file__ + " and " + sys.argv[0])
    check(os.path.isdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models")),
          "the asset directory next to the script was not found")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    test_bulk_mesh_readback()
    test_material_readback()
    test_equation_readback()
    test_read_back_round_trips()
    test_script_context()
    clean()
    print("TEST PASSED")


main()
