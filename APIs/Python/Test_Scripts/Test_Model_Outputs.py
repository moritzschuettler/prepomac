#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the step scoped field outputs, history outputs and defined fields. A meshed model must be
#open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Outputs.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile

PREFIX = "PyTest_"
STEP = PREFIX + "Static"
NEXT_STEP = PREFIX + "Static_2"
THERMAL_STEP = PREFIX + "Thermal"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def check_refused(function, fragment, message):
    # The refusal must be the API's own, which names what is wrong
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


def node_selection(ids):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', ids)


def surface_selection(face):
    return pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [face]})


def element_selection(ids):
    return pmx.create.selection_by_ids('femodel', 'elements', 'elements', ids)


def setup():
    pmx.model.steps.add_static(STEP, time_period=1.0)
    pmx.model.steps.add_static(NEXT_STEP, time_period=1.0)
    pmx.model.steps.add_uncoupled_temp_disp(THERMAL_STEP)


def test_field_outputs():
    outputs = pmx.model.steps[STEP].field_outputs
    check(outputs.step_name == STEP, "the collection must carry the step name")
    # A new static step already carries the default nodal and element field outputs
    count_before = len(outputs)
    check(count_before == 2, "a new static step has two field outputs, found " + str(count_before))
    # Nodal
    output = outputs.add_nodal(PREFIX + "NF", ["U", "RF"], last_iterations=True)
    check(output.item_type == "nodal", "unexpected type " + output.item_type)
    check(sorted(list(output.get_variables())) == ["RF", "U"],
          "unexpected variables " + str(list(output.get_variables())))
    check(output.last_iterations, "last_iterations was not set")
    check(output.global_output, "a field output is global by default")
    check(output.active, "a new field output must be active")
    # A single variable does not need a list, and a combined value reads back by name
    output = outputs.add_nodal(PREFIX + "NF_One", "NT")
    check(list(output.get_variables()) == ["NT"], "the single variable was not set")
    check(output.get_value("variables") == "NT", "unexpected variables value")
    check_raises(lambda: outputs.add_nodal(PREFIX + "Bad", ["U", "NotAVariable"]),
                 "an unknown nodal variable must be rejected")
    check_raises(lambda: outputs.add_nodal(PREFIX + "Bad", []),
                 "an empty variable list must be rejected")
    # Element
    output = outputs.add_element(PREFIX + "EF", ["S", "E"], output="ThreeD")
    check(output.get_value("output") == "ThreeD", "the element output was not set")
    check(sorted(list(output.get_variables())) == ["E", "S"], "unexpected element variables")
    check_raises(lambda: outputs.add_element(PREFIX + "Bad", ["S"], output="NotAnOutput"),
                 "an unknown element output must be rejected")
    # Contact
    output = outputs.add_contact(PREFIX + "CF", ["CDIS", "CSTR"])
    check(sorted(list(output.get_variables())) == ["CDIS", "CSTR"], "unexpected contact variables")
    # A value that is not given survives the replace
    output = outputs.replace_nodal(PREFIX + "NF", global_output=False)
    check(not output.global_output, "global_output was not replaced")
    check(sorted(list(output.get_variables())) == ["RF", "U"], "the variables must survive a replace")
    check(output.last_iterations, "last_iterations must survive a replace")
    output = outputs.replace_nodal(PREFIX + "NF", name=PREFIX + "NF_R", variables="V")
    check(outputs.contains(PREFIX + "NF_R"), "the field output was not renamed")
    check(list(output.get_variables()) == ["V"], "the variables were not replaced")
    outputs.rename(PREFIX + "NF_R", PREFIX + "NF")
    # The type cannot change on a replace
    check_raises(lambda: outputs.replace_element(PREFIX + "NF"),
                 "replacing a nodal field output by an element one must be rejected")
    check_raises(lambda: outputs.add_nodal(PREFIX + "NF", "U"), "a duplicate name must be rejected")
    # Collection verbs
    check(len([o for o in outputs]) == len(outputs), "iteration failed")
    outputs.deactivate([PREFIX + "NF"])
    check(not outputs[PREFIX + "NF"].active, "the field output was not deactivated")
    outputs.activate([PREFIX + "NF"])
    count = len(outputs)
    outputs.duplicate([PREFIX + "NF"])
    check(len(outputs) == count + 1, "the field output was not duplicated")
    outputs.remove([n for n in outputs.get_names() if n.startswith(PREFIX + "NF") and n != PREFIX + "NF"])
    # Propagate
    following = pmx.model.steps[NEXT_STEP].field_outputs
    check(not following.contains(PREFIX + "NF"), "the following step must not carry it yet")
    outputs.propagate(PREFIX + "NF")
    check(following.contains(PREFIX + "NF"), "the field output was not propagated")
    # The thermal step is the last one, so a propagation from it would record a command that copies nothing
    last = pmx.model.steps[THERMAL_STEP].field_outputs
    check(len(last) > 0, "the last step must carry its default field outputs")
    check_refused(lambda: last.propagate(last.get_names()[0]), "no following step",
                  "propagating a field output from the last step must be refused")


def test_history_outputs():
    outputs = pmx.model.steps[STEP].history_outputs
    check(len(outputs) == 0, "a new step has no history outputs")
    # Nodal
    output = outputs.add_nodal(PREFIX + "HN", node_selection([1, 2]), ["U", "RF"], totals_type="Only")
    check(output.item_type == "nodal", "unexpected type " + output.item_type)
    check(output.region_type == "NodeSetName", "unexpected region type " + output.region_type)
    check(sorted(list(output.get_variables())) == ["RF", "U"], "unexpected variables")
    check(output.totals_type == "Only", "the totals type was not set")
    check_raises(lambda: outputs.add_nodal(PREFIX + "Bad", node_selection([1]), "U",
                                           totals_type="NotATotalsType"),
                 "an unknown totals type must be rejected")
    # Element
    output = outputs.add_element(PREFIX + "HE", element_selection([1]), ["S", "E"])
    check(output.region_type == "ElementSetName", "unexpected region type " + output.region_type)
    check(sorted(list(output.get_variables())) == ["E", "S"], "unexpected element variables")
    # Section
    output = outputs.add_section(PREFIX + "HS", surface_selection(1), "SOF")
    check(output.region_type == "SurfaceName", "unexpected region type " + output.region_type)
    check(list(output.get_variables()) == ["SOF"], "unexpected section variables")
    check_raises(lambda: outputs.add_section(PREFIX + "Bad", surface_selection(1), "U"),
                 "a nodal variable in a section history output must be rejected")
    # A contact history output names a contact pair, which this model has none of
    check_raises(lambda: outputs.add_contact(PREFIX + "Bad", "NoSuchContactPair", "CDIS"),
                 "an unknown contact pair must be rejected")
    # A value that is not given survives the replace
    output = outputs.replace_nodal(PREFIX + "HN", variables="V")
    check(list(output.get_variables()) == ["V"], "the variables were not replaced")
    check(output.totals_type == "Only", "the totals type must survive a replace")
    check_raises(lambda: outputs.replace_element(PREFIX + "HN"),
                 "replacing a nodal history output by an element one must be rejected")
    # Collection verbs and propagate
    outputs.rename(PREFIX + "HN", PREFIX + "HN_R")
    check(outputs.contains(PREFIX + "HN_R"), "the history output was not renamed")
    outputs.rename(PREFIX + "HN_R", PREFIX + "HN")
    following = pmx.model.steps[NEXT_STEP].history_outputs
    outputs.propagate(PREFIX + "HN")
    check(following.contains(PREFIX + "HN"), "the history output was not propagated")
    last = pmx.model.steps[THERMAL_STEP].history_outputs
    check(last.contains(PREFIX + "HN"), "the history output was not propagated to the last step")
    check_refused(lambda: last.propagate(PREFIX + "HN"), "no following step",
                  "propagating a history output from the last step must be refused")
    outputs.remove([PREFIX + "HE"])
    check(not outputs.contains(PREFIX + "HE"), "the history output was not removed")


def test_defined_fields():
    fields = pmx.model.steps[STEP].defined_fields
    check(len(fields) == 0, "a new step has no defined fields")
    field = fields.add_temperature(PREFIX + "DF", node_selection([1, 2]), 100)
    check(field.item_type == "temperature", "unexpected type " + field.item_type)
    check(field.region_type == "NodeSetName", "unexpected region type " + field.region_type)
    check(field.get_value("temperature") == 100, "the temperature was not set")
    check(field.get_value("type") == "ByValue", "unexpected defined temperature type")
    check(field.amplitude_name == "Default", "unexpected amplitude name " + field.amplitude_name)
    check(field.active and field.visible, "a new defined field must be active and visible")
    check_raises(lambda: fields.add_temperature(PREFIX + "Bad", node_selection([1]), 100,
                                                distribution_name="NoSuchDistribution"),
                 "an unknown distribution must be rejected")
    check_raises(lambda: fields.add_temperature(PREFIX + "Bad", node_selection([1]), 100,
                                                amplitude_name="NoSuchAmplitude"),
                 "an unknown amplitude must be rejected")
    # A temperature is a scalar, which the model would only find out when the deck is written
    vector = PREFIX + "Vector"
    pmx.model.distributions.add_from_equation(vector, ["x", "y", "z"])
    try:
        check_refused(lambda: fields.add_temperature(PREFIX + "Bad", node_selection([1]), 100,
                                                     distribution_name=vector),
                      "needs a Scalar one", "a vector distribution of a defined temperature must be refused")
        check(not fields.contains(PREFIX + "Bad"), "the refused defined temperature was recorded")
        check_refused(lambda: fields.replace_temperature(PREFIX + "DF", distribution_name=vector),
                      "needs a Scalar one", "a vector distribution of a replaced defined temperature must be refused")
    finally:
        pmx.model.distributions.remove([vector])
    # A thermal step carries no defined temperature
    thermal = pmx.model.steps[THERMAL_STEP].defined_fields
    check_raises(lambda: thermal.add_temperature(PREFIX + "Bad", node_selection([1]), 100),
                 "a defined temperature in an uncoupled step must be rejected")
    # A value that is not given survives the replace
    field = fields.replace_temperature(PREFIX + "DF", temperature=250)
    check(field.get_value("temperature") == 250, "the temperature was not replaced")
    check("temperature" not in field.get_equations(),
          "a plain temperature was read back as an equation: " + str(field.get_equations()))
    field = fields.replace_temperature(PREFIX + "DF", temperature="=125 * 2")
    check(field.get_equations()["temperature"].replace(" ", "") == "=125*2",
          "the temperature expression did not read back: " + str(field.get_equations()))
    check(field.get_value("temperature") == 250, "the temperature expression did not evaluate")
    check(field.region_type == "NodeSetName", "the region must survive a replace")
    # The from file form is a different add method, and its file is checked
    check_raises(lambda: fields.add_temperature_from_file(PREFIX + "Bad", "no_such_file.frd"),
                 "a missing temperature file must be rejected")
    check_raises(lambda: fields.replace_temperature_from_file(PREFIX + "DF"),
                 "a by value defined field must need a file name to be read from a file")
    check(fields[PREFIX + "DF"].get_value("type") == "ByValue", "the refused replacement changed the type")
    # Any existing file proves the path check; a defined temperature from a file needs no region and its
    # file is only read when the analysis is written
    file_name = os.path.join(tempfile.gettempdir(), "pytest_temperature.frd")
    open(file_name, "w").close()
    try:
        field = fields.replace_temperature_from_file(PREFIX + "DF", file_name=file_name, step_id=1)
        check(field.get_value("type") == "FromFile", "the defined field was not switched to a file")
        check(field.item_type == "temperature_from_file", "unexpected type " + field.item_type)
        # Back to a value needs the region that a field read from a file does not carry
        check_raises(lambda: fields.replace_temperature(PREFIX + "DF", temperature=300),
                     "a from file defined field was given a value without a region")
        # Nor does it carry a temperature, so a region alone would leave the field at zero
        check_refused(lambda: fields.replace_temperature(PREFIX + "DF", region=node_selection([1, 2])),
                      "'temperature'", "a from file defined field was given a region without a temperature")
        check(fields[PREFIX + "DF"].get_value("type") == "FromFile",
              "the refused replacement changed the type")
        field = fields.replace_temperature(PREFIX + "DF", region=node_selection([1, 2]), temperature=300)
        check(field.get_value("type") == "ByValue", "the defined field was not switched back to a value")
        check(field.get_value("temperature") == 300, "the temperature was not replaced")
        check(field.region_type == "NodeSetName", "unexpected region type " + field.region_type)
        check(pmx.model.node_sets[field.region_name].count == 2,
              "the region given with the switch back does not resolve to the selected nodes")
    finally:
        os.remove(file_name)
    # Hide, show and propagate
    fields.hide([PREFIX + "DF"])
    check(not fields[PREFIX + "DF"].visible, "the defined field was not hidden")
    fields.show([PREFIX + "DF"])
    check(fields[PREFIX + "DF"].visible, "the defined field was not shown")
    following = pmx.model.steps[NEXT_STEP].defined_fields
    fields.propagate(PREFIX + "DF")
    check(following.contains(PREFIX + "DF"), "the defined field was not propagated")
    # The thermal step carries no defined temperature, so a static step is added after it to be the last one
    pmx.model.steps.add_static(PREFIX + "Static_Last", time_period=1.0)
    last = pmx.model.steps[PREFIX + "Static_Last"].defined_fields
    last.add_temperature(PREFIX + "DF_Last", node_selection([1, 2]), 100)
    check_refused(lambda: last.propagate(PREFIX + "DF_Last"), "no following step",
                  "propagating a defined field from the last step must be refused")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    setup()
    test_field_outputs()
    test_history_outputs()
    test_defined_fields()
    clean()
    print("TEST PASSED")


main()
