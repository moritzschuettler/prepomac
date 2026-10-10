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
    pmx.model.steps.add_static(STEP)
    pmx.model.steps.add_static(NEXT_STEP)
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
    # The internal node set belongs to the boundary condition, which the model tree does not show, so it
    # cannot be replaced, renamed or removed on its own, and a refusal leaves it as it was
    internal_name = bc.region_name
    check(pmx.model.node_sets[internal_name].is_internal, "the converted selection is not an internal node set")
    other = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [7])
    check_raises(lambda: pmx.model.node_sets.replace(internal_name, selection=other),
                 "an internal node set was replaced")
    check_raises(lambda: pmx.model.node_sets.rename(internal_name, PREFIX + "Internal_Renamed"),
                 "an internal node set was renamed")
    check_raises(lambda: pmx.model.node_sets.remove(internal_name), "an internal node set was removed")
    check(pmx.model.node_sets.contains(internal_name) and pmx.model.node_sets[internal_name].is_internal and
          list(pmx.model.node_sets[internal_name].get_node_ids()) == [5, 6] and
          not pmx.model.node_sets.contains(PREFIX + "Internal_Renamed"),
          "a refused change of an internal node set changed it")
    # A named region points at an existing entity instead
    bc = bcs.add_fixed(PREFIX + "Fix_Set", pmx.create.region('NodeSetName', NODE_SET))
    check(bc.region_type == "NodeSetName", "unexpected region type " + bc.region_type)
    check(bc.region_name == NODE_SET, "unexpected region name " + str(bc.region_name))
    # A region named in another case is stored in the model's own spelling, which the controller compares
    # exactly when the node set is renamed or replaced
    bc = bcs.add_fixed(PREFIX + "Fix_Case", pmx.create.region('NodeSetName', NODE_SET.upper()))
    check(bc.region_name == NODE_SET, "the region name was stored in the caller's spelling: " + str(bc.region_name))
    bcs.remove(PREFIX + "Fix_Case")
    # The named region is checked
    check_raises(lambda: bcs.add_fixed(PREFIX + "Bad", pmx.create.region('NodeSetName', 'NoSuchNodeSet')),
                 "an unknown node set must be rejected")
    # A region type no named region carries is refused when the region is created, naming the argument
    for region_type in ['None', 'NodeId', 'ElementId', 'Selection', 'ContactPair']:
        check_refused(lambda: pmx.create.region(region_type, '1'), "'region_type'",
                      "the region type " + region_type + " cannot name a region and must be refused")
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
    # A padded name is no name, so the value is not found under it
    check_raises(lambda: bc.get_value(" u1"), "a padded value name must not be trimmed")
    # The colour reads back as red, green, blue and alpha
    check(list(bc.color) == [255, 0, 0, 255], "the color was not set, got " + str(list(bc.color)))
    check(type(bc.color) is list, "the color must read back as a plain list: " + repr(bc.color))
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
    # A static step is not complex, so a phase would be kept without effect
    check_refused(lambda: bcs.add_displacement_rotation(PREFIX + "Bad", selection, u1=0, phase_deg=45),
                  "'phase_deg'", "a phase in a static step must be refused")
    check_refused(lambda: bcs.replace_displacement_rotation(name, phase_deg=45), "'phase_deg'",
                  "a phase given to a boundary condition of a static step must be refused")
    check(not bcs.contains(PREFIX + "Bad"), "a boundary condition with a refused phase was recorded")
    # An equation the application cannot evaluate is refused naming the argument, before anything is recorded
    count_before = len(bcs)
    check_refused(lambda: bcs.add_displacement_rotation(PREFIX + "Bad", selection, u1="=undefined_param"), "'u1'",
                  "an equation naming an undefined parameter must be refused naming u1")
    check_refused(lambda: bcs.add_displacement_rotation(PREFIX + "Bad", selection, u1=0, ur3="=1+"), "'ur3'",
                  "an equation that does not parse must be refused naming ur3")
    check_refused(lambda: bcs.replace_displacement_rotation(name, u2="=undefined_param"), "'u2'",
                  "a replace with an equation that cannot be evaluated must be refused naming u2")
    check(len(bcs) == count_before and not bcs.contains(PREFIX + "Bad"),
          "a boundary condition with an invalid equation was recorded")
    check(bcs[name].get_value("u2") == 0, "the refused replace changed u2, got " + str(bcs[name].get_value("u2")))
    # The default names are matched without regard to case
    bc = bcs.replace_displacement_rotation(name, amplitude_name="DEFAULT", coordinate_system_name="global")
    check(bc.amplitude_name == "Default", "unexpected amplitude name " + bc.amplitude_name)
    # A value that is not given survives the replace, and free_dofs unconstrains one
    bc = bcs.replace_displacement_rotation(name, u1=2.0)
    check(bc.get_value("u1") == 2.0, "u1 was not replaced")
    check(bc.get_value("u3") == 1.5, "u3 must survive a replace")
    bc = bcs.replace_displacement_rotation(name, free_dofs=["U3"])
    check(bc.get_value("u3") is None, "u3 was not unconstrained")
    check(bc.get_value("u1") == 2.0, "u1 must survive the unconstraining of u3")
    check_raises(lambda: bcs.replace_displacement_rotation(name, free_dofs=["U9"]),
                 "an unknown degree of freedom must be rejected")
    # A degree of freedom given a value and freed in the same call would lose the value, so it is refused
    check_refused(lambda: bcs.replace_displacement_rotation(name, u1=0.5, free_dofs=["U1"]), "'u1'",
                  "a value for a degree of freedom named in free_dofs must be refused")
    check_refused(lambda: bcs.replace_displacement_rotation(name, ur2=0.1, free_dofs=["U3", "ur2"]), "'ur2'",
                  "a rotation named in free_dofs and given a value must be refused")
    bc = bcs[name]
    check(bc.get_value("u1") == 2.0, "the refused replace changed u1, got " + str(bc.get_value("u1")))
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
    # Only a complex displacement rotation takes a phase, so only it reads one back; a static step is not complex
    check("phase_deg" not in list(bc.get_value_names()),
          "phase_deg must not be listed for a displacement rotation of a static step")
    check("phase_deg" not in bc.get_equations(), "a displacement rotation of a static step reported a phase")
    check("phase_deg" not in list(bcs[PREFIX + "Fix_Set"].get_value_names()),
          "a fixed boundary condition takes no phase")


def test_complex_phase():
    # A steady state dynamics step makes its boundary conditions complex, and only then they take a phase
    name = PREFIX + "SteadyState"
    pmx.model.steps.add_steady_state_dynamics(name)
    try:
        bcs = pmx.model.steps[name].bcs
        selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2])
        bc = bcs.add_displacement_rotation(PREFIX + "DR_Phase", selection, u1=0.1, phase_deg=45)
        # The phase reads back under its argument name, and goes back into it
        check(bc.get_value("phase_deg") == 45, "unexpected phase " + str(bc.get_value("phase_deg")))
        bc = bcs.replace_displacement_rotation(PREFIX + "DR_Phase", phase_deg="=20 + 25")
        check(bc.get_value("phase_deg") == 45, "the phase expression was not evaluated")
        bc = bcs.replace_displacement_rotation(PREFIX + "DR_Phase", phase_deg=bc.get_equations()["phase_deg"])
        check(bc.get_equations()["phase_deg"].replace(" ", "") == "=20+25", "the read back phase did not go back")
        check("phase_deg" in list(bc.get_value_names()), "phase_deg must be listed for a complex displacement")
        # The boundary condition would wrap a phase outside 0 to 360 degrees and store the number in place of
        # the expression
        for phase in [360, -45, "=20 * 20"]:
            check_refused(lambda: bcs.replace_displacement_rotation(PREFIX + "DR_Phase", phase_deg=phase),
                          "'phase_deg'", "the phase " + str(phase) + " must be refused")
        check(bc.get_equations()["phase_deg"].replace(" ", "") == "=20+25", "a refused phase changed the phase")
    finally:
        pmx.model.steps.remove([name])


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


def test_fixed_dofs():
    # The dialog offers Fixed, which the deck writes as *Boundary, Fixed without a value
    bcs = pmx.model.steps[STEP].bcs
    name = PREFIX + "DR_Fixed"
    bc = bcs.add_displacement_rotation(name, pmx.create.region('NodeSetName', NODE_SET), fixed_dofs=["u1", "U2"])
    check(list(bc.get_value("fixed_dofs")) == ["U1", "U2"], "the fixed dofs did not read back: " +
          str(bc.get_value("fixed_dofs")))
    check(type(bc.get_value("fixed_dofs")) is list, "fixed_dofs must read back as a plain list")
    check(bc.get_value("u1") is None and bc.get_value("u3") is None,
          "a fixed and a free degree of freedom must read back as None")
    lines = read_deck_lines()
    keyword = lines[lines.index("** Name: " + name) + 1]
    check(keyword.lower().replace(" ", "").startswith("*boundary,fixed"), "the deck did not fix the dofs: " + keyword)
    # A replace keeps the fixed dofs it is not given, and the read back goes back into the argument
    bc = bcs.replace_displacement_rotation(name, fixed_dofs=["U3"])
    check(list(bc.get_value("fixed_dofs")) == ["U1", "U2", "U3"], "the replace dropped a fixed dof: " +
          str(bc.get_value("fixed_dofs")))
    lines = read_deck_lines()
    index = lines.index("** Name: " + name)
    check(lines[index + 1].lower().replace(" ", "").startswith("*boundary,fixed"),
          "the deck did not fix the dofs: " + lines[index + 1])
    dofs = [line.replace(" ", "").split(",")[1:] for line in lines[index + 2:index + 5]]
    check(dofs == [["1", "1"], ["2", "2"], ["3", "3"]], "the deck did not fix U1, U2 and U3 without a value: " +
          str(lines[index + 1:index + 6]))
    bc = bcs.replace_displacement_rotation(name, free_dofs=["U2", "U3"], fixed_dofs=bc.get_value("fixed_dofs")[:1])
    check(list(bc.get_value("fixed_dofs")) == ["U1"], "free_dofs did not unfix: " + str(bc.get_value("fixed_dofs")))
    # Values replace fixed dofs when every constrained one is given a value
    bc = bcs.replace_displacement_rotation(name, u1=0, u2=0)
    check(list(bc.get_value("fixed_dofs")) == [] and bc.get_value("u1") == 0, "a value did not replace Fixed")
    # The refusals name the argument and record nothing
    check_refused(lambda: bcs.replace_displacement_rotation(name, fixed_dofs=["U3"]), "cannot be combined",
                  "a fixed dof next to valued ones must be refused")
    check_refused(lambda: bcs.replace_displacement_rotation(name, u1=1, fixed_dofs=["U1"]), "'u1'",
                  "a dof both fixed and given a value must be refused")
    check_refused(lambda: bcs.replace_displacement_rotation(name, free_dofs=["U1"], fixed_dofs=["u1"]),
                  "'free_dofs'", "a dof both fixed and freed must be refused")
    check_raises(lambda: bcs.add_displacement_rotation(PREFIX + "Bad", pmx.create.region('NodeSetName', NODE_SET),
                                                       fixed_dofs=["U9"]),
                 "an unknown degree of freedom must be rejected")
    check(not bcs.contains(PREFIX + "Bad"), "a refused boundary condition was recorded")
    bc = bcs[name]
    check(list(bc.get_value("fixed_dofs")) == [] and bc.get_value("u1") == 0 and bc.get_value("u3") is None,
          "a refused replace changed the boundary condition")
    # The dialog offers Fixed for the displacements only, and a rotation would be stored as 0, so fixing one is
    # refused, naming the argument that fixes it instead
    for rotation in ["UR1", "ur2", "UR3"]:
        check_refused(lambda: bcs.add_displacement_rotation(PREFIX + "Bad",
                                                            pmx.create.region('NodeSetName', NODE_SET),
                                                            fixed_dofs=["U1", rotation]),
                      "'" + rotation.lower() + "'", "a fixed rotation must be refused")
        check(not bcs.contains(PREFIX + "Bad"), "a refused boundary condition was recorded")
    check_refused(lambda: bcs.replace_displacement_rotation(name, fixed_dofs=["UR3"]), "rotation",
                  "a fixed rotation must be refused by a replace")
    check(list(bcs[name].get_value("fixed_dofs")) == [], "a refused replace changed the boundary condition")
    bcs.remove([name])


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
    # The boundary condition would read a step below 1 as step 1
    check_refused(lambda: bcs.replace_submodel(PREFIX + "SM", step_id=0), "'step_id'",
                  "a submodel step below 1 must be refused")
    check(bcs[PREFIX + "SM"].get_value("step_id") == 2, "the refused replace changed the step")
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
    # A name is found regardless of case and passed on in the model's spelling
    bcs.propagate(name.upper())
    check(name in list(following.get_names()), "the boundary condition was not propagated in the model's spelling")
    check_raises(lambda: bcs.propagate("NoSuchBC"), "propagating an unknown name must be rejected")
    # The thermal step is the last one: the dialog propagates from it too and copies nothing, so a journal of
    # that replays
    last = pmx.model.steps[THERMAL_STEP].bcs
    check(last.contains(PREFIX + "T"), "the last step must carry the temperature boundary condition")
    names = list(last.get_names())
    last.propagate(PREFIX + "T")
    check(list(last.get_names()) == names, "propagating from the last step changed its boundary conditions")
    # A following step that does not take the type is skipped, as the dialog does
    frequency = PREFIX + "Frequency"
    pmx.model.steps.add_frequency(frequency)
    try:
        check(not following.contains(PREFIX + "SM"), "the following step must not carry the submodel yet")
        bcs.propagate(PREFIX + "SM")
        check(following.contains(PREFIX + "SM"), "the submodel was not propagated to the static step")
        check(not pmx.model.steps[frequency].bcs.contains(PREFIX + "SM"), "the frequency step took a submodel")
    finally:
        pmx.model.steps.remove([frequency])


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    setup()
    test_regions()
    test_displacement_rotation()
    test_complex_phase()
    test_displacement_rotation_distribution()
    test_displacement_rotation_distribution_local_system()
    test_fixed_dofs()
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
