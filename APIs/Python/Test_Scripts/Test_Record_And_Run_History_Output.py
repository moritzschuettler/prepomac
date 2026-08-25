# PrePoMax Python Script
# The PrePoMax object is available as 'pmx'.

# Parameters. Surface query IDs are local to a part, so both IDs are required.
PART_ID = 1
SURFACE_ID = 1
FACTOR = 2.0
X_AXIS_UNIT = "s"

SOURCE_OUTPUT_NAME = "PyTest_Surface_1_DISP"
TABLE_OUTPUT_NAME = "PyTest_Surface_1_DISP_x2"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


result = pmx.results.current
check(result is not None, "Open and select a result before running this script.")

history_outputs = result.history_outputs

# Make the script safe to run repeatedly. Remove the derived table before its source.
for output_name in [TABLE_OUTPUT_NAME, SOURCE_OUTPUT_NAME]:
    if history_outputs.contains(output_name):
        history_outputs.remove([output_name])

# Select all result nodes belonging to surface query ID 1 of part ID 1.
surface_nodes = pmx.selection.create_by_ids(
    view="results",
    target="nodes",
    source="surfaces",
    ids={PART_ID: [SURFACE_ID]}
)

# Extract DISP.ALL for all steps and increments on the selected surface.
source_output = history_outputs.create_history_output_from_field(
    name=SOURCE_OUTPUT_NAME,
    field_name="DISP",
    component_names=["ALL"],
    region=surface_nodes
)
history_outputs.add(source_output)

# Read the generated component. For multiple surface nodes, values is a matrix
# with one row per time point and one column per history entry (node).
component = history_outputs[SOURCE_OUTPUT_NAME].fields["DISP"].components["ALL"]
component_data = component.get_data()
entry_count = len(component_data.entry_names)

check(component_data.count > 0, "DISP.ALL contains no time values on the selected surface.")
check(entry_count > 0, "DISP.ALL contains no history entries on the selected surface.")

table_rows = []
for row_index in range(component_data.count):
    row = [float(component_data.time[row_index])]
    for column_index in range(entry_count):
        if entry_count == 1:
            value = component_data.values[row_index]
        else:
            value = component_data.values[row_index][column_index]
        row.append(float(value) * FACTOR)
    table_rows.append(row)

column_names = []
for entry_name in component_data.entry_names:
    column_names.append("%s_x%g" % (str(entry_name), FACTOR))

table_output = history_outputs.create_history_output_from_table(
    name=TABLE_OUTPUT_NAME,
    data_points=table_rows,
    y_column_names=column_names,
    x_axis_unit=X_AXIS_UNIT,
    y_axis_unit=component_data.unit
)
history_outputs.add(table_output)

print("Created history output '%s' from part %s, surface %s." %
      (SOURCE_OUTPUT_NAME, PART_ID, SURFACE_ID))
print("Created table history output '%s' with %s DISP.ALL column(s) multiplied by %g." %
      (TABLE_OUTPUT_NAME, entry_count, FACTOR))
