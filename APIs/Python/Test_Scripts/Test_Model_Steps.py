#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the step collection: the 12 step types, the shared solution values, the step controls and the
#collection verbs. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Steps.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def check_refused(function, fragment, message):
    # The refusal must be the API's own, which names the argument that is wrong
    try:
        function()
    except Exception as ex:
        check(fragment in str(ex), message + ": " + str(ex)[0:200])
        return
    raise Exception("TEST FAILED: " + message)


def clean():
    names = [n for n in pmx.model.steps.get_names() if n.startswith(PREFIX)]
    if names:
        pmx.model.steps.remove(names)


def test_static_and_shared_values():
    steps = pmx.model.steps
    count_before = len(steps)
    step = steps.add_static(PREFIX + "S1", time_period=2.5, initial_time_increment=0.5,
                            min_time_increment=1e-4, max_time_increment=1.0, direct=True,
                            nlgeom=True, max_increments=50, incrementation_type="Automatic",
                            solver_type="Pardiso", output_frequency=2, run_analysis=False)
    check(len(steps) == count_before + 1, "the static step was not added")
    check(step.item_type == "static", "unexpected step type " + step.item_type)
    # The values shared by every step type are properties
    check(step.nlgeom, "nlgeom was not set")
    check(step.max_increments == 50, "max_increments was not set")
    check(step.incrementation_type == "Automatic", "unexpected incrementation type " + step.incrementation_type)
    check(step.solver_type == "Pardiso", "unexpected solver type " + step.solver_type)
    check(step.output_frequency == 2, "output_frequency was not set")
    check(not step.run_analysis, "run_analysis was not set")
    check(step.active, "a new step must be active")
    # The values of the step type are read by name
    names = list(step.get_value_names())
    check("time_period" in names, "time_period is missing from " + str(names))
    check(step.get_value("time_period") == 2.5, "unexpected time period " + str(step.get_value("time_period")))
    check(step.get_value("initial_time_increment") == 0.5, "initial_time_increment was not set")
    check(step.get_value("max_time_increment") == 1.0, "max_time_increment was not set")
    check(step.get_value("direct"), "direct was not set")
    check_raises(lambda: step.get_value("no_such_value"), "an unknown value name must be rejected")
    # A static step is never a perturbation step
    check_raises(lambda: pmx.model.steps.add_static(PREFIX + "Bad", perturbation=True),
                 "a perturbation static step must be rejected")
    # The enums are checked
    check_raises(lambda: pmx.model.steps.add_static(PREFIX + "Bad", solver_type="NotASolver"),
                 "an unknown solver type must be rejected")
    check_raises(lambda: pmx.model.steps.add_static(PREFIX + "Bad", incrementation_type="NotAType"),
                 "an unknown incrementation type must be rejected")
    check_raises(lambda: pmx.model.steps.add_static(PREFIX + "S1"), "a duplicate step name must be rejected")


def test_replace():
    steps = pmx.model.steps
    # A value that is not given keeps what the step already carries
    step = steps.replace_static(PREFIX + "S1", nlgeom=False)
    check(not step.nlgeom, "nlgeom was not replaced")
    check(step.get_value("time_period") == 2.5, "the time period must survive a replace")
    check(step.solver_type == "Pardiso", "the solver type must survive a replace")
    # A replace can rename
    step = steps.replace_static(PREFIX + "S1", name=PREFIX + "S1_R", time_period=3.0)
    check(steps.contains(PREFIX + "S1_R"), "the step was not renamed by the replace")
    check(not steps.contains(PREFIX + "S1"), "the old step name still exists")
    check(step.get_value("time_period") == 3.0, "the time period was not replaced")
    # The step type cannot change on a replace
    check_raises(lambda: steps.replace_frequency(PREFIX + "S1_R"),
                 "replacing a static step by a frequency step must be rejected")
    steps.rename(PREFIX + "S1_R", PREFIX + "S1")


def test_collection_verbs():
    steps = pmx.model.steps
    name = PREFIX + "S1"
    check(steps[name].name == name, "indexing by name failed")
    check(steps.get(name).name == name, "get failed")
    check(name in list(steps.get_names()), "the step is missing from get_names")
    check(len([s for s in steps]) == len(steps), "iteration failed")
    check_raises(lambda: steps["NoSuchStep"], "an unknown step name must be rejected")
    # Duplicate and remove
    count_before = len(steps)
    steps.duplicate([name])
    check(len(steps) == count_before + 1, "the step was not duplicated")
    duplicated = [n for n in steps.get_names() if n.startswith(PREFIX) and n != name]
    steps.remove(duplicated)
    check(len(steps) == count_before, "the duplicated step was not removed")
    # A name given twice, in any case, is refused: the controller would remove the removed step again
    for verb in [steps.remove, steps.duplicate, steps.deactivate]:
        check_refused(lambda: verb([name, name.lower()]), "'names'", "a step named twice must be refused")
    check(len(steps) == count_before and steps[name].active, "a refused call with a repeated name changed the steps")
    # Activate and deactivate
    steps.deactivate([name])
    check(not steps[name].active, "the step was not deactivated")
    steps.activate([name])
    check(steps[name].active, "the step was not activated")
    # The model keys its steps without regard to case, and so does the name check
    steps.add_static(PREFIX + "S2")
    message = raised_message(lambda: steps.rename(name, (PREFIX + "S2").lower()))
    check(message is not None and "already exists" in message,
          "a rename onto another step in other case must be refused: " + str(message))
    check(steps[name].name == name, "the refused rename changed the name to " + steps[name].name)
    message = raised_message(lambda: steps.add_static(name.upper()))
    check(message is not None and "already exists" in message,
          "a step named like another in other case must be refused: " + str(message))
    # The model would trim surrounding spaces and throw on a space inside, so both are refused naming the argument
    for bad_name in [" " + name + " ", PREFIX + "Bad Name"]:
        message = raised_message(lambda: steps.add_static(bad_name))
        check(message is not None and "'name'" in message,
              "the step name " + repr(bad_name) + " must be refused: " + str(message))
    message = raised_message(lambda: steps.replace_static(name, name=PREFIX + "Bad Name"))
    check(message is not None and "'name'" in message, "a replace to a bad name must be refused: " + str(message))
    check(steps.contains(name) and not steps.contains(PREFIX + "Bad Name"), "a refused step name was recorded")
    steps.replace_static(name, name=name.upper())
    check(steps[name.upper()].name == name.upper(), "a rename to the same name in other case must be allowed")
    steps.rename(name.upper(), name)
    steps.remove([PREFIX + "S2"])


def test_case_insensitive_names():
    steps = pmx.model.steps
    name = PREFIX + "S1"
    other_case = name.lower()
    # The model keys every other collection without regard to case, so the step is found the same way and
    # carries its own name on to the controller, which matches a step name exactly
    check(steps[other_case].name == name, "a step looked up in other case must carry its own name")
    check(steps[other_case].loads.step_name == name, "a step collection must carry the step name the model uses")
    period = steps[name].get_value("time_period")
    step = steps.replace_static(other_case, time_period=period + 1.0)
    check(step.name == name, "a replace by a name in other case renamed the step to " + step.name)
    check(steps[name].get_value("time_period") == period + 1.0, "a replace by a name in other case was not applied")
    steps.replace_static(name, time_period=period)
    count_before = len(steps)
    steps.duplicate([other_case])
    check(len(steps) == count_before + 1, "a duplicate by a name in other case was not applied")
    duplicated = [n for n in steps.get_names() if n.startswith(PREFIX) and n not in (name, )]
    steps.remove([n.upper() for n in duplicated])
    check(len(steps) == count_before, "a remove by names in other case was not applied")
    steps.deactivate([other_case])
    check(not steps[name].active, "a deactivate by a name in other case was not applied")
    steps.activate([other_case])
    check(steps[name].active, "an activate by a name in other case was not applied")
    steps[other_case].set_controls(pmx.create.reset_control())
    check(list(steps[name].get_control_types()) == ["reset_control"],
          "step controls set through a name in other case were not applied")
    steps[name].clear_controls()


def test_refused_values():
    steps = pmx.model.steps
    bad = PREFIX + "Bad"
    count_before = len(steps)
    # A value the model would clamp, or refuse without naming the argument, is refused naming it
    refusals = [(lambda: steps.add_static(bad, max_increments=0), "'max_increments'"),
                (lambda: steps.add_static(bad, output_frequency=-1), "'output_frequency'"),
                (lambda: steps.add_static(bad, time_period=0), "'time_period'"),
                (lambda: steps.add_static(bad, initial_time_increment=-1.0), "'initial_time_increment'"),
                (lambda: steps.add_static(bad, time_period=1.0, initial_time_increment=2.0),
                 "'initial_time_increment'"),
                (lambda: steps.add_static(bad, time_period=1.0, min_time_increment=2.0), "'min_time_increment'"),
                (lambda: steps.add_dynamic(bad, alpha=0.1), "'alpha'"),
                (lambda: steps.add_dynamic(bad, alpha=-0.5), "'alpha'"),
                (lambda: steps.add_modal_dynamics(bad, relative_error=-0.1), "'relative_error'"),
                (lambda: steps.add_modal_dynamics(bad, relative_error=2.0), "'relative_error'"),
                (lambda: steps.add_steady_state_dynamics(bad, number_of_data_points=1), "'number_of_data_points'"),
                (lambda: steps.add_steady_state_dynamics(bad, bias=0.5), "'bias'"),
                (lambda: steps.add_steady_state_dynamics(bad, number_of_fourier_terms=0),
                 "'number_of_fourier_terms'"),
                (lambda: steps.add_steady_state_dynamics(bad, lower_frequency=50.0, upper_frequency=10.0),
                 "'lower_frequency'"),
                (lambda: steps.add_frequency(bad, number_of_frequencies=0), "'number_of_frequencies'"),
                (lambda: steps.add_frequency(bad, lower_frequency=-1.0), "'lower_frequency'"),
                (lambda: steps.add_frequency(bad, lower_frequency=100.0, upper_frequency=10.0), "'upper_frequency'"),
                (lambda: steps.add_frequency(bad, cyclic_symmetry_mode_min=-1), "'cyclic_symmetry_mode_min'"),
                (lambda: steps.add_frequency(bad, cyclic_symmetry_mode_min=3, cyclic_symmetry_mode_max=1),
                 "'cyclic_symmetry_mode_max'"),
                (lambda: steps.add_complex_frequency(bad, number_of_frequencies=0), "'number_of_frequencies'"),
                (lambda: steps.add_buckle(bad, number_of_buckling_factors=0), "'number_of_buckling_factors'")]
    for function, fragment in refusals:
        check_refused(function, fragment, "a value of " + fragment + " the model would change must be refused")
    check(len(steps) == count_before and not steps.contains(bad), "a refused step was recorded")
    # A replace checks the values it is given against the ones the step keeps
    name = PREFIX + "S1"
    stored = steps[name].get_value("time_period")
    check_refused(lambda: steps.replace_static(name, time_period=steps[name].get_value("initial_time_increment") / 10),
                  "'time_period'", "a time period below the stored initial increment must be refused")
    check(steps[name].get_value("time_period") == stored, "the refused replace changed the time period")
    check_refused(lambda: steps.replace_frequency(PREFIX + "F2", upper_frequency=1.0), "'upper_frequency'",
                  "an upper frequency below the stored lower one must be refused")
    step = steps.replace_frequency(PREFIX + "F2", cyclic_symmetry_mode_min=2, cyclic_symmetry_mode_max=4)
    check(step.get_value("cyclic_symmetry_mode_max") == 4, "the cyclic symmetry modes were not set")
    check_refused(lambda: steps.replace_frequency(PREFIX + "F2", cyclic_symmetry_mode_max=1),
                  "'cyclic_symmetry_mode_max'", "a mode maximum below the stored minimum must be refused")
    check(steps[PREFIX + "F2"].get_value("cyclic_symmetry_mode_min") == 2,
          "the refused replace moved the stored minimum mode")
    # An initial increment above the maximum one is stored as given, as the dialog does, and a replace that
    # sets the maximum, which would shorten it, keeps it
    initial = PREFIX + "S_Initial"
    step = steps.add_static(initial, time_period=1.0, initial_time_increment=0.5, max_time_increment=0.1)
    check(step.get_value("initial_time_increment") == 0.5 and step.get_value("max_time_increment") == 0.1,
          "an initial increment above the maximum one was not kept")
    step = steps.replace_static(initial, max_time_increment=0.2)
    check(step.get_value("initial_time_increment") == 0.5 and step.get_value("max_time_increment") == 0.2,
          "setting the maximum increment changed the kept initial one")
    step = steps.replace_static(initial, time_period=2.0)
    check(step.get_value("initial_time_increment") == 0.5, "a replace changed the kept initial increment")
    steps.remove([initial])
    # The step control parameters clamp a value outside their range
    control_refusals = [(lambda: pmx.create.time_incrementation_control(i0=0), "'i0'"),
                        (lambda: pmx.create.time_incrementation_control(dd=-1.0), "'dd'"),
                        (lambda: pmx.create.field_control(rna=-0.1), "'rna'"),
                        (lambda: pmx.create.contact_control(alea=1.5), "'alea'"),
                        (lambda: pmx.create.contact_control(kscalemax=0), "'kscalemax'")]
    for function, fragment in control_refusals:
        check_refused(function, fragment, "a step control value of " + fragment + " out of range must be refused")


def test_frequency_types():
    steps = pmx.model.steps
    step = steps.add_frequency(PREFIX + "F1", number_of_frequencies=20, lower_frequency=1.0,
                               upper_frequency=500.0, storage=True, perturbation=True)
    check(step.get_value("number_of_frequencies") == 20, "number_of_frequencies was not set")
    check(step.get_value("lower_frequency") == 1.0, "lower_frequency was not set")
    check(step.get_value("storage"), "storage was not set")
    check(step.perturbation, "a frequency step supports a perturbation step")
    # A range moved wholly above the stored upper frequency is accepted, whatever order the model
    # would check the two limits in
    steps.add_frequency(PREFIX + "F2", lower_frequency=1.0, upper_frequency=100.0)
    step = steps.replace_frequency(PREFIX + "F2", lower_frequency=200.0, upper_frequency=300.0)
    check(step.get_value("lower_frequency") == 200.0, "the lower frequency was not replaced")
    check(step.get_value("upper_frequency") == 300.0, "the upper frequency was not replaced")
    step = steps.replace_frequency(PREFIX + "F2", lower_frequency=5.0, upper_frequency=50.0)
    check(step.get_value("lower_frequency") == 5.0 and step.get_value("upper_frequency") == 50.0,
          "a range moved below the stored one was not accepted")
    check_raises(lambda: steps.replace_frequency(PREFIX + "F2", lower_frequency=80.0),
                 "a lower frequency above the stored upper one must be rejected")
    # A limit that is not set reads back as None
    unset = PREFIX + "F3"
    step = steps.add_frequency(unset)
    check(step.get_value("lower_frequency") is None and step.get_value("upper_frequency") is None,
          "unset frequency limits must read back as None: " + str(step.get_value("lower_frequency")))
    # An upper limit of 0 is no upper limit, so the dialog lets it sit below a lower limit, and so does the API
    step = steps.replace_frequency(unset, lower_frequency=5.0, upper_frequency=0)
    check(step.get_value("lower_frequency") == 5.0 and step.get_value("upper_frequency") == 0,
          "a lower limit with no upper limit was not accepted")
    step = steps.add_frequency(PREFIX + "F4", lower_frequency=5.0, upper_frequency=0)
    check(step.get_value("upper_frequency") == 0, "an add with no upper limit was not accepted")
    steps.remove([PREFIX + "F4"])
    # float('nan') clears a limit, which a value left out, None, cannot do on a replace
    step = steps.replace_frequency(unset, lower_frequency=float("nan"))
    check(step.get_value("lower_frequency") is None and step.get_value("upper_frequency") == 0,
          "the lower limit was not cleared: " + str(step.get_value("lower_frequency")))
    step = steps.replace_frequency(unset, upper_frequency=float("nan"))
    check(step.get_value("upper_frequency") is None, "the upper limit was not cleared")
    step = steps.replace_frequency(unset, number_of_frequencies=12)
    check(step.get_value("lower_frequency") is None, "a replace that left the limits out set one")
    check_refused(lambda: steps.replace_frequency(unset, upper_frequency=float("inf")), "'upper_frequency'",
                  "an infinite upper limit must be refused")
    steps.remove([unset])
    #
    step = steps.add_complex_frequency(PREFIX + "CF1", number_of_frequencies=5)
    check(step.get_value("number_of_frequencies") == 5, "the complex frequency count was not set")
    #
    step = steps.add_buckle(PREFIX + "B1", number_of_buckling_factors=3, accuracy=1e-6)
    check(step.get_value("number_of_buckling_factors") == 3, "number_of_buckling_factors was not set")
    check(step.get_value("accuracy") == 1e-6, "accuracy was not set")


def test_dynamic_types():
    steps = pmx.model.steps
    # The damping part is created before the step, the same way a material property is
    damping = pmx.create.damping("Rayleigh", alpha=0.1, beta=0.001)
    step = steps.add_dynamic(PREFIX + "D1", alpha=-0.1, solution_procedure="ImplicitExplicit",
                             relative_to_absolute=True, damping=damping, time_period=0.5)
    check(step.get_value("alpha") == -0.1, "the step alpha was not set")
    check(step.get_value("solution_procedure") == "ImplicitExplicit", "the solution procedure was not set")
    check(step.get_value("relative_to_absolute"), "relative_to_absolute was not set")
    read_damping = step.get_value("damping")
    check(read_damping.DampingType.ToString() == "Rayleigh", "the damping type was not set")
    check(read_damping.Alpha == 0.1 and read_damping.Beta == 0.001, "the damping alpha and beta were not set")
    # The damping read back is the part pmx.create.damping makes, so it goes back into the argument
    step = steps.replace_dynamic(PREFIX + "D1", damping=read_damping)
    check(step.get_value("damping").Alpha == 0.1, "the damping read back did not go back into the step")
    check(step.get_value("time_period") == 0.5, "a dynamic step also takes the static values")
    check_raises(lambda: steps.add_dynamic(PREFIX + "Bad", damping="Rayleigh"),
                 "a damping given as a string must be rejected")
    check_raises(lambda: steps.add_dynamic(PREFIX + "Bad", solution_procedure="NotAProcedure"),
                 "an unknown solution procedure must be rejected")
    # Modal damping of the Direct type carries a ratio per mode range
    modal_damping = pmx.create.modal_damping("Direct", damping_ratios=[
        pmx.create.damping_ratio(0.02, 1, 5), pmx.create.damping_ratio(0.03, 6, 10)])
    step = steps.add_modal_dynamics(PREFIX + "MD1", steady_state=True, relative_error=0.001,
                                    modal_damping=modal_damping)
    read_modal_damping = step.get_value("modal_damping")
    check(read_modal_damping.DampingType.ToString() == "Direct", "the modal damping type was not set")
    check(read_modal_damping.DampingRatiosAndRanges.Count == 2, "the modal damping ratios were not set")
    step = steps.replace_modal_dynamics(PREFIX + "MD1", modal_damping=read_modal_damping)
    check(step.get_value("modal_damping").DampingRatiosAndRanges.Count == 2,
          "the modal damping read back did not go back into the step")
    check(step.get_value("steady_state"), "steady_state was not set")
    check(step.get_value("relative_error") == 0.001, "relative_error was not set")
    check_raises(lambda: steps.add_modal_dynamics(PREFIX + "Bad", modal_damping=pmx.create.damping()),
                 "a plain damping given as a modal damping must be rejected")
    #
    step = steps.add_steady_state_dynamics(PREFIX + "SSD1", harmonic=False, lower_frequency=5.0,
                                           upper_frequency=50.0, number_of_data_points=10, bias=2.0,
                                           number_of_fourier_terms=15, time_lower=0.0, time_upper=2.0,
                                           modal_damping=pmx.create.modal_damping(
                                               "Constant", viscous_damping_ratio=0.02))
    check(not step.get_value("harmonic"), "harmonic was not set")
    check(step.get_value("number_of_data_points") == 10, "number_of_data_points was not set")
    check(step.get_value("modal_damping").ViscousDampingRatio == 0.02, "the viscous damping ratio was not set")
    # The default upper frequency is 10, so a range above it is set upper limit first
    step = steps.add_steady_state_dynamics(PREFIX + "SSD2", lower_frequency=20.0, upper_frequency=100.0)
    check(step.get_value("lower_frequency") == 20.0, "the lower frequency above the default was not set")
    check(step.get_value("upper_frequency") == 100.0, "the upper frequency was not set")
    step = steps.replace_steady_state_dynamics(PREFIX + "SSD2", lower_frequency=200.0, upper_frequency=300.0)
    check(step.get_value("lower_frequency") == 200.0 and step.get_value("upper_frequency") == 300.0,
          "a range moved above the stored one was not accepted")


def test_thermal_and_wear_types():
    steps = pmx.model.steps
    step = steps.add_heat_transfer(PREFIX + "HT1", steady_state=False, deltmx=5.0, time_period=10.0)
    check(not step.get_value("steady_state"), "steady_state was not set")
    check(step.get_value("deltmx") == 5.0, "deltmx was not set")
    check(step.get_value("time_period") == 10.0, "a heat transfer step also takes the static values")
    #
    step = steps.add_uncoupled_temp_disp(PREFIX + "UTD1", steady_state=False, deltmx=2.0)
    check(step.item_type == "uncoupled_temp_disp", "unexpected step type " + step.item_type)
    check(step.get_value("deltmx") == 2.0, "deltmx was not set on the uncoupled step")
    # A temperature change limit that is not a positive number is refused before anything is recorded
    count_before_refusals = len(steps)
    for deltmx in [float("nan"), 0.0, -1.0, float("-inf")]:
        check_raises(lambda: steps.add_heat_transfer(PREFIX + "Bad", deltmx=deltmx),
                     "deltmx=" + str(deltmx) + " must be rejected")
        check_raises(lambda: steps.replace_uncoupled_temp_disp(PREFIX + "UTD1", deltmx=deltmx),
                     "a replace with deltmx=" + str(deltmx) + " must be rejected")
    check(len(steps) == count_before_refusals, "a refused add was recorded")
    check(steps[PREFIX + "UTD1"].get_value("deltmx") == 2.0, "a refused replace changed deltmx")
    step = steps.replace_uncoupled_temp_disp(PREFIX + "UTD1", deltmx=float("inf"))
    check(step.get_value("deltmx") == float("inf"), "deltmx was not put back to the default")
    #
    step = steps.add_coupled_temp_disp(PREFIX + "CTD1", time_period=4.0)
    check(step.item_type == "coupled_temp_disp", "unexpected step type " + step.item_type)
    check(step.get_value("time_period") == 4.0, "the time period was not set on the coupled step")
    # A boundary displacement step follows a slip wear step
    step = steps.add_slip_wear(PREFIX + "SW1", time_period=1.0)
    check(step.item_type == "slip_wear", "unexpected step type " + step.item_type)
    step = steps.add_boundary_displacement(PREFIX + "BD1", max_increments=20)
    check(step.item_type == "boundary_displacement", "unexpected step type " + step.item_type)
    check(step.max_increments == 20, "max_increments was not set on the boundary displacement step")
    check_raises(lambda: steps.add_boundary_displacement(PREFIX + "Bad", perturbation=True),
                 "a perturbation boundary displacement step must be rejected")
    # These step types derive from another one in the model, yet a replace still only edits the exact type
    refused_replaces = [(steps.replace_static, PREFIX + "HT1", "heat_transfer"),
                        (steps.replace_static, PREFIX + "D1", "dynamic"),
                        (steps.replace_static, PREFIX + "MD1", "modal_dynamics"),
                        (steps.replace_static, PREFIX + "SW1", "slip_wear"),
                        (steps.replace_static, PREFIX + "UTD1", "uncoupled_temp_disp"),
                        (steps.replace_heat_transfer, PREFIX + "UTD1", "uncoupled_temp_disp"),
                        (steps.replace_heat_transfer, PREFIX + "CTD1", "coupled_temp_disp"),
                        (steps.replace_uncoupled_temp_disp, PREFIX + "CTD1", "coupled_temp_disp")]
    for replace, step_name, item_type in refused_replaces:
        message = raised_message(lambda: replace(step_name, time_period=7.0))
        check(message is not None and "add_" + item_type + "," in message,
              "replacing the " + item_type + " " + step_name + " by a base type must be refused: " + str(message))
        check(steps[step_name].item_type == item_type, "a refused replace changed the type of " + step_name)


def test_controls():
    step = pmx.model.steps[PREFIX + "S1"]
    check(len(step.get_control_types()) == 0, "a new step has no step controls")
    step.set_controls([pmx.create.time_incrementation_control(i0=8, ic=32, dd=2.0),
                       pmx.create.contact_control(delcon=0.5, itf2f=4)])
    types = list(step.get_control_types())
    check(types == ["time_incrementation_control", "contact_control"],
          "unexpected step control types " + str(types))
    # A single parameter does not need a list
    step.set_controls(pmx.create.reset_control())
    check(list(step.get_control_types()) == ["reset_control"], "the reset control was not set")
    step.set_controls([pmx.create.field_control(rna=0.005)])
    check(list(step.get_control_types()) == ["field_control"], "the field control was not set")
    # None is a value left out: the solver default stays, unset where the solver has none
    step.set_controls([pmx.create.time_incrementation_control(i0=8, is_value=None, ds=None, wg=None),
                       pmx.create.field_control(q0a=None, qua=None)])
    check(list(step.get_control_types()) == ["time_incrementation_control", "field_control"],
          "step controls with values left out were not set")
    step.clear_controls()
    check(len(step.get_control_types()) == 0, "the step controls were not cleared")
    check_raises(lambda: step.set_controls(["not a control parameter"]),
                 "a control parameter given as a string must be rejected")


def test_move():
    steps = pmx.model.steps
    names = [n for n in steps.get_names() if n.startswith(PREFIX)]
    check(len(names) > 2, "at least three steps are needed to check the move")
    last = names[-1]
    steps.move([last], 0)
    check(list(steps.get_names())[0] == last, "the step was not moved to the front")
    check_raises(lambda: steps.move([last], len(steps) + 5), "an out of range target index must be rejected")
    # The model refuses a step named twice only after the command is recorded
    order = list(steps.get_names())
    check_refused(lambda: steps.move([names[0], names[0].lower()], 0), "'names'",
                  "a step named twice in a move must be refused")
    check(list(steps.get_names()) == order, "the refused move changed the step order")
    steps.move([last.lower()], len(steps) - 1)
    check(list(steps.get_names())[-1] == last, "a move by a name in other case was not applied")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    test_static_and_shared_values()
    test_replace()
    test_collection_verbs()
    test_case_insensitive_names()
    test_frequency_types()
    test_dynamic_types()
    test_refused_values()
    test_thermal_and_wear_types()
    test_controls()
    test_move()
    clean()
    check(len([n for n in pmx.model.steps.get_names() if n.startswith(PREFIX)]) == 0,
          "the steps were not removed")
    print("TEST PASSED")


main()
