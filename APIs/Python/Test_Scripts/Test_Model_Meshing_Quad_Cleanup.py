#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks QuadCleanup, which turns the quads and triangles of one face into quads only on the same boundary nodes, on its
#own, without a mesher: the mesh is given as the positions of its nodes in the plane, its elements and the nodes that
#are fixed, every boundary node among them.
#
#  - Planted pairs: a structured grid of squares in which a row of cells is sheared so that a triangle sits at each
#    end of the row, the two triangles 1 to 10 cells apart, each in a grid of its own: quads only, the pair paired, no
#    fold, the worst scaled Jacobian at least 0.2 and every inner node back at four quads. Ten such rows in one grid of
#    24 x 24, two rows apart, so that a triangle may find one of another row nearer: quads only, ten pairs, the worst
#    scaled Jacobian at least 0.2; the irregular nodes left are reported.
#  - A fuzz of 240 faces meshed by gmsh with its simple recombination through GmshCommon: random star-shaped polygons of
#    5 to 12 corners, some with a round or a polygonal hole, at random element sizes. A face whose boundary carries an
#    even number of elements ends with quads only, an odd one with exactly one triangle, in 95 percent of the faces at
#    least; the number that does is reported with the worst scaled Jacobian.
#  - Odd totals: a square with 5, 5, 5 and 4 elements on its edges keeps exactly one triangle.
#  - Every result: no element folds, the fixed nodes are where they were, the boundary edges are the same, every element
#    turns the way the input turned and the elements cover the area of the input.
#  - Determinism: the same input twice gives the same elements and positions, to the last bit.
#  - Timing: a disc of about 10,000 elements is cleaned in under the time bound; the time is reported.
#  With the environment variable PMX_QUAD_PLOT_DIR set, the planted grid, the odd square, the timing disc and the worst
#  cases of the fuzz are written into that folder as PNG images before and after the cleanup.
#
#  PrePoMax.com -u MM_TON_S_C --script Test_Model_Meshing_Quad_Cleanup.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import math
import os
import random
import time
import clr
clr.AddReference('CaeMesh')
clr.AddReference('GmshCommon')
clr.AddReference('System.Drawing')
from CaeMesh import QuadCleanup
from GmshCommon import Gmsh
from System import Array, Boolean, Double, Int32
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat

PLOT_DIR = os.environ.get("PMX_QUAD_PLOT_DIR")
FLOOR = 0.2
TIME_BOUND = 5.0
NUM_OF_FUZZ_CASES = 240


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Cleanup --------------------------------------------------------------------------------------------------------------
class Cleaned(object):
    """The input of the cleanup and what it gave."""

    def __init__(self, points, elements, fixed):
        self.points_in = [list(p) for p in points]
        self.elements_in = [list(e) for e in elements]
        self.fixed = list(fixed)
        start = time.time()
        self.result = QuadCleanup.Clean(Array[Array[Double]]([Array[Double](p) for p in points]),
                                        Array[Array[Int32]]([Array[Int32](e) for e in elements]),
                                        Array[Boolean](fixed))
        self.seconds = time.time() - start
        self.elements = [[int(n) for n in e] for e in self.result.Elements]
        self.points = [[float(p[0]), float(p[1])] for p in self.result.Points]
        self.triangles = sum(1 for e in self.elements if len(e) == 3)


def signed_area(points, element):
    area = 0.0
    for c in range(len(element)):
        p, q = points[element[c]], points[element[(c + 1) % len(element)]]
        area += p[0] * q[1] - q[0] * p[1]
    return 0.5 * area


def min_scaled_jacobian(points, elements, orientation):
    """The smallest sine of a corner, a triangle scaled so that 60 degrees count as 1, negative where it folds."""
    worst = 1.0
    for element in elements:
        n = len(element)
        for c in range(n):
            p, a, b = points[element[c]], points[element[(c + 1) % n]], points[element[(c - 1) % n]]
            e1, e2 = (a[0] - p[0], a[1] - p[1]), (b[0] - p[0], b[1] - p[1])
            length = math.hypot(*e1) * math.hypot(*e2)
            sine = orientation * (e1[0] * e2[1] - e1[1] * e2[0]) / length if length > 0 else -1.0
            worst = min(worst, sine * (2 / math.sqrt(3) if n == 3 else 1.0))
    return worst


def boundary_edges(elements):
    uses = {}
    for element in elements:
        for c in range(len(element)):
            edge = tuple(sorted((element[c], element[(c + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return set(edge for edge, count in uses.items() if count == 1)


def irregular_inner_nodes(elements):
    boundary = set(node for edge in boundary_edges(elements) for node in edge)
    valence = {}
    for element in elements:
        for node in element:
            valence[node] = valence.get(node, 0) + 1
    return [node for node, count in valence.items() if node not in boundary and count != 4]


def validate(name, cleaned, expected_triangles=None):
    """Checks what every result must keep; returns the worst scaled Jacobian."""
    area_in = sum(signed_area(cleaned.points_in, e) for e in cleaned.elements_in)
    orientation = 1.0 if area_in > 0 else -1.0
    area_out = sum(signed_area(cleaned.points, e) for e in cleaned.elements)
    check(abs(area_out - area_in) <= 1E-9 * abs(area_in), name + ": the elements cover " + str(area_out) +
          " instead of " + str(area_in))
    check(all(orientation * signed_area(cleaned.points, e) > 0 for e in cleaned.elements),
          name + ": an element turns the other way")
    worst = min_scaled_jacobian(cleaned.points, cleaned.elements, orientation)
    check(worst > 0 and cleaned.result.NumberOfFoldedElements == 0, name + ": an element folds, worst " + str(worst))
    check(abs(worst - cleaned.result.MinScaledJacobian) < 1E-9, name + ": the result reports the worst scaled " +
          "Jacobian " + str(cleaned.result.MinScaledJacobian) + " instead of " + str(worst))
    for node, is_fixed in enumerate(cleaned.fixed):
        if is_fixed:
            check(cleaned.points[node] == cleaned.points_in[node], name + ": the fixed node " + str(node) + " moved")
    check(boundary_edges(cleaned.elements) == boundary_edges(cleaned.elements_in), name + ": the boundary changed")
    check(cleaned.triangles == cleaned.result.NumberOfTriangles, name + ": the triangles are miscounted")
    if expected_triangles is not None:
        check(cleaned.triangles == expected_triangles, name + ": " + str(cleaned.triangles) + " triangles left " +
              "instead of " + str(expected_triangles))
    return worst


# Plots ----------------------------------------------------------------------------------------------------------------
def plot(name, title, points, elements):
    """The elements as a PNG image, quads blue, triangles orange, inner nodes without four quads red."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    used = set(n for e in elements for n in e)
    us = [points[n][0] for n in used]
    vs = [points[n][1] for n in used]
    scale = 760.0 / max(max(us) - min(us), max(vs) - min(vs))
    width, height = int((max(us) - min(us)) * scale + 40), int((max(vs) - min(vs)) * scale + 70)
    bitmap = Bitmap(width, height)
    graphics = Graphics.FromImage(bitmap)
    graphics.Clear(Color.White)
    pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
    quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
    triangle_brush = SolidBrush(Color.FromArgb(246, 190, 160))

    def at(node):
        return PointF(float(20 + (points[node][0] - min(us)) * scale),
                      float(height - 20 - (points[node][1] - min(vs)) * scale))

    for element in elements:
        polygon = Array[PointF]([at(n) for n in element])
        graphics.FillPolygon(quad_brush if len(element) == 4 else triangle_brush, polygon)
        graphics.DrawPolygon(pen, polygon)
    for node in irregular_inner_nodes(elements):
        p = at(node)
        graphics.FillEllipse(Brushes.Firebrick, p.X - 2.5, p.Y - 2.5, 5.0, 5.0)
    graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, name + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


def plot_both(name, cleaned, worst):
    before = sum(1 for e in cleaned.elements_in if len(e) == 3)
    plot(name + "_before", name + " before: " + str(len(cleaned.elements_in) - before) + " quads, " + str(before) +
         " triangles", cleaned.points_in, cleaned.elements_in)
    plot(name + "_after", name + " after: " + str(len(cleaned.elements) - cleaned.triangles) + " quads, " +
         str(cleaned.triangles) + " triangles, worst SJ " + str(round(worst, 3)) + ", irregular " +
         str(len(irregular_inner_nodes(cleaned.elements))), cleaned.points, cleaned.elements)


# Planted pairs --------------------------------------------------------------------------------------------------------
def planted_grid(n, rows):
    """A grid of n x n unit squares; in every row j of rows, the cells from column 2 to 2 + d are sheared so that a
    triangle sits at each end, d being the distance of the row."""
    points = [[float(i), float(j)] for j in range(n + 1) for i in range(n + 1)]

    def node(i, j):
        return j * (n + 1) + i

    elements = []
    sheared = {}
    for j, d in rows:
        sheared[j] = d
    for j in range(n):
        if j in sheared:
            d = sheared[j]
            b = [node(2 + k, j) for k in range(d + 2)]
            t = [node(2 + k, j + 1) for k in range(d + 2)]
            for i in range(2):
                elements.append([node(i, j), node(i + 1, j), node(i + 1, j + 1), node(i, j + 1)])
            elements.append([b[0], b[1], t[0]])
            for k in range(1, d + 1):
                elements.append([b[k], b[k + 1], t[k], t[k - 1]])
            elements.append([b[d + 1], t[d + 1], t[d]])
            for i in range(3 + d, n):
                elements.append([node(i, j), node(i + 1, j), node(i + 1, j + 1), node(i, j + 1)])
        else:
            for i in range(n):
                elements.append([node(i, j), node(i + 1, j), node(i + 1, j + 1), node(i, j + 1)])
    fixed = [p[0] == 0 or p[1] == 0 or p[0] == n or p[1] == n for p in points]
    return points, elements, fixed


def check_planted():
    # One pair in a grid of its own: the triangle is carried along its row and the grid comes back regular
    for d in range(10):
        points, elements, fixed = planted_grid(d + 8, [((d + 8) // 2, d)])
        cleaned = Cleaned(points, elements, fixed)
        worst = validate("the pair " + str(d + 1) + " apart", cleaned, 0)
        irregular = len(irregular_inner_nodes(cleaned.elements))
        print("a pair", d + 1, "apart:", cleaned.result.NumberOfTrianglesBefore, "triangles ->", cleaned.triangles,
              "pairs", cleaned.result.NumberOfPairs, "worst scaled Jacobian", round(worst, 3), "irregular inner nodes",
              cleaned.result.NumberOfIrregularNodesBefore, "->", irregular)
        check(cleaned.result.NumberOfPairs == 1 and worst >= FLOOR and irregular == 0,
              "the pair " + str(d + 1) + " apart is not turned back into the regular grid")
        if d == 6:
            plot_both("cleanup_planted_pair_7", cleaned, worst)
    # Ten pairs in one grid, the rows two apart, so that a triangle may find one of another row nearer
    rows = [(1 + 2 * d, d) for d in range(10)]
    points, elements, fixed = planted_grid(24, rows)
    cleaned = Cleaned(points, elements, fixed)
    worst = validate("planted pairs", cleaned, 0)
    irregular = len(irregular_inner_nodes(cleaned.elements))
    print("ten planted pairs at distances 1 to 10 in one grid:", cleaned.result.NumberOfTrianglesBefore,
          "triangles ->", cleaned.triangles, "pairs", cleaned.result.NumberOfPairs, "worst scaled Jacobian",
          round(worst, 3), "irregular inner nodes", cleaned.result.NumberOfIrregularNodesBefore, "->", irregular)
    plot_both("cleanup_planted", cleaned, worst)
    check(cleaned.result.NumberOfPairs == 10, "the planted pairs were paired " + str(cleaned.result.NumberOfPairs) +
          " times instead of 10")
    check(worst >= FLOOR, "the planted grid has a corner of the scaled Jacobian " + str(worst))


# gmsh -----------------------------------------------------------------------------------------------------------------
def gmsh_mesh(outer, holes, size, curve_counts=None):
    """The mesh gmsh makes of a plane face with its simple recombination: the positions of the nodes, the elements and
    which nodes lie on the curves. outer and holes are lists of points; curve_counts the elements of every curve of the
    outer loop, else the size rules."""
    Gmsh.Initialize()
    try:
        Gmsh.Option.SetNumber("General.Terminal", 0)
        Gmsh.Model.Add("cleanup")
        occ = Gmsh.Model.OCC
        loops = []
        outer_curves = []
        for k, polygon in enumerate([outer] + holes):
            tags = [occ.AddPoint(float(x), float(y), 0.0) for x, y in polygon]
            curves = [occ.AddLine(tags[i], tags[(i + 1) % len(tags)]) for i in range(len(tags))]
            if k == 0:
                outer_curves = curves
            loops.append(occ.AddCurveLoop(Array[Int32](curves)))
        occ.AddPlaneSurface(Array[Int32](loops))
        occ.Synchronize()
        Gmsh.Option.SetNumber("Mesh.MeshSizeMax", size)
        Gmsh.Option.SetNumber("Mesh.MeshSizeMin", size / 10)
        Gmsh.Option.SetNumber("Mesh.RecombineAll", 1)
        Gmsh.Option.SetNumber("Mesh.RecombinationAlgorithm", 0)
        if curve_counts is not None:
            for curve, count in zip(outer_curves, curve_counts):
                Gmsh.Model.Mesh.SetTransfiniteCurve(curve, count + 1)
        Gmsh.Model.Mesh.Generate(2)
        tags, coordinates = Gmsh.Model.Mesh.GetNodes(None, None, -1, -1, True, False)
        index = {}
        points = []
        for i in range(len(tags)):
            index[int(tags[i].ToInt64())] = len(points)
            points.append([coordinates[3 * i], coordinates[3 * i + 1]])
        curve_tags, _ = Gmsh.Model.Mesh.GetNodes(None, None, 1, -1, True, False)
        fixed = [False] * len(points)
        for tag in curve_tags:
            fixed[index[int(tag.ToInt64())]] = True
        types, _, node_tags = Gmsh.Model.Mesh.GetElements(None, None, None, 2, -1)
        elements = []
        for t in range(len(types)):
            n = {2: 3, 3: 4}[int(types[t])]
            flat = [index[int(tag.ToInt64())] for tag in node_tags[t]]
            for e in range(len(flat) // n):
                elements.append(flat[n * e:n * e + n])
        return points, elements, fixed
    finally:
        Gmsh.FinalizeAll()


def boundary_total(elements):
    return len(boundary_edges(elements))


def star_polygon(rng, cx, cy, radius, corners):
    """A polygon star-shaped about the centre, its corners spread round it so that no edge comes nearer to the centre
    than about a quarter of the radius."""
    step = 2 * math.pi / corners
    angles = [step * i + rng.uniform(-0.3, 0.3) * step for i in range(corners)]
    return [(cx + radius * rng.uniform(0.55, 1.0) * math.cos(a), cy + radius * rng.uniform(0.55, 1.0) * math.sin(a))
            for a in angles]


def regular_polygon(cx, cy, radius, corners, turn=0.0):
    return [(cx + radius * math.cos(turn + 2 * math.pi * i / corners),
             cy + radius * math.sin(turn + 2 * math.pi * i / corners)) for i in range(corners)]


def fuzz_case(rng):
    outer = star_polygon(rng, 0.0, 0.0, 50.0, rng.randint(5, 12))
    holes = []
    kind = rng.random()
    if kind < 0.2:
        holes.append(regular_polygon(rng.uniform(-3, 3), rng.uniform(-3, 3), rng.uniform(3, 8), 24))
    elif kind < 0.3:
        holes.append(regular_polygon(rng.uniform(-3, 3), rng.uniform(-3, 3), rng.uniform(3, 8), rng.randint(3, 6),
                                     rng.uniform(0, 1)))
    return outer, holes, rng.uniform(3.0, 12.0)


def check_fuzz():
    rng = random.Random(20261115)
    num_of_even = 0
    num_of_even_all_quad = 0
    num_of_odd = 0
    num_of_odd_one = 0
    worst_case = (2.0, None)
    worst_overall = 1.0
    fewest_quads = []
    total_seconds = 0.0
    total_triangles = 0
    for case in range(NUM_OF_FUZZ_CASES):
        outer, holes, size = fuzz_case(rng)
        points, elements, fixed = gmsh_mesh(outer, holes, size)
        name = "fuzz " + str(case)
        cleaned = Cleaned(points, elements, fixed)
        worst = validate(name, cleaned)
        total_seconds += cleaned.result.Seconds
        total_triangles += cleaned.result.NumberOfTrianglesBefore
        odd = boundary_total(elements) % 2 == 1
        if odd:
            num_of_odd += 1
            num_of_odd_one += 1 if cleaned.triangles == 1 else 0
        else:
            num_of_even += 1
            num_of_even_all_quad += 1 if cleaned.triangles == 0 else 0
        if cleaned.triangles > (1 if odd else 0):
            fewest_quads.append((case, cleaned.triangles))
            plot_both("cleanup_fuzz_" + str(case) + "_left", cleaned, worst)
        worst_overall = min(worst_overall, worst)
        if cleaned.triangles <= (1 if odd else 0) and worst < worst_case[0]:
            worst_case = (worst, case, cleaned)
    print("fuzz of", NUM_OF_FUZZ_CASES, "faces,", total_triangles, "triangles before:", num_of_even_all_quad, "of",
          num_of_even, "even totals end with quads only,", num_of_odd_one, "of", num_of_odd,
          "odd totals with exactly one triangle; worst scaled Jacobian", round(worst_overall, 3),
          "of a converted face", round(worst_case[0], 3), "(case " + str(worst_case[1]) + "); cleanup time",
          round(total_seconds, 3), "s in all")
    print("   faces left with more triangles:", fewest_quads)
    if worst_case[1] is not None:
        plot_both("cleanup_fuzz_worst_" + str(worst_case[1]), worst_case[2], worst_case[0])
    check(num_of_even_all_quad + num_of_odd_one >= 0.95 * NUM_OF_FUZZ_CASES,
          "only " + str(num_of_even_all_quad + num_of_odd_one) + " of the fuzz faces ended with the fewest triangles")


def check_odd():
    square = [(0, 0), (20, 0), (20, 20), (0, 20)]
    points, elements, fixed = gmsh_mesh(square, [], 4.0, [5, 5, 5, 4])
    check(boundary_total(elements) == 19, "the odd square has " + str(boundary_total(elements)) +
          " boundary segments")
    cleaned = Cleaned(points, elements, fixed)
    worst = validate("odd square", cleaned, 1)
    print("square of 5, 5, 5 and 4 elements:", cleaned.result.NumberOfTrianglesBefore, "triangles ->",
          cleaned.triangles, ", worst scaled Jacobian", round(worst, 3))
    plot_both("cleanup_odd_square", cleaned, worst)


def check_determinism():
    rng = random.Random(7)
    outer, holes, size = fuzz_case(rng)
    points, elements, fixed = gmsh_mesh(outer, holes, size)
    first = Cleaned(points, elements, fixed)
    second = Cleaned(points, elements, fixed)
    check(first.elements == second.elements, "the same input gave different elements")
    check(all(a[0] == b[0] and a[1] == b[1] for a, b in zip(first.points, second.points)),
          "the same input gave different positions")
    print("determinism: the same input twice gives the same", len(first.elements), "elements and positions")


def check_timing():
    disc = regular_polygon(0.0, 0.0, 50.0, 320)
    points, elements, fixed = gmsh_mesh(disc, [], 1.0)
    cleaned = Cleaned(points, elements, fixed)
    worst = validate("timing disc", cleaned)
    again = Cleaned(points, elements, fixed)
    check(again.elements == cleaned.elements, "the timing disc is not deterministic")
    print("timing disc:", len(elements), "elements,", cleaned.result.NumberOfTrianglesBefore, "triangles ->",
          cleaned.triangles, "in", round(cleaned.result.Seconds, 3), "s (" + str(round(again.result.Seconds, 3)) +
          " s again), pairs", cleaned.result.NumberOfPairs, "doublets", cleaned.result.NumberOfDoublets, "collapses",
          cleaned.result.NumberOfCollapses, "rotations", cleaned.result.NumberOfRotations, "irregular inner nodes",
          cleaned.result.NumberOfIrregularNodesBefore, "->", cleaned.result.NumberOfIrregularNodes,
          "worst scaled Jacobian", round(cleaned.result.MinScaledJacobianBefore, 3), "->", round(worst, 3))
    plot_both("cleanup_timing_disc", cleaned, worst)
    check(min(cleaned.result.Seconds, again.result.Seconds) < TIME_BOUND, "the timing disc took " +
          str(cleaned.result.Seconds) + " s")
    check(cleaned.triangles == boundary_total(elements) % 2, "the timing disc keeps " + str(cleaned.triangles) +
          " triangles")


def main():
    check_planted()
    check_odd()
    check_determinism()
    check_timing()
    check_fuzz()
    print("TEST PASSED")


main()
