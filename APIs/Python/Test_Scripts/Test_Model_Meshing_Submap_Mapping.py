#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks SubmapMapping, the structured mapping of faces whose corners are all near multiples of 90 degrees, on its
#own, without a mesher: the loop is given as edges with a logical direction and the nodes along them, and the
#surface as a function.
#
#  - Planar L, T, U, Z, plus and staircase shapes with straight sides and uniform divisions: every quad is an axis
#    aligned rectangle, so every inner node must sit at its exact lattice position and the smallest scaled Jacobian
#    is 1.
#  - The same shapes with graded edges and with edges split into pieces of different density, sheared shapes and
#    shapes with corners of about 80 and 100 degrees, L and U shapes on a cylinder and an L on a sphere: no fold,
#    every inner node inside the outline and on the surface, and a bounded smallest scaled Jacobian.
#  - A sweep over 300 pseudo-random shapes with a fixed seed, whose counts follow their lengths as an interval
#    assignment would give them, down to one element on short edges, webs one cell thick and notches one cell wide:
#    as many quads as the outline encloses lattice cells and as many inner nodes as Pick's theorem gives, and no
#    fold, except at a corner whose three nodes are all on the boundary - a web one cell thick between edges
#    divided differently - which the edges fix and no placement of the inner nodes can mend.
#  - The topology: every inner node has four quads, the boundary node ids follow the loop and the quads turn with
#    it, in the lattice and in the parameter space.
#  - Counts that do not close, an outline that touches itself, a reversal, a clockwise loop and arrays that disagree
#    are refused with a message that names the reason.
#  - Outlines with holes, each hole given with the lattice offset of its first node: a rectangle with a centred hole,
#    with two holes and with an L-shaped hole, and an L with a square hole, uniform, where every inner node sits at its
#    lattice position, and the centred hole sheared and on a cylinder: as many quads as cells inside, as many inner
#    nodes as Euler's formula gives, no fold, and SubmapHolePlacement finds the offsets from the parameters of the
#    nodes. Mean value coordinates reproduce a linear map inside an L, and the multi-loop AreCountsValid names a hole
#    that touches the outer loop or another hole, lies outside the outline or inside another hole, turns the wrong way
#    or does not close, and offsets that do not match the holes.
#  - A 100 x 100 cell L is mapped in well under the time bound.
#  With the environment variable PMX_SUBMAP_PLOT_DIR set, every named case and the worst case of the sweep are
#  written into that folder as SVG drawings of the outline and the quads.
#
#  PrePoMax.com -u MM_TON_S_C --script Test_Model_Meshing_Submap_Mapping.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import io
import math
import os
import random
import time
import clr
clr.AddReference('CaeMesh')
from CaeMesh import SubmapHolePlacement, SubmapMapping
from System import Array, Double, Func, Int32

TOLERANCE = 1E-9
TIME_BOUND = 2.0
PLOT_DIR = os.environ.get("PMX_SUBMAP_PLOT_DIR")
DI = [1, 0, -1, 0]
DJ = [0, 1, 0, -1]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Vectors ------------------------------------------------------------------------------------------------------------
def cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def dot(a, b):
    return sum(p * q for p, q in zip(a, b))


def sub(a, b):
    return [p - q for p, q in zip(a, b)]


def norm(a):
    return math.sqrt(dot(a, a))


# Surfaces: a map from plane coordinates x, y to the parameters, the evaluation of the parameters, and the scale of
# the parameters in the drawings ---------------------------------------------------------------------------------------
class Plane(object):
    name = "plane"
    constant_speed = True
    plot_scale = (1.0, 1.0)

    def to_uv(self, x, y):
        return [x, y]

    def eval(self, u, v):
        return [u, v, 0.0]


class Cylinder(object):
    name = "cylinder"
    constant_speed = True

    def __init__(self, radius):
        self.radius = radius
        self.plot_scale = (radius, 1.0)

    def to_uv(self, x, y):
        return [x / self.radius, y]

    def eval(self, u, v):
        return [self.radius * math.cos(u), self.radius * math.sin(u), v]


class Sphere(object):
    name = "sphere"
    constant_speed = False

    def __init__(self, radius):
        self.radius = radius
        self.plot_scale = (radius, radius)

    def to_uv(self, x, y):
        return [x / self.radius, y / self.radius - 0.4]

    def eval(self, u, v):
        r = self.radius
        return [r * math.cos(v) * math.cos(u), r * math.cos(v) * math.sin(u), r * math.sin(v)]


def surface_normal(surface, u, v, eps=1E-6):
    a = [(p - q) / (2 * eps) for p, q in zip(surface.eval(u + eps, v), surface.eval(u - eps, v))]
    b = [(p - q) / (2 * eps) for p, q in zip(surface.eval(u, v + eps), surface.eval(u, v - eps))]
    return cross(a, b)


# Shapes: a family turns its dimensions into the sides of a counter-clockwise rectilinear outline, as (direction,
# length); the same family gives the element counts from integer dimensions and the geometry from real ones ----------
def shape_l(a, b, c, e):
    return [(0, a), (1, e), (2, a - c), (1, b - e), (2, c), (3, b)]


def shape_t(l, s, r, h1, h2):
    return [(0, s), (1, h1), (0, r), (1, h2), (2, l + s + r), (3, h2), (0, l), (3, h1)]


def shape_u(w1, notch, w2, t, depth):
    return [(0, w1 + notch + w2), (1, t + depth), (2, w2), (3, depth), (2, notch), (1, depth), (2, w1), (3, t + depth)]


def shape_z(b1, w, b2, t1, web, t2):
    return [(0, w + b1), (1, t1), (2, b1), (1, web + t2), (2, b2 + w), (3, t2), (0, b2), (3, web + t1)]


def shape_plus(cw, ch, l, r, u, d):
    return [(0, cw), (1, d), (0, r), (1, ch), (2, r), (1, u), (2, cw), (3, u), (2, l), (3, ch), (0, l), (3, d)]


def shape_stair(*dims):
    n = len(dims) // 2
    widths = dims[:n]
    rises = dims[n:]
    sides = [(0, sum(widths))]
    for i in range(n - 1, -1, -1):
        sides.append((1, rises[i]))
        sides.append((2, widths[i]))
    sides.append((3, sum(rises)))
    return sides


FAMILIES = {"L": (shape_l, 4), "T": (shape_t, 5), "U": (shape_u, 5), "Z": (shape_z, 6), "plus": (shape_plus, 6)}


def l_counts(a, b, c, e):
    return [a + c, b + e, c, e]


# Building a case ------------------------------------------------------------------------------------------------------
def weights(n, ratio):
    w = [ratio ** i for i in range(n)]
    t = [0.0]
    for x in w:
        t.append(t[-1] + x)
    return [x / t[-1] for x in t]


def distribute(surface, a, b, n, ratio):
    """n elements along the straight line from a to b in the parameters, of equal length in space or in a geometric
    progression; a line of constant speed is divided by its parameter, any other through samples of its length."""
    fractions = weights(n, ratio)
    if surface.constant_speed:
        ts = fractions
    else:
        samples = 400
        points = [surface.eval(a[0] + (b[0] - a[0]) * i / float(samples), a[1] + (b[1] - a[1]) * i / float(samples))
                  for i in range(samples + 1)]
        lengths = [0.0]
        for i in range(1, len(points)):
            lengths.append(lengths[-1] + norm(sub(points[i], points[i - 1])))
        ts = []
        j = 0
        for f in fractions:
            target = f * lengths[-1]
            while j < samples - 1 and lengths[j + 1] < target:
                j += 1
            span = lengths[j + 1] - lengths[j]
            g = 0.0 if span <= 0 else min(1.0, max(0.0, (target - lengths[j]) / span))
            ts.append((j + g) / float(samples))
        ts[0] = 0.0
        ts[-1] = 1.0
    uv = [[a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t] for t in ts]
    uv[0] = list(a)
    uv[-1] = list(b)
    return uv


def build(surface, counts_sides, length_sides, transform=None, ratios=None, splits=None):
    """The edges of a case: side s of the outline runs counts_sides[s] elements over length_sides[s]; ratios[s]
    grades it, and splits[s] lists the counts of the pieces it is split into, each its own edge, of equal length or,
    given as (counts, fractions), of those fractions of the side."""
    check([d for d, _ in counts_sides] == [d for d, _ in length_sides], "a family gives two outlines")
    ratios = ratios or {}
    splits = splits or {}
    directions = []
    edges = []
    x, y = 0.0, 0.0
    for s, ((direction, count), (_, length)) in enumerate(zip(counts_sides, length_sides)):
        pieces = splits.get(s, [count])
        if isinstance(pieces, tuple):
            pieces, fractions = pieces
        else:
            fractions = [1.0 / len(pieces)] * len(pieces)
        check(sum(pieces) == count, "the pieces of a split side carry another count")
        ends = [0.0]
        for f in fractions:
            ends.append(ends[-1] + f)
        for p, n in enumerate(pieces):
            x0 = x + DI[direction] * length * ends[p]
            y0 = y + DJ[direction] * length * ends[p]
            x1 = x + DI[direction] * length * ends[p + 1]
            y1 = y + DJ[direction] * length * ends[p + 1]
            a = (transform or (lambda q: q))([x0, y0])
            b = (transform or (lambda q: q))([x1, y1])
            directions.append(direction)
            edges.append(distribute(surface, surface.to_uv(*a), surface.to_uv(*b), n, ratios.get(s, 1.0)))
        x += DI[direction] * length
        y += DJ[direction] * length
    for k in range(len(edges)):
        edges[k][-1] = list(edges[(k + 1) % len(edges)][0])
    return directions, edges


def scaled(sides, h):
    return [(d, n * h) for d, n in sides]


def shear(degrees):
    t = math.tan(math.radians(degrees))
    return lambda q: [q[0] + t * q[1], q[1]]


# The mapping ----------------------------------------------------------------------------------------------------------
def to_net(edges):
    return Array[Array[Array[Double]]]([Array[Array[Double]]([Array[Double](p) for p in edge]) for edge in edges])


def map_case(surface, directions, edges_uv, use_evaluate=True):
    edges_xyz = [[surface.eval(*uv) for uv in edge] for edge in edges_uv]

    def evaluate(flat):
        xyz = []
        for i in range(len(flat) // 2):
            xyz.extend(surface.eval(flat[2 * i], flat[2 * i + 1]))
        return Array[Double](xyz)

    function = Func[Array[Double], Array[Double]](evaluate) if use_evaluate else None
    return SubmapMapping.Map(Array[Int32](directions), to_net(edges_uv), to_net(edges_xyz), function), edges_xyz


def point_in_polygon(p, polygon):
    inside = False
    n = len(polygon)
    for i in range(n):
        a = polygon[i]
        b = polygon[(i + 1) % n]
        if (a[1] > p[1]) != (b[1] > p[1]):
            x = a[0] + (p[1] - a[1]) * (b[0] - a[0]) / (b[1] - a[1])
            if p[0] < x:
                inside = not inside
    return inside


def signed_area(points):
    return 0.5 * sum(points[i][0] * points[(i + 1) % len(points)][1] - points[(i + 1) % len(points)][0] * points[i][1]
                     for i in range(len(points)))


def lattice_counts(directions, counts):
    """The enclosed lattice area and the inner lattice points by Pick's theorem."""
    i, j = 0, 0
    twice_area = 0
    for d, n in zip(directions, counts):
        i1 = i + DI[d] * n
        j1 = j + DJ[d] * n
        twice_area += i * j1 - i1 * j
        i, j = i1, j1
    area = twice_area // 2
    boundary = sum(counts)
    return area, area - boundary // 2 + 1


def corner_cross(uv, quad, c):
    a = sub(uv[quad[(c + 1) % 4]], uv[quad[c]])
    b = sub(uv[quad[(c + 3) % 4]], uv[quad[c]])
    return a[0] * b[1] - a[1] * b[0]


def check_grid(name, surface, directions, edges_uv, allow_fixed_folds=False):
    """The checks every case shares; returns the grid, the parameters of all nodes, their logical coordinates, the
    smallest scaled Jacobian and the number of folded quads. A quad may fold only when allowed and only at a corner
    whose three nodes are all on the boundary, where the edges fix it and no placement of the inner nodes can help."""
    grid, edges_xyz = map_case(surface, directions, edges_uv)
    counts = [len(edge) - 1 for edge in edges_uv]
    num_boundary = grid.NumberOfBoundaryNodes
    check(num_boundary == sum(counts), name + ": the grid has another number of boundary nodes")
    boundary_uv = [uv for edge in edges_uv for uv in edge[:-1]]
    inner_uv = [[float(c) for c in uv] for uv in grid.InnerUv]
    inner_xyz = [[float(c) for c in p] for p in grid.InnerXyz]
    uv = boundary_uv + inner_uv
    xyz = [p for edge in edges_xyz for p in edge[:-1]] + inner_xyz
    quads = [[int(n) for n in quad] for quad in grid.Quads]
    logical = [[int(c) for c in ij] for ij in grid.LogicalCoordinates]
    # the counts the lattice must have
    area, inner = lattice_counts(directions, counts)
    check(len(quads) == area, name + ": %d quads, but the outline encloses %d cells" % (len(quads), area))
    check(len(inner_uv) == inner, name + ": %d inner nodes, but Pick's theorem gives %d" % (len(inner_uv), inner))
    check(len(logical) == num_boundary + inner, name + ": not every node has logical coordinates")
    # the boundary node ids follow the loop
    for k, d in enumerate(directions):
        for i in range(counts[k]):
            a = grid.GetBoundaryNodeId(k, i)
            b = grid.GetBoundaryNodeId(k, i + 1)
            check(b == (a + 1) % num_boundary, name + ": the boundary node ids do not follow the loop")
            check(logical[b][0] - logical[a][0] == DI[d] and logical[b][1] - logical[a][1] == DJ[d],
                  name + ": boundary node %d does not step along the direction of edge %d" % (a, k))
    check(min(ij[0] for ij in logical) == 0 and min(ij[1] for ij in logical) == 0,
          name + ": the logical coordinates do not start at 0")
    # every inner node has four quads, and the quads turn with the loop in the lattice and in the parameters
    valence = [0] * len(uv)
    loop_sign = 1.0 if signed_area(boundary_uv) > 0 else -1.0
    folded = 0
    for quad in quads:
        for node in quad:
            valence[node] += 1
        check(signed_area([logical[n] for n in quad]) == 1.0, name + ": a quad is not a counter-clockwise unit cell")
        bad_corners = [c for c in range(4) if loop_sign * corner_cross(uv, quad, c) <= 0]
        if bad_corners:
            folded += 1
            fixed = any(quad[c] < num_boundary and quad[(c + 1) % 4] < num_boundary and quad[(c + 3) % 4] < num_boundary
                        for c in bad_corners)
            check(allow_fixed_folds and fixed, name + ": quad %s turns against the loop" % quad)
    check(grid.NumberOfFoldedQuads == folded, name + ": the grid counts %d folded quads, the test %d" %
          (grid.NumberOfFoldedQuads, folded))
    for n in range(num_boundary, len(uv)):
        check(valence[n] == 4, name + ": inner node %d has %d quads" % (n, valence[n]))
    # the inner nodes lie inside the outline and on the surface
    for i, p in enumerate(inner_xyz):
        check(norm(sub(p, surface.eval(*inner_uv[i]))) < 1E-9, name + ": inner node " + str(i) + " is off the surface")
        check(point_in_polygon(inner_uv[i], boundary_uv), name + ": inner node " + str(i) + " is outside the outline")
    # the smallest scaled Jacobian in space, against the normal of the surface
    min_sj = 1.0
    for quad in quads:
        n = surface_normal(surface, *[sum(uv[i][d] for i in quad) / 4.0 for d in range(2)])
        for c in range(4):
            e1 = sub(xyz[quad[(c + 1) % 4]], xyz[quad[c]])
            e2 = sub(xyz[quad[(c + 3) % 4]], xyz[quad[c]])
            min_sj = min(min_sj, loop_sign * dot(cross(e1, e2), n) / (norm(e1) * norm(e2) * norm(n)))
    if PLOT_DIR:
        write_svg(name, surface, boundary_uv, uv, quads, min_sj)
    return grid, uv, logical, min_sj, folded


# Drawings ---------------------------------------------------------------------------------------------------------
def write_svg(name, surface, boundary_uv, uv, quads, min_sj):
    su, sv = surface.plot_scale
    points = [[p[0] * su, p[1] * sv] for p in uv]
    xs = [p[0] for p in points]
    ys = [p[1] for p in points]
    size = max(max(xs) - min(xs), max(ys) - min(ys)) or 1.0
    scale = 560.0 / size
    margin = 20.0

    def at(p):
        return "%.3f,%.3f" % (margin + (p[0] - min(xs)) * scale, margin + (max(ys) - p[1]) * scale)

    width = 2 * margin + (max(xs) - min(xs)) * scale
    height = 2 * margin + (max(ys) - min(ys)) * scale + 20
    lines = ['<svg xmlns="http://www.w3.org/2000/svg" width="%.0f" height="%.0f" viewBox="0 0 %.0f %.0f">' %
             (width, height, width, height),
             '<rect width="100%" height="100%" fill="white"/>']
    for quad in quads:
        lines.append('<polygon points="%s" fill="#dbe8f6" stroke="#1f4e8c" stroke-width="0.7"/>' %
                     " ".join(at(points[n]) for n in quad))
    lines.append('<polygon points="%s" fill="none" stroke="black" stroke-width="2"/>' %
                 " ".join(at(points[n]) for n in range(len(boundary_uv))))
    lines.append('<text x="%.0f" y="%.0f" font-family="sans-serif" font-size="13">%s  quads %d  min SJ %.3f</text>' %
                 (margin, height - 8, name, len(quads), min_sj))
    lines.append('</svg>')
    with io.open(os.path.join(PLOT_DIR, name + ".svg"), "w", encoding="utf-8", newline="\n") as writer:
        writer.write("\n".join(lines) + "\n")


# Groups -----------------------------------------------------------------------------------------------------------
UNIFORM_SHAPES = [("L", shape_l(10, 8, 4, 3)),
                  ("T", shape_t(3, 4, 5, 6, 3)),
                  ("U", shape_u(3, 4, 2, 2, 5)),
                  ("Z", shape_z(4, 3, 5, 2, 4, 3)),
                  ("plus", shape_plus(3, 4, 5, 3, 2, 4)),
                  ("stair", shape_stair(3, 2, 4, 1, 2, 3, 1, 2))]


def group_uniform():
    worst = 1.0
    plane = Plane()
    for name, sides in UNIFORM_SHAPES:
        for hx, hy, suffix in [(2.5, 2.5, ""), (3.0, 1.75, "_aniso")]:
            lengths = [(d, n * (hx if d % 2 == 0 else hy)) for d, n in sides]
            directions, edges = build(plane, sides, lengths)
            case = "uniform_" + name + suffix
            grid, uv, logical, min_sj, folded = check_grid(case, plane, directions, edges)
            # node 0 sits at the origin, so lattice point i, j belongs at ((i - i0) hx, (j - j0) hy)
            i0, j0 = logical[0]
            size = max(abs(c) for p in uv for c in p)
            deviation = 0.0
            for n in range(grid.NumberOfBoundaryNodes, len(uv)):
                expected = [(logical[n][0] - i0) * hx, (logical[n][1] - j0) * hy]
                deviation = max(deviation, abs(uv[n][0] - expected[0]), abs(uv[n][1] - expected[1]))
            check(deviation <= TOLERANCE * size, case + ": the inner nodes are off their lattice positions by %g" %
                  deviation)
            check(min_sj > 1 - 1E-9, case + ": the smallest scaled Jacobian is %.12f, not 1" % min_sj)
            worst = min(worst, min_sj)
            print("%-22s edges %2d  quads %4d  deviation %.1e  min SJ %.6f" % (case, len(directions), len(grid.Quads),
                                                                             deviation, min_sj))
    return worst


# The bounds sit some 15% below what the mapping gives, so that a change for the worse shows. A graded side or a
# side split into pieces of different density, opposite a uniform one, cannot be met by straight lattice lines: the
# worst named case splits a side 3 to 9 over equal halves, a jump of three in the element size. The sweep holds a few
# shapes whose counts follow their lengths only within a quarter, with webs one element thick between long sides, so
# its worst quad is poor; its mean is the measure.
GRADED_BOUND = 0.5
SKEW_FACTOR = 0.85
CURVED_BOUND = 0.6
SWEEP_BOUND = 0.05
SWEEP_MEAN_BOUND = 0.8
MAX_FIXED_FOLD_CASES = 1


def group_graded():
    """Graded sides opposite uniform ones, and sides split into pieces of equal length but different counts."""
    worst = 1.0
    plane = Plane()
    h = 2.5
    variants = [("L", shape_l(10, 8, 4, 3), {0: 1.12, 5: 0.9}, {3: [2, 3]}),
                ("T", shape_t(3, 4, 5, 6, 3), {4: 1.08}, {1: [2, 4], 4: [3, 9]}),
                ("U", shape_u(3, 4, 2, 2, 5), {0: 1.1, 7: 1.15}, {3: [1, 4]}),
                ("Z", shape_z(4, 3, 5, 2, 4, 3), {0: 0.88, 3: 1.12}, {4: [3, 5]}),
                ("plus", shape_plus(3, 4, 5, 3, 2, 4), {2: 1.15, 8: 1.15}, {0: [1, 2]}),
                ("stair", shape_stair(3, 2, 4, 1, 2, 3, 1, 2), {0: 1.1}, {9: [2, 6]})]
    for name, sides, ratios, splits in variants:
        for kind, r, s in [("graded", ratios, None), ("split", None, splits), ("graded_split", ratios, splits)]:
            directions, edges = build(plane, sides, scaled(sides, h), ratios=r, splits=s)
            case = kind + "_" + name
            grid, uv, logical, min_sj, folded = check_grid(case, plane, directions, edges)
            check(min_sj > GRADED_BOUND, case + ": the smallest scaled Jacobian %.3f is below %.2f" %
                  (min_sj, GRADED_BOUND))
            worst = min(worst, min_sj)
            print("%-22s edges %2d  quads %4d  min SJ %.3f" % (case, len(directions), len(grid.Quads), min_sj))
    return worst


def group_skewed():
    worst = 1.0
    plane = Plane()
    h = 2.5
    # corners of about 80 and 100 degrees: the vertices of an L moved off the lattice
    moved = {(25.0, 7.5): (26.3, 7.5), (0.0, 20.0): (3.5, 20.0), (10.0, 20.0): (11.5, 20.0)}

    def move(q):
        for key, value in moved.items():
            if abs(q[0] - key[0]) < 1E-9 and abs(q[1] - key[1]) < 1E-9:
                return list(value)
        return q

    sides = shape_l(10, 8, 4, 3)
    variants = [("sheared_L", sides, shear(15), None, None),
                ("sheared_U", shape_u(3, 4, 2, 2, 5), shear(15), None, None),
                ("sheared_plus", shape_plus(3, 4, 5, 3, 2, 4), shear(-15), None, None),
                ("sheared_graded_Z", shape_z(4, 3, 5, 2, 4, 3), shear(15), {0: 0.88, 3: 1.12}, None),
                ("angled_L", sides, move, None, None),
                ("angled_graded_L", sides, move, {0: 1.12}, {3: [2, 3]})]
    for case, sides, transform, ratios, splits in variants:
        directions, edges = build(plane, sides, scaled(sides, h), transform=transform, ratios=ratios, splits=splits)
        angles = []
        loop = [edge[0] for edge in edges]
        for k in range(len(loop)):
            a = sub(loop[(k + 1) % len(loop)], loop[k])
            b = sub(loop[k - 1], loop[k])
            if directions[k] != directions[k - 1]:
                angles.append(math.degrees(math.acos(dot(a, b) / (norm(a) * norm(b)))))
        grid, uv, logical, min_sj, folded = check_grid(case, plane, directions, edges)
        # the quads at a corner share its angle, so the sine of the most skewed corner bounds the scaled Jacobian
        bound = SKEW_FACTOR * min(math.sin(math.radians(a)) for a in angles)
        check(min_sj > bound, case + ": the smallest scaled Jacobian %.3f is below %.3f" % (min_sj, bound))
        worst = min(worst, min_sj)
        print("%-22s corners %.0f to %.0f deg  quads %4d  min SJ %.3f  sine of the worst corner %.3f" %
              (case, min(angles), max(angles), len(grid.Quads), min_sj,
               min(math.sin(math.radians(a)) for a in angles)))
    return worst


def group_curved():
    worst = 1.0
    variants = [("cylinder_L", Cylinder(20.0), shape_l(10, 8, 4, 3), 3.0, None),
                ("cylinder_U", Cylinder(15.0), shape_u(3, 4, 2, 2, 5), 2.0, {0: 1.1}),
                ("sphere_L", Sphere(30.0), shape_l(10, 8, 4, 3), 3.0, None)]
    for case, surface, sides, h, ratios in variants:
        directions, edges = build(surface, sides, scaled(sides, h), ratios=ratios)
        grid, uv, logical, min_sj, folded = check_grid(case, surface, directions, edges)
        check(min_sj > CURVED_BOUND, case + ": the smallest scaled Jacobian %.3f is below %.2f" % (min_sj, CURVED_BOUND))
        worst = min(worst, min_sj)
        print("%-22s quads %4d  min SJ %.3f" % (case, len(grid.Quads), min_sj))
    return worst


def random_dimension(rng, h):
    """A thin web or a narrow notch of one element about a third of the time, otherwise up to seven elements."""
    return rng.uniform(0.3, 1.2) * h if rng.random() < 0.3 else rng.uniform(1.5, 7.0) * h


def random_case(rng, index):
    """A shape of random dimensions whose counts follow its lengths, within a quarter, as an interval assignment
    would give them; at least one element on every side. A side is graded or split into two edges at random, and
    the pieces of a split side carry counts that follow their lengths too."""
    h = 2.0
    family = rng.choice(["L", "T", "U", "Z", "plus", "stair"])
    if family == "stair":
        function = shape_stair
        size = 2 * rng.randint(3, 5)
    else:
        function, size = FAMILIES[family]
    dims = [random_dimension(rng, h) for _ in range(size)]
    counts = [max(1, int(round(d / h * rng.uniform(0.8, 1.25)))) for d in dims]
    if family == "L":
        dims = l_counts(*dims)
        counts = l_counts(*counts)
    count_sides = function(*counts)
    length_sides = function(*dims)
    ratios = {}
    splits = {}
    for s, (_, n) in enumerate(count_sides):
        if n >= 2 and rng.random() < 0.15:
            ratios[s] = rng.uniform(0.85, 1.18)
        if n >= 2 and rng.random() < 0.15:
            fraction = rng.uniform(0.2, 0.8)
            first = min(n - 1, max(1, int(round(n * fraction))))
            splits[s] = ([first, n - first], [fraction, 1.0 - fraction])
    surface = Plane() if rng.random() < 0.8 else Cylinder(rng.uniform(10.0, 40.0))
    transform = shear(rng.uniform(-15.0, 15.0)) if rng.random() < 0.3 else None
    name = "sweep_%03d_%s_%s" % (index, family, surface.name)
    directions, edges = build(surface, count_sides, length_sides, transform=transform, ratios=ratios, splits=splits)
    return name, surface, directions, edges


def group_sweep(count=300):
    rng = random.Random(20261115)
    worst = (1.0, None)
    global PLOT_DIR
    plot_dir = PLOT_DIR
    PLOT_DIR = None
    total_quads = 0
    fixed = []
    min_sjs = []
    try:
        for index in range(count):
            name, surface, directions, edges = random_case(rng, index)
            grid, uv, logical, min_sj, folded = check_grid(name, surface, directions, edges, allow_fixed_folds=True)
            total_quads += len(grid.Quads)
            if folded:
                fixed.append((name, folded, grid, directions, edges))
                continue
            check(min_sj > SWEEP_BOUND, name + ": the smallest scaled Jacobian %.3f is below %.2f" % (min_sj, SWEEP_BOUND))
            min_sjs.append(min_sj)
            if min_sj < worst[0]:
                worst = (min_sj, (name, surface, directions, edges))
    finally:
        PLOT_DIR = plot_dir
    mean = sum(min_sjs) / len(min_sjs)
    print("sweep: %d cases, %d quads, worst min SJ %.3f in %s, mean min SJ %.3f" %
          (count, total_quads, worst[0], worst[1][0], mean))
    check(mean > SWEEP_MEAN_BOUND, "sweep: the mean smallest scaled Jacobian %.3f is below %.2f" % (mean, SWEEP_MEAN_BOUND))
    # a quad of boundary nodes only, in a web one cell thick, can fold however the edges divide; the mapping cannot
    # help it, and must not let it spread to the quads it places
    for name, folded, grid, directions, edges in fixed:
        print("sweep: %s folds %d quads at corners the edges fix: directions %s counts %s" %
              (name, folded, directions, [len(edge) - 1 for edge in edges]))
    check(len(fixed) <= MAX_FIXED_FOLD_CASES, "sweep: %d cases fold where the edges fix the quads" % len(fixed))
    if PLOT_DIR:
        name, surface, directions, edges = worst[1]
        check_grid("sweep_worst", surface, directions, edges)
    return worst[0]


# Refusals ---------------------------------------------------------------------------------------------------------
def refused(directions, edges_uv, edges_xyz, words):
    try:
        SubmapMapping.Map(Array[Int32](directions), to_net(edges_uv), to_net(edges_xyz), None)
    except Exception as e:
        message = str(e)
        check(all(word in message for word in words), "the refusal '%s' does not name %s" % (message, words))
        return message
    raise Exception("TEST FAILED: the mapping accepted a loop it must refuse: " + str(words))


def counts_valid(directions, counts):
    result = SubmapMapping.AreCountsValid(Array[Int32](directions), Array[Int32](counts), None)
    return bool(result[0]), result[1]


def group_refusals():
    plane = Plane()
    sides = shape_l(10, 8, 4, 3)
    directions, edges = build(plane, sides, scaled(sides, 1.0))
    xyz = [[plane.eval(*uv) for uv in edge] for edge in edges]
    messages = []
    # counts that do not close: one more element on the bottom
    bad = [list(edge) for edge in edges]
    bad[0] = distribute(plane, edges[0][0], edges[0][-1], len(edges[0]), 1.0)
    bad_xyz = [[plane.eval(*uv) for uv in edge] for edge in bad]
    messages.append(refused(directions, bad, bad_xyz, ["does not close"]))
    # a U whose notch is as deep as the web is tall: the notch touches the bottom
    u_sides = shape_u(3, 4, 2, 0, 5)
    u_directions, u_edges = build(plane, [s for s in u_sides], [(d, max(n, 1)) for d, n in u_sides])
    u_xyz = [[plane.eval(*uv) for uv in edge] for edge in u_edges]
    messages.append(refused(u_directions, u_edges, u_xyz, ["touch itself"]))
    # a reversal: an edge that runs straight back
    r_directions = [0, 1, 3, 1, 2, 3]
    r_edges = [[[0, 0], [1, 0], [2, 0]], [[2, 0], [2, 1]], [[2, 1], [2, 0.5]], [[2, 0.5], [2, 1], [2, 2]],
               [[2, 2], [1, 2], [0, 2]], [[0, 2], [0, 1], [0, 0]]]
    r_xyz = [[plane.eval(*uv) for uv in edge] for edge in r_edges]
    messages.append(refused(r_directions, r_edges, r_xyz, ["opposite directions", "reversal"]))
    # a clockwise loop
    cw_directions = [1, 0, 3, 2]
    cw_edges = [[[0, 0], [0, 1]], [[0, 1], [1, 1]], [[1, 1], [1, 0]], [[1, 0], [0, 0]]]
    cw_xyz = [[plane.eval(*uv) for uv in edge] for edge in cw_edges]
    messages.append(refused(cw_directions, cw_edges, cw_xyz, ["clockwise"]))
    # a direction out of range
    messages.append(refused([0, 1, 4, 3], cw_edges, cw_xyz, ["direction 4"]))
    # arrays that disagree
    messages.append(refused(directions[:-1], edges, xyz, ["edge directions", "edges of parameters"]))
    messages.append(refused(directions, edges, xyz[:-1], ["edges of coordinates"]))
    short = [list(e) for e in xyz]
    short[2] = short[2][:-1]
    messages.append(refused(directions, edges, short, ["Edge 2", "coordinates of its nodes"]))
    for message in messages:
        print("refused: " + message.splitlines()[0])
    # the cheap check stage 2 calls before meshing
    ok, reason = counts_valid([0, 1, 2, 1, 2, 3], [10, 3, 6, 5, 4, 8])
    check(ok and reason is None, "AreCountsValid refuses a valid L: " + str(reason))
    ok, reason = counts_valid([0, 1, 2, 1, 2, 3], [10, 3, 6, 5, 4, 7])
    check(not ok and "does not close" in reason, "AreCountsValid accepts counts that do not close")
    ok, reason = counts_valid([d for d, _ in u_sides], [max(n, 1) for _, n in u_sides])
    check(not ok and "touch itself" in reason, "AreCountsValid accepts an outline that touches itself")
    ok, reason = counts_valid([0, 1, 2, 3], [1, 1, 1, 0])
    check(not ok and "needs one" in reason, "AreCountsValid accepts an edge without elements")
    ok, reason = counts_valid([0, 1, 2], [1, 1, 1])
    check(not ok and "at least four" in reason, "AreCountsValid accepts three edges")
    print("refusals: %d messages, AreCountsValid agrees" % len(messages))


# Holes ------------------------------------------------------------------------------------------------------------
def loop_edges(surface, start, sides, h, transform=None):
    """The edges of a loop of uniform divisions from the start point, side by side as (direction, count)."""
    directions = []
    edges = []
    x, y = start
    move = transform or (lambda q: q)
    for direction, count in sides:
        x1 = x + DI[direction] * count * h
        y1 = y + DJ[direction] * count * h
        directions.append(direction)
        edges.append(distribute(surface, surface.to_uv(*move([x, y])), surface.to_uv(*move([x1, y1])), count, 1.0))
        x, y = x1, y1
    for k in range(len(edges)):
        edges[k][-1] = list(edges[(k + 1) % len(edges)][0])
    return directions, edges


def to_loops(loops):
    """The loops as the nested arrays of the multi-loop mapping: directions, parameters and coordinates."""
    return (Array[Array[Int32]]([Array[Int32](directions) for directions, _, _ in loops]),
            Array[Array[Array[Array[Double]]]]([to_net(edges) for _, edges, _ in loops]),
            Array[Array[Array[Array[Double]]]]([to_net(xyz) for _, _, xyz in loops]))


def counts_valid_loops(loops, offsets):
    directions = Array[Array[Int32]]([Array[Int32](d) for d, _ in loops])
    counts = Array[Array[Int32]]([Array[Int32](c) for _, c in loops])
    net_offsets = None if offsets is None else Array[Array[Int32]]([Array[Int32](o) for o in offsets])
    result = SubmapMapping.AreCountsValid(directions, counts, net_offsets, None)
    return bool(result[0]), result[1]


def check_holes_grid(name, surface, loops, offsets, exact_h=None):
    """Maps the loops, the outer one first, with the holes at the offsets: as many quads as the cells inside the outer
    outline and outside the holes, as many inner nodes as Euler's formula gives for an outline with holes, four quads
    around every inner node, no fold, every inner node inside the outer outline and outside the holes, and with
    exact_h every inner node at its lattice position."""
    loops = [(d, e, [[surface.eval(*uv) for uv in edge] for edge in e]) for d, e in loops]
    directions, net_uv, net_xyz = to_loops(loops)

    def evaluate(flat):
        xyz = []
        for i in range(len(flat) // 2):
            xyz.extend(surface.eval(flat[2 * i], flat[2 * i + 1]))
        return Array[Double](xyz)

    grid = SubmapMapping.Map(directions, net_uv, net_xyz, Array[Array[Int32]]([Array[Int32](o) for o in offsets]),
                             Func[Array[Double], Array[Double]](evaluate))
    outlines = [[uv for edge in e for uv in edge[:-1]] for _, e, _ in loops]
    boundary = sum(len(outline) for outline in outlines)
    area = 0
    for l, (d, e, _) in enumerate(loops):
        loop_area, _ = lattice_counts(d, [len(edge) - 1 for edge in e])
        area += loop_area
    inner = area - boundary // 2 + 1 - (len(loops) - 1)
    quads = [[int(n) for n in quad] for quad in grid.Quads]
    inner_uv = [[float(c) for c in uv] for uv in grid.InnerUv]
    check(grid.NumberOfBoundaryNodes == boundary, name + ": %d boundary nodes, not %d" %
          (grid.NumberOfBoundaryNodes, boundary))
    check(len(quads) == area, name + ": %d quads, but the outline with its holes encloses %d cells" %
          (len(quads), area))
    check(len(inner_uv) == inner, name + ": %d inner nodes, but Euler's formula gives %d" % (len(inner_uv), inner))
    check(grid.NumberOfFoldedQuads == 0, name + ": %d quads fold" % grid.NumberOfFoldedQuads)
    uv = [p for outline in outlines for p in outline] + inner_uv
    valence = [0] * len(uv)
    loop_sign = 1.0 if signed_area(outlines[0]) > 0 else -1.0
    min_sj = 1.0
    for quad in quads:
        for node in quad:
            valence[node] += 1
        for c in range(4):
            a = sub(uv[quad[(c + 1) % 4]], uv[quad[c]])
            b = sub(uv[quad[(c + 3) % 4]], uv[quad[c]])
            min_sj = min(min_sj, loop_sign * (a[0] * b[1] - a[1] * b[0]) / (norm(a) * norm(b)))
    for n in range(boundary, len(uv)):
        check(valence[n] == 4, name + ": inner node %d has %d quads" % (n, valence[n]))
    for i, p in enumerate(inner_uv):
        check(point_in_polygon(p, outlines[0]) and not any(point_in_polygon(p, o) for o in outlines[1:]),
              name + ": inner node %d is outside the face" % i)
    if exact_h is not None:
        logical = [[int(c) for c in ij] for ij in grid.LogicalCoordinates]
        i0, j0 = logical[0]
        size = max(abs(c) for p in uv for c in p)
        deviation = max(max(abs(uv[n][0] - (logical[n][0] - i0) * exact_h),
                            abs(uv[n][1] - (logical[n][1] - j0) * exact_h)) for n in range(boundary, len(uv)))
        check(deviation <= TOLERANCE * size, name + ": the inner nodes are off their lattice positions by %g" %
              deviation)
    check(min_sj > HOLES_BOUND, name + ": the smallest scaled Jacobian %.3f is below %.2f" % (min_sj, HOLES_BOUND))
    if PLOT_DIR:
        write_svg(name, surface, outlines[0], uv, quads, min_sj)
    return grid, min_sj


HOLES_BOUND = 0.5
RECTANGLE = [(0, 10), (1, 8), (2, 10), (3, 8)]
SQUARE_HOLE = [(1, 2), (0, 4), (3, 2), (2, 4)]


def group_holes():
    """Outlines with holes given with their lattice offsets: a rectangle with a centred hole, with two holes and with an
    L-shaped hole, an L with a square hole, all uniform, where every inner node sits at its lattice position; the
    centred hole sheared and on a cylinder; the offsets SubmapHolePlacement finds from the geometry; and the reasons
    the multi-loop AreCountsValid gives."""
    worst = 1.0
    plane = Plane()
    h = 2.5
    l_hole = [(1, 4), (0, 4), (3, 2), (2, 2), (3, 2), (2, 2)]
    cases = [("holes_centred", plane, RECTANGLE, [((3, 3), SQUARE_HOLE)], None),
             ("holes_two", plane, [(0, 14), (1, 8), (2, 14), (3, 8)], [((2, 2), SQUARE_HOLE), ((8, 3), SQUARE_HOLE)],
              None),
             ("holes_L_shaped", plane, [(0, 10), (1, 10), (2, 10), (3, 10)], [((3, 3), l_hole)], None),
             ("holes_in_L", plane, shape_l(12, 10, 6, 4), [((5, 1), [(1, 2), (0, 2), (3, 2), (2, 2)])], None),
             ("holes_sheared", plane, RECTANGLE, [((3, 3), SQUARE_HOLE)], shear(15)),
             ("holes_cylinder", Cylinder(20.0), RECTANGLE, [((3, 3), SQUARE_HOLE)], None)]
    for name, surface, outer_sides, holes, transform in cases:
        loops = [loop_edges(surface, (0.0, 0.0), outer_sides, h, transform)]
        offsets = []
        for offset, sides in holes:
            loops.append(loop_edges(surface, (offset[0] * h, offset[1] * h), sides, h, transform))
            offsets.append(list(offset))
        exact = h if surface is plane and transform is None else None
        grid, min_sj = check_holes_grid(name, surface, loops, offsets, exact)
        # The placement finds the offsets the geometry gives, on the parameters of the nodes
        directions = [d for d, _ in loops]
        counts = [[len(edge) - 1 for edge in e] for _, e in loops]
        points = [[uv for edge in e for uv in edge[:-1]] for _, e in loops]
        result = SubmapHolePlacement.TryPlace(Array[Array[Int32]]([Array[Int32](d) for d in directions]),
                                              Array[Array[Int32]]([Array[Int32](c) for c in counts]),
                                              Array[Array[Array[Double]]]([Array[Array[Double]]([Array[Double](p)
                                                                                                  for p in loop])
                                                                           for loop in points]), 1, None, None)
        placed = [[int(c) for c in o] for o in result[1]] if result[0] else None
        check(placed == offsets, name + ": the placement finds the offsets %s, not %s (%s)" %
              (placed, offsets, result[2]))
        worst = min(worst, min_sj)
        print("%-22s loops %d  quads %4d  inner nodes %4d  offsets %s  min SJ %.3f" %
              (name, len(loops), len(grid.Quads), grid.NumberOfInnerNodes, placed, min_sj))
    # Mean value coordinates reproduce a linear map on an outline that is not convex
    l_outline = [[0.0, 0.0], [10.0, 0.0], [10.0, 4.0], [4.0, 4.0], [4.0, 10.0], [0.0, 10.0]]
    values = [[2 * x - 3 * y + 1, x + y] for x, y in l_outline]
    for point in [[2.0, 2.0], [7.0, 1.5], [1.0, 8.0], [3.9, 3.9]]:
        value = SubmapHolePlacement.InterpolateMeanValue(Array[Array[Double]]([Array[Double](p) for p in l_outline]),
                                                         Array[Array[Double]]([Array[Double](v) for v in values]),
                                                         Array[Double](point))
        expected = [2 * point[0] - 3 * point[1] + 1, point[0] + point[1]]
        check(abs(value[0] - expected[0]) < 1E-9 and abs(value[1] - expected[1]) < 1E-9,
              "the mean value coordinates do not reproduce a linear map at %s: %s" % (point, list(value)))
    # The reasons of the multi-loop validity
    outer = ([d for d, _ in RECTANGLE], [n for _, n in RECTANGLE])
    hole = ([d for d, _ in SQUARE_HOLE], [n for _, n in SQUARE_HOLE])
    ok, reason = counts_valid_loops([outer, hole], [[3, 3]])
    check(ok and reason is None, "a rectangle with a centred hole is refused: " + str(reason))
    ok, reason = counts_valid_loops([outer, hole], None)
    check(ok, "the loops on their own are refused: " + str(reason))
    reasons = []
    for loops, offsets, words in [([outer, hole], [[0, 3]], ["touch itself"]),
                                  ([outer, hole], [[12, 3]], ["Edge 4", "outside"]),
                                  ([outer, hole, hole], [[2, 2], [3, 3]], ["touch itself"]),
                                  ([outer, ([0, 1, 2, 3], [1, 1, 1, 1])], [[3, 3]],
                                   ["counter-clockwise", "right turns"]),
                                  ([outer, ([1, 0, 3, 2], [2, 4, 2, 3])], [[3, 3]], ["hole starting at Edge 4",
                                                                                     "does not close"]),
                                  ([outer, hole], [], ["1 holes and 0 hole offsets"]),
                                  ([([0, 1, 2, 3], [20, 20, 20, 20]), ([1, 0, 3, 2], [10, 10, 10, 10]), hole],
                                   [[5, 5], [8, 8]], ["Edge 8", "inside the hole starting at Edge 4"])]:
        ok, reason = counts_valid_loops(loops, offsets)
        check(not ok and all(word in reason for word in words), "the multi-loop validity gives '%s', not %s" %
              (reason, words))
        reasons.append(reason)
    for reason in reasons:
        print("refused: " + reason)
    return worst


def group_timing():
    plane = Plane()
    sides = shape_l(100, 100, 50, 50)
    directions, edges = build(plane, sides, scaled(sides, 1.0))
    edges_xyz = [[plane.eval(*uv) for uv in edge] for edge in edges]
    net_uv = to_net(edges)
    net_xyz = to_net(edges_xyz)
    net_directions = Array[Int32](directions)
    times = []
    for _ in range(3):
        start = time.perf_counter()
        grid = SubmapMapping.Map(net_directions, net_uv, net_xyz, None)
        times.append(time.perf_counter() - start)
    check(grid.NumberOfFoldedQuads == 0, "timing L: quads fold")
    check(len(grid.Quads) == 7500, "timing L: %d quads, not 7500" % len(grid.Quads))
    print("timing: 100 x 100 cell L, %d quads, %d inner nodes, %.3f s (first %.3f s)" %
          (len(grid.Quads), len(grid.InnerUv), min(times), times[0]))
    check(min(times) < TIME_BOUND, "timing L: %.3f s is above %.1f s" % (min(times), TIME_BOUND))
    return min(times)


def main():
    if PLOT_DIR and not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    results = [("uniform", group_uniform()), ("graded and split", group_graded()), ("skewed", group_skewed()),
               ("curved", group_curved()), ("sweep", group_sweep()), ("holes", group_holes())]
    group_refusals()
    group_timing()
    for name, worst in results:
        print("worst min SJ %-18s %.3f" % (name, worst))
    print("TEST PASSED")


main()
