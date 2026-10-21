// PrePoMax for macOS - NetGenMesher: meshing of CAD shapes with gmsh (its 3D algorithm is the one of Netgen) into the
// Netgen VOL format PrePoMax reads. The surfaces and edges of the mesh carry the face and edge numbers of the shape,
// the same numbers the visualization (BREP_VISUALIZATION) uses.
//
//   BREP_MESH <brep> <vol> <mesh parameters> <local mesh sizes>
//   BREP_EDGE_MESH <brep> <vol> <mesh parameters> <local mesh sizes>
//   VOL_VOLUME_MESH <vol> <out vol> <mesh parameters>   the cells bounded by surface elements filled with tetrahedra
//       the mesh parameters may name a transfinite file (transfinite_filename): its faces are meshed as structured grids
//       and its edges get the numbers of elements set on them
#include "Common.h"

#include <BRepAdaptor_Surface.hxx>
#include <BRepBndLib.hxx>
#include <BRepGProp.hxx>
#include <Bnd_Box.hxx>
#include <TopoDS_Vertex.hxx>
#include <BRepLProp_SLProps.hxx>
#include <BRep_Tool.hxx>
#include <GProp_GProps.hxx>
#include <GeomAPI_ProjectPointOnSurf.hxx>
#include <TopExp.hxx>
#include <TopoDS.hxx>

#include <gmsh.h>
#include <unistd.h>
#include <sys/wait.h>
#include <iostream>
#include <functional>

#include <algorithm>
#include <array>
#include <cctype>
#include <cmath>
#include <fstream>
#include <limits>
#include <map>
#include <set>
#include <sstream>

namespace ngm
{
    namespace
    {
        struct LocalSizes
        {
            std::vector<std::array<double, 4>> points;          // x y z h
            std::vector<std::array<double, 7>> lines;           // x1 y1 z1 x2 y2 z2 h
        };
        LocalSizes ReadLocalSizes(const std::string& fileName)
        {
            LocalSizes sizes;
            std::ifstream file(fileName);
            if (!file) return sizes;
            int n = 0;
            if (file >> n)
            {
                for (int i = 0; i < n; i++)
                {
                    std::array<double, 4> p;
                    if (!(file >> p[0] >> p[1] >> p[2] >> p[3])) return sizes;
                    sizes.points.push_back(p);
                }
            }
            if (file >> n)
            {
                for (int i = 0; i < n; i++)
                {
                    std::array<double, 7> l;
                    if (!(file >> l[0] >> l[1] >> l[2] >> l[3] >> l[4] >> l[5] >> l[6])) return sizes;
                    sizes.lines.push_back(l);
                }
            }
            return sizes;
        }
        double DistanceToSegment(const double* p, const double* a, const double* b)
        {
            double ab[3] = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double ap[3] = { p[0] - a[0], p[1] - a[1], p[2] - a[2] };
            double l2 = ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2];
            double t = l2 > 0 ? (ab[0] * ap[0] + ab[1] * ap[1] + ab[2] * ap[2]) / l2 : 0;
            t = std::max(0.0, std::min(1.0, t));
            double d[3] = { ap[0] - t * ab[0], ap[1] - t * ab[1], ap[2] - t * ab[2] };
            return std::sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]);
        }

        struct EntityProps { double mass; gp_Pnt center; };
        EntityProps OccProps(const TopoDS_Shape& shape, int dim)
        {
            GProp_GProps props;
            if (dim == 2) BRepGProp::SurfaceProperties(shape, props);
            else BRepGProp::LinearProperties(shape, props);
            return { props.Mass(), props.CentreOfMass() };
        }
        // The gmsh entities of a dimension matched to the shapes of the map by their centres of mass and sizes
        std::map<int, int> MatchEntities(int dim, const TopTools_IndexedMapOfShape& map)
        {
            std::map<int, int> tagToId;
            gmsh::vectorpair entities;
            gmsh::model::getEntities(entities, dim);
            std::vector<EntityProps> occ;
            double scale = 0;
            for (int i = 1; i <= map.Extent(); i++)
            {
                bool degenerated = dim == 1 && BRep_Tool::Degenerated(TopoDS::Edge(map(i)));
                occ.push_back(degenerated ? EntityProps{ -1, gp_Pnt() } : OccProps(map(i), dim));
                scale = std::max(scale, std::fabs(occ.back().mass));
            }
            std::set<int> used;
            for (auto& entity : entities)
            {
                double mass = 0;
                std::vector<double> center(3, 0.0);
                try
                {
                    gmsh::model::occ::getMass(dim, entity.second, mass);
                    gmsh::model::occ::getCenterOfMass(dim, entity.second, center[0], center[1], center[2]);
                }
                catch (...) { continue; }
                gp_Pnt c(center[0], center[1], center[2]);
                int best = -1;
                double bestScore = std::numeric_limits<double>::max();
                for (size_t i = 0; i < occ.size(); i++)
                {
                    if (occ[i].mass < 0) continue;
                    double score = occ[i].center.Distance(c) + std::fabs(occ[i].mass - mass) / std::max(1e-12, scale);
                    if (used.count((int)i + 1)) score += 1e6;
                    if (score < bestScore) { bestScore = score; best = (int)i + 1; }
                }
                if (best > 0)
                {
                    tagToId[entity.second] = best;
                    used.insert(best);
                }
            }
            return tagToId;
        }

        // The structured faces of the transfinite file PrePoMax writes: every face by its kernel number with its sides as
        // chains of edges in loop order, and the numbers of elements set on edges (0 = free)
        struct TransfiniteFace
        {
            int face = 0;
            int numOfSides = 0;
            int polar = -1;
            std::vector<std::vector<int>> sides;
        };
        struct TransfiniteData
        {
            std::vector<TransfiniteFace> faces;
            std::map<int, int> edgeCounts;
        };
        TransfiniteData ReadTransfinite(const std::string& fileName)
        {
            TransfiniteData data;
            std::ifstream file(fileName);
            std::string line;
            while (std::getline(file, line))
            {
                std::istringstream in(line);
                std::string key;
                in >> key;
                if (key == "face")
                {
                    TransfiniteFace face;
                    in >> face.face >> face.numOfSides;
                    std::string word;
                    if (in >> word && word == "polar") in >> face.polar;
                    data.faces.push_back(face);
                }
                else if (key == "side" && !data.faces.empty())
                {
                    int n;
                    in >> n;
                    std::vector<int> side(std::max(0, n));
                    for (int& id : side) in >> id;
                    data.faces.back().sides.push_back(side);
                }
                else if (key == "edge")
                {
                    int id, count;
                    if (in >> id >> count) data.edgeCounts[id] = count;
                }
            }
            return data;
        }
        // A three-sided face meshed as three structured blocks: its corners (vertex tags) and its sides as curves
        struct MappedFace
        {
            int surface = 0;
            std::vector<int> corners;
            std::vector<std::vector<int>> sides;
        };

        // The edge counts of the structured faces settled so that the opposite sides of every face have the same number of
        // elements; the counts set are kept and the free edges are moved. Faces that cannot agree are dropped. A three-sided
        // face that is not polar is meshed as three blocks, whose sides must have an even number of elements together
        bool ApplyTransfinite(const TransfiniteData& data, const ShapeMaps& maps, double size, bool recombine,
                              std::vector<MappedFace>& mappedFaces)
        {
            std::map<int, int> faceTags, edgeTags, vertexTags;
            for (auto& entry : MatchEntities(2, maps.faces)) faceTags[entry.second] = entry.first;
            for (auto& entry : MatchEntities(1, maps.edges)) edgeTags[entry.second] = entry.first;
            gmsh::vectorpair points;
            gmsh::model::getEntities(points, 0);
            for (auto& point : points)
            {
                std::vector<double> xyz;
                gmsh::model::getValue(0, point.second, {}, xyz);
                if (xyz.size() < 3) continue;
                gp_Pnt p(xyz[0], xyz[1], xyz[2]);
                int best = -1;
                double bestDistance = std::numeric_limits<double>::max();
                for (int i = 1; i <= maps.vertices.Extent(); i++)
                {
                    double d = BRep_Tool::Pnt(TopoDS::Vertex(maps.vertices(i))).Distance(p);
                    if (d < bestDistance) { bestDistance = d; best = i; }
                }
                if (best > 0) vertexTags[best] = point.second;
            }
            //
            std::map<int, int> counts;
            std::set<int> fixed;
            std::map<int, double> lengths;
            auto length = [&](int edge) {
                auto it = lengths.find(edge);
                if (it != lengths.end()) return it->second;
                double l = 0;
                if (edge >= 1 && edge <= maps.edges.Extent())
                {
                    GProp_GProps props;
                    BRepGProp::LinearProperties(maps.edges(edge), props);
                    l = props.Mass();
                }
                lengths[edge] = l;
                return l;
            };
            for (auto& entry : data.edgeCounts)
            {
                if (entry.second > 0) { counts[entry.first] = entry.second; fixed.insert(entry.first); }
                else counts[entry.first] = std::max(1, (int)std::lround(length(entry.first) / size));
            }
            std::vector<const TransfiniteFace*> faces;
            for (auto& face : data.faces)
            {
                if ((int)face.sides.size() != face.numOfSides) continue;
                if (face.numOfSides == 4 || face.numOfSides == 3) faces.push_back(&face);
            }
            for (auto* face : faces)
                for (auto& side : face->sides)
                    for (int edge : side)
                        if (!counts.count(edge)) counts[edge] = std::max(1, (int)std::lround(length(edge) / size));
            // The pairs of sides that must agree
            std::vector<std::pair<std::vector<int>, std::vector<int>>> pairs;
            std::vector<int> pairFace;
            for (size_t f = 0; f < faces.size(); f++)
            {
                auto& sides = faces[f]->sides;
                if (faces[f]->numOfSides == 4)
                {
                    pairs.push_back({ sides[0], sides[2] }); pairFace.push_back((int)f);
                    pairs.push_back({ sides[1], sides[3] }); pairFace.push_back((int)f);
                }
                else if (faces[f]->polar >= 0)
                {
                    int p = faces[f]->polar;
                    pairs.push_back({ sides[(p + 2) % 3], sides[p] }); pairFace.push_back((int)f);
                }
            }
            // The three-block faces: every side shorter than the other two together, the three together even
            std::vector<int> triples;
            for (size_t f = 0; f < faces.size(); f++)
                if (faces[f]->numOfSides == 3 && faces[f]->polar < 0) triples.push_back((int)f);
            auto sum = [&](const std::vector<int>& side) { int n = 0; for (int e : side) n += counts[e]; return n; };
            std::set<int> dropped;
            for (int iteration = 0; iteration < 200; iteration++)
            {
                bool changed = false;
                for (size_t i = 0; i < pairs.size(); i++)
                {
                    if (dropped.count(pairFace[i])) continue;
                    int a = sum(pairs[i].first), b = sum(pairs[i].second);
                    if (a == b) continue;
                    const std::vector<int>& smaller = a < b ? pairs[i].first : pairs[i].second;
                    const std::vector<int>& larger = a < b ? pairs[i].second : pairs[i].first;
                    int difference = std::abs(a - b);
                    // The free edge with the longest elements of the smaller side takes the difference
                    int best = -1;
                    double bestRatio = -1;
                    for (int e : smaller)
                        if (!fixed.count(e) && length(e) / counts[e] > bestRatio) { bestRatio = length(e) / counts[e]; best = e; }
                    if (best > 0) { counts[best] += difference; changed = true; continue; }
                    // Else the free edge with the shortest elements of the larger side gives it up
                    double worst = std::numeric_limits<double>::max();
                    for (int e : larger)
                        if (!fixed.count(e) && counts[e] - difference >= 1 && length(e) / counts[e] < worst)
                        { worst = length(e) / counts[e]; best = e; }
                    if (best > 0) { counts[best] -= difference; changed = true; continue; }
                    dropped.insert(pairFace[i]);
                }
                for (int f : triples)
                {
                    if (dropped.count(f)) continue;
                    auto& sides = faces[f]->sides;
                    int total = sum(sides[0]) + sum(sides[1]) + sum(sides[2]);
                    int longest = 0;
                    for (int k = 1; k < 3; k++) if (sum(sides[k]) > sum(sides[longest])) longest = k;
                    // The longest side must be shorter than the other two together, and the sum even: the other two
                    // sides take the elements needed
                    int excess = 2 * sum(sides[longest]) - total;
                    int needed = excess >= 0 ? excess + 1 : 0;
                    if ((total + needed) % 2) needed++;
                    if (needed == 0) continue;
                    int best = -1;
                    double bestRatio = -1;
                    for (int k = 0; k < 3; k++)
                    {
                        if (k == longest) continue;
                        for (int e : sides[k])
                            if (!fixed.count(e) && length(e) / counts[e] > bestRatio) { bestRatio = length(e) / counts[e]; best = e; }
                    }
                    if (best < 0) { dropped.insert(f); continue; }
                    counts[best] += needed;
                    changed = true;
                }
                if (!changed) break;
            }
            for (int f : triples)
            {
                auto& sides = faces[f]->sides;
                int a = sum(sides[0]), b = sum(sides[1]), c = sum(sides[2]);
                if ((a + b + c) % 2 != 0 || 2 * std::max(a, std::max(b, c)) >= a + b + c) dropped.insert(f);
            }
            // Opposite sides whose numbers of elements are all set and differ cannot be meshed as a structured grid
            for (size_t i = 0; i < pairs.size(); i++)
            {
                if (sum(pairs[i].first) == sum(pairs[i].second)) continue;
                bool allFixed = true;
                for (int e : pairs[i].first) allFixed &= fixed.count(e) > 0;
                for (int e : pairs[i].second) allFixed &= fixed.count(e) > 0;
                if (allFixed)
                {
                    Error("The opposite sides of the structured face " + std::to_string(faces[pairFace[i]]->face) +
                          " are set to " + std::to_string(sum(pairs[i].first)) + " and " +
                          std::to_string(sum(pairs[i].second)) + " elements, which a structured grid cannot join.");
                    return false;
                }
                dropped.insert(pairFace[i]);
            }
            //
            for (auto& entry : counts)
            {
                auto tag = edgeTags.find(entry.first);
                if (tag != edgeTags.end()) gmsh::model::mesh::setTransfiniteCurve(tag->second, entry.second + 1);
            }
            int numOfFaces = 0;
            for (size_t f = 0; f < faces.size(); f++)
            {
                if (dropped.count((int)f)) continue;
                const TransfiniteFace& face = *faces[f];
                auto faceTag = faceTags.find(face.face);
                if (faceTag == faceTags.end()) continue;
                // The corners: the vertex every side starts at, the one it shares with the side before it
                std::vector<int> corners;
                int n = (int)face.sides.size();
                for (int i = 0; i < n; i++)
                {
                    const std::vector<int>& previous = face.sides[(i + n - 1) % n];
                    const std::vector<int>& current = face.sides[i];
                    if (previous.empty() || current.empty()) break;
                    int e1 = previous.back(), e2 = current.front();
                    if (e1 < 1 || e2 < 1 || e1 > maps.edges.Extent() || e2 > maps.edges.Extent()) break;
                    TopoDS_Vertex vertex;
                    if (!TopExp::CommonVertex(TopoDS::Edge(maps.edges(e1)), TopoDS::Edge(maps.edges(e2)), vertex)) break;
                    int id = maps.vertices.FindIndex(vertex);
                    auto tag = vertexTags.find(id);
                    if (tag == vertexTags.end()) break;
                    corners.push_back(tag->second);
                }
                if ((int)corners.size() != n) continue;
                if (n == 3 && face.polar < 0)
                {
                    MappedFace mapped;
                    mapped.surface = faceTag->second;
                    mapped.corners = corners;
                    bool complete = true;
                    for (auto& side : face.sides)
                    {
                        std::vector<int> curves;
                        for (int e : side)
                        {
                            auto tag = edgeTags.find(e);
                            if (tag == edgeTags.end()) { complete = false; break; }
                            curves.push_back(tag->second);
                        }
                        mapped.sides.push_back(curves);
                    }
                    if (complete) { mappedFaces.push_back(mapped); numOfFaces++; }
                    continue;
                }
                // The collapsed corner of a polar face comes first
                if (face.polar > 0) std::rotate(corners.begin(), corners.begin() + face.polar, corners.end());
                gmsh::model::mesh::setTransfiniteSurface(faceTag->second, "Left", corners);
                if (recombine) gmsh::model::mesh::setRecombine(2, faceTag->second);
                numOfFaces++;
            }
            Log("Structured faces: " + std::to_string(numOfFaces) + " of " + std::to_string(data.faces.size()) + ".");
            return true;
        }

        // The nodes of a side of a face from its first corner on, the corners included: the vertex nodes and the nodes of
        // its curves in the order of the curve parameter, every curve walked the way the side runs
        bool SideNodes(const std::vector<int>& curves, int startVertex, std::vector<std::size_t>& tags,
                       std::vector<std::array<double, 3>>& points, int& endVertex)
        {
            auto vertexNode = [&](int vertex, std::size_t& tag, std::array<double, 3>& point) {
                std::vector<std::size_t> nodeTags;
                std::vector<double> coord, param;
                gmsh::model::mesh::getNodes(nodeTags, coord, param, 0, vertex, false, false);
                if (nodeTags.empty() || coord.size() < 3) return false;
                tag = nodeTags[0];
                point = { coord[0], coord[1], coord[2] };
                return true;
            };
            int current = startVertex;
            std::size_t tag;
            std::array<double, 3> point;
            if (!vertexNode(current, tag, point)) return false;
            tags.push_back(tag);
            points.push_back(point);
            for (int curve : curves)
            {
                gmsh::vectorpair ends;
                gmsh::model::getBoundary({ { 1, curve } }, ends, false, false, false);
                if (ends.size() != 2) return false;
                int first = std::abs(ends[0].second), last = std::abs(ends[1].second);
                bool forward = first == current;
                if (!forward && last != current) return false;
                std::vector<std::size_t> nodeTags;
                std::vector<double> coord, param;
                gmsh::model::mesh::getNodes(nodeTags, coord, param, 1, curve, false, true);
                std::vector<std::size_t> order(nodeTags.size());
                for (std::size_t i = 0; i < order.size(); i++) order[i] = i;
                std::sort(order.begin(), order.end(), [&](std::size_t x, std::size_t y) { return param[x] < param[y]; });
                if (!forward) std::reverse(order.begin(), order.end());
                for (std::size_t i : order)
                {
                    tags.push_back(nodeTags[i]);
                    points.push_back({ coord[3 * i], coord[3 * i + 1], coord[3 * i + 2] });
                }
                current = forward ? last : first;
                if (!vertexNode(current, tag, point)) return false;
                tags.push_back(tag);
                points.push_back(point);
            }
            endVertex = current;
            return true;
        }
        // The three-sided faces meshed as three structured blocks, on the mesh of their curves: a node on every side
        // splits it, the three are joined to a centre node, and every block is filled by transfinite interpolation
        // between its sides, its nodes put onto the face. gmsh then meshes only the faces left empty
        void BuildMappedFaces(const std::vector<MappedFace>& mappedFaces, bool recombine)
        {
            std::size_t nextTag = 0;
            gmsh::model::mesh::getMaxNodeTag(nextTag);
            nextTag++;
            for (const MappedFace& face : mappedFaces)
            {
                // The sides from their corners: side k runs from corner k to corner k + 1
                std::vector<std::vector<std::size_t>> sideTags(3);
                std::vector<std::vector<std::array<double, 3>>> sidePoints(3);
                bool valid = true;
                for (int k = 0; k < 3 && valid; k++)
                {
                    int end = 0;
                    valid = SideNodes(face.sides[k], face.corners[k], sideTags[k], sidePoints[k], end) &&
                            end == face.corners[(k + 1) % 3];
                }
                if (!valid) { Log("A three-sided face could not be meshed as three blocks."); continue; }
                // Side k has n[k] segments; with A, B, C the corners 0, 1, 2 the sides are c = AB, a = BC, b = CA
                int c = (int)sideTags[0].size() - 1, a = (int)sideTags[1].size() - 1, b = (int)sideTags[2].size() - 1;
                int sa = (b + c - a) / 2, sb = (a + c - b) / 2, sc = (a + b - c) / 2;
                if (sa < 1 || sb < 1 || sc < 1 || (a + b + c) % 2) { Log("A three-sided face cannot be split into blocks."); continue; }
                // The split nodes: on AB after sb segments from A, on BC after sc from B, on CA after sa from C
                int mc = sb, ma = sc, mb = sa;
                std::vector<std::size_t> newTags;
                std::vector<double> newCoord, newParam;
                auto onFace = [&](const std::array<double, 3>& p, std::size_t& tag) {
                    std::vector<double> closest, uv;
                    gmsh::model::getClosestPoint(2, face.surface, { p[0], p[1], p[2] }, closest, uv);
                    tag = nextTag++;
                    newTags.push_back(tag);
                    newCoord.insert(newCoord.end(), closest.begin(), closest.begin() + 3);
                    newParam.insert(newParam.end(), uv.begin(), uv.begin() + 2);
                    return std::array<double, 3>{ closest[0], closest[1], closest[2] };
                };
                std::array<double, 3> Mc = sidePoints[0][mc], Ma = sidePoints[1][ma], Mb = sidePoints[2][mb];
                std::array<double, 3> center;
                for (int i = 0; i < 3; i++) center[i] = (Ma[i] + Mb[i] + Mc[i]) / 3;
                std::size_t centerTag;
                center = onFace(center, centerTag);
                // The spokes from the split nodes to the centre, the centre last
                auto spoke = [&](const std::array<double, 3>& from, std::size_t fromTag, int segments,
                                 std::vector<std::size_t>& tags, std::vector<std::array<double, 3>>& points) {
                    tags = { fromTag };
                    points = { from };
                    for (int i = 1; i < segments; i++)
                    {
                        double t = (double)i / segments;
                        std::array<double, 3> p;
                        for (int k = 0; k < 3; k++) p[k] = from[k] + t * (center[k] - from[k]);
                        std::size_t tag;
                        points.push_back(onFace(p, tag));
                        tags.push_back(tag);
                    }
                    tags.push_back(centerTag);
                    points.push_back(center);
                };
                std::vector<std::size_t> spokeATags, spokeBTags, spokeCTags;
                std::vector<std::array<double, 3>> spokeA, spokeB, spokeC;
                spoke(Ma, sideTags[1][ma], sa, spokeATags, spokeA);
                spoke(Mb, sideTags[2][mb], sb, spokeBTags, spokeB);
                spoke(Mc, sideTags[0][mc], sc, spokeCTags, spokeC);
                // A block from its four sides: bottom and top of nu segments, left and right of nv segments, all running
                // from the corner at their start; the grid nodes in rows
                std::vector<std::size_t> elements;
                auto block = [&](std::vector<std::size_t> bottomT, std::vector<std::array<double, 3>> bottomP,
                                 std::vector<std::size_t> topT, std::vector<std::array<double, 3>> topP,
                                 std::vector<std::size_t> leftT, std::vector<std::array<double, 3>> leftP,
                                 std::vector<std::size_t> rightT, std::vector<std::array<double, 3>> rightP) {
                    int nu = (int)bottomT.size() - 1, nv = (int)leftT.size() - 1;
                    std::vector<std::vector<std::size_t>> grid(nv + 1, std::vector<std::size_t>(nu + 1));
                    for (int i = 0; i <= nu; i++) { grid[0][i] = bottomT[i]; grid[nv][i] = topT[i]; }
                    for (int j = 0; j <= nv; j++) { grid[j][0] = leftT[j]; grid[j][nu] = rightT[j]; }
                    for (int j = 1; j < nv; j++)
                        for (int i = 1; i < nu; i++)
                        {
                            double u = (double)i / nu, v = (double)j / nv;
                            std::array<double, 3> p;
                            for (int k = 0; k < 3; k++)
                                p[k] = (1 - v) * bottomP[i][k] + v * topP[i][k] + (1 - u) * leftP[j][k] + u * rightP[j][k] -
                                       ((1 - u) * (1 - v) * bottomP[0][k] + u * (1 - v) * bottomP[nu][k] +
                                        (1 - u) * v * topP[0][k] + u * v * topP[nu][k]);
                            std::size_t tag;
                            onFace(p, tag);
                            grid[j][i] = tag;
                        }
                    for (int j = 0; j < nv; j++)
                        for (int i = 0; i < nu; i++)
                        {
                            std::size_t q[4] = { grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i] };
                            elements.insert(elements.end(), q, q + 4);
                        }
                };
                auto part = [](const std::vector<std::size_t>& tags, int from, int to) {
                    std::vector<std::size_t> r;
                    if (from <= to) for (int i = from; i <= to; i++) r.push_back(tags[i]);
                    else for (int i = from; i >= to; i--) r.push_back(tags[i]);
                    return r;
                };
                auto partP = [](const std::vector<std::array<double, 3>>& points, int from, int to) {
                    std::vector<std::array<double, 3>> r;
                    if (from <= to) for (int i = from; i <= to; i++) r.push_back(points[i]);
                    else for (int i = from; i >= to; i--) r.push_back(points[i]);
                    return r;
                };
                auto& T0 = sideTags[0]; auto& T1 = sideTags[1]; auto& T2 = sideTags[2];
                auto& P0 = sidePoints[0]; auto& P1 = sidePoints[1]; auto& P2 = sidePoints[2];
                // Block A: bottom A -> Mc (side 0), top Mb -> centre (spoke B), left A -> Mb (side 2 backwards),
                // right Mc -> centre (spoke C)
                block(part(T0, 0, mc), partP(P0, 0, mc), spokeBTags, spokeB,
                      part(T2, b, mb), partP(P2, b, mb), spokeCTags, spokeC);
                // Block B: bottom B -> Ma (side 1), top Mc -> centre, left B -> Mc (side 0 backwards), right Ma -> centre
                block(part(T1, 0, ma), partP(P1, 0, ma), spokeCTags, spokeC,
                      part(T0, c, mc), partP(P0, c, mc), spokeATags, spokeA);
                // Block C: bottom C -> Mb (side 2), top Ma -> centre, left C -> Ma (side 1 backwards), right Mb -> centre
                block(part(T2, 0, mb), partP(P2, 0, mb), spokeATags, spokeA,
                      part(T1, a, ma), partP(P1, a, ma), spokeBTags, spokeB);
                gmsh::model::mesh::addNodes(2, face.surface, newTags, newCoord, newParam);
                // The quadrilaterals face the way the surface does
                std::vector<double> normal, uv = { newParam[0], newParam[1] };
                gmsh::model::getNormal(face.surface, uv, normal);
                std::map<std::size_t, std::array<double, 3>> position;
                for (std::size_t i = 0; i < newTags.size(); i++)
                    position[newTags[i]] = { newCoord[3 * i], newCoord[3 * i + 1], newCoord[3 * i + 2] };
                for (int k = 0; k < 3; k++)
                    for (std::size_t i = 0; i < sideTags[k].size(); i++) position[sideTags[k][i]] = sidePoints[k][i];
                auto& q0 = position[elements[0]]; auto& q1 = position[elements[1]]; auto& q2 = position[elements[2]];
                double e1[3] = { q1[0] - q0[0], q1[1] - q0[1], q1[2] - q0[2] }, e2[3] = { q2[0] - q0[0], q2[1] - q0[1], q2[2] - q0[2] };
                double n[3] = { e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0] };
                bool flip = normal.size() >= 3 && n[0] * normal[0] + n[1] * normal[1] + n[2] * normal[2] < 0;
                if (flip)
                    for (std::size_t e = 0; e < elements.size(); e += 4) std::swap(elements[e + 1], elements[e + 3]);
                if (recombine) gmsh::model::mesh::addElementsByType(face.surface, 3, {}, elements);
                else
                {
                    std::vector<std::size_t> triangles;
                    for (std::size_t e = 0; e < elements.size(); e += 4)
                    {
                        std::size_t t[6] = { elements[e], elements[e + 1], elements[e + 2],
                                             elements[e], elements[e + 2], elements[e + 3] };
                        triangles.insert(triangles.end(), t, t + 6);
                    }
                    gmsh::model::mesh::addElementsByType(face.surface, 2, {}, triangles);
                }
            }
            if (!mappedFaces.empty()) gmsh::option::setNumber("Mesh.MeshOnlyEmpty", 1);
        }

        // Generates the mesh and runs what follows on it in a child process when quadrilaterals are recombined: the
        // blossom recombination of gmsh crashes on some faces, and the mesh is then made again by the simple
        // recombination. Returns the status of the run that finished.
        int GenerateWithRecombinationFallback(int dimension, bool recombine, const std::function<void()>& prepare,
                                              const std::function<int()>& finish)
        {
            if (!recombine)
            {
                prepare();
                gmsh::model::mesh::generate(dimension);
                return finish();
            }
            std::cout.flush();
            std::cerr.flush();
            pid_t child = fork();
            if (child == 0)
            {
                int status = 1;
                try
                {
                    prepare();
                    gmsh::model::mesh::generate(dimension);
                    status = finish();
                }
                catch (const std::exception& ex) { Error(std::string("Meshing failed: ") + ex.what()); }
                catch (...) { Error("Meshing failed."); }
                std::cout.flush();
                std::cerr.flush();
                _exit(status);
            }
            if (child > 0)
            {
                int childStatus = 0;
                waitpid(child, &childStatus, 0);
                if (WIFEXITED(childStatus)) return WEXITSTATUS(childStatus);
                Log("The blossom recombination failed; the quadrilaterals are recombined by the simple algorithm.");
            }
            // The child meshed a copy of the model, which in this process is still unmeshed
            gmsh::option::setNumber("Mesh.RecombinationAlgorithm", 0);
            prepare();
            gmsh::model::mesh::generate(dimension);
            return finish();
        }

        // gmsh element node orders (equal to Abaqus) turned into the Netgen orders PrePoMax reads back
        std::vector<size_t> SurfaceOrder(int type, const size_t* n)
        {
            switch (type)
            {
                case 2: return { n[0], n[1], n[2] };                                       // tri3
                case 3: return { n[0], n[1], n[2], n[3] };                                 // quad4
                case 9: return { n[0], n[1], n[2], n[4], n[5], n[3] };                     // tri6
                case 16: return { n[0], n[1], n[2], n[3], n[4], n[7], n[5], n[6] };        // quad8
                default: return {};
            }
        }
        std::vector<size_t> VolumeOrder(int type, const size_t* n)
        {
            switch (type)
            {
                case 4: return { n[0], n[2], n[1], n[3] };                                 // tet4
                case 11:                                                                   // tet10: gmsh 8 = (2,3), 9 = (1,3)
                    {
                        size_t a[10] = { n[0], n[1], n[2], n[3], n[4], n[5], n[6], n[7], n[9], n[8] };
                        return { a[0], a[2], a[1], a[3], a[6], a[4], a[7], a[5], a[9], a[8] };
                    }
                case 6: return { n[0], n[2], n[1], n[3], n[5], n[4] };                     // prism6
                default: return {};
            }
        }
        int NodesOfType(int type)
        {
            switch (type)
            {
                case 1: return 2; case 8: return 3;
                case 2: return 3; case 3: return 4; case 9: return 6; case 16: return 8; case 10: return 9;
                case 4: return 4; case 11: return 10; case 6: return 6; case 5: return 8; case 7: return 5;
                default: return 0;
            }
        }

        // The mesh of the gmsh model in the Netgen VOL format; the faces and edges carry the numbers of the shape when
        // a shape is given (the surface elements are then turned to the side of their face)
        int WriteVol(const std::string& fileName, const ShapeMaps* maps, const TopoDS_Shape* shape,
                     std::map<int, int>& faceIds, std::map<int, int>& edgeIds, bool onlyEdges)
        {
        // Nodes
        std::vector<size_t> nodeTags;
        std::vector<double> coordinates, parametric;
        gmsh::model::mesh::getNodes(nodeTags, coordinates, parametric, -1, -1, false, false);
        std::map<size_t, size_t> nodeIndex;
        for (size_t i = 0; i < nodeTags.size(); i++) nodeIndex[nodeTags[i]] = i + 1;
        auto index = [&](size_t tag) { return nodeIndex[tag]; };
        //
        std::ostringstream surface, volume, edges;
        size_t numSurface = 0, numVolume = 0, numEdges = 0;
        // Surface elements, turned to the side of their face
        gmsh::vectorpair faces;
        gmsh::model::getEntities(faces, 2);
        for (auto& face : faces)
        {
            int faceId = faceIds.count(face.second) ? faceIds[face.second] : face.second;
            std::vector<int> types;
            std::vector<std::vector<size_t>> tags, nodes;
            gmsh::model::mesh::getElements(types, tags, nodes, 2, face.second);
            bool flip = false;
            if (!onlyEdges && faceId >= 1 && faceId <= (maps ? maps->faces.Extent() : 0))
            {
                // The normal of the first element against the normal of the face
                for (size_t t = 0; t < types.size() && !flip; t++)
                {
                    int nn = NodesOfType(types[t]);
                    if (nn == 0 || nodes[t].size() < (size_t)nn) continue;
                    const size_t* n = &nodes[t][0];
                    double c[3][3];
                    for (int k = 0; k < 3; k++)
                    {
                        size_t i = nodeIndex[n[k]] - 1;
                        c[k][0] = coordinates[3 * i]; c[k][1] = coordinates[3 * i + 1]; c[k][2] = coordinates[3 * i + 2];
                    }
                    gp_Vec e1(c[1][0] - c[0][0], c[1][1] - c[0][1], c[1][2] - c[0][2]);
                    gp_Vec e2(c[2][0] - c[0][0], c[2][1] - c[0][1], c[2][2] - c[0][2]);
                    gp_Vec normal = e1.Crossed(e2);
                    gp_Pnt centre((c[0][0] + c[1][0] + c[2][0]) / 3, (c[0][1] + c[1][1] + c[2][1]) / 3,
                                  (c[0][2] + c[1][2] + c[2][2]) / 3);
                    TopoDS_Face occFace = TopoDS::Face(maps->faces(faceId));
                    Handle(Geom_Surface) geomSurface = BRep_Tool::Surface(occFace);
                    GeomAPI_ProjectPointOnSurf projector(centre, geomSurface);
                    if (projector.NbPoints() == 0) break;
                    double u, v;
                    projector.LowerDistanceParameters(u, v);
                    BRepAdaptor_Surface adaptor(occFace, false);
                    BRepLProp_SLProps props(adaptor, u, v, 1, 1e-9);
                    if (!props.IsNormalDefined()) break;
                    gp_Dir faceNormal = props.Normal();
                    if (occFace.Orientation() == TopAbs_REVERSED) faceNormal.Reverse();
                    flip = normal.Dot(gp_Vec(faceNormal)) < 0;
                    break;
                }
            }
            for (size_t t = 0; t < types.size(); t++)
            {
                int nn = NodesOfType(types[t]);
                if (nn == 0) continue;
                for (size_t e = 0; e + nn <= nodes[t].size(); e += nn)
                {
                    std::vector<size_t> a(nodes[t].begin() + e, nodes[t].begin() + e + nn);
                    if (flip)
                    {
                        // The same element seen from the other side, in the gmsh (Abaqus) order
                        std::vector<size_t> o = a;
                        if (nn == 3) a = { o[0], o[2], o[1] };
                        else if (nn == 4) a = { o[0], o[3], o[2], o[1] };
                        else if (nn == 6) a = { o[0], o[2], o[1], o[5], o[4], o[3] };
                        else if (nn == 8) a = { o[0], o[3], o[2], o[1], o[7], o[6], o[5], o[4] };
                    }
                    std::vector<size_t> order = SurfaceOrder(types[t], a.data());
                    if (order.empty()) continue;
                    surface << faceId << " " << faceId << " 1 0 " << order.size();
                    for (size_t node : order) surface << " " << index(node);
                    surface << "\n";
                    numSurface++;
                }
            }
        }
        // Volume elements
        gmsh::vectorpair solids;
        gmsh::model::getEntities(solids, 3);
        int material = 0;
        for (auto& solid : solids)
        {
            material++;
            std::vector<int> types;
            std::vector<std::vector<size_t>> tags, nodes;
            gmsh::model::mesh::getElements(types, tags, nodes, 3, solid.second);
            for (size_t t = 0; t < types.size(); t++)
            {
                int nn = NodesOfType(types[t]);
                std::vector<size_t> probe = nn > 0 && nodes[t].size() >= (size_t)nn ?
                                            VolumeOrder(types[t], &nodes[t][0]) : std::vector<size_t>();
                if (probe.empty())
                {
                    if (!nodes[t].empty()) Log("Warning: volume elements of an unsupported type were skipped.");
                    continue;
                }
                for (size_t e = 0; e + nn <= nodes[t].size(); e += nn)
                {
                    std::vector<size_t> order = VolumeOrder(types[t], &nodes[t][e]);
                    volume << material << " " << order.size();
                    for (size_t node : order) volume << " " << index(node);
                    volume << "\n";
                    numVolume++;
                }
            }
        }
        // Edge segments with the parameters of their nodes along the edge
        gmsh::vectorpair curves;
        gmsh::model::getEntities(curves, 1);
        TopTools_IndexedDataMapOfShapeListOfShape edgeFaces;
        if (shape) TopExp::MapShapesAndAncestors(*shape, TopAbs_EDGE, TopAbs_FACE, edgeFaces);
        for (auto& curve : curves)
        {
            int edgeId = edgeIds.count(curve.second) ? edgeIds[curve.second] : curve.second;
            int faceId = 0;
            if (edgeId >= 1 && edgeId <= (maps ? maps->edges.Extent() : 0))
            {
                int k = edgeFaces.FindIndex(maps->edges(edgeId));
                if (k > 0 && !edgeFaces(k).IsEmpty()) faceId = maps->faces.FindIndex(edgeFaces(k).First());
            }
            std::vector<size_t> curveNodes;
            std::vector<double> curveCoordinates, curveParameters;
            gmsh::model::mesh::getNodes(curveNodes, curveCoordinates, curveParameters, 1, curve.second, true, true);
            std::map<size_t, double> parameter;
            for (size_t i = 0; i < curveNodes.size() && i < curveParameters.size(); i++)
                parameter[curveNodes[i]] = curveParameters[i];
            std::vector<int> types;
            std::vector<std::vector<size_t>> tags, nodes;
            gmsh::model::mesh::getElements(types, tags, nodes, 1, curve.second);
            for (size_t t = 0; t < types.size(); t++)
            {
                int nn = NodesOfType(types[t]);
                if (nn == 0) continue;
                for (size_t e = 0; e + nn <= nodes[t].size(); e += nn)
                {
                    size_t a = nodes[t][e], b = nodes[t][e + 1];
                    edges << faceId << " 0 " << index(a) << " " << index(b) << " -1 -1 0 0 " << edgeId << " "
                          << Num(parameter.count(a) ? parameter[a] : 0) << " " << edgeId << " "
                          << Num(parameter.count(b) ? parameter[b] : 0) << "\n";
                    numEdges++;
                }
            }
        }
        //
        std::ofstream out(fileName);
        out << "mesh3d\n";
        out << "dimension\n3\n";
        out << "geomtype\n11\n";
        out << "\n# surfnr    bcnr   domin  domout      np      p1      p2      p3\n";
        out << "surfaceelements\n" << numSurface << "\n" << surface.str();
        out << "\n#  matnr      np      p1      p2      p3      p4\n";
        out << "volumeelements\n" << numVolume << "\n" << volume.str();
        out << "\n# surfid  0   p1   p2   trignum1    trignum2   domin/surfnr1    domout/surfnr2   ednr1   dist1   ednr2   dist2\n";
        out << "edgesegmentsgi2\n" << numEdges << "\n" << edges.str();
        out << "\n#          X             Y             Z\n";
        out << "points\n" << nodeTags.size() << "\n";
        for (size_t i = 0; i < nodeTags.size(); i++)
            out << Num(coordinates[3 * i]) << " " << Num(coordinates[3 * i + 1]) << " " << Num(coordinates[3 * i + 2]) << "\n";
        out << "\nendmesh\n";
        out.close();
        Log("Mesh: " + std::to_string(nodeTags.size()) + " nodes, " + std::to_string(numVolume) +
            " volume elements, " + std::to_string(numSurface) + " surface elements.");
        if (!onlyEdges && numSurface == 0 && numVolume == 0)
        {
            Error("The mesher created no elements.");
            return 1;
        }
            return 0;
        }
        int MeshBrep(const std::vector<std::string>& args, bool onlyEdges)
        {
            if (args.size() < 3)
            {
                Error("The mesh command needs a BREP file, a VOL file and a mesh parameters file.");
                return 1;
            }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            ShapeMaps maps(shape);
            std::map<std::string, std::string> parameters = ReadParameterFile(args[2]);
            LocalSizes local = args.size() > 3 ? ReadLocalSizes(args[3]) : LocalSizes();
            //
            double maxh = GetDouble(parameters, "maxh", 1e10);
            double minh = GetDouble(parameters, "minh", 0);
            double grading = GetDouble(parameters, "grading", 0.3);
            double elementsPerCurve = GetDouble(parameters, "elementspercurve", 2);
            bool secondOrder = GetInt(parameters, "second_order", 0) != 0;
            bool quadDominated = GetInt(parameters, "quad_dominated", 0) != 0;
            int optimize3D = GetInt(parameters, "optsteps_3d", 3);
            int optimize2D = GetInt(parameters, "optsteps_2d", 3);
            int threads = GetInt(parameters, "parallel_meshing_threads", 1);
            if (maxh <= 0) maxh = 1e10;
            //
            gmsh::initialize();
            int status = 0;
            try
            {
                gmsh::option::setNumber("General.Terminal", 1);
                gmsh::option::setNumber("General.Verbosity", 3);
                gmsh::option::setNumber("General.NumThreads", std::max(1, threads));
                gmsh::option::setNumber("Geometry.OCCParallel", 1);
                gmsh::model::add("NetGenMesher");
                gmsh::vectorpair imported;
                // Only the highest dimension is imported: the edges of a compound of faces would otherwise come in a
                // second time as free curves, on which the second order mesh cannot be made
                gmsh::model::occ::importShapesNativePointer(&shape, imported, true);
                gmsh::model::occ::synchronize();
                //
                gmsh::option::setNumber("Mesh.MeshSizeMax", maxh);
                gmsh::option::setNumber("Mesh.MeshSizeMin", minh);
                gmsh::option::setNumber("Mesh.MeshSizeFromCurvature", std::max(0.0, 2 * M_PI * elementsPerCurve));
                gmsh::option::setNumber("Mesh.MeshSizeExtendFromBoundary", 1);
                gmsh::option::setNumber("Mesh.Algorithm", 6);   // quadrilaterals recombined; 8 leaves trimmed tubes partly unmeshed
                gmsh::option::setNumber("Mesh.Algorithm3D", 4);                 // Frontal: the Netgen mesher
                gmsh::option::setNumber("Mesh.OptimizeNetgen", optimize3D > 0 ? 1 : 0);
                gmsh::option::setNumber("Mesh.Optimize", 1);
                gmsh::option::setNumber("Mesh.Smoothing", std::max(1, optimize2D));
                if (quadDominated)
                {
                    gmsh::option::setNumber("Mesh.RecombineAll", 1);
                    gmsh::option::setNumber("Mesh.RecombinationAlgorithm", 1);
                    // A pair of triangles that makes a flat quadrilateral (an angle near 180 degrees, as along the line
                    // where two tubes meet) stays two triangles: a shell of such quadrilaterals expanded through its
                    // thickness has a negative Jacobian
                    try { gmsh::option::setNumber("Mesh.RecombineMinimumQuality", 0.5); } catch (...) { }
                }
                if (secondOrder)
                {
                    gmsh::option::setNumber("Mesh.ElementOrder", 2);
                    gmsh::option::setNumber("Mesh.SecondOrderIncomplete", 1);
                }
                // The local sizes, spread with the grading of the mesh
                if (!local.points.empty() || !local.lines.empty())
                {
                    double rate = std::max(0.1, grading);
                    gmsh::model::mesh::setSizeCallback([local, rate, maxh](int, int, double x, double y, double z, double lc) {
                        double p[3] = { x, y, z };
                        double h = std::min(lc, maxh);
                        for (auto& q : local.points)
                        {
                            double d = std::sqrt((x - q[0]) * (x - q[0]) + (y - q[1]) * (y - q[1]) + (z - q[2]) * (z - q[2]));
                            h = std::min(h, q[3] + rate * d);
                        }
                        for (auto& l : local.lines)
                        {
                            double d = DistanceToSegment(p, &l[0], &l[3]);
                            h = std::min(h, l[6] + rate * d);
                        }
                        return h;
                    });
                }
                // The structured faces and the numbers of elements set on edges
                std::vector<MappedFace> mappedFaces;
                std::string transfiniteFileName = parameters.count("transfinite_filename") ?
                                                  parameters["transfinite_filename"] : "";
                while (!transfiniteFileName.empty() && std::isspace((unsigned char)transfiniteFileName.back()))
                    transfiniteFileName.pop_back();
                if (!transfiniteFileName.empty() && std::ifstream(transfiniteFileName).good())
                {
                    Bnd_Box box;
                    BRepBndLib::Add(shape, box);
                    double size = maxh < 1e9 ? maxh : (box.IsVoid() ? 1.0 : std::sqrt(box.SquareExtent()) / 10);
                    if (!ApplyTransfinite(ReadTransfinite(transfiniteFileName), maps, size, quadDominated, mappedFaces))
                    {
                        gmsh::finalize();
                        return 1;
                    }
                }
                int dimension = onlyEdges ? 1 : (maps.solids.Extent() > 0 ? 3 : 2);
                Log("Meshing " + std::string(dimension == 3 ? "solid" : dimension == 2 ? "surface" : "edge") + " geometry...");
                auto prepare = [&]() {
                    if (mappedFaces.empty() || onlyEdges) return;
                    gmsh::model::mesh::generate(1);
                    BuildMappedFaces(mappedFaces, quadDominated);
                };
                status = GenerateWithRecombinationFallback(dimension, quadDominated && !onlyEdges, prepare, [&]() {
                    if (secondOrder && !onlyEdges) gmsh::model::mesh::setOrder(2);
                    std::map<int, int> faceIds = MatchEntities(2, maps.faces);
                    std::map<int, int> edgeIds = MatchEntities(1, maps.edges);
                    return WriteVol(args[1], &maps, &shape, faceIds, edgeIds, onlyEdges);
                });
            }
            catch (const std::exception& ex)
            {
                Error(std::string("Meshing failed: ") + ex.what());
                status = 1;
            }
            catch (...)
            {
                Error("Meshing failed.");
                status = 1;
            }
            gmsh::finalize();
            return status;
        }
        // STL_MESH (volume), STL_SURFACE_MESH and STL_EDGE_MESH <stl> <vol> <mesh parameters> <local sizes> <edge nodes>:
        // the triangles are grouped into surfaces at their sharp edges, a geometry is made of them and meshed
        int MeshStl(const std::vector<std::string>& args, int dimension)
        {
            if (args.size() < 3)
            {
                Error("The STL mesh command needs an STL file, a VOL file and a mesh parameters file.");
                return 1;
            }
            std::map<std::string, std::string> parameters = ReadParameterFile(args[2]);
            LocalSizes local = args.size() > 3 ? ReadLocalSizes(args[3]) : LocalSizes();
            double maxh = GetDouble(parameters, "maxh", 1e10);
            double minh = GetDouble(parameters, "minh", 0);
            double grading = GetDouble(parameters, "grading", 0.3);
            bool secondOrder = GetInt(parameters, "second_order", 0) != 0;
            bool quadDominated = GetInt(parameters, "quad_dominated", 0) != 0;
            int threads = GetInt(parameters, "parallel_meshing_threads", 1);
            if (maxh <= 0) maxh = 1e10;
            //
            gmsh::initialize();
            int status = 0;
            try
            {
                gmsh::option::setNumber("General.Terminal", 1);
                gmsh::option::setNumber("General.NumThreads", std::max(1, threads));
                gmsh::model::add("NetGenMesherStl");
                gmsh::merge(args[0]);
                // Sharp edges of 40 degrees and more bound the surfaces, as the edges PrePoMax detected do
                gmsh::model::mesh::classifySurfaces(40 * M_PI / 180, true, true, M_PI);
                gmsh::model::mesh::createGeometry();
                if (dimension == 3)
                {
                    gmsh::vectorpair surfaces;
                    gmsh::model::getEntities(surfaces, 2);
                    std::vector<int> tags;
                    for (auto& surface : surfaces) tags.push_back(surface.second);
                    int loop = gmsh::model::geo::addSurfaceLoop(tags);
                    gmsh::model::geo::addVolume({ loop });
                }
                gmsh::model::geo::synchronize();
                gmsh::option::setNumber("Mesh.MeshSizeMax", maxh);
                gmsh::option::setNumber("Mesh.MeshSizeMin", minh);
                gmsh::option::setNumber("Mesh.Algorithm", 6);   // quadrilaterals recombined; 8 leaves trimmed tubes partly unmeshed
                gmsh::option::setNumber("Mesh.Algorithm3D", 4);
                gmsh::option::setNumber("Mesh.OptimizeNetgen", 1);
                if (quadDominated) gmsh::option::setNumber("Mesh.RecombineAll", 1);
                if (secondOrder)
                {
                    gmsh::option::setNumber("Mesh.ElementOrder", 2);
                    gmsh::option::setNumber("Mesh.SecondOrderIncomplete", 1);
                }
                if (!local.points.empty() || !local.lines.empty())
                {
                    double rate = std::max(0.1, grading);
                    gmsh::model::mesh::setSizeCallback([local, rate, maxh](int, int, double x, double y, double z, double lc) {
                        double p[3] = { x, y, z };
                        double h = std::min(lc, maxh);
                        for (auto& q : local.points)
                            h = std::min(h, q[3] + rate * std::sqrt((x - q[0]) * (x - q[0]) + (y - q[1]) * (y - q[1]) +
                                                                     (z - q[2]) * (z - q[2])));
                        for (auto& l : local.lines) h = std::min(h, l[6] + rate * DistanceToSegment(p, &l[0], &l[3]));
                        return h;
                    });
                }
                gmsh::model::mesh::generate(dimension);
                if (secondOrder && dimension > 1) gmsh::model::mesh::setOrder(2);
                std::map<int, int> faceIds, edgeIds;
                status = WriteVol(args[1], nullptr, nullptr, faceIds, edgeIds, dimension == 1);
            }
            catch (const std::exception& ex)
            {
                Error(std::string("Meshing failed: ") + ex.what());
                status = 1;
            }
            catch (...)
            {
                Error("Meshing failed.");
                status = 1;
            }
            gmsh::finalize();
            return status;
        }
        // VOL_VOLUME_MESH <vol> <out vol> <mesh parameters>: the cells bounded by the surface elements of the vol file
        // (every cell its faces wound outward, the cell as the domain inside them) are filled with tetrahedra, and with a
        // pyramid on every quadrilateral. The points of the input stay where they are and come first, in their order;
        // the volume elements are written in the winding of Netgen, the other way round from gmsh
        int MeshVolVolume(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("VOL_VOLUME_MESH needs an input vol file, an output vol file and mesh parameters."); return 1; }
            std::ifstream in(args[0]);
            if (!in) { Error("The file " + args[0] + " could not be read."); return 1; }
            std::vector<std::array<double, 3>> points;
            std::map<int, std::vector<std::vector<int>>> domainFaces;
            std::string line;
            while (std::getline(in, line))
            {
                std::string key = line;
                key.erase(0, key.find_first_not_of(" \t\r"));
                key.erase(key.find_last_not_of(" \t\r") + 1);
                if (key == "points")
                {
                    int count = 0;
                    in >> count;
                    points.resize(std::max(0, count));
                    for (auto& p : points) in >> p[0] >> p[1] >> p[2];
                }
                else if (key == "surfaceelements")
                {
                    int count = 0;
                    in >> count;
                    for (int i = 0; i < count; i++)
                    {
                        int surface, bc, inside, outside, n;
                        in >> surface >> bc >> inside >> outside >> n;
                        std::vector<int> face(std::max(0, n));
                        for (int& id : face) in >> id;
                        if (inside > 0) domainFaces[inside].push_back(face);
                        else if (outside > 0) domainFaces[outside].push_back(std::vector<int>(face.rbegin(), face.rend()));
                    }
                }
            }
            if (points.empty() || domainFaces.empty()) { Error("The vol file holds no boundary to mesh."); return 1; }
            std::map<std::string, std::string> parameters = ReadParameterFile(args[2]);
            double maxh = GetDouble(parameters, "maxh", 1e10);
            double minh = GetDouble(parameters, "minh", 0);
            if (maxh <= 0) maxh = 1e10;
            //
            std::vector<std::array<double, 3>> newPoints;
            std::vector<std::pair<int, std::vector<int>>> elements;     // domain, Netgen point numbers
            int status = 0;
            gmsh::initialize();
            try
            {
                gmsh::option::setNumber("General.Terminal", 1);
                gmsh::option::setNumber("General.Verbosity", 2);
                for (auto& entry : domainFaces)
                {
                    gmsh::clear();
                    gmsh::model::add("Cell" + std::to_string(entry.first));
                    gmsh::option::setNumber("Mesh.MeshSizeMax", maxh);
                    gmsh::option::setNumber("Mesh.MeshSizeMin", minh);
                    gmsh::option::setNumber("Mesh.MeshSizeExtendFromBoundary", 1);
                    gmsh::option::setNumber("Mesh.MeshSizeFromPoints", 0);
                    gmsh::option::setNumber("Mesh.Algorithm3D", 1);
                    gmsh::option::setNumber("Mesh.Optimize", 1);
                    // The boundary nodes keep the numbers of the input points
                    std::set<int> used;
                    for (auto& face : entry.second) for (int id : face) used.insert(id);
                    std::vector<std::size_t> nodeTags;
                    std::vector<double> coordinates;
                    for (int id : used)
                    {
                        if (id < 1 || id > (int)points.size()) throw std::runtime_error("a face names a missing point");
                        nodeTags.push_back((std::size_t)id);
                        coordinates.insert(coordinates.end(), points[id - 1].begin(), points[id - 1].end());
                    }
                    gmsh::model::addDiscreteEntity(2, 1);
                    gmsh::model::mesh::addNodes(2, 1, nodeTags, coordinates);
                    std::vector<std::size_t> triangles, quads, triangleTags, quadTags;
                    std::size_t elementTag = 1;
                    for (auto& face : entry.second)
                    {
                        if (face.size() == 3) { triangles.insert(triangles.end(), face.begin(), face.end()); triangleTags.push_back(elementTag++); }
                        else if (face.size() == 4) { quads.insert(quads.end(), face.begin(), face.end()); quadTags.push_back(elementTag++); }
                        else throw std::runtime_error("a boundary face has neither three nor four points");
                    }
                    if (!triangles.empty()) gmsh::model::mesh::addElementsByType(1, 2, triangleTags, triangles);
                    if (!quads.empty()) gmsh::model::mesh::addElementsByType(1, 3, quadTags, quads);
                    int loop = gmsh::model::geo::addSurfaceLoop({ 1 });
                    gmsh::model::geo::addVolume({ loop }, 1);
                    gmsh::model::geo::synchronize();
                    gmsh::model::mesh::generate(3);
                    // The points gmsh added become new points
                    std::map<std::size_t, int> pointNumbers;
                    for (int id : used) pointNumbers[(std::size_t)id] = id;
                    std::vector<std::size_t> volumeNodeTags;
                    std::vector<double> volumeCoordinates, parametric;
                    gmsh::model::mesh::getNodes(volumeNodeTags, volumeCoordinates, parametric, 3, 1, true, false);
                    for (std::size_t i = 0; i < volumeNodeTags.size(); i++)
                    {
                        if (pointNumbers.count(volumeNodeTags[i])) continue;
                        newPoints.push_back({ volumeCoordinates[3 * i], volumeCoordinates[3 * i + 1], volumeCoordinates[3 * i + 2] });
                        pointNumbers[volumeNodeTags[i]] = (int)(points.size() + newPoints.size());
                    }
                    std::vector<int> types;
                    std::vector<std::vector<std::size_t>> tags, nodes;
                    gmsh::model::mesh::getElements(types, tags, nodes, 3, 1);
                    for (std::size_t t = 0; t < types.size(); t++)
                    {
                        int n = types[t] == 4 ? 4 : types[t] == 7 ? 5 : 0;
                        if (n == 0) throw std::runtime_error("gmsh made volume elements of type " + std::to_string(types[t]));
                        for (std::size_t e = 0; e + n <= nodes[t].size(); e += n)
                        {
                            std::vector<int> element(n);
                            for (int k = 0; k < n; k++)
                            {
                                auto it = pointNumbers.find(nodes[t][e + k]);
                                if (it == pointNumbers.end()) throw std::runtime_error("an element uses a node of no point");
                                element[k] = it->second;
                            }
                            // Netgen winds the other way: the tetrahedron and the base of the pyramid are reversed
                            if (n == 4) std::swap(element[1], element[2]);
                            else std::swap(element[1], element[3]);
                            elements.push_back({ entry.first, element });
                        }
                    }
                    Log("Cell " + std::to_string(entry.first) + " meshed.");
                }
            }
            catch (const std::exception& ex)
            {
                Error(std::string("The volume mesh failed: ") + ex.what());
                status = 1;
            }
            catch (...)
            {
                Error("The volume mesh failed.");
                status = 1;
            }
            gmsh::finalize();
            if (status != 0) return status;
            //
            std::ofstream out(args[1]);
            out.precision(17);
            out << "mesh3d\ndimension\n3\ngeomtype\n0\n\n";
            out << "volumeelements\n" << elements.size() << "\n";
            for (auto& element : elements)
            {
                out << element.first << " " << element.second.size();
                for (int id : element.second) out << " " << id;
                out << "\n";
            }
            out << "\npoints\n" << points.size() + newPoints.size() << "\n";
            for (auto& p : points) out << p[0] << " " << p[1] << " " << p[2] << "\n";
            for (auto& p : newPoints) out << p[0] << " " << p[1] << " " << p[2] << "\n";
            out << "\nendmesh\n";
            Log("Volume mesh: " + std::to_string(elements.size()) + " elements in " + std::to_string(domainFaces.size()) + " cells.");
            return 0;
        }
    }

    void RegisterMeshCommands()
    {
        RegisterCommand("BREP_MESH", [](auto& a) { return MeshBrep(a, false); });
        RegisterCommand("VOL_VOLUME_MESH", MeshVolVolume);
        RegisterCommand("BREP_EDGE_MESH", [](auto& a) { return MeshBrep(a, true); });
        RegisterCommand("STL_MESH", [](auto& a) { return MeshStl(a, 3); });
        RegisterCommand("STL_SURFACE_MESH", [](auto& a) { return MeshStl(a, 2); });
        RegisterCommand("STL_EDGE_MESH", [](auto& a) { return MeshStl(a, 1); });
    }
}
