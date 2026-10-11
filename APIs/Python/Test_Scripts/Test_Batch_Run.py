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
#
#It also checks that what a script prints to stderr does not fail it, that the folder of the script
#is on sys.path so a module next to it imports, and that -g No -x No without --rpc is refused.

import os
import subprocess
import sys


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


def check_hidden_session_without_exit_is_refused():
    import System
    executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
    launcher = os.path.join(os.path.dirname(executable), "PrePoMax.com")
    if not os.path.isfile(launcher):
        print("Skipped the -g No -x No check: " + launcher + " does not exist.")
        return
    # Refused while the options are checked, before a window is created
    completed = subprocess.run([launcher, "--script", os.path.abspath(__file__), "-g", "No", "-x", "No",
                                "-w", os.path.dirname(os.path.abspath(__file__))],
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=120)
    output = completed.stdout.decode("utf-8", "replace")
    check(completed.returncode == 1, "-g No -x No without --rpc must exit with 1, not " +
          str(completed.returncode) + ": " + output)
    check("-x No" in output, "the refusal must name -x No: " + output)


def main():
    check(len(sys.argv) >= 1, "sys.argv must contain the script file name")
    check(sys.argv[0].lower().endswith(".py"), "sys.argv[0] must be the script file name")
    arguments = sys.argv[1:]
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
    #
    check_hidden_session_without_exit_is_refused()
    #
    print("TEST PASSED")


main()
