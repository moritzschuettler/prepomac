#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, stream W: the review of the camera feel and the widgets. Run it from the Python panel of a
#PrePoMax window started from this worktree's build: the first run covers the viewport with the native VTK 9.7 view,
#with the parts, the widgets and the ActiViz camera; the native interaction style then takes the mouse and the keys.
#The next run hides it and the ActiViz view is back. Try:
#
#  - the middle button: turn the camera; with shift pan; with ctrl zoom slowly; a middle click on the model sets
#    the rotation center (the orange cross shows while turning)
#  - the wheel: the smooth zoom at the mouse, ctrl for small steps
#  - the arrow keys: turn by 5 degrees; z with a left drag: the box zoom; a left drag: the box selection rubber
#    band, escape cancels it
#  - the triad: hover shows the negative axes, a click on a cone, an end, a shaft or the origin turns the view
#  - the widgets: hover highlights, drags, double clicks on the color bar, the status block and the annotations
#    (open a result for the status block, the scale bar and the min/max annotations)

import sys

HARNESS = r"C:\Pc\Programiranje\PrePoMax\PrePoMax v3.0.n dev_vtk-pilot\.claude\worktrees\port-widgets-401a67\VtkPort\Harness"
AREAS = ["geometry", "widgets"]
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402

vtkport.PMX = pmx
if vtkport.on_ui(lambda: vtkport.VTK.NativeInWindow):
    vtkport.disable_native()
    print("The native view is hidden and the ActiViz view is back.")
else:
    names = vtkport.enable_native(AREAS, in_window=True)
    widgets = vtkport.on_ui(lambda: list(vtkport.VTK.GetNativeMirrorWidgetNames()))
    print("The native view shows %s: %s, widgets %s." % (", ".join(AREAS), names, widgets))
    print("The native style has the mouse and the keys. Run the script again to close it.")
