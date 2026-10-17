#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the composite shell sections: the API, the layers and the orientations written to the solver input for a
#flat plate, an inclined plate with a material orientation and a cylinder without one, a layer angle driven by a
#parameter, the S8R and S6 element type requirement, the mass of a layup of two materials, and the refusals.
#
#  PrePoMax.com -f composite_shells.inp -u MM_TON_S_C --script Test_Model_Composite_Sections.py -g No -x Yes -w <dir>
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


def close(u, v, tolerance=1e-6):
    return all(abs(a - b) <= tolerance for a, b in zip(u, v))


def read_deck():
    """Exports the solver input and returns {composite elset: (layer rows, {orientation: axes})}."""
    export_file = os.path.join(tempfile.gettempdir(), "pytest_composite_sections.inp")
    if os.path.isfile(export_file):
        os.remove(export_file)
    pmx.export_to_calculix(export_file)
    with open(export_file, "r") as reader:
        lines = [line.strip() for line in reader.read().splitlines()]
    os.remove(export_file)
    #
    def parameters(line):
        return {p.split("=")[0].strip().lower(): p.split("=")[1].strip() for p in line.split(",")[1:] if "=" in p}
    orientations, sections = {}, {}
    for i, line in enumerate(lines):
        lower = line.lower()
        if lower.startswith("*orientation"):
            values = [float(v) for v in lines[i + 1].split(",")]
            orientations[parameters(line)["name"].lower()] = (values[:3], values[3:])
        if lower.startswith("*shell section") and "composite" in lower:
            rows = []
            j = i + 1
            while j < len(lines) and not lines[j].startswith("*"):
                rows.append([v.strip() for v in lines[j].split(",")])
                j += 1
            sections[parameters(line)["elset"].lower()] = (line, rows)
    return {elset: (line, rows, {r[3].lower(): orientations[r[3].lower()] for r in rows if len(r) > 3})
            for elset, (line, rows) in sections.items()}


def section_groups(deck, section_name):
    return {elset: value for elset, value in deck.items() if elset.startswith(section_name.lower())}


def test_element_types():
    sections = pmx.model.sections
    # CalculiX takes a composite shell of S8R and S6 elements only
    pmx.model.parts.set_element_type(list(pmx.model.parts.get_names()), "parabolic_quad", "S8")
    message = raised_message(lambda: sections.add_composite_shell(PREFIX + "Bad", region("PLATE"),
                                                                  [[0.25, PREFIX + "Ply", 0]]))
    check(message is not None and "S8R" in message and "'region'" in message,
          "a composite section on S8 elements must be refused naming S8R: " + str(message))
    check(not sections.contains(PREFIX + "Bad"), "a refused composite section was recorded")
    pmx.model.parts.set_element_type(list(pmx.model.parts.get_names()), "parabolic_quad", "S8R")


def test_api():
    sections = pmx.model.sections
    laminate = sections.add_composite_shell(PREFIX + "Plate", region("PLATE"),
                                            [[0.25, PREFIX + "Ply", 0], [0.25, PREFIX + "Ply_2", 90]], symmetric=True)
    check(laminate.item_type == "composite_shell", "unexpected type " + laminate.item_type)
    check(laminate.symmetric is True and abs(laminate.thickness - 1.0) < 1e-12,
          "a symmetric layup of two 0.25 layers must be 1 thick: " + str(laminate.thickness))
    check([list(row) for row in laminate.layers] == [[0.25, PREFIX + "Ply", 0], [0.25, PREFIX + "Ply_2", 90]],
          "the layers did not read back: " + str(laminate.layers))
    check(laminate.material_name == PREFIX + "Ply", "the section material must be the one of layer 1")
    check(laminate.valid, "a composite section on S8R elements must be valid")
    # A replace keeps every value it is not given and replaces all layers at once
    laminate = sections.replace_composite_shell(PREFIX + "Plate", offset=0.5)
    check(laminate.symmetric is True and len(laminate.layers) == 2 and laminate.offset == 0.5,
          "a replace did not keep the layers or the symmetry: " + str(laminate.layers))
    laminate = sections.replace_composite_shell(PREFIX + "Plate", layers=[[0.25, PREFIX + "Ply", 0],
                                                                           [0.25, PREFIX + "Ply_2", 90]], offset=0)
    check(laminate.get_equations() == {}, "plain values must leave no equations: " + str(laminate.get_equations()))
    # Refusals name the argument and record nothing
    count = len(sections)
    for function, argument in [(lambda: sections.add_composite_shell(PREFIX + "Bad", region("PLATE"), []),
                                "'layers'"),
                               (lambda: sections.add_composite_shell(PREFIX + "Bad", region("PLATE"),
                                                                     [[0.25, PREFIX + "Ply"]]), "'layers[0]'"),
                               (lambda: sections.add_composite_shell(PREFIX + "Bad", region("PLATE"),
                                                                     [[0.25, "NoSuchMaterial", 0]]), "'layers[0]'"),
                               (lambda: sections.add_composite_shell(PREFIX + "Bad", region("PLATE"),
                                                                     [[0, PREFIX + "Ply", 0]]), "'layers[0]'"),
                               (lambda: sections.add_composite_shell(PREFIX + "Bad", region("PLATE"),
                                                                     [[0.25, PREFIX + "Ply", "=NoSuchParameter"]]),
                                "'layers[0]'")]:
        message = raised_message(function)
        check(message is not None and argument in message,
              "a refused composite section must name " + argument + ": " + str(message))
    check(len(sections) == count, "a refused composite section was recorded")
    # A replace never turns one section type into another
    message = raised_message(lambda: sections.replace_shell(PREFIX + "Plate", thickness=2))
    check(message is not None and "is made by add_composite_shell" in message,
          "a replace_shell of a composite section must be refused: " + str(message))


def test_deck():
    sections = pmx.model.sections
    r = rotation()
    origin = [0, 200, 0]
    pmx.model.coordinate_systems.add(PREFIX + "Inclined", origin, [origin[i] + r[i][0] for i in range(3)],
                                     [origin[i] + r[i][1] for i in range(3)])
    pmx.model.material_orientations.add(PREFIX + "Inclined", region("INCLINED"),
                                        coordinate_system_name=PREFIX + "Inclined")
    pmx.model.parameters.add(PREFIX + "Angle", 30)
    sections.add_composite_shell(PREFIX + "Inclined", region("INCLINED"),
                                 [[0.3, PREFIX + "Ply", "=" + PREFIX + "Angle"], [0.2, PREFIX + "Ply", 0]])
    sections.add_composite_shell(PREFIX + "Cylinder", region("CYLINDER"), [[0.5, PREFIX + "Ply", 0]])
    deck = read_deck()
    # The flat plate is one group whose layers turn the solver default by their angles, the mirror included
    plate = section_groups(deck, PREFIX + "Plate")
    check(len(plate) == 1, "a flat plate must be one group: " + str(plate.keys()))
    line, rows, orientations = list(plate.values())[0]
    check([[row[0], row[2].lower()] for row in rows] ==
          [["0.25", (PREFIX + "Ply").lower()], ["0.25", (PREFIX + "Ply_2").lower()],
           ["0.25", (PREFIX + "Ply_2").lower()], ["0.25", (PREFIX + "Ply").lower()]],
          "the symmetric layup must be written mirrored: " + str(rows))
    check(len(orientations) == 2, "two distinct angles must share two orientations: " + str(orientations.keys()))
    axes = [orientations[row[3].lower()][0] for row in rows]
    check(close(axes[0], [1, 0, 0]) and close(axes[1], [0, 1, 0]) and close(axes[2], [0, 1, 0]) and
          close(axes[3], [1, 0, 0]), "the plate layer axes are " + str(axes))
    # The inclined plate turns the axis of its orientation by the parameter
    line, rows, orientations = list(section_groups(deck, PREFIX + "Inclined").values())[0]
    c, s = math.cos(math.radians(30)), math.sin(math.radians(30))
    expected = [c * r[i][0] + s * r[i][1] for i in range(3)]
    check(close(orientations[rows[0][3].lower()][0], expected) and close(orientations[rows[1][3].lower()][0],
                                                                          [r[i][0] for i in range(3)]),
          "the inclined layer axes do not follow the orientation and the parameter: " + str(orientations))
    # A changed parameter changes the written axes, as a batch run with a .pmp does
    pmx.model.parameters.replace(PREFIX + "Angle", value=60)
    line, rows, orientations = list(section_groups(read_deck(), PREFIX + "Inclined").values())[0]
    c, s = math.cos(math.radians(60)), math.sin(math.radians(60))
    check(close(orientations[rows[0][3].lower()][0], [c * r[i][0] + s * r[i][1] for i in range(3)]),
          "a changed parameter must change the layer axes")
    # The cylinder without an orientation takes the projected global x axis, which turns with the element
    cylinder = section_groups(deck, PREFIX + "Cylinder")
    check(len(cylinder) > 1, "the cylinder must be several groups: " + str(len(cylinder)))


def plate_part():
    element_ids = set(pmx.model.element_sets["PLATE"].get_element_ids())
    for part in pmx.model.parts:
        if set(part.get_element_ids()) == element_ids:
            return part
    raise Exception("TEST FAILED: no part holds the element set PLATE")


def test_mass():
    clean(pmx.model.sections)
    skin, core = 1.6e-9, 1.0e-10
    pmx.model.materials.add(PREFIX + "Skin", [pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 3500, 0]]),
                                              pmx.create.density(skin)])
    pmx.model.materials.add(PREFIX + "Core", [pmx.create.elastic(100, 0.3), pmx.create.density(core)])
    # A symmetric skin and core layup weighs every solver layer with the density of its own material
    pmx.model.sections.add_composite_shell(PREFIX + "Sandwich", region("PLATE"),
                                           [[0.5, PREFIX + "Skin", 0], [2, PREFIX + "Core", 0]], symmetric=True)
    part = plate_part()
    expected = part.surface_area * 2 * (0.5 * skin + 2 * core)
    mass = part.get_mass()
    check(mass is not None and abs(mass - expected) <= 1e-9 * expected,
          "the sandwich plate must weigh %g, not %s" % (expected, mass))
    check(len(part.get_mass_warnings()) == 0, "the sandwich mass has warnings: " + str(part.get_mass_warnings()))
    # A layer material without a density excludes the elements and is named
    pmx.model.materials.add(PREFIX + "Dry", pmx.create.elastic(100, 0.3))
    pmx.model.sections.replace_composite_shell(PREFIX + "Sandwich", layers=[[0.5, PREFIX + "Skin", 0],
                                                                            [2, PREFIX + "Dry", 0]])
    check(part.get_mass() is None, "a layer without a density must leave the plate without a mass")
    warnings = " ".join(part.get_mass_warnings())
    check(PREFIX + "Dry" in warnings and PREFIX + "Sandwich" in warnings and PREFIX + "Skin" not in warnings,
          "the warning must name the layer material and the section: " + warnings)


def main():
    check(pmx.model.is_open(), "a model must be open")
    pmx.model.materials.add(PREFIX + "Ply", pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 3500, 0]]))
    pmx.model.materials.add(PREFIX + "Ply_2", pmx.create.elastic_lamina([[40000, 10000, 0.3, 4000, 4000, 3500, 0]]))
    test_element_types()
    test_api()
    test_deck()
    test_mass()
    clean(pmx.model.sections)
    clean(pmx.model.material_orientations)
    clean(pmx.model.materials)
    clean(pmx.model.coordinate_systems)
    clean(pmx.model.parameters)
    print("TEST PASSED")


main()
