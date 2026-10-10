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


def work_directory():
    """The work directory of the settings, which a new analysis inherits."""
    name = "PyTest_WorkDirectory"
    job = pmx.jobs.add(name)
    try:
        return job.work_directory
    finally:
        pmx.jobs.remove(name)


def test_properties():
    check(pmx.model.is_open(), "a model must be open")
    check(pmx.model.properties.unit_system == "MM_TON_S_C",
          "unexpected unit system " + pmx.model.properties.unit_system)
    check(pmx.model.properties.model_space == "ThreeD",
          "unexpected model space " + pmx.model.properties.model_space)
    #
    pmx.model.properties.name = "PyTest_Model"
    check(pmx.model.name == "PyTest_Model", "the model name was not changed")
    # A padded name is no name: it is refused rather than trimmed, by the property and by replace
    for bad_name in [" PyTest_Model", "PyTest_Model ", "  "]:
        for call in [lambda: setattr(pmx.model.properties, "name", bad_name),
                     lambda: pmx.model.properties.replace(name=bad_name)]:
            message = raised_message(call)
            check(message is not None and "'name'" in message,
                  "the model name " + repr(bad_name) + " must be refused naming it: " + str(message))
    check(pmx.model.name == "PyTest_Model", "a refused model name changed the name")
    # A value that is not a string is refused naming the property, rather than with a TypeError of pythonnet
    for name in ["name", "model_space", "unit_system", "model_type", "global_results_file_name", "slip_wear_results"]:
        message = raised_message(lambda: setattr(pmx.model.properties, name, 5))
        check(message is not None and "'" + name + "'" in message and "must be a string" in message,
              "a number assigned to " + name + " must be refused naming it: " + str(message))
    check(pmx.model.name == "PyTest_Model", "a refused assignment changed the model name")
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
    # A value the model type does not use is refused naming it, judged by the model type the call ends with
    model_type = pmx.model.properties.model_type
    pmx.model.properties.model_type = "GeneralModel"
    for argument, value in [("global_results_file_name", "global.frd"), ("slip_wear_results", "All"),
                            ("number_of_cycles", 2), ("cycles_increment", 1), ("number_of_smoothing_steps", 1),
                            ("bdm_remeshing", True)]:
        message = raised_message(lambda: pmx.model.properties.replace(**{argument: value}))
        check(message is not None and "'" + argument + "'" in message and "GeneralModel" in message,
              "a " + argument + " on a general model must be refused naming it: " + str(message))
        message = raised_message(lambda: setattr(pmx.model.properties, argument, value))
        check(message is not None and "'" + argument + "'" in message,
              "an assigned " + argument + " on a general model must be refused naming it: " + str(message))
    message = raised_message(lambda: pmx.model.properties.replace(model_type="Submodel", number_of_cycles=2))
    check(message is not None and "'number_of_cycles'" in message and "Submodel" in message,
          "a slip wear value on a submodel must be refused naming it: " + str(message))
    message = raised_message(lambda: pmx.model.properties.replace(model_type="SlipWearModel",
                                                                  global_results_file_name="global.frd"))
    check(message is not None and "'global_results_file_name'" in message,
          "a submodel file on a slip wear model must be refused naming it: " + str(message))
    check(pmx.model.properties.model_type == "GeneralModel", "a refused replace changed the model type")
    check(len(pmx.model.properties.get_equations()) == 0, "a general model must return no slip wear equations")
    # A replace that changes the model type takes the values of the new type in the same call
    pmx.model.properties.replace(model_type="SlipWearModel", number_of_cycles="=1 + 3", cycles_increment=1)
    check(pmx.model.properties.model_type == "SlipWearModel" and pmx.model.properties.number_of_cycles == 4,
          "a slip wear value given with the model type was not accepted")
    # The dialog refuses cycles the increment does not divide, in these words, and so does the API, judging the
    # properties the call ends with and the value an expression evaluates to
    for arguments in [{"cycles_increment": 3}, {"number_of_cycles": "=2 + 3", "cycles_increment": 2}]:
        message = raised_message(lambda: pmx.model.properties.replace(**arguments))
        check(message is not None and "must be a multiple of the cycles increment" in message and
              "'cycles_increment'" in message, "cycles the increment does not divide must be refused: " + str(message))
    message = raised_message(lambda: setattr(pmx.model.properties, "cycles_increment", 3))
    check(message is not None and "must be a multiple of the cycles increment" in message,
          "an assigned increment that does not divide the cycles must be refused: " + str(message))
    check(pmx.model.properties.number_of_cycles == 4 and pmx.model.properties.cycles_increment == 1,
          "a refused replace changed the slip wear cycles")
    # The model raises a count below its minimum to the minimum, so the value is refused naming the argument
    counts = [pmx.model.properties.number_of_cycles, pmx.model.properties.cycles_increment,
              pmx.model.properties.number_of_smoothing_steps]
    for argument, value in [("number_of_cycles", 0), ("cycles_increment", 0), ("number_of_smoothing_steps", -2),
                            ("number_of_cycles", "=1 - 1")]:
        message = raised_message(lambda: pmx.model.properties.replace(**{argument: value}))
        check(message is not None and "'" + argument + "'" in message,
              "a " + argument + " of " + str(value) + " must be refused naming it: " + str(message))
    check([pmx.model.properties.number_of_cycles, pmx.model.properties.cycles_increment,
           pmx.model.properties.number_of_smoothing_steps] == counts, "a refused count changed the model properties")
    pmx.model.properties.replace(number_of_cycles=2, cycles_increment=1, number_of_smoothing_steps=0)
    check(pmx.model.properties.number_of_cycles == 2 and pmx.model.properties.number_of_smoothing_steps == 0,
          "the counts at their minimum were not accepted")
    pmx.model.properties.replace(number_of_cycles=counts[0], cycles_increment=counts[1],
                                 number_of_smoothing_steps=counts[2])
    # A property assignment the replace would take as not given is refused rather than ignored, and a property
    # typed as a whole number or a boolean takes what replace takes
    model_type = pmx.model.properties.model_type
    for argument in ["model_type", "global_results_file_name", "slip_wear_results", "number_of_cycles",
                     "cycles_increment", "number_of_smoothing_steps", "bdm_remeshing"]:
        message = raised_message(lambda: setattr(pmx.model.properties, argument, None))
        check(message is not None and "'" + argument + "'" in message,
              "None must be refused for " + argument + ": " + str(message))
    check(pmx.model.properties.model_type == model_type, "a refused assignment changed the model type")
    pmx.model.properties.cycles_increment = "=1 + 1"
    check(pmx.model.properties.cycles_increment == 2, "an equation must set cycles_increment")
    check(pmx.model.properties.get_equations().get("cycles_increment", "").replace(" ", "") == "=1+1",
          "the cycles_increment equation did not read back: " + str(pmx.model.properties.get_equations()))
    pmx.model.properties.cycles_increment = counts[1]
    pmx.model.properties.model_type = model_type
    message = raised_message(lambda: pmx.model.properties.replace(clear_undefined_constants=1))
    check(message is not None and "'clear_undefined_constants'" in message,
          "a non boolean clear_undefined_constants must be refused naming it: " + str(message))
    # A physical constant reads back as a float or as None, and either goes back into the property
    absolute_zero = pmx.model.properties.absolute_zero
    check(absolute_zero is None or isinstance(absolute_zero, float),
          "a physical constant must read back as a float or None: " + repr(absolute_zero))
    pmx.model.properties.absolute_zero = absolute_zero
    check(pmx.model.properties.absolute_zero == absolute_zero, "the physical constant did not go back")


def test_model_space_and_unused_values():
    properties = pmx.model.properties
    # The dialog changes the model space only between the 2D spaces, and updates the element types when it does;
    # the cube is a 3D model, so a 2D space and Undefined are refused naming the argument
    for value in ["PlaneStress", "PlaneStrain", "Axisymmetric", "Undefined"]:
        message = raised_message(lambda: properties.replace(model_space=value))
        check(message is not None and "'model_space'" in message,
              "a replace to the model space " + value + " must be refused naming it: " + str(message))
    check(properties.model_space == "ThreeD", "a refused replace changed the model space")
    properties.replace(model_space="threed")
    check(properties.model_space == "ThreeD", "a replace keeping the model space was not accepted")
    # Assigning the model space sets it as the new model dialog does, without the element types, so a defined
    # model space of a model with a mesh is changed by the replace
    message = raised_message(lambda: setattr(properties, "model_space", "PlaneStrain"))
    check(message is not None and "replace(model_space" in message,
          "assigning another model space to a meshed model must point to the replace: " + str(message))
    check(properties.model_space == "ThreeD", "a refused assignment changed the model space")
    # None would record a command that changes nothing
    for argument in ["model_space", "unit_system"]:
        message = raised_message(lambda: setattr(properties, argument, None))
        check(message is not None and "'" + argument + "'" in message,
              "None must be refused for " + argument + ": " + str(message))
    check(properties.unit_system == "MM_TON_S_C", "a refused assignment changed the unit system")
    # A value the model type does not use reads back as None, which goes back into the replace as not given
    model_type = properties.model_type
    properties.model_type = "GeneralModel"
    unused = ["global_results_file_name", "slip_wear_results", "number_of_cycles", "cycles_increment",
              "number_of_smoothing_steps", "bdm_remeshing"]
    values = {argument: getattr(properties, argument) for argument in unused}
    check(all(value is None for value in values.values()),
          "a general model must read the submodel and slip wear values as None: " + str(values))
    properties.replace(**values)
    # The dialog refuses a submodel whose global results file it cannot find in the work directory, and a file name
    # with a path or a special character, in these words, and so does the API
    file_name = "pytest_global.frd"
    full_file_name = os.path.join(work_directory(), file_name)
    open(full_file_name, "w").close()
    try:
        for function, fragment in [
                (lambda: properties.replace(model_type="Submodel", global_results_file_name=file_name + ".missing"),
                 "does not exist"),
                (lambda: properties.replace(model_type="Submodel"), "does not exist"),
                (lambda: setattr(properties, "model_type", "Submodel"), "does not exist"),
                (lambda: properties.replace(model_type="Submodel", global_results_file_name=""), "must not be empty"),
                (lambda: properties.replace(model_type="Submodel", global_results_file_name=full_file_name),
                 "without path"),
                (lambda: properties.replace(model_type="Submodel", global_results_file_name="sub/" + file_name),
                 "without path"),
                (lambda: properties.replace(model_type="Submodel", global_results_file_name="pytest global.frd"),
                 "special characters")]:
            message = raised_message(function)
            check(message is not None and fragment in message and "'global_results_file_name'" in message,
                  "a submodel without a global results file in the work directory must be refused: " + str(message))
        check(properties.model_type == "GeneralModel", "a refused replace changed the model type")
        properties.replace(model_type="Submodel", global_results_file_name=file_name)
        check(properties.global_results_file_name == file_name and properties.number_of_cycles is None,
              "a submodel must read its file and no slip wear value: " + str(properties.global_results_file_name))
    finally:
        os.remove(full_file_name)
    # A setter, a rename and a replace that neither gives the file nor makes the model a submodel have no dialog OK,
    # so they do not look the stored file up again, and a replayed journal does not depend on the work directory
    properties.absolute_zero = -273.15
    properties.absolute_zero = None
    model_name = pmx.model.name
    properties.name = model_name + "_Submodel"
    properties.name = model_name
    properties.replace(model_type="Submodel")
    check(properties.global_results_file_name == file_name, "a replace without the file changed the stored file")
    message = raised_message(lambda: properties.replace(global_results_file_name=file_name))
    check(message is not None and "does not exist" in message and "'global_results_file_name'" in message,
          "a given global results file must be looked up in the work directory: " + str(message))
    properties.replace(model_type="SlipWearModel")
    check(properties.global_results_file_name is None and isinstance(properties.number_of_cycles, int) and
          isinstance(properties.bdm_remeshing, bool), "a slip wear model must read its values and no file")
    properties.model_type = model_type


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
    # A change that leaves another parameter unable to be evaluated is refused naming it, the way the parameter
    # editor refuses such a table, rather than recorded and reported by the model in a message box
    pmx.model.parameters.add("pytest_u", "1 / pytest_r")
    for call, argument in [(lambda: pmx.model.parameters.remove(["pytest_r"]), "'names'"),
                           (lambda: pmx.model.parameters.rename("pytest_r", "pytest_t"), "'new_name'"),
                           (lambda: pmx.model.parameters.replace("pytest_r", name="pytest_t"), "'name'"),
                           (lambda: pmx.model.parameters.replace("pytest_r", value=0), "'value'")]:
        message = raised_message(call)
        check(message is not None and argument in message and "pytest_u" in message,
              "a change that breaks a dependent parameter must be refused naming both: " + str(message))
    check(pmx.model.parameters.contains("pytest_r") and pmx.model.parameters["pytest_r"].value == 1.0 and
          not pmx.model.parameters.contains("pytest_t"), "a refused change altered the parameters")
    # A parameter named twice is refused
    message = raised_message(lambda: pmx.model.parameters.remove(["pytest_u", "PYTEST_U"]))
    check(message is not None and "'names'" in message, "a parameter named twice must be refused: " + str(message))
    # Removing a parameter together with every one that uses it is allowed
    pmx.model.parameters.remove(["pytest_u", "pytest_s", "pytest_r"])
    # set_all refuses a padded name rather than trimming it, and names the item that is not a value
    message = raised_message(lambda: pmx.model.parameters.set_all([" pytest_p"], [1.0]))
    check(message is not None and "'names'" in message, "a padded name must be refused: " + str(message))
    message = raised_message(lambda: pmx.model.parameters.set_all(["pytest_p", "pytest_q"], [1.0, [2.0]]))
    check(message is not None and "values[1]" in message, "the item that is not a value must be named: " + str(message))
    message = raised_message(lambda: pmx.model.parameters.set_all(["pytest_p", "pytest_q"], [1.0, None]))
    check(message is not None and "values[1]" in message, "a None item must be named: " + str(message))
    # A name with a hyphen is refused naming the argument it was given in
    pmx.model.parameters.add("pytest_w", 1.0)
    for call, argument in [(lambda: pmx.model.parameters.add("pytest-x", 1.0), "'name'"),
                           (lambda: pmx.model.parameters.replace("pytest_w", name="pytest-x"), "'name'"),
                           (lambda: pmx.model.parameters.rename("pytest_w", "pytest-x"), "'new_name'"),
                           (lambda: pmx.model.parameters.set_all(["pytest-x"], [1.0]), "'names'")]:
        message = raised_message(call)
        check(message is not None and argument in message and "hyphen" in message,
              "a hyphen must be refused naming " + argument + ": " + str(message))
    # A replace without a name keeps the parameter under its own spelling, whatever spelling old_name has
    for spelling in ["PYTEST_W", "Pytest_W"]:
        pmx.model.parameters.replace(spelling, value=2.0)
        check("pytest_w" in list(pmx.model.parameters.get_names()),
              "a replace of '" + spelling + "' changed the stored spelling: " + str(list(pmx.model.parameters.get_names())))
    check(pmx.model.parameters["pytest_w"].value == 2.0, "the mixed case replace did not change the value")
    pmx.model.parameters.remove(["pytest_w"])
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
    test_model_space_and_unused_values()
    test_parameters()
    test_parameter_refusals()
    test_parameters_set_all()
    test_export()
    test_open_is_refused_in_a_script()
    print("TEST PASSED")


main()
