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
    check(list(interaction.get_property_types()) == ["Friction"],
          "unexpected property types " + str(list(interaction.get_property_types())))
    # A surface interaction is composed of properties, the same way a material is
    interaction = interactions.replace(INTERACTION, INTERACTION,
                                       [pmx.create.friction(0.2, stick_slope=1000),
                                        pmx.create.surface_behavior('Linear', k=2000)])
    check(interaction.property_count == 2, "the properties were not replaced")
    check(sorted(list(interaction.get_property_types())) == ["Friction", "SurfaceBehavior"],
          "unexpected property types " + str(list(interaction.get_property_types())))
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
    check_raises(lambda: step.history_outputs.add_contact(PREFIX + "Bad", "NoSuchPair", "CDIS"),
                 "an unknown contact pair must be rejected")


def main():
    check(pmx.model.is_open(), "a model must be open")
    clean()
    test_surface_interactions()
    test_contact_pairs()
    test_contact_history_output()
    clean()
    print("TEST PASSED")


main()
