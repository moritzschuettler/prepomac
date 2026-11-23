#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the mirrored meshing of a shell part that is its own mirror image: the element counts of an edge and of its
#image are equal, the grids of its structured faces are laid as mirror images, and the nodes behind a mirror plane are
#placed at the mirror images of those in front of it. The planes are found from the geometry alone, up to three that
#are perpendicular to each other. The edges need not be split alike on both sides of a plane: where the image of a
#vertex - the seam of a circle, the end of an edge inside a straight side - falls inside an edge, the edge gets a node
#there.
#
#  - mirror_faces.brep: one part per shape - a plate 60 x 40 with a bore of r 8, a plate 80 x 50 with four bores of r 4,
#    a tee, a disc of r 20, a slot of two half circles of r 15, an annulus of r 20 and r 10, a plate 60 x 40 with a
#    window 20 x 16, the plate with the bore turned by 30 degrees, the plate with a bore of r 6 off both middle lines,
#    a channel of a bottom 60 x 40 with a bore of r 6 and two walls 20 high, and three shapes that are not split
#    alike: the plate with a vertex 18 mm along one long side and the seam of its bore at 40 degrees, the annulus with
#    the seam of its inner circle at 40 degrees, and the tee with a vertex 15 mm from one end of its top. Every other
#    bore and circle is one closed edge with its seam on the +x side, on the turned plate along its long side.
#    At the element size 5 under the quad-dominant recombination every part but the plate with the bore off its middle
#    lines writes the mirror line with the planes and the surfaces recorded, and is quadrilaterals only, as many as
#    recorded, none with a corner sine below 0.3; across every plane each node has a node as its mirror image, to
#    1E-6 mm, and each element an element. The tee has one plane and the others two, the turned plate along and
#    across its long side, and the channel across all its three faces. The three shapes that are not split alike
#    are mirrored as the same shapes split alike are. The plate with the bore off its middle lines has no plane and
#    writes no mirror line.
#  - The simple recombination and second order elements keep the plate with the bore mirrored across both planes.
#  - mirror_sweep.brep: nine of the shapes extruded by 20 mm and swept from their ends in z = 0. Each writes the mirror
#    line of a sweep with the planes recorded, the plane half way up among them, and all its source and side surfaces
#    mirrored, and is hexahedra only, as many as recorded; across every plane each node of the hexahedra has a node as
#    its mirror image, to 1E-6 mm, and each hexahedron a hexahedron. The plate that is not split alike has two walls
#    under its long side with the vertex and one under the other, and the bores are cylinders closed along a seam:
#    the edge between the two walls and the seams have a line of the mesh as their mirror image.
#  - Pinned counts: 6 elements on one short side of the plate with the bore are not mirrored across the plane between
#    the short sides, which is then not used and named in a line of its own; the mesh stays mirrored across the other
#    plane. 6 elements on both short sides are mirrored: both planes are used and both sides carry 6. 16 elements on
#    one of the four bores are mirrored across neither plane: both are named as not used, no mirror line follows and
#    the mesh is mirrored across neither.
#  The models were made by gmsh's OCC kernel through GmshCommon: plane surfaces of lines and closed circles, the
#  channel glued along the long sides of its bottom, the solids extruded (generator
#  Models/Generators/Make_Mirror_Faces.py).
#
#  PrePoMax.com -f mirror_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_Mirror.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import re
import sys
import clr
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

MODELS = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Models")
MAX_SIZE = 5.0
SPACING = 150.0
HEIGHT = 20.0
TOLERANCE = 1E-6
MIN_CORNER_SINE = 0.3
X = (1.0, 0.0, 0.0)
Y = (0.0, 1.0, 0.0)
Z = (0.0, 0.0, 1.0)
ALONG = (math.cos(math.radians(30.0)), math.sin(math.radians(30.0)), 0.0)
ACROSS = (-math.sin(math.radians(30.0)), math.cos(math.radians(30.0)), 0.0)
# The shape, the point all its planes pass through, their normals, its surfaces and its quadrilaterals
SHAPES = [("plate with bore", (30.0, 20.0, 0.0), [X, Y], 1, 80),
          ("four bores", (40.0, 25.0, 0.0), [X, Y], 1, 134),
          ("tee", (30.0, 0.0, 0.0), [X], 1, 60),
          ("disc", (20.0, 20.0, 0.0), [X, Y], 1, 76),
          ("slot", (30.0, 15.0, 0.0), [X, Y], 1, 80),
          ("annulus", (30.0, 30.0, 0.0), [X, Y], 1, 78),
          ("plate with window", (30.0, 20.0, 0.0), [X, Y], 1, 84),
          ("turned plate", (30.0, 20.0, 0.0), [ALONG, ACROSS], 1, 80),
          ("plate with bore off", None, [], 1, None),
          ("channel", (30.0, 20.0, 0.0), [X, Y], 3, 188),
          ("split plate", (30.0, 20.0, 0.0), [X, Y], 1, 108),
          ("turned seam", (30.0, 30.0, 0.0), [X, Y], 1, 78),
          ("split tee", (30.0, 0.0, 0.0), [X], 1, 60)]
# The solid, the point its planes pass through, their normals, its source and side surfaces and its hexahedra
SOLIDS = [("plate with bore", (30.0, 20.0, 10.0), [X, Y, Z], 6, 320),
          ("four bores", (40.0, 25.0, 10.0), [X, Y, Z], 9, 536),
          ("tee", (30.0, 0.0, 10.0), [X, Z], 9, 240),
          ("disc", (20.0, 20.0, 10.0), [X, Y, Z], 2, 304),
          ("slot", (30.0, 15.0, 10.0), [X, Y, Z], 5, 320),
          ("annulus", (30.0, 30.0, 10.0), [X, Y, Z], 3, 312),
          ("plate with window", (30.0, 20.0, 10.0), [X, Y, Z], 9, 420),
          ("split plate", (30.0, 20.0, 10.0), [X, Y, Z], 7, 432),
          ("turned seam", (30.0, 30.0, 10.0), [X, Y, Z], 3, 312)]
SWEEP_LINE = re.compile(r"Mirror: the part is symmetric across (\d+) plane/s - the mesh of (\d+) of its (\d+) source "
                        r"and side surfaces is mirrored\.")
MIRROR_LINE = re.compile(r"Mirror: the part is symmetric across (\d+) plane/s - the mesh of (\d+) of its (\d+) "
                         r"surfaces is mirrored\.")
UNUSED_LINE = ("Mirror: the element sizes and numbers set on the part are not mirrored across %d of its mirror " +
               "plane/s, so its mesh is not mirrored across %s.")


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_x(shapes, known=()):
    """The geometry parts not in known by the shape they were made from, told apart by where they lie along x."""
    parts = {}
    for part in pmx.model.geometry.parts:
        if part.name not in known:
            parts[shapes[int(round(part.get_bounding_box()[0] / SPACING))][0]] = part
    check(sorted(parts) == sorted(shape[0] for shape in shapes), "the parts were not all found: " + str(sorted(parts)))
    return parts


def mirror_messages(part_name):
    """The mirror lines the last meshing of the part wrote to the output window."""
    form = [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    return [line[line.index("Mirror:"):].rstrip() for line in lines[starts[-1]:] if "Mirror:" in line]


def edges_of_length(part, length):
    data = part.get_edge_data()
    return [(int(edge_id), int(geometry_id)) for edge_id, geometry_id, edge_length in
            zip(data.ids, data.geometry_ids, data.lengths) if abs(float(edge_length) - length) < 1E-6]


def edge_nodes(geometry_id):
    return pmx.create.selection_by_ids('femodel', 'nodes', 'geometry', [geometry_id]).preview().count


def mesh(part, recombine="QuadDominant", second_order=False, pins=()):
    """Meshes the part by a shell item; pins are (edge id, number of elements)."""
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Mirror_Size", pmx.create.selection_by_ids('geometry', 'parts', 'parts',
                                                                                   [part.id]),
                                 relative_size=False, max_size=MAX_SIZE, min_size=MAX_SIZE / 10,
                                 second_order=second_order)
    names = ["PyTest_Mirror_Size"]
    for edge_id, count in pins:
        name = "PyTest_Mirror_Pin_" + str(edge_id)
        items.add_local_mesh_size(name, pmx.create.selection_by_ids('geometry', 'geometry', 'edges',
                                                                    {part.id: [edge_id]}), number_of_elements=count)
        names.append(name)
    items.add_shell_gmsh("PyTest_Mirror", pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id]),
                         algorithm_recombine=recombine)
    names.append("PyTest_Mirror")
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(names)
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])


# Mesh access ----------------------------------------------------------------------------------------------------------
class ShellMesh(object):
    """The elements of a meshed part by their corner nodes, with the coordinates of these nodes."""

    def __init__(self, part_name):
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name]
        check(len(meshed) == 1, "the part " + part_name + " was not meshed")
        xyz = dict(zip([int(i) for i in meshed[0].get_node_ids()], [list(c) for c in meshed[0].get_node_coordinates()]))
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        self.elements = []
        for element_id in meshed[0].get_element_ids():
            i = index[int(element_id)]
            corners = 4 if node_counts[i] in (4, 8) else 3
            self.elements.append(connectivity[offsets[i]:offsets[i] + corners])
        self.xyz = dict((node, xyz[node]) for element in self.elements for node in element)

    def num_of_triangles(self):
        return len([element for element in self.elements if len(element) == 3])

    def min_corner_sine(self):
        worst = 1.0
        for element in self.elements:
            n = len(element)
            for k in range(n):
                p, a, b = self.xyz[element[k]], self.xyz[element[(k + 1) % n]], self.xyz[element[(k - 1) % n]]
                u = [a[c] - p[c] for c in range(3)]
                v = [b[c] - p[c] for c in range(3)]
                cross = [u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0]]
                worst = min(worst, math.sqrt(sum(c * c for c in cross)) /
                            math.sqrt(sum(c * c for c in u) * sum(c * c for c in v)))
        return worst

    def mirror_miss(self, point, normal):
        return mirror_miss(self.xyz, self.elements, point, normal)


def mirror_miss(xyz, elements, point, normal):
    """How far the mirror image of a node lies from the nearest node, at most, and the number of elements whose mirror
    image is no element."""
    cells = {}
    for node, p in xyz.items():
        cells.setdefault(tuple(int(math.floor(c / MAX_SIZE)) for c in p), []).append(node)
    image = {}
    miss = 0.0
    for node, p in xyz.items():
        distance = sum((p[c] - point[c]) * normal[c] for c in range(3))
        q = [p[c] - 2 * distance * normal[c] for c in range(3)]
        cell = tuple(int(math.floor(c / MAX_SIZE)) for c in q)
        best = (4 * MAX_SIZE * MAX_SIZE, node)
        for i in (-1, 0, 1):
            for j in (-1, 0, 1):
                for k in (-1, 0, 1):
                    for other in cells.get((cell[0] + i, cell[1] + j, cell[2] + k), ()):
                        o = xyz[other]
                        d = (o[0] - q[0]) ** 2 + (o[1] - q[1]) ** 2 + (o[2] - q[2]) ** 2
                        if d < best[0]:
                            best = (d, other)
        image[node] = best[1]
        miss = max(miss, math.sqrt(best[0]))
    sets = set(frozenset(element) for element in elements)
    unmatched = len([element for element in elements if frozenset(image[node] for node in element) not in sets])
    return miss, unmatched


class SweptMesh(object):
    """The elements of a meshed solid by their corner nodes, with the coordinates of these nodes."""

    def __init__(self, part_name):
        meshed = [mesh_part for mesh_part in pmx.model.parts if mesh_part.name == part_name]
        check(len(meshed) == 1, "the part " + part_name + " was not meshed")
        xyz = dict(zip([int(i) for i in meshed[0].get_node_ids()], [list(c) for c in meshed[0].get_node_coordinates()]))
        data = pmx.model.elements.get_data()
        offsets = [int(i) for i in data.offsets]
        connectivity = [int(i) for i in data.connectivity]
        node_counts = [int(n) for n in data.node_counts]
        index = dict((int(element_id), i) for i, element_id in enumerate(data.ids))
        self.elements = []
        self.num_of_hexahedra = 0
        for element_id in meshed[0].get_element_ids():
            i = index[int(element_id)]
            corners = {8: 8, 20: 8, 6: 6, 15: 6, 4: 4, 10: 4, 5: 5, 13: 5}[node_counts[i]]
            self.num_of_hexahedra += 1 if corners == 8 else 0
            self.elements.append(connectivity[offsets[i]:offsets[i] + corners])
        self.xyz = dict((node, xyz[node]) for element in self.elements for node in element)


# Checks ---------------------------------------------------------------------------------------------------------------
def plane_point(index, point):
    return (point[0] + SPACING * index, point[1], point[2])


def check_mirrored(name, shell, index, point, normals):
    for normal in normals:
        miss, unmatched = shell.mirror_miss(plane_point(index, point), normal)
        check(miss < TOLERANCE and unmatched == 0, "the mesh of the " + name + " is not mirrored across the plane " +
              "with the normal " + str(normal) + ": " + str(miss) + ", " + str(unmatched))


def check_shapes(parts):
    for index, (name, point, normals, num_of_surfaces, num_of_quads) in enumerate(SHAPES):
        part = parts[name]
        mesh(part)
        shell = ShellMesh(part.name)
        messages = mirror_messages(part.name)
        print("%s: %d quads, %d triangles, worst corner sine %.3f %s" %
              (name, len(shell.elements) - shell.num_of_triangles(), shell.num_of_triangles(), shell.min_corner_sine(),
               messages))
        if len(normals) == 0:
            check(len(messages) == 0, "the " + name + " writes a mirror line: " + str(messages))
            continue
        check(len(messages) == 1 and MIRROR_LINE.match(messages[0]) is not None, "the " + name + " must write one " +
              "mirror line: " + str(messages))
        numbers = [int(number) for number in MIRROR_LINE.match(messages[0]).groups()]
        check(numbers == [len(normals), num_of_surfaces, num_of_surfaces], "the " + name + " must be mirrored across " +
              str(len(normals)) + " planes on its " + str(num_of_surfaces) + " surfaces: " + messages[0])
        check(shell.num_of_triangles() == 0 and len(shell.elements) == num_of_quads, "the " + name + " must have " +
              str(num_of_quads) + " quadrilaterals only: " + str(len(shell.elements)) + ", " +
              str(shell.num_of_triangles()) + " triangles")
        check(shell.min_corner_sine() >= MIN_CORNER_SINE, "the " + name + " has a corner sine of " +
              str(shell.min_corner_sine()))
        check_mirrored(name, shell, index, point, normals)


def check_variants(parts):
    name, point, normals = SHAPES[0][0], SHAPES[0][1], SHAPES[0][2]
    part = parts[name]
    for recombine, second_order in (("Simple", False), ("QuadDominant", True)):
        mesh(part, recombine=recombine, second_order=second_order)
        shell = ShellMesh(part.name)
        messages = mirror_messages(part.name)
        what = "the " + name + " with " + recombine + (" and second order elements" if second_order else "")
        check(len(messages) == 1 and MIRROR_LINE.match(messages[0]) is not None and
              MIRROR_LINE.match(messages[0]).groups() == ("2", "1", "1"), what + " must be mirrored across 2 planes: " +
              str(messages))
        check(shell.num_of_triangles() == 0, what + " has triangles")
        check_mirrored(what[4:], shell, 0, point, normals)


def check_pins(parts):
    name, point = SHAPES[0][0], SHAPES[0][1]
    part = parts[name]
    short_sides = edges_of_length(part, 40.0)
    check(len(short_sides) == 2, "the " + name + " must have two short sides: " + str(short_sides))
    # One short side pinned: the plane between the short sides is not used
    mesh(part, pins=[(short_sides[0][0], 6)])
    shell = ShellMesh(part.name)
    messages = mirror_messages(part.name)
    check(len(messages) == 2 and messages[0] == UNUSED_LINE % (1, "it") and
          MIRROR_LINE.match(messages[1]) is not None and MIRROR_LINE.match(messages[1]).groups() == ("1", "1", "1"),
          "the " + name + " with one short side pinned must be mirrored across one plane and name the other: " +
          str(messages))
    check_mirrored(name + " with one short side pinned", shell, 0, point, [Y])
    check(edge_nodes(short_sides[0][1]) == 7, "the pinned short side must carry 6 elements")
    # Both pinned alike: both planes are used
    mesh(part, pins=[(side[0], 6) for side in short_sides])
    shell = ShellMesh(part.name)
    messages = mirror_messages(part.name)
    check(len(messages) == 1 and MIRROR_LINE.match(messages[0]) is not None and
          MIRROR_LINE.match(messages[0]).groups() == ("2", "1", "1"), "the " + name + " with both short sides pinned " +
          "must be mirrored across 2 planes: " + str(messages))
    check_mirrored(name + " with both short sides pinned", shell, 0, point, [X, Y])
    check([edge_nodes(side[1]) for side in short_sides] == [7, 7], "both pinned short sides must carry 6 elements")
    # One of four bores pinned: neither plane is used
    name, point = SHAPES[1][0], SHAPES[1][1]
    part = parts[name]
    bores = edges_of_length(part, 2 * math.pi * 4.0)
    check(len(bores) == 4, "the " + name + " must have four bores: " + str(bores))
    mesh(part, pins=[(bores[0][0], 16)])
    shell = ShellMesh(part.name)
    messages = mirror_messages(part.name)
    check(messages == [UNUSED_LINE % (2, "them")], "the " + name + " with one bore pinned must name both planes as " +
          "not used: " + str(messages))
    check(edge_nodes(bores[0][1]) == 16, "the pinned bore must carry 16 elements")
    for normal in (X, Y):
        miss, unmatched = shell.mirror_miss(plane_point(1, point), normal)
        check(miss > 0.1, "the " + name + " with one bore pinned is mirrored across a plane that is not used")


def check_sweeps():
    known = set(part.name for part in pmx.model.geometry.parts)
    pmx.import_file(os.path.join(MODELS, "mirror_sweep.brep"))
    parts = parts_by_x(SOLIDS, known)
    items = pmx.model.mesh.setup_items
    for index, (name, point, normals, num_of_surfaces, num_of_hexahedra) in enumerate(SOLIDS):
        part = parts[name]
        selection = pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id])
        items.add_meshing_parameters("PyTest_Mirror_Size", selection, relative_size=False, max_size=MAX_SIZE,
                                     min_size=MAX_SIZE / 10)
        faces = part.get_face_data()
        bottom = [int(faces.ids[i]) for i in range(faces.count)
                  if str(faces.types[i]) == "Plane" and abs(float(faces.centroids[i][2])) < 1E-6]
        check(len(bottom) == 1, "the " + name + " has not one end face in z = 0: " + str(bottom))
        items.add_sweep_mesh("PyTest_Mirror_Sweep", pmx.create.selection_by_ids('geometry', 'geometry', 'surfaces',
                                                                                {part.id: bottom}))
        try:
            report = pmx.model.mesh.generate(part.name)
        finally:
            items.remove(["PyTest_Mirror_Sweep", "PyTest_Mirror_Size"])
        check(report[part.name] == "", "sweeping the " + name + " reported: " + report[part.name])
        solid = SweptMesh(part.name)
        messages = mirror_messages(part.name)
        print("swept %s: %d elements, %d hexahedra %s" % (name, len(solid.elements), solid.num_of_hexahedra, messages))
        check(len(messages) == 1 and SWEEP_LINE.match(messages[0]) is not None, "the swept " + name + " must write " +
              "one mirror line: " + str(messages))
        numbers = [int(number) for number in SWEEP_LINE.match(messages[0]).groups()]
        check(numbers == [len(normals), num_of_surfaces, num_of_surfaces], "the swept " + name + " must be mirrored " +
              "across " + str(len(normals)) + " planes on its " + str(num_of_surfaces) + " source and side surfaces: " +
              messages[0])
        check(solid.num_of_hexahedra == len(solid.elements) == num_of_hexahedra, "the swept " + name + " must have " +
              str(num_of_hexahedra) + " hexahedra only: " + str(len(solid.elements)) + ", " +
              str(solid.num_of_hexahedra) + " hexahedra")
        for normal in normals:
            miss, unmatched = mirror_miss(solid.xyz, solid.elements, plane_point(index, point), normal)
            check(miss < TOLERANCE and unmatched == 0, "the mesh of the swept " + name + " is not mirrored across " +
                  "the plane with the normal " + str(normal) + ": " + str(miss) + ", " + str(unmatched))


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(SHAPES)
    check_shapes(parts)
    check_variants(parts)
    check_pins(parts)
    check_sweeps()
    print("TEST PASSED")


main()
