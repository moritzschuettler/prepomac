#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the mesh setup item replacements and the mesher settings: a replacement keeps the region and
#every value it is not told about.
#
#Every item type is added on geometry it can actually be defined on, because an item the model cannot
#honour is refused the way the dialog refuses it. box_and_cylinder.brep carries the CAD box the
#transfinite and sweep items need and a quarter cylinder; the two
#STL parts are imported for the shell items and the tetrahedral one.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Meshing_Coverage.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
PREFIX = "PyTest_Coverage_"


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


def settings(items, name):
    return items[name].get_settings()


def test_meshing_parameters(items, selection):
    items.add_meshing_parameters(PREFIX + "Parameters", selection, max_size=0.5, grading=0.4)
    # A replacement keeps the region it was created from and every value it is not told about
    items.replace_meshing_parameters(PREFIX + "Parameters", max_size=0.25)
    values = settings(items, PREFIX + "Parameters")
    check(values["max_size"] == 0.25, "the maximum size was not replaced: " + str(values["max_size"]))
    check(values["grading"] == 0.4, "the omitted grading was not kept: " + str(values))
    check(len(items[PREFIX + "Parameters"].get_geometry_ids()) > 0, "the region was lost")


def test_netgen_mesh(items, selection, shell_selection):
    items.add_netgen_mesh(PREFIX + "Netgen", selection, optimize_steps_2d=4, transfinite_four_sided=True)
    items.replace_netgen_mesh(PREFIX + "Netgen", optimize_steps_3d=5)
    values = settings(items, PREFIX + "Netgen")
    check(values["optimize_steps_3d"] == 5, "the Netgen 3-D optimization steps were not replaced")
    check(values["optimize_steps_2d"] == 4, "the omitted Netgen optimization steps were not kept: " + str(values))
    check(values["transfinite_four_sided"] is True and values["transfinite_three_sided"] == "No",
          "the omitted Netgen transfinite settings were not kept: " + str(values))
    check(items[PREFIX + "Netgen"].mesher_type == "Netgen", "the Netgen mesh item does not select Netgen")
    check(len(items[PREFIX + "Netgen"].get_geometry_ids()) > 0, "the Netgen region was lost")
    check_raises(lambda: items.replace_netgen_mesh(PREFIX + "Netgen", optimize_steps_2d=2.5),
                 "a fractional optimization step count was accepted")
    # The item meshes shell parts as well as solid ones
    items.add_netgen_mesh(PREFIX + "NetgenShell", shell_selection, quad_dominated=True)
    check(settings(items, PREFIX + "NetgenShell")["quad_dominated"] is True,
          "the Netgen quad-dominated setting was not stored")


def test_local_mesh_size(items, selection):
    items.add_local_mesh_size(PREFIX + "Local", selection, element_size=0.4)
    items.replace_local_mesh_size(PREFIX + "Local", element_size=0.2)
    values = settings(items, PREFIX + "Local")
    check(values["element_size"] == 0.2, "the element size was not replaced")
    # Only the value of the stored type is reported, so the settings go back into the replace that refuses both
    check("number_of_elements" not in values and "type" not in values,
          "a local mesh size reported a value its type does not use: " + str(values))
    items.replace_local_mesh_size(PREFIX + "Local", element_size=values["element_size"])
    items.replace_local_mesh_size(PREFIX + "Local", number_of_elements=3)
    values = settings(items, PREFIX + "Local")
    check(values.get("number_of_elements") == 3 and "element_size" not in values,
          "a local mesh size by count reported the size it does not use: " + str(values))
    items.replace_local_mesh_size(PREFIX + "Local", element_size=0.2)
    check_raises(lambda: items.replace_local_mesh_size(PREFIX + "Local", number_of_elements=2.5),
                 "a fractional element count was accepted")
    check_raises(lambda: items.replace_local_mesh_size(PREFIX + "Local", element_size=1, number_of_elements=2),
                 "two mesh size definitions at once were accepted")


def test_shell_mmg(items, selection):
    items.add_shell_mmg(PREFIX + "MMG", selection, relative_size=True, relative_hausdorff=0.002,
                        keep_model_edges=False)
    items.replace_shell_mmg(PREFIX + "MMG", relative_hausdorff=0.003)
    values = settings(items, PREFIX + "MMG")
    check(values["relative_size"], "the MMG Hausdorff definition was not stored")
    check(values["relative_hausdorff"] == 0.003, "the MMG Hausdorff factor was not replaced")
    check(values["keep_model_edges"] is False, "the omitted MMG edge setting was not kept")
    check(items[PREFIX + "MMG"].mesher_type == "Mmg", "the shell MMG item does not select MMG")


def test_thicken_shell(items, selection):
    items.add_thicken_shell_mesh(PREFIX + "Thicken", selection, thickness=1.0, number_of_layers=3)
    items.replace_thicken_shell_mesh(PREFIX + "Thicken", thickness=2.0)
    values = settings(items, PREFIX + "Thicken")
    check(values["thickness"] == 2.0, "the thickness was not replaced")
    check(values["number_of_layers"] == 3, "the omitted layer count was not kept")


def test_gmsh_items(items, selection, shell_selection, box_selection):
    items.add_tetrahedral_gmsh(PREFIX + "Tet", selection, algorithm_recombine="Simple",
                               transfinite_three_sided="Polar")
    items.replace_tetrahedral_gmsh(PREFIX + "Tet", optimize_high_order="HighOrder")
    values = settings(items, PREFIX + "Tet")
    check(values["algorithm_recombine"] == "Simple", "the omitted recombine algorithm was not kept")
    check(values["transfinite_three_sided"] == "Polar", "the omitted transfinite setting was not kept")
    #
    items.add_shell_gmsh(PREFIX + "Shell", shell_selection, transfinite_angle_deg=120.0)
    items.replace_shell_gmsh(PREFIX + "Shell", algorithm_recombine="Blossom")
    check(settings(items, PREFIX + "Shell")["transfinite_angle_deg"] == 120.0,
          "the omitted transfinite angle was not kept")
    #
    items.add_transfinite_mesh(PREFIX + "Transfinite", box_selection, recombine=True,
                               allow_pyramid_elements=True)
    items.replace_transfinite_mesh(PREFIX + "Transfinite", transfinite_angle_deg=110.0)
    values = settings(items, PREFIX + "Transfinite")
    check(values["recombine"] is True and "algorithm_recombine" not in values,
          "the omitted recombine switch was not kept, or not read back as the switch: " + str(values))
    check(values["transfinite_angle_deg"] == 110.0, "the transfinite angle was not replaced")
    check(values["allow_pyramid_elements"] is True, "the omitted pyramid element switch was not kept")
    # The read back switch goes straight back into the replace
    items.replace_transfinite_mesh(PREFIX + "Transfinite", recombine=values["recombine"],
                                   transfinite_angle_deg=values["transfinite_angle_deg"],
                                   allow_pyramid_elements=values["allow_pyramid_elements"])
    check(settings(items, PREFIX + "Transfinite")["recombine"] is True, "the read back switch did not round trip")


def test_derived_arguments(items, selection):
    items.add_sweep_mesh(PREFIX + "Sweep", selection, number_of_global_smooth_steps=1)
    items.replace_sweep_mesh(PREFIX + "Sweep", transfinite_angle_deg=100.0)
    check(settings(items, PREFIX + "Sweep")["number_of_global_smooth_steps"] == 1,
          "the omitted smooth step count was not kept")
    # A list valued setting reads back as a Python list, the form elements_per_layer takes
    layers = settings(items, PREFIX + "Sweep")["elements_per_layer"]
    check(type(layers) is list and all(type(count) is int for count in layers),
          "the elements per layer must read back as a list of ints: " + repr(layers))


def test_layer_validation(items):
    check_raises(lambda: items.replace_sweep_mesh(PREFIX + "Sweep", elements_per_layer=[2, 0]),
                 "a zero number of elements per sweep layer was accepted")


def test_quad_wild(items, selection):
    items.add_shell_quad_wild(PREFIX + "QuadWild", selection, triangulation_mesher="Netgen",
                              alpha=0.02, flow_profile="NoAlignment", keep_files_on_failure=True)
    items.replace_shell_quad_wild(PREFIX + "QuadWild", smooth=False, flow_profile="Aligned")
    values = settings(items, PREFIX + "QuadWild")
    check(values["smooth"] is False, "the smoothing flag was not replaced")
    check(values["flow_profile"] == "Aligned", "the flow profile was not replaced")
    check(values["alpha"] == 0.02, "the omitted alpha was not kept")
    check(values["triangulation_mesher"] == "Netgen", "the omitted triangulation mesher was not kept")
    check(values["keep_files_on_failure"] is True, "the file retention flag was not kept")
    # The model refuses an alpha outside 0 to 1, on a replace without a selection as on an add
    message = raised_message(lambda: items.add_shell_quad_wild(PREFIX + "QuadWild_Bad", selection, alpha=1.5))
    check(message is not None and "alpha" in message, "an alpha above 1 was accepted by the add: " + str(message))
    check(not items.contains(PREFIX + "QuadWild_Bad"), "a refused add left an item behind")
    message = raised_message(lambda: items.replace_shell_quad_wild(PREFIX + "QuadWild", alpha=1.5))
    check(message is not None and "alpha" in message, "an alpha above 1 was accepted by the replace: " + str(message))
    check(settings(items, PREFIX + "QuadWild")["alpha"] == 0.02, "a refused replace changed the alpha")


# What get_settings returns goes back into the replace of the item type, every key an argument it takes, and the
# replace changes nothing
def test_settings_round_trip(items):
    names = [name for name in items.get_names() if name.startswith(PREFIX)]
    item_types = set(items[name].item_type for name in names)
    check(len(item_types) == 10, "every mesh setup item type must be covered: " + str(sorted(item_types)))
    for name in names:
        item = items[name]
        before = settings(items, name)
        geometry_ids = list(item.get_geometry_ids())
        getattr(items, "replace_" + item.item_type)(name, **before)
        after = settings(items, name)
        check(after == before, "the settings of " + name + " did not round trip: " + str(before) + " became " +
              str(after))
        check(list(items[name].get_geometry_ids()) == geometry_ids, "the round trip of " + name +
              " changed its region")


def refused(function, words, message):
    text = raised_message(function)
    check(text is not None and all(word in text for word in words), message + ": " + str(text))


# A value outside the range the model holds it to is refused naming the argument, rather than clamped,
# moved along with another value or passed on as an exception of the model, and nothing is recorded
def test_value_ranges(items, solid_selection, shell_selection):
    before = list(items.get_names())
    name = PREFIX + "Parameters"
    kept = settings(items, name)
    replace = items.replace_meshing_parameters
    refused(lambda: replace(name, min_size=kept["max_size"] * 2), ["min_size", "max_size"],
            "a minimum size above the kept maximum was accepted")
    refused(lambda: replace(name, max_size=1.0, min_size=2.0), ["min_size", "max_size"],
            "a minimum size above the maximum was accepted")
    refused(lambda: replace(name, factor_max=0.01, factor_min=0.02), ["factor_min", "factor_max"],
            "a minimum factor above the maximum was accepted")
    for arguments in [{"max_size": -1.0}, {"min_size": float("nan")}, {"factor_max": 1.5}, {"factor_min": 0},
                      {"fineness": 0}, {"grading": 1.5}, {"elements_per_edge": 0}, {"elements_per_curve": -1}]:
        argument = list(arguments)[0]
        refused(lambda: replace(name, **arguments), [argument], "the value " + str(arguments) + " was accepted")
    refused(lambda: replace(name, split_compound_mesh=True, merge_compound_parts=True),
            ["split_compound_mesh", "merge_compound_parts"], "splitting and merging a compound were both accepted")
    check(settings(items, name) == kept, "a refused replace changed the meshing parameters")
    # A pair whose maximum lies below the kept minimum is set as given, not dragged by the order of the setters
    replace(name, min_size=kept["max_size"] / 2)
    replace(name, max_size=kept["max_size"] / 8, min_size=kept["max_size"] / 16)
    values = settings(items, name)
    check(values["max_size"] == kept["max_size"] / 8 and values["min_size"] == kept["max_size"] / 16,
          "the sizes were not set as given: " + str(values))
    replace(name, max_size=kept["max_size"], min_size=kept["min_size"])
    refused(lambda: items.add_meshing_parameters(PREFIX + "Bad", solid_selection, min_size=1e9),
            ["min_size", "max_size"], "a minimum size above the part default maximum was accepted")
    #
    refused(lambda: items.add_local_mesh_size(PREFIX + "Bad", solid_selection, element_size=0), ["element_size"],
            "a zero element size was accepted")
    refused(lambda: items.replace_local_mesh_size(PREFIX + "Local", number_of_elements=0), ["number_of_elements"],
            "a zero number of elements was accepted")
    refused(lambda: items.replace_netgen_mesh(PREFIX + "Netgen", optimize_steps_2d=-1), ["optimize_steps_2d"],
            "a negative number of optimization steps was accepted")
    refused(lambda: items.replace_netgen_mesh(PREFIX + "Netgen", transfinite_angle_deg=180),
            ["transfinite_angle_deg"], "a Netgen transfinite angle of 180 degrees was accepted")
    refused(lambda: items.replace_shell_mmg(PREFIX + "MMG", hausdorff=0), ["hausdorff"],
            "a zero Hausdorff value was accepted")
    refused(lambda: items.replace_thicken_shell_mesh(PREFIX + "Thicken", thickness=0), ["thickness"],
            "a zero thickness was accepted")
    refused(lambda: items.replace_thicken_shell_mesh(PREFIX + "Thicken", number_of_layers=0), ["number_of_layers"],
            "a zero number of layers was accepted")
    refused(lambda: items.add_thicken_shell_mesh(PREFIX + "Bad", shell_selection, float("nan")), ["thickness"],
            "a thickness that is not finite was accepted")
    refused(lambda: items.replace_sweep_mesh(PREFIX + "Sweep", quad_wild_alpha=1.5), ["quad_wild_alpha"],
            "a sweep QuadWild alpha above 1 was accepted")
    refused(lambda: items.replace_sweep_mesh(PREFIX + "Sweep", number_of_global_smooth_steps=-2),
            ["number_of_global_smooth_steps"], "a negative number of sweep smooth steps was accepted")
    refused(lambda: items.add_sweep_mesh(PREFIX + "Bad", solid_selection, number_of_global_smooth_steps=-1),
            ["number_of_global_smooth_steps"], "a negative number of sweep smooth steps was accepted by the add")
    check(settings(items, PREFIX + "Sweep")["number_of_global_smooth_steps"] == 1,
          "a refused replace changed the sweep smooth steps")
    # QuadWild triangulates with Gmsh, Netgen or MMG only
    for mesher in ["QuadWild", "None"]:
        refused(lambda: items.replace_shell_quad_wild(PREFIX + "QuadWild", triangulation_mesher=mesher),
                ["triangulation_mesher", "Gmsh"], "the triangulation mesher " + mesher + " was accepted")
    for arguments in [{"singularity_position": -1}, {"count_search": -1}, {"transfinite_angle_deg": 80}]:
        argument = list(arguments)[0]
        refused(lambda: items.replace_shell_quad_wild(PREFIX + "QuadWild", **arguments), [argument],
                "the QuadWild value " + str(arguments) + " was accepted")
    items.replace_shell_quad_wild(PREFIX + "QuadWild", triangulation_mesher="gmsh")
    check(settings(items, PREFIX + "QuadWild")["triangulation_mesher"] == "Gmsh",
          "the triangulation mesher was not set in its own spelling")
    check(list(items.get_names()) == before, "a refused add or replace changed the collection")


def test_rejection_leaves_no_trace(items):
    before = list(items.get_names())
    check_raises(lambda: items.replace_local_mesh_size(PREFIX + "Local", number_of_elements=2.5),
                 "a fractional element count was accepted")
    # A replace meant for another item type is refused naming the item and its type, not by a failed cast
    message = raised_message(lambda: items.replace_shell_mmg(PREFIX + "Local", relative_hausdorff=0.003))
    check(message is not None and PREFIX + "Local" in message and "add_local_mesh_size" in message,
          "a replace of another item type must name the item and its type: " + str(message))
    # A name the model could not take is refused before the item is built or its defaults are fetched
    for add in [lambda: items.add_local_mesh_size(PREFIX + "Bad Name", None, element_size=0.4),
                lambda: items.add_meshing_parameters(PREFIX + "Bad#", None),
                lambda: items.add_shell_mmg(" " + PREFIX + "Spaced", None)]:
        message = raised_message(add)
        check(message is not None and "'name'" in message, "a bad name must be refused naming it: " + str(message))
    message = raised_message(lambda: items.replace_local_mesh_size(PREFIX + "Local", name=PREFIX + "Bad Name"))
    check(message is not None and "'name'" in message, "a replace to a bad name must be refused: " + str(message))
    check(list(items.get_names()) == before, "a refused replacement changed the collection")


def diagonal(part):
    box = part.get_bounding_box()
    return ((box[1] - box[0]) ** 2 + (box[3] - box[2]) ** 2 + (box[5] - box[4]) ** 2) ** 0.5


# The dialog rebuilds the size based values from the parts that are now selected whenever the user
# has typed none of them, so a replace that only moves the item to other geometry does the same and
# one that also sets a value leaves the rest alone. Without this a scripted item kept the sizes of a
# part it no longer covers, which on a much larger part is a mesh nobody asked for
def test_size_defaults(items, small, small_selection, large, large_selection):
    name = PREFIX + "Sizes"
    items.add_meshing_parameters(name, small_selection)
    small_max = settings(items, name)["max_size"]
    check(small_max > 0, "the added item has no maximum size: " + str(small_max))
    # Moving it to the larger part and saying nothing else lets the sizes follow the geometry
    items.replace_meshing_parameters(name, selection=large_selection)
    large_max = settings(items, name)["max_size"]
    ratio = (large_max / small_max) / (diagonal(large) / diagonal(small))
    check(abs(ratio - 1.0) < 0.1, "the maximum size did not follow the selected part: " +
          str(small_max) + " to " + str(large_max) + " for a diagonal ratio of " +
          str(diagonal(large) / diagonal(small)))
    # A replace that also sets a value is the caller's, so the rest is kept rather than rebuilt
    kept_min = settings(items, name)["min_size"]
    items.replace_meshing_parameters(name, selection=small_selection, max_size=7.0)
    values = settings(items, name)
    check(values["max_size"] == 7.0, "the given maximum size was not applied: " + str(values["max_size"]))
    check(values["min_size"] == kept_min, "a replace that sets a value must keep the rest: " +
          str(values["min_size"]) + " against " + str(kept_min))
    # Sizes an earlier call set are no longer the part defaults, so moving the item does not rebuild them
    items.replace_meshing_parameters(name, selection=large_selection)
    values = settings(items, name)
    check(values["max_size"] == 7.0, "moving the item discarded the maximum size set earlier: " +
          str(values["max_size"]))
    check(values["min_size"] == kept_min, "moving the item discarded the minimum size set earlier: " +
          str(values["min_size"]) + " against " + str(kept_min))
    items.remove([name])


def part_selection(part):
    return pmx.create.selection_by_ids("geometry", "geometry", "parts", [part.id])


def face_selection(part, face_id):
    return pmx.create.selection_by_ids("geometry", "geometry_surfaces", "surfaces", {part.id: [face_id]})


def import_parts(file_name):
    before = list(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, file_name))
    return [part for part in pmx.model.geometry.parts if part.name not in before]


def main():
    check(pmx.model.is_open(), "a model must be open")
    cad = list(pmx.model.geometry.parts)
    check(len(cad) == 2, "the CAD model must carry the box and the quarter cylinder")
    box = [part for part in cad if part.get_face_data().count == 6][0]
    cylinder = [part for part in cad if part.get_face_data().count != 6][0]
    shell = import_parts("open_shell.stl")[0]
    solid = import_parts("cube.stl")[0]
    check(shell.part_type == "Shell", "the shell part is a " + shell.part_type)
    # The bottom face of the box is what a sweep is built from
    box_face = face_selection(box, 5)
    solid_selection = part_selection(solid)
    shell_selection = part_selection(shell)
    items = pmx.model.mesh.setup_items
    for name in list(items.get_names()):
        if name.startswith(PREFIX):
            items.remove([name])
    #
    test_meshing_parameters(items, solid_selection)
    test_netgen_mesh(items, solid_selection, shell_selection)
    test_local_mesh_size(items, solid_selection)
    test_shell_mmg(items, shell_selection)
    test_thicken_shell(items, shell_selection)
    test_gmsh_items(items, solid_selection, shell_selection, part_selection(box))
    test_derived_arguments(items, box_face)
    test_layer_validation(items)
    test_quad_wild(items, shell_selection)
    test_settings_round_trip(items)
    test_value_ranges(items, solid_selection, shell_selection)
    test_size_defaults(items, solid, solid_selection, box, part_selection(box))
    test_rejection_leaves_no_trace(items)
    #
    items.remove([n for n in items.get_names() if n.startswith(PREFIX)])
    print("TEST PASSED")


main()
