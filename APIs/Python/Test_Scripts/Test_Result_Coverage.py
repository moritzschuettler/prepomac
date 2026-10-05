#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the result editing commands: the part verbs, the result field output duplication and the
#result lifecycle. A result must be open before the script runs.
#
#  PrePoMax.com -f PyTest_Job.frd --script Test_Result_Coverage.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def test_part_commands():
    parts = pmx.results.current.mesh.parts
    names = parts.get_names().to_array()
    check(len(names) > 0, "the result mesh has no parts")
    name = names[0]
    part = parts[name]
    check(part.volume >= 0, "the result part volume is invalid")
    check(part.surface_area >= 0, "the result part surface area is invalid")
    check(len(part.center_of_mass) in (0, 3), "the result center of mass must be empty or have 3 values")
    check(len(part.get_bounding_box()) == 6, "the result bounding box must have 6 values")
    check(len(part.get_inertia_at_center_of_mass()) in (0, 3), "the result center inertia is invalid")
    check(len(part.get_inertia_at_origin()) in (0, 3), "the result origin inertia is invalid")
    check(part.get_face_data().count >= 0, "the result face data is unreadable")
    check(part.get_edge_data().count >= 0, "the result edge data is unreadable")
    check(part.get_vertex_data().count >= 0, "the result vertex data is unreadable")
    node_ids = list(part.node_ids)
    coordinates = part.get_node_coordinates()
    check(len(coordinates) == len(node_ids), "the result node coordinate count does not match")
    check(len(coordinates[0]) == 3, "a result node coordinate must have 3 values")
    mesh_nodes = pmx.results.current.mesh.nodes.get_data()
    lookup = dict(zip(list(mesh_nodes.ids), [list(row) for row in mesh_nodes.coordinates]))
    check(lookup[node_ids[0]] == list(coordinates[0]),
          "the part coordinates must be the undeformed result mesh coordinates")
    metadata = part.get_geometry_metadata()
    check(metadata.format_version >= 0, "the result metadata format version is invalid")
    check(len(metadata.get_entities("face")) >= 0, "the result metadata is unreadable")
    #
    parts.hide(name)
    parts.show(name)
    parts.set_color(name, [10, 20, 30])
    parts.reset_color(name)
    parts.set_transparency(name, 128)
    parts.set_representation(name, False)
    parts.set_representation(name, True)
    parts.replace_properties(name)
    check(parts.contains(name), "the part disappeared after the property replacement")
    # A name the result parts would refuse is refused before the part is renamed in place
    message = raised_message(lambda: parts.replace_properties(name, name="  "))
    check(message is not None and "must not be empty" in message, "an empty part name was accepted: " + str(message))
    message = raised_message(lambda: parts.replace_properties(name, name="two words"))
    check(message is not None and "is not valid" in message, "an invalid part name was accepted: " + str(message))
    check(list(parts.get_names().to_array()) == list(names), "a refused rename changed the part names")
    check(parts[name].name == name, "a refused rename changed the part name to " + parts[name].name)
    if len(names) > 1:
        message = raised_message(lambda: parts.replace_properties(name, name=names[1].swapcase()))
        check(message is not None and "already exists" in message,
              "a rename onto another result part in other case was accepted: " + str(message))
        check(parts[name].name == name, "a refused rename changed the part name to " + parts[name].name)
    # The cube result has a single part, so there the check against the other parts shows only through the case
    other_case = name.lower() if name != name.lower() else name.upper()
    parts.replace_properties(name, name=other_case)
    renamed_names = list(parts.get_names().to_array())
    check(other_case in renamed_names and name not in renamed_names and len(renamed_names) == len(names),
          "a rename to the same name in other case was not applied: " + str(renamed_names))
    parts.replace_properties(other_case, name=name)
    check(parts.contains(name) and parts[name].name == name, "the part was not renamed back")
    # A name that does not exist is refused before the command is recorded
    check_raises(lambda: parts.hide("__missing_part__"), "a missing part name was accepted")
    check_raises(lambda: parts.set_transparency(name, 300), "an out of range transparency was accepted")
    check_raises(lambda: parts.set_transparency(name, 12.5), "a fractional transparency was accepted")


def test_field_extremes():
    result = pmx.results.current
    step_id = list(result.steps.get_ids())[0]
    increment = result.steps[step_id].increments.last()
    field_name = list(increment.fields.get_names())[0]
    field = increment.fields[field_name]
    component_name = list(field.components.get_names())[0]
    extremes = field.get_extremes(component_name)
    check(extremes.component_name == component_name, "the extremes name the wrong component")
    check(extremes.max_value >= extremes.min_value, "the maximum must not be below the minimum")
    # The extremes must be the extremes of the values the bulk transfer reports
    data = field.components[component_name].get_data()
    values = list(data.values)
    ids = list(data.ids)
    check(extremes.max_value == max(values), "the maximum does not match the transferred values")
    check(extremes.min_value == min(values), "the minimum does not match the transferred values")
    check(extremes.max_node_id == ids[values.index(max(values))], "the maximum node ID does not match")
    check(extremes.min_node_id == ids[values.index(min(values))], "the minimum node ID does not match")
    check(extremes.unit == data.unit, "the extremes report a different unit than the data")
    # The component spelling and the field spelling answer the same thing
    component_extremes = field.components[component_name].get_extremes()
    check(component_extremes.max_value == extremes.max_value, "the two spellings disagree")
    check(component_extremes.max_node_id == extremes.max_node_id, "the two spellings name different nodes")
    check_raises(lambda: field.get_extremes("__missing_component__"),
                 "a missing component name was accepted")


def test_field_output_duplication():
    field_outputs = pmx.results.current.field_outputs
    before = list(field_outputs.get_names())
    if before:
        field_outputs.duplicate(before[0])
        after = list(field_outputs.get_names())
        check(len(after) == len(before) + 1, "the duplicated field output was not added")
        for name in after:
            if name not in before:
                field_outputs.remove(name)
    check_raises(lambda: field_outputs.duplicate("__missing_field_output__"),
                 "a missing field output name was accepted")


def test_result_lifecycle():
    names = list(pmx.results.get_names())
    check(len(names) > 0, "no result is loaded")
    pmx.results.remove_current()
    check(len(list(pmx.results.get_names())) == len(names) - 1, "the current result was not removed")
    pmx.results.clear()
    check(len(list(pmx.results.get_names())) == 0, "the results were not cleared")


def main():
    check(len(pmx.results.get_names().to_array()) > 0, "no result is loaded")
    test_part_commands()
    test_field_extremes()
    test_field_output_duplication()
    test_result_lifecycle()
    print("TEST PASSED")


main()
