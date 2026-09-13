#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the mesh setup item collection and the meshing call. A geometry part must be present, so the
#script is run against an imported geometry rather than an imported mesh. A sweep mesh item is only
#defined on a CAD solid, so box_and_cylinder.brep is imported for the checks that need one.
#
#  PrePoMax.com -f cube.stl -u MM_TON_S_C --script Test_Model_Meshing.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
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


def import_cad_box():
    """The 6-faced solid of box_and_cylinder.brep, which a sweep mesh item can be defined on."""
    before = list(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "box_and_cylinder.brep"))
    imported = [part for part in pmx.model.geometry.parts if part.name not in before]
    box = [part for part in imported if part.get_face_data().count == 6]
    check(len(box) > 0, "the CAD box was not imported")
    return box[0]


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
    # A sweep mesh item stores only the source surfaces; the model derives the sweep topology. It can
    # only be defined on a CAD solid, and is refused on the geometry of an STL part
    check_raises(lambda: items.add_sweep_mesh("PyTest_Bad", selection),
                 "a sweep mesh item on an STL part must be refused")
    box = import_cad_box()
    box_face = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {box.id: [5]})
    pmx.model.mesh.setup_items.add_sweep_mesh("PyTest_Sweep", box_face)
    check(items["PyTest_Sweep"].item_type == "SweepMesh", "the sweep mesh item was not added")
    check(items["PyTest_Sweep"].valid, "the sweep mesh item on the CAD box is not valid")
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


def test_dialog_targets(part_name):
    # The dialog points the picker at what the item type is defined on, and a session journal replays
    # exactly that, so a whole part mesher must take a part selection and not only a geometry one
    items = pmx.model.mesh.setup_items
    part = pmx.model.geometry.parts[part_name]
    part_selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
    #
    items.add_tetrahedral_gmsh("PyTest_Part_target", part_selection)
    check(items.contains("PyTest_Part_target"), "a part selection was refused by a whole part mesher")
    check(len(items["PyTest_Part_target"].get_geometry_ids()) > 0, "the part selection resolved to nothing")
    items.remove(["PyTest_Part_target"])
    # A local mesh size is defined on a geometry entity or an edge, so a part selection is not its target
    check_raises(lambda: items.add_local_mesh_size("PyTest_Bad", part_selection, element_size=0.25),
                 "a part selection must be refused by an element size")
    check_raises(lambda: items.add_local_mesh_size("PyTest_Bad", part_selection, number_of_elements=4),
                 "a part selection must be refused by a number of elements")
    # A sweep, extrude or revolve mesh is defined on the geometry surfaces it is built from, and only
    # on a CAD solid, so the box is what carries this one
    box = import_cad_box()
    box_surface = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {box.id: [5]})
    items.add_sweep_mesh("PyTest_Surface_target", box_surface)
    check(items.contains("PyTest_Surface_target"), "a geometry surface selection was refused by a sweep mesh")
    check(len(items["PyTest_Surface_target"].get_geometry_ids()) > 0, "the surface selection resolved to nothing")
    items.remove(["PyTest_Surface_target"])
    surface_selection = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {part.id: [1]})
    check_raises(lambda: items.add_tetrahedral_gmsh("PyTest_Bad", surface_selection),
                 "a geometry surface selection must be refused by a whole part mesher")
    # Only faces name a geometry surface
    check_raises(lambda: pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'parts', [part.id]),
                 "a part source must be refused for a geometry surface target")


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
    test_dialog_targets(part_name)
    test_meshing(part_name, selection)
    print("TEST PASSED")


main()
