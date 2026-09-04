#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the mesh setup item collection and the meshing call. A geometry part must be present, so the
#script is run against an imported geometry rather than an imported mesh.
#
#  PrePoMax.com -f cube.stl -u MM_TON_S_C --script Test_Model_Meshing.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

MESHING_PARAMETERS_NAME = "PyTest_Meshing_parameters"
LOCAL_SIZE_NAME = "PyTest_Local_size"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def get_part_selection():
    parts = pmx.model.geometry.parts
    check(len(parts) > 0, "a geometry part is required")
    part = parts.first()
    return part.name, pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])


def test_setup_items(part_name, selection):
    items = pmx.model.mesh.setup_items
    for name in list(items.get_names()):
        if name.startswith("PyTest_"):
            items.remove([name])
    count_before = len(items)
    # Meshing parameters - only the given values override the application defaults
    item = pmx.model.mesh.setup_items.add_meshing_parameters(MESHING_PARAMETERS_NAME, selection,
                                                             max_size=0.5, min_size=0.1, second_order=False)
    check(items.contains(MESHING_PARAMETERS_NAME), "the meshing parameters were not added")
    check(item.item_type == "MeshingParameters", "unexpected item type " + item.item_type)
    check(len(item.get_geometry_ids()) > 0, "the item has no geometry IDs")
    # Local mesh size
    pmx.model.mesh.setup_items.add_local_mesh_size(LOCAL_SIZE_NAME, selection, element_size=0.25)
    check(len(items) == count_before + 2, "the local mesh size was not added")
    check(items[LOCAL_SIZE_NAME].item_type == "LocalMeshSize", "unexpected local mesh size type")
    # Either size or number of elements, never both and never neither
    check_raises(lambda: items.add_local_mesh_size("PyTest_Bad", selection),
                 "a local mesh size without a value must be rejected")
    check_raises(lambda: items.add_local_mesh_size("PyTest_Bad", selection, element_size=1, number_of_elements=2),
                 "a local mesh size with both values must be rejected")
    # A duplicate name is rejected
    check_raises(lambda: items.add_local_mesh_size(LOCAL_SIZE_NAME, selection, element_size=1),
                 "a duplicate name must be rejected")
    # A sweep mesh item stores only the source surfaces; the model derives the sweep topology
    pmx.model.mesh.setup_items.add_sweep_mesh("PyTest_Sweep", selection)
    check(items["PyTest_Sweep"].item_type == "SweepMesh", "the sweep mesh item was not added")
    items.remove(["PyTest_Sweep"])
    # An unknown enum value is rejected
    check_raises(lambda: items.add_tetrahedral_gmsh("PyTest_Bad", selection, algorithm_2d="NotAnAlgorithm"),
                 "an unknown Gmsh algorithm must be rejected")
    # Collection verbs
    items.rename(LOCAL_SIZE_NAME, "PyTest_Renamed")
    check(items.contains("PyTest_Renamed"), "the item was not renamed")
    items.duplicate(["PyTest_Renamed"])
    check(len(items) == count_before + 3, "the item was not duplicated")
    #
    remove_names = [n for n in items.get_names() if n.startswith("PyTest_")]
    items.remove(remove_names)
    check(len(items) == count_before, "the items were not removed")


def test_meshing(part_name, selection):
    pmx.model.mesh.setup_items.add_meshing_parameters(MESHING_PARAMETERS_NAME, selection, max_size=0.5)
    check_raises(lambda: pmx.model.mesh.create("NoSuchPart"), "an unknown part must be rejected")
    #
    pmx.model.mesh.create(part_name)
    check(len(pmx.model.parts) > 0, "the meshing did not create a model part")
    mesh_part = pmx.model.parts.first()
    check(mesh_part.element_count > 0, "the meshed part has no elements")
    print("meshed:", mesh_part)
    #
    pmx.model.mesh.setup_items.remove([MESHING_PARAMETERS_NAME])


def main():
    check(pmx.model.is_open(), "a model must be open")
    part_name, selection = get_part_selection()
    test_setup_items(part_name, selection)
    test_meshing(part_name, selection)
    print("TEST PASSED")


main()
