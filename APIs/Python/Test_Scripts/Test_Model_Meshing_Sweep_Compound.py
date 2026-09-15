#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the sweep mesh of a compound part, where every sub-volume is one sweep block and the blocks feed
#each other through the surfaces they share. The three geometries were built with gmsh from stacked
#boxes that were fragmented, so the boxes share their contact faces and import as a compound part.
#
#  - sweep_stack3.brep: three boxes in a row. The middle one reads its source from the cap of the first
#    and leaves a cap for the third, from either end of the stack.
#  - sweep_stack_small.brep: a small box on a large one. Swept from the top, the large box takes the
#    shared face plus its exposed rim as one source. Swept from the bottom the shared face ends inside
#    the target of the large box, and the hand-off is refused rather than recorded.
#  - sweep_split_target.brep: a single box whose top is split by an imprint into an inner face and a rim.
#    The target may be split, and the cap has to land on both faces.
#
#  PrePoMax.com -f cube.stl -u MM_TON_S_C --script Test_Model_Meshing_Sweep_Compound.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import sys

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
SWEEP_NAME = "PyTest_Sweep"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def import_geometry(file_name):
    """Imports a brep and returns the parts it added: the solid sub-parts and the compound, if any."""
    before = set(pmx.model.geometry.parts.get_names())
    pmx.import_file(os.path.join(MODELS, file_name))
    added = [part for part in pmx.model.geometry.parts if part.name not in before]
    solids = [part for part in added if part.part_type in ("Solid", "SolidAsShell") and part.is_cad_part]
    compounds = [part for part in added if part.part_type == "Compound"]
    check(len(solids) > 0, "no CAD solid was imported from " + file_name)
    return solids, compounds[0] if compounds else None


def find_end_face(solids, top):
    """The face with the highest or the lowest centroid among the given solids, as (part, face id)."""
    best = None
    for part in solids:
        faces = part.get_face_data()
        ids = list(faces.ids)
        centroids = list(faces.centroids)
        for i in range(faces.count):
            z = list(centroids[i])[2]
            if best is None or (z > best[2] if top else z < best[2]):
                best = (part, ids[i], z)
    return best[0], best[1]


def face_selection(part, face_id):
    return pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces', {part.id: [face_id]})


def mesh_with_sweep(part_to_mesh, solids, source_part, face_id):
    """Meshes a part with a sweep from the given face and returns the mesh parts of its solids, which a
    compound names after its sub-parts."""
    items = pmx.model.mesh.setup_items
    items.add_sweep_mesh(SWEEP_NAME, face_selection(source_part, face_id))
    check(items[SWEEP_NAME].valid, "the sweep mesh item on " + source_part.name + " is not valid")
    #
    report = pmx.model.mesh.create(part_to_mesh.name)
    check(report[part_to_mesh.name] == "", "meshing " + part_to_mesh.name + " reported: " + report[part_to_mesh.name])
    names = set(solid.name for solid in solids)
    meshed = [part for part in pmx.model.parts if part.name in names]
    check(len(meshed) == len(solids),
          "expected %d mesh parts from %s, got %d" % (len(solids), part_to_mesh.name, len(meshed)))
    for part in meshed:
        check(part.element_count > 0, "the mesh part " + part.name + " has no elements")
        print("meshed:", part.name, part.node_count, "nodes", part.element_count, "elements")
    #
    items.remove([SWEEP_NAME])
    return meshed


def test_chain_of_three():
    solids, compound = import_geometry("sweep_stack3.brep")
    check(compound is not None and len(solids) == 3, "the three fragmented boxes must import as a compound part")
    # From the bottom: the first block leaves a cap for the second, the second for the third
    part, face_id = find_end_face(solids, top=False)
    meshed = mesh_with_sweep(compound, solids, part, face_id)
    # The blocks share their interface nodes, so the model holds fewer nodes than the parts add up to
    check(pmx.model.nodes.count < sum(part.node_count for part in meshed),
          "the sub-parts of the chain do not share their interface nodes")
    # From the top the chain runs the other way and must work just the same
    part, face_id = find_end_face(solids, top=True)
    mesh_with_sweep(compound, solids, part, face_id)


def test_small_block_on_large():
    solids, compound = import_geometry("sweep_stack_small.brep")
    check(compound is not None and len(solids) == 2, "the two fragmented boxes must import as a compound part")
    # From the bottom the shared face ends inside the target of the large box; the cap the large box
    # leaves cannot match the gmsh nodes on the imprint, so the item is refused, and nothing is recorded
    part, face_id = find_end_face(solids, top=False)
    items = pmx.model.mesh.setup_items
    check_raises(lambda: items.add_sweep_mesh(SWEEP_NAME, face_selection(part, face_id)),
                 "a hand-off through a shared face that ends inside the target must be refused")
    check(not items.contains(SWEEP_NAME), "a refused sweep mesh item was recorded")
    # From the top the small box is swept first, and the large box takes the shared face together with
    # its exposed rim as one source, so both conform along the imprint
    part, face_id = find_end_face(solids, top=True)
    mesh_with_sweep(compound, solids, part, face_id)


def test_split_target():
    solids, compound = import_geometry("sweep_split_target.brep")
    check(compound is None and len(solids) == 1, "the imprinted box must import as a single part")
    check(solids[0].get_face_data().count == 7, "the top of the box must be split into two faces")
    part, face_id = find_end_face(solids, top=False)
    mesh_with_sweep(part, solids, part, face_id)


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_chain_of_three()
    test_small_block_on_large()
    test_split_target()
    print("TEST PASSED")


main()
