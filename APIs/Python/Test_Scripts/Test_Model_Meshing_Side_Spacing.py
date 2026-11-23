#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks opposite-side node spacing on structured shell faces. Split plates and strips cover pinned counts, shared
#edges, differently sized sides, and the grid and free-mesh choices. A cylinder, ring sector and cone test curved
#sides. Two faces cut from extruded solids check both orientations. L and U parts cover submap alignment and
#propagation. Two pins on a sharp L give right-angled grids. On the filleted L, the quality veto restores the
#original boundary and grid. Other controls cover an unchanged hole and different-shaped sides, a retained
#low-gain chain, and edges held fixed by thin neighbours.
#Below 5 degrees, a split plate and the user's rectangular frame scaled by 1/11 can align. Fixed split vertices
#or a shared edge beside a free face can prevent alignment and leave the whole mesh unchanged. A second frame
#has a wall a third of the default element size. The GmshCommon generator is
#Models/Generators/Make_Side_Spacing.py. Closed shells from the fillet3 and twist examples cover the removal
#of refused submaps and the guard for a free face beside a chain.
#A strip shares its pin pattern with a plate through a round hole at both sides of the 5-degree threshold.
#A mixed-hole plate aligns the cornered hole with its outer edges while both cases keep their round rims fixed.
#
#  PrePoMax.com -f side_spacing.brep -u MM_TON_S_C --script Test_Model_Meshing_Side_Spacing.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import hashlib
import math
import time
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

EPS = 1.e-6
MAX_SIZE = 5.0


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def edge_selection(part, edge_id):
    return pmx.create.selection_by_ids("geometry", "geometry", "edges", {part.id: [edge_id]})


def messages(part):
    form = next(form for form in Application.OpenForms if form.GetType().Name == "FrmMain")
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    start = max(i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part.name))
    return [line.strip() for line in lines[start:]
            if any(kind in line for kind in ("Transfinite:", "Quad-dominant meshing:", "Submapping:"))]


def mesh(part, pins, recombine, second_order, algorithm_2d, max_size=MAX_SIZE, min_size=0.5):
    items = pmx.model.mesh.setup_items
    names = ["Spacing_Size", "Spacing_Shell"]
    selection = pmx.create.selection_by_ids("geometry", "parts", "parts", [part.id])
    items.add_meshing_parameters(names[0], selection, relative_size=False, max_size=max_size, min_size=min_size,
                                 second_order=second_order)
    selection = pmx.create.selection_by_ids("geometry", "geometry", "parts", [part.id])
    items.add_shell_gmsh(names[1], selection, algorithm_2d=algorithm_2d, algorithm_recombine=recombine)
    for edge_id, count in pins:
        names.append("Spacing_Pin" + str(len(names)))
        items.add_local_mesh_size(names[-1], edge_selection(part, edge_id), number_of_elements=count)
    start = time.perf_counter()
    try:
        report = pmx.model.mesh.generate(part.name)
        elapsed = time.perf_counter() - start
        output = messages(part)
    finally:
        items.remove(names)
    check(report[part.name] == "", part.name + " reported " + report[part.name])
    return elapsed, output


class FaceMesh:
    def __init__(self, part, project):
        meshed = pmx.model.parts[part.name]
        self.xyz = dict(zip(map(int, meshed.get_node_ids()), map(tuple, meshed.get_node_coordinates())))
        data = pmx.model.elements.get_data()
        offsets = list(map(int, data.offsets))
        connectivity = list(map(int, data.connectivity))
        indices = {int(e): i for i, e in enumerate(data.ids)}
        self.elements = []
        self.node_counts = set()
        for element_id in meshed.get_element_ids():
            i = indices[int(element_id)]
            count = int(data.node_counts[i])
            self.node_counts.add(count)
            self.elements.append(connectivity[offsets[i]:offsets[i] + (4 if count in (4, 8) else 3)])
        self.uv = {node: project(self.xyz[node]) for element in self.elements for node in element}

    def row(self, v):
        return sorted(u for u, y in self.uv.values() if abs(y - v) < EPS)

    def worst_corner(self, elements=None, normal=None):
        sines = []
        for element in self.elements if elements is None else elements:
            for i, node in enumerate(element):
                point = self.xyz[node]
                a, b, c = (x - y for x, y in zip(self.xyz[element[(i + 1) % len(element)]], point))
                d, e, f = (x - y for x, y in zip(self.xyz[element[i - 1]], point))
                nx, ny, nz = normal(point) if normal else (0, 0, 1)
                length = math.hypot(a, b, c) * math.hypot(d, e, f)
                cross = (b * f - c * e, c * d - a * f, a * e - b * d)
                sine = (nx * cross[0] + ny * cross[1] + nz * cross[2]) / length if length else 0
                sines.append(sine * 2 / math.sqrt(3) if len(element) == 3 else sine)
        return min(sines) if sum(sines) >= 0 else -max(sines)

    def fingerprint(self, subset=None):
        points = self.xyz.values() if subset is None else (self.xyz[n] for n in {n for e in subset for n in e})
        nodes = sorted(tuple(float(c).hex() for c in p) for p in points)
        elements = sorted(tuple(sorted(self.xyz[node] for node in e))
                          for e in (self.elements if subset is None else subset))
        return hashlib.sha256(repr((nodes, elements)).encode("ascii")).hexdigest()


def edge_ids(part, curved):
    data = part.get_edge_data()
    ids = list(map(int, data.ids))
    lengths = list(map(float, data.lengths))
    short_length = 50.0 * math.radians(36.0) if curved else 30.0
    short = [edge_id for edge_id, length in zip(ids, lengths) if abs(length - short_length) < 0.1]
    long = [edge_id for edge_id, length in zip(ids, lengths) if abs(length - 60.0) < 0.1]
    return short, long


def intervals(row, a, b):
    return [u for u in row if u >= a - EPS and u <= b + EPS]


def check_count(row, a, b, count):
    nodes = intervals(row, a, b)
    check(len(nodes) == count + 1, "expected %d elements on [%.3f, %.3f], got %d" % (count, a, b, len(nodes) - 1))
    minimum = (b - a) / (3 * count)
    check(all(q - p >= minimum - EPS for p, q in zip(nodes, nodes[1:])), "an edge interval fell below the bound")


def max_lean(first, second, height):
    check(len(first) == len(second), "opposite sides have different counts")
    return math.degrees(math.atan2(max(abs(a - b) for a, b in zip(first, second)), height))


def planar(x):
    return lambda p: (p[0] - x, p[1])


def cylinder(p):
    return (50.0 * (math.atan2(p[1], p[0] - 600.0) - math.radians(20.0)), p[2])


def radial_normal(x, slope):
    def normal(p):
        radius = math.hypot(p[0] - x, p[1])
        magnitude = math.hypot(1, slope)
        return ((p[0] - x) / (radius * magnitude), p[1] / (radius * magnitude), -slope / magnitude)
    return normal


def case(label, key, bottom_pin=0, top_pin=0, recombine="Simple", second_order=False, algorithm_2d="Automatic",
         edge_pins=()):
    part = parts[key]
    curved = key == 600
    pins = []
    short, long = edge_ids(part, curved)
    if bottom_pin:
        check(len(short) == 1, "the split edge is missing or ambiguous on " + label)
        pins.append((short[0], bottom_pin))
    if top_pin:
        check(len(long) == 1, "the opposite split edge is missing or ambiguous on " + label)
        pins.append((long[0], top_pin))
    data = part.get_edge_data()
    for y, wanted_length, count in edge_pins:
        edges = [int(edge_id) for edge_id, length, center in zip(data.ids, data.lengths, data.centroids)
                 if abs(length - wanted_length) < EPS and abs(center[1] - y) < EPS]
        check(len(edges) == 1, "a pinned edge is missing or ambiguous on " + label)
        pins.append((edges[0], count))
    elapsed, output = mesh(part, pins, recombine, second_order, algorithm_2d)
    grid = FaceMesh(part, cylinder if curved else planar(key))
    height = 12 if key == 750 else 80 if key in (300, 450) else 40
    rows = [grid.row(v) for v in ([0, 40, 80] if height == 80 else [0, height])]
    lean = (fractions_lean(rows, height) if key == 1050 else
            max(max_lean(a, b, height / (len(rows) - 1)) for a, b in zip(rows, rows[1:])))
    worst = grid.worst_corner(normal=radial_normal(600, 0) if curved else None)
    print("%-21s elements=%-4d orders=%-8s sides=%-12s lean=%7.3f worst=%.6f time=%.4fs" %
          (label, len(grid.elements), sorted(grid.node_counts), [len(r) - 1 for r in rows], lean, worst, elapsed))
    for line in output:
        print(line)
    return grid, rows, lean, worst, output


def fractions_lean(rows, height):
    first, second = rows
    a = first[-1] - first[0]
    b = second[-1] - second[0]
    first = [(u - first[0]) / a * (a + b) / 2 for u in first]
    second = [(u - second[0]) / b * (a + b) / 2 for u in second]
    return max_lean(first, second, height)


def check_baseline(label, grid):
    fingerprints = {
        "different shape": "f38987573725471903b8a480eb24d1d36fd215761ad774c5c94b054f6bcdc2af",
        "filleted L quality veto": "b7be82287d52252813f54bb464f2d1d9882e9982e4f58fd4207d4b2b05ed8010",
        "hole plate": "9a70daade7aa049577d0d3137d71f2e4f2fc79c2a531b59c86b56386162033b0",
        "below threshold with fixed vertices": "6112b70cbf9f86544be1a2e3e3ec852c4a5f64c8844fd6afb359ea1e46fbc850",
        "below threshold with a free neighbour": "93e59d09cfd2a4a72d050f8901bf9414d283bbfbba99ff2605582bb5bf5ac22d",
        "all edges fixed": "f4db3849465e14aab4aa14e0e43a39e7a916f375e608f28caea3fd3bc9aeafe3",
    }
    check(grid.fingerprint() == fingerprints[label], label + " differs from its baseline mesh")


parts = {}
for part in pmx.model.geometry.parts:
    vertices = list(part.get_vertex_data().coordinates)
    key = int(round(min(p[0] for p in vertices) / 150)) * 150
    check(key not in parts, "the two faces of a strip did not import into one part")
    parts[key] = part
check(sorted(parts) == list(range(0, 4651, 150)), "model parts were not all found")

for key in (0, 150, 900):
    grid, rows, lean, worst, output = case("unassigned " + str(key), key)
    print("  fingerprint " + grid.fingerprint())
    check(grid.node_counts == {4} and len(grid.elements) == 160 and lean < 0.1 and not output,
          "the unassigned plate changed")

for count, total in ((12, 26), (3, 20)):
    grid, rows, lean, worst, output = case("split pin " + str(count), 0, count)
    check(grid.node_counts == {4} and len(grid.elements) == 8 * total and not output, "expected a grid of quads")
    check_count(rows[0], 0, 30, count)
    check_count(rows[0], 30, 100, total - count)
    check_count(rows[1], 0, 100, total)
    check(lean < 0.1 and worst > 0.99999, "opposite sides of the split plate are skewed")

grid, rows, lean, worst, output = case("shared edge", 300, 12)
check(grid.node_counts == {4} and len(grid.elements) == 416 and not output, "the strip did not stay structured")
check(lean < 0.1 and worst > 0.99999, "the spacing did not reach the opposite edge of the second face")
for row in rows[1:]:
    check_count(row, 0, 100, 26)

grid, rows, lean, worst, output = case("shared edge conflict", 450, 12, 6)
check(grid.node_counts == {4} and len(grid.elements) == 416 and not output, "the strip did not stay structured")
check_count(rows[0], 0, 30, 12)
check_count(rows[0], 30, 100, 14)
check_count(rows[1], 0, 100, 26)
check_count(rows[2], 0, 60, 6)
check_count(rows[2], 60, 100, 20)
check(rows[0][6] < rows[1][6] < rows[2][6] and rows[0][12] < rows[1][12] < rows[2][12],
      "the shared side did not blend the two spacing patterns")

for key, label in ((150, "two splits kept"), (1500, "solid face 1500"), (1650, "solid face 1650")):
    grid, rows, lean, worst, output = case(label, key, 12, 6)
    check(grid.node_counts == {4} and len(grid.elements) == 208 and not output,
          label + " did not keep its structured grid")
    check(abs(worst - 0.752577) < EPS and abs(lean - 41.186) < 0.001,
          label + " did not keep the matched spacing")


short_settings = (("Simple", False), ("QuadDominant", False), ("Simple", True), ("BlossomFullQuad", False))
for recombine, second_order in short_settings:
    label = "short " + recombine + (" order 2" if second_order else "")
    grid, rows, lean, worst, output = case(label, 750, 12, 6, recombine, second_order)
    check(any("is meshed with its grid" in line and "and the free mesh is no better" in line for line in output),
          label + " did not report that it kept the grid")
    check(not any("is meshed free" in line for line in output), label + " also reported keeping the free mesh")
    check_count(rows[0], 0, 30, 12)
    check_count(rows[1], 0, 60, 6)
    check(grid.node_counts == ({8} if second_order else {4}) and len(grid.elements) == 78,
          label + " did not retain the expected structured quads")
    check(abs(worst - 0.324324) < EPS and abs(lean - 71.075) < 0.001,
          label + " did not retain the expected matched grid corner")

grid, rows, lean, worst, output = case("cylinder", 600, 12)
check(grid.node_counts == {4} and lean < 0.1 and worst > 0.99 and not output, "the cylindrical grid is skewed")
check_count(rows[0], 0, 50.0 * math.radians(36), 12)
check(all(abs(math.hypot(p[0] - 600.0, p[1]) - 50.0) < EPS for p in grid.xyz.values()),
      "a mesh node left the cylinder")

grid, rows, lean, worst, output = case("second order", 0, 12, second_order=True)
check(grid.node_counts == {8} and len(grid.elements) == 208 and lean < 0.1 and not output, "second-order quads changed")
check_count(rows[0], 0, 30, 12)

grid, rows, lean, worst, output = case("triangles", 0, 12, recombine="None")
check(grid.node_counts == {3} and len(grid.elements) == 416 and lean < 0.1 and not output, "triangle grid changed")
check_count(rows[0], 0, 30, 12)

grid, rows, lean, worst, output = case("quasi structured", 0, 12, algorithm_2d="QuasiStructuredQuad")
check(grid.node_counts == {4} and len(grid.elements) == 260 and not output, "quasi-structured quads changed")
check([len(row) - 1 for row in rows] == [26, 26] and lean < 0.1,
      "quasi-structured meshing lost the matched boundary spacing")
check(worst > 0.5, "quasi-structured meshing left a poor corner")
grid, rows, lean, worst, output = case("trapezoid", 1050, 12)
print("  trapezoid fingerprint " + grid.fingerprint())
check(grid.node_counts == {4} and not output and lean < 0.1, "the trapezoid's side fractions differ")
check_count(rows[0], 0, 30, 12)

free_neighbour_baseline = {"Simple": "a03b1408dfefa78f36834525caad8d7488341a4fd4ca36a3dbbc43d474f65d97",
                           "QuadDominant": "c6b8d290e1c4343c64af304fc2b12d0309a6dfa70c3d60e96d9aeffb39016929"}
for recombine in ("Simple", "QuadDominant"):
    grid, rows, lean, worst, output = case("free neighbour " + recombine, 1200, 12, recombine=recombine)
    plate = [element for element in grid.elements if max(grid.uv[node][1] for node in element) <= 40 + EPS]
    neighbour = [element for element in grid.elements if element not in plate]
    check(len(plate) == 208 and all(len(element) == 4 for element in plate), "the plate lost its grid")
    check(lean < 0.1 and grid.worst_corner(plate) > 0.99999, "the neighbour held the plate's shared edge fixed")
    check(neighbour and grid.worst_corner(neighbour) > 0, "the free neighbour has no valid mesh")
    check(grid.fingerprint() == free_neighbour_baseline[recombine], "a passing free-face trial changed the mesh")
    if recombine == "QuadDominant":
        uses = {}
        for element in neighbour:
            for i, node in enumerate(element):
                edge = tuple(sorted((node, element[i - 1])))
                uses[edge] = uses.get(edge, 0) + 1
        check(sum(count == 1 for count in uses.values()) % 2 == 0, "the free neighbour has an odd boundary")
        check(all(len(e) == 4 for e in neighbour) or any("keeps" in line and " - " in line for line in output),
              "the quad-dominant pass did not explain its remaining triangles")

pins = ((0, 30, 4), (0, 170, 4), (40, 170, 4), (40, 30, 4))
grid, rows, lean, worst, output = case("wide QuadDominant", 1350, recombine="QuadDominant", edge_pins=pins)
check(any("is meshed free" in line and "lean its grid" in line for line in output), "the wide free mesh was not kept")
check(grid.node_counts == {4} and len(grid.elements) == 45 and abs(worst - 0.525039) < EPS,
      "the wide plate did not retain the expected free quads")
check(worst > math.cos(math.radians(lean)) and lean > 60, "the wide plate has a worse corner than its grid")


def fingerprint_rows(grid, ids):
    """Record the coordinates of a boundary that must stay in place."""
    nodes = sorted(tuple(float(c).hex() for c in grid.xyz[node]) for node in set(ids))
    return hashlib.sha256(repr(nodes).encode("ascii")).hexdigest()


def boundary_nodes(part, predicate):
    data = part.get_edge_data()
    result = set()
    for geometry_id, length, center, kind in zip(data.geometry_ids, data.lengths, data.centroids, data.types):
        if predicate(float(length), list(center), str(kind)):
            result.update(map(int, pmx.create.selection_by_ids(
                "femodel", "nodes", "geometry", [int(geometry_id)]).preview().ids))
    return result


def measure(label, key, pins, read_lean, project=None, normal=None):
    part = parts[key]
    edge_data = part.get_edge_data()
    exact = []
    for length, y, count in pins:
        ids = [int(edge_id) for edge_id, value, center in zip(edge_data.ids, edge_data.lengths, edge_data.centroids)
               if abs(float(value) - length) < EPS and (y is None or abs(center[1] - y) < EPS)]
        check(len(ids) == 1, "a pinned edge is missing or ambiguous on " + label)
        exact.append((ids[0], count))
    elapsed, output = mesh(part, exact, "Simple", False, "Automatic")
    grid = FaceMesh(part, project or planar(key))
    lean = read_lean(grid)
    worst = grid.worst_corner(normal=normal)
    print("%-21s elements=%-4d orders=%-8s sides=[] lean=%7.3f worst=%.6f time=%.4fs" %
          (label, len(grid.elements), sorted(grid.node_counts), lean, worst, elapsed))
    print("  %s fingerprint %s" % (label, grid.fingerprint()))
    for line in output:
        print(line)
    return grid, lean, worst, output


grid, lean, worst, output = measure("5 percent sides", 1800, ((30, 0, 12),),
                                     lambda g: fractions_lean([g.row(0), g.row(40)], 40))
check(lean < 0.1 and grid.node_counts == {4}, "the different-length sides lost their matched spacing")


def angular_project(x, conical):
    return lambda p: (math.atan2(p[1], p[0] - x), p[2] if conical else math.hypot(p[0] - x, p[1]))


def angular_lean(grid, a, b):
    return math.degrees(max(abs(p - q) for p, q in zip(grid.row(a), grid.row(b))))


for label, key, a, b in (("ring sector", 1950, 40, 60), ("cone patch", 2100, 0, 40)):
    normal = radial_normal(key, 0.5) if key == 2100 else None
    grid, lean, worst, output = measure(label, key, ((40 * math.pi / 6, None, 12),),
                                       lambda g: angular_lean(g, a, b), angular_project(key, key == 2100), normal)
    check(len(grid.row(a)) == len(grid.row(b)) and len(grid.row(a)) > 2, label + " has unequal angular counts")
    check(lean < 0.1 and grid.node_counts == {4}, label + " has skewed radial lines")
    angles = grid.row(a)
    check(all(min(abs(theta - angle) for angle in angles) < math.radians(0.1) for theta, _ in grid.uv.values()),
          label + " has an interior node off its generator")


def notch_lean(grid):
    tags = boundary_nodes(parts[2250], lambda length, p, kind: p[1] < 14)
    return max_lean(sorted(grid.uv[tag][0] for tag in tags), grid.row(55), 55)


grid, lean, worst, output = measure("different shape", 2250, ((10, 13, 6),), notch_lean)
check_baseline("different shape", grid)


def l_lean(grid):
    i = len(grid.row(80)) - 1
    return math.degrees(math.atan2(abs(grid.row(0)[i] - 30), 40))


def u_lean(grid):
    top = grid.row(80)
    left = len([x for x in top if x < 50])
    right = len(top) - left
    bottom = grid.row(0)
    return math.degrees(math.atan2(max(abs(bottom[left - 1] - 30), abs(bottom[-right] - 70)), 40))


submaps = (("L", 2400, 70, 8, l_lean, 256, 0.504404720), ("U", 2550, 40, 16, u_lean, 304, 0.697697),
           ("L and rectangle", 2700, 70, 6, l_lean, 432, 0.355446977))
for label, key, length, count, read_lean, elements, before_worst in submaps:
    grid, lean, worst, output = measure(label, key, ((length, 40, count),), read_lean)
    check(grid.node_counts == {4} and len(grid.elements) == elements and not output,
          label + " lost its submapped grid")
    check(lean < 0.1, label + " did not align a reentrant corner")
    check(worst + EPS >= before_worst, label + " has a worse corner than its before-exe grid")
    if key == 2700:
        check(max_lean(grid.row(-40), grid.row(0), 40) < 0.1, "the L did not pass its spacing to the far edge")


for pin, quads in ((21, 297), (42, 492)):
    label = "right-angled L with %d on its 70 mm edge" % pin
    grid, lean, worst, output = measure(label, 2400, ((70, 40, pin),), l_lean)
    check(grid.node_counts == {4} and len(grid.elements) == quads and not output, label + " lost its submapped grid")
    check(lean < 0.1 and abs(worst - 1.0) < EPS, label + " did not align its boundary and interior")
    check_count(grid.row(40), 30, 100, pin)

grid, lean, worst, output = measure("filleted L", 2850, ((70, 40, 21),),
                                     lambda g: math.degrees(math.atan2(min(abs(x - 30) for x in g.row(0)), 40)))
check(grid.node_counts == {4} and not output, "the filleted L lost its submapped grid")
arc = boundary_nodes(parts[2850], lambda length, p, kind: kind == "Circle")
check(len(arc) > 2, "the fillet has no interior nodes")
print("  fillet boundary fingerprint " + fingerprint_rows(grid, arc))
check(fingerprint_rows(grid, arc) == "28e8a7d49ea703fceb92a98b182a76a93c497946a4edada146251f8d7e47012f",
      "the fillet's arc nodes moved")

label = "filleted L quality veto"
grid, lean, worst, output = measure(label, 2850, ((70, 40, 46),), lambda g: math.degrees(
    math.atan2(abs(g.row(0)[len(g.row(80))] - 30), 40)))
check(grid.node_counts == {4} and len(grid.elements) == 600 and not output, "the vetoed L lost its submapped grid")
check(lean > 20 and abs(worst - 0.554068) < EPS, "the vetoed L did not recover its original grid")
check_count(grid.row(40), 30, 100, 46)
boundary = boundary_nodes(parts[2850], lambda *_: True)
check(fingerprint_rows(grid, boundary) == "edba55cc9a17371c8c4dd22cc06cd634ba76a43617d4b792120f329a78c8dca2",
      "the vetoed L did not restore its boundary")
check_baseline(label, grid)

grid, lean, worst, output = measure("hole plate", 3000, (), lambda g: max_lean(g.row(0), g.row(40), 40))
check(grid.node_counts == {4} and len(grid.elements) == 72 and not output,
      "the plate with a hole lost its submapped grid")
check_baseline("hole plate", grid)

def taper_lean(grid):
    return fractions_lean([grid.row(0), grid.row(60)], 60)


grid, lean, worst, output = measure("below threshold aligned", 3150, ((30, 0, 7),), taper_lean)
check(lean < 0.1 and abs(grid.row(60)[7] - 31) < EPS and
      len(grid.elements) == 273 and grid.node_counts == {4} and not output,
      "the isolated split plate did not align below the threshold")

label = "below threshold with fixed vertices"
grid, lean, worst, output = measure(label, 3900, ((30, 0, 7), (33, 60, 7)), taper_lean)
check(2.8 < lean < 2.9 and abs(grid.row(0)[7] - 30) < EPS and abs(grid.row(60)[7] - 33) < EPS,
      "the mismatched split vertices did not stay fixed")
check_baseline(label, grid)

label = "below threshold with a free neighbour"
grid, lean, worst, output = measure(label, 4050, ((30, 0, 7),), taper_lean)
check(3.0 < lean < 3.2 and abs(grid.row(60)[7] - 34.1666666667) < EPS,
      "the split plate moved the shared edge of its free neighbour")
check_baseline(label, grid)


def chain_lean(grid):
    return max(fractions_lean([grid.row(0), grid.row(100)], 100),
               max_lean(grid.row(100), grid.row(195), 95))


grid, lean, worst, output = measure("low-gain chain kept", 3300, ((30, 0, 10),), chain_lean)
check(3.4 < lean < 3.45, "the chain with an initial lean of 6.49 degrees did not keep its small improvement")

grid, lean, worst, output = measure("all edges fixed", 3450, ((30, 0, 12),),
                                   lambda g: fractions_lean([g.row(0), g.row(40)], 40))
check(17 < lean < 18, "the fixed chain did not retain its above-threshold lean")
check_baseline("all edges fixed", grid)


def check_frame(key, width, height, wall, size, cells, quads):
    _, output = mesh(parts[key], (), "Simple", False, "Automatic", max_size=size)
    grid = FaceMesh(parts[key], planar(key))
    worst = grid.worst_corner()
    check(grid.node_counts == {4} and len(grid.elements) == quads and not output,
          "the rectangular frame did not keep its %d-quad lattice" % quads)
    check(worst >= math.cos(math.radians(0.5)), "a frame corner is more than 0.5 degrees from 90")
    uses = {}
    valence = {}
    for element in grid.elements:
        for i, node in enumerate(element):
            valence[node] = valence.get(node, 0) + 1
            edge = tuple(sorted((node, element[i - 1])))
            uses[edge] = uses.get(edge, 0) + 1
    boundary = {node for edge, count in uses.items() if count == 1 for node in edge}
    check(all(n in boundary or count == 4 for n, count in valence.items()),
          "the rectangular frame has an interior node without four quads")
    across = [sum(abs(v - wall) < EPS and u <= wall + EPS for u, v in grid.uv.values()) - 1,
              sum(abs(v - wall) < EPS and u >= width - wall - EPS for u, v in grid.uv.values()) - 1,
              sum(abs(u - wall) < EPS and v <= wall + EPS for u, v in grid.uv.values()) - 1,
              sum(abs(u - wall) < EPS and v >= height - wall - EPS for u, v in grid.uv.values()) - 1]
    check(across == [cells] * 4, "the rectangular frame has unexpected wall counts: " + str(across))
    print("frame", key, "size", size, "quads", quads, "cells across", across, "worst scaled Jacobian", worst)


check_frame(3600, 644 / 11, 1149 / 11, 62 / 11, 65 / 11, 1, 52)
check_frame(3600, 644 / 11, 1149 / 11, 62 / 11, MAX_SIZE, 1, 62)
check_frame(3600, 644 / 11, 1149 / 11, 62 / 11, 30 / 11, 2, 228)
check_frame(3600, 644 / 11, 1149 / 11, 62 / 11, 28.8 / 11, 2, 236)
check_frame(3750, 40 + 10 / 3, 80 + 10 / 3, MAX_SIZE / 3, MAX_SIZE, 1, 88)

def face_elements(part, grid, face_id):
    data = part.get_face_data()
    geometry_id = next(g for i, g in zip(data.ids, data.geometry_ids) if i == face_id)
    selection = pmx.create.selection_by_ids("femodel", "elements", "geometry", [int(geometry_id)])
    selected = set(selection.preview().ids)
    ids = list(map(int, pmx.model.parts[part.name].get_element_ids()))
    return [element for tag, element in zip(ids, grid.elements) if tag in selected]


def boundary_fingerprint(grid, elements):
    uses = {}
    for element in elements:
        for i, node in enumerate(element):
            edge = tuple(sorted((node, element[i - 1])))
            uses[edge] = uses.get(edge, 0) + 1
    boundary = {node for edge, count in uses.items() if count == 1 for node in edge}
    points = sorted(tuple(round(c, 9) for c in grid.xyz[node]) for node in boundary)
    return hashlib.sha256(repr(points).encode("ascii")).hexdigest()


def check_refused_patches():
    part = parts[4200]
    data = part.get_edge_data()
    split_edge = next(i for i, length in zip(data.ids, data.lengths) if abs(length - 0.1) < EPS)
    _, output = mesh(part, ((int(split_edge), 5),), "QuadDominant", False, "Automatic",
                     max_size=0.0361, min_size=0.00361)
    grid = FaceMesh(part, planar(4200))
    baseline = {9: "aa3eac48e46860efdc87e93501246610869f796cebc195ef2dde7cacace4a297",
                12: "e8b153142fbed7a0d75b807a1985506a6ad7d4567bfaae34aeef2112394144ef"}
    for face_id, expected in baseline.items():
        elements = face_elements(part, grid, face_id)
        check(any("Surface %d " % face_id in line and "is meshed free" in line for line in output),
              "the refused submap did not fall back to a free face")
        expected_quads = 550 if face_id == 9 else 548
        check(len(elements) == expected_quads and grid.node_counts == {4}, "the refused submap lost its free quads")
        check(boundary_fingerprint(grid, elements) == expected, "the refused submap kept its matched boundary")
    wall = face_elements(part, grid, 3)
    check(len(wall) == 132 and grid.worst_corner(wall) > 1 - EPS, "the adjoining wall lost its right-angled grid")
    sides = [sorted(y for x, y, z in grid.xyz.values() if abs(x - 4200 - side) < EPS and abs(z + 0.3) < EPS)
             for side in (-0.2, 0.2)]
    check(all(len(side) == 12 for side in sides), "the adjoining wall did not keep 11 intervals on each side")
    check(abs(sides[0][5] + 0.05) < EPS and max(abs(a - b) for a, b in zip(*sides)) < EPS,
          "the pin pattern did not reach the opposite side after the submaps left")


def check_free_face_guard():
    part = parts[4350]
    mesh(part, (), "QuadDominant", False, "Automatic", max_size=0.0433, min_size=0.00433)
    grid = FaceMesh(part, planar(4350))
    free = face_elements(part, grid, 6)
    submap = face_elements(part, grid, 9)
    check(boundary_fingerprint(grid, free) == "bc90d909fd813d1b34b258bc17098202a05d7147e23abe38e4c9bc98ec1bf83f",
          "the poor free face did not recover its fixed boundary")
    check(len(free) == 683 and abs(grid.worst_corner(free) - 0.644742) < EPS,
          "the poor free face did not recover its baseline quality")
    check(grid.fingerprint(free) == "98cb8c2d37e34025d3edf6299df0043fa05e1e3a481f0c9d8a2ae38aedb85c70",
          "the poor free face changed from its baseline mesh")
    check(len(submap) == 476 and abs(grid.worst_corner(submap) - 0.414856) < EPS,
          "spacing did not rematch the chain around the fixed free face")


check_refused_patches()
check_free_face_guard()


def check_round_rim(grid, cx, cy, radius, fingerprint):
    rim = sorted((x, y) for x, y in grid.uv.values() if abs(math.hypot(x - cx, y - cy) - radius) < EPS)
    check(len(rim) == 16 and hashlib.sha256(repr(rim).encode("ascii")).hexdigest() == fingerprint,
          "the round rim moved from its baseline")


def check_round_strip(pin, total, plate_count, expected_quality, baseline_quality):
    part = parts[4500]
    data = part.get_edge_data()
    pins = [int(i) for i, length, center in zip(data.ids, data.lengths, data.centroids)
            if abs(length - 30) < EPS and abs(center[1]) < EPS]
    check(len(pins) == 1, "the split edge of the round-hole strip is missing")
    _, output = mesh(part, [(pins[0], pin)], "Simple", False, "Automatic")
    grid = FaceMesh(part, planar(4500))
    bottom, shared, top = [grid.row(y) for y in (0, 40, 100)]
    for row in (bottom, shared, top):
        check_count(row, 0, 100, total)
    check_count(bottom, 0, 30, pin)
    check_count(bottom, 30, 100, total - pin)
    strip = [element for element in grid.elements if max(grid.uv[n][1] for n in element) <= 40 + EPS]
    plate = [element for element in grid.elements if min(grid.uv[n][1] for n in element) >= 40 - EPS]
    check(len(strip) == 8 * total and len(plate) == plate_count and grid.node_counts == {4} and not output,
          "the strip or the round-hole plate lost its grid")
    strip_lean = max_lean(bottom, shared, 40)
    plate_lean = max_lean(shared, top, 60)
    quality = grid.worst_corner(plate)
    check(strip_lean < 0.1 and plate_lean < 0.1 and grid.worst_corner(strip) > 1 - EPS,
          "the spacing did not cross the shared edge and the round hole")
    check(abs(quality - expected_quality) < EPS, "the round-hole grid changed quality")
    if pin == 7:
        check(quality >= baseline_quality - 0.02, "the exact chain made the round-hole grid too much worse")
    check_round_rim(grid, 50, 70, 12, "519f76c546585d3b0b94ed61768b53269edd9ff341c5b2887ed4d45f29c1a610")
    print("round strip pin %d lean %.6f, worst %.6f; round plate lean %.6f, worst %.6f" %
          (pin, strip_lean, grid.worst_corner(strip), plate_lean, quality))


def check_mixed_holes():
    part = parts[4650]
    _, output = mesh(part, (), "Simple", False, "Automatic")
    grid = FaceMesh(part, planar(4650))
    check(len(grid.elements) == 264 and grid.node_counts == {4} and not output,
          "the mixed-hole plate lost its grid")
    quality = grid.worst_corner()
    check(abs(quality - 0.858053) < EPS, "the mixed-hole grid changed quality")

    def column(x):
        return sorted(y for u, y in grid.uv.values() if abs(u - x) < EPS)

    bottom, top = grid.row(0), grid.row(80)
    left, right = column(0), column(100)
    check(max_lean(bottom, top, 80) < 0.1 and max_lean(left, right, 100) < 0.1,
          "the outer edges of the mixed-hole plate are skewed")
    for edge in (bottom, top):
        for y in (23, 57):
            hole = intervals(grid.row(y), 53, 78)
            match = intervals(edge, 53, 78)
            check(len(hole) == 6 and len(match) == 6 and max(abs(a - b) for a, b in zip(match, hole)) < EPS,
                  "a horizontal edge of the cornered hole did not align with the boundary")
    for edge in (left, right):
        for x in (53, 78):
            hole = intervals(column(x), 23, 57)
            match = intervals(edge, 23, 57)
            check(len(hole) == 9 and len(match) == 9 and max(abs(a - b) for a, b in zip(match, hole)) < EPS,
                  "a vertical edge of the cornered hole did not align through the round hole")
    check_round_rim(grid, 25, 40, 10, "33cfbad06ae4eb52eb1ee6409029ac18bfdd539580c83dbab9d126437cff9bc2")
    print("mixed holes: 264 quads, worst %.6f" % quality)


check_round_strip(12, 26, 296, 0.723421, 0.789909)
check_round_strip(7, 21, 236, 0.834268, 0.771253)
check_mixed_holes()

print("TEST PASSED")
