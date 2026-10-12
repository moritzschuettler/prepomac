#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the mesh setup item collection and the meshing call. A geometry part must be present, so the
#script is run against an imported geometry rather than an imported mesh. A sweep mesh item is only
#defined on a CAD solid, so box_and_cylinder.brep is imported for the checks that need one.
#
#The gmsh tetrahedra of revolve_sphere_top.brep must have no element of zero volume, which gmsh leaves when its own
#optimization is off. On the box of box_and_cylinder.brep the local mesh sizes are kept or refused, never changed: a
#number of elements coarser than the maximum element size is kept, and an element size outside the minimum and the
#maximum element size, a number of elements making them shorter than the minimum, and sizes that the structured
#faces of the gmsh tetrahedral mesh cannot all keep on the edges they link are refused.
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


def check_raises(function, message, unknown_keyword=None):
    # Only the API's own refusal passes: a PythonAPIException, or for a call that deliberately passes the
    # keyword unknown_keyword, the TypeError of the keyword guard. Anything else, and a message carrying a
    # .NET stack trace, is a fault of the application that happened to stop the call
    try:
        function()
    except Exception as ex:
        kind, text = type(ex).__name__, str(ex)
        if unknown_keyword is None:
            expected, refusal = kind == "PythonAPIException", "a PythonAPIException"
        else:
            expected = kind == "TypeError" and "unexpected keyword argument '%s'" % unknown_keyword in text
            refusal = "the keyword guard's TypeError for '%s'" % unknown_keyword
        if not expected or "   at " in text:
            raise Exception("TEST FAILED: " + message + " - the refusal must be " + refusal +
                            " without a .NET stack trace, not " + kind + ": " + text[0:500])
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
    check(item.item_type == "meshing_parameters", "unexpected item type " + item.item_type)
    check(len(item.get_geometry_ids()) > 0, "the item has no geometry IDs")
    # Local mesh size
    pmx.model.mesh.setup_items.add_local_mesh_size(LOCAL_SIZE_NAME, selection, element_size=0.25)
    check(len(items) == count_before + 2, "the local mesh size was not added")
    check(items[LOCAL_SIZE_NAME].item_type == "local_mesh_size", "unexpected local mesh size type")
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
    check(items["PyTest_Sweep"].item_type == "sweep_mesh", "the sweep mesh item was not added")
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
    check_raises(lambda: pmx.model.mesh.generate("NoSuchPart"), "an unknown part must be rejected")
    #
    # Meshing several parts can succeed for some of them, so the call reports every part it was
    # given with the reason it was not meshed, and an empty reason for one that was
    report = pmx.model.mesh.generate(part_name)
    check(part_name in report, "the meshing report must name the part it was given: " + str(dict(report)))
    check(report[part_name] == "", "a part that meshed must report no reason: " + report[part_name])
    check(len(pmx.model.parts) > 0, "the meshing did not create a model part")
    mesh_part = pmx.model.parts.first()
    check(mesh_part.element_count > 0, "the meshed part has no elements")
    print("meshed:", mesh_part)
    #
    pmx.model.mesh.setup_items.remove([MESHING_PARAMETERS_NAME])


def generate_report(part_name):
    """What meshing the part reports, the refusal included: a call that meshes no part raises it."""
    try:
        return pmx.model.mesh.generate(part_name)[part_name]
    except Exception as ex:
        return str(ex)


def test_gmsh_tetrahedra_without_slivers():
    # gmsh's own optimization of the tetrahedra was switched off when no optimizer was chosen, the default, and
    # left tetrahedra of zero volume; this one came out with one
    items = pmx.model.mesh.setup_items
    before = list(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, "revolve_sphere_top.brep"))
    part = [part for part in pmx.model.geometry.parts if part.name not in before][0]
    selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
    items.add_meshing_parameters(MESHING_PARAMETERS_NAME, selection, relative_size=False, max_size=4.5,
                                 min_size=0.225, second_order=True)
    items.add_tetrahedral_gmsh("PyTest_Gmsh", selection)
    report = generate_report(part.name)
    check(report == "", "the gmsh tetrahedra of " + part.name + " reported: " + report)
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part.name][0]
    inverted = list(meshed.get_inverted_element_ids())
    check(len(inverted) == 0, "the gmsh tetrahedra of " + part.name + " have degenerate elements: " + str(inverted))
    items.remove([MESHING_PARAMETERS_NAME, "PyTest_Gmsh"])


def test_local_mesh_sizes_kept():
    # A size the user set is never changed to fit: an element size outside the minimum and the maximum element size,
    # so many elements that they are shorter than the minimum, and sizes the structured faces of the gmsh tetrahedral
    # mesh cannot all keep on the edges they link are refused, naming them
    items = pmx.model.mesh.setup_items
    box = import_cad_box()
    data = box.get_edge_data()
    by_length = {}
    for edge_id, geometry_id, length in zip([int(i) for i in data.ids], [int(i) for i in data.geometry_ids],
                                            [float(l) for l in data.lengths]):
        by_length.setdefault(round(length, 6), []).append((edge_id, geometry_id))
    length = max(key for key, edges in by_length.items() if len(edges) >= 2)
    (first, first_geometry_id), (second, _) = by_length[length][0:2]
    selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [box.id])
    items.add_meshing_parameters(MESHING_PARAMETERS_NAME, selection, relative_size=False, max_size=length / 4,
                                 min_size=length / 100, second_order=False)
    items.add_tetrahedral_gmsh("PyTest_Gmsh", selection)

    def edge(edge_id):
        return pmx.create.selection_by_ids('geometry', 'geometry', 'edges', {box.id: [edge_id]})

    def refusal(sizes, expected, what):
        for k, (edge_id, element_size, number_of_elements) in enumerate(sizes):
            items.add_local_mesh_size("PyTest_Edge_%d" % k, edge(edge_id), element_size=element_size,
                                      number_of_elements=number_of_elements)
        report = generate_report(box.name)
        print("refused:", what, "-", report.replace("\n", " ")[0:300])
        check(expected in report and "   at " not in report, what + " must be refused naming the conflict: " + report)
        items.remove(["PyTest_Edge_%d" % k for k in range(len(sizes))])

    items.add_local_mesh_size("PyTest_Edge_0", edge(first), number_of_elements=3)
    report = generate_report(box.name)
    check(report == "", "a number of elements coarser than the maximum element size must be kept: " + report)
    count = pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [first_geometry_id]).preview().count - 1
    check(count == 3, "the edge set to 3 elements has " + str(count))
    items.remove(["PyTest_Edge_0"])
    #
    refusal([(first, None, 3), (second, length / 20, None)], "cannot all be kept",
            "a number of elements against an element size on edges a structured face links")
    refusal([(first, length / 5, None), (second, length / 20, None)], "cannot all be kept",
            "two element sizes on edges a structured face links")
    refusal([(first, length / 200, None)], "smaller than the minimum element size",
            "an element size below the minimum element size")
    refusal([(first, length / 2, None)], "larger than the maximum element size",
            "an element size above the maximum element size")
    refusal([(first, None, 200)], "shorter than the minimum element size",
            "a number of elements making them shorter than the minimum element size")
    items.remove([MESHING_PARAMETERS_NAME, "PyTest_Gmsh"])


def main():
    check(pmx.model.is_open(), "a model must be open")
    part_name, selection = get_part_selection()
    test_setup_items(part_name, selection)
    test_dialog_targets(part_name)
    test_meshing(part_name, selection)
    test_gmsh_tetrahedra_without_slivers()
    test_local_mesh_sizes_kept()
    print("TEST PASSED")


main()
