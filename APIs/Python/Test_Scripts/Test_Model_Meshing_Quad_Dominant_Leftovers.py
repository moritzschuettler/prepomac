#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the faces on which the quad-dominant recombination of a Gmsh shell kept triangles before its last passes: the
#cleanup (QuadCleanup) pairs the triangles it has left by meshing their region again with quads only or by splitting
#the path between them, holds a face with a sharp corner to what that corner allows, GmshQuadMesher projects a face
#whose parameters fold gmsh's own mesh on its mean plane and cuts a face open along the curves inside it, and the
#balance gives such a face an even total and meshes the edges of the faces laid before gmsh as gmsh meshes them for the
#others.
#
#  - quad_dominant_leftovers.brep: faces cut out of the corpus models, one group per part side by side, 100 apart along
#    x (generator Models/Generators/Make_Quad_Dominant_Leftovers.py): the square of gmsh's example simple3
#    cut by an ellipse (a face with a corner of 50 degrees that kept two triangles, a lens face laid before gmsh beside
#    it), two slivers of a B-spline blend with corners of 4 degrees (t20_data.step), a part each, and the boundary of
#    gmsh's example baffles, two boxes whose faces x = 0, y = 1 and y = 0 carry a curve inside them, ending inside the
#    face or on its boundary. The cases that a face cut out alone does not reproduce are in
#    Test_Model_Meshing_Quad_Cleanup.py. Each group at the element size the corpus gave it, QuadDominant against gmsh's
#    own Blossom mesh: every face ends with quads only (the blend slivers one pair at most, which the shape guard keeps,
#    with its message), an even number of elements around it, no fold, no other quad-dominant message, and a worst
#    scaled Jacobian in space not below the lower of gmsh's worst on the face and what its sharpest corner allows (half
#    the sine of a corner below 90 degrees, 0.2 at most), measured against the mean normal of the elements at each node.
#    No two nodes at one point, and the nodes on the curves inside the baffle faces inside the mesh, which they do not
#    tear open.
#  - Pinned odd total: 15, 15, 15 and 16 elements on the edges of the baffle face x = 0 leave one triangle there, with
#    the message that names the odd total 61 and the numbers set on its edges as its cause; a face beside it that the
#    structured faces then hold odd keeps one as well, its message naming them.
#  With the environment variable PMX_QUAD_PLOT_DIR set, the meshes are written into that folder as PNG images.
#
#  PrePoMax.com -f quad_dominant_leftovers.brep -u MM_TON_S_C --script Test_Model_Meshing_Quad_Dominant_Leftovers.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import time
import clr
clr.AddReference('System.Drawing')
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

PLOT_DIR = os.environ.get("PMX_QUAD_PLOT_DIR")
SPACING = 100.0
FLOOR = 0.2
TOLERANCE = 2E-3
# The groups by their place along x, with the element size the corpus gave them
GROUPS = [("simple3", 0.0372), ("blend fold", 1.64), ("blend", 1.64), ("baffles", 0.0612)]
BAFFLES_X = 3 * SPACING
# The blend slivers keep a pair: the split that would join it narrows their elements below what gmsh's triangles
# had, which the shape guard of the passes that add nodes refuses
KEPT_PAIRS = ["blend fold", "blend"]
PLANE_TOLERANCE = 1E-5


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_group():
    """The part of every group, the one of most faces where the faces of a group form several."""
    parts = {}
    names = [name for name, size in GROUPS]
    for part in pmx.model.geometry.parts:
        xs = [c[0] for c in part.get_vertex_data().coordinates]
        name = names[int(round(min(xs) / SPACING))]
        if name not in parts or part.get_face_data().count > parts[name].get_face_data().count: parts[name] = part
    check(sorted(parts) == sorted(names), "the parts were not all found: " + str(sorted(parts)))
    return parts


def output_messages(part_name):
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    return [line[line.index("Quad-dominant meshing:"):] for line in lines[starts[-1]:]
            if "Quad-dominant meshing:" in line]


def mesh(part, recombine, size, pins=()):
    """Meshes the part by a shell item; pins are (edge id, number of elements)."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Leftover_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                     [part.id]),
                                 relative_size=False, max_size=size, min_size=size / 10)
    names = ["PyTest_Leftover_Size"]
    for edge_id, count in pins:
        name = "PyTest_Leftover_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Leftover", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine)
    names.append("PyTest_Leftover")
    start = time.time()
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(names)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])
    return time.time() - start


# Mesh access ----------------------------------------------------------------------------------------------------------
class PartMesh(object):
    """The elements of every face of a meshed part by their corner nodes, with the coordinates of the nodes."""

    def __init__(self, part):
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part.name]
        check(len(meshed) == 1, "the part " + part.name + " was not meshed")
        self.xyz = dict(zip([int(i) for i in meshed[0].get_node_ids()],
                            [list(c) for c in meshed[0].get_node_coordinates()]))
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        part_elements = set(int(i) for i in meshed[0].get_element_ids())
        face_data = part.get_face_data()
        self.faces = {}
        for face_id, geometry_id in zip([int(i) for i in face_data.ids], [int(i) for i in face_data.geometry_ids]):
            ids = set(int(i) for i in pmx.create.selection_by_ids('femodel', 'elements', 'geometry',
                                                                  [geometry_id]).preview().ids) & part_elements
            elements = []
            for element_id in sorted(ids):
                i = index[element_id]
                corners = 4 if node_counts[i] in (4, 8) else 3
                elements.append(connectivity[offsets[i]:offsets[i] + corners])
            self.faces[face_id] = elements


def edge_uses(elements):
    uses = {}
    for element in elements:
        for k in range(len(element)):
            edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return uses


def cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def node_normals(xyz, elements):
    """The mean of the unit normals of the corners at every node, None where they cancel out."""
    sums = {}
    for element in elements:
        n = len(element)
        for c in range(n):
            p, a, b = xyz[element[c]], xyz[element[(c + 1) % n]], xyz[element[(c - 1) % n]]
            product = cross([a[k] - p[k] for k in range(3)], [b[k] - p[k] for k in range(3)])
            length = math.sqrt(sum(v * v for v in product))
            total = sums.setdefault(element[c], [0.0, 0.0, 0.0])
            for k in range(3):
                total[k] += product[k] / length if length > 0 else 0.0
    normals = {}
    for node, s in sums.items():
        length = math.sqrt(sum(c * c for c in s))
        normals[node] = [c / length for c in s] if length > 0 else None
    return normals


def worst_scaled_jacobian(xyz, elements):
    """The smallest sine of a corner in space against the mean normal at its node (a triangle scaled so that 60
    degrees count as 1), negative where an element folds."""
    normals = node_normals(xyz, elements)
    worst = 1.0
    for element in elements:
        n = len(element)
        for c in range(n):
            p, a, b = xyz[element[c]], xyz[element[(c + 1) % n]], xyz[element[(c - 1) % n]]
            e1 = [a[k] - p[k] for k in range(3)]
            e2 = [b[k] - p[k] for k in range(3)]
            normal = normals[element[c]] or [0.0, 0.0, 1.0]
            length = math.sqrt(sum(v * v for v in e1) * sum(v * v for v in e2))
            sine = sum(cross(e1, e2)[k] * normal[k] for k in range(3)) / length if length > 0 else -1.0
            worst = min(worst, sine * (2 / math.sqrt(3) if n == 3 else 1.0))
    return worst


def corner_floor(xyz, elements):
    """What the sharpest corner of the boundary allows a quad in it: half the sine of a corner below 90 degrees between
    the boundary segments at a node, FLOOR at most."""
    normals = node_normals(xyz, elements)
    outgoing = {}
    incoming = {}
    uses = edge_uses(elements)
    for element in elements:
        for k in range(len(element)):
            a, b = element[k], element[(k + 1) % len(element)]
            if uses[tuple(sorted((a, b)))] == 1:
                outgoing.setdefault(a, []).append(b)
                incoming.setdefault(b, []).append(a)
    floor = FLOOR
    for node, nexts in outgoing.items():
        if len(nexts) != 1 or len(incoming.get(node, [])) != 1 or normals[node] is None: continue
        p, a, b = xyz[node], xyz[nexts[0]], xyz[incoming[node][0]]
        e1 = [a[k] - p[k] for k in range(3)]
        e2 = [b[k] - p[k] for k in range(3)]
        angle = math.atan2(sum(cross(e1, e2)[k] * normals[node][k] for k in range(3)),
                           sum(e1[k] * e2[k] for k in range(3)))
        if 0 < angle < 0.5 * math.pi: floor = min(floor, 0.5 * math.sin(angle))
    return floor


# Plots ----------------------------------------------------------------------------------------------------------------
def plot(name, xyz, elements):
    """The elements projected on their mean plane as a PNG image, quads blue, triangles orange."""
    if not PLOT_DIR or not elements:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    normal = [0.0, 0.0, 0.0]
    for element in elements:
        p = [xyz[n] for n in element]
        product = cross([p[2][k] - p[0][k] for k in range(3)], [p[-1][k] - p[1][k] for k in range(3)])
        for k in range(3): normal[k] += product[k]
    length = math.sqrt(sum(c * c for c in normal)) or 1.0
    normal = [c / length for c in normal]
    u = [1.0, 0.0, 0.0] if abs(normal[0]) < 0.9 else [0.0, 1.0, 0.0]
    along = sum(u[k] * normal[k] for k in range(3))
    u = [u[k] - along * normal[k] for k in range(3)]
    length = math.sqrt(sum(c * c for c in u))
    u = [c / length for c in u]
    v = cross(normal, u)
    used = set(n for e in elements for n in e)
    uv = dict((n, (sum(xyz[n][k] * u[k] for k in range(3)), sum(xyz[n][k] * v[k] for k in range(3)))) for n in used)
    us = [p[0] for p in uv.values()]
    vs = [p[1] for p in uv.values()]
    scale = 900.0 / max(max(us) - min(us), max(vs) - min(vs))
    width, height = int((max(us) - min(us)) * scale + 40), int((max(vs) - min(vs)) * scale + 70)
    bitmap = Bitmap(max(width, 200), height)
    graphics = Graphics.FromImage(bitmap)
    graphics.Clear(Color.White)
    pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
    quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
    triangle_brush = SolidBrush(Color.FromArgb(246, 190, 160))
    for element in elements:
        polygon = Array[PointF]([PointF(float(20 + (uv[n][0] - min(us)) * scale),
                                        float(height - 20 - (uv[n][1] - min(vs)) * scale)) for n in element])
        graphics.FillPolygon(quad_brush if len(element) == 4 else triangle_brush, polygon)
        graphics.DrawPolygon(pen, polygon)
    graphics.DrawString(name, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, name.replace(" ", "_") + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


# Checks ---------------------------------------------------------------------------------------------------------------
def faces_of(part, recombine, size):
    seconds = mesh(part, recombine, size)
    return PartMesh(part), seconds


def check_part(name, part, size):
    blossom, blossom_seconds = faces_of(part, "Blossom", size)
    quad_dominant, seconds = faces_of(part, "QuadDominant", size)
    messages = output_messages(part.name)
    check(all("the cleanup found no good way to join them into quads" in message for message in messages) and
          len(messages) <= (1 if name in KEPT_PAIRS else 0),
          name + ": the quad-dominant meshing wrote " + str(messages))
    for face_id in sorted(quad_dominant.faces):
        elements = quad_dominant.faces[face_id]
        quads = sum(1 for e in elements if len(e) == 4)
        triangles = len(elements) - quads
        total = sum(1 for count in edge_uses(elements).values() if count == 1)
        worst = worst_scaled_jacobian(quad_dominant.xyz, elements)
        gmsh_elements = blossom.faces[face_id]
        gmsh_worst = worst_scaled_jacobian(blossom.xyz, gmsh_elements)
        gmsh_triangles = sum(1 for e in gmsh_elements if len(e) == 3)
        floor = min(corner_floor(quad_dominant.xyz, elements), gmsh_worst)
        print("%-10s face %2d  Blossom: %4d elements, %3d triangles, worst %.3f | QuadDominant: %4d quads, "
              "%d triangles, total %3d, worst %.3f, floor %.3f" % (name, face_id, len(gmsh_elements), gmsh_triangles,
                                                                   gmsh_worst, quads, triangles, total, worst, floor))
        plot(name + " face " + str(face_id) + " Blossom", blossom.xyz, gmsh_elements)
        plot(name + " face " + str(face_id) + " QuadDominant", quad_dominant.xyz, elements)
        check(triangles <= (2 if name in KEPT_PAIRS else 0),
              name + " face " + str(face_id) + " keeps " + str(triangles) + " triangles")
        check(total % 2 == 0, name + " face " + str(face_id) + " has an odd total " + str(total))
        check(worst > 0, name + " face " + str(face_id) + " folds: " + str(worst))
        check(worst >= floor - TOLERANCE, name + " face " + str(face_id) + " has a worst scaled Jacobian " +
              str(round(worst, 4)) + " below " + str(round(floor, 4)))
    print("%-10s meshed in %.2f s with QuadDominant, %.2f s with Blossom" % (name, seconds, blossom_seconds))
    return quad_dominant


def on_box_plane(point):
    """Whether the point lies in a plane of a face of the two boxes of the baffles."""
    return (any(abs(point[0] - BAFFLES_X - x) < PLANE_TOLERANCE for x in (0.0, 1.0, 2.0)) or
            any(abs(point[k] - c) < PLANE_TOLERANCE for k in (1, 2) for c in (0.0, 1.0)))


def check_inner_curves(part, part_mesh):
    """Every node once, and the nodes on every curve inside a face of the boxes - an edge of a baffle shorter than half
    the side of a box lying in a plane of a box face - inside the mesh, which the curve does not tear open."""
    points = [tuple(round(c, 9) for c in p) for p in part_mesh.xyz.values()]
    check(len(points) == len(set(points)), "baffles: two nodes lie at one point")
    elements = [e for face in part_mesh.faces.values() for e in face]
    boundary = set(n for edge, count in edge_uses(elements).items() if count == 1 for n in edge)
    edge_data = part.get_edge_data()
    curves = [geometry_id for geometry_id, length, centroid in zip([int(i) for i in edge_data.geometry_ids],
                                                                    [float(l) for l in edge_data.lengths],
                                                                    [list(c) for c in edge_data.centroids])
              if length < 0.45 and on_box_plane(centroid)]
    check(len(curves) >= 3, "baffles: " + str(len(curves)) + " curves inside the faces of the boxes")
    for curve in curves:
        nodes = set(int(i) for i in pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [curve]).preview().ids)
        inner = [node for node in nodes if node not in boundary]
        check(len(nodes) > 2 and len(inner) >= len(nodes) - 2, "baffles: a curve inside a face carries " +
              str(len(nodes)) + " nodes of which " + str(len(inner)) + " are inside the mesh")
    print("baffles: %d curves inside the faces of the boxes, their nodes inside the mesh" % len(curves))


# Main -----------------------------------------------------------------------------------------------------------------
parts = parts_by_group()
for name, size in GROUPS:
    part_mesh = check_part(name, parts[name], size)
    if name == "baffles": check_inner_curves(parts[name], part_mesh)
# A total pinned odd keeps one triangle, and the message names its cause: the sides of the face x = 0 of the first box
part = parts["baffles"]
edge_data = part.get_edge_data()
sides = [edge_id for edge_id, length, centroid in zip([int(i) for i in edge_data.ids],
                                                      [float(l) for l in edge_data.lengths],
                                                      [list(c) for c in edge_data.centroids])
         if abs(length - 1.0) < 1E-9 and abs(centroid[0] - BAFFLES_X) < PLANE_TOLERANCE]
check(len(sides) == 4, "the baffle face x = 0 has " + str(len(sides)) + " sides of length 1")
mesh(part, "QuadDominant", 0.0612, pins=list(zip(sides, [15, 15, 15, 16])))
pinned = PartMesh(part)
triangles = sum(1 for face in pinned.faces.values() for e in face if len(e) == 3)
messages = output_messages(part.name)
print("pinned odd total:", triangles, "triangle(s);", messages)
face_data = part.get_face_data()
face_x0 = [int(i) for i, c in zip(face_data.ids, face_data.centroids) if abs(list(c)[0] - BAFFLES_X) < PLANE_TOLERANCE]
check(len(face_x0) == 1, "the baffle face x = 0 was not found")
check(triangles == len(messages) and triangles >= 1, "the pinned odd totals keep " + str(triangles) +
      " triangles and write " + str(len(messages)) + " messages")
check(all("keeps 1 triangle - the elements around its boundary add up to an odd number, " in message and
          "which the numbers of elements set on its edges" in message for message in messages),
      "a pinned odd total is not named with its cause: " + str(messages))
check(any(message.startswith("Quad-dominant meshing: Surface " + str(face_x0[0]) + " of ") and
          "an odd number, 61, " in message for message in messages),
      "the face x = 0 is not named with its odd total of 61: " + str(messages))
print("TEST PASSED")
