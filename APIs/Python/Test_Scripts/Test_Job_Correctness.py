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
#read back as increments numbered from 1 that hold their frequencies, with and without fields before them. On the plate
#and on a 2D ring it checks that the results move a reference point with the nodes it is connected to: a point a rigid
#body or a kinematic coupling moves by the displacements CalculiX prints for it, also turned by a large angle, and in
#the directions a kinematic coupling couples, and the point of a distributing coupling, offset and centred, by the
#weighted mean motion and rotation of the coupled nodes, all without a node or an element of their own in the result
#mesh. A point mass on a moved point solves a frequency step with one of its directions held, a boundary condition and
#a point mass on the point of a distributing coupling do not stop the run, and the printed history of the modes of a
#frequency step after a static step is read back one entry per mode at its frequency.
#Last, it replaces the cube by an axisymmetric ring and checks that a point spring, a surface spring, the
#reaction history, the reaction heat flux and body heating of both result files, the participation factor and the
#effective masses, a compression only support and a distributed mass are given for the full revolution, as the
#loads and the forces of the results file are, and that a point and a distributed mass of the ring, axisymmetric and
#in plane strain, solve in a frequency step at the hand computed frequency. The modes of the ring are normalized to the
#mass of the revolution, with 2D and 3D output, in the fields and the printed history, so the axial participation
#factor times the mode is 1, while the radial and rotational participation, which cancels over the revolution, is left
#for the segment. The buckling modes of the ring are normalized to the stiffness of the revolution, so the strain
#energy of a mode is one half, in the history and in the field, after a static step too. The section print of the ring
#is given for the full revolution for each element type, and left as CalculiX writes it when its elements are of types
#CalculiX writes for different angles; its moments, which cancel over the revolution, are always left as CalculiX
#writes them.
#With 3D output the ring is the 2 degree segment: its fields are those of the segment, its .dat history is given for the
#full revolution, the node sets and surfaces of the model are not copied onto it, and an axisymmetric cross section is
#refused and not the default. The results of the ring are opened as another model's, and a .dat file on its own, and are
#still given for the full revolution, since the results file names the model space; a file that does not name it is not
#scaled. Last, two rings in contact give the contact energy of a slave face as the sum of its contact elements for the
#full revolution and its contact stress as their average, a line and a volume integral of the 2D result are given for
#the full revolution unless the cross section None is asked for, and a surface integral of its edges is refused.
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
# Names no path may hold: the characters Windows refuses are valid elsewhere, the null character is valid nowhere
BAD_DIRECTORY = "C:\\bad<|>directory" if os.name == "nt" else "/tmp/bad\0directory"
BAD_EXECUTABLE = "bad|solver.exe" if os.name == "nt" else "bad\0solver"
# A shell that simply waits is a solver that never finishes, which is what a timeout needs
if os.name == "nt":
    SLEEPING_SOLVER = os.path.join(os.environ.get("WINDIR", "C:\\Windows"), "System32", "WindowsPowerShell",
                                   "v1.0", "powershell.exe")
    SLEEPING_ARGUMENT = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 30\""
else:
    SLEEPING_SOLVER = "/bin/sleep"
    SLEEPING_ARGUMENT = "30"


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
    if os.name == "nt":
        executable = SLEEPING_SOLVER
        argument = "-NoProfile -NonInteractive -Command \"Set-Content " + name + ".frd ('x' * 400); exit 3\""
    else:
        executable = "/bin/sh"
        argument = "-c \"printf '%0400d' 0 > " + name + ".frd; exit 3\""
    pmx.jobs.add(name, fem_solver="Calculix", executable=executable, argument=argument, work_directory=work)
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
    check_refused(lambda: pmx.jobs.add(name, work_directory=BAD_DIRECTORY), ["'work_directory'"],
                  "an invalid work directory was accepted")
    check_refused(lambda: pmx.jobs.add(name, work_directory=""), ["'work_directory'"],
                  "an empty work directory was accepted")
    check_refused(lambda: pmx.jobs.add(name, executable=BAD_EXECUTABLE), ["'executable'"],
                  "an invalid executable was accepted")
    check(not pmx.jobs.contains(name), "a refused analysis was recorded")
    explicit = pmx.jobs[PREFIX + "Explicit"]
    work_directory = explicit.work_directory
    check_refused(lambda: pmx.jobs.replace(PREFIX + "Explicit", work_directory=BAD_DIRECTORY),
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
    if os.name != "nt":
        # pgrep lists the children of this process by the name of the solver
        import subprocess
        deadline = time.time() + timeout_seconds
        while time.time() < deadline:
            found = subprocess.run(["pgrep", "-P", str(os.getpid()), "-x", os.path.basename(SLEEPING_SOLVER)],
                                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            if found.returncode == 0:
                return True
            time.sleep(0.1)
        return False
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


def displacement_scale(result, step_id, increment_id, node_ids):
    # The largest nodal displacement of the increment, which a computed point is compared against, since a mode leaves a
    # point that barely moves at the noise of the results file
    return max(math.sqrt(sum(result.get_nodal_value(step_id, increment_id, "DISP", component, node_id) ** 2
                             for component in ["U1", "U2", "U3"])) for node_id in node_ids)


def check_reference_point_displacements(result, point_name, coordinates, dat_file_name, node_ids, step_factors=None,
                                        tolerance=1e-5, free_directions=()):
    # The results compute the motion of the point from the nodes it is connected to, and it is the one CalculiX prints
    # for the node of the point, increment by increment and mode by mode, times the factor of the step. A direction a
    # kinematic coupling leaves free is not computed: the point stays undeformed in it and its value is refused
    with open(dat_file_name) as dat_file:
        printed = [[float(value) for value in values.split()] for values in
                   re.findall(r"displacements \(vx,vy,vz\) for set " + re.escape(point_name.upper()) +
                              r"_REF_\d+ and time[^\n]*\n\s*\n\s*\d+((?:\s+\S+){3})", dat_file.read())]
    point = result.mesh.reference_points[point_name]
    count = 0
    for step_id in result.steps.get_ids():
        factor = (step_factors or {}).get(step_id, 1.0)
        for increment_id in [i for i in result.steps[step_id].increments.get_ids() if i > 0]:
            check(count < len(printed), "the reference point " + point_name + " has more increments than the " +
                  str(len(printed)) + " printed displacements")
            printed[count] = [value * factor for value in printed[count]]
            allowed = tolerance * max(displacement_scale(result, step_id, increment_id, node_ids), 1e-12)
            deformed = list(point.get_deformed_coordinates(step_id, increment_id))
            for i in range(3):
                expected = 0.0 if i in free_directions else printed[count][i]
                check(abs(deformed[i] - coordinates[i] - expected) <= allowed,
                      "the reference point " + point_name + " is at " + str(deformed) + " in step " + str(step_id) +
                      ", increment " + str(increment_id) + " instead of the printed displacement " +
                      str(printed[count]) + " from " + str(coordinates))
            for i in free_directions:
                check_raises(lambda: point.get_nodal_value(step_id, increment_id, "DISP", "U" + str(i + 1)),
                             "the free direction U" + str(i + 1) + " of the reference point " + point_name +
                             " was not refused")
            if 1 not in free_directions:
                value = point.get_nodal_value(step_id, increment_id, "DISP", "U2")
                check(abs(value - printed[count][1]) <= allowed,
                      "the value U2 of the reference point " + point_name + " is " + str(value) + " instead of " +
                      str(printed[count][1]))
            count += 1
    check(count > 0 and count == len(printed), "the reference point " + point_name + " has " + str(count) +
          " increments with results and " + str(len(printed)) + " printed displacements")


def solve_three(matrix, rhs):
    def determinant(m):
        return (m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1]) - m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0]) +
                m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]))
    solution = []
    for k in range(3):
        replaced = [[rhs[i] if j == k else matrix[i][j] for j in range(3)] for i in range(3)]
        solution.append(determinant(replaced) / determinant(matrix))
    return solution


def follower_displacement(result, step_id, increment_id, weights, point, two_d):
    # The weighted mean displacement of the nodes plus their least squares small rotation about their weighted centre
    # times the lever arm of the point
    coordinates = dict((node_id, result.get_nodal_coordinates(node_id)) for node_id in weights)
    displacements = dict((node_id, [result.get_nodal_value(step_id, increment_id, "DISP", component, node_id)
                                    for component in ["U1", "U2", "U3"]]) for node_id in weights)
    center = [sum(weight * coordinates[node_id][i] for node_id, weight in weights.items()) for i in range(3)]
    mean = [sum(weight * displacements[node_id][i] for node_id, weight in weights.items()) for i in range(3)]
    arms = dict((node_id, [coordinates[node_id][i] - center[i] for i in range(3)]) for node_id in weights)
    relative = dict((node_id, [displacements[node_id][i] - mean[i] for i in range(3)]) for node_id in weights)
    if two_d:
        rotation = [0.0, 0.0, sum(weight * (arms[n][0] * relative[n][1] - arms[n][1] * relative[n][0])
                                  for n, weight in weights.items()) /
                    sum(weight * (arms[n][0] ** 2 + arms[n][1] ** 2) for n, weight in weights.items())]
    else:
        inertia = [[sum(weight * ((sum(a * a for a in arms[n]) if i == j else 0) - arms[n][i] * arms[n][j])
                        for n, weight in weights.items()) for j in range(3)] for i in range(3)]
        moment = [sum(weight * (arms[n][(i + 1) % 3] * relative[n][(i + 2) % 3] -
                                arms[n][(i + 2) % 3] * relative[n][(i + 1) % 3]) for n, weight in weights.items())
                  for i in range(3)]
        rotation = solve_three(inertia, moment)
    arm = [point[i] - center[i] for i in range(3)]
    return [mean[i] + rotation[(i + 1) % 3] * arm[(i + 2) % 3] - rotation[(i + 2) % 3] * arm[(i + 1) % 3]
            for i in range(3)]


def check_distributing_point_displacements(result, point_name, coordinates, weights, node_ids, two_d=False):
    # CalculiX does not move the point of a distributing coupling, which follows the mean of the coupled nodes,
    # weighted by their share of the surface, and their rotation times its lever arm, increment by increment and mode
    # by mode
    point = result.mesh.reference_points[point_name]
    count = 0
    for step_id in result.steps.get_ids():
        for increment_id in [i for i in result.steps[step_id].increments.get_ids() if i > 0]:
            allowed = 1e-5 * max(displacement_scale(result, step_id, increment_id, node_ids), 1e-12)
            expected = follower_displacement(result, step_id, increment_id, weights, coordinates, two_d)
            deformed = list(point.get_deformed_coordinates(step_id, increment_id))
            for i, component in enumerate(["U1", "U2", "U3"]):
                value = point.get_nodal_value(step_id, increment_id, "DISP", component)
                check(abs(value - expected[i]) <= allowed and
                      abs(deformed[i] - coordinates[i] - expected[i]) <= allowed,
                      "the reference point " + point_name + " has " + component + " " + str(value) + " and is at " +
                      str(deformed) + " in step " + str(step_id) + ", increment " + str(increment_id) +
                      " instead of following the coupled nodes by " + str(expected))
            count += 1
    check(count > 1, "the reference point " + point_name + " has results in only " + str(count) + " increments")


def find_point_springs(name, work):
    # A point mass on a point that a constraint moves goes on a node of its own, held by a spring of no stiffness
    with open(os.path.join(work, name + ".inp")) as input_file:
        text = input_file.read()
    return text, re.findall(r"\*Element, Type=SPRING2, Elset=Internal_Reference_Point_Mass\s+\d+, (\d+), (\d+)", text,
                            re.I)


def check_frequency_history_after_static(result, set_name):
    # CalculiX prints the history of every mode of a frequency step with the total time, which is 2 after a static
    # step, so the modes are told apart by their frequencies, one entry each, and none is lost
    set_names = [name for name in result.history_outputs.get_names() if str(name).upper() == set_name.upper()]
    check(len(set_names) == 1, "the history of " + set_name + " is missing: " +
          str(list(result.history_outputs.get_names())))
    entries = result.get_history_output_entries(set_names[0], "DISPLACEMENTS", "U2")
    times = list(entries[str(entries.get_names()[0])].get_data().time)
    step_ids = list(result.steps.get_ids())
    frequencies = [result.steps[step_ids[-1]].increments[increment_id].step_time
                   for increment_id in result.steps[step_ids[-1]].increments.get_ids() if increment_id > 0]
    check(len(frequencies) == 3 and len(times) == 1 + len(frequencies) and
          all(abs(a - b) <= 1e-5 * b for a, b in zip(times[1:], frequencies)),
          "the history of the modes of a frequency step after a static step is " + str(times) + " instead of the " +
          "static time and the frequencies " + str(frequencies))


def check_reference_point_mesh(result, number_of_nodes):
    # The node of a point is no node of the result mesh, and its spring of no length no element
    check(len(result.mesh.parts) == 1, "the result mesh has the parts " + str(list(result.mesh.parts.get_names())))
    nodes = sum(len(part.get_node_ids()) for part in result.mesh.parts)
    check(nodes == number_of_nodes, "the result mesh has " + str(nodes) + " nodes instead of " + str(number_of_nodes))


def test_reference_point_results(work):
    # The results move a reference point with the nodes it is connected to, computed from their displacements: by the
    # rigid motion of a rigid body or a kinematic coupling, which is the motion CalculiX prints for the point, and by
    # the weighted mean motion and rotation of the nodes of a distributing coupling, which CalculiX does not compute.
    # The input gives a point no node of its own, unless a constraint moves it and it carries a point mass, which goes
    # on a node held by a spring of no stiffness, so that a frequency step solves with one direction of the point held
    part = pmx.model.parts.first()
    pmx.model.materials.add(PREFIX + "Steel_Points", [pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])
    pmx.model.sections.add_solid(PREFIX + "Points_Section", PREFIX + "Steel_Points",
                                 pmx.create.selection_by_ids('femodel', 'elements', 'parts', [part.id]))
    clamp, tip, tip_surface = PREFIX + "Points_Clamp", PREFIX + "Points_Tip", PREFIX + "Points_Tip_Surface"
    pmx.model.node_sets.add(clamp, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', list(range(1, 50, 5))))
    pmx.model.node_sets.add(tip, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', list(range(5, 51, 5))))
    pmx.model.surfaces.add_from_node_set(tip_surface, tip)
    moved, free = PREFIX + "Moved_Point", PREFIX + "Free_Point"
    coordinates = [50.0, 20.0, 1.0]
    pmx.model.reference_points.add(moved, coordinates)
    pmx.model.reference_points.add(free, [0.0, 50.0, 1.0])
    constraint, mass, held = PREFIX + "Points_Constraint", PREFIX + "Points_Mass", PREFIX + "Points_Held"
    nodes = list(range(1, 51))
    try:
        pmx.model.constraints.add_rigid_body(constraint, moved, pmx.create.region('NodeSetName', tip))
        pmx.model.sections.add_point_mass(mass, pmx.create.region('ReferencePointName', moved), 1e-3)
        static = pmx.model.steps.add_static(PREFIX + "Points_Static")
        frequency = pmx.model.steps.add_frequency(PREFIX + "Points_Frequency", number_of_frequencies=3)
        for step in [static, frequency]:
            step.bcs.add_fixed(PREFIX + "Points_Fixed", pmx.create.region('NodeSetName', clamp))
            step.bcs.add_displacement_rotation(held, pmx.create.region('ReferencePointName', moved), u1=0)
            step.history_outputs.add_nodal(PREFIX + "Points_Output", pmx.create.region('ReferencePointName', moved),
                                           ["U"])
        static.loads.add_concentrated_force(PREFIX + "Points_Force", pmx.create.region('ReferencePointName', moved),
                                            f2=50, f3=-30)
        name = PREFIX + "Points_Rigid"
        result, _, _ = run_and_open(name, work)
        check_reference_point_mesh(result, 50)
        check_reference_point_displacements(result, moved, coordinates, os.path.join(work, name + ".dat"), nodes)
        check_frequency_history_after_static(result, moved)
        check_raises(lambda: result.mesh.reference_points[free].get_deformed_coordinates(1, 1),
                     "a reference point that nothing moves has a displacement")
        check_raises(lambda: result.mesh.reference_points[moved].get_nodal_value(1, 1, "STRESS", "S11"),
                     "a field other than the displacement of a reference point was not refused")
        text, springs = find_point_springs(name, work)
        masses = re.findall(r"\*Element, Type=Mass, Elset=" + PREFIX + r"Points_Mass\S*\s+\d+, (\d+)", text, re.I)
        check(len(springs) == 1 and springs[0][0] == springs[0][1] and masses == [springs[0][0]],
              "the moved point has no node of its own carrying its mass: springs " + str(springs) + ", masses " +
              str(masses))
        # A kinematic coupling moves its point too, which without a mass gets no node of its own
        pmx.model.sections.remove([mass])
        pmx.model.constraints.remove([constraint])
        for step in [static, frequency]:
            step.bcs.remove([held])
        pmx.model.constraints.add_kinematic_coupling(constraint, moved, pmx.create.region('SurfaceName', tip_surface))
        name = PREFIX + "Points_Coupled"
        result, _, _ = run_and_open(name, work)
        check_reference_point_mesh(result, 50)
        check_reference_point_displacements(result, moved, coordinates, os.path.join(work, name + ".dat"), nodes)
        check(find_point_springs(name, work)[1] == [], "a point without a mass got a node of its own")
        # A kinematic coupling that leaves a direction free moves the point in the others
        pmx.model.constraints.remove([constraint])
        pmx.model.constraints.add_kinematic_coupling(constraint, moved, pmx.create.region('SurfaceName', tip_surface),
                                                     u3=False)
        for step in [static, frequency]:
            step.bcs.add_displacement_rotation(held, pmx.create.region('ReferencePointName', moved), u3=0)
        name = PREFIX + "Points_Coupled_Partly"
        result, _, _ = run_and_open(name, work)
        check_reference_point_displacements(result, moved, coordinates, os.path.join(work, name + ".dat"), nodes,
                                            free_directions=(2,))
        check_raises(lambda: result.mesh.reference_points[moved].get_nodal_value(1, 1, "DISP", "ALL"),
                     "the magnitude of a point with a free direction was not refused")
        # The point of a distributing coupling follows the coupled nodes, weighted by their share of the surface: the
        # four rectangles of the tip give a corner node of the tip a sixteenth and the other nodes an eighth. Its
        # boundary condition and its mass, which do nothing in CalculiX, are warned about and do not stop the run,
        # and the mass stays on the point
        pmx.model.constraints.remove([constraint])
        pmx.model.constraints.add_distributing_coupling(constraint, moved,
                                                        pmx.create.region('SurfaceName', tip_surface))
        pmx.model.sections.add_point_mass(mass, pmx.create.region('ReferencePointName', moved), 1e-3)
        weights = dict((node_id, 1.0 / 16 if node_id in (5, 25, 30, 50) else 1.0 / 8) for node_id in range(5, 51, 5))
        for point_coordinates, label in [(coordinates, "Offset"), ([40.0, 20.0, 1.0], "Centred")]:
            pmx.model.reference_points.replace(moved, coordinates=point_coordinates)
            name = PREFIX + "Points_Distributed_" + label
            result, _, _ = run_and_open(name, work)
            check_reference_point_mesh(result, 50)
            check_distributing_point_displacements(result, moved, point_coordinates, weights, nodes)
            text, springs = find_point_springs(name, work)
            masses = re.findall(r"\*Element, Type=Mass, Elset=" + PREFIX + r"Points_Mass\S*\s+\d+, (\d+)", text, re.I)
            points = re.findall(r"\*Nset, Nset=" + PREFIX + r"Moved_Point_Ref_\d+\s+(\d+)", text, re.I)
            check(springs == [] and len(masses) == 1 and masses == points,
                  "the point of the distributing coupling does not keep its mass: springs " + str(springs) +
                  ", masses " + str(masses) + ", points " + str(points))
        # A rigid body that turns by a large angle moves its point 20 mm above the tip with the rotation itself
        pmx.model.sections.remove([mass])
        pmx.model.constraints.remove([constraint])
        pmx.model.steps.remove([static.name, frequency.name])
        lever = [40.0, 20.0, 21.0]
        pmx.model.reference_points.replace(moved, coordinates=lever)
        pmx.model.constraints.add_rigid_body(constraint, moved, pmx.create.region('NodeSetName', tip))
        turn = pmx.model.steps.add_static(PREFIX + "Points_Turn", nlgeom=True, incrementation_type='Direct',
                                          time_period=1.0, initial_time_increment=0.25, max_increments=100)
        turn.bcs.add_fixed(PREFIX + "Points_Fixed", pmx.create.region('NodeSetName', clamp))
        turn.bcs.add_displacement_rotation(PREFIX + "Points_Turned", pmx.create.region('ReferencePointName', moved),
                                           ur2=0.6)
        turn.history_outputs.add_nodal(PREFIX + "Points_Output", pmx.create.region('ReferencePointName', moved), ["U"])
        name = PREFIX + "Points_Turned"
        result, _, _ = run_and_open(name, work)
        check_reference_point_displacements(result, moved, lever, os.path.join(work, name + ".dat"), nodes,
                                            tolerance=1e-4)
    finally:
        pmx.model.constraints.remove([name for name in pmx.model.constraints.get_names() if name.startswith(PREFIX)])
        pmx.model.reference_points.remove([moved, free])
        pmx.model.surfaces.remove([tip_surface])
        pmx.model.node_sets.remove([clamp, tip])


def test_two_d_reference_point_results(work):
    # A 2D model moves a reference point in plane. The radial displacement CalculiX writes for the nodes of an
    # axisymmetric model is that of the 2 degree segment, smaller by the cosine of 1 degree than the one it prints for
    # the point, so the points of an axisymmetric model are compared to 3e-4. The point of a distributing coupling
    # follows the coupled edges weighted by their length, since CalculiX applies a 2D distributing coupling to other
    # nodes
    point, constraint, held = PREFIX + "Ring_Point", PREFIX + "Ring_Rigid", PREFIX + "Ring_Point_Held"
    coordinates = [15.0, 4.0, 0.0]
    add_ring_section(PREFIX + "Ring_Steel", [pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])
    pmx.model.reference_points.add(point, coordinates)
    static = pmx.model.steps.add_static(PREFIX + "Ring_Point_Static")
    frequency = pmx.model.steps.add_frequency(PREFIX + "Ring_Point_Frequency", number_of_frequencies=2)
    for step in [static, frequency]:
        step.bcs.add_fixed(PREFIX + "Ring_Point_Fixed", pmx.create.region('NodeSetName', PREFIX + "Ring_Bottom"))
        step.history_outputs.add_nodal(PREFIX + "Ring_Point_Output", pmx.create.region('ReferencePointName', point),
                                       ["U"])
    static.loads.add_concentrated_force(PREFIX + "Ring_Point_Force", pmx.create.region('ReferencePointName', point),
                                        f1=10, f2=-20)
    nodes = list(range(1, 16))
    try:
        for model_space in ["PlaneStrain", "Axisymmetric"]:
            pmx.model.properties.replace(model_space=model_space)
            tolerance = 3e-4 if model_space == "Axisymmetric" else 1e-5
            # The .dat file prints the modes of an axisymmetric model for the mass of the segment
            step_factors = {2: 1 / math.sqrt(180)} if model_space == "Axisymmetric" else None
            for kind in ["Rigid", "Coupled_Partly", "Coupled_Undetermined", "Distributed"]:
                if constraint in pmx.model.constraints.get_names():
                    pmx.model.constraints.remove([constraint])
                for step in [static, frequency]:
                    if held in step.bcs.get_names():
                        step.bcs.remove([held])
                if kind == "Rigid":
                    pmx.model.constraints.add_rigid_body(constraint, point,
                                                         pmx.create.selection_by_ids('femodel', 'nodes', 'nodes',
                                                                                     [11, 12, 13, 14, 15]))
                elif kind.startswith("Coupled"):
                    # The top edge moves in the direction U1 as the point does when it turns about the point, which lies
                    # above the edge, so coupling U1 alone does not determine the motion of the point
                    free = 0 if kind == "Coupled_Partly" else 1
                    pmx.model.constraints.add_kinematic_coupling(constraint, point,
                                                                 pmx.create.region('SurfaceName', PREFIX + "Ring_Top"),
                                                                 u1=free != 0, u2=free != 1)
                    for step in [static, frequency]:
                        step.bcs.add_displacement_rotation(held, pmx.create.region('ReferencePointName', point),
                                                           **{"u" + str(free + 1): 0})
                else:
                    pmx.model.constraints.add_distributing_coupling(constraint, point,
                                                                    pmx.create.region('SurfaceName',
                                                                                      PREFIX + "Ring_Top"))
                name = PREFIX + model_space + "_Point_" + kind
                result, frequency_step_id, _ = run_and_open(name, work)
                check_reference_point_mesh(result, 15)
                check(frequency_step_id == 2, "the frequency step of " + name + " is " + str(frequency_step_id))
                if kind == "Distributed":
                    weights = {11: 0.125, 12: 0.25, 13: 0.25, 14: 0.25, 15: 0.125}
                    check_distributing_point_displacements(result, point, coordinates, weights, nodes, two_d=True)
                elif kind == "Coupled_Undetermined":
                    deformed = list(result.mesh.reference_points[point].get_deformed_coordinates(1, 1))
                    check(deformed == coordinates, "the point whose motion the coupled nodes do not determine is at " +
                          str(deformed) + " instead of staying at " + str(coordinates))
                    for component in ["U1", "U2"]:
                        check_raises(lambda: result.mesh.reference_points[point].get_nodal_value(1, 1, "DISP",
                                                                                                 component),
                                     "the value " + component + " the coupled nodes do not determine was not refused")
                else:
                    check_reference_point_displacements(result, point, coordinates, os.path.join(work, name + ".dat"),
                                                        nodes, step_factors, tolerance,
                                                        (0,) if kind == "Coupled_Partly" else ())
    finally:
        pmx.model.properties.replace(model_space='Axisymmetric')
        if constraint in pmx.model.constraints.get_names():
            pmx.model.constraints.remove([constraint])
        pmx.model.reference_points.remove([point])


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


def history_total(result, set_name, field_name, component_name):
    entries = result.get_history_output_entries(set_name, field_name, component_name)
    return entries[str(entries.get_names()[0])].get_data().values[-1]


def add_ring_section(material_name, properties):
    # The ring of test_axisymmetric_per_revolution, which clean leaves without a material and a section
    pmx.model.materials.add(material_name, properties)
    pmx.model.sections.add_solid(PREFIX + "Ring_Section", material_name,
                                 pmx.create.selection_by_ids('femodel', 'elements', 'parts',
                                                             [pmx.model.parts.first().id]))


def test_axisymmetric_thermal_per_revolution(work):
    # CalculiX writes the reaction heat flux and the body heating for the 2 degree segment into both files, while
    # the heat fluxes are given for the full revolution, and so they are read back
    revolved_area = math.pi * (20.0 ** 2 - 10.0 ** 2)
    bottom = PREFIX + "Ring_Bottom"
    add_ring_section(PREFIX + "Conductor", [pmx.create.thermal_conductivity(50.0)])
    step = pmx.model.steps.add_heat_transfer(PREFIX + "Ring_Heat", steady_state=True)
    step.bcs.add_temperature(PREFIX + "Ring_Cold", pmx.create.region('NodeSetName', bottom), 0.0)
    step.loads.add_surface_flux(PREFIX + "Ring_Flux", pmx.create.region('SurfaceName', PREFIX + "Ring_Top"), 1.0)
    step.history_outputs.add_nodal(PREFIX + "Ring_Heat_Flow", pmx.create.region('NodeSetName', bottom), ["RFL"],
                                   totals_type="Yes")
    result, step_id, increment_id = run_and_open(PREFIX + "Axi_Heat", work)
    history = history_total(result, bottom.upper(), "TOTAL_HEAT_GENERATION", "RFL")
    field = sum(result.get_nodal_value(step_id, increment_id, 'RFL', 'RFL', node_id) for node_id in range(1, 6))
    check(abs(history - field) < 1e-5 * revolved_area and abs(history + revolved_area) < 1e-3 * revolved_area,
          "the reaction heat flux of an axisymmetric model is not given for the full revolution: the history is " +
          str(history) + " and the field " + str(field) + " against the flux " + str(-revolved_area))
    step.loads.remove([PREFIX + "Ring_Flux"])
    part_id = pmx.model.parts.first().id
    step.loads.add_body_flux(PREFIX + "Ring_Body_Flux", pmx.create.selection_by_ids('femodel', 'parts', 'parts',
                                                                                    [part_id]), 1.0)
    step.history_outputs.add_element(PREFIX + "Ring_Body_Heating",
                                     pmx.create.selection_by_ids('femodel', 'elements', 'parts', [part_id]),
                                     ["EBHE"], totals_type="Only")
    result, _, _ = run_and_open(PREFIX + "Axi_Body_Heat", work)
    heating = [history_total(result, name, "TOTAL_BODY_HEATING", "BHE") for name in result.history_outputs.get_names()
               if "TOTAL_BODY_HEATING" in list(result.history_outputs[name].fields.get_names())]
    check(len(heating) == 1 and abs(heating[0] - 2 * revolved_area) < 1e-3 * revolved_area,
          "the body heating of an axisymmetric model is not given for the full revolution: " + str(heating) +
          " instead of " + str(2 * revolved_area))


def bottom_node_ids(result):
    # The nodes of the ring at height 0: 1 to 5 in its 2D mesh, and the nodes of both faces of its segment in 3D
    data = result.mesh.nodes.get_data()
    return [node_id for node_id, coordinates in zip(list(data.ids), data.coordinates) if abs(coordinates[1]) < 1e-6]


def bottom_field_total(result, field_name, component_name):
    step_id = list(result.steps.get_ids())[-1]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    return sum(result.get_nodal_value(step_id, increment_id, field_name, component_name, node_id)
               for node_id in bottom_node_ids(result))


def history_entries_total(result, set_name, field_name, component_name):
    entries = result.get_history_output_entries(set_name, field_name, component_name)
    return sum(entries[str(entry)].get_data().values[-1] for entry in entries.get_names())


def check_three_d_mesh_of_the_ring(result, name):
    # The mesh is the 2 degree segment, which has nodes of its own, so the node sets and surfaces of the model are not
    # copied onto it, while its elements keep the numbers and the element sets of the model
    data = result.mesh.nodes.get_data()
    z_values = [abs(coordinates[2]) for coordinates in data.coordinates]
    check(max(z_values) > 0.3, "the " + name + " result with 3D output is not the expanded segment: " + str(max(z_values)))
    node_set_names = [str(set_name).upper() for set_name in result.mesh.node_sets.get_names()]
    surface_names = [str(surface_name).upper() for surface_name in result.mesh.surfaces.get_names()]
    element_set_names = [str(set_name).upper() for set_name in result.mesh.element_sets.get_names()]
    check((PREFIX + "Ring_Bottom").upper() not in node_set_names and (PREFIX + "Ring_Top").upper() not in surface_names,
          "the node sets and surfaces of the 2D model were copied onto the 3D " + name + " result, whose nodes they " +
          "do not hold: " + str(node_set_names) + " " + str(surface_names))
    check((PREFIX + "Ring").upper() in element_set_names,
          "the element sets of the 2D model were not copied onto the 3D " + name + " result: " + str(element_set_names))


def test_axisymmetric_three_d_output(work):
    # With 3D output CalculiX writes the 2 degree segment it computes, and does not scale its fields: the forces and the
    # reaction heat flux of the results file are those of the segment, while the .dat file is given for the full
    # revolution as with 2D output. An axisymmetric cross section, which integrates the 2D mesh, is refused on it
    revolved_area = math.pi * (20.0 ** 2 - 10.0 ** 2)
    bottom = PREFIX + "Ring_Bottom"
    add_ring_section(PREFIX + "Steel_Conductor", [pmx.create.elastic(210000, 0.3),
                                                  pmx.create.thermal_conductivity(50.0)])
    step = pmx.model.steps.add_static(PREFIX + "Ring_Step_3D")
    step.bcs.add_fixed(PREFIX + "Ring_Fixed", pmx.create.region('NodeSetName', bottom))
    step.loads.add_pressure(PREFIX + "Ring_Pressure", pmx.create.region('SurfaceName', PREFIX + "Ring_Top"), 1.0)
    step.field_outputs.replace_element('EF-Output-1', output='ThreeD')
    step.history_outputs.add_nodal(PREFIX + "Ring_Reactions", pmx.create.region('NodeSetName', bottom), ["RF"])
    result, _, _ = run_and_open(PREFIX + "Axi_Reactions_3D", work)
    with open(os.path.join(work, PREFIX + "Axi_Reactions_3D.frd")) as results_file:
        heading = [results_file.readline() for _ in range(3)]
    check(any(line.strip() == "1UModel space: Axisymmetric" for line in heading),
          "the results file does not name the model space in its heading: " + str(heading))
    check_three_d_mesh_of_the_ring(result, "static")
    field = bottom_field_total(result, 'FORC', 'F2')
    history = history_entries_total(result, bottom.upper(), "FORCES", "RF2")
    check(abs(field - revolved_area / 180) < 1e-3 * revolved_area / 180 and
          abs(history - revolved_area) < 1e-3 * revolved_area,
          "the reactions of the 3D output of an axisymmetric model are not the segment in the field and the full " +
          "revolution in the history: " + str(field) + " and " + str(history) + " against " + str(revolved_area))
    outputs = result.history_outputs
    volume = PREFIX + "Axi_Volume"
    check_raises(lambda: outputs.add(outputs.create_history_output_from_integration(
                     volume, "Volume", pmx.create.selection_by_ids('results', 'elements', 'parts', [1]),
                     cross_section="Axisymmetric")),
                 "an axisymmetric cross section was accepted on the 3D output of an axisymmetric model")
    check(volume not in list(outputs.get_names()), "a refused integration was added to the result")
    draft = outputs.create_history_output_from_integration(
        volume, "Volume", pmx.create.selection_by_ids('results', 'elements', 'parts', [1]))
    check(draft.cross_section == "None", "the 3D output of an axisymmetric model is integrated with the cross " +
          "section " + draft.cross_section + " when none is given, instead of None")
    pmx.model.steps.remove([PREFIX + "Ring_Step_3D"])
    # The reaction heat flux of the segment
    step = pmx.model.steps.add_heat_transfer(PREFIX + "Ring_Heat_3D", steady_state=True)
    step.bcs.add_temperature(PREFIX + "Ring_Cold", pmx.create.region('NodeSetName', bottom), 0.0)
    step.loads.add_surface_flux(PREFIX + "Ring_Flux", pmx.create.region('SurfaceName', PREFIX + "Ring_Top"), 1.0)
    step.field_outputs.replace_element('EF-Output-1', output='ThreeD')
    step.history_outputs.add_nodal(PREFIX + "Ring_Heat_Flow", pmx.create.region('NodeSetName', bottom), ["RFL"],
                                   totals_type="Yes")
    result, _, _ = run_and_open(PREFIX + "Axi_Heat_3D", work)
    check_three_d_mesh_of_the_ring(result, "thermal")
    field = bottom_field_total(result, 'RFL', 'RFL')
    history = history_total(result, bottom.upper(), "TOTAL_HEAT_GENERATION", "RFL")
    check(abs(field + revolved_area / 180) < 1e-3 * revolved_area / 180 and
          abs(history + revolved_area) < 1e-3 * revolved_area,
          "the reaction heat flux of the 3D output of an axisymmetric model is not the segment in the field and the " +
          "full revolution in the history: " + str(field) + " and " + str(history) + " against " + str(-revolved_area))


def copy_results_of_another_model(work, name, keep_model_space):
    # The results files of the analysis as another model would have written them: a model hash that is not the hash of
    # the open model, and, when keep_model_space is false, without the model space line older versions did not write
    copy_name = PREFIX + "Other_" + name + ("" if keep_model_space else "_Unnamed_Space")
    for extension in [".frd", ".dat"]:
        with open(os.path.join(work, PREFIX + name + extension)) as source:
            lines = source.read().splitlines(True)
        if extension == ".frd":
            lines = [re.sub(r"Hash: [^,]*,", "Hash: Other123,", line) for line in lines
                     if keep_model_space or "Model space:" not in line]
        with open(os.path.join(work, copy_name + extension), "w") as target:
            target.write("".join(lines))
    return os.path.join(work, copy_name)


def test_axisymmetric_results_of_another_model(work):
    # The results file names the model space, so the results of the axisymmetric ring are given for the full revolution
    # whichever model is open, and a .dat file opened on its own takes it from the .frd file beside it. A results file
    # without the line is axisymmetric only beside the model it was computed from
    revolved_area = math.pi * (20.0 ** 2 - 10.0 ** 2)
    bottom = (PREFIX + "Ring_Bottom").upper()
    pmx.results.clear()
    pmx.results.open(copy_results_of_another_model(work, "Axi_Reactions", True) + ".dat")
    history = history_entries_total(pmx.results.current, bottom, "FORCES", "RF2")
    check(abs(history - revolved_area) < 1e-3 * revolved_area, "the reaction history of an axisymmetric model opened " +
          "alone is not given for the full revolution: " + str(history) + " instead of " + str(revolved_area))
    pmx.results.open(copy_results_of_another_model(work, "Axi_Heat", True) + ".frd")
    result = pmx.results.current
    field = bottom_field_total(result, 'RFL', 'RFL')
    history = history_total(result, bottom, "TOTAL_HEAT_GENERATION", "RFL")
    check(abs(field + revolved_area) < 1e-3 * revolved_area and abs(history + revolved_area) < 1e-3 * revolved_area,
          "the reaction heat flux of an axisymmetric model opened beside another model is not given for the full " +
          "revolution: " + str(field) + " and " + str(history) + " instead of " + str(-revolved_area))
    pmx.results.open(copy_results_of_another_model(work, "Axi_Heat_3D", True) + ".frd")
    result = pmx.results.current
    field = bottom_field_total(result, 'RFL', 'RFL')
    history = history_total(result, bottom, "TOTAL_HEAT_GENERATION", "RFL")
    check(abs(field + revolved_area / 180) < 1e-3 * revolved_area / 180 and
          abs(history + revolved_area) < 1e-3 * revolved_area,
          "the reaction heat flux of the 3D output of an axisymmetric model opened beside another model is not the " +
          "segment in the field and the full revolution in the history: " + str(field) + " and " + str(history))
    pmx.results.open(copy_results_of_another_model(work, "Axi_Heat", False) + ".frd")
    result = pmx.results.current
    field = bottom_field_total(result, 'RFL', 'RFL')
    history = history_total(result, bottom, "TOTAL_HEAT_GENERATION", "RFL")
    check(abs(field + revolved_area / 180) < 1e-3 * revolved_area / 180 and
          abs(history + revolved_area / 180) < 1e-3 * revolved_area / 180,
          "a results file that does not name its model space was taken as axisymmetric beside another model: " +
          str(field) + " and " + str(history))


def write_two_ring_mesh(file_name):
    # Two rings of write_ring_mesh, the second one on top of the first from height 2 to 4
    lines = ["*NODE"]
    elements = []
    for ring in range(2):
        for j in range(3):
            for i in range(5):
                lines.append("%d, %.6f, %.6f, 0" % (100 * ring + 5 * j + i + 1, 10.0 + 2.5 * i, 2.0 * ring + 1.0 * j))
        elements.append("*ELEMENT, TYPE=CAX4, ELSET=" + PREFIX + "Ring_" + str(ring + 1))
        for j in range(2):
            for i in range(4):
                first = 100 * ring + 5 * j + i + 1
                elements.append("%d, %d, %d, %d, %d" % (100 * ring + 4 * j + i + 1, first, first + 1, first + 6,
                                                        first + 5))
    with open(file_name, "w") as mesh_file:
        mesh_file.write("\n".join(lines + elements) + "\n")


def read_dat_contact_energies(dat_file_name):
    # The energies CalculiX prints for the contact elements of the last time, summed by slave element and face
    with open(dat_file_name) as dat_file:
        block = dat_file.read().split("contact spring energy")[-1].split("\n\n\n")[0]
    energies = {}
    for line in block.splitlines()[1:]:
        values = line.split()
        if len(values) == 3:
            key = values[0] + "_" + values[1]
            energies[key] = energies.get(key, 0.0) + float(values[2])
    return energies


def test_axisymmetric_contact_energy_and_integration(work):
    # CalculiX prints a contact energy for every contact element of a slave face, and the energy of the face is their
    # sum, while its contact stress is their average. The pressure of 1 on the upper ring closes the contact of
    # stiffness 1e6 by 1e-6 over the revolved area, which stores half of the pressure times the closure times the area.
    # A line and a volume integral of the 2D result are given for the full revolution unless asked otherwise, as its
    # other values are, and a surface of the 2D result, which is made of element edges, is integrated as a line
    revolved_area = math.pi * (20.0 ** 2 - 10.0 ** 2)
    for collection in [pmx.model.surfaces, pmx.model.node_sets]:
        names = [name for name in collection.get_names() if name.startswith(PREFIX)]
        if names:
            collection.remove(names)
    pmx.model.parts.remove([part.name for part in pmx.model.parts])
    pmx.model.element_sets.remove(list(pmx.model.element_sets.get_names()))
    mesh_file_name = os.path.join(work, PREFIX + "Two_Rings.inp")
    write_two_ring_mesh(mesh_file_name)
    pmx.import_file(mesh_file_name)
    pmx.model.materials.add(PREFIX + "Steel_Contact", pmx.create.elastic(210000, 0.3))
    pmx.model.sections.add_solid(PREFIX + "Rings_Section", PREFIX + "Steel_Contact",
                                 pmx.create.selection_by_ids('femodel', 'elements', 'parts',
                                                             [part.id for part in pmx.model.parts]))
    surfaces = {}
    for name, height in [("Lower_Bottom", 0.0), ("Lower_Top", 1.99), ("Upper_Bottom", 2.01), ("Upper_Top", 4.0)]:
        surfaces[name] = PREFIX + name
        pmx.model.surfaces.add(surfaces[name], pmx.create.selection_by_pick('femodel', 'surfaces',
                                                                            [15.0, height, 0.0], 'Edge'))
    bottom = PREFIX + "Rings_Bottom"
    pmx.model.node_sets.add(bottom, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4, 5]))
    interaction, contact = PREFIX + "Rings_Linear", PREFIX + "Rings_Contact"
    pmx.model.surface_interactions.add(interaction, [pmx.create.surface_behavior('Linear', k=1e6)])
    pmx.model.contact_pairs.add(contact, interaction, pmx.create.region('SurfaceName', surfaces["Lower_Top"]),
                                pmx.create.region('SurfaceName', surfaces["Upper_Bottom"]), method='SurfaceToSurface')
    step = pmx.model.steps.add_static(PREFIX + "Rings_Step")
    step.bcs.add_fixed(PREFIX + "Rings_Fixed", pmx.create.region('NodeSetName', bottom))
    step.loads.add_pressure(PREFIX + "Rings_Pressure", pmx.create.region('SurfaceName', surfaces["Upper_Top"]), 1.0)
    step.history_outputs.add_contact(PREFIX + "Rings_Contact_Output", contact, ["CELS", "CSTR"])
    try:
        name = PREFIX + "Axi_Contact"
        result, _, _ = run_and_open(name, work)
        printed = read_dat_contact_energies(os.path.join(work, name + ".dat"))
        entries = result.get_history_output_entries("ALL_CONTACT_ELEMENTS", "CONTACT_SPRING_ENERGY", "ENERGY")
        read = {str(entry): entries[str(entry)].get_data().values[-1] for entry in entries.get_names()}
        check(len(printed) == 4 and sorted(read) == sorted(printed) and
              all(abs(read[key] - 180 * printed[key]) <= 1e-6 * read[key] for key in printed),
              "the contact energy of a slave face is not the sum of its contact elements for the full revolution: " +
              str(read) + " against the printed " + str(printed))
        energy = 0.5 * 1.0 * 1e-6 * revolved_area
        check(abs(sum(read.values()) - energy) < 0.1 * energy, "the contact energy of the rings is " +
              str(sum(read.values())) + " instead of about " + str(energy))
        entries = result.get_history_output_entries("ALL_CONTACT_ELEMENTS", "CONTACT_STRESS", "PRESS")
        pressures = [entries[str(entry)].get_data().values[-1] for entry in entries.get_names()]
        check(len(pressures) == 4 and all(abs(pressure - 1) < 0.02 for pressure in pressures),
              "the contact stress of a slave face is not the average of its contact elements: " + str(pressures))
        # The integrals of the 2D result
        outputs = result.history_outputs
        edges = pmx.create.selection_by_pick('results', 'edges', [15.0, 0.0, 0.0], 'Geometry', part_ids=[1])
        elements = pmx.create.selection_by_ids('results', 'elements', 'parts', [1])
        for integral_type, region, plain, revolved in [("Line", edges, 10.0, revolved_area),
                                                       ("Volume", elements, 20.0, 2 * revolved_area)]:
            draft = outputs.create_history_output_from_integration(PREFIX + integral_type, integral_type, region)
            check(draft.cross_section == "Axisymmetric", "a " + integral_type + " integral of an axisymmetric " +
                  "result takes the cross section " + draft.cross_section + " when none is given")
            outputs.add(draft)
            outputs.add_from_integration(PREFIX + integral_type + "_Plain", integral_type, region, cross_section="None")
            for output_name, expected in [(PREFIX + integral_type, revolved),
                                          (PREFIX + integral_type + "_Plain", plain)]:
                size = history_entries_total(pmx.results.current, output_name, "GEOMETRY", "SIZE")
                check(abs(size - expected) < 1e-3 * expected, "the " + integral_type + " integral " + output_name +
                      " of the axisymmetric result is " + str(size) + " instead of " + str(expected))
        names_before = list(outputs.get_names())
        for region in [pmx.create.region('SurfaceName', surfaces["Lower_Bottom"]),
                       pmx.create.selection_by_pick('results', 'surfaces', [15.0, 0.0, 0.0], 'Edge')]:
            check_raises(lambda: outputs.add_from_integration(PREFIX + "Surface", "Surface", region),
                         "a surface integral of the edges of a 2D result was accepted")
        check(list(outputs.get_names()) == names_before, "a refused integration was added to the result")
    finally:
        pmx.model.contact_pairs.remove([contact])
        pmx.model.surface_interactions.remove([interaction])


def run_ring_section_print(name, work):
    # The section print of the bottom edge carries the pressure of 1 on the top, both given for the full revolution
    step = pmx.model.steps.add_static(PREFIX + "Ring_Section_Step")
    step.bcs.add_displacement_rotation(PREFIX + "Ring_Axial", pmx.create.region('NodeSetName', PREFIX + "Ring_Bottom"),
                                       u2=0)
    step.loads.add_pressure(PREFIX + "Ring_Pressure", pmx.create.region('SurfaceName', PREFIX + "Ring_Top"), 1.0)
    step.history_outputs.add_section(PREFIX + "Ring_Section_Print",
                                     pmx.create.region('SurfaceName', PREFIX + "Ring_Bottom_Edge"),
                                     ["SOF", "SOM", "SOAREA"])
    result, _, _ = run_and_open(name, work)
    set_name = (PREFIX + "Ring_Bottom_Edge").upper()
    area = history_total(result, set_name, "SURFACE_AREA", "A")
    force = history_total(result, set_name, "TOTAL_SURFACE_FORCE", "FY")
    check_section_print_moments(result, set_name, os.path.join(work, name + ".dat"))
    pmx.model.steps.remove([PREFIX + "Ring_Section_Step"])
    return area, force


def check_section_print_moments(result, set_name, dat_file_name):
    # The moments of a section print cancel over the revolution, so they are left as CalculiX prints them
    with open(dat_file_name) as dat_file:
        text = dat_file.read()
    origin = [float(value) for value in re.findall(r"moment about the origin\(mx,my,mz\)\s*\n\s*((?:\S+\s+){5}\S+)",
                                                   text)[-1].split()[3:]]
    center = [float(value) for value in re.findall(r"moment about the center of gravity\(mx,my,mz\)\s*\n\s*" +
                                                   r"((?:\S+\s+){2}\S+)", text)[-1].split()]
    loads = [float(value) for value in re.findall(r"torque and bending moment \(size\)\s*\n\s*((?:\S+\s+){4}\S+)",
                                                  text)[-1].split()[3:]]
    read = [history_total(result, set_name, "MOMENT_ABOUT_ORIGIN", name) for name in ["MX", "MY", "MZ"]] + \
           [history_total(result, set_name, "MOMENT_ABOUT_CG", name) for name in ["MX", "MY", "MZ"]] + \
           [history_total(result, set_name, "SURFACE_LOADS", name) for name in ["TORQUE", "BENDING_MOMENT"]]
    printed = origin + center + loads
    check(abs(printed[2]) > 1e-3 and
          all(abs(a - b) <= 1e-5 * max(abs(b), 1e-12) for a, b in zip(read, printed)),
          "the moments of a section print of an axisymmetric model are not left as CalculiX prints them: " +
          str(read) + " instead of " + str(printed))


def write_mixed_ring_mesh(file_name):
    # The ring of write_ring_mesh with triangles from radius 10 to 15 and quadrilaterals from 15 to 20, in one part
    lines = ["*NODE"]
    for j in range(3):
        for i in range(5):
            lines.append("%d, %.6f, %.6f, 0" % (5 * j + i + 1, 10.0 + 2.5 * i, 1.0 * j))
    quadrilaterals = ["*ELEMENT, TYPE=CAX4, ELSET=" + PREFIX + "Ring"]
    triangles = ["*ELEMENT, TYPE=CAX3, ELSET=" + PREFIX + "Ring"]
    for j in range(2):
        for i in range(4):
            first = 5 * j + i + 1
            if i < 2:
                triangles.append("%d, %d, %d, %d" % (len(triangles) + 100, first, first + 1, first + 6))
                triangles.append("%d, %d, %d, %d" % (len(triangles) + 100, first, first + 6, first + 5))
            else:
                quadrilaterals.append("%d, %d, %d, %d, %d" % (len(quadrilaterals), first, first + 1, first + 6,
                                                              first + 5))
    with open(file_name, "w") as mesh_file:
        mesh_file.write("\n".join(lines + quadrilaterals + triangles) + "\n")


def test_axisymmetric_section_print_per_revolution(work):
    # CalculiX writes the section print of CAX4 and CAX8R elements for 1 degree and of the other axisymmetric types
    # for 2 degrees; it is read back for the full revolution, unless the section spans types of different angles
    revolved_area = math.pi * (20.0 ** 2 - 10.0 ** 2)
    add_ring_section(PREFIX + "Steel", pmx.create.elastic(210000, 0.3))
    for element_type in ["CAX4", "CAX4R"]:
        pmx.model.parts.set_element_type([pmx.model.parts.first().name], 'linear_quad', element_type)
        area, force = run_ring_section_print(PREFIX + "Axi_Section_" + element_type, work)
        check(abs(area - revolved_area) < 1e-3 * revolved_area and abs(force - revolved_area) < 1e-2 * revolved_area,
              "the section print of " + element_type + " elements of an axisymmetric model is not given for the " +
              "full revolution: the area is " + str(area) + " and the force " + str(force) + " instead of " +
              str(revolved_area))
    # A part of triangles and quadrilaterals
    pmx.model.sections.remove([PREFIX + "Ring_Section"])
    pmx.model.surfaces.remove([PREFIX + "Ring_Bottom_Edge", PREFIX + "Ring_Top"])
    pmx.model.node_sets.remove([PREFIX + "Ring_Bottom"])
    pmx.model.parts.remove([part.name for part in pmx.model.parts])
    pmx.model.element_sets.remove(list(pmx.model.element_sets.get_names()))
    mesh_file_name = os.path.join(work, PREFIX + "Mixed_Ring.inp")
    write_mixed_ring_mesh(mesh_file_name)
    pmx.import_file(mesh_file_name)
    part = pmx.model.parts.first()
    pmx.model.sections.add_solid(PREFIX + "Mixed_Ring_Section", PREFIX + "Steel",
                                 pmx.create.selection_by_ids('femodel', 'elements', 'parts', [part.id]))
    pmx.model.node_sets.add(PREFIX + "Ring_Bottom", pmx.create.selection_by_ids('femodel', 'nodes', 'nodes',
                                                                                [1, 2, 3, 4, 5]))
    pmx.model.surfaces.add(PREFIX + "Ring_Bottom_Edge",
                           pmx.create.selection_by_pick('femodel', 'surfaces', [15.0, 0.0, 0.0], 'Edge'))
    pmx.model.surfaces.add(PREFIX + "Ring_Top", pmx.create.selection_by_pick('femodel', 'surfaces', [15.0, 2.0, 0.0],
                                                                             'Edge'))
    check(pmx.model.parts.get_element_types(part.name) == {'linear_tria': 'CAX3', 'linear_quad': 'CAX4'},
          "the mixed ring has the element types " + str(pmx.model.parts.get_element_types(part.name)))
    # CAX3 for 2 degrees and CAX4 for 1 degree: no single factor is right, so the history is left as CalculiX gives it.
    # The ccx builds of macOS write CAX4 for 2 degrees as well, so there the history is given for the full revolution
    if os.name == "nt":
        segment_area = math.pi * (15.0 ** 2 - 10.0 ** 2) / 180 + math.pi * (20.0 ** 2 - 15.0 ** 2) / 360
    else:
        segment_area = revolved_area
    area, _ = run_ring_section_print(PREFIX + "Axi_Section_CAX3_CAX4", work)
    check(abs(area - segment_area) < 1e-3 * segment_area, "the section print of CAX3 and CAX4 elements of an " +
          "axisymmetric model is not left as CalculiX writes it: the area is " + str(area) + " instead of " +
          str(segment_area))
    pmx.model.parts.set_element_type([part.name], 'linear_quad', 'CAX4R')
    area, force = run_ring_section_print(PREFIX + "Axi_Section_CAX3_CAX4R", work)
    check(abs(area - revolved_area) < 1e-3 * revolved_area and abs(force - revolved_area) < 5e-2 * revolved_area,
          "the section print of CAX3 and CAX4R elements of an axisymmetric model is not given for the full " +
          "revolution: the area is " + str(area) + " and the force " + str(force) + " instead of " +
          str(revolved_area))


def read_dat_modal_rows(dat_file_name, title):
    # The rows of a modal block of the .dat file, the X, Y, Z components and rotations, by mode number, TOTAL for the
    # sum of the modes and ALL for the one row of the total effective mass
    with open(dat_file_name) as dat_file:
        lines = dat_file.read().split(title + "\n", 1)[1].splitlines()[3:]
    rows = {}
    for line in lines:
        values = line.split()
        if len(values) == 7:
            rows[values[0]] = [float(value) for value in values[1:]]
        elif len(values) == 6:
            rows["ALL"] = [float(value) for value in values]
        else:
            break
    return rows


def test_axisymmetric_frequency_per_revolution(work):
    # CalculiX normalizes the modes to the mass of its 2 degree segment. The free ring moves along the axis as a
    # whole in its first mode, which carries the whole mass of the revolved ring as its effective mass, and its
    # participation factor is the square root of that mass. The modes are read back normalized to that mass, so the
    # axial displacement of the first mode is one over the square root, and the participation factor times the mode
    # is 1, with 2D and with 3D output. The radial and rotational participation cancels over the revolution and is
    # left as CalculiX prints it for the segment
    density = 7.85e-9
    mass = density * math.pi * (20.0 ** 2 - 10.0 ** 2) * 2.0
    bottom = PREFIX + "Ring_Bottom"
    add_ring_section(PREFIX + "Steel_Mass", [pmx.create.elastic(210000, 0.3), pmx.create.density(density)])
    step = pmx.model.steps.add_frequency(PREFIX + "Ring_Frequency", number_of_frequencies=3)
    step.history_outputs.add_nodal(PREFIX + "Ring_Mode", pmx.create.region('NodeSetName', bottom), ["U"])
    for output in ["2D", "3D"]:
        if output == "3D":
            step.field_outputs.replace_element('EF-Output-1', output='ThreeD')
        name = PREFIX + "Axi_Frequency_" + output
        result, step_id, _ = run_and_open(name, work)
        set_name = [name for name in result.history_outputs.get_names() if str(name).startswith("STEP_")][0]
        values = {}
        for component in ["PARTICIPATION_FACTORS", "EFFECTIVE_MODAL_MASS", "TOTAL_EFFECTIVE_MASS"]:
            entries = result.get_history_output_entries(set_name, "FACTOR", component)
            values[component] = entries["Y_COMPONENT"].get_data().values[0]
        check(abs(abs(values["PARTICIPATION_FACTORS"]) - math.sqrt(mass)) < 1e-3 * math.sqrt(mass) and
              abs(values["EFFECTIVE_MODAL_MASS"] - mass) < 1e-3 * mass and
              abs(values["TOTAL_EFFECTIVE_MASS"] - mass) < 1e-3 * mass,
              "the participation factor, the effective modal mass and the total effective mass of an axisymmetric " +
              "model with " + output + " output are not given for the full revolution: " + str(values) +
              " for the mass " + str(mass))
        first_mode = list(result.steps[step_id].increments.get_ids())[0]
        field = [result.get_nodal_value(step_id, first_mode, 'DISP', 'U2', node_id)
                 for node_id in bottom_node_ids(result)]
        entries = result.get_history_output_entries(bottom.upper(), "DISPLACEMENTS", "U2")
        history = [entries[str(entry)].get_data().values[0] for entry in entries.get_names()]
        check(all(abs(abs(value) - 1 / math.sqrt(mass)) < 1e-3 / math.sqrt(mass) for value in field + history) and
              abs(values["PARTICIPATION_FACTORS"] * field[0] - 1) < 1e-3,
              "the first mode of an axisymmetric model with " + output + " output is not normalized to the mass of " +
              "the full revolution: the axial displacements " + str(field) + " in the field and " + str(history) +
              " in the history, and the participation factor " + str(values["PARTICIPATION_FACTORS"]) +
              ", instead of " + str(1 / math.sqrt(mass)))
        dat_file_name = os.path.join(work, name + ".dat")
        for component, title, total_component in [
                ("PARTICIPATION_FACTORS", "P A R T I C I P A T I O N   F A C T O R S", None),
                ("EFFECTIVE_MODAL_MASS", "E F F E C T I V E   M O D A L   M A S S", "TOTAL_EFFECTIVE_MODAL_MASS"),
                ("TOTAL_EFFECTIVE_MASS", "T O T A L   E F F E C T I V E   M A S S", None)]:
            rows = read_dat_modal_rows(dat_file_name, title)
            printed = [rows[mode] for mode in sorted(mode for mode in rows if mode != "TOTAL")]
            for index, direction in [(0, "X_COMPONENT"), (5, "Z_ROTATION")]:
                read = list(result.get_history_output_entries(set_name, "FACTOR", component)[direction]
                            .get_data().values)
                check(len(read) == len(printed) and
                      all(abs(a - b[index]) <= 1e-5 * max(abs(b[index]), 1e-30) for a, b in zip(read, printed)),
                      "the " + direction + " of the " + component + " of an axisymmetric model, which cancels over " +
                      "the revolution, is not left as CalculiX prints it: " + str(read) + " instead of " +
                      str([row[index] for row in printed]))
                if total_component is not None:
                    total = result.get_history_output_entries(set_name, "FACTOR", total_component)[direction]
                    total = total.get_data().values[0]
                    check(abs(total - rows["TOTAL"][index]) <= 1e-5 * max(abs(rows["TOTAL"][index]), 1e-30),
                          "the total " + direction + " of the " + component + " of an axisymmetric model is not left " +
                          "as CalculiX prints it: " + str(total) + " instead of " + str(rows["TOTAL"][index]))


def read_frd_displacements(frd_file_name, node_id):
    # The X displacement of the node in every DISP block of the results file, with the time of the block
    values = []
    with open(frd_file_name) as frd_file:
        lines = frd_file.read().splitlines()
    time = None
    for index, line in enumerate(lines):
        if line.strip().startswith("100CL"):
            time = float(line.split()[2])
        elif line.strip().startswith("-4  DISP"):
            for data_line in lines[index + 1:]:
                if data_line.startswith(" -3"):
                    break
                if data_line.startswith(" -1") and int(data_line[3:13]) == node_id:
                    values.append((time, float(data_line[13:25])))
    return values


def test_axisymmetric_buckling_per_revolution(work):
    # CalculiX normalizes the buckling modes to the stiffness of its 2 degree segment, so they are read back divided
    # by the square root of 180, normalized to the stiffness of the revolution: the strain energy of a mode over the
    # revolution is one half, in the printed history and in the energy field integrated over the revolution, which
    # the volume integral of an axisymmetric result is when no cross section is given. The base state of the step is
    # no mode and the buckling factors are left as they are, and a static step before the buckling one changes none of
    # it
    add_ring_section(PREFIX + "Steel_Buckling", pmx.create.elastic(210000, 0.3))
    part_id = pmx.model.parts.first().id
    static = pmx.model.steps.add_static(PREFIX + "Ring_Preload")
    step = pmx.model.steps.add_buckle(PREFIX + "Ring_Buckle", number_of_buckling_factors=2)
    for item in [static, step]:
        item.bcs.add_fixed(PREFIX + "Ring_Fixed", pmx.create.region('NodeSetName', PREFIX + "Ring_Bottom"))
        item.loads.add_pressure(PREFIX + "Ring_Pressure", pmx.create.region('SurfaceName', PREFIX + "Ring_Top"), 100.0)
    step.field_outputs.replace_element('EF-Output-1', variables=['S', 'E', 'ENER'])
    step.history_outputs.add_element(PREFIX + "Ring_Energy",
                                     pmx.create.selection_by_ids('femodel', 'elements', 'parts', [part_id]),
                                     ["ELSE"], totals_type="Only")
    name = PREFIX + "Axi_Buckle"
    result, step_id, _ = run_and_open(name, work)
    set_name = [set_name for set_name in result.history_outputs.get_names() if str(set_name).startswith("STEP_")][0]
    factors = result.get_history_output_entries(set_name, "BUCKLING", "BUCKLING_FACTOR")
    factors = list(factors[str(factors.get_names()[0])].get_data().values)
    increments = result.steps[step_id].increments
    times = [increments[increment_id].step_time for increment_id in increments.get_ids()]
    check(len(times) == 3 and times[0] == 0 and len(factors) == 2 and
          all(abs(a - b) < 1e-4 * b for a, b in zip(times[1:], factors)),
          "the buckling step reads back the increments " + str(times) + " for the buckling factors " + str(factors))
    # The displacements of the results file
    printed = read_frd_displacements(os.path.join(work, name + ".frd"), 13)[-3:]
    read = [result.get_nodal_value(step_id, increment_id, 'DISP', 'U1', 13) for increment_id in increments.get_ids()]
    expected = [printed[0][1]] + [value / math.sqrt(180) for _, value in printed[1:]]
    check(all(abs(a - b) <= 1e-5 * max(abs(b), 1e-12) for a, b in zip(read, expected)) and abs(expected[1]) > 0,
          "the buckling modes of an axisymmetric model are not normalized to the stiffness of the revolution: the " +
          "displacements " + str(read) + " instead of " + str(expected) + ", the base state as printed")
    # The strain energy of the modes in the history and in the field
    entries = [name for name in result.history_outputs.get_names() if
               "TOTAL_INTERNAL_ENERGY" in list(result.history_outputs[name].fields.get_names())]
    energy = result.get_history_output_entries(entries[0], "TOTAL_INTERNAL_ENERGY", "SE")
    energy = list(energy[str(energy.get_names()[0])].get_data().values)
    outputs = result.history_outputs
    outputs.add_from_integration(PREFIX + "Ring_Mode_Energy", "Volume",
                                 pmx.create.selection_by_ids('results', 'elements', 'parts', [part_id]),
                                 field_name="ENER", component_names=["ENER"], integral_result="Field")
    entries = pmx.results.current.get_history_output_entries(PREFIX + "Ring_Mode_Energy", "ENER", "ENER")
    integrated = {}
    for entry in entries.get_names():
        data = entries[str(entry)].get_data()
        for time, value in zip(data.time, data.values):
            integrated[time] = integrated.get(time, 0.0) + value
    integrated = [integrated[time] for time in sorted(integrated) if time in times]
    # The field is the energy density averaged at the nodes of the coarse ring, so it integrates to within 2 %
    check(len(energy) == 3 and len(integrated) == 3 and abs(energy[0] - integrated[0]) < 1e-2 * energy[0] and
          all(abs(value - 0.5) < 1e-3 for value in energy[1:]) and
          all(abs(value - 0.5) < 1e-2 for value in integrated[1:]),
          "the strain energy of the buckling modes of an axisymmetric model over the revolution is " + str(energy) +
          " in the history and " + str(integrated) + " in the field instead of one half, after the base state")


def calculix_has_pardiso():
    """Whether the CalculiX of the settings is linked with PARDISO, as the ccx of Windows is."""
    probe = PREFIX + "Pardiso_Probe"
    pmx.jobs.add(probe)
    executable = pmx.jobs[probe].executable
    pmx.jobs.remove(probe)
    # A ccx without PARDISO says so in a message it carries: the PARDISO library is not linked
    try:
        with open(executable, "rb") as program:
            return b"the PARDISO library is not linked" not in program.read()
    except (IOError, OSError):
        return True


def test_axisymmetric_gap_and_mass_per_revolution(work):
    # A compression only support acts on the 2 degree segment like a spring, so its stiffness is given for the full
    # revolution. The gap is closed by a nonlinear step, as a linear one takes half its stiffness at the closure
    if os.name != "nt" and not calculix_has_pardiso():
        # The open gaps leave the matrix singular for SPOOLES, the only direct solver of the macOS builds of ccx
        print("Skipped the compression only support: the ccx has no PARDISO solver, and SPOOLES finds the matrix of "
              "the open gaps singular.")
        return
    revolved_area = math.pi * (20.0 ** 2 - 10.0 ** 2)
    add_ring_section(PREFIX + "Stiff_Light", [pmx.create.elastic(1e9, 0), pmx.create.density(1e-15)])
    pmx.model.constraints.add_compression_only(PREFIX + "Ring_Support",
                                               pmx.create.region('SurfaceName', PREFIX + "Ring_Bottom_Edge"),
                                               spring_stiffness=1000)
    step = pmx.model.steps.add_static(PREFIX + "Ring_Support_Step", nlgeom=True)
    step.loads.add_pressure(PREFIX + "Ring_Pressure", pmx.create.region('SurfaceName', PREFIX + "Ring_Top"), 1.0)
    displacement = mean_top_displacement(*run_and_open(PREFIX + "Axi_Gap", work))
    check(abs(displacement + revolved_area / 1000) < 1e-3 * revolved_area / 1000, "the compression only support of " +
          "an axisymmetric model is not given for the full revolution: the displacement is " + str(displacement) +
          " instead of " + str(-revolved_area / 1000))
    pmx.model.constraints.remove([PREFIX + "Ring_Support"])
    pmx.model.steps.remove([PREFIX + "Ring_Support_Step"])
    # CalculiX itself scales a mass for the segment, as it does a load. A distributed mass of 0.05 on the ring, carried
    # by five springs of 100, moves it by 0.1 under a gravity of 1000, and it is spread by the revolved area
    pmx.model.constraints.add_point_spring(PREFIX + "Point_Spring",
                                           pmx.create.region('NodeSetName', PREFIX + "Ring_Bottom"), k2=100)
    pmx.model.sections.add_distributed_mass(PREFIX + "Ring_Mass",
                                            pmx.create.region('SurfaceName', PREFIX + "Ring_Top"), 0.05)
    step = pmx.model.steps.add_static(PREFIX + "Ring_Mass_Step")
    step.loads.add_gravity(PREFIX + "Ring_Gravity", pmx.create.region('MassSection', PREFIX + "Ring_Mass"), f2=-1000)
    try:
        displacement = mean_top_displacement(*run_and_open(PREFIX + "Axi_Mass", work))
        check(abs(displacement + 0.1) < 1e-4, "the distributed mass of an axisymmetric model is not given for the " +
              "full revolution: the displacement is " + str(displacement) + " instead of -0.1")
        with open(os.path.join(work, PREFIX + "Axi_Mass.inp")) as input_file:
            masses = {int(node_id): float(value) for node_id, value in
                      re.findall(r"\*Mass, Elset=" + PREFIX + r"Ring_Mass_NID_(\d+)\s+(\S+)", input_file.read(), re.I)}
        # The node at radius 10 takes the revolved area of the half edge next to it, 2 pi 2.5 (2 10 + 12.5) / 6 of
        # 2 pi 150, and the node at radius 20 2 pi 2.5 (2 20 + 17.5) / 6
        inner, outer = 0.05 * 2.5 * 32.5 / 6 / 150, 0.05 * 2.5 * 57.5 / 6 / 150
        check(abs(masses.get(11, 0) - inner) < 1e-6 and abs(masses.get(15, 0) - outer) < 1e-6,
              "the distributed mass of an axisymmetric model is not spread by the revolved area: " + str(masses))
    finally:
        pmx.model.constraints.remove([PREFIX + "Point_Spring"])


def test_two_d_mass_frequency(work):
    # CalculiX stops a frequency step on a mass whose node has a constrained degree of freedom and no other element,
    # as every node of a 2D element has, so the mass goes on a tied node of its own. A stiff light ring held radially
    # on five springs of 100 bounces with a point mass of 0.001 on each of its five top nodes, or a distributed mass of
    # 0.005 on its top, at the frequency of one spring and one mass, axisymmetric and in plane strain
    frequency = math.sqrt(100 / 1e-3) / (2 * math.pi)
    part_name = pmx.model.parts.first().name
    all_nodes = PREFIX + "Ring_All"
    add_ring_section(PREFIX + "Stiff_Light", [pmx.create.elastic(1e9, 0), pmx.create.density(1e-15)])
    pmx.model.node_sets.add(all_nodes, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', list(range(1, 16))))
    pmx.model.constraints.add_point_spring(PREFIX + "Point_Spring",
                                           pmx.create.region('NodeSetName', PREFIX + "Ring_Bottom"), k2=100)
    step = pmx.model.steps.add_frequency(PREFIX + "Ring_Mass_Frequency", number_of_frequencies=2)
    step.bcs.add_displacement_rotation(PREFIX + "Ring_Radial", pmx.create.region('NodeSetName', all_nodes), u1=0)
    masses = [("Point", lambda name: pmx.model.sections.add_point_mass(
                  name, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [11, 12, 13, 14, 15]), 1e-3)),
              ("Distributed", lambda name: pmx.model.sections.add_distributed_mass(
                  name, pmx.create.region('SurfaceName', PREFIX + "Ring_Top"), 5e-3))]
    try:
        for model_space, element_type in [("Axisymmetric", "CAX4"), ("PlaneStrain", "CPE4")]:
            pmx.model.properties.replace(model_space=model_space)
            check(pmx.model.parts.get_element_types(part_name)['linear_quad'] == element_type,
                  "the ring of the " + model_space + " model has the element types " +
                  str(pmx.model.parts.get_element_types(part_name)))
            for kind, add_mass in masses:
                add_mass(PREFIX + "Ring_Mass")
                result, step_id, _ = run_and_open(PREFIX + model_space + "_" + kind + "_Mass_Frequency", work)
                increments = result.steps[step_id].increments
                first = increments[list(increments.get_ids())[0]].step_time
                check(abs(first - frequency) < 1e-4 * frequency, "the " + kind.lower() + " mass of a " + model_space +
                      " model gives the first frequency " + str(first) + " instead of " + str(frequency))
                pmx.model.sections.remove([PREFIX + "Ring_Mass"])
    finally:
        pmx.model.properties.replace(model_space='Axisymmetric')
        pmx.model.constraints.remove([PREFIX + "Point_Spring"])
        pmx.model.node_sets.remove([all_nodes])


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
        test_reference_point_results(work)
        clean()
        test_axisymmetric_per_revolution(work)
        clean()
        test_axisymmetric_thermal_per_revolution(work)
        clean()
        test_axisymmetric_three_d_output(work)
        clean()
        test_axisymmetric_frequency_per_revolution(work)
        clean()
        test_axisymmetric_buckling_per_revolution(work)
        clean()
        test_axisymmetric_gap_and_mass_per_revolution(work)
        clean()
        test_two_d_mass_frequency(work)
        clean()
        test_two_d_reference_point_results(work)
        clean()
        test_axisymmetric_section_print_per_revolution(work)
        clean()
        test_axisymmetric_results_of_another_model(work)
        clean()
        test_axisymmetric_contact_energy_and_integration(work)
    finally:
        # A failure may leave the sleeping solver running in the folder, which then cannot be removed
        for job in list(pmx.jobs):
            if job.name.startswith(PREFIX) and job.status == "Running":
                job.kill()
        shutil.rmtree(work, ignore_errors=True)
    print("TEST PASSED")


main()
