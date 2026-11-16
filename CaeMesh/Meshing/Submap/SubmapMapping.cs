// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The structured mapping of a face whose boundary loops have corners near multiples of 90 degrees only, the same
    /// for every mesher. Every loop vertex is an end (a left turn), a side (no turn) or a corner (a right turn), so
    /// that every edge runs along one of the logical directions +i, +j, -i, -j and the boundary nodes get integer
    /// logical coordinates. The outer loop is then a simple rectilinear lattice polygon, counter-clockwise, and the
    /// quads are the unit cells inside it; every inner node has four.
    /// A face may have holes, loops inside the outer one that run clockwise in the lattice, each placed by the integer
    /// offset of its first node from the first node of the outer loop (SubmapHolePlacement). No lattice point lies on
    /// two loops, and the cells inside are those inside the outer loop and outside every hole, by the parity of the
    /// boundary edges along i of all loops that a row of cells crosses. The edges of all loops are numbered one after
    /// another, the outer loop first.
    /// The inner nodes are placed in the parameter space, in the frame of ThreeBlockMapping that is close to isometric,
    /// fitted over the outer loop. They start on the mean of the two straight lattice lines through them, each
    /// interpolated between the nearest boundary nodes it ends on, of the outer loop or of a hole, are relaxed to the
    /// discrete harmonic map, every node at the mean of its four neighbours, and then to the discrete Winslow map, in
    /// which the logical coordinates are harmonic over the face instead. The outline is not convex, so neither map is
    /// sure not to fold: the harmonic one bunches its lattice lines at a corner of 270 degrees, the Winslow one much
    /// less. Folded quads are then untangled by moves that raise their worst corner, and the condition energy descent
    /// of ThreeBlockMapping, run until it settles, evens out the quads without folding one. The quads turn the way the
    /// outer loop turns.
    /// When the grid still folds but the start did not, the start is relaxed instead by steps that never fold a quad.
    /// The arithmetic is plain and ordered, so that a port gives the same grid.
    /// </summary>
    public static class SubmapMapping
    {
        // Variables                                                                                                                
        private const int MaxNumOfWinslowSweeps = 100;
        private const double WinslowTolerance = 1E-6;
        private const int MaxNumOfUntangleSweeps = 100;
        private const int NumOfUntangleStepTrials = 8;
        private const int MaxNumOfDescentSweeps = 200;
        private const int NumOfDescentStepTrials = 6;
        private const double DescentStepFactor = 0.1;
        private const double DescentMinImprovement = 1E-9;
        private const int MaxNumOfGuardedSweeps = 500;
        private const int NumOfGuardedStepTrials = 4;
        private const double GuardedTolerance = 1E-10;
        private static readonly int[] DirectionI = new int[] { 1, 0, -1, 0 };
        private static readonly int[] DirectionJ = new int[] { 0, 1, 0, -1 };
        private static readonly int[] CornerOffsets = new int[] { 0, 1, 3 };


        // Methods                                                                                                                  
        /// <summary>
        /// Whether a loop whose edge k runs in direction edgeDirections[k] (0, 1, 2, 3 for +i, +j, -i, -j) with
        /// edgeCounts[k] elements closes into a simple counter-clockwise lattice outline; otherwise the reason why not.
        /// </summary>
        public static bool AreCountsValid(int[] edgeDirections, int[] edgeCounts, out string reason)
        {
            return AreCountsValid(new int[][] { edgeDirections }, new int[][] { edgeCounts }, new int[0][], out reason);
        }
        /// <summary>
        /// Whether the loops of a face, the outer one first and then its holes, each given by the logical directions
        /// and the numbers of elements of its edges, close into simple lattice outlines, the outer one counter-clockwise
        /// and every hole clockwise. With holeOffsets, the lattice offset of the first node of every hole from the first
        /// node of the outer loop, also whether no lattice point lies on two loops and every hole lies inside the outer
        /// outline and outside the other holes; without them every loop is judged on its own. The reason names the
        /// edges by their places in all loops one after another.
        /// </summary>
        public static bool AreCountsValid(int[][] loopDirections, int[][] loopCounts, int[][] holeOffsets,
                                          out string reason)
        {
            reason = null;
            int numOfLoops = loopDirections == null ? 0 : loopDirections.Length;
            if (numOfLoops == 0 || loopCounts == null || loopCounts.Length != numOfLoops)
            {
                reason = "The face has " + numOfLoops + " loops of edge directions and " +
                         (loopCounts == null ? 0 : loopCounts.Length) + " loops of edge counts.";
                return false;
            }
            long[][] loopI = new long[numOfLoops][];
            long[][] loopJ = new long[numOfLoops][];
            int[] firstEdges = new int[numOfLoops];
            for (int l = 0; l < numOfLoops; l++)
            {
                if (l > 0) firstEdges[l] = firstEdges[l - 1] + loopDirections[l - 1].Length;
                if (!IsLoopValid(loopDirections[l], loopCounts[l], l, firstEdges[l], out loopI[l], out loopJ[l],
                                 out reason)) return false;
            }
            if (holeOffsets == null) return true;
            if (holeOffsets.Length != numOfLoops - 1)
            {
                reason = "The face has " + (numOfLoops - 1) + " holes and " + holeOffsets.Length + " hole offsets.";
                return false;
            }
            // No lattice point on two loops
            long[] offsetI = new long[numOfLoops];
            long[] offsetJ = new long[numOfLoops];
            for (int l = 1; l < numOfLoops; l++)
            {
                offsetI[l] = holeOffsets[l - 1][0];
                offsetJ[l] = holeOffsets[l - 1][1];
            }
            HashSet<long> visited = new HashSet<long>();
            for (int l = 0; l < numOfLoops; l++)
            {
                int p = 0;
                for (int k = 0; k < loopDirections[l].Length; k++)
                {
                    for (int s = 0; s < loopCounts[l][k]; s++)
                    {
                        long i = loopI[l][p] + offsetI[l];
                        long j = loopJ[l][p] + offsetJ[l];
                        if (!visited.Add((i << 32) ^ (j & 0xFFFFFFFFL)))
                        {
                            reason = "The edge counts make the lattice outline of the face touch itself on edge " +
                                     (firstEdges[l] + k) + ".";
                            return false;
                        }
                        p++;
                    }
                }
            }
            // Every hole inside the outer outline and outside the other holes
            for (int h = 1; h < numOfLoops; h++)
            {
                long i = loopI[h][0] + offsetI[h];
                long j = loopJ[h][0] + offsetJ[h];
                if (!IsInsideLoop(i, j, loopI[0], loopJ[0], 0, 0))
                {
                    reason = "The hole starting at Edge " + firstEdges[h] + " lies outside the lattice outline of the face.";
                    return false;
                }
                for (int g = 1; g < numOfLoops; g++)
                {
                    if (g == h || !IsInsideLoop(i, j, loopI[g], loopJ[g], offsetI[g], offsetJ[g])) continue;
                    reason = "The hole starting at Edge " + firstEdges[h] + " lies inside the hole starting at Edge " +
                             firstEdges[g] + " in the lattice.";
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Whether one loop of a face, loop 0 the outer one and any other a hole whose first edge is firstEdge of all
        /// loops, closes into a simple lattice outline that turns the way it must, and the lattice coordinates of its
        /// nodes from its first node.
        /// </summary>
        private static bool IsLoopValid(int[] edgeDirections, int[] edgeCounts, int loop, int firstEdge, out long[] nodeI,
                                        out long[] nodeJ, out string reason)
        {
            reason = null;
            nodeI = null;
            nodeJ = null;
            string loopName = loop == 0 ? "The loop of the face" : "The hole starting at Edge " + firstEdge + " of the face";
            if (edgeDirections == null || edgeCounts == null || edgeDirections.Length != edgeCounts.Length)
            {
                reason = loopName + " has " + (edgeDirections == null ? 0 : edgeDirections.Length) +
                         " edge directions and " + (edgeCounts == null ? 0 : edgeCounts.Length) + " edge counts.";
                return false;
            }
            int numOfEdges = edgeDirections.Length;
            if (numOfEdges < 4)
            {
                reason = loopName + " has " + numOfEdges + " edges, and a lattice outline needs at least four.";
                return false;
            }
            for (int k = 0; k < numOfEdges; k++)
            {
                if (edgeDirections[k] < 0 || edgeDirections[k] > 3)
                {
                    reason = "Edge " + (firstEdge + k) + " of the face runs in direction " + edgeDirections[k] +
                             ", which is not one of 0, 1, 2 and 3 (+i, +j, -i and -j).";
                    return false;
                }
                if (edgeCounts[k] < 1)
                {
                    reason = "Edge " + (firstEdge + k) + " of the face carries " + edgeCounts[k] +
                             " elements, and every edge needs one.";
                    return false;
                }
            }
            for (int k = 0; k < numOfEdges; k++)
            {
                int next = (k + 1) % numOfEdges;
                if ((edgeDirections[next] - edgeDirections[k] + 4) % 4 == 2)
                {
                    reason = "Edges " + (firstEdge + k) + " and " + (firstEdge + next) + " of the face run in opposite " +
                             "directions, a reversal the lattice cannot follow.";
                    return false;
                }
            }
            //
            long[] sums = new long[4];
            long numOfNodes = 0;
            for (int k = 0; k < numOfEdges; k++)
            {
                sums[edgeDirections[k]] += edgeCounts[k];
                numOfNodes += edgeCounts[k];
            }
            if (sums[0] != sums[2] || sums[1] != sums[3])
            {
                reason = (loop == 0 ? "The edges of the face" : "The edges of the hole starting at Edge " + firstEdge) +
                         " carry " + sums[0] + " elements along +i and " + sums[2] + " along -i, " + sums[1] +
                         " along +j and " + sums[3] + " along -j, so " + (loop == 0 ? "the" : "its") +
                         " lattice outline does not close.";
                return false;
            }
            // A closed path of unit steps is simple when it visits no lattice point twice
            nodeI = new long[numOfNodes];
            nodeJ = new long[numOfNodes];
            HashSet<long> visited = new HashSet<long>();
            long i = 0;
            long j = 0;
            long twiceArea = 0;
            int p = 0;
            for (int k = 0; k < numOfEdges; k++)
            {
                for (int s = 0; s < edgeCounts[k]; s++)
                {
                    if (!visited.Add((i << 32) ^ (j & 0xFFFFFFFFL)))
                    {
                        reason = "The edge counts make the lattice outline of the face touch itself on edge " +
                                 (firstEdge + k) + ".";
                        return false;
                    }
                    nodeI[p] = i;
                    nodeJ[p] = j;
                    p++;
                    long nextI = i + DirectionI[edgeDirections[k]];
                    long nextJ = j + DirectionJ[edgeDirections[k]];
                    twiceArea += i * nextJ - nextI * j;
                    i = nextI;
                    j = nextJ;
                }
            }
            if (loop == 0 && twiceArea <= 0)
            {
                reason = "The edges of the face turn clockwise in the lattice; the ends must be left turns.";
                return false;
            }
            if (loop > 0 && twiceArea >= 0)
            {
                reason = "The edges of the hole starting at Edge " + firstEdge + " turn counter-clockwise in the lattice; " +
                         "the corners of a hole must be right turns.";
                return false;
            }
            return true;
        }
        /// <summary>
        /// Whether a lattice point off a loop lies inside it: whether a ray from the point along +i, raised by half a
        /// cell, crosses the edges along j of the loop, offset by oi and oj, an odd number of times.
        /// </summary>
        private static bool IsInsideLoop(long pi, long pj, long[] loopI, long[] loopJ, long oi, long oj)
        {
            bool inside = false;
            int n = loopI.Length;
            for (int p = 0; p < n; p++)
            {
                int q = (p + 1) % n;
                if (loopI[p] != loopI[q] || loopI[p] + oi <= pi) continue;
                if (Math.Min(loopJ[p], loopJ[q]) + oj == pj) inside = !inside;
            }
            return inside;
        }
        /// <summary>
        /// Maps the face whose edge k runs in the logical direction edgeDirections[k] (0, 1, 2, 3 for +i, +j, -i, -j)
        /// from loop vertex k to loop vertex k + 1 through the nodes edgeUv[k] in the parameter space and edgeXyz[k] in
        /// space; the last node of an edge is the first of the next. evaluate takes the parameters of nodes as u0, v0,
        /// u1, v1, ... and returns their coordinates as x0, y0, z0, x1, ...; without it the grid carries no coordinates
        /// of the inner nodes.
        /// </summary>
        public static SubmapGrid Map(int[] edgeDirections, double[][][] edgeUv, double[][][] edgeXyz,
                                     Func<double[], double[]> evaluate)
        {
            return Map(new int[][] { edgeDirections }, new double[][][][] { edgeUv }, new double[][][][] { edgeXyz },
                       new int[0][], evaluate);
        }
        /// <summary>
        /// Maps the face of several loops, the outer one first and then its holes, each given as the single loop of Map
        /// is, every hole placed by holeOffsets, the lattice offset of its first node from the first node of the outer
        /// loop. The boundary nodes of the grid run over the edges of all loops one after another.
        /// </summary>
        public static SubmapGrid Map(int[][] loopDirections, double[][][][] loopUv, double[][][][] loopXyz,
                                     int[][] holeOffsets, Func<double[], double[]> evaluate)
        {
            int numOfLoops = loopDirections == null ? 0 : loopDirections.Length;
            if (numOfLoops == 0 || loopUv == null || loopXyz == null || loopUv.Length != numOfLoops ||
                loopXyz.Length != numOfLoops)
                throw new CaeException("The face has " + numOfLoops + " loops of edge directions, " +
                                       (loopUv == null ? 0 : loopUv.Length) + " loops of parameters and " +
                                       (loopXyz == null ? 0 : loopXyz.Length) + " loops of coordinates.");
            int[][] loopCounts = new int[numOfLoops][];
            List<int> edgeDirectionList = new List<int>();
            List<int> edgeCountList = new List<int>();
            int[] loopNumOfEdges = new int[numOfLoops];
            int firstEdge = 0;
            for (int l = 0; l < numOfLoops; l++)
            {
                int[] edgeDirections = loopDirections[l];
                double[][][] edgeUv = loopUv[l];
                double[][][] edgeXyz = loopXyz[l];
                int numOfEdges = edgeDirections == null ? 0 : edgeDirections.Length;
                string loopName = l == 0 ? "The loop of the face" : "The hole starting at Edge " + firstEdge + " of the face";
                if (edgeUv == null || edgeXyz == null || edgeUv.Length != numOfEdges || edgeXyz.Length != numOfEdges)
                    throw new CaeException(loopName + " has " + numOfEdges + " edge directions, " +
                                           (edgeUv == null ? 0 : edgeUv.Length) + " edges of parameters and " +
                                           (edgeXyz == null ? 0 : edgeXyz.Length) + " edges of coordinates.");
                loopCounts[l] = new int[numOfEdges];
                for (int k = 0; k < numOfEdges; k++)
                {
                    if (edgeUv[k] == null || edgeXyz[k] == null || edgeXyz[k].Length != edgeUv[k].Length)
                        throw new CaeException("Edge " + (firstEdge + k) + " of the face has " +
                                               (edgeUv[k] == null ? 0 : edgeUv[k].Length) + " parameters and " +
                                               (edgeXyz[k] == null ? 0 : edgeXyz[k].Length) + " coordinates of its nodes.");
                    loopCounts[l][k] = edgeUv[k].Length - 1;
                    edgeDirectionList.Add(edgeDirections[k]);
                    edgeCountList.Add(loopCounts[l][k]);
                }
                loopNumOfEdges[l] = numOfEdges;
                firstEdge += numOfEdges;
            }
            string reason;
            if (!AreCountsValid(loopDirections, loopCounts, holeOffsets, out reason)) throw new CaeException(reason);
            // The logical coordinates of the boundary nodes of all loops, the smallest being 0, and their parameters
            int numOfBoundaryNodes = 0;
            foreach (var count in edgeCountList) numOfBoundaryNodes += count;
            int numOfOuterNodes = 0;
            foreach (var count in loopCounts[0]) numOfOuterNodes += count;
            int[] boundaryI = new int[numOfBoundaryNodes];
            int[] boundaryJ = new int[numOfBoundaryNodes];
            double[][] boundaryUv = new double[numOfBoundaryNodes][];
            int[] nextNodeIds = new int[numOfBoundaryNodes];
            int minI = 0;
            int minJ = 0;
            int id = 0;
            for (int l = 0; l < numOfLoops; l++)
            {
                int ci = l == 0 ? 0 : holeOffsets[l - 1][0];
                int cj = l == 0 ? 0 : holeOffsets[l - 1][1];
                int loopStart = id;
                for (int k = 0; k < loopCounts[l].Length; k++)
                {
                    for (int s = 0; s < loopCounts[l][k]; s++)
                    {
                        boundaryI[id] = ci;
                        boundaryJ[id] = cj;
                        boundaryUv[id] = loopUv[l][k][s];
                        nextNodeIds[id] = id + 1;
                        minI = Math.Min(minI, ci);
                        minJ = Math.Min(minJ, cj);
                        ci += DirectionI[loopDirections[l][k]];
                        cj += DirectionJ[loopDirections[l][k]];
                        id++;
                    }
                }
                nextNodeIds[id - 1] = loopStart;
            }
            int width = 0;
            int height = 0;
            for (int b = 0; b < numOfBoundaryNodes; b++)
            {
                boundaryI[b] -= minI;
                boundaryJ[b] -= minJ;
                width = Math.Max(width, boundaryI[b]);
                height = Math.Max(height, boundaryJ[b]);
            }
            int rowLength = width + 1;
            int[] nodeIds = new int[rowLength * (height + 1)];
            for (int p = 0; p < nodeIds.Length; p++) nodeIds[p] = -1;
            for (int b = 0; b < numOfBoundaryNodes; b++) nodeIds[boundaryJ[b] * rowLength + boundaryI[b]] = b;
            // The cells inside the outline, by the parity of the boundary edges along i that a row crosses
            bool[] verticalEdges = new bool[rowLength * height];
            for (int b = 0; b < numOfBoundaryNodes; b++)
            {
                int next = nextNodeIds[b];
                if (boundaryI[next] == boundaryI[b])
                    verticalEdges[Math.Min(boundaryJ[b], boundaryJ[next]) * rowLength + boundaryI[b]] = true;
            }
            bool[] cellsInside = new bool[width * height];
            for (int j = 0; j < height; j++)
            {
                bool inside = false;
                for (int i = 0; i < width; i++)
                {
                    if (verticalEdges[j * rowLength + i]) inside = !inside;
                    cellsInside[j * width + i] = inside;
                }
            }
            // The inner nodes row by row: the lattice points off the boundary next to a cell inside
            List<int[]> logical = new List<int[]>();
            for (int b = 0; b < numOfBoundaryNodes; b++) logical.Add(new int[] { boundaryI[b], boundaryJ[b] });
            for (int j = 1; j < height; j++)
            {
                for (int i = 1; i < width; i++)
                {
                    int p = j * rowLength + i;
                    if (nodeIds[p] < 0 && cellsInside[j * width + i])
                    {
                        nodeIds[p] = logical.Count;
                        logical.Add(new int[] { i, j });
                    }
                }
            }
            int numOfNodes = logical.Count;
            int numOfInnerNodes = numOfNodes - numOfBoundaryNodes;
            List<int[]> quads = new List<int[]>();
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    if (!cellsInside[j * width + i]) continue;
                    int p = j * rowLength + i;
                    quads.Add(new int[] { nodeIds[p], nodeIds[p + 1], nodeIds[p + 1 + rowLength], nodeIds[p + rowLength] });
                }
            }
            // The frame in which the parameters are close to isometric, fitted over the whole outer loop as one side
            double[][] outerUv = new double[numOfOuterNodes + 1][];
            double[][] outerXyz = new double[numOfOuterNodes + 1][];
            id = 0;
            for (int k = 0; k < loopCounts[0].Length; k++)
            {
                for (int s = 0; s < loopCounts[0][k]; s++)
                {
                    outerUv[id] = loopUv[0][k][s];
                    outerXyz[id] = loopXyz[0][k][s];
                    id++;
                }
            }
            outerUv[numOfOuterNodes] = outerUv[0];
            outerXyz[numOfOuterNodes] = outerXyz[0];
            double[] w = ThreeBlockMapping.GetMetricFrame(new double[][][] { outerUv, new double[0][], new double[0][] },
                                                          new double[][][] { outerXyz, new double[0][], new double[0][] });
            double[][] points = new double[numOfNodes][];
            for (int b = 0; b < numOfBoundaryNodes; b++) points[b] = ThreeBlockMapping.ToFrame(w, boundaryUv[b]);
            //
            SetLatticeLinePoints(points, nodeIds, logical, numOfBoundaryNodes, rowLength, height);
            int[][] neighbours = ThreeBlockMapping.GetNeighbours(numOfNodes, quads);
            int[][] quadArray = quads.ToArray();
            int[] cornerStart;
            int[] cornerQuads;
            int[] cornerIndices;
            GetCorners(quadArray, numOfNodes, numOfBoundaryNodes, out cornerStart, out cornerQuads, out cornerIndices);
            double orientation = ThreeBlockMapping.GetSignedArea(points, numOfOuterNodes) < 0 ? -1 : 1;
            int numOfStartFolds = CountFoldedQuads(points, quadArray, orientation);
            double[][] start = CopyPoints(points);
            ThreeBlockMapping.SolveBarycentricEmbedding(points, neighbours, numOfBoundaryNodes);
            SolveWinslow(points, nodeIds, logical, numOfBoundaryNodes, rowLength);
            if (CountFoldedQuads(points, quadArray, orientation) > 0)
                Untangle(points, neighbours, quadArray, cornerStart, cornerQuads, cornerIndices, numOfBoundaryNodes,
                         orientation);
            LowerConditionEnergy(points, neighbours, quadArray, cornerStart, cornerQuads, cornerIndices, numOfBoundaryNodes,
                                 orientation);
            int numOfFoldedQuads = CountFoldedQuads(points, quadArray, orientation);
            if (numOfFoldedQuads > 0 && numOfStartFolds == 0)
            {
                RelaxWithoutFolding(start, neighbours, quadArray, numOfBoundaryNodes, orientation);
                LowerConditionEnergy(start, neighbours, quadArray, cornerStart, cornerQuads, cornerIndices,
                                     numOfBoundaryNodes, orientation);
                points = start;
                numOfFoldedQuads = CountFoldedQuads(points, quadArray, orientation);
            }
            //
            double[][] innerUv = new double[numOfInnerNodes][];
            for (int n = 0; n < numOfInnerNodes; n++)
                innerUv[n] = ThreeBlockMapping.FromFrame(w, points[numOfBoundaryNodes + n]);
            double[][] innerXyz = null;
            if (evaluate != null)
            {
                double[] flatUv = new double[2 * numOfInnerNodes];
                for (int n = 0; n < numOfInnerNodes; n++)
                {
                    flatUv[2 * n] = innerUv[n][0];
                    flatUv[2 * n + 1] = innerUv[n][1];
                }
                double[] flatXyz = numOfInnerNodes > 0 ? evaluate(flatUv) : new double[0];
                if (flatXyz == null || flatXyz.Length != 3 * numOfInnerNodes)
                    throw new CaeException("The inner nodes of the face could not be placed on its surface.");
                innerXyz = new double[numOfInnerNodes][];
                for (int n = 0; n < numOfInnerNodes; n++)
                    innerXyz[n] = new double[] { flatXyz[3 * n], flatXyz[3 * n + 1], flatXyz[3 * n + 2] };
            }
            return new SubmapGrid(edgeDirectionList.ToArray(), edgeCountList.ToArray(), loopNumOfEdges, logical.ToArray(),
                                  innerUv, innerXyz, quadArray, numOfFoldedQuads,
                                  GetMinScaledJacobian(points, quadArray, orientation));
        }
        /// <summary>
        /// Places every inner node at the mean of the two lattice lines through it, along i and along j, each a straight
        /// line between the two boundary nodes it ends on, divided by the lattice distance.
        /// </summary>
        private static void SetLatticeLinePoints(double[][] points, int[] nodeIds, List<int[]> logical,
                                                 int numOfBoundaryNodes, int rowLength, int height)
        {
            int numOfInnerNodes = points.Length - numOfBoundaryNodes;
            int[] first = new int[numOfInnerNodes];
            int[] last = new int[numOfInnerNodes];
            // Along i
            for (int j = 0; j <= height; j++)
            {
                int boundaryNode = -1;
                for (int i = 0; i < rowLength; i++) boundaryNode = SetLineEnd(nodeIds[j * rowLength + i], boundaryNode,
                                                                             numOfBoundaryNodes, first);
                boundaryNode = -1;
                for (int i = rowLength - 1; i >= 0; i--) boundaryNode = SetLineEnd(nodeIds[j * rowLength + i], boundaryNode,
                                                                                  numOfBoundaryNodes, last);
            }
            double[][] alongI = new double[numOfInnerNodes][];
            for (int n = 0; n < numOfInnerNodes; n++)
                alongI[n] = Interpolate(points[first[n]], points[last[n]], logical[first[n]][0],
                                        logical[last[n]][0], logical[numOfBoundaryNodes + n][0]);
            // Along j
            for (int i = 0; i < rowLength; i++)
            {
                int boundaryNode = -1;
                for (int j = 0; j <= height; j++) boundaryNode = SetLineEnd(nodeIds[j * rowLength + i], boundaryNode,
                                                                           numOfBoundaryNodes, first);
                boundaryNode = -1;
                for (int j = height; j >= 0; j--) boundaryNode = SetLineEnd(nodeIds[j * rowLength + i], boundaryNode,
                                                                           numOfBoundaryNodes, last);
            }
            for (int n = 0; n < numOfInnerNodes; n++)
            {
                double[] alongJ = Interpolate(points[first[n]], points[last[n]], logical[first[n]][1],
                                              logical[last[n]][1], logical[numOfBoundaryNodes + n][1]);
                points[numOfBoundaryNodes + n] = new double[] { 0.5 * (alongI[n][0] + alongJ[0]),
                                                                0.5 * (alongI[n][1] + alongJ[1]) };
            }
        }
        /// <summary>
        /// Walking along a lattice line, records for an inner node the last boundary node passed, and returns the last
        /// boundary node passed so far.
        /// </summary>
        private static int SetLineEnd(int nodeId, int boundaryNode, int numOfBoundaryNodes, int[] lineEnds)
        {
            if (nodeId < 0) return boundaryNode;
            if (nodeId < numOfBoundaryNodes) return nodeId;
            lineEnds[nodeId - numOfBoundaryNodes] = boundaryNode;
            return boundaryNode;
        }
        private static double[] Interpolate(double[] a, double[] b, int ta, int tb, int t)
        {
            double f = (double)(t - ta) / (tb - ta);
            return new double[] { a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f };
        }
        /// <summary>
        /// Moves the nodes after the boundary ones, in turn and sweep after sweep, towards the mean of their neighbours,
        /// by the whole way or by a half, a quarter or an eighth of it, whichever first folds none of their quads, until
        /// no node moves by more than the tolerance; a grid that does not fold stays so.
        /// </summary>
        private static void RelaxWithoutFolding(double[][] points, int[][] neighbours, int[][] quads,
                                                int numOfBoundaryNodes, double orientation)
        {
            int numOfNodes = points.Length;
            int[] quadStart = new int[numOfNodes + 1];
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++) quadStart[quad[c] + 1]++;
            }
            for (int n = 0; n < numOfNodes; n++) quadStart[n + 1] += quadStart[n];
            int[] nodeQuads = new int[quadStart[numOfNodes]];
            int[] fill = (int[])quadStart.Clone();
            for (int q = 0; q < quads.Length; q++)
            {
                for (int c = 0; c < 4; c++) nodeQuads[fill[quads[q][c]]++] = q;
            }
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            for (int b = 0; b < numOfBoundaryNodes; b++)
            {
                minX = Math.Min(minX, points[b][0]);
                maxX = Math.Max(maxX, points[b][0]);
                minY = Math.Min(minY, points[b][1]);
                maxY = Math.Max(maxY, points[b][1]);
            }
            double tolerance = GuardedTolerance * Math.Max(maxX - minX, maxY - minY);
            //
            for (int sweep = 0; sweep < MaxNumOfGuardedSweeps; sweep++)
            {
                double maxMove = 0;
                for (int node = numOfBoundaryNodes; node < numOfNodes; node++)
                {
                    double[] point = points[node];
                    double x0 = point[0];
                    double y0 = point[1];
                    double meanX = 0;
                    double meanY = 0;
                    foreach (var neighbour in neighbours[node])
                    {
                        meanX += points[neighbour][0];
                        meanY += points[neighbour][1];
                    }
                    meanX /= neighbours[node].Length;
                    meanY /= neighbours[node].Length;
                    double fraction = 1;
                    for (int trial = 0; trial < NumOfGuardedStepTrials; trial++)
                    {
                        point[0] = x0 + (meanX - x0) * fraction;
                        point[1] = y0 + (meanY - y0) * fraction;
                        if (!IsAnyQuadFolded(points, quads, nodeQuads, quadStart[node], quadStart[node + 1], orientation))
                            break;
                        point[0] = x0;
                        point[1] = y0;
                        fraction *= 0.5;
                    }
                    maxMove = Math.Max(maxMove, Math.Max(Math.Abs(point[0] - x0), Math.Abs(point[1] - y0)));
                }
                if (maxMove <= tolerance) break;
            }
        }
        /// <summary>
        /// Moves the inner nodes, in turn and sweep after sweep, to where the discrete Winslow equations put them: the
        /// logical coordinates are harmonic over the face rather than the parameters over the lattice, which keeps the
        /// lattice lines smooth around a corner of 270 degrees. Started from the harmonic map, the sweeps soon settle
        /// the nodes near such corners, where the two maps differ most; the slow rest of the convergence is left to the
        /// condition energy descent, which changes the grid anyway.
        /// </summary>
        private static void SolveWinslow(double[][] points, int[] nodeIds, List<int[]> logical, int numOfBoundaryNodes,
                                         int rowLength)
        {
            int numOfInnerNodes = points.Length - numOfBoundaryNodes;
            if (numOfInnerNodes == 0) return;
            // The eight neighbours of every inner node: +i, -i, +j, -j, then +i +j, -i +j, +i -j, -i -j
            int[][] stencils = new int[numOfInnerNodes][];
            for (int n = 0; n < numOfInnerNodes; n++)
            {
                int p = logical[numOfBoundaryNodes + n][1] * rowLength + logical[numOfBoundaryNodes + n][0];
                stencils[n] = new int[] { nodeIds[p + 1], nodeIds[p - 1], nodeIds[p + rowLength], nodeIds[p - rowLength],
                                          nodeIds[p + 1 + rowLength], nodeIds[p - 1 + rowLength],
                                          nodeIds[p + 1 - rowLength], nodeIds[p - 1 - rowLength] };
            }
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            for (int b = 0; b < numOfBoundaryNodes; b++)
            {
                minX = Math.Min(minX, points[b][0]);
                maxX = Math.Max(maxX, points[b][0]);
                minY = Math.Min(minY, points[b][1]);
                maxY = Math.Max(maxY, points[b][1]);
            }
            double tolerance = WinslowTolerance * Math.Max(maxX - minX, maxY - minY);
            //
            for (int sweep = 0; sweep < MaxNumOfWinslowSweeps; sweep++)
            {
                double maxMove = 0;
                for (int n = 0; n < numOfInnerNodes; n++)
                {
                    int[] s = stencils[n];
                    double[] e = points[s[0]];
                    double[] w = points[s[1]];
                    double[] north = points[s[2]];
                    double[] south = points[s[3]];
                    double xXi = 0.5 * (e[0] - w[0]);
                    double yXi = 0.5 * (e[1] - w[1]);
                    double xEta = 0.5 * (north[0] - south[0]);
                    double yEta = 0.5 * (north[1] - south[1]);
                    double alpha = xEta * xEta + yEta * yEta;
                    double beta = xXi * xEta + yXi * yEta;
                    double gamma = xXi * xXi + yXi * yXi;
                    double denominator = 2 * (alpha + gamma);
                    if (!(denominator > 0)) continue;
                    double[] point = points[numOfBoundaryNodes + n];
                    for (int d = 0; d < 2; d++)
                    {
                        double cross = points[s[4]][d] - points[s[5]][d] - points[s[6]][d] + points[s[7]][d];
                        double target = (alpha * (e[d] + w[d]) + gamma * (north[d] + south[d]) - 0.5 * beta * cross) /
                                        denominator;
                        double move = target - point[d];
                        maxMove = Math.Max(maxMove, Math.Abs(move));
                        point[d] += move;
                    }
                }
                if (maxMove <= tolerance) break;
            }
        }
        /// <summary>
        /// Moves the inner nodes of folded quads, in turn and sweep after sweep, by a pattern search that only takes a
        /// step when it raises the smallest scaled Jacobian of the quad corners the node changes, until no quad folds
        /// or no node moves.
        /// </summary>
        private static void Untangle(double[][] points, int[][] neighbours, int[][] quads, int[] cornerStart,
                                     int[] cornerQuads, int[] cornerIndices, int numOfBoundaryNodes, double orientation)
        {
            int numOfNodes = points.Length;
            double[][] directions = GetSearchDirections();
            //
            for (int sweep = 0; sweep < MaxNumOfUntangleSweeps; sweep++)
            {
                bool folded = false;
                bool moved = false;
                for (int node = numOfBoundaryNodes; node < numOfNodes; node++)
                {
                    double current = GetMinScaledJacobian(points, quads, cornerStart, cornerQuads, cornerIndices, node,
                                                          orientation);
                    if (current > 0) continue;
                    folded = true;
                    double[] point = points[node];
                    double length = 0;
                    foreach (var neighbour in neighbours[node])
                    {
                        double dx = points[neighbour][0] - point[0];
                        double dy = points[neighbour][1] - point[1];
                        length += Math.Sqrt(dx * dx + dy * dy);
                    }
                    double step = 0.5 * length / neighbours[node].Length;
                    for (int trial = 0; trial < NumOfUntangleStepTrials; trial++)
                    {
                        double x0 = point[0];
                        double y0 = point[1];
                        double best = current + 1E-12;
                        int bestDirection = -1;
                        for (int d = 0; d < directions.Length; d++)
                        {
                            point[0] = x0 + directions[d][0] * step;
                            point[1] = y0 + directions[d][1] * step;
                            double q = GetMinScaledJacobian(points, quads, cornerStart, cornerQuads, cornerIndices, node,
                                                            orientation);
                            if (q > best)
                            {
                                best = q;
                                bestDirection = d;
                            }
                        }
                        if (bestDirection >= 0)
                        {
                            point[0] = x0 + directions[bestDirection][0] * step;
                            point[1] = y0 + directions[bestDirection][1] * step;
                            current = best;
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
                if (!folded || !moved) break;
            }
        }
        /// <summary>
        /// The condition energy descent of ThreeBlockMapping.LowerConditionEnergy, step for step, but with more sweeps,
        /// and visiting a node only when it or a node of its quads moved since its last visit; any other visit would
        /// find the same energies and move nothing, so the sweeps give the same grid, but a sweep over a large grid that
        /// has settled costs little.
        /// </summary>
        private static void LowerConditionEnergy(double[][] points, int[][] neighbours, int[][] quads, int[] cornerStart,
                                                 int[] cornerQuads, int[] cornerIndices, int numOfBoundaryNodes,
                                                 double orientation)
        {
            int numOfNodes = points.Length;
            bool[] changed = new bool[numOfNodes];
            for (int node = numOfBoundaryNodes; node < numOfNodes; node++) changed[node] = true;
            double[][] directions = GetSearchDirections();
            for (int sweep = 0; sweep < MaxNumOfDescentSweeps; sweep++)
            {
                bool moved = false;
                for (int node = numOfBoundaryNodes; node < numOfNodes; node++)
                {
                    if (!changed[node]) continue;
                    changed[node] = false;
                    if (cornerStart[node] == cornerStart[node + 1]) continue;
                    double[] point = points[node];
                    double length = 0;
                    foreach (var neighbour in neighbours[node])
                    {
                        double dx = points[neighbour][0] - point[0];
                        double dy = points[neighbour][1] - point[1];
                        length += Math.Sqrt(dx * dx + dy * dy);
                    }
                    double step = DescentStepFactor * length / neighbours[node].Length;
                    double current = ThreeBlockMapping.GetNodeQuality(points, quads, cornerStart, cornerQuads,
                                                                      cornerIndices, node, orientation);
                    bool nodeMoved = false;
                    for (int trial = 0; trial < NumOfDescentStepTrials; trial++)
                    {
                        double x0 = point[0];
                        double y0 = point[1];
                        double bestQuality = double.IsNegativeInfinity(current) ? double.MinValue :
                                             current + DescentMinImprovement * Math.Max(1, Math.Abs(current));
                        int best = -1;
                        for (int d = 0; d < directions.Length; d++)
                        {
                            point[0] = x0 + directions[d][0] * step;
                            point[1] = y0 + directions[d][1] * step;
                            double q = ThreeBlockMapping.GetNodeQuality(points, quads, cornerStart, cornerQuads,
                                                                        cornerIndices, node, orientation);
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
                            nodeMoved = true;
                        }
                        else
                        {
                            point[0] = x0;
                            point[1] = y0;
                            step *= 0.5;
                        }
                    }
                    if (!nodeMoved) continue;
                    moved = true;
                    for (int i = cornerStart[node]; i < cornerStart[node + 1]; i++)
                    {
                        foreach (var quadNode in quads[cornerQuads[i]]) changed[quadNode] = true;
                    }
                }
                if (!moved) break;
            }
        }
        /// <summary>
        /// The corners every inner node changes, as quad index and corner index, the nodes after each other in the
        /// order of ThreeBlockMapping.LowerConditionEnergy.
        /// </summary>
        private static void GetCorners(int[][] quads, int numOfNodes, int numOfBoundaryNodes, out int[] cornerStart,
                                       out int[] cornerQuads, out int[] cornerIndices)
        {
            cornerStart = new int[numOfNodes + 1];
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    for (int o = 0; o < 3; o++)
                    {
                        int node = quad[(c + CornerOffsets[o]) % 4];
                        if (node >= numOfBoundaryNodes) cornerStart[node + 1]++;
                    }
                }
            }
            for (int n = 0; n < numOfNodes; n++) cornerStart[n + 1] += cornerStart[n];
            cornerQuads = new int[cornerStart[numOfNodes]];
            cornerIndices = new int[cornerQuads.Length];
            int[] fill = (int[])cornerStart.Clone();
            for (int q = 0; q < quads.Length; q++)
            {
                for (int c = 0; c < 4; c++)
                {
                    for (int o = 0; o < 3; o++)
                    {
                        int node = quads[q][(c + CornerOffsets[o]) % 4];
                        if (node < numOfBoundaryNodes) continue;
                        cornerQuads[fill[node]] = q;
                        cornerIndices[fill[node]] = c;
                        fill[node]++;
                    }
                }
            }
        }
        private static double[][] GetSearchDirections()
        {
            double diagonal = Math.Sqrt(0.5);
            return new double[][]
            {
                new double[] { 1, 0 }, new double[] { -1, 0 }, new double[] { 0, 1 }, new double[] { 0, -1 },
                new double[] { diagonal, diagonal }, new double[] { -diagonal, diagonal },
                new double[] { diagonal, -diagonal }, new double[] { -diagonal, -diagonal }
            };
        }
        private static double GetMinScaledJacobian(double[][] points, int[][] quads, int[] cornerStart, int[] cornerQuads,
                                                   int[] cornerIndices, int node, double orientation)
        {
            double min = double.MaxValue;
            for (int i = cornerStart[node]; i < cornerStart[node + 1]; i++)
                min = Math.Min(min, ThreeBlockMapping.GetScaledJacobian(points, quads[cornerQuads[i]], cornerIndices[i],
                                                                        orientation));
            return min;
        }
        private static double GetMinScaledJacobian(double[][] points, int[][] quads, double orientation)
        {
            double min = 1;
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                    min = Math.Min(min, ThreeBlockMapping.GetScaledJacobian(points, quad, c, orientation));
            }
            return min;
        }
        private static bool IsAnyQuadFolded(double[][] points, int[][] quads, int[] nodeQuads, int start, int end,
                                            double orientation)
        {
            for (int i = start; i < end; i++)
            {
                int[] quad = quads[nodeQuads[i]];
                for (int c = 0; c < 4; c++)
                {
                    if (ThreeBlockMapping.GetScaledJacobian(points, quad, c, orientation) <= 0) return true;
                }
            }
            return false;
        }
        private static double[][] CopyPoints(double[][] points)
        {
            double[][] copy = new double[points.Length][];
            for (int n = 0; n < points.Length; n++) copy[n] = new double[] { points[n][0], points[n][1] };
            return copy;
        }
        private static int CountFoldedQuads(double[][] points, int[][] quads, double orientation)
        {
            int numOfFoldedQuads = 0;
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (ThreeBlockMapping.GetScaledJacobian(points, quad, c, orientation) <= 0)
                    {
                        numOfFoldedQuads++;
                        break;
                    }
                }
            }
            return numOfFoldedQuads;
        }
    }
}
