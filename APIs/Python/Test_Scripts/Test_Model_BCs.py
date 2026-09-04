#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the step scoped boundary condition collection: the 4 types, the selection and named region
#forms, and the step scoped collection verbs. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_BCs.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"
STEP = PREFIX + "Static"
NEXT_STEP = PREFIX + "Static_2"
THERMAL_STEP = PREFIX + "Thermal"
NODE_SET = PREFIX + "Bottom"


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
    names = [n for n in pmx.model.node_sets.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.node_sets.remove(names)


def setup():
    pmx.model.node_sets.add(NODE_SET, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))
    pmx.model.steps.add_static(STEP, time_period=1.0)
    pmx.model.steps.add_static(NEXT_STEP, time_period=1.0)
    pmx.model.steps.add_uncoupled_temp_disp(THERMAL_STEP)


def test_regions():
    bcs = pmx.model.steps[STEP].bcs
    check(bcs.step_name == STEP, "the collection must carry the step name")
    check(len(bcs) == 0, "a new step has no boundary conditions")
    # A selection based region is converted into an internal node set by the controller, so it reads
    # back as a node set region that points at a set the user did not create
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [5, 6])
    bc = bcs.add_fixed(PREFIX + "Fix_Sel", selection)
    check(bc.boundary_condition_type == "FixedBC", "unexpected type " + bc.boundary_condition_type)
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


def test_submodel():
    bcs = pmx.model.steps[STEP].bcs
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])
    bc = bcs.add_submodel(PREFIX + "SM", selection, step_number=2, u1=True, u3=True)
    check(bc.get_value("step_number") == 2, "the step number was not set")
    check(bc.get_value("u1"), "u1 was not set")
    check(not bc.get_value("u2"), "u2 must stay false")
    bc = bcs.replace_submodel(PREFIX + "SM", u2=True)
    check(bc.get_value("u1"), "u1 must survive a replace")
    check(bc.get_value("u2"), "u2 was not replaced")


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


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    setup()
    test_regions()
    test_displacement_rotation()
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
