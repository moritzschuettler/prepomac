#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the analysis settings contract: an explicit value survives the application settings, a
#replacement keeps what it is not told about, an inherit switch restores the application default,
#a timeout reports the same status through the return value and the analysis itself, and an argument
#that is the solver default follows a new name or solver while an edited one is kept. It also checks
#the refusals: an invalid path, a blank environment variable, a kill of an analysis that is
#not running, opening the results of a running analysis or a missing results file, and removing or
#renaming a running analysis or replacing the model while it runs. Then it solves the cube with item names
#too long for CalculiX, which the input file shortens, and reads their history back under the item names.
#It solves a spinning plate with a frequency and a complex frequency step and checks that the complex modes are
#read back as increments numbered from 1 that hold their frequencies, with and without fields before them.
#Last, it replaces the cube by an axisymmetric ring and checks that a point spring, a surface spring and the
#reaction history are given for the full revolution, as the loads and the results file are.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Job_Correctness.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import math
import os
import re
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


def long_name(base, length):
    return (PREFIX + base + "_" + "LongDescriptiveName" * 5)[:length]


def test_long_names(work):
    # CalculiX keeps 80 characters of a name and loses one of 80 in its .dat output, while the names the input
    # file derives from an item name add a prefix or a suffix. Such a name is written shortened with a hash
    node_set = long_name("Bottom", 80)
    point = long_name("RP", 78)
    rigid_body = long_name("Rigid", 78)
    step = pmx.model.steps[PREFIX + "Step"]
    pmx.model.node_sets.add(node_set, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))
    pmx.model.reference_points.add(point, [0.5, 0.5, 1.0])
    pmx.model.constraints.add_rigid_body(rigid_body, point,
                                         pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [5, 6, 7, 8]))
    step.bcs.add_fixed(long_name("Fix", 79), pmx.create.region('NodeSetName', node_set))
    step.loads.add_concentrated_force(long_name("Force", 78), pmx.create.region('ReferencePointName', point),
                                      f3=-1000)
    step.loads.add_pressure(long_name("Pressure", 75),
                            pmx.create.selection_by_pick('femodel', 'surfaces', [1.0, 0.5, 0.5], 'Surface'), 1.0)
    step.history_outputs.add_nodal(long_name("Point_Output", 77), pmx.create.region('ReferencePointName', point),
                                   ["U", "RF"])
    step.history_outputs.add_nodal(long_name("Bottom_Output", 80), pmx.create.region('NodeSetName', node_set),
                                   ["RF"])
    name = PREFIX + "Long_Names"
    pmx.jobs.add(name, work_directory=work)
    try:
        status = pmx.jobs[name].run(timeout=300)
        check(status == "OK", "the analysis with long names did not succeed, got " + status + "\n" +
              pmx.jobs[name].get_output()[-3000:])
        with open(os.path.join(work, name + ".inp")) as input_file:
            lines = [line for line in input_file.read().splitlines() if not line.startswith("**")]
        names = [token for line in lines for token in re.split(r'[,=\s"]+', line)]
        too_long = [token for token in names if len(token) > 79]
        check(not too_long, "the input file holds names longer than CalculiX accepts: " + str(too_long))
        check(node_set[:60] in "".join(lines) and node_set not in "".join(lines),
              "the node set name was not written shortened")
        # The history output is read back under the names the model gives the node set and the reference point
        pmx.jobs.open_results(name)
        result = pmx.results.current
        history_names = [str(history_name) for history_name in result.history_outputs.get_names()]
        check(node_set.upper() in history_names and point.upper() in history_names,
              "the history of the long names was not read back under them: " + str(history_names))
        point_history = result.history_outputs[point.upper()]
        components = [str(component) for component in point_history.fields["DISPLACEMENTS"].components.get_names()]
        check("UR1" in components, "the rotation node of the reference point was not recognized: " + str(components))
        entries = result.get_history_output_entries(node_set.upper(), "FORCES", "RF3")
        reaction = sum(entries[str(entry)].get_data().values[-1] for entry in entries.get_names())
        check(abs(reaction - 1000) < 1e-3, "the reaction of the fixed nodes is not the applied force: " + str(reaction))
    finally:
        pmx.jobs.remove(name)
        for collection in [step.history_outputs, step.loads, step.bcs]:
            collection.remove([item for item in collection.get_names() if item.startswith(PREFIX)])
        pmx.model.constraints.remove([rigid_body])
        pmx.model.reference_points.remove([point])
        pmx.model.node_sets.remove([node_set])


def write_plate_mesh(file_name):
    # A plate of 4 by 4 by 1 hexahedra, 40 by 40 by 2; the nodes at x = 0 are 1, 6, ... 46 and node 50 is the far
    # top corner
    lines = ["*NODE"]
    for k in range(2):
        for j in range(5):
            for i in range(5):
                lines.append("%d, %.6f, %.6f, %.6f" % (25 * k + 5 * j + i + 1, 10.0 * i, 10.0 * j, 2.0 * k))
    lines.append("*ELEMENT, TYPE=C3D8, ELSET=" + PREFIX + "Plate")
    for j in range(4):
        for i in range(4):
            first = 5 * j + i + 1
            lines.append("%d, %d, %d, %d, %d, %d, %d, %d, %d" % (4 * j + i + 1, first, first + 1, first + 6, first + 5,
                                                                 first + 25, first + 26, first + 31, first + 30))
    with open(file_name, "w") as mesh_file:
        mesh_file.write("\n".join(lines) + "\n")


def solve_complex_frequency(name, work):
    pmx.jobs.add(name, work_directory=work)
    status = pmx.jobs[name].run(timeout=300)
    check(status == "OK", "the complex frequency analysis " + name + " did not succeed, got " + status + "\n" +
          pmx.jobs[name].get_output()[-3000:])
    pmx.jobs.open_results(name)
    result = pmx.results.current
    modes = {}
    for step_id in result.steps.get_ids():
        increments = result.steps[step_id].increments
        modes[step_id] = [(increment_id, increments[increment_id].step_time) for increment_id in increments.get_ids()]
    return result, modes


def test_complex_frequency_modes(work):
    # The modes of a complex frequency step are read back as those of a frequency step are: increments numbered
    # from 1 that hold their frequencies. CalculiX writes them in blocks of the analysis type of a frequency
    # sensitivity, whose increment 0 holds the normals of the design, and they were read back as one
    pmx.model.parts.remove([part.name for part in pmx.model.parts])
    pmx.model.element_sets.remove(list(pmx.model.element_sets.get_names()))
    mesh_file_name = os.path.join(work, PREFIX + "Plate.inp")
    write_plate_mesh(mesh_file_name)
    pmx.import_file(mesh_file_name)
    part = pmx.model.parts.first()
    pmx.model.materials.add(PREFIX + "Steel_Mass", [pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])
    pmx.model.sections.add_solid(PREFIX + "Plate_Section", PREFIX + "Steel_Mass",
                                 pmx.create.selection_by_ids('femodel', 'elements', 'parts', [part.id]))
    clamp = PREFIX + "Plate_Clamp"
    pmx.model.node_sets.add(clamp, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes',
                                                               [1, 6, 11, 16, 21, 26, 31, 36, 41, 46]))
    # CalculiX takes the speed of the Coriolis forces from the centrifugal load of the preceding static step
    steps = pmx.model.steps
    spin = steps.add_static(PREFIX + "Spin")
    spin.bcs.add_fixed(PREFIX + "Plate_Fixed", pmx.create.region('NodeSetName', clamp))
    spin.loads.add_centrifugal(PREFIX + "Speed", pmx.create.selection_by_ids('femodel', 'parts', 'parts', [part.id]),
                               axis_point=[0, 0, 0], axis_direction=[0, 0, 1], rotational_speed=1000)
    frequency = steps.add_frequency(PREFIX + "Frequency", number_of_frequencies=4, storage=True, perturbation=True,
                                    copy_bcs_and_loads=True)
    steps.add_complex_frequency(PREFIX + "Complex_Frequency", number_of_frequencies=3, perturbation=True,
                                copy_bcs_and_loads=True)
    try:
        result, modes = solve_complex_frequency(PREFIX + "Complex_Frequency", work)
        check(sorted(modes.keys()) == [1, 2, 3], "the steps of the complex frequency results are " + str(modes))
        check([mode[0] for mode in modes[2]] == [1, 2, 3, 4], "the frequency modes are not the increments 1 to 4: " +
              str(modes[2]))
        check([mode[0] for mode in modes[3]] == [1, 2, 3], "the complex frequency modes are not the increments " +
              "1 to 3: " + str(modes[3]))
        # The Coriolis forces of the slowly spinning plate barely move the frequencies
        for (increment_id, complex_frequency), (_, real_frequency) in zip(modes[3], modes[2]):
            check(abs(complex_frequency - real_frequency) < 1e-3 * real_frequency,
                  "the complex frequency mode " + str(increment_id) + " holds " + str(complex_frequency) +
                  " instead of its frequency " + str(real_frequency))
        check(abs(result.get_nodal_value(3, 1, 'DISP', 'U3', 50)) > 0, "the first complex frequency mode has no shape")
        # Without the fields of the steps before, the complex frequency fields are the first of the file
        for step in [spin, frequency]:
            step.field_outputs.remove(list(step.field_outputs.get_names()))
        result, modes_alone = solve_complex_frequency(PREFIX + "Complex_Frequency_Alone", work)
        check(list(modes_alone.keys()) == [3] and len(modes_alone[3]) == len(modes[3]) and
              all(alone[0] == mode[0] and abs(alone[1] - mode[1]) < 1e-6 * mode[1]
                  for alone, mode in zip(modes_alone[3], modes[3])),
              "the complex frequency modes written alone are read back as " + str(modes_alone) + " instead of " +
              str({3: modes[3]}))
        check(abs(result.get_nodal_value(3, 1, 'DISP', 'U3', 50)) > 0,
              "the first complex frequency mode written alone has no shape")
    finally:
        pmx.model.node_sets.remove([clamp])


def write_ring_mesh(file_name):
    # A ring of 4 by 2 axisymmetric elements from radius 10 to 20 and height 0 to 2; the bottom nodes are 1 to 5 and
    # the top nodes 11 to 15
    lines = ["*NODE"]
    for j in range(3):
        for i in range(5):
            lines.append("%d, %.6f, %.6f, 0" % (5 * j + i + 1, 10.0 + 2.5 * i, 1.0 * j))
    lines.append("*ELEMENT, TYPE=CAX4, ELSET=" + PREFIX + "Ring")
    for j in range(2):
        for i in range(4):
            first = 5 * j + i + 1
            lines.append("%d, %d, %d, %d, %d" % (4 * j + i + 1, first, first + 1, first + 6, first + 5))
    with open(file_name, "w") as mesh_file:
        mesh_file.write("\n".join(lines) + "\n")


def run_and_open(name, work):
    pmx.jobs.add(name, work_directory=work)
    status = pmx.jobs[name].run(timeout=300)
    check(status == "OK", "the axisymmetric analysis " + name + " did not succeed, got " + status + "\n" +
          pmx.jobs[name].get_output()[-3000:])
    pmx.jobs.open_results(name)
    result = pmx.results.current
    step_id = list(result.steps.get_ids())[-1]
    return result, step_id, list(result.steps[step_id].increments.get_ids())[-1]


def mean_top_displacement(result, step_id, increment_id):
    return sum(result.get_nodal_value(step_id, increment_id, 'DISP', 'U2', node_id)
               for node_id in range(11, 16)) / 5


def test_axisymmetric_per_revolution(work):
    # CalculiX solves an axisymmetric model as a 2 degree segment. The loads are given for the full revolution, and
    # so are the springs and the totals CalculiX prints for the segment once they are read back
    pmx.model.parts.remove([part.name for part in pmx.model.parts])
    pmx.model.element_sets.remove(list(pmx.model.element_sets.get_names()))
    pmx.model.properties.model_space = 'Axisymmetric'
    mesh_file_name = os.path.join(work, PREFIX + "Ring.inp")
    write_ring_mesh(mesh_file_name)
    pmx.import_file(mesh_file_name)
    part = pmx.model.parts.first()
    check(pmx.model.parts.get_element_types(part.name)['linear_quad'] == 'CAX4', "the ring is not axisymmetric")
    # A stiff ring moves as a whole, so its displacement is the load over the spring stiffness
    pmx.model.materials.add(PREFIX + "Stiff", pmx.create.elastic(1e9, 0))
    pmx.model.sections.add_solid(PREFIX + "Ring_Section", PREFIX + "Stiff",
                                 pmx.create.selection_by_ids('femodel', 'elements', 'parts', [part.id]))
    bottom = PREFIX + "Ring_Bottom"
    pmx.model.node_sets.add(bottom, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4, 5]))
    bottom_edge = PREFIX + "Ring_Bottom_Edge"
    pmx.model.surfaces.add(bottom_edge, pmx.create.selection_by_pick('femodel', 'surfaces', [15.0, 0.0, 0.0], 'Edge'))
    top = PREFIX + "Ring_Top"
    pmx.model.surfaces.add(top, pmx.create.selection_by_pick('femodel', 'surfaces', [15.0, 2.0, 0.0], 'Edge'))
    step = pmx.model.steps.add_static(PREFIX + "Ring_Step")
    revolved_area = math.pi * (20.0 ** 2 - 10.0 ** 2)
    # A point spring: five springs of 100 carry the force of 1000
    pmx.model.constraints.add_point_spring(PREFIX + "Point_Spring", pmx.create.region('NodeSetName', bottom), k2=100)
    step.loads.add_concentrated_force(PREFIX + "Ring_Force", pmx.create.selection_by_ids('femodel', 'nodes', 'nodes',
                                                                                        [13]), f2=-1000)
    displacement = mean_top_displacement(*run_and_open(PREFIX + "Axi_Point_Spring", work))
    check(abs(displacement + 2.0) < 1e-4, "the point spring of an axisymmetric model is not given for the full " +
          "revolution: the displacement is " + str(displacement) + " instead of -2")
    pmx.model.constraints.remove([PREFIX + "Point_Spring"])
    step.loads.remove([PREFIX + "Ring_Force"])
    # A surface spring takes its stiffness per area on the revolved surface, so a pressure of 1 on the same area
    # moves the ring by 1 over the stiffness. CalculiX applies the pressure to the chords of its segment, which
    # carry 0.02 % less than the revolved area
    pmx.model.constraints.add_surface_spring(PREFIX + "Surface_Spring", pmx.create.region('SurfaceName', bottom_edge),
                                             k2=10, stiffness_per_area=True)
    step.loads.add_pressure(PREFIX + "Ring_Pressure", pmx.create.region('SurfaceName', top), 1.0)
    displacement = mean_top_displacement(*run_and_open(PREFIX + "Axi_Surface_Spring", work))
    check(abs(displacement + 0.1) < 1e-3 * 0.1, "the surface spring of an axisymmetric model is not given per area " +
          "of the revolved surface: the displacement is " + str(displacement) + " instead of -0.1")
    pmx.model.constraints.remove([PREFIX + "Surface_Spring"])
    # The reactions the .dat file prints for the segment are read back for the full revolution, as the results file
    # gives them
    step.bcs.add_fixed(PREFIX + "Ring_Fixed", pmx.create.region('NodeSetName', bottom))
    step.history_outputs.add_nodal(PREFIX + "Ring_Reactions", pmx.create.region('NodeSetName', bottom), ["RF"])
    result, step_id, increment_id = run_and_open(PREFIX + "Axi_Reactions", work)
    entries = result.get_history_output_entries(bottom.upper(), "FORCES", "RF2")
    history = sum(entries[str(entry)].get_data().values[-1] for entry in entries.get_names())
    field = sum(result.get_nodal_value(step_id, increment_id, 'FORC', 'F2', node_id) for node_id in range(1, 6))
    check(abs(history - field) < 1e-5 * revolved_area and abs(history - revolved_area) < 1e-3 * revolved_area,
          "the reaction history of an axisymmetric model is not given for the full revolution: " + str(history) +
          " against the field " + str(field) + " and the load " + str(revolved_area))


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
        test_long_names(work)
        clean()
        test_complex_frequency_modes(work)
        clean()
        test_axisymmetric_per_revolution(work)
        clean()
    finally:
        # A failure may leave the sleeping solver running in the folder, which then cannot be removed
        for job in list(pmx.jobs):
            if job.name.startswith(PREFIX) and job.status == "Running":
                job.kill()
        shutil.rmtree(work, ignore_errors=True)
    print("TEST PASSED")


main()
