#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the geometry and model part collections. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Parts.py -g No -x Yes -w <dir>
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


def test_part_reading(parts, label):
    check(len(parts) > 0, "no " + label + " found")
    name = parts.get_names()[0]
    part = parts[name]
    check(part.name == name, "the part name does not match")
    check(part.node_count > 0, "the part has no nodes")
    check(part.element_count > 0, "the part has no elements")
    check(part.part_type != "", "the part type is empty")
    check(type(part.color) is list and len(part.color) == 4,
          "the color must be a plain list of 4 channels: " + repr(part.color))
    check(len(part.get_bounding_box()) == 6, "the bounding box must have 6 values")
    check(len(part.get_node_ids()) == part.node_count, "the node ID count does not match")
    coordinates = part.get_node_coordinates()
    check(len(coordinates) == part.node_count, "the node coordinate count does not match")
    check(len(coordinates[0]) == 3, "a node coordinate must have 3 values")
    check(len(part.get_element_ids()) == part.element_count, "the element ID count does not match")
    # A fixed size value is a plain list; the bounding box of a mesh part spans its node coordinates
    box = part.get_bounding_box()
    check(type(box) is list and all(type(value) is float for value in box),
          "the bounding box must be a list[float]: " + type(box).__name__)
    for axis in range(3):
        values = [float(row[axis]) for row in coordinates]
        check(abs(box[2 * axis] - min(values)) < 1e-6 and abs(box[2 * axis + 1] - max(values)) < 1e-6,
              "the bounding box " + str(box) + " does not span the node coordinates")
    # A list of IDs is an IntList, from a part, from the mesh and from its bulk data alike
    for ids in (part.get_node_ids(), part.get_element_ids(), pmx.model.nodes.get_ids(), pmx.model.elements.get_ids(),
                pmx.model.nodes.get_data().ids, pmx.model.elements.get_data().ids):
        check(type(ids).__name__ == "IntListAPI", "a model ID list must be an IntList: " + type(ids).__name__)
    check(part.volume >= 0, "the part volume is invalid")
    check(part.surface_area >= 0, "the part surface area is invalid")
    check(len(part.center_of_mass) in (0, 3), "the center of mass must be empty or have 3 values")
    check(len(part.get_inertia_at_center_of_mass()) in (0, 3), "the center inertia matrix is invalid")
    check(len(part.get_inertia_at_origin()) in (0, 3), "the origin inertia matrix is invalid")
    center = part.center_of_mass
    check(type(center) is list, "the center of mass must be a list: " + type(center).__name__)
    for axis in range(len(center)):
        check(box[2 * axis] - 1e-6 <= center[axis] <= box[2 * axis + 1] + 1e-6,
              "the center of mass " + str(center) + " lies outside the bounding box " + str(box))
    for inertia in (part.get_inertia_at_center_of_mass(), part.get_inertia_at_origin()):
        check(type(inertia) is list and all(type(row) is list and len(row) == 3 for row in inertia),
              "an inertia matrix must be a list of three-value lists: " + str(inertia))
    metadata = part.get_geometry_metadata()
    check(metadata.format_version >= 0, "the metadata format version is invalid")
    check(len(metadata.entity_kinds) >= 0, "the metadata entity kinds are unreadable")
    faces = metadata.get_entities("face")
    check(len(faces) == faces.count, "the metadata face count does not match")
    if faces.count > 0:
        face = faces[0]
        check(face.id > 0, "a metadata face ID is invalid")
        for property_name in face.property_names:
            check(face.get_property(property_name) is not None,
                  "a metadata property could not be exported")
        # A name this entity does not carry is answered, not refused, because the property set
        # varies by surface type - see Test_Model_Geometry_Metadata.py, which runs against CAD
        check(face.get_property("PyTest_no_such_property") is None,
              "a missing metadata property must return None")
        check(face.get_property("PyTest_no_such_property", -1.0) == -1.0,
              "a missing metadata property must return the default")
    # Iteration and len come from the collection base
    check(len([p for p in parts]) == len(parts), "iteration failed")
    check([p.name for p in parts][0] == name, "iteration order does not match get_names")
    return name


def test_model_parts():
    parts = pmx.model.parts
    name = test_part_reading(parts, "model parts")
    # Appearance
    parts.set_color(name, [255, 0, 0])
    check(list(parts[name].color)[0:3] == [255, 0, 0], "the color was not set")
    parts.set_color(name, "Green")
    check(list(parts[name].color)[0:3] == [0, 128, 0], "the named color was not set")
    # The color read back goes back into the argument that sets it
    color = parts[name].color
    parts.set_color(name, [255, 0, 0])
    parts.set_color(name, color)
    check(parts[name].color == color, "the color did not survive a round trip: " + repr(parts[name].color))
    parts.reset_color(name)
    # The opacity is the alpha channel of the color: 0 is fully transparent, 255 opaque
    parts.set_opacity(name, 128)
    check(parts[name].color[3] == 128, "the opacity was not set")
    parts.set_opacity(name, 0)
    check(parts[name].color[3] == 0, "an opacity of 0 must be fully transparent")
    parts.set_opacity(name, 255)
    check(parts[name].color[3] == 255, "an opacity of 255 must be opaque")
    check(not hasattr(parts, "set_transparency"), "set_transparency was renamed to set_opacity")
    check_raises(lambda: parts.set_opacity(name, 256), "an opacity above 255 was accepted")
    # Visibility
    parts.hide(part_names=name)
    check(not parts[name].visible, "the part was not hidden")
    parts.show(part_names=[name])
    check(parts[name].visible, "the part was not shown")
    # A part named in another case is passed on in the model's spelling: the view hides a part by the name of
    # its actor, compared exactly, so a part hidden as 'part-1' stayed drawn
    check(parts[name.upper()].name == name, "an item must carry the stored name: " + parts[name.upper()].name)
    parts.hide(name.lower())
    check(not parts[name].visible, "the part named in another case was not hidden")
    parts.show([name.upper()])
    check(parts[name].visible, "the part named in another case was not shown")
    parts.set_color(name.upper(), [0, 0, 255])
    check(list(parts[name].color)[0:3] == [0, 0, 255], "the part named in another case was not coloured")
    parts.reset_color(name.lower())
    parts.replace_properties(name.upper(), color=[4, 5, 6])
    check(list(parts[name].color)[0:3] == [4, 5, 6], "replace_properties did not find the part in another case")
    parts.reset_color(name)
    parts.rename(name.upper(), "PyTest_Part")
    check(parts.contains("PyTest_Part") and not parts.contains(name), "the part named in another case was not renamed")
    parts.rename("PyTest_Part", name)
    # Rename
    parts.rename(name, "PyTest_Part")
    check(parts.contains("PyTest_Part"), "the part was not renamed")
    parts.rename("PyTest_Part", name)
    # Transform - a copy keeps the original and adds a new part
    count_before = len(parts)
    parts.translate(name, [10, 0, 0], 1)
    check(len(parts) == count_before + 1, "the translated copy was not created")
    created = [n for n in parts.get_names() if n != name]
    parts.remove(created)
    check(len(parts) == count_before, "the copy was not removed")
    # The name, the colour and the element types are replaced in one call, which is what one part
    # property dialog records. The cube holds hexahedra only, and a part keeps the element type of
    # the element kinds it has, so the hexahedron slot is the one that is set
    # A map the API returns is a Python dict, which prints its content and needs no conversion
    slots = parts.get_element_types(name)
    check(type(slots) is dict, "a returned map must be a Python dict, not " + type(slots).__name__)
    check(str(slots).startswith("{"), "a returned map must print its content: " + str(slots))
    check(slots == {'linear_hexa': slots.get('linear_hexa')} and slots['linear_hexa'] != 'None',
          "only the element kinds the part contains must be listed: " + str(slots))
    parts.replace_properties(name, name="PyTest_Renamed", color=[1, 2, 3],
                             element_types={'linear_hexa': 'C3D8R'})
    check(parts.contains("PyTest_Renamed"), "the part was not renamed by replace_properties")
    check(list(parts["PyTest_Renamed"].color)[0:3] == [1, 2, 3], "the colour was not replaced")
    check(parts.get_element_types("PyTest_Renamed")['linear_hexa'] == 'C3D8R',
          "the element type slot does not read back the name it was given")
    # A value that is not given is kept
    parts.replace_properties("PyTest_Renamed", name=name)
    check(parts.contains(name), "the part was not renamed back")
    check(list(parts[name].color)[0:3] == [1, 2, 3], "an omitted colour must be kept")
    parts.reset_color(name)
    check_raises(lambda: parts.replace_properties(name, element_types={'no_such_slot': 'C3D4'}),
                 "an unknown element type slot must be rejected")
    check_raises(lambda: parts.replace_properties(name, element_types={'linear_hexa': 'NoSuchType'}),
                 "an unknown element type value must be rejected")
    # The cube holds no tetrahedra, so the part would drop the tetrahedron slot without a word
    check_raises(lambda: parts.replace_properties(name, name="PyTest_Refused",
                                                  element_types={'linear_tetra': 'C3D4'}),
                 "an element kind the part does not contain must be rejected")
    check(parts.contains(name) and not parts.contains("PyTest_Refused"),
          "a refused replace_properties must change nothing")
    # Argument validation
    check_raises(lambda: parts.translate(name, [1, 2]), "a 2 value vector must be rejected")
    check_raises(lambda: parts.set_color(name, [300, 0, 0]), "an out of range channel must be rejected")
    check_raises(lambda: parts.set_color(name, "NotAColour"), "an unknown color name must be rejected")
    check_raises(lambda: parts.translate("NoSuchPart", [1, 2, 3]), "an unknown part must be rejected")


def test_geometry_parts():
    parts = pmx.model.geometry.parts
    # An imported mesh has no geometry parts, so only the empty collection contract is checked here
    check(len(parts) == len(list(parts.get_names())), "the geometry part count does not match")
    check_raises(lambda: parts["NoSuchPart"], "an unknown geometry part must be rejected")


def main():
    check(pmx.model.is_open(), "a model must be open")
    test_model_parts()
    test_geometry_parts()
    print("TEST PASSED")


main()
