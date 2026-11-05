#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native pilot, slice B: for the user's interactive check. Run it from the Python panel of a PrePoMax window:
#the first run covers the viewport with the VTK 9.7 view showing a mesh and the native scalar bar, and leaves it
#open. Drag the bar, hover over it and its value labels, double click a value label or the title, rotate the mesh
#beside the bar, and resize the window. Double clicks are echoed to the output. The next run removes the bar and
#the ActiViz view is back.

import clr

import System
from System import Action
from System.Reflection import BindingFlags

clr.AddReference("vtkControl")
clr.AddReference("System.Drawing")
from vtkControl.Native import VtkNativeScalarBar, VtkNativeTextStyle, VtkNativeWidgetAnchor

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
assembly = [a for a in System.AppDomain.CurrentDomain.GetAssemblies() if a.GetName().Name == "PrePoMax"][0]
FORM = assembly.GetType("PrePoMax.Program").GetField("_mainForm", FLAGS).GetValue(None)
VTK = FORM.GetType().GetField("_vtk", FLAGS).GetValue(FORM)
MESH_NAME = "ScalarBarCheck"


def grid(n, size):
    points = []
    for k in range(n + 1):
        for j in range(n + 1):
            for i in range(n + 1):
                points += [i * size / n, j * size / n, k * size / n]

    def index(i, j, k):
        return i + j * (n + 1) + k * (n + 1) * (n + 1)
    cells = []
    for k in range(n):
        for j in range(n):
            for i in range(n):
                cells += [8, index(i, j, k), index(i + 1, j, k), index(i + 1, j + 1, k), index(i, j + 1, k),
                          index(i, j, k + 1), index(i + 1, j, k + 1), index(i + 1, j + 1, k + 1), index(i, j + 1, k + 1)]
    return (System.Array[System.Double](points), System.Array[System.Int32](cells),
            System.Array[System.Byte]([12] * (n * n * n)))


def rainbow(t):
    # Blue over cyan, green and yellow to red, as the default legend
    stops = [(0.0, (0, 0, 1)), (0.25, (0, 1, 1)), (0.5, (0, 1, 0)), (0.75, (1, 1, 0)), (1.0, (1, 0, 0))]
    for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
        if t <= t1:
            f = (t - t0) / (t1 - t0)
            return System.Array[System.Double]([c0[i] + f * (c1[i] - c0[i]) for i in range(3)])
    return System.Array[System.Double]([1, 0, 0])


def on_double_click(sender, e):
    empty = System.Drawing.Rectangle.Empty
    ok, edit_minimum, rectangle, text, low, high = sender.TryGetValueEditData(e.X, e.Y, False, empty, "", 0.0, 0.0)
    if ok:
        print("double click on the %s label %s: edit box %s, limits %g to %g" %
              ("minimum" if edit_minimum else "maximum", text, rectangle, low, high))
    else:
        print("double click on the scalar bar at (%d, %d): the legend settings would open" % (e.X, e.Y))


def toggle():
    host = VTK.NativeHost
    if host.Visible and host.Tag is not None:
        host.Tag.Dispose()
        host.Tag = None
        host.View.RemoveActor(MESH_NAME)
        VTK.ShowNativeView(False)
        return "The scalar bar is removed and the ActiViz view is back."
    VTK.ShowNativeView(True)
    System.Windows.Forms.Application.DoEvents()
    view = host.View
    points, cells, types = grid(6, 1.0)
    view.AddMeshActor(MESH_NAME, points, cells, types, System.Array[System.Double]([0.6, 0.8, 0.6]), True)
    view.ResetCamera()
    bar = VtkNativeScalarBar(view)
    bar.SetTextStyle(VtkNativeTextStyle.CreateWidgetStyle())
    bar.SetPadding(15)
    bar.SetLabelFormat("E3")
    bar.SetNumberOfColors(9)
    bar.CreateLookupTable(System.Func[System.Double, System.Array[System.Double]](rainbow), 0.0, 134.3)
    bar.SetText("STRESS", "MISES", "MPa", None, "Automatic")
    bar.SetFrame(True, System.Array[System.Double]([1, 1, 1]), True, System.Array[System.Double]([0, 0, 0]))
    bar.SetPosition(VtkNativeWidgetAnchor.TopLeft, 20, 20)
    bar.MouseDoubleClick += on_double_click
    host.Tag = bar
    view.Render()
    return ("The VTK 9.7 view with the native scalar bar is shown. Drag the bar, hover over it, double click a value "
            "label or the title, rotate the mesh beside the bar and resize the window. Run the script again to close.")


box = {}
FORM.Invoke(Action(lambda: box.setdefault("text", toggle())))
print(box["text"])
