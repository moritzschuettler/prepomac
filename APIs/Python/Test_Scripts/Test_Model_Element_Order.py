#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks mesh.change_element_order: every model part is converted to the other element order and back. The cube is a
#linear hexahedron, the composite shells are parabolic quadrilaterals in three parts that share nodes, and the box and
#cylinder geometry is meshed with parabolic tetrahedra first. Element ids, element sets and surfaces are kept, a node
#set picked on a face gains and loses the midside nodes of that face, the CalculiX element type follows the order, and
#a linear mesh comes back node for node after the round trip.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Element_Order.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
#
#  PrePoMax.com -f composite_shells.inp -u MM_TON_S_C --script Test_Model_Element_Order.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
#
#  PrePoMax.com -f box_and_cylinder.brep -u MM_TON_S_C --script Test_Model_Element_Order.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"
OTHER_ORDER_TYPE = {"C3D8": "C3D20", "C3D8R": "C3D20R", "C3D20": "C3D8", "C3D20R": "C3D8R",
                    "C3D4": "C3D10", "C3D10": "C3D4", "C3D10T": "C3D4", "S4": "S8", "S4R": "S8R",
                    "S8": "S4", "S8R": "S4R", "S3": "S6", "S6": "S3"}


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        if type(ex).__name__ != "PythonAPIException" or "   at " in str(ex):
            raise Exception("TEST FAILED: the refusal must be a PythonAPIException without a .NET stack trace, not " +
                            type(ex).__name__ + ": " + str(ex)[0:500])
        return str(ex)
    return None


def ensure_model_mesh():
    if pmx.model.nodes.count > 0:
        return
    geometry_part_names = list(pmx.model.geometry.parts.get_names())
    check(len(geometry_part_names) > 0, "the model has neither a mesh nor a geometry part")
    report = pmx.model.mesh.generate(geometry_part_names)
    for part_name in geometry_part_names:
        check(report[part_name] == "", "the part " + part_name + " was not meshed: " + report[part_name])


def get_state(part_names):
    parts = pmx.model.parts
    state = {}
    for part_name in part_names:
        part = parts[part_name]
        node_ids = list(part.get_node_ids())
        coordinates = part.get_node_coordinates()
        state[part_name] = {"node_ids": set(node_ids),
                            "coordinates": {node_ids[i]: tuple(float(value) for value in coordinates[i])
                                            for i in range(len(node_ids))},
                            "element_ids": sorted(part.get_element_ids()),
                            "element_types": dict(parts.get_element_types(part_name))}
    state["node_count"] = pmx.model.nodes.count
    state["element_count"] = pmx.model.elements.count
    state["surface_node_count"] = pmx.model.node_sets[PREFIX + "SurfaceNodes"].count
    state["surface_face_ids"] = sorted(pmx.model.surfaces[PREFIX + "Surface"].get_face_ids())
    state["element_set_count"] = pmx.model.element_sets[PREFIX + "Elements"].count
    return state


def is_parabolic(state, part_names):
    slots = [slot for part_name in part_names for slot in state[part_name]["element_types"]]
    check(all(slot.startswith("linear_") for slot in slots) or all(slot.startswith("parabolic_") for slot in slots),
          "every part must hold elements of one order: " + str(slots))
    return slots[0].startswith("parabolic_")


def check_converted(before, after, part_names, second_order):
    check(after["element_count"] == before["element_count"], "the element count changed")
    check(after["element_set_count"] == before["element_set_count"], "the element set lost elements")
    check(after["surface_face_ids"] == before["surface_face_ids"], "the surface faces changed")
    if second_order:
        check(after["node_count"] > before["node_count"], "no midside node was added")
        check(after["surface_node_count"] > before["surface_node_count"], "the surface did not gain its midside nodes")
    else:
        check(after["node_count"] < before["node_count"], "no midside node was removed")
        check(after["surface_node_count"] < before["surface_node_count"], "the surface kept its midside nodes")
    for part_name in part_names:
        check(after[part_name]["element_ids"] == before[part_name]["element_ids"],
              "the element ids of the part " + part_name + " changed")
        if second_order:
            check(before[part_name]["node_ids"] < after[part_name]["node_ids"],
                  "the part " + part_name + " lost a corner node or gained no midside node")
        else:
            check(after[part_name]["node_ids"] < before[part_name]["node_ids"],
                  "the part " + part_name + " gained a node or lost no midside node")
        for node_id in after[part_name]["node_ids"] & before[part_name]["node_ids"]:
            check(after[part_name]["coordinates"][node_id] == before[part_name]["coordinates"][node_id],
                  "the node " + str(node_id) + " moved")
        expected_types = {}
        for slot, value in before[part_name]["element_types"].items():
            other_slot = slot.replace("linear_", "parabolic_") if second_order else slot.replace("parabolic_", "linear_")
            expected_types[other_slot] = OTHER_ORDER_TYPE[value]
        check(after[part_name]["element_types"] == expected_types,
              "the element types of the part " + part_name + " are " + str(after[part_name]["element_types"]) +
              ", not " + str(expected_types))


def check_refusals(part_names):
    mesh = pmx.model.mesh
    node_count = pmx.model.nodes.count
    for call, words in [(lambda: mesh.change_element_order("NoSuchPart"), ["NoSuchPart"]),
                        (lambda: mesh.change_element_order([]), ["part_names"]),
                        (lambda: mesh.change_element_order([part_names[0], part_names[0]]), ["part_names"])]:
        message = raised_message(call)
        check(message is not None and all(word in message for word in words),
              "the refusal must name " + str(words) + ": " + str(message))
    check(pmx.model.nodes.count == node_count, "a refused call changed the mesh")


def main():
    check(pmx.model.is_open(), "a model must be open")
    ensure_model_mesh()
    part_names = list(pmx.model.parts.get_names())
    first_part = pmx.model.parts[part_names[0]]
    surface_selection = pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {first_part.id: [1]})
    pmx.model.surfaces.add(PREFIX + "Surface", surface_selection)
    pmx.model.node_sets.add(PREFIX + "SurfaceNodes",
                            pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {first_part.id: [1]}))
    pmx.model.element_sets.add(PREFIX + "Elements",
                               pmx.create.selection_by_ids('femodel', 'elements', 'elements',
                                                           list(first_part.get_element_ids())))
    # A reference point takes the node ids after the last mesh node, which the new midside nodes must not reuse
    reference_point = pmx.model.reference_points.add(PREFIX + "RP", [0, 0, 100])
    reference_node_ids = {reference_point.node_id, reference_point.rotation_node_id}
    if "linear_hexa" in pmx.model.parts.get_element_types(part_names[0]):
        pmx.model.parts.set_element_type(part_names[0], "linear_hexa", "C3D8R")
    check_refusals(part_names)
    #
    original = get_state(part_names)
    second_order = not is_parabolic(original, part_names)
    print("parts:", part_names, "second order:", second_order, "nodes:", original["node_count"])
    pmx.model.mesh.change_element_order(part_names, second_order=second_order)
    converted = get_state(part_names)
    check_converted(original, converted, part_names, second_order)
    for part_name in part_names:
        check(not converted[part_name]["node_ids"] & reference_node_ids,
              "a midside node of the part " + part_name + " took a reference point node id")
    print("converted nodes:", converted["node_count"])
    #
    pmx.model.mesh.change_element_order(part_names, second_order=not second_order)
    restored = get_state(part_names)
    check_converted(converted, restored, part_names, not second_order)
    check(restored["node_count"] == original["node_count"], "the round trip changed the node count from " +
          str(original["node_count"]) + " to " + str(restored["node_count"]))
    check(restored["surface_node_count"] == original["surface_node_count"], "the round trip changed the surface")
    for part_name in part_names:
        check(restored[part_name]["element_types"] == original[part_name]["element_types"],
              "the round trip changed the element types of the part " + part_name)
        if second_order:
            check(restored[part_name]["coordinates"] == original[part_name]["coordinates"],
                  "the round trip did not bring the linear part " + part_name + " back node for node")
    print("TEST PASSED")


main()
