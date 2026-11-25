#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks that a surface load of a two dimensional model acts on element edges only, since the solver input gives a 2D
#element no front or back face: a strip of 4 by 2 plane stress quadrilaterals takes a film heat transfer on its left
#and right edges. A surface selection is limited to element edges, as the dialog limits its picks, so an edge pick
#gives the edges, while a geometry box over an edge, which takes the faces of the elements it reaches, a selection of
#element faces and a named surface made of them are refused. The exported
#*Surface and *Film lines name the edges, S1 to S4 and F1 to F4, and CalculiX, solving the strip between two films,
#gives the temperatures of one dimensional conduction.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Loads_Two_D_Edges.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import os
import shutil
import tempfile

PREFIX = "PyTest_TwoD_Edges_"
STEP = PREFIX + "Heat"
LENGTH = 10.0
HEIGHT = 4.0
CONDUCTIVITY = 1.0
LEFT_SINK, LEFT_FILM = 20.0, 0.1
RIGHT_SINK, RIGHT_FILM = -10.0, 0.05
LEFT_NODES = [1, 6, 11]
RIGHT_NODES = [5, 10, 15]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_refused(function, fragment, message):
    try:
        function()
    except Exception as ex:
        text = str(ex)
        check(type(ex).__name__ == "PythonAPIException" and fragment in text and "   at " not in text,
              message + ": " + type(ex).__name__ + ": " + text[0:500])
        return
    raise Exception("TEST FAILED: " + message)


def write_strip_mesh(file_name):
    # Nodes 5 * j + i + 1 at x = 2.5 * i and y = 2 * j; the elements run counterclockwise, so the left edge of an
    # element is its CalculiX face 4 and the right edge its face 2
    lines = ["*NODE"]
    for j in range(3):
        for i in range(5):
            lines.append("%d, %.6f, %.6f, 0" % (5 * j + i + 1, LENGTH / 4 * i, HEIGHT / 2 * j))
    lines.append("*ELEMENT, TYPE=CPS4, ELSET=Strip")
    for j in range(2):
        for i in range(4):
            first = 5 * j + i + 1
            lines.append("%d, %d, %d, %d, %d" % (4 * j + i + 1, first, first + 1, first + 6, first + 5))
    with open(file_name, "w") as mesh_file:
        mesh_file.write("\n".join(lines) + "\n")


def setup(work):
    pmx.model.parts.remove([part.name for part in pmx.model.parts])
    pmx.model.element_sets.remove(list(pmx.model.element_sets.get_names()))
    pmx.model.properties.model_space = 'PlaneStress'
    mesh_file_name = os.path.join(work, PREFIX + "Strip.inp")
    write_strip_mesh(mesh_file_name)
    pmx.import_file(mesh_file_name)
    part = pmx.model.parts.first()
    check(pmx.model.parts.get_element_types(part.name)['linear_quad'] == 'CPS4', "the strip is not plane stress")
    pmx.model.materials.add(PREFIX + "Conductor", [pmx.create.thermal_conductivity(CONDUCTIVITY)])
    pmx.model.sections.add_solid(PREFIX + "Section", PREFIX + "Conductor",
                                 pmx.create.selection_by_ids('femodel', 'elements', 'parts', [part.id]))
    pmx.model.steps.add_heat_transfer(STEP, steady_state=True)


def edge_pick(x, select_by):
    return pmx.create.selection_by_pick('femodel', 'surfaces', [x, HEIGHT / 4, 0.0], select_by)


def element_faces(face_ids):
    return pmx.create.selection_by_ids('femodel', 'surfaces', 'element_faces', face_ids)


def test_refusals():
    loads = pmx.model.steps[STEP].loads
    # Face index 1 of an element is the element itself, S2, which the solver input cannot name in 2D
    check_refused(lambda: loads.add_film_heat_transfer(PREFIX + "Bad", element_faces([11, 21]), LEFT_SINK, LEFT_FILM),
                  "acts on element edges only", "a film on the faces of 2D elements must be refused")
    check_refused(lambda: loads.add_surface_flux(PREFIX + "Bad", element_faces([11]), 1.0),
                  "acts on element edges only", "a surface flux on the faces of 2D elements must be refused")
    check_refused(lambda: loads.add_radiation_heat_transfer(PREFIX + "Bad", element_faces([11]), LEFT_SINK, 0.5),
                  "acts on element edges only", "a radiation on the faces of 2D elements must be refused")
    pmx.model.surfaces.add(PREFIX + "Element_Faces", element_faces([11, 21]))
    check_refused(lambda: loads.add_film_heat_transfer(PREFIX + "Bad",
                                                       pmx.create.region('SurfaceName', PREFIX + "Element_Faces"),
                                                       LEFT_SINK, LEFT_FILM),
                  "is not made of element edges", "a film on a named surface of element faces must be refused")
    pmx.model.surfaces.remove([PREFIX + "Element_Faces"])
    # A geometry box over the left edge reaches the face of the part, so it takes the faces of all its elements
    box = pmx.create.selection_by_box('femodel', 'surfaces', [-0.1, -0.1, -1], [0.1, HEIGHT + 0.1, 1], 'Geometry')
    check_refused(lambda: loads.add_film_heat_transfer(PREFIX + "Bad", box, LEFT_SINK, LEFT_FILM),
                  "select_by 'Edge'", "a geometry box over a 2D edge must be refused naming the picks that work")
    check(not loads.contains(PREFIX + "Bad"), "a refused load was recorded")


def test_edge_picks(work):
    loads = pmx.model.steps[STEP].loads
    for select_by in ["Edge", "GeometryEdge", "Geometry"]:
        loads.add_film_heat_transfer(PREFIX + "Left", edge_pick(0.0, select_by), LEFT_SINK, LEFT_FILM)
        file_name = os.path.join(work, PREFIX + select_by + ".inp")
        pmx.export_model(file_name)
        labels = read_labels(file_name, "*film")
        check(labels == ["F4"], "an " + select_by + " pick on the left edge must give the left element edges, " +
              "face 4, got " + str(labels))
        loads.remove([PREFIX + "Left"])


def read_labels(file_name, keyword):
    labels = []
    in_keyword = False
    with open(file_name) as reader:
        for line in reader:
            line = line.strip()
            if line.startswith("**"):
                continue
            if line.startswith("*"):
                in_keyword = line.lower().startswith(keyword)
                continue
            if in_keyword and line:
                labels.append(line.split(",")[1].strip().upper())
    return labels


def test_exported_labels(work):
    loads = pmx.model.steps[STEP].loads
    loads.add_film_heat_transfer(PREFIX + "Left", edge_pick(0.0, "Edge"), LEFT_SINK, LEFT_FILM)
    loads.add_film_heat_transfer(PREFIX + "Right", edge_pick(LENGTH, "Edge"), RIGHT_SINK, RIGHT_FILM)
    file_name = os.path.join(work, PREFIX + "Films.inp")
    pmx.export_model(file_name)
    surface_labels = read_labels(file_name, "*surface")
    film_labels = read_labels(file_name, "*film")
    check(sorted(surface_labels) == ["S2", "S4"], "the films must be written on faces S4 and S2, got " +
          str(surface_labels))
    check(sorted(film_labels) == ["F2", "F4"], "the films must be written as F4 and F2, got " + str(film_labels))


def test_solved_like_conduction(work):
    # Steady conduction through the strip between two films: the heat flux is the sink difference over the three
    # resistances in series, and each edge lies below or above its sink by the flux over its film coefficient
    flux = (LEFT_SINK - RIGHT_SINK) / (1 / LEFT_FILM + LENGTH / CONDUCTIVITY + 1 / RIGHT_FILM)
    expected = {LEFT_SINK - flux / LEFT_FILM: LEFT_NODES, RIGHT_SINK + flux / RIGHT_FILM: RIGHT_NODES}
    name = PREFIX + "Films"
    pmx.jobs.add(name, work_directory=work)
    try:
        status = pmx.jobs[name].run(timeout=300)
        check(status == "OK", "the analysis did not succeed, got " + status + "\n" +
              pmx.jobs[name].get_output()[-3000:])
        pmx.jobs.open_results(name)
        result = pmx.results.current
        step_id = list(result.steps.get_ids())[-1]
        increment_id = list(result.steps[step_id].increments.get_ids())[-1]
        for temperature, node_ids in expected.items():
            for node_id in node_ids:
                value = result.get_nodal_value(step_id, increment_id, 'NDTEMP', 'T', node_id)
                check(abs(value - temperature) < 1e-6 * abs(temperature) + 1e-9,
                      "node %d must be at %g, got %g" % (node_id, temperature, value))
    finally:
        pmx.jobs.remove(name)


def clean():
    for collection in [pmx.jobs, pmx.model.steps, pmx.model.sections, pmx.model.materials, pmx.model.surfaces]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    work = tempfile.mkdtemp(prefix=PREFIX)
    try:
        setup(work)
        test_refusals()
        test_edge_picks(work)
        test_exported_labels(work)
        test_solved_like_conduction(work)
        clean()
    finally:
        shutil.rmtree(work, ignore_errors=True)
    print("TEST PASSED")


main()
