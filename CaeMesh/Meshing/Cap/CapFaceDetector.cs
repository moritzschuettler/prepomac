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
    /// Finds the faces of a part that are meshed as structured caps: faces closed along exactly one seam that ends in a
    /// pole at one end and on one closed rim at the other, or in a pole at both ends with no other edge. PrePoMax's
    /// topology does not list the degenerate edge of a pole (an edge of a length below 1E-9 times the square root of the
    /// face area is left out where it does), so a pole is the vertex of the seam that no other edge of the face reaches;
    /// the other edges must form exactly one closed loop through the other vertex of the seam, the rim, which starts at
    /// that vertex. A face with a hole besides its rim has a second loop, a torus or a face with two rims has no pole,
    /// and a face with a degenerate edge but no seam - a half dome, a sphere octant - has no edge with a free end, so
    /// none of them matches. That the seam closes the surface and ends in its poles, and the quality of the grid, are
    /// judged by GmshCapMesher on the surface itself.
    /// </summary>
    public static class CapFaceDetector
    {
        // Variables                                                                                                                
        private const double MaxDegenerateLength = 1E-9;


        // Methods                                                                                                                  
        public static CapFace[] Detect(BasePart part)
        {
            VisualizationData vis = part.Visualization;
            if (vis == null || vis.FaceCount == 0 || vis.FaceEdgeIds == null || vis.CellIdsByFace == null ||
                vis.EdgeLengths == null || vis.FaceAreas == null) return new CapFace[0];
            //
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            List<CapFace> capFaces = new List<CapFace>();
            for (int i = 0; i < vis.FaceCount; i++)
            {
                CapFace capFace = DetectFace(vis, i, edgeIdVertexIds);
                if (capFace != null) capFaces.Add(capFace);
            }
            return capFaces.ToArray();
        }
        private static CapFace DetectFace(VisualizationData vis, int faceId, Dictionary<int, int[]> edgeIdVertexIds)
        {
            if (faceId >= vis.CellIdsByFace.Length || vis.FaceEdgeIds[faceId] == null) return null;
            double faceSize = Math.Sqrt(Math.Max(0, vis.FaceAreas[faceId]));
            if (!(faceSize > 0)) return null;
            int[] edgeIds = vis.FaceEdgeIds[faceId].Distinct()
                                                   .Where(edgeId => vis.EdgeLengths[edgeId] > MaxDegenerateLength * faceSize)
                                                   .ToArray();
            if (edgeIds.Length == 0) return null;
            // The seam ends at a vertex no other edge reaches
            int seamEdgeId = -1;
            int[] seamVertexIds = null;
            foreach (var edgeId in edgeIds)
            {
                int[] vertexIds = vis.GetVertexNodeIdsForEdgeId(edgeId);
                if (vertexIds == null || vertexIds.Length != 2 || vertexIds[0] == vertexIds[1]) continue;
                bool[] reached = new bool[2];
                foreach (var otherEdgeId in edgeIds)
                {
                    if (otherEdgeId == edgeId) continue;
                    int[] otherVertexIds = vis.GetVertexNodeIdsForEdgeId(otherEdgeId);
                    for (int k = 0; k < 2; k++) reached[k] |= otherVertexIds.Contains(vertexIds[k]);
                }
                if (reached[0] && reached[1]) continue;
                if (seamEdgeId >= 0) return null;
                seamEdgeId = edgeId;
                seamVertexIds = reached[0] ? new int[] { vertexIds[0], vertexIds[1] } :
                                reached[1] ? new int[] { vertexIds[1], vertexIds[0] } : vertexIds;
            }
            if (seamEdgeId < 0) return null;
            int[] rimEdgeIds = edgeIds.Where(edgeId => edgeId != seamEdgeId).ToArray();
            // A pole at both ends: no other edge
            if (rimEdgeIds.Length == 0)
                return new CapFace(faceId, seamEdgeId, new int[0], new int[0], new int[] { seamVertexIds[0], seamVertexIds[1] });
            // A pole at one end and one closed rim through the other, the rim from its vertex on the seam
            List<int[]> loopsEdgeIds;
            List<int[]> loopsVertexIds;
            if (!SubmapFaceDetector.GetBoundaryLoops(vis, rimEdgeIds, edgeIdVertexIds, out loopsEdgeIds, out loopsVertexIds) ||
                loopsEdgeIds.Count != 1) return null;
            int start = Array.IndexOf(loopsVertexIds[0], seamVertexIds[0]);
            if (start < 0 || loopsVertexIds[0].Contains(seamVertexIds[1])) return null;
            int n = loopsEdgeIds[0].Length;
            int[] rimEdges = new int[n];
            int[] rimVertices = new int[n];
            for (int k = 0; k < n; k++)
            {
                rimEdges[k] = loopsEdgeIds[0][(start + k) % n];
                rimVertices[k] = loopsVertexIds[0][(start + k) % n];
            }
            return new CapFace(faceId, seamEdgeId, rimVertices, rimEdges, new int[] { seamVertexIds[1] });
        }
    }
}
