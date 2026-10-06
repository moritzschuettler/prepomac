#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the step scoped boundary condition collection: the 4 types, the selection and named region
#forms, a distribution on a displacement and rotation, and the step scoped collection verbs. A meshed
#model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_BCs.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile

PREFIX = "PyTest_"
STEP = PREFIX + "Static"
NEXT_STEP = PREFIX + "Static_2"
THERMAL_STEP = PREFIX + "Thermal"
NODE_SET = PREFIX + "Bottom"
REFERENCE_POINT = PREFIX + "RP"
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
    names = [n for n in pmx.model.node_sets.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.node_sets.remove(names)
    names = [n for n in pmx.model.reference_points.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.reference_points.remove(names)
    names = [n for n in pmx.model.distributions.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.distributions.remove(names)
    names = [n for n in pmx.model.coordinate_systems.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.coordinate_systems.remove(names)


def setup():
    pmx.model.node_sets.add(NODE_SET, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))
    pmx.model.reference_points.add(REFERENCE_POINT, [0.5, 0.5, 2.0])
    pmx.model.steps.add_static(STEP, time_period=1.0)
    pmx.model.steps.add_static(NEXT_STEP, time_period=1.0)
    pmx.model.steps.add_uncoupled_temp_disp(THERMAL_STEP)
    pmx.model.distributions.add_from_equation(SCALAR_DISTRIBUTION, "1 + 0.01 * x")
    pmx.model.distributions.add_from_equation(VECTOR_DISTRIBUTION, ["x", "y", "z"])


def test_regions():
    bcs = pmx.model.steps[STEP].bcs
    check(bcs.step_name == STEP, "the collection must carry the step name")
    check(len(bcs) == 0, "a new step has no boundary conditions")
    # A selection based region is converted into an internal node set by the controller, so it reads
    # back as a node set region that points at a set the user did not create
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [5, 6])
    bc = bcs.add_fixed(PREFIX + "Fix_Sel", selection)
    check(bc.item_type == "fixed", "unexpected type " + bc.item_type)
    check(bc.region_type == "NodeSetName", "unexpected region type " + bc.region_type)
    check(bc.region_name is not None and bc.region_name != NODE_SET,
          "the selection was not converted into its own node set, got " + str(bc.region_name))
    check(bc.active, "a new boundary condition must be active")
    check(len(bc.get_value_names()) == 0, "a fixed boundary condition has no values of its own")
    # A named region points at an existing entity instead
    bc = bcs.add_fixed(PREFIX + "Fix_Set", pmx.create.region('NodeSetName', NODE_SET))
    check(bc.region_type == "NodeSetName", "unexpected region type " + bc.region_type)
    check(bc.region_name == NODE_SET, "unexpected region name " + str(bc.region_name))
    # The named region is checked
    check_raises(lambda: bcs.add_fixed(PREFIX + "Bad", pmx.create.region('NodeSetName', 'NoSuchNodeSet')),
                 "an unknown node set must be rejected")
    # A named region is checked when it is used, not when it is created
    check_raises(lambda: bcs.add_fixed(PREFIX + "Bad", pmx.create.region('NodeId', '1')),
                 "a region type that cannot name a region must be rejected")
    check_raises(lambda: pmx.create.region('NotAType', 'x'), "an unknown region type must be rejected")
    check_raises(lambda: bcs.add_fixed(PREFIX + "Bad", NODE_SET),
                 "a region given as a plain string must be rejected")
    check_raises(lambda: bcs.add_fixed(PREFIX + "Bad", None), "a region of None must be rejected")


def test_displacement_rotation():
    bcs = pmx.model.steps[STEP].bcs
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [7, 8])
    name = PREFIX + "DR"
    bc = bcs.add_displacement_rotation(name, selection, u1=0, u2=0, u3=1.5, color=[255, 0, 0])
    check(bc.get_value("u1") == 0, "u1 was not set")
    check(bc.get_value("u3") == 1.5, "u3 was not set, got " + str(bc.get_value("u3")))
    # A degree of freedom that is not given is left unconstrained
    check(bc.get_value("ur1") is None, "ur1 must be unconstrained, got " + str(bc.get_value("ur1")))
    # The colour reads back as red, green, blue and alpha
    check(list(bc.color) == [255, 0, 0, 255], "the color was not set, got " + str(list(bc.color)))
    check(bc.amplitude_name == "Default", "unexpected amplitude name " + bc.amplitude_name)
    # The names are checked against the model
    check_raises(lambda: bcs.add_displacement_rotation(PREFIX + "Bad", selection, u1=0,
                                                       amplitude_name="NoSuchAmplitude"),
                 "an unknown amplitude must be rejected")
    check_raises(lambda: bcs.add_displacement_rotation(PREFIX + "Bad", selection, u1=0,
                                                       coordinate_system_name="NoSuchSystem"),
                 "an unknown coordinate system must be rejected")
    check_raises(lambda: bcs.add_displacement_rotation(name, selection, u1=0),
                 "a duplicate name must be rejected")
    # A value that is not given survives the replace, and free_dofs unconstrains one
    bc = bcs.replace_displacement_rotation(name, u1=2.0)
    check(bc.get_value("u1") == 2.0, "u1 was not replaced")
    check(bc.get_value("u3") == 1.5, "u3 must survive a replace")
    bc = bcs.replace_displacement_rotation(name, free_dofs=["U3"])
    check(bc.get_value("u3") is None, "u3 was not unconstrained")
    check(bc.get_value("u1") == 2.0, "u1 must survive the unconstraining of u3")
    check_raises(lambda: bcs.replace_displacement_rotation(name, free_dofs=["U9"]),
                 "an unknown degree of freedom must be rejected")
    # The type cannot change on a replace
    check_raises(lambda: bcs.replace_fixed(name), "replacing a displacement by a fixed must be rejected")
    # An equation is accepted where a value is
    pmx.model.parameters.add(PREFIX + "d", 0.75)
    bc = bcs.replace_displacement_rotation(name, u1=PREFIX + "d * 2")
    check(bc.get_value("u1") == 1.5, "the equation did not evaluate, got " + str(bc.get_value("u1")))
    # The equation reads back unevaluated, under the argument name get_value uses
    equations = bc.get_equations()
    # The model stores an expression without its spaces
    check(equations["u1"].replace(" ", "") == "=" + PREFIX + "d*2",
          "the u1 equation did not read back: " + str(equations))
    # Only an expression is an equation: a plain value is left out, and get_value returns it as a number
    check(sorted(equations) == ["u1"], "a plain value must be left out of the equations: " + str(sorted(equations)))
    check(bc.get_value("u2") == 0, "the plain u2 value did not read back: " + str(bc.get_value("u2")))


def read_boundary_lines(name):
    """The node coordinates and the *Boundary data lines of the named boundary condition in the written deck."""
    file_name = os.path.join(tempfile.gettempdir(), PREFIX + "bcs.inp")
    pmx.export_to_calculix(file_name)
    with open(file_name) as f:
        lines = [line.strip() for line in f]
    os.remove(file_name)
    coordinates = {}
    in_nodes = False
    for line in lines:
        if line.startswith("*"):
            in_nodes = line.lower().startswith("*node") and not line.lower().startswith("*node ")
            continue
        if in_nodes and line:
            values = [v.strip() for v in line.split(",")]
            coordinates[int(values[0])] = [float(v) for v in values[1:4]]
    start = lines.index("** Name: " + name)
    check(lines[start + 1].lower().startswith("*boundary"), "no *Boundary follows " + name)
    rows = []
    for line in lines[start + 2:]:
        if not line or line.startswith("*"):
            break
        rows.append([v.strip() for v in line.split(",")])
    return coordinates, rows


def test_displacement_rotation_distribution():
    bcs = pmx.model.steps[STEP].bcs
    region = pmx.create.region('NodeSetName', NODE_SET)
    name = PREFIX + "DR_Dist"
    bc = bcs.add_displacement_rotation(name, region, u1=0, u3=1.5)
    check(bc.get_value("distribution_name") == "Constant",
          "unexpected distribution name " + str(bc.get_value("distribution_name")))
    # A scalar distribution scales every prescribed value node by node, and a zero stays zero
    bc = bcs.replace_displacement_rotation(name, distribution_name=SCALAR_DISTRIBUTION)
    check(bc.get_value("distribution_name") == SCALAR_DISTRIBUTION, "the scalar distribution was not set")
    coordinates, rows = read_boundary_lines(name)
    check(len(rows) == 8, "expected two lines per node of the node set, got " + str(rows))
    for row in rows:
        node_id, dof, value = int(row[0]), int(row[1]), float(row[3])
        expected = 0 if dof == 1 else 1.5 * (1 + 0.01 * coordinates[node_id][0])
        check(abs(value - expected) < 1e-9, "node " + str(node_id) + " dof " + str(dof) + " has " + str(value) +
              " instead of " + str(expected))
    # A vector distribution scales u1, u2 and u3 by its components
    bc = bcs.replace_displacement_rotation(name, u1=0.1, distribution_name=VECTOR_DISTRIBUTION)
    coordinates, rows = read_boundary_lines(name)
    for row in rows:
        node_id, dof, value = int(row[0]), int(row[1]), float(row[3])
        expected = (0.1 if dof == 1 else 1.5) * coordinates[node_id][dof - 1]
        check(abs(value - expected) < 1e-9, "node " + str(node_id) + " dof " + str(dof) + " has " + str(value) +
              " instead of " + str(expected))
    # The refusals name the arguments and record nothing
    check_refused(lambda: bcs.replace_displacement_rotation(name, ur3=0.2), "scales only u1, u2 and u3",
                  "a vector distribution with a prescribed rotation must be refused")
    check(bc.get_value("ur3") is None, "the refused replacement changed ur3")
    check_refused(lambda: bcs.add_displacement_rotation(PREFIX + "Bad", region, u1=0,
                                                        distribution_name=SCALAR_DISTRIBUTION),
                  "none of u1, u2, u3, ur1, ur2 and ur3 has a nonzero value",
                  "a distribution without a nonzero value must be refused")
    check_refused(lambda: bcs.add_displacement_rotation(PREFIX + "Bad",
                                                        pmx.create.region('ReferencePointName', REFERENCE_POINT),
                                                        u1=1, distribution_name=SCALAR_DISTRIBUTION),
                  "'distribution_name' cannot be used with a reference point region",
                  "a distribution on a reference point must be refused")
    check(not bcs.contains(PREFIX + "Bad"), "a refused boundary condition was recorded")
    check_raises(lambda: bcs.add_displacement_rotation(PREFIX + "Bad", region, u1=1,
                                                       distribution_name="NoSuchDistribution"),
                 "an unknown distribution must be rejected")
    # The constant distribution brings the node set line back
    bcs.replace_displacement_rotation(name, distribution_name="Constant")
    coordinates, rows = read_boundary_lines(name)
    check([row[0] for row in rows] == [NODE_SET, NODE_SET], "the node set lines did not come back: " +
          str(rows))
    bcs.remove([name])


def read_deck_lines():
    file_name = os.path.join(tempfile.gettempdir(), PREFIX + "bcs_cs.inp")
    pmx.export_to_calculix(file_name)
    with open(file_name) as f:
        lines = [line.strip() for line in f]
    os.remove(file_name)
    return lines


def test_displacement_rotation_distribution_local_system():
    # CalculiX applies *Transform to nodes, so the per-node lines of a distributed boundary condition are
    # rotated only when the transformed node set holds every node they name
    bcs = pmx.model.steps[STEP].bcs
    system = PREFIX + "Rotated"
    pmx.model.coordinate_systems.add(system, [0, 0, 0], [0, 1, 0], [-1, 0, 0])
    name = PREFIX + "DR_Local"
    bcs.add_displacement_rotation(name, pmx.create.region('NodeSetName', NODE_SET), u1=0.1, u3=1.5,
                                  distribution_name=SCALAR_DISTRIBUTION, coordinate_system_name=system)
    coordinates, rows = read_boundary_lines(name)
    lines = read_deck_lines()
    transforms = [i for i, line in enumerate(lines) if line.lower().startswith("*transform")]
    check(len(transforms) == 1, "expected one *Transform, got " + str([lines[i] for i in transforms]))
    keyword = lines[transforms[0]]
    check("nset=" + NODE_SET.lower() in keyword.lower().replace(" ", ""),
          "the *Transform does not name the node set of the boundary condition: " + keyword)
    check("type=r" in keyword.lower().replace(" ", ""), "the *Transform is not rectangular: " + keyword)
    axes = [float(v) for v in lines[transforms[0] + 1].split(",")]
    expected_axes = [0, 1, 0, -1, 0, 0]
    check(all(abs(a - b) < 1e-9 for a, b in zip(axes, expected_axes)),
          "the *Transform axes are " + str(axes) + " instead of " + str(expected_axes))
    # Every node the per-node lines name is a node the transform rotates
    start = lines.index("*Nset, Nset=" + NODE_SET)
    transformed = set()
    for line in lines[start + 1:]:
        if not line or line.startswith("*"):
            break
        transformed.update(int(v) for v in line.split(",") if v.strip())
    named = set(int(row[0]) for row in rows)
    check(len(named) == 4 and named <= transformed, "the per-node lines name nodes " + str(sorted(named)) +
          " outside the transformed node set " + str(sorted(transformed)))
    # The values stay in the local directions; the solver does the rotation
    for row in rows:
        node_id, dof, value = int(row[0]), int(row[1]), float(row[3])
        expected = (0.1 if dof == 1 else 1.5) * (1 + 0.01 * coordinates[node_id][0])
        check(dof in (1, 3) and abs(value - expected) < 1e-9, "node " + str(node_id) + " dof " + str(dof) +
              " has " + str(value) + " instead of the local value " + str(expected))
    bcs.remove([name])
    pmx.model.coordinate_systems.remove([system])


def test_submodel():
    bcs = pmx.model.steps[STEP].bcs
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])
    bc = bcs.add_submodel(PREFIX + "SM", selection, step_id=2, u1=True, u3=True)
    check(bc.get_value("step_id") == 2, "the step number was not set")
    check(bc.get_value("u1"), "u1 was not set")
    check(not bc.get_value("u2"), "u2 must stay false")
    bc = bcs.replace_submodel(PREFIX + "SM", u2=True)
    check(bc.get_value("u1"), "u1 must survive a replace")
    check(bc.get_value("u2"), "u2 was not replaced")
    # A submodel boundary condition does not accept a reference point region, and the dialog does
    # not offer one for it
    check_raises(lambda: bcs.add_submodel(PREFIX + "Bad",
                                          pmx.create.region('ReferencePointName', REFERENCE_POINT),
                                          step_id=1),
                 "a submodel boundary condition on a reference point must be rejected")
    check(not bcs.contains(PREFIX + "Bad"), "the refused submodel boundary condition was recorded")
    # A replacement is refused the same way
    check_raises(lambda: bcs.replace_submodel(PREFIX + "SM",
                                              region=pmx.create.region('ReferencePointName', REFERENCE_POINT)),
                 "a submodel boundary condition replaced onto a reference point must be rejected")
    check(bcs[PREFIX + "SM"].region_type == "NodeSetName",
          "the refused replacement changed the region to " + bcs[PREFIX + "SM"].region_type)


def test_temperature():
    bcs = pmx.model.steps[STEP].bcs
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2])
    # A static step does not support a temperature boundary condition
    check_raises(lambda: bcs.add_temperature(PREFIX + "Bad", selection, 100),
                 "a temperature boundary condition in a static step must be rejected")
    #
    thermal = pmx.model.steps[THERMAL_STEP].bcs
    bc = thermal.add_temperature(PREFIX + "T", selection, 100)
    check(bc.get_value("temperature") == 100, "the temperature was not set")
    check(bc.get_value("distribution_name") == "Constant",
          "unexpected distribution name " + str(bc.get_value("distribution_name")))
    check_raises(lambda: thermal.add_temperature(PREFIX + "Bad", selection, 100,
                                                 distribution_name="NoSuchDistribution"),
                 "an unknown distribution must be rejected")
    bc = thermal.replace_temperature(PREFIX + "T", temperature=250)
    check(bc.get_value("temperature") == 250, "the temperature was not replaced")
    # A temperature is a scalar, which the model would only find out when the deck is written
    message = raised_message(lambda: thermal.add_temperature(PREFIX + "Bad", selection, 100,
                                                             distribution_name=VECTOR_DISTRIBUTION))
    check(message is not None and VECTOR_DISTRIBUTION in message and "Scalar" in message,
          "a vector distribution of a temperature must be refused: " + str(message))
    check(not thermal.contains(PREFIX + "Bad"), "the refused temperature boundary condition was recorded")
    message = raised_message(lambda: thermal.replace_temperature(PREFIX + "T",
                                                                 distribution_name=VECTOR_DISTRIBUTION))
    check(message is not None and VECTOR_DISTRIBUTION in message and "Scalar" in message,
          "a vector distribution of a replaced temperature must be refused: " + str(message))
    check(thermal[PREFIX + "T"].get_value("distribution_name") == "Constant",
          "the refused replacement changed the distribution")
    bc = thermal.replace_temperature(PREFIX + "T", distribution_name=SCALAR_DISTRIBUTION)
    check(bc.get_value("distribution_name") == SCALAR_DISTRIBUTION, "the scalar distribution was not set")
    # A temperature boundary condition does not accept a reference point region either
    check_raises(lambda: thermal.add_temperature(PREFIX + "Bad",
                                                 pmx.create.region('ReferencePointName', REFERENCE_POINT),
                                                 100),
                 "a temperature boundary condition on a reference point must be rejected")
    check(not thermal.contains(PREFIX + "Bad"),
          "the refused temperature boundary condition was recorded")
    check_raises(lambda: thermal.replace_temperature(PREFIX + "T",
                                                     region=pmx.create.region('ReferencePointName',
                                                                              REFERENCE_POINT)),
                 "a temperature boundary condition replaced onto a reference point must be rejected")
    check(thermal[PREFIX + "T"].region_type == "NodeSetName",
          "the refused replacement changed the region to " + thermal[PREFIX + "T"].region_type)


def test_collection_verbs():
    bcs = pmx.model.steps[STEP].bcs
    name = PREFIX + "DR"
    check(bcs.contains(name), "the boundary condition is missing")
    check(bcs[name].name == name, "indexing by name failed")
    check(bcs.get(name).name == name, "get failed")
    check(len([b for b in bcs]) == len(bcs), "iteration failed")
    check_raises(lambda: bcs["NoSuchBC"], "an unknown name must be rejected")
    # A boundary condition of the same name may live in another step
    other = pmx.model.steps[NEXT_STEP].bcs
    other.add_fixed(name, pmx.create.region('NodeSetName', NODE_SET))
    check(other.contains(name), "the name is scoped to the step")
    other.remove([name])
    # Rename, duplicate, remove
    bcs.rename(name, name + "_R")
    check(bcs.contains(name + "_R"), "the boundary condition was not renamed")
    bcs.rename(name + "_R", name)
    count_before = len(bcs)
    bcs.duplicate([name])
    check(len(bcs) == count_before + 1, "the boundary condition was not duplicated")
    duplicated = [n for n in bcs.get_names() if n.startswith(name) and n != name]
    bcs.remove(duplicated)
    check(len(bcs) == count_before, "the duplicate was not removed")
    # Activate, deactivate, hide, show
    bcs.deactivate([name])
    check(not bcs[name].active, "the boundary condition was not deactivated")
    bcs.activate([name])
    check(bcs[name].active, "the boundary condition was not activated")
    bcs.hide([name])
    check(not bcs[name].visible, "the boundary condition was not hidden")
    bcs.show([name])
    check(bcs[name].visible, "the boundary condition was not shown")


def test_propagate():
    bcs = pmx.model.steps[STEP].bcs
    name = PREFIX + "DR"
    following = pmx.model.steps[NEXT_STEP].bcs
    check(not following.contains(name), "the following step must not carry the boundary condition yet")
    bcs.propagate(name)
    check(following.contains(name), "the boundary condition was not propagated")
    check_raises(lambda: bcs.propagate("NoSuchBC"), "propagating an unknown name must be rejected")
    # The thermal step is the last one, so a propagation from it would record a command that copies nothing
    last = pmx.model.steps[THERMAL_STEP].bcs
    check(last.contains(PREFIX + "T"), "the last step must carry the temperature boundary condition")
    check_refused(lambda: last.propagate(PREFIX + "T"), "no following step",
                  "propagating from the last step must be refused")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    setup()
    test_regions()
    test_displacement_rotation()
    test_displacement_rotation_distribution()
    test_displacement_rotation_distribution_local_system()
    test_submodel()
    test_temperature()
    test_collection_verbs()
    test_propagate()
    clean()
    for name in [PREFIX + "d"]:
        if pmx.model.parameters.contains(name):
            pmx.model.parameters.remove([name])
    print("TEST PASSED")


main()
