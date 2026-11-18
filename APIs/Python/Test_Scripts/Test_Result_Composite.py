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
INCLINED_ORIGIN = [0, 200, 0]
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


def rotation():
    """The axes of the inclined plate as columns: its plate is the flat one turned by them about INCLINED_ORIGIN."""
    def rx(a):
        c, s = math.cos(a), math.sin(a)
        return [[1, 0, 0], [0, c, -s], [0, s, c]]

    def rz(a):
        c, s = math.cos(a), math.sin(a)
        return [[c, -s, 0], [s, c, 0], [0, 0, 1]]
    a, b = rz(math.radians(20)), rx(math.radians(35))
    return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


def add_plate_supports(step, name, nodes, coordinate_system_name=None):
    """Pulls a plate to a strain of 0.001 along its x axis with the lateral contraction left free."""
    def bc(suffix, node_ids, **values):
        step.bcs.add_displacement_rotation(PREFIX + name + suffix, node_region(node_ids),
                                           coordinate_system_name=coordinate_system_name, **values)
    bc("X0", [n for n, c in nodes.items() if abs(c[0]) < 1e-6], u1=0)
    bc("Origin", [n for n, c in nodes.items() if abs(c[0]) + abs(c[1]) < 1e-6], u2=0)
    bc("Plane", nodes.keys(), u3=0)
    bc("X1", [n for n, c in nodes.items() if abs(c[0] - 100) < 1e-6], u1=0.1)


def build_and_solve():
    pmx.model.materials.add(PREFIX + "Ply", [pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 3333.333333, 0]]),
                                             pmx.create.composite_strength(STRENGTH["xt"], STRENGTH["xc"], STRENGTH["yt"],
                                                                           STRENGTH["yc"], STRENGTH["s12"],
                                                                           STRENGTH["s23"])])
    pmx.model.materials.add(PREFIX + "Iso", pmx.create.elastic(200000, 0.3))
    layup = [[0.5, PREFIX + "Ply", 0], [0.5, PREFIX + "Ply", 90]]
    pmx.model.sections.add_composite_shell(PREFIX + "Plate", pmx.create.region("ElementSetName", "PLATE"), layup,
                                           symmetric=True)
    # The inclined plate is the flat one turned in space, with its layers turned from its own axes
    r = rotation()
    pmx.model.coordinate_systems.add(PREFIX + "Inclined", INCLINED_ORIGIN,
                                     [INCLINED_ORIGIN[i] + r[i][0] for i in range(3)],
                                     [INCLINED_ORIGIN[i] + r[i][1] for i in range(3)])
    pmx.model.material_orientations.add(PREFIX + "Inclined", pmx.create.region("ElementSetName", "INCLINED"),
                                        coordinate_system_name=PREFIX + "Inclined")
    pmx.model.sections.add_composite_shell(PREFIX + "Inclined", pmx.create.region("ElementSetName", "INCLINED"), layup,
                                           symmetric=True)
    pmx.model.sections.add_shell(PREFIX + "CYLINDER", PREFIX + "Iso", pmx.create.region("ElementSetName", "CYLINDER"),
                                 thickness=1)
    step = pmx.model.steps.add_static(PREFIX + "Step")
    plate_part, plate = nodes_of("PLATE")
    add_plate_supports(step, "", plate)
    inclined_part, inclined = nodes_of("INCLINED")
    local = {n: [sum(r[k][j] * (c[k] - INCLINED_ORIGIN[k]) for k in range(3)) for j in range(3)]
             for n, c in inclined.items()}
    add_plate_supports(step, "Inclined_", local, PREFIX + "Inclined")
    part, nodes = nodes_of("CYLINDER")
    step.bcs.add_fixed(PREFIX + "Fix_CYLINDER", node_region(nodes.keys()))
    pmx.jobs.add(JOB)
    status = pmx.jobs[JOB].run(timeout=300)
    check(status == "OK", "the analysis did not succeed, got " + status + "\n" + pmx.jobs[JOB].get_output())
    pmx.jobs.open_results(JOB)
    return pmx.results.current


def layer_nodes(result, layer, section="Plate"):
    elements = result.mesh.elements.get_data()
    node_ids = set()
    for element_id in result.mesh.element_sets[PREFIX + section + "_LAYER-" + str(layer)].get_element_ids():
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


def lamination_theory_plies():
    """The ply stresses of the [0/90]s plate at a strain of 0.001 along x and no force along y, in the ply axes."""
    e1, e2, v12 = 140000.0, 10000.0, 0.3
    denominator = 1 - v12 * v12 * e2 / e1
    q11, q22, q12 = e1 / denominator, e2 / denominator, v12 * e2 / denominator
    ex = 0.001
    ey = -2 * q12 / (q11 + q22) * ex
    return {0: (q11 * ex + q12 * ey, q12 * ex + q22 * ey), 90: (q11 * ey + q12 * ex, q12 * ey + q22 * ex)}


def check_plane_stress_from_strains(result, step_id, increment_id, layers):
    outputs = result.field_outputs
    fields = result.steps[step_id].increments[increment_id].fields
    check("TOSTRAIN" in list(fields.get_names()), "the result must hold the strains: " + str(list(fields.get_names())))
    plane = outputs.add_shell_in_plane_state(PREFIX + "PLANE_E", "TOSTRAIN", quantity="PlaneStressFromStrain")
    check(plane.quantity == "PlaneStressFromStrain" and outputs[PREFIX + "PLANE"].quantity == "Stress" and
          plane.item_type == "shell_in_plane_state", "the quantity did not read back")
    outputs.add_composite_failure(PREFIX + "TW_E", "TOSTRAIN", "TsaiWu", quantity="planestressfromstrain")
    check(fields[PREFIX + "PLANE_E"].components["S11"].get_data().unit ==
          fields["STRESS"].components["S11"].get_data().unit, "plane stresses from strains must be stresses")
    # The plane stresses from the strains are classical lamination theory, within 0.5 % of the largest stress of a ply:
    # the small stress the lateral contraction alone gives follows the edges of the plate, not the ideal laminate
    expected = lamination_theory_plies()
    for k, angle in ((1, 0), (2, 90), (3, 90), (4, 0)):
        s11, s22 = expected[angle]
        tolerance = 0.005 * max(abs(s11), abs(s22))
        m11 = mean(result, step_id, increment_id, PREFIX + "PLANE_E", "S11", layers[k])
        m22 = mean(result, step_id, increment_id, PREFIX + "PLANE_E", "S22", layers[k])
        check(abs(m11 - s11) <= tolerance and abs(m22 - s22) <= tolerance,
              "layer %d reads %.3f and %.3f from the strains, not %.3f and %.3f of lamination theory" %
              (k, m11, m22, s11, s22))
    node = layers[2][0]
    s11, s22, s12 = [result.get_nodal_value(step_id, increment_id, PREFIX + "PLANE_E", c, node)
                     for c in ("S11", "S22", "S12")]
    index = tsai_wu(s11, s22, s12)[0]
    check(abs(result.get_nodal_value(step_id, increment_id, PREFIX + "TW_E", "FAILURE_INDEX", node) - index) < 1e-4,
          "the Tsai-Wu index from the strains does not follow the formula")
    # The in-plane strains themselves: the 0 degree ply is stretched by the imposed strain and contracts laterally
    strains = outputs.add_shell_in_plane_state(PREFIX + "PLANE_EPS", "TOSTRAIN", quantity="Strain")
    check(fields[PREFIX + "PLANE_EPS"].components["E11"].get_data().unit == "/" and
          "S11" not in list(fields[PREFIX + "PLANE_EPS"].components.get_names()),
          "the in-plane strains must be the unitless components E11, E22 and E12")
    e11 = mean(result, step_id, increment_id, PREFIX + "PLANE_EPS", "E11", layers[1])
    e22 = mean(result, step_id, increment_id, PREFIX + "PLANE_EPS", "E22", layers[1])
    e1, e2, v12 = 140000.0, 10000.0, 0.3
    denominator = 1 - v12 * v12 * e2 / e1
    q11, q22, q12 = e1 / denominator, e2 / denominator, v12 * e2 / denominator
    ey = -2 * q12 / (q11 + q22) * 0.001
    check(abs(e11 - 0.001) <= 0.005 * 0.001 and abs(e22 - ey) <= 0.05 * abs(ey),
          "the 0 degree ply strains are %g and %g, not 0.001 and %g" % (e11, e22, ey))
    message = raised_message(lambda: outputs.add_shell_in_plane_state(PREFIX + "Bad", "TOSTRAIN", quantity="Bad"))
    check(message is not None and "'quantity'" in message, "a bad quantity must be refused: " + str(message))
    message = raised_message(lambda: outputs.add_composite_failure(PREFIX + "Bad", "TOSTRAIN", "TsaiWu",
                                                                   quantity="Strain"))
    check(message is not None and "'quantity'" in message, "a failure of strains must be refused: " + str(message))
    # A stress field takes the stresses as solved, and a strain field the others, so that the stiffness of the plies
    # never turns stresses into stresses
    for add, needed in ((lambda: outputs.add_shell_in_plane_state(PREFIX + "Bad", "STRESS", quantity="Strain"),
                         "TOSTRAIN"),
                        (lambda: outputs.add_shell_in_plane_state(PREFIX + "Bad", "STRESS",
                                                                  quantity="PlaneStressFromStrain"), "TOSTRAIN"),
                        (lambda: outputs.add_composite_failure(PREFIX + "Bad", "STRESS", "TsaiWu",
                                                               quantity="PlaneStressFromStrain"), "TOSTRAIN"),
                        (lambda: outputs.add_shell_in_plane_state(PREFIX + "Bad", "TOSTRAIN"), "STRESS")):
        message = raised_message(add)
        check(message is not None and "'field_name'" in message and needed in message,
              "a field of the wrong kind must be refused naming %s: %s" % (needed, message))


def main():
    check(pmx.model.is_open(), "a model must be open")
    result = build_and_solve()
    step_id = list(result.steps.get_ids())[0]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    outputs = result.field_outputs
    outputs.add_material_axes(PREFIX + "PLY", "STRESS")
    outputs.add_shell_in_plane_state(PREFIX + "PLANE", "STRESS")
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
    groups = {entry["group_name"]: list(entry["items"]) for entry in pmx.model.tree.get_layout("results", "parts")
              if isinstance(entry, dict)}
    inclined_part, inclined = nodes_of("INCLINED")
    check(groups.get(plate_part.name) == layer_part_names and
          groups.get(inclined_part.name) == [inclined_part.name + "_Layer-" + str(k) for k in (1, 2, 3, 4)],
          "the layers of each plate must be gathered in a group named after it: %s" % groups)
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
    # The largest index over the layers of a shell element is shown on every layer: the 90 degree plies govern here
    def value(field, component, n):
        return result.get_nodal_value(step_id, increment_id, field, component, n)
    plate_max = max(value(PREFIX + "MS", "FAILURE_INDEX", n) for k in (1, 2, 3, 4) for n in layers[k])
    layer_max = []
    for n in layers[1]:
        layer_max.append(value(PREFIX + "MS", "MAX_FAILURE_INDEX", n))
        check(layer_max[-1] >= value(PREFIX + "MS", "FAILURE_INDEX", n) - 1e-6 and
              value(PREFIX + "MS", "CRITICAL_LAYER", n) in (2, 3),
              "the largest index over the layers at node %d must come from a 90 degree ply" % n)
    check(abs(max(layer_max) - plate_max) < 1e-6, "the largest index over the layers must be the largest of the plate")
    # The smallest reserve factor over the layers, which for Tsai-Wu is not the inverse of the largest index
    plate_min = min(value(PREFIX + "TW", "RESERVE_FACTOR", n) for k in (1, 2, 3, 4) for n in layers[k])
    layer_min = []
    for n in layers[1]:
        layer_min.append(value(PREFIX + "TW", "MIN_RESERVE_FACTOR", n))
        check(layer_min[-1] <= value(PREFIX + "TW", "RESERVE_FACTOR", n) + 1e-6,
              "the smallest reserve factor over the layers at node %d exceeds the one of its layer" % n)
    check(abs(min(layer_min) - plate_min) < 1e-6 * plate_min,
          "the smallest reserve factor over the layers must be the smallest of the plate")
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
    check_plane_stress_from_strains(result, step_id, increment_id, layers)
    # The inclined plate is turned in space and its layers from its own axes. Its global stresses rotated by hand into
    # the ply axes give its stresses in the material axes exactly
    r = rotation()
    for k, angle in ((1, 0), (2, 90)):
        c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
        axis1 = [c * r[i][0] + s * r[i][1] for i in range(3)]
        axis2 = [-s * r[i][0] + c * r[i][1] for i in range(3)]
        for node in layer_nodes(result, k, "Inclined")[:5]:
            sxx, syy, szz, sxy, syz, szx = [result.get_nodal_value(step_id, increment_id, "STRESS", component, node)
                                            for component in ("S11", "S22", "S33", "S12", "S23", "S13")]
            tensor = [[sxx, sxy, szx], [sxy, syy, syz], [szx, syz, szz]]
            for component, axis in (("S11", axis1), ("S22", axis2)):
                by_hand = sum(axis[i] * tensor[i][j] * axis[j] for i in range(3) for j in range(3))
                read = result.get_nodal_value(step_id, increment_id, PREFIX + "PLY", component, node)
                check(abs(read - by_hand) <= 1e-3 * max(1.0, abs(by_hand)),
                      "the inclined layer %d reads %s %.4f at node %d, not %.4f" % (k, component, read, node, by_hand))
    # And it gives the plies of the flat plate as closely as CalculiX solves the turned plate: its strains differ from
    # those of the flat plate by up to 2 % in the 90 degree plies
    for k in (1, 2, 3, 4):
        inclined_layer = layer_nodes(result, k, "Inclined")
        scale = max(abs(mean(result, step_id, increment_id, PREFIX + "PLY", c, layers[k])) for c in ("S11", "S22"))
        for component in ("S11", "S22"):
            flat = mean(result, step_id, increment_id, PREFIX + "PLY", component, layers[k])
            turned = mean(result, step_id, increment_id, PREFIX + "PLY", component, inclined_layer)
            check(abs(turned - flat) <= 0.025 * scale, "layer %d of the inclined plate reads %s %.3f, the flat one %.3f"
                  % (k, component, turned, flat))
    # The components with a direction in the material axes are drawn as a tensor plot
    pmx.view.current = "results"
    for field, component, plottable in ((PREFIX + "PLY", "S11", True), (PREFIX + "PLY", "PRINCIPAL_MAX", True),
                                        (PREFIX + "PLANE", "S11", True), (PREFIX + "PLANE", "S22", True),
                                        (PREFIX + "PLANE", "PRINCIPAL_MAX", True),
                                        (PREFIX + "PLANE", "PRINCIPAL_MIN", True), (PREFIX + "PLANE", "ANGLE", False),
                                        (PREFIX + "MS", "FAILURE_INDEX", False)):
        pmx.view.set_result_field(field, component)
        message = raised_message(lambda: setattr(pmx.view, "result_type", "TensorPlot"))
        check((message is None) == plottable, "%s.%s must %sbe a tensor plot: %s" %
              (field, component, "" if plottable else "not ", message))
        pmx.view.result_type = "ColorContours"
    # Refusals name the argument
    message = raised_message(lambda: outputs.add_composite_failure(PREFIX + "Bad", "STRESS", "Puck"))
    check(message is not None and "'criterion'" in message, "an unknown criterion must be refused: " + str(message))
    message = raised_message(lambda: outputs.add_material_axes(PREFIX + "Bad", "DISP"))
    check(message is not None and "'field_name'" in message, "a vector field must be refused: " + str(message))
    message = raised_message(lambda: outputs.add_shell_in_plane_state(PREFIX + "Bad", PREFIX + "PLY"))
    check(message is not None and "'field_name'" in message,
          "a field already in material axes must be refused: " + str(message))
    print("TEST PASSED")


main()
