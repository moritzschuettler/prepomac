// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FileInOut.Output
{
    static public class QuadWildInputWriter
    {
        // The largest angle by which a face normal may deviate from the mean normal for the surface to
        // count as planar. QuadWild computes its own cross field from principal curvature directions,
        // which are undefined on a flat surface and make its solver return a field full of NaN.
        private const double PlanarNormalTolerance = 1E-3;

        private class FaceEdge
        {
            public int FaceIndex;
            public int LocalEdgeIndex;
            public int StartNodeId;
            public int EndNodeId;
        }

        private class SharpEdge
        {
            public int Type;
            public int FaceIndex;
            public int LocalEdgeIndex;
        }


        public static QuadWildInputFileInfo Write(string objFileName, string sharpFileName, string rosyFileName,
                                                   FeMesh mesh, string partName)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (String.IsNullOrWhiteSpace(partName)) throw new ArgumentException("The part name is not defined.");
            if (!mesh.Parts.TryGetValue(partName, out BasePart part))
                throw new CaeException("The QuadWild input mesh part '" + partName + "' does not exist.");
            if (part.PartType != PartType.Shell)
                throw new CaeException("QuadWild input can only be written for a shell mesh part.");
            //
            int[] elementIds = part.Labels.OrderBy(id => id).ToArray();
            int[][] faceNodeIds = new int[elementIds.Length][];
            double[][] faceNormals = new double[elementIds.Length][];
            HashSet<int> usedNodeIds = new HashSet<int>();
            Dictionary<(int, int), List<FaceEdge>> faceEdges =
                new Dictionary<(int, int), List<FaceEdge>>();
            //
            for (int faceIndex = 0; faceIndex < elementIds.Length; faceIndex++)
            {
                if (!mesh.Elements.TryGetValue(elementIds[faceIndex], out FeElement element))
                    throw new CaeException("The QuadWild input contains a missing element.");
                if (!(element is LinearTriangleElement) || element.NodeIds == null || element.NodeIds.Length != 3)
                    throw new CaeException("QuadWild input must contain only first-order triangular elements.");
                //
                int[] nodeIds = element.NodeIds.ToArray();
                if (nodeIds.Distinct().Count() != 3)
                    throw new CaeException("The QuadWild input contains a triangle with repeated nodes.");
                foreach (int nodeId in nodeIds)
                {
                    if (!mesh.Nodes.ContainsKey(nodeId))
                        throw new CaeException("The QuadWild input contains a triangle with a missing node.");
                    usedNodeIds.Add(nodeId);
                }
                faceNodeIds[faceIndex] = nodeIds;
                faceNormals[faceIndex] = GetFaceNormal(mesh, nodeIds);
                //
                for (int localEdgeIndex = 0; localEdgeIndex < 3; localEdgeIndex++)
                {
                    int startNodeId = nodeIds[localEdgeIndex];
                    int endNodeId = nodeIds[(localEdgeIndex + 1) % 3];
                    (int, int) key = GetEdgeKey(startNodeId, endNodeId);
                    if (!faceEdges.TryGetValue(key, out List<FaceEdge> references))
                    {
                        references = new List<FaceEdge>();
                        faceEdges.Add(key, references);
                    }
                    references.Add(new FaceEdge()
                    {
                        FaceIndex = faceIndex,
                        LocalEdgeIndex = localEdgeIndex,
                        StartNodeId = startNodeId,
                        EndNodeId = endNodeId
                    });
                }
            }
            if (faceNodeIds.Length == 0)
                throw new CaeException("The QuadWild input mesh does not contain any triangles.");
            //
            foreach (var entry in faceEdges)
            {
                if (entry.Value.Count > 2)
                    throw new CaeException("QuadWild input must be manifold; an edge belongs to more than two triangles.");
                if (entry.Value.Count == 2)
                {
                    FaceEdge first = entry.Value[0];
                    FaceEdge second = entry.Value[1];
                    if (first.StartNodeId != second.EndNodeId || first.EndNodeId != second.StartNodeId)
                        throw new CaeException("The QuadWild input contains inconsistently oriented triangles.");
                }
            }
            //
            QuadWildModelEdge[] modelEdges;
            double modelDiagonal = GetModelDiagonal(mesh, usedNodeIds);
            HashSet<(int, int)> sharpEdgeKeys = GetModelEdgeKeys(mesh, part, faceEdges, modelDiagonal,
                                                                 out modelEdges);
            // QuadWild does not infer boundary edges when an external .sharp file is supplied.
            foreach (var entry in faceEdges)
            {
                if (entry.Value.Count == 1) sharpEdgeKeys.Add(entry.Key);
            }
            //
            List<SharpEdge> sharpEdges = new List<SharpEdge>();
            foreach ((int, int) key in sharpEdgeKeys)
            {
                List<FaceEdge> references = faceEdges[key];
                FaceEdge reference = references.OrderBy(item => item.FaceIndex)
                                               .ThenBy(item => item.LocalEdgeIndex)
                                               .First();
                int type = GetSharpEdgeType(mesh, reference, references, faceNormals);
                sharpEdges.Add(new SharpEdge()
                {
                    Type = type,
                    FaceIndex = reference.FaceIndex,
                    LocalEdgeIndex = reference.LocalEdgeIndex
                });
            }
            sharpEdges = sharpEdges.OrderBy(item => item.FaceIndex)
                                   .ThenBy(item => item.LocalEdgeIndex)
                                   .ToList();
            //
            int[] nodeIdsOrdered = usedNodeIds.OrderBy(id => id).ToArray();
            Dictionary<int, int> nodeIdObjIndex = new Dictionary<int, int>();
            for (int i = 0; i < nodeIdsOrdered.Length; i++) nodeIdObjIndex.Add(nodeIdsOrdered[i], i + 1);
            //
            StringBuilder obj = new StringBuilder();
            obj.AppendLine("# PrePoMax QuadWild triangular input");
            foreach (int nodeId in nodeIdsOrdered)
            {
                FeNode node = mesh.Nodes[nodeId];
                obj.Append("v ");
                obj.Append(node.X.ToString("R", CultureInfo.InvariantCulture)).Append(" ");
                obj.Append(node.Y.ToString("R", CultureInfo.InvariantCulture)).Append(" ");
                obj.AppendLine(node.Z.ToString("R", CultureInfo.InvariantCulture));
            }
            foreach (int[] nodeIds in faceNodeIds)
            {
                obj.Append("f ");
                obj.Append(nodeIdObjIndex[nodeIds[0]]).Append(" ");
                obj.Append(nodeIdObjIndex[nodeIds[1]]).Append(" ");
                obj.AppendLine(nodeIdObjIndex[nodeIds[2]].ToString(CultureInfo.InvariantCulture));
            }
            //
            StringBuilder sharp = new StringBuilder();
            sharp.AppendLine(sharpEdges.Count.ToString(CultureInfo.InvariantCulture));
            foreach (SharpEdge edge in sharpEdges)
            {
                sharp.Append(edge.Type.ToString(CultureInfo.InvariantCulture)).Append(",");
                sharp.Append(edge.FaceIndex.ToString(CultureInfo.InvariantCulture)).Append(",");
                sharp.AppendLine(edge.LocalEdgeIndex.ToString(CultureInfo.InvariantCulture));
            }
            //
            string rosy = GetPlanarCrossField(mesh, faceNodeIds, faceNormals, sharpEdges);
            //
            UTF8Encoding utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(objFileName, obj.ToString(), utf8WithoutBom);
            File.WriteAllText(sharpFileName, sharp.ToString(), utf8WithoutBom);
            if (rosy != null) File.WriteAllText(rosyFileName, rosy, utf8WithoutBom);
            //
            return new QuadWildInputFileInfo(nodeIdsOrdered.Length, faceNodeIds.Length, sharpEdges.Count,
                                             modelEdges, GetModelFaceEdgeIds(part.Visualization), rosy != null);
        }

        // A flat surface leaves QuadWild's curvature driven cross field solver without any constraint and it
        // returns NaN directions, after which no patch can be traced. The field is constant on a flat
        // surface, so it is supplied here instead and QuadWild skips its own field computation.
        private static string GetPlanarCrossField(FeMesh mesh, int[][] faceNodeIds, double[][] faceNormals,
                                                  List<SharpEdge> sharpEdges)
        {
            double[] normal = GetPlanarNormal(faceNormals);
            if (normal == null) return null;
            //
            double[] axis1 = GetOrthogonalAxis(normal);
            double[] axis2 = Cross(normal, axis1);
            double[] direction = GetDominantSharpEdgeDirection(mesh, faceNodeIds, sharpEdges, axis1, axis2);
            //
            StringBuilder rosy = new StringBuilder();
            rosy.AppendLine(faceNodeIds.Length.ToString(CultureInfo.InvariantCulture));
            rosy.AppendLine("4");
            string line = direction[0].ToString("R", CultureInfo.InvariantCulture) + " " +
                          direction[1].ToString("R", CultureInfo.InvariantCulture) + " " +
                          direction[2].ToString("R", CultureInfo.InvariantCulture);
            for (int faceIndex = 0; faceIndex < faceNodeIds.Length; faceIndex++) rosy.AppendLine(line);
            return rosy.ToString();
        }

        private static double[] GetPlanarNormal(double[][] faceNormals)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (double[] faceNormal in faceNormals)
            {
                x += faceNormal[0];
                y += faceNormal[1];
                z += faceNormal[2];
            }
            double length = Math.Sqrt(x * x + y * y + z * z);
            if (length < 1E-20) return null;
            double[] normal = new double[] { x / length, y / length, z / length };
            //
            double minimumDot = Math.Cos(PlanarNormalTolerance);
            foreach (double[] faceNormal in faceNormals)
            {
                if (Dot(faceNormal, normal) < minimumDot) return null;
            }
            return normal;
        }

        // The cross field is four way symmetric, so the sharp edge directions are averaged at four times
        // their in-plane angle. Edges that are 90 degrees apart then reinforce each other instead of
        // canceling out, and the result aligns the quads with the dominant feature direction.
        private static double[] GetDominantSharpEdgeDirection(FeMesh mesh, int[][] faceNodeIds,
                                                              List<SharpEdge> sharpEdges,
                                                              double[] axis1, double[] axis2)
        {
            double sumX = 0;
            double sumY = 0;
            foreach (SharpEdge sharpEdge in sharpEdges)
            {
                int[] nodeIds = faceNodeIds[sharpEdge.FaceIndex];
                FeNode start = mesh.Nodes[nodeIds[sharpEdge.LocalEdgeIndex]];
                FeNode end = mesh.Nodes[nodeIds[(sharpEdge.LocalEdgeIndex + 1) % 3]];
                double[] edge = new double[] { end.X - start.X, end.Y - start.Y, end.Z - start.Z };
                double component1 = Dot(edge, axis1);
                double component2 = Dot(edge, axis2);
                double length = Math.Sqrt(component1 * component1 + component2 * component2);
                if (length < 1E-20) continue;
                double angle = 4 * Math.Atan2(component2, component1);
                sumX += length * Math.Cos(angle);
                sumY += length * Math.Sin(angle);
            }
            if (Math.Sqrt(sumX * sumX + sumY * sumY) < 1E-12) return axis1;
            //
            double dominantAngle = Math.Atan2(sumY, sumX) / 4;
            double cos = Math.Cos(dominantAngle);
            double sin = Math.Sin(dominantAngle);
            return new double[]
            {
                axis1[0] * cos + axis2[0] * sin,
                axis1[1] * cos + axis2[1] * sin,
                axis1[2] * cos + axis2[2] * sin
            };
        }

        private static double[] GetOrthogonalAxis(double[] normal)
        {
            double[] candidate = Math.Abs(normal[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double[] axis = Cross(normal, candidate);
            double length = Math.Sqrt(Dot(axis, axis));
            return new double[] { axis[0] / length, axis[1] / length, axis[2] / length };
        }

        private static double[] Cross(double[] vector1, double[] vector2)
        {
            return new double[]
            {
                vector1[1] * vector2[2] - vector1[2] * vector2[1],
                vector1[2] * vector2[0] - vector1[0] * vector2[2],
                vector1[0] * vector2[1] - vector1[1] * vector2[0]
            };
        }

        private static double Dot(double[] vector1, double[] vector2)
        {
            return vector1[0] * vector2[0] + vector1[1] * vector2[1] + vector1[2] * vector2[2];
        }

        private static int[][] GetModelFaceEdgeIds(VisualizationData visualization)
        {
            if (visualization.FaceEdgeIds == null) return null;
            //
            int[] edgeIds;
            int[][] modelFaceEdgeIds = new int[visualization.FaceEdgeIds.Length][];
            for (int faceId = 0; faceId < modelFaceEdgeIds.Length; faceId++)
            {
                edgeIds = visualization.FaceEdgeIds[faceId];
                if (edgeIds == null || edgeIds.Length == 0) return null;
                //
                modelFaceEdgeIds[faceId] = edgeIds.ToArray();
                Array.Sort(modelFaceEdgeIds[faceId]);
            }
            return modelFaceEdgeIds;
        }

        private static HashSet<(int, int)> GetModelEdgeKeys(
            FeMesh mesh, BasePart part, Dictionary<(int, int), List<FaceEdge>> faceEdges,
            double modelDiagonal, out QuadWildModelEdge[] modelEdges)
        {
            VisualizationData visualization = part.Visualization;
            if (visualization == null || visualization.EdgeCells == null ||
                visualization.EdgeCellIdsByEdge == null)
                throw new CaeException("The QuadWild input mesh does not contain model-edge topology.");
            //
            HashSet<(int, int)> modelEdgeKeys = new HashSet<(int, int)>();
            modelEdges = new QuadWildModelEdge[visualization.EdgeCellIdsByEdge.Length];
            for (int edgeId = 0; edgeId < visualization.EdgeCellIdsByEdge.Length; edgeId++)
            {
                int[] edgeCellIds = visualization.EdgeCellIdsByEdge[edgeId];
                if (edgeCellIds == null || edgeCellIds.Length == 0)
                    throw new CaeException("A QuadWild model edge does not contain any mesh segments.");
                bool isOnFreeBoundary = true;
                List<int[]> edgeCells = new List<int[]>();
                foreach (int edgeCellId in edgeCellIds)
                {
                    if (edgeCellId < 0 || edgeCellId >= visualization.EdgeCells.Length)
                        throw new CaeException("The QuadWild input contains an invalid model-edge reference.");
                    int[] edgeCell = visualization.EdgeCells[edgeCellId];
                    if (edgeCell == null || edgeCell.Length < 2 || edgeCell[0] == edgeCell[1])
                        throw new CaeException("The QuadWild input contains an invalid model edge.");
                    (int, int) key = GetEdgeKey(edgeCell[0], edgeCell[1]);
                    if (!faceEdges.ContainsKey(key))
                        throw new CaeException("A QuadWild model edge does not match any triangle edge.");
                    if (faceEdges[key].Count != 1) isOnFreeBoundary = false;
                    modelEdgeKeys.Add(key);
                    edgeCells.Add(edgeCell);
                }
                // The ordered polyline and the open/closed state are resolved here, from node connectivity,
                // so that the recovery never has to rediscover them by comparing floating point coordinates.
                int[] nodeIds = GetOrderedModelEdgeNodeIds(edgeId, edgeCells, out bool isClosed);
                double[][] points = new double[nodeIds.Length][];
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    FeNode node = mesh.Nodes[nodeIds[i]];
                    points[i] = new double[] { node.X, node.Y, node.Z };
                }
                modelEdges[edgeId] = new QuadWildModelEdge(edgeId, points, nodeIds, isClosed, isOnFreeBoundary,
                                                           modelDiagonal);
            }
            return modelEdgeKeys;
        }

        private static int[] GetOrderedModelEdgeNodeIds(int edgeId, List<int[]> edgeCells, out bool isClosed)
        {
            Dictionary<int, List<int>> nodeIdEdgeCellIds = new Dictionary<int, List<int>>();
            for (int i = 0; i < edgeCells.Count; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int nodeId = edgeCells[i][j];
                    if (nodeIdEdgeCellIds.TryGetValue(nodeId, out List<int> cellIds)) cellIds.Add(i);
                    else nodeIdEdgeCellIds.Add(nodeId, new List<int>() { i });
                }
            }
            if (nodeIdEdgeCellIds.Values.Any(ids => ids.Count > 2))
                throw new CaeException("The QuadWild input model edge " + edgeId + " branches.");
            int[] endNodeIds = nodeIdEdgeCellIds.Where(entry => entry.Value.Count == 1)
                                                .Select(entry => entry.Key).OrderBy(id => id).ToArray();
            if (endNodeIds.Length != 0 && endNodeIds.Length != 2)
                throw new CaeException("The QuadWild input model edge " + edgeId + " is not a continuous path.");
            isClosed = endNodeIds.Length == 0;
            //
            int currentNodeId = isClosed ? nodeIdEdgeCellIds.Keys.Min() : endNodeIds[0];
            bool[] used = new bool[edgeCells.Count];
            List<int> orderedNodeIds = new List<int>(edgeCells.Count + 1) { currentNodeId };
            for (int i = 0; i < edgeCells.Count; i++)
            {
                int nextCellId = -1;
                int nextNodeId = Int32.MaxValue;
                foreach (int cellId in nodeIdEdgeCellIds[currentNodeId])
                {
                    if (used[cellId]) continue;
                    int[] candidate = edgeCells[cellId];
                    int otherNodeId = candidate[0] == currentNodeId ? candidate[1] : candidate[0];
                    if (otherNodeId < nextNodeId)
                    {
                        nextCellId = cellId;
                        nextNodeId = otherNodeId;
                    }
                }
                if (nextCellId == -1)
                    throw new CaeException("The QuadWild input model edge " + edgeId +
                                           " contains disconnected mesh segments.");
                used[nextCellId] = true;
                currentNodeId = nextNodeId;
                if (isClosed && i == edgeCells.Count - 1) break;
                orderedNodeIds.Add(currentNodeId);
            }
            if (orderedNodeIds.Count != (isClosed ? edgeCells.Count : edgeCells.Count + 1))
                throw new CaeException("The QuadWild input model edge " + edgeId +
                                       " contains disconnected mesh segments.");
            return orderedNodeIds.ToArray();
        }

        private static double GetModelDiagonal(FeMesh mesh, HashSet<int> usedNodeIds)
        {
            double minX = Double.MaxValue;
            double minY = Double.MaxValue;
            double minZ = Double.MaxValue;
            double maxX = -Double.MaxValue;
            double maxY = -Double.MaxValue;
            double maxZ = -Double.MaxValue;
            foreach (int nodeId in usedNodeIds)
            {
                FeNode node = mesh.Nodes[nodeId];
                minX = Math.Min(minX, node.X);
                minY = Math.Min(minY, node.Y);
                minZ = Math.Min(minZ, node.Z);
                maxX = Math.Max(maxX, node.X);
                maxY = Math.Max(maxY, node.Y);
                maxZ = Math.Max(maxZ, node.Z);
            }
            double dx = maxX - minX;
            double dy = maxY - minY;
            double dz = maxZ - minZ;
            double diagonal = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (diagonal <= 0)
                throw new CaeException("The QuadWild model-edge geometry is degenerate.");
            return diagonal;
        }

        private static int GetSharpEdgeType(FeMesh mesh, FaceEdge reference,
                                            List<FaceEdge> references, double[][] faceNormals)
        {
            // QuadWild uses 0 for concave and 1 for convex. Boundary edges are convex.
            if (references.Count == 1) return 1;
            //
            FaceEdge opposite = references[0] == reference ? references[1] : references[0];
            double[] normal0 = faceNormals[reference.FaceIndex];
            double[] normal1 = faceNormals[opposite.FaceIndex];
            double crossX = normal0[1] * normal1[2] - normal0[2] * normal1[1];
            double crossY = normal0[2] * normal1[0] - normal0[0] * normal1[2];
            double crossZ = normal0[0] * normal1[1] - normal0[1] * normal1[0];
            FeNode start = mesh.Nodes[reference.StartNodeId];
            FeNode end = mesh.Nodes[reference.EndNodeId];
            double dot = crossX * (end.X - start.X) +
                         crossY * (end.Y - start.Y) +
                         crossZ * (end.Z - start.Z);
            return dot < 0 ? 0 : 1;
        }

        private static double[] GetFaceNormal(FeMesh mesh, int[] nodeIds)
        {
            FeNode node0 = mesh.Nodes[nodeIds[0]];
            FeNode node1 = mesh.Nodes[nodeIds[1]];
            FeNode node2 = mesh.Nodes[nodeIds[2]];
            double x1 = node1.X - node0.X;
            double y1 = node1.Y - node0.Y;
            double z1 = node1.Z - node0.Z;
            double x2 = node2.X - node0.X;
            double y2 = node2.Y - node0.Y;
            double z2 = node2.Z - node0.Z;
            double nx = y1 * z2 - z1 * y2;
            double ny = z1 * x2 - x1 * z2;
            double nz = x1 * y2 - y1 * x2;
            double length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (length < 1E-20)
                throw new CaeException("The QuadWild input contains a degenerate triangle.");
            return new double[] { nx / length, ny / length, nz / length };
        }

        private static (int, int) GetEdgeKey(int nodeId1, int nodeId2)
        {
            if (nodeId1 < nodeId2) return (nodeId1, nodeId2);
            else return (nodeId2, nodeId1);
        }
    }
}
