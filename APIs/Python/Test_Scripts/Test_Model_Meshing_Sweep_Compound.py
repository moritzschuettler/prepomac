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
#  - sweep_side2.brep and sweep_grid4.brep: boxes standing side by side, sharing walls. One selected face
#    sweeps them all, and the shared walls carry one layer count.
#  - sweep_base_two.brep: two boxes side by side on a base. From the top of one block the other is found
#    across the shared wall, and the base is fed by both caps before it is swept. From the base the line
#    between the two blocks is an imprint inside the base's target, so the hand-off is refused.
#  - sweep_perpendicular.brep: two boxes sharing a wall, swept in perpendicular directions from two
#    selected faces. The shared wall is one transfinite grid that each block walks along its own family
#    of lines, and each block's layer count is the other's cross-edge count on that wall.
#
#A sweep item given a part selection finds its source surfaces itself. The last tests sweep each of the
#compounds above and a single imprinted box that way, including the two whose hand-off is refused from
#one end: the detection has to pick the other end. Two boxes with a bore each, the bores perpendicular,
#are the case a wall with a hole must be refused on: side by side each box is swept along its own bore
#across their shared wall; stacked, the upper box has no sweepable direction at all.
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
    nodes_before = pmx.model.nodes.count
    meshed = mesh_with_sweep(compound, solids, part, face_id)
    # The blocks share their interface nodes, so the mesh grew by fewer nodes than the parts add up to
    check(pmx.model.nodes.count - nodes_before < sum(part.node_count for part in meshed),
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


def test_side_by_side(file_name, count):
    solids, compound = import_geometry(file_name)
    check(compound is not None and len(solids) == count, "%s must import as a compound of %d" % (file_name, count))
    # One face on one block sweeps every block alongside it, and every block shares the layer count
    part, face_id = find_end_face(solids, top=False)
    nodes_before = pmx.model.nodes.count
    meshed = mesh_with_sweep(compound, solids, part, face_id)
    check(len(set(part.element_count for part in meshed)) == 1,
          "equal blocks swept side by side must carry the same number of elements")
    check(pmx.model.nodes.count - nodes_before < sum(part.node_count for part in meshed),
          "blocks swept side by side do not share the nodes of their common wall")


def test_two_blocks_on_a_base():
    solids, compound = import_geometry("sweep_base_two.brep")
    check(compound is not None and len(solids) == 3, "the base with two blocks must import as a compound of 3")
    # From the top of one block the other is swept alongside it, and the base collects both their caps,
    # which meet on the grid of the wall the two blocks share
    part, face_id = find_end_face(solids, top=True)
    meshed = mesh_with_sweep(compound, solids, part, face_id)
    upper = [part for part in meshed if part.element_count == min(p.element_count for p in meshed)]
    check(len(upper) == 2, "the two blocks on the base must carry the same number of elements")
    # From the base the line between the two blocks lies inside the base's target; the cap the base
    # leaves cannot match the nodes the shared upper wall starts from, so the item is refused
    part, face_id = find_end_face(solids, top=False)
    items = pmx.model.mesh.setup_items
    check_raises(lambda: items.add_sweep_mesh(SWEEP_NAME, face_selection(part, face_id)),
                 "a hand-off through shared faces meeting inside the target must be refused")
    check(not items.contains(SWEEP_NAME), "a refused sweep mesh item was recorded")


def find_face(part, axis, value):
    """The face of a part whose centroid has the given coordinate on the given axis."""
    faces = part.get_face_data()
    ids = list(faces.ids)
    centroids = list(faces.centroids)
    for i in range(faces.count):
        if abs(list(centroids[i])[axis] - value) < 1e-6:
            return ids[i]
    raise Exception("TEST FAILED: no face of " + part.name + " has coordinate %g on axis %d" % (value, axis))


def test_perpendicular_directions():
    solids, compound = import_geometry("sweep_perpendicular.brep")
    check(compound is not None and len(solids) == 2, "the two boxes must import as a compound of 2")
    lower = [part for part in solids if list(part.center_of_mass)[1] < 0][0]
    upper = [part for part in solids if list(part.center_of_mass)[1] > 0][0]
    # The lower box is swept along z from its bottom face, the upper one along x from its x = 0 face; the
    # wall at y = 0 is shared and carries both families of sweep lines
    selection = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces',
                                            {lower.id: [find_face(lower, 2, 0.0)], upper.id: [find_face(upper, 0, 0.0)]})
    items = pmx.model.mesh.setup_items
    items.add_sweep_mesh(SWEEP_NAME, selection)
    check(items[SWEEP_NAME].valid, "a sweep with perpendicular directions across a shared wall must be valid")
    nodes_before = pmx.model.nodes.count
    report = pmx.model.mesh.create(compound.name)
    check(report[compound.name] == "", "meshing the perpendicular sweep reported: " + report[compound.name])
    meshed = [part for part in pmx.model.parts if part.name in (lower.name, upper.name)]
    check(len(meshed) == 2 and all(part.element_count > 0 for part in meshed), "both boxes must be meshed")
    check(pmx.model.nodes.count - nodes_before < sum(part.node_count for part in meshed),
          "blocks swept in perpendicular directions do not share the nodes of their common wall")
    for part in meshed:
        print("meshed:", part.name, part.node_count, "nodes", part.element_count, "elements")
    items.remove([SWEEP_NAME])


def test_automatic_sources():
    items = pmx.model.mesh.setup_items
    for file_name in ("sweep_stack3.brep", "sweep_stack_small.brep", "sweep_grid4.brep", "sweep_base_two.brep",
                      "sweep_perpendicular.brep", "sweep_split_target.brep"):
        solids, compound = import_geometry(file_name)
        part_to_mesh = compound if compound is not None else solids[0]
        selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part_to_mesh.id])
        items.add_sweep_mesh(SWEEP_NAME, selection)
        check(items[SWEEP_NAME].valid, "the automatic sweep of " + file_name + " is not valid")
        report = pmx.model.mesh.create(part_to_mesh.name)
        check(report[part_to_mesh.name] == "", "the automatic sweep of " + file_name + " reported: " + report[part_to_mesh.name])
        names = set(solid.name for solid in solids)
        meshed = [part for part in pmx.model.parts if part.name in names]
        check(len(meshed) == len(solids) and all(part.element_count > 0 for part in meshed),
              "the automatic sweep of " + file_name + " did not mesh every solid")
        print("automatic:", file_name, [(part.name, part.element_count) for part in meshed])
        items.remove([SWEEP_NAME])


def test_perpendicular_bores():
    items = pmx.model.mesh.setup_items
    # Side by side: each box is swept along its own bore, perpendicular to the other, across the shared wall
    solids, compound = import_geometry("sweep_holes_side.brep")
    selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [compound.id])
    items.add_sweep_mesh(SWEEP_NAME, selection)
    check(items[SWEEP_NAME].valid, "the automatic sweep of the boxes with perpendicular bores is not valid")
    report = pmx.model.mesh.create(compound.name)
    check(report[compound.name] == "", "the boxes with perpendicular bores reported: " + report[compound.name])
    names = set(solid.name for solid in solids)
    meshed = [part for part in pmx.model.parts if part.name in names]
    check(len(meshed) == 2 and all(part.element_count > 0 for part in meshed), "both bored boxes must be meshed")
    print("automatic:", "sweep_holes_side.brep", [(part.name, part.element_count) for part in meshed])
    items.remove([SWEEP_NAME])
    # Stacked: the upper box is bored across every direction the lower box hands on, so no wall of it is
    # free of a hole, and the item is refused rather than recorded and failed by gmsh
    solids, compound = import_geometry("sweep_holes_stacked.brep")
    selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [compound.id])
    check_raises(lambda: items.add_sweep_mesh(SWEEP_NAME, selection),
                 "a sweep whose only walls carry a bore must be refused")
    check(not items.contains(SWEEP_NAME), "a refused automatic sweep item was recorded")


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_chain_of_three()
    test_small_block_on_large()
    test_split_target()
    test_side_by_side("sweep_side2.brep", 2)
    test_side_by_side("sweep_grid4.brep", 4)
    test_two_blocks_on_a_base()
    test_perpendicular_directions()
    test_automatic_sources()
    test_perpendicular_bores()
    print("TEST PASSED")


main()
