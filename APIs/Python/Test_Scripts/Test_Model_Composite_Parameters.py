#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that a batch regeneration with -p changes the layers of a composite shell section: a child run builds a
#symmetric [A/90] laminate on the flat plate of composite_shells.inp whose first layer takes its thickness and its
#angle from parameters, saves it and exports the solver input, and two -r regenerations of the saved .pmx, one with a
#.pmp file and one with the parameters on the command line, must write that input again with the new thickness and
#the new layer axes.
#
#  PrePoMax.com --script Test_Model_Composite_Parameters.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import shutil
import subprocess
import tempfile

# A console program started from a process without a visible console opens a window of its own
NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)

PREFIX = "PyTest_"
MODEL = "composite_shells.inp"
DECK = "layup.inp"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def launcher():
    import System
    executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
    path = os.path.join(os.path.dirname(executable), "PrePoMax.com")
    check(os.path.isfile(path), "the launcher " + path + " does not exist")
    return path


def run(arguments, work_directory):
    completed = subprocess.run([launcher()] + arguments + ["-g", "No", "-x", "Yes", "-w", work_directory,
                                                           "--timeout", "180"],
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=300, creationflags=NO_WINDOW)
    output = completed.stdout.decode("utf-8", "replace")
    check(completed.returncode == 0, " ".join(arguments) + " must exit with 0, not " + str(completed.returncode) +
          ":\n" + output)


def write_build_script(work_directory):
    deck = os.path.join(work_directory, DECK)
    script = os.path.join(work_directory, "build_layup.py")
    with open(script, "w") as writer:
        writer.write("pmx.model.parameters.add('{0}T', 0.25)\n"
                     "pmx.model.parameters.add('{0}A', 30)\n"
                     "pmx.model.parts.set_element_type(list(pmx.model.parts.get_names()), 'parabolic_quad', 'S8R')\n"
                     "pmx.model.materials.add('{0}Ply', pmx.create.elastic_lamina("
                     "[[140000, 10000, 0.3, 5000, 5000, 3500, 0]]))\n"
                     "pmx.model.sections.add_composite_shell('{0}Plate', pmx.create.region('ElementSetName', 'PLATE'),"
                     " [['={0}T', '{0}Ply', '={0}A'], [0.25, '{0}Ply', 90]], symmetric=True)\n"
                     "pmx.export_to_calculix(r'{1}')\n".format(PREFIX, deck))
    return script


def read_layers(work_directory):
    """The thickness and the first axis of every layer of the composite section, as the solver input writes them."""
    path = os.path.join(work_directory, DECK)
    check(os.path.isfile(path), "the solver input " + path + " was not written")
    with open(path, "r") as reader:
        lines = [line.strip() for line in reader.read().splitlines()]
    os.remove(path)
    #
    def parameters(line):
        return {p.split("=")[0].strip().lower(): p.split("=")[1].strip() for p in line.split(",")[1:] if "=" in p}
    orientations, rows = {}, []
    for i, line in enumerate(lines):
        lower = line.lower()
        if lower.startswith("*orientation"):
            orientations[parameters(line)["name"].lower()] = [float(v) for v in lines[i + 1].split(",")][:3]
        if lower.startswith("*shell section") and "composite" in lower:
            check(not rows, "the flat plate must be one composite group")
            j = i + 1
            while j < len(lines) and not lines[j].startswith("*"):
                rows.append([v.strip() for v in lines[j].split(",")])
                j += 1
    check(len(rows) == 4, "the symmetric layup must have 4 solver layers: " + str(rows))
    return [(float(row[0]), orientations[row[3].lower()]) for row in rows]


def check_layers(layers, thickness, angle, source):
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    expected = [(thickness, [c, s, 0]), (0.25, [0, 1, 0]), (0.25, [0, 1, 0]), (thickness, [c, s, 0])]
    for k, ((t, axis), (expected_t, expected_axis)) in enumerate(zip(layers, expected)):
        check(abs(t - expected_t) < 1e-9 and all(abs(a - b) < 1e-6 for a, b in zip(axis, expected_axis)),
              "%s: layer %d is %g thick along %s, not %g along %s" % (source, k + 1, t, axis, expected_t,
                                                                      expected_axis))


def main():
    work_directory = tempfile.mkdtemp(prefix="PyTest_Composite_Parameters_")
    try:
        shutil.copy(os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models", MODEL), work_directory)
        saved = os.path.join(work_directory, "layup.pmx")
        # The saved model, with the values of its parameters
        run(["-f", os.path.join(work_directory, MODEL), "-u", "MM_TON_S_C", "--script",
             write_build_script(work_directory), "--save", saved], work_directory)
        check_layers(read_layers(work_directory), 0.25, 30, "the saved model")
        # A regeneration with a .pmp file
        parameters_file = os.path.join(work_directory, "layup.pmp")
        with open(parameters_file, "w") as writer:
            writer.write("$Name;Value/Equation;Evaluates to;Description\n"
                         "{0}T;0.4;0.4;\n"
                         "{0}A;60;60;\n".format(PREFIX))
        run(["-r", saved, "-p", parameters_file], work_directory)
        check_layers(read_layers(work_directory), 0.4, 60, "the regeneration with the .pmp file")
        # A regeneration with the parameters on the command line
        run(["-r", saved, "-p", "{0}T=0.3; {0}A=-45".format(PREFIX)], work_directory)
        check_layers(read_layers(work_directory), 0.3, -45, "the regeneration with -p values")
    finally:
        shutil.rmtree(work_directory, ignore_errors=True)
    print("TEST PASSED")


main()
