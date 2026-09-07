#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the node and element operations, the part element types, activate/deactivate on the
#collection base, and the region arguments of the one-shot mesh tools. A meshed model must be open
#before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Mesh_Tools.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def test_nodes_and_elements():
    check(pmx.model.nodes.count > 0, "the model has no nodes")
    check(pmx.model.elements.count > 0, "the model has no elements")
    print("nodes:", pmx.model.nodes, "elements:", pmx.model.elements)
    # Renumbering keeps the counts
    node_count = pmx.model.nodes.count
    element_count = pmx.model.elements.count
    pmx.model.nodes.renumber(101)
    check(pmx.model.nodes.count == node_count, "renumbering changed the node count")
    check(min(pmx.model.parts.first().get_node_ids()) == 101, "the nodes were not renumbered")
    pmx.model.elements.renumber(201)
    check(pmx.model.elements.count == element_count, "renumbering changed the element count")
    check(min(pmx.model.parts.first().get_element_ids()) == 201, "the elements were not renumbered")
    #
    check_raises(lambda: pmx.model.nodes.renumber(0), "a start ID of zero must be rejected")
    check_raises(lambda: pmx.model.elements.renumber(-1), "a negative start ID must be rejected")


def test_element_types():
    parts = pmx.model.parts
    part_name = parts.get_names()[0]
    names = list(parts.get_element_type_names())
    check(len(names) > 0, "no element type names were returned")
    check("linear_hexa" in names, "linear_hexa is missing from " + str(names))
    #
    types = parts.get_element_types(part_name)
    check("linear_hexa" in list(types.get_names()), "linear_hexa is missing from the part element types")
    # The cube is a linear hexahedral mesh, so the type can be switched between the supported values
    parts.set_element_type(part_name, "linear_hexa", "C3D8R")
    parts.set_element_type(part_name, "linear_hexa", "C3D8")
    #
    check_raises(lambda: parts.set_element_type(part_name, "no_such_type", "C3D8"),
                 "an unknown element type must be rejected")
    check_raises(lambda: parts.set_element_type(part_name, "linear_hexa", "NotAValue"),
                 "an unknown element type value must be rejected")


def test_activate_deactivate():
    # Materials support activation
    for name in list(pmx.model.materials.get_names()):
        if name.startswith(PREFIX):
            pmx.model.materials.remove([name])
    pmx.model.materials.add(PREFIX + "Mat", pmx.create.elastic(210000, 0.3))
    pmx.model.materials.deactivate(PREFIX + "Mat")
    pmx.model.materials.activate(PREFIX + "Mat")
    pmx.model.materials.remove([PREFIX + "Mat"])
    # Model parts support activation
    part_name = pmx.model.parts.get_names()[0]
    pmx.model.parts.deactivate(part_name)
    check(not pmx.model.parts[part_name].active, "the part was not deactivated")
    pmx.model.parts.activate(part_name)
    check(pmx.model.parts[part_name].active, "the part was not activated")
    # Node sets do not - the controller has no branch for them
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [101, 102])
    pmx.model.node_sets.add(PREFIX + "NS", selection)
    check_raises(lambda: pmx.model.node_sets.deactivate(PREFIX + "NS"),
                 "deactivating a node set must be reported as not supported")
    pmx.model.node_sets.remove([PREFIX + "NS"])


def test_element_set_from_quality():
    for name in list(pmx.model.element_sets.get_names()):
        if name.startswith(PREFIX):
            pmx.model.element_sets.remove([name])
    check_raises(lambda: pmx.model.elements.create_element_set_from_quality(
        PREFIX + "Q", "Volume", "NoSuchPart", True, 0.0), "an unknown part must be rejected")


def test_mesh_tool_regions():
    # The remeshing dialog offers a selection or an element set, and the split dialog a part for the
    # base region and a surface for the splitter region. A region of any other type is refused, and
    # the refusal happens before the tool runs, so the mesh is unchanged.
    mesh = pmx.model.mesh
    element_selection = pmx.create.selection_by_ids('femodel', 'elements', 'elements', [201])
    node_count = pmx.model.nodes.count
    element_count = pmx.model.elements.count
    for name in list(pmx.model.node_sets.get_names()):
        if name.startswith(PREFIX):
            pmx.model.node_sets.remove([name])
    pmx.model.node_sets.add(PREFIX + "NS_T",
                            pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [101]))
    part_name = pmx.model.parts.get_names()[0]
    #
    check_raises(lambda: mesh.remesh_elements(None), "remeshing must reject a missing region")
    check_raises(lambda: mesh.remesh_elements(pmx.create.region('NodeSetName', PREFIX + "NS_T")),
                 "remeshing must reject a node set region")
    check_raises(lambda: mesh.remesh_elements(pmx.create.region('ElementSetName', 'NoSuchSet')),
                 "remeshing must reject an unknown element set")
    check_raises(lambda: mesh.split_part_mesh_using_surface(
        pmx.create.region('NodeSetName', PREFIX + "NS_T"), element_selection),
        "splitting must reject a node set as the base part region")
    check_raises(lambda: mesh.split_part_mesh_using_surface(
        pmx.create.region('PartName', part_name), pmx.create.region('PartName', part_name)),
        "splitting must reject a part as the splitter surface region")
    check_raises(lambda: mesh.split_part_mesh_using_surface(
        pmx.create.region('PartName', 'NoSuchPart'), element_selection),
        "splitting must reject an unknown part")
    #
    check(pmx.model.nodes.count == node_count, "a refused mesh tool changed the node count")
    check(pmx.model.elements.count == element_count, "a refused mesh tool changed the element count")
    pmx.model.node_sets.remove([PREFIX + "NS_T"])


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_nodes_and_elements()
    test_element_types()
    test_activate_deactivate()
    test_element_set_from_quality()
    test_mesh_tool_regions()
    print("TEST PASSED")


main()
