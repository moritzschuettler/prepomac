#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks ThreeBlockMapping, the structured mapping of three-sided faces that gmsh, Netgen and QuadWild share, on
#its own, without a mesher: the three sides are given as nodes along them, and the surface as a function.
#
#  - The cases: a right angle, an equilateral triangle, the 60 x 40 rectangle split along its diagonal (34 degrees),
#    corners of 10 and 5 degrees, a quarter disk, the deltoid limit (the side opposite a sharp corner with 2
#    elements), a graded side, a side of two edges at different sizes, and triangles on a cylinder and on a sphere.
#  - No grid folds, every inner node lies on the surface, the singular node has three quads around it, and the
#    smallest scaled Jacobian is within 10% of the best any grid can have: the sine of the sharpest corner, or of
#    120 degrees, since the three quads at the singular node share 360 degrees there.
#  - The sharp corner of a deltoid case is a single quad that reaches the singular node.
#  - The grids equal the test vectors in CaeMesh/Meshing/Transfinite/Cpp/three_block_vectors.txt, which the C++ port
#    three_block_mapping.h must reproduce too (three_block_mapping_check.cpp). With the environment variable
#    PMX_WRITE_THREE_BLOCK_VECTORS=1 the vectors are written instead, after a change to the mapping.
#
#  PrePoMax.com -u MM_TON_S_C --script Test_Model_Meshing_Three_Block_Mapping.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import io
import math
import os
import clr
clr.AddReference('CaeMesh')
from CaeMesh import ThreeBlockMapping
from System import Array, Double, Func

VECTORS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "CaeMesh", "Meshing",
                       "Transfinite", "Cpp", "three_block_vectors.txt")
TOLERANCE = 1E-9


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Surfaces ---------------------------------------------------------------------------------------------------------
def plane(u, v):
    return [u, v, 0.0]


def cylinder(radius):
    return lambda u, v: [radius * math.cos(u), radius * math.sin(u), v]


def sphere(radius):
    return lambda u, v: [radius * math.cos(v) * math.cos(u), radius * math.cos(v) * math.sin(u), radius * math.sin(v)]


def normal(surface, u, v, eps=1E-6):
    a = [(p - q) / (2 * eps) for p, q in zip(surface(u + eps, v), surface(u - eps, v))]
    b = [(p - q) / (2 * eps) for p, q in zip(surface(u, v + eps), surface(u, v - eps))]
    return cross(a, b)


def cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def dot(a, b):
    return sum(p * q for p, q in zip(a, b))


def sub(a, b):
    return [p - q for p, q in zip(a, b)]


def norm(a):
    return math.sqrt(dot(a, a))


# Sides --------------------------------------------------------------------------------------------------------------
def line(a, b):
    return lambda t: [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]


def arc(radius, t0, t1):
    return lambda t: [radius * math.cos(t0 + (t1 - t0) * t), radius * math.sin(t0 + (t1 - t0) * t)]


def part(curve, t0, t1):
    return lambda t: curve(t0 + (t1 - t0) * t)


def distribute(surface, curve, n, ratio=1.0):
    """n elements along the curve, of equal length in space or in a geometric progression."""
    samples = 2000
    ts = [i / float(samples) for i in range(samples + 1)]
    points = [surface(*curve(t)) for t in ts]
    lengths = [0.0]
    for i in range(1, len(points)):
        lengths.append(lengths[-1] + norm(sub(points[i], points[i - 1])))
    weights = [ratio ** i for i in range(n)]
    targets = [0.0]
    for w in weights:
        targets.append(targets[-1] + w)
    targets = [lengths[-1] * t / targets[-1] for t in targets]
    uv = []
    j = 0
    for target in targets:
        while j < samples - 1 and lengths[j + 1] < target:
            j += 1
        span = lengths[j + 1] - lengths[j]
        f = 0.0 if span <= 0 else min(1.0, max(0.0, (target - lengths[j]) / span))
        uv.append(curve(ts[j] + f * (ts[j + 1] - ts[j])))
    uv[0] = curve(0.0)
    uv[-1] = curve(1.0)
    return uv


def chain(surface, curve, counts):
    """A side of edges of equal length, edge e carrying counts[e] elements."""
    uv = []
    for e, n in enumerate(counts):
        nodes = distribute(surface, part(curve, e / float(len(counts)), (e + 1) / float(len(counts))), n)
        uv.extend(nodes if e == 0 else nodes[1:])
    return uv


def isoceles(degrees, length=60.0):
    t = math.radians(degrees / 2.0)
    return [0.0, 0.0], [length * math.cos(t), -length * math.sin(t)], [length * math.cos(t), length * math.sin(t)]


def triangle(surface, corners, counts, ratios=(1.0, 1.0, 1.0)):
    return [distribute(surface, line(corners[k], corners[(k + 1) % 3]), counts[k], ratios[k]) for k in range(3)]


def cases():
    out = []
    out.append(("right_45_45", plane, triangle(plane, [[0, 0], [50, 0], [0, 50]], [12, 18, 12])))
    h = 25 * math.sqrt(3)
    out.append(("equilateral", plane, triangle(plane, [[0, 0], [50, 0], [25, h]], [12, 12, 12])))
    out.append(("diagonal_34", plane, triangle(plane, [[0, 0], [60, 0], [0, 40]], [15, 17, 10])))
    out.append(("sharp_10", plane, triangle(plane, isoceles(10), [29, 5, 30])))
    out.append(("sharp_5", plane, triangle(plane, isoceles(5), [29, 3, 30])))
    r = 30.0
    out.append(("quarter_disk", plane, [distribute(plane, line([0, 0], [r, 0]), 10),
                                        distribute(plane, arc(r, 0, math.pi / 2), 16),
                                        distribute(plane, line([0, r], [0, 0]), 10)]))
    out.append(("deltoid_10", plane, triangle(plane, isoceles(10), [15, 2, 15])))
    out.append(("deltoid_30", plane, triangle(plane, isoceles(30), [15, 2, 15])))
    out.append(("deltoid_all", plane, triangle(plane, [[0, 0], [50, 0], [25, h]], [2, 2, 2])))
    out.append(("graded_34", plane, triangle(plane, [[0, 0], [60, 0], [0, 40]], [15, 17, 10], (1.12, 1.0, 1.0))))
    out.append(("chained_34", plane, [chain(plane, line([0, 0], [60, 0]), [14, 6])] +
                triangle(plane, [[0, 0], [60, 0], [0, 40]], [20, 23, 13])[1:]))
    surface = cylinder(20.0)
    out.append(("cylinder", surface, triangle(surface, [[0, 0], [math.pi / 2, 0], [0, 40]], [10, 17, 13])))
    surface = sphere(30.0)
    out.append(("sphere", surface, triangle(surface, [[0, 0], [1.0, 0], [0, 1.0]], [12, 16, 12])))
    return out


# The mapping ---------------------------------------------------------------------------------------------------
def to_net(sides):
    return Array[Array[Array[Double]]]([Array[Array[Double]]([Array[Double](p) for p in side]) for side in sides])


def run(surface, sides_uv):
    sides_xyz = [[surface(*uv) for uv in side] for side in sides_uv]

    def evaluate(flat):
        xyz = []
        for i in range(len(flat) // 2):
            xyz.extend(surface(flat[2 * i], flat[2 * i + 1]))
        return Array[Double](xyz)

    grid = ThreeBlockMapping.Map(to_net(sides_uv), to_net(sides_xyz), Func[Array[Double], Array[Double]](evaluate))
    inner_uv = [[float(c) for c in uv] for uv in grid.InnerUv]
    inner_xyz = [[float(c) for c in p] for p in grid.InnerXyz]
    quads = [[int(n) for n in quad] for quad in grid.Quads]
    return grid, sides_xyz, inner_uv, inner_xyz, quads


def corner_angle(points, k):
    a = sub(points[1], points[0])
    b = sub(points[-1], points[0])
    return math.degrees(math.acos(max(-1.0, min(1.0, dot(a, b) / (norm(a) * norm(b))))))


def check_case(name, surface, sides_uv):
    grid, sides_xyz, inner_uv, inner_xyz, quads = run(surface, sides_uv)
    check(grid.NumberOfFoldedQuads == 0, name + ": " + str(grid.NumberOfFoldedQuads) + " quads fold")
    num_boundary = grid.NumberOfBoundaryNodes
    boundary_uv = [uv for side in sides_uv for uv in side[:-1]]
    uv = boundary_uv + inner_uv
    xyz = [p for side in sides_xyz for p in side[:-1]] + inner_xyz
    for i, p in enumerate(inner_xyz):
        check(norm(sub(p, surface(*inner_uv[i]))) < 1E-9, name + ": inner node " + str(i) + " is off the surface")
    # the sharpest corner angle in space and the singular node bound the smallest scaled Jacobian
    corners = []
    for k in range(3):
        previous = sides_xyz[(k + 2) % 3]
        corners.append(corner_angle([sides_xyz[k][0], sides_xyz[k][1], previous[-2]], k))
    bound = min(math.sin(math.radians(min(corners))), math.sin(math.radians(120)))
    orientation = 1.0 if sum(boundary_uv[i][0] * boundary_uv[(i + 1) % num_boundary][1] -
                             boundary_uv[(i + 1) % num_boundary][0] * boundary_uv[i][1]
                             for i in range(num_boundary)) > 0 else -1.0
    min_sj = 1.0
    for quad in quads:
        n = normal(surface, *[sum(uv[i][d] for i in quad) / 4 for d in range(2)])
        for c in range(4):
            e1 = sub(xyz[quad[(c + 1) % 4]], xyz[quad[c]])
            e2 = sub(xyz[quad[(c + 3) % 4]], xyz[quad[c]])
            min_sj = min(min_sj, orientation * dot(cross(e1, e2), n) / (norm(e1) * norm(e2) * norm(n)))
    check(min_sj > 0.9 * bound, name + ": the smallest scaled Jacobian %.3f is far below the corner bound %.3f" %
          (min_sj, bound))
    # the singular node has three quads around it
    valence = {}
    for quad in quads:
        for node in quad:
            valence[node] = valence.get(node, 0) + 1
    centre = num_boundary
    check(valence[centre] == 3, name + ": the singular node has " + str(valence[centre]) + " quads around it")
    counts = [len(side) - 1 for side in sides_uv]
    for k in range(3):
        if counts[(k + 1) % 3] == 2:
            at_corner = [quad for quad in quads if grid.GetBoundaryNodeId(k, 0) in quad]
            check(len(at_corner) == 1 and centre in at_corner[0],
                  name + ": the corner opposite the side of 2 elements is not a single quad at the singular node")
    print("%-14s sides %-14s quads %4d  min SJ %.3f  bound %.3f" % (name, counts, len(quads), min_sj, bound))
    return sides_uv, sides_xyz, inner_uv, quads, grid.NumberOfFoldedQuads


# Test vectors -----------------------------------------------------------------------------------------------------
def write_vectors(results):
    lines = ["# Test vectors of ThreeBlockMapping (C#) and three_block_mapping.h (C++), written by",
             "# Test_Model_Meshing_Three_Block_Mapping.py. Per case: the three sides as u v x y z of every node from",
             "# corner k to corner k + 1, then the inner node parameters u v, the quads by node id and the folded",
             "# quads. Node ids run along the boundary loop first, then over the inner nodes."]
    for name, (sides_uv, sides_xyz, inner_uv, quads, folded) in results:
        lines.append("case " + name)
        for k in range(3):
            lines.append("side %d %d" % (k, len(sides_uv[k])))
            for uv, p in zip(sides_uv[k], sides_xyz[k]):
                lines.append(" ".join("%.17g" % c for c in list(uv) + list(p)))
        lines.append("inner %d" % len(inner_uv))
        lines.extend("%.17g %.17g" % tuple(uv) for uv in inner_uv)
        lines.append("quads %d" % len(quads))
        lines.extend("%d %d %d %d" % tuple(quad) for quad in quads)
        lines.append("folded %d" % folded)
        lines.append("end")
    with io.open(VECTORS, "w", encoding="utf-8", newline="\n") as writer:
        writer.write("\n".join(lines) + "\n")
    print("written:", os.path.abspath(VECTORS))


def read_vectors():
    cases = {}
    with io.open(VECTORS, encoding="utf-8") as reader:
        tokens = [line.split() for line in reader if line.strip() and not line.startswith("#")]
    i = 0
    while i < len(tokens):
        name = tokens[i][1]
        i += 1
        sides = []
        for k in range(3):
            n = int(tokens[i][2])
            sides.append([[float(c) for c in row] for row in tokens[i + 1:i + 1 + n]])
            i += 1 + n
        n = int(tokens[i][1])
        inner = [[float(c) for c in row] for row in tokens[i + 1:i + 1 + n]]
        i += 1 + n
        n = int(tokens[i][1])
        quads = [[int(c) for c in row] for row in tokens[i + 1:i + 1 + n]]
        i += 1 + n
        folded = int(tokens[i][1])
        i += 2
        cases[name] = (sides, inner, quads, folded)
    return cases


def compare_vectors(results):
    expected = read_vectors()
    check(sorted(expected) == sorted(name for name, _ in results),
          "the test vectors hold other cases: " + str(sorted(expected)))
    for name, (sides_uv, sides_xyz, inner_uv, quads, folded) in results:
        sides, expected_uv, expected_quads, expected_folded = expected[name]
        # the mapping run again on the inputs as the file stores them, which is what the C++ check reads
        side_uv = [[row[:2] for row in side] for side in sides]
        side_xyz = [[row[2:] for row in side] for side in sides]
        grid = ThreeBlockMapping.Map(to_net(side_uv), to_net(side_xyz), None)
        uv = [[float(c) for c in p] for p in grid.InnerUv]
        got_quads = [[int(n) for n in quad] for quad in grid.Quads]
        size = max(abs(c) for side in side_uv for p in side for c in p) or 1.0
        check(got_quads == expected_quads, name + ": the quads differ from the test vectors")
        check(grid.NumberOfFoldedQuads == expected_folded, name + ": the folded quads differ from the test vectors")
        deviation = max([abs(a - b) for p, q in zip(uv, expected_uv) for a, b in zip(p, q)] or [0.0])
        check(len(uv) == len(expected_uv) and deviation <= TOLERANCE * size,
              name + ": the inner nodes differ from the test vectors by %g" % deviation)
    print("test vectors:", len(results), "cases agree")


def main():
    results = [(name, check_case(name, surface, sides)) for name, surface, sides in cases()]
    if os.environ.get("PMX_WRITE_THREE_BLOCK_VECTORS") == "1":
        write_vectors(results)
    compare_vectors(results)
    print("PASSED")


main()
