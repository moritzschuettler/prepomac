#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the result history output definitions: from field, by equation, by integration, merged,
#extracted, XY and from table, with their filters and the CSV export. A result must be open before
#the script runs, so it is run after Test_Analysis.py, which solves the cube and writes
#PyTest_Job.frd into the work directory.
#
#  PrePoMax.com -f PyTest_Job.frd --script Test_Result_History_Outputs.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile

FROM_FIELD_NAME = "PyTest_History_from_field"
FROM_FIELD_OPTIONS_NAME = "PyTest_History_from_field_options"
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
                 ELEMENT_SIZE_NAME, MERGE_SOURCE_2_NAME, MERGE_SOURCE_1_NAME, FROM_FIELD_NAME,
                 FROM_FIELD_OPTIONS_NAME]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
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
        if len(region.get_node_ids()) > 0:
            regions.append(region)
            break
    return regions


def find_solid_element_set(result):
    for name in result.mesh.element_sets.get_names():
        region = result.mesh.element_sets[str(name)]
        ids = region.get_element_ids()
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

node_selection = pmx.create.selection_by_ids(
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
    output.output_node_coordinates = "Undeformed"
    output.filter1 = history_outputs.create_average_filter(operate_on="Rows")
    output.filter2 = history_outputs.create_maximum_filter(return_value="Row")
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
    replacement.output_node_coordinates = "Off"
    replacement.filter1 = history_outputs.create_minimum_filter(return_value="Column")
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

    # NAMED REGION: pmx.create.region names a region the way the model side does, and it is resolved by
    # its name against the result mesh
    for named_region in named_field_regions:
        region_type = "NodeSetName" if result.mesh.node_sets.contains(named_region.name) else "SurfaceName"
        draft = history_outputs.create_history_output_from_field(
            name=FROM_FIELD_NAME, field_name="FORC", component_names=[force_component],
            region=pmx.create.region(region_type, named_region.name))
        check(draft.region.region_type == region_type and draft.region.name == named_region.name,
              "the draft read its named region back as %s" % draft.region)
        replaced = history_outputs.replace(FROM_FIELD_NAME, draft)
        check(replaced.region_type == region_type and replaced.region_name == named_region.name,
              "the named region read back as %s '%s'" % (replaced.region_type, replaced.region_name))
        print("PASS: replace From Field history output using pmx.create.region " + named_region.name)
    names_before = get_history_output_names(history_outputs)
    check_raises(lambda: history_outputs.add_from_field(
                     name="PyTest_History_missing_region", field_name="DISP", component_names=[disp_component],
                     region=pmx.create.region("NodeSetName", "PyTest_History_no_such_node_set")),
                 "a named region missing from the result mesh was accepted")
    check_raises(lambda: history_outputs.create_history_output_from_field(
                     name="PyTest_History_part_region", field_name="DISP", component_names=[disp_component],
                     region=pmx.create.region("PartName", "PyTest_History_any_part")),
                 "a From Field history output accepted a PartName region")
    check_raises(lambda: history_outputs.create_history_output_from_integration(
                     name="PyTest_History_node_set_integration", integral_type="Volume",
                     region=pmx.create.region("NodeSetName", "PyTest_History_any_node_set")),
                 "a volume integration accepted a NodeSetName region")
    check(get_history_output_names(history_outputs) == names_before,
          "a refused named region left a history output behind")
    print("PASS: refuse a named region the result mesh or the history output cannot take")
    # A draft name is checked as a model item name is: a padded one is refused, never trimmed
    for name in [" PyTest_History_padded", "PyTest_History_padded ", "PyTest#History"]:
        try:
            history_outputs.create_history_output_from_equation(name=name, equation="=1", unit="/")
        except Exception as ex:
            check("'name'" in str(ex), "the refusal must name the argument: " + str(ex))
        else:
            raise Exception("TEST FAILED: the history output name '" + name + "' was accepted")
    check(get_history_output_names(history_outputs) == names_before, "a refused name left a history output behind")
    print("PASS: refuse a history output name that is not a valid name")

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
        element_selection = pmx.create.selection_by_ids(
            view="results",
            target="elements",
            source="elements",
            ids=[solid_element_id]
        )
        solid_element_set = find_solid_element_set(result)
        volume_region = solid_element_set if solid_element_set is not None else element_selection

        output = history_outputs.create_history_output_from_integration(
            name=ELEMENT_SIZE_NAME,
            integral_type="Volume",
            region=volume_region,
            integral_result="Geometry",
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
            integral_type="Volume",
            field_name="STRESS",
            component_names=[stress_component],
            region=volume_region,
            integral_result="Field",
            deformation_variable_name="Displacements",
            unit="/"
        )
        history_outputs.add(output)
        created_names.append(INTEGRATION_NAME)
        check(history_outputs.contains(INTEGRATION_NAME),
              "add() did not create the volume-integration output.")
        print("PASS: add From Integration history output")

        # OPTIONS: an option is the name of its enumeration, matched ignoring case like every model
        # side option, and read back in the spelling the argument takes
        integration_output = history_outputs[INTEGRATION_NAME]
        check(integration_output.integral_type == "Volume" and integration_output.integral_result == "Field" and
              integration_output.cross_section == "None",
              "the integration options read back as %s, %s, %s" % (integration_output.integral_type,
                                                                    integration_output.integral_result,
                                                                    integration_output.cross_section))
        check(integration_output.region_type in ["Selection", "ElementSetName"],
              "the integration region type read back as %s" % integration_output.region_type)
        draft = history_outputs.create_history_output_from_integration(
            name=INTEGRATION_NAME, integral_type="volume", region=volume_region, integral_result="GEOMETRY",
            cross_section="none")
        check(draft.integral_type == "Volume" and draft.integral_result == "Geometry" and
              draft.cross_section == "None", "the integration options are compared with their case")
        check_raises(lambda: history_outputs.create_history_output_from_integration(
                         name=INTEGRATION_NAME, integral_type="Volume", field_name="STRESS",
                         component_names=[stress_component], region=volume_region,
                         integral_result="field_by_geometry"),
                     "the old integral_result spelling was accepted")
        # The deformation is an option string as well: the results call it 'Surface Normals', the API
        # SurfaceNormals, and it reads back in the spelling the argument takes
        check(integration_output.deformation_variable_name == "Displacements",
              "the deformation read back as " + integration_output.deformation_variable_name)
        draft = history_outputs.create_history_output_from_integration(
            name=INTEGRATION_NAME, integral_type="Volume", region=volume_region,
            deformation_variable_name="surfacenormals")
        check(draft.deformation_variable_name == "SurfaceNormals",
              "the deformation draft read back as " + draft.deformation_variable_name)
        for old_spelling in ["Surface Normals", "Disp&Def"]:
            check_raises(lambda: history_outputs.create_history_output_from_integration(
                             name=INTEGRATION_NAME, integral_type="Volume", region=volume_region,
                             deformation_variable_name=old_spelling),
                         "the results spelling '%s' was accepted" % old_spelling)
        print("PASS: From Integration history output options")
        # A value the integral does not use is refused rather than stored and ignored
        names_before = get_history_output_names(history_outputs)
        refused_name = INTEGRATION_NAME + "_refused"
        check_raises(lambda: history_outputs.add_from_integration(
                         name=refused_name, integral_type="Volume", region=volume_region,
                         field_name="STRESS", component_names=[stress_component]),
                     "a Geometry integral took field_name and component_names")
        check_raises(lambda: history_outputs.add_from_integration(
                         name=refused_name, integral_type="Volume", region=volume_region, thickness=2.0),
                     "thickness was taken without the Thickness cross_section")
        check_raises(lambda: history_outputs.add_from_integration(
                         name=refused_name, integral_type="Volume", region=volume_region,
                         cross_section="Axisymmetric", thickness=2.0),
                     "thickness was taken with the Axisymmetric cross_section")
        check_raises(lambda: history_outputs.add_from_integration(name=refused_name, integral_type="Volume"),
                     "an integration without a region was accepted")
        check(get_history_output_names(history_outputs) == names_before,
              "a refused integration left a history output behind")
        # The dialog offers the cross section of a Line and a Volume integral on any result, so a 3D
        # result takes it too, and the thickness reads back only where it applies
        draft = history_outputs.create_history_output_from_integration(
            name=INTEGRATION_NAME, integral_type="Volume", region=volume_region, cross_section="Thickness",
            thickness=2.0)
        check(draft.cross_section == "Thickness" and draft.thickness == 2.0,
              "the thickness draft read back %s, %s" % (draft.cross_section, draft.thickness))
        draft = history_outputs.create_history_output_from_integration(
            name=INTEGRATION_NAME, integral_type="Volume", region=volume_region, cross_section="Axisymmetric")
        check(draft.thickness is None and draft.field_name is None and draft.component_names is None,
              "a Geometry draft without the Thickness cross_section read back a value it does not take")
        geometry_output = history_outputs[ELEMENT_SIZE_NAME]
        check(geometry_output.field_name is None and geometry_output.component_names is None and
              geometry_output.thickness is None,
              "the Geometry integral read back a field or a thickness it does not take")
        check(integration_output.field_name == "STRESS" and
              list(integration_output.component_names) == [stress_component],
              "the Field integral did not read back its field")
        print("PASS: refuse an integration value that does not apply, and read back only what applies")
        if solid_element_set is not None:
            replaced = history_outputs.replace_from_integration(
                INTEGRATION_NAME, name=INTEGRATION_NAME, integral_type="Volume", field_name="STRESS",
                component_names=[stress_component],
                region=pmx.create.region("ElementSetName", solid_element_set.name), integral_result="Field")
            check(replaced.region_type == "ElementSetName" and replaced.region_name == solid_element_set.name,
                  "the named integration region read back as %s '%s'" % (replaced.region_type,
                                                                         replaced.region_name))
            print("PASS: replace From Integration history output using pmx.create.region")
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
    replacement.filter1 = history_outputs.create_maximum_filter(return_value="Column")
    history_outputs.replace(TABLE_NAME, replacement)
    check(history_outputs.contains(TABLE_NAME), "replace() removed the table output.")
    print("PASS: replace From Table history output")

    # READ-BACK: a user-defined history output is returned as its type-specific object.
    table_output = history_outputs[TABLE_NAME]
    check(table_output.is_user_defined, "The table output is not reported as user defined.")
    check(table_output.item_type == "from_table",
          "The table output reported the wrong item type: %s" % table_output.item_type)
    check(table_output.number_of_y_columns == 1, "The table output reported the wrong Y-column count.")
    check(table_output.x_axis_unit == "s", "The table output reported the wrong X-axis unit.")
    check(table_output.y_axis_unit == "mm", "The table output reported the wrong Y-axis unit.")
    check(table_output.filter1 is not None, "The table output did not return its filter.")
    check(table_output.filter1.type == "Maximum" and table_output.filter1.return_value == "Column" and
          table_output.filter1.operate_on is None,
          "The table output returned the wrong filter.")
    check(table_output.filter2 is None, "The table output returned an unused second filter.")
    print("PASS: read back a From Table history-output definition")

    merged_output = history_outputs[MERGED_NAME]
    check(merged_output.item_type == "from_merged_histories",
          "The merged output reported the wrong item type: %s" % merged_output.item_type)
    merge_source_1 = [MERGE_SOURCE_1_NAME, merge_field.name, merge_component.name]
    merge_source_2 = [MERGE_SOURCE_2_NAME, merge_field.name, merge_component.name]
    check([list(source) for source in merged_output.sources] == [merge_source_1, merge_source_2],
          "The merged output reported the wrong sources: %s" % [list(s) for s in merged_output.sources])
    extracted_output = history_outputs[EXTRACTED_NAME]
    check(extracted_output.entry_name == str(entry_names[0]),
          "The extracted-entry output reported the wrong entry name.")
    check(list(extracted_output.source) == merge_source_1,
          "The extracted-entry output reported the wrong source: %s" % extracted_output.source)
    xy_output = history_outputs[XY_NAME]
    expected_x_source = [EXTRACTED_NAME, extracted_field.name, extracted_component.name]
    check(list(xy_output.x_source) == expected_x_source,
          "The XY output reported the wrong X source: %s" % xy_output.x_source)
    check(list(xy_output.y_source) == merge_source_1,
          "The XY output reported the wrong Y source: %s" % xy_output.y_source)
    # ROUND TRIP: a source read back is the form a source argument takes, so it is passed straight back.
    history_outputs.replace_xy(XY_NAME, XY_NAME, xy_output.x_source, xy_output.y_source)
    check(list(history_outputs[XY_NAME].x_source) == expected_x_source and
          list(history_outputs[XY_NAME].y_source) == merge_source_1,
          "The XY sources read back did not round trip through replace_xy().")
    history_outputs.replace_from_merged_histories(MERGED_NAME, MERGED_NAME, *merged_output.sources)
    check([list(source) for source in history_outputs[MERGED_NAME].sources] == [merge_source_1, merge_source_2],
          "The merged sources read back did not round trip through replace_from_merged_histories().")
    check(isinstance(merged_output.sources, list), "The merged sources are not a plain Python list.")
    # A source is found regardless of case and stored in the spelling of the result
    history_outputs.replace_from_merged_histories(MERGED_NAME, MERGED_NAME,
                                                  [name.upper() for name in merge_source_1],
                                                  [name.lower() for name in merge_source_2])
    check([list(source) for source in history_outputs[MERGED_NAME].sources] == [merge_source_1, merge_source_2],
          "The merged sources were not stored in the spelling of the result: %s" %
          [list(s) for s in history_outputs[MERGED_NAME].sources])
    history_outputs.replace_xy(XY_NAME, XY_NAME, [name.lower() for name in expected_x_source],
                               [name.upper() for name in merge_source_1])
    check(list(history_outputs[XY_NAME].x_source) == expected_x_source and
          list(history_outputs[XY_NAME].y_source) == merge_source_1,
          "The XY sources were not stored in the spelling of the result.")
    history_outputs.replace_from_extracted_entry(EXTRACTED_NAME, EXTRACTED_NAME,
                                                 [name.upper() for name in merge_source_1], str(entry_names[0]))
    check(list(history_outputs[EXTRACTED_NAME].source) == merge_source_1,
          "The extracted-entry source was not stored in the spelling of the result.")
    # A table column name may contain a dot, so a source is three names rather than one dotted string.
    dotted_source = [TABLE_NAME, "Y", "Force.Measured"]
    dotted_draft = history_outputs.create_history_output_xy(name=XY_NAME, x_source=expected_x_source,
                                                            y_source=dotted_source)
    merged_draft = history_outputs.create_history_output_from_merged_histories(
        name=MERGED_NAME, source1=merge_source_1, source2=merge_source_2)
    check(isinstance(merged_draft.sources, list) and
          [list(source) for source in merged_draft.sources] == [merge_source_1, merge_source_2],
          "The merged draft sources are not a plain Python list of the three names.")
    check(list(dotted_draft.y_source) == dotted_source,
          "A source with a dotted component name did not read back as its three names: %s" % dotted_draft.y_source)
    print("PASS: read back history sources in the form the source arguments take")
    from_field_output = history_outputs[FROM_FIELD_NAME]
    check(from_field_output.item_type == "from_field",
          "The From Field output reported the wrong item type: %s" % from_field_output.item_type)
    check(len(from_field_output.field_name) > 0, "The From Field output did not report its field name.")
    check(from_field_output.region_type in ["Selection", "NodeSetName", "SurfaceName"],
          "The From Field output reported an unexpected region type: %s" % from_field_output.region_type)
    check(from_field_output.output_node_coordinates == "Off" and from_field_output.complex_result_type == "Real",
          "The From Field output read back %s, %s" % (from_field_output.output_node_coordinates,
                                                      from_field_output.complex_result_type))
    # A filter option is matched ignoring case and read back in the spelling the argument takes
    check(history_outputs.create_sum_filter(operate_on="rows").operate_on == "Rows",
          "operate_on is compared with its case")
    check(history_outputs.create_minimum_filter().return_value == "Column", "the default return_value is not Column")
    # A filter reads back under the argument that sets it, and the other one is None
    check(history_outputs.create_sum_filter().return_value is None and
          history_outputs.create_maximum_filter().operate_on is None,
          "a filter read back an option its maker does not take")
    check(not hasattr(history_outputs.create_sum_filter(), "option"), "the filter still exposes option")
    round_trip = history_outputs.create_maximum_filter(
        return_value=history_outputs.create_maximum_filter(return_value="Row").return_value)
    check(round_trip.return_value == "Row", "the return_value read back did not go back into the filter maker")
    check(history_outputs.create_average_filter().type == "Average", "the filter type is not read back as Average")
    check_raises(lambda: history_outputs.create_maximum_filter(return_value="Rows"),
                 "a min/max filter took an aggregate option")
    options_draft = history_outputs.create_history_output_from_field(
        name=FROM_FIELD_NAME, field_name="DISP", component_names=[disp_component], region=node_selection)
    options_draft.output_node_coordinates = "deformed"
    options_draft.complex_result_type = "realatangle"
    check(options_draft.output_node_coordinates == "Deformed" and options_draft.complex_result_type == "RealAtAngle",
          "the From Field options are compared with their case")

    def set_old_complex_result_type():
        options_draft.complex_result_type = "angle"
    check_raises(set_old_complex_result_type, "the old complex_result_type spelling was accepted")
    # The one call form takes the options of the draft, which is the form the session journal writes
    options_output = history_outputs.add_from_field(FROM_FIELD_OPTIONS_NAME, "DISP", [disp_component],
                                                    node_selection, step_id=step.id, increment_id=increment.id,
                                                    output_node_coordinates="deformed")
    created_names.append(FROM_FIELD_OPTIONS_NAME)
    check(options_output.step_id == step.id and options_output.increment_id == increment.id and
          options_output.output_node_coordinates == "Deformed",
          "add_from_field() did not apply its options: %s, %s, %s" % (options_output.step_id,
                                                                     options_output.increment_id,
                                                                     options_output.output_node_coordinates))
    check_raises(lambda: history_outputs.add_from_field(FROM_FIELD_OPTIONS_NAME + "_bad", "DISP",
                                                        [disp_component], node_selection,
                                                        increment_id=increment.id),
                 "an increment without a step was accepted")
    check(not history_outputs.contains(FROM_FIELD_OPTIONS_NAME + "_bad"), "a refused From Field output was added")
    # A result replace re-specifies the whole definition, so an option it is not given returns to its default
    options_output = history_outputs.replace_from_field(FROM_FIELD_OPTIONS_NAME, FROM_FIELD_OPTIONS_NAME, "DISP",
                                                        [disp_component], node_selection, step_id=step.id)
    check(options_output.step_id == step.id and options_output.increment_id is None and
          options_output.output_node_coordinates == "Off",
          "replace_from_field() did not re-specify the options: %s, %s" % (options_output.increment_id,
                                                                          options_output.output_node_coordinates))
    history_outputs.remove([FROM_FIELD_OPTIONS_NAME])
    created_names.remove(FROM_FIELD_OPTIONS_NAME)
    print("PASS: add and replace a From Field history output with its options in one call")
    equation_output = history_outputs[EQUATION_NAME]
    check(equation_output.item_type == "from_equation",
          "The equation output reported the wrong item type: %s" % equation_output.item_type)
    check(equation_output.equation.startswith("="), "The equation output did not report its equation.")
    print("PASS: read back the merged, extracted-entry, XY, From Field, and equation definitions")

    # REPLACE TYPE: a replace never turns one output type into another, and a refused one records nothing
    names_before = get_history_output_names(history_outputs)
    check_raises(lambda: history_outputs.replace_from_equation(TABLE_NAME, TABLE_NAME, "=1"),
                 "replace_from_equation turned a table output into an equation output")
    try:
        history_outputs.replace(EQUATION_NAME, history_outputs.create_history_output_from_table(
            name=EQUATION_NAME, data_points=[[0.0, 0.0], [1.0, 1.0]]))
    except Exception as ex:
        check("add_from_equation" in str(ex) and "replace_from_table" in str(ex),
              "the refusal must name both types: " + str(ex))
    else:
        raise Exception("TEST FAILED: replace turned an equation output into a table output")
    check(get_history_output_names(history_outputs) == names_before, "a refused replace changed the history outputs")
    check(history_outputs[TABLE_NAME].item_type == "from_table", "a refused replace changed the output type")
    print("PASS: refuse a replace that changes the output type")

    # DUPLICATE: the duplicate keeps the definition type and gets a generated name.
    names_before = get_history_output_names(history_outputs)
    history_outputs.duplicate(TABLE_NAME.lower())
    new_names = [name for name in get_history_output_names(history_outputs) if name not in names_before]
    check(len(new_names) == 1, "duplicate() did not create exactly one history output.")
    duplicated_name = new_names[0]
    check(history_outputs[duplicated_name].item_type == "from_table",
          "The duplicated history output has the wrong type.")
    print("PASS: duplicate a history output")

    # RENAME: the rename the results view records, exercised on the duplicate, which nothing references.
    # A new name must not collide with another history output in any case, and a refused rename
    # changes nothing.
    renamed_name = TABLE_NAME + "_renamed"
    history_outputs.rename(duplicated_name, renamed_name)
    check(history_outputs.contains(renamed_name) and not history_outputs.contains(duplicated_name),
          "rename() did not rename the duplicated output.")
    duplicated_name = renamed_name
    check_raises(lambda: history_outputs.rename(duplicated_name, FROM_FIELD_NAME.upper()),
                 "a rename onto another history output in other case was accepted")
    check_raises(lambda: history_outputs.rename(duplicated_name, ""), "an empty new name was accepted")
    check_raises(lambda: history_outputs.rename("__missing_history_output__", TABLE_NAME + "_x"),
                 "renaming a missing history output was accepted")
    check(history_outputs.contains(duplicated_name) and not history_outputs.contains(TABLE_NAME + "_x"),
          "a refused rename changed the history outputs")
    print("PASS: rename a history output")

    # NAMES: a user-defined history output is found in any case, and every command is given its stored name
    other_case = duplicated_name.upper()
    check(history_outputs.contains(other_case) and other_case in history_outputs,
          "contains() compared a user-defined history output with its case.")
    check(history_outputs[other_case].name == duplicated_name,
          "the indexer did not return the stored name: %s" % history_outputs[other_case].name)
    history_outputs.replace_from_table(other_case, duplicated_name, [[0.0, 0.0], [1.0, 2.0]],
                                       y_column_names=["Case_check"])
    replaced = history_outputs[duplicated_name]
    replaced_field = str(replaced.fields.get_names()[0])
    replaced_component = str(replaced.fields[replaced_field].components.get_names()[0])
    check([str(name) for name in replaced.fields[replaced_field].components[replaced_component].get_entry_names()]
          == ["Case_check"], "a replace through another case kept the old data.")
    check(duplicated_name in get_history_output_names(history_outputs) and
          other_case not in get_history_output_names(history_outputs),
          "a replace through another case changed the stored name.")
    check_raises(lambda: history_outputs.remove([duplicated_name, other_case]),
                 "a remove naming one output twice was accepted")
    check(history_outputs.contains(duplicated_name), "a refused remove removed the output")
    print("PASS: find, replace and remove a history output in another case")

    # REMOVE FIELDS AND COMPONENTS: exercised on the duplicate so the tested outputs stay intact.
    duplicated = history_outputs[duplicated_name]
    field_name = str(duplicated.fields.get_names()[0])
    component_name = str(duplicated.fields[field_name].components.get_names()[0])
    history_outputs.remove_components(duplicated_name.lower(), field_name, [component_name])
    check(len(history_outputs[duplicated_name].fields[field_name].components) == 0,
          "remove_components() did not remove the history component.")
    print("PASS: remove a history-output component")

    history_outputs.remove_fields(duplicated_name.lower(), [field_name])
    check(len(history_outputs[duplicated_name].fields) == 0, "remove_fields() did not remove the history field.")
    print("PASS: remove a history-output field")

    # EXPORT: all fields and components of the selected outputs are written to one CSV file.
    csv_file_name = os.path.join(tempfile.gettempdir(), "PyTest_History_export.csv")
    if os.path.exists(csv_file_name):
        os.remove(csv_file_name)
    history_outputs.export_to_csv(csv_file_name, [TABLE_NAME.upper()], delimiter=";")
    check(os.path.exists(csv_file_name), "export_to_csv() did not create the file.")
    os.remove(csv_file_name)
    print("PASS: export a history output to CSV")

    history_outputs.remove(duplicated_name.upper())
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
    # AFFINE FIT: the From Affine Fit output is made in the user interface only, so the API must not
    # offer a way to add or replace one
    for member in ["add_from_affine_fit", "replace_from_affine_fit"]:
        check(not hasattr(history_outputs, member), "the API must not offer %s" % member)
    print("PASS: the API offers no From Affine Fit history output")

    print("RESULT: all applicable result history-output command tests passed")
    print("TEST PASSED")
finally:
    pmx.selection.clear_highlight()
    # Make the script rerunnable after a failed assertion or command.
    remove_existing(history_outputs)
