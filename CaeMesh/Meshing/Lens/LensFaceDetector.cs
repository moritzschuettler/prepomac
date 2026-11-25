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
    /// face out, but for one on a surface that can have a pole (not a plane, a cylinder or a surface of extrusion)
    /// with MinNumOfCornersBesidePole to MaxNumOfCornersBesidePole corners beside it, at which GmshLensMesher splits
    /// the face when it is a pole of the surface (and otherwise leaves the face to gmsh). An edge of a length below
    /// 1E-9 times the square root of the face area, a degenerate edge, is left out first. Such a face is never three-
    /// or four-sided, a ring, a tube or a cap; the submapping may claim it, which then keeps it. Where the poles of the
    /// surface lie, and the quality of the grid, are judged by GmshLensMesher on the surface itself. The loop starts
    /// at a corner, of two at the first by SubmapFaceDetector.GetFirstVertexIndex, and with no corner at the first
    /// vertex by it, and runs the way SubmapFaceDetector.TurnLoop turns it, so that where the edges of the face
    /// start the loop, and which way they run it, do not change the grid.
    /// </summary>
    public static class LensFaceDetector
    {
        // Variables                                                                                                                
        private const double MaxDegenerateLength = 1E-9;
        // A face with a reentrant vertex is split there into pieces; the first and the last need a corner each
        private const int MinNumOfCornersBesidePole = 2;
        private const int MaxNumOfCornersBesidePole = 6;


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
            List<int> reentrants = new List<int>();
            for (int k = 0; k < n; k++)
            {
                if (TransfiniteFaceDetector.IsReentrant(anglesDeg[k], transfiniteAngleDeg)) reentrants.Add(k);
                else if (anglesDeg[k] <= maxCornerAngleDeg) corners.Add(k);
            }
            if (reentrants.Count == 0 && corners.Count > 2) return null;
            if (reentrants.Count > 0 && (reentrants.Count > 1 || !CanHavePole(vis, faceId) ||
                                         corners.Count < MinNumOfCornersBesidePole ||
                                         corners.Count > MaxNumOfCornersBesidePole)) return null;
            int reentrantNodeId = reentrants.Count > 0 ? loopVertexIds[reentrants[0]] : -1;
            // The loop from its corner, of two the first by SubmapFaceDetector.GetFirstVertexIndex, and with no corner
            // from its first vertex by it, turned by SubmapFaceDetector.TurnLoop, wherever the edges of the face start
            // it and whichever way they run it
            int start;
            if (corners.Count == 0) start = SubmapFaceDetector.GetFirstVertexIndex(mesh, loopVertexIds);
            else
            {
                int[] cornerVertexIds = corners.Select(corner => loopVertexIds[corner]).ToArray();
                start = corners[SubmapFaceDetector.GetFirstVertexIndex(mesh, cornerVertexIds)];
            }
            HashSet<int> cornerNodeIds = new HashSet<int>(corners.Select(corner => loopVertexIds[corner]));
            SubmapFaceDetector.TurnLoop(mesh, vis, start, ref loopEdgeIds, ref loopVertexIds);
            int[] cornerIndices = Enumerable.Range(0, n).Where(k => cornerNodeIds.Contains(loopVertexIds[k])).ToArray();
            int[] reentrantIndices = Enumerable.Range(0, n).Where(k => loopVertexIds[k] == reentrantNodeId).ToArray();
            return new LensFace(faceId, loopVertexIds, loopEdgeIds, cornerIndices, reentrantIndices);
        }
        // Whether the surface of the face can degenerate into a pole, by its type when the part knows it
        private static bool CanHavePole(VisualizationData vis, int faceId)
        {
            return vis.FaceTypes == null || faceId >= vis.FaceTypes.Length ||
                   CapFaceDetector.CanHavePole(vis.FaceTypes[faceId]);
        }
    }
}
