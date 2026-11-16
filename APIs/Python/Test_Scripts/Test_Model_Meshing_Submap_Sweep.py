#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the submapped source surfaces of the sweep: a source of one boundary loop whose vertices are all ends, sides
#or corners by its interior angles is meshed by SubmapMapping and swept into hexahedra when transfinite_submapped is
#on, the counts of its edges settled with the walls in the interval assignment of the sweep.
#
#  - submap_sweep.brep: solids extruded by 40 mm from L, U, T, plus and staircase profiles: hexahedra only, as many as
#    the cells of the lattice outline of the source times the layers, four quads around every node inside the source,
#    and the target carrying the grid of the source.
#  - A number of elements set on a source edge of the L is kept and carried through the sums of its opposite sides, and
#    a number set on an edge along the sweep gives the number of layers.
#  - Numbers set on the U that make its notch as deep as its sides: the source is meshed free, the sweep succeeds and
#    the output says why. An L with legs of 2 mm, whose corner quad folds at the element size 5, falls back in the
#    mesher and is swept from the free source.
#  - Second order hexahedra; full-quad recombination, simple and Blossom, and the quasi-structured quad algorithm:
#    structured, or the source meshed free with a message, the sweep succeeding every time.
#  - An L profile swept a quarter turn about an axis 30 mm from it, two L cells of 20 mm stacked into a compound,
#    which the sweep meshes one of and mirrors onto the other, and two of 15 and 25 mm, swept as two blocks:
#    hexahedra only, the source of the first block submapped.
#  - The QuadWild source mesher over the L and a five-sided house profile, swept by one item: QuadWild leaves the L
#    to gmsh, which keeps its submapped grid, and meshes the house.
#  - transfinite_submapped=False meshes the sources free, and the free sources are turned into quads only, so the
#    blocks are hexahedra only, of the element and node counts recorded in BEFORE (the U kept two of its triangles,
#    with an all-quad message and two columns of wedges, (792, 1152), until the cleanup paired triangles on the
#    boundary on a second try). Before free sources were turned into quads only, these counts were L (872, 1071),
#    U (992, 1215), T (752, 927), plus (976, 1188), staircase (1112, 1332), thin L (216, 500) and bent L (1962, 2242),
#    with wedges but in the thin L.
#  - get_settings reads transfinite_submapped back on the sweep item, and replace_sweep_mesh keeps it.
#  The model was made by gmsh's OCC kernel through GmshCommon: every profile a plane surface of lines in z = 0 extruded
#  by 40 mm, the bent L a plane surface in y = 0 revolved a quarter turn about the z axis, the stacks two L extrusions of
#  20 and 20 mm and of 15 and 25 mm fragmented.
#  With the environment variable PMX_SUBMAP_PLOT_DIR set, the source grids and an outside view of the meshes are
#  written into that folder as SVG drawings.
#
#  PrePoMax.com -f submap_sweep.brep -u MM_TON_S_C --script Test_Model_Meshing_Submap_Sweep.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
PLOT_DIR = os.environ.get("PMX_SUBMAP_PLOT_DIR")
MAX_SIZE = 5.0
HEXAHEDRON = (8, 20)
# Element and node counts with transfinite_submapped=False, first order elements and the element size 5, the free
# sources turned into quads only
# The cells of the stacks by stack
CELLS = {}
BEFORE = {"L": (720, 1017), "U": (704, 1062), "T": (624, 900), "plus": (800, 1125), "staircase": (968, 1314),
          "thin L": (216, 500), "bent L": (1638, 2166)}


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_x():
    """The geometry parts by the profile they were made from, told apart by where they lie along x."""
    names = ["L", "U", "T", "plus", "staircase", "thin L", "bent L", "stack", "house", "uneven stack"]
    parts = {}
    # A compound has no vertices of its own and is listed after its cells
    last_name = None
    for part in pmx.model.geometry.parts:
        if part.part_type == "Compound":
            parts[last_name] = part
            continue
        xs = [c[0] for c in part.get_vertex_data().coordinates]
        last_name = names[int(round(min(xs) / 150.0))]
        if "stack" in last_name: CELLS.setdefault(last_name, []).append(part.name)
        else: parts[last_name] = part
    check(sorted(parts) == sorted(names), "the parts of submap_sweep.brep were not all found: " + str(sorted(parts)))
    return parts


def submapping_messages(part_name):
    """The submapping lines the last meshing of the part wrote to the output window, read off the main form, as
    Test_Rpc_Live_Session reaches it: the API returns no mesher output."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    return [line[line.index("Submapping:"):] for line in lines[starts[-1]:] if "Submapping:" in line]


def all_quad_messages(part_name):
    """The all-quad lines the last meshing of the part wrote to the output window."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    return [line[line.index("All-quad meshing:"):] for line in lines[starts[-1]:] if "All-quad meshing:" in line]


def edges_at(part, length, z=None):
    """The edges of the part of the given length, by edge id and geometry id, those with the centroid at z if given."""
    data = part.get_edge_data()
    result = []
    for edge_id, geometry_id, edge_length, centroid in zip([int(i) for i in data.ids], [int(i) for i in data.geometry_ids],
                                                           [float(l) for l in data.lengths],
                                                           [list(c) for c in data.centroids]):
        if abs(edge_length - length) < 1E-3 and (z is None or abs(centroid[2] - z) < 1E-6):
            result.append((edge_id, geometry_id))
    return result


def source_face_id(part):
    """The face of the part in z = 0."""
    data = part.get_face_data()
    for face_id, centroid, normal in zip([int(i) for i in data.ids], [list(c) for c in data.centroids],
                                         [list(n) for n in data.normals]):
        if abs(centroid[2]) < 1E-6 and abs(abs(normal[2]) - 1) < 1E-6:
            return face_id
    raise Exception("TEST FAILED: " + part.name + " has no face in z = 0")


def edge_count(geometry_id):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().count - 1


def sweep(parts, second_order=False, selection=None, pins=(), **arguments):
    """Sweeps the parts by one item with the given arguments; pins are (part, edge id, number of elements)."""
    items = pmx.model.mesh.setup_items
    ids = [part.id for part in parts]
    items.add_meshing_parameters("PyTest_Submap_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts', ids),
                                 relative_size=False, max_size=MAX_SIZE, min_size=MAX_SIZE / 10,
                                 second_order=second_order)
    names = ["PyTest_Submap_Size"]
    for part, edge_id, count in pins:
        name = "PyTest_Submap_Pin_" + str(part.id) + "_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    if selection is None: selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', ids)
    items.add_sweep_mesh("PyTest_Submap_Sweep", selection, **arguments)
    names.append("PyTest_Submap_Sweep")
    try:
        report = pmx.model.mesh.generate([part.name for part in parts])
    finally:
        items.remove(names)
    for part in parts:
        check(report[part.name] == "", "sweeping " + part.name + " reported: " + report[part.name])


class SweptMesh(object):
    """The elements of the mesh parts of a geometry part by their corner nodes, with the node coordinates."""

    def __init__(self, part_names):
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        self.xyz = {}
        self.elements = []
        self.node_counts = []
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name in part_names]
        check(len(meshed) == len(part_names), "not every part of " + str(part_names) + " was meshed")
        for mesh_part in meshed:
            self.xyz.update(zip([int(i) for i in mesh_part.get_node_ids()],
                                [list(c) for c in mesh_part.get_node_coordinates()]))
            for element_id in mesh_part.get_element_ids():
                i = index[int(element_id)]
                corners = {8: 8, 20: 8, 6: 6, 15: 6, 4: 4, 10: 4, 5: 5, 13: 5}[node_counts[i]]
                self.elements.append(connectivity[offsets[i]:offsets[i] + corners])
                self.node_counts.append(node_counts[i])

    def kinds(self):
        names = {8: "hexahedron", 20: "hexahedron", 6: "wedge", 15: "wedge", 4: "tetrahedron", 10: "tetrahedron",
                 5: "pyramid", 13: "pyramid"}
        result = {}
        for count in self.node_counts:
            result[names[count]] = result.get(names[count], 0) + 1
        return result

    def cap(self, on_cap):
        """The quads of the hexahedra faces whose four nodes all lie on the cap."""
        faces = [(0, 1, 2, 3), (4, 5, 6, 7)]
        quads = []
        for element in self.elements:
            if len(element) != 8: continue
            for face in faces:
                nodes = [element[k] for k in face]
                if all(on_cap(self.xyz[node]) for node in nodes): quads.append(nodes)
        return quads


def cap_grid(quads, xyz, project):
    """The quads of a cap with their projection: the number of quads, the lattice cells of the outline its boundary
    draws, the boundary steps along +u, +v, -u, -v, the nodes inside without four quads and the worst scaled
    Jacobian."""
    uv = dict((node, project(xyz[node])) for quad in quads for node in quad)
    uses = {}
    valence = {}
    for quad in quads:
        for k in range(4):
            edge = tuple(sorted((quad[k], quad[(k + 1) % 4])))
            uses[edge] = uses.get(edge, 0) + 1
            valence[quad[k]] = valence.get(quad[k], 0) + 1
    neighbours = {}
    for (a, b), count in uses.items():
        if count == 1:
            neighbours.setdefault(a, []).append(b)
            neighbours.setdefault(b, []).append(a)
    irregular = [node for node, count in valence.items() if node not in neighbours and count != 4]
    start = min(neighbours)
    loop = [start]
    previous, current = None, start
    while True:
        following = [n for n in neighbours[current] if n != previous][0]
        if following == start: break
        loop.append(following)
        previous, current = current, following
    steps = [0, 0, 0, 0]
    i = j = twice_area = 0
    for k in range(len(loop)):
        a, b = uv[loop[k]], uv[loop[(k + 1) % len(loop)]]
        du, dv = b[0] - a[0], b[1] - a[1]
        direction = (0 if du > 0 else 2) if abs(du) >= abs(dv) else (1 if dv > 0 else 3)
        steps[direction] += 1
        ni, nj = i + [1, 0, -1, 0][direction], j + [0, 1, 0, -1][direction]
        twice_area += i * nj - ni * j
        i, j = ni, nj
    sines = []
    signs = 0
    for quad in quads:
        for c in range(4):
            p, a, b = uv[quad[c]], uv[quad[(c + 1) % 4]], uv[quad[(c + 3) % 4]]
            e1, e2 = (a[0] - p[0], a[1] - p[1]), (b[0] - p[0], b[1] - p[1])
            length = math.hypot(*e1) * math.hypot(*e2)
            sine = (e1[0] * e2[1] - e1[1] * e2[0]) / length if length > 0 else 0
            sines.append(sine)
            signs += 1 if sine > 0 else -1
    quality = min(s if signs > 0 else -s for s in sines)
    return len(quads), abs(twice_area) // 2, steps, irregular, quality, uv


def plot(name, title, quads, uv):
    if not PLOT_DIR: return
    us = [p[0] for p in uv.values()]
    vs = [p[1] for p in uv.values()]
    scale = 560.0 / max(max(us) - min(us), max(vs) - min(vs))
    width, height = (max(us) - min(us)) * scale + 40, (max(vs) - min(vs)) * scale + 70
    lines = ['<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d">' % (width, height),
             '<rect width="100%" height="100%" fill="white"/>',
             '<text x="20" y="24" font-family="sans-serif" font-size="15">%s</text>' % title]
    for quad in quads:
        points = " ".join("%.2f,%.2f" % (20 + (uv[n][0] - min(us)) * scale, height - 20 - (uv[n][1] - min(vs)) * scale)
                          for n in quad)
        lines.append('<polygon points="%s" fill="%s" stroke="#1d3c5a" stroke-width="0.6"/>' %
                     (points, "#dbe9f6" if len(quad) == 4 else "#f6d9c8"))
    lines.append("</svg>")
    with open(os.path.join(PLOT_DIR, name + ".svg"), "w") as file:
        file.write("\n".join(lines))


def plot_outside(name, title, mesh):
    """The faces of the elements on the outside of the mesh, in an oblique view, nearer faces drawn last."""
    if not PLOT_DIR: return
    faces = {8: [(0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)],
             6: [(0, 1, 2), (3, 4, 5), (0, 1, 4, 3), (1, 2, 5, 4), (2, 0, 3, 5)]}
    uses = {}
    for element in mesh.elements:
        for face in faces.get(len(element), []):
            nodes = tuple(element[k] for k in face)
            key = tuple(sorted(nodes))
            uses[key] = (uses.get(key, (0, nodes))[0] + 1, nodes)
    outside = [nodes for count, nodes in uses.values() if count == 1]
    view = lambda p: (p[0] - 0.5 * p[1], p[2] + 0.35 * p[1] - 0.15 * p[0])
    depth = lambda nodes: sum(mesh.xyz[n][1] - mesh.xyz[n][0] - mesh.xyz[n][2] for n in nodes) / len(nodes)
    outside.sort(key=depth, reverse=True)
    uv = dict((node, view(mesh.xyz[node])) for nodes in outside for node in nodes)
    plot(name, title, outside, uv)


# Checks ---------------------------------------------------------------------------------------------------------------
def check_profile(parts, name, project=lambda p: (p[0], p[1]), lattice=True, on_source=lambda p: abs(p[2]) < 1E-6,
                  on_target=lambda p: abs(p[2] - 40.0) < 1E-6, project_target=lambda p: (p[0], p[1]), **arguments):
    part = parts[name]
    sweep([part], **arguments)
    mesh = SweptMesh([part.name])
    kinds = mesh.kinds()
    check(list(kinds.keys()) == ["hexahedron"], "the swept " + name + " must be hexahedra only: " + str(kinds))
    source = mesh.cap(on_source)
    target = mesh.cap(on_target)
    count, area, steps, irregular, quality, uv = cap_grid(source, mesh.xyz, project)
    check(len(irregular) == 0, "the source of the " + name + " has " + str(len(irregular)) + " inner nodes without " +
          "four quads")
    check(not lattice or (count == area and steps[0] == steps[2] and steps[1] == steps[3]),
          "the source of the " + name + " has " + str(count) + " quads against " + str(area) + " lattice cells, " +
          "boundary steps " + str(steps))
    layers = len(mesh.elements) // count
    check(layers * count == len(mesh.elements), "the " + name + " has " + str(len(mesh.elements)) + " hexahedra, not a " +
          "multiple of its " + str(count) + " source quads")
    source_points = sorted((round(uv[n][0], 6), round(uv[n][1], 6)) for n in uv)
    target_points = sorted(tuple(round(c, 6) for c in project_target(mesh.xyz[n])) for quad in target for n in quad)
    target_points = sorted(set(target_points))
    check(len(target) == count and target_points == sorted(set(source_points)),
          "the target of the " + name + " does not carry the grid of its source")
    return mesh, source, steps, quality, layers, uv


def check_profiles(parts):
    for name in ["L", "U", "T", "plus", "staircase"]:
        mesh, source, steps, quality, layers, uv = check_profile(parts, name)
        messages = submapping_messages(parts[name].name)
        check(len(messages) == 0, "the source of the swept " + name + " wrote: " + str(messages))
        print(name + ": submapped source,", len(source), "quads x", layers, "layers =", len(mesh.elements),
              "hexahedra, boundary steps", steps, "worst scaled Jacobian of the source", round(quality, 3))
        plot("source_" + name, "Source of the swept " + name + ": " + str(len(source)) + " quads", source, uv)
        plot_outside("hex_" + name, "Swept " + name + ": " + str(len(mesh.elements)) + " hexahedra", mesh)


def check_pins(parts):
    part = parts["L"]
    source_edge = edges_at(part, 40.0, z=0.0)
    along = edges_at(part, 40.0)
    along = [edge for edge in along if edge not in edges_at(part, 40.0, z=0.0) and
             edge not in edges_at(part, 40.0, z=40.0)]
    check(len(source_edge) == 1 and len(along) > 0, "the L has no edge of 40 mm in its source, or none along the sweep")
    pins = [(part, source_edge[0][0], 11), (part, along[0][0], 7)]
    mesh, source, steps, quality, layers, uv = check_profile(parts, "L", pins=pins)
    check(edge_count(source_edge[0][1]) == 11 and layers == 7,
          "the pins on the L must hold: source edge " + str(edge_count(source_edge[0][1])) + " of 11, " + str(layers) +
          " layers of 7")
    print("L with 11 elements pinned on a source edge and 7 along the sweep: boundary steps", steps, "layers", layers)


def check_fallbacks(parts):
    part = parts["U"]
    pins = [(part, edge_id, 10) for length in (35.0, 50.0) for edge_id, geometry_id in edges_at(part, length, z=0.0)]
    check(len(pins) == 4, "the U has not two notch sides and two outer sides in its source: " + str(pins))
    sweep([part], pins=pins)
    mesh = SweptMesh([part.name])
    messages = submapping_messages(part.name)
    print("U with its notch pinned as deep as its sides:", mesh.kinds(), messages)
    check(len(messages) == 1 and "is meshed free" in messages[0], "the invalid U source must be meshed free with a " +
          "message that says why: " + str(messages))
    check(set(mesh.kinds()) <= {"hexahedron", "wedge"}, "the U must be swept: " + str(mesh.kinds()))
    source = mesh.cap(lambda xyz: abs(xyz[2]) < 1E-6)
    plot_outside("fallback_counts_U", "U pinned as deep as its sides: free source, " + str(mesh.kinds()), mesh)
    #
    part = parts["thin L"]
    sweep([part])
    mesh = SweptMesh([part.name])
    messages = submapping_messages(part.name)
    print("L with legs of 2 mm:", mesh.kinds(), messages)
    check(len(messages) == 1 and "is meshed free" in messages[0] and "scaled Jacobian" in messages[0] or
          len(messages) == 1 and "fold" in messages[0], "the thin L source must fall back in the mesher: " + str(messages))
    check(set(mesh.kinds()) <= {"hexahedron", "wedge"}, "the thin L must be swept: " + str(mesh.kinds()))
    plot_outside("fallback_mesher_thin_L", "L with legs of 2 mm: free source, " + str(mesh.kinds()), mesh)


def check_variants(parts):
    for name in ["L", "U"]:
        mesh, source, steps, quality, layers, uv = check_profile(parts, name, second_order=True)
        check(all(count == 20 for count in mesh.node_counts), "the second order " + name + " has elements of other " +
              "than 20 nodes")
        print(name + ": second order,", len(mesh.elements), "hexahedra of 20 nodes")
        for label, arguments in (("full-quad recombination", {"algorithm_recombine": "SimpleFullQuad"}),
                                 ("Blossom full-quad recombination", {"algorithm_recombine": "BlossomFullQuad"}),
                                 ("quasi-structured quad algorithm", {"algorithm_2d": "QuasiStructuredQuad"})):
            part = parts[name]
            sweep([part], **arguments)
            mesh = SweptMesh([part.name])
            messages = submapping_messages(part.name)
            source = mesh.cap(lambda xyz: abs(xyz[2]) < 1E-6)
            structured = list(mesh.kinds().keys()) == ["hexahedron"] and len(cap_grid(source, mesh.xyz,
                                                                                       lambda p: (p[0], p[1]))[3]) == 0
            check(structured or (len(messages) == 1 and "is meshed free" in messages[0]),
                  "the " + name + " with the " + label + " was neither structured nor meshed free with a message: " +
                  str(mesh.kinds()) + " " + str(messages))
            print(name + " with the " + label + ":", "submapped" if structured else "free", mesh.kinds(), messages)


def check_bent_and_stack(parts):
    part = parts["bent L"]
    # The source lies in y = 0, and the target, a quarter turn about the z axis through x = 900, in x = 900
    mesh, source, steps, quality, layers, uv = check_profile(parts, "bent L", project=lambda p: (p[0], p[2]),
                                                             on_source=lambda p: abs(p[1]) < 1E-6,
                                                             on_target=lambda p: abs(p[0] - 900.0) < 1E-6,
                                                             project_target=lambda p: (900.0 + p[1], p[2]))
    print("bent L: submapped source,", len(source), "quads x", layers, "layers, worst scaled Jacobian", round(quality, 3))
    plot("source_bent_L", "Source of the bent L: " + str(len(source)) + " quads", source, uv)
    plot_outside("hex_bent_L", "Bent L: " + str(len(mesh.elements)) + " hexahedra", mesh)
    #
    # Two cells of 20 mm, which the sweep meshes one of and mirrors, and two of 15 and 25 mm, swept as two blocks
    for name in ["stack", "uneven stack"]:
        sweep([parts[name]])
        mesh = SweptMesh(CELLS[name])
        kinds = mesh.kinds()
        check(list(kinds.keys()) == ["hexahedron"], "the " + name + " must be hexahedra only: " + str(kinds))
        source = mesh.cap(lambda xyz: abs(xyz[2]) < 1E-6)
        count, area, steps, irregular, quality, uv = cap_grid(source, mesh.xyz, lambda p: (p[0], p[1]))
        check(len(irregular) == 0 and count == area, "the source of the " + name + " is not the submapped grid: " +
              str(steps))
        print(name + " of two L cells:", kinds, "source", count, "quads, worst scaled Jacobian", round(quality, 3))
        label = name.replace(" ", "_")
        plot("source_" + label, "Source of the " + name + " of L cells: " + str(count) + " quads", source, uv)
        plot_outside("hex_" + label, "The " + name + " of two L cells: " + str(len(mesh.elements)) + " hexahedra",
                     mesh)


def check_quadwild(parts):
    l_part, plate = parts["L"], parts["house"]
    selection = pmx.create.selection_by_ids('geometry', 'geometry_surfaces', 'surfaces',
                                            {l_part.id: [source_face_id(l_part)], plate.id: [source_face_id(plate)]})
    sweep([l_part, plate], selection=selection, algorithm_2d="QuadWild")
    mesh = SweptMesh([l_part.name])
    source = mesh.cap(lambda xyz: abs(xyz[2]) < 1E-6)
    count, area, steps, irregular, quality, uv = cap_grid(source, mesh.xyz, lambda p: (p[0], p[1]))
    check(list(mesh.kinds().keys()) == ["hexahedron"] and len(irregular) == 0 and count == area,
          "the L swept with the QuadWild source mesher must keep its submapped grid: " + str(mesh.kinds()) + " " +
          str(steps))
    plate_mesh = SweptMesh([plate.name])
    print("QuadWild sweep: L", mesh.kinds(), "submapped source of", count, "quads; house", plate_mesh.kinds())
    check(len(plate_mesh.elements) > 0, "the house was not swept")


def check_off(parts):
    counts = {}
    for name in ["L", "U", "T", "plus", "staircase", "thin L", "bent L"]:
        part = parts[name]
        sweep([part], transfinite_submapped=False)
        mesh = SweptMesh([part.name])
        # A source meshed free is turned into quads only, so the blocks are hexahedra only, unless the conversion
        # gives up on the source and says so
        kept = all_quad_messages(part.name)
        counts[name] = (len(mesh.elements), len(mesh.xyz), mesh.kinds(), kept)
        check(list(mesh.kinds().keys()) == ["hexahedron"] or len(kept) == 1,
              "the " + name + " swept with transfinite_submapped=False is not hexahedra only: " + str(mesh.kinds()))
        if name in BEFORE:
            check((len(mesh.elements), len(mesh.xyz)) == BEFORE[name],
                  "the " + name + " swept with transfinite_submapped=False has " + str(len(mesh.elements)) +
                  " elements and " + str(len(mesh.xyz)) + " nodes, against " + str(BEFORE[name]) + " before")
    print("transfinite_submapped=False (elements, nodes):", dict((k, v[:2]) for k, v in counts.items()),
          "kinds and all-quad messages:", dict((k, v[2:]) for k, v in counts.items()))
    check(len(BEFORE) == 0 or all(name in BEFORE for name in counts), "BEFORE lacks a profile")


def check_settings(parts):
    items = pmx.model.mesh.setup_items
    part = parts["L"]
    items.add_sweep_mesh("PyTest_Submap_Settings", pmx.create.selection_by_ids('geometry', 'geometry', 'parts',
                                                                               [part.id]))
    values = items["PyTest_Submap_Settings"].get_settings()
    check(values["transfinite_submapped"] is True, "a new sweep item must be submapped: " + str(values))
    items.replace_sweep_mesh("PyTest_Submap_Settings", transfinite_submapped=False)
    items.replace_sweep_mesh("PyTest_Submap_Settings", algorithm_recombine="Blossom")
    values = items["PyTest_Submap_Settings"].get_settings()
    check(values["transfinite_submapped"] is False, "replace_sweep_mesh must keep transfinite_submapped: " + str(values))
    items.remove(["PyTest_Submap_Settings"])


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x()
    check_settings(parts)
    check_off(parts)
    check_profiles(parts)
    check_pins(parts)
    check_fallbacks(parts)
    check_variants(parts)
    check_bent_and_stack(parts)
    check_quadwild(parts)
    print("TEST PASSED")


main()
