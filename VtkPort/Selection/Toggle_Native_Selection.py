#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native port, stream S: for the user's review of selection and hover. Run it from the Python panel of a PrePoMax
#window: the first run covers the viewport with the native view and sends it the parts and the selection area, so the
#mouse over the model highlights what a click would select and a click selects (a drag rotates); the next run hides
#it and the ActiViz view is back. NATIVE_EXACT_PICKING is the open decision of slice C (vtkControl.NativeExactPicking).

import sys

HARNESS = r"C:\Pc\Programiranje\PrePoMax\PrePoMax v3.0.n dev_vtk-pilot\VtkPort\Harness"
AREAS = ["geometry", "widgets", "selection"]
NATIVE_EXACT_PICKING = True
sys.path.insert(0, HARNESS)
import vtkport  # noqa: E402

if vtkport.on_ui(lambda: vtkport.VTK.NativeInWindow):
    vtkport.disable_native()
    print("The native view is hidden and the ActiViz view is back.")
else:
    vtkport.on_ui(lambda: setattr(vtkport.VTK, "NativeExactPicking", NATIVE_EXACT_PICKING))
    names = vtkport.enable_native(AREAS, in_window=True)
    print("The native view shows %s: %s. Run the script again to close it." % (", ".join(AREAS), names))
