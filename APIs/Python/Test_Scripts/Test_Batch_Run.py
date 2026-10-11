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
#is on sys.path so a module next to it imports, and that -g No -x No without --rpc is refused.

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
    #
    print("TEST PASSED")


main()
