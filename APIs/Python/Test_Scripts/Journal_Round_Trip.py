"""Checks that a recorded command history journals to a Python script that rebuilds the same model.

This script runs OUTSIDE PrePoMax, with the system Python. For every .pmh history in the fixture
directory it performs four runs:

    1. regenerate the history and export the model to CalculiX      -> the reference
    2. regenerate the history and write the session journal         -> the emitted script
    3. run the emitted script and export the model to CalculiX      -> the candidate
    4. compare the two exports, and report the commands the journal refused to emit

The CalculiX export is the comparison because it is an existing deterministic serializer that covers
the mesh, the sets, the materials, the sections, the steps and their content. Its header carries a
hash and a date that differ between two runs of the same model, so that line is normalised away.

The histories are recorded in the user interface rather than generated, because a script run through
--script is recorded as a single command: the script itself is the artifact, so that regeneration
re-runs its computations rather than replaying the values they produced. A fixture therefore carries
absolute paths to the geometry it imports, and it only replays on the machine it was recorded on.

    python Journal_Round_Trip.py [--histories <directory>] [--exe <PrePoMax.com>] [--work <directory>]

The process exit code is 0 when every history round tripped with no refused command.
"""
import argparse
import io
import os
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_EXE = os.path.abspath(os.path.join(HERE, "..", "..", "..", "PrePoMax", "bin", "x64", "Debug", "PrePoMax.com"))
DEFAULT_HISTORIES = os.path.join(HERE, "Histories")
# The two lines the exporter stamps with a per run hash and the date it was written
VOLATILE = re.compile(r"^\s*(Hash:|Date:)", re.IGNORECASE)
TIMEOUT = 600
PROCESS_TIMEOUT = TIMEOUT + 60
REFUSAL = re.compile(r"^\s*Not emitted:\s*(?P<refusal>.*)$", re.MULTILINE)
SUMMARY = re.compile(r"^Session journal: .* - (?P<total>\d+) commands, (?P<refused>\d+) not emitted",
                     re.MULTILINE)


class RoundTrip(object):
    def __init__(self, name):
        self.name = name
        self.refusals = []
        self.total = self.refused = 0
        self.failure = None

    @property
    def passed(self):
        return self.failure is None and self.refused == 0


def run(executable, arguments, work_directory):
    command = [executable] + arguments + ["-g", "No", "-x", "Yes", "--timeout", str(TIMEOUT),
                                          "-w", work_directory]
    try:
        completed = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                   timeout=PROCESS_TIMEOUT)
    except subprocess.TimeoutExpired:
        return 4, "the process did not finish within %d s" % PROCESS_TIMEOUT
    return completed.returncode, completed.stdout.decode("utf-8", "replace")


def write_script(path, body):
    io.open(path, "w", encoding="utf-8", newline="\n").write(body)


def read_normalised(path):
    lines = io.open(path, encoding="utf-8", errors="replace").read().splitlines()
    return [line for line in lines if not VOLATILE.search(line)]


def round_trip(executable, history, work_directory):
    result = RoundTrip(os.path.basename(history))
    reference = os.path.join(work_directory, "reference.inp")
    candidate = os.path.join(work_directory, "candidate.inp")
    journal = os.path.join(work_directory, "journal.py")
    export_reference = os.path.join(work_directory, "export_reference.py")
    replay = os.path.join(work_directory, "replay.py")
    for stale in [reference, candidate, journal]:
        if os.path.exists(stale):
            os.remove(stale)
    # 1 - the model the history itself builds
    write_script(export_reference, "pmx.export_to_calculix(r'%s')\n" % reference)
    code, output = run(executable, ["-f", history, "--script", export_reference], work_directory)
    if code != 0 or not os.path.exists(reference):
        result.failure = "the history did not regenerate and export (exit %s)" % code
        return result
    # 2 - the journal the history emits
    code, output = run(executable, ["-r", history, "--journal", journal], work_directory)
    if code != 0 or not os.path.exists(journal):
        result.failure = "the journal was not written (exit %s)" % code
        return result
    summary = SUMMARY.search(output)
    if summary is not None:
        result.total = int(summary.group("total"))
        result.refused = int(summary.group("refused"))
    result.refusals = [match.group("refusal").strip() for match in REFUSAL.finditer(output)]
    # 3 - the model the journal rebuilds
    write_script(replay, io.open(journal, encoding="utf-8").read() +
                 "\npmx.export_to_calculix(r'%s')\n" % candidate)
    code, output = run(executable, ["--script", replay], work_directory)
    if code != 0 or not os.path.exists(candidate):
        result.failure = "the emitted journal did not run (exit %s)%s" % (code, tail(output))
        return result
    # 4 - the two models
    expected, actual = read_normalised(reference), read_normalised(candidate)
    if expected != actual:
        result.failure = "the rebuilt model differs from the recorded one:%s" % first_difference(expected, actual)
    return result


def first_difference(expected, actual):
    for index in range(min(len(expected), len(actual))):
        if expected[index] != actual[index]:
            return ("\n      line %d\n      recorded: %s\n      rebuilt:  %s"
                    % (index + 1, expected[index], actual[index]))
    return "\n      the exports have %d and %d lines" % (len(expected), len(actual))


def tail(output, lines=12):
    if not output:
        return ""
    return "\n      " + "\n      ".join(output.strip().splitlines()[-lines:])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", default=DEFAULT_EXE)
    parser.add_argument("--histories", default=DEFAULT_HISTORIES)
    parser.add_argument("--work")
    arguments = parser.parse_args()
    #
    if not os.path.isdir(arguments.histories):
        print("No history directory: %s" % arguments.histories)
        print("Record a session in the user interface and save it with Edit - Commands - Save As.")
        return 1
    histories = sorted(os.path.join(arguments.histories, name)
                       for name in os.listdir(arguments.histories) if name.lower().endswith(".pmh"))
    if not histories:
        print("No .pmh histories in %s" % arguments.histories)
        return 1
    #
    work_directory = arguments.work or tempfile.mkdtemp(prefix="pmx_journal_")
    if not os.path.isdir(work_directory):
        os.makedirs(work_directory)
    print("Work directory: %s" % work_directory)
    #
    results = [round_trip(arguments.exe, history, work_directory) for history in histories]
    print("")
    for result in results:
        state = "PASS" if result.passed else "FAIL"
        print("%-6s %-40s %d commands, %d not emitted" % (state, result.name, result.total, result.refused))
        for refusal in result.refusals:
            print("         not emitted: %s" % refusal)
        if result.failure is not None:
            print("         %s" % result.failure)
    #
    failed = [result for result in results if not result.passed]
    print("")
    print("%d of %d histories round tripped" % (len(results) - len(failed), len(results)))
    return 1 if failed else 0


sys.exit(main())
