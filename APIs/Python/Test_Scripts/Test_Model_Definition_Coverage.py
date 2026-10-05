#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the typed model definition replacements and the model properties. A meshed model must be
#open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Definition_Coverage.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_Def_"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def magnitude(equation):
    # A stored value carries its unit, exactly the way the property grid shows it
    return float(equation.split(" ")[0])


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def clean():
    # The parameter goes last, because an equation that still names it cannot be evaluated without it
    for collection in [pmx.model.initial_conditions, pmx.model.sections, pmx.model.materials,
                       pmx.model.node_sets, pmx.model.parameters]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def test_section_replacement():
    part_selection = pmx.create.selection_by_ids('femodel', 'elements', 'parts',
                                                 [pmx.model.parts.first().id])
    pmx.model.materials.add(PREFIX + "Steel", pmx.create.elastic(210000, 0.3))
    pmx.model.materials.add(PREFIX + "Alu", pmx.create.elastic(70000, 0.33))
    pmx.model.sections.add_solid(PREFIX + "Solid", PREFIX + "Steel", part_selection)
    element_count = pmx.model.element_sets[pmx.model.sections[PREFIX + "Solid"].region_name].count
    # A replacement changes only what it is told, so the region survives a material change
    pmx.model.sections.replace_solid(PREFIX + "Solid", material_name=PREFIX + "Alu")
    section = pmx.model.sections[PREFIX + "Solid"]
    check(section.material_name == PREFIX + "Alu", "the material was not replaced")
    check(section.region_type != "Selection", "the converted region was lost")
    # The replacement deletes the internal set of the old section, so the kept selection must be
    # converted again rather than left pointing at the deleted set
    check(section.valid, "the section region is no longer valid after the replacement")
    check(pmx.model.element_sets.contains(section.region_name),
          "the section points at an element set that no longer exists: " + str(section.region_name))
    check(pmx.model.element_sets[section.region_name].count == element_count,
          "the section region no longer resolves to the same elements")
    # A replacement of the wrong type is refused rather than silently converting the section
    check_raises(lambda: pmx.model.sections.replace_shell(PREFIX + "Solid", thickness=2.0),
                 "a solid section was replaced as a shell section")
    check_raises(lambda: pmx.model.sections.replace_solid("__missing_section__"),
                 "a missing section was accepted")


def test_initial_condition_replacement():
    pmx.model.node_sets.add(PREFIX + "Nodes",
                            pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4]))
    region = pmx.create.region('NodeSetName', PREFIX + "Nodes")
    pmx.model.initial_conditions.add_temperature(PREFIX + "Temp", region, 20)
    pmx.model.initial_conditions.replace_temperature(PREFIX + "Temp", temperature=30)
    equations = pmx.model.initial_conditions[PREFIX + "Temp"].get_equations()
    check(magnitude(equations["temperature"]) == 30, "the temperature was not replaced")
    #
    pmx.model.initial_conditions.add_translational_velocity(PREFIX + "Vel", region, 1, 2, 3)
    pmx.model.initial_conditions.replace_translational_velocity(PREFIX + "Vel", v2=20)
    equations = pmx.model.initial_conditions[PREFIX + "Vel"].get_equations()
    check(magnitude(equations["v1"]) == 1 and magnitude(equations["v2"]) == 20 and
          magnitude(equations["v3"]) == 3,
          "the omitted velocity components were not kept: " + str(equations))
    #
    pmx.model.initial_conditions.add_angular_velocity(PREFIX + "Ang", region, [0, 0, 0], [0, 0, 1], 5)
    pmx.model.initial_conditions.replace_angular_velocity(PREFIX + "Ang", rotational_speed=7)
    equations = pmx.model.initial_conditions[PREFIX + "Ang"].get_equations()
    check(magnitude(equations["rotational_speed"]) == 7, "the rotational speed was not replaced")
    check(magnitude(equations["n3"]) == 1, "the omitted axis direction was not kept: " + str(equations))


def test_model_properties():
    properties = pmx.model.properties
    original_type = properties.model_type
    properties.replace(model_type="Submodel", global_results_file_name="global.frd")
    check(properties.model_type == "Submodel", "the model type was not replaced")
    check(properties.global_results_file_name == "global.frd", "the submodel file name was not replaced")
    #
    properties.replace(model_type="SlipWearModel", number_of_cycles=10, cycles_increment=2,
                       num_of_smoothing_steps=3, bdm_remeshing=True, slip_wear_results="All")
    check(magnitude(properties.number_of_cycles) == 10, "the cycle count did not read back: " +
          str(properties.number_of_cycles))
    check(magnitude(properties.cycles_increment) == 2, "the cycle increment did not read back")
    check(properties.bdm_remeshing, "BDM remeshing was not set")
    check(properties.slip_wear_results == "All", "the slip wear results were not set")
    # An undefined physical constant reads back as None, and only clear_undefined_constants clears one
    properties.replace(absolute_zero=-273.15, stefan_boltzmann=5.67e-11)
    check(properties.absolute_zero == -273.15, "absolute zero was not stored")
    properties.replace(bdm_remeshing=False)
    check(properties.absolute_zero == -273.15, "absolute zero was cleared by an unrelated replacement")
    properties.replace(clear_undefined_constants=True)
    check(properties.absolute_zero is None, "absolute zero was not cleared")
    check(properties.stefan_boltzmann is None, "the Stefan Boltzmann constant was not cleared")
    #
    check_raises(lambda: properties.replace(model_type="NotAType"), "an unknown model type was accepted")
    check_raises(lambda: properties.replace(absolute_zero=float('nan')), "a NaN constant was accepted")
    properties.replace(model_type=original_type)
    check(properties.model_type == original_type, "the model type was not restored")


def test_creation_equations():
    # A creation carries the expression a replacement can, so the model parameter survives both ways
    if not pmx.model.parameters.contains(PREFIX + "length"):
        pmx.model.parameters.add(PREFIX + "length", 0.2)
    part_selection = pmx.create.selection_by_ids('femodel', 'elements', 'parts',
                                                 [pmx.model.parts.first().id])
    region = pmx.create.region('NodeSetName', PREFIX + "Nodes")
    #
    pmx.model.sections.add_shell(PREFIX + "Shell", PREFIX + "Steel", part_selection,
                                 thickness=PREFIX + "length")
    equations = pmx.model.sections[PREFIX + "Shell"].get_equations()
    check(equations["thickness"] == "=" + PREFIX + "length",
          "the shell thickness expression was not stored: " + str(equations))
    #
    pmx.model.sections.add_point_mass(PREFIX + "Mass", region, PREFIX + "length")
    check(pmx.model.sections[PREFIX + "Mass"].get_equations()["mass"] == "=" + PREFIX + "length",
          "the point mass expression was not stored")
    #
    pmx.model.initial_conditions.add_temperature(PREFIX + "TempEq", region, PREFIX + "length")
    check(pmx.model.initial_conditions[PREFIX + "TempEq"].get_equations()["temperature"] ==
          "=" + PREFIX + "length", "the initial temperature expression was not stored")
    #
    pmx.model.initial_conditions.add_translational_velocity(PREFIX + "VelEq", region,
                                                            PREFIX + "length", 2, 3)
    check(pmx.model.initial_conditions[PREFIX + "VelEq"].get_equations()["v1"] ==
          "=" + PREFIX + "length", "the initial velocity expression was not stored")
    #
    pmx.model.initial_conditions.add_angular_velocity(PREFIX + "AngEq", region,
                                                      [PREFIX + "length", 0, 0], [0, 0, 1],
                                                      PREFIX + "length")
    equations = pmx.model.initial_conditions[PREFIX + "AngEq"].get_equations()
    check(equations["x"] == "=" + PREFIX + "length", "the axis point expression was not stored")
    check(equations["rotational_speed"] == "=" + PREFIX + "length",
          "the rotational speed expression was not stored")


def test_export_validation():
    before = pmx.model.properties.model_type
    check_raises(lambda: pmx.export_mesh(["__missing_part__"], "missing.stl"),
                 "an export of a missing part was accepted")
    check(pmx.model.properties.model_type == before, "the refused export changed the model")
    # Each export names the parts of its own source, and this model has neither CAD nor a result
    part_name = pmx.model.parts.first().name
    check_raises(lambda: pmx.export_geometry([part_name], "missing.step"),
                 "a geometry export of a mesh part was accepted")
    check_raises(lambda: pmx.export_results([part_name], "missing.inp"),
                 "a results export without a result was accepted")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    test_section_replacement()
    test_initial_condition_replacement()
    test_model_properties()
    test_creation_equations()
    test_export_validation()
    clean()
    print("TEST PASSED")


main()
