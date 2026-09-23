// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CaeMesh
{
    public static class QuadWildEdgeRecovery
    {
        // Variables
        private const double DeviationCostFactor = 4;
        private const double NonFeatureCostFactor = 3;
        private const int MaxReportedFailures = 5;
        private const int FreeLoopSampleCount = 32;
        //
        private class CandidateEdge
        {
            public int Index;
            public int[] EdgeCell;
            public int UseCount;
            public HashSet<string> MaterialNames;
            public bool AngleDetected;
            public double Length;
            public double[] Box;
            public int OwnerEdgeId;
            public bool IsFeature
            {
                // An edge shared by more than two faces is a seam of a shell compound
                get { return UseCount != 2 || MaterialNames.Count > 1 || AngleDetected; }
            }
            public CandidateEdge(int index, int[] edgeCell)
            {
                Index = index;
                EdgeCell = edgeCell;
                MaterialNames = new HashSet<string>(StringComparer.Ordinal);
                OwnerEdgeId = -1;
            }
        }
        //
        private class FreeInputLoop
        {
            public List<QuadWildModelEdge> ModelEdges;
            // The vertex where the model edge with the same index starts, in the walked direction
            public List<int> VertexNodeIds;
            public FreeInputLoop(List<QuadWildModelEdge> modelEdges, List<int> vertexNodeIds)
            {
                ModelEdges = modelEdges;
                VertexNodeIds = vertexNodeIds;
            }
        }
        //
        private class FreeOutputLoop
        {
            public List<CandidateEdge> Candidates;
            // The mesh node where the candidate with the same index starts, in the walked direction
            public List<int> NodeIds;
            public FreeOutputLoop()
            {
                Candidates = new List<CandidateEdge>();
                NodeIds = new List<int>();
            }
        }
        //
        private class TraceContext
        {
            public FeMesh Mesh;
            public List<CandidateEdge> Candidates;
            public QuadWildEdgeGrid Grid;
            public Dictionary<int, List<int>> NodeIdCandidateIndices;
            public int[] SeenIds;
            public int[] TubeIds;
            public int[] PathIds;
            public double[] Costs;
            public int Stamp;
            public double MeshEdgeLength;
            public Dictionary<int, int> AnchorNodeIds;
            public List<int> QueryBuffer = new List<int>();
        }


        // Methods
        public static QuadWildEdgeRecoveryInfo Restore(FeMesh mesh, string partName,
                                                       QuadWildModelEdge[] modelEdges, int[][] modelFaceEdgeIds,
                                                       Dictionary<int, string> elementIdMaterialName)
        {
            if (mesh == null) return Failed("The generated mesh is missing.");
            if (modelEdges == null) return Failed("The model-edge recovery data is missing.");
            if (!mesh.Parts.TryGetValue(partName, out BasePart part))
                return Failed("The output mesh part '" + partName + "' does not exist.");
            //
            if (modelEdges.Length == 0)
            {
                string emptyError = TrySetRecoveredTopology(mesh, part, new Dictionary<int, List<int[]>>());
                if (emptyError != null) return Failed("The zero-edge topology could not be restored. " + emptyError);
                TryDetectGeometryTypes(mesh, part);
                return new QuadWildEdgeRecoveryInfo(true, 0, 0, false, null);
            }
            //
            try
            {
                // QuadWild writes one material per traced patch. Material boundaries are the strongest
                // feature signal, but are optional because some valid outputs omit or incompletely write
                // material blocks.
                bool completeMaterialData = elementIdMaterialName != null && part.Labels.All(elementId =>
                    elementIdMaterialName.TryGetValue(elementId, out string name) &&
                    !String.IsNullOrWhiteSpace(name));
                if (!completeMaterialData) elementIdMaterialName = null;
                //
                TraceContext context = BuildTraceContext(mesh, part, elementIdMaterialName);
                context.AnchorNodeIds = ResolveAnchorNodeIds(context, modelEdges);
                //
                Dictionary<int, List<int[]>> edgeIdEdgeCells = new Dictionary<int, List<int[]>>();
                List<string> failures = new List<string>();
                List<int> relaxedEdgeIds = new List<int>();
                // The model edges are traced in ascending id order so that the result depends only on the
                // input data and never on a hash table enumeration order. Regenerate replays meshing, and a
                // reordered result would silently repoint every saved geometry selection.
                QuadWildModelEdge[] orderedModelEdges = modelEdges.OrderBy(item => item.Id).ToArray();
                // The free boundary is recovered from the mesh topology before anything is traced. A hole
                // boundary moves too far through the meshing stages for a geometric trace to follow it, and
                // the traced edges must not consume its mesh edges.
                int freeEdgeCount;
                string freeReason = RecoverFreeBoundaryEdges(context, orderedModelEdges, edgeIdEdgeCells,
                                                             out freeEdgeCount);
                // Every edge is first traced on the strict tolerance. Only then is the relaxed tolerance
                // offered to what is left, so that a relaxed trace can never consume the mesh edges another
                // model edge would have matched exactly.
                Dictionary<int, string> strictFailureReasons = new Dictionary<int, string>();
                foreach (QuadWildModelEdge modelEdge in orderedModelEdges)
                {
                    if (edgeIdEdgeCells.ContainsKey(modelEdge.Id)) continue;
                    List<CandidateEdge> path = TraceModelEdge(context, modelEdge, false, out string reason);
                    if (path == null) strictFailureReasons.Add(modelEdge.Id, reason);
                    else Commit(modelEdge, path, edgeIdEdgeCells);
                }
                foreach (QuadWildModelEdge modelEdge in orderedModelEdges)
                {
                    if (!strictFailureReasons.ContainsKey(modelEdge.Id)) continue;
                    List<CandidateEdge> path = TraceModelEdge(context, modelEdge, true, out string reason);
                    if (path == null)
                    {
                        failures.Add(Describe(modelEdge, strictFailureReasons[modelEdge.Id], reason));
                        continue;
                    }
                    Commit(modelEdge, path, edgeIdEdgeCells);
                    relaxedEdgeIds.Add(modelEdge.Id);
                }
                // Recovery is deliberately all or nothing. SetPartVisualizationModelEdges rebuilds the model
                // edges as a contiguous zero based sequence, so a partially recovered set would shift the ids
                // the geometry selections refer to. A failed recovery keeps the angle detected topology.
                if (failures.Count > 0)
                {
                    int reportedCount = Math.Min(failures.Count, MaxReportedFailures);
                    string report = String.Join(" ", failures.Take(reportedCount));
                    if (failures.Count > reportedCount)
                        report += " And " + (failures.Count - reportedCount) + " more.";
                    return Failed("Not all input model edges could be matched. " + report);
                }
                //
                string topologyError = TrySetRecoveredTopology(mesh, part, edgeIdEdgeCells);
                if (topologyError != null)
                    return Failed("The matched edge paths are not a valid model-edge topology. " + topologyError);
                int faceCount = RestoreFaceOrder(part, modelFaceEdgeIds, out string faceWarning);
                TryDetectGeometryTypes(mesh, part);
                //
                bool usedFallback = !completeMaterialData || relaxedEdgeIds.Count > 0 || freeReason != null;
                string warning = usedFallback
                               ? GetFallbackWarning(completeMaterialData, relaxedEdgeIds, freeReason) : null;
                return new QuadWildEdgeRecoveryInfo(true, edgeIdEdgeCells.Count,
                                                    edgeIdEdgeCells.Values.Sum(cells => cells.Count),
                                                    freeEdgeCount, faceCount, usedFallback, warning, faceWarning);
            }
            catch (Exception ex)
            {
                return Failed("The recovery algorithm could not complete. " + ex.Message);
            }
        }
        private static void Commit(QuadWildModelEdge modelEdge, List<CandidateEdge> path,
                                   Dictionary<int, List<int[]>> edgeIdEdgeCells)
        {
            foreach (CandidateEdge candidate in path) candidate.OwnerEdgeId = modelEdge.Id;
            edgeIdEdgeCells.Add(modelEdge.Id, path.Select(item => item.EdgeCell).ToList());
        }
        private static QuadWildEdgeRecoveryInfo Failed(string warning)
        {
            return new QuadWildEdgeRecoveryInfo(false, 0, 0, false, warning);
        }
        private static string GetFallbackWarning(bool completeMaterialData, List<int> relaxedEdgeIds,
                                                 string freeReason)
        {
            List<string> parts = new List<string>();
            if (!completeMaterialData) parts.Add("the QuadWild patch materials were unusable");
            if (freeReason != null)
                parts.Add("the free boundary edges were traced geometrically because " + freeReason);
            if (relaxedEdgeIds.Count > 0)
                parts.Add("edges " + String.Join(", ", relaxedEdgeIds) + " required the relaxed tolerance");
            return String.Join("; ", parts) + ".";
        }
        private static string Describe(QuadWildModelEdge modelEdge, string strictReason, string relaxedReason)
        {
            double[] midPoint = modelEdge.GetMidPoint();
            return "Edge " + modelEdge.Id + " near (" + Format(midPoint[0]) + ", " + Format(midPoint[1]) +
                   ", " + Format(midPoint[2]) + "): " + (relaxedReason ?? strictReason) + ".";
        }
        private static string Format(double value)
        {
            return value.ToString("G4", CultureInfo.InvariantCulture);
        }
        private static TraceContext BuildTraceContext(FeMesh mesh, BasePart part,
                                                      Dictionary<int, string> elementIdMaterialName)
        {
            VisualizationData visualization = part.Visualization;
            if (visualization == null || visualization.Cells == null || visualization.CellIds == null ||
                visualization.Cells.Length != visualization.CellIds.Length)
                throw new CaeException("The QuadWild output does not contain valid visualization cells.");
            //
            List<CandidateEdge> candidates = new List<CandidateEdge>();
            Dictionary<(int, int), CandidateEdge> keyCandidates = new Dictionary<(int, int), CandidateEdge>();
            for (int cellId = 0; cellId < visualization.Cells.Length; cellId++)
            {
                string materialName = null;
                if (elementIdMaterialName != null)
                    elementIdMaterialName.TryGetValue(visualization.CellIds[cellId], out materialName);
                int[][] edgeCells = FeMesh.GetVisualizationEdgeCells(visualization.Cells[cellId],
                                                                      ElementFaceType.Face);
                foreach (int[] edgeCell in edgeCells)
                {
                    if (edgeCell[0] == edgeCell[1]) continue;
                    (int, int) key = GetEdgeKey(edgeCell[0], edgeCell[1]);
                    if (!keyCandidates.TryGetValue(key, out CandidateEdge candidate))
                    {
                        candidate = new CandidateEdge(candidates.Count, edgeCell.ToArray());
                        keyCandidates.Add(key, candidate);
                        candidates.Add(candidate);
                    }
                    candidate.UseCount++;
                    if (!String.IsNullOrWhiteSpace(materialName)) candidate.MaterialNames.Add(materialName);
                }
            }
            // Angle detected creases from the imported mesh remain a feature signal next to the patch
            // materials and the free boundary edges.
            if (visualization.EdgeCells != null)
            {
                foreach (int[] edgeCell in visualization.EdgeCells)
                {
                    if (edgeCell == null || edgeCell.Length < 2) continue;
                    if (keyCandidates.TryGetValue(GetEdgeKey(edgeCell[0], edgeCell[1]),
                                                  out CandidateEdge candidate))
                        candidate.AngleDetected = true;
                }
            }
            //
            List<double[]> boxes = new List<double[]>(candidates.Count);
            Dictionary<int, List<int>> nodeIdCandidateIndices = new Dictionary<int, List<int>>();
            foreach (CandidateEdge candidate in candidates)
            {
                FeNode node0 = mesh.Nodes[candidate.EdgeCell[0]];
                FeNode node1 = mesh.Nodes[candidate.EdgeCell[1]];
                double dx = node1.X - node0.X;
                double dy = node1.Y - node0.Y;
                double dz = node1.Z - node0.Z;
                candidate.Length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                candidate.Box = new double[] { Math.Min(node0.X, node1.X), Math.Min(node0.Y, node1.Y),
                                               Math.Min(node0.Z, node1.Z), Math.Max(node0.X, node1.X),
                                               Math.Max(node0.Y, node1.Y), Math.Max(node0.Z, node1.Z) };
                if (candidate.EdgeCell.Length == 3)
                {
                    FeNode midNode = mesh.Nodes[candidate.EdgeCell[2]];
                    candidate.Box[0] = Math.Min(candidate.Box[0], midNode.X);
                    candidate.Box[1] = Math.Min(candidate.Box[1], midNode.Y);
                    candidate.Box[2] = Math.Min(candidate.Box[2], midNode.Z);
                    candidate.Box[3] = Math.Max(candidate.Box[3], midNode.X);
                    candidate.Box[4] = Math.Max(candidate.Box[4], midNode.Y);
                    candidate.Box[5] = Math.Max(candidate.Box[5], midNode.Z);
                }
                boxes.Add(candidate.Box);
                for (int i = 0; i < 2; i++)
                {
                    if (nodeIdCandidateIndices.TryGetValue(candidate.EdgeCell[i], out List<int> indices))
                        indices.Add(candidate.Index);
                    else nodeIdCandidateIndices.Add(candidate.EdgeCell[i], new List<int>() { candidate.Index });
                }
            }
            //
            List<double> candidateLengths = new List<double>(candidates.Count);
            foreach (CandidateEdge candidate in candidates)
            {
                if (candidate.Length > 0) candidateLengths.Add(candidate.Length);
            }
            if (candidateLengths.Count == 0)
                throw new CaeException("The QuadWild output does not contain any mesh edges.");
            candidateLengths.Sort();
            //
            return new TraceContext()
            {
                Mesh = mesh,
                Candidates = candidates,
                Grid = new QuadWildEdgeGrid(boxes),
                NodeIdCandidateIndices = nodeIdCandidateIndices,
                SeenIds = new int[candidates.Count],
                TubeIds = new int[candidates.Count],
                PathIds = new int[candidates.Count],
                Costs = new double[candidates.Count],
                Stamp = 0,
                MeshEdgeLength = candidateLengths[candidateLengths.Count / 2]
            };
        }
        // A source model vertex is resolved to one output node once, for every model edge that meets there.
        // Resolving it per model edge lets the two halves of a split hole snap to different output nodes and
        // leaves the mesh edges between them owned by neither half.
        private static Dictionary<int, int> ResolveAnchorNodeIds(TraceContext context,
                                                                 QuadWildModelEdge[] modelEdges)
        {
            Dictionary<int, double[]> vertexPoints = new Dictionary<int, double[]>();
            foreach (QuadWildModelEdge modelEdge in modelEdges)
            {
                vertexPoints[modelEdge.StartNodeId] = modelEdge.StartPoint;
                if (!modelEdge.IsClosed) vertexPoints[modelEdge.EndNodeId] = modelEdge.EndPoint;
            }
            //
            double radius = QuadWildModelEdge.GetAnchorSearchRadius(context.MeshEdgeLength,
                                                                    modelEdges[0].ModelDiagonal);
            double radiusSquared = radius * radius;
            Dictionary<int, int> anchorNodeIds = new Dictionary<int, int>();
            double[] box = new double[6];
            foreach (int vertexNodeId in vertexPoints.Keys.OrderBy(id => id))
            {
                double[] point = vertexPoints[vertexNodeId];
                for (int i = 0; i < 3; i++)
                {
                    box[i] = point[i];
                    box[i + 3] = point[i];
                }
                context.Grid.Query(box, radius, context.QueryBuffer);
                int anchorNodeId = -1;
                double closestDistanceSquared = Double.MaxValue;
                foreach (int index in context.QueryBuffer)
                {
                    int[] edgeCell = context.Candidates[index].EdgeCell;
                    for (int i = 0; i < 2; i++)
                    {
                        FeNode node = context.Mesh.Nodes[edgeCell[i]];
                        double dx = node.X - point[0];
                        double dy = node.Y - point[1];
                        double dz = node.Z - point[2];
                        double distanceSquared = dx * dx + dy * dy + dz * dz;
                        if (distanceSquared < closestDistanceSquared ||
                            (distanceSquared == closestDistanceSquared && edgeCell[i] < anchorNodeId))
                        {
                            closestDistanceSquared = distanceSquared;
                            anchorNodeId = edgeCell[i];
                        }
                    }
                }
                if (anchorNodeId != -1 && closestDistanceSquared <= radiusSquared)
                    anchorNodeIds.Add(vertexNodeId, anchorNodeId);
            }
            return anchorNodeIds;
        }
        // Free boundary recovery
        // A free model edge cannot be traced reliably. The QuadWild stages move the boundary nodes of a hole
        // far enough that no tube around the source polyline holds them. The output free boundary is however
        // known exactly from the mesh topology - a mesh edge used by one cell - so the recovery only has to
        // split the output boundary loops the way the source loops are split.
        private static string RecoverFreeBoundaryEdges(TraceContext context, QuadWildModelEdge[] modelEdges,
                                                       Dictionary<int, List<int[]>> edgeIdEdgeCells,
                                                       out int recoveredEdgeCount)
        {
            recoveredEdgeCount = 0;
            List<QuadWildModelEdge> freeModelEdges = new List<QuadWildModelEdge>();
            foreach (QuadWildModelEdge modelEdge in modelEdges)
            {
                if (modelEdge.IsOnFreeBoundary) freeModelEdges.Add(modelEdge);
            }
            if (freeModelEdges.Count == 0) return null;
            //
            string reason;
            List<FreeInputLoop> inputLoops = GetFreeInputLoops(freeModelEdges, out reason);
            if (inputLoops == null) return reason;
            List<FreeOutputLoop> outputLoops = GetFreeOutputLoops(context, out reason);
            if (outputLoops == null) return reason;
            if (inputLoops.Count != outputLoops.Count)
                return "the input geometry has " + inputLoops.Count + " free boundary loop/s and the output " +
                       "mesh " + outputLoops.Count;
            //
            int[] inputLoopIds = PairFreeLoops(context, inputLoops, outputLoops);
            if (inputLoopIds == null)
                return "the free boundary loops of the input geometry and of the output mesh could not be paired";
            //
            List<KeyValuePair<QuadWildModelEdge, List<CandidateEdge>>> edgePaths =
                new List<KeyValuePair<QuadWildModelEdge, List<CandidateEdge>>>();
            for (int i = 0; i < outputLoops.Count; i++)
            {
                reason = SplitFreeOutputLoop(context, inputLoops[inputLoopIds[i]], outputLoops[i], edgePaths);
                if (reason != null) return reason;
            }
            // Committed only after every loop is split, so that a failure leaves the whole free boundary to
            // the geometric tracing instead of half of it
            foreach (var entry in edgePaths.OrderBy(item => item.Key.Id))
            {
                Commit(entry.Key, entry.Value, edgeIdEdgeCells);
            }
            recoveredEdgeCount = edgePaths.Count;
            return null;
        }
        private static List<FreeInputLoop> GetFreeInputLoops(List<QuadWildModelEdge> freeModelEdges,
                                                             out string reason)
        {
            reason = null;
            List<FreeInputLoop> loops = new List<FreeInputLoop>();
            List<QuadWildModelEdge> openModelEdges = new List<QuadWildModelEdge>();
            Dictionary<int, List<QuadWildModelEdge>> vertexModelEdges =
                new Dictionary<int, List<QuadWildModelEdge>>();
            foreach (QuadWildModelEdge modelEdge in freeModelEdges)
            {
                if (modelEdge.IsClosed)
                {
                    loops.Add(new FreeInputLoop(new List<QuadWildModelEdge>() { modelEdge },
                                                new List<int>() { modelEdge.StartNodeId }));
                    continue;
                }
                openModelEdges.Add(modelEdge);
                for (int i = 0; i < 2; i++)
                {
                    int vertexNodeId = i == 0 ? modelEdge.StartNodeId : modelEdge.EndNodeId;
                    if (vertexModelEdges.TryGetValue(vertexNodeId, out List<QuadWildModelEdge> vertexEdges))
                        vertexEdges.Add(modelEdge);
                    else vertexModelEdges.Add(vertexNodeId, new List<QuadWildModelEdge>() { modelEdge });
                }
            }
            foreach (var entry in vertexModelEdges)
            {
                if (entry.Value.Count != 2)
                {
                    reason = "a free boundary vertex of the input geometry joins " + entry.Value.Count +
                             " free model edge/s";
                    return null;
                }
            }
            //
            HashSet<int> walkedEdgeIds = new HashSet<int>();
            foreach (QuadWildModelEdge seed in openModelEdges)
            {
                if (walkedEdgeIds.Contains(seed.Id)) continue;
                List<int> loopVertexNodeIds = new List<int>();
                List<QuadWildModelEdge> loopModelEdges = new List<QuadWildModelEdge>();
                QuadWildModelEdge current = seed;
                int vertexNodeId = seed.StartNodeId;
                while (true)
                {
                    loopModelEdges.Add(current);
                    loopVertexNodeIds.Add(vertexNodeId);
                    walkedEdgeIds.Add(current.Id);
                    int nextVertexNodeId = current.StartNodeId == vertexNodeId ? current.EndNodeId
                                                                               : current.StartNodeId;
                    List<QuadWildModelEdge> vertexEdges = vertexModelEdges[nextVertexNodeId];
                    QuadWildModelEdge next = vertexEdges[0].Id == current.Id ? vertexEdges[1] : vertexEdges[0];
                    if (next.Id == seed.Id) break;
                    if (walkedEdgeIds.Contains(next.Id))
                    {
                        reason = "the free model edges of the input geometry do not form closed loops";
                        return null;
                    }
                    current = next;
                    vertexNodeId = nextVertexNodeId;
                }
                loops.Add(new FreeInputLoop(loopModelEdges, loopVertexNodeIds));
            }
            return loops;
        }
        private static List<FreeOutputLoop> GetFreeOutputLoops(TraceContext context, out string reason)
        {
            reason = null;
            List<CandidateEdge> freeCandidates = new List<CandidateEdge>();
            Dictionary<int, List<CandidateEdge>> nodeCandidates = new Dictionary<int, List<CandidateEdge>>();
            foreach (CandidateEdge candidate in context.Candidates)
            {
                if (candidate.UseCount != 1) continue;
                freeCandidates.Add(candidate);
                for (int i = 0; i < 2; i++)
                {
                    if (nodeCandidates.TryGetValue(candidate.EdgeCell[i], out List<CandidateEdge> nodeEdges))
                        nodeEdges.Add(candidate);
                    else nodeCandidates.Add(candidate.EdgeCell[i], new List<CandidateEdge>() { candidate });
                }
            }
            if (freeCandidates.Count == 0)
            {
                reason = "the output mesh does not contain any free mesh edges";
                return null;
            }
            foreach (var entry in nodeCandidates)
            {
                if (entry.Value.Count != 2)
                {
                    reason = "mesh node " + entry.Key + " joins " + entry.Value.Count + " free mesh edge/s";
                    return null;
                }
            }
            //
            HashSet<int> walkedIndices = new HashSet<int>();
            List<FreeOutputLoop> loops = new List<FreeOutputLoop>();
            foreach (CandidateEdge seed in freeCandidates)
            {
                if (walkedIndices.Contains(seed.Index)) continue;
                FreeOutputLoop loop = new FreeOutputLoop();
                CandidateEdge current = seed;
                int nodeId = seed.EdgeCell[0];
                while (true)
                {
                    loop.Candidates.Add(current);
                    loop.NodeIds.Add(nodeId);
                    walkedIndices.Add(current.Index);
                    int nextNodeId = current.EdgeCell[0] == nodeId ? current.EdgeCell[1] : current.EdgeCell[0];
                    List<CandidateEdge> nodeEdges = nodeCandidates[nextNodeId];
                    CandidateEdge next = nodeEdges[0].Index == current.Index ? nodeEdges[1] : nodeEdges[0];
                    if (next.Index == seed.Index) break;
                    if (walkedIndices.Contains(next.Index))
                    {
                        reason = "the free mesh edges of the output mesh do not form closed loops";
                        return null;
                    }
                    current = next;
                    nodeId = nextNodeId;
                }
                loops.Add(loop);
            }
            return loops;
        }
        // Every output loop takes the input loop it lies closest to, and the result has to be a bijection.
        // Two loops that are equally close to one source hole are therefore reported instead of guessed.
        private static int[] PairFreeLoops(TraceContext context, List<FreeInputLoop> inputLoops,
                                           List<FreeOutputLoop> outputLoops)
        {
            int loopCount = outputLoops.Count;
            int[] inputLoopIds = new int[loopCount];
            bool[] paired = new bool[loopCount];
            for (int i = 0; i < loopCount; i++)
            {
                int bestId = -1;
                double bestDistance = Double.MaxValue;
                for (int j = 0; j < loopCount; j++)
                {
                    double distance = GetFreeLoopDistance(context, outputLoops[i], inputLoops[j]);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestId = j;
                    }
                }
                if (bestId == -1 || paired[bestId]) return null;
                inputLoopIds[i] = bestId;
                paired[bestId] = true;
            }
            return inputLoopIds;
        }
        private static double GetFreeLoopDistance(TraceContext context, FreeOutputLoop outputLoop,
                                                  FreeInputLoop inputLoop)
        {
            int nodeCount = outputLoop.NodeIds.Count;
            int sampleCount = Math.Min(nodeCount, FreeLoopSampleCount);
            double distance = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                FeNode node = context.Mesh.Nodes[outputLoop.NodeIds[i * nodeCount / sampleCount]];
                double closestDistanceSquared = Double.MaxValue;
                foreach (QuadWildModelEdge modelEdge in inputLoop.ModelEdges)
                {
                    closestDistanceSquared = Math.Min(closestDistanceSquared,
                                                      modelEdge.GetDistanceSquared(node.X, node.Y, node.Z));
                }
                distance += Math.Sqrt(closestDistanceSquared);
            }
            return distance / sampleCount;
        }
        private static string SplitFreeOutputLoop(TraceContext context, FreeInputLoop inputLoop,
                                                  FreeOutputLoop outputLoop,
                                                  List<KeyValuePair<QuadWildModelEdge,
                                                                    List<CandidateEdge>>> edgePaths)
        {
            int edgeCount = inputLoop.ModelEdges.Count;
            int nodeCount = outputLoop.NodeIds.Count;
            if (nodeCount < edgeCount)
                return "an output free boundary loop of " + nodeCount + " mesh edge/s cannot carry " +
                       edgeCount + " model edge/s";
            if (edgeCount == 1)
            {
                edgePaths.Add(new KeyValuePair<QuadWildModelEdge, List<CandidateEdge>>(
                                  inputLoop.ModelEdges[0], new List<CandidateEdge>(outputLoop.Candidates)));
                return null;
            }
            // The output node closest to a source vertex splits the loop. A vertex that the mesher moved
            // along the boundary keeps its closest node as long as it stays inside its own arc, so the drift
            // that defeats the tube tracing does not matter here.
            int[] splitPositions = new int[edgeCount];
            HashSet<int> usedPositions = new HashSet<int>();
            for (int i = 0; i < edgeCount; i++)
            {
                splitPositions[i] = GetClosestFreeLoopPosition(context, outputLoop,
                                                               GetFreeVertexPoint(inputLoop, i));
                if (!usedPositions.Add(splitPositions[i]))
                    return "two source vertices of a free boundary loop resolve to the same mesh node, the " +
                           "output mesh is too coarse to carry the boundary";
            }
            // The split positions of a correctly assigned loop advance around it exactly once, in the
            // direction the source loop happens to be walked
            List<int> directions = new List<int>();
            if (GetFreeLoopAdvance(splitPositions, nodeCount, 1) == nodeCount) directions.Add(1);
            if (GetFreeLoopAdvance(splitPositions, nodeCount, -1) == nodeCount) directions.Add(-1);
            if (directions.Count == 0)
                return "the source vertices of a free boundary loop do not follow the output mesh boundary";
            //
            double bestDeviation = Double.MaxValue;
            List<List<CandidateEdge>> bestArcs = null;
            foreach (int direction in directions)
            {
                List<List<CandidateEdge>> arcs = GetFreeLoopArcs(outputLoop, splitPositions, direction);
                if (arcs == null) continue;
                double deviation = GetFreeLoopArcDeviation(context, inputLoop, arcs);
                if (deviation < bestDeviation)
                {
                    bestDeviation = deviation;
                    bestArcs = arcs;
                }
            }
            if (bestArcs == null)
                return "an output free boundary loop cannot be split into " + edgeCount + " arcs";
            //
            for (int i = 0; i < edgeCount; i++)
            {
                edgePaths.Add(new KeyValuePair<QuadWildModelEdge, List<CandidateEdge>>(
                                  inputLoop.ModelEdges[i], bestArcs[i]));
            }
            return null;
        }
        private static double[] GetFreeVertexPoint(FreeInputLoop inputLoop, int index)
        {
            QuadWildModelEdge modelEdge = inputLoop.ModelEdges[index];
            if (modelEdge.StartNodeId == inputLoop.VertexNodeIds[index]) return modelEdge.StartPoint;
            else return modelEdge.EndPoint;
        }
        private static int GetClosestFreeLoopPosition(TraceContext context, FreeOutputLoop outputLoop,
                                                      double[] point)
        {
            int position = 0;
            double closestDistanceSquared = Double.MaxValue;
            for (int i = 0; i < outputLoop.NodeIds.Count; i++)
            {
                FeNode node = context.Mesh.Nodes[outputLoop.NodeIds[i]];
                double dx = node.X - point[0];
                double dy = node.Y - point[1];
                double dz = node.Z - point[2];
                double distanceSquared = dx * dx + dy * dy + dz * dz;
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    position = i;
                }
            }
            return position;
        }
        private static int GetFreeLoopAdvance(int[] splitPositions, int nodeCount, int direction)
        {
            int advance = 0;
            for (int i = 0; i < splitPositions.Length; i++)
            {
                int from = splitPositions[i];
                int to = splitPositions[(i + 1) % splitPositions.Length];
                int step = direction > 0 ? to - from : from - to;
                if (step <= 0) step += nodeCount;
                advance += step;
            }
            return advance;
        }
        private static List<List<CandidateEdge>> GetFreeLoopArcs(FreeOutputLoop outputLoop, int[] splitPositions,
                                                                 int direction)
        {
            int nodeCount = outputLoop.NodeIds.Count;
            List<List<CandidateEdge>> arcs = new List<List<CandidateEdge>>();
            for (int i = 0; i < splitPositions.Length; i++)
            {
                int position = splitPositions[i];
                int endPosition = splitPositions[(i + 1) % splitPositions.Length];
                List<CandidateEdge> arc = new List<CandidateEdge>();
                while (position != endPosition)
                {
                    // The candidate at a position starts at the node with the same position
                    if (direction > 0)
                    {
                        arc.Add(outputLoop.Candidates[position]);
                        position = (position + 1) % nodeCount;
                    }
                    else
                    {
                        position = (position - 1 + nodeCount) % nodeCount;
                        arc.Add(outputLoop.Candidates[position]);
                    }
                    if (arc.Count > nodeCount) return null;
                }
                if (arc.Count == 0) return null;
                arcs.Add(arc);
            }
            return arcs;
        }
        private static double GetFreeLoopArcDeviation(TraceContext context, FreeInputLoop inputLoop,
                                                      List<List<CandidateEdge>> arcs)
        {
            double deviation = 0;
            for (int i = 0; i < arcs.Count; i++)
            {
                QuadWildModelEdge modelEdge = inputLoop.ModelEdges[i];
                foreach (CandidateEdge candidate in arcs[i])
                {
                    FeNode node = context.Mesh.Nodes[candidate.EdgeCell[0]];
                    deviation += modelEdge.GetDistanceSquared(node.X, node.Y, node.Z);
                }
            }
            return deviation;
        }
        private static List<CandidateEdge> TraceModelEdge(TraceContext context, QuadWildModelEdge modelEdge,
                                                          bool relaxed, out string failureReason)
        {
            failureReason = null;
            double anchorRadius = QuadWildModelEdge.GetAnchorSearchRadius(context.MeshEdgeLength,
                                                                           modelEdge.ModelDiagonal);
            int startNodeId;
            int endNodeId;
            if (!context.AnchorNodeIds.TryGetValue(modelEdge.StartNodeId, out startNodeId)) startNodeId = -1;
            if (modelEdge.IsClosed) endNodeId = startNodeId;
            else if (!context.AnchorNodeIds.TryGetValue(modelEdge.EndNodeId, out endNodeId)) endNodeId = -1;
            if (startNodeId == -1 || endNodeId == -1)
            {
                failureReason = "the source edge end points have no mesh node within " + Format(anchorRadius);
                return null;
            }
            if (!modelEdge.IsClosed && startNodeId == endNodeId)
            {
                failureReason = "both source edge end points resolve to the same mesh node, the output mesh " +
                                "is too coarse to carry this edge";
                return null;
            }
            //
            double tolerance = modelEdge.GetTubeTolerance(relaxed, context.MeshEdgeLength);
            List<int> tubeIndices = CollectTube(context, modelEdge, relaxed, tolerance, startNodeId, endNodeId);
            if (tubeIndices.Count == 0)
            {
                failureReason = "no mesh edge lies within " + Format(tolerance) + " of the source edge";
                return null;
            }
            //
            int stamp = context.Stamp;
            List<CandidateEdge> path = modelEdge.IsClosed
                                     ? FindShortestCycle(context, startNodeId, stamp)
                                     : FindShortestPath(context, startNodeId, endNodeId, stamp, -1);
            if (path == null)
            {
                failureReason = "no connected mesh-edge path follows the source edge within " +
                                Format(tolerance);
                return null;
            }
            //
            double coverageTolerance = modelEdge.GetCoverageTolerance(context.MeshEdgeLength);
            if (!IsCovered(context, modelEdge, path, coverageTolerance, out double deviation))
            {
                failureReason = "the closest mesh-edge path leaves the source edge by " + Format(deviation) +
                                " (allowed " + Format(coverageTolerance) + ")";
                return null;
            }
            return path;
        }
        private static List<int> CollectTube(TraceContext context, QuadWildModelEdge modelEdge, bool relaxed,
                                             double tolerance, int startNodeId, int endNodeId)
        {
            context.Stamp++;
            int stamp = context.Stamp;
            List<int> tubeIndices = new List<int>();
            // The first and the last mesh edge of a path can straddle the source vertex, because a vertex
            // that is not a geometric corner falls between two output nodes. Such an edge only has its far
            // node on the source polyline, so it is admitted through its anchor before the polyline sweep.
            AddAnchorTubeCandidates(context, modelEdge, relaxed, tolerance, stamp, startNodeId, tubeIndices);
            if (endNodeId != startNodeId)
                AddAnchorTubeCandidates(context, modelEdge, relaxed, tolerance, stamp, endNodeId, tubeIndices);
            //
            int segmentCount = modelEdge.SegmentCount;
            double[] start;
            double[] end;
            double[] box = new double[6];
            for (int i = 0; i < segmentCount; i++)
            {
                modelEdge.GetSegment(i, out start, out end);
                for (int j = 0; j < 3; j++)
                {
                    box[j] = Math.Min(start[j], end[j]);
                    box[j + 3] = Math.Max(start[j], end[j]);
                }
                context.Grid.Query(box, tolerance, context.QueryBuffer);
                foreach (int index in context.QueryBuffer)
                {
                    if (context.SeenIds[index] == stamp) continue;
                    context.SeenIds[index] = stamp;
                    CandidateEdge candidate = context.Candidates[index];
                    if (!IsTubeCandidate(candidate, relaxed)) continue;
                    if (!modelEdge.BoxOverlaps(candidate.Box, tolerance)) continue;
                    double deviation = GetTubeDeviation(context, modelEdge, candidate, -1);
                    if (deviation > tolerance) continue;
                    AddTubeCandidate(context, candidate, deviation, tolerance, stamp, tubeIndices);
                }
            }
            tubeIndices.Sort();
            return tubeIndices;
        }
        private static void AddAnchorTubeCandidates(TraceContext context, QuadWildModelEdge modelEdge,
                                                    bool relaxed, double tolerance, int stamp, int anchorNodeId,
                                                    List<int> tubeIndices)
        {
            if (!context.NodeIdCandidateIndices.TryGetValue(anchorNodeId, out List<int> indices)) return;
            foreach (int index in indices)
            {
                if (context.SeenIds[index] == stamp) continue;
                context.SeenIds[index] = stamp;
                CandidateEdge candidate = context.Candidates[index];
                if (!IsTubeCandidate(candidate, relaxed)) continue;
                double deviation = GetTubeDeviation(context, modelEdge, candidate, anchorNodeId);
                if (deviation > tolerance) continue;
                AddTubeCandidate(context, candidate, deviation, tolerance, stamp, tubeIndices);
            }
        }
        private static bool IsTubeCandidate(CandidateEdge candidate, bool relaxed)
        {
            if (candidate.OwnerEdgeId != -1 || candidate.Length <= 0) return false;
            return relaxed || candidate.IsFeature;
        }
        private static double GetTubeDeviation(TraceContext context, QuadWildModelEdge modelEdge,
                                               CandidateEdge candidate, int exemptNodeId)
        {
            double deviation = 0;
            for (int i = 0; i < 2; i++)
            {
                if (candidate.EdgeCell[i] == exemptNodeId) continue;
                FeNode node = context.Mesh.Nodes[candidate.EdgeCell[i]];
                deviation = Math.Max(deviation,
                                     Math.Sqrt(modelEdge.GetDistanceSquared(node.X, node.Y, node.Z)));
            }
            return deviation;
        }
        private static void AddTubeCandidate(TraceContext context, CandidateEdge candidate, double deviation,
                                             double tolerance, int stamp, List<int> tubeIndices)
        {
            context.TubeIds[candidate.Index] = stamp;
            context.Costs[candidate.Index] = candidate.Length *
                                             (1 + DeviationCostFactor * deviation / tolerance) *
                                             (candidate.IsFeature ? 1 : NonFeatureCostFactor);
            tubeIndices.Add(candidate.Index);
        }
        private static List<CandidateEdge> FindShortestCycle(TraceContext context, int seedNodeId, int stamp)
        {
            if (!context.NodeIdCandidateIndices.TryGetValue(seedNodeId, out List<int> seedIndices)) return null;
            List<CandidateEdge> bestCycle = null;
            double bestCost = Double.MaxValue;
            foreach (int index in seedIndices.OrderBy(item => item))
            {
                if (context.TubeIds[index] != stamp) continue;
                CandidateEdge candidate = context.Candidates[index];
                int otherNodeId = candidate.EdgeCell[0] == seedNodeId ? candidate.EdgeCell[1]
                                                                      : candidate.EdgeCell[0];
                List<CandidateEdge> path = FindShortestPath(context, otherNodeId, seedNodeId, stamp, index);
                if (path == null) continue;
                double cost = context.Costs[index];
                foreach (CandidateEdge pathCandidate in path) cost += context.Costs[pathCandidate.Index];
                if (cost < bestCost)
                {
                    bestCost = cost;
                    path.Insert(0, candidate);
                    bestCycle = path;
                }
            }
            return bestCycle;
        }
        private static List<CandidateEdge> FindShortestPath(TraceContext context, int startNodeId,
                                                            int endNodeId, int stamp,
                                                            int forbiddenCandidateIndex)
        {
            Dictionary<int, double> nodeDistance = new Dictionary<int, double>();
            Dictionary<int, int> nodePreviousIndex = new Dictionary<int, int>();
            SortedSet<(double, int)> queue = new SortedSet<(double, int)>();
            nodeDistance.Add(startNodeId, 0);
            queue.Add((0, startNodeId));
            //
            while (queue.Count > 0)
            {
                (double, int) current = queue.Min;
                queue.Remove(current);
                int nodeId = current.Item2;
                if (nodeId == endNodeId) break;
                if (current.Item1 > nodeDistance[nodeId]) continue;
                if (!context.NodeIdCandidateIndices.TryGetValue(nodeId, out List<int> indices)) continue;
                foreach (int index in indices)
                {
                    if (context.TubeIds[index] != stamp || index == forbiddenCandidateIndex) continue;
                    CandidateEdge candidate = context.Candidates[index];
                    int otherNodeId = candidate.EdgeCell[0] == nodeId ? candidate.EdgeCell[1]
                                                                      : candidate.EdgeCell[0];
                    double distance = current.Item1 + context.Costs[index];
                    if (nodeDistance.TryGetValue(otherNodeId, out double otherDistance))
                    {
                        if (distance >= otherDistance) continue;
                        queue.Remove((otherDistance, otherNodeId));
                    }
                    nodeDistance[otherNodeId] = distance;
                    nodePreviousIndex[otherNodeId] = index;
                    queue.Add((distance, otherNodeId));
                }
            }
            if (!nodeDistance.ContainsKey(endNodeId) || endNodeId == startNodeId) return null;
            //
            List<CandidateEdge> path = new List<CandidateEdge>();
            int walkNodeId = endNodeId;
            while (walkNodeId != startNodeId)
            {
                if (!nodePreviousIndex.TryGetValue(walkNodeId, out int index)) return null;
                CandidateEdge candidate = context.Candidates[index];
                path.Add(candidate);
                walkNodeId = candidate.EdgeCell[0] == walkNodeId ? candidate.EdgeCell[1]
                                                                 : candidate.EdgeCell[0];
                if (path.Count > context.Candidates.Count) return null;
            }
            path.Reverse();
            return path;
        }
        private static bool IsCovered(TraceContext context, QuadWildModelEdge modelEdge,
                                      List<CandidateEdge> path, double coverageTolerance, out double deviation)
        {
            context.Stamp++;
            int stamp = context.Stamp;
            foreach (CandidateEdge candidate in path) context.PathIds[candidate.Index] = stamp;
            //
            deviation = 0;
            double[] box = new double[6];
            foreach (double[] samplePoint in modelEdge.GetSamplePoints())
            {
                for (int i = 0; i < 3; i++)
                {
                    box[i] = samplePoint[i];
                    box[i + 3] = samplePoint[i];
                }
                context.Grid.Query(box, coverageTolerance, context.QueryBuffer);
                bool covered = false;
                foreach (int index in context.QueryBuffer)
                {
                    if (context.PathIds[index] != stamp) continue;
                    if (GetPointToCandidateDistanceSquared(context, samplePoint, context.Candidates[index]) <=
                        coverageTolerance * coverageTolerance)
                    {
                        covered = true;
                        break;
                    }
                }
                if (!covered)
                {
                    deviation = Math.Sqrt(GetPointToPathDistanceSquared(context, samplePoint, path));
                    return false;
                }
            }
            return true;
        }
        private static double GetPointToPathDistanceSquared(TraceContext context, double[] point,
                                                            List<CandidateEdge> path)
        {
            double minDistanceSquared = Double.MaxValue;
            foreach (CandidateEdge candidate in path)
            {
                minDistanceSquared = Math.Min(minDistanceSquared,
                                              GetPointToCandidateDistanceSquared(context, point, candidate));
            }
            return minDistanceSquared;
        }
        private static double GetPointToCandidateDistanceSquared(TraceContext context, double[] point,
                                                                 CandidateEdge candidate)
        {
            FeNode node0 = context.Mesh.Nodes[candidate.EdgeCell[0]];
            FeNode node1 = context.Mesh.Nodes[candidate.EdgeCell[1]];
            if (candidate.EdgeCell.Length == 3)
            {
                FeNode midNode = context.Mesh.Nodes[candidate.EdgeCell[2]];
                return Math.Min(QuadWildModelEdge.GetPointToSegmentDistanceSquared(
                                    point[0], point[1], point[2], node0.X, node0.Y, node0.Z,
                                    midNode.X, midNode.Y, midNode.Z),
                                QuadWildModelEdge.GetPointToSegmentDistanceSquared(
                                    point[0], point[1], point[2], midNode.X, midNode.Y, midNode.Z,
                                    node1.X, node1.Y, node1.Z));
            }
            return QuadWildModelEdge.GetPointToSegmentDistanceSquared(point[0], point[1], point[2],
                                                                       node0.X, node0.Y, node0.Z,
                                                                       node1.X, node1.Y, node1.Z);
        }
        private static int RestoreFaceOrder(BasePart part, int[][] modelFaceEdgeIds, out string warning)
        {
            warning = null;
            int[][] faceEdgeIds = part.Visualization.FaceEdgeIds;
            if (modelFaceEdgeIds == null || faceEdgeIds == null)
            {
                warning = GetFaceOrderWarning("the input model faces were not recorded");
                return 0;
            }
            if (faceEdgeIds.Length != modelFaceEdgeIds.Length)
            {
                warning = GetFaceOrderWarning("the mesh contains " + faceEdgeIds.Length + " faces and the input " +
                                              "geometry " + modelFaceEdgeIds.Length);
                return 0;
            }
            // A recovered face is bounded by exactly the model edges that bound its input face, and the model
            // edges keep the input ids, so the bounding edge ids identify the input face without any tolerance
            CompareIntArray comparer = new CompareIntArray();
            Dictionary<int[], int> modelFaceIdByEdgeIds = new Dictionary<int[], int>(comparer);
            for (int modelFaceId = 0; modelFaceId < modelFaceEdgeIds.Length; modelFaceId++)
            {
                if (modelFaceEdgeIds[modelFaceId] == null ||
                    modelFaceIdByEdgeIds.ContainsKey(modelFaceEdgeIds[modelFaceId]))
                {
                    warning = GetFaceOrderWarning("the input model faces are not uniquely bounded by their " +
                                                  "model edges");
                    return 0;
                }
                modelFaceIdByEdgeIds.Add(modelFaceEdgeIds[modelFaceId], modelFaceId);
            }
            //
            int[] key;
            int[] orderedSurfaceIds = new int[faceEdgeIds.Length];
            bool[] mapped = new bool[faceEdgeIds.Length];
            for (int faceId = 0; faceId < faceEdgeIds.Length; faceId++)
            {
                if (faceEdgeIds[faceId] == null || faceEdgeIds[faceId].Length == 0)
                {
                    warning = GetFaceOrderWarning("mesh face " + faceId + " is not bounded by any model edge");
                    return 0;
                }
                key = faceEdgeIds[faceId].ToArray();
                Array.Sort(key);
                if (!modelFaceIdByEdgeIds.TryGetValue(key, out int modelFaceId) || mapped[modelFaceId])
                {
                    warning = GetFaceOrderWarning("mesh face " + faceId + " does not match any input model face");
                    return 0;
                }
                orderedSurfaceIds[modelFaceId] = faceId;
                mapped[modelFaceId] = true;
            }
            part.RenumberVisualizationSurfaces(orderedSurfaceIds);
            //
            return orderedSurfaceIds.Length;
        }
        private static string GetFaceOrderWarning(string reason)
        {
            return "The QuadWild model face order could not be restored because " + reason + ". Geometry based " +
                   "selections and the thicken shell mesh normals can be mapped to the wrong faces.";
        }
        private static string TrySetRecoveredTopology(FeMesh mesh, BasePart part,
                                                      Dictionary<int, List<int[]>> edgeIdEdgeCells)
        {
            VisualizationData originalVisualization = part.Visualization.DeepCopy();
            try
            {
                mesh.SetPartVisualizationModelEdges(part, edgeIdEdgeCells);
                return null;
            }
            catch (Exception ex)
            {
                // SetPartVisualizationModelEdges validates paths before mutation, but retain a full snapshot
                // as protection against failures while recomputing lengths, faces, or areas.
                part.Visualization = originalVisualization;
                return ex.Message;
            }
        }
        private static void TryDetectGeometryTypes(FeMesh mesh, BasePart part)
        {
            try
            {
                mesh.DetectPartFaceTypes(part, true);
                mesh.DetectPartEdgeTypes(part, true);
            }
            catch
            {
                // Curve/surface type recognition is optional; the recovered edge topology remains valid.
            }
        }
        private static (int, int) GetEdgeKey(int nodeId1, int nodeId2)
        {
            return nodeId1 < nodeId2 ? (nodeId1, nodeId2) : (nodeId2, nodeId1);
        }
    }
}
