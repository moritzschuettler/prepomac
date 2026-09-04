#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the materials and the sections. Materials are the first composed model item, so this script
#also covers the pmx.create part convention. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Materials_Sections.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def clean(collection):
    names = [n for n in collection.get_names() if n.startswith(PREFIX)]
    if names:
        collection.remove(names)


def test_materials():
    clean(pmx.model.sections)
    clean(pmx.model.materials)
    clean(pmx.model.element_sets)
    materials = pmx.model.materials
    count_before = len(materials)
    # A part is created first and passed as an argument
    elastic = pmx.create.elastic(210000, 0.3)
    density = pmx.create.density(7.85e-9)
    material = materials.add(PREFIX + "Steel", [elastic, density], description="Test steel")
    check(materials.contains(PREFIX + "Steel"), "the material was not added")
    check(material.property_count == 2, "2 properties expected, found " + str(material.property_count))
    check(material.description == "Test steel", "the description was not stored")
    types = list(material.get_property_types())
    check("Elastic" in types and "Density" in types, "unexpected property types " + str(types))
    # A single property does not have to be wrapped in a list
    materials.add(PREFIX + "Simple", pmx.create.elastic_with_density(70000, 0.33, 2.7e-9))
    check(pmx.model.materials[PREFIX + "Simple"].property_count == 1, "the single property was not stored")
    # Temperature dependent properties are given as rows, the last value being the temperature
    materials.add(PREFIX + "Temp", [pmx.create.elastic([[210000, 0.3, 0], [180000, 0.32, 400]]),
                                    pmx.create.thermal_expansion([[1.2e-5, 0], [1.4e-5, 400]])])
    check(pmx.model.materials[PREFIX + "Temp"].property_count == 2, "the tabular properties were not stored")
    # The model rejects a second elastic property
    check_raises(lambda: materials.add(PREFIX + "Bad", [pmx.create.elastic(1, 0.3), pmx.create.elastic(2, 0.3)]),
                 "a second elastic property must be rejected")
    # A row of the wrong width is rejected
    check_raises(lambda: pmx.create.elastic([[210000, 0.3]]), "a 2 value elastic row must be rejected")
    check_raises(lambda: pmx.create.plastic(200), "plastic must require a table")
    # Only parts created by pmx.create are accepted
    check_raises(lambda: materials.add(PREFIX + "Bad", ["not a property"]),
                 "a non property must be rejected")
    check_raises(lambda: materials.add(PREFIX + "Bad", []), "an empty property list must be rejected")
    # Collection verbs
    materials.rename(PREFIX + "Simple", PREFIX + "Renamed")
    check(materials.contains(PREFIX + "Renamed"), "the material was not renamed")
    materials.duplicate([PREFIX + "Renamed"])
    check(len(materials) == count_before + 4, "the material was not duplicated")
    return PREFIX + "Steel"


def test_sections(material_name):
    sections = pmx.model.sections
    count_before = len(sections)
    element_selection = pmx.create.selection_by_ids('femodel', 'elements', 'elements', [1])
    node_selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])
    #
    section = sections.add_solid(PREFIX + "Solid", material_name, element_selection)
    check(sections.contains(PREFIX + "Solid"), "the solid section was not added")
    check(section.section_type == "SolidSection", "unexpected section type " + section.section_type)
    check(section.material_name == material_name, "unexpected material name")
    # The controller converts the selection into an internal element set
    check(section.region_type == "ElementSetName",
          "the selection was not converted, region type is " + section.region_type)
    check(section.region_name is not None and len(section.region_name) > 0, "the region name is empty")
    #
    sections.add_point_mass(PREFIX + "Mass", node_selection, 0.5)
    check(sections[PREFIX + "Mass"].section_type == "PointMassSection", "the point mass section was not added")
    check(len(sections) == count_before + 2, "unexpected section count")
    # An unknown material is rejected before anything is recorded
    check_raises(lambda: sections.add_solid(PREFIX + "Bad", "NoSuchMaterial", element_selection),
                 "an unknown material must be rejected")
    check(not sections.contains(PREFIX + "Bad"), "a rejected section must not be recorded")
    # A section takes a named region as well as a selection
    pmx.model.element_sets.add(PREFIX + "ES", element_selection)
    section = sections.add_solid(PREFIX + "Solid_R", material_name,
                                 pmx.create.region('ElementSetName', PREFIX + "ES"))
    check(section.region_name == PREFIX + "ES", "unexpected region name " + str(section.region_name))
    check(section.region_type == "ElementSetName", "unexpected region type " + section.region_type)
    check_raises(lambda: sections.add_solid(PREFIX + "Bad", material_name,
                                            pmx.create.region('ElementSetName', 'NoSuchSet')),
                 "an unknown element set must be rejected")
    # Collection verbs
    sections.rename(PREFIX + "Mass", PREFIX + "Mass_R")
    check(sections.contains(PREFIX + "Mass_R"), "the section was not renamed")


def main():
    check(pmx.model.is_open(), "a model must be open")
    material_name = test_materials()
    test_sections(material_name)
    # A section references a material, so the sections go first
    clean(pmx.model.sections)
    clean(pmx.model.materials)
    print("TEST PASSED")


main()
