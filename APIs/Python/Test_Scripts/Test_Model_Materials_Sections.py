#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the materials and the sections. Materials are the first composed model item, so this script
#also covers the pmx.create part convention. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Materials_Sections.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os
import tempfile

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
    temp = materials.add(PREFIX + "Temp",
                         [pmx.create.elastic([[210000, 0.3, 0], [180000, 0.32, 400]]),
                          pmx.create.thermal_expansion([[1.2e-5, 0], [1.4e-5, 400]])],
                         temperature_dependent=True)
    check(temp.property_count == 2, "the tabular properties were not stored")
    check(temp.temperature_dependent, "the temperature-dependent flag was not stored")
    # Orthotropic and lamina tables use v12/v13/v23 and carry temperature as the last value
    engineering = materials.add(
        PREFIX + "Engineering",
        pmx.create.elastic_engineering_constants([
            [140000, 10000, 9000, 0.28, 0.27, 0.35, 5000, 4500, 3500, 20],
            [123457, 9000, 8000, 0.27, 0.26, 0.34, 4500, 4000, 3000, 321]]),
        temperature_dependent=True)
    check(list(engineering.get_property_types()) == ["ElasticEngineeringConstants"],
          "the engineering constants property was not stored")
    check(engineering.temperature_dependent, "engineering constants lost temperature dependence")
    lamina = materials.add(
        PREFIX + "Lamina",
        pmx.create.elastic_lamina([
            [135000, 9500, 0.28, 5000, 4500, 3500, 20],
            [125001, 9000, 0.27, 4500, 4000, 3000, 322]]),
        description="Test lamina", temperature_dependent=True)
    check(list(lamina.get_property_types()) == ["ElasticLamina"], "the lamina property was not stored")
    # An omitted replace argument keeps its old value, including the property and the temperature flag
    lamina = materials.replace(PREFIX + "Lamina", description="Updated lamina")
    check(lamina.description == "Updated lamina", "the lamina description was not replaced")
    check(lamina.temperature_dependent, "replace lost the lamina temperature-dependent flag")
    check(list(lamina.get_property_types()) == ["ElasticLamina"], "replace lost the lamina property")
    # The Python-created state must reach the shared CalculiX/Abaqus material exporter
    export_file = os.path.join(tempfile.gettempdir(), "pytest_orthotropic_materials.inp")
    if os.path.isfile(export_file):
        os.remove(export_file)
    pmx.export_to_calculix(export_file)
    with open(export_file, "r") as reader:
        exported = reader.read()
    os.remove(export_file)
    engineering_block = exported.split("*Material, Name=" + PREFIX + "Engineering", 1)[1]
    engineering_block = engineering_block.split("*Material, Name=", 1)[0]
    lamina_block = exported.split("*Material, Name=" + PREFIX + "Lamina", 1)[1]
    lamina_block = lamina_block.split("*Material, Name=", 1)[0]
    check("*Elastic, Type=Engineering constants" in engineering_block,
          "engineering constants were not exported")
    check("123457" in engineering_block and "321" in engineering_block,
          "the second temperature-dependent engineering row was not exported")
    check("*Elastic, Type=Engineering constants" in lamina_block, "lamina was not exported")
    check("125001" in lamina_block and "322" in lamina_block,
          "the second temperature-dependent lamina row was not exported")
    # The model rejects a second elastic property
    check_raises(lambda: materials.add(PREFIX + "Bad", [pmx.create.elastic(1, 0.3), pmx.create.elastic(2, 0.3)]),
                 "a second elastic property must be rejected")
    # A row of the wrong width is rejected
    check_raises(lambda: pmx.create.elastic([[210000, 0.3]]), "a 2 value elastic row must be rejected")
    check_raises(lambda: pmx.create.elastic_engineering_constants([[1, 2, 3, 0.1, 0.1, 0.1, 4, 5, 6]]),
                 "a 9 value engineering constants row must be rejected")
    check_raises(lambda: pmx.create.elastic_lamina([[1, 2, 0.1, 3, 4, 5]]),
                 "a 6 value lamina row must be rejected")
    check_raises(lambda: pmx.create.plastic(200), "plastic must require a table")
    # Only parts created by pmx.create are accepted
    check_raises(lambda: materials.add(PREFIX + "Bad", ["not a property"]),
                 "a non property must be rejected")
    check_raises(lambda: materials.add(PREFIX + "Bad", []), "an empty property list must be rejected")
    # Collection verbs
    materials.rename(PREFIX + "Simple", PREFIX + "Renamed")
    check(materials.contains(PREFIX + "Renamed"), "the material was not renamed")
    materials.duplicate([PREFIX + "Renamed"])
    check(len(materials) == count_before + 6, "the material was not duplicated")
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
