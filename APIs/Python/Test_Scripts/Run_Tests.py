"""Runs the PrePoMax Python API test scripts through the batch mode and checks every run report.

This script runs OUTSIDE PrePoMax, with the system Python. Every test script describes how it is run
in its own header, so this runner reads the header rather than keeping a second list:

    #  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Loads.py -g No -x Yes -w <dir>
    #
    #Expected process exit code 0.

The command line and the expected exit code are both required; a header that declares neither is a
runner failure rather than a silently skipped test. The model is optional, because a test of the
batch contract itself opens no model.

The model files named by the headers live in Models, next to this script. A header that names
PyTest_Job.frd depends on Test_Analysis.py, which solves the cube and writes that file into the work
directory, so those tests are run last and a filter that selects one without the other is refused.

The offline checks run first: every Audit_*.py script, which checks the API sources and the overview
against each other, and the unit tests of this runner. They need no PrePoMax, take about a second,
and count as tests of the run; a filter selects them by name like any other test.

    python Run_Tests.py [--exe <PrePoMax.com>] [--work <directory>] [--filter <substring>]

The process exit code is 0 when every test met its expectation.
"""
import argparse
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

HERE = os.path.dirname(os.path.abspath(__file__))
MODELS = os.path.join(HERE, "Models")
# The solved model Test_Analysis.py leaves behind, which the result tests read
RESULT_FILE = "PyTest_Job.frd"
DEFAULT_EXE = os.path.abspath(os.path.join(HERE, "..", "..", "..", "PrePoMax", "bin", "x64", "Debug", "PrePoMax.com"))
COMMAND_LINE = re.compile(r"^#\s*PrePoMax\.com\s+(?P<arguments>.*)$")
EXIT_CODE = re.compile(r"^#\s*Expected process exit code\s+(?P<code>\d+)")
# The switches a test may ask for in its header; everything else the runner supplies itself
HEADER_SWITCHES = ("-f", "--rpc")
STATUS_BY_CODE = {0: "Ok", 1: "OptionError", 2: "ModelError", 3: "ScriptError", 4: "TimedOut",
                  5: "PromptRefused", 6: "LauncherError", 7: "Cancelled"}
# The application is given --timeout, so the parent waits a little longer before it kills the tree
SCRIPT_TIMEOUT = 600
PROCESS_TIMEOUT = SCRIPT_TIMEOUT + 60
OFFLINE_TIMEOUT = 120
OFFLINE_RUNNER_TESTS = "test_runner_offline.py"


class Test(object):
    def __init__(self, path, switches, expected_code):
        self.path, self.name = path, os.path.basename(path)
        self.switches = switches
        self.model = switches.get("-f")
        self.expected_code = expected_code
        self.needs_result = self.model == RESULT_FILE

    def __repr__(self):
        return self.name


def read_header(path):
    """Returns the switches a test script asks for and the exit code it expects.

    The switches are the ones in HEADER_SWITCHES: -f names the model and --rpc asks for the
    automation server. Everything else on the declared command line is supplied by the runner.
    """
    switches = {}
    expected_code = None
    declares_command = False
    with io.open(path, encoding="utf-8") as reader:
        for line in reader:
            if not line.startswith("#"):
                break
            command = COMMAND_LINE.match(line.rstrip())
            if command:
                declares_command = True
                arguments = command.group("arguments").split()
                for switch in HEADER_SWITCHES:
                    if switch in arguments and arguments.index(switch) + 1 < len(arguments):
                        switches[switch] = arguments[arguments.index(switch) + 1]
            code = EXIT_CODE.match(line.rstrip())
            if code and expected_code is None:
                expected_code = int(code.group("code"))
    if not declares_command or expected_code is None:
        raise ValueError("%s: the header must show a PrePoMax.com command line and an expected exit code"
                         % os.path.basename(path))
    return switches, expected_code


def collect(filter_text):
    tests = []
    for name in sorted(os.listdir(HERE)):
        if not name.startswith("Test_") or not name.endswith(".py"):
            continue
        if filter_text and filter_text.lower() not in name.lower():
            continue
        switches, expected_code = read_header(os.path.join(HERE, name))
        tests.append(Test(os.path.join(HERE, name), switches, expected_code))
    if any(test.needs_result for test in tests) and not any(test.name == "Test_Analysis.py" for test in tests):
        raise RuntimeError("result tests require Test_Analysis.py in the same run")
    # A test that reads the solved model runs after the one that writes it
    return sorted(tests, key=lambda test: (test.needs_result, test.name))


def collect_offline(filter_text):
    """The checks that run without PrePoMax: the audits, and the unit tests of this runner."""
    names = [name for name in sorted(os.listdir(HERE)) if name.startswith("Audit_") and name.endswith(".py")]
    names.append(OFFLINE_RUNNER_TESTS)
    return [name for name in names if not filter_text or filter_text.lower() in name.lower()]


def run_offline(name):
    started = time.time()
    try:
        completed = subprocess.run([sys.executable, os.path.join(HERE, name)], cwd=HERE, stdout=subprocess.PIPE,
                                   stderr=subprocess.STDOUT, timeout=OFFLINE_TIMEOUT)
        code, output = completed.returncode, completed.stdout.decode("utf-8", "replace")
    except subprocess.TimeoutExpired:
        code, output = None, "RUNNER TIMEOUT after %d s" % OFFLINE_TIMEOUT
    problems = [] if code == 0 else ["exit code %s, expected 0" % code]
    return problems, time.time() - started, output


def build_command(test, executable, work_directory, report_file):
    command = [executable]
    if test.model is not None:
        directory = work_directory if test.needs_result else MODELS
        command += ["-f", os.path.join(directory, test.model), "-u", "MM_TON_S_C"]
    command += ["--script", test.path, "-g", "No", "-x", "Yes", "-w", work_directory,
                "--report", report_file, "--timeout", str(SCRIPT_TIMEOUT)]
    if "--rpc" in test.switches:
        command += ["--rpc", test.switches["--rpc"]]
    return command


def run(test, executable, work_directory):
    report_file = os.path.join(work_directory, test.name + ".json")
    # A report left behind by an earlier run must not be read as this run's result
    if os.path.exists(report_file):
        os.remove(report_file)
    command = build_command(test, executable, work_directory, report_file)
    started = time.time()
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    try:
        output = process.communicate(timeout=PROCESS_TIMEOUT)[0].decode("utf-8", "replace")
    except subprocess.TimeoutExpired:
        if os.name == "nt":
            subprocess.call(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        else:
            process.kill()
        output = process.communicate()[0].decode("utf-8", "replace") + "\nRUNNER TIMEOUT: process tree terminated\n"
    problems = []
    if process.returncode != test.expected_code:
        problems.append("exit code %d, expected %d" % (process.returncode, test.expected_code))
    # The run report must agree with the exit code, which is the Layer 0 contract
    if os.path.isfile(report_file):
        with io.open(report_file, encoding="utf-8") as reader:
            report = json.load(reader)
        if report.get("exitCode") != test.expected_code:
            problems.append("report exitCode %s" % report.get("exitCode"))
        expected_status = STATUS_BY_CODE.get(test.expected_code)
        if expected_status and report.get("exitStatus") != expected_status:
            problems.append("report exitStatus %s, expected %s" % (report.get("exitStatus"), expected_status))
    else:
        problems.append("no run report was written")
    if test.expected_code == 0 and "TEST PASSED" not in output:
        problems.append("the script did not print TEST PASSED")
    if problems:
        with io.open(os.path.join(work_directory, test.name + ".output.txt"), "w", encoding="utf-8") as writer:
            writer.write(output)
    return problems, time.time() - started, output


def clear_result_fixture(work_directory):
    """Removes the solved model of an earlier run, so a stale fixture cannot hide a failure."""
    stale = os.path.join(work_directory, RESULT_FILE)
    if os.path.isfile(stale):
        os.remove(stale)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", default=DEFAULT_EXE)
    parser.add_argument("--work", default=None)
    parser.add_argument("--filter", default=None)
    args = parser.parse_args()
    try:
        tests = collect(args.filter)
    except (RuntimeError, ValueError) as error:
        print("TEST COLLECTION FAILED: %s" % error)
        return 1
    offline = collect_offline(args.filter)
    if not tests and not offline:
        print("TEST COLLECTION FAILED: no tests matched")
        return 1
    if tests and not os.path.isfile(args.exe):
        print("The launcher was not found: %s" % args.exe)
        return 1
    work = args.work or tempfile.mkdtemp(prefix="prepomax_api_tests_")
    temporary = args.work is None
    if not os.path.isdir(work):
        os.makedirs(work)
    clear_result_fixture(work)
    failed = []
    for name in offline:
        problems, elapsed, output = run_offline(name)
        print("%-4s %-38s %6.1f s  %s" % ("PASS" if not problems else "FAIL", name, elapsed, "; ".join(problems)))
        if problems:
            with io.open(os.path.join(work, name + ".output.txt"), "w", encoding="utf-8") as writer:
                writer.write(output)
            failed.append((name, output))
    for test in tests:
        problems, elapsed, output = run(test, args.exe, work)
        print("%-4s %-38s %6.1f s  %s" % ("PASS" if not problems else "FAIL", test.name, elapsed, "; ".join(problems)))
        if problems:
            failed.append((test, output))
    total = len(offline) + len(tests)
    print("%d of %d tests passed." % (total - len(failed), total))
    if failed and temporary:
        print("The work directory was kept for inspection: %s" % work)
    if temporary and not failed:
        shutil.rmtree(work, ignore_errors=True)
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
