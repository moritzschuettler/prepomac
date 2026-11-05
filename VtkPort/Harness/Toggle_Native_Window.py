#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port: for the user's review. Run it from the Python panel of a PrePoMax window: the first run covers the
#viewport with the native view and sends it the ported areas (geometry and widgets: the parts, the scalar bar of a
#result); the next run hides it and the ActiViz view is back. The areas can be named in AREAS below.

import os
import sys

HARNESS = r"C:\Pc\Programiranje\PrePoMax\PrePoMax v3.0.n dev_vtk-pilot\VtkPort\Harness"
AREAS = ["geometry", "widgets"]
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402

if vtkport.on_ui(lambda: vtkport.VTK.NativeInWindow):
    vtkport.disable_native()
    print("The native view is hidden and the ActiViz view is back.")
else:
    names = vtkport.enable_native(AREAS, in_window=True)
    print("The native view shows %s: %s. Run the script again to close it." % (", ".join(AREAS), names))
