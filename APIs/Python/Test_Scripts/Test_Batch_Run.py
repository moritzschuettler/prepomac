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

import sys


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def main():
    check(len(sys.argv) >= 1, "sys.argv must contain the script file name")
    check(sys.argv[0].lower().endswith(".py"), "sys.argv[0] must be the script file name")
    arguments = sys.argv[1:]
    #
    print("Script file name: " + sys.argv[0])
    print("Arguments: " + str(arguments))
    print("Result names: " + str(pmx.results.get_names()))
    #
    if "fail" in arguments:
        raise Exception("Deliberate failure used to check the script error exit code.")
    #
    print("TEST PASSED")


main()
