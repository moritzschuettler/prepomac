#!/usr/bin/env python3
# PrePoMax for macOS - makes PrePoMax.app stand alone: copies the Homebrew libraries the app needs into
# Contents/Frameworks, CalculiX into Contents/MacOS/Solver and a Python into Contents/MacOS/Python, and points every
# copied program and library at the copies, so that the app runs on a Mac without Homebrew.
#
#   python3 bundle-deps.py <PrePoMax.app>          (make-app.sh --standalone runs it)
#
# PrePoMax looks for these files in the bundle before it looks in Homebrew: libgmsh.dylib and liblpsolve55.dylib in
# Contents/Frameworks, ccx in Contents/MacOS/Solver, the Python home in Contents/MacOS/Python.

import glob
import os
import shutil
import subprocess
import sys
import tarfile
import urllib.request

HOMEBREW = "/opt/homebrew"
PYTHON_URL = ("https://github.com/astral-sh/python-build-standalone/releases/download/20250317/"
              "cpython-3.12.9+20250317-aarch64-apple-darwin-install_only.tar.gz")
HERE = os.path.dirname(os.path.abspath(__file__))


def run(*args):
    return subprocess.run(args, check=True, capture_output=True, text=True).stdout


def rpaths(path):
    """The LC_RPATH entries of a Mach-O file, @loader_path resolved."""
    lines = run("otool", "-l", path).splitlines()
    result = []
    for i, line in enumerate(lines):
        if line.strip() == "cmd LC_RPATH":
            entry = lines[i + 2].strip().split(" ")[1]
            result.append(entry)
    return result


def references(path):
    """The libraries a Mach-O file loads, as they are written in it (without its own install name)."""
    lines = run("otool", "-L", path).splitlines()[1:]
    names = [line.strip().split(" (")[0] for line in lines]
    own = run("otool", "-D", path).splitlines()[1:]
    return [n for n in names if not own or n != own[0].strip()]


def resolve(name, referrer):
    """The file a reference of the referrer names, or None for a system library."""
    if name.startswith("/usr/lib/") or name.startswith("/System/"):
        return None
    if name.startswith("@rpath/"):
        leaf = name[len("@rpath/"):]
        folders = []
        for entry in rpaths(referrer):
            folders.append(entry.replace("@loader_path", os.path.dirname(referrer))
                                .replace("@executable_path", os.path.dirname(referrer)))
        folders += [HOMEBREW + "/lib"] + glob.glob(HOMEBREW + "/opt/*/lib") + glob.glob(HOMEBREW + "/opt/*/lib/gcc/current")
        for folder in folders:
            candidate = os.path.join(folder, leaf)
            if os.path.exists(candidate):
                return os.path.realpath(candidate)
        raise FileNotFoundError("%s of %s was not found" % (name, referrer))
    if name.startswith("@"):
        return None
    if name.startswith(HOMEBREW) or name.startswith("/usr/local/"):
        return os.path.realpath(name)
    return None


class Bundler:
    def __init__(self, app):
        self.app = os.path.abspath(app)
        self.frameworks = os.path.join(self.app, "Contents", "Frameworks")
        os.makedirs(self.frameworks, exist_ok=True)
        self.copied = {}            # the real file of a library -> its name in Frameworks
        self.changed = []           # every file whose load commands were changed, to sign again

    def library(self, real, name):
        """Copies a library into Frameworks under the name it is referenced by, once, with its own libraries."""
        if real in self.copied:
            first = self.copied[real]
            if name != first and not os.path.exists(os.path.join(self.frameworks, name)):
                os.symlink(first, os.path.join(self.frameworks, name))
            return
        self.copied[real] = name
        target = os.path.join(self.frameworks, name)
        shutil.copy2(real, target)
        os.chmod(target, 0o755)
        run("install_name_tool", "-id", "@rpath/" + name, target)
        self.relink(target, real, "@loader_path")

    def relink(self, target, original, rpath):
        """Points the libraries the file loads at their copies in Frameworks; rpath is how the file reaches them."""
        for reference in references(target):
            real = resolve(reference, original)
            if real is None:
                continue
            name = os.path.basename(reference)
            self.library(real, name)
            run("install_name_tool", "-change", reference, "@rpath/" + name, target)
        for entry in rpaths(target):
            if entry.startswith(HOMEBREW) or entry.startswith("/usr/local") or entry.startswith("@loader_path/../lib"):
                run("install_name_tool", "-delete_rpath", entry, target)
        if rpath not in rpaths(target):
            run("install_name_tool", "-add_rpath", rpath, target)
        self.changed.append(target)

    def program(self, path):
        """A program inside the bundle that loads Homebrew libraries."""
        rel = os.path.relpath(self.frameworks, os.path.dirname(path))
        self.relink(path, path, "@executable_path/" + rel)

    def licenses(self, extra):
        """The license files of the Homebrew formulas the copied files come from, which their licenses ask to be
        passed on with them."""
        target = os.path.join(self.app, "Contents", "Resources", "Licenses", "Bundled")
        formulas = set()
        for real in list(self.copied) + extra:
            if "/Cellar/" in real:
                formula, version = real.split("/Cellar/")[1].split("/")[:2]
                formulas.add((formula, version))
        os.makedirs(target, exist_ok=True)
        for formula, version in sorted(formulas):
            folder = os.path.join(HOMEBREW, "Cellar", formula, version)
            # The formula folder, or its documentation folder (OpenCASCADE keeps its license there)
            files = []
            for where in [folder] + glob.glob(os.path.join(folder, "share", "doc", "*")):
                if os.path.isdir(where):
                    files += [os.path.join(where, f) for f in os.listdir(where)
                              if f.upper().startswith(("LICENSE", "COPYING", "COPYRIGHT", "NOTICE"))]
            if files:
                os.makedirs(os.path.join(target, formula), exist_ok=True)
                for f in files:
                    shutil.copy2(f, os.path.join(target, formula, os.path.basename(f)))
        # The versions shipped, which name the matching sources (Homebrew formulas at these versions)
        with open(os.path.join(target, "VERSIONS.txt"), "w") as f:
            f.write("Homebrew formulas whose libraries or programs this app carries, at the versions shipped. Their\n"
                    "sources are the ones the formulas name (https://formulae.brew.sh).\n\n")
            for formula, version in sorted(formulas):
                f.write("%s %s\n" % (formula, version))
            f.write("\nPython: %s\n" % PYTHON_URL)
        return len(formulas)

    def sign(self):
        # install_name_tool breaks the signatures, and Apple Silicon runs no unsigned code
        for path in self.changed:
            run("codesign", "--force", "--sign", "-", path)


def find_ccx():
    for name in ["ccx"] + sorted(glob.glob(HOMEBREW + "/bin/ccx_*"), reverse=True):
        path = name if os.path.isabs(name) else os.path.join(HOMEBREW, "bin", name)
        if os.path.exists(path):
            return os.path.realpath(path)
    return None


def add_python(app):
    home = os.path.join(app, "Contents", "MacOS", "Python")
    if os.path.isdir(home):
        shutil.rmtree(home)
    cache = os.path.join(HERE, "Native", "Python", os.path.basename(PYTHON_URL))
    if not os.path.exists(cache):
        os.makedirs(os.path.dirname(cache), exist_ok=True)
        print("downloading " + PYTHON_URL)
        urllib.request.urlretrieve(PYTHON_URL, cache)
    with tarfile.open(cache) as archive:
        archive.extractall(os.path.dirname(home), filter="data")
    os.rename(os.path.join(os.path.dirname(home), "python"), home)
    python = os.path.join(home, "bin", "python3")
    # NumPy, which the API hands arrays out as where it is available
    subprocess.run([python, "-m", "pip", "install", "--quiet", "--disable-pip-version-check", "numpy"], check=True)
    return home


def main():
    if len(sys.argv) != 2:
        sys.exit("usage: bundle-deps.py <PrePoMax.app>")
    app = sys.argv[1]
    macos = os.path.join(app, "Contents", "MacOS")
    bundler = Bundler(app)
    # The CAD and mesher process
    bundler.program(os.path.join(macos, "Meshers", "Netgen", "NetGenMesher"))
    # The libraries PrePoMax itself loads by name
    for name in ("libgmsh.dylib", "liblpsolve55.dylib"):
        bundler.library(os.path.realpath(os.path.join(HOMEBREW, "lib", name)), name)
    # The other programs of the bundle, in case one of them loads a Homebrew library
    for path in glob.glob(os.path.join(macos, "Meshers", "**", "*"), recursive=True):
        if os.path.isfile(path) and os.access(path, os.X_OK) and path not in bundler.changed and \
                "Mach-O" in run("file", "-b", path) and any(resolve(r, path) for r in references(path)):
            bundler.program(path)
    # CalculiX
    ccx = find_ccx()
    if ccx:
        solver = os.path.join(macos, "Solver")
        os.makedirs(solver, exist_ok=True)
        target = os.path.join(solver, "ccx")
        shutil.copy2(ccx, target)
        os.chmod(target, 0o755)
        bundler.relink(target, ccx, "@executable_path/" + os.path.relpath(bundler.frameworks, solver))
    else:
        print("warning: no CalculiX ccx found in Homebrew; the app will need one installed")
    bundler.sign()
    formulas = bundler.licenses([ccx] if ccx else [])
    home = add_python(app)
    size = sum(os.path.getsize(os.path.join(root, f)) for root, _, files in os.walk(bundler.frameworks) for f in files
               if not os.path.islink(os.path.join(root, f)))
    print("bundled %d libraries (%.0f MB) in %s" % (len(bundler.copied), size / 1048576, bundler.frameworks))
    print("license files of %d Homebrew formulas in Contents/Resources/Licenses/Bundled" % formulas)
    print("CalculiX: %s" % (ccx or "-"))
    print("Python: %s" % home)
    # Nothing in the bundle may still load a library from Homebrew
    left = []
    for root, _, files in os.walk(app):
        for f in files:
            path = os.path.join(root, f)
            if os.path.islink(path) or not os.path.isfile(path) or path.startswith(home):
                continue
            if f.endswith((".dylib", ".so")) or os.access(path, os.X_OK):
                kind = run("file", "-b", path)
                if "Mach-O" in kind:
                    for reference in references(path):
                        if reference.startswith(HOMEBREW) or (reference.startswith("/usr/local/") and
                                                              "libAvalonia.Native" not in reference):
                            left.append("%s -> %s" % (os.path.relpath(path, app), reference))
    if left:
        sys.exit("still linked to Homebrew:\n  " + "\n  ".join(left))


main()
