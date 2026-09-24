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


        public static QuadWildInputFileInfo Write(string objFileName, string sharpFileName, string nodesFileName,
                                                   FeMesh mesh, string partName, bool pinEdgeNodes = false)
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
            // A shell compound has edges shared by more than two triangles. The sheets are detached there (see
            // SplitSheets), so QuadWild meshes a manifold input whose seams carry vertex copies; the copies get the
            // same prescribed nodes and are merged again after meshing.
            int[] nodeIdsOrdered = usedNodeIds.OrderBy(id => id).ToArray();
            Dictionary<int, int> nodeIdObjIndex = new Dictionary<int, int>();
            for (int i = 0; i < nodeIdsOrdered.Length; i++) nodeIdObjIndex.Add(nodeIdsOrdered[i], i + 1);
            List<int> objIndexNodeId = new List<int>(nodeIdsOrdered);         // obj index - 1 -> node id
            HashSet<int> splitNodeIds;
            int[][] faceObjIndices = SplitSheets(mesh, faceNodeIds, faceEdges, GetFaceModelFaceIds(part, elementIds),
                                                 GetSegmentModelEdgeIds(modelEdges), nodeIdObjIndex, objIndexNodeId,
                                                 out splitNodeIds);
            // Edges of the split input: the model edges, including every copy of a seam, and all boundaries
            Dictionary<(int, int), List<FaceEdge>> splitFaceEdges = new Dictionary<(int, int), List<FaceEdge>>();
            foreach (var entry in faceEdges)
            {
                foreach (FaceEdge faceEdge in entry.Value)
                {
                    int[] objIndices = faceObjIndices[faceEdge.FaceIndex];
                    (int, int) key = GetEdgeKey(objIndices[faceEdge.LocalEdgeIndex],
                                                objIndices[(faceEdge.LocalEdgeIndex + 1) % 3]);
                    if (!splitFaceEdges.TryGetValue(key, out List<FaceEdge> references))
                    {
                        references = new List<FaceEdge>();
                        splitFaceEdges.Add(key, references);
                    }
                    references.Add(faceEdge);
                }
            }
            List<SharpEdge> sharpEdges = new List<SharpEdge>();
            foreach (var entry in splitFaceEdges)
            {
                FaceEdge reference = entry.Value.OrderBy(item => item.FaceIndex)
                                                .ThenBy(item => item.LocalEdgeIndex)
                                                .First();
                if (entry.Value.Count != 1 &&
                    !sharpEdgeKeys.Contains(GetEdgeKey(reference.StartNodeId, reference.EndNodeId))) continue;
                int type = GetSharpEdgeType(mesh, reference, entry.Value, faceNormals);
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
            StringBuilder obj = new StringBuilder();
            obj.AppendLine("# PrePoMax QuadWild triangular input");
            foreach (int nodeId in objIndexNodeId)
            {
                FeNode node = mesh.Nodes[nodeId];
                obj.Append("v ");
                obj.Append(node.X.ToString("R", CultureInfo.InvariantCulture)).Append(" ");
                obj.Append(node.Y.ToString("R", CultureInfo.InvariantCulture)).Append(" ");
                obj.AppendLine(node.Z.ToString("R", CultureInfo.InvariantCulture));
            }
            foreach (int[] objIndices in faceObjIndices)
            {
                obj.Append("f ");
                obj.Append(objIndices[0]).Append(" ");
                obj.Append(objIndices[1]).Append(" ");
                obj.AppendLine(objIndices[2].ToString(CultureInfo.InvariantCulture));
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
            // Model vertices, the end points of the open model edges, as single prescribed nodes. QuadWild only
            // keeps a node where the boundary turns sharply, so without them the nodes at the other model
            // vertices slide along the edges while smoothing. The coordinates are written exactly as in the .obj
            // file, so QuadWild finds them as vertices of its input.
            SortedSet<int> modelVertexNodeIds = new SortedSet<int>();
            foreach (QuadWildModelEdge modelEdge in modelEdges)
            {
                if (modelEdge.IsClosed) continue;
                modelVertexNodeIds.Add(modelEdge.NodeIds[0]);
                modelVertexNodeIds.Add(modelEdge.NodeIds[modelEdge.NodeIds.Length - 1]);
            }
            StringBuilder nodes = new StringBuilder();
            // Seams between detached sheets: every node of the model edge, so that each sheet gets the same nodes.
            // With pinEdgeNodes every model edge is written this way and keeps the nodes of the triangulation
            HashSet<(int, int)> seamKeys = new HashSet<(int, int)>(faceEdges.Where(entry => entry.Value.Count > 2)
                                                                            .Select(entry => entry.Key));
            int seamEdgeCount = 0;
            if (seamKeys.Count > 0 || pinEdgeNodes)
            {
                nodes.AppendLine(pinEdgeNodes ? "# PrePoMax model edges with pinned nodes, one polyline per model edge" :
                                                "# PrePoMax seams between sheets of a shell compound, one polyline per model edge");
                foreach (QuadWildModelEdge modelEdge in modelEdges)
                {
                    int[] ids = modelEdge.NodeIds;
                    List<int> polyline = new List<int>(ids);
                    if (modelEdge.IsClosed) polyline.Add(ids[0]);
                    bool isSeam = false;
                    for (int i = 0; i + 1 < polyline.Count && !isSeam; i++)
                        isSeam = seamKeys.Contains(GetEdgeKey(polyline[i], polyline[i + 1]));
                    if (isSeam) seamEdgeCount++;
                    else if (!pinEdgeNodes) continue;
                    nodes.Append(polyline.Count.ToString(CultureInfo.InvariantCulture));
                    foreach (int nodeId in polyline)
                    {
                        FeNode node = mesh.Nodes[nodeId];
                        nodes.Append(" ").Append(node.X.ToString("R", CultureInfo.InvariantCulture));
                        nodes.Append(" ").Append(node.Y.ToString("R", CultureInfo.InvariantCulture));
                        nodes.Append(" ").Append(node.Z.ToString("R", CultureInfo.InvariantCulture));
                    }
                    nodes.AppendLine();
                }
            }
            nodes.AppendLine("# PrePoMax model vertices, one prescribed node per line");
            foreach (int nodeId in modelVertexNodeIds)
            {
                FeNode node = mesh.Nodes[nodeId];
                nodes.Append("1 ");
                nodes.Append(node.X.ToString("R", CultureInfo.InvariantCulture)).Append(" ");
                nodes.Append(node.Y.ToString("R", CultureInfo.InvariantCulture)).Append(" ");
                nodes.AppendLine(node.Z.ToString("R", CultureInfo.InvariantCulture));
            }
            //
            UTF8Encoding utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(objFileName, obj.ToString(), utf8WithoutBom);
            File.WriteAllText(sharpFileName, sharp.ToString(), utf8WithoutBom);
            File.WriteAllText(nodesFileName, nodes.ToString(), utf8WithoutBom);
            //
            double[][] splitNodeCoordinates = splitNodeIds.OrderBy(id => id)
                                                          .Select(id => mesh.Nodes[id].Coor.ToArray()).ToArray();
            return new QuadWildInputFileInfo(nodeIdsOrdered.Length, faceNodeIds.Length, sharpEdges.Count,
                                             modelEdges, GetModelFaceEdgeIds(part.Visualization),
                                             seamEdgeCount, splitNodeCoordinates);
        }

        // Detaches the sheets of a shell compound at edges shared by more than two triangles. At such an edge the pair
        // of triangles that continues most smoothly stays joined as one sheet: their in-plane directions away from the
        // edge are the most opposite (at least 120 degrees apart) and they traverse the edge in opposite directions.
        // Every other triangle there is detached. The pairing of the model faces that most segments of a model edge
        // agree on is then used along the whole model edge, so that a curved seam is not joined differently from one
        // segment to the next. The vertices are split by the triangle fans around them over the edges that stay
        // joined: the first fan keeps the vertex, each further fan gets a copy appended to objIndexNodeId. Returns
        // the obj indices of the triangle corners.
        private static int[][] SplitSheets(FeMesh mesh, int[][] faceNodeIds,
                                           Dictionary<(int, int), List<FaceEdge>> faceEdges,
                                           int[] faceModelFaceIds, Dictionary<(int, int), int> segmentModelEdgeIds,
                                           Dictionary<int, int> nodeIdObjIndex, List<int> objIndexNodeId,
                                           out HashSet<int> splitNodeIds)
        {
            splitNodeIds = new HashSet<int>();
            int[][] faceObjIndices = new int[faceNodeIds.Length][];
            for (int faceIndex = 0; faceIndex < faceNodeIds.Length; faceIndex++)
                faceObjIndices[faceIndex] = faceNodeIds[faceIndex].Select(id => nodeIdObjIndex[id]).ToArray();
            // The joined neighbour of a triangle across one of its edges: (face, local edge) -> face
            Dictionary<(int, int), int> joined = new Dictionary<(int, int), int>();
            // The joined pairs of every seam segment, as indices into its references
            Dictionary<(int, int), List<(int, int)>> segmentPairs = new Dictionary<(int, int), List<(int, int)>>();
            foreach (var entry in faceEdges)
            {
                List<FaceEdge> references = entry.Value;
                if (references.Count == 2)
                {
                    joined[(references[0].FaceIndex, references[0].LocalEdgeIndex)] = references[1].FaceIndex;
                    joined[(references[1].FaceIndex, references[1].LocalEdgeIndex)] = references[0].FaceIndex;
                }
                else if (references.Count > 2)
                {
                    Vec3D[] inward = references.Select(item => GetInwardDirection(mesh, faceNodeIds[item.FaceIndex],
                                                                                  item.StartNodeId, item.EndNodeId))
                                               .ToArray();
                    List<(double, int, int)> candidates = new List<(double, int, int)>();
                    for (int i = 0; i < references.Count; i++)
                    {
                        for (int j = i + 1; j < references.Count; j++)
                        {
                            if (references[i].StartNodeId == references[j].StartNodeId) continue;
                            candidates.Add((Vec3D.DotProduct(inward[i], inward[j]), i, j));
                        }
                    }
                    HashSet<int> used = new HashSet<int>();
                    List<(int, int)> pairs = new List<(int, int)>();
                    foreach (var candidate in candidates.OrderBy(item => item.Item1))
                    {
                        if (candidate.Item1 > -0.5 || used.Contains(candidate.Item2) || used.Contains(candidate.Item3))
                            continue;
                        pairs.Add((candidate.Item2, candidate.Item3));
                        used.Add(candidate.Item2);
                        used.Add(candidate.Item3);
                    }
                    segmentPairs.Add(entry.Key, pairs);
                }
            }
            if (segmentPairs.Count == 0) return faceObjIndices;
            MakeSeamPairingConsistent(faceEdges, segmentPairs, faceModelFaceIds, segmentModelEdgeIds);
            foreach (var entry in segmentPairs)
            {
                List<FaceEdge> references = faceEdges[entry.Key];
                foreach (var pair in entry.Value)
                {
                    FaceEdge first = references[pair.Item1];
                    FaceEdge second = references[pair.Item2];
                    joined[(first.FaceIndex, first.LocalEdgeIndex)] = second.FaceIndex;
                    joined[(second.FaceIndex, second.LocalEdgeIndex)] = first.FaceIndex;
                }
            }
            // Triangle fans around each vertex
            Dictionary<int, List<int>> nodeIdFaceIndices = new Dictionary<int, List<int>>();
            for (int faceIndex = 0; faceIndex < faceNodeIds.Length; faceIndex++)
            {
                foreach (int nodeId in faceNodeIds[faceIndex])
                {
                    if (!nodeIdFaceIndices.TryGetValue(nodeId, out List<int> faceIndices))
                    {
                        faceIndices = new List<int>();
                        nodeIdFaceIndices.Add(nodeId, faceIndices);
                    }
                    faceIndices.Add(faceIndex);
                }
            }
            foreach (var entry in nodeIdFaceIndices)
            {
                int nodeId = entry.Key;
                Dictionary<int, int> parent = entry.Value.ToDictionary(faceIndex => faceIndex, faceIndex => faceIndex);
                Func<int, int> find = null;
                find = x => parent[x] == x ? x : (parent[x] = find(parent[x]));
                foreach (int faceIndex in entry.Value)
                {
                    int[] nodeIds = faceNodeIds[faceIndex];
                    for (int localEdgeIndex = 0; localEdgeIndex < 3; localEdgeIndex++)
                    {
                        if (nodeIds[localEdgeIndex] != nodeId && nodeIds[(localEdgeIndex + 1) % 3] != nodeId) continue;
                        if (joined.TryGetValue((faceIndex, localEdgeIndex), out int other) && parent.ContainsKey(other))
                            parent[find(faceIndex)] = find(other);
                    }
                }
                List<int> roots = entry.Value.Select(faceIndex => find(faceIndex)).Distinct().ToList();
                if (roots.Count < 2) continue;
                splitNodeIds.Add(nodeId);
                for (int k = 1; k < roots.Count; k++)
                {
                    objIndexNodeId.Add(nodeId);
                    int copyIndex = objIndexNodeId.Count;
                    foreach (int faceIndex in entry.Value)
                    {
                        if (find(faceIndex) != roots[k]) continue;
                        int[] nodeIds = faceNodeIds[faceIndex];
                        for (int c = 0; c < 3; c++)
                            if (nodeIds[c] == nodeId) faceObjIndices[faceIndex][c] = copyIndex;
                    }
                }
            }
            return faceObjIndices;
        }

        // Along each model edge, the pairing of model faces that most of its segments chose is applied to every segment
        // where it fits: each pair needs two unused triangles of its model faces that traverse the segment in opposite
        // directions. A segment where it does not fit keeps its own pairing.
        private static void MakeSeamPairingConsistent(Dictionary<(int, int), List<FaceEdge>> faceEdges,
                                                      Dictionary<(int, int), List<(int, int)>> segmentPairs,
                                                      int[] faceModelFaceIds,
                                                      Dictionary<(int, int), int> segmentModelEdgeIds)
        {
            if (faceModelFaceIds == null) return;
            Dictionary<int, List<(int, int)>> modelEdgeSegments = new Dictionary<int, List<(int, int)>>();
            foreach (var key in segmentPairs.Keys)
            {
                if (!segmentModelEdgeIds.TryGetValue(key, out int modelEdgeId)) continue;
                if (!modelEdgeSegments.TryGetValue(modelEdgeId, out List<(int, int)> keys))
                {
                    keys = new List<(int, int)>();
                    modelEdgeSegments.Add(modelEdgeId, keys);
                }
                keys.Add(key);
            }
            foreach (var entry in modelEdgeSegments)
            {
                if (entry.Value.Count < 2) continue;
                // The pairing of a segment in model face ids: sorted "a-b" pairs
                Dictionary<(int, int), List<(int, int)>> segmentFacePairs = new Dictionary<(int, int), List<(int, int)>>();
                Dictionary<string, int> signatureCount = new Dictionary<string, int>();
                foreach (var key in entry.Value)
                {
                    List<FaceEdge> references = faceEdges[key];
                    List<(int, int)> facePairs = segmentPairs[key]
                        .Select(pair => (faceModelFaceIds[references[pair.Item1].FaceIndex],
                                         faceModelFaceIds[references[pair.Item2].FaceIndex]))
                        .Select(pair => pair.Item1 <= pair.Item2 ? pair : (pair.Item2, pair.Item1))
                        .OrderBy(pair => pair.Item1).ThenBy(pair => pair.Item2).ToList();
                    segmentFacePairs.Add(key, facePairs);
                    string signature = String.Join(";", facePairs.Select(pair => pair.Item1 + "-" + pair.Item2));
                    signatureCount.TryGetValue(signature, out int count);
                    signatureCount[signature] = count + 1;
                }
                if (signatureCount.Count < 2) continue;
                string mostCommon = signatureCount.OrderByDescending(item => item.Value).First().Key;
                List<(int, int)> chosen = segmentFacePairs.First(item =>
                    String.Join(";", item.Value.Select(pair => pair.Item1 + "-" + pair.Item2)) == mostCommon).Value;
                foreach (var key in entry.Value)
                {
                    List<FaceEdge> references = faceEdges[key];
                    HashSet<int> used = new HashSet<int>();
                    List<(int, int)> pairs = new List<(int, int)>();
                    foreach (var facePair in chosen)
                    {
                        int first = -1;
                        int second = -1;
                        for (int i = 0; i < references.Count && second < 0; i++)
                        {
                            if (used.Contains(i) || faceModelFaceIds[references[i].FaceIndex] != facePair.Item1) continue;
                            for (int j = 0; j < references.Count; j++)
                            {
                                if (j == i || used.Contains(j) ||
                                    faceModelFaceIds[references[j].FaceIndex] != facePair.Item2 ||
                                    references[i].StartNodeId == references[j].StartNodeId) continue;
                                first = i;
                                second = j;
                                break;
                            }
                        }
                        if (second < 0) break;
                        pairs.Add((first, second));
                        used.Add(first);
                        used.Add(second);
                    }
                    if (pairs.Count == chosen.Count) segmentPairs[key] = pairs;
                }
            }
        }

        // The model face of every triangle, from the visualization faces of the input part, or null
        private static int[] GetFaceModelFaceIds(BasePart part, int[] elementIds)
        {
            VisualizationData visualization = part.Visualization;
            if (visualization == null || visualization.CellIds == null || visualization.CellIdsByFace == null) return null;
            Dictionary<int, int> elementIdFaceId = new Dictionary<int, int>();
            for (int faceId = 0; faceId < visualization.CellIdsByFace.Length; faceId++)
            {
                foreach (int cellId in visualization.CellIdsByFace[faceId])
                    elementIdFaceId[visualization.CellIds[cellId]] = faceId;
            }
            int[] faceModelFaceIds = new int[elementIds.Length];
            for (int i = 0; i < elementIds.Length; i++)
            {
                if (!elementIdFaceId.TryGetValue(elementIds[i], out faceModelFaceIds[i])) return null;
            }
            return faceModelFaceIds;
        }

        // The model edge of every mesh segment of a model edge
        private static Dictionary<(int, int), int> GetSegmentModelEdgeIds(QuadWildModelEdge[] modelEdges)
        {
            Dictionary<(int, int), int> segmentModelEdgeIds = new Dictionary<(int, int), int>();
            foreach (QuadWildModelEdge modelEdge in modelEdges)
            {
                int[] ids = modelEdge.NodeIds;
                int count = modelEdge.IsClosed ? ids.Length : ids.Length - 1;
                for (int i = 0; i < count; i++)
                    segmentModelEdgeIds[GetEdgeKey(ids[i], ids[(i + 1) % ids.Length])] = modelEdge.Id;
            }
            return segmentModelEdgeIds;
        }

        private static Vec3D GetInwardDirection(FeMesh mesh, int[] nodeIds, int startNodeId, int endNodeId)
        {
            int otherNodeId = nodeIds.First(id => id != startNodeId && id != endNodeId);
            Vec3D start = new Vec3D(mesh.Nodes[startNodeId].Coor);
            Vec3D edge = new Vec3D(mesh.Nodes[endNodeId].Coor) - start;
            edge.Normalize();
            Vec3D toOther = new Vec3D(mesh.Nodes[otherNodeId].Coor) - start;
            Vec3D inward = toOther - edge * Vec3D.DotProduct(toOther, edge);
            inward.Normalize();
            return inward;
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
