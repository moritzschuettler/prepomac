"""The scenes of the port checks: a model, how PrePoMax opens it, and the steps that set it up. A stream adds the
scenes its slices need. Inside PrePoMax, setup(name) prepares a scene with pmx; with the system Python,

    py -3 scenes.py <scene>

prints the PrePoMax command line arguments that open the scene's model, one per line, for the runner.
"""

import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
MODELS = os.path.join(ROOT, "APIs", "Python", "Test_Scripts", "Models")
JOINTS = os.path.join(ROOT, "Tests", "Joints")


def _mesh_first_part(pmx):
    pmx.model.mesh.generate(list(pmx.model.geometry.parts)[0].name)
    pmx.view.current = "femodel"


def _results(field, component):
    def setup(pmx):
        job_name = list(pmx.jobs)[0].name
        pmx.jobs[job_name].run(timeout=600)
        pmx.jobs.open_results(job_name)
        pmx.view.current = "results"
        pmx.view.set_result_field(field, component)
    return setup


# name: (model file, extra arguments, setup)
SCENES = {
    "solid_geometry": (os.path.join(MODELS, "L_bracket_r.step"), ["-u", "MM_TON_S_C"],
                       lambda pmx: setattr(pmx.view, "current", "geometry")),
    "solid_femodel": (os.path.join(MODELS, "L_bracket_r.step"), ["-u", "MM_TON_S_C"], _mesh_first_part),
    "hinge_femodel": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], lambda pmx: setattr(pmx.view, "current", "femodel")),
    "hinge_result_disp": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("DISP", "ALL")),
    "hinge_result_mises": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("STRESS", "MISES")),
}


# Stream G (geometry): the scenes of its slices, and the steps that must run after the native areas are switched on,
# since switching them on redraws the model and so drops highlights and hidden actors set before
def _view(view):
    return lambda pmx: setattr(pmx.view, "current", view)


def _on_ui_controller(call):
    import vtkport
    return vtkport.on_ui(lambda: call(vtkport.CONTROLLER))


def _highlight_hinge(pmx):
    import System
    _on_ui_controller(lambda c: c.HighlightElements(System.Array[System.Int32](list(range(1, 400, 7)))))
    _on_ui_controller(lambda c: c.HighlightNodes(System.Array[System.Array[System.Double]](
        [System.Array[System.Double]([0.0, 0.0, 0.0]), System.Array[System.Double]([10.0, 5.0, 2.0])])))


def _hide_lever(pmx):
    import System
    _on_ui_controller(lambda c: c.HideModelParts(System.Array[System.String](["Lever"])))


def _highlight_geometry_part(pmx):
    import System
    _on_ui_controller(lambda c: c.HighlightGeometryParts(
        System.Array[System.String]([list(pmx.model.geometry.parts)[0].name])))


SCENES.update({
    "shell_femodel": (os.path.join(JOINTS, "Joint_Plates_Hinge.pmx"), [], _view("femodel")),
    "springs_femodel": (os.path.join(JOINTS, "Joint_Plates_Springs_Hinge.pmx"), [], _view("femodel")),
    "hinge_highlight": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
    "hinge_hidden": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
    "solid_geometry_highlight": (os.path.join(MODELS, "L_bracket_r.step"), ["-u", "MM_TON_S_C"], _view("geometry")),
})
SCENES.update({
    "hinge_symbols": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
    "springs_symbols": (os.path.join(JOINTS, "Joint_Springs_Weld.pmx"), [], _view("femodel")),
    "slider_symbols": (os.path.join(JOINTS, "Joint_Springs_Slider.pmx"), [], _view("femodel")),
    "ball_symbols": (os.path.join(JOINTS, "Joint_Springs_Ball.pmx"), [], _view("femodel")),
    "plates_weld_symbols": (os.path.join(JOINTS, "Joint_Plates_Springs_Weld.pmx"), [], _view("femodel")),
})
def _section_view(point, normal, lighten, hatch, element_wise=False):
    def run(pmx):
        import System
        _on_ui_controller(lambda c: c.CreateSectionView(System.Array[System.Double](point),
                                                        System.Array[System.Double](normal), lighten,
                                                        System.Drawing.Color.Empty, element_wise, hatch))
    return run


SCENES.update({
    "hinge_section": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
    "hinge_result_section": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("STRESS", "MISES")),
    "solid_geometry_section": (os.path.join(MODELS, "L_bracket_r.step"), ["-u", "MM_TON_S_C"], _view("geometry")),
})
def _exploded(pmx):
    pmx.view.apply_exploded_view()


def _transformations(symmetry_point, circular):
    def run(pmx):
        import System
        import vtkport

        def apply():
            vtk = vtkport.VTK
            vtk.AddSymmetry(0, System.Array[System.Double](symmetry_point))
            if circular:
                vtk.AddCircularPattern(System.Array[System.Double]([0.0, 0.0, 0.0]),
                                       System.Array[System.Double]([0.0, 1.0, 0.0]), 120.0, 3)
            vtk.ApplyTransforms()
        vtkport.on_ui(apply)
    return run


SCENES.update({
    "hinge_exploded": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
    "hinge_result_symmetry": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("DISP", "ALL")),
    "hinge_pattern": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
})
def _animation_frame(number_of_frames, frame):
    def run(pmx):
        import vtkport

        def animate():
            vtkport.CONTROLLER.DrawScaleFactorAnimation(number_of_frames)
            vtkport.VTK.SetAnimationFrame(frame, False)
        vtkport.on_ui(animate)
    return run


SCENES.update({
    "hinge_animation_frame": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("DISP", "ALL")),
})
def _result_plot(field, component, result_type):
    def setup(pmx):
        _results(field, component)(pmx)
        pmx.view.result_type = result_type
    return setup


SCENES.update({
    "hinge_result_vector": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _result_plot("DISP", "ALL", "VectorPlot")),
    "hinge_result_tensor": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [],
                            _result_plot("STRESS", "PRINCIPAL_MAX", "TensorPlot")),
    "hinge_vector_animation_frame": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [],
                                     _result_plot("DISP", "ALL", "VectorPlot")),
    "hinge_tensor_animation_frame": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [],
                                     _result_plot("STRESS", "PRINCIPAL_MAX", "TensorPlot")),
})
AFTER_ENABLE = {
    "hinge_vector_animation_frame": _animation_frame(8, 3),
    "hinge_tensor_animation_frame": _animation_frame(8, 5),
    "hinge_animation_frame": _animation_frame(8, 3),
    "hinge_exploded": _exploded,
    "hinge_result_symmetry": _transformations([95.0, 0.0, 0.0], False),
    "hinge_pattern": _transformations([-35.0, 0.0, 0.0], True),
    "hinge_section": _section_view([30.0, 11.0, 5.0], [0.3, 0.2, 1.0], False, True),
    "hinge_result_section": _section_view([30.0, 11.0, 5.0], [0.3, 0.2, 1.0], True, False),
    "solid_geometry_section": _section_view([20.0, 20.0, 5.0], [1.0, 1.0, 0.0], False, True),
    "hinge_highlight": _highlight_hinge,
    "hinge_hidden": _hide_lever,
    "solid_geometry_highlight": _highlight_geometry_part,
}


# The result coordinates, the transformed copies and the actor positions of an exploded view are single precision on
# the ActiViz side: the framing may differ by one float rounding step
CAMERA_TOLERANCES = {"hinge_result_disp": 1e-7, "hinge_result_mises": 1e-7, "hinge_result_section": 1e-7,
                     "hinge_result_symmetry": 1e-7, "hinge_exploded": 1e-7, "hinge_pattern": 1e-7,
                     "hinge_animation_frame": 1e-7, "hinge_result_vector": 1e-7, "hinge_result_tensor": 1e-7,
                     "hinge_vector_animation_frame": 1e-7, "hinge_tensor_animation_frame": 1e-7}


def _doubles(values):
    import System
    return System.Array[System.Double]([float(v) for v in values])


def _points(points):
    import System
    return System.Array[System.Array[System.Double]]([_doubles(p) for p in points])


# Captions of the overlay and selection layers, as PrePoMax draws them (ActiViz draws no caption added to the base
# layer, which PrePoMax never does), labels of both overloads and a caption moved in place
def _captions(pmx):
    import System
    import vtkport
    from System.Drawing import Color
    from vtkControl import vtkRendererLayer

    def add():
        vtk = vtkport.VTK
        vtk.AddCaptionActor("caption_overlay", "Overlay caption", Color.Black, _doubles([40, 0, 0]),
                            _doubles([20, 20, 0]), 1.2, vtkRendererLayer.Overlay)
        vtk.AddCaptionActor("caption_selection", "Selected caption", Color.Black, _doubles([0, 10, 10]), None, 0.9,
                            vtkRendererLayer.Selection)
        positions = _points([[0, 0, 0], [20, 0, 5], [60, 0, 0]])
        vtk.AddLabelActor("labels_int", positions, System.Array[System.Int32]([1, 22, 333]), Color.Blue, 0.9,
                          vtkRendererLayer.Overlay)
        vtk.AddLabelActor("labels_text", _points([[30, 10, 5], [70, 5, 0]]), System.Array[System.String](["Aa", "Bbb"]),
                          Color.DarkGreen, 1.0, vtkRendererLayer.Base)
        vtk.UpdateOverlayCaptionActorPosition("caption_overlay", _doubles([45, 2, 0]))
    vtkport.on_ui(add)


def _item_ids(pmx):
    import vtkport
    from PrePoMax import ShowItemIdsEnum
    vtkport.on_ui(lambda: setattr(vtkport.CONTROLLER, "ShowItemIds", ShowItemIdsEnum.Parts | ShowItemIdsEnum.PartNames))


def _measurement(pmx):
    import vtkport

    def draw():
        controller = vtkport.CONTROLLER
        controller.DrawMeasurementAnnotation("distance", _points([[0, 0, 0], [60, 10, 0]]),
                                             _points([[0, 0, 0], [30, 20, 5], [60, 10, 0]]), True, True)
        controller.UpdateMeasurementAnnotation("distance", _points([[5, 0, 0], [70, 10, 5]]),
                                               _points([[5, 0, 0], [35, 25, 5], [70, 10, 5]]), True, True)
    vtkport.on_ui(draw)


SCENES.update({
    "hinge_captions": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
    "hinge_item_ids": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
    "hinge_measurement": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
})
AFTER_ENABLE.update({
    "hinge_captions": _captions,
    "hinge_item_ids": _item_ids,
    "hinge_measurement": _measurement,
})


# The element-wise section: the hinge with its constraint faces, a result, and a plane that misses the lever
SCENES.update({
    "hinge_section_element_wise": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
    "hinge_result_section_element_wise": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("STRESS", "MISES")),
    "hinge_section_element_wise_side": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
})
AFTER_ENABLE.update({
    "hinge_section_element_wise": _section_view([30.0, 11.0, 5.0], [0.3, 0.2, 1.0], False, False, True),
    "hinge_result_section_element_wise": _section_view([30.0, 11.0, 5.0], [0.3, 0.2, 1.0], True, False, True),
    "hinge_section_element_wise_side": _section_view([12.0, 0.0, 0.0], [1.0, 0.0, 0.0], False, False, True),
})
# The exposed faces come from the volume grid, which is single precision on the ActiViz side
for _name in ["hinge_section_element_wise", "hinge_result_section_element_wise", "hinge_section_element_wise_side"]:
    CAMERA_TOLERANCES[_name] = 1e-7


# A section view of an animated result: the section is made again at every frame, from the volume grid at it
def _animated_section(element_wise):
    def run(pmx):
        import System
        import vtkport

        def animate():
            vtkport.CONTROLLER.DrawScaleFactorAnimation(8)
            vtkport.VTK.SetAnimationFrame(2, False)
            vtkport.CONTROLLER.CreateSectionView(System.Array[System.Double]([30.0, 11.0, 5.0]),
                                                 System.Array[System.Double]([0.3, 0.2, 1.0]), True,
                                                 System.Drawing.Color.Empty, element_wise, False)
            vtkport.VTK.SetAnimationFrame(6, False)
        vtkport.on_ui(animate)
    return run


SCENES.update({
    "hinge_animated_section": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("DISP", "ALL")),
    "hinge_animated_section_element_wise": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("DISP", "ALL")),
})
AFTER_ENABLE.update({
    "hinge_animated_section": _animated_section(False),
    "hinge_animated_section_element_wise": _animated_section(True),
})
CAMERA_TOLERANCES["hinge_animated_section"] = 1e-7
CAMERA_TOLERANCES["hinge_animated_section_element_wise"] = 1e-7
# Seen from the right the section plane is nearly edge on: VTK 9 draws a one pixel sliver of the section face that
# VTK 5.8 left out
SECTION_EDGE_ON_COVERAGE = 0.998


# Directional results on transformed copies: U1 changes sign under the symmetry, the principal direction of a tensor
# turns with the circular pattern
SCENES.update({
    "hinge_result_symmetry_u1": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("DISP", "U1")),
    "hinge_result_pattern_s11": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _results("STRESS", "S11")),
})
AFTER_ENABLE.update({
    "hinge_result_symmetry_u1": _transformations([95.0, 0.0, 0.0], False),
    "hinge_result_pattern_s11": _transformations([95.0, 0.0, 0.0], True),
})
CAMERA_TOLERANCES["hinge_result_symmetry_u1"] = 1e-7
CAMERA_TOLERANCES["hinge_result_pattern_s11"] = 1e-7


# Coordinate systems of the model in the overlay layer (two sizes, one rotated), a selected one and material axes
def _coordinate_systems(pmx):
    import math
    import System
    import vtkport
    from vtkControl import vtkRendererLayer

    def labels(names):
        return System.Array[System.String](names)

    def add():
        vtk = vtkport.VTK
        c, s = math.cos(math.radians(30)), math.sin(math.radians(30))
        vtk.AddCoordinateSystemSymbol(_doubles([0, 0, 0]), _points([[1, 0, 0], [0, 1, 0], [0, 0, 1]]),
                                      labels(["X", "Y", "Z"]), 50.0, vtkRendererLayer.Overlay)
        vtk.AddCoordinateSystemSymbol(_doubles([60, 5, 5]), _points([[c, s, 0], [-s, c, 0], [0, 0, 1]]),
                                      labels(["R", "T", "Z"]), 70.0, vtkRendererLayer.Overlay)
        vtk.AddCoordinateSystemSymbol(_doubles([30, 10, 10]), _points([[0, 1, 0], [0, 0, 1], [1, 0, 0]]),
                                      labels(["X", "Y", "Z"]), 50.0, vtkRendererLayer.Selection)
        vtk.AddMaterialAxesSymbol(_doubles([80, 0, 0]), _points([[c, 0, s], [0, 1, 0], [-s, 0, c]]),
                                  labels(["1", "2", "3"]), 40.0)
    vtkport.on_ui(add)


SCENES.update({
    "hinge_coordinate_systems": (os.path.join(JOINTS, "Joint_Hinge.pmx"), [], _view("femodel")),
})
AFTER_ENABLE["hinge_coordinate_systems"] = _coordinate_systems


# The vector and tensor plots draw thousands of dots: VTK 5.8 smoothed their edges, the sphere imposters of VTK 9 have
# hard edges (rendering default 1 of stream G), which alone differs on up to 6 % of the pixels
DIFFERING_LIMITS = {"hinge_result_vector": 0.07, "hinge_result_tensor": 0.07, "hinge_vector_animation_frame": 0.07,
                    "hinge_tensor_animation_frame": 0.07}


# Text: FreeType of VTK 9 draws some glyphs a row shorter than VTK 5.8 did (the descenders), so the outline of a
# scene with much text on the background covers a little less
COVERAGE_LIMITS = {"hinge_captions": 0.99, "hinge_item_ids": 0.99, "hinge_measurement": 0.99,
                   "hinge_coordinate_systems": 0.99}
COVERAGE_LIMITS["hinge_animated_section"] = SECTION_EDGE_ON_COVERAGE


def after_enable(name, pmx):
    if name in AFTER_ENABLE:
        AFTER_ENABLE[name](pmx)


def setup(name, pmx):
    SCENES[name][2](pmx)


def launch_arguments(name):
    model, extra, _ = SCENES[name]
    return ["-f", model] + extra


if __name__ == "__main__":
    if len(sys.argv) != 2 or sys.argv[1] not in SCENES:
        sys.stderr.write("Scenes: %s\n" % ", ".join(sorted(SCENES)))
        sys.exit(2)
    print("\n".join(launch_arguments(sys.argv[1])))
