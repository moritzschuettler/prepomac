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
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Meshes a sweep wall that is one periodic face closed along a single seam - the wall of a bore - with the
    /// structured grid of its layers. gmsh holds such a face as two loops and refuses it as a transfinite surface, so
    /// it meshes the wall free and the elements are then replaced: the nodes of the source edge are carried level by
    /// level along the seam in the parameters of the surface onto the nodes of the target edge (GmshSeamGridMesher).
    /// The grid turns the way gmsh turned its own elements on the face.
    /// </summary>
    public static class SweepSeamWallMesher
    {
        // Methods
        public static bool IsSeamWall(SweepWall wall)
        {
            return wall.DirectionEdgeIds.Length == 1 && wall.SourceEdgeIds.Length == 1 && wall.TargetEdgeIds.Length == 1 &&
                   IsClosedEdge(wall.SourceEdgeIds[0]) && IsClosedEdge(wall.TargetEdgeIds[0]);
        }
        public static void Mesh(SweepWall wall, int seamNumOfElements)
        {
            int surfaceId = wall.SurfaceId;
            int seamId = wall.DirectionEdgeIds[0];
            int sourceVertexId = GetVertexIds(wall.SourceEdgeIds[0]).First();
            int targetVertexId = GetVertexIds(wall.TargetEdgeIds[0]).First();
            // The seam from the source edge to the target edge: the levels of the grid. Read as one level against the six
            // of the walls beside it, the seam of sweep_51 sent every sweep under the quasi-structured quad algorithm back
            // to the Frontal-Delaunay one
            List<IntPtr> seamTags;
            List<double[]> seamXyz;
            int numOfNewSeamNodes;
            GmshSeamGridMesher.GetSeamNodes(surfaceId, seamId, sourceVertexId, targetVertexId, seamNumOfElements,
                                            out seamTags, out seamXyz, out numOfNewSeamNodes);
            List<IntPtr> sourceTags;
            List<double[]> sourceXyz;
            GetRingNodes(wall.SourceEdgeIds[0], sourceVertexId, out sourceTags, out sourceXyz);
            List<IntPtr> targetTags;
            List<double[]> targetXyz;
            GetRingNodes(wall.TargetEdgeIds[0], targetVertexId, out targetTags, out targetXyz);
            if (sourceTags.Count != targetTags.Count)
                throw new CaeException("The source and the target edges of Surface " + surfaceId + " carry " +
                                       sourceTags.Count + " and " + targetTags.Count + " elements, which a structured " +
                                       "wall around its seam cannot join.");
            if (sourceTags.Count < 3)
                throw new CaeException("The source edge of Surface " + surfaceId + " carries fewer than three elements.");
            //
            List<IntPtr> newTags;
            List<double> newXyz;
            List<double> newUv;
            Dictionary<IntPtr, double[]> tagXyz;
            IntPtr[][] grid = GmshSeamGridMesher.LayGrid(surfaceId, seamTags, seamXyz, numOfNewSeamNodes, sourceTags, sourceXyz,
                                                         targetTags, targetXyz, out newTags, out newXyz, out newUv,
                                                         out tagXyz, out _, out _, out _, out _);
            GmshSeamGridMesher.ReplaceSurfaceMesh(surfaceId, GmshSeamGridMesher.GetQuadNodeTags(grid), newTags, newXyz, newUv,
                                                  tagXyz, true);
        }
        private static bool IsClosedEdge(int edgeId)
        {
            return GetVertexIds(edgeId).Length == 1;
        }
        private static int[] GetVertexIds(int edgeId)
        {
            int[] vertexIds;
            Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
            return vertexIds.Distinct().ToArray();
        }
        // The vertex node and then the nodes inside the edge
        private static void GetRingNodes(int edgeId, int vertexId, out List<IntPtr> tags, out List<double[]> xyz)
        {
            IntPtr[] vertexTags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out vertexTags, out coor, 0, vertexId, false, false);
            tags = new List<IntPtr>() { vertexTags[0] };
            xyz = new List<double[]>() { new double[] { coor[0], coor[1], coor[2] } };
            IntPtr[] edgeTags;
            double[] edgeCoor;
            SweepQuadWildInput.GetCurveNodes(edgeId, out edgeTags, out edgeCoor);
            for (int i = 0; i < edgeTags.Length; i++)
            {
                tags.Add(edgeTags[i]);
                xyz.Add(new double[] { edgeCoor[3 * i], edgeCoor[3 * i + 1], edgeCoor[3 * i + 2] });
            }
        }
    }
}
