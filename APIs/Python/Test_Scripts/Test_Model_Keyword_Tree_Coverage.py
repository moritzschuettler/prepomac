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


def generated_roots(generated):
    return [entry for entry in generated if len(entry.indices) == 1]


def test_generated_keywords():
    keywords = pmx.model.keywords
    generated = keywords.get_model_keywords()
    check(len(generated) > 0, "the model generated no CalculiX keywords")
    check(len(generated[0].indices) == 1, "a top level keyword has a one element index path")
    check(len(generated[0].data) > 0, "the generated keyword carries no text")
    return generated


def test_user_keywords(generated):
    keywords = pmx.model.keywords
    keywords.clear_overrides()
    for entry in keywords.get_user_keywords():
        keywords.remove_user_keyword(entry.indices)
    check(keywords.count == 0, "the user keywords were not cleared")
    #
    path = list(generated[-1].indices)
    keywords.set_user_keyword(path, USER_KEYWORD)
    check(keywords.count == 1, "the user keyword was not added")
    entries = keywords.get_user_keywords()
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


def test_tree():
    tree = pmx.model.tree
    part_names = list(pmx.model.parts.get_names())
    check(len(part_names) > 0, "a mesh part is required")
    group = tree.create_group('Model', 'parts', 'PyTest_Group', item_names=[part_names[0]])
    check(len(group) > 0, "create_group returned no group id")
    tree.rename_group('Model', 'parts', group, 'PyTest_Group_Renamed')
    tree.move_items('Model', 'parts', [part_names[0]])
    tree.move_group('Model', 'parts', group)
    tree.remove_groups('Model', 'parts', group)
    #
    check_raises(lambda: tree.create_group('NotAView', 'parts', 'PyTest_Bad'), "an unknown view was accepted")
    check_raises(lambda: tree.create_group('Model', 'widgets', 'PyTest_Bad'),
                 "an unknown item type was accepted")
    check_raises(lambda: tree.create_group('Model', 'parts', 'PyTest_Bad', group_id='not-a-guid'),
                 "an invalid group id was accepted")
    check_raises(lambda: tree.create_group('Model', 'parts', '   '), "an empty group name was accepted")


def test_tree_item_types():
    tree = pmx.model.tree
    # Every collection the application can organize has a name here, and each one is exercised
    model_types = list(tree.get_item_types('Model'))
    for name in ('materials', 'node_sets', 'coordinate_systems', 'jobs', 'loads'):
        check(name in model_types, "the Model view does not offer " + name)
    check('parts' in list(tree.get_item_types('Geometry')), "the Geometry view does not offer parts")
    check('surfaces' in list(tree.get_item_types('Results')), "the Results view does not offer surfaces")
    # The steps are not a groupable collection, and the controller has no layout for them
    check('steps' not in model_types, "steps are offered although they cannot be organized")
    check_raises(lambda: tree.create_group('Model', 'steps', 'PyTest_Bad'), "steps were accepted")
    #
    # A collection the tree API could not reach before is now organized like any other
    material_name = PREFIX + "Material"
    if material_name not in list(pmx.model.materials.get_names()):
        pmx.model.materials.add(material_name, pmx.create.elastic(210000, 0.3))
    group = tree.create_group('Model', 'materials', GROUP_NAME, item_names=[material_name])
    tree.move_items('Model', 'materials', [material_name], target_group_id=group)
    tree.remove_groups('Model', 'materials', group)
    pmx.model.materials.remove([material_name])
    # A step owned collection is named by its step, and only such a collection takes one
    step_name = PREFIX + "Step"
    if step_name not in list(pmx.model.steps.get_names()):
        pmx.model.steps.add_static(step_name)
    check_raises(lambda: tree.create_group('Model', 'loads', 'PyTest_Bad'),
                 "a step owned collection without its step was accepted")
    check_raises(lambda: tree.create_group('Model', 'loads', 'PyTest_Bad', step_name='__missing_step__'),
                 "an unknown owning step was accepted")
    step_group = tree.create_group('Model', 'loads', GROUP_NAME, step_name=step_name)
    tree.remove_groups('Model', 'loads', step_group, step_name=step_name)
    pmx.model.steps.remove([step_name])
    check_raises(lambda: tree.create_group('Model', 'parts', 'PyTest_Bad', step_name=step_name),
                 "a step name on a collection no step owns was accepted")


def main():
    check(pmx.model.is_open(), "a model must be open")
    generated = test_generated_keywords()
    test_user_keywords(generated)
    test_overrides(generated)
    test_tree()
    test_tree_item_types()
    print("TEST PASSED")


main()
