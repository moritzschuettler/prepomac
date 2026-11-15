"""Round trip of a corpus of .pmx files through a PrePoMax build.

This script runs OUTSIDE PrePoMax, with the system Python. For every .pmx file listed in the corpus file (one path
per line, # starts a comment) it works on a copy, never on the listed file, and runs four batch sessions:

    A  open the copy, write the fingerprint A.json, save the model as saved.pmx
    B  open saved.pmx, write B.json                          (a saved file reopens to the same model)
    R  regenerate the pre-processing history of a copy of saved.pmx (-r1 ... -o Yes) into regen.pmx
    C  open regen.pmx, write C.json                          (the regenerated model)

A file passes when A, B and C ran, A equals B, and - with --baseline, the output folder of a run of another build -
A equals the baseline's A (an old file opens to the same model as before) and C equals the baseline's C. A file the
baseline could not open or regenerate is reported as such rather than failed.

    python Run_Corpus.py --exe <PrePoMax.com> --corpus <list.txt> --out <folder> [--baseline <folder>] [--jobs N]
"""
import argparse
import concurrent.futures
import json
import os
import shutil
import subprocess
import sys
import threading
import time

HERE = os.path.dirname(os.path.abspath(__file__))
FINGERPRINT = os.path.join(HERE, "Corpus_Fingerprint.py")
NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)
TIMEOUT = 900
# The application seeds its random file names with the clock, so two sessions started in the same moment
# collide on the write access test of the program folder; starts are spaced out
START_SPACING = 1.5
_start_lock = threading.Lock()
_last_start = [0.0]


def run(exe, arguments, folder, log_name):
    with _start_lock:
        wait = _last_start[0] + START_SPACING - time.time()
        if wait > 0:
            time.sleep(wait)
        _last_start[0] = time.time()
    start = time.time()
    with open(os.path.join(folder, log_name + ".log"), "w", encoding="utf-8", errors="replace") as log:
        try:
            process = subprocess.run([exe] + arguments, cwd=folder, stdout=log, stderr=subprocess.STDOUT,
                                     timeout=TIMEOUT, creationflags=NO_WINDOW)
            code = process.returncode
        except subprocess.TimeoutExpired:
            code = "timeout"
    return code, round(time.time() - start, 1)


def load(file_name):
    if not os.path.exists(file_name):
        return None
    with open(file_name, "r", encoding="utf-8") as file:
        return json.load(file)


def comparable(fingerprint):
    if fingerprint is None:
        return None
    copy = dict(fingerprint)
    copy.pop("unreadable", None)
    return copy


def differences(first, second, path="", found=None, limit=8):
    if found is None:
        found = []
    if len(found) >= limit:
        return found
    if isinstance(first, dict) and isinstance(second, dict):
        for key in sorted(set(first) | set(second)):
            differences(first.get(key), second.get(key), path + "/" + str(key), found, limit)
    elif isinstance(first, list) and isinstance(second, list) and len(first) == len(second):
        for index, (a, b) in enumerate(zip(first, second)):
            differences(a, b, path + "[" + str(index) + "]", found, limit)
    elif first != second:
        text = lambda value: json.dumps(value, default=str)[0:160]
        found.append(path + ": " + text(first) + " != " + text(second))
    return found


def finished(folder, log_name):
    file_name = os.path.join(folder, log_name + ".log")
    if not os.path.exists(file_name):
        return False
    with open(file_name, "r", encoding="utf-8", errors="replace") as file:
        return "Finished" in file.read()


def is_complete(folder):
    if not finished(folder, "A"):
        return False
    if not os.path.exists(os.path.join(folder, "saved.pmx")):
        return True
    return finished(folder, "C")


def round_trip(exe, source, folder, resume=False):
    if resume and is_complete(folder):
        return {"source": source, "resumed": True}
    if os.path.exists(folder):
        shutil.rmtree(folder)
    os.makedirs(folder)
    shutil.copyfile(source, os.path.join(folder, "orig.pmx"))
    common = ["-g", "No", "-x", "Yes", "-w", folder]
    record = {"source": source}
    record["A"] = run(exe, ["-f", "orig.pmx", "--script", FINGERPRINT, "--scriptArgs",
                            "A.json saved.pmx", "--report", "A.report.json"] + common, folder, "A")
    if os.path.exists(os.path.join(folder, "saved.pmx")):
        record["saved_size"] = os.path.getsize(os.path.join(folder, "saved.pmx"))
        record["B"] = run(exe, ["-f", "saved.pmx", "--script", FINGERPRINT, "--scriptArgs", "B.json", "--report",
                                 "B.report.json"] + common,
                          folder, "B")
        shutil.copyfile(os.path.join(folder, "saved.pmx"), os.path.join(folder, "regen.pmx"))
        record["R"] = run(exe, ["--r1", "regen.pmx", "-o", "Yes"] + common, folder, "R")
        record["C"] = run(exe, ["-f", "regen.pmx", "--script", FINGERPRINT, "--scriptArgs", "C.json"] + common,
                          folder, "C")
    return record


def own_problems(folder):
    a, b, c = (comparable(load(os.path.join(folder, name + ".json"))) for name in ("A", "B", "C"))
    problems = {}
    if a is None:
        problems["open"] = "does not open"
    elif b is None:
        problems["reopen"] = "saved file does not reopen"
    elif a != b:
        problems["reopen"] = "reopened model differs: " + "; ".join(differences(a, b))
    if a is not None and c is None:
        problems["regenerate"] = "does not regenerate"
    return a, c, problems


def judge(record, folder, baseline_folder):
    a, c, problems = own_problems(folder)
    notes = []
    if baseline_folder is not None:
        base_a, base_c, base_problems = own_problems(baseline_folder)
        # What the baseline build fails as well is not a failure of this build
        for kind in list(problems):
            if kind in base_problems:
                notes.append("as the baseline: " + problems.pop(kind)[0:200])
        if a is not None and base_a is not None and a != base_a:
            problems["baseline_open"] = "opens differently from the baseline: " + "; ".join(differences(base_a, a))
        if c is not None and base_c is not None and c != base_c:
            problems["baseline_regenerate"] = "regenerates differently from the baseline: " +                 "; ".join(differences(base_c, c))
    record["problems"] = list(problems.values())
    record["notes"] = notes
    return record


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", required=True)
    parser.add_argument("--corpus", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--baseline")
    parser.add_argument("--jobs", type=int, default=4)
    parser.add_argument("--judge-only", action="store_true")
    parser.add_argument("--resume", action="store_true", help="keep the files a previous run finished")
    arguments = parser.parse_args()
    #
    sources = []
    with open(arguments.corpus, "r", encoding="utf-8") as file:
        for line in file:
            line = line.split("#")[0].strip()
            if line:
                sources.append(line)
    os.makedirs(arguments.out, exist_ok=True)
    folders = [os.path.join(arguments.out, "%03d" % index) for index in range(len(sources))]
    #
    records = []
    if arguments.judge_only:
        records = [{"source": source} for source in sources]
    else:
        with concurrent.futures.ThreadPoolExecutor(max_workers=arguments.jobs) as pool:
            futures = [pool.submit(round_trip, arguments.exe, source, folder, arguments.resume)
                       for source, folder in zip(sources, folders)]
            for index, future in enumerate(futures):
                records.append(future.result())
                print("%3d/%d %s" % (index + 1, len(sources), sources[index]), flush=True)
    failed = 0
    for record, folder in zip(records, folders):
        baseline = None if arguments.baseline is None else \
            os.path.join(arguments.baseline, os.path.basename(folder))
        judge(record, folder, baseline)
        if record["problems"]:
            failed += 1
    with open(os.path.join(arguments.out, "report.json"), "w", encoding="utf-8") as file:
        json.dump(records, file, indent=1)
    for record in records:
        state = "FAIL" if record["problems"] else "ok  "
        print(state, record["source"], "|", "; ".join(record["problems"] + record["notes"])[0:600])
    print("%d of %d files failed" % (failed, len(records)))
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
