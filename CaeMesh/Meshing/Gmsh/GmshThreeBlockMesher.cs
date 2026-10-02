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
using CaeMesh.Sweep;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// Meshes a three-sided face as three structured blocks of quads around one inner node: the inner lines run from
    /// that node to a split point on every side, and each block lies between two of them and a corner. The numbers on
    /// the sides decide where they are split, so a sharp corner, whose two sides are long against the third, gets the
    /// smallest block, and the inner node, placed at the mean of the split points, moves towards it; in the limit the
    /// block at the corner is a single element. The face is meshed by gmsh first, onto the nodes its edges carry, and
    /// its elements are then replaced and turned the way gmsh turned its own.
    /// </summary>
    public static class GmshThreeBlockMesher
    {
        // Methods
        /// <summary>
        /// The face carries gmsh ids: its surface, its corner points and the curves of its sides.
        /// </summary>
        public static void Mesh(TransfiniteFace face)
        {
            int surfaceId = face.FaceId;
            // The nodes of every side from its corner to the next one
            List<IntPtr>[] sideTags = new List<IntPtr>[3];
            List<double[]>[] sideXyz = new List<double[]>[3];
            for (int k = 0; k < 3; k++)
                GetSideNodes(surfaceId, face.CornerNodeIds[k], face.CornerNodeIds[(k + 1) % 3], face.SideEdgeIds[k],
                             out sideTags[k], out sideXyz[k]);
            int[] sideCounts = sideTags.Select(tags => tags.Count - 1).ToArray();
            if (!TransfiniteSideTriple.AreCountsValid(sideCounts))
                throw new CaeException("The sides of Surface " + surfaceId + " carry " + string.Join(", ", sideCounts) +
                                       " elements, which three blocks of quads cannot fill.");
            int[] innerCounts = Enumerable.Range(0, 3).Select(k => TransfiniteSideTriple.GetInnerLineCount(sideCounts, k))
                                          .ToArray();
            // Parameters of the boundary nodes
            double[][][] sideUv = new double[3][][];
            for (int k = 0; k < 3; k++) sideUv[k] = GetParametricCoordinates(surfaceId, sideXyz[k]);
            // The split points: side k is split L(k - 1) elements from its first corner
            double[][] splitUv = new double[3][];
            IntPtr[] splitTags = new IntPtr[3];
            for (int k = 0; k < 3; k++)
            {
                int index = innerCounts[(k + 2) % 3];
                splitUv[k] = sideUv[k][index];
                splitTags[k] = sideTags[k][index];
            }
            double[] centreUv = new double[] { splitUv.Average(uv => uv[0]), splitUv.Average(uv => uv[1]) };
            // New nodes
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            long nodeTag = maxTag.ToInt64();
            List<IntPtr> newTags = new List<IntPtr>();
            List<double[]> newUv = new List<double[]>();
            Func<double[], IntPtr> addNode = uv =>
            {
                IntPtr tag = new IntPtr(++nodeTag);
                newTags.Add(tag);
                newUv.Add(uv);
                return tag;
            };
            IntPtr centreTag = addNode(centreUv);
            // Inner line k from the split point of side k to the inner node
            IntPtr[][] lineTags = new IntPtr[3][];
            double[][][] lineUv = new double[3][][];
            for (int k = 0; k < 3; k++)
            {
                int n = innerCounts[k];
                lineTags[k] = new IntPtr[n + 1];
                lineUv[k] = new double[n + 1][];
                for (int j = 0; j <= n; j++)
                {
                    double t = (double)j / n;
                    lineUv[k][j] = new double[] { (1 - t) * splitUv[k][0] + t * centreUv[0],
                                                  (1 - t) * splitUv[k][1] + t * centreUv[1] };
                    if (j == 0) lineTags[k][j] = splitTags[k];
                    else if (j == n) lineTags[k][j] = centreTag;
                    else lineTags[k][j] = addNode(lineUv[k][j]);
                }
            }
            // Block k at corner k: side k to its split point, inner line k to the inner node, inner line k - 1 from the
            // split point of side k - 1, and side k - 1 back to the corner
            List<IntPtr[]> quads = new List<IntPtr[]>();
            List<double[][]> quadUv = new List<double[][]>();
            for (int k = 0; k < 3; k++)
            {
                int previous = (k + 2) % 3;
                int a = innerCounts[previous];
                int b = innerCounts[k];
                IntPtr[,] tags = new IntPtr[a + 1, b + 1];
                double[,][] uvs = new double[a + 1, b + 1][];
                int previousCount = sideCounts[previous];
                for (int i = 0; i <= a; i++)
                {
                    tags[i, 0] = sideTags[k][i];
                    uvs[i, 0] = sideUv[k][i];
                    tags[i, b] = lineTags[previous][i];
                    uvs[i, b] = lineUv[previous][i];
                }
                for (int j = 0; j <= b; j++)
                {
                    tags[0, j] = sideTags[previous][previousCount - j];
                    uvs[0, j] = sideUv[previous][previousCount - j];
                    tags[a, j] = lineTags[k][j];
                    uvs[a, j] = lineUv[k][j];
                }
                for (int i = 1; i < a; i++)
                {
                    for (int j = 1; j < b; j++)
                    {
                        uvs[i, j] = GetTransfiniteUv(uvs, a, b, i, j);
                        tags[i, j] = addNode(uvs[i, j]);
                    }
                }
                for (int i = 0; i < a; i++)
                {
                    for (int j = 0; j < b; j++)
                    {
                        quads.Add(new IntPtr[] { tags[i, j], tags[i + 1, j], tags[i + 1, j + 1], tags[i, j + 1] });
                        quadUv.Add(new double[][] { uvs[i, j], uvs[i + 1, j], uvs[i + 1, j + 1], uvs[i, j + 1] });
                    }
                }
            }
            // The quads turn the way gmsh turned its own elements on the face
            double signedArea = quadUv.Sum(uv => GetSignedArea(uv));
            if (Math.Sign(signedArea) != GetGmshOrientation(surfaceId))
            {
                foreach (var quad in quads) Array.Reverse(quad);
            }
            //
            double[] newCoor = new double[0];
            double[] flatUv = newUv.SelectMany(uv => uv).ToArray();
            if (flatUv.Length > 0) Gmsh.Model.GetValue(2, surfaceId, flatUv, out newCoor);
            if (newCoor.Length != 3 * newTags.Count)
                throw new CaeException("The nodes inside Surface " + surfaceId + " could not be placed on the surface.");
            //
            Gmsh.Model.Mesh.Clear(new Tuple<int, int>[] { new Tuple<int, int>(2, surfaceId) });
            Gmsh.Model.Mesh.AddNodes(2, surfaceId, newTags.ToArray(), newCoor, flatUv);
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            long elementTag = maxTag.ToInt64();
            IntPtr[] elementTags = new IntPtr[quads.Count];
            for (int i = 0; i < elementTags.Length; i++) elementTags[i] = new IntPtr(++elementTag);
            Gmsh.Model.Mesh.AddElements(2, surfaceId, new int[] { (int)GmshElementType.LinearQuadrilateral },
                                        new IntPtr[][] { elementTags },
                                        new IntPtr[][] { quads.SelectMany(quad => quad).ToArray() });
        }
        /// <summary>
        /// The nodes of a side, from its first corner to its last, along the chain of its edges.
        /// </summary>
        private static void GetSideNodes(int surfaceId, int firstCornerId, int lastCornerId, int[] edgeIds,
                                         out List<IntPtr> tags, out List<double[]> xyz)
        {
            tags = new List<IntPtr>();
            xyz = new List<double[]>();
            int vertexId = firstCornerId;
            AddVertexNode(vertexId, tags, xyz);
            foreach (var edgeId in edgeIds)
            {
                int[] vertexIds;
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                if (vertexIds.Length != 2 || !vertexIds.Contains(vertexId))
                    throw new CaeException("The sides of Surface " + surfaceId + " do not run along Edge " + edgeId + ".");
                // The interior nodes run along the parameter of the edge, from its first vertex
                IntPtr[] edgeTags;
                double[] edgeCoor;
                SweepQuadWildInput.GetCurveNodes(edgeId, out edgeTags, out edgeCoor);
                int[] order = Enumerable.Range(0, edgeTags.Length).ToArray();
                if (vertexIds[0] != vertexId) Array.Reverse(order);
                foreach (var i in order)
                {
                    tags.Add(edgeTags[i]);
                    xyz.Add(new double[] { edgeCoor[3 * i], edgeCoor[3 * i + 1], edgeCoor[3 * i + 2] });
                }
                vertexId = vertexIds[0] == vertexId ? vertexIds[1] : vertexIds[0];
                AddVertexNode(vertexId, tags, xyz);
            }
            if (vertexId != lastCornerId)
                throw new CaeException("The sides of Surface " + surfaceId + " do not end at its corners.");
        }
        private static void AddVertexNode(int vertexId, List<IntPtr> tags, List<double[]> xyz)
        {
            IntPtr[] vertexTags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out vertexTags, out coor, 0, vertexId, false, false);
            tags.Add(vertexTags[0]);
            xyz.Add(new double[] { coor[0], coor[1], coor[2] });
        }
        private static double[][] GetParametricCoordinates(int surfaceId, List<double[]> xyz)
        {
            double[] uv;
            Gmsh.Model.GetParametrization(2, surfaceId, xyz.SelectMany(point => point).ToArray(), out uv);
            if (uv.Length != 2 * xyz.Count)
                throw new CaeException("The nodes on the sides of Surface " + surfaceId + " could not be placed on it.");
            return Enumerable.Range(0, xyz.Count).Select(i => new double[] { uv[2 * i], uv[2 * i + 1] }).ToArray();
        }
        /// <summary>
        /// The transfinite (Coons) interpolation of a block between its four sides, by the index along each.
        /// </summary>
        private static double[] GetTransfiniteUv(double[,][] uvs, int a, int b, int i, int j)
        {
            double xi = (double)i / a;
            double eta = (double)j / b;
            double[] uv = new double[2];
            for (int d = 0; d < 2; d++)
            {
                uv[d] = (1 - eta) * uvs[i, 0][d] + eta * uvs[i, b][d] + (1 - xi) * uvs[0, j][d] + xi * uvs[a, j][d] -
                        ((1 - xi) * (1 - eta) * uvs[0, 0][d] + xi * (1 - eta) * uvs[a, 0][d] +
                         xi * eta * uvs[a, b][d] + (1 - xi) * eta * uvs[0, b][d]);
            }
            return uv;
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
        /// <summary>
        /// Whether the elements gmsh meshed on the surface turn with its parameters (1) or against them (-1).
        /// </summary>
        private static int GetGmshOrientation(int surfaceId)
        {
            IntPtr[] tags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out tags, out coor, 2, surfaceId, true, false);
            Dictionary<IntPtr, int> tagIndex = new Dictionary<IntPtr, int>();
            for (int i = 0; i < tags.Length; i++) tagIndex[tags[i]] = i;
            double[] uv;
            Gmsh.Model.GetParametrization(2, surfaceId, coor, out uv);
            if (uv.Length != 2 * tags.Length) return 1;
            //
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
            double signedArea = 0;
            for (int t = 0; t < elementTypes.Length; t++)
            {
                int numOfNodes = elementTypes[t] == (int)GmshElementType.LinearTriangle ? 3 :
                                 elementTypes[t] == (int)GmshElementType.LinearQuadrilateral ? 4 : 0;
                if (numOfNodes == 0) continue;
                for (int e = 0; e < elementTags[t].Length; e++)
                {
                    double[][] polygon = new double[numOfNodes][];
                    for (int n = 0; n < numOfNodes; n++)
                    {
                        int index = tagIndex[nodeTags[t][e * numOfNodes + n]];
                        polygon[n] = new double[] { uv[2 * index], uv[2 * index + 1] };
                    }
                    signedArea += GetSignedArea(polygon);
                }
            }
            return signedArea < 0 ? -1 : 1;
        }
    }
}
