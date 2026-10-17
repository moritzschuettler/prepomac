#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Solves a [0/90]s laminate on the flat plate of composite_shells.inp pulled to a strain of 0.001 along x, opens the
#result and checks the shell result outputs: the ply stresses in the material axes against Phase 0 of the composite
#plan (c13: 140.6 and 2.41 in the 0 degree plies, -2.41 and 10.15 in the 90 degree plies), the in-plane principal
#stresses, and the failure criteria against the formulas evaluated here. A CalculiX executable must be configured.
#
#  PrePoMax.com -f composite_shells.inp -u MM_TON_S_C --script Test_Result_Composite.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math

PREFIX = "PyTest_"
JOB = PREFIX + "Composite"
STRENGTH = {"xt": 1500.0, "xc": 1200.0, "yt": 50.0, "yc": 250.0, "s12": 70.0, "s23": 40.0, "f12": -0.5}


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def raised_message(function):
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def nodes_of(element_set_name):
    """The model part whose elements are the element set, and its node ids and coordinates."""
    element_ids = set(pmx.model.element_sets[element_set_name].get_element_ids())
    for part in pmx.model.parts:
        if set(part.get_element_ids()) == element_ids:
            ids = list(part.get_node_ids())
            coordinates = part.get_node_coordinates()
            return part, {ids[i]: [float(coordinates[i][j]) for j in range(3)] for i in range(len(ids))}
    raise Exception("TEST FAILED: no part holds the element set " + element_set_name)


def node_region(node_ids):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', sorted(node_ids))


def build_and_solve():
    pmx.model.materials.add(PREFIX + "Ply", [pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 3333.333333, 0]]),
                                             pmx.create.composite_strength(STRENGTH["xt"], STRENGTH["xc"], STRENGTH["yt"],
                                                                           STRENGTH["yc"], STRENGTH["s12"],
                                                                           STRENGTH["s23"])])
    pmx.model.materials.add(PREFIX + "Iso", pmx.create.elastic(200000, 0.3))
    pmx.model.sections.add_composite_shell(PREFIX + "Plate", pmx.create.region("ElementSetName", "PLATE"),
                                           [[0.5, PREFIX + "Ply", 0], [0.5, PREFIX + "Ply", 90]], symmetric=True)
    for name in ("INCLINED", "CYLINDER"):
        pmx.model.sections.add_shell(PREFIX + name, PREFIX + "Iso", pmx.create.region("ElementSetName", name),
                                     thickness=1)
    step = pmx.model.steps.add_static(PREFIX + "Step")
    plate_part, plate = nodes_of("PLATE")
    step.bcs.add_displacement_rotation(PREFIX + "X0", node_region([n for n, c in plate.items() if abs(c[0]) < 1e-6]),
                                       u1=0)
    step.bcs.add_displacement_rotation(PREFIX + "Origin",
                                       node_region([n for n, c in plate.items() if abs(c[0]) + abs(c[1]) < 1e-6]),
                                       u2=0)
    step.bcs.add_displacement_rotation(PREFIX + "Plane", node_region(plate.keys()), u3=0)
    step.bcs.add_displacement_rotation(PREFIX + "X1",
                                       node_region([n for n, c in plate.items() if abs(c[0] - 100) < 1e-6]), u1=0.1)
    for name in ("INCLINED", "CYLINDER"):
        part, nodes = nodes_of(name)
        step.bcs.add_fixed(PREFIX + "Fix_" + name, node_region(nodes.keys()))
    pmx.jobs.add(JOB)
    status = pmx.jobs[JOB].run(timeout=300)
    check(status == "OK", "the analysis did not succeed, got " + status + "\n" + pmx.jobs[JOB].get_output())
    pmx.jobs.open_results(JOB)
    return pmx.results.current


def layer_nodes(result, layer):
    elements = result.mesh.elements.get_data()
    node_ids = set()
    for element_id in result.mesh.element_sets[PREFIX + "Plate_LAYER-" + str(layer)].get_element_ids():
        node_ids.update(elements.get_node_ids(element_id))
    return sorted(node_ids)


def mean(result, step_id, increment_id, field, component, node_ids):
    values = [result.get_nodal_value(step_id, increment_id, field, component, n) for n in node_ids]
    return sum(values) / len(values)


def tsai_wu(s1, s2, t12):
    s = STRENGTH
    f1, f2 = 1 / s["xt"] - 1 / s["xc"], 1 / s["yt"] - 1 / s["yc"]
    f11, f22, f66 = 1 / (s["xt"] * s["xc"]), 1 / (s["yt"] * s["yc"]), 1 / s["s12"] ** 2
    f12 = s["f12"] * math.sqrt(f11 * f22)
    a = f11 * s1 * s1 + f22 * s2 * s2 + f66 * t12 * t12 + 2 * f12 * s1 * s2
    b = f1 * s1 + f2 * s2
    return a + b, (-b + math.sqrt(b * b + 4 * a)) / (2 * a)


def main():
    check(pmx.model.is_open(), "a model must be open")
    result = build_and_solve()
    step_id = list(result.steps.get_ids())[0]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    outputs = result.field_outputs
    outputs.add_material_axes(PREFIX + "PLY", "STRESS")
    outputs.add_shell_plane_stress(PREFIX + "PLANE", "STRESS")
    outputs.add_composite_failure(PREFIX + "MS", "STRESS", "MaxStress")
    outputs.add_composite_failure(PREFIX + "TW", "STRESS", "TsaiWu")
    outputs.add_composite_failure(PREFIX + "HA", "STRESS", "Hashin")
    check(outputs[PREFIX + "TW"].item_type == "composite_failure" and outputs[PREFIX + "TW"].criterion == "TsaiWu",
          "the composite failure output did not read back")
    for name in (PREFIX + "PLY", PREFIX + "PLANE", PREFIX + "MS"):
        check(outputs[name].valid, "the shell output " + name + " must be valid")
    # The stresses keep the unit of the source field, the angle is in degrees and the failure values have no unit
    fields = result.steps[step_id].increments[increment_id].fields
    stress_unit = fields["STRESS"].components["S11"].get_data().unit
    units = {(PREFIX + "PLY", "S11"): stress_unit, (PREFIX + "PLANE", "S11"): stress_unit,
             (PREFIX + "PLANE", "PRINCIPAL_MAX"): stress_unit, (PREFIX + "PLANE", "ANGLE"): "°",
             (PREFIX + "MS", "FAILURE_INDEX"): "/", (PREFIX + "TW", "RESERVE_FACTOR"): "/"}
    for (field, component), unit in units.items():
        read = fields[field].components[component].get_data().unit
        check(read == unit, "%s.%s must be in %s, not %s" % (field, component, unit, read))
    # Each layer is a result part named after the model part and the layer, gathered in a group named after the part
    plate_part, plate = nodes_of("PLATE")
    layer_part_names = [plate_part.name + "_Layer-" + str(k) for k in (1, 2, 3, 4)]
    for k, name in enumerate(layer_part_names):
        check(name in result.mesh.parts and set(result.mesh.parts[name].get_element_ids()) ==
              set(result.mesh.element_sets[PREFIX + "Plate_LAYER-" + str(k + 1)].get_element_ids()),
              "the layer %d must be the result part %s: %s" % (k + 1, name, list(result.mesh.parts.get_names())))
    groups = [entry for entry in pmx.model.tree.get_layout("results", "parts") if isinstance(entry, dict)]
    check([group["group_name"] for group in groups] == [plate_part.name] and list(groups[0]["items"]) ==
          layer_part_names, "the layers must be gathered in the group %s: %s" % (plate_part.name, groups))
    # The plies in the material axes, as Phase 0 found them
    layers = {k: layer_nodes(result, k) for k in (1, 2, 3, 4)}
    check(all(len(nodes) > 0 for nodes in layers.values()), "every layer must have its own result element set")
    expected = {1: (140.6, 2.41), 2: (-2.41, 10.15), 3: (-2.41, 10.15), 4: (140.6, 2.41)}
    for k, (s11, s22) in expected.items():
        m11 = mean(result, step_id, increment_id, PREFIX + "PLY", "S11", layers[k])
        m22 = mean(result, step_id, increment_id, PREFIX + "PLY", "S22", layers[k])
        check(abs(m11 - s11) <= 0.01 * abs(s11) + 0.05 and abs(m22 - s22) <= 0.03 * abs(s22) + 0.05,
              "layer %d reads S11 %.3f and S22 %.3f, not %.2f and %.2f" % (k, m11, m22, s11, s22))
    # The in-plane principal stresses of the plies: the 0 degree ply along axis 1, the 90 degree ply along axis 2
    node = layers[1][len(layers[1]) // 2]
    p_max = result.get_nodal_value(step_id, increment_id, PREFIX + "PLANE", "PRINCIPAL_MAX", node)
    s11 = result.get_nodal_value(step_id, increment_id, PREFIX + "PLY", "S11", node)
    s22 = result.get_nodal_value(step_id, increment_id, PREFIX + "PLY", "S22", node)
    s12 = result.get_nodal_value(step_id, increment_id, PREFIX + "PLY", "S12", node)
    center, radius = (s11 + s22) / 2, math.sqrt((s11 - s22) ** 2 / 4 + s12 * s12)
    check(abs(p_max - (center + radius)) < 1e-3 * abs(p_max), "the in-plane maximum principal value is wrong")
    # A ply whose shear is negligible is in its principal axes: exactly 0 or 90, never -90
    for k, angle in ((1, 0), (2, 90)):
        read = set(result.get_nodal_value(step_id, increment_id, PREFIX + "PLANE", "ANGLE", n) for n in layers[k])
        check(read == {angle}, "the principal direction of layer %d must be %d degrees, not %s" % (k, angle,
                                                                                                   sorted(read)[:5]))
    # The criteria at that node of the 0 degree ply and at a node of the 90 degree ply
    index = result.get_nodal_value(step_id, increment_id, PREFIX + "MS", "FAILURE_INDEX", node)
    mode = result.get_nodal_value(step_id, increment_id, PREFIX + "MS", "MODE", node)
    check(abs(index - s11 / STRENGTH["xt"]) < 1e-4 and mode == 1,
          "the maximum stress index of a 0 degree ply must be S11 / Xt in fiber tension: %f, %f" % (index, mode))
    index, reserve = tsai_wu(s11, s22, s12)
    check(abs(result.get_nodal_value(step_id, increment_id, PREFIX + "TW", "FAILURE_INDEX", node) - index) < 1e-4 and
          abs(result.get_nodal_value(step_id, increment_id, PREFIX + "TW", "RESERVE_FACTOR", node) - reserve) <
          1e-3 * reserve, "the Tsai-Wu values do not follow the formula")
    hashin = (s11 / STRENGTH["xt"]) ** 2 + (s12 / STRENGTH["s12"]) ** 2
    check(abs(result.get_nodal_value(step_id, increment_id, PREFIX + "HA", "FAILURE_INDEX", node) - hashin) < 1e-4,
          "the Hashin fiber tension index does not follow the formula")
    node = layers[2][len(layers[2]) // 2]
    s22 = result.get_nodal_value(step_id, increment_id, PREFIX + "PLY", "S22", node)
    check(result.get_nodal_value(step_id, increment_id, PREFIX + "MS", "MODE", node) == 3 and
          abs(result.get_nodal_value(step_id, increment_id, PREFIX + "MS", "FAILURE_INDEX", node) - s22 / STRENGTH["yt"]) <
          1e-4, "the maximum stress of a 90 degree ply must be matrix tension, S22 / Yt")
    # A ply without a composite strength reads NaN
    cylinder_part, cylinder = nodes_of("CYLINDER")
    elements = result.mesh.elements.get_data()
    cylinder_result_node = elements.get_node_ids(list(pmx.model.element_sets["CYLINDER"].get_element_ids())[0])[0]
    value = result.get_nodal_value(step_id, increment_id, PREFIX + "TW", "FAILURE_INDEX", cylinder_result_node)
    check(value != value, "a shell without a composite strength must read NaN, got " + str(value))
    # The projected x axis turns over between neighbouring elements of the cylinder, which still have their stresses
    nodes = set()
    for element_id in pmx.model.element_sets["CYLINDER"].get_element_ids():
        nodes.update(elements.get_node_ids(element_id))
    missing = [n for n in nodes if result.get_nodal_value(step_id, increment_id, PREFIX + "PLY", "S11", n) !=
               result.get_nodal_value(step_id, increment_id, PREFIX + "PLY", "S11", n)]
    check(not missing, "the cylinder nodes %s have no stress in the material axes" % missing[:10])
    # The fixed cylinder has no in-plane stress and so no principal direction
    angle = result.get_nodal_value(step_id, increment_id, PREFIX + "PLANE", "ANGLE", sorted(nodes)[0])
    check(angle != angle, "a node without in-plane stress must have no principal angle, got " + str(angle))
    # Refusals name the argument
    message = raised_message(lambda: outputs.add_composite_failure(PREFIX + "Bad", "STRESS", "Puck"))
    check(message is not None and "'criterion'" in message, "an unknown criterion must be refused: " + str(message))
    message = raised_message(lambda: outputs.add_material_axes(PREFIX + "Bad", "DISP"))
    check(message is not None and "'field_name'" in message, "a vector field must be refused: " + str(message))
    message = raised_message(lambda: outputs.add_shell_plane_stress(PREFIX + "Bad", PREFIX + "PLY"))
    check(message is not None and "'field_name'" in message,
          "a field already in material axes must be refused: " + str(message))
    print("TEST PASSED")


main()
