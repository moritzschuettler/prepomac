#PrePoMax Python Script
# Builds the orientation test model with an isotropic material and exports the solver input.
import math

OUT = r"C:\Users\MATEJ_~1\AppData\Local\Temp\claude\C--Pc-Programiranje-PrePoMax-PrePoMax-v3-0-n-dev\f5cfecb2-41b2-4cff-80ea-5c1ece3d3365\scratchpad\ccx\pmx_orientation\pmx_orientation.inp"


def rotation():
    def rx(a):
        c, s = math.cos(a), math.sin(a)
        return [[1, 0, 0], [0, c, -s], [0, s, c]]

    def rz(a):
        c, s = math.cos(a), math.sin(a)
        return [[c, -s, 0], [s, c, 0], [0, 0, 1]]
    a, b = rz(math.radians(20)), rx(math.radians(35))
    return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


pmx.model.materials.add("Iso", pmx.create.elastic(200000, 0.3))
for name in ("PLATE", "INCLINED", "CYLINDER"):
    pmx.model.sections.add_shell("Sec_" + name, "Iso", pmx.create.region("ElementSetName", name), thickness=1)
r = rotation()
o = [0, 200, 0]
pmx.model.coordinate_systems.add("Inclined", o, [o[i] + r[i][0] for i in range(3)], [o[i] + r[i][1] for i in range(3)])
pmx.model.coordinate_systems.add("Axis", [300, 0, 0], [301, 0, 0], [300, 1, 0], coordinate_system_type="Cylindrical")
pmx.model.material_orientations.add("Ori_Inclined", pmx.create.region("ElementSetName", "INCLINED"),
                                    coordinate_system_name="Inclined", rotation_angle_deg=30)
pmx.model.material_orientations.add("Ori_Cylinder", pmx.create.region("ElementSetName", "CYLINDER"),
                                    coordinate_system_name="Axis", reference_axis="Y")
pmx.export_to_calculix(OUT)
print("exported")
