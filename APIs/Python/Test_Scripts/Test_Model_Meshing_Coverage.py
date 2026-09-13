#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the mesh setup item replacements and the mesher settings: a replacement keeps the region and
#every value it is not told about.
#
#Every item type is added on geometry it can actually be defined on, because an item the model cannot
#honour is refused the way the dialog refuses it. box_and_cylinder.brep carries the CAD box the
#transfinite, extrude and sweep items need and the quarter cylinder the revolve item needs; the two
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


def settings(items, name):
    return items[name].get_settings()


def test_meshing_parameters(items, selection):
    items.add_meshing_parameters(PREFIX + "Parameters", selection, max_size=0.5, optimize_steps_2d=4)
    # A replacement keeps the region it was created from and every value it is not told about
    items.replace_meshing_parameters(PREFIX + "Parameters", max_size=0.25)
    values = settings(items, PREFIX + "Parameters")
    check(values["max_h"] == 0.25, "the maximum size was not replaced: " + str(values["max_h"]))
    check(values["optimize_steps2_d"] == 4, "the omitted optimization steps were not kept: " + str(values))
    check(len(items[PREFIX + "Parameters"].get_geometry_ids()) > 0, "the region was lost")
    check_raises(lambda: items.replace_meshing_parameters(PREFIX + "Parameters", optimize_steps_2d=2.5),
                 "a fractional optimization step count was accepted")


def test_local_mesh_size(items, selection):
    items.add_local_mesh_size(PREFIX + "Local", selection, element_size=0.4)
    items.replace_local_mesh_size(PREFIX + "Local", element_size=0.2)
    check(settings(items, PREFIX + "Local")["mesh_size"] == 0.2, "the element size was not replaced")
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
                               transfinite_three_sided=True)
    items.replace_tetrahedral_gmsh(PREFIX + "Tet", optimize_high_order="HighOrder")
    values = settings(items, PREFIX + "Tet")
    check(values["algorithm_recombine"] == "Simple", "the omitted recombine algorithm was not kept")
    check(values["transfinite_three_sided"], "the omitted transfinite flag was not kept")
    #
    items.add_shell_gmsh(PREFIX + "Shell", shell_selection, transfinite_angle=120.0)
    items.replace_shell_gmsh(PREFIX + "Shell", algorithm_recombine="Blossom")
    check(settings(items, PREFIX + "Shell")["transfinite_angle_deg"] == 120.0,
          "the omitted transfinite angle was not kept")
    #
    items.add_transfinite_mesh(PREFIX + "Transfinite", box_selection, recombine=True)
    items.replace_transfinite_mesh(PREFIX + "Transfinite", transfinite_angle=110.0)
    values = settings(items, PREFIX + "Transfinite")
    check(values["algorithm_recombine"] != "None", "the omitted recombine switch was not kept")
    check(values["transfinite_angle_deg"] == 110.0, "the transfinite angle was not replaced")


def test_derived_arguments(items, selection, revolve_selection):
    items.add_extrude_mesh(PREFIX + "Extrude", selection, number_of_elements=3)
    items.replace_extrude_mesh(PREFIX + "Extrude", element_scale_factor=1.5)
    check(settings(items, PREFIX + "Extrude")["number_of_elements"] == 3,
          "the omitted element count was not kept")
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Extrude", direction=[1, 0, 0]),
                 "the derived extrusion direction was accepted")
    #
    items.add_revolve_mesh(PREFIX + "Revolve", revolve_selection)
    items.replace_revolve_mesh(PREFIX + "Revolve", number_of_elements=4)
    check_raises(lambda: items.replace_revolve_mesh(PREFIX + "Revolve", angle=90.0),
                 "the derived revolution angle was accepted")
    #
    items.add_sweep_mesh(PREFIX + "Sweep", selection, number_of_global_smooth_steps=1)
    items.replace_sweep_mesh(PREFIX + "Sweep", transfinite_angle=100.0)
    check(settings(items, PREFIX + "Sweep")["number_of_global_smooth_steps"] == 1,
          "the omitted smooth step count was not kept")


def test_layer_validation(items, selection):
    # The model setters clamp a value out of their domain, so a rejected value must not reach them
    items.add_extrude_mesh(PREFIX + "Layers", selection, number_of_elements=3)
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Layers", element_scale_factor=-4),
                 "a negative element scale factor was accepted")
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Layers", element_scale_factor=0),
                 "a zero element scale factor was accepted")
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Layers", number_of_elements=0),
                 "a zero element count was accepted")
    check(settings(items, PREFIX + "Layers")["number_of_elements"] == 3,
          "a refused layer value changed the stored definition")
    #
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Layers", element_size_type="MultiLayerd",
                                                    layer_sizes=[float('nan'), -1],
                                                    elements_per_layer=[0]),
                 "non-finite and negative layer sizes were accepted")
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Layers", element_size_type="MultiLayerd",
                                                    layer_sizes=[0.5, 0.5], elements_per_layer=[2, 0]),
                 "a zero number of elements per layer was accepted")
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Layers", element_size_type="MultiLayerd",
                                                    layer_sizes=[0.5, 0.5], elements_per_layer=[2]),
                 "layer arrays of different lengths were accepted")
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Layers", element_size_type="MultiLayerd",
                                                    layer_sizes=[]),
                 "an empty layer size array was accepted")
    check(settings(items, PREFIX + "Layers")["element_size_type"] != "MultiLayerd",
          "a refused layer definition changed the element size type")
    # A kept array is part of the effective definition, so it is checked against the given one
    items.replace_extrude_mesh(PREFIX + "Layers", element_size_type="MultiLayerd",
                               layer_sizes=[0.2, 0.6, 0.2], elements_per_layer=[1, 3, 1])
    values = settings(items, PREFIX + "Layers")
    check(list(values["layer_sizes"]) == [0.2, 0.6, 0.2],
          "the layer sizes did not read back: " + str(values.get("layer_sizes")))
    check(list(values["num_of_elements_per_layer"]) == [1, 3, 1],
          "the numbers of elements per layer did not read back")
    check_raises(lambda: items.replace_extrude_mesh(PREFIX + "Layers", elements_per_layer=[1, 1]),
                 "a kept layer size array of a different length was accepted")
    check(list(settings(items, PREFIX + "Layers")["num_of_elements_per_layer"]) == [1, 3, 1],
          "a refused layer replacement changed the stored definition")
    #
    check_raises(lambda: items.replace_sweep_mesh(PREFIX + "Sweep", elements_per_layer=[2, 0]),
                 "a zero number of elements per sweep layer was accepted")


def test_quad_wild(items, selection):
    items.add_shell_quad_wild(PREFIX + "QuadWild", selection, triangulation_mesher="Netgen",
                              scale_factor=1.5, flow_profile="NoAlignment")
    items.replace_shell_quad_wild(PREFIX + "QuadWild", smooth=False, flow_profile="Aligned")
    values = settings(items, PREFIX + "QuadWild")
    check(values["smooth"] is False, "the smoothing flag was not replaced")
    check(values["flow_profile"] == "Aligned", "the flow profile was not replaced")
    check(values["scale_factor"] == 1.5, "the omitted scale factor was not kept")
    check(values["triangulation_mesher"] == "Netgen", "the omitted triangulation mesher was not kept")


def test_rejection_leaves_no_trace(items):
    before = list(items.get_names())
    check_raises(lambda: items.replace_local_mesh_size(PREFIX + "Local", number_of_elements=2.5),
                 "a fractional element count was accepted")
    check(list(items.get_names()) == before, "a refused replacement changed the collection")


def diagonal(part):
    box = list(part.get_bounding_box())
    return ((box[1] - box[0]) ** 2 + (box[3] - box[2]) ** 2 + (box[5] - box[4]) ** 2) ** 0.5


# The dialog rebuilds the size based values from the parts that are now selected whenever the user
# has typed none of them, so a replace that only moves the item to other geometry does the same and
# one that also sets a value leaves the rest alone. Without this a scripted item kept the sizes of a
# part it no longer covers, which on a much larger part is a mesh nobody asked for
def test_size_defaults(items, small, small_selection, large, large_selection):
    name = PREFIX + "Sizes"
    items.add_meshing_parameters(name, small_selection)
    small_max = settings(items, name)["max_h"]
    check(small_max > 0, "the added item has no maximum size: " + str(small_max))
    # Moving it to the larger part and saying nothing else lets the sizes follow the geometry
    items.replace_meshing_parameters(name, selection=large_selection)
    large_max = settings(items, name)["max_h"]
    ratio = (large_max / small_max) / (diagonal(large) / diagonal(small))
    check(abs(ratio - 1.0) < 0.1, "the maximum size did not follow the selected part: " +
          str(small_max) + " to " + str(large_max) + " for a diagonal ratio of " +
          str(diagonal(large) / diagonal(small)))
    # A replace that also sets a value is the caller's, so the rest is kept rather than rebuilt
    kept_min = settings(items, name)["min_h"]
    items.replace_meshing_parameters(name, selection=small_selection, max_size=7.0)
    values = settings(items, name)
    check(values["max_h"] == 7.0, "the given maximum size was not applied: " + str(values["max_h"]))
    check(values["min_h"] == kept_min, "a replace that sets a value must keep the rest: " +
          str(values["min_h"]) + " against " + str(kept_min))
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
    # The bottom face of the box is what an extrude and a sweep are built from, and one of the flat
    # radial faces of the quarter cylinder is the section the solid is a revolution of
    box_face = face_selection(box, 5)
    revolve_face = face_selection(cylinder, 4)
    solid_selection = part_selection(solid)
    shell_selection = part_selection(shell)
    items = pmx.model.mesh.setup_items
    for name in list(items.get_names()):
        if name.startswith(PREFIX):
            items.remove([name])
    #
    test_meshing_parameters(items, solid_selection)
    test_local_mesh_size(items, solid_selection)
    test_shell_mmg(items, shell_selection)
    test_thicken_shell(items, shell_selection)
    test_gmsh_items(items, solid_selection, shell_selection, part_selection(box))
    test_derived_arguments(items, box_face, revolve_face)
    test_layer_validation(items, box_face)
    test_quad_wild(items, shell_selection)
    test_size_defaults(items, solid, solid_selection, box, part_selection(box))
    test_rejection_leaves_no_trace(items)
    #
    items.remove([n for n in items.get_names() if n.startswith(PREFIX)])
    print("TEST PASSED")


main()
