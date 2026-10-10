#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the CalculiX keyword editor and the model tree layout as scripted surfaces. A meshed model
#must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Keyword_Tree_Coverage.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.


USER_KEYWORD = "*NO ANALYSIS"
PREFIX = "PyTest_Kw_"
GROUP_NAME = PREFIX + "Group"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def check_refused(function, fragment, message):
    # The refusal must be the API's own, which names what is wrong, not an exception of the tree
    try:
        function()
    except Exception as ex:
        check(fragment in str(ex), message + ": " + str(ex)[0:200])
        return
    raise Exception("TEST FAILED: " + message)


def generated_roots(generated):
    return [entry for entry in generated if len(entry.indices) == 1]


def test_generated_keywords():
    keywords = pmx.model.keywords
    generated = keywords.get_model_keywords()
    check(type(generated) is list, "the generated keywords must be a list: " + type(generated).__name__)
    check(len(generated) > 0, "the model generated no CalculiX keywords")
    check(len(generated[0].indices) == 1, "a top level keyword has a one element index path")
    check(type(generated[0].indices).__name__ == "IntListAPI", "an index path must be an IntList: " +
          type(generated[0].indices).__name__)
    check(len(generated[0].data) > 0, "the generated keyword carries no text")
    return generated


def test_user_keywords(generated):
    keywords = pmx.model.keywords
    if keywords.override_count > 0:
        keywords.clear_overrides()
    for entry in keywords.get_user_keywords():
        keywords.remove_user_keyword(entry.indices)
    check(keywords.count == 0, "the user keywords were not cleared")
    #
    path = list(generated[-1].indices)
    keywords.set_user_keyword(path, USER_KEYWORD)
    check(keywords.count == 1, "the user keyword was not added")
    entries = keywords.get_user_keywords()
    check(type(entries) is list, "the user keywords must be a list: " + type(entries).__name__)
    check(entries[0].data == USER_KEYWORD, "the keyword text did not read back: " + entries[0].data)
    check(list(entries[0].indices) == path, "the index path did not read back")
    # Replacing the whole editor state is the second way in, and it takes index path and text pairs
    keywords.replace_user_keywords([(path, USER_KEYWORD)])
    check(keywords.count == 1, "the whole state replacement lost the keyword")
    keywords.remove_user_keyword(path)
    check(keywords.count == 0, "the user keyword was not removed")
    # An index path is required, and it addresses a place in the generated tree
    check_raises(lambda: keywords.set_user_keyword([], USER_KEYWORD), "an empty index path was accepted")
    check_raises(lambda: keywords.set_user_keyword([0], ""), "empty keyword text was accepted")
    check_raises(lambda: keywords.set_user_keyword([-1], USER_KEYWORD), "a negative index was accepted")
    check_raises(lambda: keywords.remove_user_keyword([0]), "removing a missing user keyword was accepted")
    # An index path names an insertion position, so the position after the last keyword is valid
    keywords.set_user_keyword([len(generated_roots(generated))], USER_KEYWORD)
    keywords.remove_user_keyword([len(generated_roots(generated))])
    check_raises(lambda: keywords.set_user_keyword([len(generated_roots(generated)) + 1], USER_KEYWORD),
                 "an index path past every insertion position was accepted")
    check(keywords.count == 0, "a refused user keyword changed the editor state")


def test_overrides(generated):
    keywords = pmx.model.keywords
    path = list(generated[0].indices)
    keywords.set_override(path, data="** replaced by the Python API")
    overrides = keywords.get_overrides()
    check(type(overrides) is list, "the overrides must be a list: " + type(overrides).__name__)
    check(keywords.override_count == 1, "the override was not recorded")
    check(list(overrides[0].indices) == path, "the override index path did not read back")
    check(overrides[0].data == "** replaced by the Python API", "the override text did not read back")
    check(overrides[0].disabled is False, "the override must not be disabled")
    check(len(overrides[0].original_signature) > 0, "the override carries no generated keyword signature")
    # An omitted value is kept, so disabling the keyword leaves the replacement text in place
    keywords.set_override(path, disabled=True)
    overrides = keywords.get_overrides()
    check(overrides[0].data == "** replaced by the Python API", "the override text was dropped")
    check(overrides[0].disabled, "the override was not disabled")
    #
    keywords.remove_override(path)
    check(keywords.override_count == 0, "the override was not removed")
    check_raises(lambda: keywords.set_override(path), "an override without a change was accepted")
    check_raises(lambda: keywords.set_override([999999], data="**"),
                 "an override of a missing generated keyword was accepted")
    check_raises(lambda: keywords.remove_override(path), "removing a missing override was accepted")
    # Suppression is a stored property that readback must report, and editing one entry must not
    # reset it on another
    second = list(generated[1].indices)
    keywords.set_override(path, data="** first")
    keywords.set_override(second, disabled=True)
    overrides = keywords.get_overrides()
    check(len(overrides) == 2, "both overrides must be kept")
    for item in overrides:
        check(item.suppressed is False, "a live override must not read back as suppressed")
    first = [o for o in overrides if list(o.indices) == path][0]
    check(first.data == "** first", "editing the second override changed the first")
    keywords.clear_overrides()
    check(keywords.override_count == 0, "the overrides were not cleared")
    # Clearing nothing would record a command that changes nothing, so it is refused like removing a missing one
    check_refused(lambda: keywords.clear_overrides(), "no keyword overrides to clear",
                  "clearing an empty set of overrides was accepted")


def test_tree():
    tree = pmx.model.tree
    part_names = list(pmx.model.parts.get_names())
    check(len(part_names) > 0, "a mesh part is required")
    group = tree.add_group('femodel', 'parts', 'PyTest_Group', item_names=[part_names[0]])
    check(len(group) > 0, "add_group returned no group id")
    tree.rename_group('femodel', 'parts', group, 'PyTest_Group_Renamed')
    tree.move_items('femodel', 'parts', [part_names[0]])
    tree.move_group('femodel', 'parts', group)
    tree.remove_groups('femodel', 'parts', group)
    # An item name is found regardless of case and passed on in the model's spelling
    group = tree.add_group('femodel', 'parts', 'PyTest_Case', item_names=[part_names[0].swapcase()])
    groups = [entry for entry in tree.get_layout('femodel', 'parts') if isinstance(entry, dict)]
    check(any(entry['group_id'] == group and entry['items'] == [part_names[0]] for entry in groups),
          "add_group did not move the item in the model's spelling: " + repr(groups))
    tree.move_items('femodel', 'parts', [part_names[0].swapcase()])
    check(part_names[0] in tree.get_layout('femodel', 'parts'),
          "move_items did not move the item in the model's spelling")
    tree.remove_groups('femodel', 'parts', group)
    #
    check_raises(lambda: tree.add_group('NotAView', 'parts', 'PyTest_Bad'), "an unknown view was accepted")
    check_raises(lambda: tree.add_group('femodel', 'widgets', 'PyTest_Bad'),
                 "an unknown item type was accepted")
    check_raises(lambda: tree.add_group('femodel', 'parts', 'PyTest_Bad', group_id='not-a-guid'),
                 "an invalid group id was accepted")
    check_raises(lambda: tree.add_group('femodel', 'parts', '   '), "an empty group name was accepted")


def test_tree_views():
    tree = pmx.model.tree
    # The tree names its views the way the selections and pmx.view do, ignoring case like them
    check(list(tree.get_item_types('FEModel')) == list(tree.get_item_types('femodel')),
          "the view name is compared with its case")
    check('mesh_setup_items' in list(tree.get_item_types('geometry')), "the geometry view is not offered")
    check('surfaces' in list(tree.get_item_types('results')), "the results view is not offered")
    # The old name of the finite element model view is gone rather than kept beside the new one
    for old_name in ('Model', 'fe_model'):
        check_refused(lambda: tree.get_item_types(old_name), "'femodel'",
                      "the old view name '" + old_name + "' was accepted")


def test_tree_existence():
    tree = pmx.model.tree
    part_names = list(pmx.model.parts.get_names())
    missing_id = '00000000-0000-0000-0000-000000000001'
    group = tree.add_group('femodel', 'parts', 'PyTest_Exists', item_names=[part_names[0]])
    second = tree.add_group('femodel', 'parts', 'PyTest_Exists_Second')
    try:
        # A group id, an item name, a step and an index are checked against the tree before anything
        # is recorded, and the refusal names the argument instead of surfacing a dictionary exception
        check_refused(lambda: tree.rename_group('femodel', 'parts', missing_id, 'PyTest_Bad'), "group_id",
                      "renaming an unknown group was accepted")
        check_refused(lambda: tree.remove_groups('femodel', 'parts', [missing_id]), "group_ids",
                      "removing an unknown group was accepted")
        check_refused(lambda: tree.remove_groups('femodel', 'parts', [group, group]), "more than once",
                      "removing a group twice in one call was accepted")
        check_refused(lambda: tree.move_group('femodel', 'parts', missing_id), "group_id",
                      "moving an unknown group was accepted")
        check_refused(lambda: tree.move_group('femodel', 'parts', group, target_group_id=missing_id),
                      "target_group_id", "moving a group into an unknown group was accepted")
        check_refused(lambda: tree.move_group('femodel', 'parts', group, target_group_id=group), "into itself",
                      "moving a group into itself was accepted")
        check_refused(lambda: tree.add_group('femodel', 'parts', 'PyTest_Bad', parent_group_id=missing_id),
                      "parent_group_id", "a group under an unknown parent was accepted")
        check_refused(lambda: tree.move_items('femodel', 'parts', ['__missing_part__']), "item_names",
                      "moving an unknown item was accepted")
        check_refused(lambda: tree.move_items('femodel', 'parts', [part_names[0], part_names[0]]),
                      "more than once", "moving one item twice was accepted")
        check_refused(lambda: tree.move_items('femodel', 'parts', [part_names[0]], target_group_id=missing_id),
                      "target_group_id", "moving items into an unknown group was accepted")
        check_refused(lambda: tree.add_group('femodel', 'parts', 'PyTest_Bad', item_names=['__missing_part__']),
                      "item_names", "a group holding an unknown item was accepted")
        check_refused(lambda: tree.add_group('femodel', 'parts', 'PyTest_Bad', group_id=group), "already exists",
                      "a duplicate group id was accepted")
        check_refused(lambda: tree.add_group('femodel', 'parts', 'PyTest_Exists_second'), "already exists",
                      "a sibling group of the same name was accepted")
        check_refused(lambda: tree.rename_group('femodel', 'parts', second, 'PyTest_Exists'), "already exists",
                      "renaming onto a sibling group name was accepted")
        # A part group shares the names of the parts themselves
        check_refused(lambda: tree.add_group('femodel', 'parts', part_names[0]), "part group",
                      "a part group named as a part was accepted")
        # Its name follows the rules of a part name, and a bad one is refused in the API's own words
        check_refused(lambda: tree.add_group('femodel', 'parts', 'PyTest_Bad#'), "group_name",
                      "a part group name with a character a name cannot hold was accepted")
        check_refused(lambda: tree.add_group('femodel', 'parts', 'PyTest Bad'), "group_name",
                      "a part group name with a space was accepted")
        check_refused(lambda: tree.add_group('geometry', 'parts', 'PyTest_Bad#'), "group_name",
                      "a geometry part group name with a character a name cannot hold was accepted")
        check_refused(lambda: tree.rename_group('femodel', 'parts', second, 'PyTest_Bad#'), "group_name",
                      "renaming a part group to a name with a character a name cannot hold was accepted")
        # The index may name the position after the last entry, but no further
        top_level = len(part_names) - 1 + 2
        check_refused(lambda: tree.add_group('femodel', 'parts', 'PyTest_Bad', target_index=top_level + 1),
                      "target_index", "a group index past the last position was accepted")
        check_refused(lambda: tree.move_items('femodel', 'parts', [part_names[0]], target_group_id=second,
                                              target_index=1),
                      "target_index", "an item index past the end of an empty group was accepted")
        check_refused(lambda: tree.move_group('femodel', 'parts', second, target_index=top_level),
                      "target_index", "a group index past the last position was accepted")
        tree.move_group('femodel', 'parts', second, target_index=top_level - 1)
        # A step owned collection names a step that exists
        check_refused(lambda: tree.add_group('femodel', 'loads', 'PyTest_Bad', step_name='__missing_step__'),
                      "__missing_step__", "an unknown owning step was accepted")
    finally:
        tree.remove_groups('femodel', 'parts', [group, second])


def test_tree_item_types():
    tree = pmx.model.tree
    # Every collection the application can organize has a name here, and each one is exercised
    model_types = list(tree.get_item_types('femodel'))
    for name in ('materials', 'node_sets', 'coordinate_systems', 'jobs', 'loads'):
        check(name in model_types, "the femodel view does not offer " + name)
    check('parts' in list(tree.get_item_types('geometry')), "the geometry view does not offer parts")
    check('surfaces' in list(tree.get_item_types('results')), "the results view does not offer surfaces")
    # The steps are not a groupable collection, and the controller has no layout for them
    check('steps' not in model_types, "steps are offered although they cannot be organized")
    check_raises(lambda: tree.add_group('femodel', 'steps', 'PyTest_Bad'), "steps were accepted")
    #
    # A collection the tree API could not reach before is now organized like any other
    material_name = PREFIX + "Material"
    if material_name not in list(pmx.model.materials.get_names()):
        pmx.model.materials.add(material_name, pmx.create.elastic(210000, 0.3))
    group = tree.add_group('femodel', 'materials', GROUP_NAME, item_names=[material_name])
    tree.move_items('femodel', 'materials', [material_name], target_group_id=group)
    # The collection word is compared regardless of case, as the application compares it
    tree.move_items('femodel', 'Materials', [material_name], target_group_id=group)
    # Every group name follows the rules of a model item name, not only a part group's
    check_refused(lambda: tree.add_group('femodel', 'materials', 'PyTest_Bad#'), "group_name",
                  "a material group name with a character a name cannot hold was accepted")
    check_refused(lambda: tree.rename_group('femodel', 'materials', group, ' ' + GROUP_NAME), "group_name",
                  "a material group name padded with a space was accepted")
    tree.remove_groups('femodel', 'materials', group)
    pmx.model.materials.remove([material_name])
    # A step owned collection is named by its step, and only such a collection takes one
    step_name = PREFIX + "Step"
    if step_name not in list(pmx.model.steps.get_names()):
        pmx.model.steps.add_static(step_name)
    check_raises(lambda: tree.add_group('femodel', 'loads', 'PyTest_Bad'),
                 "a step owned collection without its step was accepted")
    check_raises(lambda: tree.add_group('femodel', 'loads', 'PyTest_Bad', step_name='__missing_step__'),
                 "an unknown owning step was accepted")
    step_group = tree.add_group('femodel', 'loads', GROUP_NAME, step_name=step_name)
    # The owning step is found regardless of case, like every model name
    tree.rename_group('femodel', 'loads', step_group, GROUP_NAME + "_Renamed", step_name=step_name.upper())
    tree.remove_groups('femodel', 'loads', step_group, step_name=step_name.lower())
    pmx.model.steps.remove([step_name])
    check_raises(lambda: tree.add_group('femodel', 'parts', 'PyTest_Bad', step_name=step_name),
                 "a step name on a collection no step owns was accepted")


def find_group(layout, group_id):
    for entry in layout:
        if isinstance(entry, dict):
            if entry['group_id'] == group_id:
                return entry
            found = find_group(entry['items'], group_id)
            if found is not None:
                return found
    return None


# A part group of the geometry and the mesh with one id is one group shown in both views, so set_layout
# renames it in both, as rename_group does, and group names are checked against the names they carry once the
# layout is set, so two groups may swap their names
def test_tree_layout_renames():
    tree = pmx.model.tree
    linked = tree.add_group('geometry', 'parts', PREFIX + 'Linked')
    tree.add_group('femodel', 'parts', PREFIX + 'Linked', group_id=linked)
    geometry_only = tree.add_group('geometry', 'parts', PREFIX + 'Geometry_Only')
    first = tree.add_group('femodel', 'parts', PREFIX + 'First')
    second = tree.add_group('femodel', 'parts', PREFIX + 'Second')
    try:
        layout = tree.get_layout('femodel', 'parts')
        find_group(layout, linked)['group_name'] = PREFIX + 'Linked_Renamed'
        find_group(layout, first)['group_name'] = PREFIX + 'Second'
        find_group(layout, second)['group_name'] = PREFIX + 'First'
        tree.set_layout('femodel', 'parts', layout)
        layout = tree.get_layout('femodel', 'parts')
        check(find_group(layout, first)['group_name'] == PREFIX + 'Second' and
              find_group(layout, second)['group_name'] == PREFIX + 'First', "the two groups did not swap their names")
        check(find_group(tree.get_layout('geometry', 'parts'), linked)['group_name'] == PREFIX + 'Linked_Renamed',
              "the linked group was not renamed in the geometry view")
        # A name a group of the other view keeps is refused, and nothing is recorded
        find_group(layout, first)['group_name'] = PREFIX + 'Geometry_Only'
        find_group(layout, linked)['group_name'] = PREFIX + 'Linked_Refused'
        check_refused(lambda: tree.set_layout('femodel', 'parts', layout), "already the name",
                      "a group name the geometry view keeps must be refused")
        check(find_group(tree.get_layout('geometry', 'parts'), linked)['group_name'] == PREFIX + 'Linked_Renamed',
              "the refused layout renamed the linked group in the geometry view")
        check(find_group(tree.get_layout('femodel', 'parts'), first)['group_name'] == PREFIX + 'Second',
              "the refused layout was recorded")
        # The empty id is no group id
        empty = '00000000-0000-0000-0000-000000000000'
        check_refused(lambda: tree.add_group('femodel', 'parts', PREFIX + 'Empty', group_id=empty), "'group_id'",
                      "the empty group id must be refused naming group_id")
        check_refused(lambda: tree.rename_group('femodel', 'parts', empty, PREFIX + 'Empty'), "'group_id'",
                      "the empty group id must be refused naming group_id")
    finally:
        tree.remove_groups('femodel', 'parts', [linked, first, second])
        tree.remove_groups('geometry', 'parts', [linked, geometry_only])


def main():
    check(pmx.model.is_open(), "a model must be open")
    generated = test_generated_keywords()
    test_user_keywords(generated)
    test_overrides(generated)
    test_tree()
    test_tree_views()
    test_tree_existence()
    test_tree_item_types()
    test_tree_layout_renames()
    print("TEST PASSED")


main()
