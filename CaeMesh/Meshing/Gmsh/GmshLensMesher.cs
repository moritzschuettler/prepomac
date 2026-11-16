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

namespace CaeMesh
{
    /// <summary>
    /// Meshes a lens face (LensFace) on the nodes of its edges: gmsh supplies the nodes its edges carry, their
    /// parameters on the surface and the evaluation of the surface. A two-sided face is split by one inner line into
    /// two three-sided pieces of three blocks of quads each, a face with no corner first by a line between its vertices
    /// (GmshLensGrid). The face is meshed once gmsh has meshed the edges and before it meshes the surfaces, which it
    /// then leaves as it is (Mesh.MeshOnlyEmpty); elements already on it are replaced, as when the face is the fallback
    /// of a submapped face whose grid was refused. Counts the pieces cannot take, a grid that folds or one with a quad
    /// corner below SubmapFaceDetector.MinScaledJacobian, measured in space against the normal of the surface (at a
    /// pole against the normal of the quad), leave the face to gmsh. Also lays the three-sided faces with a pole of
    /// their surface at a corner, mapped in the chart of the pole.
    /// </summary>
    public static class GmshLensMesher
    {
        // Methods                                                                                                                  
        /// <summary>
        /// The face carries gmsh ids: its surface, its vertices and its edges. Without quads, as for a surface that is
        /// not recombined, every quad is split into two triangles. False, with the reason, when the face is left to
        /// gmsh.
        /// </summary>
        public static bool Mesh(LensFace face, bool quads, out string reason)
        {
            reason = null;
            GmshLensGrid grid = new GmshLensGrid(face.FaceId);
            int[] vertexNodeIds;
            int[][] sides;
            try
            {
                int[] starts = face.SideStartIndices;
                int n = face.EdgeIds.Length;
                int[][] sideEdgeIds = face.SideEdgeIds;
                int[][] sideVertexIds = new int[2][];
                for (int k = 0; k < 2; k++)
                {
                    int end = k == 0 ? starts[1] : n;
                    sideVertexIds[k] = Enumerable.Range(starts[k], end - starts[k] + 1)
                                                 .Select(i => face.VertexNodeIds[i % n]).ToArray();
                }
                AddLoop(grid, face.FaceId, sideVertexIds, sideEdgeIds, true, out vertexNodeIds, out sides);
                grid.SetBoundary(vertexNodeIds);
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            int a = sides[0].Length - 1;
            int b = sides[1].Length - 1;
            if ((a + b) % 2 != 0)
            {
                reason = "its edges carry " + (a + b) + " elements in all, which must be even";
                return false;
            }
            if (Math.Min(a, b) < LensFace.MinNumberOfElementsPerSide)
            {
                reason = "a side carries " + Math.Min(a, b) + " elements, fewer than " +
                         LensFace.MinNumberOfElementsPerSide;
                return false;
            }
            bool laid = face.NumberOfSides == 2 ? grid.LayTwoSided(sides[0], sides[1], out reason) :
                                                  grid.LayZeroSided(sides[0], sides[1], out reason);
            if (!laid) return false;
            if (!Judge(grid, true, out reason)) return false;
            grid.Write(quads);
            return true;
        }
        /// <summary>
        /// Whether a corner of the three-sided face, which carries gmsh ids, is a pole of its surface.
        /// </summary>
        public static bool HasPoleCorner(TransfiniteFace face)
        {
            double minX, minY, minZ, maxX, maxY, maxZ;
            Gmsh.Model.GetBoundingBox(2, face.FaceId, out minX, out minY, out minZ, out maxX, out maxY, out maxZ);
            double size = GmshPoleChart.GetDistance(new double[] { minX, minY, minZ },
                                                    new double[] { maxX, maxY, maxZ });
            foreach (var vertexId in face.CornerNodeIds)
            {
                double[] coor;
                Gmsh.Model.GetValue(0, vertexId, new double[0], out coor);
                if (coor == null || coor.Length < 3) continue;
                int p;
                double poleV;
                double farV;
                double[] point = new double[] { coor[0], coor[1], coor[2] };
                if (GmshPoleChart.FindPole(face.FaceId, point, size, out p, out poleV, out farV)) return true;
            }
            return false;
        }
        /// <summary>
        /// Meshes a three-sided face with a pole of its surface at a corner by three blocks of quads mapped in the
        /// chart of the pole, on the nodes of its edges. False, with the reason, when the grid folds and the face is
        /// left to gmsh.
        /// </summary>
        public static bool MeshThreeSided(TransfiniteFace face, bool quads, out string reason)
        {
            reason = null;
            GmshLensGrid grid = new GmshLensGrid(face.FaceId);
            int[] cornerNodeIds;
            int[][] sides;
            try
            {
                int[][] sideVertexIds = new int[3][];
                for (int k = 0; k < 3; k++)
                    sideVertexIds[k] = new int[] { face.CornerNodeIds[k], face.CornerNodeIds[(k + 1) % 3] };
                AddLoop(grid, face.FaceId, sideVertexIds, face.SideEdgeIds, false, out cornerNodeIds, out sides);
                grid.SetBoundary(cornerNodeIds);
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            int[] sideCounts = sides.Select(side => side.Length - 1).ToArray();
            if (!TransfiniteSideTriple.AreCountsValid(sideCounts))
            {
                reason = "its sides carry " + string.Join(", ", sideCounts) + " elements, which three blocks of " +
                         "quads cannot fill";
                return false;
            }
            if (!grid.MapPiece(sides, out reason)) return false;
            if (!Judge(grid, false, out reason)) return false;
            grid.Write(quads);
            return true;
        }
        /// <summary>
        /// Adds the nodes of the loop of a face to the grid, side by side; side k runs along its edges through its
        /// vertices sideVertexIds[k] - every vertex where an edge ends, or with byEdge false only its two ends - and
        /// ends where the next starts. Returns the node ids of the given vertices, side by side without the last, and
        /// of the nodes of every side.
        /// </summary>
        private static void AddLoop(GmshLensGrid grid, int surfaceId, int[][] sideVertexIds, int[][] sideEdgeIds,
                                    bool byEdge, out int[] vertexNodeIds, out int[][] sides)
        {
            int numOfSides = sideVertexIds.Length;
            List<int> vertices = new List<int>();
            List<int>[] sideNodeIds = new List<int>[numOfSides];
            for (int k = 0; k < numOfSides; k++) sideNodeIds[k] = new List<int>();
            int firstNodeId = -1;
            for (int k = 0; k < numOfSides; k++)
            {
                int numOfParts = byEdge ? sideEdgeIds[k].Length : 1;
                for (int e = 0; e < numOfParts; e++)
                {
                    int[] edgeIds = byEdge ? new int[] { sideEdgeIds[k][e] } : sideEdgeIds[k];
                    int startVertexId = sideVertexIds[k][e];
                    int endVertexId = sideVertexIds[k][byEdge ? e + 1 : 1];
                    List<IntPtr> tags;
                    List<double[]> xyz;
                    GmshThreeBlockMesher.GetSideNodes(surfaceId, startVertexId, endVertexId, edgeIds, out tags,
                                                      out xyz);
                    bool last = k == numOfSides - 1 && e == numOfParts - 1;
                    for (int i = 0; i < tags.Count; i++)
                    {
                        if (i == 0 && (k > 0 || e > 0)) continue;
                        int nodeId;
                        if (last && i == tags.Count - 1) nodeId = firstNodeId;
                        else nodeId = grid.AddBoundaryNode(tags[i], xyz[i]);
                        if (firstNodeId < 0) firstNodeId = nodeId;
                        if (i == 0) vertices.Add(nodeId);
                        sideNodeIds[k].Add(nodeId);
                    }
                    if (!last) vertices.Add(sideNodeIds[k][sideNodeIds[k].Count - 1]);
                }
                if (k < numOfSides - 1) sideNodeIds[k + 1].Add(sideNodeIds[k][sideNodeIds[k].Count - 1]);
            }
            vertexNodeIds = vertices.Distinct().ToArray();
            sides = sideNodeIds.Select(side => side.ToArray()).ToArray();
        }
        /// <summary>
        /// Whether the grid is kept: no quad folds in its chart or in space, and with the floor no quad corner lies
        /// below SubmapFaceDetector.MinScaledJacobian.
        /// </summary>
        private static bool Judge(GmshLensGrid grid, bool floor, out string reason)
        {
            reason = null;
            int numOfFoldedQuads;
            double minScaledJacobian = grid.Judge(out numOfFoldedQuads);
            numOfFoldedQuads = Math.Max(numOfFoldedQuads, grid.NumberOfFoldedQuads);
            if (numOfFoldedQuads > 0)
            {
                reason = numOfFoldedQuads + " of the " + grid.NumberOfQuads + " quads of its grid fold";
                return false;
            }
            if (floor && minScaledJacobian < SubmapFaceDetector.MinScaledJacobian)
            {
                reason = "its grid has a quad corner of the scaled Jacobian " + Math.Round(minScaledJacobian, 3) +
                         ", below " + SubmapFaceDetector.MinScaledJacobian;
                return false;
            }
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
