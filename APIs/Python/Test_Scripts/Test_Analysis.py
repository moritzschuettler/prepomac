#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Builds a complete analysis from a mesh, solves it, opens the results and reads a value. This is the
#end to end check of the API: material, section, step, boundary condition, load, solve, result. It is
#also the only test with a model and a result open at the same time, so it covers a selection that
#names one view while another is displayed.
#A meshed model must be open before the script runs, and a CalculiX executable must be configured.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Analysis.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"
JOB = PREFIX + "Job"
STEP = PREFIX + "Static"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def clean():
    for collection in [pmx.jobs, pmx.model.steps, pmx.model.sections, pmx.model.materials,
                       pmx.model.node_sets]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def build_model():
    part_name = pmx.model.parts.first().name
    part_selection = pmx.create.selection_by_ids('femodel', 'elements', 'parts', [pmx.model.parts.first().id])
    #
    pmx.model.materials.add(PREFIX + "Steel",
                            [pmx.create.elastic(210000, 0.3), pmx.create.density(7.85e-9)])
    pmx.model.sections.add_solid(PREFIX + "Section", PREFIX + "Steel", part_selection)
    # The cube is a single hexahedron: nodes 1 to 4 are the bottom face, 5 to 8 the top one
    pmx.model.node_sets.add(PREFIX + "Bottom",
                            pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))
    #
    step = pmx.model.steps.add_static(STEP)
    step.bcs.add_fixed(PREFIX + "Fix", pmx.create.region('NodeSetName', PREFIX + "Bottom"))
    step.loads.add_concentrated_force(PREFIX + "Force",
                                      pmx.create.selection_by_ids('femodel', 'nodes', 'nodes',
                                                                  [5, 6, 7, 8]),
                                      f3=-1000)
    return part_name


def test_job_collection():
    check(pmx.jobs is not None, "the analyses must be reachable as pmx.jobs")
    count_before = len(pmx.jobs)
    job = pmx.jobs.add(JOB)
    check(len(pmx.jobs) == count_before + 1, "the analysis was not added")
    check(job.name == JOB, "unexpected analysis name " + job.name)
    check(job.fem_solver == "Calculix", "unexpected solver " + job.fem_solver)
    check(len(job.executable) > 0, "the analysis must take the solver from the settings")
    check(len(job.work_directory) > 0, "the analysis must take the work directory from the settings")
    # The argument names the input file and defaults to the analysis name
    check(job.argument == JOB, "unexpected argument " + job.argument)
    check(job.status == "None", "a new analysis has not run yet, got " + job.status)
    check_raises(lambda: pmx.jobs.add(JOB), "a duplicate analysis name must be rejected")
    check_raises(lambda: pmx.jobs.add(PREFIX + "Bad", fem_solver="NotASolver"),
                 "an unknown solver must be rejected")
    check_raises(lambda: pmx.jobs["NoSuchJob"], "an unknown analysis name must be rejected")
    # Collection verbs
    check(pmx.jobs.contains(JOB), "the analysis is missing")
    check(len([j for j in pmx.jobs]) == len(pmx.jobs), "iteration failed")
    pmx.jobs.rename(JOB, JOB + "_R")
    check(pmx.jobs.contains(JOB + "_R"), "the analysis was not renamed")
    pmx.jobs.rename(JOB + "_R", JOB)
    # An analysis cannot be activated or deactivated
    check_raises(lambda: pmx.jobs.deactivate([JOB]),
                 "deactivating an analysis must report that it is not supported")
    # A timeout needs a blocking run
    check_raises(lambda: pmx.jobs[JOB].run(wait=False, timeout=10),
                 "a timeout without wait must be rejected")
    check_raises(lambda: pmx.jobs[JOB].run(timeout=-1), "a negative timeout must be rejected")


def test_check_model():
    job = pmx.jobs[JOB]
    status = job.check_model(timeout=120)
    check(status == "OK", "the model check did not succeed, got " + status + "\n" + job.get_output())


def test_run():
    job = pmx.jobs[JOB]
    status = job.run(timeout=300)
    check(status == "OK", "the analysis did not succeed, got " + status + "\n" + job.get_output())
    check(job.status == "OK", "unexpected job status " + job.status)
    check(len(job.get_output()) > 0, "the solver wrote no output")


def test_results():
    pmx.jobs.open_results(JOB)
    names = list(pmx.results.get_names())
    check(len(names) > 0, "no results were opened")
    result = pmx.results.current
    check(result is not None, "there is no current result")
    # The bottom face is fixed, so its displacement is zero and the top face has moved down
    step_id = list(result.steps.get_ids())[0]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    fixed = result.get_nodal_value(step_id, increment_id, 'DISP', 'U3', 1)
    loaded = result.get_nodal_value(step_id, increment_id, 'DISP', 'U3', 5)
    check(abs(fixed) < 1e-12, "the fixed node must not move, got " + str(fixed))
    check(loaded < 0, "the loaded node must move down, got " + str(loaded))
    # A single point is a plain list, and the deformed one is the undeformed one moved by the displacement
    undeformed = result.get_nodal_coordinates(5)
    deformed = result.get_deformed_nodal_coordinates(step_id, increment_id, 5)
    check(type(undeformed) is list and type(deformed) is list,
          "nodal coordinates must be lists: " + type(undeformed).__name__ + ", " + type(deformed).__name__)
    check(len(undeformed) == 3 and len(deformed) == 3, "nodal coordinates must have 3 values")
    for axis, component in enumerate(('U1', 'U2', 'U3')):
        moved = result.get_nodal_value(step_id, increment_id, 'DISP', component, 5)
        check(abs(deformed[axis] - undeformed[axis] - moved) < 1e-5 * (1 + abs(undeformed[axis])),
              "the deformed coordinates " + str(deformed) + " are not the undeformed " + str(undeformed) +
              " moved by the displacement")
    print("U3 of the loaded node:", loaded)


def test_selection_view_activity():
    # The model and the result share the part ID, so a selection that names the result view must not
    # be filtered by the part activity of the model view that the preceding command left displayed
    part_name = pmx.model.parts.get_names()[0]
    pmx.model.parts.deactivate(part_name)
    try:
        check(not pmx.model.parts[part_name].active, "the model part was not deactivated")
        # The last call was a model command, so the model view is the displayed one
        ids = list(pmx.create.selection_by_pick('results', 'nodes', [0, 0, 0], 'Node').get_ids())
        check(len(ids) == 1, "the result pick found " + str(ids) + " while the model view was displayed")
        all_ids = list(pmx.create.selection_all('results', 'nodes').get_ids())
        check(len(all_ids) == 8, "the result select all found " + str(len(all_ids)) + " nodes")
        # The filter still applies in the view the selection actually names
        pmx.model.parts.deactivate(part_name)
        model_ids = list(pmx.create.selection_all('femodel', 'nodes').get_ids())
        check(len(model_ids) == 0,
              "an inactive model part must contribute no nodes, found " + str(len(model_ids)))
    finally:
        pmx.model.parts.activate(part_name)
    check(pmx.model.parts[part_name].active, "the model part was not reactivated")


def test_result_outputs():
    # The single call forms of 1.10, next to the create plus add pair they supersede
    outputs = pmx.results.current.field_outputs
    count_before = len(list(outputs.get_names()))
    outputs.add_equation(PREFIX + "FO", "DISP.U3 * 2")
    check(outputs.contains(PREFIX + "FO"), "the equation field output was not added in one call")
    # The two call form still works, so existing scripts keep running
    draft = outputs.create_field_output_equation(PREFIX + "FO2", "DISP.U3 * 3")
    outputs.add(draft)
    check(outputs.contains(PREFIX + "FO2"), "the draft form must keep working")
    check(len(list(outputs.get_names())) == count_before + 2, "both field outputs must be present")
    # A replace in one call re-specifies the definition, because a result draft is built whole
    outputs.replace_equation(PREFIX + "FO", PREFIX + "FO", "DISP.U3 * 4")
    check(outputs.contains(PREFIX + "FO"), "the equation field output was not replaced")
    outputs.remove([PREFIX + "FO", PREFIX + "FO2"])


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    build_model()
    test_job_collection()
    test_check_model()
    test_run()
    test_results()
    test_selection_view_activity()
    test_result_outputs()
    print("TEST PASSED")


main()
