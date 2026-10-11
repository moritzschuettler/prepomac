#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the amplitudes, the distributions and the initial conditions with their symbol color. A meshed
#model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Fields.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile

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


def clean(collection):
    names = [n for n in collection.get_names() if n.startswith(PREFIX)]
    if names:
        collection.remove(names)


def test_amplitudes():
    clean(pmx.model.amplitudes)
    amplitudes = pmx.model.amplitudes
    count_before = len(amplitudes)
    amplitude = amplitudes.add(PREFIX + "Amp", [[0, 0], [1, 1], [2, 0]])
    check(amplitude.row_count == 3, "3 rows expected, found " + str(amplitude.row_count))
    check(len(amplitudes) == count_before + 1, "the amplitude was not added")
    check(amplitude.time_span != "", "the time span is empty")
    #
    amplitudes.add(PREFIX + "Amp_T", [[0, 0], [1, 1]], time_span="TotalTime")
    check(amplitudes[PREFIX + "Amp_T"].time_span == "TotalTime", "the time span was not set")
    # A replacement given only the table keeps the time span
    amplitude = amplitudes.replace(PREFIX + "Amp_T", time_amplitude=[[0, 0], [1, 2], [2, 3]])
    check(amplitude.row_count == 3, "the table was not replaced, found " + str(amplitude.row_count))
    check(amplitude.time_span == "TotalTime", "the replacement dropped the time span: " + amplitude.time_span)
    check([list(row) for row in amplitude.get_time_amplitude()] == [[0, 0], [1, 2], [2, 3]],
          "unexpected table " + str([list(row) for row in amplitude.get_time_amplitude()]))
    # A NumericArray counts the way numpy does: len() the rows and size every value
    table = amplitude.get_time_amplitude()
    check(len(table) == 3 and table.size == 6 and list(table.shape) == [3, 2],
          "len() must count the rows and size the values: %d, %d" % (len(table), table.size))
    check(not hasattr(table, "Count"), "the .NET Count of a NumericArray must not be exposed")
    # and one given only the time span keeps the table
    amplitude = amplitudes.replace(PREFIX + "Amp_T", time_span="StepTime")
    check(amplitude.time_span == "StepTime", "the time span was not replaced")
    check(amplitude.row_count == 3, "the replacement dropped the table")
    # Every row is a time and an amplitude value
    check_raises(lambda: amplitudes.add(PREFIX + "Bad", [[0, 0, 0]]), "a 3 value row must be rejected")
    check_raises(lambda: amplitudes.add(PREFIX + "Bad", []), "an empty table must be rejected")
    check_raises(lambda: amplitudes.add(PREFIX + "Bad", [[0, 0]], time_span="NotATimeSpan"),
                 "an unknown time span must be rejected")
    count_before_non_finite = len(amplitudes)
    for function in [lambda: amplitudes.add(PREFIX + "Bad", [[0, 0], [float("nan"), 1]]),
                     lambda: amplitudes.replace(PREFIX + "Amp_T", time_amplitude=[[0, 0], [1, float("inf")]])]:
        message = raised_message(function)
        check(message is not None and "'time_amplitude'" in message,
              "a non-finite amplitude row must be refused naming the argument: " + str(message))
    check(len(amplitudes) == count_before_non_finite and amplitudes[PREFIX + "Amp_T"].row_count == 3,
          "a refused non-finite amplitude was recorded")
    #
    amplitudes.rename(PREFIX + "Amp", PREFIX + "Amp_R")
    check(amplitudes.contains(PREFIX + "Amp_R"), "the amplitude was not renamed")
    amplitudes.duplicate([PREFIX + "Amp_R"])
    check(len(amplitudes) == count_before + 3, "the amplitude was not duplicated")


def test_distributions():
    clean(pmx.model.distributions)
    distributions = pmx.model.distributions
    count_before = len(distributions)
    # One equation for the magnitude
    distribution = distributions.add_from_equation(PREFIX + "D", "x + y")
    check(distribution.item_type == "from_equation",
          "unexpected distribution type " + distribution.item_type)
    # Three equations, one per direction
    distributions.add_from_equation(PREFIX + "D3", ["x", "y", "z"])
    check(len(distributions) == count_before + 2, "the distributions were not added")
    #
    check_raises(lambda: distributions.add_from_equation(PREFIX + "Bad", ["x", "y"]),
                 "two equations must be rejected")
    check_raises(lambda: distributions.add_from_file(PREFIX + "Bad", "no_such_file.txt"),
                 "a missing distribution file must be rejected")
    #
    distributions.rename(PREFIX + "D", PREFIX + "D_R")
    check(distributions.contains(PREFIX + "D_R"), "the distribution was not renamed")
    # A distribution keeps its equation as plain text, so the leading equals sign the application
    # evaluates with has to be supplied by the API rather than by the caller
    scalar = distributions.add_from_equation(PREFIX + "Scalar", "1 + 0.01 * x")
    check(scalar.item_type == "from_equation", "unexpected scalar distribution type")
    vector = distributions.add_from_equation(PREFIX + "Vector", ["x", "y", "z"])
    # The equation reads back in the shape the argument takes it, with the sign the API supplied
    check(scalar.equation == "=1 + 0.01 * x", "the scalar equation did not read back: " + str(scalar.equation))
    check(list(vector.equation) == ["=x", "=y", "=z"],
          "the vector equations did not read back: " + str(list(vector.equation)))
    check(scalar.file_name is None and scalar.interpolator_type is None and scalar.interpolator_radius is None and
          scalar.scale is None and scalar.translate is None,
          "an equation distribution must report no file values")
    check(len(scalar.get_equations()) == 0, "an equation distribution reported mapped equations")
    check(scalar.coordinate_system_name == "Global",
          "unexpected coordinate system " + str(scalar.coordinate_system_name))
    # The read back equation goes straight back into a replacement
    vector = distributions.replace_from_equation(PREFIX + "Vector", equation=vector.equation)
    check(list(vector.equation) == ["=x", "=y", "=z"], "the vector equations did not round trip")
    scalar = distributions.replace_from_equation(PREFIX + "Scalar", equation=scalar.equation)
    check(scalar.equation == "=1 + 0.01 * x", "the scalar equation did not round trip: " + str(scalar.equation))
    # An equation that already carries the sign is stored unchanged
    distributions.add_from_equation(PREFIX + "Signed", "=1")
    # A replacement may keep the name, given or not
    distribution = distributions.replace_from_equation(PREFIX + "Signed", PREFIX + "Signed", "=2")
    check(distribution.name == PREFIX + "Signed", "the replacement under the same name failed")
    distribution = distributions.replace_from_equation(PREFIX + "Signed", equation="x + 1")
    check(distribution.name == PREFIX + "Signed" and distributions.contains(PREFIX + "Signed"),
          "a replacement of the equation alone did not keep the name")
    # A replacement that only renames keeps the equation, so a vector distribution stays one and is still
    # refused for a temperature
    distributions.add_from_equation(PREFIX + "Kept", ["x", "y", "z"])
    distribution = distributions.replace_from_equation(PREFIX + "Kept", name=PREFIX + "Kept_R")
    check(distributions.contains(PREFIX + "Kept_R") and not distributions.contains(PREFIX + "Kept"),
          "the distribution was not renamed")
    check(distribution.item_type == "from_equation", "the replacement changed the type")
    check_raises(lambda: pmx.model.initial_conditions.add_temperature(
                     PREFIX + "Bad", pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1]), 20,
                     distribution_name=PREFIX + "Kept_R"),
                 "the replacement did not keep the three equations of the vector distribution")
    # A distribution read from a file has no equation to keep, so the replacement must be given one
    file_name = os.path.join(tempfile.gettempdir(), "pytest_distribution.txt")
    vector_file_name = os.path.join(tempfile.gettempdir(), "pytest_distribution_vector.txt")
    bad_file_name = os.path.join(tempfile.gettempdir(), "pytest_distribution_bad.txt")
    with open(file_name, "w") as writer:
        writer.write("0 0 0 1\n1 0 0 1\n0 1 0 1\n0 0 1 1\n")
    try:
        distributions.add_from_file(PREFIX + "Mapped", file_name, interpolator_type="Gauss",
                                    interpolator_radius=2, scale=[1, 2, 3], translate=["0.5", "0", "0"])
        mapped = distributions[PREFIX + "Mapped"]
        # Every value add_from_file sets reads back under its argument name
        # The temporary folder may be named in its short form, so both paths are resolved first
        check(os.path.normcase(os.path.realpath(mapped.file_name)) ==
              os.path.normcase(os.path.realpath(file_name)),
              "the file name did not read back: " + str(mapped.file_name))
        check(mapped.equation is None, "a file distribution must report no equation")
        check(mapped.interpolator_type == "Gauss", "the interpolator type did not read back")
        check(mapped.interpolator_radius == 2, "the interpolator radius did not read back")
        check(type(mapped.scale) is list and type(mapped.translate) is list,
              "the scale and the translate must be lists: " + type(mapped.scale).__name__)
        check(mapped.scale == [1, 2, 3], "the scale did not read back: " + str(mapped.scale))
        check(mapped.translate == [0.5, 0, 0], "the translate did not read back: " + str(mapped.translate))
        check(mapped.coordinate_system_name == "Global", "unexpected coordinate system")
        equations = mapped.get_equations()
        # The radius and the scale were given as plain numbers, so only the translate is an expression
        check(sorted(equations) == ["translate"], "unexpected equation names " + str(sorted(equations)))
        check(list(equations["translate"]) == ["=0.5", "=0", "=0"],
              "the translate expressions did not read back: " + str(list(equations["translate"])))
        # The read back values go straight back into a replacement that changes nothing
        mapped = distributions.replace_from_file(PREFIX + "Mapped", interpolator_type=mapped.interpolator_type,
                                                 interpolator_radius=mapped.interpolator_radius, scale=mapped.scale,
                                                 translate=equations["translate"])
        check(mapped.interpolator_type == "Gauss" and mapped.interpolator_radius == 2 and
              mapped.scale == [1, 2, 3] and mapped.translate == [0.5, 0, 0],
              "the file values did not round trip through replace_from_file")
        check(list(mapped.get_equations()["translate"])[0] == "=0.5", "the translate expression did not round trip")
        distributions.remove([PREFIX + "Mapped"])
        distributions.add_from_file(PREFIX + "File", file_name)
        # The closest point interpolation, the default, has no radius, which the dialog then hides
        check(distributions[PREFIX + "File"].interpolator_type == "ClosestPoint" and
              distributions[PREFIX + "File"].interpolator_radius is None,
              "a closest point distribution must read back no radius")
        check("interpolator_radius" not in distributions[PREFIX + "File"].get_equations(),
              "a closest point distribution must not report a radius expression")
        message = raised_message(lambda: distributions.replace_from_file(PREFIX + "File", interpolator_radius=2))
        check(message is not None and "'interpolator_radius'" in message and "interpolator_type" in message,
              "a radius of a closest point distribution must be refused naming interpolator_type: " + str(message))
        message = raised_message(lambda: distributions.add_from_file(PREFIX + "Bad", file_name,
                                                                     interpolator_type="ClosestPoint",
                                                                     interpolator_radius="=2"))
        check(message is not None and "'interpolator_radius'" in message,
              "an add with a radius for the closest point must be refused: " + str(message))
        check(not distributions.contains(PREFIX + "Bad"), "a refused distribution was recorded")
        mapped = distributions.replace_from_file(PREFIX + "File", interpolator_type="Shepard", interpolator_radius=3)
        check(mapped.interpolator_radius == 3, "a radius given with the Shepard interpolator was not set")
        mapped = distributions.replace_from_file(PREFIX + "File", interpolator_type="ClosestPoint")
        check(mapped.interpolator_radius is None, "the radius of a closest point distribution must read back None")
        # A replace never turns one form into the other, and is refused before anything is recorded
        message = raised_message(lambda: distributions.replace_from_equation(PREFIX + "File", equation="1"))
        check(message is not None and "is made by add_from_file, so replace_from_equation cannot replace it" in
              message, "an equation replacing a file distribution must be refused: " + str(message))
        check(distributions[PREFIX + "File"].item_type == "from_file",
              "the refused replacement changed the file distribution")
        distributions.add_from_equation(PREFIX + "Equation", "1")
        message = raised_message(lambda: distributions.replace_from_file(PREFIX + "Equation", file_name=file_name))
        check(message is not None and "is made by add_from_equation, so replace_from_file cannot replace it" in
              message, "a file replacing an equation distribution must be refused: " + str(message))
        check(distributions[PREFIX + "Equation"].item_type == "from_equation",
              "the refused replacement changed the equation distribution")
        distributions.remove([PREFIX + "File", PREFIX + "Equation"])
        # A new file is read again and may turn a scalar distribution into a vector one, which is refused
        # while an item uses it, the way an equation that does so is
        with open(vector_file_name, "w") as writer:
            writer.write("0 0 0 1 2 3\n1 0 0 1 2 3\n0 1 0 1 2 3\n0 0 1 1 2 3\n")
        with open(bad_file_name, "w") as writer:
            writer.write("0 0 0 1 2\n1 0 0 1 2\n")
        distributions.add_from_file(PREFIX + "File_Used", file_name)
        nodes = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2])
        pmx.model.initial_conditions.add_temperature(PREFIX + "IC_File", nodes, 20,
                                                     distribution_name=PREFIX + "File_Used")
        message = raised_message(lambda: distributions.replace_from_file(PREFIX + "File_Used",
                                                                         file_name=vector_file_name))
        check(message is not None and "initial condition " + PREFIX + "IC_File" in message,
              "a file that makes a used scalar distribution a vector one must be refused: " + str(message))
        pmx.model.initial_conditions.remove([PREFIX + "IC_File"])
        distributions.replace_from_file(PREFIX + "File_Used", file_name=vector_file_name)
        check_raises(lambda: pmx.model.initial_conditions.add_temperature(PREFIX + "Bad", nodes, 20,
                                                                          distribution_name=PREFIX + "File_Used"),
                     "the unused file distribution did not become a vector one")
        # A file that cannot be read is refused naming the argument, before anything is recorded
        count_before_bad = len(distributions)
        message = raised_message(lambda: distributions.add_from_file(PREFIX + "Bad", bad_file_name))
        check(message is not None and "'file_name'" in message,
              "a file of the wrong shape must be refused naming the argument: " + str(message))
        message = raised_message(lambda: distributions.replace_from_file(PREFIX + "File_Used",
                                                                         file_name=bad_file_name))
        check(message is not None and "'file_name'" in message,
              "a replace with a file of the wrong shape must be refused naming the argument: " + str(message))
        check(len(distributions) == count_before_bad, "a refused distribution was recorded")
        distributions.remove([PREFIX + "File_Used"])
    finally:
        for name in [file_name, vector_file_name, bad_file_name]:
            if os.path.isfile(name):
                os.remove(name)


def test_initial_conditions():
    clean(pmx.model.initial_conditions)
    conditions = pmx.model.initial_conditions
    count_before = len(conditions)
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4])
    #
    condition = conditions.add_temperature(PREFIX + "Temp", selection, 20)
    check(condition.item_type == "temperature",
          "unexpected condition type " + condition.item_type)
    # The controller converts the selection into an internal node set
    check(condition.region_type == "NodeSetName",
          "the selection was not converted, region type is " + condition.region_type)
    # The activation state reads back on the item
    check(condition.active, "a new initial condition must be active")
    conditions.deactivate([PREFIX + "Temp"])
    check(not conditions[PREFIX + "Temp"].active, "the initial condition was not deactivated")
    conditions.activate([PREFIX + "Temp"])
    check(conditions[PREFIX + "Temp"].active, "the initial condition was not activated")
    #
    conditions.add_translational_velocity(PREFIX + "Vel", selection, 1, 0, 0)
    conditions.add_angular_velocity(PREFIX + "Ang", selection, [0, 0, 0], [0, 0, 1], 10)
    check(len(conditions) == count_before + 3, "the initial conditions were not added")
    check("distribution_name" in list(conditions[PREFIX + "Temp"].get_value_names()),
          "the initial temperature does not expose its distribution name")
    angular = conditions[PREFIX + "Ang"]
    check(angular.item_type == "angular_velocity", "unexpected condition type " + angular.item_type)
    check(list(angular.get_value("axis_direction")) == [0, 0, 1], "the axis direction does not read back by its name")
    #
    check_raises(lambda: conditions.add_angular_velocity(PREFIX + "Bad", selection, [0, 0], [0, 0, 1], 1),
                 "a 2 value axis point must be rejected")
    # The model refuses a negative rotational speed in words that name no argument
    message = raised_message(lambda: conditions.add_angular_velocity(PREFIX + "Bad", selection, [0, 0, 0],
                                                                     [0, 0, 1], -10))
    check(message is not None and "'rotational_speed'" in message,
          "a negative rotational speed must be refused naming the argument: " + str(message))
    message = raised_message(lambda: conditions.replace_angular_velocity(PREFIX + "Ang", rotational_speed=-1))
    check(message is not None and "'rotational_speed'" in message,
          "a replace with a negative rotational speed must be refused: " + str(message))
    check(conditions[PREFIX + "Ang"].get_value("rotational_speed") == 10, "a refused replace changed the condition")
    # An axis of zero length is no axis, which the dialog refuses as well
    message = raised_message(lambda: conditions.add_angular_velocity(PREFIX + "Bad", selection, [0, 0, 0],
                                                                     [0, 0, 0], 10))
    check(message is not None and "'axis_direction'" in message and "non-zero" in message,
          "a zero axis direction must be refused naming the argument: " + str(message))
    message = raised_message(lambda: conditions.replace_angular_velocity(PREFIX + "Ang", axis_direction=[0, 0, 0]))
    check(message is not None and "'axis_direction'" in message,
          "a replace with a zero axis direction must be refused: " + str(message))
    check(list(conditions[PREFIX + "Ang"].get_value("axis_direction")) == [0, 0, 1],
          "a refused replace changed the axis direction")
    # The dialog refuses a velocity that is zero, in these words, and so does the API, naming the arguments; it
    # judges the condition a replace ends with and the value an expression evaluates to
    for function, fragments in [
            (lambda: conditions.add_translational_velocity(PREFIX + "Bad", selection, 0, "=0", 0),
             ["At least one velocity component must not be equal to 0", "'v1'"]),
            (lambda: conditions.replace_translational_velocity(PREFIX + "Vel", v1=0),
             ["At least one velocity component must not be equal to 0", "'v3'"]),
            (lambda: conditions.add_angular_velocity(PREFIX + "Bad", selection, [0, 0, 0], [0, 0, 1], 0),
             ["Rotational speed must not be equal to 0", "'rotational_speed'"]),
            (lambda: conditions.replace_angular_velocity(PREFIX + "Ang", rotational_speed="=10 - 10"),
             ["Rotational speed must not be equal to 0", "'rotational_speed'"])]:
        message = raised_message(function)
        check(message is not None and all(fragment in message for fragment in fragments),
              "a zero velocity must be refused with " + str(fragments) + ": " + str(message))
    check(conditions[PREFIX + "Vel"].get_value("v1") == 1, "a refused replace changed the velocity")
    check(conditions[PREFIX + "Ang"].get_value("rotational_speed") == 10, "a refused replace changed the speed")
    check(not conditions.contains(PREFIX + "Bad"), "a refused initial condition was recorded")
    # A replacement that is not given a region keeps the selection, which is converted again because
    # the replacement deletes the internal node set of the old initial condition
    condition = conditions.replace_temperature(PREFIX + "Temp", temperature=25)
    check(condition.get_value("temperature") == 25, "the temperature was not replaced")
    check("temperature" not in condition.get_equations(), "a plain temperature was read back as an equation")
    check(condition.region_type == "NodeSetName" and pmx.model.node_sets.contains(condition.region_name),
          "the replacement lost the region: " + str(condition.region_name))
    check(pmx.model.node_sets[condition.region_name].count == 4,
          "the region no longer resolves to the selected nodes")
    check(condition.valid, "the region is no longer valid after the replacement")
    # A replacement of another type is refused with a message rather than failing on a cast
    try:
        conditions.replace_translational_velocity(PREFIX + "Temp", v1=1)
        refused = None
    except Exception as ex:
        refused = str(ex)
    check(refused is not None, "an initial temperature was replaced as a translational velocity")
    check("is made by add_temperature, so replace_translational_velocity cannot replace it" in refused, "the refusal must name the type: " + refused)
    check(conditions[PREFIX + "Temp"].item_type == "temperature",
          "the refused replacement changed the initial condition")
    #
    # An initial condition with no distribution carries the default one
    check(condition.get_value("distribution_name") == "Constant",
          "unexpected default distribution " + str(condition.get_value("distribution_name")))
    # A scalar distribution scales the temperature over the region, the way the temperature BC does
    condition = conditions.add_temperature(PREFIX + "Temp_D", selection, 20,
                                           distribution_name=PREFIX + "Scalar")
    check(condition.get_value("distribution_name") == PREFIX + "Scalar",
          "the distribution was not stored, it is " + str(condition.get_value("distribution_name")))
    # A replacement keeps a distribution it is not given and changes one it is. The region is given
    # again, because an initial condition that carries a distribution and keeps its selection based
    # region cannot be replaced: the application renames the internal node set the distribution is
    # expanded over before the model is checked again.
    condition = conditions.replace_temperature(PREFIX + "Temp_D", region=selection, temperature=30)
    check(condition.get_value("distribution_name") == PREFIX + "Scalar",
          "the replacement dropped the distribution")
    condition = conditions.replace_temperature(PREFIX + "Temp_D", region=selection,
                                               distribution_name="constant")
    check(condition.get_value("distribution_name") == "Constant",
          "the default distribution must be found regardless of case and read back as Constant: " +
          str(condition.get_value("distribution_name")))
    # A used distribution keeps its scalar or vector type, because its users were checked against it
    distributions = pmx.model.distributions
    condition = conditions.replace_temperature(PREFIX + "Temp_D", region=selection,
                                               distribution_name=PREFIX + "Scalar")
    message = raised_message(lambda: distributions.replace_from_equation(PREFIX + "Scalar",
                                                                         equation=["x", "y", "z"]))
    check(message is not None and "initial condition " + PREFIX + "Temp_D" in message,
          "a used scalar distribution was made a vector one: " + str(message))
    distributions.replace_from_equation(PREFIX + "Scalar", equation="2 + 0.01 * x")
    condition = conditions.replace_temperature(PREFIX + "Temp_D", region=selection, temperature=30)
    check(condition.get_value("distribution_name") == PREFIX + "Scalar",
          "the scalar distribution no longer serves the temperature")
    condition = conditions.replace_temperature(PREFIX + "Temp_D", region=selection,
                                               distribution_name="Constant")
    # Unused, it may change its type
    distributions.replace_from_equation(PREFIX + "Scalar", equation=["x", "y", "z"])
    check_raises(lambda: conditions.add_temperature(PREFIX + "Bad", selection, 20,
                                                    distribution_name=PREFIX + "Scalar"),
                 "the unused distribution did not become a vector one")
    distributions.replace_from_equation(PREFIX + "Scalar", equation="1 + 0.01 * x")
    check_raises(lambda: conditions.add_temperature(PREFIX + "Bad", selection, 20,
                                                    distribution_name="NoSuchDistribution"),
                 "an unknown distribution must be rejected")
    # A temperature is a scalar, so a vector distribution is refused rather than failing later
    check_raises(lambda: conditions.add_temperature(PREFIX + "Bad", selection, 20,
                                                    distribution_name=PREFIX + "Vector"),
                 "a vector distribution must be rejected for a temperature")
    check(not conditions.contains(PREFIX + "Bad"), "a rejected initial condition must not be recorded")
    #
    conditions.hide(PREFIX + "Temp")
    check(not conditions[PREFIX + "Temp"].visible, "the initial condition was not hidden")
    conditions.show(PREFIX + "Temp")
    check(conditions[PREFIX + "Temp"].visible, "the initial condition was not shown")
    # The symbol color, read back as a list that goes back into the argument
    check(list(conditions[PREFIX + "Temp"].color) == [255, 165, 0, 255],
          "unexpected default color " + str(list(conditions[PREFIX + "Temp"].color)))
    condition = conditions.add_translational_velocity(PREFIX + "Vel_C", selection, 1, 0, 0, color=[10, 20, 30])
    check(list(condition.color) == [10, 20, 30, 255], "the color was not set: " + str(list(condition.color)))
    condition = conditions.replace_translational_velocity(PREFIX + "Vel_C", v2=1)
    check(list(condition.color) == [10, 20, 30, 255], "a replace that does not name the color must keep it")
    condition = conditions.replace_angular_velocity(PREFIX + "Ang", color=condition.color)
    check(list(condition.color) == [10, 20, 30, 255], "the color read back must go back into the argument")
    message = raised_message(lambda: conditions.add_temperature(PREFIX + "Bad", selection, 20, color=[1, 2]))
    check(message is not None and "'color'" in message,
          "a two channel color must be refused naming the argument: " + str(message))
    check(not conditions.contains(PREFIX + "Bad"), "a refused initial condition was recorded")
    conditions.remove([PREFIX + "Vel_C"])


def geometry_face_selection(face):
    """The selection the dialog records for a picked face: its geometry IDs, as the journal writes them."""
    geometry_ids = list(pmx.create.selection_by_ids('femodel', 'geometry', 'surfaces', {1: [face]}).get_ids())
    check(len(geometry_ids) > 0, "the face resolved to no geometry IDs")
    return pmx.create.selection_by_ids('femodel', 'geometry', 'geometry', geometry_ids)


def test_initial_condition_geometry_regions():
    # The initial condition dialog picks geometry, so its journal hands that selection back, and it must
    # resolve to the nodes the same face selected by nodes gives
    conditions = pmx.model.initial_conditions
    face_nodes = sorted(pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {1: [1]}).get_ids())
    added = [conditions.add_temperature(PREFIX + "Geo_T", geometry_face_selection(1), 20),
             conditions.add_translational_velocity(PREFIX + "Geo_V", geometry_face_selection(1), 1, 0, 0),
             conditions.add_angular_velocity(PREFIX + "Geo_A", geometry_face_selection(1), [0, 0, 0], [0, 0, 1],
                                             10)]
    for condition in added:
        check(condition.region_type == "NodeSetName",
              condition.name + " has the region type " + condition.region_type)
        node_ids = sorted(pmx.model.node_sets[condition.region_name].get_node_ids())
        check(node_ids == face_nodes, condition.name + " resolved to the nodes " + str(node_ids) + ", not " +
              str(face_nodes))
    condition = conditions.replace_temperature(PREFIX + "Geo_T", region=geometry_face_selection(2))
    face_2_nodes = sorted(pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {1: [2]}).get_ids())
    check(sorted(pmx.model.node_sets[condition.region_name].get_node_ids()) == face_2_nodes,
          "the replaced geometry selection did not resolve to the nodes of the face")
    # The geometry target adds no region type, so a named region is still checked against the node ones
    message = raised_message(lambda: conditions.add_temperature(PREFIX + "Bad",
                                                                pmx.create.region('ElementSetName', 'AnySet'), 20))
    check(message is not None and "ElementSetName region was given" in message,
          "an element set region must be refused: " + str(message))
    message = raised_message(lambda: conditions.add_temperature(
        PREFIX + "Bad", pmx.create.selection_by_ids('femodel', 'elements', 'elements', [1]), 20))
    check(message is not None and "'nodes' or 'geometry'" in message,
          "an element selection must be refused naming the targets: " + str(message))
    check(not conditions.contains(PREFIX + "Bad"), "a refused initial condition was recorded")
    conditions.remove([condition.name for condition in added])


def test_amplitude_shifts():
    amplitudes = pmx.model.amplitudes
    # The shifts default to zero and are set by the add
    amplitude = amplitudes.add(PREFIX + "Shift", [[0, 0], [1, 1]])
    check(amplitude.shift_time == 0 and amplitude.shift_amplitude == 0,
          "the shifts must default to zero: %s, %s" % (amplitude.shift_time, amplitude.shift_amplitude))
    amplitude = amplitudes.add(PREFIX + "Shifted", [[0, 0], [1, 1]], shift_time=0.5, shift_amplitude=-0.25)
    check(amplitude.shift_time == 0.5 and amplitude.shift_amplitude == -0.25,
          "the shifts were not set: %s, %s" % (amplitude.shift_time, amplitude.shift_amplitude))
    # A replace keeps the shifts it is not given, and the read back goes back into the argument
    amplitude = amplitudes.replace(PREFIX + "Shifted", time_span="TotalTime")
    check(amplitude.shift_time == 0.5 and amplitude.shift_amplitude == -0.25, "the replace dropped the shifts")
    amplitude = amplitudes.replace(PREFIX + "Shifted", shift_time=0, shift_amplitude=amplitude.shift_amplitude)
    check(amplitude.shift_time == 0 and amplitude.shift_amplitude == -0.25, "the shift was not set back to 0")
    message = raised_message(lambda: amplitudes.add(PREFIX + "Bad", [[0, 0]], shift_time=float("inf")))
    check(message is not None and "'shift_time'" in message, "an infinite shift must be refused: " + str(message))
    check(not amplitudes.contains(PREFIX + "Bad"), "a refused amplitude was added")
    amplitudes.remove([PREFIX + "Shift", PREFIX + "Shifted"])


def test_distribution_coordinate_system():
    distributions = pmx.model.distributions
    systems = pmx.model.coordinate_systems
    clean(systems)
    systems.add(PREFIX + "CS", [0, 0, 0], [0, 1, 0], [-1, 0, 0])
    # The name is matched regardless of case and stored in the model's spelling
    distribution = distributions.add_from_equation(PREFIX + "Local", "x", coordinate_system_name=(PREFIX + "cs").upper())
    check(distribution.coordinate_system_name == PREFIX + "CS",
          "the coordinate system was not stored in the model's spelling: " + distribution.coordinate_system_name)
    # A replace keeps it, and Global, in any case, sets it back
    distribution = distributions.replace_from_equation(PREFIX + "Local", equation=["x", "y", "z"])
    check(distribution.coordinate_system_name == PREFIX + "CS", "the replace dropped the coordinate system")
    distribution = distributions.replace_from_equation(PREFIX + "Local", coordinate_system_name="global")
    check(distribution.coordinate_system_name == "Global", "Global did not set the coordinate system back")
    distribution = distributions.replace_from_equation(PREFIX + "Local",
                                                       coordinate_system_name=PREFIX + "CS")
    check(distribution.coordinate_system_name == PREFIX + "CS", "the read back did not go back into the replace")
    message = raised_message(lambda: distributions.add_from_equation(PREFIX + "Bad", "x",
                                                                     coordinate_system_name="NoSuchSystem"))
    check(message is not None and "'coordinate_system_name'" in message,
          "a missing coordinate system must be refused naming the argument: " + str(message))
    check(not distributions.contains(PREFIX + "Bad"), "a refused distribution was added")
    distributions.remove([PREFIX + "Local"])
    clean(systems)


def test_distribution_reaches_the_deck():
    # An initial temperature driven by a distribution is expanded per node when the deck is written,
    # which is the only place a wrongly stored equation shows up
    export_file = os.path.join(tempfile.gettempdir(), "pytest_distribution.inp")
    if os.path.isfile(export_file):
        os.remove(export_file)
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4])
    pmx.model.distributions.add_from_equation(PREFIX + "Deck", "1 + 0.01 * x")
    pmx.model.initial_conditions.add_temperature(PREFIX + "IC_Deck", selection, 20,
                                                 distribution_name=PREFIX + "Deck")
    pmx.export_to_calculix(export_file)
    with open(export_file, "r") as reader:
        exported = reader.read()
    os.remove(export_file)
    check("*Initial conditions, Type=Temperature" in exported,
          "the initial temperature was not exported")


def test_mixed_case_replace():
    """A replace without a name keeps the item under the model's own spelling, whatever spelling old_name has."""
    amplitudes = pmx.model.amplitudes
    distributions = pmx.model.distributions
    amplitudes.add(PREFIX + "Amp_Case", [[0, 0], [1, 1]])
    distributions.add_from_equation(PREFIX + "D_Case", "x + 1")
    cases = [(amplitudes, PREFIX + "Amp_Case", lambda c, n: c.replace(n, time_span="TotalTime")),
             (distributions, PREFIX + "D_Case", lambda c, n: c.replace_from_equation(n, equation="x + 2"))]
    for collection, name, replace in cases:
        count = len(collection)
        for spelling in (name.lower(), name.upper()):
            item = replace(collection, spelling)
            check(item.name == name, "a replace of '" + spelling + "' renamed " + name + " to " + item.name)
            check(name in list(collection.get_names()),
                  name + " is not stored in its own spelling after a replace of '" + spelling + "'")
            check(len(collection) == count, "a replace of '" + spelling + "' changed the number of items")
    check(amplitudes[PREFIX + "Amp_Case"].time_span == "TotalTime", "the mixed case replace changed nothing")
    amplitudes.remove([PREFIX + "Amp_Case"])
    distributions.remove([PREFIX + "D_Case"])


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_mixed_case_replace()
    test_amplitudes()
    test_amplitude_shifts()
    test_distributions()
    test_distribution_coordinate_system()
    test_initial_conditions()
    test_initial_condition_geometry_regions()
    test_distribution_reaches_the_deck()
    clean(pmx.model.initial_conditions)
    clean(pmx.model.distributions)
    clean(pmx.model.amplitudes)
    print("TEST PASSED")


main()
