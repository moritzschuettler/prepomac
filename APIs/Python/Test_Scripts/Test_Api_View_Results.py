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
import shutil
import tempfile

# A name no file may hold: "|" is valid outside Windows, the null character is valid nowhere
BAD_FILE_NAME = "bad|name.png" if os.name == "nt" else "bad\0name.png"

# The pictures saved under names of the test go into a folder of their own, which main removes whether the
# test passes or not
TEMPORARY_FOLDER = tempfile.mkdtemp(prefix="PyTest_View_")


def check(condition, message):
    if not condition:
        raise Exception("TEST FAILED: " + message)


def check_raises(function, message, unknown_keyword=None):
    # Only the API's own refusal passes: a PythonAPIException, or for a call that deliberately passes the
    # keyword unknown_keyword, the TypeError of the keyword guard. Anything else, and a message carrying a
    # .NET stack trace, is a fault of the application that happened to stop the call
    try:
        function()
    except Exception as ex:
        kind, text = type(ex).__name__, str(ex)
        if unknown_keyword is None:
            expected, refusal = kind == "PythonAPIException", "a PythonAPIException"
        else:
            expected = kind == "TypeError" and "unexpected keyword argument '%s'" % unknown_keyword in text
            refusal = "the keyword guard's TypeError for '%s'" % unknown_keyword
        if not expected or "   at " in text:
            raise Exception("TEST FAILED: " + message + " - the refusal must be " + refusal +
                            " without a .NET stack trace, not " + kind + ": " + text[0:500])
        return
    raise Exception("TEST FAILED: " + message)


def check_refused(function, words, message):
    try:
        function()
    except Exception as ex:
        text = str(ex)
        for word in words:
            check(word in text, message + " - the refusal must say '" + word + "': " + text)
        check("   at " not in text, message + " - a .NET exception leaked: " + text)
        return
    raise Exception("TEST FAILED: " + message)


def test_the_view_is_the_results_view():
    check(pmx.view.current == "results", "a result file opens in the results view: " + pmx.view.current)
    pmx.view.current = "femodel"
    check(pmx.view.current == "femodel", "the view must be settable: " + pmx.view.current)
    pmx.view.current = "results"
    check_raises(lambda: setattr(pmx.view, "current", "nosuchview"), "an unknown view must be rejected")
    # A value that is not a string is refused naming the property, rather than with a TypeError of pythonnet
    for name in ["current", "result_type", "deformation_type", "undeformed_model"]:
        try:
            setattr(pmx.view, name, 5)
            message = None
        except Exception as ex:
            message = str(ex)
        check(message is not None and "'" + name + "'" in message and "must be a string" in message,
              "a number assigned to pmx.view." + name + " must be refused naming it: " + str(message))
    check(pmx.view.current == "results", "a refused assignment changed the view")


def test_the_plotted_field():
    result = pmx.results.current
    step_id = list(result.steps.get_ids())[0]
    increment_ids = list(result.steps[step_id].increments.get_ids())
    increment = result.steps[step_id].increments[increment_ids[-1]]
    field_names = list(increment.fields.get_names())
    check("DISP" in field_names, "the solved cube must have a displacement field: " + str(field_names))
    #
    pmx.view.set_result_field("DISP", "U3")
    plotted = pmx.view.get_result_field()
    # The plotted field reads back under the names of the arguments that set it, with the IDs as integers
    check(isinstance(plotted, dict), "the plotted field must be a dict: " + str(type(plotted)))
    check(sorted(plotted.keys()) == ["component_name", "field_name", "increment_id", "step_id"],
          "the plotted field must be keyed by the argument names: " + str(plotted))
    check(plotted["field_name"] == "DISP" and plotted["component_name"] == "U3",
          "the plotted field must be the one that was asked for: " + str(plotted))
    check(type(plotted["step_id"]) is int and type(plotted["increment_id"]) is int,
          "the plotted step and increment must be integers: " + str(plotted))
    # The component that is not given is the first one of the field
    pmx.view.set_result_field("DISP")
    check(pmx.view.get_result_field()["field_name"] == "DISP", "the field must still be DISP")
    # A field or a component the result does not have is refused, and the refusal lists what it has
    check_raises(lambda: pmx.view.set_result_field("NOSUCHFIELD"), "an unknown field must be rejected")
    check_raises(lambda: pmx.view.set_result_field("DISP", "U9"), "an unknown component must be rejected")
    #
    pmx.view.set_result_increment(step_id, increment_ids[-1])
    plotted = pmx.view.get_result_field()
    check(plotted["step_id"] == step_id and plotted["increment_id"] == increment_ids[-1],
          "the plotted increment must be the one that was asked for: " + str(plotted))
    # What it reads back sets the same plot again
    pmx.view.set_result_field(plotted["field_name"], plotted["component_name"])
    pmx.view.set_result_increment(plotted["step_id"], plotted["increment_id"])
    check(pmx.view.get_result_field() == plotted, "the plotted field must round trip: " + str(plotted))
    # An increment the result does not have must be refused, not replaced by the default one
    check_raises(lambda: pmx.view.set_result_increment(step_id, 9999),
                 "an increment the result does not have must be rejected")
    check_raises(lambda: pmx.view.set_result_increment(999, increment_ids[-1]),
                 "a step the result does not have must be rejected")
    plotted = pmx.view.get_result_field()
    check(plotted["increment_id"] == increment_ids[-1],
          "a refused increment must leave the plot alone: " + str(plotted))


def test_a_redraw_keeps_the_plotted_field():
    # A redraw plots the component selected in the results tree, so a field set by the API must be selected
    # there too; DISP U3 is not the component the tree selects when the result opens
    pmx.view.set_result_field("DISP", "U3")
    plotted = pmx.view.get_result_field()
    shown = pmx.view.annotations.get_min_max()
    deformation_type = pmx.view.deformation_type
    deformation_factor = pmx.view.deformation_factor
    redraws = [("show_min_max", lambda: pmx.view.annotations.show_min_max(not shown[0], shown[1])),
               ("set_legend_range", lambda: pmx.view.set_legend_range(-1.0, 1.0)),
               ("reset_legend_range", lambda: pmx.view.reset_legend_range()),
               ("deformation_factor", lambda: setattr(pmx.view, "deformation_factor", 3.0))]
    try:
        for name, redraw in redraws:
            redraw()
            check(pmx.view.get_result_field() == plotted,
                  name + " must keep the plotted field " + str(plotted) + ": " + str(pmx.view.get_result_field()))
    finally:
        pmx.view.annotations.show_min_max(shown[0], shown[1])
        pmx.view.reset_legend_range()
        pmx.view.deformation_type = deformation_type
        pmx.view.deformation_factor = deformation_factor


def test_the_plot_type():
    for name in ["Undeformed", "Deformed", "ColorContours"]:
        pmx.view.result_type = name
        check(pmx.view.result_type == name, "the plot type must be set: " + pmx.view.result_type)
    check_raises(lambda: setattr(pmx.view, "result_type", "Rainbow"), "an unknown plot type must be rejected")
    # An option string is matched regardless of case and reads back in the spelling of its enumeration
    pmx.view.result_type = "colorcontours"
    check(pmx.view.result_type == "ColorContours", "the plot type must be taken regardless of case: " +
          pmx.view.result_type)
    # The plotted component picks a vector or a tensor plot, so the other one is refused rather than replaced
    pmx.view.set_result_field("DISP", "U3")
    pmx.view.result_type = "VectorPlot"
    check(pmx.view.result_type == "VectorPlot", "a displacement must be drawn as a vector plot: " +
          pmx.view.result_type)
    check_refused(lambda: setattr(pmx.view, "result_type", "TensorPlot"), ["'result_type'", "VectorPlot"],
                  "a tensor plot of a displacement was accepted")
    check(pmx.view.result_type == "VectorPlot", "a refused plot type changed the plot: " + pmx.view.result_type)
    # The toolbar changes the plot type only in the results view, which another view must not pretend to
    pmx.view.result_type = "ColorContours"
    pmx.view.current = "femodel"
    check_refused(lambda: setattr(pmx.view, "result_type", "Deformed"), ["results view"],
                  "a plot type set outside the results view was accepted")
    pmx.view.current = "results"
    check(pmx.view.result_type == "ColorContours", "a refused plot type changed the plot: " + pmx.view.result_type)


# The PNG header says everything this has to check: the size after the signature and the IHDR
# length and tag, then the colour type, which is 2 for RGB and 6 for RGB with an alpha channel
def read_png_header(path):
    with open(path, "rb") as stream:
        header = stream.read(26)
    check(header[:8] == b"\x89PNG\r\n\x1a\n", "the file must be a PNG: " + path)
    width = int.from_bytes(header[16:20], "big")
    height = int.from_bytes(header[20:24], "big")
    return width, height, header[25]


def test_the_picture():
    camera = pmx.view.get_parameters()
    pmx.view.set_orientation("isometricnegative")
    check_raises(lambda: pmx.view.set_orientation("Sideways"), "an unknown orientation must be rejected")
    check("IsometricNegative" in list(pmx.view.get_orientation_names()), "the orientation names are incomplete")
    pmx.view.set_orientation("Isometric")
    pmx.view.zoom_to_fit()
    path = pmx.view.save_image()
    check(os.path.exists(path), "the image must exist: " + path)
    check(os.path.getsize(path) > 0, "the image must not be empty: " + path)
    width, height, color_type = read_png_header(path)
    check(color_type == 2, "the default picture must carry the background it shows: " + str(color_type))
    os.remove(path)
    # A name of its own, and a name that is not a PNG
    named = os.path.join(TEMPORARY_FOLDER, "PyTest_View.png")
    check(pmx.view.save_image(named) == named, "the image must be written where it was asked for")
    check(os.path.exists(named), "the named image must exist: " + named)
    os.remove(named)
    check_raises(lambda: pmx.view.save_image("picture.jpg"), "only a .png may be written")
    # A relative name lands where the screenshot tool of the automation server puts it, not in the
    # working directory of the process
    relative = pmx.view.save_image("PyTest_Relative.png")
    expected = os.path.join(tempfile.gettempdir(), "PrePoMax", "PyTest_Relative.png")
    check(os.path.exists(relative), "the relative image must exist: " + relative)
    # TEMP may hold the short form of the same folder, so the files are compared, not the spellings
    check(os.path.exists(expected) and os.path.samefile(relative, expected),
          "a relative name must be placed in the PrePoMax temporary folder: " + relative)
    os.remove(relative)
    check_refused(lambda: pmx.view.save_image(BAD_FILE_NAME), ["'file_name'"], "an invalid file name was accepted")
    pmx.view.set_parameters(camera)


def test_the_picture_options():
    camera = pmx.view.get_parameters()
    pmx.view.set_orientation("Isometric")
    pmx.view.zoom_to_fit()
    plain = pmx.view.save_image()
    width, height, color_type = read_png_header(plain)
    os.remove(plain)
    # A transparent background is written with an alpha channel, at the size of the viewport
    transparent = pmx.view.save_image(None, True)
    check(read_png_header(transparent) == (width, height, 6),
          "a transparent picture must be the same size with an alpha channel: " + str(read_png_header(transparent)))
    os.remove(transparent)
    # A magnified picture is that many times larger in both directions
    magnified = pmx.view.save_image(None, False, 2)
    check(read_png_header(magnified) == (2 * width, 2 * height, 2),
          "a magnified picture must be twice the size: " + str(read_png_header(magnified)))
    os.remove(magnified)
    both = pmx.view.save_image(None, True, 2)
    check(read_png_header(both) == (2 * width, 2 * height, 6),
          "the two options must combine: " + str(read_png_header(both)))
    os.remove(both)
    # The background the user is looking at must be the one they had before the transparent picture
    after = pmx.view.save_image()
    check(read_png_header(after) == (width, height, 2),
          "the viewport background must be put back: " + str(read_png_header(after)))
    os.remove(after)
    # A magnification the renderer will not draw is refused, and nothing is written for it
    refused = os.path.join(TEMPORARY_FOLDER, "PyTest_View_Refused.png")
    check_raises(lambda: pmx.view.save_image(refused, False, 0), "a magnification below 1 must be rejected")
    check_raises(lambda: pmx.view.save_image(refused, False, 5), "a magnification above 4 must be rejected")
    check_raises(lambda: pmx.view.save_image(refused, False, "big"), "a magnification must be a number")
    check_raises(lambda: pmx.view.save_image(refused, "maybe"), "a transparent background must be a boolean")
    check(not os.path.exists(refused), "a refused picture must write nothing: " + refused)
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
    # The factor is kept as a single precision float, so a value it cannot hold is refused rather than made infinite
    check_refused(lambda: setattr(pmx.view, "deformation_factor", 1e39), ["'deformation_factor'"],
                  "a deformation factor beyond the float range was accepted")
    check(abs(float(pmx.view.deformation_factor) - 2.5) < 1e-6,
          "a refused deformation factor changed it: " + str(pmx.view.deformation_factor))
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


# The heading of an exported .inp carries a hash of the moment it was written, so it is left out of the comparison
def read_export(path):
    with open(path, "r") as reader:
        lines = [line for line in reader.read().splitlines() if not line.startswith("Hash:")]
    os.remove(path)
    return lines


def test_the_exported_increment():
    # An export records the result, the increment and the deformation scale factor it wrote, so that a
    # regeneration writes the same file whatever the view shows then; those not given are the ones shown
    result = pmx.results.current
    part_name = list(result.mesh.parts.get_names())[0]
    step_id = list(result.steps.get_ids())[0]
    increment_ids = list(result.steps[step_id].increments.get_ids())
    plotted = pmx.view.get_result_field()
    deformation_type = pmx.view.deformation_type
    deformation_factor = pmx.view.deformation_factor
    file_name = os.path.join(TEMPORARY_FOLDER, "PyTest_Export.inp")
    try:
        pmx.view.set_result_increment(step_id, increment_ids[-1])
        pmx.view.deformation_type = "UserDefined"
        pmx.view.deformation_factor = 3.0
        for extension in [".inp", ".stl"]:
            shown_file_name = os.path.join(TEMPORARY_FOLDER, "PyTest_Export_Shown" + extension)
            pmx.export_results(part_name, shown_file_name)
            shown = read_export(shown_file_name)
            # The view moves on, and the values given export what was shown before
            pmx.view.set_result_increment(step_id, increment_ids[0])
            pmx.view.deformation_factor = 1.0
            given_file_name = os.path.join(TEMPORARY_FOLDER, "PyTest_Export_Given" + extension)
            pmx.export_results(part_name, given_file_name, result_name=result.name, step_id=step_id,
                               increment_id=increment_ids[-1], deformation_factor=3.0)
            check(read_export(given_file_name) == shown,
                  "an export given the shown values must write the shown file, whatever the view shows now")
            pmx.export_results(part_name, given_file_name, step_id=step_id, increment_id=increment_ids[-1],
                               deformation_factor=1.0)
            check(read_export(given_file_name) != shown, "the deformation factor given must be the one exported")
            pmx.view.set_result_increment(step_id, increment_ids[-1])
            pmx.view.deformation_factor = 3.0
        # The file name alone finds the result a replay in another work directory loaded
        pmx.export_results(part_name, file_name, result_name=os.path.basename(result.name))
        check(os.path.isfile(file_name), "the file name of the result must find it")
        os.remove(file_name)
        # What the result does not have is refused before anything is written
        check_refused(lambda: pmx.export_results(part_name, file_name, result_name="PyTest_No_Such.frd"),
                      ["'result_name'"], "an export of a result that is not loaded was accepted")
        check_refused(lambda: pmx.export_results(part_name, file_name, step_id=999), ["999", "'step_id'"],
                      "an export of a step the result does not have was accepted")
        check_refused(lambda: pmx.export_results(part_name, file_name, increment_id=9999),
                      ["9999", "'increment_id'"], "an export of an increment the result does not have was accepted")
        check_refused(lambda: pmx.export_results(part_name, file_name, deformation_factor=1e39),
                      ["'deformation_factor'"], "a deformation factor beyond the float range was accepted")
        check_raises(lambda: pmx.export_results(part_name, file_name, deformation_factor="big"),
                     "a deformation factor must be a number")
        check_raises(lambda: pmx.export_results(part_name, file_name, step_id=1.5), "a step ID must be an integer")
        check(not os.path.isfile(file_name), "a refused export wrote the file")
    finally:
        pmx.view.set_result_increment(plotted["step_id"], plotted["increment_id"])
        pmx.view.deformation_type = deformation_type
        pmx.view.deformation_factor = deformation_factor


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
    camera = pmx.view.get_parameters()
    check(type(camera) is list and len(camera) == 10 and all(type(value) is float for value in camera),
          "the camera must be a list[float] of 10 values: " + repr(camera))
    pmx.view.set_orientation("Front")
    before = pmx.view.get_parameters()
    pmx.view.rotate(30, 20)
    after = pmx.view.get_parameters()
    check(any(abs(after[i] - before[i]) > 1e-6 for i in range(3)), "a rotation must move the camera: " + str(after))
    pmx.view.rotate(-30, -20)
    reference = pmx.view.get_parameters()
    # Looking at a point keeps the viewing direction and the view up, and puts the point in the centre
    target = [0.5, 0.5, 1.0]
    pmx.view.look_at(target, 2.0)
    framed = pmx.view.get_parameters()
    check(centre_offset(framed, target) < 1e-6, "look_at must put the point in the centre: " + str(framed))
    check(abs(framed[9] - 2.0) < 1e-6, "look_at must set the parallel scale: " + str(framed))
    direction = unit_direction(framed)
    previous = unit_direction(reference)
    for i in range(3):
        check(abs(direction[i] - previous[i]) < 1e-6, "look_at must keep the viewing direction: " + str(framed))
        check(abs(framed[6 + i] - reference[6 + i]) < 1e-6, "look_at must keep the view up: " + str(framed))
    pmx.view.look_at([0.0, 0.0, 0.0])
    check(abs(pmx.view.get_parameters()[9] - 2.0) < 1e-6, "a scale that is not given keeps the zoom")
    check_raises(lambda: pmx.view.look_at([1.0, 2.0]), "a short point must be rejected")
    check_raises(lambda: pmx.view.look_at([0.0, 0.0, 0.0], -1.0), "a negative parallel scale must be rejected")
    check_raises(lambda: pmx.view.rotate("a lot"), "a rotation must be a number")
    pmx.view.set_parameters(camera)


def test_the_annotations():
    annotations = pmx.view.annotations
    names_before = sorted(annotations.get_names())
    text_name = annotations.add_text("PyTest_Label", "Top of the cube", [0.5, 0.5, 1.0])
    check(text_name == "PyTest_Label", "add_text did not return the name it was given: " + str(text_name))
    node_name = annotations.add_node(7)
    element_name = annotations.add_element(1)
    names = list(annotations.get_names())
    check("PyTest_Label" in names and node_name in names and element_name in names,
          "the added annotations must be listed: " + str(names))
    check(node_name != element_name, "the generated names must differ: " + node_name)
    check_raises(lambda: annotations.add_text("PyTest_Label", "again", [0.0, 0.0, 0.0]), "a taken name must be rejected")
    check_raises(lambda: annotations.add_text("Min_Annotation", "mine", [0.0, 0.0, 0.0]), "a reserved name must be rejected")
    # Names are compared regardless of case, as model names are
    check_refused(lambda: annotations.add_text("pytest_label", "again", [0.0, 0.0, 0.0]), ["PyTest_Label"],
                  "a taken name in another case must be rejected")
    check_refused(lambda: annotations.add_text("max_annotation", "mine", [0.0, 0.0, 0.0]), ["reserved"],
                  "a reserved name in another case must be rejected")
    check_refused(lambda: annotations.remove("MIN_ANNOTATION"), ["application"],
                  "a reserved annotation in another case cannot be removed")
    check_raises(lambda: annotations.remove(" PyTest_Label"), "a padded name must find nothing")
    check_raises(lambda: annotations.add_text("PyTest_Short", "short", [0.0, 0.0]), "a short anchor must be rejected")
    check_raises(lambda: annotations.add_node(999), "an unknown node must be rejected")
    check_raises(lambda: annotations.add_element(999), "an unknown element must be rejected")
    check_raises(lambda: annotations.remove("PyTest_NoSuchAnnotation"), "an unknown annotation must be rejected")
    check_raises(lambda: annotations.remove("Max_Annotation"), "a reserved annotation cannot be removed")
    # The min/max labels are a setting of the application, so they are put back
    shown = annotations.get_min_max()
    check(type(shown) is list and len(shown) == 2 and all(type(value) is bool for value in shown),
          "the min/max setting is a list of two bools: " + repr(shown))
    annotations.show_min_max(True, True)
    check(annotations.get_min_max() == [True, True], "the min/max labels must be shown")
    annotations.show_min_max(False, False)
    check(annotations.get_min_max() == [False, False], "the min/max labels must be hidden")
    annotations.show_min_max(shown[0], shown[1])
    check(annotations.get_min_max() == shown, "the min/max setting must be put back: " + str(shown))
    #
    path = pmx.view.save_image()
    check(os.path.exists(path), "the annotated image must exist: " + path)
    os.remove(path)
    #
    annotations.remove(["PYTEST_LABEL", node_name, element_name])
    check(sorted(annotations.get_names()) == names_before,
          "the removed annotations must be gone: " + str(list(annotations.get_names())))


def test_a_result_opened_by_the_api():
    # A result opened by a script must fill the step and increment list the view selects from, as File - Open
    # fills it; an empty list once made the selection of an increment recurse until the process died
    file_name = pmx.results.current.file_name
    pmx.results.clear()
    pmx.results.open(file_name)
    check(pmx.view.get_result_field() is not None, "a result opened by the API must be plotted")
    pmx.view.set_result_field("DISP", "U3")
    result = pmx.results.current
    pictures = []
    for step_id in result.steps.get_ids():
        for increment_id in result.steps[step_id].increments.get_ids():
            pmx.view.set_result_increment(step_id, increment_id)
            plotted = pmx.view.get_result_field()
            check(plotted["step_id"] == step_id and plotted["increment_id"] == increment_id,
                  "the increment of a result opened by the API must be selectable: " + str(plotted))
            path = pmx.view.save_image()
            with open(path, "rb") as reader:
                pictures.append(reader.read())
            os.remove(path)
    # The plot is drawn for the increment that was selected, not left at the one drawn before it
    if len(pictures) > 1:
        check(pictures[0] != pictures[-1], "selecting another increment must redraw the plot")
    # Removing the current one of two results makes the other current, as Close Current Result does
    copy_name = os.path.join(TEMPORARY_FOLDER, "PyTest_Copy.frd")
    shutil.copyfile(file_name, copy_name)
    pmx.results.open(copy_name)
    pmx.results.remove_current()
    names = list(pmx.results.get_names())
    check(len(names) == 1 and pmx.results.current is not None, "the other result must be current: " + str(names))


def main():
    check(pmx.results.current is not None, "a result must be open")
    try:
        test_the_view_is_the_results_view()
        test_the_plotted_field()
        test_a_redraw_keeps_the_plotted_field()
        test_the_plot_type()
        test_the_deformation()
        test_the_exported_increment()
        test_the_legend_range()
        test_the_camera()
        test_the_annotations()
        test_the_picture()
        test_the_picture_options()
        test_a_result_opened_by_the_api()
    finally:
        shutil.rmtree(TEMPORARY_FOLDER, ignore_errors=True)
    print("TEST PASSED")


main()
