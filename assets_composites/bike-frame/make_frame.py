# Builds the mid-surface of a simplified carbon road bike frame (size 56, all lengths in mm) as a BREP shell for
# PrePoMax: the tubes are fused, the end caps are taken away, and what is left is the skin of the joined tubes, whose
# faces meet conformally at the joints. Every face is named after its tube in bike_frame_faces.json, so a study can
# give each tube its own layup.
#
#   PYTHONPATH=/opt/homebrew/lib python3 make_frame.py          (gmsh with its Python API, brew install gmsh)
#
# x points forward, y to the left, z up; the bottom bracket is at the origin.

import json
import math
import os

import gmsh

HERE = os.path.dirname(os.path.abspath(__file__))

# Points of the frame
BB = (0.0, 0.0, 0.0)
SEAT_CLUSTER = (-155.0, 0.0, 520.0)
SEAT_TOP = (-170.0, 0.0, 570.0)
HEAD_TOP = (395.0, 0.0, 570.0)
HEAD_BOTTOM = (435.0, 0.0, 430.0)
TOP_TUBE_FRONT = (403.0, 0.0, 542.0)
DOWN_TUBE_FRONT = (427.0, 0.0, 458.0)
AXLE_Y = 62.0
AXLE = (-405.0, 0.0, 70.0)

# name: (start, end, outer diameter)
TUBES = {
    "HEAD_TUBE": (HEAD_BOTTOM, HEAD_TOP, 46.0),
    "TOP_TUBE": (SEAT_CLUSTER, TOP_TUBE_FRONT, 34.0),
    "DOWN_TUBE": (BB, DOWN_TUBE_FRONT, 52.0),
    "SEAT_TUBE": (BB, SEAT_TOP, 34.0),
    "BB_SHELL": ((0.0, -43.0, 0.0), (0.0, 43.0, 0.0), 46.0),
    "CHAIN_STAY_L": ((-10.0, 35.0, 5.0), (AXLE[0], AXLE_Y, AXLE[2]), 22.0),
    "CHAIN_STAY_R": ((-10.0, -35.0, 5.0), (AXLE[0], -AXLE_Y, AXLE[2]), 22.0),
    "SEAT_STAY_L": ((SEAT_CLUSTER[0] - 5.0, 12.0, SEAT_CLUSTER[2] - 10.0), (AXLE[0], AXLE_Y, AXLE[2]), 16.0),
    "SEAT_STAY_R": ((SEAT_CLUSTER[0] - 5.0, -12.0, SEAT_CLUSTER[2] - 10.0), (AXLE[0], -AXLE_Y, AXLE[2]), 16.0),
    "DROPOUT_L": ((AXLE[0], AXLE_Y - 6.0, AXLE[2]), (AXLE[0], AXLE_Y + 6.0, AXLE[2]), 24.0),
    "DROPOUT_R": ((AXLE[0], -AXLE_Y + 6.0, AXLE[2]), (AXLE[0], -AXLE_Y - 6.0, AXLE[2]), 24.0),
}


# The angle the seam of a tube wall is turned by about the tube axis
SEAM_ANGLE = {}


def distance_to_axis(point, start, end):
    axis = [end[i] - start[i] for i in range(3)]
    length2 = sum(a * a for a in axis)
    t = max(0.0, min(1.0, sum((point[i] - start[i]) * axis[i] for i in range(3)) / length2))
    closest = [start[i] + t * axis[i] for i in range(3)]
    return math.sqrt(sum((point[i] - closest[i]) ** 2 for i in range(3)))


def main():
    gmsh.initialize()
    gmsh.option.setNumber("General.Terminal", 0)
    gmsh.model.add("frame")
    solids = []
    for name, (start, end, diameter) in TUBES.items():
        axis = [end[i] - start[i] for i in range(3)]
        tag = gmsh.model.occ.addCylinder(*start, *axis, diameter / 2)
        # The seam of the tube wall is turned away from the joints, as a joint across the seam is not meshed whole
        gmsh.model.occ.rotate([(3, tag)], *start, *axis, SEAM_ANGLE.get(name, 0.0))
        solids.append((3, tag))
    fused, _ = gmsh.model.occ.fuse(solids[:1], solids[1:])
    gmsh.model.occ.synchronize()
    # The skin of the fused tubes without the flat end caps
    faces = gmsh.model.getBoundary(fused, oriented=False)
    caps = [f for f in faces if gmsh.model.getType(*f) == "Plane"]
    gmsh.model.occ.remove(fused)
    gmsh.model.occ.remove(caps)
    # The faces are sewn, so the tubes share the edges along which they meet
    gmsh.model.occ.healShapes(gmsh.model.occ.getEntities(2), tolerance=1e-4, fixDegenerated=False,
                              fixSmallEdges=False, fixSmallFaces=False, sewFaces=True, makeSolids=False)
    # Faces of no area, slivers the fusion leaves where the stays end in the dropouts, are taken away
    slivers = [(2, tag) for _, tag in gmsh.model.occ.getEntities(2) if gmsh.model.occ.getMass(2, tag) < 1.0]
    if slivers:
        gmsh.model.occ.remove(slivers, recursive=True)
    gmsh.model.occ.synchronize()
    skin = gmsh.model.getEntities(2)
    # Each face belongs to the tube whose wall it lies on
    # (the centre of mass of a tube wall lies on its axis, so a point of the face itself decides), and is found again
    # in PrePoMax by its centre of mass and its area
    names = {}
    for dim, tag in skin:
        low, high = gmsh.model.getParametrizationBounds(dim, tag)
        point = gmsh.model.getValue(dim, tag, [(low[0] + high[0]) / 2, (low[1] + high[1]) / 2])
        best = min(TUBES, key=lambda n: abs(distance_to_axis(point, TUBES[n][0], TUBES[n][1]) - TUBES[n][2] / 2))
        names.setdefault(best, []).append({"center": gmsh.model.occ.getCenterOfMass(dim, tag),
                                           "area": gmsh.model.occ.getMass(dim, tag)})
    # A brep keeps the edges the faces share; a STEP file of the faces would write each of them on its own
    gmsh.write(os.path.join(HERE, "bike_frame.brep"))
    # The faces of each tube by their centres, which are matched to the face ids PrePoMax gives them
    data = {"tubes": {n: {"start": TUBES[n][0], "end": TUBES[n][1], "diameter": TUBES[n][2]} for n in TUBES},
            "face_centers": names, "bottom_bracket": BB, "head_tube": [HEAD_BOTTOM, HEAD_TOP],
            "seat_top": SEAT_TOP, "axle": [AXLE[0], AXLE_Y, AXLE[2]]}
    with open(os.path.join(HERE, "bike_frame_faces.json"), "w") as writer:
        json.dump(data, writer, indent=1)
    print("bike_frame.brep: %d faces, %s" % (len(skin), {n: len(c) for n, c in names.items()}))
    gmsh.finalize()


if __name__ == "__main__":
    main()
