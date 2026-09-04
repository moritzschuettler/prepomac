"""Runs the PrePoMax Python API test scripts through the batch mode and checks every run report.

This script runs OUTSIDE PrePoMax, with the system Python. Every test script describes how it is run
in its own header, so this runner reads the header rather than keeping a second list:

    #  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Loads.py -g No -x Yes -w <dir>
    #
    #Expected process exit code 0.

The model files named by the headers live in Models, next to this script. A header that names
PyTest_Job.frd depends on Test_Analysis.py, which solves the cube and writes that file into the work
directory, so those tests are run last.

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
MODELS = os.path.join(HERE, 'Models')
# The solved model Test_Analysis.py leaves behind, which the result tests read
RESULT_FILE = 'PyTest_Job.frd'
DEFAULT_EXE = os.path.abspath(os.path.join(HERE, '..', '..', '..', 'PrePoMax', 'bin', 'x64', 'Debug',
                                           'PrePoMax.com'))
COMMAND_LINE = re.compile(r'^#\s*PrePoMax\.com\s+(?P<arguments>.*)$')
EXIT_CODE = re.compile(r'^#\s*Expected process exit code\s+(?P<code>\d+)')
STATUS_BY_CODE = {0: 'Ok', 1: 'OptionError', 2: 'ModelError', 3: 'ScriptError', 4: 'TimedOut',
                  5: 'PromptRefused', 6: 'LauncherError'}


class Test(object):
    def __init__(self, path, model, expected_code):
        self.path = path
        self.name = os.path.basename(path)
        self.model = model
        self.expected_code = expected_code
        self.needs_result = model == RESULT_FILE

    def __repr__(self):
        return self.name


def read_header(path):
    """Returns the model file and the expected exit code a test script declares, or None."""
    model = None
    expected_code = None
    with io.open(path, encoding='utf-8') as reader:
        for line in reader:
            if not line.startswith('#'):
                break
            command = COMMAND_LINE.match(line.rstrip())
            if command is not None:
                arguments = command.group('arguments').split()
                if '-f' in arguments:
                    model = arguments[arguments.index('-f') + 1]
            code = EXIT_CODE.match(line.rstrip())
            if code is not None and expected_code is None:
                expected_code = int(code.group('code'))
    if expected_code is None:
        return None
    return model, expected_code


def collect(filter_text):
    tests = []
    for name in sorted(os.listdir(HERE)):
        if not name.startswith('Test_') or not name.endswith('.py'):
            continue
        if filter_text and filter_text.lower() not in name.lower():
            continue
        header = read_header(os.path.join(HERE, name))
        if header is None:
            print('  skipped %s: the header declares no expected exit code' % name)
            continue
        model, expected_code = header
        tests.append(Test(os.path.join(HERE, name), model, expected_code))
    # A test that reads the solved model runs after the one that writes it
    return sorted(tests, key=lambda test: (test.needs_result, test.name))


def run(test, executable, work_directory):
    report_file = os.path.join(work_directory, test.name + '.json')
    command = [executable]
    if test.model is not None:
        model = test.model if test.needs_result else os.path.join(MODELS, test.model)
        if test.needs_result:
            model = os.path.join(work_directory, model)
        command += ['-f', model, '-u', 'MM_TON_S_C']
    command += ['--script', test.path, '-g', 'No', '-x', 'Yes', '-w', work_directory,
                '--report', report_file, '--timeout', '600']
    #
    started = time.time()
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    output = process.communicate()[0].decode('utf-8', 'replace')
    elapsed = time.time() - started
    #
    problems = []
    if process.returncode != test.expected_code:
        problems.append('exit code %d, expected %d' % (process.returncode, test.expected_code))
    # The run report must agree with the exit code, which is the Layer 0 contract
    if os.path.isfile(report_file):
        with io.open(report_file, encoding='utf-8') as reader:
            report = json.load(reader)
        expected_status = STATUS_BY_CODE.get(test.expected_code)
        if report.get('exitCode') != test.expected_code:
            problems.append('report exitCode %s' % report.get('exitCode'))
        if expected_status is not None and report.get('exitStatus') != expected_status:
            problems.append('report exitStatus %s, expected %s'
                            % (report.get('exitStatus'), expected_status))
    else:
        problems.append('no run report was written')
    # A passing script says so, which catches a script that exits early without failing
    if test.expected_code == 0 and 'TEST PASSED' not in output:
        problems.append('the script did not print TEST PASSED')
    #
    return problems, elapsed, output


def main():
    parser = argparse.ArgumentParser(description='Runs the PrePoMax Python API test scripts.')
    parser.add_argument('--exe', default=DEFAULT_EXE, help='the PrePoMax.com launcher to run')
    parser.add_argument('--work', default=None, help='the work directory, a temporary one by default')
    parser.add_argument('--filter', default=None, help='runs only the tests whose name contains this')
    arguments = parser.parse_args()
    #
    if not os.path.isfile(arguments.exe):
        print('The launcher was not found: %s' % arguments.exe)
        return 1
    #
    work_directory = arguments.work
    temporary = work_directory is None
    if temporary:
        work_directory = tempfile.mkdtemp(prefix='prepomax_api_tests_')
    elif not os.path.isdir(work_directory):
        os.makedirs(work_directory)
    #
    print('Launcher:       %s' % arguments.exe)
    print('Work directory: %s' % work_directory)
    print('')
    #
    tests = collect(arguments.filter)
    failed = []
    try:
        for test in tests:
            problems, elapsed, output = run(test, arguments.exe, work_directory)
            state = 'PASS' if not problems else 'FAIL'
            print('%-4s %-38s %6.1f s  %s' % (state, test.name, elapsed, '; '.join(problems)))
            if problems:
                failed.append((test, output))
    finally:
        if temporary and not failed:
            shutil.rmtree(work_directory, ignore_errors=True)
    #
    print('')
    print('%d of %d tests passed.' % (len(tests) - len(failed), len(tests)))
    for test, output in failed:
        print('')
        print('===== %s' % test.name)
        print('\n'.join(output.strip().split('\n')[-25:]))
    if failed and temporary:
        print('')
        print('The work directory was kept for inspection: %s' % work_directory)
    #
    return 1 if failed else 0


sys.exit(main())
