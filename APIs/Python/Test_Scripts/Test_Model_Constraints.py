#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the constraint collection: the 9 constraint types, the master and slave regions, the
#reference point based ones and the collection verbs. A meshed model must be open before the run.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Constraints.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"
RP = PREFIX + "RP"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
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
    for collection in [pmx.model.constraints, pmx.model.reference_points, pmx.model.surfaces]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def node_selection(ids):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', ids)


def surface_selection(face):
    return pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [face]})


def element_selection(ids):
    return pmx.create.selection_by_ids('femodel', 'elements', 'elements', ids)


def setup():
    pmx.model.reference_points.add(RP, [0.5, 0.5, 2.0])


def test_tie():
    constraints = pmx.model.constraints
    check(len(constraints) == 0, "the model has no constraints yet")
    tie = constraints.add_tie(PREFIX + "Tie", surface_selection(1), surface_selection(2),
                              position_tolerance=0.01, adjust=False)
    check(tie.item_type == "tie", "unexpected type " + tie.item_type)
    # A selection based region is converted into an internal surface
    check(tie.master_region_type == "SurfaceName", "unexpected master region " + tie.master_region_type)
    check(tie.slave_region_type == "SurfaceName", "unexpected slave region " + tie.slave_region_type)
    check(abs(tie.get_value("position_tolerance") - 0.01) < 1e-12, "the position tolerance was not set")
    check(not tie.get_value("adjust"), "adjust was not set")
    check(tie.active and tie.visible, "a new constraint must be active and visible")
    # A value that is not given survives the replace
    tie = constraints.replace_tie(PREFIX + "Tie", adjust=True)
    check(tie.get_value("adjust"), "adjust was not replaced")
    check(abs(tie.get_value("position_tolerance") - 0.01) < 1e-12,
          "the position tolerance must survive a replace")
    # The type cannot change on a replace
    check_raises(lambda: constraints.replace_point_spring(PREFIX + "Tie"),
                 "replacing a tie by a point spring must be rejected")
    check_raises(lambda: constraints.add_tie(PREFIX + "Tie", surface_selection(1), surface_selection(2)),
                 "a duplicate name must be rejected")


def test_cyclic_symmetry():
    constraints = pmx.model.constraints
    # The result expansion region is only used for more than one graph sector, and the API rejects
    # the combination the controller would otherwise discard
    check_raises(lambda: constraints.add_cyclic_symmetry(PREFIX + "Bad", surface_selection(1),
                                                         surface_selection(2),
                                                         region=element_selection([1])),
                 "a result expansion region with one graph sector must be rejected")
    cs = constraints.add_cyclic_symmetry(PREFIX + "CS", surface_selection(1), surface_selection(2),
                                         region=element_selection([1]), number_of_graph_sectors=2,
                                         axis_point=[0, 0, 0],
                                         axis_direction=[0, 0, "1"], number_of_sectors=6)
    check(cs.item_type == "cyclic_symmetry", "unexpected type " + cs.item_type)
    # A vector argument with an expression reads back as one list under the argument name, as get_value
    # reads it, its plain components as numbers; a vector of plain values is not an equation
    equations = cs.get_equations()
    check(sorted(equations) == ["axis_direction"],
          "the equations are not the expressions named after the arguments: " + str(sorted(equations)))
    check(type(equations["axis_direction"]) is list and equations["axis_direction"] == [0, 0, "=1"],
          "the axis direction equations did not read back: " + repr(equations["axis_direction"]))
    check(cs.get_value("number_of_sectors") == 6, "the sector count was not set")
    # The sector angle is derived from the sector count
    check(abs(cs.get_value("sector_angle_deg") - 60.0) < 1e-9,
          "unexpected sector angle " + str(cs.get_value("sector_angle_deg")))
    check(list(cs.get_value("axis_direction")) == [0, 0, 1], "the axis direction was not set")
    # The result expansion region is a third region, separate from the two sector faces
    check(cs.get_value("number_of_graph_sectors") == 2, "the graph sector count was not set")
    check(cs.get_value("region_type") == "ElementSetName",
          "unexpected sector region " + str(cs.get_value("region_type")))
    check_raises(lambda: constraints.add_cyclic_symmetry(PREFIX + "Bad", surface_selection(1),
                                                         surface_selection(2), axis_direction=[0, 1]),
                 "a two value axis direction must be rejected")
    # More than one graph sector needs the result expansion region, which the controller would otherwise
    # refuse in its own words
    message = raised_message(lambda: constraints.add_cyclic_symmetry(PREFIX + "Bad", surface_selection(1),
                                                                     surface_selection(2), number_of_sectors=6,
                                                                     number_of_graph_sectors=3))
    check(message is not None and "number_of_graph_sectors" in message and "'region'" in message,
          "more graph sectors without an expansion region must be refused: " + str(message))
    check(not constraints.contains(PREFIX + "Bad"), "a refused cyclic symmetry was added")
    # More graph sectors than sectors is refused in the API's words, not the model's
    message = raised_message(lambda: constraints.add_cyclic_symmetry(PREFIX + "Bad", surface_selection(1),
                                                                     surface_selection(2), number_of_sectors=2,
                                                                     number_of_graph_sectors=3))
    check(message is not None and "number_of_graph_sectors" in message and "between 1" in message,
          "more graph sectors than sectors must be refused by the API: " + str(message))
    # A cyclic symmetry derives from a tie, yet a tie replace does not edit it
    message = raised_message(lambda: constraints.replace_tie(PREFIX + "CS", adjust=False))
    check(message is not None and "cyclic_symmetry" in message,
          "replacing a cyclic symmetry by a tie must be refused: " + str(message))
    # The final state is checked, so lowering the graph sectors must also drop the region
    message = raised_message(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_graph_sectors=1))
    check(message is not None and "number_of_graph_sectors" in message and "clear_expansion_region" in message,
          "one graph sector with a kept expansion region must be refused: " + str(message))
    check(constraints[PREFIX + "CS"].get_value("number_of_graph_sectors") == 2, "a refused replace changed the count")
    check_raises(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", region=element_selection([1]),
                                                             clear_expansion_region=True),
                 "a region and clear_expansion_region together must be refused")
    message = raised_message(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", clear_expansion_region=True))
    check(message is not None and "number_of_graph_sectors" in message,
          "clearing the region of two graph sectors must be refused: " + str(message))
    cs = constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_graph_sectors=1, clear_expansion_region=True)
    check(cs.get_value("number_of_graph_sectors") == 1, "the graph sector count was not lowered")
    check(cs.get_value("region_type") != "ElementSetName", "the expansion region was not cleared")
    # Raising the count again needs the region again
    check_raises(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_graph_sectors=3),
                 "more graph sectors without an expansion region must be refused on a replace")
    cs = constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_graph_sectors=3, region=element_selection([1]))
    check(cs.get_value("number_of_graph_sectors") == 3, "the graph sector count was not raised")
    check(cs.get_value("region_type") == "ElementSetName", "the expansion region was not set again")
    constraints.remove([PREFIX + "CS"])


def test_joint():
    constraints = pmx.model.constraints
    # A joint defaults to a hinge whose centre and axis are taken from the master surface
    joint = constraints.add_joint(PREFIX + "J", surface_selection(1), surface_selection(2))
    check(joint.item_type == "joint", "unexpected type " + joint.item_type)
    check(joint.master_region_type == "SurfaceName", "unexpected master region " + joint.master_region_type)
    check(joint.slave_region_type == "SurfaceName", "unexpected slave region " + joint.slave_region_type)
    check(joint.get_value("joint_type") == "Hinge", "a joint defaults to a hinge")
    check(joint.get_value("center_defined_by") == "MasterSurface", "the centre defaults to the master surface")
    check(joint.get_value("axis_defined_by") == "MasterSurface", "the axis defaults to the master surface")
    # A given centre or axis direction switches its definition to user defined
    joint = constraints.replace_joint(PREFIX + "J", joint_type="Slider", center=[0.5, 0.5, 0.5],
                                      axis_direction=[0, 0, 2])
    check(joint.get_value("joint_type") == "Slider", "the joint type was not replaced")
    check(joint.get_value("center_defined_by") == "Coordinates", "a given centre is given as coordinates")
    check(list(joint.get_value("center")) == [0.5, 0.5, 0.5], "the centre was not set")
    check(joint.get_value("axis_defined_by") == "UserDefined", "a given axis is user defined")
    check(list(joint.get_value("axis_direction"))[2] == 2, "the axis direction was not set")
    # A value that is not given survives the replace, and a definition can be switched back explicitly
    joint = constraints.replace_joint(PREFIX + "J", joint_type="Ball", axis_defined_by="MasterSurface")
    check(joint.get_value("joint_type") == "Ball", "the joint type was not replaced")
    check(joint.get_value("axis_defined_by") == "MasterSurface", "the axis definition was not replaced")
    check(joint.get_value("center_defined_by") == "Coordinates", "the centre must survive a replace")
    check(list(joint.get_value("center")) == [0.5, 0.5, 0.5], "the centre must survive a replace")
    # The formulation defaults to rigid bodies; a given stiffness switches the spring stiffness to user defined
    check(joint.get_value("formulation") == "RigidBody", "a joint defaults to the rigid body formulation")
    check(joint.get_value("stiffness_defined_by") == "FromModel", "the spring stiffness defaults to the model")
    joint = constraints.replace_joint(PREFIX + "J", formulation="Springs", stiffness=5e6)
    check(joint.get_value("formulation") == "Springs", "the formulation was not replaced")
    check(joint.get_value("stiffness_defined_by") == "UserDefined", "a given stiffness is user defined")
    check(abs(joint.get_value("stiffness") - 5e6) < 1e-6, "the stiffness was not set")
    joint = constraints.replace_joint(PREFIX + "J", stiffness_defined_by="FromModel")
    check(joint.get_value("stiffness_defined_by") == "FromModel", "the stiffness definition was not replaced")
    check(joint.get_value("formulation") == "Springs", "the formulation must survive a replace")
    check_raises(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1), surface_selection(2),
                                               formulation="Beams"),
                 "an unknown formulation must be rejected")
    # The joint centre point selection modes pick nodes in the dialog and are refused from a script
    check_raises(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1), surface_selection(2),
                                               center_defined_by="CircleCenter"),
                 "a point selection centre mode must be rejected")
    check_raises(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1), surface_selection(2),
                                               formulation="Springs", stiffness=0),
                 "a zero spring stiffness must be rejected")
    #
    check_raises(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1), surface_selection(2),
                                               joint_type="Screw"),
                 "an unknown joint type must be rejected")
    check_raises(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1), surface_selection(2),
                                               axis_direction=[0, 0, 0]),
                 "a zero axis direction must be rejected")
    check_raises(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1), surface_selection(2),
                                               center=[0, 0]),
                 "a two value centre must be rejected")
    check(not constraints.contains(PREFIX + "Bad"), "a rejected joint must not be added")
    # Swap works on a joint
    master_before = constraints[PREFIX + "J"].master_region_name
    constraints.swap_master_slave([PREFIX + "J"])
    check(constraints[PREFIX + "J"].slave_region_name == master_before, "the joint sides were not swapped")
    constraints.remove([PREFIX + "J"])


def test_reference_point_constraints():
    constraints = pmx.model.constraints
    # A rigid body ties the nodes to a reference point
    rb = constraints.add_rigid_body(PREFIX + "RB", RP, node_selection([1, 2, 3, 4]))
    check(rb.item_type == "rigid_body", "unexpected type " + rb.item_type)
    check(rb.master_region_type == "ReferencePointName",
          "the reference point is the master side, got " + rb.master_region_type)
    check(rb.master_region_name == RP, "unexpected reference point " + str(rb.master_region_name))
    check(rb.slave_region_type == "NodeSetName", "unexpected region " + rb.slave_region_type)
    check_raises(lambda: constraints.add_rigid_body(PREFIX + "Bad", "NoSuchRP", node_selection([1])),
                 "an unknown reference point must be rejected")
    # A kinematic coupling constrains the given degrees of freedom
    kc = constraints.add_kinematic_coupling(PREFIX + "KC", RP, surface_selection(1), u1=True, u2=False)
    check(kc.get_value("u1"), "u1 was not set")
    check(not kc.get_value("u2"), "u2 was not set")
    check(kc.get_value("u3"), "u3 stays on by default")
    # A distributing coupling adds the rotations
    dc = constraints.add_distributing_coupling(PREFIX + "DC", RP, surface_selection(2), ur1=False)
    check(not dc.get_value("ur1"), "ur1 was not set")
    check(dc.get_value("u1"), "u1 stays on by default")
    dc = constraints.replace_distributing_coupling(PREFIX + "DC", ur2=False)
    check(not dc.get_value("ur2"), "ur2 was not replaced")
    check(not dc.get_value("ur1"), "ur1 must survive a replace")
    check(dc.master_region_name == RP, "the reference point must survive a replace")


def test_springs_and_compression_only():
    constraints = pmx.model.constraints
    ps = constraints.add_point_spring(PREFIX + "PS", node_selection([5, 6]), k1=100, k3=300)
    check(ps.item_type == "point_spring", "unexpected type " + ps.item_type)
    check(ps.get_value("k1") == 100, "k1 was not set")
    check(ps.get_value("k3") == 300, "k3 was not set")
    check(ps.master_region_type == "NodeSetName", "a point spring region is a node set")
    #
    ss = constraints.add_surface_spring(PREFIX + "SS", surface_selection(1), k3=50,
                                        stiffness_per_area=True)
    check(ss.get_value("stiffness_per_area"), "stiffness_per_area was not set")
    check(ss.get_value("k3") == 50, "k3 was not set")
    check(ss.master_region_type == "SurfaceName", "a surface spring region is a surface")
    #
    co = constraints.add_compression_only(PREFIX + "CO", surface_selection(2), clearance=0.001,
                                          non_linear=True)
    check(co.item_type == "compression_only", "unexpected type " + co.item_type)
    check(abs(co.get_value("clearance") - 0.001) < 1e-12, "the clearance was not set")
    check(co.get_value("non_linear"), "non_linear was not set")
    # An equation is accepted where a value is
    pmx.model.parameters.add(PREFIX + "k", 25.0)
    ps = constraints.replace_point_spring(PREFIX + "PS", k2=PREFIX + "k * 2")
    check(ps.get_value("k2") == 50, "the equation did not evaluate")
    check(ps.get_equations()["k2"].replace(" ", "") == "=" + PREFIX + "k*2",
          "the k2 equation did not read back: " + str(ps.get_equations()))
    check(ps.get_value("k1") == 100, "k1 must survive a replace")


def test_collection_verbs():
    constraints = pmx.model.constraints
    name = PREFIX + "Tie"
    check(constraints.contains(name), "the constraint is missing")
    check(len([c for c in constraints]) == len(constraints), "iteration failed")
    check_raises(lambda: constraints["NoSuchConstraint"], "an unknown name must be rejected")
    # Swap works on the two sided constraints
    master_before = constraints[name].master_region_name
    slave_before = constraints[name].slave_region_name
    constraints.swap_master_slave([name])
    check(constraints[name].master_region_name == slave_before, "the sides were not swapped")
    constraints.swap_master_slave([name])
    check(constraints[name].master_region_name == master_before, "the sides were not swapped back")
    #
    constraints.deactivate([name])
    check(not constraints[name].active, "the constraint was not deactivated")
    constraints.activate([name])
    constraints.hide([name])
    check(not constraints[name].visible, "the constraint was not hidden")
    constraints.show([name])
    constraints.rename(name, name + "_R")
    check(constraints.contains(name + "_R"), "the constraint was not renamed")
    constraints.rename(name + "_R", name)
    #
    before = set(constraints.get_names())
    constraints.duplicate([name])
    duplicated = [n for n in constraints.get_names() if n not in before]
    check(len(duplicated) > 0, "the constraint was not duplicated")
    constraints.remove(duplicated)


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    setup()
    test_tie()
    test_cyclic_symmetry()
    test_joint()
    test_reference_point_constraints()
    test_springs_and_compression_only()
    test_collection_verbs()
    clean()
    if pmx.model.parameters.contains(PREFIX + "k"):
        pmx.model.parameters.remove([PREFIX + "k"])
    print("TEST PASSED")


main()
