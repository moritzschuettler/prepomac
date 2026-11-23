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
    /// two three-sided pieces of three blocks of quads each, a face with no corner first by a line between two nodes
    /// of its loop into two two-sided faces, a face with one corner by a line across the corner into a three-sided
    /// piece and a two-sided rest (GmshLensGrid); the nodes inside the face are then smoothed together where that
    /// leaves the grid no worse (GmshLensGrid.Smooth). The face is meshed once gmsh has meshed the edges and before
    /// it meshes the surfaces, which it then leaves as it is (Mesh.MeshOnlyEmpty); elements already on it are
    /// replaced, as when the face is the fallback of a submapped face whose grid was refused. Counts the pieces
    /// cannot take, a grid that folds or one with a quad corner below SubmapFaceDetector.MinScaledJacobian, measured
    /// in space against the normal of the surface (at a pole against the normal of the quad), leave the face to
    /// gmsh. A face with a reentrant pole is split at the pole into pieces of about a right angle
    /// (GmshLensGrid.LayPoleFan). Also lays the three- and four-sided faces with a pole of their surface at a corner,
    /// mapped in the chart of the pole and in the parameters of the surface, the better kept.
    /// </summary>
    public static class GmshLensMesher
    {
        // Variables                                                                                                                
        private const int MinNumberOfLineElements = 2;
        // A lens face is thin when its line must carry more elements than this times the number its length asks for
        private const double ThinLineFactor = 1.25;
        // The pieces round a reentrant pole take their counts from the lines between them, which can stretch them
        private const double MaxFanEdgeSizeFactor = 2;


        // Methods                                                                                                                  
        /// <summary>
        /// The face carries gmsh ids: its surface, its vertices and its edges. Without quads, as for a surface that is
        /// not recombined, every quad is split into two triangles. False, with the reason, when the face is left to
        /// gmsh. Across the mirror planes of the part the face is its own image across, its grid is laid mirrored
        /// where its nodes allow it.
        /// </summary>
        public static bool Mesh(LensFace face, bool quads, out string reason, List<GmshMirrorPlane> mirrorPlanes = null)
        {
            GmshLensGrid grid;
            double minScaledJacobian;
            if (!Lay(face, mirrorPlanes, out grid, out minScaledJacobian, out reason)) return false;
            grid.Write(quads);
            return true;
        }
        /// <summary>
        /// Whether the grid of the face, which carries gmsh ids and whose edges gmsh has meshed, would be kept, and its
        /// worst scaled Jacobian of a quad corner in space and the length of its longest element edge. Nothing is
        /// written into the surface.
        /// </summary>
        public static bool Evaluate(LensFace face, out double minScaledJacobian, out double longestEdgeLength,
                                    out string reason)
        {
            GmshLensGrid grid;
            longestEdgeLength = double.NaN;
            if (!Lay(face, null, out grid, out minScaledJacobian, out reason)) return false;
            longestEdgeLength = grid.GetLongestEdgeLength();
            return true;
        }
        /// <summary>
        /// Lays and judges the grid of the face. False, with the reason, when the face is left to gmsh.
        /// </summary>
        private static bool Lay(LensFace face, List<GmshMirrorPlane> mirrorPlanes, out GmshLensGrid grid,
                                out double minScaledJacobian, out string reason)
        {
            reason = null;
            minScaledJacobian = double.NaN;
            grid = new GmshLensGrid(face.FaceId);
            grid.SetMirrorPlanes(mirrorPlanes);
            int n = face.EdgeIds.Length;
            bool fan = face.ReentrantIndices.Length > 0;
            int[] vertexNodeIds;
            int[][] sides;
            try
            {
                int[][] sideEdgeIds;
                int[][] sideVertexIds;
                if (face.NumberOfSides == 2 && !fan)
                {
                    int[] starts = face.SideStartIndices;
                    sideEdgeIds = face.SideEdgeIds;
                    sideVertexIds = new int[2][];
                    for (int k = 0; k < 2; k++)
                    {
                        int end = k == 0 ? starts[1] : n;
                        sideVertexIds[k] = Enumerable.Range(starts[k], end - starts[k] + 1)
                                                     .Select(i => face.VertexNodeIds[i % n]).ToArray();
                    }
                }
                else
                {
                    // The whole loop as one side from its first vertex, the corner of a face with one, back to it
                    sideEdgeIds = new int[][] { face.EdgeIds };
                    sideVertexIds = new int[][] { Enumerable.Range(0, n + 1).Select(i => face.VertexNodeIds[i % n])
                                                            .ToArray() };
                }
                AddLoop(grid, face.FaceId, sideVertexIds, sideEdgeIds, true, out vertexNodeIds, out sides);
                grid.SetBoundary(vertexNodeIds);
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            int total = sides.Sum(side => side.Length - 1);
            if (total % 2 != 0)
            {
                reason = "its edges carry " + total + " elements in all, which must be even";
                return false;
            }
            bool laid;
            if (fan)
            {
                HashSet<int> cornerNodeIds = new HashSet<int>(face.CornerIndices.Select(k => vertexNodeIds[k]));
                laid = grid.LayPoleFan(sides[0], vertexNodeIds[face.ReentrantIndices[0]], cornerNodeIds, out reason);
            }
            else if (face.NumberOfSides == 2)
            {
                int a = sides[0].Length - 1;
                int b = sides[1].Length - 1;
                if (Math.Min(a, b) < LensFace.MinNumberOfElementsPerSide)
                {
                    reason = "a side carries " + Math.Min(a, b) + " elements, fewer than " +
                             LensFace.MinNumberOfElementsPerSide;
                    return false;
                }
                laid = grid.LayTwoSided(sides[0], sides[1], out reason);
            }
            else if (total < LensFace.MinNumberOfElementsInLoop)
            {
                reason = "its edges carry " + total + " elements in all, fewer than " +
                         LensFace.MinNumberOfElementsInLoop;
                return false;
            }
            else if (face.NumberOfSides == 1) laid = grid.LayOneCorner(sides[0], out reason);
            else laid = grid.LayNoCorner(sides[0], vertexNodeIds, out reason);
            if (!laid) return false;
            grid.Smooth();
            if (!Judge(grid, true, out minScaledJacobian, out reason)) return false;
            if (!fan) return true;
            double maxSize = Gmsh.Option.GetNumber("Mesh.MeshSizeMax");
            double longestEdgeLength = grid.GetLongestEdgeLength();
            if (maxSize > 0 && longestEdgeLength > MaxFanEdgeSizeFactor * maxSize)
            {
                reason = "the pieces round its pole would carry an element edge of the length " +
                         Math.Round(longestEdgeLength, 3) + ", more than " + MaxFanEdgeSizeFactor +
                         " times the element size";
                return false;
            }
            return true;
        }
        /// <summary>
        /// Whether the two-sided face, which carries gmsh ids and whose edges gmsh has meshed, is thin: the fewest
        /// elements its line can carry with the counts a and b the mesh size asks for on its sides is more than
        /// ThinLineFactor times the number its length asks for, two at least. Split at their middles, the sides leave
        /// pieces whose sides differ by half of |a - b|, made even as the total must be, and a line of the parity of
        /// that difference must carry two more (GmshLensGrid.ChooseSplit). The number the length asks for is the
        /// distance between the nodes nearest the middle of each side by length over the mean element length, the
        /// length of both sides over a + b. A thin face is meshed far finer across than the size asks unless its sides
        /// carry equal counts, which leave a line of two elements possible.
        /// </summary>
        public static bool IsThin(LensFace face, Dictionary<int, int> edgeIdCount)
        {
            if (face.NumberOfSides != 2 || face.ReentrantIndices.Length > 0) return false;
            int[] starts = face.SideStartIndices;
            int[][] sideEdgeIds = face.SideEdgeIds;
            int[] counts = new int[2];
            double[] lengths = new double[2];
            double[][] middles = new double[2][];
            for (int k = 0; k < 2; k++)
            {
                List<IntPtr> tags;
                List<double[]> xyz;
                try
                {
                    GmshThreeBlockMesher.GetSideNodes(face.FaceId, face.VertexNodeIds[starts[k]],
                                                      face.VertexNodeIds[k == 0 ? starts[1] : 0], sideEdgeIds[k],
                                                      out tags, out xyz);
                }
                catch (CaeException)
                {
                    return false;
                }
                counts[k] = sideEdgeIds[k].Sum(edgeId => edgeIdCount[edgeId]);
                double[] distances = new double[xyz.Count];
                for (int i = 1; i < xyz.Count; i++)
                    distances[i] = distances[i - 1] + GmshPoleChart.GetDistance(xyz[i - 1], xyz[i]);
                lengths[k] = distances[xyz.Count - 1];
                double half = lengths[k] / 2;
                int middle = 0;
                for (int i = 1; i < xyz.Count; i++)
                {
                    if (Math.Abs(distances[i] - half) < Math.Abs(distances[middle] - half)) middle = i;
                }
                middles[k] = xyz[middle];
            }
            double meanLength = (lengths[0] + lengths[1]) / (counts[0] + counts[1]);
            if (!(meanLength > 0)) return false;
            double width = GmshPoleChart.GetDistance(middles[0], middles[1]);
            int difference = Math.Abs(counts[0] - counts[1]);
            int numOfLineElements = (difference + difference % 2) / 2 + MinNumberOfLineElements;
            return numOfLineElements > ThinLineFactor * Math.Max(MinNumberOfLineElements, width / meanLength);
        }
        /// <summary>
        /// Whether a corner of the three- or four-sided face, which carries gmsh ids, is a pole of its surface.
        /// </summary>
        public static bool HasPoleCorner(TransfiniteFace face)
        {
            return IsAnyPole(face.FaceId, face.CornerNodeIds);
        }
        /// <summary>
        /// Whether the reentrant vertex of the lens face, which carries gmsh ids, is a pole of its surface; false for
        /// a face with none.
        /// </summary>
        public static bool HasReentrantPole(LensFace face)
        {
            return face.ReentrantIndices.Length > 0 &&
                   IsAnyPole(face.FaceId, face.ReentrantIndices.Select(k => face.VertexNodeIds[k]));
        }
        private static bool IsAnyPole(int surfaceId, IEnumerable<int> vertexIds)
        {
            double minX, minY, minZ, maxX, maxY, maxZ;
            Gmsh.Model.GetBoundingBox(2, surfaceId, out minX, out minY, out minZ, out maxX, out maxY, out maxZ);
            double size = GmshPoleChart.GetDistance(new double[] { minX, minY, minZ },
                                                    new double[] { maxX, maxY, maxZ });
            foreach (var vertexId in vertexIds)
            {
                double[] coor;
                Gmsh.Model.GetValue(0, vertexId, new double[0], out coor);
                if (coor == null || coor.Length < 3) continue;
                int p;
                double poleV;
                double farV;
                double[] point = new double[] { coor[0], coor[1], coor[2] };
                if (GmshPoleChart.FindPole(surfaceId, point, size, out p, out poleV, out farV)) return true;
            }
            return false;
        }
        /// <summary>
        /// Meshes a three- or four-sided face with a pole of its surface at a corner, on the nodes of its edges: three
        /// blocks of quads or one block, mapped in the chart of the pole and in the parameters of the surface, the
        /// better grid kept (GmshLensGrid.MapPiece); the inner nodes of a four-sided face are then smoothed together
        /// (GmshLensGrid.Smooth). False, with the reason, when the grid folds or has a quad corner below
        /// SubmapFaceDetector.MinScaledJacobian, as every structured face of a shell, and the face is left to gmsh.
        /// </summary>
        public static bool MeshPoleCornerFace(TransfiniteFace face, bool quads, out string reason)
        {
            reason = null;
            GmshLensGrid grid = new GmshLensGrid(face.FaceId);
            int numOfSides = face.NumberOfSides;
            int[] cornerNodeIds;
            int[][] sides;
            try
            {
                int[][] sideVertexIds = new int[numOfSides][];
                for (int k = 0; k < numOfSides; k++)
                    sideVertexIds[k] = new int[] { face.CornerNodeIds[k], face.CornerNodeIds[(k + 1) % numOfSides] };
                AddLoop(grid, face.FaceId, sideVertexIds, face.SideEdgeIds, false, out cornerNodeIds, out sides);
                grid.SetBoundary(cornerNodeIds);
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            int[] sideCounts = sides.Select(side => side.Length - 1).ToArray();
            if (numOfSides == 3 && !TransfiniteSideTriple.AreCountsValid(sideCounts))
            {
                reason = "its sides carry " + string.Join(", ", sideCounts) + " elements, which three blocks of " +
                         "quads cannot fill";
                return false;
            }
            if (numOfSides == 4 && (sideCounts[0] != sideCounts[2] || sideCounts[1] != sideCounts[3]))
            {
                reason = "its sides carry " + string.Join(", ", sideCounts) + " elements, unequal on opposite sides";
                return false;
            }
            if (!grid.MapPiece(sides, out reason)) return false;
            if (numOfSides == 4) grid.Smooth();
            double minScaledJacobian;
            if (!Judge(grid, true, out minScaledJacobian, out reason)) return false;
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
        /// below SubmapFaceDetector.MinScaledJacobian. Also the worst scaled Jacobian of a quad corner in space.
        /// </summary>
        private static bool Judge(GmshLensGrid grid, bool floor, out double minScaledJacobian, out string reason)
        {
            reason = null;
            int numOfFoldedQuads;
            minScaledJacobian = grid.Judge(out numOfFoldedQuads);
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
        internal static string ToReason(string message)
        {
            message = message.Trim().TrimEnd('.');
            if (message.StartsWith("The ")) message = "the " + message.Substring(4);
            return message;
        }
    }
}
