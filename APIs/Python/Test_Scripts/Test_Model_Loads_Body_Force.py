#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the body force, a force per unit volume written as *Dload BX, BY and BZ, on a plate of 4 by 4 by 1
#hexahedra that lies off the global z axis. The API adds and replaces it on parts, refuses a zero force and an
#element selection, and reads its values back. A constant force in the global system is written as one line per
#component on its element set; a force in a cylindrical coordinate system and a force scaled by a distribution
#are written per element, evaluated at the element centroid. Solved by CalculiX, a body force gives the
#reactions of a gravity load of the same force per volume.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Loads_Body_Force.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.
import math
import os
import shutil
import tempfile

PREFIX = "PyTest_BForce_"
STEP = PREFIX + "Static"
CLAMP = PREFIX + "Clamp"
CYLINDRICAL = PREFIX + "Cylindrical"
SCALAR = PREFIX + "Scalar"
VECTOR = PREFIX + "Vector"
DENSITY = 2e-6
ACCELERATION = 1500.0


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


def element_centroid(element_id):
    # Element 4 * j + i + 1 spans x from 10 + 10 i and y from -20 + 10 j, and z from 0 to 2
    i = (element_id - 1) % 4
    j = (element_id - 1) // 4
    return [15.0 + 10.0 * i, -15.0 + 10.0 * j, 1.0]


def write_plate_mesh(file_name):
    lines = ["*NODE"]
    for k in range(2):
        for j in range(5):
            for i in range(5):
                lines.append("%d, %.6f, %.6f, %.6f" % (25 * k + 5 * j + i + 1, 10.0 + 10.0 * i, -20.0 + 10.0 * j,
                                                       2.0 * k))
    lines.append("*ELEMENT, TYPE=C3D8, ELSET=Plate")
    for j in range(4):
        for i in range(4):
            first = 5 * j + i + 1
            lines.append("%d, %d, %d, %d, %d, %d, %d, %d, %d" % (4 * j + i + 1, first, first + 1, first + 6, first + 5,
                                                                 first + 25, first + 26, first + 31, first + 30))
    with open(file_name, "w") as mesh_file:
        mesh_file.write("\n".join(lines) + "\n")


def part_selection():
    return pmx.create.selection_by_ids('femodel', 'parts', 'parts', [pmx.model.parts.first().id])


def setup(work):
    pmx.model.parts.remove([part.name for part in pmx.model.parts])
    pmx.model.element_sets.remove(list(pmx.model.element_sets.get_names()))
    mesh_file_name = os.path.join(work, PREFIX + "Plate.inp")
    write_plate_mesh(mesh_file_name)
    pmx.import_file(mesh_file_name)
    pmx.model.materials.add(PREFIX + "Material", [pmx.create.elastic(210000, 0.3), pmx.create.density(DENSITY)])
    pmx.model.sections.add_solid(PREFIX + "Section", PREFIX + "Material", part_selection())
    pmx.model.node_sets.add(CLAMP, pmx.create.selection_by_ids('femodel', 'nodes', 'nodes',
                                                               [1, 6, 11, 16, 21, 26, 31, 36, 41, 46]))
    step = pmx.model.steps.add_static(STEP)
    step.bcs.add_fixed(PREFIX + "Fixed", pmx.create.region('NodeSetName', CLAMP))
    step.history_outputs.add_nodal(PREFIX + "Reactions", pmx.create.region('NodeSetName', CLAMP), ["RF"])
    pmx.model.coordinate_systems.add(CYLINDRICAL, [0, 0, 0], [1, 0, 0], [0, 1, 0],
                                     coordinate_system_type="Cylindrical")
    pmx.model.distributions.add_from_equation(SCALAR, "1 + 0.01 * x")
    pmx.model.distributions.add_from_equation(VECTOR, ["x", "y", "z"])


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
                lines.append((fields[0], fields[1].upper(), float(fields[2])))
    return lines


def exported_dload_lines(work, name):
    file_name = os.path.join(work, name + ".inp")
    pmx.export_model(file_name)
    return read_dload_lines(file_name)


def per_element_values(lines):
    values = {}
    for region, label, value in lines:
        values.setdefault(int(region), {})[label] = value
    return values


def test_api():
    loads = pmx.model.steps[STEP].loads
    load = loads.add_body_force(PREFIX + "Load", part_selection(), f1=3)
    check(load.item_type == "body_force", "unexpected item type " + load.item_type)
    check(load.region_type == "ElementSetName", "the part selection was not made an element set: " + load.region_type)
    part_element_ids = sorted(pmx.model.parts.first().get_element_ids())
    check(sorted(pmx.model.element_sets[load.region_name].get_element_ids()) == part_element_ids,
          "the part selection did not resolve to the elements of the part")
    check(load.get_value("f1") == 3 and load.get_value("f2") == 0, "the components were not set")
    check(load.get_value("distribution_name") == "Constant", "a new body force must be constant")
    # A replace keeps what it is not given
    load = loads.replace_body_force(PREFIX + "Load", f2=4)
    check(load.get_value("f1") == 3 and load.get_value("f2") == 4, "a replace must keep the components it is not given")
    load = loads.replace_body_force(PREFIX + "Load", f2=0)
    count = len(loads)
    check_refused(lambda: loads.add_body_force(PREFIX + "Bad", part_selection()), "'f1'",
                  "a body force without a component must be refused")
    check_refused(lambda: loads.add_body_force(PREFIX + "Bad", pmx.create.selection_by_ids('femodel', 'elements',
                                                                                          'elements', [1]), f1=1),
                  "requires target 'parts'", "an element selection must be refused naming the target")
    check_refused(lambda: loads.add_body_force(PREFIX + "Bad", part_selection(), f1=1, distribution_name="NoSuch"),
                  "NoSuch", "an unknown distribution must be refused")
    check(len(loads) == count and not loads.contains(PREFIX + "Bad"), "a refused body force was recorded")
    # The body force takes a coordinate system, which a gravity load still refuses
    load = loads.replace_body_force(PREFIX + "Load", coordinate_system_name=CYLINDRICAL.lower())
    check(load.coordinate_system_name == CYLINDRICAL, "the coordinate system was not kept in the model's spelling")
    load = loads.replace_body_force(PREFIX + "Load", coordinate_system_name="Global")
    check_refused(lambda: loads.add_gravity(PREFIX + "Bad", part_selection(), f1=1,
                                            coordinate_system_name=CYLINDRICAL),
                  "'coordinate_system_name'", "a gravity load must still refuse a coordinate system")
    check(not loads.contains(PREFIX + "Bad"), "a refused gravity load was recorded")


def test_uniform_lines(work):
    load = pmx.model.steps[STEP].loads[PREFIX + "Load"]
    lines = exported_dload_lines(work, "uniform")
    check(lines == [(load.region_name, "BX", 3.0)],
          "a constant body force must be one BX line on its element set, got " + str(lines))


def test_cylindrical_lines(work):
    loads = pmx.model.steps[STEP].loads
    loads.replace_body_force(PREFIX + "Load", f1=2, f2=0, f3=0, coordinate_system_name=CYLINDRICAL)
    values = per_element_values(exported_dload_lines(work, "cylindrical"))
    check(sorted(values.keys()) == list(range(1, 17)), "a cylindrical body force must be written per element, got " +
          str(sorted(values.keys())))
    for element_id, components in values.items():
        x, y, z = element_centroid(element_id)
        radius = math.hypot(x, y)
        check("BZ" not in components, "a radial force has no axial component: " + str(components))
        check(abs(components.get("BX", 0) - 2 * x / radius) < 1e-9 and
              abs(components.get("BY", 0) - 2 * y / radius) < 1e-9,
              "element %d must be loaded radially from its centroid, got %s" % (element_id, components))
    # The second component is tangential
    loads.replace_body_force(PREFIX + "Load", f1=0, f2=2)
    values = per_element_values(exported_dload_lines(work, "tangential"))
    for element_id, components in values.items():
        x, y, z = element_centroid(element_id)
        radius = math.hypot(x, y)
        check(abs(components.get("BX", 0) + 2 * y / radius) < 1e-9 and
              abs(components.get("BY", 0) - 2 * x / radius) < 1e-9,
              "element %d must be loaded tangentially, got %s" % (element_id, components))
    loads.replace_body_force(PREFIX + "Load", f1=3, f2=0, coordinate_system_name="Global")


def test_distribution_lines(work):
    loads = pmx.model.steps[STEP].loads
    load = loads.replace_body_force(PREFIX + "Load", f1=0, f3=4, distribution_name=SCALAR)
    check(load.get_value("distribution_name") == SCALAR, "the distribution was not set")
    values = per_element_values(exported_dload_lines(work, "scalar"))
    check(sorted(values.keys()) == list(range(1, 17)), "a distributed body force must be written per element")
    for element_id, components in values.items():
        x, y, z = element_centroid(element_id)
        check(list(components.keys()) == ["BZ"] and abs(components["BZ"] - 4 * (1 + 0.01 * x)) < 1e-9,
              "element %d must carry the scalar distribution at its centroid, got %s" % (element_id, components))
    # A vector distribution scales each component by its own value
    loads.replace_body_force(PREFIX + "Load", f1=1, f2=1, f3=1, distribution_name=VECTOR)
    values = per_element_values(exported_dload_lines(work, "vector"))
    for element_id, components in values.items():
        x, y, z = element_centroid(element_id)
        check(abs(components["BX"] - x) < 1e-9 and abs(components["BY"] - y) < 1e-9 and
              abs(components["BZ"] - z) < 1e-9,
              "element %d must carry the vector distribution at its centroid, got %s" % (element_id, components))
    loads.replace_body_force(PREFIX + "Load", f1=3, f2=0, f3=0, distribution_name="Constant")


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


def test_solved_like_gravity(work):
    # A body force of density times acceleration loads the elements as a gravity load of that acceleration does
    loads = pmx.model.steps[STEP].loads
    loads.replace_body_force(PREFIX + "Load", f1=DENSITY * ACCELERATION, f2=0, f3=-0.5 * DENSITY * ACCELERATION)
    body_force = solve_reactions(work, PREFIX + "Body_Force")
    loads.remove([PREFIX + "Load"])
    loads.add_gravity(PREFIX + "Gravity", part_selection(), f1=ACCELERATION, f3=-0.5 * ACCELERATION)
    gravity = solve_reactions(work, PREFIX + "Gravity")
    loads.remove([PREFIX + "Gravity"])
    check(abs(gravity[0]) > 1e-3, "the gravity load gave no reaction: " + str(gravity))
    for i in range(3):
        check(abs(body_force[i] - gravity[i]) <= 1e-6 * max(1.0, abs(gravity[i])),
              "the body force reactions %s differ from those of the gravity load %s" % (body_force, gravity))


def clean():
    for collection in [pmx.jobs, pmx.model.steps, pmx.model.sections, pmx.model.materials,
                       pmx.model.distributions, pmx.model.coordinate_systems, pmx.model.node_sets]:
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
        test_uniform_lines(work)
        test_cylindrical_lines(work)
        test_distribution_lines(work)
        test_uniform_lines(work)
        test_solved_like_gravity(work)
        clean()
    finally:
        shutil.rmtree(work, ignore_errors=True)
    print("TEST PASSED")


main()
