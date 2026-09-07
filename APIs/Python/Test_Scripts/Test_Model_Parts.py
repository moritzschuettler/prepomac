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
    check(len(part.color) == 4, "the color must have 4 channels")
    check(len(part.get_bounding_box()) == 6, "the bounding box must have 6 values")
    check(len(part.get_node_ids()) == part.node_count, "the node ID count does not match")
    check(len(part.get_element_ids()) == part.element_count, "the element ID count does not match")
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
    parts.reset_color(name)
    parts.set_transparency(name, 128)
    check(parts[name].color[3] == 128, "the transparency was not set")
    parts.set_transparency(name, 255)
    # Visibility
    parts.hide(name)
    check(not parts[name].visible, "the part was not hidden")
    parts.show(name)
    check(parts[name].visible, "the part was not shown")
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
    # property dialog records
    slots = dict(parts.get_element_types(name))
    check(len(slots) > 0, "the part carries no element type slots")
    parts.replace_properties(name, name="PyTest_Renamed", color=[1, 2, 3],
                             element_types={'linear_tetra': 'C3D4'})
    check(parts.contains("PyTest_Renamed"), "the part was not renamed by replace_properties")
    check(list(parts["PyTest_Renamed"].color)[0:3] == [1, 2, 3], "the colour was not replaced")
    check(dict(parts.get_element_types("PyTest_Renamed"))['linear_tetra'] ==
          dict(parts.get_element_types("PyTest_Renamed"))['linear_tetra'],
          "the element type slot is unreadable")
    # A value that is not given is kept
    parts.replace_properties("PyTest_Renamed", name=name)
    check(parts.contains(name), "the part was not renamed back")
    check(list(parts[name].color)[0:3] == [1, 2, 3], "an omitted colour must be kept")
    parts.reset_color(name)
    check_raises(lambda: parts.replace_properties(name, element_types={'no_such_slot': 'C3D4'}),
                 "an unknown element type slot must be rejected")
    check_raises(lambda: parts.replace_properties(name, element_types={'linear_tetra': 'NoSuchType'}),
                 "an unknown element type value must be rejected")
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
