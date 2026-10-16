#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the material orientations of shell elements: the API, the axes written to the solver input for a flat
#plate, an inclined plate and a cylinder, the grouping of elements whose axes agree, and the refusals.
#
#  PrePoMax.com -f composite_shells.inp -u MM_TON_S_C --script Test_Model_Material_Orientations.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import math
import os
import tempfile

PREFIX = "PyTest_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def clean(collection):
    names = [name for name in collection.get_names() if name.startswith(PREFIX)]
    if names:
        collection.remove(names)


def region(name):
    return pmx.create.region("ElementSetName", name)


def rotation():
    def rx(a):
        c, s = math.cos(a), math.sin(a)
        return [[1, 0, 0], [0, c, -s], [0, s, c]]

    def rz(a):
        c, s = math.cos(a), math.sin(a)
        return [[c, -s, 0], [s, c, 0], [0, 0, 1]]
    a, b = rz(math.radians(20)), rx(math.radians(35))
    return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


def column(matrix, j):
    return [matrix[i][j] for i in range(3)]


def close(u, v, tolerance=1e-6):
    return all(abs(a - b) <= tolerance for a, b in zip(u, v))


def read_deck():
    """Exports the solver input and returns {section elset: (orientation axes or None, element ids)}."""
    export_file = os.path.join(tempfile.gettempdir(), "pytest_material_orientations.inp")
    if os.path.isfile(export_file):
        os.remove(export_file)
    pmx.export_to_calculix(export_file)
    with open(export_file, "r") as reader:
        lines = [line.strip() for line in reader.read().splitlines()]
    os.remove(export_file)
    #
    def parameters(line):
        return {p.split("=")[0].strip().lower(): p.split("=")[1].strip()
                for p in line.split(",")[1:] if "=" in p}
    element_sets, orientations, sections = {}, {}, {}
    i = 0
    while i < len(lines):
        line = lines[i]
        lower = line.lower()
        if lower.startswith("*elset"):
            name = parameters(line)["elset"]
            ids = []
            i += 1
            while i < len(lines) and not lines[i].startswith("*"):
                ids += [int(v) for v in lines[i].split(",") if v.strip()]
                i += 1
            element_sets[name.lower()] = ids
            continue
        if lower.startswith("*orientation"):
            values = [float(v) for v in lines[i + 1].split(",")]
            orientations[parameters(line)["name"].lower()] = (values[:3], values[3:])
        if lower.startswith("*shell section"):
            p = parameters(line)
            sections[p["elset"].lower()] = p.get("orientation")
        i += 1
    deck = {}
    for elset, orientation in sections.items():
        axes = orientations[orientation.lower()] if orientation is not None else None
        deck[elset] = (axes, element_sets.get(elset))
    return deck


def section_groups(deck, section_name):
    return {elset: value for elset, value in deck.items() if elset.startswith(section_name.lower())}


def build():
    pmx.model.materials.add(PREFIX + "Ply", pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 3500, 0]]))
    for name in ("PLATE", "INCLINED", "CYLINDER"):
        pmx.model.sections.add_shell(PREFIX + name, PREFIX + "Ply", region(name), thickness=1)
    r = rotation()
    origin = [0, 200, 0]
    pmx.model.coordinate_systems.add(PREFIX + "Inclined", origin,
                                     [origin[i] + r[i][0] for i in range(3)],
                                     [origin[i] + r[i][1] for i in range(3)])
    pmx.model.coordinate_systems.add(PREFIX + "Axis", [300, 0, 0], [301, 0, 0], [300, 1, 0],
                                     coordinate_system_type="Cylindrical")


def test_api():
    orientations = pmx.model.material_orientations
    plate = orientations.add(PREFIX + "Plate", region("PLATE"))
    check(plate.coordinate_system_name == "Global" and plate.reference_axis == "X" and plate.rotation_angle_deg == 0,
          "the defaults were not applied: " + str(plate))
    check(plate.valid and plate.active, "a new orientation must be valid and active")
    # A replace keeps every value it is not given
    plate = orientations.replace(PREFIX + "Plate", rotation_angle_deg="=15*2")
    check(plate.reference_axis == "X" and abs(plate.rotation_angle_deg - 30) < 1e-12,
          "a replace did not keep the reference axis or set the angle: " + str(plate))
    check(plate.get_equations() == {"rotation_angle_deg": "=15*2"},
          "the angle expression did not read back: " + str(plate.get_equations()))
    plate = orientations.replace(PREFIX + "Plate", rotation_angle_deg=0, reference_axis="y")
    check(plate.reference_axis == "Y" and plate.get_equations() == {},
          "the reference axis was not replaced: " + str(plate))
    orientations.replace(PREFIX + "Plate", reference_axis="X")
    # The symbol density is a percentage of the full density of the axes symbols
    check(plate.symbol_density == 50, "the symbol density must default to 50 percent: " + str(plate.symbol_density))
    plate = orientations.replace(PREFIX + "Plate", symbol_density=25)
    check(plate.symbol_density == 25 and plate.reference_axis == "X",
          "the symbol density was not replaced, or the replace changed another value: " + str(plate))
    # Refusals name the argument and record nothing
    count = len(orientations)
    for function, argument in [(lambda: orientations.add(PREFIX + "Bad", region("PLATE"), reference_axis="W"),
                                "'reference_axis'"),
                               (lambda: orientations.add(PREFIX + "Bad", region("PLATE"),
                                                         coordinate_system_name="NoSuchSystem"),
                                "'coordinate_system_name'"),
                               (lambda: orientations.add(PREFIX + "Bad", region("PLATE"),
                                                         rotation_angle_deg="=NoSuchParameter"),
                                "'rotation_angle_deg'"),
                               (lambda: orientations.add(PREFIX + "Bad", region("PLATE"), symbol_density=101),
                                "'symbol_density'"),
                               (lambda: orientations.add(PREFIX + "Bad", region("PLATE"), symbol_density=2.5),
                                "'symbol_density'"),
                               (lambda: orientations.add(PREFIX + "Plate", region("PLATE")), "already exists")]:
        message = raised_message(function)
        check(message is not None and argument in message,
              "a refused orientation must name " + argument + ": " + str(message))
    check(len(orientations) == count, "a refused orientation was recorded")
    # Where regions overlap, the later active orientation applies, as a later section does
    overlap = orientations.add(PREFIX + "Overlap", region("PLATE"), rotation_angle_deg=90)
    check(overlap.valid and orientations[PREFIX + "Plate"].valid, "an overlap must make no orientation invalid")
    axes = list(section_groups(read_deck(), PREFIX + "PLATE").values())[0][0]
    check(close(axes[0], [0, 1, 0]), "the later orientation must apply where regions overlap: " + str(axes))
    orientations.deactivate([PREFIX + "Overlap"])
    axes = list(section_groups(read_deck(), PREFIX + "PLATE").values())[0][0]
    check(close(axes[0], [1, 0, 0]), "a deactivated later orientation must leave the earlier one: " + str(axes))
    orientations.remove([PREFIX + "Overlap"])
    # The collection verbs
    orientations.duplicate([PREFIX + "Plate"])
    duplicates = [name for name in orientations.get_names() if name != PREFIX + "Plate" and name.startswith(PREFIX)]
    check(len(duplicates) == 1, "the orientation was not duplicated: " + str(list(orientations.get_names())))
    orientations.remove(duplicates)
    orientations.rename(PREFIX + "Plate", PREFIX + "Plate_R")
    check(orientations.contains(PREFIX + "Plate_R"), "the orientation was not renamed")
    orientations.rename(PREFIX + "Plate_R", PREFIX + "Plate")


def test_deck():
    orientations = pmx.model.material_orientations
    orientations.add(PREFIX + "Inclined", region("INCLINED"), coordinate_system_name=(PREFIX + "Inclined").lower(),
                     rotation_angle_deg=30)
    orientations.add(PREFIX + "Cylinder", region("CYLINDER"), coordinate_system_name=PREFIX + "Axis",
                     reference_axis="Y")
    check(orientations[PREFIX + "Inclined"].coordinate_system_name == PREFIX + "Inclined",
          "the coordinate system must be stored in the model's own spelling")
    deck = read_deck()
    # A flat plate is one group with the global axes
    plate = section_groups(deck, PREFIX + "PLATE")
    check(len(plate) == 1, "a flat plate must be one orientation group: " + str(plate))
    axes, ids = list(plate.values())[0]
    check(axes is not None and close(axes[0], [1, 0, 0]) and close(axes[1], [0, 1, 0]),
          "the plate must take the global axes: " + str(axes))
    check(len(ids) == 8, "the plate group must hold its 8 elements: " + str(ids))
    # The inclined plate takes axis 1 of its own coordinate system turned by 30 degrees about the normal
    inclined = section_groups(deck, PREFIX + "INCLINED")
    check(len(inclined) == 1, "a flat inclined plate must be one orientation group: " + str(inclined))
    axes, ids = list(inclined.values())[0]
    r = rotation()
    c, s = math.cos(math.radians(30)), math.sin(math.radians(30))
    expected1 = [c * r[i][0] + s * r[i][1] for i in range(3)]
    expected2 = [-s * r[i][0] + c * r[i][1] for i in range(3)]
    check(close(axes[0], expected1) and close(axes[1], expected2),
          "the inclined plate axes are " + str(axes) + ", not " + str((expected1, expected2)))
    # The cylinder takes the hoop direction, which turns with the element, so each of the 8 columns of
    # elements is a group of its own whose axis 1 is tangential at the element centre
    cylinder = section_groups(deck, PREFIX + "CYLINDER")
    check(len(cylinder) == 8, "the cylinder must be 8 orientation groups: " + str(len(cylinder)))
    for axes, ids in cylinder.values():
        check(len(ids) == 2, "each cylinder group must hold the 2 elements of a column: " + str(ids))
        check(abs(axes[0][2]) < 1e-9 and abs(abs(axes[1][2]) - 1) < 1e-9,
              "a cylinder axis 1 must be tangential and axis 2 axial: " + str(axes))
    # A deactivated orientation leaves its section to the solver default
    orientations.deactivate([PREFIX + "Cylinder"])
    check(not orientations[PREFIX + "Cylinder"].active, "the orientation was not deactivated")
    deck = read_deck()
    check(len(section_groups(deck, PREFIX + "CYLINDER")) == 0 and "cylinder" in deck and deck["cylinder"][0] is None,
          "a section without an active orientation must be written on its own region without one: " + str(deck))
    orientations.activate([PREFIX + "Cylinder"])


def main():
    check(pmx.model.is_open(), "a model must be open")
    build()
    test_api()
    test_deck()
    clean(pmx.model.material_orientations)
    clean(pmx.model.sections)
    clean(pmx.model.materials)
    clean(pmx.model.coordinate_systems)
    print("TEST PASSED")


main()
