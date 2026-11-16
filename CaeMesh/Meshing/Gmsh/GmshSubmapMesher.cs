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
    /// face, so it needs an even number of elements. The nodes of the holes of a face are read loop by loop as those
    /// of its outer loop, and the holes are placed in the lattice by SubmapHolePlacement on the parameters of the nodes;
    /// a hole for which no place is found leaves the face as gmsh meshed it.
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
            reason = null;
            int surfaceId = face.FaceId;
            SubmapFace[] loops = face.Loops;
            int numOfLoops = loops.Length;
            int[][] loopDirections = face.LoopLogicalEdgeDirections;
            List<IntPtr>[][] loopTags = new List<IntPtr>[numOfLoops][];
            double[][][][] loopXyz = new double[numOfLoops][][][];
            double[][][][] loopUv = new double[numOfLoops][][][];
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
            // The holes are placed in the lattice by where they lie in the parameter space
            int[][] holeOffsets;
            if (!SubmapHolePlacement.TryPlace(loopDirections, loopCounts, loopNodeUv, 1, out holeOffsets, out reason))
            {
                reason = NameLoopEdges(face, reason);
                return false;
            }
            SubmapGrid grid;
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
            // The tags of the boundary nodes of all loops in loop order, and the parameters of the outer loop, which
            // tell the way the grid turns
            IntPtr[] boundaryNodeTags = new IntPtr[grid.NumberOfBoundaryNodes];
            double[][] outerUv = new double[grid.NumberOfOuterBoundaryNodes][];
            int edge = 0;
            for (int l = 0; l < numOfLoops; l++)
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
            GmshThreeBlockMesher.ReplaceSurfaceMesh(surfaceId, boundaryNodeTags, outerUv, grid.Quads, grid.InnerUv,
                                                    grid.InnerXyz, quads);
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
            // A turn edge is split at its middle node into its two halves
            for (int k = 0; k < numOfEdges; k++)
            {
                if (loop.GetEdgeTurn(k) == 0)
                {
                    logicalTags.Add(edgeTags[k]);
                    logicalXyz.Add(edgeXyz[k]);
                    logicalUv.Add(edgeUv[k]);
                    continue;
                }
                int count = edgeXyz[k].Length - 1;
                if (count % 2 != 0)
                {
                    reason = "Edge " + loop.EdgeIds[k] + " turns a corner in its middle and carries an odd number of " +
                             "elements";
                    return false;
                }
                int middle = count / 2;
                logicalTags.Add(edgeTags[k].GetRange(0, middle + 1));
                logicalTags.Add(edgeTags[k].GetRange(middle, count - middle + 1));
                logicalXyz.Add(GetRange(edgeXyz[k], 0, middle + 1));
                logicalXyz.Add(GetRange(edgeXyz[k], middle, count - middle + 1));
                logicalUv.Add(GetRange(edgeUv[k], 0, middle + 1));
                logicalUv.Add(GetRange(edgeUv[k], middle, count - middle + 1));
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
