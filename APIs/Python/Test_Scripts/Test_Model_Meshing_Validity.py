#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that a mesh setup item is judged by the definition it carries now. The validity check only
#ever narrows what it is given, so an item that reaches it invalid stays invalid, and an invalid item
#is silently skipped at mesh time - which is how a replaced extrude mesh item quietly became a
#tetrahedral mesh. A replace therefore resets the verdict, and a replace that is given a selection the
#model cannot honour is refused rather than recorded.
#
#The script opens a shell part and imports a solid one, because the difference between the two is what
#a shell mesher can and cannot be defined on.
#
#  PrePoMax.com -f open_shell.stl -u MM_TON_S_C --script Test_Model_Meshing_Validity.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
NAME = "PyTest_Validity_Shell"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def refusal(function):
    try:
        function()
    except Exception as error:
        return str(error)
    return None


def part_selection(part):
    return pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])


# A whole part mesher records either the parts the dialog picked or the geometry those parts cover,
# and the API offers both. The validity check used to resolve only the part form for the shell, the
# QuadWild, the thicken, the tetrahedral and the transfinite item, so one created from the geometry
# form came out invalid and was then silently skipped by every mesher.
def test_both_selection_targets(items, part):
    by_parts = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
    by_geometry = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])
    for label, adder in (("tetrahedral_gmsh", items.add_tetrahedral_gmsh),
                         ("shell_gmsh", items.add_shell_gmsh),
                         ("shell_quad_wild", items.add_shell_quad_wild)):
        for form, selection in (("parts", by_parts), ("geometry", by_geometry)):
            name = "PyTest_Target_" + label + "_" + form
            try:
                item = adder(name, selection)
            except Exception:
                continue    # the item cannot be defined on this part type at all
            check(item.valid, label + " created from the '" + form + "' target is invalid, so every " +
                  "mesher would skip it")
            items.remove([name])


def main():
    pmx.import_file(os.path.join(MODELS, "cube.stl"))
    parts = pmx.model.geometry.parts
    shells = [part for part in parts if part.part_type == "Shell"]
    solids = [part for part in parts if part.part_type != "Shell"]
    check(len(shells) > 0 and len(solids) > 0, "a shell part and a solid part are both required")
    shell, solid = shells[0], solids[0]
    #
    items = pmx.model.mesh.setup_items
    for name in list(items.get_names()):
        if name.startswith("PyTest_"):
            items.remove([name])
    # An item that can be honoured is valid
    items.add_shell_gmsh(NAME, part_selection(shell))
    check(items[NAME].valid, "a shell mesher on a shell part must be valid")
    # Removing the part it was defined on is what makes an item invalid
    pmx.model.geometry.parts.remove([shell.name])
    check(not items[NAME].valid, "an item whose part is gone must be invalid")
    check(len(items[NAME].get_geometry_ids()) == 0, "an item whose part is gone must have no region")
    # Pointing it at geometry it can be defined on makes it valid again. Before the verdict was reset on
    # a replace the item kept the new region, stayed invalid, and was skipped by every mesher
    pmx.import_file(os.path.join(MODELS, "open_shell.stl"))
    again = [part for part in pmx.model.geometry.parts if part.part_type == "Shell"][0]
    items.replace_shell_gmsh(NAME, selection=part_selection(again))
    check(items[NAME].valid, "a replace with a selection the model can honour must make the item valid")
    check(len(items[NAME].get_geometry_ids()) > 0, "the replaced item must keep the new region")
    # A selection the model cannot honour is refused, and the item keeps what it had
    geometry_ids = list(items[NAME].get_geometry_ids())
    message = refusal(lambda: items.replace_shell_gmsh(NAME, selection=part_selection(solid)))
    check(message is not None, "a selection a shell mesher cannot be defined on must be refused")
    check("shell parts" in message, "the refusal must say why: " + message)
    check(list(items[NAME].get_geometry_ids()) == geometry_ids, "a refused replace must change nothing")
    check(items[NAME].valid, "a refused replace must leave the item valid")
    # A replace that is given no selection keeps the region and is not asked the question
    items.replace_shell_gmsh(NAME, name="PyTest_Validity_Renamed")
    check(list(items["PyTest_Validity_Renamed"].get_geometry_ids()) == geometry_ids,
          "a replace without a selection must keep the region")
    items.remove(["PyTest_Validity_Renamed"])
    test_both_selection_targets(items, again)
    print("TEST PASSED")


main()
