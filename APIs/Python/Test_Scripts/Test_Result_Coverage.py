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
    node_ids = list(part.get_node_ids())
    coordinates = part.get_node_coordinates()
    check(len(coordinates) == len(node_ids), "the result node coordinate count does not match")
    check(len(coordinates[0]) == 3, "a result node coordinate must have 3 values")
    mesh_nodes = pmx.results.current.mesh.nodes.get_data()
    lookup = dict(zip(list(mesh_nodes.ids), [list(row) for row in mesh_nodes.coordinates]))
    check(lookup[node_ids[0]] == list(coordinates[0]),
          "the part coordinates must be the undeformed result mesh coordinates")
    # A fixed size value is a plain list, on the results side as on the model side
    point = pmx.results.current.get_nodal_coordinates(node_ids[0])
    check(type(point) is list, "the nodal coordinates must be a list: " + type(point).__name__)
    check(point == lookup[node_ids[0]], "the nodal coordinates " + str(point) + " do not match the mesh")
    box = part.get_bounding_box()
    check(type(box) is list and all(type(value) is float for value in box),
          "the result bounding box must be a list[float]: " + type(box).__name__)
    for axis in range(3):
        values = [float(row[axis]) for row in coordinates]
        check(abs(box[2 * axis] - min(values)) < 1e-6 and abs(box[2 * axis + 1] - max(values)) < 1e-6,
              "the result bounding box " + str(box) + " does not span the part nodes")
    center = part.center_of_mass
    check(type(center) is list, "the result center of mass must be a list: " + type(center).__name__)
    for axis in range(len(center)):
        check(box[2 * axis] - 1e-6 <= center[axis] <= box[2 * axis + 1] + 1e-6,
              "the result center of mass " + str(center) + " lies outside the bounding box")
    for inertia in (part.get_inertia_at_center_of_mass(), part.get_inertia_at_origin()):
        check(type(inertia) is list and all(type(row) is list and len(row) == 3 for row in inertia),
              "a result inertia matrix must be a list of three-value lists: " + str(inertia))
    # A list of IDs is an IntList on the results side as on the model side
    mesh = pmx.results.current.mesh
    id_lists = [part.get_node_ids(), part.get_element_ids(), mesh.nodes.get_ids(), mesh.elements.get_ids(),
                mesh_nodes.ids, mesh.elements.get_data().ids]
    for region in list(mesh.node_sets)[:1]:
        id_lists.append(region.get_node_ids())
    for region in list(mesh.element_sets)[:1]:
        id_lists.append(region.get_element_ids())
    for region in list(mesh.surfaces)[:1]:
        id_lists.append(region.get_face_ids())
        id_lists.append(region.get_node_ids())
    for ids in id_lists:
        check(type(ids).__name__ == "IntListAPI", "a result ID list must be an IntList: " + type(ids).__name__)
    metadata = part.get_geometry_metadata()
    check(metadata.format_version >= 0, "the result metadata format version is invalid")
    check(len(metadata.get_entities("face")) >= 0, "the result metadata is unreadable")
    # to_array hands over a plain Python list, of names and of IDs alike
    check(type(names) is list and all(type(item) is str for item in names),
          "StringList.to_array must return a list[str]: " + type(names).__name__)
    check(type(part.get_node_ids().to_array()) is list and part.get_node_ids().to_array() == node_ids,
          "IntList.to_array must return a list[int]")
    # A result part is found in any case and reports the spelling it is stored under
    if name.swapcase() != name:
        check(parts.contains(name.swapcase()), "contains() compared the part names with their case")
        check(parts[name.swapcase()].name == name, "the part name read back in the lookup spelling: " +
              parts[name.swapcase()].name)
        parts.hide(name.swapcase())
        parts.show([name.swapcase()])
        check(list(parts.get_names().to_array()) == list(names), "hiding in another case changed the part names")
    for region_names, regions in ((mesh.node_sets.get_names(), mesh.node_sets),
                                  (mesh.element_sets.get_names(), mesh.element_sets),
                                  (mesh.surfaces.get_names(), mesh.surfaces)):
        for region_name in list(region_names)[:1]:
            if region_name.swapcase() != region_name:
                check(regions.contains(region_name.swapcase()),
                      "contains() compared the region names with their case: " + region_name)
                check(regions[region_name.swapcase()].name == region_name,
                      "the region name read back in the lookup spelling: " + region_name)
    #
    parts.hide(name)
    parts.show(name)
    parts.set_color(name, [10, 20, 30])
    parts.reset_color(name)
    parts.set_opacity(name, 128)
    parts.set_representation(name, False)
    parts.set_representation(name, True)
    parts.replace_properties(name)
    check(parts.contains(name), "the part disappeared after the property replacement")
    # A name the result parts would refuse is refused before the part is renamed in place
    message = raised_message(lambda: parts.replace_properties(name, name="  "))
    check(message is not None and "must not be empty" in message, "an empty part name was accepted: " + str(message))
    message = raised_message(lambda: parts.replace_properties(name, name="two words"))
    check(message is not None and "is not valid" in message, "an invalid part name was accepted: " + str(message))
    message = raised_message(lambda: parts.replace_properties(name, name=" " + name))
    check(message is not None and "space" in message, "a padded part name was accepted: " + str(message))
    check(list(parts.get_names().to_array()) == list(names), "a refused rename changed the part names")
    check(parts[name].name == name, "a refused rename changed the part name to " + parts[name].name)
    # The part to replace is named in any case, and the command is given the name the result stores
    if name.swapcase() != name:
        parts.replace_properties(name.swapcase(), color=[40, 50, 60])
        check(list(parts.get_names().to_array()) == list(names),
              "a replace naming the part in another case changed the part names")
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
    # The rename the results view records refuses the same names before anything is recorded
    parts.rename(name, other_case)
    renamed_names = list(parts.get_names().to_array())
    check(other_case in renamed_names and name not in renamed_names, "rename() was not applied: " + str(renamed_names))
    parts.rename(other_case, name)
    check(parts.contains(name) and parts[name].name == name, "rename() did not rename the part back")
    message = raised_message(lambda: parts.rename(name, "two words"))
    check(message is not None and "is not valid" in message, "an invalid new name was accepted: " + str(message))
    check_raises(lambda: parts.rename("__missing_part__", "PyTest_Part"), "renaming a missing part was accepted")
    if len(names) > 1:
        message = raised_message(lambda: parts.rename(name, names[1].swapcase()))
        check(message is not None and "already exists" in message,
              "a rename onto another result part in other case was accepted: " + str(message))
    check(list(parts.get_names().to_array()) == list(names), "a refused rename changed the part names")
    # A name that does not exist is refused before the command is recorded
    check_raises(lambda: parts.hide("__missing_part__"), "a missing part name was accepted")
    check_raises(lambda: parts.set_opacity(name, 300), "an out of range opacity was accepted")
    # The result part dialogs raise an alpha below 25 to 25, so the API refuses it naming the argument
    for call, argument in [(lambda: parts.set_opacity(name, 10), "'opacity'"),
                           (lambda: parts.set_color(name, [10, 20, 30, 0]), "'color'"),
                           (lambda: parts.replace_properties(name, color=[10, 20, 30, 24]), "'color'")]:
        message = raised_message(call)
        check(message is not None and argument in message and "25" in message,
              "an alpha below 25 must be refused naming " + argument + ": " + str(message))
    check_raises(lambda: parts.set_opacity(name, 12.5), "a fractional opacity was accepted")
    check(not hasattr(parts, "set_transparency"), "set_transparency was renamed to set_opacity")


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
    check(type(data.ids).__name__ == "IntListAPI", "the field data IDs must be an IntList: " +
          type(data.ids).__name__)
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
    # A result is made current by its name in any case, and is then named as the collection stores it
    if names[0].swapcase() != names[0]:
        pmx.results.current = names[0].swapcase()
        check(pmx.results.current.name == names[0],
              "the current result is named in the lookup spelling: " + pmx.results.current.name)
    pmx.results.remove_current()
    remaining = list(pmx.results.get_names())
    check(len(remaining) == len(names) - 1, "the current result was not removed")
    # As Close Current Result does, the first remaining result becomes the current one
    if remaining:
        check(pmx.results.current.name == remaining[0], "the first remaining result was not made current")
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
