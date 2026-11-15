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
    /// face, so it needs an even number of elements.
    /// </summary>
    public static class GmshSubmapMesher
    {
        // Variables
        private static readonly Regex LoopEdge = new Regex(@"(?<![A-Za-z])[Ee]dge(s?) (\d+)(?: and (\d+))?",
                                                           RegexOptions.Compiled);


        // Methods                                                                                                                  
        /// <summary>
        /// The face carries gmsh ids: its surface, its vertices and its edges. Without quads, as for a surface that is
        /// not recombined, every quad is split into two triangles. False, with the reason, when the face is left as gmsh
        /// meshed it.
        /// </summary>
        public static bool Mesh(SubmapFace face, bool quads, out string reason)
        {
            reason = null;
            int surfaceId = face.FaceId;
            int numOfEdges = face.EdgeIds.Length;
            List<IntPtr>[] edgeTags = new List<IntPtr>[numOfEdges];
            double[][][] edgeXyz = new double[numOfEdges][][];
            double[][][] edgeUv = new double[numOfEdges][][];
            try
            {
                for (int k = 0; k < numOfEdges; k++)
                {
                    List<double[]> xyz;
                    GmshThreeBlockMesher.GetSideNodes(surfaceId, face.VertexNodeIds[k],
                                                      face.VertexNodeIds[(k + 1) % numOfEdges],
                                                      new int[] { face.EdgeIds[k] }, out edgeTags[k], out xyz);
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
            List<List<IntPtr>> logicalTags = new List<List<IntPtr>>();
            List<double[][]> logicalXyz = new List<double[][]>();
            List<double[][]> logicalUv = new List<double[][]>();
            for (int k = 0; k < numOfEdges; k++)
            {
                if (face.GetEdgeTurn(k) == 0)
                {
                    logicalTags.Add(edgeTags[k]);
                    logicalXyz.Add(edgeXyz[k]);
                    logicalUv.Add(edgeUv[k]);
                    continue;
                }
                int count = edgeXyz[k].Length - 1;
                if (count % 2 != 0)
                {
                    reason = "Edge " + face.EdgeIds[k] + " turns a corner in its middle and carries an odd number of " +
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
            SubmapGrid grid;
            try
            {
                grid = SubmapMapping.Map(face.LogicalEdgeDirections, logicalUv.ToArray(), logicalXyz.ToArray(), uv =>
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
            // The tags and parameters of the boundary nodes in loop order
            IntPtr[] boundaryNodeTags = new IntPtr[grid.NumberOfBoundaryNodes];
            double[][] loopUv = new double[grid.NumberOfBoundaryNodes][];
            for (int k = 0; k < logicalTags.Count; k++)
            {
                for (int i = 0; i < grid.EdgeCounts[k]; i++)
                {
                    int nodeId = grid.GetBoundaryNodeId(k, i);
                    boundaryNodeTags[nodeId] = logicalTags[k][i];
                    loopUv[nodeId] = logicalUv[k][i];
                }
            }
            GmshThreeBlockMesher.ReplaceSurfaceMesh(surfaceId, boundaryNodeTags, loopUv, grid.Quads, grid.InnerUv,
                                                    grid.InnerXyz, quads);
            return true;
        }
        /// <summary>
        /// A reason SubmapMapping gives, which names the edges of the face by their places in its loop, with the edges
        /// named by their gmsh ids, as a clause that follows a dash.
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
            int[] logicalEdgeIds = face.LogicalEdgeIds;
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
