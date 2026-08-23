#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'

import os
import tempfile

FROM_FIELD_NAME = "PyTest_History_from_field"
EQUATION_NAME = "PyTest_History_equation"
ELEMENT_SIZE_NAME = "PyTest_History_element_volume"
INTEGRATION_NAME = "PyTest_History_volume_integration"
MERGE_SOURCE_1_NAME = "PyTest_History_merge_source_1"
MERGE_SOURCE_2_NAME = "PyTest_History_merge_source_2"
MERGED_NAME = "PyTest_History_merged"
EXTRACTED_NAME = "PyTest_History_extracted"
XY_NAME = "PyTest_History_xy"
TABLE_NAME = "PyTest_History_table"

# Derived outputs must be removed before the outputs their equations and sources reference.
CLEANUP_ORDER = [EQUATION_NAME, XY_NAME, MERGED_NAME, EXTRACTED_NAME, TABLE_NAME, INTEGRATION_NAME,
                 ELEMENT_SIZE_NAME, MERGE_SOURCE_2_NAME, MERGE_SOURCE_1_NAME, FROM_FIELD_NAME]


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


def get_history_output_names(history_outputs):
    names = history_outputs.get_names()
    return [str(names[index]) for index in range(len(names))]


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

        output = history_outputs.create_history_output_from_integration(
            name=ELEMENT_SIZE_NAME,
            integral_type="volume",
            region=volume_region,
            integral_result="geometry",
            deformation_variable_name="Displacements",
            unit="/"
        )
        history_outputs.add(output)
        created_names.append(ELEMENT_SIZE_NAME)
        check(history_outputs.contains(ELEMENT_SIZE_NAME),
              "add() did not create the element-volume output.")
        print("PASS: add From Integration history output of the geometry")

        output = history_outputs.create_history_output_from_integration(
            name=INTEGRATION_NAME,
            integral_type="volume",
            field_name="STRESS",
            component_names=[stress_component],
            region=volume_region,
            integral_result="field",
            deformation_variable_name="Displacements",
            unit="/"
        )
        history_outputs.add(output)
        created_names.append(INTEGRATION_NAME)
        check(history_outputs.contains(INTEGRATION_NAME),
              "add() did not create the volume-integration output.")
        print("PASS: add From Integration history output")
    else:
        print("SKIP: geometry- and field-volume-integration tests require a 3D element")

    # ADD: two identical From Field outputs that share a unit and a time base, so they can be merged.
    for merge_name in [MERGE_SOURCE_1_NAME, MERGE_SOURCE_2_NAME]:
        output = history_outputs.create_history_output_from_field(
            name=merge_name,
            field_name="DISP",
            component_names=[disp_component],
            region=node_selection
        )
        history_outputs.add(output)
        created_names.append(merge_name)
        check(history_outputs.contains(merge_name), "add() did not create '%s'." % merge_name)
    print("PASS: add the From Field history outputs used as merge sources")

    merge_field = history_outputs[MERGE_SOURCE_1_NAME].fields.first()
    merge_component = merge_field.components.first()
    check(merge_component is not None, "The merge source output contains no generated component.")

    # ADD: merge the two source components. The second source is given as a name list instead of an object.
    output = history_outputs.create_history_output_from_merged_histories(
        name=MERGED_NAME,
        source1=merge_component,
        source2=[MERGE_SOURCE_2_NAME, merge_field.name, merge_component.name]
    )
    added = history_outputs.add(output)
    created_names.append(MERGED_NAME)
    check(history_outputs.contains(MERGED_NAME), "add() did not create the merged output.")
    check(added.name == MERGED_NAME, "add() returned the wrong merged output.")
    print("PASS: add From Merged Histories history output")

    # ADD: extract one named entry of the merge source component into its own history output.
    entry_names = merge_component.get_entry_names()
    check(len(entry_names) > 0, "The merge source component contains no history entries.")
    output = history_outputs.create_history_output_from_extracted_entry(
        name=EXTRACTED_NAME,
        source=merge_component,
        entry_name=str(entry_names[0])
    )
    history_outputs.add(output)
    created_names.append(EXTRACTED_NAME)
    check(history_outputs.contains(EXTRACTED_NAME), "add() did not create the extracted-entry output.")
    print("PASS: add From Extracted Entry history output")

    # ADD: the extracted output always holds exactly one entry, so it can drive the X axis.
    extracted_field = history_outputs[EXTRACTED_NAME].fields.first()
    extracted_component = extracted_field.components.first()
    output = history_outputs.create_history_output_xy(
        name=XY_NAME,
        x_source=extracted_component,
        y_source=merge_component
    )
    history_outputs.add(output)
    created_names.append(XY_NAME)
    check(history_outputs.contains(XY_NAME), "add() did not create the XY output.")
    print("PASS: add XY history output")

    # ADD: a user-defined table needs no result data at all.
    output = history_outputs.create_history_output_from_table(
        name=TABLE_NAME,
        data_points=[[0.0, 0.0, 1.0], [1.0, 10.0, 21.0], [2.0, 20.0, 41.0]],
        y_column_names=["Measured", "Computed"],
        x_axis_unit="mm",
        y_axis_unit="N"
    )
    check(output.number_of_y_columns == 2, "The table draft did not take the column count from the data.")
    history_outputs.add(output)
    created_names.append(TABLE_NAME)
    check(history_outputs.contains(TABLE_NAME), "add() did not create the table output.")
    print("PASS: add From Table history output")

    # REPLACE: change the table data and let the column names default to Y1.
    replacement = history_outputs.create_history_output_from_table(
        name=TABLE_NAME,
        data_points=[[0.0, 0.0], [1.0, 5.0]],
        x_axis_unit="s",
        y_axis_unit="mm"
    )
    check(str(replacement.y_column_names[0]) == "Y1", "The table draft did not generate default column names.")
    replacement.filter1 = history_outputs.create_maximum_filter(return_value="column")
    history_outputs.replace(TABLE_NAME, replacement)
    check(history_outputs.contains(TABLE_NAME), "replace() removed the table output.")
    print("PASS: replace From Table history output")

    # READ-BACK: a user-defined history output is returned as its type-specific object.
    table_output = history_outputs[TABLE_NAME]
    check(table_output.is_user_defined, "The table output is not reported as user defined.")
    check(table_output.type_name == "ResultHistoryOutputFromTable",
          "The table output reported the wrong type name: %s" % table_output.type_name)
    check(table_output.number_of_y_columns == 1, "The table output reported the wrong Y-column count.")
    check(table_output.x_axis_unit == "s", "The table output reported the wrong X-axis unit.")
    check(table_output.y_axis_unit == "mm", "The table output reported the wrong Y-axis unit.")
    check(table_output.filter1 is not None, "The table output did not return its filter.")
    check(table_output.filter1.type == "maximum" and table_output.filter1.option == "column",
          "The table output returned the wrong filter.")
    check(table_output.filter2 is None, "The table output returned an unused second filter.")
    print("PASS: read back a From Table history-output definition")

    merged_output = history_outputs[MERGED_NAME]
    check(merged_output.type_name == "ResultHistoryOutputFromMergedHistories",
          "The merged output reported the wrong type name: %s" % merged_output.type_name)
    check(len(merged_output.sources) == 2, "The merged output reported the wrong source count.")
    extracted_output = history_outputs[EXTRACTED_NAME]
    check(extracted_output.entry_name == str(entry_names[0]),
          "The extracted-entry output reported the wrong entry name.")
    xy_output = history_outputs[XY_NAME]
    expected_x_source = "%s.%s.%s" % (EXTRACTED_NAME, extracted_field.name, extracted_component.name)
    check(xy_output.x_source == expected_x_source,
          "The XY output reported the wrong X source: %s" % xy_output.x_source)
    from_field_output = history_outputs[FROM_FIELD_NAME]
    check(from_field_output.type_name == "ResultHistoryOutputFromField",
          "The From Field output reported the wrong type name: %s" % from_field_output.type_name)
    check(len(from_field_output.field_name) > 0, "The From Field output did not report its field name.")
    check(from_field_output.region_type in ["selection", "node_set", "surface"],
          "The From Field output reported an unexpected region type: %s" % from_field_output.region_type)
    equation_output = history_outputs[EQUATION_NAME]
    check(equation_output.type_name == "ResultHistoryOutputFromEquation",
          "The equation output reported the wrong type name: %s" % equation_output.type_name)
    check(equation_output.equation.startswith("="), "The equation output did not report its equation.")
    print("PASS: read back the merged, extracted-entry, XY, From Field, and equation definitions")

    # DUPLICATE: the duplicate keeps the definition type and gets a generated name.
    names_before = get_history_output_names(history_outputs)
    history_outputs.duplicate([TABLE_NAME])
    new_names = [name for name in get_history_output_names(history_outputs) if name not in names_before]
    check(len(new_names) == 1, "duplicate() did not create exactly one history output.")
    duplicated_name = new_names[0]
    check(history_outputs[duplicated_name].type_name == "ResultHistoryOutputFromTable",
          "The duplicated history output has the wrong type.")
    print("PASS: duplicate a history output")

    # REMOVE FIELDS AND COMPONENTS: exercised on the duplicate so the tested outputs stay intact.
    duplicated = history_outputs[duplicated_name]
    field_name = str(duplicated.fields.get_names()[0])
    component_name = str(duplicated.fields[field_name].components.get_names()[0])
    history_outputs.remove_components(duplicated_name, field_name, [component_name])
    check(len(history_outputs[duplicated_name].fields[field_name].components) == 0,
          "remove_components() did not remove the history component.")
    print("PASS: remove a history-output component")

    history_outputs.remove_fields(duplicated_name, [field_name])
    check(len(history_outputs[duplicated_name].fields) == 0, "remove_fields() did not remove the history field.")
    print("PASS: remove a history-output field")

    # EXPORT: all fields and components of the selected outputs are written to one CSV file.
    csv_file_name = os.path.join(tempfile.gettempdir(), "PyTest_History_export.csv")
    if os.path.exists(csv_file_name):
        os.remove(csv_file_name)
    history_outputs.export_to_csv(csv_file_name, [TABLE_NAME], delimiter=";")
    check(os.path.exists(csv_file_name), "export_to_csv() did not create the file.")
    os.remove(csv_file_name)
    print("PASS: export a history output to CSV")

    history_outputs.remove([duplicated_name])
    check(not history_outputs.contains(duplicated_name), "remove() did not remove the duplicated output.")

    # REMOVE: remove the dependent equation first, then all remaining test outputs together.
    history_outputs.remove([EQUATION_NAME])
    created_names.remove(EQUATION_NAME)
    check(not history_outputs.contains(EQUATION_NAME), "remove() did not remove the equation output.")
    print("PASS: remove one history output")

    # The derived outputs reference the merge sources, so they are removed before them.
    for name in [XY_NAME, MERGED_NAME, EXTRACTED_NAME]:
        history_outputs.remove([name])
        created_names.remove(name)
        check(not history_outputs.contains(name), "remove() did not remove '%s'." % name)
    print("PASS: remove the merged, extracted-entry, and XY history outputs")

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
