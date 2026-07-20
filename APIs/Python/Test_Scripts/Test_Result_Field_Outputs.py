#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'

import os
import tempfile


LIMIT_NAME = "PyTest_Stress_limit"
ENVELOPE_NAME = "PyTest_Strain_envelope"
EQUATION_NAME = "PyTest_Normalized_stress"
TEST_NAMES = [LIMIT_NAME, ENVELOPE_NAME, EQUATION_NAME]


def check(condition, message):
    if not condition:
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
        based_on="all_elements",
        item_limits={"All elements": 235.0}
    )
    output.filter1 = field_outputs.create_median_filter(
        source="surface_nodes",
        layers=2,
        iterations=1
    )
    output.filter2 = field_outputs.create_smooth_filter(
        source="all_nodes",
        layers=1,
        iterations=2
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
        based_on="all_elements",
        item_limits={"All elements": 250.0}
    )
    replacement.filter1 = field_outputs.create_average_filter(
        source="surface_nodes",
        layers=2,
        iterations=1
    )
    replaced = field_outputs.replace(LIMIT_NAME, replacement)
    check(field_outputs.contains(LIMIT_NAME), "replace() removed the limit output.")
    check(replaced.name == LIMIT_NAME, "replace() returned the wrong limit output.")
    print("PASS: replace limit field output")

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

    # REMOVE: exercise both a single-item command and a multi-item command.
    field_outputs.remove([EQUATION_NAME])
    created_names.remove(EQUATION_NAME)
    check(not field_outputs.contains(EQUATION_NAME), "remove() did not remove the equation output.")
    print("PASS: remove one field output")

    field_outputs.remove(list(created_names))
    created_names = []
    for name in TEST_NAMES:
        check(not field_outputs.contains(name), "remove() did not remove '%s'." % name)
    print("PASS: remove multiple field outputs")
    print("RESULT: all result field-output command tests passed")
finally:
    # Make the script rerunnable after a failed assertion or command.
    remove_existing(field_outputs, TEST_NAMES)

