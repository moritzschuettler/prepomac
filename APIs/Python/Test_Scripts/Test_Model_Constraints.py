#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the constraint collection: the 9 constraint types, the master and slave regions, the
#reference point based ones, the colors each type has and the collection verbs. A meshed model must be
#open before the run.
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


def part_selection():
    # The result expansion region dialog picks parts
    return pmx.create.selection_by_ids('femodel', 'parts', 'parts', [pmx.model.parts.first().id])


def geometry_face_selection(face):
    """The selection the rigid body dialog records for a picked face: its geometry IDs, as the journal writes them."""
    geometry_ids = list(pmx.create.selection_by_ids('femodel', 'geometry', 'surfaces', {1: [face]}).get_ids())
    check(len(geometry_ids) > 0, "the face resolved to no geometry IDs")
    return pmx.create.selection_by_ids('femodel', 'geometry', 'geometry', geometry_ids)


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
    # The model refuses a negative position tolerance in words that name no argument
    message = raised_message(lambda: constraints.replace_tie(PREFIX + "Tie", position_tolerance=-0.01))
    check(message is not None and "'position_tolerance'" in message,
          "a negative position tolerance must be refused naming the argument: " + str(message))
    check(abs(constraints[PREFIX + "Tie"].get_value("position_tolerance") - 0.01) < 1e-12,
          "a refused replace changed the tie")
    # Default reads back as None, which keeps the value on a replace, so float('nan') sets it back
    tie = constraints.replace_tie(PREFIX + "Tie", position_tolerance=float("nan"))
    check(tie.get_value("position_tolerance") is None,
          "float('nan') did not set the position tolerance back to Default: " + str(tie.get_value("position_tolerance")))
    tie = constraints.replace_tie(PREFIX + "Tie", position_tolerance=tie.get_value("position_tolerance"))
    check(tie.get_value("position_tolerance") is None, "the Default read back did not go back into the replace")
    tie = constraints.replace_tie(PREFIX + "Tie", position_tolerance=0.01)
    check(abs(tie.get_value("position_tolerance") - 0.01) < 1e-12, "the position tolerance was not set again")


def test_cyclic_symmetry():
    constraints = pmx.model.constraints
    # The result expansion region is only used for more than one graph sector, and the API rejects
    # the combination the controller would otherwise discard
    check_raises(lambda: constraints.add_cyclic_symmetry(PREFIX + "Bad", surface_selection(1),
                                                         surface_selection(2),
                                                         region=part_selection()),
                 "a result expansion region with one graph sector must be rejected")
    cs = constraints.add_cyclic_symmetry(PREFIX + "CS", surface_selection(1), surface_selection(2),
                                         region=part_selection(), number_of_graph_sectors=2,
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
    check(abs(cs.sector_angle_deg - 60.0) < 1e-9,
          "unexpected sector angle " + str(cs.sector_angle_deg))
    check(list(cs.get_value("axis_direction")) == [0, 0, 1], "the axis direction was not set")
    # The result expansion region is a third region, separate from the two sector faces
    check(cs.get_value("number_of_graph_sectors") == 2, "the graph sector count was not set")
    check(cs.region_type == "ElementSetName",
          "unexpected sector region " + str(cs.region_type))
    # The picked part becomes an element set made of it, which covers every element of the part
    element_ids = sorted(pmx.model.element_sets[cs.region_name].get_element_ids())
    check(element_ids == sorted(pmx.model.parts.first().get_element_ids()),
          "the result expansion region resolved to the elements " + str(element_ids))
    # The controller reads the set as part IDs, so an element selection is refused, while the dialog names a
    # part or an element set
    message = raised_message(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", region=element_selection([1])))
    check(message is not None and "requires target 'parts'" in message,
          "an element selection of the expansion region must be refused naming the target: " + str(message))
    message = raised_message(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS",
                                                                         region=pmx.create.region('NodeSetName', 'Any')))
    check(message is not None and "NodeSetName region was given" in message,
          "a node set expansion region must be refused: " + str(message))
    part_name = pmx.model.parts.first().name
    cs = constraints.replace_cyclic_symmetry(PREFIX + "CS", region=pmx.create.region('PartName', part_name))
    check(cs.region_type == "PartName" and cs.region_name == part_name, "the part expansion region was not set")
    cs = constraints.replace_cyclic_symmetry(PREFIX + "CS", region=part_selection())
    check(cs.region_type == "ElementSetName", "the expansion region was not picked again")
    check_raises(lambda: constraints.add_cyclic_symmetry(PREFIX + "Bad", surface_selection(1),
                                                         surface_selection(2), axis_direction=[0, 1]),
                 "a two value axis direction must be rejected")
    # An axis of zero length is no axis, which the dialog refuses as well
    message = raised_message(lambda: constraints.add_cyclic_symmetry(PREFIX + "Bad", surface_selection(1),
                                                                     surface_selection(2), axis_direction=[0, 0, 0]))
    check(message is not None and "'axis_direction'" in message and "non-zero" in message,
          "a zero cyclic symmetry axis must be refused naming the argument: " + str(message))
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
    check_raises(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", region=part_selection(),
                                                             clear_expansion_region=True),
                 "a region and clear_expansion_region together must be refused")
    message = raised_message(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", clear_expansion_region=True))
    check(message is not None and "number_of_graph_sectors" in message,
          "clearing the region of two graph sectors must be refused: " + str(message))
    cs = constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_graph_sectors=1, clear_expansion_region=True)
    check(cs.get_value("number_of_graph_sectors") == 1, "the graph sector count was not lowered")
    check(cs.region_type is None, "the expansion region was not cleared: " + str(cs.region_type))
    # Raising the count again needs the region again
    check_raises(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_graph_sectors=3),
                 "more graph sectors without an expansion region must be refused on a replace")
    cs = constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_graph_sectors=3, region=part_selection())
    check(cs.get_value("number_of_graph_sectors") == 3, "the graph sector count was not raised")
    check(cs.region_type == "ElementSetName", "the expansion region was not set again")
    # The model would lower the kept graph sectors to fewer sectors on its own, so the merged pair is refused
    message = raised_message(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_sectors=2))
    check(message is not None and "'number_of_graph_sectors'" in message,
          "fewer sectors than the kept graph sectors must be refused naming the graph sectors: " + str(message))
    message = raised_message(lambda: constraints.replace_cyclic_symmetry(PREFIX + "CS", number_of_sectors=1))
    check(message is not None and "'number_of_sectors'" in message,
          "a single sector must be refused naming the argument: " + str(message))
    check(constraints[PREFIX + "CS"].get_value("number_of_sectors") == 6, "a refused replace changed the sectors")
    check(constraints[PREFIX + "CS"].get_value("number_of_graph_sectors") == 3,
          "a refused replace changed the graph sectors")
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
    joint = constraints.replace_joint(PREFIX + "J", axis_defined_by="MasterSurface")
    check(joint.get_value("axis_defined_by") == "MasterSurface", "the axis definition was not replaced")
    joint = constraints.replace_joint(PREFIX + "J", joint_type="Ball")
    check(joint.get_value("joint_type") == "Ball", "the joint type was not replaced")
    # A ball has no axis, so the dialog hides its definition: it is not read back and it is refused
    check("axis_defined_by" not in list(joint.get_value_names()),
          "a ball joint read back an axis definition: " + str(list(joint.get_value_names())))
    message = raised_message(lambda: constraints.replace_joint(PREFIX + "J", axis_defined_by="UserDefined"))
    check(message is not None and "'axis_defined_by'" in message,
          "an axis definition of a ball joint must be refused naming the argument: " + str(message))
    check(joint.get_value("center_defined_by") == "Coordinates", "the centre must survive a replace")
    check(list(joint.get_value("center")) == [0.5, 0.5, 0.5], "the centre must survive a replace")
    # The formulation defaults to rigid bodies; a given stiffness switches the spring stiffness to user defined
    check(joint.get_value("formulation") == "RigidBody", "a joint defaults to the rigid body formulation")
    # The rigid body formulation refuses a spring stiffness, so none reads back, and a ball has no axis direction
    names = list(joint.get_value_names())
    check("stiffness_defined_by" not in names and "stiffness" not in names and "axis_direction" not in names,
          "a ball joint of the rigid body formulation read back values its replace refuses: " + str(names))
    joint = constraints.replace_joint(PREFIX + "J", formulation="Springs")
    check(joint.get_value("stiffness_defined_by") == "FromModel", "the spring stiffness defaults to the model")
    check("stiffness" not in list(joint.get_value_names()), "a stiffness from the model must not read back")
    joint = constraints.replace_joint(PREFIX + "J", stiffness=5e6)
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
    # The rigid body formulation has no springs and a ball or a weld has no axis, so those values are refused
    message = raised_message(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1), surface_selection(2),
                                                           stiffness=5e6))
    check(message is not None and "'stiffness'" in message,
          "a stiffness with the rigid body formulation must be refused: " + str(message))
    message = raised_message(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1), surface_selection(2),
                                                           formulation="RigidBody", stiffness_defined_by="UserDefined"))
    check(message is not None and "'stiffness_defined_by'" in message,
          "a stiffness definition with the rigid body formulation must be refused: " + str(message))
    for joint_type in ["Ball", "Weld"]:
        message = raised_message(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1),
                                                               surface_selection(2), joint_type=joint_type,
                                                               axis_direction=[0, 0, 1]))
        check(message is not None and "'axis_direction'" in message,
              "an axis direction for a " + joint_type + " joint must be refused: " + str(message))
        message = raised_message(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1),
                                                               surface_selection(2), joint_type=joint_type,
                                                               axis_defined_by="MasterSurface"))
        check(message is not None and "'axis_defined_by'" in message,
              "an axis definition for a " + joint_type + " joint must be refused: " + str(message))
    message = raised_message(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1),
                                                           surface_selection(2), axis_direction=[0, 0, 0]))
    check(message is not None and "'axis_direction'" in message and "non-zero" in message,
          "a zero joint axis must be refused naming the argument: " + str(message))
    # A value given with a definition that does not use it would be stored and never used, so both are named
    for arguments in [{"center": [0, 0, 0], "center_defined_by": "MasterSurface"},
                      {"axis_direction": [0, 0, 1], "axis_defined_by": "MasterSurface"},
                      {"formulation": "Springs", "stiffness": 5e6, "stiffness_defined_by": "FromModel"}]:
        names = [name for name in arguments if name != "formulation"]
        message = raised_message(lambda: constraints.add_joint(PREFIX + "Bad", surface_selection(1),
                                                               surface_selection(2), **arguments))
        check(message is not None and all("'" + name + "'" in message for name in names),
              "the unused value of " + str(arguments) + " must be refused naming both: " + str(message))
    message = raised_message(lambda: constraints.replace_joint(PREFIX + "J", center=[1, 1, 1],
                                                               center_defined_by="MasterSurface"))
    check(message is not None and "'center'" in message and "'center_defined_by'" in message,
          "a replaced centre with a master surface centre must be refused: " + str(message))
    check(list(constraints[PREFIX + "J"].get_value("center")) == [0.5, 0.5, 0.5],
          "a refused replace changed the joint centre")
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
    # The rigid body dialog picks geometry, so its journal hands that selection back, and it must resolve to
    # the nodes the same face selected by nodes gives
    face_nodes = sorted(pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {1: [1]}).get_ids())
    rb_geometry = constraints.add_rigid_body(PREFIX + "RB_Geo", RP, geometry_face_selection(1))
    check(rb_geometry.slave_region_type == "NodeSetName", "unexpected region " + rb_geometry.slave_region_type)
    node_ids = sorted(pmx.model.node_sets[rb_geometry.slave_region_name].get_node_ids())
    check(node_ids == face_nodes, "the geometry selection resolved to the nodes " + str(node_ids))
    rb_geometry = constraints.replace_rigid_body(PREFIX + "RB_Geo", region=geometry_face_selection(2))
    face_2_nodes = sorted(pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {1: [2]}).get_ids())
    check(sorted(pmx.model.node_sets[rb_geometry.slave_region_name].get_node_ids()) == face_2_nodes,
          "the replaced geometry selection did not resolve to the nodes of the face")
    # The geometry target adds no region type, so a named region is still checked against the node ones
    message = raised_message(lambda: constraints.add_rigid_body(PREFIX + "Bad", RP,
                                                                pmx.create.region('ElementSetName', 'Any')))
    check(message is not None and "ElementSetName region was given" in message,
          "an element set region must be refused: " + str(message))
    message = raised_message(lambda: constraints.add_rigid_body(PREFIX + "Bad", RP, element_selection([1])))
    check(message is not None and "'nodes' or 'geometry'" in message,
          "an element selection must be refused naming the targets: " + str(message))
    check(not constraints.contains(PREFIX + "Bad"), "a refused rigid body was recorded")
    constraints.remove([PREFIX + "RB_Geo"])
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
    # The dialog refuses a coupling without a degree of freedom, in these words, and so does the API, judging the
    # coupling a replace ends with
    count_before = len(constraints)
    for function, fragments in [
            (lambda: constraints.add_kinematic_coupling(PREFIX + "Bad", RP, surface_selection(3), u1=False,
                                                        u2=False, u3=False),
             ["At least one kinematic coupling degree of freedom must be coupled", "'u1'"]),
            (lambda: constraints.replace_kinematic_coupling(PREFIX + "KC", u1=False, u3=False),
             ["At least one kinematic coupling degree of freedom must be coupled", "'u3'"]),
            (lambda: constraints.add_distributing_coupling(PREFIX + "Bad", RP, surface_selection(3), u1=False,
                                                           u2=False, u3=False, ur1=False, ur2=False, ur3=False),
             ["At least one distributing coupling degree of freedom must be distributed", "'ur3'"]),
            (lambda: constraints.replace_distributing_coupling(PREFIX + "DC", u1=False, u2=False, u3=False,
                                                               ur3=False),
             ["At least one distributing coupling degree of freedom must be distributed", "'u1'"])]:
        message = raised_message(function)
        check(message is not None and all(fragment in message for fragment in fragments),
              "a coupling without a degree of freedom must be refused with " + str(fragments) + ": " + str(message))
    check(len(constraints) == count_before and not constraints.contains(PREFIX + "Bad"),
          "a refused coupling was recorded")
    check(constraints[PREFIX + "KC"].get_value("u1") and constraints[PREFIX + "DC"].get_value("u1"),
          "a refused replace changed a coupling")
    # A reference point named in another case is stored in the model's spelling, because the controller
    # follows a renamed reference point by comparing the names exactly
    rb = constraints.add_rigid_body(PREFIX + "RB_C", RP.lower(), node_selection([5, 6]))
    check(rb.master_region_name == RP,
          "the reference point must be stored in the model's spelling: " + str(rb.master_region_name))
    dc = constraints.replace_distributing_coupling(PREFIX + "DC", reference_point_name=RP.upper())
    check(dc.master_region_name == RP,
          "a replace must store the reference point in the model's spelling: " + str(dc.master_region_name))
    constraints.remove([PREFIX + "RB_C"])


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
    # The values the dialog offers as Default read back as None and are set back to Default by float('nan')
    check(co.get_value("spring_stiffness") is None and co.get_value("tensile_force_at_negative_infinity") is None,
          "the compression only defaults must read back as None")
    co = constraints.replace_compression_only(PREFIX + "CO", spring_stiffness=1000,
                                              tensile_force_at_negative_infinity=5)
    check(co.get_value("spring_stiffness") == 1000 and co.get_value("tensile_force_at_negative_infinity") == 5,
          "the compression only values were not set")
    co = constraints.replace_compression_only(PREFIX + "CO", spring_stiffness=float("nan"))
    check(co.get_value("spring_stiffness") is None, "float('nan') did not set the spring stiffness back to Default")
    check(co.get_value("tensile_force_at_negative_infinity") == 5, "a value that was not given was not kept")
    co = constraints.replace_compression_only(PREFIX + "CO", tensile_force_at_negative_infinity=float("nan"))
    check(co.get_value("tensile_force_at_negative_infinity") is None,
          "float('nan') did not set the tensile force back to Default")
    # An equation is accepted where a value is
    pmx.model.parameters.add(PREFIX + "k", 25.0)
    ps = constraints.replace_point_spring(PREFIX + "PS", k2=PREFIX + "k * 2")
    check(ps.get_value("k2") == 50, "the equation did not evaluate")
    check(ps.get_equations()["k2"].replace(" ", "") == "=" + PREFIX + "k*2",
          "the k2 equation did not read back: " + str(ps.get_equations()))
    check(ps.get_value("k1") == 100, "k1 must survive a replace")
    # The model sets a negative stiffness to zero, so it is refused instead, also through an equation
    for arguments in [{"k1": -100}, {"k3": "-" + PREFIX + "k"}]:
        message = raised_message(lambda: constraints.replace_point_spring(PREFIX + "PS", **arguments))
        check(message is not None and "'" + list(arguments)[0] + "'" in message,
              "a negative spring stiffness must be refused naming the argument: " + str(message))
    message = raised_message(lambda: constraints.add_surface_spring(PREFIX + "Bad", surface_selection(3), k2=-1))
    check(message is not None and "'k2'" in message, "a negative surface spring stiffness must be refused: " +
          str(message))
    check(not constraints.contains(PREFIX + "Bad"), "a refused spring was added")
    check(constraints[PREFIX + "PS"].get_value("k1") == 100, "a refused replace changed the spring")
    # The dialog refuses a spring without a stiffness, in these words, and so does the API, judging the spring a
    # replace ends with and the value an expression evaluates to
    for function in [lambda: constraints.add_point_spring(PREFIX + "Bad", node_selection([7]), k1=0),
                     lambda: constraints.add_surface_spring(PREFIX + "Bad", surface_selection(3), k2="=1 - 1"),
                     lambda: constraints.replace_point_spring(PREFIX + "PS", k1=0, k2=0, k3=0)]:
        message = raised_message(function)
        check(message is not None and "At least one stiffness must be larger than 0" in message and
              "'k1'" in message, "a spring without a stiffness must be refused naming k1 to k3: " + str(message))
    check(not constraints.contains(PREFIX + "Bad"), "a refused spring was added")
    check(constraints[PREFIX + "PS"].get_value("k1") == 100, "a refused replace changed the spring")


def test_colors():
    constraints = pmx.model.constraints
    yellow = [255, 255, 0, 255]
    # A tie colors its two surfaces, as the dialog shows them
    tie = constraints[PREFIX + "Tie"]
    check(list(tie.master_color) == yellow and list(tie.slave_color) == yellow,
          "unexpected default tie colors " + str(list(tie.master_color)) + ", " + str(list(tie.slave_color)))
    tie = constraints.replace_tie(PREFIX + "Tie", master_color=[10, 20, 30], slave_color="Blue")
    check(list(tie.master_color) == [10, 20, 30, 255], "the master color was not set: " + str(list(tie.master_color)))
    check(list(tie.slave_color) == [0, 0, 255, 255], "the slave color was not set: " + str(list(tie.slave_color)))
    tie = constraints.replace_tie(PREFIX + "Tie", adjust=True)
    check(list(tie.master_color) == [10, 20, 30, 255], "a replace that does not name the colors must keep them")
    tie = constraints.replace_tie(PREFIX + "Tie", master_color=tie.slave_color, slave_color=tie.master_color)
    check(list(tie.master_color) == [0, 0, 255, 255] and list(tie.slave_color) == [10, 20, 30, 255],
          "the colors read back must go back into the arguments")
    # Every other constraint has one color, which is refused on a tie and the other way around
    message = raised_message(lambda: tie.color)
    check(message is not None and "master_color" in message, "a tie has no single color: " + str(message))
    ps = constraints.replace_point_spring(PREFIX + "PS", color=[1, 2, 3, 128])
    check(list(ps.color) == [1, 2, 3, 128], "the point spring color was not set: " + str(list(ps.color)))
    message = raised_message(lambda: ps.master_color)
    check(message is not None and "has color" in message, "a point spring has no master color: " + str(message))
    rb = constraints.replace_rigid_body(PREFIX + "RB", color=ps.color)
    check(list(rb.color) == [1, 2, 3, 128], "the color read back must go back into the argument")
    for add in [lambda: constraints.add_compression_only(PREFIX + "Bad", surface_selection(3), color="Green"),
                lambda: constraints.add_kinematic_coupling(PREFIX + "Bad", RP, surface_selection(3), color="Green")]:
        item = add()
        check(list(item.color) == [0, 128, 0, 255], "the color of a new constraint was not set: " +
              str(list(item.color)))
        constraints.remove([PREFIX + "Bad"])
    # A malformed color is refused naming the argument, and records nothing
    count_before = len(constraints)
    message = raised_message(lambda: constraints.add_tie(PREFIX + "Bad", surface_selection(3), surface_selection(4),
                                                         slave_color=[1, 2]))
    check(message is not None and "'slave_color'" in message,
          "a two channel color must be refused naming the argument: " + str(message))
    message = raised_message(lambda: constraints.replace_surface_spring(PREFIX + "SS", color="NoSuchColor"))
    check(message is not None and "'color'" in message, "an unknown color name must be refused: " + str(message))
    check(len(constraints) == count_before, "a refused constraint was recorded")


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
    test_colors()
    test_collection_verbs()
    clean()
    if pmx.model.parameters.contains(PREFIX + "k"):
        pmx.model.parameters.remove([PREFIX + "k"])
    print("TEST PASSED")


main()
