// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

// The structured mapping of a three-sided face that PrePoMax, Netgen and QuadWild share. It is the C++ port of
// CaeMesh/Meshing/Transfinite/ThreeBlockMapping.cs and must stay identical to it: the same arithmetic in the same
// order, checked by three_block_vectors.txt, which the C# mapping writes and three_block_mapping_check.cpp reads.
// A change here is a change there too, and the vectors are written again.
//
// Three blocks of quads lie around one singular node, joined to a split point on every side by an inner line. The
// inner line to side k carries L(k) = (s(k - 1) + s(k + 1) - s(k)) / 2 elements and the block at corner k is
// L(k - 1) by L(k), so a sharp corner, whose opposite side is short, gets a small block and the singular node moves
// towards it; when that side carries two elements the block is a single deltoid. The free nodes are placed in the
// parameter space, in a frame fitted to the lengths of the boundary segments: first the barycentric (Tutte)
// embedding, every node at the mean of its neighbours, which cannot fold for a convex boundary, then node by node a
// pattern search that only steps where it lowers the condition energy (|a|^2 + |b|^2) / (2 a x b) of the quad
// corners the node changes, which is infinite for a folded corner.

#ifndef PREPOMAX_THREE_BLOCK_MAPPING_H
#define PREPOMAX_THREE_BLOCK_MAPPING_H

#include <algorithm>
#include <array>
#include <cmath>
#include <functional>
#include <limits>
#include <set>
#include <stdexcept>
#include <string>
#include <vector>

namespace three_block
{
    typedef std::array<double, 2> Uv;
    typedef std::array<double, 3> Xyz;

    // The nodes are numbered along the boundary loop first, node i of side k being boundaryNodeId(k, i), and then
    // the inner nodes, inner node n being numberOfBoundaryNodes() + n. The quads turn the way the boundary loop
    // turns in the parameter space.
    struct Grid
    {
        int sideCounts[3] = {0, 0, 0};
        std::vector<Uv> innerUv;
        std::vector<Xyz> innerXyz;
        std::vector<std::array<int, 4> > quads;
        // quads that turn against the boundary loop or have a corner without area; valid when there are none
        int numberOfFoldedQuads = 0;

        int numberOfBoundaryNodes() const { return sideCounts[0] + sideCounts[1] + sideCounts[2]; }
        // node i of side k, which runs from corner k (i = 0) to corner k + 1 (i = sideCounts[k])
        int boundaryNodeId(int k, int i) const
        {
            if (i == sideCounts[k]) return k == 2 ? 0 : boundaryNodeId(k + 1, 0);
            int id = i;
            for (int side = 0; side < k; side++) id += sideCounts[side];
            return id;
        }
    };

    // Takes the parameters of nodes as u0, v0, u1, v1, ... and returns their coordinates as x0, y0, z0, x1, ...
    typedef std::function<void(const std::vector<double>& uv, std::vector<double>& xyz)> Evaluator;

    inline int innerLineCount(const int s[3], int k)
    {
        return (s[(k + 2) % 3] + s[(k + 1) % 3] - s[k]) / 2;
    }

    inline bool countsValid(const int s[3])
    {
        if ((s[0] + s[1] + s[2]) % 2 != 0) return false;
        for (int k = 0; k < 3; k++)
            if (innerLineCount(s, k) < 1) return false;
        return true;
    }

    namespace detail
    {
        const int maxNumOfSweeps = 20;
        const int numOfStepTrials = 6;
        const double initialStepFactor = 0.1;
        const double minImprovement = 1E-9;
        const double solverTolerance = 1E-13;

        inline bool trySolve3(double m[3][4], double x[3])
        {
            double scale = 0;
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++) scale = std::max(scale, std::fabs(m[r][c]));
            if (!(scale > 0)) return false;
            for (int col = 0; col < 3; col++)
            {
                int pivot = col;
                for (int r = col + 1; r < 3; r++)
                    if (std::fabs(m[r][col]) > std::fabs(m[pivot][col])) pivot = r;
                if (!(std::fabs(m[pivot][col]) > 1E-14 * scale)) return false;
                if (pivot != col)
                    for (int c = 0; c < 4; c++) std::swap(m[col][c], m[pivot][c]);
                for (int r = col + 1; r < 3; r++)
                {
                    double factor = m[r][col] / m[col][col];
                    for (int c = col; c < 4; c++) m[r][c] -= factor * m[col][c];
                }
            }
            for (int r = 2; r >= 0; r--)
            {
                double sum = m[r][3];
                for (int c = r + 1; c < 3; c++) sum -= m[r][c] * x[c];
                x[r] = sum / m[r][r];
            }
            return true;
        }

        // The upper triangle w0, w1, w2 of the Cholesky factor of the metric that best gives the squared lengths of
        // the boundary segments from their parameters; the identity when the fit is not positive definite.
        inline std::array<double, 3> metricFrame(const std::vector<Uv> sideUv[3], const std::vector<Xyz> sideXyz[3])
        {
            double m[3][4] = {{0, 0, 0, 0}, {0, 0, 0, 0}, {0, 0, 0, 0}};
            for (int k = 0; k < 3; k++)
            {
                for (size_t i = 1; i < sideUv[k].size(); i++)
                {
                    double du = sideUv[k][i][0] - sideUv[k][i - 1][0];
                    double dv = sideUv[k][i][1] - sideUv[k][i - 1][1];
                    double dx = sideXyz[k][i][0] - sideXyz[k][i - 1][0];
                    double dy = sideXyz[k][i][1] - sideXyz[k][i - 1][1];
                    double dz = sideXyz[k][i][2] - sideXyz[k][i - 1][2];
                    double row[3] = {du * du, 2 * du * dv, dv * dv};
                    double lengthSquared = dx * dx + dy * dy + dz * dz;
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++) m[r][c] += row[r] * row[c];
                        m[r][3] += row[r] * lengthSquared;
                    }
                }
            }
            const std::array<double, 3> identity = {1, 0, 1};
            double g[3];
            if (!trySolve3(m, g)) return identity;
            if (!(g[0] > 0)) return identity;
            double w0 = std::sqrt(g[0]);
            double w1 = g[1] / w0;
            double rest = g[2] - w1 * w1;
            if (!(rest > 1E-12 * std::max(g[0], g[2]))) return identity;
            std::array<double, 3> w = {w0, w1, std::sqrt(rest)};
            return w;
        }

        inline Uv toFrame(const std::array<double, 3>& w, const Uv& uv)
        {
            Uv p = {w[0] * uv[0] + w[1] * uv[1], w[2] * uv[1]};
            return p;
        }

        inline Uv fromFrame(const std::array<double, 3>& w, const Uv& p)
        {
            double v = p[1] / w[2];
            Uv uv = {(p[0] - w[1] * v) / w[0], v};
            return uv;
        }

        inline double scaledJacobian(const std::vector<Uv>& points, const std::array<int, 4>& quad, int c,
                                     double orientation)
        {
            const Uv& p = points[quad[c]];
            const Uv& next = points[quad[(c + 1) % 4]];
            const Uv& previous = points[quad[(c + 3) % 4]];
            double ax = next[0] - p[0];
            double ay = next[1] - p[1];
            double bx = previous[0] - p[0];
            double by = previous[1] - p[1];
            double lengths = std::sqrt((ax * ax + ay * ay) * (bx * bx + by * by));
            if (!(lengths > 0)) return -1;
            return orientation * (ax * by - ay * bx) / lengths;
        }

        inline double conditionEnergy(const std::vector<Uv>& points, const std::array<int, 4>& quad, int c,
                                      double orientation)
        {
            const Uv& p = points[quad[c]];
            const Uv& next = points[quad[(c + 1) % 4]];
            const Uv& previous = points[quad[(c + 3) % 4]];
            double ax = next[0] - p[0];
            double ay = next[1] - p[1];
            double bx = previous[0] - p[0];
            double by = previous[1] - p[1];
            double cross = orientation * (ax * by - ay * bx);
            if (!(cross > 0)) return std::numeric_limits<double>::infinity();
            return (ax * ax + ay * ay + bx * bx + by * by) / (2 * cross);
        }

        inline void barycentricEmbedding(std::vector<Uv>& points, const std::vector<std::vector<int> >& neighbours,
                                         int numOfBoundaryNodes)
        {
            const int n = static_cast<int>(points.size()) - numOfBoundaryNodes;
            if (n == 0) return;
            std::vector<double> x(n), b(n), r(n), p(n), ap(n);
            auto multiply = [&](const std::vector<double>& vector, std::vector<double>& result) {
                for (int row = 0; row < n; row++)
                {
                    const std::vector<int>& nodeNeighbours = neighbours[numOfBoundaryNodes + row];
                    double sum = nodeNeighbours.size() * vector[row];
                    for (int neighbour : nodeNeighbours)
                        if (neighbour >= numOfBoundaryNodes) sum -= vector[neighbour - numOfBoundaryNodes];
                    result[row] = sum;
                }
            };
            for (int d = 0; d < 2; d++)
            {
                double bb = 0;
                for (int row = 0; row < n; row++)
                {
                    x[row] = points[numOfBoundaryNodes + row][d];
                    double sum = 0;
                    for (int neighbour : neighbours[numOfBoundaryNodes + row])
                        if (neighbour < numOfBoundaryNodes) sum += points[neighbour][d];
                    b[row] = sum;
                    bb += sum * sum;
                }
                multiply(x, ap);
                double rr = 0;
                for (int row = 0; row < n; row++)
                {
                    r[row] = b[row] - ap[row];
                    p[row] = r[row];
                    rr += r[row] * r[row];
                }
                double limit = solverTolerance * solverTolerance * bb;
                for (int iteration = 0; iteration < 10 * n + 100 && rr > limit; iteration++)
                {
                    multiply(p, ap);
                    double pap = 0;
                    for (int row = 0; row < n; row++) pap += p[row] * ap[row];
                    if (!(pap > 0)) break;
                    double alpha = rr / pap;
                    double rrNew = 0;
                    for (int row = 0; row < n; row++)
                    {
                        x[row] += alpha * p[row];
                        r[row] -= alpha * ap[row];
                        rrNew += r[row] * r[row];
                    }
                    double beta = rrNew / rr;
                    for (int row = 0; row < n; row++) p[row] = r[row] + beta * p[row];
                    rr = rrNew;
                }
                for (int row = 0; row < n; row++) points[numOfBoundaryNodes + row][d] = x[row];
            }
        }

        inline void lowerConditionEnergy(std::vector<Uv>& points, const std::vector<std::vector<int> >& neighbours,
                                         const std::vector<std::array<int, 4> >& quads, int numOfBoundaryNodes,
                                         double orientation)
        {
            const int numOfNodes = static_cast<int>(points.size());
            std::vector<std::vector<std::array<int, 2> > > nodeCorners(numOfNodes);
            for (int q = 0; q < static_cast<int>(quads.size()); q++)
            {
                const std::array<int, 4>& quad = quads[q];
                for (int c = 0; c < 4; c++)
                {
                    const int cornerNodes[3] = {quad[c], quad[(c + 1) % 4], quad[(c + 3) % 4]};
                    for (int node : cornerNodes)
                        if (node >= numOfBoundaryNodes) nodeCorners[node].push_back({q, c});
                }
            }
            const double diagonal = std::sqrt(0.5);
            const double directions[8][2] = {{1, 0}, {-1, 0}, {0, 1}, {0, -1}, {diagonal, diagonal},
                                             {-diagonal, diagonal}, {diagonal, -diagonal}, {-diagonal, -diagonal}};
            auto quality = [&](int node) {
                double energy = 0;
                for (const auto& corner : nodeCorners[node])
                {
                    double cornerEnergy = conditionEnergy(points, quads[corner[0]], corner[1], orientation);
                    if (std::isinf(cornerEnergy) && cornerEnergy > 0) return -std::numeric_limits<double>::infinity();
                    energy += cornerEnergy;
                }
                return -energy;
            };
            for (int sweep = 0; sweep < maxNumOfSweeps; sweep++)
            {
                bool moved = false;
                for (int node = numOfBoundaryNodes; node < numOfNodes; node++)
                {
                    if (nodeCorners[node].empty()) continue;
                    Uv& point = points[node];
                    double length = 0;
                    for (int neighbour : neighbours[node])
                    {
                        double dx = points[neighbour][0] - point[0];
                        double dy = points[neighbour][1] - point[1];
                        length += std::sqrt(dx * dx + dy * dy);
                    }
                    double step = initialStepFactor * length / static_cast<double>(neighbours[node].size());
                    double current = quality(node);
                    for (int trial = 0; trial < numOfStepTrials; trial++)
                    {
                        double x0 = point[0];
                        double y0 = point[1];
                        double bestQuality = (std::isinf(current) && current < 0) ?
                                             -std::numeric_limits<double>::max() :
                                             current + minImprovement * std::max(1.0, std::fabs(current));
                        int best = -1;
                        for (int d = 0; d < 8; d++)
                        {
                            point[0] = x0 + directions[d][0] * step;
                            point[1] = y0 + directions[d][1] * step;
                            double q = quality(node);
                            if (q > bestQuality)
                            {
                                bestQuality = q;
                                best = d;
                            }
                        }
                        if (best >= 0)
                        {
                            point[0] = x0 + directions[best][0] * step;
                            point[1] = y0 + directions[best][1] * step;
                            current = bestQuality;
                            moved = true;
                        }
                        else
                        {
                            point[0] = x0;
                            point[1] = y0;
                            step *= 0.5;
                        }
                    }
                }
                if (!moved) break;
            }
        }

        inline Uv transfinitePoint(const std::vector<Uv>& y, const std::vector<std::vector<int> >& ids, int a, int b,
                                   int i, int j)
        {
            double xi = static_cast<double>(i) / a;
            double eta = static_cast<double>(j) / b;
            Uv p;
            for (int d = 0; d < 2; d++)
            {
                p[d] = (1 - eta) * y[ids[i][0]][d] + eta * y[ids[i][b]][d] + (1 - xi) * y[ids[0][j]][d] +
                       xi * y[ids[a][j]][d] -
                       ((1 - xi) * (1 - eta) * y[ids[0][0]][d] + xi * (1 - eta) * y[ids[a][0]][d] +
                        xi * eta * y[ids[a][b]][d] + (1 - xi) * eta * y[ids[0][b]][d]);
            }
            return p;
        }
    }

    // Maps the face whose side k runs from corner k to corner k + 1 through the nodes sideUv[k] in the parameter
    // space and sideXyz[k] in space; the last node of a side is the first of the next. Without evaluate the grid
    // carries no coordinates of the inner nodes. Throws std::invalid_argument when the counts cannot be filled.
    inline Grid map(const std::vector<Uv> sideUv[3], const std::vector<Xyz> sideXyz[3],
                    const Evaluator& evaluate = Evaluator())
    {
        using namespace detail;
        Grid grid;
        int* s = grid.sideCounts;
        for (int k = 0; k < 3; k++)
        {
            s[k] = static_cast<int>(sideUv[k].size()) - 1;
            if (sideXyz[k].size() != sideUv[k].size())
                throw std::invalid_argument("Side " + std::to_string(k) + " of the face has " +
                                            std::to_string(sideUv[k].size()) + " parameters and " +
                                            std::to_string(sideXyz[k].size()) + " coordinates of its nodes.");
        }
        if (!countsValid(s))
            throw std::invalid_argument("The sides of the face carry " + std::to_string(s[0]) + ", " +
                                        std::to_string(s[1]) + ", " + std::to_string(s[2]) +
                                        " elements, which three blocks of quads cannot fill.");
        int innerCounts[3];
        for (int k = 0; k < 3; k++) innerCounts[k] = innerLineCount(s, k);
        const int numOfBoundaryNodes = s[0] + s[1] + s[2];
        const int offsets[3] = {0, s[0], s[0] + s[1]};
        auto boundaryId = [&](int k, int i) { return i == s[k] ? offsets[(k + 1) % 3] : offsets[k] + i; };
        // the frame in which the parameters are close to isometric
        const std::array<double, 3> w = metricFrame(sideUv, sideXyz);
        std::vector<Uv> y;
        for (int k = 0; k < 3; k++)
            for (int i = 0; i < s[k]; i++) y.push_back(toFrame(w, sideUv[k][i]));
        // the singular node, first at the mean of the split points; side k is split L(k - 1) from corner k
        int splitIds[3];
        for (int k = 0; k < 3; k++) splitIds[k] = boundaryId(k, innerCounts[(k + 2) % 3]);
        const int centreId = static_cast<int>(y.size());
        Uv centre = {(y[splitIds[0]][0] + y[splitIds[1]][0] + y[splitIds[2]][0]) / 3,
                     (y[splitIds[0]][1] + y[splitIds[1]][1] + y[splitIds[2]][1]) / 3};
        y.push_back(centre);
        // the inner lines from the split points to the singular node, first straight
        std::vector<int> lineIds[3];
        for (int k = 0; k < 3; k++)
        {
            const int n = innerCounts[k];
            lineIds[k].assign(n + 1, 0);
            lineIds[k][0] = splitIds[k];
            lineIds[k][n] = centreId;
            const Uv a = y[splitIds[k]];
            const Uv b = y[centreId];
            for (int j = 1; j < n; j++)
            {
                double t = static_cast<double>(j) / n;
                lineIds[k][j] = static_cast<int>(y.size());
                Uv p = {a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t};
                y.push_back(p);
            }
        }
        // block k at corner k, first a transfinite interpolation between its four sides
        std::vector<std::array<int, 4> >& quads = grid.quads;
        for (int k = 0; k < 3; k++)
        {
            const int previous = (k + 2) % 3;
            const int a = innerCounts[previous];
            const int b = innerCounts[k];
            std::vector<std::vector<int> > ids(a + 1, std::vector<int>(b + 1, 0));
            for (int i = 0; i <= a; i++)
            {
                ids[i][0] = boundaryId(k, i);
                ids[i][b] = lineIds[previous][i];
            }
            for (int j = 0; j <= b; j++)
            {
                ids[0][j] = boundaryId(previous, s[previous] - j);
                ids[a][j] = lineIds[k][j];
            }
            for (int i = 1; i < a; i++)
            {
                for (int j = 1; j < b; j++)
                {
                    ids[i][j] = static_cast<int>(y.size());
                    Uv p = transfinitePoint(y, ids, a, b, i, j);
                    y.push_back(p);
                }
            }
            for (int i = 0; i < a; i++)
                for (int j = 0; j < b; j++)
                    quads.push_back({ids[i][j], ids[i + 1][j], ids[i + 1][j + 1], ids[i][j + 1]});
        }
        //
        std::vector<std::set<int> > sets(y.size());
        for (const auto& quad : quads)
        {
            for (int c = 0; c < 4; c++)
            {
                sets[quad[c]].insert(quad[(c + 1) % 4]);
                sets[quad[c]].insert(quad[(c + 3) % 4]);
            }
        }
        std::vector<std::vector<int> > neighbours(y.size());
        for (size_t i = 0; i < y.size(); i++) neighbours[i].assign(sets[i].begin(), sets[i].end());
        barycentricEmbedding(y, neighbours, numOfBoundaryNodes);
        double area = 0;
        for (int i = 0; i < numOfBoundaryNodes; i++)
        {
            const Uv& p = y[i];
            const Uv& q = y[(i + 1) % numOfBoundaryNodes];
            area += p[0] * q[1] - q[0] * p[1];
        }
        const double orientation = 0.5 * area < 0 ? -1 : 1;
        lowerConditionEnergy(y, neighbours, quads, numOfBoundaryNodes, orientation);
        //
        for (const auto& quad : quads)
        {
            for (int c = 0; c < 4; c++)
            {
                if (scaledJacobian(y, quad, c, orientation) <= 0)
                {
                    grid.numberOfFoldedQuads++;
                    break;
                }
            }
        }
        //
        const int numOfInnerNodes = static_cast<int>(y.size()) - numOfBoundaryNodes;
        for (int i = 0; i < numOfInnerNodes; i++) grid.innerUv.push_back(fromFrame(w, y[numOfBoundaryNodes + i]));
        if (evaluate)
        {
            std::vector<double> flatUv, flatXyz;
            for (const auto& uv : grid.innerUv)
            {
                flatUv.push_back(uv[0]);
                flatUv.push_back(uv[1]);
            }
            if (numOfInnerNodes > 0) evaluate(flatUv, flatXyz);
            if (flatXyz.size() != 3 * static_cast<size_t>(numOfInnerNodes))
                throw std::runtime_error("The inner nodes of the face could not be placed on its surface.");
            for (int i = 0; i < numOfInnerNodes; i++)
            {
                Xyz p = {flatXyz[3 * i], flatXyz[3 * i + 1], flatXyz[3 * i + 2]};
                grid.innerXyz.push_back(p);
            }
        }
        return grid;
    }
}

#endif
