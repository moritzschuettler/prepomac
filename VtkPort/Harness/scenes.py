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
