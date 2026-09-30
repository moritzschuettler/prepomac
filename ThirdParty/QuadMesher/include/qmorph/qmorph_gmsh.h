#pragma once
// Gmsh integration of the Q-Morph mesher (links the Gmsh C++ API, gmsh.h).
//
// Typical use inside a Gmsh-based mesher such as PrePoMax's GmshCaller:
//
//   gmsh::model::mesh::generate(1);                 // curves (even per face)
//   qmg::makeFaceNodeCountsEven(faces);            // optional parity fix
//   qmg::setBackgroundAlgorithms(faces);            // before the 2D mesh
//   gmsh::model::mesh::generate(2);                 // triangles
//   auto report = qmg::quadMeshSurfaces(faces);     // triangles -> quads
//   // faces that Q-Morph rejected keep their triangles and are flagged
//   // with setRecombine; call gmsh::model::mesh::recombine() for them.
#include "qmorph/qmorph.h"

#include <string>
#include <vector>

namespace qmg
{
struct FaceReport
{
    int tag = 0;
    bool qmorph = false;         // quads written by Q-Morph (else: fallback)
    std::string reason;          // why the face fell back
    int background = 0;          // Gmsh 2D algorithm of the triangulation
    int triangles = 0;           // background triangles (input)
    int quads = 0;
    double p95 = 0, maxCondition = 0, minAngle = 0, maxAngle = 0;
    double seconds = 0;
    bool periodic = false, singular = false;
};

// Make every face's boundary node count even: odd faces are paired through
// the face-adjacency graph and one segment is added to each curve on the
// connecting path (transfinite curve counts). Requires the 1D mesh; the
// caller regenerates it if the returned count is non-zero.
int makeFaceNodeCountsEven(const std::vector<int>& faces);

// Per-face background triangulation: Frontal-Delaunay-for-quads (8) for
// regular faces, Frontal-Delaunay (6) for periodic or singular charts.
void setBackgroundAlgorithms(const std::vector<int>& faces);

// Replace each face's triangles by Q-Morph quads. A regular face whose result
// misses the quality gate (p95 <= 2, max <= 4, angles 45..135) is triangulated
// again with each of the alternative Gmsh algorithms and the best result is
// kept (empty list: no retries). Faces Q-Morph rejects keep their triangles
// and get setRecombine(2, tag); calling recombine() is left to the caller.
// Never throws for a single bad face.
std::vector<FaceReport> quadMeshSurfaces(const std::vector<int>& faces, const qm::Options& options = {},
                                         const std::vector<int>& alternatives = {6, 5});
}
