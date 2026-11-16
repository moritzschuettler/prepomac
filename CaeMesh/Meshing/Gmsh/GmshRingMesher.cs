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
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// Meshes a ring face by RingMapping, the way GmshSubmapMesher meshes a submapped face: gmsh supplies the nodes its
    /// edges carry, their parameters on the surface and the evaluation of the surface, and the elements gmsh meshed on the
    /// face are replaced. The nodes of each loop are read edge by edge from its vertices; a loop of one closed edge runs
    /// along the parameter of the edge, so the inner loop is turned when it runs the other way round than the outer loop
    /// in the parameter space. The number of layers is RingMapping.GetNumberOfLayers of the nodes read. Loops of different
    /// numbers of elements, a grid that cannot be made, folds or has a quad corner below
    /// SubmapFaceDetector.MinScaledJacobian leave the face as gmsh meshed it.
    /// </summary>
    public static class GmshRingMesher
    {
        // Methods                                                                                                                  
        /// <summary>
        /// The face carries gmsh ids: its surface, its vertices and its edges. Without quads, as for a surface that is not
        /// recombined, every quad is split into two triangles. False, with the reason, when the face is left as gmsh
        /// meshed it.
        /// </summary>
        public static bool Mesh(RingFace face, bool quads, out string reason)
        {
            reason = null;
            int surfaceId = face.FaceId;
            List<IntPtr> outerTags;
            List<double[]> outerXyz;
            List<IntPtr> innerTags;
            List<double[]> innerXyz;
            double[][] outerUv;
            double[][] innerUv;
            try
            {
                GetLoopNodes(surfaceId, face.OuterVertexNodeIds, face.OuterEdgeIds, out outerTags, out outerXyz);
                GetLoopNodes(surfaceId, face.InnerVertexNodeIds, face.InnerEdgeIds, out innerTags, out innerXyz);
                outerUv = GmshThreeBlockMesher.GetParametricCoordinates(surfaceId, outerXyz);
                innerUv = GmshThreeBlockMesher.GetParametricCoordinates(surfaceId, innerXyz);
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            if (outerTags.Count != innerTags.Count)
            {
                reason = "its outer loop carries " + outerTags.Count + " elements and its inner loop " + innerTags.Count +
                         ", which must be equal";
                return false;
            }
            // The inner loop the same way round as the outer loop in the parameter space, still from its first node
            if (Math.Sign(GetSignedArea(outerUv)) != Math.Sign(GetSignedArea(innerUv)))
            {
                Reverse(innerTags);
                Reverse(innerXyz);
                List<double[]> reversedUv = new List<double[]>(innerUv);
                Reverse(reversedUv);
                innerUv = reversedUv.ToArray();
            }
            //
            RingGrid grid;
            try
            {
                double[][] outerXyzArray = outerXyz.ToArray();
                double[][] innerXyzArray = innerXyz.ToArray();
                int numOfLayers = RingMapping.GetNumberOfLayers(outerUv, outerXyzArray, innerUv, innerXyzArray);
                grid = RingMapping.Map(outerUv, outerXyzArray, innerUv, innerXyzArray, numOfLayers, uv =>
                {
                    double[] coor;
                    Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
                    return coor;
                });
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
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
            // The tags of the boundary nodes, the outer loop and then the inner one, and the parameters of the outer loop,
            // which tell the way the grid turns
            IntPtr[] boundaryNodeTags = new IntPtr[grid.NumberOfBoundaryNodes];
            for (int i = 0; i < outerTags.Count; i++)
            {
                boundaryNodeTags[i] = outerTags[i];
                boundaryNodeTags[outerTags.Count + i] = innerTags[i];
            }
            GmshThreeBlockMesher.ReplaceSurfaceMesh(surfaceId, boundaryNodeTags, outerUv, grid.Quads, grid.InnerUv,
                                                    grid.InnerXyz, quads);
            return true;
        }
        /// <summary>
        /// The nodes gmsh meshed around one loop, edge by edge from its vertices, the last node of an edge being the first
        /// of the next and not repeated.
        /// </summary>
        internal static void GetLoopNodes(int surfaceId, int[] vertexIds, int[] edgeIds, out List<IntPtr> tags,
                                          out List<double[]> xyz)
        {
            tags = new List<IntPtr>();
            xyz = new List<double[]>();
            int n = edgeIds.Length;
            for (int k = 0; k < n; k++)
            {
                List<IntPtr> edgeTags;
                List<double[]> edgeXyz;
                GmshThreeBlockMesher.GetSideNodes(surfaceId, vertexIds[k], vertexIds[(k + 1) % n], new int[] { edgeIds[k] },
                                                  out edgeTags, out edgeXyz);
                for (int i = 0; i < edgeTags.Count - 1; i++)
                {
                    tags.Add(edgeTags[i]);
                    xyz.Add(edgeXyz[i]);
                }
            }
        }
        /// <summary>
        /// Reverses a loop in place, its first node staying first.
        /// </summary>
        private static void Reverse<T>(List<T> loop)
        {
            if (loop.Count < 3) return;
            loop.Reverse(1, loop.Count - 1);
        }
        private static double GetSignedArea(double[][] polygon)
        {
            double area = 0;
            for (int i = 0; i < polygon.Length; i++)
            {
                double[] p = polygon[i];
                double[] q = polygon[(i + 1) % polygon.Length];
                area += p[0] * q[1] - q[0] * p[1];
            }
            return 0.5 * area;
        }
        private static string ToReason(string message)
        {
            message = message.Trim().TrimEnd('.');
            if (message.StartsWith("The ")) message = "the " + message.Substring(4);
            return message;
        }
    }
}
