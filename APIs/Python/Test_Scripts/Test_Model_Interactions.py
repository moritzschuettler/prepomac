#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the surface interactions and the contact pairs, including the master and slave regions and
#the contact history output that names a contact pair. A meshed model must be open before the run.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Interactions.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"
INTERACTION = PREFIX + "SI"
PAIR = PREFIX + "CP"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def raised_message(function):
    """The message of the exception the function raises, or None when it raises none."""
    try:
        function()
    except Exception as ex:
        return str(ex)
    return None


def clean():
    for collection in [pmx.model.steps, pmx.model.contact_pairs, pmx.model.surface_interactions,
                       pmx.model.surfaces]:
        names = [n for n in collection.get_names() if n.startswith(PREFIX)]
        if names:
            collection.remove(names)


def surface_selection(face):
    return pmx.create.selection_by_ids('femodel', 'surfaces', 'surfaces', {1: [face]})


def test_surface_interactions():
    interactions = pmx.model.surface_interactions
    count_before = len(interactions)
    # A single property does not have to be wrapped in a list
    interaction = interactions.add(INTERACTION, pmx.create.friction(0.1))
    check(len(interactions) == count_before + 1, "the surface interaction was not added")
    check(interaction.property_count == 1, "unexpected property count")
    check(list(interaction.get_property_types()) == ["friction"],
          "unexpected property types " + str(list(interaction.get_property_types())))
    # A surface interaction is composed of properties, the same way a material is
    interaction = interactions.replace(INTERACTION, INTERACTION,
                                       [pmx.create.friction(0.2, stick_slope=1000),
                                        pmx.create.surface_behavior('Linear', k=2000)])
    check(interaction.property_count == 2, "the properties were not replaced")
    check(sorted(list(interaction.get_property_types())) == ["friction", "surface_behavior"],
          "unexpected property types " + str(list(interaction.get_property_types())))
    # A value that is not given is kept: a rename alone keeps the properties, and properties alone keep the name
    interaction = interactions.replace(INTERACTION, INTERACTION + "_K")
    check(interaction.name == INTERACTION + "_K", "the surface interaction was not renamed by the replace")
    check(interaction.property_count == 2, "a rename alone must keep the properties")
    interaction = interactions.replace(INTERACTION + "_K", properties=pmx.create.friction(0.3))
    check(interaction.name == INTERACTION + "_K", "a replace of the properties alone must keep the name")
    check(list(interaction.get_property_types()) == ["friction"], "the properties were not replaced")
    interaction = interactions.replace(INTERACTION + "_K", INTERACTION,
                                       [pmx.create.friction(0.2, stick_slope=1000),
                                        pmx.create.surface_behavior('Linear', k=2000)])
    # The model keys its collections without regard to case, and so does contains
    check(interactions.contains(INTERACTION.lower()), "contains must not depend on the case of the name")
    check(not interactions.contains(INTERACTION + "_Missing"), "contains found a missing surface interaction")
    # A name the model could not take is refused before anything is recorded, naming the argument
    count_before_refusals = len(interactions)
    for bad_name in [PREFIX + "Bad Name", PREFIX + "Bad#", " " + PREFIX + "Spaced", "123"]:
        message = raised_message(lambda: interactions.add(bad_name, pmx.create.friction(0.1)))
        check(message is not None and "'name'" in message,
              "the name " + repr(bad_name) + " must be refused naming the argument: " + str(message))
    check(len(interactions) == count_before_refusals, "a refused add was recorded")
    message = raised_message(lambda: interactions.replace(INTERACTION, PREFIX + "Bad#"))
    check(message is not None and "'name'" in message, "a replace to a bad name must be refused: " + str(message))
    message = raised_message(lambda: interactions.rename(INTERACTION, PREFIX + "Bad Name"))
    check(message is not None and "'new_name'" in message,
          "a rename to a bad name must be refused naming new_name: " + str(message))
    check(interactions.contains(INTERACTION) and interactions[INTERACTION].property_count == 2,
          "a refused replace or rename changed the surface interaction")
    # The gap conductance is either constant or tabular, never both and never neither
    check_raises(lambda: pmx.create.gap_conductance(),
                 "a gap conductance without a value must be rejected")
    check_raises(lambda: pmx.create.gap_conductance(conductance=5, conductance_table=[[5, 0, 0]]),
                 "a gap conductance with both forms must be rejected")
    interactions.add(PREFIX + "SI_Gap", pmx.create.gap_conductance(conductance=5))
    interactions.add(PREFIX + "SI_Gap_T",
                     pmx.create.gap_conductance(conductance_table=[[5, 0, 20], [4, 10, 20]]))
    check_raises(lambda: pmx.create.gap_conductance(conductance_table=[[5, 0]]),
                 "a gap conductance row must contain three values")
    check_raises(lambda: pmx.create.surface_behavior('NotAType'),
                 "an unknown pressure overclosure type must be rejected")
    check_raises(lambda: interactions.add(PREFIX + "Bad", "not a property"),
                 "a property given as a string must be rejected")
    check_raises(lambda: interactions.add(INTERACTION, pmx.create.friction(0.1)),
                 "a duplicate name must be rejected")
    # The dialog adds a property type once, so a repeated one is refused naming the argument
    count_before_repeated = len(interactions)
    message = raised_message(lambda: interactions.add(PREFIX + "Bad", [pmx.create.friction(0.1),
                                                                       pmx.create.friction(0.3)]))
    check(message is not None and "'properties'" in message and "friction" in message,
          "a repeated surface interaction property type must be refused: " + str(message))
    message = raised_message(lambda: interactions.replace(INTERACTION, properties=[pmx.create.friction(0.1),
                                                                                   pmx.create.friction(0.3)]))
    check(message is not None and "'properties'" in message,
          "a replace with a repeated property type must be refused: " + str(message))
    check(len(interactions) == count_before_repeated, "a refused surface interaction was recorded")
    # Collection verbs
    interactions.rename(INTERACTION, INTERACTION + "_R")
    check(interactions.contains(INTERACTION + "_R"), "the surface interaction was not renamed")
    interactions.rename(INTERACTION + "_R", INTERACTION)
    # The names are diffed rather than filtered by prefix, because PyTest_SI_Gap also starts with it
    before = set(interactions.get_names())
    interactions.duplicate([INTERACTION])
    duplicated = [n for n in interactions.get_names() if n not in before]
    check(len(duplicated) > 0, "the surface interaction was not duplicated")
    interactions.remove(duplicated)
    interactions.remove([PREFIX + "SI_Gap", PREFIX + "SI_Gap_T"])


def plain_values(prop):
    """The values of a property with every table turned into a list of row lists, so that they compare."""
    values = {}
    for key, value in prop.get_values().items():
        if value is not None and not isinstance(value, (str, int, float)):
            value = [list(row) for row in value]
        values[key] = value
    return values


def test_surface_interaction_properties():
    interactions = pmx.model.surface_interactions
    name = PREFIX + "SI_Props"
    interactions.add(name, [pmx.create.friction(0.2, stick_slope=1000),
                            pmx.create.surface_behavior('Linear', k=2000),
                            pmx.create.gap_conductance(conductance_table=[[5, 0, 20], [4, 10, 20]])])
    properties = list(interactions[name].get_properties())
    check([p.property_type for p in properties] == ["friction", "surface_behavior", "gap_conductance"],
          "unexpected property types " + str([p.property_type for p in properties]))
    # Every value reads back under the pmx.create argument that set it
    friction = plain_values(properties[0])
    check(friction["coefficient"] == 0.2 and friction["stick_slope"] == 1000,
          "the friction values did not read back: " + str(friction))
    check(properties[0].get_value("coefficient") == 0.2, "get_value did not read the coefficient")
    check("coefficient=0.2" in repr(properties[0]), "the property does not print its values: " + repr(properties[0]))
    behavior = plain_values(properties[1])
    check(behavior["pressure_overclosure_type"] == "Linear" and behavior["k"] == 2000,
          "the surface behavior values did not read back: " + str(behavior))
    check(behavior["c0"] is None, "a default c0 must read back as None: " + str(behavior["c0"]))
    check(plain_values(properties[2]) == {"conductance_table": [[5, 0, 20], [4, 10, 20]]},
          "the gap conductance table did not read back: " + str(plain_values(properties[2])))
    check(plain_values(pmx.create.friction(0.1)) == {"coefficient": 0.1, "stick_slope": None},
          "an omitted stick slope must read back as None")
    check(plain_values(pmx.create.gap_conductance(conductance=5)) == {"conductance": 5},
          "a constant gap conductance must read back as conductance")
    check_raises(lambda: properties[0].get_value("no_such_value"), "an unknown value name was accepted")
    # A surface behavior reports only the values its type writes, and refuses the others
    check(plain_values(pmx.create.surface_behavior('Hard')) == {"pressure_overclosure_type": "Hard"},
          "a hard surface behavior must report no values: " + str(plain_values(pmx.create.surface_behavior('Hard'))))
    check(sorted(plain_values(pmx.create.surface_behavior('Exponential')).keys()) ==
          ["c0", "p0", "pressure_overclosure_type"], "an exponential surface behavior must report c0 and p0")
    check(sorted(plain_values(pmx.create.surface_behavior('Tied', k=500)).keys()) ==
          ["k", "pressure_overclosure_type"], "a tied surface behavior must report k only")
    for maker, argument in [(lambda: pmx.create.surface_behavior('Hard', k=10), "'k'"),
                            (lambda: pmx.create.surface_behavior('Exponential', s_inf=1), "'s_inf'"),
                            (lambda: pmx.create.surface_behavior('Linear', p0=1), "'p0'"),
                            (lambda: pmx.create.surface_behavior('Tied', c0=1), "'c0'"),
                            (lambda: pmx.create.surface_behavior('Linear', pressure_overclosure=[[0, 0], [1, 1]]),
                             "'pressure_overclosure'"),
                            (lambda: pmx.create.surface_behavior('Linear', k=-1), "'k'"),
                            (lambda: pmx.create.surface_behavior('Exponential', c0=0), "'c0'"),
                            (lambda: pmx.create.friction(-0.1), "'coefficient'"),
                            (lambda: pmx.create.friction(0.1, stick_slope=0), "'stick_slope'")]:
        message = raised_message(maker)
        check(message is not None and argument in message,
              "a value the property does not take must be refused naming " + argument + ": " + str(message))
    # The values are the keyword arguments of the maker, and the properties go straight back into a replace
    for prop in properties:
        maker = getattr(pmx.create, prop.property_type)
        check(plain_values(maker(**prop.get_values())) == plain_values(prop),
              "the " + prop.property_type + " values do not make the same property again")
    before = [plain_values(p) for p in properties]
    interactions.replace(name, properties=interactions[name].get_properties())
    after = [plain_values(p) for p in interactions[name].get_properties()]
    check(after == before, "the properties did not round trip through replace: " + str(after))
    # A read back property is a copy, so changing the surface interaction does not change it
    interactions.replace(name, properties=pmx.create.friction(0.5))
    check(plain_values(properties[0])["coefficient"] == 0.2, "a read back property followed the model")
    interactions.remove([name])


def test_contact_pairs():
    pairs = pmx.model.contact_pairs
    check(len(pairs) == 0, "the model has no contact pairs yet")
    pair = pairs.add(PAIR, INTERACTION, surface_selection(1), surface_selection(2))
    check(pair.surface_interaction_name == INTERACTION,
          "unexpected surface interaction " + pair.surface_interaction_name)
    # A selection based region is converted into an internal surface
    check(pair.master_region_type == "SurfaceName",
          "unexpected master region type " + pair.master_region_type)
    check(pair.slave_region_type == "SurfaceName",
          "unexpected slave region type " + pair.slave_region_type)
    check(pair.master_region_name != pair.slave_region_name, "the two sides must be different surfaces")
    check(pair.method == "SurfaceToSurface", "unexpected default method " + pair.method)
    check(not pair.small_sliding, "small sliding is off by default")
    check(pair.adjustment_size is None, "no adjustment size is given by default")
    check(pair.active and pair.visible, "a new contact pair must be active and visible")
    # The surface interaction name is checked
    check_raises(lambda: pairs.add(PREFIX + "Bad", "NoSuchInteraction", surface_selection(1),
                                   surface_selection(2)),
                 "an unknown surface interaction must be rejected")
    check_raises(lambda: pairs.add(PREFIX + "Bad", INTERACTION, surface_selection(1),
                                   surface_selection(2), method="NotAMethod"),
                 "an unknown contact method must be rejected")
    # The model clears small sliding for a surface to surface pair, so the API rejects the pair of
    # values instead of letting the request be silently dropped
    check_raises(lambda: pairs.replace(PAIR, small_sliding=True),
                 "small sliding with the surface to surface method must be rejected")
    # A value that is not given survives the replace
    master_before = pair.master_region_name
    pair = pairs.replace(PAIR, method="NodeToSurface", small_sliding=True)
    check(pair.small_sliding, "small sliding was not replaced")
    check(pair.method == "NodeToSurface", "the method was not replaced")
    check(pair.master_region_name == master_before, "the master region must survive a replace")
    check(pair.surface_interaction_name == INTERACTION, "the surface interaction must survive a replace")
    # An adjustment size implies the adjustment
    pair = pairs.replace(PAIR, adjustment_size=0.01)
    check(pair.adjust, "an adjustment size must turn the adjustment on")
    check(abs(pair.adjustment_size - 0.01) < 1e-12, "the adjustment size was not set")
    # The model clears small sliding when the method becomes surface to surface, so that change has to
    # say so explicitly
    message = raised_message(lambda: pairs.replace(PAIR, method="SurfaceToSurface"))
    check(message is not None and "small_sliding=False" in message,
          "a surface to surface method that drops small sliding unasked must be refused: " + str(message))
    pair = pmx.model.contact_pairs[PAIR]
    check(pair.method == "NodeToSurface" and pair.small_sliding, "the refused replace changed the contact pair")
    pair = pairs.replace(PAIR, method="SurfaceToSurface", small_sliding=False)
    check(pair.method == "SurfaceToSurface", "the method was not replaced")
    check(not pair.small_sliding, "small sliding was not turned off")
    pair = pairs.replace(PAIR, method="NodeToSurface", small_sliding=True)
    # Only NodeToSurface writes small sliding, so every other method refuses it rather than dropping it
    count_before = len(pairs)
    for method in ["SurfaceToSurface", "Mortar", "Massless"]:
        message = raised_message(lambda: pairs.add(PREFIX + "CP_Bad", INTERACTION, surface_selection(3),
                                                   surface_selection(4), method=method, small_sliding=True))
        check(message is not None and "small sliding" in message,
              "small sliding with the " + method + " method must be refused: " + str(message))
    check(len(pairs) == count_before, "a refused contact pair was recorded")
    message = raised_message(lambda: pairs.replace(PAIR, method="Mortar"))
    check(message is not None and "small_sliding=False" in message,
          "a Mortar method that drops small sliding unasked must be refused: " + str(message))
    pair = pairs.replace(PAIR, method="Massless", small_sliding=False)
    check(pair.method == "Massless" and not pair.small_sliding, "the Massless method was not replaced")
    # An adjustment size must not be negative, and it contradicts adjust=False
    message = raised_message(lambda: pairs.replace(PAIR, adjustment_size=-0.1))
    check(message is not None and "'adjustment_size'" in message,
          "a negative adjustment size must be refused naming the argument: " + str(message))
    message = raised_message(lambda: pairs.replace(PAIR, adjust=False, adjustment_size=0.1))
    check(message is not None and "'adjustment_size'" in message,
          "an adjustment size with adjust=False must be refused: " + str(message))
    check(pmx.model.contact_pairs[PAIR].method == "Massless", "a refused replace changed the contact pair")
    pair = pairs.replace(PAIR, method="NodeToSurface", small_sliding=True)
    # Swap and the collection verbs
    master_before = pair.master_region_name
    slave_before = pair.slave_region_name
    pairs.swap_master_slave([PAIR])
    check(pmx.model.contact_pairs[PAIR].master_region_name == slave_before, "the sides were not swapped")
    check(pmx.model.contact_pairs[PAIR].slave_region_name == master_before, "the sides were not swapped")
    pairs.swap_master_slave([PAIR])
    #
    pairs.deactivate([PAIR])
    check(not pmx.model.contact_pairs[PAIR].active, "the contact pair was not deactivated")
    pairs.activate([PAIR])
    pairs.hide([PAIR])
    check(not pmx.model.contact_pairs[PAIR].visible, "the contact pair was not hidden")
    pairs.show([PAIR])
    pairs.rename(PAIR, PAIR + "_R")
    check(pairs.contains(PAIR + "_R"), "the contact pair was not renamed")
    pairs.rename(PAIR + "_R", PAIR)


def test_contact_history_output():
    # This is what the contact history output of 1.8 could not be tested against before
    step = pmx.model.steps.add_static(PREFIX + "Static", time_period=1.0)
    output = step.history_outputs.add_contact(PREFIX + "HC", PAIR, ["CDIS", "CSTR"])
    check(output.region_type == "ContactPair", "unexpected region type " + output.region_type)
    check(output.region_name == PAIR, "unexpected region name " + str(output.region_name))
    check(sorted(list(output.get_variables())) == ["CDIS", "CSTR"], "unexpected contact variables")
    # The contact pair reads back under the argument that sets it
    check(output.get_value("contact_pair_name") == PAIR,
          "unexpected contact_pair_name " + str(output.get_value("contact_pair_name")))
    check("contact_pair_name" in list(output.get_value_names()), "contact_pair_name must be listed")
    output = step.history_outputs.replace_contact(PREFIX + "HC",
                                                  contact_pair_name=output.get_value("contact_pair_name"),
                                                  variables=output.get_value("variables"))
    check(sorted(list(output.get_variables())) == ["CDIS", "CSTR"], "the read back values did not go back")
    check_raises(lambda: step.history_outputs.add_contact(PREFIX + "Bad", "NoSuchPair", "CDIS"),
                 "an unknown contact pair must be rejected")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    test_surface_interactions()
    test_surface_interaction_properties()
    test_contact_pairs()
    test_contact_history_output()
    clean()
    print("TEST PASSED")


main()
