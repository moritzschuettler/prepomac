"""Puts the ActiViz side of a saved run of the port checks into a new run, so that the native view of a build
without ActiViz is compared with what ActiViz drew. The recorders still write both sides; once ActiViz is gone the
old side is the native view again, and this replaces it with the saved one before the comparison runs:

    py -3 graft_reference.py <reference suite folder> <new suite folder>

Every file under a folder named old is copied from the reference, and in every JSON file the values of the keys
named old or old_<something> are taken from the reference JSON of the same path, walking both documents together.
A JSON file or an old picture the reference does not have, or a document whose shape differs where an old value
sits, is reported and fails the graft.
"""

import json
import os
import re
import shutil
import sys

OLD_KEY = re.compile(r"^old($|_)")


def graft_value(reference, new, path, problems):
    if isinstance(new, dict):
        if not isinstance(reference, dict):
            problems.append("%s: not an object in the reference" % path)
            return new
        for key in list(new.keys()):
            if OLD_KEY.match(key):
                if key not in reference:
                    problems.append("%s.%s: missing in the reference" % (path, key))
                    continue
                new[key] = reference[key]
            elif key in reference:
                new[key] = graft_value(reference[key], new[key], path + "." + key, problems)
        return new
    if isinstance(new, list):
        if not isinstance(reference, list):
            problems.append("%s: not a list in the reference" % path)
            return new
        if contains_old(new) and len(reference) != len(new):
            problems.append("%s: %d items, the reference has %d" % (path, len(new), len(reference)))
            return new
        for i in range(min(len(new), len(reference))):
            new[i] = graft_value(reference[i], new[i], "%s[%d]" % (path, i), problems)
        return new
    return new


def contains_old(value):
    if isinstance(value, dict):
        return any(OLD_KEY.match(k) or contains_old(v) for k, v in value.items())
    if isinstance(value, list):
        return any(contains_old(v) for v in value)
    return False


def graft(reference_dir, new_dir):
    problems = []
    grafted_files = 0
    grafted_documents = 0
    for root, dirs, files in os.walk(new_dir):
        relative = os.path.relpath(root, new_dir)
        parts = relative.split(os.sep)
        for name in files:
            new_path = os.path.join(root, name)
            reference_path = os.path.join(reference_dir, relative, name)
            if "old" in parts:
                if not os.path.isfile(reference_path):
                    problems.append("%s: no reference file" % os.path.join(relative, name))
                    continue
                shutil.copyfile(reference_path, new_path)
                grafted_files += 1
            elif name.endswith(".json"):
                with open(new_path) as stream:
                    document = json.load(stream)
                if not contains_old(document):
                    continue
                if not os.path.isfile(reference_path):
                    problems.append("%s: no reference document" % os.path.join(relative, name))
                    continue
                with open(reference_path) as stream:
                    reference = json.load(stream)
                document = graft_value(reference, document, os.path.join(relative, name), problems)
                with open(new_path, "w") as stream:
                    json.dump(document, stream, indent=1)
                grafted_documents += 1
    return grafted_files, grafted_documents, problems


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(2)
    files, documents, problems = graft(sys.argv[1], sys.argv[2])
    for problem in problems:
        print("GRAFT PROBLEM " + problem)
    print("GRAFT %d old files and %d documents from %s" % (files, documents, sys.argv[1]))
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
