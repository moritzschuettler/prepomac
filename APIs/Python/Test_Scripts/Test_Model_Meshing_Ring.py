#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the ring mapping: a face of exactly two boundary loops that run roughly parallel is meshed as a structured ring,
#both loops carrying the same number of nodes, node i of one joined to node i of the other, with layers of quads between
#them. The edges of a ring pair up when the loops have as many and they face each other one to one; each pair then
#carries as many elements, otherwise only the two loops do. A face that is a ring and submapped as well is meshed as a
#ring when its wall is thin - the smallest distance between its loops below 1.5 times the element size - and by its
#lattice otherwise.
#
#  - ring_faces.brep: plane faces side by side, one part each - an annulus of r 20 and r 10 of two closed edges, the
#    same of half circles, the annulus whose inner seam is turned by 40 degrees against the outer one, a thin washer of
#    r 20 and r 17, an annulus whose bore of r 8 is off the centre by 4 mm and one off by 11.8 mm, a rectangular tube
#    60 x 40 with corner radii of 6 outside and 4 inside and a wall of 2, the tube with sharp corners and a wall of 2,
#    the tube with a wall of 10, a square plate of 60 with a bore of r 28 and with one of r 8, an elliptic ring of the
#    half axes 30 x 20 and 24 x 14, an L with a hole of r 3, a plate with two holes and a square plate of 60 with a
#    square hole of the diagonal 8 turned by 45 degrees.
#    At the element size 5 every face is detected as recorded - a ring or not, paired or not, submapped or not - and
#    meshed as recorded: a ring is quadrilaterals only, every boundary node in two quads and every inner node in four,
#    with as many layers and quads as recorded, no folded quad and a worst scaled Jacobian of 0.2 at least, and writes
#    no message; a lattice has a boundary node in one quad. The annulus far off the centre, whose ring grid has a quad
#    corner below the floor, is no ring and is meshed free, and so is the plate with the small turned hole, whose
#    inner loop is shorter than a quarter of the outer one. The thin tube meshed as a ring keeps the counts of its outer
#    edges, which its lattice refined at the corners of the hole.
#  - Pinned counts: 30 elements on the outer edge of the annulus are carried to the inner one; 20 and 24 on the two
#    leave it free with a message.
#  - A full-quad recombination meshes the annulus, the thin tube and the tube with radii as rings, and second order
#    elements keep the annulus a ring.
#  - ring_sweep.brep: a pipe of r 20 and r 12, a thin pipe of r 20 and r 18, the tube with radii and a square flange of
#    60 with a bore of r 28, extruded by 30 mm and swept from their ends in z = 0: hexahedra only, the source a ring,
#    the hexahedra a multiple of its quads, as many as recorded.
#  The models were made by gmsh's OCC kernel through GmshCommon: plane surfaces of lines, circle arcs, closed circles
#  and closed ellipses in z = 0, the solids extruded by 30 mm.
#  With the environment variable PMX_RING_PLOT_DIR set, the meshes are written into that folder as PNG images.
#
#  PrePoMax.com -f ring_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_Ring.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys
import clr
clr.AddReference('CaeMesh')
clr.AddReference('System.Drawing')
from CaeMesh import RingFaceDetector, SubmapFaceDetector
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
PLOT_DIR = os.environ.get("PMX_RING_PLOT_DIR")
MAX_SIZE = 5.0
SPACING = 150.0
SHELLS = ["annulus", "annulus of halves", "turned seam", "thin washer", "eccentric", "very eccentric",
          "tube with radii", "thin tube", "thick tube", "plate large bore", "plate small bore", "elliptic ring",
          "L with small hole", "two holes", "small diamond"]
SOLIDS = ["pipe", "thin pipe", "tube with radii", "flange"]
# By shape: detected as a ring (None for not, else whether paired), submapped, the mapping chosen, the layers of a ring,
# and the elements at the element size 5
EXPECTED = {"annulus": (True, False, "ring", 3, 78),
            "annulus of halves": (True, False, "ring", 3, 78),
            "turned seam": (True, False, "ring", 3, 78),
            "thin washer": (True, False, "ring", 1, 26),
            "eccentric": (True, False, "ring", 4, 104),
            "very eccentric": (None, False, "free", 0, 0),
            "tube with radii": (True, True, "ring", 1, 52),
            "thin tube": (True, True, "ring", 1, 40),
            "thick tube": (True, True, "lattice", 0, 64),
            "plate large bore": (False, True, "ring", 2, 96),
            "plate small bore": (None, True, "lattice", 0, 135),
            "elliptic ring": (True, False, "ring", 1, 32),
            "L with small hole": (None, True, "lattice", 0, 78),
            "two holes": (None, True, "lattice", 0, 110),
            "small diamond": (None, False, "free", 0, 0)}
EXPECTED_HEXAHEDRA = {"pipe": 312, "thin pipe": 156, "tube with radii": 468, "flange": 576}
PLOTTED = ["annulus", "turned seam", "thin washer", "eccentric", "tube with radii", "thin tube", "plate large bore",
           "elliptic ring"]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_x(names, known=()):
    """The geometry parts not in known by the shape they were made from, told apart by where they lie along x."""
    parts = {}
    for part in pmx.model.geometry.parts:
        if part.name in known:
            continue
        xs = [c[0] for c in part.get_vertex_data().coordinates]
        parts[names[int(round(min(xs) / SPACING))]] = part
    check(sorted(parts) == sorted(names), "the parts were not all found: " + str(sorted(parts)))
    return parts


def controller():
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    return form.GetType().GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form)


def mapping_messages(part_name):
    """The ring mapping and submapping lines the last meshing of the part wrote to the output window."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    result = []
    for line in lines[starts[-1]:]:
        for marker in ("Ring mapping:", "Submapping:"):
            if marker in line:
                result.append(line[line.index(marker):])
    return result


def detected_rings(part):
    geometry = controller().Model.Geometry
    return list(RingFaceDetector.Detect(geometry, geometry.Parts[part.name]))


def detected_submaps(part):
    geometry = controller().Model.Geometry
    return list(SubmapFaceDetector.Detect(geometry, geometry.Parts[part.name], 135.0))


def edges_by_length(part):
    data = part.get_edge_data()
    result = {}
    for edge_id, geometry_id, length in zip([int(i) for i in data.ids], [int(i) for i in data.geometry_ids],
                                            [float(l) for l in data.lengths]):
        result.setdefault(round(length, 3), []).append((edge_id, geometry_id))
    return result


def edge_nodes(geometry_id):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().count


def mesh(part, recombine="Simple", second_order=False, max_size=MAX_SIZE, pins=()):
    """Meshes the part by a shell item; pins are (edge id, number of elements)."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Ring_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                 [part.id]),
                                 relative_size=False, max_size=max_size, min_size=max_size / 10,
                                 second_order=second_order)
    names = ["PyTest_Ring_Size"]
    for edge_id, count in pins:
        name = "PyTest_Ring_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Ring", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine)
    names.append("PyTest_Ring")
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(names)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])


# Mesh access ----------------------------------------------------------------------------------------------------------
class FaceMesh(object):
    """The elements of a meshed part by their corner nodes, with the coordinates of the nodes in the plane z = 0."""

    def __init__(self, part_name):
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
            self.elements.append(connectivity[offsets[i]:offsets[i] + corners])
            self.node_counts.append(node_counts[i])
        self.uv = dict((node, (self.xyz[node][0], self.xyz[node][1])) for element in self.elements for node in element)

    def kinds(self):
        result = {}
        for element in self.elements:
            kind = "quadrilateral" if len(element) == 4 else "triangle"
            result[kind] = result.get(kind, 0) + 1
        return result


def boundary_nodes(quads):
    uses = {}
    for quad in quads:
        for k in range(len(quad)):
            edge = tuple(sorted((quad[k], quad[(k + 1) % len(quad)])))
            uses[edge] = uses.get(edge, 0) + 1
    return set(node for edge, count in uses.items() if count == 1 for node in edge)


def valences(quads):
    valence = {}
    for quad in quads:
        for node in quad:
            valence[node] = valence.get(node, 0) + 1
    return valence


def irregular_inner_nodes(quads):
    valence = valences(quads)
    boundary = boundary_nodes(quads)
    return [node for node, count in valence.items() if node not in boundary and count != 4]


def min_scaled_jacobian(quads, uv):
    """The smallest sine of a quad corner in the plane, the quads turned the way most of them turn; negative where a
    quad folds."""
    sines = []
    signs = 0
    for quad in quads:
        if len(quad) != 4:
            continue
        for c in range(4):
            p, a, b = uv[quad[c]], uv[quad[(c + 1) % 4]], uv[quad[(c + 3) % 4]]
            e1, e2 = (a[0] - p[0], a[1] - p[1]), (b[0] - p[0], b[1] - p[1])
            length = math.hypot(*e1) * math.hypot(*e2)
            sine = (e1[0] * e2[1] - e1[1] * e2[0]) / length if length > 0 else 0
            sines.append(sine)
            signs += 1 if sine > 0 else -1
    if not sines:
        return None
    return min(s if signs > 0 else -s for s in sines)


def mapping_of(elements):
    """How a mesh of quads was laid: a ring has every boundary node in two quads and every inner node in four, a lattice
    has the inner nodes in four and a boundary node in one, anything else is free. Returns the mapping and the layers of
    a ring: its quads over the nodes of one of its two loops."""
    if not elements or any(len(e) != 4 for e in elements) or len(irregular_inner_nodes(elements)) > 0:
        return "free", 0
    valence = valences(elements)
    boundary = boundary_nodes(elements)
    if all(valence[node] == 2 for node in boundary):
        return "ring", len(elements) * 2 // len(boundary)
    if any(valence[node] == 1 for node in boundary):
        return "lattice", 0
    return "free", 0


def plot(name, title, elements, uv):
    """The elements as a PNG image, quads blue, triangles orange, inner nodes without four quads red."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    us = [p[0] for p in uv.values()]
    vs = [p[1] for p in uv.values()]
    scale = 560.0 / max(max(us) - min(us), max(vs) - min(vs))
    width, height = int((max(us) - min(us)) * scale + 40), int((max(vs) - min(vs)) * scale + 70)
    bitmap = Bitmap(width, height)
    graphics = Graphics.FromImage(bitmap)
    graphics.Clear(Color.White)
    pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
    quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
    triangle_brush = SolidBrush(Color.FromArgb(246, 217, 200))

    def at(node):
        return PointF(float(20 + (uv[node][0] - min(us)) * scale), float(height - 20 - (uv[node][1] - min(vs)) * scale))

    for element in elements:
        points = Array[PointF]([at(n) for n in element])
        graphics.FillPolygon(quad_brush if len(element) == 4 else triangle_brush, points)
        graphics.DrawPolygon(pen, points)
    for node in irregular_inner_nodes([e for e in elements if len(e) == 4]):
        p = at(node)
        graphics.FillEllipse(Brushes.Firebrick, p.X - 3, p.Y - 3, 6.0, 6.0)
    graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, name + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


def check_ring(face, name):
    """A ring: quadrilaterals only, every boundary node in two quads, every inner node in four, and no quad corner
    below the floor or folded. Returns the layers and the worst scaled Jacobian."""
    mapping, layers = mapping_of(face.elements)
    check(mapping == "ring", name + " must be meshed as a ring, not " + mapping + ": " + str(face.kinds()))
    quality = min_scaled_jacobian(face.elements, face.uv)
    check(quality is not None and quality >= 0.2, name + " has a quad corner of the scaled Jacobian " + str(quality))
    return layers, quality


# Checks ---------------------------------------------------------------------------------------------------------------
def check_shells(parts):
    results = {}
    for name in SHELLS:
        part = parts[name]
        rings = detected_rings(part)
        submaps = detected_submaps(part)
        paired = rings[0].IsPaired if len(rings) == 1 else None
        mesh(part)
        face = FaceMesh(part.name)
        messages = mapping_messages(part.name)
        mapping, layers = mapping_of(face.elements)
        quality = min_scaled_jacobian(face.elements, face.uv) if mapping != "free" else None
        print(name + ": ring", len(rings), "paired", paired, "submapped", len(submaps), "mapping", mapping, "layers",
              layers, "elements", face.kinds(), "worst scaled Jacobian", None if quality is None else round(quality, 3),
              "messages", messages)
        expected_paired, expected_submapped, expected_mapping, expected_layers, expected_elements = EXPECTED[name]
        check(len(rings) == (0 if expected_paired is None else 1) and paired == expected_paired,
              "the " + name + " is detected as " + str(len(rings)) + " rings, paired " + str(paired))
        check(len(submaps) == (1 if expected_submapped else 0), "the " + name + " is submapped " + str(len(submaps)))
        check(mapping == expected_mapping and layers == expected_layers,
              "the " + name + " is meshed by " + mapping + " in " + str(layers) + " layers")
        check(len(messages) == 0, "the " + name + " wrote: " + str(messages))
        if mapping != "free":
            check(quality >= 0.2, "the " + name + " has a quad corner of the scaled Jacobian " + str(quality))
            check(len(face.elements) == expected_elements, "the " + name + " has " + str(len(face.elements)) +
                  " quads instead of " + str(expected_elements))
        title = name + ": " + mapping + (" " + str(layers) + " layers" if mapping == "ring" else "") + ", " + \
                str(len(face.elements)) + " elements" + ("" if quality is None else ", worst scaled Jacobian " +
                                                         str(round(quality, 3)))
        if name in PLOTTED or mapping == "free":
            plot("ring_" + name.replace(" ", "_"), title, face.elements, face.uv)
        results[name] = face
    return results


def check_thin_tube(parts):
    """The thin tube meshed as a ring keeps the natural counts of its outer edges: 12 along the long side of 60."""
    part = parts["thin tube"]
    edges = edges_by_length(part)
    counts = dict((length, [edge_nodes(g) - 1 for _, g in edges[length]]) for length in (60.0, 40.0, 56.0, 36.0))
    print("thin tube as a ring: elements on the edges by length", counts)
    check(counts[60.0] == [12, 12] and counts[56.0] == [12, 12] and counts[40.0] == [8, 8] and counts[36.0] == [8, 8],
          "the thin tube does not carry the natural counts on its edges: " + str(counts))


def check_pins(parts):
    part = parts["annulus"]
    edges = edges_by_length(part)
    outer = edges[round(2 * math.pi * 20, 3)]
    inner = edges[round(2 * math.pi * 10, 3)]
    check(len(outer) == 1 and len(inner) == 1, "the annulus has not two closed edges: " + str(edges))
    # 30 elements on the outer edge are carried to the inner one; a closed edge carries as many nodes as elements
    mesh(part, pins=[(outer[0][0], 30)])
    face = FaceMesh(part.name)
    counts = [edge_nodes(outer[0][1]), edge_nodes(inner[0][1])]
    layers, quality = check_ring(face, "the annulus with 30 elements on its outer edge")
    check(counts == [30, 30] and len(mapping_messages(part.name)) == 0,
          "30 elements on the outer edge are not carried to the inner one: " + str(counts))
    print("annulus with 30 elements on its outer edge: the inner carries", counts[1], "layers", layers, "quads",
          len(face.elements), "worst scaled Jacobian", round(quality, 3))
    # 20 and 24 leave it free
    mesh(part, pins=[(outer[0][0], 20), (inner[0][0], 24)])
    face = FaceMesh(part.name)
    messages = mapping_messages(part.name)
    print("annulus with 20 and 24 elements:", face.kinds(), messages)
    check(mapping_of(face.elements)[0] == "free" and len(messages) == 1 and
          messages[0].startswith("Ring mapping:") and "is meshed free" in messages[0] and "20" in messages[0] and
          "24" in messages[0], "20 and 24 elements must leave the annulus free with a message: " + str(messages))
    plot("ring_annulus_pinned_20_24", "annulus, 20 and 24 elements: free " + str(face.kinds()), face.elements, face.uv)


def check_variants(parts):
    for name in ["annulus", "thin tube", "tube with radii"]:
        part = parts[name]
        mesh(part, recombine="SimpleFullQuad")
        face = FaceMesh(part.name)
        messages = mapping_messages(part.name)
        layers, quality = check_ring(face, "the " + name + " with the full-quad recombination")
        check(len(messages) == 0, "the full-quad " + name + " wrote: " + str(messages))
        print(name + " with the full-quad recombination: a ring of", layers, "layers,", len(face.elements),
              "quads, worst scaled Jacobian", round(quality, 3))
        plot("ring_" + name.replace(" ", "_") + "_full_quad", name + ", full-quad: " + str(len(face.elements)) +
             " quads", face.elements, face.uv)
    part = parts["annulus"]
    mesh(part, second_order=True)
    face = FaceMesh(part.name)
    check(all(count == 8 for count in face.node_counts), "the second order annulus has elements of other than 8 " +
          "nodes: " + str(set(face.node_counts)))
    check_ring(face, "the second order annulus")
    print("annulus: second order ring,", len(face.elements), "quads of 8 nodes")


# Sweep ----------------------------------------------------------------------------------------------------------------
class SweptMesh(object):
    """The elements of a meshed part by their corner nodes, with the node coordinates."""

    def __init__(self, part_name):
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name]
        check(len(meshed) == 1, "the part " + part_name + " was not meshed")
        self.xyz = dict(zip([int(i) for i in meshed[0].get_node_ids()],
                            [list(c) for c in meshed[0].get_node_coordinates()]))
        self.elements = []
        self.node_counts = []
        for element_id in meshed[0].get_element_ids():
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

    def source(self):
        """The quads of the hexahedra faces whose four nodes all lie in the end the sweep started from, z = 0 or z = 30,
        whichever has them."""
        for z in (0.0, 30.0):
            quads = []
            for element in self.elements:
                if len(element) != 8:
                    continue
                for face in [(0, 1, 2, 3), (4, 5, 6, 7)]:
                    nodes = [element[k] for k in face]
                    if all(abs(self.xyz[node][2] - z) < 1E-6 for node in nodes):
                        quads.append(nodes)
            if quads:
                return quads
        return []


def check_sweeps():
    known = set(part.name for part in pmx.model.geometry.parts)
    pmx.import_file(os.path.join(MODELS, "ring_sweep.brep"))
    parts = parts_by_x(SOLIDS, known)
    items = pmx.model.mesh.setup_items
    for name in SOLIDS:
        part = parts[name]
        selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
        items.add_meshing_parameters("PyTest_Ring_Size", selection, relative_size=False, max_size=MAX_SIZE,
                                     min_size=MAX_SIZE / 10)
        # The end in z = 0 is the source: on its own the sweep takes the walls of the thin tube with radii as its
        # source and sweeps through its thickness
        faces = part.get_face_data()
        bottom = [int(faces.ids[i]) for i in range(faces.count)
                  if str(faces.types[i]) == "Plane" and abs(float(faces.centroids[i][2])) < 1E-6]
        check(len(bottom) == 1, "the " + name + " has not one end face in z = 0: " + str(bottom))
        items.add_sweep_mesh("PyTest_Ring_Sweep", pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                                                              {part.id: bottom}))
        try:
            report = pmx.model.mesh.generate(part.name)
        finally:
            items.remove(["PyTest_Ring_Sweep", "PyTest_Ring_Size"])
        check(report[part.name] == "", "sweeping the " + name + " reported: " + report[part.name])
        mesh_ = SweptMesh(part.name)
        kinds = mesh_.kinds()
        messages = mapping_messages(part.name)
        source = mesh_.source()
        print("swept " + name + ":", kinds, messages, "source quads", len(source))
        check(list(kinds.keys()) == ["hexahedron"] and len(messages) == 0,
              "the swept " + name + " must be hexahedra only: " + str(kinds) + " " + str(messages))
        uv = dict((node, (mesh_.xyz[node][0], mesh_.xyz[node][1])) for quad in source for node in quad)
        mapping, layers = mapping_of(source)
        quality = min_scaled_jacobian(source, uv)
        check(mapping == "ring" and quality >= 0.2, "the source of the swept " + name + " is " + mapping +
              " with a worst scaled Jacobian of " + str(quality))
        check(len(source) > 0 and len(mesh_.elements) % len(source) == 0,
              "the hexahedra of the " + name + " are not a multiple of its " + str(len(source)) + " source quads")
        check(len(mesh_.elements) == EXPECTED_HEXAHEDRA[name], "the swept " + name + " has " +
              str(len(mesh_.elements)) + " hexahedra instead of " + str(EXPECTED_HEXAHEDRA[name]))
        print("   hexahedra", len(mesh_.elements), "source", len(source), "quads in", layers, "ring layers x",
              len(mesh_.elements) // len(source), "sweep layers, worst scaled Jacobian of the source",
              round(quality, 3))
        plot("ring_source_" + name.replace(" ", "_"), "Source of the swept " + name + ": a ring of " + str(layers) +
             " layers, " + str(len(source)) + " quads", source, uv)


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    check_shells(parts)
    check_thin_tube(parts)
    check_pins(parts)
    check_variants(parts)
    check_sweeps()
    print("TEST PASSED")


main()
