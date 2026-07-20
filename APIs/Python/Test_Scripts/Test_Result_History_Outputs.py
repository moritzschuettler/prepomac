#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'


FROM_FIELD_NAME = "PyTest_History_from_field"
EQUATION_NAME = "PyTest_History_equation"
ELEMENT_SIZE_NAME = "PyTest_History_element_volume"
INTEGRATION_NAME = "PyTest_History_volume_integration"

# Equation outputs must be removed before outputs referenced by their equations.
CLEANUP_ORDER = [EQUATION_NAME, INTEGRATION_NAME, ELEMENT_SIZE_NAME, FROM_FIELD_NAME]


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


def remove_existing(history_outputs):
    for name in CLEANUP_ORDER:
        if history_outputs.contains(name):
            history_outputs.remove([name])


def find_first_solid_element_id(result):
    element_ids = result.mesh.elements.get_ids()
    for index in range(len(element_ids)):
        element_id = int(element_ids[index])
        if result.mesh.elements[element_id].dimension == 3:
            return element_id
    return None


def get_nonempty_named_field_regions(result):
    regions = []
    for name in result.mesh.node_sets.get_names():
        region = result.mesh.node_sets[str(name)]
        if region.count > 0:
            regions.append(region)
            break
    for name in result.mesh.surfaces.get_names():
        region = result.mesh.surfaces[str(name)]
        if len(region.node_ids) > 0:
            regions.append(region)
            break
    return regions


def find_solid_element_set(result):
    for name in result.mesh.element_sets.get_names():
        region = result.mesh.element_sets[str(name)]
        ids = region.element_ids
        if len(ids) == 0:
            continue
        all_solid = True
        for index in range(len(ids)):
            if result.mesh.elements[int(ids[index])].dimension != 3:
                all_solid = False
                break
        if all_solid:
            return region
    return None


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

node_ids = result.mesh.nodes.get_ids()
check(len(node_ids) > 0, "The current result mesh contains no nodes.")
node_id = int(node_ids[0])
solid_element_id = find_first_solid_element_id(result)

print("Testing result history-output commands on: " + result.name)
print("Step %s, increment %s, node %s" % (step.id, increment.id, node_id))
print("Components: DISP.%s, STRESS.%s, TOSTRAIN.%s, FORC.%s" %
      (disp_component, stress_component, strain_component, force_component))

history_outputs = result.history_outputs
remove_existing(history_outputs)

node_selection = pmx.selection.create_by_ids(
    view="results",
    target="nodes",
    source="nodes",
    ids=[node_id]
)

created_names = []

try:
    # Highlighting uses the same selection that will be stored in the history-output command.
    pmx.selection.highlight(node_selection)
    pmx.selection.clear_highlight()
    print("PASS: create and highlight a result node selection")

    # ADD: From Field output for one node and one result increment.
    output = history_outputs.create_history_output_from_field(
        name=FROM_FIELD_NAME,
        field_name="DISP",
        component_names=[disp_component],
        region=node_selection
    )
    output.step_id = step.id
    output.increment_id = increment.id
    output.output_node_coordinates = "undeformed"
    output.filter1 = history_outputs.create_average_filter(operate_on="rows")
    output.filter2 = history_outputs.create_maximum_filter(return_value="row")
    added = history_outputs.add(output)
    created_names.append(FROM_FIELD_NAME)
    check(history_outputs.contains(FROM_FIELD_NAME), "add() did not create the From Field output.")
    check(added.name == FROM_FIELD_NAME, "add() returned the wrong From Field output.")
    print("PASS: add From Field history output")

    # REPLACE: keep the name but change the source field, region, and filter options.
    named_field_regions = get_nonempty_named_field_regions(result)
    replacement_region = named_field_regions[0] if len(named_field_regions) > 0 else node_selection
    replacement = history_outputs.create_history_output_from_field(
        name=FROM_FIELD_NAME,
        field_name="FORC",
        component_names=[force_component],
        region=replacement_region
    )
    replacement.step_id = step.id
    replacement.increment_id = increment.id
    replacement.output_node_coordinates = "off"
    replacement.filter1 = history_outputs.create_minimum_filter(return_value="column")
    replaced = history_outputs.replace(FROM_FIELD_NAME, replacement)
    check(history_outputs.contains(FROM_FIELD_NAME), "replace() removed the From Field output.")
    check(replaced.name == FROM_FIELD_NAME, "replace() returned the wrong From Field output.")
    print("PASS: replace From Field history output")

    # Exercise every available named From Field region through command history.
    for named_region in named_field_regions[1:]:
        replacement = history_outputs.create_history_output_from_field(
            name=FROM_FIELD_NAME,
            field_name="FORC",
            component_names=[force_component],
            region=named_region
        )
        replacement.step_id = step.id
        replacement.increment_id = increment.id
        replaced = history_outputs.replace(FROM_FIELD_NAME, replacement)
        print("PASS: replace From Field history output using " + named_region.name)

    # ADD: equation output referencing the generated history set, field, and component.
    generated_field = replaced.fields.first()
    check(generated_field is not None, "The From Field output contains no generated field.")
    generated_component = generated_field.components.first()
    check(generated_component is not None, "The From Field output contains no generated component.")
    equation_parameter = "%s.%s.%s" % (
        FROM_FIELD_NAME,
        generated_field.name,
        generated_component.name
    )
    output = history_outputs.create_history_output_from_equation(
        name=EQUATION_NAME,
        equation="=" + equation_parameter,
        unit="/"
    )
    history_outputs.add(output)
    created_names.append(EQUATION_NAME)
    check(history_outputs.contains(EQUATION_NAME), "add() did not create the equation output.")
    print("PASS: add history-output equation")

    # REPLACE: change the equation while preserving its name and dependency.
    replacement = history_outputs.create_history_output_from_equation(
        name=EQUATION_NAME,
        equation="=2 * " + equation_parameter,
        unit="/"
    )
    history_outputs.replace(EQUATION_NAME, replacement)
    check(history_outputs.contains(EQUATION_NAME), "replace() removed the equation output.")
    print("PASS: replace history-output equation")

    # Volume outputs need a 3D element. Shell-only results skip these two draft types.
    if solid_element_id is not None:
        element_selection = pmx.selection.create_by_ids(
            view="results",
            target="elements",
            source="elements",
            ids=[solid_element_id]
        )
        solid_element_set = find_solid_element_set(result)
        volume_region = solid_element_set if solid_element_set is not None else element_selection

        output = history_outputs.create_history_output_from_element_size(
            name=ELEMENT_SIZE_NAME,
            size_type="volume",
            region=volume_region,
            deformation_variable_name="Displacements"
        )
        history_outputs.add(output)
        created_names.append(ELEMENT_SIZE_NAME)
        check(history_outputs.contains(ELEMENT_SIZE_NAME),
              "add() did not create the element-volume output.")
        print("PASS: add From Element Size history output")

        output = history_outputs.create_history_output_from_integration(
            name=INTEGRATION_NAME,
            integral_type="volume",
            field_name="STRESS",
            component_names=[stress_component],
            region=volume_region,
            deformation_variable_name="Displacements",
            unit="/"
        )
        history_outputs.add(output)
        created_names.append(INTEGRATION_NAME)
        check(history_outputs.contains(INTEGRATION_NAME),
              "add() did not create the volume-integration output.")
        print("PASS: add From Integration history output")
    else:
        print("SKIP: element-size and volume-integration tests require a 3D element")

    # REMOVE: remove the dependent equation first, then all remaining test outputs together.
    history_outputs.remove([EQUATION_NAME])
    created_names.remove(EQUATION_NAME)
    check(not history_outputs.contains(EQUATION_NAME), "remove() did not remove the equation output.")
    print("PASS: remove one history output")

    history_outputs.remove(list(created_names))
    created_names = []
    for name in CLEANUP_ORDER:
        check(not history_outputs.contains(name), "remove() did not remove '%s'." % name)
    print("PASS: remove multiple history outputs")
    print("RESULT: all applicable result history-output command tests passed")
finally:
    pmx.selection.clear_highlight()
    # Make the script rerunnable after a failed assertion or command.
    remove_existing(history_outputs)
