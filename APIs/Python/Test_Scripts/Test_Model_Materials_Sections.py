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


def check_raises(function, message, unknown_keyword=None):
    # Only the API's own refusal passes: a PythonAPIException, or for a call that deliberately passes the
    # keyword unknown_keyword, the TypeError of the keyword guard. Anything else, and a message carrying a
    # .NET stack trace, is a fault of the application that happened to stop the call
    try:
        function()
    except Exception as ex:
        kind, text = type(ex).__name__, str(ex)
        if unknown_keyword is None:
            expected, refusal = kind == "PythonAPIException", "a PythonAPIException"
        else:
            expected = kind == "TypeError" and "unexpected keyword argument '%s'" % unknown_keyword in text
            refusal = "the keyword guard's TypeError for '%s'" % unknown_keyword
        if not expected or "   at " in text:
            raise Exception("TEST FAILED: " + message + " - the refusal must be " + refusal +
                            " without a .NET stack trace, not " + kind + ": " + text[0:500])
        return
    raise Exception("TEST FAILED: " + message)


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


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
    # The activation state reads back on the item
    check(material.active, "a new material must be active")
    check(material.valid, "a new material must be valid")
    materials.deactivate([PREFIX + "Steel"])
    check(not materials[PREFIX + "Steel"].active, "the material was not deactivated")
    materials.activate([PREFIX + "Steel"])
    check(materials[PREFIX + "Steel"].active, "the material was not activated")
    types = list(material.get_property_types())
    check("elastic" in types and "density" in types, "unexpected property types " + str(types))
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
    check(list(engineering.get_property_types()) == ["elastic_engineering_constants"],
          "the engineering constants property was not stored")
    check(engineering.temperature_dependent, "engineering constants lost temperature dependence")
    # A plastic hardening rule and a thermal expansion zero temperature are user inputs, so a script
    # that gives them must not fall back to the constructor defaults
    materials.add(PREFIX + "Kinematic",
                  [pmx.create.elastic(210000, 0.3),
                   pmx.create.plastic([[250, 0, 0]], hardening="Kinematic")])
    materials.add(PREFIX + "Isotropic",
                  [pmx.create.elastic(210000, 0.3), pmx.create.plastic([[250, 0, 0]])])
    materials.add(PREFIX + "Expansion",
                  [pmx.create.elastic(210000, 0.3),
                   pmx.create.thermal_expansion(1.2e-5, zero_temperature=123)])
    check_raises(lambda: pmx.create.plastic([[250, 0, 20]], hardening="NoSuchRule"),
                 "an unknown hardening rule must be rejected")
    #
    lamina = materials.add(
        PREFIX + "Lamina",
        pmx.create.elastic_lamina([
            [135000, 9500, 0.28, 5000, 4500, 3500, 20],
            [125001, 9000, 0.27, 4500, 4000, 3000, 322]]),
        description="Test lamina", temperature_dependent=True)
    check(list(lamina.get_property_types()) == ["elastic_lamina"], "the lamina property was not stored")
    # An omitted replace argument keeps its old value, including the property and the temperature flag
    lamina = materials.replace(PREFIX + "Lamina", description="Updated lamina")
    check(lamina.description == "Updated lamina", "the lamina description was not replaced")
    check(lamina.temperature_dependent, "replace lost the lamina temperature-dependent flag")
    check(list(lamina.get_property_types()) == ["elastic_lamina"], "replace lost the lamina property")
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
    kinematic_block = exported.split("*Material, Name=" + PREFIX + "Kinematic", 1)[1]
    kinematic_block = kinematic_block.split("*Material, Name=", 1)[0]
    isotropic_block = exported.split("*Material, Name=" + PREFIX + "Isotropic", 1)[1]
    isotropic_block = isotropic_block.split("*Material, Name=", 1)[0]
    expansion_block = exported.split("*Material, Name=" + PREFIX + "Expansion", 1)[1]
    expansion_block = expansion_block.split("*Material, Name=", 1)[0]
    check("Hardening=Kinematic" in kinematic_block, "the kinematic hardening rule was not stored")
    check("Hardening" not in isotropic_block, "the default hardening rule must stay implicit")
    check("Zero=" in expansion_block, "the thermal expansion zero temperature was not stored")
    zero_text = expansion_block.split("Zero=", 1)[1].split("\n", 1)[0].strip().rstrip(",")
    check(abs(float(zero_text) - 123.0) < 1e-6,
          "the given zero temperature was not stored, it is " + zero_text)
    # The model rejects a second elastic property, and the API names the argument
    message = raised_message(lambda: materials.add(PREFIX + "Bad", [pmx.create.elastic(1, 0.3),
                                                                    pmx.create.elastic(2, 0.3)]))
    check(message is not None and "'properties'" in message,
          "a second elastic property must be refused naming the argument: " + str(message))
    # The dialog adds a property type once, so a repeated one is refused naming the argument and the type
    count_before_repeated = len(materials)
    for properties in [[pmx.create.density(1e-9), pmx.create.density(2e-9)],
                       [pmx.create.elastic(1, 0.3), pmx.create.plastic([[250, 0, 20]]),
                        pmx.create.plastic([[300, 0, 20]])]]:
        message = raised_message(lambda: materials.add(PREFIX + "Bad", properties))
        check(message is not None and "'properties'" in message and "more than once" in message,
              "a repeated material property type must be refused: " + str(message))
    check(len(materials) == count_before_repeated, "a material with a repeated property type was recorded")
    # The elastic property with density holds the density, which the solver input would otherwise get twice
    message = raised_message(lambda: materials.add(PREFIX + "Bad", [pmx.create.elastic_with_density(1, 0.3, 1e-9),
                                                                    pmx.create.density(2e-9)]))
    check(message is not None and "'properties'" in message and "elastic_with_density" in message and
          "density" in message, "a density beside elastic_with_density must be refused: " + str(message))
    message = raised_message(lambda: materials.replace(PREFIX + "Simple", properties=[
        pmx.create.density(2e-9), pmx.create.elastic_with_density(70000, 0.33, 2.7e-9)]))
    check(message is not None and "'properties'" in message,
          "a replace with a density beside elastic_with_density must be refused: " + str(message))
    check(list(materials[PREFIX + "Simple"].get_property_types()) == ["elastic_with_density"],
          "a refused replace changed the material")
    check(len(materials) == count_before_repeated, "a material with two densities was recorded")
    # A material that is not temperature dependent writes only the first row without its temperature, so a
    # property the analysis would cut short is refused rather than recorded
    count_before_rows = len(materials)
    for properties in [pmx.create.elastic([[210000, 0.3, 0], [180000, 0.32, 400]]),
                       pmx.create.elastic([[210000, 0.3, 20]]),
                       [pmx.create.elastic(210000, 0.3), pmx.create.density([[7.85e-9, 0], [7.8e-9, 400]])],
                       [pmx.create.elastic(210000, 0.3), pmx.create.plastic([[250, 0, 20]])],
                       [pmx.create.elastic(210000, 0.3),
                        pmx.create.plastic([[250, 0, 0], [300, 0.05, 0], [200, 0, 400], [250, 0.05, 400]])]]:
        message = raised_message(lambda: materials.add(PREFIX + "Bad", properties))
        check(message is not None and "temperature_dependent" in message,
              "rows a material that is not temperature dependent drops must be refused: " + str(message))
    check(len(materials) == count_before_rows, "a refused material was recorded")
    message = raised_message(lambda: materials.replace(PREFIX + "Temp", temperature_dependent=False))
    check(message is not None and "temperature_dependent" in message,
          "turning off the temperature dependence of a tabular material must be refused: " + str(message))
    check(materials[PREFIX + "Temp"].temperature_dependent, "a refused replace changed the material")
    # A value the model refuses is refused by the maker, naming the argument
    for maker, argument in [(lambda: pmx.create.elastic(210000), "'poissons_ratio'"),
                            (lambda: pmx.create.elastic(-1, 0.3), "'youngs_modulus'"),
                            (lambda: pmx.create.density(-1), "'density'"),
                            (lambda: pmx.create.specific_heat(0), "'specific_heat'"),
                            (lambda: pmx.create.thermal_conductivity([[50, 0], [-45, 400]]), "'thermal_conductivity'"),
                            (lambda: pmx.create.thermal_expansion(0), "'thermal_expansion'"),
                            (lambda: pmx.create.elastic_with_density(70000, 0.33, -1), "'density'"),
                            (lambda: pmx.create.slip_wear(0, 1e-4), "'hardness'"),
                            (lambda: pmx.create.elastic_engineering_constants(
                                [[1000, 1000, 1000, 0.9, 0.9, 0.9, 400, 400, 400, 0]]), "'constants'"),
                            (lambda: pmx.create.elastic_lamina(
                                [[135000, 9500, 0.28, 5000, 4500, 3500, 20],
                                 [125001, 9000, 0.27, 4500, 4000, 3000, 10]]), "'constants'")]:
        message = raised_message(maker)
        check(message is not None and argument in message,
              "a value the model refuses must be refused naming " + argument + ": " + str(message))
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
    # The model keys its materials without regard to case, and so does the name check
    message = raised_message(lambda: materials.rename(PREFIX + "Steel", (PREFIX + "Renamed").lower()))
    check(message is not None and "already exists" in message,
          "a rename onto another material in other case must be refused: " + str(message))
    check(materials[PREFIX + "Steel"].name == PREFIX + "Steel",
          "the refused rename changed the name to " + materials[PREFIX + "Steel"].name)
    message = raised_message(lambda: materials.add((PREFIX + "Steel").lower(), pmx.create.elastic(1, 0.3)))
    check(message is not None and "already exists" in message,
          "a material named like another in other case must be refused: " + str(message))
    # The model would trim a name with surrounding spaces and store it under another spelling than the one the
    # script goes on to use, and it cannot take a space or a character it does not allow at all, so all of those
    # are refused before anything is recorded
    count_before_refusals = len(materials)
    for bad_name in [" " + PREFIX + "Spaced ", PREFIX + "Bad Name", PREFIX + "Bad#", "123"]:
        message = raised_message(lambda: materials.add(bad_name, pmx.create.elastic(1, 0.3)))
        check(message is not None and "'name'" in message,
              "the name " + repr(bad_name) + " must be refused naming the argument: " + str(message))
    message = raised_message(lambda: materials.replace(PREFIX + "Steel", name=PREFIX + "Bad Name"))
    check(message is not None and "'name'" in message, "a replace to a bad name must be refused: " + str(message))
    check(len(materials) == count_before_refusals and materials.contains(PREFIX + "Steel"),
          "a refused material name was recorded")
    check(materials.contains((PREFIX + "Steel").lower()), "contains must not depend on the case of the name")
    materials.rename(PREFIX + "Steel", (PREFIX + "Steel").upper())
    check(materials[(PREFIX + "Steel").upper()].name == (PREFIX + "Steel").upper(),
          "a rename to the same name in other case must be allowed")
    materials.rename((PREFIX + "Steel").upper(), PREFIX + "Steel")
    materials.duplicate([PREFIX + "Renamed"])
    check(len(materials) == count_before + 9, "the material was not duplicated")
    # A material name also takes the character Material adds to the ones every name may contain
    materials.add(PREFIX + "At@1", pmx.create.elastic(1, 0.3))
    check(materials.contains(PREFIX + "At@1"), "a material name with an at sign must be accepted")
    materials.remove([PREFIX + "At@1"])
    return PREFIX + "Steel"


def plain_values(prop):
    """The values of a property with every table turned into a list of row lists, so that they compare."""
    values = {}
    for key, value in prop.get_values().items():
        if value is not None and not isinstance(value, (str, int, float)):
            value = [list(row) for row in value]
        values[key] = value
    return values


def close(first, second):
    """True when two read back values agree, numbers to a relative tolerance."""
    if isinstance(first, dict) and isinstance(second, dict):
        return sorted(first) == sorted(second) and all(close(first[k], second[k]) for k in first)
    if isinstance(first, list) and isinstance(second, list):
        return len(first) == len(second) and all(close(a, b) for a, b in zip(first, second))
    if isinstance(first, (int, float)) and isinstance(second, (int, float)):
        return abs(first - second) <= 1e-9 * max(1.0, abs(first), abs(second))
    return first == second


def test_material_properties():
    materials = pmx.model.materials
    materials.add(PREFIX + "Thermal", [pmx.create.specific_heat(500),
                                       pmx.create.thermal_conductivity([[50, 0], [45, 400]]),
                                       pmx.create.slip_wear(200, 1e-4)], temperature_dependent=True)
    # Every value reads back under the pmx.create argument that set it, a table as its rows
    steel = [plain_values(p) for p in materials[PREFIX + "Steel"].get_properties()]
    check(close(steel, [{"youngs_modulus": 210000, "poissons_ratio": 0.3}, {"density": 7.85e-9}]),
          "the constant properties did not read back: " + str(steel))
    temp = [plain_values(p) for p in materials[PREFIX + "Temp"].get_properties()]
    check(close(temp[0], {"youngs_modulus": [[210000, 0.3, 0], [180000, 0.32, 400]]}),
          "the elastic table did not read back: " + str(temp[0]))
    check(close(temp[1]["thermal_expansion"], [[1.2e-5, 0], [1.4e-5, 400]]),
          "the thermal expansion table did not read back: " + str(temp[1]))
    kinematic = plain_values(list(materials[PREFIX + "Kinematic"].get_properties())[1])
    check(close(kinematic, {"stress_strain": [[250, 0, 0]], "hardening": "Kinematic"}),
          "the plastic values did not read back: " + str(kinematic))
    expansion = list(materials[PREFIX + "Expansion"].get_properties())[1]
    check(expansion.property_type == "thermal_expansion", "unexpected type " + expansion.property_type)
    check(close(expansion.get_value("zero_temperature"), 123), "the zero temperature did not read back")
    check(close(expansion.get_value("thermal_expansion"), 1.2e-5), "the expansion did not read back")
    check(close(plain_values(list(materials[PREFIX + "Renamed"].get_properties())[0]),
                {"youngs_modulus": 70000, "poissons_ratio": 0.33, "density": 2.7e-9}),
          "the elastic with density values did not read back")
    lamina = plain_values(list(materials[PREFIX + "Lamina"].get_properties())[0])
    check(close(lamina, {"constants": [[135000, 9500, 0.28, 5000, 4500, 3500, 20],
                                       [125001, 9000, 0.27, 4500, 4000, 3000, 322]]}),
          "the lamina constants did not read back: " + str(lamina))
    thermal = [plain_values(p) for p in materials[PREFIX + "Thermal"].get_properties()]
    check(close(thermal, [{"specific_heat": 500}, {"thermal_conductivity": [[50, 0], [45, 400]]},
                          {"hardness": 200, "wear_coefficient": 1e-4}]),
          "the thermal and wear values did not read back: " + str(thermal))
    check("youngs_modulus=210000" in repr(list(materials[PREFIX + "Steel"].get_properties())[0]),
          "the property does not print its values")
    check_raises(lambda: expansion.get_value("no_such_value"), "an unknown value name was accepted")
    # A padded name is no name, so the value is not found under it
    message = raised_message(lambda: expansion.get_value(" zero_temperature"))
    check(message is not None and "thermal_expansion material property" in message,
          "a padded value name must not be trimmed: " + str(message))
    # The property values read back as their own names, the same way every detached part does
    check(sorted(expansion.get_value_names()) == sorted(expansion.get_values().keys()),
          "get_value_names must list the get_values names: " + str(list(expansion.get_value_names())))
    # The values are the keyword arguments of the maker, and the properties go straight back into a replace
    for name in [PREFIX + "Steel", PREFIX + "Temp", PREFIX + "Engineering", PREFIX + "Kinematic",
                 PREFIX + "Expansion", PREFIX + "Lamina", PREFIX + "Renamed", PREFIX + "Thermal"]:
        properties = list(materials[name].get_properties())
        for prop in properties:
            maker = getattr(pmx.create, prop.property_type)
            check(close(plain_values(maker(**prop.get_values())), plain_values(prop)),
                  "the " + prop.property_type + " values of " + name + " do not make the same property again")
        before = [plain_values(p) for p in properties]
        materials.replace(name, properties=materials[name].get_properties())
        after = [plain_values(p) for p in materials[name].get_properties()]
        check(close(after, before), "the properties of " + name + " did not round trip through replace: " +
              str(after))
    # The properties are a plain Python list, so a list operation extends them
    properties = materials[PREFIX + "Thermal"].get_properties()
    check(type(properties) is list, "get_properties must return a list: " + type(properties).__name__)
    check(len(properties + [pmx.create.density(7.8e-9)]) == len(properties) + 1,
          "the properties do not extend like a list")
    # A replace without a name keeps the material under its own spelling, whatever spelling old_name has
    count = len(materials)
    for spelling in [(PREFIX + "Thermal").lower(), (PREFIX + "Thermal").upper()]:
        material = materials.replace(spelling, description="mixed case")
        check(material.name == PREFIX + "Thermal" and PREFIX + "Thermal" in list(materials.get_names()),
              "a replace of '" + spelling + "' changed the stored spelling: " + str(list(materials.get_names())))
    check(len(materials) == count, "a mixed case replace changed the number of materials")
    materials.remove([PREFIX + "Thermal"])


def test_parametric_materials():
    # The material dialog takes an expression for every value, and the makers take it as an equation string
    materials = pmx.model.materials
    parameters = pmx.model.parameters
    young = PREFIX + "E"
    zero = PREFIX + "Zero"
    parameters.add(young, 210000)
    parameters.add(zero, 0)
    elastic = pmx.create.elastic("=" + young, 0.3)
    values = elastic.get_values()
    check(values["youngs_modulus"].replace(" ", "") == "=" + young and close(values["poissons_ratio"], 0.3),
          "the elastic equation did not read back as an expression: " + str(values))
    # A table with an expression reads back as a list of rows, its plain values numbers
    table = pmx.create.elastic([[young + " * 1", 0.3, 0], [180000, 0.32, 400]]).get_values()["youngs_modulus"]
    check(type(table) is list and table[0][0].replace(" ", "") == "=" + young + "*1" and close(table[1][0], 180000),
          "the elastic table did not read back with its expression: " + str(table))
    expansion = pmx.create.thermal_expansion(1.2e-5, zero_temperature=zero)
    check(expansion.get_value("zero_temperature") == "=" + zero,
          "a zero temperature expression evaluating to 0 did not read back: " + str(expansion.get_value("zero_temperature")))
    properties = [elastic, pmx.create.density("=" + young + " * 3.7e-14"), expansion,
                  pmx.create.slip_wear("=" + young + " / 1000", 1e-4)]
    materials.add(PREFIX + "Param", properties)
    # The expressions survive the material, the maker and a replace
    for prop in materials[PREFIX + "Param"].get_properties():
        maker = getattr(pmx.create, prop.property_type)
        check(close(plain_values(maker(**prop.get_values())), plain_values(prop)),
              "the " + prop.property_type + " expression does not make the same property again: " +
              str(prop.get_values()))
    before = [plain_values(p) for p in materials[PREFIX + "Param"].get_properties()]
    materials.replace(PREFIX + "Param", properties=materials[PREFIX + "Param"].get_properties())
    after = [plain_values(p) for p in materials[PREFIX + "Param"].get_properties()]
    check(close(after, before), "the expressions did not round trip through replace: " + str(after))
    check(any(isinstance(v, str) and v.startswith("=") for p in after for v in p.values()),
          "the material lost its expressions: " + str(after))
    # An expression is checked as a value is, and the refusal names the argument
    message = raised_message(lambda: pmx.create.elastic("-" + young, 0.3))
    check(message is not None and "'youngs_modulus'" in message,
          "a negative Young's modulus expression must be refused naming the argument: " + str(message))
    message = raised_message(lambda: pmx.create.slip_wear(1, "=" + PREFIX + "NoSuchParameter"))
    check(message is not None and "'wear_coefficient'" in message,
          "an unknown parameter must be refused naming the argument: " + str(message))
    materials.remove([PREFIX + "Param"])
    parameters.remove([young, zero])


def import_shell_part():
    """Imports a single triangle as a shell part beside the solid cube, and returns its name and element ID."""
    names_before = set(pmx.model.parts.get_names())
    file_name = os.path.join(tempfile.mkdtemp(prefix="pytest_shell_"), "triangle.obj")
    with open(file_name, "w") as writer:
        writer.write("v 0 0 20\nv 10 0 20\nv 0 10 20\nf 1 2 3\n")
    try:
        pmx.import_file(file_name)
    finally:
        os.remove(file_name)
        os.rmdir(os.path.dirname(file_name))
    names = [name for name in pmx.model.parts.get_names() if name not in names_before]
    check(len(names) == 1, "the triangle must be imported as one part: " + str(names))
    part = pmx.model.parts[names[0]]
    check(part.part_type == "Shell", "the triangle must be a shell part, it is " + part.part_type)
    return part.name, list(part.get_element_ids())[0]


def test_sections(material_name):
    sections = pmx.model.sections
    count_before = len(sections)
    element_selection = pmx.create.selection_by_ids('femodel', 'elements', 'elements', [1])
    node_selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])
    shell_part_name, shell_element_id = import_shell_part()
    shell_selection = pmx.create.selection_by_ids('femodel', 'elements', 'elements', [shell_element_id])
    solid_part_name = [name for name in pmx.model.parts.get_names() if name != shell_part_name][0]
    #
    section = sections.add_solid(PREFIX + "Solid", material_name, element_selection)
    check(sections.contains(PREFIX + "Solid"), "the solid section was not added")
    check(section.item_type == "solid", "unexpected section type " + section.item_type)
    check(section.material_name == material_name, "unexpected material name")
    # The controller converts the selection into an internal element set
    check(section.region_type == "ElementSetName",
          "the selection was not converted, region type is " + section.region_type)
    check(section.region_name is not None and len(section.region_name) > 0, "the region name is empty")
    # No dialog lists the internal element set, so it is neither duplicated, converted nor named as a region
    internal_set = section.region_name
    element_set_count = len(pmx.model.element_sets)
    part_count = len(pmx.model.parts)
    section_count = len(sections)
    for function, fragment in [(lambda: pmx.model.element_sets.duplicate([internal_set]), "internal"),
                               (lambda: pmx.model.element_sets.convert_to_mesh_parts([internal_set]), "internal"),
                               (lambda: sections.add_solid(PREFIX + "Bad", material_name,
                                                           pmx.create.region('ElementSetName', internal_set)),
                                "'region'")]:
        message = raised_message(function)
        check(message is not None and fragment in message and internal_set in message,
              "a call on the internal element set must be refused: " + str(message))
    check(len(pmx.model.element_sets) == element_set_count and len(pmx.model.parts) == part_count and
          len(sections) == section_count, "a refused call on an internal element set was recorded")
    #
    sections.add_point_mass(PREFIX + "Mass", node_selection, 0.5)
    check(sections[PREFIX + "Mass"].item_type == "point_mass", "the point mass section was not added")
    check(len(sections) == count_before + 2, "unexpected section count")
    # An unknown material is rejected before anything is recorded
    check_raises(lambda: sections.add_solid(PREFIX + "Bad", "NoSuchMaterial", element_selection),
                 "an unknown material must be rejected")
    check(not sections.contains(PREFIX + "Bad"), "a rejected section must not be recorded")
    # A material named in another case is stored in the model's spelling, because the controller follows a
    # renamed material by comparing the names exactly
    section = sections.add_solid(PREFIX + "Solid_C", material_name.upper(), element_selection)
    check(section.material_name == material_name,
          "the material must be stored in the model's spelling: " + str(section.material_name))
    section = sections.replace_solid(PREFIX + "Solid_C", material_name=material_name.lower())
    check(section.material_name == material_name,
          "a replace must store the material in the model's spelling: " + str(section.material_name))
    sections.remove([PREFIX + "Solid_C"])
    # A material is required on an add, so None is refused rather than recorded as a section without one
    count_before_none = len(sections)
    for function in [lambda: sections.add_solid(PREFIX + "Bad", None, element_selection),
                     lambda: sections.add_shell(PREFIX + "Bad", None, element_selection),
                     lambda: sections.add_membrane(PREFIX + "Bad", None, element_selection)]:
        message = raised_message(function)
        check(message is not None and "'material_name'" in message,
              "a section without a material must be refused naming material_name: " + str(message))
    check(len(sections) == count_before_none, "a section without a material was recorded")
    # A section takes a named region as well as a selection
    pmx.model.element_sets.add(PREFIX + "ES", element_selection)
    section = sections.add_solid(PREFIX + "Solid_R", material_name,
                                 pmx.create.region('ElementSetName', PREFIX + "ES"))
    check(section.region_name == PREFIX + "ES", "unexpected region name " + str(section.region_name))
    check(section.region_type == "ElementSetName", "unexpected region type " + section.region_type)
    check_raises(lambda: sections.add_solid(PREFIX + "Bad", material_name,
                                            pmx.create.region('ElementSetName', 'NoSuchSet')),
                 "an unknown element set must be rejected")
    # A named region of a type the section cannot be defined on is refused, rather than recorded and only
    # flagged invalid later
    pmx.model.node_sets.add(PREFIX + "NS", node_selection)
    message = raised_message(lambda: sections.add_solid(PREFIX + "Bad", material_name,
                                                        pmx.create.region('NodeSetName', PREFIX + "NS")))
    check(message is not None and "ElementSetName" in message and "NodeSetName" in message,
          "a node set region must be refused for a solid section: " + str(message))
    check(not sections.contains(PREFIX + "Bad"), "a section with a refused region must not be recorded")
    pmx.model.node_sets.remove([PREFIX + "NS"])
    # A distributed mass is defined on a surface, as in its dialog, both by selection and by name
    pmx.model.surfaces.add(PREFIX + "SU", pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces',
                                                                      {1: [1]}))
    mass = sections.add_distributed_mass(PREFIX + "DMass_R", pmx.create.region('SurfaceName', PREFIX + "SU"), 0.1)
    check(mass.region_type == "SurfaceName" and mass.region_name == PREFIX + "SU",
          "the distributed mass did not keep its surface region")
    mass = sections.add_distributed_mass(PREFIX + "DMass_S", pmx.create.selection_by_ids('femodel', 'surfaces',
                                                                                        'surfaces', {1: [2]}), 0.1)
    check(mass.region_type == "SurfaceName", "a surface selection was not converted, it is " + mass.region_type)
    sections.remove([PREFIX + "DMass_R", PREFIX + "DMass_S"])
    pmx.model.surfaces.remove([PREFIX + "SU"])
    # The solid section dialog picks parts rather than elements, and the controller turns a part
    # selection into an element set that follows those parts, so an element region takes either target
    part_selection = pmx.create.selection_by_ids('femodel', 'parts', 'parts', [1])
    section = sections.add_solid(PREFIX + "Solid_P", material_name, part_selection)
    check(section.region_type == "ElementSetName",
          "a part selection was not converted, region type is " + section.region_type)
    check_raises(lambda: sections.add_solid(PREFIX + "Bad", material_name,
                                            pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1])),
                 "a node selection must be rejected for an element region")
    # The dialog refuses a section whose type does not suit the elements of its region, in these words, and so
    # does the API, for a selection and for a named region alike
    count_before_refusals = len(sections)
    for function in [lambda: sections.add_shell(PREFIX + "Bad", material_name, element_selection),
                     lambda: sections.add_membrane(PREFIX + "Bad", material_name, element_selection),
                     lambda: sections.add_solid(PREFIX + "Bad", material_name, shell_selection),
                     lambda: sections.add_shell(PREFIX + "Bad", material_name,
                                                pmx.create.region('PartName', solid_part_name)),
                     lambda: sections.add_solid(PREFIX + "Bad", material_name,
                                                pmx.create.region('PartName', shell_part_name)),
                     lambda: sections.add_solid(PREFIX + "Bad", material_name,
                                                pmx.create.selection_by_ids('femodel', 'parts', 'parts',
                                                                            [pmx.model.parts[shell_part_name].id]))]:
        message = raised_message(function)
        check(message is not None and "The section type and the section region are not compatible." in message and
              "'region'" in message, "a section on elements of the other kind must be refused: " + str(message))
    check(len(sections) == count_before_refusals, "a section on an incompatible region was recorded")
    # A shell and a membrane section carry an offset of the mid surface, in thicknesses
    shell = sections.add_shell(PREFIX + "Shell", material_name, shell_selection, thickness=2, offset=0.5)
    check(shell.offset == 0.5, "the shell offset was not stored, it is " + str(shell.offset))
    check(len(shell.get_equations()) == 0, "plain values were read back as equations: " + str(shell.get_equations()))
    membrane = sections.add_membrane(PREFIX + "Membrane", material_name, shell_selection, 1.5, -0.25)
    check(membrane.offset == -0.25, "the membrane offset was not stored")
    check(membrane.mass is None, "a membrane section must have no mass")
    # A replacement keeps an offset it is not given and changes one it is
    shell = sections.replace_shell(PREFIX + "Shell", thickness=3)
    check(shell.offset == 0.5, "the replacement dropped the shell offset")
    check(shell.thickness == 3, "the replacement did not change the thickness")
    # The selection based region is converted again rather than left at the deleted internal set
    check(shell.region_type == "ElementSetName" and pmx.model.element_sets.contains(shell.region_name),
          "the replacement lost the shell region: " + str(shell.region_name))
    check(list(pmx.model.element_sets[shell.region_name].get_element_ids()) == [shell_element_id],
          "the shell region no longer resolves to the selected element")
    # A replace is judged on the section it ends with
    message = raised_message(lambda: sections.replace_shell(PREFIX + "Shell", region=element_selection))
    check(message is not None and "not compatible" in message and "'region'" in message,
          "a replace onto solid elements must be refused: " + str(message))
    check(list(pmx.model.element_sets[sections[PREFIX + "Shell"].region_name].get_element_ids()) ==
          [shell_element_id], "a refused replace changed the shell region")
    shell = sections.replace_shell(PREFIX + "Shell", offset="-0.25 * 2")
    check(shell.offset == -0.5, "the replacement did not change the shell offset")
    check(shell.get_equations()["offset"].replace(" ", "") == "=-0.25*2",
          "the offset expression did not read back: " + str(shell.get_equations()))
    # A thickness that is not positive and a negative mass are refused naming the argument, and a 3D solid
    # section writes no thickness, so one given there is refused
    count_before_refusals = len(sections)
    for function in [lambda: sections.add_solid(PREFIX + "Bad", material_name, element_selection, thickness=2),
                     lambda: sections.add_shell(PREFIX + "Bad", material_name, shell_selection, thickness=0),
                     lambda: sections.add_point_mass(PREFIX + "Bad", node_selection, -0.5)]:
        message = raised_message(function)
        check(message is not None and ("'thickness'" in message or "'mass'" in message),
              "the value must be refused naming the argument: " + str(message))
    check(len(sections) == count_before_refusals, "a refused section was recorded")
    for function, argument in [(lambda: sections.replace_solid(PREFIX + "Solid", thickness=1), "'thickness'"),
                               (lambda: sections.replace_shell(PREFIX + "Shell", thickness=-1), "'thickness'"),
                               (lambda: sections.replace_point_mass(PREFIX + "Mass", mass=-1), "'mass'")]:
        message = raised_message(function)
        check(message is not None and argument in message,
              "a replace with a refused value must name " + argument + ": " + str(message))
    check(sections[PREFIX + "Shell"].thickness == 3, "a refused replace changed the shell")
    # The thickness reads back only where the replace of the type takes it, so it goes straight back into it
    for name in [PREFIX + "Solid", PREFIX + "Mass"]:
        check(sections[name].thickness is None, name + " must read back no thickness: " + str(sections[name].thickness))
        check("thickness" not in sections[name].get_equations(), name + " must return no thickness equation")
    shell = sections.replace_shell(PREFIX + "Shell", thickness=sections[PREFIX + "Shell"].thickness)
    check(shell.thickness == 3, "the shell thickness did not go back into replace_shell")
    # A replace without a name keeps the section under its own spelling, whatever spelling old_name has
    count = len(sections)
    for spelling in [(PREFIX + "Shell").lower(), (PREFIX + "Shell").upper()]:
        shell = sections.replace_shell(spelling, thickness=3)
        check(shell.name == PREFIX + "Shell" and PREFIX + "Shell" in list(sections.get_names()),
              "a replace of '" + spelling + "' changed the stored spelling: " + str(list(sections.get_names())))
    check(len(sections) == count, "a mixed case replace changed the number of sections")
    # A replace never turns one section type into another
    for function, expected in [(lambda: sections.replace_solid(PREFIX + "Mass"),
                                "is made by add_point_mass, so replace_solid cannot replace it"),
                               (lambda: sections.replace_distributed_mass(PREFIX + "Mass", mass=1),
                                "is made by add_point_mass, so replace_distributed_mass cannot replace it"),
                               (lambda: sections.replace_point_mass(PREFIX + "Shell", mass=1),
                                "is made by add_shell, so replace_point_mass cannot replace it")]:
        message = raised_message(function)
        check(message is not None and expected in message,
              "a replace of another section type must be refused: " + str(message))
    check(sections[PREFIX + "Mass"].item_type == "point_mass", "a refused replace changed the section type")
    # A default offset stays zero rather than becoming undefined
    plain = sections.add_shell(PREFIX + "Shell_D", material_name, shell_selection)
    check(plain.offset == 0, "an omitted offset must stay zero")
    # Collection verbs
    sections.rename(PREFIX + "Mass", PREFIX + "Mass_R")
    check(sections.contains(PREFIX + "Mass_R"), "the section was not renamed")
    return shell_part_name


def exported_material_block(material_name):
    export_file = os.path.join(tempfile.gettempdir(), "pytest_composite_materials.inp")
    if os.path.isfile(export_file):
        os.remove(export_file)
    pmx.export_to_calculix(export_file)
    with open(export_file, "r") as reader:
        exported = reader.read()
    os.remove(export_file)
    block = exported.split("*Material, Name=" + material_name + "\n", 1)[1]
    return block.split("*Material, Name=", 1)[0].split("**", 1)[0]


def test_composite_strength():
    materials = pmx.model.materials
    ply = materials.add(PREFIX + "Ply", [pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 3333.333333, 0]]),
                                         pmx.create.composite_strength(2000, 1200, 50, 200, 70, 40)])
    check(list(ply.get_property_types()) == ["elastic_lamina", "composite_strength"],
          "the composite strength property was not stored: " + str(list(ply.get_property_types())))
    # The interaction coefficient defaults to -0.5, and every value reads back under its maker argument
    strength = list(ply.get_properties())[1]
    expected = {"tensile_strength_1": 2000, "compressive_strength_1": 1200, "tensile_strength_2": 50,
                "compressive_strength_2": 200, "shear_strength_12": 70, "shear_strength_23": 40,
                "tsai_wu_coefficient": -0.5}
    check(close(plain_values(strength), expected), "the strength values did not read back: " + str(plain_values(strength)))
    check(close(plain_values(pmx.create.composite_strength(**strength.get_values())), expected),
          "the strength values do not make the same property again")
    materials.replace(PREFIX + "Ply", properties=materials[PREFIX + "Ply"].get_properties())
    check(close(plain_values(list(materials[PREFIX + "Ply"].get_properties())[1]), expected),
          "the strength did not round trip through replace")
    custom = pmx.create.composite_strength(2000, 1200, 50, 200, 70, 40, tsai_wu_coefficient=-0.3)
    check(close(custom.get_value("tsai_wu_coefficient"), -0.3), "a given interaction coefficient was not kept")
    # A value the model refuses is refused by the maker, naming the argument
    for maker, argument in [(lambda: pmx.create.composite_strength(0, 1200, 50, 200, 70, 40), "'tensile_strength_1'"),
                            (lambda: pmx.create.composite_strength(2000, -1, 50, 200, 70, 40),
                             "'compressive_strength_1'"),
                            (lambda: pmx.create.composite_strength(2000, 1200, 50, 200, 70, 0), "'shear_strength_23'"),
                            (lambda: pmx.create.composite_strength(2000, 1200, 50, 200, 70, 40,
                                                                   tsai_wu_coefficient=1), "'tsai_wu_coefficient'"),
                            (lambda: pmx.create.composite_strength(2000, 1200, 50, 200, 70, 40,
                                                                   tsai_wu_coefficient=-1), "'tsai_wu_coefficient'")]:
        message = raised_message(maker)
        check(message is not None and argument in message,
              "a strength the model refuses must be refused naming " + argument + ": " + str(message))
    # The strength has no temperature, so it sits in a temperature dependent material as it is
    materials.add(PREFIX + "Ply_T", [pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 3500, 20],
                                                                [130000, 9000, 0.3, 4500, 4500, 3000, 120]]),
                                     pmx.create.composite_strength(2000, 1200, 50, 200, 70, 40)],
                  temperature_dependent=True)
    # The solver gets the lamina completed as transversely isotropic and no strength keyword
    block = exported_material_block(PREFIX + "Ply")
    keywords = [line.split(",")[0].strip() for line in block.splitlines() if line.startswith("*")]
    check(keywords == ["*Elastic"], "the strength must not reach the solver input: " + str(keywords))
    data = [float(v) for line in block.splitlines()[1:3] for v in line.split(",") if v.strip()]
    e1, e2, e3, v12, v13, v23, g12, g13, g23 = data[:9]
    check(close(e3, e2) and close(v13, v12), "the lamina was not completed with E3 = E2 and v13 = v12: " + str(data))
    check(abs(v23 - (e2 / (2 * g23) - 1)) < 1e-6 and abs(v23 - 0.5) < 1e-6,
          "the lamina v23 must follow from G23 as E2 / (2 G23) - 1: " + str(v23))
    check("Composite" not in exported_material_block(PREFIX + "Ply_T"),
          "the strength of a temperature dependent material must not reach the solver input")
    # A G23 whose completed v23 makes the lamina unstable is refused, naming the argument
    message = raised_message(lambda: pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 2000, 0]]))
    check(message is not None and "'constants'" in message and "v23" in message,
          "a lamina whose completed v23 is unstable must be refused: " + str(message))


def main():
    check(pmx.model.is_open(), "a model must be open")
    material_name = test_materials()
    test_material_properties()
    test_parametric_materials()
    test_composite_strength()
    shell_part_name = test_sections(material_name)
    # A section references a material, so the sections go first
    clean(pmx.model.sections)
    clean(pmx.model.materials)
    pmx.model.parts.remove([shell_part_name])
    print("TEST PASSED")


main()
