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

namespace CaeMesh
{
    /// <summary>
    /// Finds the faces of a part that are meshed as lens faces: one boundary loop whose vertices, by the angles of
    /// TransfiniteFaceDetector.GetLoopAnglesDeg, are two corners (the transfinite angle or sharper), one corner or
    /// none, and flat joints otherwise; a loop of one closed edge, a disc, has one vertex. A reentrant vertex leaves the
    /// face out. An edge of a length below 1E-9 times the square root of the face area, a degenerate edge, is left out
    /// first. Such a face is never three- or four-sided, a ring, a tube or a cap; the submapping may claim it, which
    /// then keeps it. Where the poles of the surface lie, and the quality of the grid, are judged by GmshLensMesher on
    /// the surface itself.
    /// </summary>
    public static class LensFaceDetector
    {
        // Variables                                                                                                                
        private const double MaxDegenerateLength = 1E-9;


        // Methods                                                                                                                  
        public static LensFace[] Detect(FeMesh mesh, BasePart part, double transfiniteAngleDeg)
        {
            VisualizationData vis = part.Visualization;
            if (vis == null || vis.FaceCount == 0 || vis.FaceEdgeIds == null || vis.CellIdsByFace == null ||
                vis.EdgeLengths == null || vis.FaceAreas == null) return new LensFace[0];
            //
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            List<LensFace> lensFaces = new List<LensFace>();
            for (int i = 0; i < vis.FaceCount; i++)
            {
                LensFace lensFace = DetectFace(mesh, vis, i, transfiniteAngleDeg, edgeIdVertexIds);
                if (lensFace != null) lensFaces.Add(lensFace);
            }
            return lensFaces.ToArray();
        }
        private static LensFace DetectFace(FeMesh mesh, VisualizationData vis, int faceId, double transfiniteAngleDeg,
                                           Dictionary<int, int[]> edgeIdVertexIds)
        {
            if (faceId >= vis.CellIdsByFace.Length || vis.FaceEdgeIds[faceId] == null) return null;
            double faceSize = Math.Sqrt(Math.Max(0, vis.FaceAreas[faceId]));
            if (!(faceSize > 0)) return null;
            int[] edgeIds = vis.FaceEdgeIds[faceId].Distinct()
                                                   .Where(edgeId => vis.EdgeLengths[edgeId] >
                                                                    MaxDegenerateLength * faceSize)
                                                   .ToArray();
            if (edgeIds.Length < 1) return null;
            //
            int[] loopEdgeIds;
            int[] loopVertexIds;
            if (!TransfiniteFaceDetector.GetBoundaryLoop(vis, edgeIds, edgeIdVertexIds, out loopEdgeIds,
                                                         out loopVertexIds, true)) return null;
            int n = loopEdgeIds.Length;
            double[] anglesDeg = TransfiniteFaceDetector.GetLoopAnglesDeg(mesh, vis, faceId, loopEdgeIds,
                                                                          loopVertexIds);
            if (anglesDeg == null) return null;
            double maxCornerAngleDeg = transfiniteAngleDeg + TransfiniteFaceDetector._cornerAngleToleranceDeg;
            List<int> corners = new List<int>();
            for (int k = 0; k < n; k++)
            {
                if (TransfiniteFaceDetector.IsReentrant(anglesDeg[k], transfiniteAngleDeg)) return null;
                if (anglesDeg[k] <= maxCornerAngleDeg) corners.Add(k);
            }
            if (corners.Count == 0)
                return new LensFace(faceId, loopVertexIds.ToArray(), loopEdgeIds.ToArray(), new int[0]);
            if (corners.Count > 2) return null;
            // The loop from the first corner
            int start = corners[0];
            int[] vertexIds = new int[n];
            int[] loopEdges = new int[n];
            for (int k = 0; k < n; k++)
            {
                vertexIds[k] = loopVertexIds[(start + k) % n];
                loopEdges[k] = loopEdgeIds[(start + k) % n];
            }
            int[] cornerIndices = corners.Count == 1 ? new int[] { 0 } : new int[] { 0, corners[1] - start };
            return new LensFace(faceId, vertexIds, loopEdges, cornerIndices);
        }
    }
}
