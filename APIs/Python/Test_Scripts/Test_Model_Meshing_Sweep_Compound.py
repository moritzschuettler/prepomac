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
#    the target of the large box; the box is a prism, so the split is carried to its bottom and the
#    sweep lines under the imprint are pinned to it, and the small box is fed from the cap.
#  - sweep_split_target.brep: a single box whose top is split by an imprint into an inner face and a rim.
#    The target may be split, and the cap has to land on both faces.
#  - sweep_rings.brep: a single box whose walls are split by two loops around it, so that the walls stand
#    in three rings along the sweep and the middle ring touches neither the source nor the target.
#  - sweep_half_loop.brep: a single box with one wall split by a line partway up and no loop around the
#    body, so that the walls beside the split have direction sides of two edges with no ring to match.
#  - sweep_pipe_wall.brep: a quarter of a pipe wall, which the detection has to sweep through its thickness,
#    from one of the cylinders, rather than along its length from an end.
#  - sweep_collar.brep: a post through a collar plate around its middle. The collar is swept through its
#    thickness alongside the middle ring of the post's walls, its short chains in one family with the long ones.
#  - sweep_side2.brep and sweep_grid4.brep: boxes standing side by side, sharing walls. One selected face
#    sweeps them all, and the shared walls carry one layer count.
#  - sweep_base_two.brep: two boxes side by side on a base. From the top of one block the other is found
#    across the shared wall, and the base is fed by both caps before it is swept. From the base the line
#    between the two blocks is an imprint inside the base's target, carried to the base's bottom and
#    pinned, so both caps end on the nodes the shared upper wall starts from.
#  - sweep_perpendicular.brep: two boxes sharing a wall, swept in perpendicular directions from two
#    selected faces. The shared wall is one transfinite grid that each block walks along its own family
#    of lines, and each block's layer count is the other's cross-edge count on that wall.
#
#A sweep item given a part selection finds its source surfaces itself. The last tests sweep each of the
#compounds above and a single imprinted box that way. Two boxes with a bore each, the bores perpendicular,
#are the case a wall with a hole must be refused on: side by side each box is swept along its own bore
#across their shared wall; stacked, the upper box has no sweepable direction at all.
#
#One item may also sweep parts that share nothing at all. sweep_two_apart.brep holds two boxes standing
#apart, which import as two separate parts rather than as a compound; they are two groups of blocks in
#one item, selected either by a source surface on each or by naming both parts, and both parts are
#meshed in one call.
#
#sweep_plate_both.brep is a plate with a smaller block above and a different one below, footprints offset
#and overlapping, so no orientation avoids an imprint inside a target: the plate carries both imprints to
#its other end, where they cross, and every block is swept along pinned lines. sweep_plate_overhang.brep
#has the upper block reaching past an edge of the plate. Both are swept from either end and automatically.
#
#sweep_bent_plate.brep is a plate bent along a cylinder, with a bore through each of its two wings. Its
#front face is a plane, the cylinder of the bend and a plane again, and the bores make every other face a
#wall with a hole, so no single face of it is a sweep source. The detection has to try the faces that
#continue one another smoothly - the group one click of the angle picker selects - because the extension
#over coplanar surfaces does not reach across a bend. Both of its ends are split by the tangency lines of
#the bend, which lie inside the target and not on a wall, so the mesh keeps every face of the part only
#when the lines between the two splits are pinned: the face count is what tells a lost tangency line,
#since the two faces it separates meet smoothly and nothing else would part them.
#
#sweep_spline_edges.brep is a lofted plate on a round post, which is how the same arrangement arrives from
#an imported CAD file: the plate's vertical edges run straight but the kernel holds them as splines rather
#than as lines, and the post's wall is one closed cylinder whose centre of mass lies on its axis and not on
#the wall. Swept from the plate's top the post's footprint ends inside the plate's target, so the split has
#to be carried to the source whatever type the edges carry, and every surface piece the carrying leaves has
#to be traced back to the face it came from, the closed cylinder included.
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
    # From the bottom the shared face ends inside the target of the large box. The block is a prism, so
    # the split is carried to its source and the sweep lines are pinned along it: the cap follows the
    # imprint and the small box is fed from it
    part, face_id = find_end_face(solids, top=False)
    nodes_before = pmx.model.nodes.count
    meshed = mesh_with_sweep(compound, solids, part, face_id)
    check(pmx.model.nodes.count - nodes_before < sum(part.node_count for part in meshed),
          "the small box fed through a pinned imprint does not share the interface nodes")
    # From the top the small box is swept first, and the large box takes the shared face together with
    # its exposed rim as one source, so both conform along the imprint
    part, face_id = find_end_face(solids, top=True)
    mesh_with_sweep(compound, solids, part, face_id)


def test_split_target():
    solids, compound = import_geometry("sweep_split_target.brep")
    check(compound is None and len(solids) == 1, "the imprinted box must import as a single part")
    check(solids[0].get_face_data().count == 7, "the top of the box must be split into two faces")
    part, face_id = find_end_face(solids, top=False)
    meshed = mesh_with_sweep(part, solids, part, face_id)
    # The split is carried to the bottom to pin the sweep lines, but the carried edges are scaffolding:
    # the mesh part has the faces of the geometry
    check(meshed[0].get_face_data().count == 7, "the carried imprint edges must not split the faces of the mesh")
    # Kept as edges of the mesh, they split the bottom face along the imprint
    items = pmx.model.mesh.setup_items
    items.add_sweep_mesh(SWEEP_NAME, face_selection(part, face_id), keep_imprint_edges=True)
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "meshing with kept imprint edges reported: " + report[part.name])
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part.name]
    check(meshed[0].get_face_data().count == 8, "the kept imprint edges must split the bottom face of the mesh")
    items.remove([SWEEP_NAME])


def test_wall_rings():
    # A single box whose walls are split by two loops around it: the walls stand in three rings and the
    # walls of the middle ring touch neither the source nor the target. One sweep runs through all three
    solids, compound = import_geometry("sweep_rings.brep")
    check(compound is None and len(solids) == 1 and solids[0].get_face_data().count == 14,
          "the ring-walled box must import as a single part with 14 faces")
    part, face_id = find_end_face(solids, top=False)
    meshed = mesh_with_sweep(part, solids, part, face_id)
    check(meshed[0].get_face_data().count == 14, "the mesh of the ring-walled box must keep its 14 faces")
    items = pmx.model.mesh.setup_items
    items.add_sweep_mesh(SWEEP_NAME, pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]))
    check(items[SWEEP_NAME].valid, "the automatic sweep of the ring-walled box is not valid")
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "the automatic sweep of the ring-walled box reported: " + report[part.name])
    items.remove([SWEEP_NAME])


def test_half_loop():
    # A single box with one wall split by a line partway up, and no loop around the body: the walls beside
    # the split run the whole height, so their direction sides are two edges each with no ring to match.
    # The walk from the source cannot describe it; the edges are followed from the source instead
    solids, compound = import_geometry("sweep_half_loop.brep")
    check(compound is None and len(solids) == 1 and solids[0].get_face_data().count == 7,
          "the half-loop box must import as a single part with 7 faces")
    part, face_id = find_end_face(solids, top=False)
    meshed = mesh_with_sweep(part, solids, part, face_id)
    check(meshed[0].get_face_data().count == 7, "the mesh of the half-loop box must keep its 7 faces")
    part, face_id = find_end_face(solids, top=True)
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
    # From the base the line between the two blocks lies inside the base's target; it is carried to the
    # base's bottom and pinned, so both caps end on the nodes the shared upper wall starts from
    part, face_id = find_end_face(solids, top=False)
    nodes_before = pmx.model.nodes.count
    meshed = mesh_with_sweep(compound, solids, part, face_id)
    check(pmx.model.nodes.count - nodes_before < sum(part.node_count for part in meshed),
          "the blocks fed through a pinned imprint do not share the interface nodes")


def test_plate_imprinted_on_both_sides():
    # A plate with a smaller block above and a different one below, footprints offset: no orientation
    # avoids an imprint inside a target, so every sweep of it needs a pinned imprint. The overhanging
    # variant has one block reaching past an edge of the plate.
    for file_name in ("sweep_plate_both.brep", "sweep_plate_overhang.brep"):
        solids, compound = import_geometry(file_name)
        check(compound is not None and len(solids) == 3, file_name + " must import as a compound of 3")
        for top in (False, True):
            part, face_id = find_end_face(solids, top=top)
            nodes_before = pmx.model.nodes.count
            meshed = mesh_with_sweep(compound, solids, part, face_id)
            check(pmx.model.nodes.count - nodes_before < sum(part.node_count for part in meshed),
                  file_name + " swept through pinned imprints does not share the interface nodes")
        items = pmx.model.mesh.setup_items
        items.add_sweep_mesh(SWEEP_NAME, pmx.create.selection_by_ids('geometry', 'parts', 'parts', [compound.id]))
        check(items[SWEEP_NAME].valid, "the automatic sweep of " + file_name + " is not valid")
        report = pmx.model.mesh.create(compound.name)
        check(report[compound.name] == "", "the automatic sweep of " + file_name + " reported: " + report[compound.name])
        items.remove([SWEEP_NAME])


def test_bent_plate():
    solids, compound = import_geometry("sweep_bent_plate.brep")
    check(compound is None and len(solids) == 1, "the bent plate must import as a single part")
    part = solids[0]
    face_count = part.get_face_data().count
    edge_count = part.get_edge_data().count
    items = pmx.model.mesh.setup_items
    # No single face sweeps the plate, so a source found by extending one face over its plane cannot exist
    for face_id in list(part.get_face_data().ids):
        check_raises(lambda: items.add_sweep_mesh(SWEEP_NAME, face_selection(part, int(face_id))),
                     "the face " + str(face_id) + " of the bent plate must not be a sweep source on its own")
    # The front face of the plate is a plane, the cylinder of the bend and a plane again
    items.add_sweep_mesh(SWEEP_NAME, pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]))
    check(items[SWEEP_NAME].valid, "the automatic sweep of the bent plate is not valid")
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "the automatic sweep of the bent plate reported: " + report[part.name])
    meshed = [meshPart for meshPart in pmx.model.parts if meshPart.name == part.name]
    check(len(meshed) == 1 and meshed[0].element_count > 0, "the bent plate was not meshed")
    print("meshed:", meshed[0].name, meshed[0].node_count, "nodes", meshed[0].element_count, "elements")
    # The tangency lines of the bend split both ends inside, so the swept mesh keeps them only when the
    # lines between them are pinned; without that the three faces of the target come back as one
    check(meshed[0].get_face_data().count == face_count,
          "the swept bent plate has %d faces instead of the %d of the geometry" %
          (meshed[0].get_face_data().count, face_count))
    check(meshed[0].get_edge_data().count == edge_count,
          "the swept bent plate has %d edges instead of the %d of the geometry" %
          (meshed[0].get_edge_data().count, edge_count))
    items.remove([SWEEP_NAME])


def test_spline_edges_and_round_post():
    solids, compound = import_geometry("sweep_spline_edges.brep")
    check(compound is not None and len(solids) == 2, "the plate and the post must import as a compound of 2")
    # From the top the plate is swept first and its cap has to follow the post's footprint; from the bottom
    # the post goes first and the plate takes the shared disc together with the rim around it as one source
    for top in (True, False):
        part, face_id = find_end_face(solids, top=top)
        nodes_before = pmx.model.nodes.count
        meshed = mesh_with_sweep(compound, solids, part, face_id)
        check(pmx.model.nodes.count - nodes_before < sum(part.node_count for part in meshed),
              "the plate and the post swept from the %s do not share the interface nodes" %
              ("top" if top else "bottom"))
    items = pmx.model.mesh.setup_items
    items.add_sweep_mesh(SWEEP_NAME, pmx.create.selection_by_ids('geometry', 'parts', 'parts', [compound.id]))
    check(items[SWEEP_NAME].valid, "the automatic sweep of sweep_spline_edges.brep is not valid")
    report = pmx.model.mesh.create(compound.name)
    check(report[compound.name] == "",
          "the automatic sweep of sweep_spline_edges.brep reported: " + report[compound.name])
    items.remove([SWEEP_NAME])


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


def test_post_through_collar():
    # A post through a collar plate around its middle. The collar shares the post's walls between its two
    # faces, so the post's walls stand in three rings and the collar is swept through its thickness alongside
    # the middle ring: its short chains and the post's long ones are one family, and the collar's faces mark
    # two levels of it. Swept from the post's end and the collar's top, and automatically
    solids, compound = import_geometry("sweep_collar.brep")
    check(compound is not None and len(solids) == 2, "the post and the collar must import as a compound of 2")
    post = max(solids, key=lambda solid: solid.get_face_data().count)
    collar = [solid for solid in solids if solid is not post][0]
    post_bottom = find_end_face([post], top=False)[1]
    collar_top = find_end_face([collar], top=True)[1]
    items = pmx.model.mesh.setup_items
    selection = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces',
                                            {post.id: [post_bottom], collar.id: [collar_top]})
    items.add_sweep_mesh(SWEEP_NAME, selection)
    check(items[SWEEP_NAME].valid, "the sweep of the post and the collar from two sources is not valid")
    nodes_before = pmx.model.nodes.count
    report = pmx.model.mesh.create(compound.name)
    check(report[compound.name] == "", "the sweep of the post and the collar reported: " + report[compound.name])
    names = set(solid.name for solid in solids)
    meshed = [part for part in pmx.model.parts if part.name in names]
    check(len(meshed) == 2 and pmx.model.nodes.count - nodes_before < sum(part.node_count for part in meshed),
          "the post and the collar do not share their interface nodes")
    items.remove([SWEEP_NAME])
    items.add_sweep_mesh(SWEEP_NAME, pmx.create.selection_by_ids('geometry', 'parts', 'parts', [compound.id]))
    check(items[SWEEP_NAME].valid, "the automatic sweep of the post and the collar is not valid")
    report = pmx.model.mesh.create(compound.name)
    check(report[compound.name] == "", "the automatic sweep of the post and the collar reported: " + report[compound.name])
    items.remove([SWEEP_NAME])


def test_constant_thickness_source():
    # A quarter of a pipe wall, 5 thick, 100 long: every face is a valid source, the two end planes give a
    # long sweep of many layers and the two cylinders a sweep through the thickness. The detection has to
    # prefer a cylinder, whose target is an offset of it
    solids, compound = import_geometry("sweep_pipe_wall.brep")
    check(compound is None and len(solids) == 1, "the pipe wall must import as a single part")
    part = solids[0]
    faces = part.get_face_data()
    cylinder_ids = [int(list(faces.ids)[i]) for i in range(faces.count) if list(faces.types)[i] == "Cylinder"]
    check(len(cylinder_ids) == 2, "the pipe wall must have two cylindrical faces")
    counts = []
    for face_id in cylinder_ids:
        meshed = mesh_with_sweep(part, solids, part, face_id)
        counts.append(meshed[0].element_count)
    items = pmx.model.mesh.setup_items
    items.add_sweep_mesh(SWEEP_NAME, pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]))
    check(items[SWEEP_NAME].valid, "the automatic sweep of the pipe wall is not valid")
    report = pmx.model.mesh.create(part.name)
    check(report[part.name] == "", "the automatic sweep of the pipe wall reported: " + report[part.name])
    meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part.name]
    check(meshed[0].element_count in counts, "the automatic sweep of the pipe wall did not start from a cylinder")
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


def test_two_parts_apart():
    solids, compound = import_geometry("sweep_two_apart.brep")
    check(compound is None and len(solids) == 2, "the two boxes standing apart must import as two separate parts")
    first, second = solids
    items = pmx.model.mesh.setup_items
    # One item with a source surface on each part: the parts are two groups swept independently of each
    # other, and each of them is meshed with the blocks of its own group
    selection = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces',
                                            {first.id: [find_face(first, 2, 0.0)],
                                             second.id: [find_face(second, 2, 0.0)]})
    items.add_sweep_mesh(SWEEP_NAME, selection)
    check(items[SWEEP_NAME].valid, "a sweep with a source surface on each of two separate parts must be valid")
    mesh_both(first, second, "the sweep of the two parts standing apart")
    items.remove([SWEEP_NAME])
    # The same two parts named by a part selection: every group finds its own source surfaces
    selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [first.id, second.id])
    items.add_sweep_mesh(SWEEP_NAME, selection)
    check(items[SWEEP_NAME].valid, "the automatic sweep of two separate parts must be valid")
    mesh_both(first, second, "the automatic sweep of the two parts standing apart")
    items.remove([SWEEP_NAME])


def mesh_both(first, second, what):
    """Meshes two parts in one call and checks that both of them carry elements."""
    report = pmx.model.mesh.create([first.name, second.name])
    for name in (first.name, second.name):
        check(report[name] == "", what + " reported for " + name + ": " + report[name])
    meshed = [part for part in pmx.model.parts if part.name in (first.name, second.name)]
    check(len(meshed) == 2 and all(part.element_count > 0 for part in meshed),
          what + " did not mesh both parts")
    for part in meshed:
        print("meshed:", part.name, part.node_count, "nodes", part.element_count, "elements")


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_chain_of_three()
    test_small_block_on_large()
    test_split_target()
    test_wall_rings()
    test_half_loop()
    test_side_by_side("sweep_side2.brep", 2)
    test_side_by_side("sweep_grid4.brep", 4)
    test_two_blocks_on_a_base()
    test_perpendicular_directions()
    test_automatic_sources()
    test_constant_thickness_source()
    test_post_through_collar()
    test_perpendicular_bores()
    test_two_parts_apart()
    test_plate_imprinted_on_both_sides()
    test_spline_edges_and_round_post()
    test_bent_plate()
    print("TEST PASSED")


main()
