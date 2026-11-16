#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the all-quad recombination of a Gmsh shell: gmsh meshes every face with the base recombination
#GmshAPI.GetGmshRecombinationAlgorithm chooses (Blossom; the checks hold for Simple as well), the structured faces are
#handled as with Simple, and every face left free is turned into quads only on the boundary nodes gmsh placed
#(GmshQuadMesher, QuadCleanup). The number of elements around a free face is balanced to an even total where nothing
#else holds it, since only an even total allows quads only, on the counts gmsh gives the edges in the end. A recombined
#sweep turns its free sources into quads only by default, so that the blocks are swept into hexahedra only.
#
#  - all_quad_faces.brep: one face per part side by side - a disc of r 20, a half disc of r 20, a triangle, a house
#    whose roof meets its walls at 117 and 127 degrees, a disc of r 20 with a bore of r 8 off the centre by 11.8 mm, a
#    square plate of 60 with a square hole of the diagonal 8 turned by 45 degrees, a plate of 80 x 50 with holes of r 4,
#    6 and 9, an irregular heptagon, an L whose legs are 2 mm wide and 30 long, and a quarter of a cylinder of r 40 and
#    50 high with a round hole of r 6 cut through it radially. The half disc and the cylinder patch fall back from the
#    submapping, the plate with three holes and the thin L are submapped, the others are free.
#    At the element size 5, without the three-sided mapping so that the triangle is meshed free as well: Simple leaves
#    triangles on the free faces; AllQuad gives an even number of elements around every face and quads only, none
#    folded, a worst scaled Jacobian of 0.2 at least measured in space, the same boundary nodes as Simple where it has
#    as many elements around the face as Simple, and no all-quad message. The table of both and the time are printed.
#    Simple meshes every face as recorded, as before the all-quad recombination existed.
#  - Pinned odd totals: 25 elements on the circle of the disc, and 5 on every edge of the heptagon, leave exactly one
#    triangle on each and an all-quad message that names the odd total. A number set on one edge of a face whose other
#    edges are free is balanced to an even total instead.
#  - Second order: the house with AllQuad is quads of 8 nodes only.
#  - all_quad_sweep.brep: the first eight profiles extruded by 20 mm and swept from their ends in z = 0 with the default
#    recombination: hexahedra only, the hexahedra a multiple of the source quads.
#  The models were made by gmsh's OCC kernel through GmshCommon: plane surfaces of lines, circle arcs and closed circles
#  in z = 0, the solids extruded by 20 mm, the cylinder patch revolved from a line and fragmented by a radial cylinder.
#  With the environment variable PMX_QUAD_PLOT_DIR set, the meshes are written into that folder as PNG images, Simple
#  before AllQuad.
#
#  PrePoMax.com -f all_quad_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_All_Quad.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import sys
import time
import clr
clr.AddReference('System.Drawing')
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
PLOT_DIR = os.environ.get("PMX_QUAD_PLOT_DIR")
MAX_SIZE = 5.0
SPACING = 150.0
FLOOR = 0.2
SHELLS = ["disc", "half disc", "triangle", "house", "eccentric annulus", "plate with diamond", "three holes",
          "heptagon", "thin L", "cylinder patch"]
SOLIDS = SHELLS[:8]
# Simple at the element size 5: quads and triangles, as before the all-quad recombination existed
EXPECTED_SIMPLE = {"disc": (58, 16), "half disc": (33, 12), "triangle": (56, 23), "house": (61, 10),
                   "eccentric annulus": (58, 14), "plate with diamond": (165, 54), "three holes": (133, 0),
                   "heptagon": (97, 31), "thin L": (17, 0), "cylinder patch": (152, 46)}
CYLINDER_RADIUS = 40.0


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


def output_messages(part_name):
    """The all-quad, ring mapping and submapping lines the last meshing of the part wrote to the output window."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    result = []
    for line in lines[starts[-1]:]:
        for marker in ("All-quad meshing:", "Ring mapping:", "Submapping:"):
            if marker in line:
                result.append(line[line.index(marker):])
    return result


def all_quad_messages(part_name):
    return [m for m in output_messages(part_name) if m.startswith("All-quad meshing:")]


def edges_by_length(part):
    data = part.get_edge_data()
    result = {}
    for edge_id, length in zip([int(i) for i in data.ids], [float(l) for l in data.lengths]):
        result.setdefault(round(length, 3), []).append(edge_id)
    return result


def mesh(part, recombine, second_order=False, pins=()):
    """Meshes the part by a shell item without the three-sided mapping; pins are (edge id, number of elements). Returns
    the time the meshing took."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Quad_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                 [part.id]),
                                 relative_size=False, max_size=MAX_SIZE, min_size=MAX_SIZE / 10,
                                 second_order=second_order)
    names = ["PyTest_Quad_Size"]
    for edge_id, count in pins:
        name = "PyTest_Quad_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Quad", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine, transfinite_three_sided="No")
    names.append("PyTest_Quad")
    start = time.time()
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(names)
    seconds = time.time() - start
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    return seconds


# Mesh access ----------------------------------------------------------------------------------------------------------
class FaceMesh(object):
    """The elements of a meshed part by their corner nodes, with the coordinates of the nodes."""

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

    def kinds(self):
        quads = sum(1 for e in self.elements if len(e) == 4)
        return quads, len(self.elements) - quads

    def boundary_points(self):
        return sorted(tuple(round(c, 9) for c in self.xyz[node]) for node in boundary_nodes(self.elements))


def boundary_edges(elements):
    uses = {}
    for element in elements:
        for k in range(len(element)):
            edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return [edge for edge, count in uses.items() if count == 1]


def boundary_nodes(elements):
    return set(node for edge in boundary_edges(elements) for node in edge)


def irregular_inner_nodes(elements):
    boundary = boundary_nodes(elements)
    valence = {}
    for element in elements:
        for node in element:
            valence[node] = valence.get(node, 0) + 1
    return [node for node, count in valence.items() if node not in boundary and count != 4]


def normal(name, point, dx):
    if name == "cylinder patch":
        x, y = point[0] - dx, point[1]
        r = math.hypot(x, y)
        return (x / r, y / r, 0.0)
    return (0.0, 0.0, 1.0)


def min_scaled_jacobian(name, face, dx):
    """The smallest sine of a corner in space against the normal of the surface, a triangle scaled so that 60 degrees
    count as 1, the elements turned the way most corners turn; negative where an element folds."""
    sines = []
    signs = 0
    for element in face.elements:
        n = len(element)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            e1 = [a[k] - p[k] for k in range(3)]
            e2 = [b[k] - p[k] for k in range(3)]
            cross = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]
            nrm = normal(name, p, dx)
            length = math.sqrt(sum(v * v for v in e1) * sum(v * v for v in e2))
            sine = sum(cross[k] * nrm[k] for k in range(3)) / length if length > 0 else 0.0
            sines.append(sine * (2 / math.sqrt(3) if n == 3 else 1.0))
            signs += 1 if sine > 0 else -1
    return min(s if signs > 0 else -s for s in sines)


def plot_points(name, face, dx):
    """The plane coordinates the plots use: x and y, the cylinder patch unrolled."""
    if name != "cylinder patch":
        return dict((node, (p[0], p[1])) for node, p in face.xyz.items())
    return dict((node, (CYLINDER_RADIUS * math.atan2(p[1], p[0] - dx), p[2])) for node, p in face.xyz.items())


def plot(file_name, title, elements, uv):
    """The elements as a PNG image, quads blue, triangles orange, inner nodes without four quads red."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    used = set(n for e in elements for n in e)
    us = [uv[n][0] for n in used]
    vs = [uv[n][1] for n in used]
    scale = 560.0 / max(max(us) - min(us), max(vs) - min(vs))
    width, height = int((max(us) - min(us)) * scale + 40), int((max(vs) - min(vs)) * scale + 70)
    bitmap = Bitmap(width, height)
    graphics = Graphics.FromImage(bitmap)
    graphics.Clear(Color.White)
    pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
    quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
    triangle_brush = SolidBrush(Color.FromArgb(246, 190, 160))

    def at(node):
        return PointF(float(20 + (uv[node][0] - min(us)) * scale), float(height - 20 - (uv[node][1] - min(vs)) * scale))

    for element in elements:
        points = Array[PointF]([at(n) for n in element])
        graphics.FillPolygon(quad_brush if len(element) == 4 else triangle_brush, points)
        graphics.DrawPolygon(pen, points)
    for node in irregular_inner_nodes(elements):
        p = at(node)
        graphics.FillEllipse(Brushes.Firebrick, p.X - 3, p.Y - 3, 6.0, 6.0)
    graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, file_name + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


def describe(face, name, dx):
    quads, triangles = face.kinds()
    return quads, triangles, min_scaled_jacobian(name, face, dx), len(irregular_inner_nodes(face.elements))


# Checks ---------------------------------------------------------------------------------------------------------------
def check_shells(parts):
    rows = []
    for i, name in enumerate(SHELLS):
        part = parts[name]
        dx = SPACING * i
        # Simple
        simple_seconds = mesh(part, "Simple")
        simple = FaceMesh(part.name)
        simple_messages = output_messages(part.name)
        s_quads, s_triangles, s_quality, s_irregular = describe(simple, name, dx)
        simple_total = len(boundary_edges(simple.elements))
        print(name + ": Simple", s_quads, "quads", s_triangles, "triangles, boundary total", simple_total,
              "worst scaled Jacobian", round(s_quality, 3), "irregular", s_irregular, "messages", simple_messages)
        if name in EXPECTED_SIMPLE:
            check((s_quads, s_triangles) == EXPECTED_SIMPLE[name], "Simple meshes the " + name + " with " +
                  str((s_quads, s_triangles)) + " instead of " + str(EXPECTED_SIMPLE[name]))
        plot("all_quad_" + name.replace(" ", "_") + "_simple", name + ", Simple: " + str(s_quads) + " quads, " +
             str(s_triangles) + " triangles, worst SJ " + str(round(s_quality, 3)), simple.elements,
             plot_points(name, simple, dx))
        # All-quad
        seconds = mesh(part, "AllQuad")
        face = FaceMesh(part.name)
        messages = all_quad_messages(part.name)
        quads, triangles, quality, irregular = describe(face, name, dx)
        total = len(boundary_edges(face.elements))
        print(name + ": AllQuad", quads, "quads", triangles, "triangles, boundary total", total,
              "worst scaled Jacobian", round(quality, 3), "irregular", irregular, "in", round(seconds, 3), "s (Simple",
              round(simple_seconds, 3), "s), messages", output_messages(part.name))
        plot("all_quad_" + name.replace(" ", "_") + "_all_quad", name + ", AllQuad: " + str(quads) + " quads, " +
             str(triangles) + " triangles, worst SJ " + str(round(quality, 3)) + ", irregular " + str(irregular),
             face.elements, plot_points(name, face, dx))
        check(total % 2 == 0, "the " + name + " has an odd number of elements around it with AllQuad, " + str(total))
        check(triangles == 0, "the " + name + " keeps " + str(triangles) + " triangles with AllQuad")
        check(quality >= FLOOR, "the " + name + " has a corner of the scaled Jacobian " + str(quality))
        check(len(messages) == 0, "the " + name + " wrote: " + str(messages))
        if total == simple_total:
            check(face.boundary_points() == simple.boundary_points(), "the " + name + " has other boundary nodes " +
                  "with AllQuad than with Simple")
        rows.append((name, s_quads, s_triangles, round(s_quality, 3), quads, round(quality, 3), s_irregular, irregular,
                     round(seconds, 2)))
    print("face | Simple quads | Simple triangles | Simple worst SJ | AllQuad quads | AllQuad worst SJ | "
          "irregular Simple | irregular AllQuad | AllQuad time s")
    for row in rows:
        print(" | ".join(str(v) for v in row))


def check_pinned_odd(parts):
    cases = [("disc", lambda edges: [(edges[round(2 * math.pi * 20, 3)][0], 25)]),
             ("heptagon", lambda edges: [(edge_id, 5) for ids in edges.values() for edge_id in ids])]
    # 7 elements on the smallest hole of the plate with three holes: the free edges are balanced to an even total
    part = parts["three holes"]
    mesh(part, "AllQuad", pins=[(edges_by_length(part)[round(2 * math.pi * 4, 3)][0], 7)])
    face = FaceMesh(part.name)
    print("three holes with 7 elements on its smallest hole: boundary total", len(boundary_edges(face.elements)),
          face.kinds(), all_quad_messages(part.name))
    check(face.kinds()[1] == 0 and len(all_quad_messages(part.name)) == 0,
          "the three holes with 7 elements on a hole is not quads only")
    for name, pins in cases:
        part = parts[name]
        mesh(part, "AllQuad", pins=pins(edges_by_length(part)))
        face = FaceMesh(part.name)
        messages = all_quad_messages(part.name)
        quads, triangles = face.kinds()
        total = len(boundary_edges(face.elements))
        print(name + " with an odd total of", total, ":", quads, "quads,", triangles, "triangle,", messages)
        check(total % 2 == 1 and triangles == 1, "the " + name + " with an odd total keeps " + str(triangles) +
              " triangles")
        check(len(messages) == 1 and "keeps 1 triangle - " in messages[0] and
              "odd number, " + str(total) in messages[0],
              "the " + name + " with an odd total wrote " + str(messages))
        dx = SPACING * SHELLS.index(name)
        plot("all_quad_" + name.replace(" ", "_") + "_odd", name + ", odd total " + str(total) + ": " + str(quads) +
             " quads, 1 triangle", face.elements, plot_points(name, face, dx))


def check_second_order(parts):
    part = parts["house"]
    mesh(part, "AllQuad", second_order=True)
    face = FaceMesh(part.name)
    check(all(count == 8 for count in face.node_counts), "the second order house has elements of other than 8 nodes: " +
          str(set(face.node_counts)))
    print("house: AllQuad of second order,", len(face.elements), "quads of 8 nodes")


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
        """The faces of the elements whose nodes all lie in z = 0."""
        faces = []
        for element in self.elements:
            for face in ([(0, 1, 2, 3), (4, 5, 6, 7)] if len(element) == 8 else [(0, 1, 2), (3, 4, 5)]):
                nodes = [element[k] for k in face]
                if all(abs(self.xyz[node][2]) < 1E-6 for node in nodes):
                    faces.append(nodes)
        return faces


def check_sweeps():
    known = set(part.name for part in pmx.model.geometry.parts)
    pmx.import_file(os.path.join(MODELS, "all_quad_sweep.brep"))
    parts = parts_by_x(SOLIDS, known)
    items = pmx.model.mesh.setup_items
    for i, name in enumerate(SOLIDS):
        part = parts[name]
        selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
        items.add_meshing_parameters("PyTest_Quad_Size", selection, relative_size=False, max_size=MAX_SIZE,
                                     min_size=MAX_SIZE / 10)
        faces = part.get_face_data()
        bottom = [int(faces.ids[k]) for k in range(faces.count)
                  if str(faces.types[k]) == "Plane" and abs(float(faces.centroids[k][2])) < 1E-6]
        check(len(bottom) == 1, "the " + name + " has not one end face in z = 0: " + str(bottom))
        items.add_sweep_mesh("PyTest_Quad_Sweep", pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                                                              {part.id: bottom}),
                             transfinite_three_sided="No")
        start = time.time()
        try:
            report = pmx.model.mesh.generate(part.name)
        finally:
            items.remove(["PyTest_Quad_Sweep", "PyTest_Quad_Size"])
        seconds = time.time() - start
        check(report[part.name] == "", "sweeping the " + name + " reported: " + report[part.name])
        swept = SweptMesh(part.name)
        kinds = swept.kinds()
        source = swept.source()
        messages = output_messages(part.name)
        print("swept " + name + ":", kinds, "source", len(source), "faces in", round(seconds, 2), "s, messages",
              messages)
        uv = dict((node, (swept.xyz[node][0], swept.xyz[node][1])) for f in source for node in f)
        plot("all_quad_source_" + name.replace(" ", "_"), "Source of the swept " + name + ": " + str(kinds), source, uv)
        check(list(kinds.keys()) == ["hexahedron"], "the swept " + name + " must be hexahedra only: " + str(kinds))
        check(len(source) > 0 and len(swept.elements) % len(source) == 0,
              "the hexahedra of the " + name + " are not a multiple of its " + str(len(source)) + " source quads")


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHELLS)
    check_shells(parts)
    check_pinned_odd(parts)
    check_second_order(parts)
    check_sweeps()
    print("TEST PASSED")


main()
