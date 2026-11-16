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
    /// Meshes a tube face by the grid of GmshSeamGridMesher, the way GmshRingMesher meshes a ring face: gmsh supplies the
    /// nodes its seam and its rims carry, their parameters on the surface and the evaluation of the surface, and the
    /// elements gmsh meshed on the face are replaced. The nodes of each rim are read edge by edge from its vertex on the
    /// seam and must run once round the periodic parameter of the surface, one way or the other. Rims of different
    /// numbers of elements, an edge that is not a seam of the surface, and a grid that folds or has a quad corner below
    /// SubmapFaceDetector.MinScaledJacobian, measured in space against the normal of the surface, leave the face as gmsh
    /// meshed it.
    /// </summary>
    public static class GmshTubeMesher
    {
        // Methods                                                                                                                  
        /// <summary>
        /// The face carries gmsh ids: its surface, its vertices and its edges. seamNumOfElements is the number of elements
        /// the seam was given, used only where gmsh moved the nodes of the seam onto the face. Without quads, as for a
        /// surface that is not recombined, every quad is split into two triangles. False, with the reason, when the face is
        /// left as gmsh meshed it.
        /// </summary>
        public static bool Mesh(TubeFace face, int seamNumOfElements, bool quads, out string reason)
        {
            reason = null;
            int surfaceId = face.FaceId;
            int seamDirection;
            double period;
            if (!GmshQuadMesher.GetSeamPeriod(surfaceId, face.SeamEdgeId, out seamDirection, out period))
            {
                reason = "Edge " + face.SeamEdgeId + " is not a seam of its surface";
                return false;
            }
            List<IntPtr> seamTags;
            List<double[]> seamXyz;
            int numOfNewSeamNodes;
            List<IntPtr> firstTags;
            List<double[]> firstXyz;
            List<IntPtr> secondTags;
            List<double[]> secondXyz;
            try
            {
                GmshSeamGridMesher.GetSeamNodes(surfaceId, face.SeamEdgeId, face.FirstRimVertexNodeIds[0],
                                                face.SecondRimVertexNodeIds[0], seamNumOfElements, out seamTags, out seamXyz,
                                                out numOfNewSeamNodes);
                GmshRingMesher.GetLoopNodes(surfaceId, face.FirstRimVertexNodeIds, face.FirstRimEdgeIds, out firstTags,
                                            out firstXyz);
                GmshRingMesher.GetLoopNodes(surfaceId, face.SecondRimVertexNodeIds, face.SecondRimEdgeIds, out secondTags,
                                            out secondXyz);
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            if (firstTags.Count != secondTags.Count)
            {
                reason = "its rims carry " + firstTags.Count + " and " + secondTags.Count + " elements, which must be equal";
                return false;
            }
            if (firstTags.Count < 3)
            {
                reason = "its rims carry " + firstTags.Count + " elements, fewer than three";
                return false;
            }
            //
            IntPtr[][] grid;
            List<IntPtr> newTags;
            List<double> newXyz;
            List<double> newUv;
            Dictionary<IntPtr, double[]> tagXyz;
            Dictionary<IntPtr, double[]> tagUv;
            int[] firstOrder;
            int[] secondOrder;
            int periodicDirection;
            try
            {
                grid = GmshSeamGridMesher.LayGrid(surfaceId, seamTags, seamXyz, numOfNewSeamNodes, firstTags, firstXyz,
                                                  secondTags, secondXyz, out newTags, out newXyz, out newUv, out tagXyz,
                                                  out tagUv, out firstOrder, out secondOrder, out periodicDirection);
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            if (periodicDirection != seamDirection)
            {
                reason = "the nodes of its rims do not run round the parameter its seam closes";
                return false;
            }
            if (!GmshSeamGridMesher.RunsOnceRound(firstOrder) || !GmshSeamGridMesher.RunsOnceRound(secondOrder))
            {
                int edgeId = GmshSeamGridMesher.RunsOnceRound(firstOrder) ? face.SecondRimEdgeIds[0] : face.FirstRimEdgeIds[0];
                reason = "the nodes of the rim of Edge " + edgeId + " do not run once round its seam";
                return false;
            }
            // The grid judged in space
            IntPtr[] quadNodeTags = GmshSeamGridMesher.GetQuadNodeTags(grid);
            int numOfQuads = quadNodeTags.Length / 4;
            int numOfFoldedQuads;
            double minScaledJacobian = GmshSeamGridMesher.GetMinScaledJacobian(surfaceId, quadNodeTags, tagXyz, tagUv, null,
                                                                               out numOfFoldedQuads);
            if (numOfFoldedQuads > 0)
            {
                reason = numOfFoldedQuads + " of the " + numOfQuads + " quads of its grid fold";
                return false;
            }
            if (minScaledJacobian < SubmapFaceDetector.MinScaledJacobian)
            {
                reason = "its grid has a quad corner of the scaled Jacobian " + Math.Round(minScaledJacobian, 3) + ", below " +
                         SubmapFaceDetector.MinScaledJacobian;
                return false;
            }
            GmshSeamGridMesher.ReplaceSurfaceMesh(surfaceId, quadNodeTags, newTags, newXyz, newUv, tagXyz, quads);
            return true;
        }
        private static string ToReason(string message)
        {
            message = message.Trim().TrimEnd('.');
            if (message.StartsWith("The ")) message = "the " + message.Substring(4);
            return message;
        }
    }
}
