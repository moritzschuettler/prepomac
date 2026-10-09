#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the document surface: model properties, equation parameters, and the file exports.
#A model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Document.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile


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


def test_properties():
    check(pmx.model.is_open(), "a model must be open")
    check(pmx.model.properties.unit_system == "MM_TON_S_C",
          "unexpected unit system " + pmx.model.properties.unit_system)
    check(pmx.model.properties.model_space == "ThreeD",
          "unexpected model space " + pmx.model.properties.model_space)
    #
    pmx.model.properties.name = "PyTest_Model"
    check(pmx.model.name == "PyTest_Model", "the model name was not changed")
    #
    check_raises(lambda: setattr(pmx.model.properties, "unit_system", "NOT_A_UNIT_SYSTEM"),
                 "an unknown unit system must be rejected")
    # Undefined is the state of a model nothing has set, not a value a model can be given
    for argument in ["model_space", "unit_system"]:
        message = raised_message(lambda: setattr(pmx.model.properties, argument, "undefined"))
        check(message is not None and argument in message and "Undefined" in message,
              "an undefined " + argument + " must be refused naming it: " + str(message))
    check(pmx.model.properties.unit_system == "MM_TON_S_C" and pmx.model.properties.model_space == "ThreeD",
          "a refused value changed the model properties")


def test_parameters():
    for name in list(pmx.model.parameters.get_names()):
        if name.startswith("pytest"):
            pmx.model.parameters.remove([name])
    count_before = len(pmx.model.parameters)
    # A number is a value, a string is an equation
    pmx.model.parameters.add("pytest_a", 2.5)
    pmx.model.parameters.add("pytest_b", "pytest_a * 4")
    check(len(pmx.model.parameters) == count_before + 2, "the parameters were not added")
    check(pmx.model.parameters["pytest_a"].value == 2.5, "unexpected value of pytest_a")
    check(pmx.model.parameters["pytest_b"].value == 10.0,
          "the equation did not evaluate, got " + str(pmx.model.parameters["pytest_b"].value))
    # Replace
    pmx.model.parameters.replace("pytest_a", "pytest_a", 3.0)
    check(pmx.model.parameters["pytest_b"].value == 12.0, "the dependent parameter did not update")
    # A parameter that is itself an equation must be visible to the one that follows it
    pmx.model.parameters.add("pytest_d", "2 * 3")
    pmx.model.parameters.add("pytest_e", "pytest_d * 3")
    check(pmx.model.parameters["pytest_e"].value == 18.0,
          "the chained equation did not evaluate, got " + str(pmx.model.parameters["pytest_e"].value))
    pmx.model.parameters.remove(["pytest_e", "pytest_d"])
    # A description is free text kept beside the parameter
    pmx.model.parameters.add("pytest_m", 1.0, description="A described parameter")
    check(pmx.model.parameters["pytest_m"].description == "A described parameter",
          "the description was not kept, got " + str(pmx.model.parameters["pytest_m"].description))
    # A replace that is not given one keeps it, an empty one clears it
    pmx.model.parameters.replace("pytest_m", "pytest_m", 2.0)
    check(pmx.model.parameters["pytest_m"].description == "A described parameter",
          "the description was not kept by the replace")
    pmx.model.parameters.replace("pytest_m", "pytest_m", None, "")
    check(not pmx.model.parameters["pytest_m"].description, "the description was not cleared")
    pmx.model.parameters.remove(["pytest_m"])
    # Export
    file_name = os.path.join(tempfile.gettempdir(), "pytest_parameters.pmp")
    pmx.model.parameters.export_to_file(file_name)
    check(os.path.isfile(file_name), "the parameters file was not written")
    os.remove(file_name)
    # Rename uses replace, because the parameters have no rename command. A renamed parameter is not
    # rewritten inside the equations that reference it, so the dependent one is removed first.
    pmx.model.parameters.remove(["pytest_b"])
    pmx.model.parameters.rename("pytest_a", "pytest_c")
    check(pmx.model.parameters.contains("pytest_c"), "the parameter was not renamed")
    check(not pmx.model.parameters.contains("pytest_a"), "the old parameter name still exists")
    # Duplicating is not supported for parameters
    check_raises(lambda: pmx.model.parameters.duplicate(["pytest_c"]),
                 "duplicating a parameter must be rejected")
    #
    pmx.model.parameters.remove(["pytest_c"])
    check(len(pmx.model.parameters) == count_before, "the parameters were not removed")


def test_parameter_refusals():
    names = list(pmx.model.parameters.get_names())
    # An equation the model cannot evaluate is refused naming the value, before anything is recorded
    for call in [lambda: pmx.model.parameters.add("pytest_self", "pytest_self + 1"),
                 lambda: pmx.model.parameters.add("pytest_bad", "pytest_missing * 2")]:
        message = raised_message(call)
        check(message is not None and "'value'" in message, "the refusal must name the value: " + str(message))
    pmx.model.parameters.add("pytest_r", 1.0)
    pmx.model.parameters.add("pytest_s", "pytest_r * 2")
    for value in ["pytest_r * 2", "pytest_s + 1"]:
        message = raised_message(lambda: pmx.model.parameters.replace("pytest_r", value=value))
        check(message is not None and "'value'" in message,
              "a replace that makes the parameter use itself must be refused naming the value: " + str(message))
    check(pmx.model.parameters["pytest_r"].value == 1.0, "a refused replace changed the parameter")
    pmx.model.parameters.remove(["pytest_s", "pytest_r"])
    # set_all refuses a padded name rather than trimming it, and names the item that is not a value
    message = raised_message(lambda: pmx.model.parameters.set_all([" pytest_p"], [1.0]))
    check(message is not None and "'names'" in message, "a padded name must be refused: " + str(message))
    message = raised_message(lambda: pmx.model.parameters.set_all(["pytest_p", "pytest_q"], [1.0, [2.0]]))
    check(message is not None and "values[1]" in message, "the item that is not a value must be named: " + str(message))
    message = raised_message(lambda: pmx.model.parameters.set_all(["pytest_p", "pytest_q"], [1.0, None]))
    check(message is not None and "values[1]" in message, "a None item must be named: " + str(message))
    check(list(pmx.model.parameters.get_names()) == names, "a refused call changed the parameters")


def test_parameters_set_all():
    # set_all replaces the whole table, so whatever is there is put back at the end
    kept_names = list(pmx.model.parameters.get_names())
    kept_values = [pmx.model.parameters[name].equation for name in kept_names]
    # A parameter may use a parameter that is given after it, which add cannot do
    pmx.model.parameters.set_all(kept_names + ["pytest_g", "pytest_f"], kept_values + ["pytest_f * 2", 4.0])
    check(pmx.model.parameters["pytest_g"].value == 8.0,
          "the forward reference did not evaluate, got " + str(pmx.model.parameters["pytest_g"].value))
    names = list(pmx.model.parameters.get_names())
    check(names[-2:] == ["pytest_g", "pytest_f"], "the order of the table was not kept: " + str(names))
    # A table that cannot be evaluated is rejected before anything is recorded
    check_raises(lambda: pmx.model.parameters.set_all(["pytest_h"], ["pytest_missing * 2"]),
                 "a table that cannot be evaluated must be rejected")
    check_raises(lambda: pmx.model.parameters.set_all(["pytest_i", "pytest_j"],
                                                      ["pytest_j + 1", "pytest_i + 1"]),
                 "a circular reference must be rejected")
    check_raises(lambda: pmx.model.parameters.set_all(["pytest_k", "pytest_k"], [1.0, 2.0]),
                 "a repeated name must be rejected")
    check(list(pmx.model.parameters.get_names()) == names, "a rejected call must not change the table")
    # A description survives a .pmp round trip, and the file may name a parameter before the one it
    # uses: the import resolves the whole file before it builds the parameters
    pmx.model.parameters.set_all(kept_names + ["pytest_g", "pytest_f"], kept_values + ["pytest_f * 2", 4.0],
                                 [""] * len(kept_names) + ["", "A described parameter"])
    file_name = os.path.join(tempfile.gettempdir(), "pytest_described.pmp")
    pmx.model.parameters.export_to_file(file_name)
    pmx.model.parameters.import_from_file(file_name)
    os.remove(file_name)
    check(pmx.model.parameters["pytest_f"].description == "A described parameter",
          "the description did not survive the .pmp round trip, got " +
          str(pmx.model.parameters["pytest_f"].description))
    check(not pmx.model.parameters["pytest_g"].description, "an empty description came back as text")
    check(pmx.model.parameters["pytest_g"].value == 8.0, "the round tripped equation did not evaluate")
    check(list(pmx.model.parameters.get_names()) == names, "the file order was not kept: " +
          str(list(pmx.model.parameters.get_names())))
    #
    pmx.model.parameters.remove(["pytest_g", "pytest_f"])
    check(list(pmx.model.parameters.get_names()) == kept_names, "the parameters were not restored")


def test_export():
    file_name = os.path.join(tempfile.gettempdir(), "pytest_model.inp")
    if os.path.isfile(file_name):
        os.remove(file_name)
    pmx.export_to_calculix(file_name)
    check(os.path.isfile(file_name), "the CalculiX file was not written")
    os.remove(file_name)
    #
    check_raises(lambda: pmx.export_to_calculix(os.path.join(tempfile.gettempdir(), "no_such_dir", "m.inp")),
                 "a missing output directory must be rejected")
    # 3MF takes its edge thickness ratio as a plain value
    check_raises(lambda: pmx.export_to_3mf(os.path.join(tempfile.gettempdir(), "m.3mf"), 0),
                 "a zero edge thickness ratio must be rejected")


def test_open_is_refused_in_a_script():
    # Opening replaces the command history, which the running script is part of
    check_raises(lambda: pmx.open("any.pmx"), "open must be refused inside a recorded script")
    check_raises(lambda: pmx.new_model("ThreeD", "MM_TON_S_C"),
                 "new_model must be refused inside a recorded script")
    message = raised_message(lambda: pmx.new_model("Undefined", "MM_TON_S_C"))
    check(message is not None and "model_space" in message, "an undefined model space must be refused: " + str(message))


def main():
    test_properties()
    test_parameters()
    test_parameter_refusals()
    test_parameters_set_all()
    test_export()
    test_open_is_refused_in_a_script()
    print("TEST PASSED")


main()
