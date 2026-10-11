#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the session rules of the API: a relative file name is taken from pmx.work_directory, pmx.is_rerun
#is False on the run that records a script, and new_model and open take discard_changes.
#A model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Api_Session_Rules.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os

PREFIX = "pytest_session_rules"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message, expected_text=None):
    # Only the API's own refusal passes, and one that carries a .NET stack trace is a fault of the application
    try:
        function()
    except Exception as ex:
        kind, text = type(ex).__name__, str(ex)
        if kind != "PythonAPIException" or "   at " in text:
            raise Exception("TEST FAILED: " + message + " - the refusal must be a PythonAPIException without a " +
                            ".NET stack trace, not " + kind + ": " + text[0:500])
        if expected_text is not None and expected_text not in text:
            raise Exception("TEST FAILED: " + message + " - the refusal must say '" + expected_text + "': " +
                            text[0:500])
        return
    raise Exception("TEST FAILED: " + message)


def remove(path):
    if os.path.isfile(path):
        os.remove(path)


def test_is_rerun():
    check(pmx.is_rerun is False, "a script on the run that records it is no rerun: " + repr(pmx.is_rerun))


def test_relative_file_names():
    work_directory = pmx.work_directory
    check(os.path.isdir(work_directory), "the work directory must exist: " + work_directory)
    #
    model_file = PREFIX + ".inp"
    parameters_file = PREFIX + ".pmp"
    full_model_file = os.path.join(work_directory, model_file)
    full_parameters_file = os.path.join(work_directory, parameters_file)
    try:
        remove(full_model_file)
        pmx.export_to_calculix(model_file)
        check(os.path.isfile(full_model_file), "a relative export must be written to the work directory")
        #
        pmx.model.parameters.add(PREFIX, 2.5)
        remove(full_parameters_file)
        pmx.model.parameters.export_to_file(parameters_file)
        check(os.path.isfile(full_parameters_file), "relative parameters must be written to the work directory")
        pmx.model.parameters.remove([PREFIX])
        pmx.model.parameters.import_from_file(parameters_file)
        check(PREFIX in list(pmx.model.parameters.get_names()),
              "relative parameters must be read from the work directory")
        #
        # A relative folder that does not exist under the work directory is refused, naming the full path
        missing = os.path.join("pytest_no_such_folder", model_file)
        check_raises(lambda: pmx.export_to_calculix(missing), "a missing folder must be refused",
                     os.path.join(work_directory, "pytest_no_such_folder"))
        check_raises(lambda: pmx.import_file(PREFIX + "_missing.inp"), "a missing relative file must be refused",
                     os.path.join(work_directory, PREFIX + "_missing.inp"))
    finally:
        remove(full_model_file)
        remove(full_parameters_file)
        if PREFIX in list(pmx.model.parameters.get_names()):
            pmx.model.parameters.remove([PREFIX])


def test_discard_changes():
    # Inside a recorded script both stay refused for replacing the command history, whatever discard_changes says,
    # and a discard_changes that is no boolean is refused naming it
    check_raises(lambda: pmx.new_model("ThreeD", "MM_TON_S_C", discard_changes=True),
                 "new_model must be refused inside a recorded script", "recorded script")
    check_raises(lambda: pmx.new_model("ThreeD", "MM_TON_S_C", discard_changes="yes"),
                 "a discard_changes that is no boolean must be refused", "discard_changes")
    check_raises(lambda: pmx.open(PREFIX + "_missing.pmx", discard_changes="yes"),
                 "a discard_changes that is no boolean must be refused", "discard_changes")


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_is_rerun()
    test_relative_file_names()
    test_discard_changes()
    print("TEST PASSED")


main()
