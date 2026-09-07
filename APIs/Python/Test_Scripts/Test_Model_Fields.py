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
    # A distribution keeps its equation as plain text, so the leading equals sign the application
    # evaluates with has to be supplied by the API rather than by the caller
    scalar = distributions.add_from_equation(PREFIX + "Scalar", "1 + 0.01 * x")
    check(scalar.distribution_type == "DistributionFromEquation", "unexpected scalar distribution type")
    distributions.add_from_equation(PREFIX + "Vector", ["x", "y", "z"])
    # An equation that already carries the sign is stored unchanged
    distributions.add_from_equation(PREFIX + "Signed", "=1")


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
    check("distribution_name" in list(conditions[PREFIX + "Temp"].get_value_names()),
          "the initial temperature does not expose its distribution name")
    #
    check_raises(lambda: conditions.add_angular_velocity(PREFIX + "Bad", selection, [0, 0], [0, 0, 1], 1),
                 "a 2 value axis point must be rejected")
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
