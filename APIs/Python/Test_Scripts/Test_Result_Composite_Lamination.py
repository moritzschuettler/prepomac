#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Compares the composite shell results with classical lamination theory evaluated here, on an unsymmetric and unbalanced
#5 ply carbon/epoxy laminate, so that A16, A26, B, D16 and D26 all couple. composite_lamination_plates.inp holds three free
#100 x 50 plates of S8R elements, each resting on three points and loaded by consistent nodal edge loads:
#  PLATE_T  N_x = 65 N/mm, the plate bends and twists by the coupling
#  PLATE_O  the same under a material orientation turned 30 degrees about the normal, the ply angles theta + 30
#  PLATE_B  M_x = 12 N mm/mm
#Lamination theory gives [eps0; kappa] = ABD^-1 [N; M]. In the middle of each plate the script checks the mid-plane strains
#and curvatures CalculiX solves, that every layer lies at its heights with layer 1 at the bottom, the ply stresses of the
#material axes and of the shell in-plane state at every layer node, and the four failure criteria with their mode against
#the textbook formulas. A CalculiX executable must be configured.
#
#  PrePoMax.com -f composite_lamination_plates.inp -u MM_TON_S_C --script Test_Result_Composite_Lamination.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math

PREFIX = "PyTest_"
JOB = PREFIX + "Lamination"
E1, E2, V12, G12, G13, G23 = 181000.0, 10300.0, 0.28, 7170.0, 7170.0, 3700.0
STRENGTH = {"xt": 1500.0, "xc": 1500.0, "yt": 40.0, "yc": 246.0, "s12": 68.0, "s23": 60.0, "f12": -0.5}
LAYUP = [(0.15, 0.0), (0.25, 45.0), (0.20, 90.0), (0.25, -30.0), (0.15, 60.0)]
NX, MX, LENGTH, WIDTH, ELEMENT, ORIENTATION = 65.0, 12.0, 100.0, 50.0, 10.0, 30.0
PLATES = {"T": ("PLATE_T", 0.0), "O": ("PLATE_O", 100.0), "B": ("PLATE_B", 200.0)}
CRITERIA = {"MaxStress": PREFIX + "MS", "TsaiHill": PREFIX + "TH", "TsaiWu": PREFIX + "TW", "Hashin": PREFIX + "HA"}


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Lamination theory ####################################################################################################
def ply_stiffness():
    d = 1 - V12 * V12 * E2 / E1
    return E1 / d, E2 / d, V12 * E2 / d, G12


def rotated_stiffness(theta_deg):
    q11, q22, q12, q66 = ply_stiffness()
    c, s = math.cos(math.radians(theta_deg)), math.sin(math.radians(theta_deg))
    c2, s2, cs = c * c, s * s, c * s
    b11 = q11 * c2 * c2 + 2 * (q12 + 2 * q66) * s2 * c2 + q22 * s2 * s2
    b12 = (q11 + q22 - 4 * q66) * s2 * c2 + q12 * (s2 * s2 + c2 * c2)
    b16 = (q11 - q12 - 2 * q66) * cs * c2 + (q12 - q22 + 2 * q66) * cs * s2
    b22 = q11 * s2 * s2 + 2 * (q12 + 2 * q66) * s2 * c2 + q22 * c2 * c2
    b26 = (q11 - q12 - 2 * q66) * cs * s2 + (q12 - q22 + 2 * q66) * cs * c2
    b66 = (q11 + q22 - 2 * q12 - 2 * q66) * s2 * c2 + q66 * (s2 * s2 + c2 * c2)
    return [[b11, b12, b16], [b12, b22, b26], [b16, b26, b66]]


def layer_heights():
    z = -sum(t for t, _ in LAYUP) / 2
    heights = []
    for t, _ in LAYUP:
        heights.append((z, z + t))
        z += t
    return heights


def solve(matrix, vector):
    n = len(vector)
    m = [row[:] + [vector[i]] for i, row in enumerate(matrix)]
    for i in range(n):
        pivot = max(range(i, n), key=lambda r: abs(m[r][i]))
        m[i], m[pivot] = m[pivot], m[i]
        for r in range(n):
            if r != i:
                f = m[r][i] / m[i][i]
                m[r] = [m[r][k] - f * m[i][k] for k in range(n + 1)]
    return [m[i][n] / m[i][i] for i in range(n)]


def mid_plane_state(case):
    """The mid-plane strains [ex, ey, gxy] and curvatures [kx, ky, kxy] of the plate, and the angle its plies turn by."""
    offset = ORIENTATION if case == "O" else 0.0
    abd = [[0.0] * 6 for _ in range(6)]
    for (t, angle), (z0, z1) in zip(LAYUP, layer_heights()):
        q = rotated_stiffness(angle + offset)
        for i in range(3):
            for j in range(3):
                abd[i][j] += q[i][j] * (z1 - z0)
                abd[i][j + 3] += q[i][j] * (z1 ** 2 - z0 ** 2) / 2
                abd[i + 3][j] += q[i][j] * (z1 ** 2 - z0 ** 2) / 2
                abd[i + 3][j + 3] += q[i][j] * (z1 ** 3 - z0 ** 3) / 3
    x = solve(abd, [NX, 0, 0, 0, 0, 0] if case in ("T", "O") else [0, 0, 0, MX, 0, 0])
    return x[:3], x[3:], offset


def ply_stresses(state, layer, z):
    eps, kappa, offset = state
    ex, ey, gxy = [eps[i] + z * kappa[i] for i in range(3)]
    theta = math.radians(LAYUP[layer - 1][1] + offset)
    c, s = math.cos(theta), math.sin(theta)
    e1 = c * c * ex + s * s * ey + c * s * gxy
    e2 = s * s * ex + c * c * ey - c * s * gxy
    g12 = -2 * c * s * ex + 2 * c * s * ey + (c * c - s * s) * gxy
    q11, q22, q12, q66 = ply_stiffness()
    return q11 * e1 + q12 * e2, q12 * e1 + q22 * e2, q66 * g12


# Failure criteria, textbook forms #####################################################################################
def max_stress(s1, s2, t12):
    s = STRENGTH
    candidates = [(s1 / s["xt"] if s1 >= 0 else -s1 / s["xc"], 1 if s1 >= 0 else 2),
                  (s2 / s["yt"] if s2 >= 0 else -s2 / s["yc"], 3 if s2 >= 0 else 4), (abs(t12) / s["s12"], 5)]
    best = candidates[0]
    for candidate in candidates[1:]:
        if candidate[0] > best[0]:
            best = candidate
    return best


def tsai_hill(s1, s2, t12):
    s = STRENGTH
    x = s["xt"] if s1 >= 0 else s["xc"]
    y = s["yt"] if s2 >= 0 else s["yc"]
    return (s1 * s1 - s1 * s2) / (x * x) + s2 * s2 / (y * y) + t12 * t12 / s["s12"] ** 2


def tsai_wu(s1, s2, t12):
    s = STRENGTH
    f11, f22 = 1 / (s["xt"] * s["xc"]), 1 / (s["yt"] * s["yc"])
    f12 = s["f12"] * math.sqrt(f11 * f22)
    return (f11 * s1 * s1 + f22 * s2 * s2 + t12 * t12 / s["s12"] ** 2 + 2 * f12 * s1 * s2 +
            (1 / s["xt"] - 1 / s["xc"]) * s1 + (1 / s["yt"] - 1 / s["yc"]) * s2)


def hashin(s1, s2, t12):
    s = STRENGTH
    shear = (t12 / s["s12"]) ** 2
    fiber = (s1 / s["xt"]) ** 2 + shear if s1 >= 0 else (s1 / s["xc"]) ** 2
    if s2 >= 0:
        matrix = (s2 / s["yt"]) ** 2 + shear
    else:
        matrix = (s2 / (2 * s["s23"])) ** 2 + ((s["yc"] / (2 * s["s23"])) ** 2 - 1) * s2 / s["yc"] + shear
    return max(fiber, matrix)


FORMULAS = {"MaxStress": lambda s1, s2, t12: max_stress(s1, s2, t12)[0], "TsaiHill": tsai_hill, "TsaiWu": tsai_wu,
            "Hashin": hashin}


# Model ################################################################################################################
def nodes_of(element_set_name):
    """The model part whose elements are the element set, and its node coordinates."""
    element_ids = set(pmx.model.element_sets[element_set_name].get_element_ids())
    for part in pmx.model.parts:
        if set(part.get_element_ids()) == element_ids:
            ids = list(part.get_node_ids())
            coordinates = part.get_node_coordinates()
            return part.name, {ids[i]: [float(coordinates[i][j]) for j in range(3)] for i in range(len(ids))}
    raise Exception("TEST FAILED: no part holds the element set " + element_set_name)


def node_region(node_ids):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', sorted(node_ids))


def add_supports_and_edge_loads(step, case, nodes, y0):
    def at(x, y):
        return [n for n, c in nodes.items() if abs(c[0] - x) < 1e-6 and abs(c[1] - y0 - y) < 1e-6]
    # Statically determinate, so the self-equilibrated edge loads leave no reactions
    step.bcs.add_displacement_rotation(PREFIX + case + "_A", node_region(at(0, 0)), u1=0, u2=0, u3=0)
    step.bcs.add_displacement_rotation(PREFIX + case + "_B", node_region(at(LENGTH, 0)), u2=0, u3=0)
    step.bcs.add_displacement_rotation(PREFIX + case + "_C", node_region(at(0, WIDTH)), u3=0)
    # A quadratic edge takes 1/6, 4/6 and 1/6 of the load of an element edge, a corner node from both its elements
    for edge, x, sign in (("X0", 0.0, -1.0), ("X1", LENGTH, 1.0)):
        groups = {}
        for n, c in nodes.items():
            if abs(c[0] - x) > 1e-6:
                continue
            y = c[1] - y0
            if int(round(y / (ELEMENT / 2))) % 2 == 1:
                weight = 4.0 / 6.0
            elif abs(y) < 1e-6 or abs(y - WIDTH) < 1e-6:
                weight = 1.0 / 6.0
            else:
                weight = 2.0 / 6.0
            groups.setdefault(weight, []).append(n)
        for i, (weight, node_ids) in enumerate(sorted(groups.items())):
            name = PREFIX + case + "_" + edge + "_" + str(i)
            if case in ("T", "O"):
                step.loads.add_concentrated_force(name, node_region(node_ids), f1=sign * NX * ELEMENT * weight)
            else:
                step.loads.add_moment(name, node_region(node_ids), m2=sign * MX * ELEMENT * weight)


def build_and_solve():
    s = STRENGTH
    pmx.model.materials.add(PREFIX + "CFRP", [pmx.create.elastic_lamina([[E1, E2, V12, G12, G13, G23, 0]]),
                                              pmx.create.composite_strength(s["xt"], s["xc"], s["yt"], s["yc"], s["s12"],
                                                                            s["s23"], tsai_wu_coefficient=s["f12"])])
    layers = [[t, PREFIX + "CFRP", angle] for t, angle in LAYUP]
    for case, (element_set, _) in PLATES.items():
        pmx.model.sections.add_composite_shell(PREFIX + case, pmx.create.region("ElementSetName", element_set), layers)
    c, s_ = math.cos(math.radians(ORIENTATION)), math.sin(math.radians(ORIENTATION))
    y0 = PLATES["O"][1]
    pmx.model.coordinate_systems.add(PREFIX + "Turned", [0, y0, 0], [c, y0 + s_, 0], [-s_, y0 + c, 0])
    pmx.model.material_orientations.add(PREFIX + "Turned", pmx.create.region("ElementSetName", "PLATE_O"),
                                        coordinate_system_name=PREFIX + "Turned")
    step = pmx.model.steps.add_static(PREFIX + "Step")
    part_names = {}
    for case, (element_set, y0) in PLATES.items():
        part_names[case], nodes = nodes_of(element_set)
        add_supports_and_edge_loads(step, case, nodes, y0)
    pmx.jobs.add(JOB)
    status = pmx.jobs[JOB].run(timeout=300)
    check(status == "OK", "the analysis did not succeed, got " + status + "\n" + pmx.jobs[JOB].get_output())
    pmx.jobs.open_results(JOB)
    return pmx.results.current, part_names


# Checks ###############################################################################################################
def fit_line(points):
    n = len(points)
    mean_z = sum(z for z, _ in points) / n
    mean_v = sum(v for _, v in points) / n
    slope = sum((z - mean_z) * (v - mean_v) for z, v in points) / sum((z - mean_z) ** 2 for z, _ in points)
    return mean_v - slope * mean_z, slope


def main():
    check(pmx.model.is_open(), "a model must be open")
    result, part_names = build_and_solve()
    step_id = list(result.steps.get_ids())[0]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    outputs = result.field_outputs
    outputs.add_material_axes(PREFIX + "PLY", "STRESS")
    outputs.add_shell_in_plane_state(PREFIX + "PLANE", "STRESS")
    for criterion, name in CRITERIA.items():
        outputs.add_composite_failure(name, "STRESS", criterion)

    def value(field, component, node):
        return result.get_nodal_value(step_id, increment_id, field, component, node)

    for case, (_, y0) in PLATES.items():
        state = mid_plane_state(case)
        heights = layer_heights()
        rows = []
        for layer in range(1, len(LAYUP) + 1):
            name = part_names[case] + "_Layer-" + str(layer)
            check(name in result.mesh.parts, "the layer part %s is missing: %s" % (name, list(result.mesh.parts.get_names())))
            part = result.mesh.parts[name]
            ids = list(part.get_node_ids())
            coordinates = part.get_node_coordinates()
            z0, z1 = heights[layer - 1]
            for i, node in enumerate(ids):
                x, y, z = [float(coordinates[i][k]) for k in range(3)]
                check(z0 - 1e-6 <= z <= z1 + 1e-6, "plate %s layer %d has a node at z = %.4f, outside [%.3f, %.3f]: "
                      "layer 1 must lie at the bottom, opposite the normal" % (case, layer, z, z0, z1))
                # The middle of the plate, clear of the loaded edges and the free edges
                if 30 <= x <= 70 and abs(y - y0 - WIDTH / 2) <= 10.0001:
                    rows.append((layer, node, z))
        check(len(rows) == 5 * 76, "plate %s: %d layer nodes in the middle, not 380" % (case, len(rows)))
        # The mid-plane strains and curvatures CalculiX solves are those of lamination theory
        eps, kappa, _ = state
        for k, component, factor in ((0, "E11", 1), (1, "E22", 1), (2, "E12", 2)):
            e0, curvature = fit_line([(z, factor * value("TOSTRAIN", component, node)) for _, node, z in rows])
            check(abs(e0 - eps[k]) <= 0.005 * max(abs(v) for v in eps) and
                  abs(curvature - kappa[k]) <= 0.005 * max(abs(v) for v in kappa),
                  "plate %s: %s fits %.4e + z %.4e, lamination theory gives %.4e + z %.4e" %
                  (case, component, e0, curvature, eps[k], kappa[k]))
        # The ply stresses and the failure criteria at every layer node
        expected = {(layer, node): ply_stresses(state, layer, z) for layer, node, z in rows}
        scale = max(max(abs(v) for v in stresses) for stresses in expected.values())
        for (layer, node), (s1, s2, t12) in expected.items():
            for field in (PREFIX + "PLY", PREFIX + "PLANE"):
                read = [value(field, component, node) for component in ("S11", "S22", "S12")]
                check(all(abs(read[k] - (s1, s2, t12)[k]) <= 0.005 * scale for k in range(3)),
                      "plate %s layer %d node %d: %s reads %s, lamination theory gives %.3f %.3f %.3f" %
                      (case, layer, node, field, ["%.3f" % v for v in read], s1, s2, t12))
            for criterion, field in CRITERIA.items():
                index = value(field, "FAILURE_INDEX", node)
                check(abs(index - FORMULAS[criterion](s1, s2, t12)) <= 0.005,
                      "plate %s layer %d node %d: %s reads %.4f, lamination theory gives %.4f" %
                      (case, layer, node, criterion, index, FORMULAS[criterion](s1, s2, t12)))
            mode = value(CRITERIA["MaxStress"], "MODE", node)
            check(mode == max_stress(s1, s2, t12)[1], "plate %s layer %d node %d: the maximum stress mode is %s, not %d" %
                  (case, layer, node, mode, max_stress(s1, s2, t12)[1]))
        print("plate %s: %d layer nodes agree with lamination theory" % (case, len(rows)))
    print("TEST PASSED")


main()
