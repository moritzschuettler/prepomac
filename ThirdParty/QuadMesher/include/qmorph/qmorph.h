#pragma once
// Q-Morph quad meshing (Owen, Staten, Canann, Saigal, IJNME 44:1317-1340,
// 1999). C++ port of prototype/qmorph.py and prototype/cleanup.py; the
// algorithm notes and our interpretations are documented in those files and
// in README.md.
//
// Surfaces: nodes carry parameter coordinates (u, v) and a 3D position on the
// surface. Topology decisions (validity, swaps, edge recovery) are made in
// parameter space; angles, lengths, side-edge directions, smoothing and
// quality use the surface itself (tangent planes, Section 3.3.3). Without a
// surface the parameter plane is the geometry.
#include <array>
#include <map>
#include <string>
#include <vector>

namespace qm
{
struct V2 { double x = 0, y = 0; };
struct V3 { double x = 0, y = 0, z = 0; };

// A regular, orientation-preserving chart: position and first derivatives.
class Surface
{
public:
    virtual ~Surface() = default;
    virtual void eval(V2 uv, V3& X, V3& Xu, V3& Xv) const = 0;
};

// Background triangulation in parameter space. Triangles may be given in
// either orientation; fixed nodes (the prescribed boundary nodes) never move.
struct InputMesh
{
    std::vector<V2> nodes;           // (u, v)
    std::vector<char> fixed;
    std::vector<std::array<int, 3>> triangles;
    const Surface* surface = nullptr;   // nullptr: the (u, v) plane
};

struct Options
{
    bool transitions = true;   // transition seam / split (Sections 3.8, 3.9)
    // Run with and without transitions and keep the better mesh (fewer
    // triangles, then fewer invalid quads, then the FEM gate p95 <= 2 and
    // max <= 4, then lower p95). Overrides 'transitions'.
    bool autoTransitions = true;
    bool cleanup = true;       // Section 3.10 clean-up and smoothing
};

struct Result
{
    std::vector<V2> nodes;           // (u, v)
    std::vector<V3> xyz;             // positions on the surface
    std::vector<char> fixed;
    std::vector<int> source;         // input node index, or -1 for a new node
    std::vector<std::array<int, 4>> quads;
    std::vector<std::array<int, 3>> triangles;   // left over (odd loops)
    std::map<std::string, long> stats;
    double convertSeconds = 0, cleanupSeconds = 0;
    // physical Q1 condition of the quads
    double p95Condition = 0, maxCondition = 0;
    double minAngle = 0, maxAngle = 0;   // corner angles in degrees (on the surface)
    // the same without the quads at input corners sharper than 60 degrees,
    // whose shape the input dictates (used for the quality gate)
    double gateMaxCondition = 0, gateMinAngle = 0, gateMaxAngle = 0;
    int sharpCornerQuads = 0;
    int invalidQuads = 0;
    // ok: an all-quad mesh (one triangle allowed only for an odd boundary)
    // with no invalid quad and all fixed nodes unchanged. Otherwise the caller
    // should keep its own mesh for this face; message says why.
    bool ok = false;
    std::string message;
};

Result qmorph(const InputMesh& input, const Options& options = {});

// Q1 condition number (max over 4 corners + centre); +inf for an inverted
// sample. Planar version, and a surface version with the reference normal.
double q1Condition(const std::array<V2, 4>& quad);
double q1Condition(const std::array<V3, 4>& quad, V3 normal);
}
