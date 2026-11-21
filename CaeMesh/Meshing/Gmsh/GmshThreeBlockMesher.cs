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
    /// Meshes a three-sided face by ThreeBlockMapping, the structured mapping every mesher shares: gmsh supplies the
    /// nodes its edges carry, their parameters on the surface and the evaluation of the surface. The face is meshed
    /// by gmsh first, onto the nodes of its edges, and its elements are then replaced and turned the way gmsh turned
    /// its own. A grid that folds is refused; on a shell the grid must also pass the gate of the other structured
    /// faces, judged in space, or the face is left as gmsh meshed it.
    /// </summary>
    public static class GmshThreeBlockMesher
    {
        // Variables                                                                                                                
        private const double SharpCornerShare = 0.5;


        // Methods                                                                                                                  
        /// <summary>
        /// The face carries gmsh ids: its surface, its corner points and the curves of its sides. Without quads, as for a
        /// surface that is not recombined, every quad is split into two triangles.
        /// </summary>
        public static void Mesh(TransfiniteFace face, bool quads = true)
        {
            ThreeBlockGrid grid;
            Mesh(face, quads, out grid);
        }
        /// <summary>
        /// Meshes the face and returns the gmsh tags of the nodes of its grid, by the node ids of the grid.
        /// </summary>
        public static IntPtr[] Mesh(TransfiniteFace face, bool quads, out ThreeBlockGrid grid)
        {
            int surfaceId = face.FaceId;
            List<IntPtr>[] sideTags;
            List<double[]>[] sideXyz;
            double[][][] sideUv;
            grid = Map(face, out sideTags, out sideXyz, out sideUv);
            if (grid.NumberOfFoldedQuads > 0)
                throw new CaeException("The three blocks of quads on Surface " + surfaceId + " fold: " +
                                       grid.NumberOfFoldedQuads + " of its " + grid.Quads.Length +
                                       " quads are inverted.");
            return Replace(surfaceId, grid, sideTags, sideUv, quads);
        }
        /// <summary>
        /// Meshes the face of a shell as Mesh does when its grid passes the gate of the other structured faces: no quad
        /// folds in the parameters of the surface or in space, and no quad corner in space, against the normal of the
        /// surface, lies below SubmapFaceDetector.MinScaledJacobian. False, with the reason, when the face is left as
        /// gmsh meshed it.
        /// </summary>
        public static bool Mesh(TransfiniteFace face, bool quads, out string reason)
        {
            reason = null;
            int surfaceId = face.FaceId;
            List<IntPtr>[] sideTags;
            List<double[]>[] sideXyz;
            double[][][] sideUv;
            ThreeBlockGrid grid;
            try
            {
                grid = Map(face, out sideTags, out sideXyz, out sideUv);
            }
            catch (CaeException ex)
            {
                reason = GmshLensMesher.ToReason(ex.Message);
                return false;
            }
            if (grid.NumberOfFoldedQuads > 0)
            {
                reason = grid.NumberOfFoldedQuads + " of the " + grid.Quads.Length + " quads of its grid fold";
                return false;
            }
            // The inner nodes are keyed by negative tags, which no node of gmsh carries
            IntPtr[] nodeTags = new IntPtr[grid.NumberOfBoundaryNodes + grid.InnerUv.Length];
            Dictionary<IntPtr, double[]> tagXyz = new Dictionary<IntPtr, double[]>();
            Dictionary<IntPtr, double[]> tagUv = new Dictionary<IntPtr, double[]>();
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < sideTags[k].Count - 1; i++)
                {
                    int nodeId = grid.GetBoundaryNodeId(k, i);
                    nodeTags[nodeId] = sideTags[k][i];
                    tagXyz[nodeTags[nodeId]] = sideXyz[k][i];
                    tagUv[nodeTags[nodeId]] = sideUv[k][i];
                }
            }
            for (int n = 0; n < grid.InnerUv.Length; n++)
            {
                IntPtr tag = new IntPtr(-1 - n);
                nodeTags[grid.NumberOfBoundaryNodes + n] = tag;
                tagXyz[tag] = grid.InnerXyz[n];
                tagUv[tag] = grid.InnerUv[n];
            }
            IntPtr[] quadNodeTags = grid.Quads.SelectMany(quad => quad.Select(nodeId => nodeTags[nodeId])).ToArray();
            int numOfFoldedQuads;
            double minScaledJacobian = GmshSeamGridMesher.GetMinScaledJacobian(surfaceId, quadNodeTags, tagXyz, tagUv,
                                                                               null, out numOfFoldedQuads);
            if (numOfFoldedQuads > 0)
            {
                reason = numOfFoldedQuads + " of the " + grid.Quads.Length + " quads of its grid fold in space";
                return false;
            }
            double minAllowedScaledJacobian = GetMinAllowedScaledJacobian(sideXyz);
            if (minScaledJacobian < minAllowedScaledJacobian)
            {
                reason = "its grid has a quad corner of the scaled Jacobian " + Math.Round(minScaledJacobian, 3) +
                         ", below " + Math.Round(minAllowedScaledJacobian, 3);
                return false;
            }
            Replace(surfaceId, grid, sideTags, sideUv, quads);
            return true;
        }
        /// <summary>
        /// The floor of the other structured faces, lowered for a face with a sharp corner: the quad in that corner
        /// cannot be better than the sine of its angle, and a free mesh is no better there and keeps triangles, so the
        /// grid is asked for SharpCornerShare of what the corner allows.
        /// </summary>
        private static double GetMinAllowedScaledJacobian(List<double[]>[] sideXyz)
        {
            double minAllowedScaledJacobian = SubmapFaceDetector.MinScaledJacobian;
            for (int k = 0; k < 3; k++)
            {
                List<double[]> previousSide = sideXyz[(k + 2) % 3];
                double[] corner = sideXyz[k][0];
                double[] next = sideXyz[k][1];
                double[] previous = previousSide[previousSide.Count - 2];
                double dot = 0;
                double nextLength2 = 0;
                double previousLength2 = 0;
                for (int i = 0; i < 3; i++)
                {
                    dot += (next[i] - corner[i]) * (previous[i] - corner[i]);
                    nextLength2 += (next[i] - corner[i]) * (next[i] - corner[i]);
                    previousLength2 += (previous[i] - corner[i]) * (previous[i] - corner[i]);
                }
                if (dot <= 0 || nextLength2 == 0 || previousLength2 == 0) continue;
                double cosine = Math.Min(1, dot / Math.Sqrt(nextLength2 * previousLength2));
                minAllowedScaledJacobian = Math.Min(minAllowedScaledJacobian,
                                                    SharpCornerShare * Math.Sqrt(1 - cosine * cosine));
            }
            return minAllowedScaledJacobian;
        }
        /// <summary>
        /// The grid of the face on the nodes of its edges, and the tags, coordinates and parameters of the nodes of
        /// every side from its corner to the next one.
        /// </summary>
        private static ThreeBlockGrid Map(TransfiniteFace face, out List<IntPtr>[] sideTags,
                                          out List<double[]>[] sideXyz, out double[][][] sideUv)
        {
            int surfaceId = face.FaceId;
            sideTags = new List<IntPtr>[3];
            sideXyz = new List<double[]>[3];
            for (int k = 0; k < 3; k++)
                GetSideNodes(surfaceId, face.CornerNodeIds[k], face.CornerNodeIds[(k + 1) % 3], face.SideEdgeIds[k],
                             out sideTags[k], out sideXyz[k]);
            int[] sideCounts = sideTags.Select(tags => tags.Count - 1).ToArray();
            if (!TransfiniteSideTriple.AreCountsValid(sideCounts))
                throw new CaeException("The sides of Surface " + surfaceId + " carry " + string.Join(", ", sideCounts) +
                                       " elements, which three blocks of quads cannot fill.");
            sideUv = new double[3][][];
            for (int k = 0; k < 3; k++) sideUv[k] = GetParametricCoordinates(surfaceId, sideXyz[k]);
            //
            return ThreeBlockMapping.Map(sideUv, sideXyz.Select(xyz => xyz.ToArray()).ToArray(), uv =>
            {
                double[] coor;
                Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
                return coor;
            });
        }
        /// <summary>
        /// Replaces what gmsh meshed on the surface by the grid, and returns the gmsh tags of the nodes of the grid.
        /// </summary>
        private static IntPtr[] Replace(int surfaceId, ThreeBlockGrid grid, List<IntPtr>[] sideTags,
                                        double[][][] sideUv, bool quads)
        {
            int[] sideCounts = sideTags.Select(tags => tags.Count - 1).ToArray();
            IntPtr[] boundaryNodeTags = new IntPtr[grid.NumberOfBoundaryNodes];
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < sideCounts[k]; i++) boundaryNodeTags[grid.GetBoundaryNodeId(k, i)] = sideTags[k][i];
            }
            double[][] loopUv = Enumerable.Range(0, 3).SelectMany(k => sideUv[k].Take(sideCounts[k])).ToArray();
            return ReplaceSurfaceMesh(surfaceId, boundaryNodeTags, loopUv, grid.Quads, grid.InnerUv, grid.InnerXyz, quads);
        }
        /// <summary>
        /// Replaces what gmsh meshed on the surface by the quads of a grid whose boundary nodes, in loop order, are the
        /// nodes of the given tags and parameters, followed by its inner nodes, which are added. Without quads every quad
        /// is split into two triangles. Returns the gmsh tags of the nodes of the grid, by the node ids of the grid.
        /// </summary>
        internal static IntPtr[] ReplaceSurfaceMesh(int surfaceId, IntPtr[] boundaryNodeTags, double[][] loopUv,
                                                    int[][] gridQuads, double[][] innerUv, double[][] innerXyz, bool quads)
        {
            // The quads turn as the boundary loop does in the parameter space, and must turn the way gmsh turned its
            // own elements on the face
            bool reverse = Math.Sign(GetSignedArea(loopUv)) != GetGmshOrientation(surfaceId);
            return ReplaceSurfaceElements(surfaceId, boundaryNodeTags, gridQuads, innerUv, innerXyz, reverse, quads);
        }
        /// <summary>
        /// Replaces what gmsh meshed on the surface by quads and triangles whose nodes are the boundary nodes of the given
        /// tags followed by the inner nodes, which are added; reversed, every element is turned the other way round.
        /// Without quads every quad is split into two triangles. Returns the gmsh tags of the nodes, by their ids.
        /// </summary>
        internal static IntPtr[] ReplaceSurfaceElements(int surfaceId, IntPtr[] boundaryNodeTags, int[][] elements,
                                                        double[][] innerUv, double[][] innerXyz, bool reverse, bool quads)
        {
            // The tags of the boundary nodes and the new inner nodes
            int numOfBoundaryNodes = boundaryNodeTags.Length;
            IntPtr[] nodeTags = new IntPtr[numOfBoundaryNodes + innerUv.Length];
            for (int i = 0; i < numOfBoundaryNodes; i++) nodeTags[i] = boundaryNodeTags[i];
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            long nodeTag = maxTag.ToInt64();
            IntPtr[] newTags = new IntPtr[innerUv.Length];
            for (int i = 0; i < newTags.Length; i++)
            {
                newTags[i] = new IntPtr(++nodeTag);
                nodeTags[numOfBoundaryNodes + i] = newTags[i];
            }
            List<IntPtr> quadNodeTags = new List<IntPtr>();
            List<IntPtr> triangleNodeTags = new List<IntPtr>();
            foreach (var element in elements)
            {
                int n = element.Length;
                List<IntPtr> elementNodes = n == 4 ? quadNodeTags : triangleNodeTags;
                for (int c = 0; c < n; c++) elementNodes.Add(nodeTags[element[reverse ? n - 1 - c : c]]);
            }
            //
            Gmsh.Model.Mesh.Clear(new Tuple<int, int>[] { new Tuple<int, int>(2, surfaceId) });
            Gmsh.Model.Mesh.AddNodes(2, surfaceId, newTags, innerXyz.SelectMany(xyz => xyz).ToArray(),
                                     innerUv.SelectMany(uv => uv).ToArray());
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            long elementTag = maxTag.ToInt64();
            if (!quads)
            {
                int[] corners = new int[] { 0, 1, 2, 0, 2, 3 };
                List<IntPtr> splitNodeTags = new List<IntPtr>();
                for (int q = 0; q < quadNodeTags.Count / 4; q++)
                {
                    for (int c = 0; c < 6; c++) splitNodeTags.Add(quadNodeTags[4 * q + corners[c]]);
                }
                splitNodeTags.AddRange(triangleNodeTags);
                triangleNodeTags = splitNodeTags;
                quadNodeTags = new List<IntPtr>();
            }
            List<int> elementTypes = new List<int>();
            List<IntPtr[]> elementTagLists = new List<IntPtr[]>();
            List<IntPtr[]> elementNodeTagLists = new List<IntPtr[]>();
            if (quadNodeTags.Count > 0)
            {
                elementTypes.Add((int)GmshElementType.LinearQuadrilateral);
                elementNodeTagLists.Add(quadNodeTags.ToArray());
            }
            if (triangleNodeTags.Count > 0)
            {
                elementTypes.Add((int)GmshElementType.LinearTriangle);
                elementNodeTagLists.Add(triangleNodeTags.ToArray());
            }
            for (int t = 0; t < elementTypes.Count; t++)
            {
                int numOfNodes = elementTypes[t] == (int)GmshElementType.LinearQuadrilateral ? 4 : 3;
                IntPtr[] elementTags = new IntPtr[elementNodeTagLists[t].Length / numOfNodes];
                for (int i = 0; i < elementTags.Length; i++) elementTags[i] = new IntPtr(++elementTag);
                elementTagLists.Add(elementTags);
            }
            Gmsh.Model.Mesh.AddElements(2, surfaceId, elementTypes.ToArray(), elementTagLists.ToArray(),
                                        elementNodeTagLists.ToArray());
            return nodeTags;
        }
        /// <summary>
        /// The nodes of a side, from its first corner to its last, along the chain of its edges; a closed edge, of one
        /// vertex, runs along its parameter from that vertex back to it.
        /// </summary>
        internal static void GetSideNodes(int surfaceId, int firstCornerId, int lastCornerId, int[] edgeIds,
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
                if (vertexIds.Length == 1) vertexIds = new int[] { vertexIds[0], vertexIds[0] };
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
        internal static double[][] GetParametricCoordinates(int surfaceId, List<double[]> xyz)
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
