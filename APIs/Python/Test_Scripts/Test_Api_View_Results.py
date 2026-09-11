#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#Checks the part of the view API that needs an open result: which field is plotted, which increment,
#how it is plotted, and the picture of it. A result must be open before the script runs, so this
#reads the file Test_Analysis.py leaves in the work directory.
#
#What matters here is that a wrong field or a wrong increment is refused rather than silently
#replaced. The toolbar the API goes through falls back to the default increment for a combination its
#list does not hold, which would show a script a different increment than the one it asked for.
#
#  PrePoMax.com -f PyTest_Job.frd --script Test_Api_View_Results.py -g No -x Yes -w <dir>
#
#Expected process exit code 0.

import os


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message):
    try:
        function()
    except Exception:
        return
    raise Exception("TEST FAILED: " + message)


def test_the_view_is_the_results_view():
    check(pmx.view.current == "results", "a result file opens in the results view: " + pmx.view.current)
    pmx.view.current = "femodel"
    check(pmx.view.current == "femodel", "the view must be settable: " + pmx.view.current)
    pmx.view.current = "results"
    check_raises(lambda: setattr(pmx.view, "current", "nosuchview"), "an unknown view must be rejected")


def test_the_plotted_field():
    result = pmx.results.current
    step_id = list(result.steps.get_ids())[0]
    increment_ids = list(result.steps[step_id].increments.get_ids())
    increment = result.steps[step_id].increments[increment_ids[-1]]
    field_names = list(increment.fields.get_names())
    check("DISP" in field_names, "the solved cube must have a displacement field: " + str(field_names))
    #
    pmx.view.set_result_field("DISP", "U3")
    plotted = list(pmx.view.get_result_field())
    check(plotted[0] == "DISP" and plotted[1] == "U3",
          "the plotted field must be the one that was asked for: " + str(plotted))
    # The component that is not given is the first one of the field
    pmx.view.set_result_field("DISP")
    check(list(pmx.view.get_result_field())[0] == "DISP", "the field must still be DISP")
    # A field or a component the result does not have is refused, and the refusal lists what it has
    check_raises(lambda: pmx.view.set_result_field("NOSUCHFIELD"), "an unknown field must be rejected")
    check_raises(lambda: pmx.view.set_result_field("DISP", "U9"), "an unknown component must be rejected")
    #
    pmx.view.set_result_increment(step_id, increment_ids[-1])
    plotted = list(pmx.view.get_result_field())
    check(int(plotted[2]) == step_id and int(plotted[3]) == increment_ids[-1],
          "the plotted increment must be the one that was asked for: " + str(plotted))
    # An increment the result does not have must be refused, not replaced by the default one
    check_raises(lambda: pmx.view.set_result_increment(step_id, 9999),
                 "an increment the result does not have must be rejected")
    check_raises(lambda: pmx.view.set_result_increment(999, increment_ids[-1]),
                 "a step the result does not have must be rejected")
    plotted = list(pmx.view.get_result_field())
    check(int(plotted[3]) == increment_ids[-1], "a refused increment must leave the plot alone: " + str(plotted))


def test_the_plot_type():
    for name in ["Undeformed", "Deformed", "ColorContours"]:
        pmx.view.result_type = name
        check(pmx.view.result_type == name, "the plot type must be set: " + pmx.view.result_type)
    check_raises(lambda: setattr(pmx.view, "result_type", "Rainbow"), "an unknown plot type must be rejected")


def test_the_picture():
    camera = list(pmx.view.get_parameters())
    pmx.view.set_orientation("Isometric")
    pmx.view.zoom_to_fit()
    path = pmx.view.save_image()
    check(os.path.exists(path), "the image must exist: " + path)
    check(os.path.getsize(path) > 0, "the image must not be empty: " + path)
    os.remove(path)
    # A name of its own, and a name that is not a PNG
    named = os.path.join(os.environ["TEMP"], "PyTest_View.png")
    check(pmx.view.save_image(named) == named, "the image must be written where it was asked for")
    check(os.path.exists(named), "the named image must exist: " + named)
    os.remove(named)
    check_raises(lambda: pmx.view.save_image("picture.jpg"), "only a .png may be written")
    pmx.view.set_parameters(camera)


def main():
    check(pmx.results.current is not None, "a result must be open")
    test_the_view_is_the_results_view()
    test_the_plotted_field()
    test_the_plot_type()
    test_the_picture()
    print("TEST PASSED")


main()
