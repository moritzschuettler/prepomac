#PrePoMax Python Script
# Builds a [0/90]s laminate on the flat and on the inclined plate of composite_shells.inp and exports the deck.
import math

OUT = r"C:\Users\MATEJ_~1\AppData\Local\Temp\claude\C--Pc-Programiranje-PrePoMax-PrePoMax-v3-0-n-dev\f5cfecb2-41b2-4cff-80ea-5c1ece3d3365\scratchpad\ccx\pmx_composite\pmx_composite.inp"


def rotation():
    def rx(a):
        c, s = math.cos(a), math.sin(a)
        return [[1, 0, 0], [0, c, -s], [0, s, c]]

    def rz(a):
        c, s = math.cos(a), math.sin(a)
        return [[c, -s, 0], [s, c, 0], [0, 0, 1]]
    a, b = rz(math.radians(20)), rx(math.radians(35))
    return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


def region(name):
    return pmx.create.region("ElementSetName", name)


pmx.model.materials.add("Ply", pmx.create.elastic_lamina([[140000, 10000, 0.3, 5000, 5000, 3333.333333, 0]]))
pmx.model.materials.add("Iso", pmx.create.elastic(200000, 0.3))
layers = [[0.5, "Ply", 0], [0.5, "Ply", 90]]
pmx.model.sections.add_composite_shell("Lam_Plate", region("PLATE"), layers, symmetric=True)
r = rotation()
o = [0, 200, 0]
pmx.model.coordinate_systems.add("Inclined", o, [o[i] + r[i][0] for i in range(3)], [o[i] + r[i][1] for i in range(3)])
pmx.model.material_orientations.add("Ori_Inclined", region("INCLINED"), coordinate_system_name="Inclined")
pmx.model.sections.add_composite_shell("Lam_Inclined", region("INCLINED"), layers, symmetric=True)
pmx.model.sections.add_shell("Iso_Cylinder", "Iso", region("CYLINDER"), thickness=1)
pmx.export_to_calculix(OUT)
print("exported")
