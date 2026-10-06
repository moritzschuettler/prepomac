#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the amplitudes, the distributions and the initial conditions. A meshed model must be open
#before the script runs.
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
        check(list(mapped.scale) == [1, 2, 3], "the scale did not read back: " + str(list(mapped.scale)))
        check(list(mapped.translate) == [0.5, 0, 0], "the translate did not read back: " + str(list(mapped.translate)))
        check(mapped.coordinate_system_name == "Global", "unexpected coordinate system")
        equations = mapped.get_equations()
        check(sorted(equations) == ["interpolator_radius", "scale", "translate"],
              "unexpected equation names " + str(sorted(equations)))
        check(list(equations["translate"]) == ["=0.5", "=0", "=0"],
              "the translate expressions did not read back: " + str(list(equations["translate"])))
        # The read back values go straight back into a replacement that changes nothing
        mapped = distributions.replace_from_file(PREFIX + "Mapped", interpolator_type=mapped.interpolator_type,
                                                 interpolator_radius=mapped.interpolator_radius, scale=mapped.scale,
                                                 translate=equations["translate"])
        check(mapped.interpolator_type == "Gauss" and mapped.interpolator_radius == 2 and
              list(mapped.scale) == [1, 2, 3] and list(mapped.translate) == [0.5, 0, 0],
              "the file values did not round trip through replace_from_file")
        check(list(mapped.get_equations()["translate"])[0] == "=0.5", "the translate expression did not round trip")
        distributions.remove([PREFIX + "Mapped"])
        distributions.add_from_file(PREFIX + "File", file_name)
        check_raises(lambda: distributions.replace_from_equation(PREFIX + "File"),
                     "a file distribution was replaced without an equation")
        check(distributions[PREFIX + "File"].item_type == "from_file",
              "the refused replacement changed the file distribution")
        distribution = distributions.replace_from_equation(PREFIX + "File", equation="1")
        check(distribution.item_type == "from_equation",
              "the file distribution was not replaced by an equation one")
    finally:
        os.remove(file_name)


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
    # A replacement that is not given a region keeps the selection, which is converted again because
    # the replacement deletes the internal node set of the old initial condition
    condition = conditions.replace_temperature(PREFIX + "Temp", temperature=25)
    check(float(condition.get_equations()["temperature"].split(" ")[0]) == 25, "the temperature was not replaced")
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
                                               distribution_name="Constant")
    check(condition.get_value("distribution_name") == "Constant",
          "the replacement did not change the distribution")
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


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_amplitudes()
    test_distributions()
    test_initial_conditions()
    test_distribution_reaches_the_deck()
    clean(pmx.model.initial_conditions)
    clean(pmx.model.distributions)
    clean(pmx.model.amplitudes)
    print("TEST PASSED")


main()
