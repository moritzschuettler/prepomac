#PrePoMax Python Script
# Writes quad_dominant_leftovers.brep for Test_Model_Meshing_Quad_Dominant_Leftovers.py: faces cut out of the corpus
# models on which the quad-dominant recombination kept triangles before 2026-11-17, each group translated to
# x = SPACING i, through GmshCommon (OCC kernel). The faces are found by their area (and centre where areas repeat) in
# the BREP files the corpus measured, which are not in the repository: geo/geo_simple3.brep (gmsh's example simple3
# converted to BREP), scratch/component8.step_boundary.brep and scratch/geo_baffles.brep_boundary.brep (the boundary
# shells of the solids of component8.step and of gmsh's example baffles).
#
#  PrePoMax.com -u MM_TON_S_C --script Make_Quad_Dominant_Leftovers.py -g No -x Yes -w <work folder>
#  with PMX_CORPUS_DIR = the folder that holds geo and scratch
#  The files go to the folder in PMX_MODEL_DIR, or to the Models folder above this script when it is not set.
import os
import sys
import clr
clr.AddReference('GmshCommon')
from GmshCommon import Gmsh
from System import Array, Int32, Tuple

OUT = os.environ.get("PMX_MODEL_DIR") or os.path.dirname(os.path.dirname(os.path.abspath(sys.argv[0])))
CORPUS = os.environ["PMX_CORPUS_DIR"]
OCC = Gmsh.Model.OCC
SPACING = 100.0

# name, source BREP, the faces kept as (area, centre or None); all faces when None
GROUPS = [
    # A face of a square cut by an ellipse with a corner of 50 degrees, two triangles left in it (geo_simple3 S2 and
    # geo_extrude S12), with the square around it and the piece of the ellipse, which is laid as a lens face first
    ("simple3", os.path.join("geo", "geo_simple3.brep"), None),
    # Slivers of a B-spline blend with two corners of 4 degrees, whose parameters fold gmsh's mesh (S2) and do not
    # (S11): component8.step
    ("blend fold", os.path.join("scratch", "component8.step_boundary.brep"), [(15.886794, None)]),
    ("blend", os.path.join("scratch", "component8.step_boundary.brep"), [(15.889143, None)]),
    # The boundary of gmsh's example baffles: two boxes side by side whose faces x = 0, y = 1 and y = 0 carry a curve
    # inside them, ending inside the face or on its boundary (geo_baffles S4, S6, S9)
    ("baffles", os.path.join("scratch", "geo_baffles.brep_boundary.brep"), None),
]


def dimtags(dim, tags):
    return Array[Tuple[Int32, Int32]]([Tuple[Int32, Int32](dim, t) for t in tags])


def surfaces():
    OCC.Synchronize()
    return set(t.Item2 for t in OCC.GetEntities(2))


def matches(tag, area, centre):
    if abs(OCC.GetMass(2, tag, 0.0) - area) > 1E-5 * max(1.0, area): return False
    if centre is None: return True
    x, y, z = OCC.GetCenterOfMass(2, tag)
    return abs(x - centre[0]) < 1E-6 and abs(y - centre[1]) < 1E-6 and abs(z - centre[2]) < 1E-6


Gmsh.Initialize()
Gmsh.Option.SetNumber("General.Terminal", 0)
Gmsh.Model.Add("m")
for i, (name, source, faces) in enumerate(GROUPS):
    existing = surfaces()
    OCC.ImportShapes(os.path.join(CORPUS, source), None, True, "")
    OCC.Synchronize()
    volumes = [t.Item2 for t in OCC.GetEntities(3)]
    if volumes: OCC.Remove(dimtags(3, volumes), False)
    new = sorted(tag for tag in surfaces() if tag not in existing)
    if faces is None: keep = new
    else:
        keep = []
        for area, centre in faces:
            found = [tag for tag in new if matches(tag, area, centre)]
            if len(found) == 0: raise Exception("TEST FAILED: " + name + " has " + str(len(found)) + " faces of area " +
                                                str(area))
            keep.append(found[0])
    OCC.Remove(dimtags(2, [tag for tag in new if tag not in keep]), True)
    OCC.Synchronize()
    boxes = [Gmsh.Model.GetBoundingBox(2, tag) for tag in keep]
    x0 = min(box[0] for box in boxes)
    OCC.Translate(dimtags(2, keep), SPACING * i - x0, 0.0, 0.0)
    OCC.Synchronize()
    for tag in keep:
        print("group", name, "surface", tag, Gmsh.Model.GetType(2, tag), "area", round(OCC.GetMass(2, tag, 0.0), 6))
OCC.Synchronize()
Gmsh.Write(os.path.join(OUT, "quad_dominant_leftovers.brep"))
Gmsh.FinalizeAll()
print("TEST PASSED")
