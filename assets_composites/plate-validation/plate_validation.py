#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Validation of the three ways PrePoMax solves composite shells against classical laminate theory (CLT):
#  - CalculiX layered: every ply block a layer of its own, expanded by CalculiX into solid elements
#  - CalculiX smeared: one orthotropic layer with the in-plane stiffness (A matrix) of the laminate, the plies
#    recovered from the strain of the laminate, as the bike frame study does it
#  - Code_Aster: true layered shells (DKT, DEFI_COMPOSITE), in Docker
#
#Three cases with a closed solution, all of the T700/epoxy UD ply of the bike frame:
#  NAVIER    a square plate of 200 mm, simply supported on all edges, under a uniform load (gravity); cross-ply
#            [0/90]s, which is specially orthotropic, so the Navier series is exact: deflection of the centre and the
#            Tsai-Wu reserve factor at the centre
#  TENSION   a strip of 200 x 20 mm pulled along its axis; the layup of the head tube [+45/-45/0/90]s: the strain is
#            the inverse of A times the force per width, the elongation and the reserve factor follow
#  CANTILEVER the same strip clamped at one end and loaded at the other: the deflection F L^3 d11 / (3 b) with d the
#            inverse of D, and the reserve factor in the middle of the span from the curvature d M
#The loads are scaled so that the analytic reserve factor is 2.
#
#  PrePoMax.com --script plate_validation.py -g No -x Yes -w <work directory inside the home folder>
#  PLATE_CASE=NAVIER, TENSION or CANTILEVER chooses the case (one case per run, as a script cannot start a new
#  model), PLATE_VARIANTS=layered,smeared,codeaster (the default) the variants; run_validation.sh runs all three

import math
import os
import time

HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else os.getcwd()
CASE = os.environ.get("PLATE_CASE", "NAVIER").upper()
VARIANTS = os.environ.get("PLATE_VARIANTS", "layered,smeared,codeaster").split(",")
PLY_THICKNESS = 0.15
DENSITY = 1.55e-9
LAMINA = [125000, 8500, 0.3, 4500, 4500, 3000]
STRENGTH = {"xt": 2000, "xc": 1200, "yt": 50, "yc": 200, "s12": 80, "s23": 60, "f12": -0.5}
MESH_SIZE = 5.0
TARGET_RESERVE = 2.0
PREFIX = "Val_"

# The ply blocks of the half laminate from the outside in, as (angle, plies); the layup is mirrored
CASES = {
    "NAVIER":     {"geometry": "square_plate.brep", "size": (200.0, 200.0), "half": [(0, 2), (90, 2)]},
    "TENSION":    {"geometry": "strip.brep", "size": (200.0, 20.0), "half": [(45, 2), (-45, 2), (0, 2), (90, 1)]},
    "CANTILEVER": {"geometry": "strip.brep", "size": (200.0, 20.0), "half": [(45, 2), (-45, 2), (0, 2), (90, 1)]},
}


def log(text):
    print(text)


# Classical laminate theory
def ply_q():
    e1, e2, nu12, g12 = LAMINA[0], LAMINA[1], LAMINA[2], LAMINA[3]
    nu21 = nu12 * e2 / e1
    d = 1 - nu12 * nu21
    return [[e1 / d, nu12 * e2 / d, 0.0], [nu12 * e2 / d, e2 / d, 0.0], [0.0, 0.0, g12]]


def rotated_q(angle_deg):
    q = ply_q()
    q11, q12, q22, q66 = q[0][0], q[0][1], q[1][1], q[2][2]
    c, s = math.cos(math.radians(angle_deg)), math.sin(math.radians(angle_deg))
    c2, s2, cs = c * c, s * s, c * s
    b11 = q11 * c2 * c2 + 2 * (q12 + 2 * q66) * s2 * c2 + q22 * s2 * s2
    b12 = (q11 + q22 - 4 * q66) * s2 * c2 + q12 * (s2 * s2 + c2 * c2)
    b22 = q11 * s2 * s2 + 2 * (q12 + 2 * q66) * s2 * c2 + q22 * c2 * c2
    b16 = (q11 - q12 - 2 * q66) * c2 * cs + (q12 - q22 + 2 * q66) * s2 * cs
    b26 = (q11 - q12 - 2 * q66) * s2 * cs + (q12 - q22 + 2 * q66) * c2 * cs
    b66 = (q11 + q22 - 2 * q12 - 2 * q66) * s2 * c2 + q66 * (s2 * s2 + c2 * c2)
    return [[b11, b12, b16], [b12, b22, b26], [b16, b26, b66]]


def inverse3(m):
    a, b, c = m[0]
    d, e, f = m[1]
    g, h, i = m[2]
    det = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g)
    return [[(e * i - f * h) / det, (c * h - b * i) / det, (b * f - c * e) / det],
            [(f * g - d * i) / det, (a * i - c * g) / det, (c * d - a * f) / det],
            [(d * h - e * g) / det, (b * g - a * h) / det, (a * e - b * d) / det]]


def blocks(case):
    """The ply blocks from the top surface down as (angle, z top, z bottom)."""
    half = [(angle, plies * PLY_THICKNESS) for angle, plies in CASES[case]["half"]]
    full = half + half[::-1]
    t = sum(h for _, h in full)
    z = t / 2
    out = []
    for angle, h in full:
        out.append((angle, z, z - h))
        z -= h
    return out, t


def abd(case):
    layers, t = blocks(case)
    a = [[0.0] * 3 for _ in range(3)]
    d = [[0.0] * 3 for _ in range(3)]
    for angle, top, bottom in layers:
        q = rotated_q(angle)
        for i in range(3):
            for j in range(3):
                a[i][j] += q[i][j] * (top - bottom)
                d[i][j] += q[i][j] * (top ** 3 - bottom ** 3) / 3
    return a, d, t


def ply_reserve(strain, angle):
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    ex, ey, gxy = strain
    e1 = c * c * ex + s * s * ey + c * s * gxy
    e2 = s * s * ex + c * c * ey - c * s * gxy
    g12 = -2 * c * s * ex + 2 * c * s * ey + (c * c - s * s) * gxy
    q = ply_q()
    s1 = q[0][0] * e1 + q[0][1] * e2
    s2 = q[0][1] * e1 + q[1][1] * e2
    t12 = q[2][2] * g12
    st = STRENGTH
    f1, f2 = 1 / st["xt"] - 1 / st["xc"], 1 / st["yt"] - 1 / st["yc"]
    f11, f22, f66 = 1 / (st["xt"] * st["xc"]), 1 / (st["yt"] * st["yc"]), 1 / st["s12"] ** 2
    f12 = st["f12"] * math.sqrt(f11 * f22)
    quadratic = f11 * s1 * s1 + f22 * s2 * s2 + f66 * t12 * t12 + 2 * f12 * s1 * s2
    linear = f1 * s1 + f2 * s2
    if quadratic <= 0:
        return float("inf") if linear <= 0 else 1 / linear
    return (-linear + math.sqrt(linear * linear + 4 * quadratic)) / (2 * quadratic)


def laminate_reserve(case, membrane, curvature):
    """The smallest reserve factor of all plies at their top and bottom surfaces."""
    layers, _ = blocks(case)
    worst = float("inf")
    for angle, top, bottom in layers:
        for z in (top, bottom):
            strain = [membrane[i] + z * curvature[i] for i in range(3)]
            worst = min(worst, ply_reserve(strain, angle))
    return worst


def analytic(case):
    """The load, the deflection or elongation and the reserve factor of the closed solution."""
    a, d, t = abd(case)
    length, width = CASES[case]["size"]
    if case == "NAVIER":
        # A unit pressure first, then scaled to the target reserve; the reserve factor is a load factor
        def centre(q):
            w = kx = ky = 0.0
            for m in range(1, 200, 2):
                for n in range(1, 200, 2):
                    am, bn = m * math.pi / length, n * math.pi / width
                    wmn = 16 * q / (math.pi ** 2 * m * n) / (d[0][0] * am ** 4 + 2 * (d[0][1] + 2 * d[2][2]) *
                                                               am ** 2 * bn ** 2 + d[1][1] * bn ** 4)
                    sign = math.sin(m * math.pi / 2) * math.sin(n * math.pi / 2)
                    w += wmn * sign
                    kx += wmn * am ** 2 * sign
                    ky += wmn * bn ** 2 * sign
            return w, [kx, ky, 0.0]
        _, k = centre(1.0)
        q = laminate_reserve(case, [0, 0, 0], k) / TARGET_RESERVE
        w, k = centre(q)
        return {"load": q, "value": -w, "reserve": laminate_reserve(case, [0, 0, 0], k), "t": t}
    if case == "TENSION":
        inv = inverse3(a)
        strain = [inv[i][0] for i in range(3)]                    # per unit force per width
        n = laminate_reserve(case, strain, [0, 0, 0]) / TARGET_RESERVE
        force = n * width
        return {"load": force, "value": inv[0][0] * n * length,
                "reserve": laminate_reserve(case, [s * n for s in strain], [0, 0, 0]), "t": t}
    inv = inverse3(d)
    # The moment per width in the middle of the span is F (L / 2) / b
    curvature = [inv[i][0] * (length / 2) / width for i in range(3)]          # per unit force
    force = laminate_reserve(case, [0, 0, 0], curvature) / TARGET_RESERVE
    return {"load": force, "value": -force * length ** 3 * inv[0][0] / (3 * width),
            "reserve": laminate_reserve(case, [0, 0, 0], [c * force for c in curvature]), "t": t}


# Model
def build(case, load):
    # A script cannot start a new model, so every case runs in a PrePoMax of its own (PLATE_CASE)
    pmx.model.properties.unit_system = 'MM_TON_S_C'
    pmx.model.properties.model_space = 'ThreeD'
    pmx.import_file(os.path.join(HERE, CASES[case]["geometry"]))
    part = pmx.model.geometry.parts.first()
    selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
    pmx.model.mesh.setup_items.add_meshing_parameters(PREFIX + "Mesh", selection, max_size=MESH_SIZE,
                                                      min_size=MESH_SIZE / 4, second_order=True)
    pmx.model.mesh.setup_items.add_netgen_mesh(PREFIX + "Netgen", selection, quad_dominated=True)
    report = pmx.model.mesh.generate(part.name)
    if report[part.name] != "":
        raise Exception("The plate could not be meshed: " + report[part.name])
    mesh_part = pmx.model.parts.first()
    types = pmx.model.parts.get_element_types(mesh_part.name)
    if "parabolic_quad" in types:
        pmx.model.parts.set_element_type([mesh_part.name], 'parabolic_quad', 'S8R')
    if "parabolic_tria" in types:
        pmx.model.parts.set_element_type([mesh_part.name], 'parabolic_tria', 'S6')
    elements = pmx.create.selection_by_ids('femodel', 'elements', 'parts', [mesh_part.id])
    pmx.model.materials.add(PREFIX + "UD_T700", [pmx.create.elastic_lamina([LAMINA + [0]]),
                                                 pmx.create.density(DENSITY),
                                                 pmx.create.composite_strength(STRENGTH["xt"], STRENGTH["xc"],
                                                                               STRENGTH["yt"], STRENGTH["yc"],
                                                                               STRENGTH["s12"], STRENGTH["s23"])])
    # The 0 degree direction along the global x axis
    pmx.model.coordinate_systems.add(PREFIX + "CS", [0, 0, 0], [1, 0, 0], [0, 1, 0])
    pmx.model.material_orientations.add(PREFIX + "Fibers", elements, coordinate_system_name=PREFIX + "CS",
                                        reference_axis='X')
    # The nodes of the edges
    data = pmx.model.nodes.get_data()
    ids = [int(i) for i in data.ids]
    xyz = [[float(c) for c in row] for row in data.coordinates]
    length, width = CASES[case]["size"]
    edge = {"x0": [i for i, p in zip(ids, xyz) if abs(p[0]) < 1e-6],
            "xL": [i for i, p in zip(ids, xyz) if abs(p[0] - length) < 1e-6],
            "y0": [i for i, p in zip(ids, xyz) if abs(p[1]) < 1e-6],
            "yB": [i for i, p in zip(ids, xyz) if abs(p[1] - width) < 1e-6],
            "origin": [i for i, p in zip(ids, xyz) if abs(p[0]) < 1e-6 and abs(p[1]) < 1e-6],
            "all": ids}
    for name, node_ids in edge.items():
        pmx.model.node_sets.add(PREFIX + name, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', node_ids))

    def region(name):
        return pmx.create.region('NodeSetName', PREFIX + name)

    step = pmx.model.steps.add_static(PREFIX + case)
    if case == "NAVIER":
        for name in ("x0", "xL", "y0", "yB"):
            step.bcs.add_displacement_rotation(PREFIX + "SS_" + name, region(name), u3=0)
        step.bcs.add_displacement_rotation(PREFIX + "Hold_x", region("x0"), u1=0)
        step.bcs.add_displacement_rotation(PREFIX + "Hold_y", region("y0"), u2=0)
        # The pressure q is the weight of the plate: q = rho g t
        step.loads.add_gravity(PREFIX + "Weight", pmx.create.selection_by_ids("femodel", "parts", "parts", [mesh_part.id]), f3=-load / (DENSITY * analytic(case)["t"]))
    elif case == "TENSION":
        step.bcs.add_displacement_rotation(PREFIX + "Hold_x", region("x0"), u1=0)
        step.bcs.add_displacement_rotation(PREFIX + "Hold_y", region("origin"), u2=0)
        step.bcs.add_displacement_rotation(PREFIX + "Flat", region("all"), u3=0)
        step.loads.add_concentrated_force(PREFIX + "Pull", region("xL"), f1=load / len(edge["xL"]))
    else:
        step.bcs.add_fixed(PREFIX + "Clamp", region("x0"))
        step.loads.add_concentrated_force(PREFIX + "Tip", region("xL"), f3=-load / len(edge["xL"]))
    step.field_outputs.add_nodal(PREFIX + "NF", ["U"])
    step.field_outputs.add_element(PREFIX + "EF", ["S"])
    return mesh_part, elements, len(ids)


def smeared_constants(case):
    a, _, t = abd(case)
    inv = inverse3(a)
    layers, _ = blocks(case)
    g13 = sum((LAMINA[4] * math.cos(math.radians(an)) ** 2 + LAMINA[5] * math.sin(math.radians(an)) ** 2) *
              (top - bottom) for an, top, bottom in layers) / t
    g23 = sum((LAMINA[4] * math.sin(math.radians(an)) ** 2 + LAMINA[5] * math.cos(math.radians(an)) ** 2) *
              (top - bottom) for an, top, bottom in layers) / t
    row = [1 / (t * inv[0][0]), 1 / (t * inv[1][1]), LAMINA[1], -inv[0][1] / inv[0][0], LAMINA[2], 0.4,
           1 / (t * inv[2][2]), g13, g23, 0]
    compliance = [[inv[i][j] * t for j in range(3)] for i in range(3)]
    return row, compliance, t


def set_section(case, elements, variant):
    name = PREFIX + "Laminate"
    if variant == "smeared":
        row, _, t = smeared_constants(case)
        material = PREFIX + "Smeared"
        if material not in list(pmx.model.materials.get_names()):
            pmx.model.materials.add(material, [pmx.create.elastic_engineering_constants([row]),
                                               pmx.create.density(DENSITY)])
        layers, symmetric = [[t, material, 0]], False
    else:
        layers = [[plies * PLY_THICKNESS, PREFIX + "UD_T700", angle] for angle, plies in CASES[case]["half"]]
        symmetric = True
    if name in list(pmx.model.sections.get_names()):
        pmx.model.sections.replace_composite_shell(name, layers=layers, symmetric=symmetric)
    else:
        pmx.model.sections.add_composite_shell(name, elements, layers, symmetric=symmetric)


# Solving and evaluation
def solve(case, variant):
    job = PREFIX + case + "_" + variant
    if not pmx.jobs.contains(job):
        if variant == "codeaster":
            pmx.jobs.add(job, fem_solver="CodeAster")
        else:
            pmx.jobs.add(job)
    start = time.time()
    status = pmx.jobs[job].run(timeout=1800)
    if status != "OK":
        raise Exception(job + " did not solve: " + status + "\n" + pmx.jobs[job].get_output()[-3000:])
    seconds = time.time() - start
    pmx.jobs.open_results(job)
    return pmx.results.current, seconds


def last_fields(result):
    step_id = list(result.steps.get_ids())[-1]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    return result.steps[step_id].increments[increment_id].fields


def node_coordinates(result):
    data = result.mesh.nodes.get_data()
    return dict(zip((int(i) for i in data.ids), ([float(c) for c in row] for row in data.coordinates)))


def where(case):
    """The nodes the deflection is measured at, and the nodes the reserve factor is compared at."""
    length, width = CASES[case]["size"]
    if case == "NAVIER":
        return (lambda p: math.hypot(p[0] - 100, p[1] - 100) < 4,
                lambda p: math.hypot(p[0] - 100, p[1] - 100) < 6)
    if case == "TENSION":
        return (lambda p: abs(p[0] - length) < 1e-3,
                lambda p: 60 < p[0] < 140)
    return (lambda p: abs(p[0] - length) < 1e-3,
            lambda p: abs(p[0] - length / 2) < 2.6 and 3 < p[1] < width - 3)


def reserves(result, case, variant):
    """The reserve factor at every node: Tsai-Wu of the plies, from the solver's ply stresses or recovered from the
    smeared stresses."""
    fields = last_fields(result)
    if variant == "codeaster":
        component = fields["TSAIWU"].components["RESERVE"]
        return lambda node: component.get_value(node)
    if variant == "layered":
        name = PREFIX + "TsaiWu"
        if name not in list(result.field_outputs.get_names()):
            result.field_outputs.add_composite_failure(name, "STRESS", "TsaiWu")
        component = last_fields(result)[name].components["RESERVE_FACTOR"]
        return lambda node: component.get_value(node)
    name = PREFIX + "Axes"
    if name not in list(result.field_outputs.get_names()):
        result.field_outputs.add_material_axes(name, "STRESS")
    components = last_fields(result)[name].components
    values = {}
    for c in ("S11", "S22", "S12"):
        data = components[c].get_data()
        values[c] = dict(zip((int(i) for i in data.ids), (float(v) for v in data.values)))
    _, compliance, _ = smeared_constants(case)
    angles = sorted(set(angle for angle, _ in CASES[case]["half"]))

    def recovered(node):
        stress = [values[c].get(node, float("nan")) for c in ("S11", "S22", "S12")]
        if any(v != v for v in stress):
            return float("nan")
        strain = [sum(compliance[i][j] * stress[j] for j in range(3)) for i in range(3)]
        return min(ply_reserve(strain, angle) for angle in angles)
    return recovered


def evaluate(result, case, variant):
    coordinates = node_coordinates(result)
    at_value, at_reserve = where(case)
    disp = last_fields(result)["DISP"].components["U1" if case == "TENSION" else "U3"]
    moved = [disp.get_value(n) for n, p in coordinates.items() if at_value(p)]
    moved = [v for v in moved if v == v]
    reserve = reserves(result, case, variant)
    values = [reserve(n) for n, p in coordinates.items() if at_reserve(p)]
    values = [v for v in values if v == v]
    return sum(moved) / len(moved), min(values)


def main():
    rows = []
    for case in [CASE]:
        exact = analytic(case)
        mesh_part, elements, node_count = build(case, exact["load"])
        log("%s: t = %.2f mm, load %.4g, %d nodes; analytic %.4f mm, reserve %.3f"
            % (case, exact["t"], exact["load"], node_count, exact["value"], exact["reserve"]))
        for variant in VARIANTS:
            set_section(case, elements, variant)
            result, seconds = solve(case, variant)
            value, reserve = evaluate(result, case, variant)
            rows.append((case, variant, value, exact["value"], reserve, exact["reserve"], seconds))
            log("  %-10s %9.4f mm (%+6.1f %%)  reserve %.3f (%+6.1f %%)  %5.0f s"
                % (variant, value, 100 * (value / exact["value"] - 1), reserve,
                   100 * (reserve / exact["reserve"] - 1), seconds))
    log("")
    log("%-11s %-10s %11s %11s %8s %9s %9s %7s %6s" % ("case", "variant", "value", "analytic", "error",
                                                      "reserve", "analytic", "error", "time"))
    for case, variant, value, exact_value, reserve, exact_reserve, seconds in rows:
        log("%-11s %-10s %11.4f %11.4f %7.1f%% %9.3f %9.3f %6.1f%% %5.0fs"
            % (case, variant, value, exact_value, 100 * (value / exact_value - 1), reserve, exact_reserve,
               100 * (reserve / exact_reserve - 1), seconds))


main()
