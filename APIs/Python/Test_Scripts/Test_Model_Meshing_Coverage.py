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
    # Only a relative Netgen mesh is sized by the factors, so they read back and are taken only with
    # relative_size=True, and are refused naming relative_size otherwise
    check(values["relative_size"] is False and "factor_max" not in values and "factor_min" not in values,
          "absolute meshing parameters read back their factors: " + str(values))
    for arguments in [{"factor_max": 0.1}, {"factor_min": 0.01}]:
        argument = list(arguments)[0]
        message = raised_message(lambda: items.replace_meshing_parameters(PREFIX + "Parameters", **arguments))
        check(message is not None and "'" + argument + "'" in message and "relative_size" in message,
              "a factor of absolute meshing parameters must be refused naming relative_size: " + str(message))
    # Gmsh, Mmg and the local mesh size limits use the sizes in either mode, so a relative item takes them and
    # reads them back as well
    items.replace_meshing_parameters(PREFIX + "Parameters", relative_size=True, factor_max=0.1, factor_min=0.01,
                                     max_size=0.3)
    values = settings(items, PREFIX + "Parameters")
    check(values["relative_size"] is True and values["factor_max"] == 0.1 and values["factor_min"] == 0.01,
          "relative meshing parameters did not read back their factors: " + str(values))
    check(values["max_size"] == 0.3 and "min_size" in values,
          "relative meshing parameters did not take and read back their sizes: " + str(values))
    items.replace_meshing_parameters(PREFIX + "Parameters", **values)
    check(settings(items, PREFIX + "Parameters") == values, "the relative settings did not round trip")
    # Going back to the absolute sizes keeps the sizes that were set
    items.replace_meshing_parameters(PREFIX + "Parameters", relative_size=False)
    values = settings(items, PREFIX + "Parameters")
    check(values["relative_size"] is False and values["max_size"] == 0.3,
          "the kept sizes did not come back with relative_size=False: " + str(values))
    # The midside node switch is used only for second order elements, as the dialog shows it, so it is read back
    # and taken only then; the fineness is written for no mesher and the dialog does not show it
    items.replace_meshing_parameters(PREFIX + "Parameters", second_order=True, midside_nodes_on_geometry=True)
    values = settings(items, PREFIX + "Parameters")
    check(values["midside_nodes_on_geometry"] is True, "the midside node switch was not set: " + str(values))
    check("fineness" not in values, "the fineness must not be read back: " + str(values))
    items.replace_meshing_parameters(PREFIX + "Parameters", second_order=False)
    values = settings(items, PREFIX + "Parameters")
    check("midside_nodes_on_geometry" not in values,
          "first order meshing parameters read back the midside node switch: " + str(values))
    items.replace_meshing_parameters(PREFIX + "Parameters", **values)
    check(settings(items, PREFIX + "Parameters") == values, "the first order settings did not round trip")
    for call, words in [(lambda: items.replace_meshing_parameters(PREFIX + "Parameters",
                                                                  midside_nodes_on_geometry=False),
                         ["'midside_nodes_on_geometry'", "second_order=True"]),
                        (lambda: items.replace_meshing_parameters(PREFIX + "Parameters", second_order=False,
                                                                  midside_nodes_on_geometry=True),
                         ["'midside_nodes_on_geometry'", "second_order=False"]),
                        (lambda: items.replace_meshing_parameters(PREFIX + "Parameters", fineness=0.5),
                         ["fineness"])]:
        message = raised_message(call)
        check(message is not None and all(word in message for word in words),
              "the refusal must name " + str(words) + ": " + str(message))
    check(settings(items, PREFIX + "Parameters") == values, "a refused replace changed the meshing parameters")
    items.replace_meshing_parameters(PREFIX + "Parameters", second_order=True)


def test_netgen_mesh(items, selection, shell_selection):
    items.add_netgen_mesh(PREFIX + "Netgen", selection, number_of_optimize_steps_2d=4, transfinite_three_sided="No",
                          transfinite_four_sided=True)
    items.replace_netgen_mesh(PREFIX + "Netgen", number_of_optimize_steps_3d=5)
    values = settings(items, PREFIX + "Netgen")
    check(values["number_of_optimize_steps_3d"] == 5, "the Netgen 3-D optimization steps were not replaced")
    check(values["number_of_optimize_steps_2d"] == 4,
          "the omitted Netgen optimization steps were not kept: " + str(values))
    check(values["transfinite_four_sided"] is True and values["transfinite_three_sided"] == "No",
          "the omitted Netgen transfinite settings were not kept: " + str(values))
    check(items[PREFIX + "Netgen"].mesher_type == "Netgen", "the Netgen mesh item does not select Netgen")
    check(len(items[PREFIX + "Netgen"].get_geometry_ids()) > 0, "the Netgen region was lost")
    check_raises(lambda: items.replace_netgen_mesh(PREFIX + "Netgen", number_of_optimize_steps_2d=2.5),
                 "a fractional optimization step count was accepted")
    # A replace without a name keeps the item under its own spelling, whatever spelling old_name has
    count = len(items)
    for spelling in [(PREFIX + "Netgen").lower(), (PREFIX + "Netgen").upper()]:
        item = items.replace_netgen_mesh(spelling, number_of_optimize_steps_3d=5)
        check(item.name == PREFIX + "Netgen" and PREFIX + "Netgen" in list(items.get_names()),
              "a replace of '" + spelling + "' changed the stored spelling: " + str(list(items.get_names())))
    check(len(items) == count, "a mixed case replace changed the number of mesh setup items")
    # The item meshes shell parts as well as solid ones
    items.add_netgen_mesh(PREFIX + "NetgenShell", shell_selection, quad_dominated=True)
    values = settings(items, PREFIX + "NetgenShell")
    check(values["quad_dominated"] is True, "the Netgen quad-dominated setting was not stored")
    # A new item meshes the 3-sided and the 4-sided faces free
    check(values["transfinite_three_sided"] == "No" and values["transfinite_four_sided"] is False,
          "a new Netgen item must mesh the 3-sided and 4-sided faces free: " + str(values))
    # The angle is used, shown and read back only while a face is structured, judged on the values the item ends
    # up with
    check("transfinite_angle_deg" not in settings(items, PREFIX + "NetgenShell"),
          "a Netgen item without structured faces read back its transfinite angle")
    for name, arguments in [(PREFIX + "NetgenShell", {"transfinite_angle_deg": 120.0}),
                            (PREFIX + "Netgen", {"transfinite_four_sided": False, "transfinite_angle_deg": 120.0})]:
        message = raised_message(lambda: items.replace_netgen_mesh(name, **arguments))
        check(message is not None and "'transfinite_angle_deg'" in message and
              "transfinite_four_sided" in message,
              "an unused Netgen transfinite angle must be refused naming the transfinite settings: " + str(message))
    items.replace_netgen_mesh(PREFIX + "Netgen", transfinite_angle_deg=120.0)
    check(settings(items, PREFIX + "Netgen")["transfinite_angle_deg"] == 120.0,
          "the transfinite angle of a Netgen item with structured faces was not set")


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
    # The dialog shows either the relative or the absolute distance, so the other one is refused and not read
    check("hausdorff" not in values, "a relative MMG item read back its absolute distance: " + str(values))
    message = raised_message(lambda: items.replace_shell_mmg(PREFIX + "MMG", hausdorff=0.5))
    check(message is not None and "'hausdorff'" in message and "relative_size" in message,
          "an absolute distance of a relative MMG item must be refused naming relative_size: " + str(message))
    items.replace_shell_mmg(PREFIX + "MMG", relative_size=False, hausdorff=0.5)
    values = settings(items, PREFIX + "MMG")
    check(values["hausdorff"] == 0.5 and "relative_hausdorff" not in values,
          "an absolute MMG item must read back only its absolute distance: " + str(values))
    message = raised_message(lambda: items.replace_shell_mmg(PREFIX + "MMG", relative_hausdorff=0.004))
    check(message is not None and "'relative_hausdorff'" in message and "relative_size" in message,
          "a relative distance of an absolute MMG item must be refused naming relative_size: " + str(message))
    items.replace_shell_mmg(PREFIX + "MMG", relative_size=True)
    check(settings(items, PREFIX + "MMG")["relative_hausdorff"] == 0.003,
          "the kept relative distance did not come back with relative_size=True")
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
    # The minimum quality is used, shown and read back only while the item recombines, judged on the
    # recombination it ends up with
    items.replace_tetrahedral_gmsh(PREFIX + "Tet", recombine_min_quality=0.3)
    check(settings(items, PREFIX + "Tet")["recombine_min_quality"] == 0.3,
          "the minimum quality of a recombining item was not set")
    message = raised_message(lambda: items.replace_tetrahedral_gmsh(PREFIX + "Tet", algorithm_recombine="None",
                                                                    recombine_min_quality=0.2))
    check(message is not None and "'recombine_min_quality'" in message and "algorithm_recombine" in message,
          "a minimum quality without recombination must be refused naming algorithm_recombine: " + str(message))
    items.replace_tetrahedral_gmsh(PREFIX + "Tet", algorithm_recombine="None")
    values = settings(items, PREFIX + "Tet")
    check("recombine_min_quality" not in values and values["algorithm_recombine"] == "None",
          "an item without recombination read back its minimum quality: " + str(values))
    message = raised_message(lambda: items.replace_tetrahedral_gmsh(PREFIX + "Tet", recombine_min_quality=0.2))
    check(message is not None and "'recombine_min_quality'" in message and "algorithm_recombine" in message,
          "a minimum quality the item keeps unused must be refused naming algorithm_recombine: " + str(message))
    #
    items.add_shell_gmsh(PREFIX + "Shell", shell_selection, transfinite_angle_deg=120.0)
    items.replace_shell_gmsh(PREFIX + "Shell", algorithm_recombine="Blossom")
    values = settings(items, PREFIX + "Shell")
    check(values["transfinite_angle_deg"] == 120.0, "the omitted transfinite angle was not kept")
    # A shell has no volume, so neither the dialog nor the API offers a 3D algorithm
    check("algorithm_3d" not in values, "a shell item read back a 3D algorithm: " + str(values))
    message = raised_message(lambda: items.replace_shell_gmsh(PREFIX + "Shell", algorithm_3d="Delaunay"))
    check(message is not None and "algorithm_3d" in message, "a shell item took a 3D algorithm: " + str(message))
    # The angle is used, shown and read back only while a face is structured
    message = raised_message(lambda: items.replace_shell_gmsh(PREFIX + "Shell", transfinite_three_sided="No",
                                                              transfinite_four_sided=False,
                                                              transfinite_angle_deg=100.0))
    check(message is not None and "'transfinite_angle_deg'" in message and "transfinite_three_sided" in message,
          "an unused transfinite angle must be refused naming the transfinite settings: " + str(message))
    check(settings(items, PREFIX + "Shell") == values, "a refused replace changed the shell item")
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
    # The QuadWild settings are used only while QuadWild meshes the sources: with a gmsh algorithm_2d they are
    # refused and not read back
    values = settings(items, PREFIX + "Sweep")
    gmsh_algorithm = values["algorithm_2d"]
    check(not any(name.startswith("quad_wild_") for name in values) and "algorithm_recombine" in values,
          "a gmsh sweep read back the QuadWild settings: " + str(values))
    for arguments in [{"quad_wild_alpha": 0.5}, {"quad_wild_smooth": True}, {"quad_wild_flow_profile": "Aligned"}]:
        argument = list(arguments)[0]
        message = raised_message(lambda: items.replace_sweep_mesh(PREFIX + "Sweep", **arguments))
        check(message is not None and "'" + argument + "'" in message and "algorithm_2d" in message,
              "a QuadWild value of a gmsh sweep must be refused naming algorithm_2d: " + str(message))
    # The sweep builds its volume mesh itself, and the dialog no longer offers the imprint edges, so neither is
    # an argument
    check("algorithm_3d" not in values and "keep_imprint_edges" not in values,
          "a sweep read back a setting it does not take: " + str(values))
    for arguments in [{"algorithm_3d": "Delaunay"}, {"keep_imprint_edges": True}]:
        argument = list(arguments)[0]
        message = raised_message(lambda: items.replace_sweep_mesh(PREFIX + "Sweep", **arguments))
        check(message is not None and argument in message, "a sweep took " + argument + ": " + str(message))
    # gmsh meshes the walls of a sweep whose sources QuadWild meshes, so the recombination and optimisation
    # stay settings of a QuadWild sweep
    items.replace_sweep_mesh(PREFIX + "Sweep", algorithm_2d="QuadWild", quad_wild_alpha=0.5,
                             algorithm_recombine="Blossom", recombine_min_quality=0.2,
                             optimize_first_order_shell="Laplace2D")
    values = settings(items, PREFIX + "Sweep")
    check(values["quad_wild_alpha"] == 0.5 and values["algorithm_recombine"] == "Blossom" and
          values["recombine_min_quality"] == 0.2 and values["optimize_first_order_shell"] == "Laplace2D",
          "a QuadWild sweep must take and read back the gmsh recombination and optimisation: " + str(values))
    items.replace_sweep_mesh(PREFIX + "Sweep", **values)
    check(settings(items, PREFIX + "Sweep") == values, "the QuadWild sweep settings did not round trip")
    items.replace_sweep_mesh(PREFIX + "Sweep", algorithm_2d=gmsh_algorithm, algorithm_recombine="Simple",
                             optimize_first_order_shell="None")
    check(settings(items, PREFIX + "Sweep")["algorithm_recombine"] == "Simple",
          "a gmsh value given with a gmsh algorithm_2d was not set")


def test_layer_validation(items):
    check_raises(lambda: items.replace_sweep_mesh(PREFIX + "Sweep", elements_per_layer=[2, 0]),
                 "a zero number of elements per sweep layer was accepted")


def test_quad_wild(items, selection):
    items.add_shell_quad_wild(PREFIX + "QuadWild", selection, triangulation_mesher="Netgen",
                              alpha=0.02, flow_profile="NoAlignment", keep_files_on_failure=True)
    items.replace_shell_quad_wild(PREFIX + "QuadWild", smooth=False, flow_profile="Aligned",
                                  number_of_search_rounds=2)
    values = settings(items, PREFIX + "QuadWild")
    check(values["number_of_search_rounds"] == 2, "the number of search rounds was not replaced: " + str(values))
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
    # A preview shows the triangulation without running QuadWild, so the dialog hides the settings of the
    # quadrangulation, and they are refused naming preview and not read back while it is on
    quadrangulation = ["smooth", "alpha", "flow_profile", "boundary_layer", "transfinite_three_sided",
                       "transfinite_four_sided", "transfinite_angle_deg"]
    message = raised_message(lambda: items.replace_shell_quad_wild(PREFIX + "QuadWild", preview=True, alpha=0.3))
    check(message is not None and "'alpha'" in message and "preview" in message,
          "an alpha of a preview must be refused naming preview: " + str(message))
    items.replace_shell_quad_wild(PREFIX + "QuadWild", preview=True)
    values = settings(items, PREFIX + "QuadWild")
    check(values["preview"] is True and not any(name in values for name in quadrangulation),
          "a preview read back the settings of the quadrangulation: " + str(values))
    for arguments in [{"smooth": True}, {"transfinite_angle_deg": 120.0}, {"boundary_layer": "Holes"}]:
        argument = list(arguments)[0]
        message = raised_message(lambda: items.replace_shell_quad_wild(PREFIX + "QuadWild", **arguments))
        check(message is not None and "'" + argument + "'" in message and "preview" in message,
              "a quadrangulation value the preview keeps unused must be refused naming preview: " + str(message))
    items.replace_shell_quad_wild(PREFIX + "QuadWild", **values)
    check(settings(items, PREFIX + "QuadWild") == values, "the preview settings did not round trip")
    items.replace_shell_quad_wild(PREFIX + "QuadWild", preview=False)
    values = settings(items, PREFIX + "QuadWild")
    check(values["alpha"] == 0.02 and values["flow_profile"] == "Aligned",
          "the settings of the quadrangulation were not kept through a preview: " + str(values))
    # The angle is used, shown and read back only while a face is structured
    message = raised_message(lambda: items.replace_shell_quad_wild(PREFIX + "QuadWild", transfinite_three_sided="No",
                                                                   transfinite_four_sided=False,
                                                                   transfinite_angle_deg=120.0))
    check(message is not None and "'transfinite_angle_deg'" in message and "transfinite_four_sided" in message,
          "an unused QuadWild transfinite angle must be refused naming the transfinite settings: " + str(message))


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
    refused(lambda: replace(name, relative_size=True, factor_max=0.01, factor_min=0.02),
            ["factor_min", "must not be larger than 'factor_max'"], "a minimum factor above the maximum was accepted")
    for arguments in [{"max_size": -1.0}, {"min_size": float("nan")}, {"factor_max": 1.5, "relative_size": True},
                      {"factor_min": 0, "relative_size": True}, {"grading": 1.5},
                      {"elements_per_edge": 0}, {"elements_per_curve": -1}]:
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
    refused(lambda: items.replace_netgen_mesh(PREFIX + "Netgen", number_of_optimize_steps_2d=-1), ["number_of_optimize_steps_2d"],
            "a negative number of optimization steps was accepted")
    refused(lambda: items.replace_netgen_mesh(PREFIX + "Netgen", transfinite_angle_deg=180),
            ["transfinite_angle_deg"], "a Netgen transfinite angle of 180 degrees was accepted")
    refused(lambda: items.replace_shell_mmg(PREFIX + "MMG", relative_size=False, hausdorff=0), ["hausdorff"],
            "a zero Hausdorff value was accepted")
    refused(lambda: items.replace_thicken_shell_mesh(PREFIX + "Thicken", thickness=0), ["thickness"],
            "a zero thickness was accepted")
    refused(lambda: items.replace_thicken_shell_mesh(PREFIX + "Thicken", number_of_layers=0), ["number_of_layers"],
            "a zero number of layers was accepted")
    refused(lambda: items.add_thicken_shell_mesh(PREFIX + "Bad", shell_selection, float("nan")), ["thickness"],
            "a thickness that is not finite was accepted")
    refused(lambda: items.replace_sweep_mesh(PREFIX + "Sweep", algorithm_2d="QuadWild", quad_wild_alpha=1.5),
            ["quad_wild_alpha", "from 0 to 1"], "a sweep QuadWild alpha above 1 was accepted")
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
    for arguments in [{"singularity_position": -1}, {"number_of_search_rounds": -1}, {"transfinite_angle_deg": 80}]:
        argument = list(arguments)[0]
        refused(lambda: items.replace_shell_quad_wild(PREFIX + "QuadWild", **arguments), [argument],
                "the QuadWild value " + str(arguments) + " was accepted")
    items.replace_shell_quad_wild(PREFIX + "QuadWild", triangulation_mesher="gmsh")
    check(settings(items, PREFIX + "QuadWild")["triangulation_mesher"] == "Gmsh",
          "the triangulation mesher was not set in its own spelling")
    check(list(items.get_names()) == before, "a refused add or replace changed the collection")


def test_rejection_leaves_no_trace(items, selection):
    before = list(items.get_names())
    check_raises(lambda: items.replace_local_mesh_size(PREFIX + "Local", number_of_elements=2.5),
                 "a fractional element count was accepted")
    # A replace meant for another item type is refused naming the item and its type, not by a failed cast
    message = raised_message(lambda: items.replace_shell_mmg(PREFIX + "Local", relative_hausdorff=0.003))
    check(message is not None and PREFIX + "Local" in message and "add_local_mesh_size" in message,
          "a replace of another item type must name the item and its type: " + str(message))
    # A name the model could not take is refused before the item is built or its defaults are fetched
    for add in [lambda: items.add_local_mesh_size(PREFIX + "Bad Name", selection, element_size=0.4),
                lambda: items.add_meshing_parameters(PREFIX + "Bad#", selection),
                lambda: items.add_shell_mmg(" " + PREFIX + "Spaced", selection)]:
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
    test_rejection_leaves_no_trace(items, solid_selection)
    #
    items.remove([n for n in items.get_names() if n.startswith(PREFIX)])
    print("TEST PASSED")


main()
