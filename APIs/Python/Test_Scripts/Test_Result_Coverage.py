#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the result editing commands: the part verbs, the result field output duplication and the
#result lifecycle. A result must be open before the script runs.
#
#  PrePoMax.com -f PyTest_Job.frd --script Test_Result_Coverage.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def test_part_commands():
    parts = pmx.results.current.mesh.parts
    names = parts.get_names().to_array()
    check(len(names) > 0, "the result mesh has no parts")
    name = names[0]
    #
    parts.hide(name)
    parts.show(name)
    parts.set_color(name, [10, 20, 30])
    parts.reset_color(name)
    parts.set_transparency(name, 128)
    parts.set_representation(name, False)
    parts.set_representation(name, True)
    parts.replace_properties(name)
    check(parts.contains(name), "the part disappeared after the property replacement")
    # A name that does not exist is refused before the command is recorded
    check_raises(lambda: parts.hide("__missing_part__"), "a missing part name was accepted")
    check_raises(lambda: parts.set_transparency(name, 300), "an out of range transparency was accepted")
    check_raises(lambda: parts.set_transparency(name, 12.5), "a fractional transparency was accepted")


def test_field_output_duplication():
    field_outputs = pmx.results.current.field_outputs
    before = list(field_outputs.get_names())
    if before:
        field_outputs.duplicate(before[0])
        after = list(field_outputs.get_names())
        check(len(after) == len(before) + 1, "the duplicated field output was not added")
        for name in after:
            if name not in before:
                field_outputs.remove(name)
    check_raises(lambda: field_outputs.duplicate("__missing_field_output__"),
                 "a missing field output name was accepted")


def test_result_lifecycle():
    names = list(pmx.results.get_names())
    check(len(names) > 0, "no result is loaded")
    pmx.results.remove_current()
    check(len(list(pmx.results.get_names())) == len(names) - 1, "the current result was not removed")
    pmx.results.clear()
    check(len(list(pmx.results.get_names())) == 0, "the results were not cleared")


def main():
    check(len(pmx.results.get_names().to_array()) > 0, "no result is loaded")
    test_part_commands()
    test_field_output_duplication()
    test_result_lifecycle()
    print("TEST PASSED")


main()
