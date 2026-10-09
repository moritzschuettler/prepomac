#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the result field output definitions: the limit, the envelope and the equation forms, their
#filters and the CSV export. A result must be open before the script runs, so it is run after
#Test_Analysis.py, which solves the cube and writes PyTest_Job.frd into the work directory.
#
#  PrePoMax.com -f PyTest_Job.frd --script Test_Result_Field_Outputs.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile


LIMIT_NAME = "PyTest_Stress_limit"
ENVELOPE_NAME = "PyTest_Strain_envelope"
EQUATION_NAME = "PyTest_Normalized_stress"
RENAMED_EQUATION_NAME = "PyTest_Normalized_stress_R"
TEST_NAMES = [LIMIT_NAME, ENVELOPE_NAME, EQUATION_NAME, RENAMED_EQUATION_NAME]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def check_refused(function, words, message):
    try:
        function()
    except Exception as ex:
        check(words in str(ex), message + ": " + str(ex))
        return
    raise Exception("TEST FAILED: " + message)


def get_component_name(increment, field_name, preferred_names):
    check(increment.fields.contains(field_name),
          "The field '%s' is not available in the last increment." % field_name)
    components = increment.fields[field_name].components
    for component_name in preferred_names:
        if components.contains(component_name):
            return component_name
    names = components.get_names()
    check(len(names) > 0, "The field '%s' contains no components." % field_name)
    return str(names[0])


def remove_existing(field_outputs, names):
    existing = []
    for name in names:
        if field_outputs.contains(name):
            existing.append(name)
    if len(existing) > 0:
        field_outputs.remove(existing)


result = pmx.results.current
check(result is not None, "Open and select a result before running this script.")

step = result.steps.last()
check(step is not None, "The current result contains no result steps.")
increment = step.increments.last()
check(increment is not None, "The last result step contains no increments.")

# Verify all standard fields and select components from the actual result.
disp_component = get_component_name(increment, "DISP", ["U1", "ALL"])
stress_component = get_component_name(increment, "STRESS", ["MISES", "S11"])
strain_component = get_component_name(increment, "TOSTRAIN", ["E11", "MISES"])
force_component = get_component_name(increment, "FORC", ["F1", "ALL"])

print("Testing result field-output commands on: " + result.name)
print("Step %s, increment %s" % (step.id, increment.id))
print("Components: DISP.%s, STRESS.%s, TOSTRAIN.%s, FORC.%s" %
      (disp_component, stress_component, strain_component, force_component))

field_outputs = result.field_outputs
remove_existing(field_outputs, TEST_NAMES)

created_names = []
export_file = os.path.join(tempfile.gettempdir(), "PrePoMax_Python_Field_Output_Test.csv")

try:
    # ADD: limit output with two filters. This also tests median and smooth filter creation.
    output = field_outputs.create_field_output_limit(
        name=LIMIT_NAME,
        field_name="STRESS",
        component_name=stress_component,
        based_on="AllElements",
        item_limits={"All elements": 235.0}
    )
    output.filter1 = field_outputs.create_median_filter(
        source="SurfaceNodes",
        number_of_layers=2,
        number_of_iterations=1
    )
    output.filter2 = field_outputs.create_smooth_filter(
        source="AllNodes",
        number_of_layers=1,
        number_of_iterations=2
    )
    added = field_outputs.add(output)
    created_names.append(LIMIT_NAME)
    check(field_outputs.contains(LIMIT_NAME), "add() did not create the limit output.")
    check(added.name == LIMIT_NAME, "add() returned the wrong limit output.")
    print("PASS: add limit field output")

    # REPLACE: keep the name, change the limit, and use the remaining average filter type.
    replacement = field_outputs.create_field_output_limit(
        name=LIMIT_NAME,
        field_name="STRESS",
        component_name=stress_component,
        based_on="AllElements",
        item_limits={"All elements": 250.0}
    )
    replacement.filter1 = field_outputs.create_average_filter(
        source="SurfaceNodes",
        number_of_layers=2,
        number_of_iterations=1
    )
    replaced = field_outputs.replace(LIMIT_NAME, replacement)
    check(field_outputs.contains(LIMIT_NAME), "replace() removed the limit output.")
    check(replaced.name == LIMIT_NAME, "replace() returned the wrong limit output.")
    print("PASS: replace limit field output")

    # OPTIONS: an option is the name of its enumeration, matched ignoring case like every model side
    # option, and read back in the spelling the argument takes
    limit_output = field_outputs[LIMIT_NAME]
    check(limit_output.based_on == "AllElements", "based_on read back as %s" % limit_output.based_on)
    check(limit_output.filter1.type == "Average" and limit_output.filter1.source == "SurfaceNodes",
          "the filter read back as %s, %s" % (limit_output.filter1.type, limit_output.filter1.source))
    check(field_outputs.create_median_filter(source="surfacenodes").source == "SurfaceNodes",
          "the filter source is compared with its case")
    check(field_outputs.create_smooth_filter().source == "AllNodes", "the default filter source is not AllNodes")
    check(field_outputs.create_field_output_limit(LIMIT_NAME, "STRESS", stress_component, "allelements",
                                                  {"All elements": 1.0}).based_on == "AllElements",
          "based_on is compared with its case")
    check_raises(lambda: field_outputs.create_median_filter(source="surface_nodes"),
                 "the old filter source spelling was accepted")
    check_raises(lambda: field_outputs.create_field_output_limit(LIMIT_NAME, "STRESS", stress_component,
                                                                 "all_elements", {"All elements": 1.0}),
                 "the old based_on spelling was accepted")
    print("PASS: field output options")

    # NAMES: a draft name is checked as a model item name is, so a padded one is refused rather than
    # trimmed, and a None name is refused by the API rather than by a .NET dictionary
    for create in [lambda: field_outputs.create_field_output_envelope(" " + ENVELOPE_NAME, "TOSTRAIN",
                                                                      strain_component),
                   lambda: field_outputs.create_field_output_equation(EQUATION_NAME + " ", "=1", "/"),
                   lambda: field_outputs.create_field_output_limit("PyTest#Limit", "STRESS", stress_component,
                                                                   "AllElements", {"All elements": 1.0}),
                   lambda: field_outputs.create_field_output_coordinate_system_transform(" PyTest_T", "DISP",
                                                                                         "Any")]:
        check_refused(create, "'name'", "a draft name that is not a valid name was accepted")
    check_refused(lambda: field_outputs[None], "None", "a None field output name was not refused by the API")
    check_refused(lambda: result.mesh.parts[None], "None", "a None part name was not refused by the API")
    check(not pmx.results.contains(None), "contains(None) must be False")
    print("PASS: field output names")

    # MAP: the item limits read back as a plain dict, which goes back into the members that set them
    limits = limit_output.item_limits
    check(isinstance(limits, dict), "the item limits must be a dict: " + str(type(limits)))
    check(len(limits) == 1, "the item limits must hold one limit: " + str(limits))
    limit_name = list(limits.keys())[0]
    check(limits[limit_name] == 250.0, "the item limit read back as %s" % limits[limit_name])
    check(limits == {limit_name: 250.0}, "the item limits read back as " + str(limits))
    check(replacement.item_limits == {"All elements": 250.0}, "the draft item limits must be a dict")
    round_trip = field_outputs.replace_limit(LIMIT_NAME, LIMIT_NAME, "STRESS", stress_component,
                                             limit_output.based_on, limits,
                                             filter1=field_outputs.create_average_filter(
                                                 source="SurfaceNodes", number_of_layers=2))
    check(round_trip.item_limits == limits, "the item limits did not survive a round trip: " +
          str(round_trip.item_limits))
    print("PASS: item limits map")

    # FILTER COUNTS: the counts read back under the argument names that set them
    filter1 = field_outputs[LIMIT_NAME].filter1
    check(filter1.number_of_layers == 2 and filter1.number_of_iterations == 1,
          "the filter counts read back as %s, %s" % (filter1.number_of_layers, filter1.number_of_iterations))
    check(not hasattr(filter1, "layers") and not hasattr(filter1, "iterations"), "the old count names remain")
    check_refused(lambda: field_outputs.create_smooth_filter(number_of_iterations=0), "'number_of_iterations'",
                  "a zero iteration count was accepted")
    print("PASS: filter counts")

    # ADD: envelope output. Its generated AVERAGE component is removed below.
    output = field_outputs.create_field_output_envelope(
        name=ENVELOPE_NAME,
        field_name="TOSTRAIN",
        component_name=strain_component
    )
    field_outputs.add(output)
    created_names.append(ENVELOPE_NAME)
    check(field_outputs.contains(ENVELOPE_NAME), "add() did not create the envelope output.")
    print("PASS: add envelope field output")

    # ADD: equation output using a component that exists in the open result.
    output = field_outputs.create_field_output_equation(
        name=EQUATION_NAME,
        equation="=STRESS.%s / 235.0" % stress_component,
        unit="/"
    )
    field_outputs.add(output)
    created_names.append(EQUATION_NAME)
    check(field_outputs.contains(EQUATION_NAME), "add() did not create the equation output.")
    print("PASS: add equation field output")

    # REPLACE TYPE: a replace never turns one output type into another, and a refused one records nothing
    names_before = list(field_outputs.get_names())
    check_refused(lambda: field_outputs.replace_equation(LIMIT_NAME, LIMIT_NAME, "=1"), "add_limit",
                  "replace_equation turned a limit output into an equation output")
    check_refused(lambda: field_outputs.replace(EQUATION_NAME, field_outputs.create_field_output_envelope(
                      EQUATION_NAME, "TOSTRAIN", strain_component)), "add_equation",
                  "replace turned an equation output into an envelope output")
    check(list(field_outputs.get_names()) == names_before, "a refused replace changed the field outputs")
    check(field_outputs[LIMIT_NAME].item_type == "limit" and field_outputs[EQUATION_NAME].item_type == "equation",
          "a refused replace changed an output type")
    print("PASS: refuse a replace that changes the output type")

    # NAMES: a field output is found in any case and reports the spelling it is stored under
    check(field_outputs.contains(LIMIT_NAME.upper()) and LIMIT_NAME.lower() in field_outputs,
          "contains() compared the field output names with their case")
    check(field_outputs[LIMIT_NAME.upper()].name == LIMIT_NAME,
          "the name read back in the lookup spelling: " + field_outputs[LIMIT_NAME.upper()].name)
    print("PASS: field output names in any case")

    # DUPLICATE: a single name is taken as well as a list
    names_before = list(field_outputs.get_names())
    field_outputs.duplicate(ENVELOPE_NAME.upper())
    copies = [name for name in field_outputs.get_names() if name not in names_before]
    check(len(copies) == 1, "duplicate() of a single name made %s" % copies)
    field_outputs.remove(copies[0])
    check(list(field_outputs.get_names()) == names_before, "the duplicate was not removed by its single name")
    print("PASS: duplicate and remove by a single name")

    # EXPORT: writes every component of the generated limit field at one increment.
    field_outputs.export_to_csv(
        file_name=export_file,
        field_output_name=LIMIT_NAME,
        step_id=step.id,
        increment_id=increment.id
    )
    check(os.path.isfile(export_file), "export_to_csv() did not create the output file.")
    print("PASS: export field output to " + export_file)

    # REMOVE COMPONENTS: operate on a generated output, never on a standard source field.
    field_outputs.remove_components(ENVELOPE_NAME, ["AVERAGE"])
    current_increment = result.steps[step.id].increments[increment.id]
    check(not current_increment.fields[ENVELOPE_NAME].components.contains("AVERAGE"),
          "remove_components() did not remove AVERAGE.")
    print("PASS: remove field-output component")

    # RENAME: the rename the results view records. A new name must not collide with any field of the
    # result, in any case, and a refused rename changes nothing.
    field_outputs.rename(EQUATION_NAME, RENAMED_EQUATION_NAME)
    check(field_outputs.contains(RENAMED_EQUATION_NAME) and not field_outputs.contains(EQUATION_NAME),
          "rename() did not rename the equation output.")
    check_raises(lambda: field_outputs.rename(RENAMED_EQUATION_NAME, LIMIT_NAME.upper()),
                 "a rename onto another field output in other case was accepted")
    check_raises(lambda: field_outputs.rename(RENAMED_EQUATION_NAME, "stress"),
                 "a rename onto a field of the result was accepted")
    check_raises(lambda: field_outputs.rename(RENAMED_EQUATION_NAME, ""), "an empty new name was accepted")
    check_raises(lambda: field_outputs.rename("__missing_field_output__", EQUATION_NAME),
                 "renaming a missing field output was accepted")
    check(field_outputs.contains(RENAMED_EQUATION_NAME), "a refused rename changed the field outputs")
    field_outputs.rename(RENAMED_EQUATION_NAME, EQUATION_NAME)
    check(field_outputs.contains(EQUATION_NAME), "rename() did not rename the equation output back.")
    print("PASS: rename field output")

    # REMOVE: exercise both a single-item command and a multi-item command.
    field_outputs.remove(EQUATION_NAME.upper())
    created_names.remove(EQUATION_NAME)
    check(not field_outputs.contains(EQUATION_NAME), "remove() did not remove the equation output.")
    print("PASS: remove one field output")

    field_outputs.remove(list(created_names))
    created_names = []
    for name in TEST_NAMES:
        check(not field_outputs.contains(name), "remove() did not remove '%s'." % name)
    print("PASS: remove multiple field outputs")
    print("RESULT: all result field-output command tests passed")
    print("TEST PASSED")
finally:
    # Make the script rerunnable after a failed assertion or command.
    remove_existing(field_outputs, TEST_NAMES)

