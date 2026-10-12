#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the unattended run contract. Run it from a command prompt:
#
#  PrePoMax.com --script Test_Batch_Run.py -g No -x Yes -w <work directory> --report run.json
#
#Expected process exit code 0 and a run.json with "exitStatus": "Ok".
#Pass the argument 'fail' to check the script error contract:
#
#  PrePoMax.com --script Test_Batch_Run.py --scriptArgs fail -g No -x Yes -w <work directory> --report run.json
#
#Expected process exit code 3 and a run.json with "exitStatus": "ScriptError".
#Expected report error contains "Deliberate failure used to check the script error exit code."
#
#Pass the argument 'sleep' with a time limit to check the time out contract:
#
#  PrePoMax.com --script Test_Batch_Run.py --scriptArgs sleep --timeout 30 -g No -x Yes -w <work directory> --report run.json
#
#Expected process exit code 4 and a run.json with "exitStatus": "TimedOut".
#Expected report error contains "exceeded the time limit of 30 s"
#
#Pass the argument 'cancel' to check the cancel contract. The script signals the cancel event the
#launcher signals on Ctrl+C, so the run ends the way a Ctrl+C ends it:
#
#  PrePoMax.com --script Test_Batch_Run.py --scriptArgs cancel -g No -x Yes -w <work directory> --report run.json
#
#Expected process exit code 7 and a run.json with "exitStatus": "Cancelled".
#Expected report error contains "cancelled"
#
#It also checks that what a script prints to stderr does not fail it, that the folder of the script
#is on sys.path so a module next to it imports, and that -g No -x No without --rpc is refused. A -r regeneration
#in which the mesh of one part fails must exit with 2, name the part on the console and in the report, while one
#without a failure exits with 0.

import os
import shutil
import subprocess
import sys
import tempfile
import time

# The argument the -g No -x No launch passes, which ends the script at once should it ever run
PROBE_ARGUMENT = "probe"
PROBE_EXIT_CODE = 9
# Longer than any time limit or cancel needs, so a run that is not ended for the script fails
SLEEP_SECONDS = 600


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_stderr_is_not_a_failure():
    # The run passes with this line printed, because only an exception fails a script
    sys.stderr.write("A line on stderr, which must not fail the run.\n")


def check_script_folder_imports():
    folder = os.path.normcase(os.path.dirname(os.path.abspath(__file__)))
    check(folder in [os.path.normcase(os.path.abspath(path)) for path in sys.path],
          "the folder of the script must be on sys.path while it runs")
    # Run_Tests.py lies next to this script and only defines things when it is imported
    import Run_Tests
    check(Run_Tests.STATUS_BY_CODE.get(7) == "Cancelled", "Run_Tests.py must know exit code 7")


def kill_process_tree(process):
    # PrePoMax.com starts PrePoMax.exe, so killing the launcher alone would leave the application running
    subprocess.call(["taskkill", "/T", "/F", "/PID", str(process.pid)],
                    stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def check_hidden_session_without_exit_is_refused():
    import System
    executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
    launcher = os.path.join(os.path.dirname(executable), "PrePoMax.com")
    if not os.path.isfile(launcher):
        print("Skipped the -g No -x No check: " + launcher + " does not exist.")
        return
    # Refused while the options are checked, before a window is created. Should the refusal ever go missing,
    # the probe argument ends the script at once, the time limit ends the application, and the process tree
    # is killed, so a session that would wait for a user who is not there cannot be left behind
    work_directory = tempfile.mkdtemp(prefix="PyTest_Batch_")
    try:
        process = subprocess.Popen([launcher, "--script", os.path.abspath(__file__), "--scriptArgs", PROBE_ARGUMENT,
                                    "-g", "No", "-x", "No", "-w", work_directory, "--timeout", "60"],
                                   stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        try:
            output = process.communicate(timeout=120)[0].decode("utf-8", "replace")
        except subprocess.TimeoutExpired:
            kill_process_tree(process)
            output = process.communicate()[0].decode("utf-8", "replace")
            raise Exception("TEST FAILED: -g No -x No without --rpc did not end within 120 s: " + output)
    finally:
        shutil.rmtree(work_directory, ignore_errors=True)
    check(process.returncode == 1, "-g No -x No without --rpc must exit with 1, not " +
          str(process.returncode) + ": " + output)
    check("-x No" in output, "the refusal must name -x No: " + output)


def sleep_past_the_time_limit():
    # The time limit given with --timeout ends the run while the script waits
    deadline = time.time() + SLEEP_SECONDS
    while time.time() < deadline:
        time.sleep(0.5)
    raise Exception("TEST FAILED: the time limit did not end the run")


def cancel_this_run():
    # PrePoMax.com signals this event on Ctrl+C, and the application ends the run as cancelled
    import System
    from System.Threading import EventWaitHandle
    process_id = System.Diagnostics.Process.GetCurrentProcess().Id
    EventWaitHandle.OpenExisting("PrePoMax.Cancel." + str(process_id)).Set()
    deadline = time.time() + SLEEP_SECONDS
    while time.time() < deadline:
        time.sleep(0.5)
    raise Exception("TEST FAILED: the cancel event did not end the run")


def check_saved_script_regenerates():
    # A script run without a file records no new model at the start of its history, and the .pmx it saves must
    # still regenerate rather than replay the script onto the model the file was saved with
    import System
    executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
    launcher = os.path.join(os.path.dirname(executable), "PrePoMax.com")
    if not os.path.isfile(launcher):
        print("Skipped the --save and -r check: " + launcher + " does not exist.")
        return
    work_directory = tempfile.mkdtemp(prefix="PyTest_Batch_")
    try:
        script = os.path.join(work_directory, "saved.py")
        with open(script, "w") as writer:
            writer.write("pmx.model.properties.unit_system = 'MM_TON_S_C'\n"
                         "pmx.model.properties.model_space = 'ThreeD'\n"
                         "pmx.model.materials.add('PyTest_M', pmx.create.elastic(1.0, 0.3))\n")
        saved = os.path.join(work_directory, "saved.pmx")
        for arguments in (["--script", script, "--save", saved], ["-r", saved]):
            completed = subprocess.run([launcher] + arguments + ["-g", "No", "-x", "Yes", "-w", work_directory,
                                                                 "--timeout", "120"],
                                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=300)
            output = completed.stdout.decode("utf-8", "replace")
            check(completed.returncode == 0, " ".join(arguments[:1]) + " must exit with 0, not " +
                  str(completed.returncode) + ": " + output)
    finally:
        shutil.rmtree(work_directory, ignore_errors=True)


def write_scaled_cube(file_name, scale):
    models = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models")
    lines = []
    with open(os.path.join(models, "cube.stl")) as reader:
        for line in reader:
            values = line.split()
            if values and values[0] == "vertex":
                line = "      vertex " + " ".join(repr(float(value) * scale) for value in values[1:]) + "\n"
            lines.append(line)
    with open(file_name, "w") as writer:
        writer.writelines(lines)


def check_failed_part_mesh_fails_regeneration():
    # A part whose mesh fails on regeneration fails the run, where the application with a window shows the message
    # and goes on. The cube swap.stl is replaced by a much larger one between the runs, so the local mesh size that
    # fitted its meshing parameters is refused on regeneration while the other part still meshes
    import System
    executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
    launcher = os.path.join(os.path.dirname(executable), "PrePoMax.com")
    if not os.path.isfile(launcher):
        print("Skipped the failed part mesh check: " + launcher + " does not exist.")
        return
    work_directory = tempfile.mkdtemp(prefix="PyTest_Batch_")
    try:
        keep = os.path.join(work_directory, "keep.stl")
        swap = os.path.join(work_directory, "swap.stl")
        write_scaled_cube(keep, 10.0)
        write_scaled_cube(swap, 10.0)
        script = os.path.join(work_directory, "mesh.py")
        with open(script, "w") as writer:
            writer.write("pmx.model.properties.unit_system = 'MM_TON_S_C'\n"
                         "pmx.model.properties.model_space = 'ThreeD'\n"
                         "pmx.import_file(%r)\n"
                         "pmx.import_file(%r)\n"
                         "names = list(pmx.model.geometry.parts.get_names())\n"
                         "pmx.model.geometry.parts.rename(names[1], 'PyTest_Swap')\n"
                         "part = pmx.model.geometry.parts['PyTest_Swap']\n"
                         "selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])\n"
                         "pmx.model.mesh.setup_items.add_local_mesh_size('PyTest_LMS', selection, element_size=0.5)\n"
                         "pmx.model.mesh.generate([names[0], 'PyTest_Swap'])\n" % (keep, swap))
        saved = os.path.join(work_directory, "mesh.pmx")
        report_file = os.path.join(work_directory, "run.json")
        for arguments, scale, exit_code in ((["--script", script, "--save", saved], None, 0),
                                            (["-r", saved], None, 0),
                                            (["-r", saved, "--report", report_file], 10000.0, 2)):
            if scale is not None:
                write_scaled_cube(swap, scale)
            completed = subprocess.run([launcher] + arguments + ["-g", "No", "-x", "Yes", "-w", work_directory,
                                                                 "--timeout", "300"],
                                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=600)
            output = completed.stdout.decode("utf-8", "replace")
            check(completed.returncode == exit_code, " ".join(arguments[:1]) + " must exit with " + str(exit_code) +
                  ", not " + str(completed.returncode) + ": " + output[-3000:])
        check("Process finished with errors." in output, "the failed regeneration must say it failed: " + output[-3000:])
        # The saved history replays the script, whose mesh.generate raises naming the part
        check("PythonAPIException: PyTest_Swap: The element size 0.5 of the local mesh size PyTest_LMS" in output,
              "the console must name the part whose mesh failed and why: " + output[-3000:])
        check("   at PrePoMax." not in output, "the failed mesh must be reported without a .NET stack trace: " +
              output[-3000:])
        import json
        with open(report_file) as reader:
            run = json.load(reader)
        check(run["exitStatus"] == "ModelError", "the report status must be ModelError, not " + str(run["exitStatus"]))
        check("PyTest_Swap: The element size 0.5" in (run["error"] or ""),
              "the report error must name the part whose mesh failed and why: " + str(run["error"]))
    finally:
        shutil.rmtree(work_directory, ignore_errors=True)


def main():
    check(len(sys.argv) >= 1, "sys.argv must contain the script file name")
    check(sys.argv[0].lower().endswith(".py"), "sys.argv[0] must be the script file name")
    arguments = sys.argv[1:]
    if PROBE_ARGUMENT in arguments:
        print("The script ran in a session that must have been refused.")
        sys.stdout.flush()
        os._exit(PROBE_EXIT_CODE)
    #
    check_stderr_is_not_a_failure()
    check_script_folder_imports()
    #
    print("Script file name: " + sys.argv[0])
    print("Arguments: " + str(arguments))
    print("Result names: " + str(pmx.results.get_names()))
    #
    if "fail" in arguments:
        raise Exception("Deliberate failure used to check the script error exit code.")
    if "sleep" in arguments:
        sleep_past_the_time_limit()
    if "cancel" in arguments:
        cancel_this_run()
    #
    check_hidden_session_without_exit_is_refused()
    check_saved_script_regenerates()
    check_failed_part_mesh_fails_regeneration()
    #
    print("TEST PASSED")


main()
