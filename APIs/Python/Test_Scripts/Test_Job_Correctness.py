#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the analysis settings contract: an explicit value survives the application settings, a
#replacement keeps what it is not told about, an inherit switch restores the application default,
#a timeout reports the same status through the return value and the analysis itself, and an argument
#that is the solver default follows a new name or solver while an edited one is kept. It also checks
#the refusals: an invalid path, a blank environment variable, a kill of an analysis that is
#not running, opening the results of a running analysis or a missing results file, and removing or
#renaming a running analysis or replacing the model while it runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Job_Correctness.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import os
import shutil
import tempfile
import time

PREFIX = "PyTest_Job_"
# A shell that simply waits is a solver that never finishes, which is what a timeout needs
SLEEPING_SOLVER = os.path.join(os.environ.get("WINDIR", "C:\\Windows"), "System32", "WindowsPowerShell",
                               "v1.0", "powershell.exe")
SLEEPING_ARGUMENT = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 30\""


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


def clean():
    for collection in [pmx.jobs, pmx.model.steps, pmx.model.sections, pmx.model.materials]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def build_solvable_model():
    # The solver refuses to start on elements without a section, and a refused prompt ends the run
    part_selection = pmx.create.selection_by_ids('femodel', 'elements', 'parts',
                                                 [pmx.model.parts.first().id])
    pmx.model.materials.add(PREFIX + "Steel", pmx.create.elastic(210000, 0.3))
    pmx.model.sections.add_solid(PREFIX + "Section", PREFIX + "Steel", part_selection)
    pmx.model.steps.add_static(PREFIX + "Step")


def test_explicit_settings(work):
    variable = pmx.create.environment_variable("PMX_TEST_VALUE", "one")
    job = pmx.jobs.add(PREFIX + "Explicit", fem_solver="Calculix", executable=SLEEPING_SOLVER,
                       argument=SLEEPING_ARGUMENT, work_directory=work, number_of_cpus=2,
                       environment_variables=[variable])
    check(job.executable == SLEEPING_SOLVER, "the explicit executable was overwritten: " + job.executable)
    check(job.work_directory == work, "the explicit work directory was overwritten")
    check(job.number_of_cpus == 2, "the explicit CPU count was overwritten")
    check(job.environment_variables[0].name == "PMX_TEST_VALUE", "the environment variable was not stored")
    # The readback is a detached part with snake_case read-backs, never the .NET model object
    copy = job.environment_variables
    check(type(copy) is list, "the environment variables must read back as a plain list: " + str(type(copy)))
    check(copy[0].get_values() == {"name": "PMX_TEST_VALUE", "value": "one", "active": True},
          "the environment variable did not read back under the pmx.create argument names: " +
          repr(copy[0].get_values()))
    check(not hasattr(copy[0], "Value"), "the environment variable must not hand out the .NET model object")
    check(pmx.create.environment_variable(**copy[0].get_values()).get_values() == copy[0].get_values(),
          "the environment variable values did not go back into pmx.create.environment_variable")
    # The read-back goes back into the argument that sets it
    pmx.jobs.replace(PREFIX + "Explicit", environment_variables=job.environment_variables)
    variables = pmx.jobs[PREFIX + "Explicit"].environment_variables
    check(len(variables) == 1 and variables[0].name == "PMX_TEST_VALUE" and variables[0].value == "one",
          "the environment variables did not survive a round trip")
    # Adding another analysis reapplies the settings, which must leave the explicit values alone
    pmx.jobs.add(PREFIX + "Other")
    check(pmx.jobs[PREFIX + "Explicit"].executable == SLEEPING_SOLVER, "the executable was reset")
    check(pmx.jobs[PREFIX + "Explicit"].number_of_cpus == 2, "the CPU count was reset")


def test_replacement_and_inheritance():
    pmx.jobs.replace(PREFIX + "Explicit", compatibility_mode=False)
    check(pmx.jobs[PREFIX + "Explicit"].executable == SLEEPING_SOLVER, "the replacement lost the executable")
    check(pmx.jobs[PREFIX + "Explicit"].number_of_cpus == 2, "the replacement lost the CPU count")
    #
    pmx.jobs.replace(PREFIX + "Explicit", inherit_number_of_cpus=True)
    check(pmx.jobs[PREFIX + "Explicit"].executable == SLEEPING_SOLVER,
          "restoring one default reset an unrelated value")
    check_raises(lambda: pmx.jobs.replace(PREFIX + "Explicit", number_of_cpus=4, inherit_number_of_cpus=True),
                 "a value and its inherit switch were accepted together")
    pmx.jobs.replace(PREFIX + "Explicit", number_of_cpus=2)
    check(pmx.jobs[PREFIX + "Explicit"].number_of_cpus == 2, "the CPU count was not restored")


def test_rename_by_case():
    # An analysis renamed by case only keeps its own name, which no other analysis holds
    name = PREFIX + "Case"
    pmx.jobs.add(name)
    pmx.jobs.rename(name, name.upper())
    check(pmx.jobs.contains(name.upper()) and name.upper() in list(pmx.jobs.get_names()),
          "the analysis was not renamed by case: " + str(pmx.jobs.get_names()))
    check_raises(lambda: pmx.jobs.rename(name.upper(), PREFIX + "Other".upper()),
                 "a rename onto another analysis was accepted")
    pmx.jobs.remove(name.upper())


def test_argument_follows_name_and_solver():
    # An argument that is the solver default for the name follows a new name or solver; an edited one is kept
    def argument(name):
        return pmx.jobs[PREFIX + name].argument
    pmx.jobs.add(PREFIX + "Arg1")
    pmx.jobs.replace(PREFIX + "Arg1", PREFIX + "Arg2")
    check(argument("Arg2") == PREFIX + "Arg2", "the default argument did not follow the name: " + argument("Arg2"))
    pmx.jobs.replace(PREFIX + "Arg2", fem_solver="Abaqus")
    check(argument("Arg2") == "job=" + PREFIX + "Arg2",
          "the default argument did not follow the solver: " + argument("Arg2"))
    pmx.jobs.replace(PREFIX + "Arg2", argument="-custom")
    pmx.jobs.replace(PREFIX + "Arg2", PREFIX + "Arg3", fem_solver="Calculix")
    check(argument("Arg3") == "-custom", "the edited argument was lost: " + argument("Arg3"))
    pmx.jobs.replace(PREFIX + "Arg3", argument=PREFIX + "Arg3")
    pmx.jobs.replace(PREFIX + "Arg3", PREFIX + "Arg4", argument="-given")
    check(argument("Arg4") == "-given", "a given argument must win: " + argument("Arg4"))
    # A rename treats the argument the way a replace does: an edited one is kept, a default one follows
    pmx.jobs.rename(PREFIX + "Arg4", PREFIX + "Arg5")
    check(argument("Arg5") == "-given", "a rename dropped the edited argument: " + argument("Arg5"))
    pmx.jobs.replace(PREFIX + "Arg5", argument=PREFIX + "Arg5")
    pmx.jobs.rename(PREFIX + "Arg5", PREFIX + "Arg6")
    check(argument("Arg6") == PREFIX + "Arg6", "the default argument did not follow a rename: " + argument("Arg6"))
    pmx.jobs.remove(PREFIX + "Arg6")


def test_rejected_arguments():
    check_raises(lambda: pmx.jobs.add(PREFIX + "Fractional", number_of_cpus=2.5), "a fractional CPU count was accepted")
    check_raises(lambda: pmx.jobs.add(PREFIX + "Zero", number_of_cpus=0), "a zero CPU count was accepted")
    check(not pmx.jobs.contains(PREFIX + "Fractional"), "a refused analysis was recorded")
    check_raises(lambda: pmx.jobs[PREFIX + "Explicit"].run(timeout=float("nan")), "a NaN timeout was accepted")
    check_raises(lambda: pmx.jobs[PREFIX + "Explicit"].run(timeout=float("inf")),
                 "an infinite timeout was accepted")
    check_raises(lambda: pmx.jobs[PREFIX + "Explicit"].run(timeout=-1), "a negative timeout was accepted")
    check_raises(lambda: pmx.jobs[PREFIX + "Explicit"].run(wait=False, timeout=10),
                 "a timeout without waiting was accepted")


def test_custom_work_directory(work):
    # An analysis with its own work directory must be self contained: the input file is written
    # where the solver runs, not where the application settings point
    settings_work = pmx.jobs[PREFIX + "Other"].work_directory
    custom = os.path.join(work, PREFIX + "Directory")
    if not os.path.isdir(custom):
        os.makedirs(custom)
    name = PREFIX + "Custom"
    input_file = os.path.join(custom, name + ".inp")
    settings_input_file = os.path.join(settings_work, name + ".inp")
    for stale in (input_file, settings_input_file):
        if os.path.isfile(stale):
            os.remove(stale)
    #
    job = pmx.jobs.add(name, fem_solver="Calculix", executable=SLEEPING_SOLVER,
                       argument=SLEEPING_ARGUMENT, work_directory=custom)
    check(job.work_directory == custom, "the analysis lost its own work directory")
    status = pmx.jobs[name].run(timeout=1)
    check(status == "TimedOut", "the run in the analysis work directory returned " + status)
    check(os.path.isfile(input_file), "the input file was not written into the analysis work directory")
    check(not os.path.isfile(settings_input_file),
          "the input file was written into the application work directory instead")
    os.remove(input_file)
    # A work directory that does not exist is reported rather than left for the solver to fail on
    pmx.jobs.replace(name, work_directory=os.path.join(custom, "__missing__"))
    check_raises(lambda: pmx.jobs[name].run(timeout=1),
                 "a missing analysis work directory was accepted")


def test_a_solver_that_crashes(work):
    # A solver that ends with an exit code other than zero has failed, although it printed no error: a crashed
    # CalculiX stops without one, leaving the results of the steps before the crash
    name = PREFIX + "Crash"
    pmx.jobs.add(name, fem_solver="Calculix", executable=SLEEPING_SOLVER,
                 argument="-NoProfile -NonInteractive -Command \"Set-Content " + name + ".frd ('x' * 400); exit 3\"",
                 work_directory=work)
    status = pmx.jobs[name].run(timeout=60)
    check(status == "FailedWithResults", "a solver that left results and exited with code 3 returned " + status)
    check("exit code 3" in pmx.jobs[name].get_output(), "the output must name the exit code")
    pmx.jobs.remove(name)


def test_invalid_items_refuse_the_run(work):
    # The run dialog asks whether to go on with invalid items, which a script cannot answer, and an invalid item would
    # be written without what it lost: a force in a removed coordinate system was applied in the global directions
    system = PREFIX + "CS"
    load_name = PREFIX + "Force"
    loads = pmx.model.steps[PREFIX + "Step"].loads
    pmx.model.coordinate_systems.add(system, [0, 0, 0], [0, 1, 0], [-1, 0, 0])
    loads.add_concentrated_force(load_name, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [5]), f1=1,
                                 coordinate_system_name=system)
    pmx.model.coordinate_systems.remove([system])
    check(not loads[load_name].valid, "a force in a removed coordinate system must be invalid")
    name = PREFIX + "Invalid"
    pmx.jobs.add(name, work_directory=work)
    check_refused(lambda: pmx.jobs[name].run(timeout=60), ["invalid items", load_name],
                  "a run with an invalid load was not refused")
    check_refused(lambda: pmx.export_model(os.path.join(work, name + ".inp")), ["invalid items", load_name],
                  "an export with an invalid load was not refused")
    check(not os.path.isfile(os.path.join(work, name + ".inp")), "a refused run or export wrote an input file")
    loads.remove([load_name])
    pmx.jobs.remove([name])


def test_timeout():
    job = pmx.jobs[PREFIX + "Explicit"]
    status = job.run(timeout=1)
    check(status == "TimedOut", "the timed out run returned " + status)
    check(job.status == "TimedOut", "the analysis kept the status " + job.status)


def check_refused(function, words, message):
    try:
        function()
    except Exception as ex:
        text = str(ex)
        for word in words:
            check(word in text, message + " - the refusal must say '" + word + "': " + text)
        check("   at " not in text, message + " - a .NET exception leaked: " + text)
        return
    raise Exception("TEST FAILED: " + message)


def test_invalid_paths():
    # A path the file system cannot hold is refused naming the argument, before anything is recorded,
    # so the read-backs that build file names from it never meet it
    name = PREFIX + "BadPath"
    check_refused(lambda: pmx.jobs.add(name, work_directory="C:\\bad<|>directory"), ["'work_directory'"],
                  "an invalid work directory was accepted")
    check_refused(lambda: pmx.jobs.add(name, work_directory=""), ["'work_directory'"],
                  "an empty work directory was accepted")
    check_refused(lambda: pmx.jobs.add(name, executable="bad|solver.exe"), ["'executable'"],
                  "an invalid executable was accepted")
    check(not pmx.jobs.contains(name), "a refused analysis was recorded")
    explicit = pmx.jobs[PREFIX + "Explicit"]
    work_directory = explicit.work_directory
    check_refused(lambda: pmx.jobs.replace(PREFIX + "Explicit", work_directory="C:\\bad<|>directory"),
                  ["'work_directory'"], "a replace with an invalid work directory was accepted")
    check(explicit.work_directory == work_directory, "a refused replace changed the work directory")
    check(explicit.results_file_name.startswith(work_directory), "the results file name must still read back")


def test_environment_variable_refusals():
    check_refused(lambda: pmx.create.environment_variable("PMX_A", ""), ["'value'"], "an empty value was accepted")
    check_refused(lambda: pmx.create.environment_variable("PMX_A", "   "), ["'value'"], "a blank value was accepted")
    check_refused(lambda: pmx.create.environment_variable("   ", "one"), ["'name'"], "a blank name was accepted")
    # The settings dialog stores a name and a value as typed, so a journal of either replays
    check(pmx.create.environment_variable(" PMX_A ", "one").name == " PMX_A ",
          "a name is the user's text and must be kept as given")
    check(pmx.create.environment_variable("PMX_A", " padded value ").value == " padded value ",
          "a value is the user's text and must be kept as given")


def wait_for_solver_process(timeout_seconds):
    """True once the sleeping solver runs as a child of this process, False when it did not start in time."""
    import clr
    clr.AddReference("System.Management")
    from System.Diagnostics import Process
    from System.Management import ManagementObjectSearcher
    query = ("SELECT ProcessId FROM Win32_Process WHERE ParentProcessId=%d AND Name='%s'"
             % (Process.GetCurrentProcess().Id, os.path.basename(SLEEPING_SOLVER)))
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        searcher = ManagementObjectSearcher(query)
        try:
            if searcher.Get().Count > 0:
                return True
        finally:
            searcher.Dispose()
        time.sleep(0.1)
    return False


def test_kill_and_open_results_refusals(work):
    job = pmx.jobs[PREFIX + "Explicit"]
    # The sleeping solver writes no results, and a stale file of an earlier run must not answer for it
    results_file_name = job.results_file_name
    if os.path.isfile(results_file_name):
        os.remove(results_file_name)
    # The user interface refuses to kill an analysis that is not running, and the API must not mark it Killed
    status = job.status
    check(status != "Running", "the analysis must have stopped: " + status)
    check_refused(lambda: job.kill(), ["not running"], "killing an analysis that is not running was accepted")
    check(job.status == status, "a refused kill changed the status to " + job.status)
    check_refused(lambda: pmx.jobs.open_results(PREFIX + "Explicit"), ["does not exist"],
                  "opening a missing results file was accepted")
    # A running analysis is killed, and its results are not opened while it runs
    job.run(wait=False)
    deadline = time.time() + 30
    while job.status != "Running" and time.time() < deadline:
        time.sleep(0.1)
    check(job.status == "Running", "the analysis that was not waited for must run: " + job.status)
    check_refused(lambda: pmx.jobs.open_results(PREFIX + "Explicit"), ["still running"],
                  "opening the results of a running analysis was accepted")
    # Its completion reports to the analysis by name, so a running analysis is neither removed nor renamed,
    # and the model it reports to is neither replaced by a new one nor by an opened file
    running_name = "'" + PREFIX + "Explicit'"
    check_refused(lambda: pmx.jobs.remove(PREFIX + "Explicit"), [running_name, "running"],
                  "removing a running analysis was accepted")
    check_refused(lambda: pmx.jobs.rename(PREFIX + "Explicit", PREFIX + "Renamed"), [running_name, "running"],
                  "renaming a running analysis was accepted")
    check(pmx.jobs.contains(PREFIX + "Explicit") and not pmx.jobs.contains(PREFIX + "Renamed"),
          "a refused remove or rename changed the analyses")
    check_refused(lambda: pmx.new_model('ThreeD', 'MM_TON_S_C'), [running_name, "running"],
                  "a new model was accepted while an analysis runs")
    model_file_name = os.path.join(work, PREFIX + "Unopened.pmx")
    with open(model_file_name, "w") as model_file:
        model_file.write("")
    try:
        check_refused(lambda: pmx.open(model_file_name), [running_name, "running"],
                      "opening a model file was accepted while an analysis runs")
    finally:
        os.remove(model_file_name)
    check(pmx.jobs.contains(PREFIX + "Explicit"), "a refused new model or open replaced the model")
    # The status turns Running before the solver process is started, and a kill before it would leave the
    # solver running, so the kill waits for the process
    check(wait_for_solver_process(60), "the solver process of the running analysis did not start")
    job.kill()
    check(job.status == "Killed", "the killed analysis has the status " + job.status)


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    build_solvable_model()
    # The analyses write their input files into a folder of their own, which goes whether the test passes or not
    work = tempfile.mkdtemp(prefix=PREFIX)
    try:
        test_explicit_settings(work)
        test_replacement_and_inheritance()
        test_rename_by_case()
        test_argument_follows_name_and_solver()
        test_rejected_arguments()
        test_custom_work_directory(work)
        test_a_solver_that_crashes(work)
        test_invalid_items_refuse_the_run(work)
        test_timeout()
        test_invalid_paths()
        test_environment_variable_refusals()
        test_kill_and_open_results_refusals(work)
        clean()
    finally:
        # A failure may leave the sleeping solver running in the folder, which then cannot be removed
        for job in list(pmx.jobs):
            if job.name.startswith(PREFIX) and job.status == "Running":
                job.kill()
        shutil.rmtree(work, ignore_errors=True)
    print("TEST PASSED")


main()
