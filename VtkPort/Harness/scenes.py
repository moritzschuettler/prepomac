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
def _section_view(point, normal, lighten, hatch):
    def run(pmx):
        import System
        _on_ui_controller(lambda c: c.CreateSectionView(System.Array[System.Double](point),
                                                        System.Array[System.Double](normal), lighten,
                                                        System.Drawing.Color.Empty, False, hatch))
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


# The vector and tensor plots draw thousands of dots: VTK 5.8 smoothed their edges, the sphere imposters of VTK 9 have
# hard edges (rendering default 1 of stream G), which alone differs on up to 6 % of the pixels
DIFFERING_LIMITS = {"hinge_result_vector": 0.07, "hinge_result_tensor": 0.07, "hinge_vector_animation_frame": 0.07,
                    "hinge_tensor_animation_frame": 0.07}


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
