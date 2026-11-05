#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks contact_pairs.search, which runs Search Contact Pairs of the Interaction menu from a script, and add() of
#what it found. The unit cube is copied twice: onto its top face, where the two cubes touch, and below it, 0.05
#away. A search distance of 0.02 finds the touching faces only, one of 0.08 both.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Interactions_Contact_Search.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

PREFIX = "PyTest_"
INTERACTION = PREFIX + "SI"
TOUCHING = 0.02
REACHING = 0.08


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_refused(function, fragment, message):
    # Only the API's own refusal passes, naming what was wrong and carrying no .NET stack trace
    try:
        function()
    except Exception as ex:
        kind, text = type(ex).__name__, str(ex)
        if kind != "PythonAPIException" or "   at " in text or fragment not in text:
            raise Exception("TEST FAILED: " + message + " - expected a PythonAPIException naming " + fragment +
                            ", not " + kind + ": " + text[0:500])
        return
    raise Exception("TEST FAILED: " + message)


def model_counts():
    return len(pmx.model.constraints), len(pmx.model.contact_pairs), len(pmx.model.surfaces)


def build_assembly():
    parts = pmx.model.parts
    base = list(parts.get_names())[0]
    before = set(parts.get_names())
    parts.translate(base, [0, 0, 1], number_of_copies=1)
    above = [name for name in parts.get_names() if name not in before]
    before = set(parts.get_names())
    parts.translate(base, [0, 0, -1.05], number_of_copies=1)
    below = [name for name in parts.get_names() if name not in before]
    check(len(above) == 1 and len(below) == 1, "the two copies of the cube were not made")
    check(len(parts) == 3, "the base cube must stay where it is")
    return base, above[0], below[0]


def test_refusals():
    search = pmx.model.contact_pairs.search
    pmx.model.surface_interactions.add(INTERACTION, pmx.create.surface_behavior('Hard'))
    counts = model_counts()
    check_refused(lambda: search(0), "'distance'", "a distance of 0 must be refused")
    check_refused(lambda: search(-1), "'distance'", "a negative distance must be refused")
    check_refused(lambda: search(float('nan')), "'distance'", "a NaN distance must be refused")
    check_refused(lambda: search(None), "'distance'", "the distance is required")
    check_refused(lambda: search(TOUCHING, angle_deg=181), "'angle_deg'", "an angle above 180 must be refused")
    check_refused(lambda: search(TOUCHING, angle_deg=-1), "'angle_deg'", "a negative angle must be refused")
    check_refused(lambda: search(TOUCHING, pair_type='Glue'), "'pair_type'", "an unknown pair type must be refused")
    check_refused(lambda: search(TOUCHING, group_by='BySurfaceAngle'), "'group_by'",
                  "the grouping the search does not support must be refused")
    check_refused(lambda: search(TOUCHING, solid=False, shell=False, shell_edges=False), "'solid'",
                  "a search of no faces must be refused")
    check_refused(lambda: search(TOUCHING, solid='yes'), "'solid'", "a filter must be True or False")
    ties = search(TOUCHING)
    check_refused(lambda: ties.add(surface_interaction_name=INTERACTION), "'surface_interaction_name'",
                  "a tie must refuse a surface interaction rather than drop it")
    check_refused(lambda: ties.add(method='NodeToSurface'), "'method'",
                  "a tie must refuse a contact method rather than drop it")
    check_refused(lambda: ties.add(adjust='yes'), "'adjust'", "adjust must be True or False")
    contacts = search(TOUCHING, pair_type='Contact')
    check_refused(lambda: contacts.add(), "'surface_interaction_name'", "a contact pair needs a surface interaction")
    check_refused(lambda: contacts.add(surface_interaction_name=PREFIX + "Missing"), PREFIX + "Missing",
                  "an unknown surface interaction must be refused")
    check_refused(lambda: contacts.add(surface_interaction_name=INTERACTION, method='Mortar'), "Mortar",
                  "a method the dialog does not offer must be refused")
    check_refused(lambda: contacts.add(surface_interaction_name=INTERACTION, method='Glue'), "'method'",
                  "an unknown method must be refused")
    check(model_counts() == counts, "a refused search or add changed the model")
    pmx.model.surface_interactions.remove([INTERACTION])


def test_tie_search(below):
    search = pmx.model.contact_pairs.search
    counts = model_counts()
    touching = search(TOUCHING)
    check(touching.pair_type == 'Tie', "the default pair type must be Tie: " + touching.pair_type)
    check(len(touching) == 1 and touching.count == 1,
          "only the touching faces lie within " + str(TOUCHING) + ": " + str(len(touching)))
    check(repr(touching).startswith("ContactSearchResult("), "unexpected repr " + repr(touching))
    check(len(search(REACHING)) == 2, "both faces lie within " + str(REACHING))
    check(model_counts() == counts, "a search must not change the model")
    # A found pair reads back what add adds, and its regions are selections a script can add itself
    pair = touching[0]
    check(touching[-1].name == pair.name, "a negative index must count from the end")
    check(pair.name == [p.name for p in touching][0], "iteration must give the pairs")
    check(pair.geometry_type == 'Solid-Solid', "the cubes touch by solid faces: " + pair.geometry_type)
    check(not pair.unresolved, "the sides of two touching cubes must be resolved")
    check(pair.master_region.target == 'surfaces' and pair.master_region.count == 1,
          "the master side must be one face: " + repr(pair.master_region))
    check(pair.slave_region is not None and pair.slave_region.count == 1, "the slave side must be one face")
    check(pair.master_region.preview().count == 1, "the master face must resolve to one element face")
    # The hidden part is left out unless ignore_hidden is False
    pmx.model.parts.hide(below)
    check(len(search(REACHING)) == 1, "a hidden part must be left out by default")
    check(len(search(REACHING, ignore_hidden=False)) == 2, "ignore_hidden=False must search a hidden part")
    pmx.model.parts.show(below)
    # The cubes have no shell faces, so a search of shells finds nothing and adding nothing adds nothing
    shells = search(REACHING, solid=False)
    check(len(shells) == 0, "a solid model has no shell faces to find")
    check(list(shells.add()) == [] and model_counts() == counts, "adding no pairs must add nothing")
    check(len(search(REACHING, group_by='None')) == 2, "one pair for every two touching faces")
    check(len(search(REACHING, group_by='ByContactGraph')) >= 1, "the contact graph must find the pairs")
    # add adds the ties as OK of the dialog does, with the search distance as the position tolerance
    names = list(touching.add())
    check(len(names) == 1, "one tie must be added: " + str(names))
    check(pmx.model.constraints.contains(names[0]), "the tie " + names[0] + " is not in the model")
    tie = pmx.model.constraints[names[0]]
    check(tie.item_type == 'tie', "a tie must be added: " + tie.item_type)
    check(tie.valid, "the added tie must be valid")
    check(abs(tie.get_value("position_tolerance") - TOUCHING) < 1e-12,
          "the position tolerance must be the search distance: " + str(tie.get_value("position_tolerance")))
    check(not tie.get_value("adjust"), "adjust must be off unless given")
    check(len(pmx.model.constraints) == counts[0] + 1, "only the tie must be added")
    # Adding again adds the pairs again, numbered
    again = list(touching.add(adjust=True))
    check(len(again) == 1 and again[0] != names[0], "the second tie must be numbered: " + str(again))
    check(pmx.model.constraints[again[0]].get_value("adjust"), "adjust=True was not applied")
    pmx.model.constraints.remove(names + again)
    check(model_counts() == counts, "removing the added ties must restore the model")
    # The regions of a found pair add a tie of the script's own
    tie = pmx.model.constraints.add_tie(PREFIX + "Tie", pair.master_region, pair.slave_region)
    check(tie.valid, "a tie added from the regions of a found pair must be valid")
    pmx.model.constraints.remove([PREFIX + "Tie"])


def test_found_pair_ids(base, above):
    # The geometry IDs and part names of a found pair are what a script writes to a file and adds from later
    pair = pmx.model.contact_pairs.search(TOUCHING)[0]
    master_ids = list(pair.master_geometry_ids)
    slave_ids = list(pair.slave_geometry_ids)
    check(len(master_ids) == 1 and len(slave_ids) == 1, "each side must be one face: " + str((master_ids, slave_ids)))
    check(all((i // 10000) % 10 == 3 for i in master_ids + slave_ids),
          "a solid face must carry the solid surface type digit 3: " + str(master_ids + slave_ids))
    master_parts = list(pair.master_part_names)
    slave_parts = list(pair.slave_part_names)
    check(len(master_parts) == 1 and len(slave_parts) == 1, "each side must be on one part")
    check(sorted(master_parts + slave_parts) == sorted([base, above]),
          "the touching faces lie on " + base + " and " + above + ": " + str(master_parts + slave_parts))
    check(len(pmx.model.contact_pairs.search(REACHING, group_by='ByParts')) == 2, "the ByParts search must find two")
    # The geometry source takes the IDs back and resolves them to the same element faces as the found regions
    create = pmx.create.selection_by_ids
    master = create('femodel', 'surfaces', 'geometry', master_ids)
    slave = create('femodel', 'surfaces', 'geometry', slave_ids)
    check(list(master.get_ids()) == list(pair.master_region.get_ids()),
          "the master IDs must resolve to the faces of the found master region")
    check(list(slave.get_ids()) == list(pair.slave_region.get_ids()),
          "the slave IDs must resolve to the faces of the found slave region")
    tie = pmx.model.constraints.add_tie(PREFIX + "TieFromIds", master, slave)
    check(tie.valid, "a tie added from the geometry IDs of a found pair must be valid")
    pmx.model.constraints.remove([PREFIX + "TieFromIds"])


def test_contact_search():
    pmx.model.surface_interactions.add(INTERACTION, pmx.create.surface_behavior('Hard'))
    counts = model_counts()
    found = pmx.model.contact_pairs.search(REACHING, pair_type='Contact')
    check(found.pair_type == 'Contact' and len(found) == 2, "both contacts must be found: " + str(len(found)))
    names = list(found.add(surface_interaction_name=INTERACTION.lower(), method='NodeToSurface', adjust=True))
    check(len(names) == 2, "two contact pairs must be added: " + str(names))
    for name in names:
        pair = pmx.model.contact_pairs[name]
        check(pair.surface_interaction_name == INTERACTION,
              "the surface interaction must be stored in the model's spelling: " + pair.surface_interaction_name)
        check(pair.method == 'NodeToSurface', "the method was not applied: " + pair.method)
        check(pair.adjust, "adjust=True was not applied")
        check(abs(pair.adjustment_size - REACHING) < 1e-12,
              "the adjustment size must be the search distance: " + str(pair.adjustment_size))
        check(pair.valid, "the added contact pair must be valid")
    pmx.model.contact_pairs.remove(names)
    # The method and adjust default to those of the dialog
    names = list(found.add(surface_interaction_name=INTERACTION))
    for name in names:
        pair = pmx.model.contact_pairs[name]
        check(pair.method == 'SurfaceToSurface', "the default method must be SurfaceToSurface: " + pair.method)
        check(not pair.adjust, "adjust must be off unless given")
    pmx.model.contact_pairs.remove(names)
    check(model_counts() == counts, "removing the added contact pairs must restore the model")
    pmx.model.surface_interactions.remove([INTERACTION])


def main():
    check(pmx.model.is_open(), "a model must be open")
    base, above, below = build_assembly()
    test_refusals()
    test_tie_search(below)
    test_found_pair_ids(base, above)
    test_contact_search()
    print("TEST PASSED")


main()
