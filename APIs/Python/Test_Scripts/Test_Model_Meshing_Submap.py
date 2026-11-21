#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the submapped faces of the gmsh shell item: a face of one boundary loop whose vertices are all ends, sides or
#corners by the interior angle of the face there, its ends outnumbering its corners by four, is meshed by
#SubmapMapping as a structured grid when transfinite_submapped is on.
#
#  - submap_plates.brep: planar plates apart from each other, one part each - an L, a T, a U, a plus, a staircase of
#    three steps, a rectangle and a rectangle with a round hole. With transfinite_submapped=True every shape but the
#    hole plate is quadrilaterals only, every node inside it has four quads, its boundary carries as many element edges
#    along +x as along -x and along +y as along -y, and its quads are as many as the cells of the lattice outline its
#    edge counts draw. The rectangle is the transfinite grid it was, and the hole plate the free mesh it was.
#  - Off: transfinite_submapped=False meshes every plate as the free mesh it gets with no structured face at all.
#  - A number of elements set on one edge of the L is kept, and the opposite sides take it up through their sums.
#  - Numbers of elements set on the U that make its notch as deep as its sides leave no valid outline: the U is meshed
#    free, the meshing succeeds, and the mesher output says why. A notch side pinned deeper than the outer sides ask
#    for raises the outer sides instead: the balance keeps the sides that face each other a cell apart.
#  - submap_thin.brep: faces thinner than the element size, one part each, the balance keeping them apart where the
#    counts the mesh size asks for would close them up: a U with a web of 2 mm, also with a full-quad recombination, a
#    face with a step of 1 mm, and an L with legs of 2 mm. At size 5, spacing gives this L 23 right-angled quads. The U
#    beside a flange pinned to 30 elements on its bottom and 2 on its notch bottom keeps 254 quads. At 50 and 2
#    its arm tops share the 48 elements left and it keeps 532 quads. With one arm top pinned to 3 as well, the other
#    takes 45: the U still closes a valid lattice outline, but the mesher leaves it free and reports a folded grid.
#    The flange stays structured with every set of pins. An L whose bottom side winds back over itself is rejected in
#    detection because its provisional grid folds. The L with a low bump in its bottom side is submapped.
#  - Second order elements mesh the L and the U structured; a full-quad recombination and the quasi-structured quad
#    algorithm mesh them structured or free, but never fail.
#  - submap_folded.brep: an L-shaped plate with a flange of 20 mm up along two of its outer edges, the three faces
#    sharing their edges. A number of elements set on an edge of the L reaches the flange across from the edge of the
#    L it is balanced with.
#  - submap_curved.brep: an L cut from the wall of a cylinder of radius 50. Its nodes lie on the cylinder and it is
#    quadrilaterals only with four quads around every inner node.
#  - get_settings reads transfinite_submapped back, True by default, and replace_shell_gmsh keeps it when not given;
#    the angle is refused only while transfinite_submapped is off too.
#  The models were made by gmsh's OCC kernel through GmshCommon: the plates and the folded sheet from points, lines and
#  plane surfaces (the folded sheet's three faces built on shared lines and fragmented), the curved L by extruding a
#  quarter circle of radius 50 by 60 mm, fragmenting it with a vertical line at 45 degrees from z = 30 to 60 and an
#  arc at z = 30 from 45 to 90 degrees, and removing the corner face.
#  With the environment variable PMX_SUBMAP_PLOT_DIR set, the meshes are written into that folder as SVG drawings.
#
#  PrePoMax.com -f submap_plates.brep -u MM_TON_S_C --script Test_Model_Meshing_Submap.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys
import time
import clr
clr.AddReference('CaeMesh')
from CaeMesh import SubmapFaceDetector, TransfiniteThreeSidedEnum
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
PLOT_DIR = os.environ.get("PMX_SUBMAP_PLOT_DIR")
MAX_SIZE = 5.0
RADIUS = 50.0


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def raised_message(action):
    try:
        action()
    except Exception as ex:
        return str(ex)
    return None


# Model access ---------------------------------------------------------------------------------------------------------
def geometry_part(name):
    return [part for part in pmx.model.geometry.parts if part.name == name][0]


def parts_selection(part):
    return pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])


def geometry_selection(part):
    return pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])


def edge_selection(part, edge_id):
    return pmx.create.selection_by_ids('geometry', 'geometry', 'edges', {part.id: [edge_id]})


def edges_by_length(part):
    data = part.get_edge_data()
    result = {}
    for edge_id, geometry_id, length in zip([int(i) for i in data.ids], [int(i) for i in data.geometry_ids],
                                            [float(l) for l in data.lengths]):
        result.setdefault(round(length, 3), []).append((edge_id, geometry_id))
    return result


def edge_count(geometry_id):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().count - 1


def submapping_messages(part_name):
    """The submapping lines the last meshing of the part wrote to the output window. The API returns no mesher
    output, so the lines are read off the main form, as Test_Rpc_Live_Session reaches it."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    return [line[line.index("Submapping:"):] for line in lines[starts[-1]:] if "Submapping:" in line]


def detected_faces(part):
    """The faces of the part SubmapFaceDetector finds, and the faces the mesh type colors show as structured, by the
    geometry of the model, which the API does not reach, read off the controller of the main form."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    controller = form.GetType().GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form)
    geometry = controller.Model.Geometry
    base_part = geometry.Parts[part.name]
    detected = [face.FaceId for face in SubmapFaceDetector.Detect(geometry, base_part, 135.0)]
    flags = list(geometry.GetTransfiniteShellFaces(base_part, TransfiniteThreeSidedEnum.Mapped, True, 135.0, True))
    return detected, flags


def mesh(part, recombine="Simple", second_order=False, algorithm_2d=None, max_size=MAX_SIZE, **shell):
    """Meshes the part by a shell item with the given arguments and returns the time it took."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Submap_Size", parts_selection(part), relative_size=False, max_size=max_size,
                                 min_size=max_size / 10, second_order=second_order)
    arguments = dict(shell)
    if algorithm_2d is not None:
        arguments["algorithm_2d"] = algorithm_2d
    items.add_shell_gmsh("PyTest_Submap", geometry_selection(part), algorithm_recombine=recombine, **arguments)
    start = time.time()
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(["PyTest_Submap", "PyTest_Submap_Size"])
    elapsed = time.time() - start
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    return elapsed


# Mesh access ----------------------------------------------------------------------------------------------------------
class FaceMesh(object):
    """The elements of a meshed part, or of those of its elements whose nodes all pass keep, by their corner nodes,
    with the coordinates of the nodes and their projection into the plane of the face."""

    def __init__(self, part_name, project, keep=None):
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name]
        check(len(meshed) == 1, "the part " + part_name + " was not meshed")
        self.xyz = dict(zip([int(i) for i in meshed[0].get_node_ids()],
                            [list(c) for c in meshed[0].get_node_coordinates()]))
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        self.elements = []
        self.node_counts = []
        for element_id in meshed[0].get_element_ids():
            i = index[int(element_id)]
            corners = 4 if node_counts[i] in (4, 8) else 3
            nodes = connectivity[offsets[i]:offsets[i] + corners]
            if keep is not None and not all(keep(self.xyz[node]) for node in nodes):
                continue
            self.elements.append(nodes)
            self.node_counts.append(node_counts[i])
        self.uv = dict((node, project(self.xyz[node])) for element in self.elements for node in element)

    def kinds(self):
        result = {}
        for element in self.elements:
            kind = "quadrilateral" if len(element) == 4 else "triangle"
            result[kind] = result.get(kind, 0) + 1
        return result

    def edge_uses(self):
        uses = {}
        for element in self.elements:
            for k in range(len(element)):
                edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
                uses[edge] = uses.get(edge, 0) + 1
        return uses

    def irregular_inner_nodes(self):
        boundary = set(node for edge, uses in self.edge_uses().items() if uses == 1 for node in edge)
        valence = {}
        for element in self.elements:
            for node in element:
                valence[node] = valence.get(node, 0) + 1
        return [node for node, count in valence.items() if node not in boundary and count != 4]

    def boundary_loop(self):
        """The nodes of the one boundary loop in order."""
        neighbours = {}
        for (a, b), uses in self.edge_uses().items():
            if uses == 1:
                neighbours.setdefault(a, []).append(b)
                neighbours.setdefault(b, []).append(a)
        check(all(len(n) == 2 for n in neighbours.values()), "the boundary of the face is not one simple loop")
        start = min(neighbours)
        loop = [start]
        previous = None
        current = start
        while True:
            following = [n for n in neighbours[current] if n != previous][0]
            if following == start:
                break
            loop.append(following)
            previous, current = current, following
        check(len(loop) == len(neighbours), "the boundary of the face is more than one loop")
        return loop

    def lattice(self):
        """The element edges of the boundary along +u, -u, +v and -v, and the area of the outline they draw as unit
        steps in those directions."""
        loop = self.boundary_loop()
        steps = [0, 0, 0, 0]
        i = j = 0
        twice_area = 0
        for k in range(len(loop)):
            a = self.uv[loop[k]]
            b = self.uv[loop[(k + 1) % len(loop)]]
            du = b[0] - a[0]
            dv = b[1] - a[1]
            if abs(du) >= abs(dv):
                direction = 0 if du > 0 else 2
            else:
                direction = 1 if dv > 0 else 3
            steps[direction] += 1
            ni = i + [1, 0, -1, 0][direction]
            nj = j + [0, 1, 0, -1][direction]
            twice_area += i * nj - ni * j
            i, j = ni, nj
        return steps, abs(twice_area) // 2

    def min_scaled_jacobian(self):
        """The smallest sine of a quad corner in the projection, the quads turned the way most of them turn."""
        sines = []
        signs = 0
        for element in self.elements:
            if len(element) != 4:
                continue
            for c in range(4):
                p = self.uv[element[c]]
                a = self.uv[element[(c + 1) % 4]]
                b = self.uv[element[(c + 3) % 4]]
                e1 = (a[0] - p[0], a[1] - p[1])
                e2 = (b[0] - p[0], b[1] - p[1])
                length = math.hypot(*e1) * math.hypot(*e2)
                sine = (e1[0] * e2[1] - e1[1] * e2[0]) / length if length > 0 else 0
                sines.append(sine)
                signs += 1 if sine > 0 else -1
        if not sines:
            return None
        return min(s if signs > 0 else -s for s in sines)

    def plot(self, name, title):
        if not PLOT_DIR:
            return
        us = [p[0] for p in self.uv.values()]
        vs = [p[1] for p in self.uv.values()]
        size = max(max(us) - min(us), max(vs) - min(vs))
        scale = 560.0 / size
        width = (max(us) - min(us)) * scale + 40
        height = (max(vs) - min(vs)) * scale + 70
        to_x = lambda u: 20 + (u - min(us)) * scale
        to_y = lambda v: height - 20 - (v - min(vs)) * scale
        lines = ['<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d">' % (width, height),
                 '<rect width="100%" height="100%" fill="white"/>',
                 '<text x="20" y="24" font-family="sans-serif" font-size="15">%s</text>' % title]
        irregular = set(self.irregular_inner_nodes())
        for element in self.elements:
            points = " ".join("%.2f,%.2f" % (to_x(self.uv[n][0]), to_y(self.uv[n][1])) for n in element)
            fill = "#dbe9f6" if len(element) == 4 else "#f6d9c8"
            lines.append('<polygon points="%s" fill="%s" stroke="#1d3c5a" stroke-width="0.8"/>' % (points, fill))
        for node in irregular:
            lines.append('<circle cx="%.2f" cy="%.2f" r="3" fill="#c0392b"/>' % (to_x(self.uv[node][0]),
                                                                                  to_y(self.uv[node][1])))
        lines.append("</svg>")
        with open(os.path.join(PLOT_DIR, name + ".svg"), "w") as file:
            file.write("\n".join(lines))


def plane_xy(xyz):
    return (xyz[0], xyz[1])


def unrolled(xyz):
    return (RADIUS * math.atan2(xyz[1], xyz[0]), xyz[2])


def oblique(xyz):
    """Seen from above the base, the flanges folded out below and left of it."""
    return (xyz[0] - 0.6 * xyz[2], xyz[1] - 0.6 * xyz[2])


def check_submapped(face, name, lattice=True):
    """Quadrilaterals only, four quads around every inner node and, for a face whose edges run along the axes of its
    projection, opposite boundary sums equal and as many quads as the lattice outline encloses cells."""
    kinds = face.kinds()
    check(list(kinds.keys()) == ["quadrilateral"], name + " must be quadrilaterals only: " + str(kinds))
    irregular = face.irregular_inner_nodes()
    check(len(irregular) == 0, name + " has " + str(len(irregular)) + " inner nodes without four quads")
    if not lattice:
        quality = face.min_scaled_jacobian()
        check(quality is not None and quality >= 0.2, name + " has a quad corner of the scaled Jacobian " + str(quality))
        return None, quality
    steps, area = face.lattice()
    check(steps[0] == steps[2] and steps[1] == steps[3],
          name + " has unequal boundary sums along +u, +v, -u, -v: " + str(steps))
    check(len(face.elements) == area, name + " has " + str(len(face.elements)) + " quads, but its lattice outline " +
          str(steps) + " encloses " + str(area) + " cells")
    quality = face.min_scaled_jacobian()
    check(quality is not None and quality >= 0.2, name + " has a quad corner of the scaled Jacobian " + str(quality))
    return steps, quality


# Checks ---------------------------------------------------------------------------------------------------------------
def plate_names():
    """The plates by shape: the number of boundary edges and, among the rectangles and the shapes of eight edges, the
    edge lengths."""
    names = {}
    for part in pmx.model.geometry.parts:
        lengths = sorted(round(float(l), 3) for l in part.get_edge_data().lengths)
        count = len(lengths)
        if count == 6:
            names["L"] = part.name
        elif count == 12:
            names["plus"] = part.name
        elif count == 4:
            names["rectangle"] = part.name
        elif count == 8 and lengths == [15.0, 15.0, 30.0, 35.0, 35.0, 50.0, 50.0, 60.0]:
            names["U"] = part.name
        elif count == 8 and lengths == [15.0, 15.0, 20.0, 20.0, 20.0, 30.0, 30.0, 60.0]:
            names["T"] = part.name
        elif count == 8 and lengths == [20.0, 20.0, 20.0, 20.0, 20.0, 20.0, 60.0, 60.0]:
            names["staircase"] = part.name
        else:
            names["hole"] = part.name
    check(sorted(names) == sorted(["L", "T", "U", "plus", "staircase", "rectangle", "hole"]),
          "the plates were not all found: " + str(names))
    return names


def check_plates(names):
    times = {}
    for shape in ["L", "T", "U", "plus", "staircase", "rectangle", "hole"]:
        part = geometry_part(names[shape])
        free_time = mesh(part, transfinite_three_sided="No", transfinite_four_sided=False,
                         transfinite_submapped=False)
        free = FaceMesh(part.name, plane_xy)
        mesh(part, transfinite_submapped=False)
        off = FaceMesh(part.name, plane_xy)
        on_time = mesh(part, transfinite_submapped=True)
        on = FaceMesh(part.name, plane_xy)
        times[shape] = (free_time, on_time)
        if shape == "hole":
            check(on.kinds() == off.kinds() and len(on.uv) == len(off.uv) and on.kinds() == free.kinds(),
                  "the hole plate must keep its free mesh: " + str(on.kinds()) + " against " + str(off.kinds()))
            check("triangle" in on.kinds(), "the hole plate was not meshed free: " + str(on.kinds()))
            print("hole plate: free", on.kinds())
            on.plot("plate_hole", "Plate with a hole: free")
            continue
        if shape != "rectangle":
            check(off.kinds() == free.kinds() and len(off.uv) == len(free.uv),
                  "the " + shape + " meshed with transfinite_submapped=False is not the free mesh: " +
                  str(off.kinds()) + " against " + str(free.kinds()))
            check("triangle" in off.kinds() or len(off.irregular_inner_nodes()) > 0,
                  "the " + shape + " meshed free looks structured: " + str(off.kinds()))
        steps, quality = check_submapped(on, "the " + shape)
        check(len(submapping_messages(part.name)) == 0, "the submapped " + shape + " wrote: " +
              str(submapping_messages(part.name)))
        print(shape + ": submapped", on.kinds(), "boundary steps", steps, "worst scaled Jacobian", round(quality, 3),
              "time", round(on_time, 2), "s against", round(free_time, 2), "s free")
        on.plot("plate_" + shape, "Submapped " + shape + " (" + str(len(on.elements)) + " quads, worst scaled " +
                "Jacobian " + str(round(quality, 3)) + ")")
    return times


def check_pinned_l(names):
    part = geometry_part(names["L"])
    edge_id, geometry_id = edges_by_length(part)[40.0][0]
    items = pmx.model.mesh.setup_items
    items.add_local_mesh_size("PyTest_Submap_Pin", edge_selection(part, edge_id), number_of_elements=11)
    try:
        mesh(part)
    finally:
        items.remove(["PyTest_Submap_Pin"])
    face = FaceMesh(part.name, plane_xy)
    steps, quality = check_submapped(face, "the L with a pinned edge")
    check(edge_count(geometry_id) == 11, "the pinned edge of the L carries " + str(edge_count(geometry_id)) +
          " elements instead of 11")
    print("L with 11 elements pinned on its edge of 40 mm: boundary steps", steps, "worst scaled Jacobian",
          round(quality, 3))
    face.plot("plate_L_pinned", "L with 11 elements pinned on its edge of 40 mm (worst scaled Jacobian " +
              str(round(quality, 3)) + ")")


def check_invalid_u(names):
    part = geometry_part(names["U"])
    lengths = edges_by_length(part)
    items = pmx.model.mesh.setup_items
    pinned = []
    for length in (35.0, 50.0):
        for edge_id, geometry_id in lengths[length]:
            name = "PyTest_Submap_Pin_" + str(edge_id)
            items.add_local_mesh_size(name, edge_selection(part, edge_id), number_of_elements=10)
            pinned.append(name)
    try:
        mesh(part)
    finally:
        items.remove(pinned)
    face = FaceMesh(part.name, plane_xy)
    messages = submapping_messages(part.name)
    print("U with its notch as deep as its sides:", face.kinds(), messages)
    check(len(messages) == 1 and "is meshed free" in messages[0] and "leave no cell between its sides" in messages[0],
          "the invalid U must be meshed free with a message that says why: " + str(messages))
    check("triangle" in face.kinds() or len(face.irregular_inner_nodes()) > 0,
          "the invalid U was not meshed free: " + str(face.kinds()))
    face.plot("plate_U_fallback", "U with notch as deep as its sides: meshed free")


def check_variants(names):
    for shape in ["L", "U"]:
        part = geometry_part(names[shape])
        mesh(part, second_order=True)
        face = FaceMesh(part.name, plane_xy)
        check(all(count == 8 for count in face.node_counts),
              "the second order " + shape + " has elements of other than 8 nodes: " + str(set(face.node_counts)))
        check_submapped(face, "the second order " + shape)
        print(shape + ": second order submapped,", len(face.elements), "quads of 8 nodes")
        for label, arguments in (("full-quad recombination", {"recombine": "SimpleFullQuad"}),
                                 ("Blossom full-quad recombination", {"recombine": "BlossomFullQuad"}),
                                 ("quasi-structured quad algorithm", {"algorithm_2d": "QuasiStructuredQuad"})):
            mesh(part, **arguments)
            face = FaceMesh(part.name, plane_xy)
            messages = submapping_messages(part.name)
            structured = face.kinds().keys() == {"quadrilateral"} and len(face.irregular_inner_nodes()) == 0
            if structured:
                check_submapped(face, "the " + shape + " with the " + label)
            else:
                check(len(messages) == 1 and "is meshed free" in messages[0],
                      "the " + shape + " with the " + label + " was meshed free without saying why: " + str(messages))
            print(shape + " with the " + label + ":", "submapped" if structured else "free", face.kinds(), messages)
            face.plot("plate_" + shape + "_" + label.split()[0].replace("-", "_"), shape + " with the " + label)


def check_folded():
    names = set(part.name for part in pmx.model.geometry.parts)
    pmx.import_file(os.path.join(MODELS, "submap_folded.brep"))
    part = [part for part in pmx.model.geometry.parts if part.name not in names][0]
    check(part.part_type == "Shell" and part.get_face_data().count == 3,
          "the folded sheet must be one shell of three faces")
    lengths = edges_by_length(part)
    check(len(lengths[40.0]) == 1 and len(lengths[60.0]) == 2, "the folded sheet has unexpected edges: " +
          str(sorted(lengths)))
    pin_edge_id, pin_geometry_id = lengths[40.0][0]
    items = pmx.model.mesh.setup_items
    items.add_local_mesh_size("PyTest_Submap_Pin", edge_selection(part, pin_edge_id), number_of_elements=11)
    try:
        elapsed = mesh(part)
    finally:
        items.remove(["PyTest_Submap_Pin"])
    base = FaceMesh(part.name, plane_xy, keep=lambda xyz: abs(xyz[2]) < 1E-6)
    steps, quality = check_submapped(base, "the L of the folded sheet")
    whole = FaceMesh(part.name, oblique)
    check(list(whole.kinds().keys()) == ["quadrilateral"], "the folded sheet must be quadrilaterals only: " +
          str(whole.kinds()))
    # The vertex the base and the two flanges share has three quads around it, as the corner of a box has
    check(len(whole.irregular_inner_nodes()) == 1, "the folded sheet has " + str(len(whole.irregular_inner_nodes())) +
          " inner nodes without four quads instead of the one at the corner of its flanges")
    counts = [edge_count(geometry_id) for edge_id, geometry_id in lengths[60.0]]
    check(edge_count(pin_geometry_id) == 11, "the pinned edge of the folded L does not carry 11 elements")
    check(counts[0] == counts[1] and counts[0] == steps[0] and counts[0] > 11,
          "the edge of the L across from the pinned one and the flange across from it must carry the same number " +
          "of elements, the pinned 11 and more: " + str(counts) + ", boundary steps of the L " + str(steps))
    print("folded sheet: L submapped, boundary steps", steps, "worst scaled Jacobian", round(quality, 3),
          "edges of 60 mm", counts, "time", round(elapsed, 2), "s")
    whole.plot("folded", "Folded sheet: submapped L between two transfinite flanges, 11 elements pinned")


def check_curved():
    names = set(part.name for part in pmx.model.geometry.parts)
    pmx.import_file(os.path.join(MODELS, "submap_curved.brep"))
    part = [part for part in pmx.model.geometry.parts if part.name not in names][0]
    elapsed = mesh(part)
    face = FaceMesh(part.name, unrolled)
    steps, quality = check_submapped(face, "the curved L")
    off_radius = max(abs(math.hypot(xyz[0], xyz[1]) - RADIUS) for xyz in face.xyz.values())
    check(off_radius < 1E-6 * RADIUS, "a node of the curved L lies " + str(off_radius) + " off the cylinder")
    print("curved L: submapped", face.kinds(), "boundary steps", steps, "worst scaled Jacobian", round(quality, 3),
          "largest distance off the cylinder", off_radius, "time", round(elapsed, 2), "s")
    face.plot("curved", "Curved L on a cylinder of radius 50, unrolled")


def check_raised_outer_sides(names):
    """A notch side of the U pinned deeper than its outer sides ask for: the outer side across raised, still a U."""
    part = geometry_part(names["U"])
    lengths = edges_by_length(part)
    edge_id, geometry_id = lengths[35.0][0]
    items = pmx.model.mesh.setup_items
    items.add_local_mesh_size("PyTest_Submap_Pin", edge_selection(part, edge_id), number_of_elements=12)
    try:
        mesh(part)
    finally:
        items.remove(["PyTest_Submap_Pin"])
    face = FaceMesh(part.name, plane_xy)
    steps, quality = check_submapped(face, "the U with a notch side pinned to 12")
    outer = [edge_count(g) for e, g in lengths[50.0]]
    notch = [edge_count(g) for e, g in lengths[35.0]]
    check(edge_count(geometry_id) == 12 and max(outer) >= 13,
          "the outer sides of the U must be raised past the pinned notch side: outer " + str(outer) + ", notch " +
          str(notch))
    print("U with a notch side pinned to 12: outer sides", outer, "notch sides", notch, "worst scaled Jacobian",
          round(quality, 3))
    face.plot("raised_outer_sides", "U, notch side pinned to 12, outer sides raised to " + str(outer))


def thin_parts():
    """The parts of submap_thin.brep by their faces and edges."""
    names = set(part.name for part in pmx.model.geometry.parts)
    pmx.import_file(os.path.join(MODELS, "submap_thin.brep"))
    parts = {}
    for part in pmx.model.geometry.parts:
        if part.name in names:
            continue
        lengths = sorted(round(float(l), 3) for l in part.get_edge_data().lengths)
        if part.get_face_data().count == 2:
            parts["U with flange"] = part
        elif len(lengths) == 8:
            parts["thin web U"] = part
        elif len(lengths) == 6 and lengths[0] == 1.0:
            parts["small step"] = part
        elif len(lengths) == 6 and lengths[0] == 2.0:
            parts["thin L leg"] = part
        elif len(lengths) == 12:
            parts["rejected bump"] = part
        elif len(lengths) == 10:
            parts["benign bump"] = part
    check(len(parts) == 6, "the parts of submap_thin.brep were not all found: " + str(sorted(parts)))
    return parts


def check_thin_faces(parts):
    """Faces thinner than the element size, which the natural counts would close up, kept apart by the separations."""
    part = parts["thin web U"]
    lengths = edges_by_length(part)
    mesh(part, transfinite_submapped=False)
    outer = [edge_count(g) for e, g in lengths[40.0]]
    notch = [edge_count(g) for e, g in lengths[38.0]]
    print("thin web U meshed free: outer sides", outer, "notch sides", notch)
    check(max(outer) <= min(notch), "the counts the mesh size asks for must give the notch of the thin web U the full " +
          "depth, or the case tests nothing: outer sides " + str(outer) + ", notch sides " + str(notch))
    for label, arguments in (("", {}), (" full-quad", {"recombine": "SimpleFullQuad"})):
        mesh(part, **arguments)
        face = FaceMesh(part.name, plane_xy)
        steps, quality = check_submapped(face, "the thin web U" + label)
        lengths = edges_by_length(part)
        outer = [edge_count(g) for e, g in lengths[40.0]]
        notch = [edge_count(g) for e, g in lengths[38.0]]
        check(all(o > n for o in outer for n in notch), "the web of the U must stay a cell thick: outer sides " +
              str(outer) + ", notch sides " + str(notch))
        print("thin web U" + label + ": submapped,", len(face.elements), "quads, outer sides", outer, "notch sides",
              notch, "worst scaled Jacobian", round(quality, 3))
        face.plot("thin_web_U" + label.replace(" ", "_").replace("-", "_"),
                  "U with a web of 2 mm" + label + ": outer sides " + str(outer) + ", notch sides " + str(notch))
    #
    part = parts["small step"]
    mesh(part)
    face = FaceMesh(part.name, plane_xy)
    steps, quality = check_submapped(face, "the face with a step of 1 mm")
    print("small step: submapped,", len(face.elements), "quads, boundary steps", steps)
    face.plot("small_step", "Face with a step of 1 mm: " + str(len(face.elements)) + " quads")
    # A leg one cell thick has only boundary nodes, and its corner quad is convex only while the element size is below
    # twice the leg's thickness
    part = parts["thin L leg"]
    mesh(part, max_size=3.0)
    face = FaceMesh(part.name, plane_xy)
    steps, quality = check_submapped(face, "the L with legs of 2 mm at the element size 3")
    print("thin L leg at the element size 3: submapped,", len(face.elements), "quads, worst scaled Jacobian",
          round(quality, 3))
    face.plot("thin_L_leg", "L with legs of 2 mm, element size 3: " + str(len(face.elements)) + " quads")


def mesh_pinned_u(part, bottom_count, top_count=None):
    lengths = edges_by_length(part)
    check(len(lengths[60.0]) == 2 and len(lengths[30.0]) == 1 and len(lengths[15.0]) == 2,
          "the U and flange have unexpected edges")
    counts = ((60.0, bottom_count), (30.0, 2))
    items = pmx.model.mesh.setup_items
    pins = []
    if top_count is not None:
        edge_id = lengths[15.0][0][0]
        name = "PyTest_Submap_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, edge_selection(part, edge_id), number_of_elements=top_count)
        pins.append(name)
    for length, count in counts:
        for edge_id, _ in lengths[length]:
            name = "PyTest_Submap_Pin_" + str(edge_id)
            items.add_local_mesh_size(name, edge_selection(part, edge_id), number_of_elements=count)
            pins.append(name)
    try:
        mesh(part)
    finally:
        items.remove(pins)
    for length, count in counts:
        check(all(edge_count(g) == count for _, g in lengths[length]),
              "the U and flange did not retain the %d elements pinned on edges of %g mm" % (count, length))
    u = FaceMesh(part.name, plane_xy, keep=lambda xyz: abs(xyz[2]) < 1E-6)
    flange = FaceMesh(part.name, lambda xyz: (xyz[0], xyz[2]), keep=lambda xyz: abs(xyz[1]) < 1E-6)
    return u, flange, submapping_messages(part.name)


def check_mesher_fallback(parts):
    """Matching untangles the thin L and pinned U. A grid the pins fold still falls back to the native free mesh."""
    tolerance = 1E-3
    part = parts["thin L leg"]
    detected, _ = detected_faces(part)
    check(detected == [0], "the thin L must be detected as submapped: " + str(detected))
    mesh(part)
    face = FaceMesh(part.name, plane_xy)
    messages = submapping_messages(part.name)
    _, quality = check_submapped(face, "the matched thin L at the element size 5")
    check(len(face.elements) == 23 and abs(quality - 1.0) < tolerance and not messages,
          "the matched thin L has an unexpected grid: " + str((face.kinds(), quality, messages)))
    print("thin L at the element size 5:", face.kinds(), "worst scaled Jacobian", quality, messages)
    face.plot("matched_thin_L", "L with legs of 2 mm, element size 5: 23 structured quads")

    part = parts["U with flange"]
    detected, _ = detected_faces(part)
    check(detected == [0], "the U beside the flange must be detected as submapped: " + str(detected))
    u, flange, messages = mesh_pinned_u(part, 30)
    _, quality = check_submapped(u, "the matched U with 30 elements on its bottom and 2 on its notch bottom")
    _, flange_quality = check_submapped(flange, "the flange beside the matched U")
    check(len(u.elements) == 254 and abs(quality - 0.846571) < tolerance and not messages,
          "the matched U did not retain its 254-quad grid: " + str((u.kinds(), quality, messages)))
    check(len(flange.elements) == 180 and abs(flange_quality - 1.0) < tolerance,
          "the flange beside the matched U did not retain its 180 right-angled quads")
    print("U pinned 30 / 2:", u.kinds(), "worst scaled Jacobian", quality, messages,
          "flange", flange.kinds(), "worst scaled Jacobian", flange_quality)
    FaceMesh(part.name, oblique).plot("matched_U_flange", "U pinned 30 / 2: U and flange structured")

    u, flange, messages = mesh_pinned_u(part, 50)
    _, quality = check_submapped(u, "the matched U with 50 elements on its bottom and 2 on its notch bottom")
    _, flange_quality = check_submapped(flange, "the flange beside the matched U pinned 50 / 2")
    check(len(u.elements) == 532 and abs(quality - 0.548525) < tolerance and not messages,
          "the matched U pinned 50 / 2 did not retain its 532-quad grid: " + str((u.kinds(), quality, messages)))
    check(len(flange.elements) == 450 and abs(flange_quality - 1.0) < tolerance,
          "the flange beside the matched U pinned 50 / 2 did not retain its 450 right-angled quads")
    print("U pinned 50 / 2:", u.kinds(), "worst scaled Jacobian", quality, messages,
          "flange", flange.kinds(), "worst scaled Jacobian", flange_quality)

    u, flange, messages = mesh_pinned_u(part, 50, 3)
    steps, area = u.lattice()
    check(steps == [50, 20, 50, 20] and area == 532, "the U pinned 50 / 2 / 3 did not close its 532-cell outline")
    check(len(messages) == 1 and "is meshed free" in messages[0] and
          "117 of the 532 quads of its grid fold" in messages[0],
          "the U pinned 50 / 2 / 3 must fall back in the mesher for its folded grid: " + str(messages))
    check(u.kinds() == {"triangle": 92, "quadrilateral": 131},
          "the U pinned 50 / 2 / 3 did not retain the free mesh: " + str(u.kinds()))
    _, flange_quality = check_submapped(flange, "the flange beside the free U")
    check(len(flange.elements) == 450 and abs(flange_quality - 1.0) < tolerance,
          "the flange beside the free U did not retain its 450 right-angled quads")
    print("U pinned 50 / 2 / 3:", u.kinds(), messages, "flange", flange.kinds())
    FaceMesh(part.name, oblique).plot("fallback_mesher_U_flange",
                                      "U pinned 50 / 2 / 3: U meshed free, flange structured")


def check_detection_rejection(parts):
    """A face whose angles make it submappable but whose provisional grid folds or is poor is not detected."""
    rejected = parts["rejected bump"]
    detected, flags = detected_faces(rejected)
    check(detected == [] and flags == [False], "the L whose bottom side winds back over itself must not be " +
          "detected: " + str(detected) + ", flags " + str(flags))
    mesh(rejected)
    face = FaceMesh(rejected.name, plane_xy)
    check("triangle" in face.kinds() and len(submapping_messages(rejected.name)) == 0,
          "the rejected L must be meshed free without a submapping message: " + str(face.kinds()))
    print("bump winding back: not detected, meshed free", face.kinds())
    face.plot("fallback_detection_bump", "L whose bottom side winds back: not detected, meshed free")
    #
    benign = parts["benign bump"]
    detected, flags = detected_faces(benign)
    check(detected == [0] and flags == [True], "the L with a low bump must be detected: " + str(detected))
    mesh(benign)
    face = FaceMesh(benign.name, plane_xy)
    # Its bottom side climbs along the edges of the bump, so its boundary does not run along the axes
    steps, quality = check_submapped(face, "the L with a low bump", lattice=False)
    print("low bump: submapped,", len(face.elements), "quads, worst scaled Jacobian", round(quality, 3))
    face.plot("benign_bump", "L with a low bump: submapped, worst scaled Jacobian " + str(round(quality, 3)))


def check_settings(names):
    items = pmx.model.mesh.setup_items
    part = geometry_part(names["L"])
    items.add_shell_gmsh("PyTest_Submap_Settings", geometry_selection(part))
    values = items["PyTest_Submap_Settings"].get_settings()
    check(values["transfinite_submapped"] is True, "a new shell item must be submapped: " + str(values))
    items.replace_shell_gmsh("PyTest_Submap_Settings", transfinite_submapped=False)
    items.replace_shell_gmsh("PyTest_Submap_Settings", algorithm_recombine="Blossom")
    values = items["PyTest_Submap_Settings"].get_settings()
    check(values["transfinite_submapped"] is False and values["algorithm_recombine"] == "Blossom",
          "replace_shell_gmsh must keep transfinite_submapped when it is not given: " + str(values))
    # The angle is used while a face is submapped, and refused only once nothing is structured
    items.replace_shell_gmsh("PyTest_Submap_Settings", transfinite_three_sided="No", transfinite_four_sided=False,
                             transfinite_submapped=True, transfinite_angle_deg=120.0)
    values = items["PyTest_Submap_Settings"].get_settings()
    check(values["transfinite_angle_deg"] == 120.0, "the angle of a submapped item was not kept: " + str(values))
    message = raised_message(lambda: items.replace_shell_gmsh("PyTest_Submap_Settings", transfinite_submapped=False,
                                                              transfinite_angle_deg=100.0))
    check(message is not None and "transfinite_submapped" in message,
          "an unused angle must be refused naming transfinite_submapped: " + str(message))
    values = items["PyTest_Submap_Settings"].get_settings()
    check(values["transfinite_submapped"] is True, "a refused replace changed the item: " + str(values))
    items.replace_shell_gmsh("PyTest_Submap_Settings", transfinite_submapped=False)
    values = items["PyTest_Submap_Settings"].get_settings()
    check("transfinite_angle_deg" not in values, "an unused angle was read back: " + str(values))
    items.remove(["PyTest_Submap_Settings"])


def main():
    check(pmx.model.is_open(), "a model must be open")
    names = plate_names()
    check_settings(names)
    times = check_plates(names)
    check_pinned_l(names)
    check_invalid_u(names)
    check_raised_outer_sides(names)
    check_variants(names)
    parts = thin_parts()
    check_thin_faces(parts)
    check_mesher_fallback(parts)
    check_detection_rejection(parts)
    check_folded()
    check_curved()
    print("meshing times [s] free and submapped:", dict((k, (round(a, 2), round(b, 2))) for k, (a, b) in times.items()))
    print("TEST PASSED")


main()
