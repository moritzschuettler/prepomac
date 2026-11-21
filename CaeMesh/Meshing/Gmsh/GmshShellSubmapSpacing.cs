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
using System.Linq;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// Pairs the boundary nodes visible to each other along a submap lattice, including its cornered holes.
    /// </summary>
    internal static class GmshShellSubmapSpacing
    {
        // Variables                                                                                                   
        private static readonly int[] _directionI = { 1, 0, -1, 0 };
        private static readonly int[] _directionJ = { 0, 1, 0, -1 };


        // Methods                                                                                                     
        public static List<(GmshShellSpacingNode First, GmshShellSpacingNode Second)>
            GetPairs(SubmapFace face, Dictionary<int, GmshShellSpacingEdge> edges)
        {
            SubmapFace[] loops = face.Loops;
            Dictionary<int, int> edgeCounts = new Dictionary<int, int>();
            foreach (SubmapFace loop in loops)
            {
                foreach (int id in loop.EdgeIds)
                {
                    GmshShellSpacingEdge edge = edges[id];
                    if (edge == null) return null;
                    edgeCounts.Add(id, edge.NumOfNodes - 1);
                }
            }
            if (face.GetIndivisibleCountReason(edgeCounts) != null) return null;
            int[][] directions = face.LoopLogicalEdgeDirections;
            int[][] counts = face.GetLoopLogicalEdgeCounts(edgeCounts);
            double[] lengths = face.EdgeIds.Select(id => edges[id].Length).ToArray();
            double[][] outer = SubmapFaceDetector.GetIdealOutline(directions[0], face.GetLogicalEdgeLengths(lengths));
            if (outer == null) return null;
            //
            var loopNodes = new (GmshShellSpacingEdge Edge, int Index, int Step)[loops.Length][];
            double[][][] outlines = new double[loops.Length][][];
            outlines[0] = outer;
            for (int l = 0; l < loops.Length; l++)
            {
                loopNodes[l] = ReadLoop(loops[l], edges, counts[l].Sum());
                if (loopNodes[l] == null || counts[l].Any(count => count <= 0)) return null;
            }
            int[][] offsets = null;
            if (face.HasHoles)
            {
                double[][][] uv = new double[loops.Length][][];
                try
                {
                    for (int l = 0; l < loops.Length; l++)
                        uv[l] = GmshThreeBlockMesher.GetParametricCoordinates(face.FaceId,
                            loopNodes[l].Select(node => node.Edge.GetPoint(node.Index)).ToList());
                }
                catch (CaeException)
                {
                    return null;
                }
                if (!SubmapHolePlacement.TryPlace(directions, counts, uv, 1, out offsets, out _)) return null;
                if (!MapHoleOutlines(uv, counts, outlines)) return null;
            }
            //
            var nodes = new List<(int I, int J, GmshShellSpacingNode X, GmshShellSpacingNode Y)>();
            var positions = new HashSet<(int I, int J)>();
            int[][] cornerI = new int[loops.Length][];
            int[][] cornerJ = new int[loops.Length][];
            for (int l = 0; l < loops.Length; l++)
            {
                int i = l == 0 ? 0 : offsets[l - 1][0];
                int j = l == 0 ? 0 : offsets[l - 1][1];
                int numOfSegments = counts[l].Length;
                cornerI[l] = new int[numOfSegments + 1];
                cornerJ[l] = new int[numOfSegments + 1];
                int firstNode = 0;
                for (int k = 0; k < numOfSegments; k++)
                {
                    cornerI[l][k] = i;
                    cornerJ[l][k] = j;
                    int di = _directionI[directions[l][k]];
                    int dj = _directionJ[directions[l][k]];
                    int count = counts[l][k];
                    var start = loopNodes[l][firstNode];
                    double from = start.Edge.GetFraction(start.Index);
                    double span = start.Edge.GetFraction(start.Index + start.Step * count) - from;
                    if (span == 0) return null;
                    double[] a = outlines[l][k];
                    double[] b = outlines[l][(k + 1) % numOfSegments];
                    double sx = (b[0] - a[0]) / span;
                    double sy = (b[1] - a[1]) / span;
                    for (int n = 0; n < count; n++, firstNode++, i += di, j += dj)
                    {
                        if (!positions.Add((i, j))) return null;
                        var node = loopNodes[l][firstNode];
                        nodes.Add((i, j, new GmshShellSpacingNode(node.Edge, node.Index, a[0] - from * sx, sx),
                                         new GmshShellSpacingNode(node.Edge, node.Index, a[1] - from * sy, sy)));
                    }
                }
                cornerI[l][numOfSegments] = i;
                cornerJ[l][numOfSegments] = j;
                if (i != cornerI[l][0] || j != cornerJ[l][0]) return null;
            }
            //
            var pairs = new List<(GmshShellSpacingNode First, GmshShellSpacingNode Second)>();
            foreach (var row in nodes.GroupBy(node => node.J))
            {
                var ordered = row.OrderBy(node => node.I).ToArray();
                for (int n = 1; n < ordered.Length; n++)
                {
                    double middle = 0.5 * (ordered[n - 1].I + ordered[n].I);
                    if (IsInterior(cornerI, cornerJ, middle, row.Key))
                        pairs.Add((ordered[n - 1].Y, ordered[n].Y));
                }
            }
            foreach (var column in nodes.GroupBy(node => node.I))
            {
                var ordered = column.OrderBy(node => node.J).ToArray();
                for (int n = 1; n < ordered.Length; n++)
                {
                    double middle = 0.5 * (ordered[n - 1].J + ordered[n].J);
                    if (IsInterior(cornerJ, cornerI, middle, column.Key))
                        pairs.Add((ordered[n - 1].X, ordered[n].X));
                }
            }
            return pairs;
        }
        private static (GmshShellSpacingEdge Edge, int Index, int Step)[]
            ReadLoop(SubmapFace loop, Dictionary<int, GmshShellSpacingEdge> edges, int count)
        {
            var nodes = new (GmshShellSpacingEdge Edge, int Index, int Step)[count];
            int node = 0;
            for (int e = 0; e < loop.EdgeIds.Length; e++)
            {
                GmshShellSpacingEdge edge = edges[loop.EdgeIds[e]];
                bool reversed = loop.VertexNodeIds[e] == edge.EndVertexId;
                if (!reversed && loop.VertexNodeIds[e] != edge.StartVertexId) return null;
                if (loop.VertexNodeIds[(e + 1) % loop.EdgeIds.Length] !=
                    (reversed ? edge.StartVertexId : edge.EndVertexId)) return null;
                int last = edge.NumOfNodes - 1;
                for (int k = 0; k < last; k++)
                    nodes[node++] = (edge, reversed ? last - k : k, reversed ? -1 : 1);
            }
            return nodes;
        }
        private static bool MapHoleOutlines(double[][][] uv, int[][] counts, double[][][] outlines)
        {
            double[][] outer = outlines[0];
            int count = outer.Length;
            double u0 = 0;
            double v0 = 0;
            double x0 = 0;
            double y0 = 0;
            for (int k = 0, n = 0; k < count; n += counts[0][k], k++)
            {
                u0 += uv[0][n][0];
                v0 += uv[0][n][1];
                x0 += outer[k][0];
                y0 += outer[k][1];
            }
            u0 /= count;
            v0 /= count;
            x0 /= count;
            y0 /= count;
            double uu = 0;
            double uvSum = 0;
            double vv = 0;
            double ux = 0;
            double vx = 0;
            double uy = 0;
            double vy = 0;
            for (int k = 0, n = 0; k < count; n += counts[0][k], k++)
            {
                double u = uv[0][n][0] - u0;
                double v = uv[0][n][1] - v0;
                double x = outer[k][0] - x0;
                double y = outer[k][1] - y0;
                uu += u * u;
                uvSum += u * v;
                vv += v * v;
                ux += u * x;
                vx += v * x;
                uy += u * y;
                vy += v * y;
            }
            double determinant = uu * vv - uvSum * uvSum;
            if (!(determinant > 1E-12 * uu * vv)) return false;
            double xu = (vv * ux - uvSum * vx) / determinant;
            double xv = (uu * vx - uvSum * ux) / determinant;
            double yu = (vv * uy - uvSum * vy) / determinant;
            double yv = (uu * vy - uvSum * uy) / determinant;
            for (int l = 1; l < outlines.Length; l++)
            {
                outlines[l] = new double[counts[l].Length][];
                for (int k = 0, n = 0; k < counts[l].Length; n += counts[l][k], k++)
                {
                    double u = uv[l][n][0] - u0;
                    double v = uv[l][n][1] - v0;
                    outlines[l][k] = new double[] { x0 + xu * u + xv * v, y0 + yu * u + yv * v };
                }
            }
            return true;
        }
        // Both sides of a lattice line must be inside, including where it passes through a reentrant corner.
        private static bool IsInterior(int[][] along, int[][] across, double midpoint, int line)
        {
            bool below = false;
            bool above = false;
            for (int l = 0; l < along.Length; l++)
            {
                for (int k = 0; k < along[l].Length - 1; k++)
                {
                    if (along[l][k] != along[l][k + 1] || along[l][k] <= midpoint) continue;
                    int low = Math.Min(across[l][k], across[l][k + 1]);
                    int high = Math.Max(across[l][k], across[l][k + 1]);
                    if (low < line && line <= high) below = !below;
                    if (low <= line && line < high) above = !above;
                }
            }
            return below && above;
        }
    }
}
