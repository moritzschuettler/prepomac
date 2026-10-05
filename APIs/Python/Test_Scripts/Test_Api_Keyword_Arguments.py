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


def main():
    check(pmx.model.is_open(), "a model must be open")
    materials = pmx.model.materials
    test_unknown_keyword(materials)
    test_misspelled_keyword(materials)
    test_keyword_given_twice(materials)
    test_inherited_method(materials)
    test_valid_call(materials)
    print("TEST PASSED")


main()
