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
using System.Text.RegularExpressions;
using CaeGlobals;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// Meshes a submapped face by SubmapMapping, the structured mapping every mesher shares, the way
    /// GmshThreeBlockMesher meshes a three-sided face: gmsh supplies the nodes its edges carry, their parameters on
    /// the surface and the evaluation of the surface, and the elements gmsh meshed on the face are replaced. A grid
    /// that cannot be made, folds or has a quad corner below SubmapFaceDetector.MinScaledJacobian leaves the face as
    /// gmsh meshed it. An edge that turns in its middle is split at its middle node into the two logical edges of the
    /// face, so it needs an even number of elements; one that turns twice at its nodes at a quarter and three quarters
    /// into three, so it needs a multiple of four. The nodes of the holes of a face are read loop by loop as those of
    /// its outer loop, and the holes are placed in the lattice by SubmapHolePlacement on the parameters of the nodes;
    /// a hole for which no place is found leaves the face as gmsh meshed it. The nodes of a round hole are read around
    /// it and split into its four logical edges from the node SubmapHolePlacement.GetRoundLoopOrder finds, so its edges
    /// must carry as many elements each and a multiple of four around it.
    /// </summary>
    public static class GmshSubmapMesher
    {
        // Variables
        private static readonly Regex LoopEdge = new Regex(@"(?<![A-Za-z])[Ee]dge(s?) (\d+)(?: and (\d+))?",
                                                           RegexOptions.Compiled);


        // Methods                                                                                                                  
        /// <summary>
        /// The face carries gmsh ids: its surface, its vertices and its edges, those of its holes as well. Without quads,
        /// as for a surface that is not recombined, every quad is split into two triangles. False, with the reason, when
        /// the face is left as gmsh meshed it.
        /// </summary>
        public static bool Mesh(SubmapFace face, bool quads, out string reason)
        {
            SubmapGrid grid;
            List<IntPtr>[][] loopTags;
            double[][][][] loopXyz;
            double[][][][] loopUv;
            if (!Map(face, out grid, out loopTags, out loopXyz, out loopUv, out reason)) return false;
            // The tags of the boundary nodes of all loops in loop order, and the parameters of the outer loop, which
            // tell the way the grid turns
            IntPtr[] boundaryNodeTags = new IntPtr[grid.NumberOfBoundaryNodes];
            double[][] outerUv = new double[grid.NumberOfOuterBoundaryNodes][];
            int edge = 0;
            for (int l = 0; l < loopTags.Length; l++)
            {
                for (int k = 0; k < loopTags[l].Length; k++, edge++)
                {
                    for (int i = 0; i < grid.EdgeCounts[edge]; i++)
                    {
                        int nodeId = grid.GetBoundaryNodeId(edge, i);
                        boundaryNodeTags[nodeId] = loopTags[l][k][i];
                        if (l == 0) outerUv[nodeId] = loopUv[l][k][i];
                    }
                }
            }
            GmshThreeBlockMesher.ReplaceSurfaceMesh(face.FaceId, boundaryNodeTags, outerUv, grid.Quads, grid.InnerUv,
                                                    grid.InnerXyz, quads);
            return true;
        }
        /// <summary>
        /// Whether the grid of the face, which carries gmsh ids and whose edges gmsh has meshed, would be kept, and its
        /// worst scaled Jacobian of a quad corner in space against the normal of the surface, at a pole against the
        /// normal of its own quad, as GmshLensMesher judges a lens grid. Nothing is written into the surface.
        /// </summary>
        public static bool Evaluate(SubmapFace face, out double minScaledJacobian, out string reason)
        {
            minScaledJacobian = double.NaN;
            SubmapGrid grid;
            List<IntPtr>[][] loopTags;
            double[][][][] loopXyz;
            double[][][][] loopUv;
            if (!Map(face, out grid, out loopTags, out loopXyz, out loopUv, out reason)) return false;
            // The inner nodes are keyed by negative tags, which no node of gmsh carries
            IntPtr[] nodeTags = new IntPtr[grid.NumberOfBoundaryNodes + grid.InnerUv.Length];
            Dictionary<IntPtr, double[]> tagXyz = new Dictionary<IntPtr, double[]>();
            Dictionary<IntPtr, double[]> tagUv = new Dictionary<IntPtr, double[]>();
            List<double[]> cornerXyz = new List<double[]>();
            int edge = 0;
            for (int l = 0; l < loopTags.Length; l++)
            {
                for (int k = 0; k < loopTags[l].Length; k++, edge++)
                {
                    cornerXyz.Add(loopXyz[l][k][0]);
                    for (int i = 0; i < grid.EdgeCounts[edge]; i++)
                    {
                        int nodeId = grid.GetBoundaryNodeId(edge, i);
                        nodeTags[nodeId] = loopTags[l][k][i];
                        tagXyz[nodeTags[nodeId]] = loopXyz[l][k][i];
                        tagUv[nodeTags[nodeId]] = loopUv[l][k][i];
                    }
                }
            }
            for (int n = 0; n < grid.InnerUv.Length; n++)
            {
                IntPtr tag = new IntPtr(-1 - n);
                nodeTags[grid.NumberOfBoundaryNodes + n] = tag;
                tagXyz[tag] = grid.InnerXyz[n];
                tagUv[tag] = grid.InnerUv[n];
            }
            IntPtr[] quadNodeTags = grid.Quads.SelectMany(quad => quad.Select(nodeId => nodeTags[nodeId])).ToArray();
            int numOfFoldedQuads;
            minScaledJacobian = GmshSeamGridMesher.GetMinScaledJacobian(face.FaceId, quadNodeTags, tagXyz, tagUv,
                                                                        GetPoleTags(face.FaceId, loopTags, cornerXyz),
                                                                        out numOfFoldedQuads);
            if (numOfFoldedQuads > 0)
            {
                reason = numOfFoldedQuads + " of the " + grid.Quads.Length + " quads of its grid fold in space";
                return false;
            }
            return true;
        }
        /// <summary>
        /// The tags of the nodes at the start of the logical edges, cornerXyz, that are poles of the surface.
        /// </summary>
        private static HashSet<IntPtr> GetPoleTags(int surfaceId, List<IntPtr>[][] loopTags, List<double[]> cornerXyz)
        {
            HashSet<IntPtr> poleTags = new HashSet<IntPtr>();
            double[] low = new double[] { double.MaxValue, double.MaxValue, double.MaxValue };
            double[] high = new double[] { double.MinValue, double.MinValue, double.MinValue };
            foreach (var point in cornerXyz)
            {
                for (int c = 0; c < 3; c++)
                {
                    low[c] = Math.Min(low[c], point[c]);
                    high[c] = Math.Max(high[c], point[c]);
                }
            }
            double size = GmshPoleChart.GetDistance(low, high);
            int p;
            double poleV;
            double farV;
            int corner = 0;
            for (int l = 0; l < loopTags.Length; l++)
            {
                for (int k = 0; k < loopTags[l].Length; k++, corner++)
                {
                    if (GmshPoleChart.FindPole(surfaceId, cornerXyz[corner], size, out p, out poleV, out farV))
                        poleTags.Add(loopTags[l][k][0]);
                }
            }
            return poleTags;
        }
        /// <summary>
        /// The grid of the face on the nodes gmsh meshed on its edges, with the tags, coordinates and parameters of the
        /// nodes of its logical edges loop by loop. False, with the reason, when the grid cannot be made, folds or has
        /// a quad corner below SubmapFaceDetector.MinScaledJacobian.
        /// </summary>
        private static bool Map(SubmapFace face, out SubmapGrid grid, out List<IntPtr>[][] loopTags,
                                out double[][][][] loopXyz, out double[][][][] loopUv, out string reason)
        {
            reason = null;
            grid = null;
            int surfaceId = face.FaceId;
            SubmapFace[] loops = face.Loops;
            int numOfLoops = loops.Length;
            int[][] loopDirections = face.LoopLogicalEdgeDirections;
            loopTags = new List<IntPtr>[numOfLoops][];
            loopXyz = new double[numOfLoops][][][];
            loopUv = new double[numOfLoops][][][];
            double[][][] loopNodeUv = new double[numOfLoops][][];
            int[][] loopCounts = new int[numOfLoops][];
            for (int l = 0; l < numOfLoops; l++)
            {
                List<List<IntPtr>> logicalTags;
                List<double[][]> logicalXyz;
                List<double[][]> logicalUv;
                if (!GetLogicalEdgeNodes(surfaceId, loops[l], out logicalTags, out logicalXyz, out logicalUv, out reason))
                    return false;
                loopTags[l] = logicalTags.ToArray();
                loopXyz[l] = logicalXyz.ToArray();
                loopUv[l] = logicalUv.ToArray();
                loopCounts[l] = new int[logicalUv.Count];
                List<double[]> nodeUv = new List<double[]>();
                for (int k = 0; k < logicalUv.Count; k++)
                {
                    loopCounts[l][k] = logicalUv[k].Length - 1;
                    for (int i = 0; i < loopCounts[l][k]; i++) nodeUv.Add(logicalUv[k][i]);
                }
                loopNodeUv[l] = nodeUv.ToArray();
            }
            // A round hole starts its logical loop at the node the lattice of the outer loop puts nearest to its +i +j
            // corner
            for (int l = 1; l < numOfLoops; l++)
            {
                if (!loops[l].IsRound) continue;
                List<IntPtr>[] roundTags;
                double[][][] roundXyz;
                double[][][] roundUv;
                if (!GetRoundLoopEdgeNodes(loops[l], loopDirections[0], loopCounts[0], loopNodeUv[0], loopTags[l],
                                           loopXyz[l], loopUv[l], out roundTags, out roundXyz, out roundUv,
                                           out loopNodeUv[l], out reason)) return false;
                loopTags[l] = roundTags;
                loopXyz[l] = roundXyz;
                loopUv[l] = roundUv;
                loopCounts[l] = new int[4];
                for (int k = 0; k < 4; k++) loopCounts[l][k] = loopUv[l][k].Length - 1;
            }
            // The holes are placed in the lattice by where they lie in the parameter space
            int[][] holeOffsets;
            if (!SubmapHolePlacement.TryPlace(loopDirections, loopCounts, loopNodeUv, 1, out holeOffsets, out reason))
            {
                reason = NameLoopEdges(face, reason);
                return false;
            }
            try
            {
                grid = SubmapMapping.Map(loopDirections, loopUv, loopXyz, holeOffsets, uv =>
                {
                    double[] coor;
                    Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
                    return coor;
                });
            }
            catch (CaeException ex)
            {
                reason = NameLoopEdges(face, ex.Message);
                return false;
            }
            if (grid.NumberOfFoldedQuads > 0)
            {
                reason = grid.NumberOfFoldedQuads + " of the " + grid.Quads.Length + " quads of its grid fold";
                return false;
            }
            if (grid.MinScaledJacobian < SubmapFaceDetector.MinScaledJacobian)
            {
                reason = "its grid has a quad corner of the scaled Jacobian " + Math.Round(grid.MinScaledJacobian, 3) +
                         ", below " + SubmapFaceDetector.MinScaledJacobian;
                return false;
            }
            return true;
        }
        /// <summary>
        /// The nodes gmsh meshed on the logical edges of one loop of a face, by their tags, coordinates and parameters
        /// on the surface. A turn edge is split at its middle node into its two halves. False, with the reason, when the
        /// nodes cannot be read or a turn edge carries an odd number of elements.
        /// </summary>
        private static bool GetLogicalEdgeNodes(int surfaceId, SubmapFace loop, out List<List<IntPtr>> logicalTags,
                                                out List<double[][]> logicalXyz, out List<double[][]> logicalUv,
                                                out string reason)
        {
            reason = null;
            logicalTags = new List<List<IntPtr>>();
            logicalXyz = new List<double[][]>();
            logicalUv = new List<double[][]>();
            int numOfEdges = loop.EdgeIds.Length;
            List<IntPtr>[] edgeTags = new List<IntPtr>[numOfEdges];
            double[][][] edgeXyz = new double[numOfEdges][][];
            double[][][] edgeUv = new double[numOfEdges][][];
            try
            {
                for (int k = 0; k < numOfEdges; k++)
                {
                    List<double[]> xyz;
                    GmshThreeBlockMesher.GetSideNodes(surfaceId, loop.VertexNodeIds[k],
                                                      loop.VertexNodeIds[(k + 1) % numOfEdges],
                                                      new int[] { loop.EdgeIds[k] }, out edgeTags[k], out xyz);
                    edgeXyz[k] = xyz.ToArray();
                    edgeUv[k] = GmshThreeBlockMesher.GetParametricCoordinates(surfaceId, xyz);
                }
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            // A turn edge is split at its middle node into its two halves, and an edge that turns twice at the nodes at a
            // quarter and three quarters of it into three pieces
            for (int k = 0; k < numOfEdges; k++)
            {
                int turn = Math.Abs(loop.GetEdgeTurn(k));
                if (turn == 0)
                {
                    logicalTags.Add(edgeTags[k]);
                    logicalXyz.Add(edgeXyz[k]);
                    logicalUv.Add(edgeUv[k]);
                    continue;
                }
                int count = edgeXyz[k].Length - 1;
                if (turn == 1 && count % 2 != 0)
                {
                    reason = "Edge " + loop.EdgeIds[k] + " turns a corner in its middle and carries an odd number of " +
                             "elements";
                    return false;
                }
                if (turn == 2 && count % 4 != 0)
                {
                    reason = "Edge " + loop.EdgeIds[k] + " turns twice and carries " + count + " elements, not a " +
                             "multiple of four";
                    return false;
                }
                int[] splits = turn == 1 ? new int[] { 0, count / 2, count } :
                                           new int[] { 0, count / 4, 3 * count / 4, count };
                for (int p = 0; p < splits.Length - 1; p++)
                {
                    int length = splits[p + 1] - splits[p] + 1;
                    logicalTags.Add(edgeTags[k].GetRange(splits[p], length));
                    logicalXyz.Add(GetRange(edgeXyz[k], splits[p], length));
                    logicalUv.Add(GetRange(edgeUv[k], splits[p], length));
                }
            }
            return true;
        }
        /// <summary>
        /// The four logical edges of a round hole from the nodes gmsh meshed on its edges, which must carry as many
        /// elements each and a multiple of four around it: its nodes in the order SubmapHolePlacement.GetRoundLoopOrder
        /// finds in the lattice of the outer loop, whose logical edges run in outerDirections with outerCounts elements
        /// through the nodes of the parameters outerNodeUv, and nodeUv the parameters of its nodes in that order. False,
        /// with the reason, when the counts do not allow it.
        /// </summary>
        private static bool GetRoundLoopEdgeNodes(SubmapFace loop, int[] outerDirections, int[] outerCounts,
                                                  double[][] outerNodeUv, List<IntPtr>[] edgeTags, double[][][] edgeXyz,
                                                  double[][][] edgeUv, out List<IntPtr>[] logicalTags,
                                                  out double[][][] logicalXyz, out double[][][] logicalUv,
                                                  out double[][] nodeUv, out string reason)
        {
            logicalTags = null;
            logicalXyz = null;
            logicalUv = null;
            nodeUv = null;
            reason = null;
            List<IntPtr> ringTags = new List<IntPtr>();
            List<double[]> ringXyz = new List<double[]>();
            List<double[]> ringUv = new List<double[]>();
            int firstCount = edgeUv[0].Length - 1;
            for (int k = 0; k < edgeUv.Length; k++)
            {
                int count = edgeUv[k].Length - 1;
                if (count != firstCount)
                {
                    reason = "Edge " + loop.EdgeIds[0] + " and Edge " + loop.EdgeIds[k] + " of a round hole carry " +
                             firstCount + " and " + count + " elements, which must be equal";
                    return false;
                }
                for (int i = 0; i < count; i++)
                {
                    ringTags.Add(edgeTags[k][i]);
                    ringXyz.Add(edgeXyz[k][i]);
                    ringUv.Add(edgeUv[k][i]);
                }
            }
            int numOfNodes = ringUv.Count;
            if (numOfNodes % 4 != 0 || numOfNodes == 0)
            {
                reason = "the round hole of Edge " + loop.EdgeIds[0] + " carries " + numOfNodes + " elements around it, " +
                         "not a multiple of four";
                return false;
            }
            int[] order = SubmapHolePlacement.GetRoundLoopOrder(outerDirections, outerCounts, outerNodeUv,
                                                                ringUv.ToArray());
            int edgeCount = numOfNodes / 4;
            nodeUv = new double[numOfNodes][];
            for (int p = 0; p < numOfNodes; p++) nodeUv[p] = ringUv[order[p]];
            logicalTags = new List<IntPtr>[4];
            logicalXyz = new double[4][][];
            logicalUv = new double[4][][];
            for (int c = 0; c < 4; c++)
            {
                logicalTags[c] = new List<IntPtr>();
                logicalXyz[c] = new double[edgeCount + 1][];
                logicalUv[c] = new double[edgeCount + 1][];
                for (int i = 0; i <= edgeCount; i++)
                {
                    int node = order[(c * edgeCount + i) % numOfNodes];
                    logicalTags[c].Add(ringTags[node]);
                    logicalXyz[c][i] = ringXyz[node];
                    logicalUv[c][i] = ringUv[node];
                }
            }
            return true;
        }
        /// <summary>
        /// A reason SubmapMapping gives, which names the edges of the face by their places in all its loops one after
        /// another, with the edges named by their gmsh ids, as a clause that follows a dash.
        /// </summary>
        internal static string NameLoopEdges(SubmapFace face, string reason)
        {
            reason = LoopEdge.Replace(reason, match =>
            {
                string text = match.Groups[1].Value.Length > 0 ? "Edges " : "Edge ";
                text += GetEdgeId(face, match.Groups[2].Value);
                if (match.Groups[3].Success) text += " and " + GetEdgeId(face, match.Groups[3].Value);
                return text;
            });
            return ToReason(reason);
        }
        private static string GetEdgeId(SubmapFace face, string loopIndex)
        {
            int index = int.Parse(loopIndex);
            int[] logicalEdgeIds = face.AllLogicalEdgeIds;
            return index >= 0 && index < logicalEdgeIds.Length ? logicalEdgeIds[index].ToString() : loopIndex;
        }
        private static double[][] GetRange(double[][] array, int start, int length)
        {
            double[][] range = new double[length][];
            Array.Copy(array, start, range, 0, length);
            return range;
        }
        private static string ToReason(string message)
        {
            message = message.Trim().TrimEnd('.');
            if (message.StartsWith("The ")) message = "the " + message.Substring(4);
            return message;
        }
    }
}
