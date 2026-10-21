#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Strength and stiffness of a carbon road bike frame of shell elements, and a simple optimization of its plies.
#
#bike_frame.brep (made by make_frame.py) is the mid-surface of the frame. Every tube group gets a composite shell
#section of its own, whose fibers run along the tube: the 0 degree direction of every tube is the axis of its own
#coordinate system. Two load cases are solved by CalculiX:
#  - PEDAL: 600 N sideways on the bottom bracket, the dropouts clamped and the head tube held as the fork holds it;
#    the bottom bracket stiffness is the force over the sideways deflection of the bottom bracket.
#  - HEAD: 500 N sideways on the head tube, the dropouts clamped and the bottom bracket held vertically; the
#    lateral head tube stiffness is the force over the sideways deflection of the head tube.
#The strength is the Tsai-Wu failure index of every ply in its material axes (a von Mises stress has no meaning for
#a laminate); with the safety factor SAFETY the reserve factor of each tube group must stay above 1.
#
#The optimization changes the number of plies of each tube group: a group that fails gets plies (0 degree plies when
#the fibers fail, +-45 degree pairs when the matrix fails), a group far below the limit loses plies, and the down
#tube and the chain stays get 0 degree plies while a stiffness is below its target. The frame mass is printed for
#every iteration, and the last layups are left in the model, which is saved as bike_frame_study.pmx.
#
#  PrePoMax.com --script bike_frame_study.py -g No -x Yes -w <work directory>       (or Tools > Run script in the GUI)

import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else os.getcwd()
FRAME = json.load(open(os.path.join(HERE, "bike_frame_faces.json")))

PLY_THICKNESS = 0.15                # mm, UD prepreg
DENSITY = 1.55e-9                   # t/mm^3
SAFETY = 1.5
ITERATIONS = int(os.environ.get("FRAME_ITERATIONS", "6"))
TARGET_BB_STIFFNESS = 150.0         # N/mm, sideways at the bottom bracket
TARGET_HEAD_STIFFNESS = 90.0        # N/mm, sideways at the head tube
MESH_SIZE = 8.0                     # mm
# T700/epoxy UD ply (MPa): E1, E2, nu12, G12, G13, G23 and the strengths
LAMINA = [125000, 8500, 0.3, 4500, 4500, 3000]
STRENGTH = {"xt": 2000, "xc": 1200, "yt": 50, "yc": 200, "s12": 80, "s23": 60}

# The tube groups and their first layup as numbers of plies: [0 degree plies, +-45 degree pairs, 90 degree plies]
GROUPS = {
    "HEAD":        (["HEAD_TUBE"], [2, 2, 1]),
    "TOP":         (["TOP_TUBE"], [2, 1, 0]),
    "DOWN":        (["DOWN_TUBE"], [3, 2, 0]),
    "SEAT":        (["SEAT_TUBE"], [2, 1, 0]),
    "BB":          (["BB_SHELL"], [2, 2, 1]),
    "CHAIN_STAYS": (["CHAIN_STAY_L", "CHAIN_STAY_R"], [3, 1, 0]),
    "SEAT_STAYS":  (["SEAT_STAY_L", "SEAT_STAY_R"], [2, 1, 0]),
    "DROPOUTS":    (["DROPOUT_L", "DROPOUT_R"], [2, 2, 1]),
}
MIN_PLIES = {"zero": 1, "pairs": 1}
PREFIX = "Frame_"


def log(text):
    print(text)


# Geometry and mesh
def import_frame():
    # The frame is opened with -f bike_frame.brep, or imported into the empty model the script starts with
    if len(list(pmx.model.geometry.parts.get_names())) == 0:
        pmx.model.properties.unit_system = 'MM_TON_S_C'
        pmx.model.properties.model_space = 'ThreeD'
        pmx.import_file(os.path.join(HERE, "bike_frame.brep"))
    part = pmx.model.geometry.parts.first()
    selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters(PREFIX + "Mesh", selection, max_size=MESH_SIZE, min_size=MESH_SIZE / 4,
                                 elements_per_curve=3, second_order=True)
    items.add_netgen_mesh(PREFIX + "Netgen", selection, quad_dominated=True)
    report = pmx.model.mesh.generate(part.name)
    if report[part.name] != "":
        raise Exception("The frame could not be meshed: " + report[part.name])
    mesh_part = pmx.model.parts.first()
    # CalculiX takes a composite shell of S8R and S6 elements
    types = pmx.model.parts.get_element_types(mesh_part.name)
    if "parabolic_quad" in types:
        pmx.model.parts.set_element_type([mesh_part.name], 'parabolic_quad', 'S8R')
    if "parabolic_tria" in types:
        pmx.model.parts.set_element_type([mesh_part.name], 'parabolic_tria', 'S6')
    log("mesh: %s, %s" % (mesh_part.name, dict(pmx.model.parts.get_element_types(mesh_part.name))))
    return mesh_part


def tube_faces(mesh_part):
    """The face ids of each tube, found by the centres and areas make_frame.py recorded."""
    data = mesh_part.get_face_data()
    ids = [int(i) for i in data.ids]
    centroids = [[float(c) for c in centroid] for centroid in data.centroids]
    areas = [float(a) for a in data.areas]
    faces = {}
    area_of = {}
    for tube, records in FRAME["face_centers"].items():
        for record in records:
            best = min(range(len(ids)), key=lambda i: math.dist(centroids[i], record["center"]) +
                       abs(areas[i] - record["area"]) / max(record["area"], 1.0) * 10)
            faces.setdefault(tube, []).append(ids[best])
            area_of[ids[best]] = areas[best]
    return faces, area_of


# Model
def add_materials():
    pmx.model.materials.add(PREFIX + "UD_T700", [pmx.create.elastic_lamina([LAMINA + [0]]),
                                                 pmx.create.density(DENSITY),
                                                 pmx.create.composite_strength(STRENGTH["xt"], STRENGTH["xc"],
                                                                               STRENGTH["yt"], STRENGTH["yc"],
                                                                               STRENGTH["s12"], STRENGTH["s23"])])


def element_selection(part, face_ids):
    return pmx.create.selection_by_ids('femodel', 'elements', 'surfaces', {part.id: face_ids})


def node_selection(part, face_ids):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'surfaces', {part.id: face_ids})


def add_orientations(part, faces):
    """The 0 degree direction of every tube along its axis."""
    for tube, ids in faces.items():
        start = FRAME["tubes"][tube]["start"]
        end = FRAME["tubes"][tube]["end"]
        axis = [end[i] - start[i] for i in range(3)]
        # Any point off the axis spans the xy plane
        helper = [0.0, 0.0, 1.0] if abs(axis[2]) < 0.9 * math.sqrt(sum(a * a for a in axis)) else [1.0, 0.0, 0.0]
        in_xy = [start[i] + helper[i] * 10 for i in range(3)]
        name = PREFIX + "CS_" + tube
        pmx.model.coordinate_systems.add(name, start, end, in_xy)
        pmx.model.material_orientations.add(PREFIX + "Fibers_" + tube, element_selection(part, ids),
                                            coordinate_system_name=name, reference_axis='X')


def layup_layers(counts):
    """The plies of one orientation in a block of their summed thickness: +45, -45, 0 and 90 degrees from the outside
    in, mirrored about the mid-surface. CalculiX expands every layer through the thickness, so blocks keep the model
    small; the stresses of a block are those of each of its plies."""
    zero, pairs, ninety = counts
    layers = []
    if pairs:
        layers += [[pairs * PLY_THICKNESS, PREFIX + "UD_T700", 45], [pairs * PLY_THICKNESS, PREFIX + "UD_T700", -45]]
    if zero:
        layers += [[zero * PLY_THICKNESS, PREFIX + "UD_T700", 0]]
    if ninety:
        layers += [[ninety * PLY_THICKNESS, PREFIX + "UD_T700", 90]]
    return layers


def set_sections(part, faces, layups, first):
    for group, (tubes, _) in GROUPS.items():
        ids = [i for tube in tubes for i in faces[tube]]
        name = PREFIX + "Laminate_" + group
        # The layup is symmetric about the mid-surface
        if first:
            pmx.model.sections.add_composite_shell(name, element_selection(part, ids), layup_layers(layups[group]),
                                                   symmetric=True)
        else:
            pmx.model.sections.replace_composite_shell(name, layers=layup_layers(layups[group]), symmetric=True)


def frame_mass(faces, area_of, layups):
    mass = 0.0
    for group, (tubes, _) in GROUPS.items():
        zero, pairs, ninety = layups[group]
        thickness = 2 * (zero + 2 * pairs + ninety) * PLY_THICKNESS          # symmetric
        mass += sum(area_of[i] for tube in tubes for i in faces[tube]) * thickness * DENSITY
    return mass * 1e6                                                      # g


def add_node_sets(part, faces):
    """The nodes of the walls held and loaded: the dropouts, the head tube and the bottom bracket shell. CalculiX
    expands every composite shell through its thickness, and couplings of whole tube walls take it very long, so the
    walls are held and loaded directly: the force is shared out over the nodes of the wall."""
    counts = {}
    for name, tubes in (("AXLE", ["DROPOUT_L", "DROPOUT_R"]), ("HEAD", ["HEAD_TUBE"]), ("BB", ["BB_SHELL"])):
        set_name = PREFIX + "Nodes_" + name
        pmx.model.node_sets.add(set_name, node_selection(part, [i for t in tubes for i in faces[t]]))
        counts[name] = len(list(pmx.model.node_sets[set_name].get_node_ids()))
    return counts


def nodes(name):
    return pmx.create.region('NodeSetName', PREFIX + "Nodes_" + name)


def add_step(load_case, counts):
    step = pmx.model.steps.add_static(PREFIX + load_case)
    # The axle clamps the dropouts
    step.bcs.add_fixed(PREFIX + load_case + "_Axle", nodes("AXLE"))
    if load_case == "PEDAL":
        # The fork holds the head tube; 600 N sideways on the bottom bracket, as the pedal stroke bends it, shared
        # over the nodes of its shell
        step.bcs.add_displacement_rotation(PREFIX + "PEDAL_Fork", nodes("HEAD"), u2=0, u3=0)
        step.loads.add_concentrated_force(PREFIX + "PEDAL_Force", nodes("BB"), f2=600.0 / counts["BB"])
    else:
        # The bottom bracket is held vertically; 500 N sideways on the head tube
        step.bcs.add_displacement_rotation(PREFIX + "HEAD_BB", nodes("BB"), u3=0)
        step.loads.add_concentrated_force(PREFIX + "HEAD_Force", nodes("HEAD"), f2=500.0 / counts["HEAD"])
    step.field_outputs.add_nodal(PREFIX + load_case + "_NF", ["U"])
    step.field_outputs.add_element(PREFIX + load_case + "_EF", ["S"])
    return step


# Solving and evaluation
def solve(load_case, iteration):
    for step_name in list(pmx.model.steps.get_names()):
        pmx.model.steps.deactivate([step_name]) if step_name != PREFIX + load_case else pmx.model.steps.activate(
            [step_name])
    job = PREFIX + load_case
    if not pmx.jobs.contains(job):
        pmx.jobs.add(job)
    status = pmx.jobs[job].run(timeout=1800)
    if status != "OK":
        raise Exception("The load case " + load_case + " did not solve: " + status + "\n" +
                        pmx.jobs[job].get_output()[-3000:])
    pmx.jobs.open_results(job)
    # The layers, and so the result nodes, change with the layups
    _group_nodes.clear()
    return pmx.results.current


def mean_displacement(result, faces, tube, component):
    """The mean displacement of the nodes of a tube group, which is how far the load point moves."""
    step_id = list(result.steps.get_ids())[-1]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    field = result.steps[step_id].increments[increment_id].fields["DISP"].components[component]
    # The result nodes of the layers the composite section of the tube expands into
    node_ids = node_ids_of_groups(result, faces)[tube]
    return sum(field.get_value(n) for n in node_ids) / len(node_ids)


def group_reserves(result, faces):
    """The smallest Tsai-Wu reserve factor in every tube group, with the failure mode the maximum stress criterion
    gives at that node (Tsai-Wu is an interactive criterion and names no mode)."""
    outputs = result.field_outputs
    existing = list(outputs.get_names())
    for name, criterion in ((PREFIX + "TsaiWu", "TsaiWu"), (PREFIX + "MaxStress", "MaxStress")):
        if name not in existing:
            outputs.add_composite_failure(name, "STRESS", criterion)
    step_id = list(result.steps.get_ids())[-1]
    increment_id = list(result.steps[step_id].increments.get_ids())[-1]
    fields = result.steps[step_id].increments[increment_id].fields
    reserve = fields[PREFIX + "TsaiWu"].components["RESERVE_FACTOR"]
    index = fields[PREFIX + "TsaiWu"].components["FAILURE_INDEX"]
    mode = fields[PREFIX + "MaxStress"].components["MODE"]
    reserves = {}
    for group, ids in node_ids_of_groups(result, faces).items():
        worst = None
        for node in ids:
            value = reserve.get_value(node)
            if value == value and (worst is None or value < worst[0]):
                worst = (value, index.get_value(node), mode.get_value(node))
        reserves[group] = worst
    return reserves


_group_nodes = {}


def node_ids_of_groups(result, faces):
    """The result nodes of every tube group, through the layer parts the composite sections expand into."""
    if _group_nodes:
        return _group_nodes
    elements = result.mesh.elements.get_data()
    for group in GROUPS:
        ids = set()
        for set_name in result.mesh.element_sets.get_names():
            if set_name.upper().startswith((PREFIX + "Laminate_" + group + "_LAYER").upper()):
                for element_id in result.mesh.element_sets[set_name].get_element_ids():
                    ids.update(elements.get_node_ids(element_id))
        _group_nodes[group] = sorted(ids)
    return _group_nodes


def mode_name(mode):
    return {1: "fiber tension", 2: "fiber compression", 3: "matrix tension", 4: "matrix compression",
            5: "shear"}.get(int(mode) if mode == mode else 0, "-")


def main():
    part = import_frame()
    faces, area_of = tube_faces(part)
    log("tubes: " + ", ".join("%s %s" % (t, ids) for t, ids in faces.items()))
    add_materials()
    add_orientations(part, faces)
    counts = add_node_sets(part, faces)
    layups = {group: list(counts) for group, (_, counts) in GROUPS.items()}
    set_sections(part, faces, layups, True)
    for load_case in ("PEDAL", "HEAD"):
        add_step(load_case, counts)
    history = []
    for iteration in range(1, ITERATIONS + 1):
        result = solve("PEDAL", iteration)
        bb = mean_displacement(result, faces, "BB", "U2")
        reserves_pedal = group_reserves(result, faces)
        result = solve("HEAD", iteration)
        head = mean_displacement(result, faces, "HEAD", "U2")
        reserves_head = group_reserves(result, faces)
        bb_stiffness = 600 / bb
        head_stiffness = 500 / head
        mass = frame_mass(faces, area_of, layups)
        log("")
        log("iteration %d: mass %.0f g, bottom bracket %.0f N/mm (target %.0f), head tube %.0f N/mm (target %.0f)"
            % (iteration, mass, bb_stiffness, TARGET_BB_STIFFNESS, head_stiffness, TARGET_HEAD_STIFFNESS))
        log("  %-12s %-14s %-24s %s" % ("group", "plies 0/45/90", "reserve PEDAL", "reserve HEAD"))
        changed = False
        for group in GROUPS:
            r_pedal, r_head = reserves_pedal[group], reserves_head[group]
            worst = min((r for r in (r_pedal, r_head) if r is not None), key=lambda r: r[0])
            log("  %-12s %-14s %-24s %s" % (group, "/".join(str(n) for n in layups[group]),
                                           "%.2f %s" % (r_pedal[0], mode_name(r_pedal[2])) if r_pedal else "-",
                                           "%.2f %s" % (r_head[0], mode_name(r_head[2])) if r_head else "-"))
            # A reserve factor below the safety factor means failure with it
            if worst[0] < SAFETY:
                if worst[2] in (1, 2):
                    layups[group][0] += 1
                else:
                    layups[group][1] += 1
                changed = True
            elif worst[0] > 4 * SAFETY and layups[group][0] > MIN_PLIES["zero"]:
                layups[group][0] -= 1
                changed = True
            elif worst[0] > 4 * SAFETY and layups[group][1] > MIN_PLIES["pairs"]:
                layups[group][1] -= 1
                changed = True
        if bb_stiffness < TARGET_BB_STIFFNESS:
            layups["DOWN"][0] += 1
            layups["CHAIN_STAYS"][0] += 1
            changed = True
        if head_stiffness < TARGET_HEAD_STIFFNESS:
            layups["DOWN"][1] += 1
            layups["HEAD"][1] += 1
            changed = True
        history.append((iteration, mass, bb_stiffness, head_stiffness))
        if not changed:
            log("  every group holds with the safety factor %.1f and the stiffness targets are met" % SAFETY)
            break
        set_sections(part, faces, layups, False)
    # The model with the last layups and both load cases, to be opened and looked at in PrePoMax
    pmx.save(os.path.join(HERE, "bike_frame_study.pmx"))
    log("")
    log("final layups (plies 0/+-45/90, each mirrored about the mid-surface):")
    for group in GROUPS:
        log("  %-12s %s" % (group, "/".join(str(n) for n in layups[group])))


main()
