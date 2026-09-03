#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
# Reads one history output component, multiplies all its values by a factor and stores the
# scaled curves in a new user-defined From Table history output. Every entry of the source
# component becomes one Y column of the table.

SOURCE_COMPONENT_PATH = "From_Field-1.STRESS.MISES"
NEW_HISTORY_OUTPUT_NAME = "From_Field-1_STRESS_MISES_x2"
FACTOR = 2.0
X_AXIS_UNIT = "/"
TIME_TOLERANCE = 1E-9


def resolve_component(history_outputs, component_path):
    parts = component_path.split(".")
    if len(parts) != 3:
        raise Exception("The source '%s' must be given as "
                        "'history_output_name.field_name.component_name'." % component_path)
    history_output_name, field_name, component_name = parts
    if not history_outputs.contains(history_output_name):
        raise Exception("The history output '%s' does not exist in the current result." % history_output_name)
    fields = history_outputs[history_output_name].fields
    if not fields.contains(field_name):
        raise Exception("The history output '%s' does not contain the field '%s'." %
                        (history_output_name, field_name))
    components = fields[field_name].components
    if not components.contains(component_name):
        raise Exception("The field '%s' does not contain the component '%s'." % (field_name, component_name))
    return components[component_name]


def get_entry_curves(component):
    entry_names = component.get_entry_names()
    if len(entry_names) == 0:
        raise Exception("The component '%s' contains no entries." % component.name)
    curves = []
    for index in range(len(entry_names)):
        entry_name = str(entry_names[index])
        data = component.entries[entry_name].get_data()
        time = [float(value) for value in data.time]
        values = [float(value) for value in data.values]
        if len(time) != len(values):
            raise Exception("The entry '%s' contains %d time values and %d result values." %
                            (entry_name, len(time), len(values)))
        curves.append((entry_name, time, values, str(data.unit)))
    return curves


def get_common_time(curves):
    entry_name, time, values, unit = curves[0]
    for other_name, other_time, other_values, other_unit in curves[1:]:
        if len(other_time) != len(time):
            raise Exception("The entries '%s' and '%s' contain a different number of time points. "
                            "Only entries sharing one time axis can be written to a table." %
                            (entry_name, other_name))
        for index in range(len(time)):
            if abs(other_time[index] - time[index]) > TIME_TOLERANCE:
                raise Exception("The entries '%s' and '%s' use different time values. "
                                "Only entries sharing one time axis can be written to a table." %
                                (entry_name, other_name))
    return time


def get_column_names(curves):
    column_names = []
    used_names = set()
    for entry_name, time, values, unit in curves:
        column_name = entry_name if len(entry_name.strip()) > 0 else "Y%d" % (len(column_names) + 1)
        while column_name.lower() in used_names:
            column_name += "_"
        used_names.add(column_name.lower())
        column_names.append(column_name)
    return column_names


def get_y_axis_unit(curves):
    units = set(unit for entry_name, time, values, unit in curves)
    if len(units) == 1:
        unit = curves[0][3]
        if len(unit.strip()) > 0:
            return unit
    return "/"


def build_data_points(time, curves, factor):
    data_points = []
    for row in range(len(time)):
        data_row = [time[row]]
        for entry_name, entry_time, entry_values, entry_unit in curves:
            data_row.append(entry_values[row] * factor)
        data_points.append(data_row)
    return data_points


def main():
    result = pmx.results.current
    history_outputs = result.history_outputs
    component = resolve_component(history_outputs, SOURCE_COMPONENT_PATH)
    curves = get_entry_curves(component)
    time = get_common_time(curves)
    data_points = build_data_points(time, curves, FACTOR)
    if history_outputs.contains(NEW_HISTORY_OUTPUT_NAME):
        history_outputs.remove([NEW_HISTORY_OUTPUT_NAME])
    output = history_outputs.create_history_output_from_table(
        name=NEW_HISTORY_OUTPUT_NAME,
        data_points=data_points,
        y_column_names=get_column_names(curves),
        x_axis_unit=X_AXIS_UNIT,
        y_axis_unit=get_y_axis_unit(curves)
    )
    history_outputs.add(output)
    print("Created the history output '%s' from '%s': %d columns, %d rows, factor %g." %
          (NEW_HISTORY_OUTPUT_NAME, SOURCE_COMPONENT_PATH, len(curves), len(data_points), FACTOR))


main()
