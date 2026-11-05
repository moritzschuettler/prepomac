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
AFTER_ENABLE = {
    "hinge_section": _section_view([30.0, 11.0, 5.0], [0.3, 0.2, 1.0], False, True),
    "hinge_result_section": _section_view([30.0, 11.0, 5.0], [0.3, 0.2, 1.0], True, False),
    "solid_geometry_section": _section_view([20.0, 20.0, 5.0], [1.0, 1.0, 0.0], False, True),
    "hinge_highlight": _highlight_hinge,
    "hinge_hidden": _hide_lever,
    "solid_geometry_highlight": _highlight_geometry_part,
}


# The result coordinates are single precision: the framing of a deformed shape may differ by one float rounding step
CAMERA_TOLERANCES = {"hinge_result_disp": 1e-7, "hinge_result_mises": 1e-7, "hinge_result_section": 1e-7}


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
