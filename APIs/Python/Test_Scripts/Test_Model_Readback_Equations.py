#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the bulk model readback and the equation preservation: a value the user could type as an
#expression must read back as that expression, not as the number it evaluated to.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Readback_Equations.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import os
import sys

PREFIX = "PyTest_Readback_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def clean():
    for collection in [pmx.model.steps, pmx.model.materials, pmx.model.parameters]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def test_bulk_mesh_readback():
    node_ids = list(pmx.model.nodes.get_ids())
    check(len(node_ids) == pmx.model.nodes.count, "the node IDs do not match the node count")
    check(len(pmx.model.nodes.get_coordinates()) == pmx.model.nodes.count, "the coordinates are incomplete")
    check(list(pmx.model.nodes.get_data().ids) == node_ids, "the node snapshot differs from the IDs")
    check(len(pmx.model.elements.get_data().ids) == pmx.model.elements.count,
          "the element snapshot is incomplete")


def test_material_readback():
    material = pmx.model.materials.add(PREFIX + "Steel", pmx.create.elastic(210000, 0.3))
    check(material.property_count == 1, "the material must carry one property")
    check(len(material.get_properties()) > 0, "the material properties did not read back")


def test_equation_readback():
    pmx.model.parameters.add(PREFIX + "P", 2.0)
    step = pmx.model.steps.add_static(PREFIX + "Step", time_period=1.0)
    load = step.loads.add_concentrated_force(
        PREFIX + "Load", pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1]),
        f1=PREFIX + "P", f2='=3', f3=4)
    equations = load.get_equations()
    # An expression reads back as the expression; a plain value is left out, and get_value returns it
    check(equations['f1'] == '=' + PREFIX + "P",
          "the parameter expression did not read back: " + str(equations['f1']))
    check(equations['f2'] == '=3', "the equation did not read back: " + str(equations['f2']))
    check('f3' not in equations, "the plain value was read back as an equation: " + str(equations))
    check(load.get_value('f3') == 4, "the plain value did not read back: " + str(load.get_value('f3')))


def test_script_context():
    # A script run from a file knows its own path, so a journal can resolve the assets next to it
    check(os.path.abspath(__file__) == os.path.abspath(sys.argv[0]),
          "__file__ and argv[0] disagree: " + __file__ + " and " + sys.argv[0])
    check(os.path.isdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), "Models")),
          "the asset directory next to the script was not found")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    test_bulk_mesh_readback()
    test_material_readback()
    test_equation_readback()
    test_script_context()
    clean()
    print("TEST PASSED")


main()
