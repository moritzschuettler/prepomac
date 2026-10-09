#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the model collection foundations: the model root, the part makers in pmx.create, and the
#node set collection verbs. A meshed model must be open before the script runs.
#
#  PrePoMax.com -f cube.inp -u MM_TON_S_C --script Test_Model_Node_Sets.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

TEST_NAME = "PyTest_Node_Set"
RENAMED_NAME = "PyTest_Node_Set_Renamed"


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception as ex:
        check("does not exist" in str(ex), message + ": " + str(ex)[0:200])
        return
    raise Exception("TEST FAILED: " + message)


def main():
    check(pmx.model.is_open(), "a model must be open")
    # The selection is a part, so it is created before the item that consumes it
    selection = pmx.create.selection_by_ids('femodel', 'nodes', 'nodes', [1, 2, 3, 4])
    check(selection.count == 4, "the selection must contain 4 source IDs")
    #
    for name in [TEST_NAME, RENAMED_NAME]:
        if pmx.model.node_sets.contains(name):
            pmx.model.node_sets.remove([name])
    #
    count_before = len(pmx.model.node_sets)
    # Add
    node_set = pmx.model.node_sets.add(TEST_NAME, selection)
    check(pmx.model.node_sets.contains(TEST_NAME), "the node set was not added")
    check(len(pmx.model.node_sets) == count_before + 1, "the node set count did not increase")
    check(node_set.count == 4, "4 nodes expected, found " + str(node_set.count))
    check(list(node_set.get_node_ids()) == [1, 2, 3, 4], "unexpected node IDs")
    check(not node_set.is_internal, "a node set added by name must not be internal")
    # Collection verbs from the generic base
    check(pmx.model.node_sets[TEST_NAME].count == 4, "indexing by name failed")
    check(pmx.model.node_sets.get(TEST_NAME).count == 4, "get failed")
    check(TEST_NAME in list(pmx.model.node_sets.get_names()), "get_names does not list the node set")
    # The in operator and contains find what get finds: regardless of case, and a padded name is no name
    check(TEST_NAME in pmx.model.node_sets, "the in operator did not find the node set")
    check(TEST_NAME.upper() in pmx.model.node_sets and pmx.model.node_sets.contains(TEST_NAME.lower()),
          "the name was not compared regardless of case")
    check("NoSuchNodeSet" not in pmx.model.node_sets, "the in operator found a missing node set")
    check(not pmx.model.node_sets.contains(" " + TEST_NAME + " "),
          "contains accepted a padded name that get does not find")
    # Every other verb treats a padded name the same way: it names no item
    for padded_call in [lambda: pmx.model.node_sets.get(" " + TEST_NAME),
                        lambda: pmx.model.node_sets.rename(TEST_NAME + " ", RENAMED_NAME),
                        lambda: pmx.model.node_sets.remove([" " + TEST_NAME]),
                        lambda: pmx.model.node_sets.duplicate([TEST_NAME + " "])]:
        check_raises(padded_call, "a padded name must name no node set")
    check(pmx.model.node_sets.contains(TEST_NAME) and not pmx.model.node_sets.contains(RENAMED_NAME),
          "a call with a padded name changed the node sets")
    check(len(pmx.model.node_sets) == count_before + 1, "a call with a padded name was recorded")
    # The item is found in any case, and a rename to the spelling it already has changes nothing
    pmx.model.node_sets.rename(TEST_NAME.upper(), TEST_NAME)
    check(pmx.model.node_sets[TEST_NAME].name == TEST_NAME, "a rename to the stored spelling changed the name")
    # Rename
    pmx.model.node_sets.rename(TEST_NAME, RENAMED_NAME)
    check(pmx.model.node_sets.contains(RENAMED_NAME), "the node set was not renamed")
    check(not pmx.model.node_sets.contains(TEST_NAME), "the old node set name still exists")
    # Duplicate
    pmx.model.node_sets.duplicate([RENAMED_NAME])
    check(len(pmx.model.node_sets) == count_before + 2, "the node set was not duplicated")
    # Remove
    remove_names = [name for name in pmx.model.node_sets.get_names() if name.startswith(TEST_NAME)]
    pmx.model.node_sets.remove(remove_names)
    check(len(pmx.model.node_sets) == count_before, "the node sets were not removed")
    #
    print("TEST PASSED")


main()
