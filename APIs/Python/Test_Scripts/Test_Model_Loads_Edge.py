#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the edge load, a force per unit length on geometry edges written as the equivalent nodal forces in *Cload
#lines. On a plate of 4 by 4 by 1 hexahedra the API adds and replaces it on a picked solid edge, refuses a zero force,
#a surface selection and a node set not picked from geometry edges, and reads its values back. The nodal forces of a
#constant load, of a load in a cylindrical coordinate system and of loads scaled by a scalar and a vector
#distribution are the integrals of the linear force per length over the edge, and CalculiX, solving the plate,
#gives reactions that balance the edge load. A picked shell edge and the parabolic edge of a plane stress element
#are loaded as well, the latter with a sixth of the force at each end and two thirds at the midside node, and an
#axisymmetric model refuses the edge load. The normal direction loads both edges in their plane, a positive value
#pushing into the part as the normal shell edge load it replaces does; it refuses a solid edge, a coordinate system
#and a vector distribution, and CalculiX balances it on the clamped shell.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Loads_Edge.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import math
import os
import shutil
import tempfile

PREFIX = "PyTest_Edge_"
STEP = PREFIX + "Static"
CLAMP = PREFIX + "Clamp"
CYLINDRICAL = PREFIX + "Cylindrical"
SCALAR = PREFIX + "Scalar"
VECTOR = PREFIX + "Vector"
# The free edge of the plate at x = 50 and z = 2 runs along y from -20 to 20 through these nodes
EDGE_NODES = [30, 35, 40, 45, 50]


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_refused(function, fragment, message):
    try:
        function()
    except Exception as ex:
        text = str(ex)
        check(type(ex).__name__ == "PythonAPIException" and fragment in text and "   at " not in text,
              message + ": " + type(ex).__name__ + ": " + text[0:300])
        return
    raise Exception("TEST FAILED: " + message)


def edge_node_coor(node_id):
    j = (node_id - 30) // 5
    return [50.0, -20.0 + 10.0 * j, 2.0]


def write_mesh(file_name, nodes, element_type, elements):
    lines = ["*NODE"]
    for node_id, coor in nodes:
        lines.append("%d, %.6f, %.6f, %.6f" % (node_id, coor[0], coor[1], coor[2]))
    lines.append("*ELEMENT, TYPE=%s, ELSET=Part" % element_type)
    for element in elements:
        lines.append(", ".join(str(value) for value in element))
    with open(file_name, "w") as mesh_file:
        mesh_file.write("\n".join(lines) + "\n")


def remove_parts():
    part_names = [part.name for part in pmx.model.parts]
    if part_names:
        pmx.model.parts.remove(part_names)
    element_set_names = list(pmx.model.element_sets.get_names())
    if element_set_names:
        pmx.model.element_sets.remove(element_set_names)


def import_mesh(work, name, nodes, element_type, elements):
    remove_parts()
    mesh_file_name = os.path.join(work, PREFIX + name + ".inp")
    write_mesh(mesh_file_name, nodes, element_type, elements)
    pmx.import_file(mesh_file_name)


def write_plate(work):
    nodes = []
    for k in range(2):
        for j in range(5):
            for i in range(5):
                nodes.append((25 * k + 5 * j + i + 1, [10.0 + 10.0 * i, -20.0 + 10.0 * j, 2.0 * k]))
    elements = []
    for j in range(4):
        for i in range(4):
            first = 5 * j + i + 1
            elements.append([4 * j + i + 1, first, first + 1, first + 6, first + 5,
                             first + 25, first + 26, first + 31, first + 30])
    import_mesh(work, "Plate", nodes, "C3D8", elements)


def part_selection():
    return pmx.create.selection_by_ids('femodel', 'parts', 'parts', [pmx.model.parts.first().id])


def edge_pick(point):
    return pmx.create.selection_by_pick('femodel', 'edges', point, 'GeometryEdge')


def setup(work):
    write_plate(work)
    pmx.model.materials.add(PREFIX + "Material", [pmx.create.elastic(210000, 0.3)])
    pmx.model.sections.add_solid(PREFIX + "Section", PREFIX + "Material", part_selection())
    pmx.model.node_sets.add(CLAMP, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes',
                                                               [1, 6, 11, 16, 21, 26, 31, 36, 41, 46]))
    step = pmx.model.steps.add_static(STEP)
    step.bcs.add_fixed(PREFIX + "Fixed", pmx.create.region('NodeSetName', CLAMP))
    step.history_outputs.add_nodal(PREFIX + "Reactions", pmx.create.region('NodeSetName', CLAMP), ["RF"])
    pmx.model.coordinate_systems.add(CYLINDRICAL, [0, 0, 0], [1, 0, 0], [0, 1, 0],
                                     coordinate_system_type="Cylindrical")
    pmx.model.distributions.add_from_equation(SCALAR, "1 + 0.01 * y")
    pmx.model.distributions.add_from_equation(VECTOR, ["x", "y", "z"])


def read_cload_forces(file_name):
    forces = {}
    in_cload = False
    with open(file_name) as reader:
        for line in reader:
            line = line.strip()
            if line.startswith("**"):
                continue
            if line.startswith("*"):
                in_cload = line.lower().startswith("*cload")
                continue
            if in_cload and line:
                fields = [field.strip() for field in line.split(",")]
                forces.setdefault(int(fields[0]), [0.0, 0.0, 0.0])[int(fields[1]) - 1] += float(fields[2])
    return forces


def read_dload_lines(file_name):
    lines = []
    in_dload = False
    with open(file_name) as reader:
        for line in reader:
            line = line.strip()
            if line.startswith("**"):
                continue
            if line.startswith("*"):
                in_dload = line.lower().startswith("*dload")
                continue
            if in_dload and line:
                fields = [field.strip() for field in line.split(",")]
                lines.append((int(fields[0]), fields[1].upper(), float(fields[2])))
    return lines


def check_dload_lines(work, name, expected):
    file_name = os.path.join(work, name + ".inp")
    pmx.export_model(file_name)
    check(not read_cload_forces(file_name), name + ": a shell edge normal load must not write nodal forces")
    lines = read_dload_lines(file_name)
    check(len(lines) == len(expected) and
          all(a[0] == b[0] and a[1] == b[1] and abs(a[2] - b[2]) < 1e-9 for a, b in zip(lines, expected)),
          "%s: the *Dload lines are %s, expected %s" % (name, lines, expected))


def exported_forces(work, name):
    file_name = os.path.join(work, name + ".inp")
    pmx.export_model(file_name)
    return read_cload_forces(file_name)


# The nodal forces of a force per length that varies linearly between the nodes of a chain of straight segments
def expected_linear_forces(node_ids, coor, values):
    forces = {node_id: [0.0, 0.0, 0.0] for node_id in node_ids}
    for s in range(len(node_ids) - 1):
        length = math.sqrt(sum((coor[s + 1][c] - coor[s][c]) ** 2 for c in range(3)))
        for c in range(3):
            forces[node_ids[s]][c] += length * (2 * values[s][c] + values[s + 1][c]) / 6
            forces[node_ids[s + 1]][c] += length * (values[s][c] + 2 * values[s + 1][c]) / 6
    return forces


def check_forces(forces, expected, message):
    expected = {node_id: force for node_id, force in expected.items() if any(abs(f) > 1e-12 for f in force)}
    check(sorted(forces.keys()) == sorted(expected.keys()),
          message + ": the loaded nodes are " + str(sorted(forces.keys())) + ", expected " +
          str(sorted(expected.keys())))
    for node_id, force in expected.items():
        for c in range(3):
            check(abs(forces[node_id][c] - force[c]) < 1e-9 * max(1.0, abs(force[c])),
                  "%s: node %d has %s, expected %s" % (message, node_id, forces[node_id], force))


def check_edge_forces(work, name, values):
    coor = [edge_node_coor(node_id) for node_id in EDGE_NODES]
    check_forces(exported_forces(work, name), expected_linear_forces(EDGE_NODES, coor, values), name)


def test_api():
    loads = pmx.model.steps[STEP].loads
    load = loads.add_edge_load(PREFIX + "Load", edge_pick([50, 5, 2]), f3=-2)
    check(load.item_type == "edge_load", "unexpected item type " + load.item_type)
    check(load.region_type == "NodeSetName", "the edge selection was not made a node set: " + load.region_type)
    check(sorted(pmx.model.node_sets[load.region_name].get_node_ids()) == EDGE_NODES,
          "the edge pick did not resolve to the nodes of the edge")
    check(load.get_value("f3") == -2 and load.get_value("f1") == 0, "the components were not set")
    check(load.get_value("distribution_name") == "Constant", "a new edge load must be constant")
    # A replace keeps what it is not given
    load = loads.replace_edge_load(PREFIX + "Load", f2=4)
    check(load.get_value("f3") == -2 and load.get_value("f2") == 4, "a replace must keep the components it is not given")
    load = loads.replace_edge_load(PREFIX + "Load", f2=0)
    count = len(loads)
    check_refused(lambda: loads.add_edge_load(PREFIX + "Bad", edge_pick([50, 5, 2])), "'f1'",
                  "an edge load without a component must be refused")
    check_refused(lambda: loads.add_edge_load(PREFIX + "Bad", pmx.create.selection_by_pick(
                      'femodel', 'surfaces', [30, 0, 2], 'Surface'), f1=1),
                  "requires target 'edges'", "a surface selection must be refused naming the target")
    check_refused(lambda: loads.add_edge_load(PREFIX + "Bad", pmx.create.region('NodeSetName', CLAMP), f1=1),
                  "not created by picking geometry edges", "a node set of picked nodes must be refused")
    check_refused(lambda: loads.add_edge_load(PREFIX + "Bad", edge_pick([50, 5, 2]), f1=1, distribution_name="NoSuch"),
                  "NoSuch", "an unknown distribution must be refused")
    check(len(loads) == count and not loads.contains(PREFIX + "Bad"), "a refused edge load was recorded")
    load = loads.replace_edge_load(PREFIX + "Load", coordinate_system_name=CYLINDRICAL.lower())
    check(load.coordinate_system_name == CYLINDRICAL, "the coordinate system was not kept in the model's spelling")
    loads.replace_edge_load(PREFIX + "Load", coordinate_system_name="Global")
    # The normal lies in the plane of a shell or of a two dimensional part, which a solid edge has not
    check_refused(lambda: loads.add_edge_load(PREFIX + "Bad", edge_pick([50, 5, 2]), f1=1, normal=1), "not both",
                  "the components and the normal together must be refused")
    check_refused(lambda: loads.add_edge_load(PREFIX + "Bad", edge_pick([50, 5, 2]), normal=1), "shell",
                  "the normal on a solid edge must be refused")
    check(len(loads) == count and not loads.contains(PREFIX + "Bad"), "a refused edge load was recorded")


def test_constant_lines(work):
    check_edge_forces(work, "constant", [[0, 0, -2]] * len(EDGE_NODES))


def test_cylindrical_lines(work):
    loads = pmx.model.steps[STEP].loads
    loads.replace_edge_load(PREFIX + "Load", f1=2, f2=0, f3=0, coordinate_system_name=CYLINDRICAL)
    values = []
    for node_id in EDGE_NODES:
        x, y, z = edge_node_coor(node_id)
        radius = math.hypot(x, y)
        values.append([2 * x / radius, 2 * y / radius, 0])
    check_edge_forces(work, "radial", values)
    # The second component is tangential
    loads.replace_edge_load(PREFIX + "Load", f1=0, f2=2)
    values = []
    for node_id in EDGE_NODES:
        x, y, z = edge_node_coor(node_id)
        radius = math.hypot(x, y)
        values.append([-2 * y / radius, 2 * x / radius, 0])
    check_edge_forces(work, "tangential", values)
    loads.replace_edge_load(PREFIX + "Load", f2=0, f3=-2, coordinate_system_name="Global")


def test_distribution_lines(work):
    loads = pmx.model.steps[STEP].loads
    load = loads.replace_edge_load(PREFIX + "Load", f3=4, distribution_name=SCALAR)
    check(load.get_value("distribution_name") == SCALAR, "the distribution was not set")
    check_edge_forces(work, "scalar", [[0, 0, 4 * (1 + 0.01 * edge_node_coor(n)[1])] for n in EDGE_NODES])
    # A vector distribution scales each component by its own value
    loads.replace_edge_load(PREFIX + "Load", f1=1, f2=1, f3=1, distribution_name=VECTOR)
    check_edge_forces(work, "vector", [edge_node_coor(n) for n in EDGE_NODES])
    loads.replace_edge_load(PREFIX + "Load", f1=0, f2=0, f3=-2, distribution_name="Constant")


def solve_reactions(work, name):
    pmx.jobs.add(name, work_directory=work)
    try:
        status = pmx.jobs[name].run(timeout=300)
        check(status == "OK", "the analysis " + name + " did not succeed, got " + status + "\n" +
              pmx.jobs[name].get_output()[-3000:])
        pmx.jobs.open_results(name)
        result = pmx.results.current
        reactions = []
        for component in ["RF1", "RF2", "RF3"]:
            entries = result.get_history_output_entries(CLAMP.upper(), "FORCES", component)
            reactions.append(sum(entries[str(entry)].get_data().values[-1] for entry in entries.get_names()))
        return reactions
    finally:
        pmx.jobs.remove(name)


def test_solved_reactions(work):
    # The clamped nodes carry the whole edge load of 2 over the length of 40, to the precision the results are stored in
    reactions = solve_reactions(work, PREFIX + "Edge_Load")
    check(abs(reactions[0]) < 1e-3 and abs(reactions[1]) < 1e-3 and abs(reactions[2] - 80) < 1e-3,
          "the reactions %s do not balance the edge load of 80" % reactions)


def remove_model_items():
    for collection in [pmx.model.steps, pmx.model.sections, pmx.model.node_sets, pmx.model.surfaces]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def test_shell_edge(work):
    remove_model_items()
    # Four shell elements in the x-y plane from x = 0 to 40 and y = 0 to 10; the end edge at x = 40 has nodes 5 and 10
    nodes = [(5 * j + i + 1, [10.0 * i, 10.0 * j, 0.0]) for j in range(2) for i in range(5)]
    elements = [[i + 1, i + 1, i + 2, i + 7, i + 6] for i in range(4)]
    import_mesh(work, "Shell", nodes, "S4", elements)
    loads = pmx.model.steps.add_static(STEP).loads
    loads.add_edge_load(PREFIX + "Shell", edge_pick([40, 5, 0]), f3=-1)
    check_forces(exported_forces(work, "shell"), {5: [0, 0, -5], 10: [0, 0, -5]}, "shell edge")
    # On a shell the normal is written as the *Dload EDNOR of the element edge; element 4 runs 4, 5, 10, 9, so the
    # edge from node 5 to node 10 is its second edge
    load = loads.replace_edge_load(PREFIX + "Shell", normal=2)
    check(load.get_value("normal") == 2 and "f1" not in list(load.get_value_names()), "the normal was not read back")
    check_dload_lines(work, "shell_normal", [(4, "EDNOR2", 2.0)])
    # A scalar distribution gives the mean value over the edge, here 1 + 0.01 * y over y from 0 to 10
    loads.replace_edge_load(PREFIX + "Shell", distribution_name=SCALAR)
    check_dload_lines(work, "shell_normal_scalar", [(4, "EDNOR2", 2.1)])
    loads.replace_edge_load(PREFIX + "Shell", distribution_name="Constant")
    check_refused(lambda: loads.replace_edge_load(PREFIX + "Shell", coordinate_system_name=CYLINDRICAL), "normal",
                  "a coordinate system must be refused with the normal")
    check_refused(lambda: loads.replace_edge_load(PREFIX + "Shell", distribution_name=VECTOR), "scalar",
                  "a vector distribution must be refused with the normal")
    load = loads.replace_edge_load(PREFIX + "Shell", f1=0, f2=0, f3=-1)
    check(load.get_value("f3") == -1 and "normal" not in list(load.get_value_names()),
          "the components did not replace the normal")
    loads.replace_edge_load(PREFIX + "Shell", normal=2)
    # CalculiX balances the normal edge load of 2 over the edge length of 10 at the clamped end; a section on the
    # selected part must cover all four elements
    pmx.model.sections.add_shell(PREFIX + "Shell_Section", PREFIX + "Material", part_selection(), thickness=1)
    pmx.model.node_sets.add(CLAMP, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 6]))
    step = pmx.model.steps[STEP]
    step.bcs.add_fixed(PREFIX + "Fixed", pmx.create.region('NodeSetName', CLAMP))
    step.history_outputs.add_nodal(PREFIX + "Reactions", pmx.create.region('NodeSetName', CLAMP), ["RF"])
    reactions = solve_reactions(work, PREFIX + "Shell_Edge_Normal")
    check(abs(reactions[0] - 20) < 1e-3 and abs(reactions[1]) < 1e-3 and abs(reactions[2]) < 1e-3,
          "the reactions %s do not balance the normal edge load of 20" % reactions)


def test_parabolic_two_d_edge(work):
    # One quadratic quadrilateral of 10 by 4; its right edge runs from node 2 to node 3 through the midside node 6
    remove_model_items()
    remove_parts()
    pmx.model.properties.model_space = 'PlaneStress'
    nodes = [(1, [0, 0, 0]), (2, [10, 0, 0]), (3, [10, 4, 0]), (4, [0, 4, 0]),
             (5, [5, 0, 0]), (6, [10, 2, 0]), (7, [5, 4, 0]), (8, [0, 2, 0])]
    import_mesh(work, "Quad", nodes, "CPS8", [[1, 1, 2, 3, 4, 5, 6, 7, 8]])
    loads = pmx.model.steps.add_static(STEP).loads
    check_refused(lambda: loads.add_edge_load(PREFIX + "Bad", edge_pick([10, 1, 0]), f1=3, f3=1), "'f3'",
                  "a two dimensional model must refuse a nonzero f3")
    loads.add_edge_load(PREFIX + "Quad", edge_pick([10, 1, 0]), f1=3)
    check_forces(exported_forces(work, "quad"), {2: [2, 0, 0], 3: [2, 0, 0], 6: [8, 0, 0]},
                 "parabolic plane stress edge")
    # A negative normal pulls the right edge out of the part, along x
    loads.replace_edge_load(PREFIX + "Quad", normal=-3)
    check_forces(exported_forces(work, "quad_normal"), {2: [2, 0, 0], 3: [2, 0, 0], 6: [8, 0, 0]},
                 "normal parabolic plane stress edge")
    # An axisymmetric model refuses an edge load
    pmx.model.steps.remove([STEP])
    remove_parts()
    pmx.model.properties.model_space = 'Axisymmetric'
    import_mesh(work, "Axisymmetric", nodes, "CAX8", [[1, 1, 2, 3, 4, 5, 6, 7, 8]])
    loads = pmx.model.steps.add_static(STEP).loads
    check_refused(lambda: loads.add_edge_load(PREFIX + "Bad", edge_pick([10, 1, 0]), f1=3), "axisymmetric",
                  "an axisymmetric model must refuse an edge load")
    remove_parts()
    pmx.model.properties.model_space = 'ThreeD'


def clean():
    for collection in [pmx.jobs, pmx.model.steps, pmx.model.sections, pmx.model.materials,
                       pmx.model.distributions, pmx.model.coordinate_systems, pmx.model.node_sets,
                       pmx.model.surfaces]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    work = tempfile.mkdtemp(prefix=PREFIX)
    try:
        setup(work)
        test_api()
        test_constant_lines(work)
        test_cylindrical_lines(work)
        test_distribution_lines(work)
        test_constant_lines(work)
        test_solved_reactions(work)
        test_shell_edge(work)
        test_parabolic_two_d_edge(work)
        clean()
    finally:
        shutil.rmtree(work, ignore_errors=True)
    print("TEST PASSED")


main()
