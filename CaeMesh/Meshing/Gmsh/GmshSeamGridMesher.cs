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
using CaeMesh.Sweep;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// The structured grid of a face closed along a single seam between two closed rims, the wall of a sweep around a
    /// bore (SweepSeamWallMesher) and a tube of a shell (GmshTubeMesher) alike. Line 0 of the grid is the seam with the
    /// nodes gmsh placed on it, its levels from the first rim to the second; the nodes of each rim run once around from
    /// the seam in the direction of the periodic parameter, and node i of the first rim is joined to node i of the
    /// second. The inner nodes lie in the parameters of the surface on those lines at the levels of the seam: the
    /// periodic parameter is that of the seam at the level plus the share of the way round interpolated between the two
    /// rims, the other parameter is that of the seam at the level plus the distance of the rim node from the end of the
    /// seam on its rim interpolated between the two rims, both by the length of the seam up to the level. A line of the
    /// grid thus stays at the level of its node on the seam also where the parameter does not run evenly along the seam.
    /// </summary>
    public static class GmshSeamGridMesher
    {
        // Methods                                                                                                                  
        /// <summary>
        /// The nodes of the seam from its vertex on the first rim to its vertex on the second. The quasi-structured quad
        /// algorithm hands the inner nodes of a seam to the face it closes, deletes the elements of the seam and goes on
        /// optimizing the face (transferSeamGEdgesVerticesToGFace in gmsh), so the nodes are no longer on the seam. The
        /// levels are then laid along it as a transfinite curve of the count would lay them, and their tags are zero
        /// until the grid is laid.
        /// </summary>
        public static void GetSeamNodes(int surfaceId, int seamId, int firstVertexId, int secondVertexId,
                                        int seamNumOfElements, out List<IntPtr> seamTags, out List<double[]> seamXyz,
                                        out int numOfNewSeamNodes)
        {
            GmshThreeBlockMesher.GetSideNodes(surfaceId, firstVertexId, secondVertexId, new int[] { seamId },
                                              out seamTags, out seamXyz);
            numOfNewSeamNodes = 0;
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
        /// <summary>
        /// Lays the grid on the nodes of the seam and of the two rims, each rim from its node on the seam, and gives the
        /// tags of its nodes by line from the first rim, the new nodes - the seam levels GetSeamNodes laid and the inner
        /// nodes - with their tags, coordinates and parameters, the coordinates and parameters of every node of the grid
        /// by its tag, the order in which each rim runs round in the periodic parameter by the indices of its nodes, and
        /// the periodic parameter, the one the nodes of the first rim spread over the most.
        /// </summary>
        public static IntPtr[][] LayGrid(int surfaceId, List<IntPtr> seamTags, List<double[]> seamXyz, int numOfNewSeamNodes,
                                         List<IntPtr> firstTags, List<double[]> firstXyz, List<IntPtr> secondTags,
                                         List<double[]> secondXyz, out List<IntPtr> newTags, out List<double> newXyz,
                                         out List<double> newUv, out Dictionary<IntPtr, double[]> tagXyz,
                                         out Dictionary<IntPtr, double[]> tagUv, out int[] firstOrder,
                                         out int[] secondOrder, out int periodicDirection)
        {
            // The periodic parameter is the one the nodes of a ring spread over
            double[][] sourceUv = GetParametricCoordinates(surfaceId, firstXyz);
            double[][] targetUv = GetParametricCoordinates(surfaceId, secondXyz);
            double[][] seamUv = GetParametricCoordinates(surfaceId, seamXyz);
            double[] min;
            double[] max;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
            int p = GetSpread(sourceUv, 0) >= GetSpread(sourceUv, 1) ? 0 : 1;
            int a = 1 - p;
            double period = max[p] - min[p];
            periodicDirection = p;
            // Every ring runs from its vertex on the seam once around, in the direction of the periodic parameter
            int n = firstTags.Count;
            double[] sourceW = GetUnwrapped(sourceUv, p, period);
            double[] targetW = GetUnwrapped(targetUv, p, period);
            int[] sourceOrder = GetRingOrder(sourceW);
            int[] targetOrder = GetRingOrder(targetW);
            firstOrder = sourceOrder;
            secondOrder = targetOrder;
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
            newTags = new List<IntPtr>();
            newXyz = new List<double>();
            newUv = new List<double>();
            for (int j = 1; j <= numOfNewSeamNodes; j++)
            {
                seamTags[j] = new IntPtr(++nodeTag);
                newTags.Add(seamTags[j]);
                newXyz.AddRange(seamXyz[j]);
                newUv.AddRange(seamUv[j]);
            }
            grid[0] = sourceOrder.Select(i => firstTags[i]).ToArray();
            grid[m] = targetOrder.Select(i => secondTags[i]).ToArray();
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
                    uv[a] = seamUv[j][a] + (sourceUv[si][a] - seamUv[0][a]) * (1 - s) +
                            (targetUv[ti][a] - seamUv[m][a]) * s;
                    double[] coor;
                    Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
                    grid[j][i] = new IntPtr(++nodeTag);
                    newTags.Add(grid[j][i]);
                    newXyz.AddRange(coor);
                    newUv.AddRange(uv);
                }
            }
            //
            tagXyz = new Dictionary<IntPtr, double[]>();
            tagUv = new Dictionary<IntPtr, double[]>();
            for (int i = 0; i < seamTags.Count; i++)
            {
                tagXyz[seamTags[i]] = seamXyz[i];
                tagUv[seamTags[i]] = seamUv[i];
            }
            for (int i = 0; i < n; i++)
            {
                tagXyz[firstTags[i]] = firstXyz[i];
                tagXyz[secondTags[i]] = secondXyz[i];
                tagUv[firstTags[i]] = sourceUv[i];
                tagUv[secondTags[i]] = targetUv[i];
            }
            for (int i = 0; i < newTags.Count; i++)
            {
                tagXyz[newTags[i]] = new double[] { newXyz[3 * i], newXyz[3 * i + 1], newXyz[3 * i + 2] };
                tagUv[newTags[i]] = new double[] { newUv[2 * i], newUv[2 * i + 1] };
            }
            return grid;
        }
        /// <summary>
        /// The tags of the quads of the grid, four by four, line by line around from the seam.
        /// </summary>
        public static IntPtr[] GetQuadNodeTags(IntPtr[][] grid)
        {
            int m = grid.Length - 1;
            int n = grid[0].Length;
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
            return quadNodeTags;
        }
        /// <summary>
        /// Whether the nodes of a rim, read along its edges from the seam, run round the periodic parameter in order one way
        /// or the other: the order of the parameter is that of the edges, or its reverse after the node on the seam.
        /// </summary>
        internal static bool RunsOnceRound(int[] order)
        {
            int n = order.Length;
            bool forward = true;
            bool backward = true;
            for (int i = 1; i < n; i++)
            {
                forward &= order[i] == i;
                backward &= order[i] == n - i;
            }
            return forward || backward;
        }
        /// <summary>
        /// The smallest scaled Jacobian of a quad corner in space against the normal of the surface, the quads turned the
        /// way most corners turn, and the number of quads with a corner that turns the other way or has no area. A corner
        /// at a singular node, the pole of a cap, where the surface has no normal, is measured against the normal of its
        /// own quad (GetOwnScaledJacobian).
        /// </summary>
        internal static double GetMinScaledJacobian(int surfaceId, IntPtr[] quadNodeTags, Dictionary<IntPtr, double[]> tagXyz,
                                                    Dictionary<IntPtr, double[]> tagUv, ICollection<IntPtr> singularTags,
                                                    out int numOfFoldedQuads)
        {
            Dictionary<IntPtr, int> tagIndex = new Dictionary<IntPtr, int>();
            List<double[]> xyz = new List<double[]>();
            List<double[]> uv = new List<double[]>();
            int numOfQuads = quadNodeTags.Length / 4;
            int[][] quads = new int[numOfQuads][];
            for (int q = 0; q < numOfQuads; q++)
            {
                quads[q] = new int[4];
                for (int c = 0; c < 4; c++)
                {
                    IntPtr tag = quadNodeTags[4 * q + c];
                    int index;
                    if (!tagIndex.TryGetValue(tag, out index))
                    {
                        index = xyz.Count;
                        tagIndex.Add(tag, index);
                        xyz.Add(tagXyz[tag]);
                        uv.Add(tagUv[tag]);
                    }
                    quads[q][c] = index;
                }
            }
            double[][] points = xyz.ToArray();
            double[][] normals = GmshQuadMesher.GetNormals(surfaceId, uv.ToArray());
            HashSet<int> singular = new HashSet<int>();
            if (singularTags != null)
            {
                int index;
                foreach (var tag in singularTags)
                {
                    if (tagIndex.TryGetValue(tag, out index)) singular.Add(index);
                }
            }
            double sum = 0;
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (!singular.Contains(quad[c]))
                        sum += GmshQuadMesher.GetSpaceScaledJacobian(quad, c, points, normals, 1);
                }
            }
            double orientation = sum < 0 ? -1 : 1;
            double min = 1;
            numOfFoldedQuads = 0;
            foreach (var quad in quads)
            {
                bool folded = false;
                for (int c = 0; c < 4; c++)
                {
                    double scaledJacobian = singular.Contains(quad[c]) ? GetOwnScaledJacobian(quad, c, points) :
                                            GmshQuadMesher.GetSpaceScaledJacobian(quad, c, points, normals, orientation);
                    folded |= !(scaledJacobian > 0);
                    min = Math.Min(min, scaledJacobian);
                }
                if (folded) numOfFoldedQuads++;
            }
            return min;
        }
        /// <summary>
        /// The sine of the angle at corner c of the quad against the normal of the quad by its diagonals, where the
        /// surface has no normal: the corners round the apex of a cone each lie in a plane of their own, which a mean
        /// normal would cut at a slant.
        /// </summary>
        private static double GetOwnScaledJacobian(int[] quad, int c, double[][] points)
        {
            double[] d1 = Subtract(points[quad[2]], points[quad[0]]);
            double[] d2 = Subtract(points[quad[3]], points[quad[1]]);
            double[] normal = Cross(d1, d2);
            double[] a = Subtract(points[quad[(c + 1) % 4]], points[quad[c]]);
            double[] b = Subtract(points[quad[(c + 3) % 4]], points[quad[c]]);
            double[] cross = Cross(a, b);
            double lengths = Math.Sqrt((a[0] * a[0] + a[1] * a[1] + a[2] * a[2]) * (b[0] * b[0] + b[1] * b[1] + b[2] * b[2]) *
                                       (normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]));
            if (!(lengths > 0)) return -1;
            return (cross[0] * normal[0] + cross[1] * normal[1] + cross[2] * normal[2]) / lengths;
        }
        private static double[] Cross(double[] a, double[] b)
        {
            return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        }
        private static double[] Subtract(double[] a, double[] b)
        {
            return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        }
        /// <summary>
        /// Replaces what gmsh meshed on the surface by the quads of the grid and its new nodes. The quads turn the way gmsh
        /// turned its own elements against the normal of the surface. Without quads every quad is split into two triangles.
        /// </summary>
        public static void ReplaceSurfaceMesh(int surfaceId, IntPtr[] quadNodeTags, List<IntPtr> newTags, List<double> newXyz,
                                              List<double> newUv, Dictionary<IntPtr, double[]> tagXyz, bool quads)
        {
            int numOfQuads = quadNodeTags.Length / 4;
            int gmshTurn = GetGmshTurn(surfaceId);
            double[] firstQuad = new double[12];
            for (int c = 0; c < 4; c++) Array.Copy(tagXyz[quadNodeTags[c]], 0, firstQuad, 3 * c, 3);
            if (GetTurn(surfaceId, firstQuad, 4) != gmshTurn)
            {
                for (int q = 0; q < numOfQuads; q++)
                {
                    IntPtr swap = quadNodeTags[4 * q + 1];
                    quadNodeTags[4 * q + 1] = quadNodeTags[4 * q + 3];
                    quadNodeTags[4 * q + 3] = swap;
                }
            }
            //
            Gmsh.Model.Mesh.Clear(new Tuple<int, int>[] { new Tuple<int, int>(2, surfaceId) });
            Gmsh.Model.Mesh.AddNodes(2, surfaceId, newTags.ToArray(), newXyz.ToArray(), newUv.ToArray());
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            long elementTag = maxTag.ToInt64();
            int elementType = (int)GmshElementType.LinearQuadrilateral;
            IntPtr[] elementNodeTags = quadNodeTags;
            if (!quads)
            {
                int[] corners = new int[] { 0, 1, 2, 0, 2, 3 };
                elementType = (int)GmshElementType.LinearTriangle;
                elementNodeTags = new IntPtr[6 * numOfQuads];
                for (int q = 0; q < numOfQuads; q++)
                {
                    for (int c = 0; c < 6; c++) elementNodeTags[6 * q + c] = quadNodeTags[4 * q + corners[c]];
                }
            }
            IntPtr[] elementTags = new IntPtr[quads ? numOfQuads : 2 * numOfQuads];
            for (int i = 0; i < elementTags.Length; i++) elementTags[i] = new IntPtr(++elementTag);
            Gmsh.Model.Mesh.AddElements(2, surfaceId, new int[] { elementType }, new IntPtr[][] { elementTags },
                                        new IntPtr[][] { elementNodeTags });
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
