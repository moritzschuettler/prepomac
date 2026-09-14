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


def test_the_deformation():
    for name in ["Undeformed", "TrueScale", "Automatic", "Automatic2", "UserDefined"]:
        pmx.view.deformation_type = name
        check(pmx.view.deformation_type == name, "the deformation type must be set: " + pmx.view.deformation_type)
    pmx.view.deformation_factor = 2.5
    check(abs(float(pmx.view.deformation_factor) - 2.5) < 1e-6,
          "the deformation factor must be set: " + str(pmx.view.deformation_factor))
    check_raises(lambda: setattr(pmx.view, "deformation_type", "Huge"), "an unknown deformation type must be rejected")
    check_raises(lambda: setattr(pmx.view, "deformation_factor", "big"), "a deformation factor must be a number")
    pmx.view.deformation_type = "Automatic"
    # The undeformed model is drawn behind a contour plot only; a plot that would discard it refuses it
    pmx.view.result_type = "ColorContours"
    for name in ["WireframeBody", "SolidBody", "None"]:
        pmx.view.undeformed_model = name
        check(pmx.view.undeformed_model == name, "the undeformed model must be set: " + pmx.view.undeformed_model)
    check_raises(lambda: setattr(pmx.view, "undeformed_model", "Ghost"), "an unknown undeformed model must be rejected")
    pmx.view.result_type = "Deformed"
    check_raises(lambda: setattr(pmx.view, "undeformed_model", "SolidBody"),
                 "a Deformed plot has no undeformed copy, so asking for one must be rejected")
    check(pmx.view.undeformed_model == "None", "a refused undeformed model must leave the setting alone")
    pmx.view.result_type = "ColorContours"


def test_the_legend_range():
    pmx.view.set_result_field("DISP", "U3")
    pmx.view.set_legend_range(-1.0, 1.0)
    check_raises(lambda: pmx.view.set_legend_range(1.0, 1.0), "an empty legend range must be rejected")
    check_raises(lambda: pmx.view.set_legend_range(2.0, 1.0), "an inverted legend range must be rejected")
    check_raises(lambda: pmx.view.set_legend_range("low", 1.0), "a legend range must be numbers")
    pmx.view.reset_legend_range()
    # The range is stored under the plotted field of the results view, so another view cannot hold it
    pmx.view.current = "femodel"
    check_raises(lambda: pmx.view.set_legend_range(0.0, 1.0), "the legend range needs the results view")
    check_raises(lambda: pmx.view.reset_legend_range(), "resetting the legend range needs the results view")
    pmx.view.current = "results"


def unit_direction(camera):
    direction = [camera[i] - camera[3 + i] for i in range(3)]
    length = sum(d * d for d in direction) ** 0.5
    return [d / length for d in direction]


def centre_offset(camera, target):
    """How far the target is from the centre of the picture, across the viewing direction.

    The application keeps the camera at the depth of the model after every camera change, which moves
    the focal point along the viewing direction and changes nothing on screen, so a point looked at is
    checked in the picture rather than against the focal point itself.
    """
    direction = unit_direction(camera)
    offset = [camera[3 + i] - target[i] for i in range(3)]
    along = sum(offset[i] * direction[i] for i in range(3))
    return sum((offset[i] - along * direction[i]) ** 2 for i in range(3)) ** 0.5


def test_the_camera():
    camera = list(pmx.view.get_parameters())
    pmx.view.set_orientation("Front")
    before = list(pmx.view.get_parameters())
    pmx.view.rotate(30, 20)
    after = list(pmx.view.get_parameters())
    check(any(abs(after[i] - before[i]) > 1e-6 for i in range(3)), "a rotation must move the camera: " + str(after))
    pmx.view.rotate(-30, -20)
    reference = list(pmx.view.get_parameters())
    # Looking at a point keeps the viewing direction and the view up, and puts the point in the centre
    target = [0.5, 0.5, 1.0]
    pmx.view.look_at(target, 2.0)
    framed = list(pmx.view.get_parameters())
    check(centre_offset(framed, target) < 1e-6, "look_at must put the point in the centre: " + str(framed))
    check(abs(framed[9] - 2.0) < 1e-6, "look_at must set the parallel scale: " + str(framed))
    direction = unit_direction(framed)
    previous = unit_direction(reference)
    for i in range(3):
        check(abs(direction[i] - previous[i]) < 1e-6, "look_at must keep the viewing direction: " + str(framed))
        check(abs(framed[6 + i] - reference[6 + i]) < 1e-6, "look_at must keep the view up: " + str(framed))
    pmx.view.look_at([0.0, 0.0, 0.0])
    check(abs(list(pmx.view.get_parameters())[9] - 2.0) < 1e-6, "a scale that is not given keeps the zoom")
    check_raises(lambda: pmx.view.look_at([1.0, 2.0]), "a short point must be rejected")
    check_raises(lambda: pmx.view.look_at([0.0, 0.0, 0.0], -1.0), "a negative parallel scale must be rejected")
    check_raises(lambda: pmx.view.rotate("a lot"), "a rotation must be a number")
    pmx.view.set_parameters(camera)


def test_the_annotations():
    annotations = pmx.view.annotations
    names_before = sorted(annotations.get_names())
    annotations.add_text("PyTest_Label", "Top of the cube", [0.5, 0.5, 1.0])
    node_name = annotations.add_node(7)
    element_name = annotations.add_element(1)
    names = list(annotations.get_names())
    check("PyTest_Label" in names and node_name in names and element_name in names,
          "the added annotations must be listed: " + str(names))
    check(node_name != element_name, "the generated names must differ: " + node_name)
    check_raises(lambda: annotations.add_text("PyTest_Label", "again", [0.0, 0.0, 0.0]), "a taken name must be rejected")
    check_raises(lambda: annotations.add_text("Min_Annotation", "mine", [0.0, 0.0, 0.0]), "a reserved name must be rejected")
    check_raises(lambda: annotations.add_text("PyTest_Short", "short", [0.0, 0.0]), "a short anchor must be rejected")
    check_raises(lambda: annotations.add_node(999), "an unknown node must be rejected")
    check_raises(lambda: annotations.add_element(999), "an unknown element must be rejected")
    check_raises(lambda: annotations.remove("PyTest_NoSuchAnnotation"), "an unknown annotation must be rejected")
    check_raises(lambda: annotations.remove("Max_Annotation"), "a reserved annotation cannot be removed")
    # The min/max labels are a setting of the application, so they are put back
    shown = list(annotations.get_min_max())
    check(len(shown) == 2, "the min/max setting is two values: " + str(shown))
    annotations.show_min_max(True, True)
    check(list(annotations.get_min_max()) == [True, True], "the min/max labels must be shown")
    annotations.show_min_max(False, False)
    check(list(annotations.get_min_max()) == [False, False], "the min/max labels must be hidden")
    annotations.show_min_max(shown[0], shown[1])
    check(list(annotations.get_min_max()) == shown, "the min/max setting must be put back: " + str(shown))
    #
    path = pmx.view.save_image()
    check(os.path.exists(path), "the annotated image must exist: " + path)
    os.remove(path)
    #
    annotations.remove(["PyTest_Label", node_name, element_name])
    check(sorted(annotations.get_names()) == names_before,
          "the removed annotations must be gone: " + str(list(annotations.get_names())))


def main():
    check(pmx.results.current is not None, "a result must be open")
    test_the_view_is_the_results_view()
    test_the_plotted_field()
    test_the_plot_type()
    test_the_deformation()
    test_the_legend_range()
    test_the_camera()
    test_the_annotations()
    test_the_picture()
    print("TEST PASSED")


main()
