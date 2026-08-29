// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;

namespace CaeMesh
{
    internal class DirectionalDisassemblyPlan
    {
        public int RootClusterId;
        public double[][] ClusterOffsets;
        public double[][][] ClusterStepOffsets;
        public int[] ParentClusterIds;
        public bool[] IsFastener;
        public DisassemblyMove[] Moves;
    }

    /// <summary>
    /// Greedy/best-first directional disassembly planner with bounded alternative search. Candidate
    /// directions come only from inferred geometry (with a radial fallback for disconnected parts), and a
    /// move is accepted only when the directional blocker evaluator finds a clear swept path. If no single
    /// cluster can move, connected pairs and triples are tested as temporary rigid subassemblies and split
    /// after they have cleared the main assembly.
    /// </summary>
    internal class DirectionalDisassemblyPlanner
    {
        private class DirectionOption
        {
            public Vec3D Direction;
            public double Confidence;
        }

        private class MoveCandidate
        {
            public int[] ClusterIds;
            public Vec3D Direction;
            public double Confidence;
            public double Distance;
            public double Score;
            public bool TemporarySubassembly;
        }

        private class CandidateSeed
        {
            public int ClusterId;
            public DirectionOption Option;
            public double Priority;
        }

        public static int MaxBacktrackingBranches = 3;
        public static int MaxSearchNodes = 256;
        public static int MaxTemporaryGroupSize = 3;
        public static int MaxTemporaryGroupCandidates = 32;
        public static int MaxSingleCandidatePool = 12;
        public static double DirectionMergeAngleDeg = 5;
        public static double FastenerSizeFactor = 0.2;
        //
        private FeMesh _mesh;
        private DisassemblyContactGraph _graph;
        private DirectionalBlockerEvaluator _blockerEvaluator;
        private int _explodedDirection;
        private int _rootClusterId;
        private bool[] _isFastener;
        private BoundingBox _assemblyBox;
        private Vec3D _assemblyCenter;
        private double _assemblyDiagonal;
        private int _searchNodeCount;

        private DirectionalDisassemblyPlanner(FeMesh mesh, DisassemblyContactGraph graph, int explodedDirection)
        {
            _mesh = mesh;
            _graph = graph;
            _explodedDirection = explodedDirection;
            _blockerEvaluator = new DirectionalBlockerEvaluator(mesh, graph);
            _assemblyBox = new BoundingBox();
            foreach (ExplodedViewCluster cluster in graph.Clusters) _assemblyBox.IncludeBox(cluster.BoundingBox);
            _assemblyCenter = new Vec3D(_assemblyBox.GetCenter());
            _assemblyDiagonal = Math.Max(_assemblyBox.GetDiagonal(), graph.Tolerance);
            _isFastener = GetFastenerFlags();
            _rootClusterId = GetRootClusterId();
        }

        public static DirectionalDisassemblyPlan Create(FeMesh mesh, DisassemblyContactGraph graph,
                                                        int explodedDirection)
        {
            if (mesh == null || graph == null || graph.Clusters.Length == 0) return null;
            return new DirectionalDisassemblyPlanner(mesh, graph, explodedDirection).Create();
        }

        private DirectionalDisassemblyPlan Create()
        {
            HashSet<int> remaining = new HashSet<int>(Enumerable.Range(0, _graph.Clusters.Length));
            List<DisassemblyMove> moves = new List<DisassemblyMove>();
            Dictionary<int, double[]> finalOffsets = new Dictionary<int, double[]>();
            finalOffsets.Add(_rootClusterId, new double[3]);
            //
            if (!Search(remaining, moves, finalOffsets)) return null;
            if (!ExpandTemporarySubassemblies(moves, finalOffsets)) return null;
            for (int i = 0; i < moves.Count; i++) moves[i].Step = i;
            if (!ValidateFinalPositions(finalOffsets)) return null;
            //
            int count = _graph.Clusters.Length;
            double[][] clusterOffsets = new double[count][];
            double[][][] clusterStepOffsets = new double[count][][];
            for (int clusterId = 0; clusterId < count; clusterId++)
            {
                clusterOffsets[clusterId] = finalOffsets.ContainsKey(clusterId) ? finalOffsets[clusterId] : new double[3];
                clusterStepOffsets[clusterId] = new double[moves.Count][];
                for (int step = 0; step < moves.Count; step++) clusterStepOffsets[clusterId][step] = new double[3];
            }
            foreach (DisassemblyMove move in moves)
            {
                double[] delta = (move.Distance * move.Direction).Coor;
                foreach (int clusterId in move.ClusterIds) clusterStepOffsets[clusterId][move.Step] = delta;
            }
            int[] parentClusterIds = new int[count];
            for (int i = 0; i < count; i++) parentClusterIds[i] = -1;
            //
            return new DirectionalDisassemblyPlan()
            {
                RootClusterId = _rootClusterId,
                ClusterOffsets = clusterOffsets,
                ClusterStepOffsets = clusterStepOffsets,
                ParentClusterIds = parentClusterIds,
                IsFastener = _isFastener,
                Moves = moves.ToArray()
            };
        }

        private bool Search(HashSet<int> remaining, List<DisassemblyMove> moves,
                            Dictionary<int, double[]> finalOffsets)
        {
            if (++_searchNodeCount > MaxSearchNodes) return false;
            if (remaining.Count <= 1) return remaining.Count == 0 || remaining.Contains(_rootClusterId);
            //
            List<MoveCandidate> candidates = GetSingleClusterCandidates(remaining, finalOffsets);
            if (candidates.Count == 0) candidates = GetTemporarySubassemblyCandidates(remaining, finalOffsets);
            if (candidates.Count == 0) return false;
            candidates.Sort((first, second) => second.Score.CompareTo(first.Score));
            //
            int branchCount = Math.Min(MaxBacktrackingBranches, candidates.Count);
            for (int branch = 0; branch < branchCount; branch++)
            {
                MoveCandidate candidate = candidates[branch];
                HashSet<int> nextRemaining = new HashSet<int>(remaining);
                Dictionary<int, double[]> nextOffsets = CloneOffsets(finalOffsets);
                double[] delta = (candidate.Distance * candidate.Direction).Coor;
                foreach (int clusterId in candidate.ClusterIds)
                {
                    nextRemaining.Remove(clusterId);
                    nextOffsets[clusterId] = delta;
                }
                //
                DisassemblyMove move = new DisassemblyMove(candidate.ClusterIds, candidate.Direction,
                    candidate.Distance, moves.Count, candidate.Confidence, candidate.TemporarySubassembly);
                moves.Add(move);
                if (Search(nextRemaining, moves, nextOffsets))
                {
                    finalOffsets.Clear();
                    foreach (var entry in nextOffsets) finalOffsets.Add(entry.Key, entry.Value);
                    return true;
                }
                moves.RemoveAt(moves.Count - 1);
            }
            return false;
        }

        private List<MoveCandidate> GetSingleClusterCandidates(HashSet<int> remaining,
                                                                Dictionary<int, double[]> finalOffsets)
        {
            List<CandidateSeed> seeds = new List<CandidateSeed>();
            foreach (int clusterId in remaining)
            {
                if (clusterId == _rootClusterId) continue;
                ExplodedViewCluster cluster = _graph.Clusters[clusterId];
                double exterior = (new Vec3D(cluster.Center) - _assemblyCenter).Len / _assemblyDiagonal;
                double degree = _graph.GetDegree(clusterId);
                foreach (DirectionOption option in GetDirectionOptions(clusterId))
                {
                    seeds.Add(new CandidateSeed()
                    {
                        ClusterId = clusterId,
                        Option = option,
                        Priority = 100 * option.Confidence + (_isFastener[clusterId] ? 20 : 0) +
                                   10 / (1 + degree) + 5 * exterior
                    });
                }
            }
            seeds.Sort((first, second) => second.Priority.CompareTo(first.Priority));
            //
            List<MoveCandidate> candidates = new List<MoveCandidate>();
            foreach (CandidateSeed seed in seeds)
            {
                int[] movingClusterIds = new int[] { seed.ClusterId };
                if (_blockerEvaluator.HasBlocker(movingClusterIds, seed.Option.Direction, remaining)) continue;
                MoveCandidate candidate = CreateCandidate(movingClusterIds, seed.Option,
                                                          remaining, finalOffsets, false);
                if (candidate != null) candidates.Add(candidate);
                // Search explores at most MaxBacktrackingBranches alternatives. A small prioritized pool
                // retains useful alternatives without exhaustively validating every removable part.
                if (candidates.Count >= Math.Max(MaxBacktrackingBranches, MaxSingleCandidatePool)) break;
            }
            return candidates;
        }

        private List<MoveCandidate> GetTemporarySubassemblyCandidates(HashSet<int> remaining,
                                                                       Dictionary<int, double[]> finalOffsets)
        {
            List<int[]> groups = GetConnectedGroups(remaining);
            List<MoveCandidate> candidates = new List<MoveCandidate>();
            foreach (int[] group in groups)
            {
                foreach (DirectionOption option in GetGroupDirectionOptions(group))
                {
                    if (_blockerEvaluator.HasBlocker(group, option.Direction, remaining)) continue;
                    MoveCandidate candidate = CreateCandidate(group, option, remaining, finalOffsets, true);
                    if (candidate != null) candidates.Add(candidate);
                }
                if (candidates.Count >= MaxTemporaryGroupCandidates) break;
            }
            return candidates;
        }

        private MoveCandidate CreateCandidate(int[] clusterIds, DirectionOption option,
                                              HashSet<int> remaining,
                                              Dictionary<int, double[]> finalOffsets,
                                              bool temporarySubassembly)
        {
            BoundingBox movingBox = GetGroupBox(clusterIds, null);
            double gap = Math.Max(0.1 * movingBox.GetDiagonal(), 0.01 * _assemblyDiagonal);
            double distance = gap;
            HashSet<int> moving = new HashSet<int>(clusterIds);
            foreach (int staticClusterId in remaining)
            {
                if (moving.Contains(staticClusterId)) continue;
                distance = Math.Max(distance, DirectionalBlockerEvaluator.GetPassDistance(
                    movingBox, _graph.Clusters[staticClusterId].BoundingBox, option.Direction, gap));
            }
            // Previously removed parts stay visible, but they must not automatically push every later part
            // beyond them. If there is room, the later part belongs between the core and the earlier one
            // (washer/nut/bolt packing); TryExtendToValidFinalPosition only reacts to an actual final overlap.
            if (!TryExtendToValidFinalPosition(clusterIds, option.Direction, ref distance,
                                               remaining, finalOffsets, gap)) return null;
            foreach (int movingClusterId in clusterIds)
            {
                foreach (var entry in finalOffsets)
                {
                    if (moving.Contains(entry.Key) || remaining.Contains(entry.Key)) continue;
                    if (_blockerEvaluator.HasSweptConflict(movingClusterId, new double[3], entry.Key, entry.Value,
                                                           option.Direction, distance, false)) return null;
                }
            }
            //
            double exterior = (new Vec3D(movingBox.GetCenter()) - _assemblyCenter).Len / _assemblyDiagonal;
            double degree = clusterIds.Sum(clusterId => _graph.GetDegree(clusterId));
            bool containsFastener = clusterIds.Any(clusterId => _isFastener[clusterId]);
            double score = 100 * option.Confidence + (containsFastener ? 20 : 0) + 10 / (1 + degree) +
                           5 * exterior - 2 * distance / _assemblyDiagonal -
                           (temporarySubassembly ? 25 + 5 * (clusterIds.Length - 2) : 0);
            return new MoveCandidate()
            {
                ClusterIds = clusterIds,
                Direction = option.Direction,
                Confidence = option.Confidence,
                Distance = distance,
                Score = score,
                TemporarySubassembly = temporarySubassembly
            };
        }

        private bool TryExtendToValidFinalPosition(int[] movingClusterIds, Vec3D direction, ref double distance,
                                                   HashSet<int> remaining,
                                                   Dictionary<int, double[]> finalOffsets, double gap)
        {
            HashSet<int> moving = new HashSet<int>(movingClusterIds);
            for (int pass = 0; pass < 8; pass++)
            {
                double[] movingOffset = (distance * direction).Coor;
                bool changed = false;
                foreach (int movingClusterId in movingClusterIds)
                {
                    BoundingBox movingBox = new BoundingBox(_graph.Clusters[movingClusterId].BoundingBox);
                    movingBox.AddOffset(movingOffset);
                    foreach (int staticClusterId in remaining)
                    {
                        if (moving.Contains(staticClusterId)) continue;
                        BoundingBox staticBox = _graph.Clusters[staticClusterId].BoundingBox;
                        if (!movingBox.Intersects(staticBox)) continue;
                        distance += DirectionalBlockerEvaluator.GetPassDistance(movingBox, staticBox, direction, gap);
                        changed = true;
                    }
                    foreach (var entry in finalOffsets)
                    {
                        if (moving.Contains(entry.Key) || remaining.Contains(entry.Key)) continue;
                        BoundingBox staticBox = new BoundingBox(_graph.Clusters[entry.Key].BoundingBox);
                        staticBox.AddOffset(entry.Value);
                        if (!movingBox.Intersects(staticBox)) continue;
                        distance += DirectionalBlockerEvaluator.GetPassDistance(movingBox, staticBox, direction, gap);
                        changed = true;
                    }
                }
                if (!changed) return true;
            }
            return false;
        }

        private List<DirectionOption> GetDirectionOptions(int clusterId)
        {
            List<DirectionOption> options = new List<DirectionOption>();
            foreach (ExplodedViewInterface relation in _graph.GetInterfaces(clusterId))
            {
                bool reverse = clusterId == relation.FirstClusterId;
                foreach (Vec3D candidate in relation.CandidateDirections)
                {
                    Vec3D direction = reverse ? -1 * candidate : new Vec3D(candidate);
                    AddDirectionOption(options, direction,
                        Math.Max(0.1, relation.RelationConfidence * Math.Max(0.25, relation.DirectionConfidence)));
                    if (relation.IsAxialDirection ||
                        (relation.RelationTypes & (ExplodedViewRelationType.Cylindrical |
                                                   ExplodedViewRelationType.Conical |
                                                   ExplodedViewRelationType.Prismatic)) != 0)
                        AddDirectionOption(options, -1 * direction, 0.9 * Math.Max(0.1, relation.RelationConfidence));
                }
                if (relation.CandidateDirections.Count == 0 && relation.HasDirection)
                    AddDirectionOption(options, relation.GetDirection(clusterId),
                                       Math.Max(0.1, relation.DirectionConfidence));
            }
            // A disconnected cluster has no inferred mating direction; radial motion is the only honest
            // fallback and is still subjected to the full blocker check.
            if (options.Count == 0)
            {
                Vec3D radial = new Vec3D(_graph.Clusters[clusterId].Center) - _assemblyCenter;
                if (radial.Len2 <= 1E-24) radial = new Vec3D(1, 0, 0);
                AddDirectionOption(options, radial, 0.05);
            }
            return options;
        }

        private List<DirectionOption> GetGroupDirectionOptions(int[] clusterIds)
        {
            List<DirectionOption> options = new List<DirectionOption>();
            HashSet<int> group = new HashSet<int>(clusterIds);
            foreach (int clusterId in clusterIds)
            {
                foreach (ExplodedViewInterface relation in _graph.GetInterfaces(clusterId))
                {
                    // Internal relations are particularly useful because they describe the common axis on
                    // which the temporary subassembly is assembled.
                    double weight = group.Contains(relation.GetOtherClusterId(clusterId)) ? 1 : 0.8;
                    foreach (DirectionOption option in GetDirectionOptions(clusterId))
                        AddDirectionOption(options, option.Direction, weight * option.Confidence);
                }
            }
            return options;
        }

        private void AddDirectionOption(List<DirectionOption> options, Vec3D direction, double confidence)
        {
            Vec3D filtered = GetFilteredDirection(direction);
            if (filtered == null) return;
            double minDot = Math.Cos(DirectionMergeAngleDeg * Math.PI / 180);
            foreach (DirectionOption existing in options)
            {
                if (Vec3D.DotProduct(existing.Direction, filtered) >= minDot)
                {
                    if (confidence > existing.Confidence) existing.Confidence = confidence;
                    return;
                }
            }
            options.Add(new DirectionOption() { Direction = filtered, Confidence = confidence });
        }

        private Vec3D GetFilteredDirection(Vec3D direction)
        {
            if (direction == null || direction.Len2 <= 1E-24) return null;
            Vec3D filtered = new Vec3D(direction);
            if (_explodedDirection == 1) filtered.Y = filtered.Z = 0;
            else if (_explodedDirection == 2) filtered.X = filtered.Z = 0;
            else if (_explodedDirection == 3) filtered.X = filtered.Y = 0;
            else if (_explodedDirection == 4) filtered.Z = 0;
            else if (_explodedDirection == 5) filtered.Y = 0;
            else if (_explodedDirection == 6) filtered.X = 0;
            if (filtered.Len2 <= 1E-24) return null;
            filtered.Normalize();
            return filtered;
        }

        private List<int[]> GetConnectedGroups(HashSet<int> remaining)
        {
            List<int[]> groups = new List<int[]>();
            HashSet<string> keys = new HashSet<string>();
            foreach (ExplodedViewInterface relation in _graph.Interfaces)
            {
                int first = relation.FirstClusterId;
                int second = relation.SecondClusterId;
                if (first == _rootClusterId || second == _rootClusterId ||
                    !remaining.Contains(first) || !remaining.Contains(second)) continue;
                AddGroup(groups, keys, new int[] { first, second });
                if (MaxTemporaryGroupSize < 3) continue;
                foreach (int member in new int[] { first, second })
                {
                    foreach (ExplodedViewInterface neighbourRelation in _graph.GetInterfaces(member))
                    {
                        int third = neighbourRelation.GetOtherClusterId(member);
                        if (third == _rootClusterId || third == first || third == second ||
                            !remaining.Contains(third)) continue;
                        AddGroup(groups, keys, new int[] { first, second, third });
                        if (groups.Count >= MaxTemporaryGroupCandidates) return groups;
                    }
                }
            }
            return groups;
        }

        private static void AddGroup(List<int[]> groups, HashSet<string> keys, int[] clusterIds)
        {
            Array.Sort(clusterIds);
            string key = string.Join(",", clusterIds);
            if (keys.Add(key)) groups.Add(clusterIds);
        }

        private bool ExpandTemporarySubassemblies(List<DisassemblyMove> moves,
                                                   Dictionary<int, double[]> finalOffsets)
        {
            DisassemblyMove[] groupMoves = moves.Where(move => move.TemporarySubassembly).ToArray();
            foreach (DisassemblyMove groupMove in groupMoves)
            {
                int anchor = groupMove.ClusterIds.OrderByDescending(id => _graph.Clusters[id].Size).First();
                foreach (int clusterId in groupMove.ClusterIds)
                {
                    if (clusterId == anchor) continue;
                    double[] currentOffset = finalOffsets[clusterId];
                    DirectionOption selected = null;
                    double selectedDistance = 0;
                    foreach (DirectionOption option in GetDirectionOptions(clusterId))
                    {
                        BoundingBox movingBox = new BoundingBox(_graph.Clusters[clusterId].BoundingBox);
                        movingBox.AddOffset(currentOffset);
                        double gap = Math.Max(0.1 * movingBox.GetDiagonal(), 0.01 * _assemblyDiagonal);
                        double distance = gap;
                        foreach (var entry in finalOffsets)
                        {
                            if (entry.Key == clusterId) continue;
                            BoundingBox staticBox = new BoundingBox(_graph.Clusters[entry.Key].BoundingBox);
                            staticBox.AddOffset(entry.Value);
                            distance = Math.Max(distance, DirectionalBlockerEvaluator.GetPassDistance(
                                movingBox, staticBox, option.Direction, gap));
                        }
                        bool blocked = false;
                        foreach (var entry in finalOffsets)
                        {
                            if (entry.Key == clusterId) continue;
                            if (_blockerEvaluator.HasSweptConflict(clusterId, currentOffset, entry.Key, entry.Value,
                                                                  option.Direction, distance, true))
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (!blocked && (selected == null || distance < selectedDistance))
                        {
                            selected = option;
                            selectedDistance = distance;
                        }
                    }
                    if (selected == null) return false;
                    //
                    double[] delta = (selectedDistance * selected.Direction).Coor;
                    finalOffsets[clusterId] = new double[] { currentOffset[0] + delta[0],
                                                             currentOffset[1] + delta[1],
                                                             currentOffset[2] + delta[2] };
                    moves.Add(new DisassemblyMove(new int[] { clusterId }, selected.Direction, selectedDistance,
                                                  moves.Count, selected.Confidence, false));
                }
            }
            return true;
        }

        private bool ValidateFinalPositions(Dictionary<int, double[]> finalOffsets)
        {
            for (int i = 0; i < _graph.Clusters.Length; i++)
            {
                BoundingBox first = new BoundingBox(_graph.Clusters[i].BoundingBox);
                first.AddOffset(finalOffsets[i]);
                for (int j = i + 1; j < _graph.Clusters.Length; j++)
                {
                    BoundingBox second = new BoundingBox(_graph.Clusters[j].BoundingBox);
                    second.AddOffset(finalOffsets[j]);
                    if (first.Intersects(second)) return false;
                }
            }
            return true;
        }

        private BoundingBox GetGroupBox(int[] clusterIds, Dictionary<int, double[]> offsets)
        {
            BoundingBox box = new BoundingBox();
            foreach (int clusterId in clusterIds)
            {
                BoundingBox clusterBox = new BoundingBox(_graph.Clusters[clusterId].BoundingBox);
                if (offsets != null && offsets.ContainsKey(clusterId)) clusterBox.AddOffset(offsets[clusterId]);
                box.IncludeBox(clusterBox);
            }
            return box;
        }

        private int GetRootClusterId()
        {
            int root = -1;
            int bestDegree = -1;
            bool hasNonFastener = _isFastener.Any(value => !value);
            foreach (ExplodedViewCluster cluster in _graph.Clusters)
            {
                if (hasNonFastener && _isFastener[cluster.Id]) continue;
                int degree = _graph.GetDegree(cluster.Id);
                if (root < 0 || degree > bestDegree ||
                    (degree == bestDegree && cluster.Size > _graph.Clusters[root].Size))
                {
                    root = cluster.Id;
                    bestDegree = degree;
                }
            }
            return root;
        }

        private bool[] GetFastenerFlags()
        {
            bool[] flags = new bool[_graph.Clusters.Length];
            foreach (ExplodedViewCluster cluster in _graph.Clusters)
            {
                int axialCount = 0;
                double maxNeighbourSize = 0;
                foreach (ExplodedViewInterface relation in _graph.GetInterfaces(cluster.Id))
                {
                    if (!relation.IsAxialDirection) continue;
                    axialCount++;
                    maxNeighbourSize = Math.Max(maxNeighbourSize,
                        _graph.Clusters[relation.GetOtherClusterId(cluster.Id)].Size);
                }
                flags[cluster.Id] = axialCount >= 2 && cluster.Size < FastenerSizeFactor * maxNeighbourSize;
            }
            return flags;
        }

        private static Dictionary<int, double[]> CloneOffsets(Dictionary<int, double[]> offsets)
        {
            Dictionary<int, double[]> clone = new Dictionary<int, double[]>(offsets.Count);
            foreach (var entry in offsets) clone.Add(entry.Key, (double[])entry.Value.Clone());
            return clone;
        }
    }
}
