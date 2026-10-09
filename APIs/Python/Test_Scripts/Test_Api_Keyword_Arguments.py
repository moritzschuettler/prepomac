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
    check(pmx.create.environment_variable("A", "b", active=None).Active, "None must take the default True")
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


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_typed_arguments()
    materials = pmx.model.materials
    test_unknown_keyword(materials)
    test_misspelled_keyword(materials)
    test_keyword_given_twice(materials)
    test_inherited_method(materials)
    test_valid_call(materials)
    print("TEST PASSED")


main()
