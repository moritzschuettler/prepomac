#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the analysis settings contract: an explicit value survives the application settings, a
#replacement keeps what it is not told about, an inherit switch restores the application default,
#and a timeout reports the same status through the return value and the analysis itself.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Job_Correctness.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import os
import tempfile

PREFIX = "PyTest_Job_"
# A shell that simply waits is a solver that never finishes, which is what a timeout needs
SLEEPING_SOLVER = os.path.join(os.environ.get("WINDIR", "C:\\Windows"), "System32", "WindowsPowerShell",
                               "v1.0", "powershell.exe")
SLEEPING_ARGUMENT = "-NoProfile -NonInteractive -Command \"Start-Sleep -Seconds 30\""


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
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
    pmx.model.steps.add_static(PREFIX + "Step", time_period=1.0)


def test_explicit_settings(work):
    variable = pmx.create.environment_variable("PMX_TEST_VALUE", "one")
    job = pmx.jobs.add(PREFIX + "Explicit", fem_solver="Calculix", executable=SLEEPING_SOLVER,
                       argument=SLEEPING_ARGUMENT, work_directory=work, number_of_cpus=2,
                       environment_variables=[variable])
    check(job.executable == SLEEPING_SOLVER, "the explicit executable was overwritten: " + job.executable)
    check(job.work_directory == work, "the explicit work directory was overwritten")
    check(job.number_of_cpus == 2, "the explicit CPU count was overwritten")
    check(job.environment_variables[0].Name == "PMX_TEST_VALUE", "the environment variable was not stored")
    # The readback is detached, so changing it cannot reach the stored analysis
    copy = job.environment_variables
    check(type(copy) is list, "the environment variables must read back as a plain list: " + str(type(copy)))
    copy[0].Value = "changed"
    check(job.environment_variables[0].Value == "one", "the environment readback was not detached")
    # The read-back goes back into the argument that sets it
    pmx.jobs.replace(PREFIX + "Explicit", environment_variables=job.environment_variables)
    variables = pmx.jobs[PREFIX + "Explicit"].environment_variables
    check(len(variables) == 1 and variables[0].Name == "PMX_TEST_VALUE" and variables[0].Value == "one",
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


def test_timeout():
    job = pmx.jobs[PREFIX + "Explicit"]
    status = job.run(timeout=1)
    check(status == "TimedOut", "the timed out run returned " + status)
    check(job.status == "TimedOut", "the analysis kept the status " + job.status)


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    build_solvable_model()
    test_explicit_settings(tempfile.gettempdir())
    test_replacement_and_inheritance()
    test_rename_by_case()
    test_rejected_arguments()
    test_custom_work_directory(tempfile.gettempdir())
    test_timeout()
    clean()
    print("TEST PASSED")


main()
