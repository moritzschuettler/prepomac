#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
#
#VTK native pilot, phase 1: for the user's interactive check. Run it from the Python panel of a PrePoMax window:
#the first run covers the viewport with the VTK 9.7 view showing a mesh of 216 hexahedra (rotate, pan and zoom it
#with the mouse), the next run hides it and the ActiViz view is back. Mouse events are echoed to the output.
import clr

import System
from System import Action
from System.Reflection import BindingFlags

clr.AddReference("vtkControl")

FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
assembly = [a for a in System.AppDomain.CurrentDomain.GetAssemblies() if a.GetName().Name == "PrePoMax"][0]
FORM = assembly.GetType("PrePoMax.Program").GetField("_mainForm", FLAGS).GetValue(None)
VTK = FORM.GetType().GetField("_vtk", FLAGS).GetValue(FORM)


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


def on_event(sender, args):
    if str(args.Event) in ("LeftButtonDown", "LeftButtonUp"):
        print("native event %s at %s" % (args.Event, list(args.Values)[:2]))


def toggle():
    host = VTK.NativeHost
    if host.Visible:
        VTK.ShowNativeView(False)
        return "The ActiViz view is back."
    VTK.ShowNativeView(True)
    view = host.View
    if len(view.GetActorNames()) == 0:
        view.NativeEvent += on_event
        view.SetBackground(System.Array[System.Double]([0.82, 0.85, 0.9]), System.Array[System.Double]([1.0, 1.0, 1.0]))
        points, cells, types = grid(6, 1.0)
        view.AddMeshActor("Cube", points, cells, types, System.Array[System.Double]([0.3, 0.6, 0.9]), True)
        view.ResetCamera()
    view.Render()
    return "The VTK 9.7 view is shown: " + view.GetRenderInfo().splitlines()[2].strip()


box = {}
FORM.Invoke(Action(lambda: box.setdefault("text", toggle())))
print(box["text"])
