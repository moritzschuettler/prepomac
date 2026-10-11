#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that the source based exports - export_geometry, export_mesh, export_model and
#export_results - pick the format from the extension, refuse an extension that names none, and
#refuse a part their source does not hold: the geometry, the model mesh or the current result. A
#geometry must be open before the script runs.
#
#  PrePoMax.com -f cube.stl -u MM_TON_S_C --script Test_Model_Export_Sources.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message, unknown_keyword=None):
    # Only the API's own refusal passes: a PythonAPIException, or for a call that deliberately passes the
    # keyword unknown_keyword, the TypeError of the keyword guard. Anything else, and a message carrying a
    # .NET stack trace, is a fault of the application that happened to stop the call
    try:
        function()
    except Exception as ex:
        kind, text = type(ex).__name__, str(ex)
        if unknown_keyword is None:
            expected, refusal = kind == "PythonAPIException", "a PythonAPIException"
        else:
            expected = kind == "TypeError" and "unexpected keyword argument '%s'" % unknown_keyword in text
            refusal = "the keyword guard's TypeError for '%s'" % unknown_keyword
        if not expected or "   at " in text:
            raise Exception("TEST FAILED: " + message + " - the refusal must be " + refusal +
                            " without a .NET stack trace, not " + kind + ": " + text[0:500])
        return
    raise Exception("TEST FAILED: " + message)


def output(name):
    file_name = os.path.join(tempfile.gettempdir(), name)
    if os.path.isfile(file_name):
        os.remove(file_name)
    return file_name


def test_export_geometry(part_name):
    # The extension picks the format, and a geometry part without CAD data may go to .stl
    file_name = output("pytest_geometry_source.stl")
    pmx.export_geometry([part_name], file_name)
    check(os.path.isfile(file_name), "the geometry part was not exported to .stl")
    os.remove(file_name)
    # A given format wins over the extension
    file_name = output("pytest_geometry_source.txt")
    pmx.export_geometry(part_name, file_name, format="Stl")
    check(os.path.isfile(file_name), "the format argument was not honoured")
    os.remove(file_name)
    # An imported .stl body is neither a solid nor a shell part, which the Mmg format needs
    check_raises(lambda: pmx.export_geometry([part_name], output("pytest_geometry_source.mesh")),
                 "an Mmg export of an .stl body was accepted")
    # An .stl part carries no CAD data, so it is not a CAD part however valid its name is
    check_raises(lambda: pmx.export_geometry([part_name], output("pytest_geometry_source.step")),
                 "a .step export of a part without CAD data was accepted")
    check_raises(lambda: pmx.export_geometry([part_name], output("pytest_geometry_source.brep")),
                 "a .brep export of a part without CAD data was accepted")
    check_raises(lambda: pmx.export_geometry([part_name], output("pytest_geometry_source.stp"),
                                             format="StepAssembly"),
                 "a STEP assembly export of a part without CAD data was accepted")
    check_raises(lambda: pmx.export_geometry([part_name], output("pytest_geometry_source.brep"),
                                             format="BrepAssembly"),
                 "a BREP assembly export of a part without CAD data was accepted")
    check_raises(lambda: pmx.export_geometry([part_name], output("pytest_geometry_source.txt")),
                 "an extension that names no geometry format was accepted")
    check_raises(lambda: pmx.export_geometry([part_name], output("pytest_geometry_source.stl"),
                                             format="Calculix"),
                 "a format the geometry export does not offer was accepted")
    check_raises(lambda: pmx.export_geometry(["__missing_part__"], output("pytest_missing.stl")),
                 "a geometry export of a missing part was accepted")
    check_raises(lambda: pmx.export_geometry([], output("pytest_missing.stl")),
                 "an empty part name list was accepted")


def test_export_mesh(part_name):
    # The geometry is not meshed, so the model mesh holds no part of that name
    check_raises(lambda: pmx.export_mesh([part_name], output("pytest_mesh_source.inp")),
                 "a mesh export of a part the model mesh does not hold was accepted")
    check_raises(lambda: pmx.export_mesh([part_name], output("pytest_mesh_source.txt")),
                 "an extension that names no mesh format was accepted")


def test_export_model():
    check_raises(lambda: pmx.export_model(output("pytest_model_source.txt")),
                 "an extension that names no model format was accepted")
    check_raises(lambda: pmx.export_model(output("pytest_model_source.inp"), format="Gmsh"),
                 "a format the model export does not offer was accepted")


def test_export_results(part_name):
    # No result is open here
    check_raises(lambda: pmx.export_results([part_name], output("pytest_results_source.inp")),
                 "a results export without a result was accepted")
    check_raises(lambda: pmx.export_results([part_name], output("pytest_results_source.stl")),
                 "a results export without a result was accepted")


def main():
    check(pmx.model.is_open(), "a model must be open")
    part_names = list(pmx.model.geometry.parts.get_names())
    check(len(part_names) > 0, "a geometry part is required")
    part_name = part_names[0]
    #
    test_export_geometry(part_name)
    test_export_mesh(part_name)
    test_export_model()
    test_export_results(part_name)
    print("TEST PASSED")


main()
