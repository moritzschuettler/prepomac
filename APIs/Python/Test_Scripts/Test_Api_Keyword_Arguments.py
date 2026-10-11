#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that an API method refuses a keyword argument it does not take, the way Python refuses one for
#a function of its own. pythonnet binds a keyword to the parameter of that name and ignores one that
#names no parameter, so without the guard a misspelled argument left its value unset without a word.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Api_Keyword_Arguments.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import inspect

PREFIX = "PyTest_Keywords_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def api_error(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def type_error(function):
    """The message of the TypeError the function raises, or None when it raises none."""
    try:
        function()
    except TypeError as ex:
        return str(ex)
    return None


def test_unknown_keyword(materials):
    message = type_error(lambda: materials.add(PREFIX + "Steel", [pmx.create.density(7.85e-9)], colour=1))
    check(message == "add() got an unexpected keyword argument 'colour'",
          "an unknown keyword must be refused: " + str(message))
    check(PREFIX + "Steel" not in list(materials.get_names()), "a refused call must change nothing")


def test_misspelled_keyword(materials):
    message = type_error(lambda: materials.add(PREFIX + "Steel", [pmx.create.density(7.85e-9)],
                                               descripton="A steel"))
    check(message is not None and message.endswith("Did you mean 'description'?"),
          "a misspelled keyword must name the argument it resembles: " + str(message))


def test_keyword_given_twice(materials):
    message = type_error(lambda: materials.add(PREFIX + "Steel", [pmx.create.density(7.85e-9)],
                                               name=PREFIX + "Iron"))
    check(message == "add() got multiple values for argument 'name'",
          "a keyword naming a positional argument must be refused: " + str(message))


def test_inherited_method(materials):
    # remove is declared on the generic collection every item collection derives from
    message = type_error(lambda: materials.remove([PREFIX + "Steel"], force=True))
    check(message == "remove() got an unexpected keyword argument 'force'",
          "a method of a generic base must be guarded too: " + str(message))


def test_valid_call(materials):
    materials.add(PREFIX + "Steel", [pmx.create.density(7.85e-9)], description="A steel")
    check(PREFIX + "Steel" in list(materials.get_names()), "a call with valid keywords must still run")
    materials.remove([PREFIX + "Steel"])
    check(PREFIX + "Steel" not in list(materials.get_names()), "a positional call must still run")
    parameters = list(inspect.signature(materials.add).parameters)
    check(parameters[:2] == ["name", "properties"],
          "the guarded method must keep the signature of the API method: " + str(parameters))


# A typed .NET parameter made pythonnet refuse None, or a float for an int, with 'No method matches given
# arguments', which names no argument. The API takes those values itself and refuses them by name.
def test_typed_arguments():
    nodes = pmx.model.nodes
    part_names = list(pmx.model.parts.get_names())
    first_ids = list(nodes.get_ids())[:3]
    for function, argument in [(lambda: nodes.renumber(None), "'start_id'"),
                               (lambda: nodes.renumber(1.5), "'start_id'"),
                               (lambda: pmx.model.elements.renumber(None), "'start_id'"),
                               (lambda: pmx.model.parts.translate(part_names[0], [0, 0, 1], 1.5),
                                "'number_of_copies'"),
                               (lambda: pmx.model.parts.mirror(part_names[0], [0, 0, 0], [1, 0, 0], copy=1),
                                "'copy'"),
                               (lambda: nodes.get_ids().contains(None), "'item'"),
                               (lambda: pmx.create.environment_variable("A", "b", active="yes"), "'active'")]:
        message = api_error(function)
        check(message is not None and argument in message and "No method matches" not in message,
              "a wrong typed value must be refused naming " + argument + ": " + str(message))
    check(list(nodes.get_ids())[:3] == first_ids, "a refused renumber changed the node ids")
    check(list(pmx.model.parts.get_names()) == part_names, "a refused transform changed the parts")
    # A float with a whole value is a whole number, and None takes the documented default
    check(nodes.get_ids().contains(float(first_ids[0])), "a whole float must be found in an IntList")
    check(pmx.create.environment_variable("A", "b", active=None).active, "None must take the default True")
    # A NaN or an infinity is refused by the name of the argument, a vector component by its index, before
    # anything is recorded
    parts = pmx.model.parts
    nan, inf = float("nan"), float("inf")
    for function, argument in [(lambda: parts.translate(part_names[0], [0, nan, 0]), "'translate_vector[1]'"),
                               (lambda: parts.rotate(part_names[0], [inf, 0, 0], [0, 0, 1], 90),
                                "'rotate_center[0]'"),
                               (lambda: parts.rotate(part_names[0], [0, 0, 0], [0, 0, 1], nan),
                                "'rotate_angle_deg'"),
                               (lambda: parts.mirror(part_names[0], [0, 0, -inf], [1, 0, 0]), "'mirror_point[2]'"),
                               (lambda: parts.scale(part_names[0], [0, 0, 0], [1, inf, 1]), "'scale_factors[1]'"),
                               (lambda: parts.scale(part_names[0], [nan, 0, 0], [1, 1, 1]), "'scale_center[0]'"),
                               (lambda: pmx.export_to_3mf("finite.3mf", edge_thickness_ratio=inf),
                                "'edge_thickness_ratio'"),
                               (lambda: pmx.create.damping("Rayleigh", alpha=nan), "'alpha'"),
                               (lambda: pmx.create.modal_damping("Constant", viscous_damping_ratio=inf),
                                "'viscous_damping_ratio'"),
                               (lambda: pmx.create.damping_ratio(nan, 1, 2), "'damping_ratio'")]:
        message = api_error(function)
        check(message is not None and argument in message and "finite" in message,
              "a non-finite value must be refused naming " + argument + ": " + str(message))
    check(list(pmx.model.parts.get_names()) == part_names, "a refused non-finite transform changed the parts")
    # A whole number property takes the values and equations replace takes, and refuses None
    properties = pmx.model.properties
    model_type = properties.model_type
    properties.model_type = "SlipWearModel"
    cycles = properties.number_of_cycles
    properties.number_of_cycles = 3.0
    check(properties.number_of_cycles == 3, "a whole float must set the number of cycles")
    properties.number_of_cycles = "=2+2"
    check(properties.number_of_cycles == 4, "an equation must set the number of cycles")
    message = api_error(lambda: setattr(properties, "number_of_cycles", None))
    check(message is not None and "'number_of_cycles'" in message,
          "None must be refused naming the property: " + str(message))
    message = api_error(lambda: setattr(properties, "bdm_remeshing", None))
    check(message is not None and "'bdm_remeshing'" in message,
          "None must be refused naming the property: " + str(message))
    check(properties.number_of_cycles == 4, "a refused assignment changed the number of cycles")
    properties.number_of_cycles = cycles
    properties.model_type = model_type


# A typed .NET parameter made pythonnet refuse a value of another kind - a number for a name, a region for a
# selection - with 'No method matches given arguments', which names no argument. The API takes those values
# itself and refuses them by name, saying what the argument takes, before anything is recorded.
def test_wrong_kind_arguments():
    model = pmx.model
    collections = [model.node_sets, model.element_sets, model.surfaces, model.steps, model.reference_points,
                   model.coordinate_systems, model.mesh.setup_items]
    names_before = [list(collection.get_names()) for collection in collections]
    node_count = len(list(model.nodes.get_ids()))
    selection = pmx.create.selection_by_ids("femodel", "nodes", "nodes", [1, 2, 3])
    region = pmx.create.region("NodeSetName", PREFIX + "Missing")
    for function, argument, words in [
            (lambda: model.node_sets.add(PREFIX + "NS", region), "'selection'", "Selection"),
            (lambda: model.node_sets.add(PREFIX + "NS", None), "'selection'", "None"),
            (lambda: model.node_sets.add(PREFIX + "NS", "nodes"), "'selection'", "str"),
            (lambda: model.node_sets.add(5, selection), "'name'", "string"),
            (lambda: model.node_sets.replace(PREFIX + "Missing", selection=region), "'selection'", "Selection"),
            (lambda: model.element_sets.add(PREFIX + "ES", region), "'selection'", "Selection"),
            (lambda: model.surfaces.add(PREFIX + "S", region), "'selection'", "Selection"),
            (lambda: model.mesh.setup_items.add_local_mesh_size(PREFIX + "LMS", region, element_size=1),
             "'selection'", "Selection"),
            (lambda: model.nodes.merge_coincident(region), "'selection'", "Selection"),
            (lambda: model.reference_points.add_from_selection(PREFIX + "RP", "CenterOfGravity", region),
             "'selection'", "Selection"),
            (lambda: model.coordinate_systems.add_from_selections(PREFIX + "CS", origin_selection=region),
             "'origin_selection'", "Selection"),
            (lambda: pmx.create.split_tool_extend_face(region), "'faces'", "Selection"),
            (lambda: pmx.selection.highlight(region), "'selection'", "Selection"),
            (lambda: model.steps.add_static(5), "'name'", "string"),
            (lambda: model.steps.add_static(None), "'name'", "None"),
            (lambda: model.steps.rename(PREFIX + "Missing", 5), "'new_name'", "string"),
            (lambda: model.node_sets.contains(5), "'name'", "string"),
            (lambda: 5 in model.node_sets, "'name'", "string"),
            # A plain number argument evaluates no equation, so a string, even of a number, is refused
            (lambda: pmx.create.friction(0.1, stick_slope="10"), "'stick_slope'", "must be a number"),
            (lambda: pmx.create.gap_conductance(conductance="5"), "'conductance'", "must be a number"),
            (lambda: model.parts.find_edges_by_angle(model.parts.first().name, "30"), "'edge_angle_deg'",
             "must be a number"),
            # A whole number argument refuses a string of a number as well
            (lambda: model.steps.add_static(PREFIX + "S", incrementation_type="Automatic", max_increments="10"),
             "'max_increments'", "must be a whole number"),
            (lambda: pmx.create.damping_ratio(0.02, "1", 5), "'lowest_mode'", "must be a whole number")]:
        message = api_error(function)
        check(message is not None and argument in message and words in message and
              "No method matches" not in message,
              "a value of the wrong kind must be refused naming " + argument + ": " + str(message))
    check([list(collection.get_names()) for collection in collections] == names_before,
          "a refused call changed the model")
    check(len(list(model.nodes.get_ids())) == node_count, "a refused merge changed the nodes")
    check(model.node_sets.contains(None) is False, "contains(None) must be False")


# One rule for whole numbers: a whole float is the number, a fraction, a bool and a string are refused, in a
# single argument, an ID list and the part ID keys of an ID map alike. A number list refuses a bool, a color
# channel is a finite whole number, and an option string is matched regardless of case and surrounding spaces.
def test_converter_rules():
    create = pmx.create
    whole = create.selection_by_ids("femodel", "nodes", "nodes", [1.0, 2.0])
    check(sorted(list(whole.get_ids())) == [1, 2], "whole floats must be taken as IDs: " + str(list(whole.get_ids())))
    by_float_key = create.selection_by_ids("femodel", "nodes", "surfaces", {1.0: [1.0]})
    by_int_key = create.selection_by_ids("femodel", "nodes", "surfaces", {1: [1]})
    check(sorted(list(by_float_key.get_ids())) == sorted(list(by_int_key.get_ids())),
          "a whole float part ID key and item ID must be taken as integers")
    for function, argument in [(lambda: create.selection_by_ids("femodel", "nodes", "nodes", [1.5]), "'ids'"),
                               (lambda: create.selection_by_ids("femodel", "nodes", "nodes", [True]), "'ids'"),
                               (lambda: create.selection_by_ids("femodel", "nodes", "nodes", ["1"]), "'ids'"),
                               (lambda: create.selection_by_ids("femodel", "nodes", "nodes", [float("nan")]),
                                "'ids'"),
                               (lambda: create.selection_by_ids("femodel", "nodes", "surfaces", {1: [True]}),
                                "'ids[1]'"),
                               (lambda: create.selection_by_ids("femodel", "nodes", "surfaces", {1.5: [1]}),
                                "'ids'"),
                               (lambda: create.selection_by_pick("femodel", "nodes", [0, True, 0], "Surface"),
                                "'point'")]:
        message = api_error(function)
        check(message is not None and argument in message and "No method matches" not in message,
              "a fraction, a bool or a string must be refused naming " + argument + ": " + str(message))
    # A color channel is a whole number from 0 to 255
    parts = pmx.model.parts
    name = parts.first().name
    color_before = list(parts[name].color)
    for channels in [[255, 0.5, 0], [float("nan"), 0, 0], [0, True, 0]]:
        message = api_error(lambda: parts.set_color(name, channels))
        check(message is not None and "'color'" in message, "the channels " + str(channels) +
              " must be refused naming color: " + str(message))
    check(list(parts[name].color) == color_before, "a refused color changed the part")
    parts.set_color(name, [0.0, 0.0, 255.0])
    check(list(parts[name].color) == [0, 0, 255, 255], "whole float channels must set the color")
    parts.reset_color(name)
    # An option string is matched regardless of case and surrounding spaces, a name is not
    padded = create.selection_by_ids(" FeModel ", " nodes", "Nodes ", [1, 2])
    check(padded.view == "femodel" and padded.target == "nodes", "padded option strings were not matched")
    padded.add_by_ids(" nodes ", [3], operation=" add ")
    check(sorted(list(padded.get_ids())) == [1, 2, 3], "a padded source or operation did not resolve")


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_typed_arguments()
    test_wrong_kind_arguments()
    test_converter_rules()
    materials = pmx.model.materials
    test_unknown_keyword(materials)
    test_misspelled_keyword(materials)
    test_keyword_given_twice(materials)
    test_inherited_method(materials)
    test_valid_call(materials)
    print("TEST PASSED")


main()
