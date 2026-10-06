#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#The single workflow check: an imported geometry is meshed, given a material, a section, a step, a
#boundary condition and a load, solved, read back, written as a solver input file and saved as a
#.pmx. It is the one script that exercises the whole chain rather than one surface of it.
#A geometry must be open before the script runs, and a CalculiX executable must be configured.
#
#  PrePoMax.com -f cube.stl -u MM_TON_S_C --script Test_Workflow_Geometry_To_Result.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import io
import os
import tempfile

PREFIX = "PyTest_Flow_"
JOB = PREFIX + "Job"
STEP = PREFIX + "Static"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def clean():
    for collection in [pmx.jobs, pmx.model.steps, pmx.model.sections, pmx.model.materials,
                       pmx.model.node_sets, pmx.model.mesh.setup_items]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def plain_metadata_value(value):
    if value is None or isinstance(value, (bool, int, float, str)):
        return value
    try:
        return tuple(plain_metadata_value(item) for item in value)
    except TypeError:
        return str(value)


def metadata_signature(part):
    metadata = part.get_geometry_metadata()
    if not metadata.available:
        return None
    entities = []
    for kind in metadata.entity_kinds:
        for entity in metadata.get_entities(kind):
            properties = tuple((name, plain_metadata_value(entity.get_property(name)))
                               for name in entity.property_names)
            entities.append((kind, entity.id, properties))
    return (metadata.format_version, metadata.coordinate_system, metadata.length_unit, tuple(entities))


def mesh_the_geometry():
    parts = pmx.model.geometry.parts
    check(len(parts) > 0, "a geometry part is required")
    part = parts.first()
    geometry_metadata = metadata_signature(part)
    selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])
    pmx.model.mesh.setup_items.add_meshing_parameters(PREFIX + "Parameters", selection, max_size=0.5)
    pmx.model.mesh.generate(part.name)
    check(len(pmx.model.parts) > 0, "the geometry was not meshed into a model part")
    mesh_part = pmx.model.parts.first()
    check(mesh_part.element_count > 0, "the meshed part has no elements")
    if geometry_metadata is not None:
        check(metadata_signature(mesh_part) == geometry_metadata,
              "supplemental CAD metadata did not reach the model mesh unchanged")
    return mesh_part, geometry_metadata


def build_node_sets(mesh_part):
    # The face node sets are found from the coordinates, so the test does not depend on the numbering
    # a particular mesher happens to produce
    box = mesh_part.get_bounding_box()
    z_min, z_max = box[4], box[5]
    tolerance = (z_max - z_min) * 1e-6
    data = pmx.model.nodes.get_data()
    ids = list(data.ids)
    coordinates = data.coordinates
    bottom = []
    top = []
    for index in range(len(ids)):
        z = coordinates[index][2]
        if z <= z_min + tolerance:
            bottom.append(ids[index])
        elif z >= z_max - tolerance:
            top.append(ids[index])
    check(len(bottom) > 0 and len(top) > 0,
          "the bounding box did not separate the bottom and the top nodes")
    pmx.model.node_sets.add(PREFIX + "Bottom",
                            pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', bottom))
    pmx.model.node_sets.add(PREFIX + "Top",
                            pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', top))
    return bottom, top


def build_analysis(mesh_part):
    part_selection = pmx.create.selection_by_ids('femodel', 'elements', 'parts', [mesh_part.id])
    pmx.model.materials.add(PREFIX + "Steel",
                            [pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])
    pmx.model.sections.add_solid(PREFIX + "Section", PREFIX + "Steel", part_selection)
    step = pmx.model.steps.add_static(STEP, time_period=1.0)
    step.bcs.add_fixed(PREFIX + "Fix", pmx.create.region('NodeSetName', PREFIX + "Bottom"))
    step.loads.add_concentrated_force(PREFIX + "Force",
                                      pmx.create.region('NodeSetName', PREFIX + "Top"), f3=-1000)


def test_solver_input(file_name):
    # The written input file must carry what the definition asked for, which is the round trip the
    # journal will eventually have to reproduce
    pmx.export_to_calculix(file_name)
    check(os.path.isfile(file_name), "the solver input file was not written")
    with io.open(file_name, encoding="utf-8", errors="replace") as reader:
        text = reader.read()
    for expected in ["*MATERIAL", "*ELASTIC", "*SOLID SECTION", "*STATIC", "*BOUNDARY", "*CLOAD"]:
        check(expected in text.upper(), "the solver input file is missing " + expected)
    check(PREFIX.upper() + "STEEL" in text.upper(), "the material name did not reach the input file")


def test_solve_and_read(bottom, top, expected_metadata):
    pmx.jobs.add(JOB)
    status = pmx.jobs[JOB].run(timeout=300)
    check(status == "OK", "the analysis did not succeed, got " + status + "\n" + pmx.jobs[JOB].get_output())
    #
    pmx.jobs.open_results(JOB)
    result = pmx.results.current
    check(result is not None, "there is no current result")
    if expected_metadata is not None:
        result_part = result.mesh.parts.first()
        check(metadata_signature(result_part) == expected_metadata,
              "supplemental CAD metadata did not reach the result mesh unchanged")
    step_id = list(result.steps.get_ids())[0]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    fixed = result.get_nodal_value(step_id, increment_id, 'DISP', 'U3', bottom[0])
    loaded = result.get_nodal_value(step_id, increment_id, 'DISP', 'U3', top[0])
    check(abs(fixed) < 1e-9, "the fixed node must not move, got " + str(fixed))
    check(loaded < 0, "the loaded node must move down, got " + str(loaded))
    print("U3 of the loaded node:", loaded)


def test_save():
    file_name = os.path.join(tempfile.gettempdir(), PREFIX + "model.pmx")
    if os.path.isfile(file_name):
        os.remove(file_name)
    pmx.save(file_name)
    check(os.path.isfile(file_name), "the model was not saved")
    check(os.path.getsize(file_name) > 0, "the saved model is empty")
    os.remove(file_name)


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    mesh_part, geometry_metadata = mesh_the_geometry()
    bottom, top = build_node_sets(mesh_part)
    build_analysis(mesh_part)
    test_solver_input(os.path.join(tempfile.gettempdir(), PREFIX + "model.inp"))
    test_solve_and_read(bottom, top, geometry_metadata)
    test_save()
    print("TEST PASSED")


main()
