#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that every part export names the parts of its own source: the displayed view for the .stl and
#Mmg exports, the CAD geometry for the .step and .brep exports, and the current result for the
#deformed export. A geometry must be open before the script runs.
#
#  PrePoMax.com -f cube.stl -u MM_TON_S_C --script Test_Model_Export_Sources.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def output(name):
    file_name = os.path.join(tempfile.gettempdir(), name)
    if os.path.isfile(file_name):
        os.remove(file_name)
    return file_name


def test_displayed_view_export(part_name):
    # A geometry part has no finite element counterpart, and the .stl export works on the displayed
    # view rather than on the model mesh, so the geometry part is a valid export
    file_name = output("pytest_geometry.stl")
    pmx.export_to_stl([part_name], file_name)
    check(os.path.isfile(file_name), "the geometry part was not exported to .stl")
    os.remove(file_name)
    #
    check_raises(lambda: pmx.export_parts_as_mmg_mesh(["__missing_part__"],
                                                      output("pytest_missing.mesh")),
                 "an Mmg export of a part the displayed view does not hold was accepted")
    check_raises(lambda: pmx.export_to_stl(["__missing_part__"], output("pytest_missing.stl")),
                 "an export of a part the displayed view does not hold was accepted")
    check_raises(lambda: pmx.export_to_stl([], output("pytest_missing.stl")),
                 "an empty part name list was accepted")


def test_cad_export(part_name):
    # An .stl part carries no CAD data, so it is not a CAD part however valid its name is
    check_raises(lambda: pmx.export_cad_parts_as_step([part_name], output("pytest_geometry.step")),
                 "a CAD export of a part without CAD data was accepted")
    check_raises(lambda: pmx.export_cad_parts_as_brep([part_name], output("pytest_geometry.brep")),
                 "a CAD export of a part without CAD data was accepted")
    check_raises(lambda: pmx.export_cad_parts_as_step(["__missing_part__"],
                                                      output("pytest_missing.step")),
                 "a CAD export of a missing part was accepted")


def test_deformed_export(part_name):
    # The deformed export reads the current result, and no result is open here
    check_raises(lambda: pmx.export_deformed_parts_to_calculix([part_name],
                                                               output("pytest_deformed.inp")),
                 "a deformed export without a result was accepted")


def main():
    check(pmx.model.is_open(), "a model must be open")
    part_names = list(pmx.model.geometry.parts.get_names())
    check(len(part_names) > 0, "a geometry part is required")
    part_name = part_names[0]
    #
    test_displayed_view_export(part_name)
    test_cad_export(part_name)
    test_deformed_export(part_name)
    print("TEST PASSED")


main()
