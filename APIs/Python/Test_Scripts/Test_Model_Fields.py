#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the amplitudes, the distributions and the initial conditions. A meshed model must be open
#before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Fields.py -g No -x Yes -w <dir>
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
    check(distribution.distribution_type == "DistributionFromEquation",
          "unexpected distribution type " + distribution.distribution_type)
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


def test_initial_conditions():
    clean(pmx.model.initial_conditions)
    conditions = pmx.model.initial_conditions
    count_before = len(conditions)
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4])
    #
    condition = conditions.add_temperature(PREFIX + "Temp", selection, 20)
    check(condition.condition_type == "InitialTemperature",
          "unexpected condition type " + condition.condition_type)
    # The controller converts the selection into an internal node set
    check(condition.region_type == "NodeSetName",
          "the selection was not converted, region type is " + condition.region_type)
    #
    conditions.add_translational_velocity(PREFIX + "Vel", selection, 1, 0, 0)
    conditions.add_angular_velocity(PREFIX + "Ang", selection, [0, 0, 0], [0, 0, 1], 10)
    check(len(conditions) == count_before + 3, "the initial conditions were not added")
    #
    check_raises(lambda: conditions.add_angular_velocity(PREFIX + "Bad", selection, [0, 0], [0, 0, 1], 1),
                 "a 2 value axis point must be rejected")
    #
    conditions.hide(PREFIX + "Temp")
    check(not conditions[PREFIX + "Temp"].visible, "the initial condition was not hidden")
    conditions.show(PREFIX + "Temp")
    check(conditions[PREFIX + "Temp"].visible, "the initial condition was not shown")


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_amplitudes()
    test_distributions()
    test_initial_conditions()
    clean(pmx.model.initial_conditions)
    clean(pmx.model.distributions)
    clean(pmx.model.amplitudes)
    print("TEST PASSED")


main()
