#PrePoMax Python Script
# Writes mirror_faces.brep and mirror_sweep.brep, nine of the shapes extruded by 20 mm, for
# Test_Model_Meshing_Mirror.py through GmshCommon (OCC kernel). mirror_faces.brep holds shell parts that are their
# own mirror images, one part per shape at x = 150 i, a plate that is not, a channel of three faces with a bore in
# its bottom, and three shapes whose edges are not split alike on both sides of their planes: a plate with a vertex
# inside one long side and the seam of its bore at 40 degrees, an annulus with the seam of its inner circle at 40
# degrees, and a tee with a vertex inside one half of its top. A bore is a circle of one closed edge with its vertex,
# the seam, at the angle given. The helpers of Make_Ring_Faces.py beside this script are used.
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Mirror_Faces.py -g No -x Yes -w <work folder>
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import math
import os
import sys
SRC = os.path.join(os.path.dirname(os.path.abspath(sys.argv[0])), "Make_Ring_Faces.py")
source = open(SRC).read()
exec(source[:source.index("L_OUTER = ")])
TEE = [(20, 0), (40, 0), (40, 30), (60, 30), (60, 45), (0, 45), (0, 30), (20, 30)]


def turned(points, cx, cy, angle_deg):
    a = math.radians(angle_deg)
    return [(cx + (x - cx) * math.cos(a) - (y - cy) * math.sin(a), cy + (x - cx) * math.sin(a) + (y - cy) * math.cos(a))
            for x, y in points]


def wall(dx, y):
    """The wall of the channel above the edge of its bottom at y, 20 high."""
    points = [OCC.AddPoint(dx + x, y, z) for x, z in ((0.0, 0.0), (60.0, 0.0), (60.0, 20.0), (0.0, 20.0))]
    lines = [OCC.AddLine(points[i], points[(i + 1) % 4]) for i in range(4)]
    return OCC.AddPlaneSurface(Array[Int32]([OCC.AddCurveLoop(Array[Int32](lines))]))


def channel(dx):
    bottom = face(rectangle(0, 0, 60, 40), [circle(30, 20, 6)], dx)
    walls = [wall(dx, 0.0), wall(dx, 40.0)]
    OCC.Fragment(dimtags(2, [bottom]), dimtags(2, walls), None, None, True, True)


SHELLS = [("plate with bore", lambda dx: face(rectangle(0, 0, 60, 40), [circle(30, 20, 8)], dx)),
          ("four bores", lambda dx: face(rectangle(0, 0, 80, 50), [circle(20, 12, 4), circle(60, 12, 4),
                                                                  circle(20, 38, 4), circle(60, 38, 4)], dx)),
          ("tee", lambda dx: face(polygon(TEE), [], dx)),
          ("disc", lambda dx: face(circle(20, 20, 20), [], dx)),
          ("slot", lambda dx: face(slot(15, 45, 15, 15), [], dx)),
          ("annulus", lambda dx: face(circle(30, 30, 20), [circle(30, 30, 10)], dx)),
          ("plate with window", lambda dx: face(rectangle(0, 0, 60, 40), [rectangle(20, 12, 40, 28)], dx)),
          ("turned plate", lambda dx: face(polygon(turned([(0, 0), (60, 0), (60, 40), (0, 40)], 30, 20, 30.0)),
                                           [circle(30, 20, 8, 30.0)], dx)),
          ("plate with bore off", lambda dx: face(rectangle(0, 0, 60, 40), [circle(22, 17, 6)], dx)),
          ("channel", channel),
          ("split plate", lambda dx: face(polygon([(0, 0), (18, 0), (60, 0), (60, 40), (0, 40)]),
                                          [circle(30, 20, 8, 40.0)], dx)),
          ("turned seam", lambda dx: face(circle(30, 30, 20), [circle(30, 30, 10, 40.0)], dx)),
          ("split tee", lambda dx: face(polygon(TEE[:5] + [(45, 45)] + TEE[5:]), [], dx))]
start()
for i, (name, make) in enumerate(SHELLS):
    make(SPACING * i)
    OCC.Synchronize()
    print("shell", name, "at x", SPACING * i)
finish("mirror_faces.brep")

SOLIDS = ["plate with bore", "four bores", "tee", "disc", "slot", "annulus", "plate with window", "split plate",
          "turned seam"]
start()
for i, name in enumerate(SOLIDS):
    extrude(dict(SHELLS)[name](SPACING * i), 20.0)
    print("solid", name, "at x", SPACING * i)
finish("mirror_sweep.brep")
print("TEST PASSED")
