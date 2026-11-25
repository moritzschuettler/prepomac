#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks RingMapping, the structured mapping of a face of two boundary loops that run roughly parallel, on its own,
#without a mesher: each loop is given as its nodes, as parameters and coordinates, the two the same way round with as
#many nodes, and the surface as a function.
#
#  - A concentric annulus: the layers GetNumberOfLayers gives, (layers - 1) x N inner nodes, N x layers quads, every
#    quad turned the way the outer loop turns, nodes on the rays and corners near right angles.
#  - The shift: the inner loop started at another node is joined to the same nodes, the grid the same up to its
#    numbering; an inner loop whose nodes are turned by 40 degrees is joined to the nearest ones.
#  - The layers: a thin washer has one layer and no inner node; a thin rectangular tube with as many nodes along each
#    pair of sides one layer, its corner quads with corners of 45 degrees; a wide annulus has more layers the finer its
#    loops.
#  - Eccentric annuli from a concentric one to a bore that almost touches the outer loop: no fold, and a worst scaled
#    Jacobian that falls with the eccentricity; the one of 10.5 mm is above the floor of 0.2, the one of 11.9 mm below
#    it even after the smoothing a grid below the floor is given.
#  - A ring on a paraboloid: every inner node on the surface.
#  - Loops of different numbers of nodes, of fewer than three, that run different ways round, whose parameters do not
#    run around them and no layer are refused with a message that names the reason.
#  - A sweep over 200 pseudo-random rings with a fixed seed, star-shaped outer loops of two harmonics, inner loops of
#    other harmonics scaled into them, off the centre and started at a random node: no fold, as many quads and inner
#    nodes as the layers give.
#  - A ring of 400 nodes and 40 layers is mapped in under the time bound.
#  With the environment variable PMX_RING_PLOT_DIR set, the named cases and the worst case of the sweep are written into
#  that folder as SVG drawings of the loops and the quads.
#
#  PrePoMax.com -u MM_TON_S_C --script Test_Model_Meshing_Ring_Mapping.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import io
import math
import os
import random
import time
import clr
clr.AddReference('CaeMesh')
from CaeMesh import RingMapping
from CaeGlobals import CaeException
from System import Array, Double, Func

TIME_BOUND = 2.0
PLOT_DIR = os.environ.get("PMX_RING_PLOT_DIR")


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Loops ----------------------------------------------------------------------------------------------------------------
def plane(u, v):
    return [u, v, 0.0]


def paraboloid(u, v):
    return [u, v, (u * u + v * v) / 100.0]


def circle(cx, cy, r, n, start_deg=0.0):
    return [[cx + r * math.cos(math.radians(start_deg) + 2 * math.pi * i / n),
             cy + r * math.sin(math.radians(start_deg) + 2 * math.pi * i / n)] for i in range(n)]


def rectangle(x0, y0, x1, y1, nx, ny):
    """The nodes of a rectangle counter-clockwise from (x0, y0), nx along x and ny along y."""
    points = []
    for i in range(nx):
        points.append([x0 + (x1 - x0) * i / nx, y0])
    for i in range(ny):
        points.append([x1, y0 + (y1 - y0) * i / ny])
    for i in range(nx):
        points.append([x1 - (x1 - x0) * i / nx, y1])
    for i in range(ny):
        points.append([x0, y1 - (y1 - y0) * i / ny])
    return points


def to_net(points):
    return Array[Array[Double]]([Array[Double](p) for p in points])


def evaluator(surface):
    def evaluate(flat):
        xyz = []
        for i in range(len(flat) // 2):
            xyz.extend(surface(flat[2 * i], flat[2 * i + 1]))
        return Array[Double](xyz)
    return Func[Array[Double], Array[Double]](evaluate)


def layers_of(outer, inner, surface=plane):
    return RingMapping.GetNumberOfLayers(to_net(outer), to_net([surface(*p) for p in outer]), to_net(inner),
                                         to_net([surface(*p) for p in inner]))


def map_ring(outer, inner, layers=None, surface=plane):
    if layers is None:
        layers = layers_of(outer, inner, surface)
    return RingMapping.Map(to_net(outer), to_net([surface(*p) for p in outer]), to_net(inner),
                           to_net([surface(*p) for p in inner]), layers, evaluator(surface))


def grid_points(grid, outer, inner):
    return [list(p) for p in outer] + [list(p) for p in inner] + [[p[0], p[1]] for p in grid.InnerUv]


def corner_sine(points, quad, c):
    p, a, b = points[quad[c]], points[quad[(c + 1) % 4]], points[quad[(c + 3) % 4]]
    e1, e2 = (a[0] - p[0], a[1] - p[1]), (b[0] - p[0], b[1] - p[1])
    length = math.hypot(*e1) * math.hypot(*e2)
    return (e1[0] * e2[1] - e1[1] * e2[0]) / length if length > 0 else -1.0


def signed_area(points):
    return 0.5 * sum(points[i][0] * points[(i + 1) % len(points)][1] - points[(i + 1) % len(points)][0] * points[i][1]
                     for i in range(len(points)))


def check_grid(name, grid, outer, inner, exact=True):
    """The topology, the turn of the quads and the worst corner, which the grid reports as it is in the plane."""
    n = len(outer)
    layers = grid.NumberOfLayers
    quads = [list(q) for q in grid.Quads]
    check(grid.NumberOfLoopNodes == n and grid.NumberOfBoundaryNodes == 2 * n, name + ": the loops carry " +
          str(grid.NumberOfLoopNodes) + " nodes, not " + str(n))
    check(len(quads) == n * layers, name + ": " + str(len(quads)) + " quads, not " + str(n * layers))
    check(len(grid.InnerUv) == (layers - 1) * n, name + ": " + str(len(grid.InnerUv)) + " inner nodes, not " +
          str((layers - 1) * n))
    check(grid.InnerXyz is not None and len(grid.InnerXyz) == len(grid.InnerUv), name + ": the inner nodes carry no " +
          "coordinates")
    valence = {}
    for quad in quads:
        for node in quad:
            valence[node] = valence.get(node, 0) + 1
    check(all(valence.get(i, 0) == 2 for i in range(2 * n)), name + ": a node of a loop is not in two quads")
    check(all(valence.get(i, 0) == 4 for i in range(2 * n, 2 * n + len(grid.InnerUv))),
          name + ": an inner node is not in four quads")
    for i in range(n):
        check(grid.GetNodeId(0, i) == i and grid.GetNodeId(layers, i) == n + (i + grid.Shift) % n,
              name + ": the loops are not numbered as joined")
    points = grid_points(grid, outer, inner)
    orientation = 1 if signed_area(outer) > 0 else -1
    sines = [orientation * corner_sine(points, quad, c) for quad in quads for c in range(4)]
    folded = sum(1 for quad in quads if min(orientation * corner_sine(points, quad, c) for c in range(4)) <= 0)
    check(folded == grid.NumberOfFoldedQuads, name + ": " + str(folded) + " quads fold, the grid says " +
          str(grid.NumberOfFoldedQuads))
    check(not exact or abs(min(sines) - grid.MinScaledJacobian) < 1E-6,
          name + ": the worst corner is " + str(min(sines)) + ", the grid says " + str(grid.MinScaledJacobian))
    if PLOT_DIR:
        write_svg(name, points, n, quads, grid.MinScaledJacobian)
    return grid.MinScaledJacobian


def write_svg(name, points, n, quads, min_sj):
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
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
             (width, height, width, height), '<rect width="100%" height="100%" fill="white"/>']
    for quad in quads:
        lines.append('<polygon points="%s" fill="#dbe8f6" stroke="#1f4e8c" stroke-width="0.7"/>' %
                     " ".join(at(points[k]) for k in quad))
    for start in (0, n):
        lines.append('<polygon points="%s" fill="none" stroke="black" stroke-width="2"/>' %
                     " ".join(at(points[k]) for k in range(start, start + n)))
    lines.append('<text x="%.0f" y="%.0f" font-family="sans-serif" font-size="13">%s  quads %d  min SJ %.3f</text>' %
                 (margin, height - 8, name, len(quads), min_sj))
    lines.append('</svg>')
    with io.open(os.path.join(PLOT_DIR, "ring_mapping_" + name.replace(" ", "_") + ".svg"), "w", encoding="utf-8",
                 newline="\n") as writer:
        writer.write("\n".join(lines) + "\n")


# Groups ---------------------------------------------------------------------------------------------------------------
def group_annulus():
    outer = circle(0, 0, 20, 32)
    inner = circle(0, 0, 10, 32)
    layers = layers_of(outer, inner)
    check(layers == 3, "annulus: " + str(layers) + " layers, not 3")
    grid = map_ring(outer, inner)
    quality = check_grid("annulus", grid, outer, inner)
    check(grid.Shift == 0 and grid.NumberOfFoldedQuads == 0 and quality > 0.95,
          "annulus: shift " + str(grid.Shift) + ", worst scaled Jacobian " + str(quality))
    # Every node of a layer on the ray of its outer node
    for k, uv in enumerate(grid.InnerUv):
        i = k % 32
        angle = math.atan2(uv[1], uv[0]) - 2 * math.pi * i / 32
        check(abs(math.sin(angle)) < 1E-6, "annulus: inner node " + str(k) + " is off its ray")
    print("annulus: %d layers, %d quads, worst scaled Jacobian %.3f" % (layers, len(grid.Quads), quality))
    return quality


def group_shift():
    outer = circle(0, 0, 20, 32)
    reference = map_ring(outer, circle(0, 0, 10, 32))
    started = circle(0, 0, 10, 32, 5 * 360.0 / 32)
    grid = map_ring(outer, started)
    check_grid("started elsewhere", grid, outer, started)
    check(grid.Shift == 27, "started elsewhere: shift " + str(grid.Shift) + ", not 27")
    worst = max(math.hypot(a[0] - b[0], a[1] - b[1]) for a, b in zip(reference.InnerUv, grid.InnerUv))
    check(worst < 1E-9, "started elsewhere: the grid differs by " + str(worst))
    turned = circle(0, 0, 10, 32, 40.0)
    grid = map_ring(outer, turned)
    quality = check_grid("turned by 40 degrees", grid, outer, turned)
    # 40 degrees is 3.56 node spacings: the nearest join is a shift of 32 - 4, off by 5 degrees
    check(grid.Shift == 28 and quality > 0.9, "turned by 40 degrees: shift " + str(grid.Shift) + ", worst scaled " +
          "Jacobian " + str(quality))
    print("shift: started 5 nodes on, shift %d and the same grid; turned by 40 degrees, shift %d, worst scaled "
          "Jacobian %.3f" % (27, grid.Shift, quality))
    return quality


def group_layers():
    outer = circle(0, 0, 20, 24)
    inner = circle(0, 0, 17, 24)
    grid = map_ring(outer, inner)
    quality = check_grid("thin washer", grid, outer, inner)
    check(grid.NumberOfLayers == 1 and len(grid.InnerUv) == 0, "thin washer: " + str(grid.NumberOfLayers) + " layers")
    outer = rectangle(0, 0, 60, 40, 12, 8)
    inner = rectangle(2, 2, 58, 38, 12, 8)
    grid = map_ring(outer, inner)
    tube = check_grid("thin tube", grid, outer, inner)
    check(grid.NumberOfLayers == 1 and grid.Shift == 0 and abs(tube - math.sqrt(0.5)) < 0.05,
          "thin tube: " + str(grid.NumberOfLayers) + " layers, shift " + str(grid.Shift) + ", worst scaled Jacobian " +
          str(tube))
    counts = []
    for n in (16, 32, 64):
        counts.append(layers_of(circle(0, 0, 20, n), circle(0, 0, 10, n)))
    check(counts[0] < counts[1] < counts[2], "the layers of a wide annulus do not grow with its nodes: " + str(counts))
    print("layers: thin washer 1, thin tube 1 (worst scaled Jacobian %.3f), annulus of 16, 32, 64 nodes %s" %
          (tube, counts))
    return min(quality, tube)


def group_eccentric():
    outer = circle(0, 0, 20, 48)
    results = []
    for e in (0.0, 2.0, 4.0, 6.0, 8.0, 10.5, 11.5, 11.9):
        inner = circle(e, 0, 8, 48)
        grid = map_ring(outer, inner)
        quality = check_grid("eccentric %.1f" % e, grid, outer, inner)
        check(grid.NumberOfFoldedQuads == 0, "eccentric %.1f: %d quads fold" % (e, grid.NumberOfFoldedQuads))
        results.append((e, grid.NumberOfLayers, quality))
    print("eccentric (offset, layers, worst scaled Jacobian):", [(e, m, round(q, 3)) for e, m, q in results])
    qualities = [q for _, _, q in results]
    check(all(qualities[i] >= qualities[i + 1] - 1E-3 for i in range(len(qualities) - 1)),
          "the worst scaled Jacobian does not fall with the eccentricity: " + str(qualities))
    check(qualities[5] >= 0.2 and qualities[-1] < 0.2, "the offsets 10.5 and 11.9 give " + str(qualities[5]) + " and " +
          str(qualities[-1]))
    return min(qualities[:6])


def group_curved():
    outer = circle(0, 0, 20, 40)
    inner = circle(3, 2, 9, 40, 17.0)
    grid = map_ring(outer, inner, surface=paraboloid)
    quality = check_grid("paraboloid", grid, outer, inner, False)
    worst = 0.0
    for uv, xyz in zip(grid.InnerUv, grid.InnerXyz):
        expected = paraboloid(uv[0], uv[1])
        worst = max(worst, max(abs(a - b) for a, b in zip(expected, xyz)))
    check(worst < 1E-9 and grid.NumberOfFoldedQuads == 0, "paraboloid: the inner nodes are off the surface by " +
          str(worst))
    print("paraboloid: %d layers, %d quads, worst scaled Jacobian %.3f" %
          (grid.NumberOfLayers, len(grid.Quads), quality))
    return quality


def refused(outer, inner, layers, words):
    try:
        map_ring(outer, inner, layers)
    except CaeException as ex:
        message = ex.Message
        check(all(word in message for word in words), "the refusal does not name the reason: " + message)
        return message
    raise Exception("TEST FAILED: the ring was not refused, " + str(words))


def group_refusals():
    outer = circle(0, 0, 20, 24)
    messages = [refused(outer, circle(0, 0, 10, 20), 2, ["24", "20", "must be equal"]),
                refused(circle(0, 0, 20, 2), circle(0, 0, 10, 2), 1, ["fewer than three"]),
                refused(outer, list(reversed(circle(0, 0, 10, 24))), 2, ["same way round"]),
                refused([[2 * math.pi * i / 24, 0.0] for i in range(24)], circle(0, 0, 10, 24), 2,
                        ["do not run around"]),
                refused(outer, circle(0, 0, 10, 24), 0, ["at least one layer"])]
    for message in messages:
        print("refused:", message)


def star(rng, radius):
    """A star-shaped loop of two random harmonics about the origin, as a function of the angle."""
    a1, a2 = rng.uniform(0.0, 0.12), rng.uniform(0.0, 0.08)
    p1, p2 = rng.uniform(0, 2 * math.pi), rng.uniform(0, 2 * math.pi)
    return lambda t: radius * (1 + a1 * math.cos(t + p1) + a2 * math.cos(2 * t + p2))


def group_sweep(count=200):
    rng = random.Random(20261115)
    worst = None
    for index in range(count):
        n = rng.randint(12, 80)
        outer_radius = star(rng, rng.uniform(15.0, 30.0))
        scale = rng.uniform(0.3, 0.75)
        inner_radius = star(rng, 1.0)
        offset = (rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1))
        start = rng.randint(0, n - 1)
        outer = [[outer_radius(t) * math.cos(t), outer_radius(t) * math.sin(t)]
                 for t in [2 * math.pi * i / n for i in range(n)]]
        inner = []
        for i in range(n):
            t = 2 * math.pi * (i + start) / n
            r = scale * outer_radius(t) * inner_radius(t) / 1.2
            inner.append([r * math.cos(t) + offset[0] * outer_radius(0), r * math.sin(t) + offset[1] * outer_radius(0)])
        grid = map_ring(outer, inner)
        quality = check_grid("random %d" % index, grid, outer, inner) if index < 3 or not PLOT_DIR else \
            grid.MinScaledJacobian
        check(grid.NumberOfFoldedQuads == 0, "random ring %d folds %d quads" % (index, grid.NumberOfFoldedQuads))
        check(len(grid.Quads) == n * grid.NumberOfLayers and len(grid.InnerUv) == n * (grid.NumberOfLayers - 1),
              "random ring %d: %d quads in %d layers of %d" % (index, len(grid.Quads), grid.NumberOfLayers, n))
        if worst is None or quality < worst[0]:
            worst = (quality, index, outer, inner, grid)
    quality, index, outer, inner, grid = worst
    if PLOT_DIR:
        check_grid("random worst %d" % index, grid, outer, inner)
    print("sweep: %d random rings, none folds, worst scaled Jacobian %.3f (ring %d, %d nodes, %d layers)" %
          (count, quality, index, len(outer), grid.NumberOfLayers))
    return quality


def group_timing():
    outer = circle(0, 0, 100, 400)
    inner = circle(5, 3, 30, 400, 13.0)
    outer_net, inner_net = to_net(outer), to_net(inner)
    outer_xyz, inner_xyz = to_net([plane(*p) for p in outer]), to_net([plane(*p) for p in inner])
    times = []
    for _ in range(3):
        start = time.perf_counter()
        grid = RingMapping.Map(outer_net, outer_xyz, inner_net, inner_xyz, 40, None)
        times.append(time.perf_counter() - start)
    check(grid.NumberOfFoldedQuads == 0 and len(grid.Quads) == 16000, "timing: %d quads, %d fold" %
          (len(grid.Quads), grid.NumberOfFoldedQuads))
    print("timing: 400 nodes x 40 layers, %d quads, %.3f s (first %.3f s)" % (len(grid.Quads), min(times), times[0]))
    check(min(times) < TIME_BOUND, "timing: %.3f s is above %.1f s" % (min(times), TIME_BOUND))


def main():
    results = [("annulus", group_annulus()), ("shift", group_shift()), ("layers", group_layers()),
               ("eccentric", group_eccentric()), ("curved", group_curved()), ("sweep", group_sweep())]
    group_refusals()
    group_timing()
    for name, worst in results:
        print("worst min SJ %-10s %.3f" % (name, worst))
    print("TEST PASSED")


main()
