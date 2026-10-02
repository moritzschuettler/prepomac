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
    /// Meshes a three-sided face by ThreeBlockMapping, the structured mapping every mesher shares: gmsh supplies the
    /// nodes its edges carry, their parameters on the surface and the evaluation of the surface. The face is meshed
    /// by gmsh first, onto the nodes of its edges, and its elements are then replaced and turned the way gmsh turned
    /// its own. A grid that folds is refused.
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
            double[][][] sideUv = new double[3][][];
            for (int k = 0; k < 3; k++) sideUv[k] = GetParametricCoordinates(surfaceId, sideXyz[k]);
            //
            ThreeBlockGrid grid = ThreeBlockMapping.Map(sideUv, sideXyz.Select(xyz => xyz.ToArray()).ToArray(), uv =>
            {
                double[] coor;
                Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
                return coor;
            });
            if (grid.NumberOfFoldedQuads > 0)
                throw new CaeException("The three blocks of quads on Surface " + surfaceId + " fold: " +
                                       grid.NumberOfFoldedQuads + " of its " + grid.Quads.Length +
                                       " quads are inverted.");
            // The tags of the boundary nodes and the new inner nodes
            int numOfBoundaryNodes = grid.NumberOfBoundaryNodes;
            IntPtr[] nodeTags = new IntPtr[numOfBoundaryNodes + grid.InnerUv.Length];
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < sideCounts[k]; i++) nodeTags[grid.GetBoundaryNodeId(k, i)] = sideTags[k][i];
            }
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            long nodeTag = maxTag.ToInt64();
            IntPtr[] newTags = new IntPtr[grid.InnerUv.Length];
            for (int i = 0; i < newTags.Length; i++)
            {
                newTags[i] = new IntPtr(++nodeTag);
                nodeTags[numOfBoundaryNodes + i] = newTags[i];
            }
            // The quads turn as the boundary loop does in the parameter space, and must turn the way gmsh turned its
            // own elements on the face
            double[][] loopUv = Enumerable.Range(0, 3).SelectMany(k => sideUv[k].Take(sideCounts[k])).ToArray();
            bool reverse = Math.Sign(GetSignedArea(loopUv)) != GetGmshOrientation(surfaceId);
            IntPtr[] quadNodeTags = new IntPtr[4 * grid.Quads.Length];
            for (int q = 0; q < grid.Quads.Length; q++)
            {
                for (int c = 0; c < 4; c++)
                    quadNodeTags[4 * q + c] = nodeTags[grid.Quads[q][reverse ? 3 - c : c]];
            }
            //
            Gmsh.Model.Mesh.Clear(new Tuple<int, int>[] { new Tuple<int, int>(2, surfaceId) });
            Gmsh.Model.Mesh.AddNodes(2, surfaceId, newTags, grid.InnerXyz.SelectMany(xyz => xyz).ToArray(),
                                     grid.InnerUv.SelectMany(uv => uv).ToArray());
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            long elementTag = maxTag.ToInt64();
            IntPtr[] elementTags = new IntPtr[grid.Quads.Length];
            for (int i = 0; i < elementTags.Length; i++) elementTags[i] = new IntPtr(++elementTag);
            Gmsh.Model.Mesh.AddElements(2, surfaceId, new int[] { (int)GmshElementType.LinearQuadrilateral },
                                        new IntPtr[][] { elementTags }, new IntPtr[][] { quadNodeTags });
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
