// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The structured mapping of a three-sided face, the same for every mesher: three blocks of quads around one
    /// singular node, joined to a split point on every side by an inner line. The numbers on the sides decide the
    /// blocks: the inner line to side k carries L(k) elements, and the block at corner k is L(k - 1) by L(k), so a
    /// sharp corner, whose opposite side is short, gets a small block and the singular node moves towards it; when
    /// that side carries two elements the block is a single deltoid.
    /// The free nodes are placed in the parameter space, in a frame fitted to the lengths of the boundary segments
    /// so that it is close to isometric. They are first the barycentric (Tutte) embedding of the grid, every node at
    /// the mean of its neighbours, which cannot fold when the boundary is convex. Every node is then moved, one at a
    /// time, only where that lowers the condition energy of the quad corners it changes, which is infinite for a folded
    /// corner, so no step can fold a quad either. The arithmetic is plain and ordered, so that the C++ port gives the
    /// same grid.
    /// </summary>
    public static class ThreeBlockMapping
    {
        // Variables
        private const int MaxNumOfSweeps = 20;
        private const int NumOfStepTrials = 6;
        private const double InitialStepFactor = 0.1;
        private const double MinImprovement = 1E-9;
        private const double SolverTolerance = 1E-13;


        // Methods
        /// <summary>
        /// Maps the face whose side k runs from corner k to corner k + 1 through the nodes sideUv[k] in the parameter
        /// space and sideXyz[k] in space; the last node of a side is the first of the next. evaluate takes the
        /// parameters of nodes as u0, v0, u1, v1, ... and returns their coordinates as x0, y0, z0, x1, ...; without
        /// it the grid carries no coordinates of the inner nodes.
        /// </summary>
        public static ThreeBlockGrid Map(double[][][] sideUv, double[][][] sideXyz, Func<double[], double[]> evaluate)
        {
            int[] s = new int[3];
            for (int k = 0; k < 3; k++)
            {
                s[k] = sideUv[k].Length - 1;
                if (sideXyz[k].Length != sideUv[k].Length)
                    throw new CaeException("Side " + k + " of the face has " + sideUv[k].Length + " parameters and " +
                                           sideXyz[k].Length + " coordinates of its nodes.");
            }
            if (!TransfiniteSideTriple.AreCountsValid(s))
                throw new CaeException("The sides of the face carry " + string.Join(", ", s) +
                                       " elements, which three blocks of quads cannot fill.");
            int[] innerCounts = new int[3];
            for (int k = 0; k < 3; k++) innerCounts[k] = TransfiniteSideTriple.GetInnerLineCount(s, k);
            int numOfBoundaryNodes = s[0] + s[1] + s[2];
            int[] offsets = new int[] { 0, s[0], s[0] + s[1] };
            Func<int, int, int> boundaryId = (k, i) => i == s[k] ? offsets[(k + 1) % 3] : offsets[k] + i;
            // The frame in which the parameters are close to isometric
            double[] w = GetMetricFrame(sideUv, sideXyz);
            List<double[]> y = new List<double[]>();
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < s[k]; i++) y.Add(ToFrame(w, sideUv[k][i]));
            }
            // The singular node, first at the mean of the split points; side k is split L(k - 1) from corner k
            int[] splitIds = new int[3];
            for (int k = 0; k < 3; k++) splitIds[k] = boundaryId(k, innerCounts[(k + 2) % 3]);
            int centreId = y.Count;
            y.Add(new double[] { (y[splitIds[0]][0] + y[splitIds[1]][0] + y[splitIds[2]][0]) / 3,
                                 (y[splitIds[0]][1] + y[splitIds[1]][1] + y[splitIds[2]][1]) / 3 });
            // The inner lines from the split points to the singular node, first straight
            int[][] lineIds = new int[3][];
            for (int k = 0; k < 3; k++)
            {
                int n = innerCounts[k];
                lineIds[k] = new int[n + 1];
                lineIds[k][0] = splitIds[k];
                lineIds[k][n] = centreId;
                double[] a = y[splitIds[k]];
                double[] b = y[centreId];
                for (int j = 1; j < n; j++)
                {
                    double t = (double)j / n;
                    lineIds[k][j] = y.Count;
                    y.Add(new double[] { a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t });
                }
            }
            // Block k at corner k: side k to its split point, inner line k to the singular node, inner line k - 1
            // from the split point of side k - 1, and side k - 1 back to the corner; first a transfinite interpolation
            List<int[]> quads = new List<int[]>();
            for (int k = 0; k < 3; k++)
            {
                int previous = (k + 2) % 3;
                int a = innerCounts[previous];
                int b = innerCounts[k];
                int[,] ids = new int[a + 1, b + 1];
                for (int i = 0; i <= a; i++)
                {
                    ids[i, 0] = boundaryId(k, i);
                    ids[i, b] = lineIds[previous][i];
                }
                for (int j = 0; j <= b; j++)
                {
                    ids[0, j] = boundaryId(previous, s[previous] - j);
                    ids[a, j] = lineIds[k][j];
                }
                for (int i = 1; i < a; i++)
                {
                    for (int j = 1; j < b; j++)
                    {
                        ids[i, j] = y.Count;
                        y.Add(GetTransfinitePoint(y, ids, a, b, i, j));
                    }
                }
                for (int i = 0; i < a; i++)
                {
                    for (int j = 0; j < b; j++)
                        quads.Add(new int[] { ids[i, j], ids[i + 1, j], ids[i + 1, j + 1], ids[i, j + 1] });
                }
            }
            //
            double[][] points = y.ToArray();
            int[][] neighbours = GetNeighbours(points.Length, quads);
            SolveBarycentricEmbedding(points, neighbours, numOfBoundaryNodes);
            double orientation = GetSignedArea(points, numOfBoundaryNodes) < 0 ? -1 : 1;
            LowerConditionEnergy(points, neighbours, quads.ToArray(), numOfBoundaryNodes, orientation);
            //
            int numOfFoldedQuads = 0;
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (GetScaledJacobian(points, quad, c, orientation) <= 0)
                    {
                        numOfFoldedQuads++;
                        break;
                    }
                }
            }
            //
            int numOfInnerNodes = points.Length - numOfBoundaryNodes;
            double[][] innerUv = new double[numOfInnerNodes][];
            for (int i = 0; i < numOfInnerNodes; i++) innerUv[i] = FromFrame(w, points[numOfBoundaryNodes + i]);
            double[][] innerXyz = null;
            if (evaluate != null)
            {
                double[] flatUv = innerUv.SelectMany(uv => uv).ToArray();
                double[] flatXyz = numOfInnerNodes > 0 ? evaluate(flatUv) : new double[0];
                if (flatXyz == null || flatXyz.Length != 3 * numOfInnerNodes)
                    throw new CaeException("The inner nodes of the face could not be placed on its surface.");
                innerXyz = new double[numOfInnerNodes][];
                for (int i = 0; i < numOfInnerNodes; i++)
                    innerXyz[i] = new double[] { flatXyz[3 * i], flatXyz[3 * i + 1], flatXyz[3 * i + 2] };
            }
            return new ThreeBlockGrid(s, innerUv, innerXyz, quads.ToArray(), numOfFoldedQuads);
        }
        /// <summary>
        /// The upper triangle w0, w1, w2 of the Cholesky factor of the metric that best gives the squared lengths of
        /// the boundary segments from their parameters, so that (w0 u + w1 v, w2 v) is close to isometric over the
        /// face. The identity when the fit is not positive definite.
        /// </summary>
        private static double[] GetMetricFrame(double[][][] sideUv, double[][][] sideXyz)
        {
            double[,] m = new double[3, 4];
            for (int k = 0; k < 3; k++)
            {
                for (int i = 1; i < sideUv[k].Length; i++)
                {
                    double du = sideUv[k][i][0] - sideUv[k][i - 1][0];
                    double dv = sideUv[k][i][1] - sideUv[k][i - 1][1];
                    double dx = sideXyz[k][i][0] - sideXyz[k][i - 1][0];
                    double dy = sideXyz[k][i][1] - sideXyz[k][i - 1][1];
                    double dz = sideXyz[k][i][2] - sideXyz[k][i - 1][2];
                    double[] row = new double[] { du * du, 2 * du * dv, dv * dv };
                    double lengthSquared = dx * dx + dy * dy + dz * dz;
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++) m[r, c] += row[r] * row[c];
                        m[r, 3] += row[r] * lengthSquared;
                    }
                }
            }
            double[] g;
            if (!TrySolve3(m, out g)) return new double[] { 1, 0, 1 };
            if (!(g[0] > 0)) return new double[] { 1, 0, 1 };
            double w0 = Math.Sqrt(g[0]);
            double w1 = g[1] / w0;
            double rest = g[2] - w1 * w1;
            if (!(rest > 1E-12 * Math.Max(g[0], g[2]))) return new double[] { 1, 0, 1 };
            return new double[] { w0, w1, Math.Sqrt(rest) };
        }
        /// <summary>
        /// Gaussian elimination with partial pivoting of the augmented 3 by 4 system.
        /// </summary>
        private static bool TrySolve3(double[,] m, out double[] x)
        {
            x = null;
            double scale = 0;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++) scale = Math.Max(scale, Math.Abs(m[r, c]));
            }
            if (!(scale > 0)) return false;
            for (int col = 0; col < 3; col++)
            {
                int pivot = col;
                for (int r = col + 1; r < 3; r++)
                {
                    if (Math.Abs(m[r, col]) > Math.Abs(m[pivot, col])) pivot = r;
                }
                if (!(Math.Abs(m[pivot, col]) > 1E-14 * scale)) return false;
                if (pivot != col)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        double swap = m[col, c];
                        m[col, c] = m[pivot, c];
                        m[pivot, c] = swap;
                    }
                }
                for (int r = col + 1; r < 3; r++)
                {
                    double factor = m[r, col] / m[col, col];
                    for (int c = col; c < 4; c++) m[r, c] -= factor * m[col, c];
                }
            }
            x = new double[3];
            for (int r = 2; r >= 0; r--)
            {
                double sum = m[r, 3];
                for (int c = r + 1; c < 3; c++) sum -= m[r, c] * x[c];
                x[r] = sum / m[r, r];
            }
            return true;
        }
        private static double[] ToFrame(double[] w, double[] uv)
        {
            return new double[] { w[0] * uv[0] + w[1] * uv[1], w[2] * uv[1] };
        }
        private static double[] FromFrame(double[] w, double[] p)
        {
            double v = p[1] / w[2];
            return new double[] { (p[0] - w[1] * v) / w[0], v };
        }
        /// <summary>
        /// The transfinite (Coons) interpolation of a block between its four sides, by the index along each.
        /// </summary>
        private static double[] GetTransfinitePoint(List<double[]> y, int[,] ids, int a, int b, int i, int j)
        {
            double xi = (double)i / a;
            double eta = (double)j / b;
            double[] p = new double[2];
            for (int d = 0; d < 2; d++)
            {
                p[d] = (1 - eta) * y[ids[i, 0]][d] + eta * y[ids[i, b]][d] + (1 - xi) * y[ids[0, j]][d] +
                       xi * y[ids[a, j]][d] -
                       ((1 - xi) * (1 - eta) * y[ids[0, 0]][d] + xi * (1 - eta) * y[ids[a, 0]][d] +
                        xi * eta * y[ids[a, b]][d] + (1 - xi) * eta * y[ids[0, b]][d]);
            }
            return p;
        }
        /// <summary>
        /// The nodes every node shares a quad edge with, in ascending order.
        /// </summary>
        private static int[][] GetNeighbours(int numOfNodes, List<int[]> quads)
        {
            SortedSet<int>[] sets = new SortedSet<int>[numOfNodes];
            for (int i = 0; i < numOfNodes; i++) sets[i] = new SortedSet<int>();
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    sets[quad[c]].Add(quad[(c + 1) % 4]);
                    sets[quad[c]].Add(quad[(c + 3) % 4]);
                }
            }
            return sets.Select(set => set.ToArray()).ToArray();
        }
        /// <summary>
        /// Places every node after the boundary ones at the mean of its neighbours, by conjugate gradients on each
        /// coordinate, starting from where the nodes are.
        /// </summary>
        private static void SolveBarycentricEmbedding(double[][] points, int[][] neighbours, int numOfBoundaryNodes)
        {
            int n = points.Length - numOfBoundaryNodes;
            if (n == 0) return;
            double[] x = new double[n];
            double[] b = new double[n];
            double[] r = new double[n];
            double[] p = new double[n];
            double[] ap = new double[n];
            Action<double[], double[]> multiply = (vector, result) =>
            {
                for (int row = 0; row < n; row++)
                {
                    int[] nodeNeighbours = neighbours[numOfBoundaryNodes + row];
                    double sum = nodeNeighbours.Length * vector[row];
                    foreach (var neighbour in nodeNeighbours)
                    {
                        if (neighbour >= numOfBoundaryNodes) sum -= vector[neighbour - numOfBoundaryNodes];
                    }
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
                    foreach (var neighbour in neighbours[numOfBoundaryNodes + row])
                    {
                        if (neighbour < numOfBoundaryNodes) sum += points[neighbour][d];
                    }
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
                double limit = SolverTolerance * SolverTolerance * bb;
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
        /// <summary>
        /// Moves the nodes after the boundary ones, in turn and sweep after sweep, by a pattern search that only takes
        /// a step when it lowers the condition energy of the quad corners the node changes: the sum over the corners
        /// of (|a|^2 + |b|^2) / (2 a x b), a and b the edges at the corner. It is 1 at a square corner, grows with
        /// skew and stretch, and is infinite for a corner that folds, so a step never folds a quad, and a node of a
        /// folded quad only takes a step that makes all its quads valid. Unlike the scaled Jacobian alone, it does not
        /// let the quads shrink to straighten their angles.
        /// </summary>
        private static void LowerConditionEnergy(double[][] points, int[][] neighbours, int[][] quads,
                                                 int numOfBoundaryNodes, double orientation)
        {
            // The corners every node changes, as quad index and corner index, the nodes after each other
            int[] cornerStart = new int[points.Length + 1];
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (quad[c] >= numOfBoundaryNodes) cornerStart[quad[c] + 1]++;
                    if (quad[(c + 1) % 4] >= numOfBoundaryNodes) cornerStart[quad[(c + 1) % 4] + 1]++;
                    if (quad[(c + 3) % 4] >= numOfBoundaryNodes) cornerStart[quad[(c + 3) % 4] + 1]++;
                }
            }
            for (int i = 0; i < points.Length; i++) cornerStart[i + 1] += cornerStart[i];
            int[] cornerQuads = new int[cornerStart[points.Length]];
            int[] cornerIndices = new int[cornerQuads.Length];
            int[] fill = (int[])cornerStart.Clone();
            for (int q = 0; q < quads.Length; q++)
            {
                int[] quad = quads[q];
                for (int c = 0; c < 4; c++)
                {
                    int[] cornerNodes = new int[] { quad[c], quad[(c + 1) % 4], quad[(c + 3) % 4] };
                    foreach (var node in cornerNodes)
                    {
                        if (node < numOfBoundaryNodes) continue;
                        cornerQuads[fill[node]] = q;
                        cornerIndices[fill[node]] = c;
                        fill[node]++;
                    }
                }
            }
            double diagonal = Math.Sqrt(0.5);
            double[][] directions = new double[][]
            {
                new double[] { 1, 0 }, new double[] { -1, 0 }, new double[] { 0, 1 }, new double[] { 0, -1 },
                new double[] { diagonal, diagonal }, new double[] { -diagonal, diagonal },
                new double[] { diagonal, -diagonal }, new double[] { -diagonal, -diagonal }
            };
            for (int sweep = 0; sweep < MaxNumOfSweeps; sweep++)
            {
                bool moved = false;
                for (int node = numOfBoundaryNodes; node < points.Length; node++)
                {
                    if (cornerStart[node] == cornerStart[node + 1]) continue;
                    double[] point = points[node];
                    double length = 0;
                    foreach (var neighbour in neighbours[node])
                    {
                        double dx = points[neighbour][0] - point[0];
                        double dy = points[neighbour][1] - point[1];
                        length += Math.Sqrt(dx * dx + dy * dy);
                    }
                    double step = InitialStepFactor * length / neighbours[node].Length;
                    double current = GetNodeQuality(points, quads, cornerStart, cornerQuads, cornerIndices, node,
                                                    orientation);
                    for (int trial = 0; trial < NumOfStepTrials; trial++)
                    {
                        double x0 = point[0];
                        double y0 = point[1];
                        double bestQuality = double.IsNegativeInfinity(current) ? double.MinValue :
                                             current + MinImprovement * Math.Max(1, Math.Abs(current));
                        int best = -1;
                        for (int d = 0; d < directions.Length; d++)
                        {
                            point[0] = x0 + directions[d][0] * step;
                            point[1] = y0 + directions[d][1] * step;
                            double q = GetNodeQuality(points, quads, cornerStart, cornerQuads, cornerIndices, node,
                                                      orientation);
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
        /// <summary>
        /// The sine of the angle at corner c of the quad, negative where the quad turns against the orientation.
        /// </summary>
        private static double GetScaledJacobian(double[][] points, int[] quad, int c, double orientation)
        {
            double[] p = points[quad[c]];
            double[] next = points[quad[(c + 1) % 4]];
            double[] previous = points[quad[(c + 3) % 4]];
            double ax = next[0] - p[0];
            double ay = next[1] - p[1];
            double bx = previous[0] - p[0];
            double by = previous[1] - p[1];
            double lengths = Math.Sqrt((ax * ax + ay * ay) * (bx * bx + by * by));
            if (!(lengths > 0)) return -1;
            return orientation * (ax * by - ay * bx) / lengths;
        }
        /// <summary>
        /// The negative condition energy of the corners a node changes, minus infinity when one of them folds.
        /// </summary>
        private static double GetNodeQuality(double[][] points, int[][] quads, int[] cornerStart, int[] cornerQuads,
                                             int[] cornerIndices, int node, double orientation)
        {
            double energy = 0;
            for (int i = cornerStart[node]; i < cornerStart[node + 1]; i++)
            {
                double cornerEnergy = GetConditionEnergy(points, quads[cornerQuads[i]], cornerIndices[i], orientation);
                if (double.IsPositiveInfinity(cornerEnergy)) return double.NegativeInfinity;
                energy += cornerEnergy;
            }
            return -energy;
        }
        /// <summary>
        /// (|a|^2 + |b|^2) / (2 a x b) of corner c of the quad, infinite where the corner turns against the orientation
        /// or has no area.
        /// </summary>
        private static double GetConditionEnergy(double[][] points, int[] quad, int c, double orientation)
        {
            double[] p = points[quad[c]];
            double[] next = points[quad[(c + 1) % 4]];
            double[] previous = points[quad[(c + 3) % 4]];
            double ax = next[0] - p[0];
            double ay = next[1] - p[1];
            double bx = previous[0] - p[0];
            double by = previous[1] - p[1];
            double cross = orientation * (ax * by - ay * bx);
            if (!(cross > 0)) return double.PositiveInfinity;
            return (ax * ax + ay * ay + bx * bx + by * by) / (2 * cross);
        }
        /// <summary>
        /// The signed area of the boundary loop, the first nodes in loop order.
        /// </summary>
        private static double GetSignedArea(double[][] points, int numOfBoundaryNodes)
        {
            double area = 0;
            for (int i = 0; i < numOfBoundaryNodes; i++)
            {
                double[] p = points[i];
                double[] q = points[(i + 1) % numOfBoundaryNodes];
                area += p[0] * q[1] - q[0] * p[1];
            }
            return 0.5 * area;
        }
    }
}
