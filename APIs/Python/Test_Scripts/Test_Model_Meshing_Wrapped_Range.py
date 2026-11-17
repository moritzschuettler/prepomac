#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the structured faces whose parameter range wraps over the seam of a periodic surface without the face being
#closed: the parameters of their nodes as the surface gives them jump by a period inside the face. Every structured kind
#that can lie there is meshed by its own grid on the right side of the surface, or left free with a message - never a
#fold, a face meshed free without a word, or nodes moved round the surface.
#
#  - wrapped_faces.brep: one part per shape side by side at x = 150 i, about the axis (150 i + 20, 0). Cylinder of r 20
#    from -45 to 45 degrees about +x and 30 high, its seam at 0 degrees (an arc made in the centre mode runs over the
#    seam of its circle, and its extrusion keeps the range): the patch (four-sided), the patch cut by an oblique plane
#    (three-sided), the patch with a corner of 15 x 11 cut out (submapped, a reentrant corner), the patch within a half
#    tube and within a tube of r 10 along x (lens faces of two corners and of none), the patch with a round hole of
#    r 6 (submapped and a ring; its submapped grid is refused), the patch within a tube of radii 12 and 5 along x (a
#    ring). The torus of radii 40 and 10 revolved by 90 degrees between -45 and 45 degrees of its tube circle about its
#    outer equator, over the seam of the tube (four-sided). Sphere and cone sectors written with the frame of their
#    surface turned by half their angle, so that their seam runs through their middle: the band of the sphere of r 20
#    from the equator to 50 degrees revolved by 90 (four-sided), the hemisphere revolved by 90 (three-sided with a pole
#    corner), by 180 (a lens face with the pole inside a side) and by 270 (a reentrant pole), the sphere between two
#    meridians 90 degrees apart (a lens face, poles at both corners), the cone of 45 degrees revolved by 90 (three-sided
#    with the apex at a corner), the cone frustum from r 20 to r 10 revolved by 90 (four-sided), the hemisphere revolved
#    by 90 and cut by the plane y = 8 (four-sided with a pole corner). Then the twins with the seam at an edge (an arc
#    of three points): the patch with the hole, the patch, the dome 90 cut, the oblique cut, the corner cut out and the
#    ring.
#  - At the element sizes 5 and 3.5 under None, Simple, AllQuad and SimpleFullQuad: the kind the detection finds, the
#    number of inner nodes without four quads of that kind under Simple, AllQuad and SimpleFullQuad (0 four-sided,
#    submapped and ring, 1 three-sided, 2 lens of two corners, 4 lens of none, 3 reentrant pole), quads only, two
#    triangles per quad under None, a worst scaled Jacobian of 0.5 at least measured in space against the normals of
#    the mesh (a corner at a pole against the normal of its element), no message; the area of the mesh within 3 % of
#    the area of the face and every node within 0.5 of the box of the geometry, so no node lies on the other side of
#    the surface. The patch with the hole, and its twin, write the submapping message and are meshed free, no fold;
#    under the full-quad recombination their free mesh keeps triangles (gmsh recombines no face of a periodic surface
#    into full quads, so a structured face of a cylinder is left out of that recombination and laid after it).
#  - The twins give the same quads, triangles, irregular inner nodes and worst scaled Jacobian within 1E-3 as the
#    faces across the seam.
#  The model was made by gmsh's OCC kernel through GmshCommon (generator scratchpad/submap/stage25/make_pole_models.py,
#  run with PMX_CAP_GENERATOR = stage14/make_cap_models.py). With the environment variable PMX_WRAPPED_PLOT_DIR set,
#  the meshes under AllQuad are written into that folder as PNG images, each part seen along the axis of its smallest
#  extent, the inner nodes without four quads red.
#
#  PrePoMax.com -f wrapped_faces.brep -u MM_TON_S_C --script Test_Model_Meshing_Wrapped_Range.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import clr
clr.AddReference('CaeMesh')
clr.AddReference('System.Drawing')
from CaeMesh import (LensFaceDetector, RingFaceDetector, SubmapFaceDetector, TransfiniteFaceDetector,
                     TransfiniteThreeSidedEnum)
from System import Array
from System.Drawing import Bitmap, Brushes, Color, Font, Graphics, Pen, PointF, SolidBrush
from System.Drawing.Imaging import ImageFormat
from System.Reflection import BindingFlags
from System.Windows.Forms import Application

PLOT_DIR = os.environ.get("PMX_WRAPPED_PLOT_DIR")
SIZES = [5.0, 3.5]
RECOMBINATIONS = ["None", "Simple", "AllQuad", "SimpleFullQuad"]
SPACING = 150.0
GOOD = 0.5
TRANSFINITE_ANGLE = 135.0
# By shape: the kind the detection finds, the inner nodes without four quads of its grid, and the area of the face
SHAPES = [("cylinder patch", "transfinite 4", 0, 942.48),
          ("cylinder triangle", "transfinite 3", 1, 269.23),
          ("cylinder notch", "submapped", 0, 752.03),
          ("cylinder half disc", "submapped lens 2", 2, 238.08),
          ("cylinder disc", "lens 0", 4, 325.04),
          ("cylinder hole", "submapped ring", None, 828.06),
          ("torus patch", "transfinite 4", 0, 1209.1),
          ("sphere band wrapped", "transfinite 4", 0, 481.32),
          ("dome 90 wrapped", "transfinite 3", 1, 628.32),
          ("dome 180 wrapped", "lens 2", 2, 1256.64),
          ("lune 90 wrapped", "lens 2", 2, 1256.64),
          ("cone 45 quarter wrapped", "transfinite 3 submapped", 1, 444.29),
          ("cone frustum wrapped", "transfinite 4", 0, 333.22),
          ("dome 90 cut wrapped", "transfinite 4", 0, 251.33),
          ("dome 270 wrapped", "lens 2 reentrant", 3, 1884.96),
          ("cylinder hole plain", "submapped ring", None, 828.07),
          ("cylinder patch plain", "transfinite 4", 0, 942.48),
          ("dome 90 cut plain", "transfinite 4", 0, 251.33),
          ("cylinder triangle plain", "transfinite 3", 1, 269.23),
          ("cylinder notch plain", "submapped", 0, 752.03),
          ("cylinder annulus", "ring", 0, 396.99),
          ("cylinder annulus plain", "ring", 0, 397.0)]
NAMES = [shape[0] for shape in SHAPES]
TWINS = [("cylinder patch", "cylinder patch plain"), ("dome 90 cut wrapped", "dome 90 cut plain"),
         ("cylinder triangle", "cylinder triangle plain"), ("cylinder notch", "cylinder notch plain"),
         ("cylinder annulus", "cylinder annulus plain")]
MAX_AREA_ERROR = 0.03
BOX_TOLERANCE = 0.5


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


# Model access ---------------------------------------------------------------------------------------------------------
def parts_by_x(names):
    """The geometry parts by the shape they were made from, told apart by where they lie along x."""
    parts = {}
    for part in pmx.model.geometry.parts:
        xs = [c[0] for c in part.get_vertex_data().coordinates]
        parts[names[int(round(min(xs) / SPACING))]] = part
    check(sorted(parts) == sorted(names), "the parts were not all found: " + str(sorted(parts)))
    return parts


def main_form():
    return [form for form in Application.OpenForms if form.GetType().Name == "FrmMain"][0]


def geometry_part(part):
    form = main_form()
    controller = form.GetType().GetField("_controller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form)
    return controller.Model.Geometry, controller.Model.Geometry.Parts[part.name]


def detected_kind(part):
    """The kinds the detection finds on the one face of the part, as words."""
    geometry, gp = geometry_part(part)
    words = []
    for face in TransfiniteFaceDetector.Detect(geometry, gp, TransfiniteThreeSidedEnum.Mapped, True, TRANSFINITE_ANGLE):
        words.append("transfinite " + str(face.NumberOfSides))
    if len(list(SubmapFaceDetector.Detect(geometry, gp, TRANSFINITE_ANGLE))) > 0:
        words.append("submapped")
    for face in LensFaceDetector.Detect(geometry, gp, TRANSFINITE_ANGLE):
        words.append("lens " + str(face.NumberOfSides) + (" reentrant" if len(list(face.ReentrantIndices)) else ""))
    if len(list(RingFaceDetector.Detect(geometry, gp))) > 0:
        words.append("ring")
    return " ".join(words)


def geometry_box(part):
    """The box of the nodes of the geometry of the part."""
    geometry, gp = geometry_part(part)
    xyz = [geometry.Nodes[label].Coor for label in gp.NodeLabels]
    return [min(c[k] for c in xyz) for k in range(3)], [max(c[k] for c in xyz) for k in range(3)]


def poles(part, dx):
    """The vertices of the part on the axis of its shape, through (dx + 20, 0): its poles and apexes."""
    return [tuple(c) for c in part.get_vertex_data().coordinates if abs(c[0] - dx - 20) < 1E-6 and abs(c[1]) < 1E-6]


def output_messages(part_name):
    """The structured-face and all-quad lines the last meshing of the part wrote to the output window."""
    form = main_form()
    lines = list(form.GetType().GetField("_outputLines", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form))
    starts = [i for i, line in enumerate(lines) if line.rstrip().endswith("Create mesh: " + part_name)]
    check(len(starts) > 0, "the output window does not show the meshing of " + part_name)
    result = []
    for line in lines[starts[-1]:]:
        for marker in ("Lens mapping:", "Transfinite:", "Submapping:", "Ring mapping:", "Cap mapping:",
                       "Tube mapping:", "All-quad meshing:"):
            if marker in line:
                result.append(line[line.index(marker):])
    return result


def mesh(part, recombine, size):
    items = pmx.model.mesh.setup_items
    items.add_meshing_parameters("PyTest_Wrap_Size",
                                 pmx.create.selection_by_ids('geometry', 'parts', 'parts', [part.id]),
                                 relative_size=False, max_size=size, min_size=size / 10)
    selection = pmx.create.selection_by_ids('geometry', 'geometry', 'parts', [part.id])
    items.add_shell_gmsh("PyTest_Wrap", selection, algorithm_recombine=recombine)
    try:
        report = pmx.model.mesh.generate(part.name)
    finally:
        items.remove(["PyTest_Wrap_Size", "PyTest_Wrap"])
    check(report[part.name] == "", "meshing " + part.name + " reported: " + report[part.name])


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
        for element_id in meshed[0].get_element_ids():
            i = index[int(element_id)]
            corners = 4 if node_counts[i] in (4, 8) else 3
            self.elements.append(connectivity[offsets[i]:offsets[i] + corners])

    def kinds(self):
        quads = sum(1 for e in self.elements if len(e) == 4)
        return quads, len(self.elements) - quads

    def used_nodes(self):
        return set(node for element in self.elements for node in element)

    def nodes_at(self, points):
        found = set()
        for point in points:
            for node in self.used_nodes():
                if sum((self.xyz[node][c] - point[c]) ** 2 for c in range(3)) < 1E-6:
                    found.add(node)
        return found

    def area(self):
        total = 0.0
        for element in self.elements:
            p = [self.xyz[node] for node in element]
            for k in range(1, len(p) - 1):
                c = cross([p[k][j] - p[0][j] for j in range(3)], [p[k + 1][j] - p[0][j] for j in range(3)])
                total += 0.5 * math.sqrt(sum(v * v for v in c))
        return total


def boundary_nodes(elements):
    uses = {}
    for element in elements:
        for k in range(len(element)):
            edge = tuple(sorted((element[k], element[(k + 1) % len(element)])))
            uses[edge] = uses.get(edge, 0) + 1
    return set(node for edge, count in uses.items() if count == 1 for node in edge)


def valences(elements):
    valence = {}
    for element in elements:
        for node in element:
            valence[node] = valence.get(node, 0) + 1
    return valence


def irregular_inner_nodes(elements):
    boundary = boundary_nodes(elements)
    return [node for node, count in valences(elements).items() if node not in boundary and count != 4]


def cross(e1, e2):
    return [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]


def own_normal(face, element):
    p = [face.xyz[node] for node in element]
    if len(element) == 4:
        normal = cross([p[2][k] - p[0][k] for k in range(3)], [p[3][k] - p[1][k] for k in range(3)])
    else:
        normal = cross([p[1][k] - p[0][k] for k in range(3)], [p[2][k] - p[0][k] for k in range(3)])
    length = math.sqrt(sum(c * c for c in normal))
    return [c / length for c in normal] if length > 0 else [0.0, 0.0, 0.0]


def node_normals(face):
    sums = {}
    for element in face.elements:
        n = len(element)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            product = cross([a[k] - p[k] for k in range(3)], [b[k] - p[k] for k in range(3)])
            length = math.sqrt(sum(v * v for v in product))
            total = sums.setdefault(element[c], [0.0, 0.0, 0.0])
            for k in range(3):
                total[k] += product[k] / length if length > 0 else 0.0
    normals = {}
    for node, n in sums.items():
        length = math.sqrt(sum(c * c for c in n))
        normals[node] = tuple(c / length for c in n) if length > 0 else (0.0, 0.0, 0.0)
    return normals


def min_scaled_jacobian(face, pole_points=()):
    """The smallest sine of a corner in space against the normal of the mesh at its node, a triangle scaled so that 60
    degrees count as 1; negative where an element folds. A corner at a pole is measured against the normal of its
    element."""
    normals = node_normals(face)
    pole_nodes = face.nodes_at(pole_points)
    worst = 1.0
    for element in face.elements:
        n = len(element)
        for c in range(n):
            p, a, b = face.xyz[element[c]], face.xyz[element[(c + 1) % n]], face.xyz[element[(c - 1) % n]]
            e1 = [a[k] - p[k] for k in range(3)]
            e2 = [b[k] - p[k] for k in range(3)]
            product = cross(e1, e2)
            normal = own_normal(face, element) if element[c] in pole_nodes else normals[element[c]]
            length = math.sqrt(sum(v * v for v in e1) * sum(v * v for v in e2))
            sine = sum(product[k] * normal[k] for k in range(3)) / length if length > 0 else -1.0
            worst = min(worst, sine * (2 / math.sqrt(3) if n == 3 else 1.0))
    return worst


# Plots ----------------------------------------------------------------------------------------------------------------
def plot(file_name, title, face):
    """The elements seen along the axis of the smallest extent of the part; quads blue, triangles orange, inner nodes
    without four quads red."""
    if not PLOT_DIR:
        return
    if not os.path.isdir(PLOT_DIR):
        os.makedirs(PLOT_DIR)
    points = list(face.xyz.values())
    extents = [max(p[k] for p in points) - min(p[k] for p in points) for k in range(3)]
    axis = extents.index(min(extents))
    i0, i1 = [k for k in range(3) if k != axis]
    xs = [face.xyz[node][i0] for node in face.used_nodes()]
    ys = [face.xyz[node][i1] for node in face.used_nodes()]
    scale = 700.0 / max(max(xs) - min(xs), max(ys) - min(ys))
    width, height = int((max(xs) - min(xs)) * scale + 40), int((max(ys) - min(ys)) * scale + 70)
    bitmap = Bitmap(width, height)
    graphics = Graphics.FromImage(bitmap)
    graphics.Clear(Color.White)
    pen = Pen(Color.FromArgb(29, 60, 90), 1.0)
    quad_brush = SolidBrush(Color.FromArgb(219, 233, 246))
    triangle_brush = SolidBrush(Color.FromArgb(246, 190, 160))

    def to_image(node):
        p = face.xyz[node]
        return PointF(float(20 + (p[i0] - min(xs)) * scale), float(height - 20 - (p[i1] - min(ys)) * scale))

    for element in face.elements:
        polygon = Array[PointF]([to_image(node) for node in element])
        graphics.FillPolygon(quad_brush if len(element) == 4 else triangle_brush, polygon)
        graphics.DrawPolygon(pen, polygon)
    for node in irregular_inner_nodes(face.elements):
        p = to_image(node)
        graphics.FillEllipse(Brushes.Firebrick, p.X - 3, p.Y - 3, 6.0, 6.0)
    graphics.DrawString(title, Font("Arial", 11.0), Brushes.Black, 12.0, 8.0)
    bitmap.Save(os.path.join(PLOT_DIR, file_name + ".png"), ImageFormat.Png)
    graphics.Dispose()
    bitmap.Dispose()


# Checks ---------------------------------------------------------------------------------------------------------------
def check_detection(parts):
    for name, kind, irregular_count, area in SHAPES:
        found = detected_kind(parts[name])
        print(name + ": " + found)
        check(found == kind, "the " + name + " is detected as '" + found + "', not '" + kind + "'")


def check_shapes(parts, size):
    """Meshes every shape; returns its quads, triangles, irregular inner nodes and worst scaled Jacobian by shape and
    recombination."""
    results = {}
    for index, (name, kind, irregular_count, area) in enumerate(SHAPES):
        part = parts[name]
        pole_points = poles(part, SPACING * index)
        low, high = geometry_box(part)
        for recombine in RECOMBINATIONS:
            mesh(part, recombine, size)
            face = FaceMesh(part.name)
            messages = output_messages(part.name)
            title = name + " at size " + str(size) + " with " + recombine
            quads, triangles = face.kinds()
            quality = min_scaled_jacobian(face, pole_points)
            irregular = irregular_inner_nodes(face.elements)
            valence = valences(face.elements)
            area_error = abs(face.area() / area - 1)
            outside = sum(1 for p in face.xyz.values()
                          if any(p[k] < low[k] - BOX_TOLERANCE or p[k] > high[k] + BOX_TOLERANCE for k in range(3)))
            print(title + ":", face.kinds(), "irregular", len(irregular), "worst scaled Jacobian", round(quality, 3),
                  "area error", round(area_error, 4), "outside", outside, messages)
            check(area_error <= MAX_AREA_ERROR and outside == 0, title + " has a mesh of " +
                  str(round(face.area(), 2)) + " against the face of " + str(area) + ", and " + str(outside) +
                  " nodes outside the box of the geometry")
            check(quality > 0, title + " has a folded element: " + str(quality))
            if irregular_count is None:
                check(len(messages) == 1 and messages[0].startswith("Submapping:") and "is meshed free" in messages[0],
                      title + " wrote " + str(messages))
            else:
                check(len(messages) == 0, title + " wrote: " + str(messages))
                if recombine == "None":
                    check(quads == 0 and triangles % 2 == 0, title + " has " + str(face.kinds()))
                else:
                    check(triangles == 0 and len(irregular) == irregular_count and
                          all(valence[node] == 3 for node in irregular), title + " has " + str(face.kinds()) +
                          " and the irregular inner nodes " + str(sorted(valence[node] for node in irregular)) +
                          ", not " + str(irregular_count) + " of 3")
                    check(quality >= GOOD, title + " has a corner of the scaled Jacobian " + str(quality))
            if recombine == "AllQuad":
                plot("wrapped_" + name.replace(" ", "_") + "_" + str(size).replace(".", "_"), title + ": " +
                     str(quads) + " quads, worst SJ " + str(round(quality, 3)), face)
            results[(name, recombine)] = (quads, triangles, len(irregular), quality)
    return results


def check_twins(results, size):
    """A face across the seam is meshed as its twin with the seam at an edge."""
    for wrapped, plain in TWINS:
        for recombine in RECOMBINATIONS:
            a = results[(wrapped, recombine)]
            b = results[(plain, recombine)]
            check(a[:3] == b[:3] and abs(a[3] - b[3]) <= 1E-3, "at size " + str(size) + " with " + recombine +
                  " the " + wrapped + " is meshed as " + str(a) + ", its twin as " + str(b))


def main():
    check(pmx.model.is_open(), "a model must be open")
    parts = parts_by_x(NAMES)
    check_detection(parts)
    for size in SIZES:
        check_twins(check_shapes(parts, size), size)
    print("TEST PASSED")


main()
