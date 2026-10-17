"""Runs the PrePoMax Python API test scripts through the batch mode and checks every run report.

This script runs OUTSIDE PrePoMax, with the system Python. Every test script describes how it is run
in its own header, so this runner reads the header rather than keeping a second list:

    #  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Loads.py -g No -x Yes -w <dir>
    #
    #Expected process exit code 0.

The command line and the expected exit code are both required; a header that declares neither is a
runner failure rather than a silently skipped test. The model is optional, because a test of the
batch contract itself opens no model.

A header may show more than one command line. Each one is a run of its own, and the exit code and the
report expectations that follow a command line, up to the next one, belong to it:

    #  PrePoMax.com --script Test_Batch_Run.py --scriptArgs fail -g No -x Yes -w <dir>
    #
    #Expected process exit code 3 and a run.json with "exitStatus": "ScriptError".
    #Expected report error contains "Deliberate failure"
    #Expected report prompts[-1].refused = true

An 'Expected report' line names a value of the run report by its keys and list indices, and either
compares it with a JSON value (=) or checks that it is a text that contains a JSON string (contains).

From a command line the runner takes the model (-f), the unit system (-u), the automation server
(--rpc), the script arguments (--scriptArgs) and the time limit (--timeout); it supplies everything
else itself. A run that names a model but no unit system is given MM_TON_S_C, and a run without a time
limit is given SCRIPT_TIMEOUT.

The model files named by the headers live in Models, next to this script. A header that names
PyTest_Job.frd depends on Test_Analysis.py, which solves the cube and writes that file into the work
directory, so those tests are run last, and a filter that selects one of them also runs Test_Analysis.py.

A run that expects exit code 0 must print TEST PASSED, and a run that expects any other code must not,
because the failure it expects has to end the script before it gets there.

The offline checks run first: every Audit_*.py script, which checks the API sources and the overview
against each other, and the unit tests of this runner. They need no PrePoMax, take about a second,
and count as tests of the run; a filter selects them by name like any other test.

A filter is a part of a script name, ignoring case; several filters select every script that matches any of them,
so the groups a change can reach run together, e.g. --filter Test_Job Test_Result Audit_.

    python Run_Tests.py [--exe <PrePoMax.com>] [--work <directory>] [--filter <substring> ...]

The process exit code is 0 when every test met its expectation.
"""
import argparse
import io
import json
import os
import re
import shlex
import shutil
import subprocess
import sys
import tempfile
import time

# A console program started from a process without a visible console opens a window of its own
NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)

HERE = os.path.dirname(os.path.abspath(__file__))
MODELS = os.path.join(HERE, "Models")
# The solved model Test_Analysis.py leaves behind, which the result tests read
RESULT_FILE = "PyTest_Job.frd"
DEFAULT_EXE = os.path.abspath(os.path.join(HERE, "..", "..", "..", "PrePoMax", "bin", "x64", "Debug", "PrePoMax.com"))
DEFAULT_UNIT_SYSTEM = "MM_TON_S_C"
COMMAND_LINE = re.compile(r"^#\s*PrePoMax\.com\s+(?P<arguments>.*)$")
EXIT_CODE = re.compile(r"^#\s*Expected process exit code\s+(?P<code>\d+)")
REPORT_CHECK = re.compile(r"^#\s*Expected report\s+(?P<path>\S+)\s+(?P<operator>=|contains)\s+(?P<value>.+?)\s*$")
# The switches a test may ask for in its header; everything else the runner supplies itself
HEADER_SWITCHES = ("-f", "-u", "--rpc", "--scriptArgs", "--timeout")
STATUS_BY_CODE = {0: "Ok", 1: "OptionError", 2: "ModelError", 3: "ScriptError", 4: "TimedOut",
                  5: "PromptRefused", 6: "LauncherError", 7: "Cancelled"}
# The application is given --timeout, so the parent waits a little longer before it kills the tree
SCRIPT_TIMEOUT = 600
PROCESS_TIMEOUT_MARGIN = 60
PROCESS_TIMEOUT = SCRIPT_TIMEOUT + PROCESS_TIMEOUT_MARGIN
OFFLINE_TIMEOUT = 120
OFFLINE_RUNNER_TESTS = "test_runner_offline.py"
RESULT_WRITER = "Test_Analysis.py"


class Test(object):
    """One run of a test script: the command line of its header and what that run is expected to do."""

    def __init__(self, path, switches, expected_code, report_checks=None, index=1, count=1):
        self.path, self.name = path, os.path.basename(path)
        self.switches = switches
        self.model = switches.get("-f")
        self.expected_code = expected_code
        self.report_checks = list(report_checks or [])
        self.index = index
        # A script with one run keeps its plain name, so its output and report files keep theirs too
        self.label = self.name if count == 1 else "%s [%d]" % (self.name, index)
        self.file_stem = self.name if count == 1 else "%s.%d" % (self.name, index)
        self.needs_result = self.model == RESULT_FILE
        self.timeout = int(switches["--timeout"]) if "--timeout" in switches else SCRIPT_TIMEOUT

    def __repr__(self):
        return self.label


def split_arguments(text):
    """The arguments of a header command line, read as a shell reads them so a quoted value stays whole."""
    try:
        return shlex.split(text, posix=True)
    except ValueError:
        return text.split()


def read_header(path):
    """Returns the runs a test script declares, each as (switches, expected exit code, report checks).

    The switches are the ones in HEADER_SWITCHES. A report check is (path, operator, value): the path of
    a value in the run report, '=' or 'contains', and the JSON value it is compared with.
    """
    runs = []
    with io.open(path, encoding="utf-8") as reader:
        for line in reader:
            if not line.startswith("#"):
                break
            line = line.rstrip()
            command = COMMAND_LINE.match(line)
            if command:
                arguments = split_arguments(command.group("arguments"))
                switches = {}
                for switch in HEADER_SWITCHES:
                    if switch in arguments and arguments.index(switch) + 1 < len(arguments):
                        switches[switch] = arguments[arguments.index(switch) + 1]
                if "--timeout" in switches and not re.match(r"^\d+$", switches["--timeout"]):
                    raise ValueError("%s: the --timeout of a header must be a whole number of seconds, not %s"
                                     % (os.path.basename(path), switches["--timeout"]))
                runs.append([switches, None, []])
                continue
            code = EXIT_CODE.match(line)
            if code and runs and runs[-1][1] is None:
                runs[-1][1] = int(code.group("code"))
                continue
            report_check = REPORT_CHECK.match(line)
            if report_check:
                if not runs:
                    raise ValueError("%s: a report expectation must follow the command line it belongs to"
                                     % os.path.basename(path))
                try:
                    value = json.loads(report_check.group("value"))
                except ValueError:
                    raise ValueError("%s: the expected report value %s is not JSON"
                                     % (os.path.basename(path), report_check.group("value")))
                if report_check.group("operator") == "contains" and not isinstance(value, str):
                    raise ValueError("%s: 'contains' needs a JSON string, not %s"
                                     % (os.path.basename(path), report_check.group("value")))
                runs[-1][2].append((report_check.group("path"), report_check.group("operator"), value))
    if not runs or any(expected_code is None for _, expected_code, _ in runs):
        raise ValueError("%s: the header must show a PrePoMax.com command line and an expected exit code for each"
                         % os.path.basename(path))
    return [(switches, expected_code, checks) for switches, expected_code, checks in runs]


def read_report_value(report, path):
    """The value a report path names - error, prompts[-1].text - or raises KeyError naming the path."""
    value = report
    for token in re.findall(r"[^.\[\]]+|\[-?\d+\]", path):
        try:
            if token.startswith("["):
                value = value[int(token[1:-1])]
            else:
                value = value[token]
        except (KeyError, IndexError, TypeError):
            raise KeyError("the report has no %s" % path)
    return value


def report_problems(report, report_checks):
    problems = []
    for path, operator, expected in report_checks:
        try:
            value = read_report_value(report, path)
        except KeyError as error:
            problems.append(str(error.args[0]))
            continue
        if operator == "=" and value != expected:
            problems.append("report %s is %s, expected %s" % (path, json.dumps(value), json.dumps(expected)))
        elif operator == "contains" and (not isinstance(value, str) or expected not in value):
            problems.append("report %s is %s, which does not contain %s"
                            % (path, json.dumps(value)[0:300], json.dumps(expected)))
    return problems


def matches(name, filter_texts):
    if not filter_texts:
        return True
    if isinstance(filter_texts, str):
        filter_texts = [filter_texts]
    return any(text.lower() in name.lower() for text in filter_texts)


def read_tests(name):
    runs = read_header(os.path.join(HERE, name))
    return [Test(os.path.join(HERE, name), switches, expected_code, report_checks, index, len(runs))
            for index, (switches, expected_code, report_checks) in enumerate(runs, 1)]


def collect(filter_texts):
    tests = []
    for name in sorted(os.listdir(HERE)):
        if not name.startswith("Test_") or not name.endswith(".py"):
            continue
        if not matches(name, filter_texts):
            continue
        tests.extend(read_tests(name))
    # The result tests read the model Test_Analysis.py solves, so selecting one selects it too
    if any(test.needs_result for test in tests) and not any(test.name == RESULT_WRITER for test in tests):
        tests.extend(read_tests(RESULT_WRITER))
    # A test that reads the solved model runs after the one that writes it
    return sorted(tests, key=lambda test: (test.needs_result, test.name, test.index))


def collect_offline(filter_texts):
    """The checks that run without PrePoMax: the audits, and the unit tests of this runner."""
    names = [name for name in sorted(os.listdir(HERE)) if name.startswith("Audit_") and name.endswith(".py")]
    names.append(OFFLINE_RUNNER_TESTS)
    return [name for name in names if matches(name, filter_texts)]


def run_offline(name):
    started = time.time()
    try:
        completed = subprocess.run([sys.executable, os.path.join(HERE, name)], cwd=HERE, stdout=subprocess.PIPE,
                                   stderr=subprocess.STDOUT, timeout=OFFLINE_TIMEOUT, creationflags=NO_WINDOW)
        code, output = completed.returncode, completed.stdout.decode("utf-8", "replace")
    except subprocess.TimeoutExpired:
        code, output = None, "RUNNER TIMEOUT after %d s" % OFFLINE_TIMEOUT
    problems = [] if code == 0 else ["exit code %s, expected 0" % code]
    return problems, time.time() - started, output


def build_command(test, executable, work_directory, report_file):
    command = [executable]
    if test.model is not None:
        directory = work_directory if test.needs_result else MODELS
        command += ["-f", os.path.join(directory, test.model)]
    unit_system = test.switches.get("-u", DEFAULT_UNIT_SYSTEM if test.model is not None else None)
    if unit_system is not None:
        command += ["-u", unit_system]
    command += ["--script", test.path]
    if "--scriptArgs" in test.switches:
        command += ["--scriptArgs", test.switches["--scriptArgs"]]
    command += ["-g", "No", "-x", "Yes", "-w", work_directory, "--report", report_file, "--timeout", str(test.timeout)]
    if "--rpc" in test.switches:
        command += ["--rpc", test.switches["--rpc"]]
    return command


def kill_process_tree(process):
    if os.name == "nt":
        subprocess.call(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, creationflags=NO_WINDOW)
    else:
        process.kill()


def output_problems(test, output):
    if test.expected_code == 0 and "TEST PASSED" not in output:
        return ["the script did not print TEST PASSED"]
    if test.expected_code != 0 and "TEST PASSED" in output:
        return ["the script printed TEST PASSED, but the run was expected to end with exit code %d"
                % test.expected_code]
    return []


def run(test, executable, work_directory):
    report_file = os.path.join(work_directory, test.file_stem + ".json")
    # A report left behind by an earlier run must not be read as this run's result
    if os.path.exists(report_file):
        os.remove(report_file)
    command = build_command(test, executable, work_directory, report_file)
    started = time.time()
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, creationflags=NO_WINDOW)
    try:
        output = process.communicate(timeout=test.timeout + PROCESS_TIMEOUT_MARGIN)[0].decode("utf-8", "replace")
    except subprocess.TimeoutExpired:
        kill_process_tree(process)
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
        problems += report_problems(report, test.report_checks)
    else:
        problems.append("no run report was written")
    problems += output_problems(test, output)
    if problems:
        with io.open(os.path.join(work_directory, test.file_stem + ".output.txt"), "w", encoding="utf-8") as writer:
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
    parser.add_argument("--filter", nargs="+", default=None)
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
        print("%-4s %-42s %6.1f s  %s" % ("PASS" if not problems else "FAIL", name, elapsed, "; ".join(problems)))
        if problems:
            with io.open(os.path.join(work, name + ".output.txt"), "w", encoding="utf-8") as writer:
                writer.write(output)
            failed.append((name, output))
    for test in tests:
        problems, elapsed, output = run(test, args.exe, work)
        print("%-4s %-42s %6.1f s  %s" % ("PASS" if not problems else "FAIL", test.label, elapsed,
                                          "; ".join(problems)))
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
