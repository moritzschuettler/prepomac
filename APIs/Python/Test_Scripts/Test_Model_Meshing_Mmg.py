#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that a shell MMG setup item opts an open STL shell out of the default Netgen mesher.
#
#  PrePoMax.com -f open_shell.stl -u MM_TON_S_C --script Test_Model_Meshing_Mmg.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_MMG_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = pmx.model.geometry.parts
    check(len(parts) == 1, "one open STL shell part is required")
    part = parts.first()
    check(part.part_type == "Shell", "the imported STL part must be open")
    selection = pmx.create.selection_by_ids("geometry", "geometry", "parts", [part.id])
    items = pmx.model.mesh.setup_items
    for name in list(items.get_names()):
        if name.startswith(PREFIX):
            items.remove([name])
    #
    items.add_meshing_parameters(PREFIX + "Size", selection, max_size=2.0, min_size=0.1,
                                 second_order=False)
    mmg = items.add_shell_mmg(PREFIX + "Mesher", selection, keep_model_edges=True)
    check(mmg.mesher_type == "Mmg", "the shell MMG setup item does not select MMG")
    check(mmg.valid, "the shell MMG setup item is not valid on the open STL shell")
    # The dialog scales the Hausdorff distance with the part, so the API must do the same: it is the
    # diagonal of the part bounding box times the relative factor, rounded to two significant digits.
    # The expectation is derived rather than written down, so that retuning the factor does not make
    # this a false failure; the tolerance is the 5 percent that rounding to two digits can move.
    # The item uses the relative distance by default, and reads back only the distance it uses
    relative_hausdorff = mmg.get_settings()["relative_hausdorff"]
    hausdorff = items.replace_shell_mmg(mmg.name, relative_size=False).get_settings()["hausdorff"]
    items.replace_shell_mmg(mmg.name, relative_size=True)
    box = part.get_bounding_box()
    diagonal = ((box[1] - box[0]) ** 2 + (box[3] - box[2]) ** 2 + (box[5] - box[4]) ** 2) ** 0.5
    expected = diagonal * relative_hausdorff
    check(expected > 0, "the part must have a size: " + str(box))
    check(abs(hausdorff - expected) <= 0.05 * expected,
          "the Hausdorff distance does not follow the size of the part: " + str(hausdorff) +
          " against " + str(expected))
    #
    pmx.model.mesh.generate(part.name)
    check(len(pmx.model.parts) == 1, "MMG did not create one shell mesh part")
    mesh_part = pmx.model.parts.first()
    check(mesh_part.part_type == "Shell", "MMG did not create a shell mesh")
    check(mesh_part.element_count > 0, "the MMG shell mesh has no elements")
    print("MMG meshed:", mesh_part)
    print("TEST PASSED")


main()
