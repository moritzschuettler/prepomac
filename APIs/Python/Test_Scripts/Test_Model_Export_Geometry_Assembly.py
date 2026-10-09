#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that export_geometry writes one file per part for the "Step" and "Brep" formats and a single
#joined file for "StepAssembly" and "BrepAssembly", and that the joined file still holds every part.
#A CAD geometry of more than one part must be open before the script runs, so box_and_cylinder.brep
#is imported.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Export_Geometry_Assembly.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import glob
import os
import tempfile

DIRECTORY = os.path.join(tempfile.gettempdir(), "PyTest_Export_Assembly")


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def empty_directory():
    if not os.path.isdir(DIRECTORY):
        os.makedirs(DIRECTORY)
    for file_name in glob.glob(os.path.join(DIRECTORY, "*")):
        os.remove(file_name)


def written_file_names():
    return sorted(os.path.basename(name) for name in glob.glob(os.path.join(DIRECTORY, "*")))


def test_export(part_names, format_name, extension, expected_count):
    empty_directory()
    pmx.export_geometry(part_names, os.path.join(DIRECTORY, "PyTest_Assembly" + extension), format=format_name)
    written = written_file_names()
    check(len(written) == expected_count,
          "the format " + format_name + " wrote the files " + str(written) + " instead of " +
          str(expected_count) + " file(s)")
    return os.path.join(DIRECTORY, written[0])


def test_assembly_holds_every_part(file_name, part_names):
    # Reading the joined file back must add the parts it was written from
    before = len(pmx.model.geometry.parts.get_names())
    pmx.import_file(file_name)
    added = len(pmx.model.geometry.parts.get_names()) - before
    check(added == len(part_names),
          "the joined file " + os.path.basename(file_name) + " holds " + str(added) +
          " parts instead of " + str(len(part_names)))


def main():
    check(pmx.model.is_open(), "a model must be open")
    part_names = list(pmx.model.geometry.parts.get_names())
    check(len(part_names) > 1, "a geometry of more than one part is required")
    #
    test_export(part_names, "Step", ".stp", len(part_names))
    test_export(part_names, "Brep", ".brep", len(part_names))
    step_file_name = test_export(part_names, "StepAssembly", ".stp", 1)
    test_assembly_holds_every_part(step_file_name, part_names)
    #
    brep_file_name = test_export(part_names, "BrepAssembly", ".brep", 1)
    test_assembly_holds_every_part(brep_file_name, part_names)
    #
    empty_directory()
    print("TEST PASSED")


main()
