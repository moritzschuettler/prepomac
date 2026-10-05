// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
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
    /// level along the seam in the parameters of the surface onto the nodes of the target edge. The grid turns the way
    /// gmsh turned its own elements on the face.
    /// </summary>
    public static class SweepSeamWallMesher
    {
        // Methods
        public static bool IsSeamWall(SweepWall wall)
        {
            return wall.DirectionEdgeIds.Length == 1 && wall.SourceEdgeIds.Length == 1 && wall.TargetEdgeIds.Length == 1 &&
                   IsClosedEdge(wall.SourceEdgeIds[0]) && IsClosedEdge(wall.TargetEdgeIds[0]);
        }
        /// <summary>
        /// The points inside a seam that divide it into the given number of pieces of equal length, from the end at start.
        /// </summary>
        private static List<double[]> GetSeamLevels(int seamId, double[] start, int numOfElements)
        {
            double[] min;
            double[] max;
            Gmsh.Model.GetParametrizationBounds(1, seamId, out min, out max);
            int numOfSamples = 200;
            double[] parameters = new double[numOfSamples + 1];
            for (int i = 0; i <= numOfSamples; i++) parameters[i] = min[0] + (max[0] - min[0]) * i / numOfSamples;
            double[] coor;
            Gmsh.Model.GetValue(1, seamId, parameters, out coor);
            double[][] points = Enumerable.Range(0, numOfSamples + 1)
                                          .Select(i => new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] }).ToArray();
            if (Distance(points[numOfSamples], start) < Distance(points[0], start)) Array.Reverse(points);
            double[] length = new double[numOfSamples + 1];
            for (int i = 1; i <= numOfSamples; i++) length[i] = length[i - 1] + Distance(points[i - 1], points[i]);
            //
            List<double[]> levels = new List<double[]>();
            int k = 1;
            for (int j = 1; j < numOfElements; j++)
            {
                double target = length[numOfSamples] * j / numOfElements;
                while (k < numOfSamples && length[k] < target) k++;
                double t = length[k] > length[k - 1] ? (target - length[k - 1]) / (length[k] - length[k - 1]) : 0;
                double[] point = new double[3];
                for (int c = 0; c < 3; c++) point[c] = points[k - 1][c] + (points[k][c] - points[k - 1][c]) * t;
                double[] closest;
                Gmsh.Model.GetClosestPoint(1, seamId, point, out closest, out _);
                levels.Add(closest.Length >= 3 ? closest : point);
            }
            return levels;
        }
        public static void Mesh(SweepWall wall, int seamNumOfElements)
        {
            int surfaceId = wall.SurfaceId;
            int seamId = wall.DirectionEdgeIds[0];
            int sourceVertexId = GetVertexIds(wall.SourceEdgeIds[0]).First();
            int targetVertexId = GetVertexIds(wall.TargetEdgeIds[0]).First();
            // The seam from the source edge to the target edge: the levels of the grid
            List<IntPtr> seamTags;
            List<double[]> seamXyz;
            GmshThreeBlockMesher.GetSideNodes(surfaceId, sourceVertexId, targetVertexId, new int[] { seamId },
                                              out seamTags, out seamXyz);
            // The quasi-structured quad algorithm hands the inner nodes of a seam to the face it closes, deletes the elements
            // of the seam and goes on optimizing the face (transferSeamGEdgesVerticesToGFace in gmsh), so the nodes are no
            // longer on the seam. The levels are then laid along it as a transfinite curve of the count would lay them;
            // read as one level against the six of the walls beside it, the seam of sweep_51 sent every sweep under
            // that algorithm back to the Frontal-Delaunay one
            int numOfNewSeamNodes = 0;
            if (seamTags.Count == 2 && seamNumOfElements > 1)
            {
                List<double[]> levels = GetSeamLevels(seamId, seamXyz[0], seamNumOfElements);
                for (int j = 0; j < levels.Count; j++)
                {
                    seamTags.Insert(1 + j, IntPtr.Zero);
                    seamXyz.Insert(1 + j, levels[j]);
                }
                numOfNewSeamNodes = levels.Count;
            }
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
            // The periodic parameter is the one the nodes of a ring spread over
            double[][] sourceUv = GetParametricCoordinates(surfaceId, sourceXyz);
            double[][] targetUv = GetParametricCoordinates(surfaceId, targetXyz);
            double[][] seamUv = GetParametricCoordinates(surfaceId, seamXyz);
            double[] min;
            double[] max;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
            int p = GetSpread(sourceUv, 0) >= GetSpread(sourceUv, 1) ? 0 : 1;
            int a = 1 - p;
            double period = max[p] - min[p];
            // Every ring runs from its vertex on the seam once around, in the direction of the periodic parameter
            int n = sourceTags.Count;
            double[] sourceW = GetUnwrapped(sourceUv, p, period);
            double[] targetW = GetUnwrapped(targetUv, p, period);
            int[] sourceOrder = GetRingOrder(sourceW);
            int[] targetOrder = GetRingOrder(targetW);
            // The levels lie along the seam by its length
            int m = seamTags.Count - 1;
            double[] seamLength = new double[m + 1];
            for (int j = 1; j <= m; j++)
                seamLength[j] = seamLength[j - 1] + Distance(seamXyz[j - 1], seamXyz[j]);
            //
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            long nodeTag = maxTag.ToInt64();
            IntPtr[][] grid = new IntPtr[m + 1][];
            List<IntPtr> newTags = new List<IntPtr>();
            List<double> newXyz = new List<double>();
            List<double> newUv = new List<double>();
            for (int j = 1; j <= numOfNewSeamNodes; j++)
            {
                seamTags[j] = new IntPtr(++nodeTag);
                newTags.Add(seamTags[j]);
                newXyz.AddRange(seamXyz[j]);
                newUv.AddRange(seamUv[j]);
            }
            grid[0] = sourceOrder.Select(i => sourceTags[i]).ToArray();
            grid[m] = targetOrder.Select(i => targetTags[i]).ToArray();
            for (int j = 1; j < m; j++)
            {
                double s = seamLength[j] / seamLength[m];
                grid[j] = new IntPtr[n];
                grid[j][0] = seamTags[j];
                for (int i = 1; i < n; i++)
                {
                    int si = sourceOrder[i];
                    int ti = targetOrder[i];
                    double w = sourceW[si] + (targetW[ti] - sourceW[si]) * s;
                    double[] uv = new double[2];
                    uv[p] = Wrap(seamUv[j][p] + w, min[p], period);
                    uv[a] = sourceUv[si][a] + (targetUv[ti][a] - sourceUv[si][a]) * s;
                    double[] coor;
                    Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
                    grid[j][i] = new IntPtr(++nodeTag);
                    newTags.Add(grid[j][i]);
                    newXyz.AddRange(coor);
                    newUv.AddRange(uv);
                }
            }
            //
            Dictionary<IntPtr, double[]> tagXyz = new Dictionary<IntPtr, double[]>();
            for (int i = 0; i < seamTags.Count; i++) tagXyz[seamTags[i]] = seamXyz[i];
            for (int i = 0; i < n; i++)
            {
                tagXyz[sourceTags[i]] = sourceXyz[i];
                tagXyz[targetTags[i]] = targetXyz[i];
            }
            for (int i = 0; i < newTags.Count; i++)
                tagXyz[newTags[i]] = new double[] { newXyz[3 * i], newXyz[3 * i + 1], newXyz[3 * i + 2] };
            //
            IntPtr[] quadNodeTags = new IntPtr[4 * m * n];
            int count = 0;
            for (int j = 0; j < m; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    quadNodeTags[count++] = grid[j][i];
                    quadNodeTags[count++] = grid[j][(i + 1) % n];
                    quadNodeTags[count++] = grid[j + 1][(i + 1) % n];
                    quadNodeTags[count++] = grid[j + 1][i];
                }
            }
            // The quads turn the way gmsh turned its own elements against the normal of the surface
            int gmshTurn = GetGmshTurn(surfaceId);
            double[] firstQuad = new double[12];
            for (int c = 0; c < 4; c++) Array.Copy(tagXyz[quadNodeTags[c]], 0, firstQuad, 3 * c, 3);
            if (GetTurn(surfaceId, firstQuad, 4) != gmshTurn)
            {
                for (int q = 0; q < m * n; q++)
                {
                    IntPtr swap = quadNodeTags[4 * q + 1];
                    quadNodeTags[4 * q + 1] = quadNodeTags[4 * q + 3];
                    quadNodeTags[4 * q + 3] = swap;
                }
            }
            //
            Gmsh.Model.Mesh.Clear(new Tuple<int, int>[] { new Tuple<int, int>(2, surfaceId) });
            Gmsh.Model.Mesh.AddNodes(2, surfaceId, newTags.ToArray(), newXyz.ToArray(), newUv.ToArray());
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            long elementTag = maxTag.ToInt64();
            IntPtr[] elementTags = new IntPtr[m * n];
            for (int i = 0; i < elementTags.Length; i++) elementTags[i] = new IntPtr(++elementTag);
            Gmsh.Model.Mesh.AddElements(2, surfaceId, new int[] { (int)GmshElementType.LinearQuadrilateral },
                                        new IntPtr[][] { elementTags }, new IntPtr[][] { quadNodeTags });
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
        private static double[][] GetParametricCoordinates(int surfaceId, List<double[]> xyz)
        {
            double[] uv;
            Gmsh.Model.GetParametrization(2, surfaceId, xyz.SelectMany(point => point).ToArray(), out uv);
            if (uv.Length != 2 * xyz.Count)
                throw new CaeException("The nodes on the edges of Surface " + surfaceId + " could not be placed on it.");
            return Enumerable.Range(0, xyz.Count).Select(i => new double[] { uv[2 * i], uv[2 * i + 1] }).ToArray();
        }
        private static double GetSpread(double[][] uv, int k)
        {
            return uv.Max(point => point[k]) - uv.Min(point => point[k]);
        }
        // The periodic parameter of every node of a ring from the vertex, the first node, in [0, period)
        private static double[] GetUnwrapped(double[][] uv, int p, double period)
        {
            double start = uv[0][p];
            return uv.Select((point, i) => i == 0 ? 0 : Wrap(point[p] - start, 0, period)).ToArray();
        }
        // The vertex first and then the nodes in the order of the periodic parameter
        private static int[] GetRingOrder(double[] w)
        {
            return new int[] { 0 }.Concat(Enumerable.Range(1, w.Length - 1).OrderBy(i => w[i])).ToArray();
        }
        private static double Wrap(double value, double start, double period)
        {
            double wrapped = (value - start) % period;
            if (wrapped < 0) wrapped += period;
            return start + wrapped;
        }
        private static double Distance(double[] p, double[] q)
        {
            return Math.Sqrt((p[0] - q[0]) * (p[0] - q[0]) + (p[1] - q[1]) * (p[1] - q[1]) + (p[2] - q[2]) * (p[2] - q[2]));
        }
        /// <summary>
        /// Whether the first element gmsh meshed on the surface turns with the normal of the surface (1) or against it.
        /// </summary>
        private static int GetGmshTurn(int surfaceId)
        {
            IntPtr[] tags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out tags, out coor, 2, surfaceId, true, false);
            Dictionary<IntPtr, int> tagIndex = new Dictionary<IntPtr, int>();
            for (int i = 0; i < tags.Length; i++) tagIndex[tags[i]] = i;
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
            for (int t = 0; t < elementTypes.Length; t++)
            {
                int numOfNodes = elementTypes[t] == (int)GmshElementType.LinearTriangle ? 3 :
                                 elementTypes[t] == (int)GmshElementType.LinearQuadrilateral ? 4 : 0;
                if (numOfNodes == 0 || nodeTags[t].Length < numOfNodes) continue;
                double[] polygon = new double[3 * numOfNodes];
                for (int c = 0; c < numOfNodes; c++) Array.Copy(coor, 3 * tagIndex[nodeTags[t][c]], polygon, 3 * c, 3);
                return GetTurn(surfaceId, polygon, numOfNodes);
            }
            return 1;
        }
        private static int GetTurn(int surfaceId, double[] polygon, int numOfNodes)
        {
            double[] centre = new double[3];
            for (int c = 0; c < numOfNodes; c++)
            {
                for (int k = 0; k < 3; k++) centre[k] += polygon[3 * c + k] / numOfNodes;
            }
            double[] normal = new double[3];
            for (int c = 0; c < numOfNodes; c++)
            {
                int d = (c + 1) % numOfNodes;
                double[] e1 = new double[] { polygon[3 * c] - centre[0], polygon[3 * c + 1] - centre[1],
                                             polygon[3 * c + 2] - centre[2] };
                double[] e2 = new double[] { polygon[3 * d] - centre[0], polygon[3 * d + 1] - centre[1],
                                             polygon[3 * d + 2] - centre[2] };
                normal[0] += e1[1] * e2[2] - e1[2] * e2[1];
                normal[1] += e1[2] * e2[0] - e1[0] * e2[2];
                normal[2] += e1[0] * e2[1] - e1[1] * e2[0];
            }
            double[] uv;
            Gmsh.Model.GetParametrization(2, surfaceId, polygon.Take(3).ToArray(), out uv);
            double[] surfaceNormal;
            Gmsh.Model.GetNormal(surfaceId, uv, out surfaceNormal);
            double dot = normal[0] * surfaceNormal[0] + normal[1] * surfaceNormal[1] + normal[2] * surfaceNormal[2];
            return dot >= 0 ? 1 : -1;
        }
    }
}
