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
using CaeMesh.Sweep;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// Matches boundary nodes across shared edges of four-sided and submapped shell faces, through round holes.
    /// A chain below MinLeanDegrees moves no edge beside a face outside the chains and must reach StraightLeanDegrees.
    /// A moved submap must reach RightAngleQuality, or keep a valid grid within QualityTolerance of the original
    /// quality if it has a round hole. Larger leans allow free faces to share moved edges.
    /// A patch refused on both boundaries leaves the chain. If an isolated free mesh gets worse and falls below the
    /// quality threshold, its edges stay fixed and the chain is matched again. The caller selects which free faces
    /// can be trialled. Full-quad and quasi-structured algorithms, periodic surfaces and optimized shells are excluded.
    /// The structured sources of a sweep are matched the same way, with the walls of the sweep in their chains.
    /// </summary>
    internal static class GmshShellSideSpacing
    {
        // Variables                                                                                                    
        private const int ShapeSamples = 9;
        private const double ShapeTolerance = 0.02;
        private const double MinLeanDegrees = 5;
        private const double StraightLeanDegrees = 0.1;
        private const double RightAngleQuality = 0.9999;
        private const double QualityTolerance = 0.02;
        private const double FractionTolerance = 1E-8;
        private const double RelaxationTolerance = 1E-10;
        private const double FreeFaceDepth = 3;
        private const int MaxRelaxationSteps = 512;


        // Methods                                                                                                      
        public static bool Match(IEnumerable<TransfiniteFace> shellFaces, IEnumerable<SubmapFace> submapFaces,
                                 HashSet<int> transfiniteSurfaceIds, HashSet<int> fixedSurfaceIds,
                                 Func<int, double> getSurfaceArea, Action meshEdges, bool recombine,
                                 Action<GmshShellGridQuality> tryFree, Func<int, double?> getFreeQuality)
        {
            return Match(shellFaces, submapFaces, null, transfiniteSurfaceIds, fixedSurfaceIds, null, getSurfaceArea,
                         meshEdges, recombine, tryFree, getFreeQuality, null);
        }
        /// <summary>
        /// The four-sided and submapped sources of a sweep, whose edges are meshed, with its walls. A wall carries the
        /// spacing of its source side to its target side. The following surfaces - the targets, which take the mesh of
        /// their sources - hold no edge back. No free face is trialled and no grid is tried free. The edges of the
        /// chains that moved are added to matchedEdgeIds.
        /// </summary>
        public static bool MatchSweepSources(IEnumerable<TransfiniteFace> sourceFaces,
                                             IEnumerable<SubmapFace> submapSources,
                                             IEnumerable<GmshShellSpacingPatch> wallPatches,
                                             HashSet<int> transfiniteSurfaceIds, HashSet<int> fixedSurfaceIds,
                                             HashSet<int> followingSurfaceIds, Func<int, double> getSurfaceArea,
                                             HashSet<int> matchedEdgeIds)
        {
            return Match(sourceFaces, submapSources, wallPatches, transfiniteSurfaceIds, fixedSurfaceIds,
                         followingSurfaceIds, getSurfaceArea, null, false, null, null, matchedEdgeIds);
        }
        private static bool Match(IEnumerable<TransfiniteFace> shellFaces, IEnumerable<SubmapFace> submapFaces,
                                  IEnumerable<GmshShellSpacingPatch> wallPatches, HashSet<int> transfiniteSurfaceIds,
                                  HashSet<int> fixedSurfaceIds, HashSet<int> followingSurfaceIds,
                                  Func<int, double> getSurfaceArea, Action meshEdges, bool recombine,
                                  Action<GmshShellGridQuality> tryFree, Func<int, double?> getFreeQuality,
                                  HashSet<int> matchedEdgeIds)
        {
            List<GmshShellSpacingPatch> patches = new List<GmshShellSpacingPatch>();
            foreach (TransfiniteFace face in shellFaces.OrderBy(face => face.FaceId))
            {
                int[] faceEdges = face.SideEdgeIds.SelectMany(side => side).ToArray();
                if (faceEdges.Distinct().Count() != faceEdges.Length) continue;
                patches.Add(new GmshShellSpacingPatch(face, 0));
                patches.Add(new GmshShellSpacingPatch(face, 1));
            }
            HashSet<int> roundEdges = new HashSet<int>();
            foreach (SubmapFace face in submapFaces.OrderBy(face => face.FaceId))
            {
                foreach (SubmapFace loop in face.Loops)
                {
                    if (loop.IsRound) roundEdges.UnionWith(loop.EdgeIds);
                }
                GmshShellSpacingPatch patch = new GmshShellSpacingPatch(face);
                if (patch.EdgeIds.Distinct().Count() == patch.EdgeIds.Length) patches.Add(patch);
            }
            if (wallPatches != null)
                patches.AddRange(wallPatches.Where(patch => patch.EdgeIds.Distinct().Count() == patch.EdgeIds.Length));
            if (!patches.Any(patch => patch.IsSeed)) return false;
            //
            Dictionary<int, List<GmshShellSpacingPatch>> edgePatches =
                new Dictionary<int, List<GmshShellSpacingPatch>>();
            foreach (GmshShellSpacingPatch patch in patches)
            {
                foreach (int edgeId in patch.EdgeIds)
                {
                    if (!edgePatches.TryGetValue(edgeId, out List<GmshShellSpacingPatch> neighbours))
                        edgePatches.Add(edgeId, neighbours = new List<GmshShellSpacingPatch>());
                    neighbours.Add(patch);
                }
            }
            meshEdges?.Invoke();
            Dictionary<int, GmshShellSpacingEdge> edges = new Dictionary<int, GmshShellSpacingEdge>();
            List<List<GmshShellSpacingPatch>> chains = GetChains(patches, edgePatches, edges);
            if (chains.Count == 0) return false;
            //
            GmshShellSpacingPatch[] matched = chains.SelectMany(chain => chain).ToArray();
            HashSet<int> matchedSurfaceIds = new HashSet<int>(matched.Select(patch => patch.FaceId));
            fixedSurfaceIds.ExceptWith(matched.Where(patch => patch.Submap != null).Select(patch => patch.FaceId));
            fixedSurfaceIds.UnionWith(transfiniteSurfaceIds.Where(id => !matchedSurfaceIds.Contains(id)));
            var turnOwners = matched.Where(patch => patch.Submap != null)
                                    .SelectMany(patch => patch.Submap.AllTurnEdgeIds
                                                              .Select(id => (EdgeId: id, patch.FaceId)))
                                    .ToLookup(entry => entry.EdgeId, entry => entry.FaceId);
            Action<int, bool, HashSet<int>> configureEdge = (edgeId, keepFreeFaces, protectedFaces) =>
            {
                GmshShellSpacingEdge edge = edges[edgeId];
                if (roundEdges.Contains(edgeId) || turnOwners[edgeId].Any(matchedSurfaceIds.Contains))
                {
                    edge.SetMovable(false);
                    return;
                }
                Gmsh.Model.GetAdjacencies(1, edgeId, out int[] adjacent, out _);
                double room = FreeFaceDepth * edge.Length * edge.Length / (edge.NumOfNodes - 1);
                edge.SetMovable(!adjacent.Any(id => fixedSurfaceIds.Contains(id) || protectedFaces.Contains(id)) &&
                               adjacent.Where(id => !matchedSurfaceIds.Contains(id) &&
                                                    (followingSurfaceIds == null || !followingSurfaceIds.Contains(id)))
                                       .All(id => !keepFreeFaces && getSurfaceArea(id) >= room));
            };
            //
            bool moved = false;
            foreach (List<GmshShellSpacingPatch> chain in chains)
            {
                if (!MatchChain(chain, edges, configureEdge, matchedSurfaceIds, getFreeQuality)) continue;
                moved = true;
                matchedEdgeIds?.UnionWith(chain.SelectMany(patch => patch.EdgeIds));
            }
            if (tryFree == null) return moved;
            foreach (TransfiniteFace face in matched.Where(patch => patch.Transfinite != null &&
                                                                   patch.HasMismatch(FractionTolerance))
                                                    .Select(patch => patch.Transfinite).Distinct()
                                                    .OrderBy(face => face.FaceId))
            {
                GmshShellGridQuality grid = GmshShellGridQuality.CreatePoorGrid(face, recombine);
                if (grid != null) tryFree(grid);
            }
            return moved;
        }
        private static List<List<GmshShellSpacingPatch>> GetChains(List<GmshShellSpacingPatch> patches,
            Dictionary<int, List<GmshShellSpacingPatch>> edgePatches, Dictionary<int, GmshShellSpacingEdge> edges)
        {
            List<List<GmshShellSpacingPatch>> chains = new List<List<GmshShellSpacingPatch>>();
            HashSet<GmshShellSpacingPatch> visited = new HashSet<GmshShellSpacingPatch>();
            foreach (GmshShellSpacingPatch seed in patches)
            {
                if (!seed.IsSeed || !visited.Add(seed)) continue;
                List<GmshShellSpacingPatch> chain = new List<GmshShellSpacingPatch>();
                Queue<GmshShellSpacingPatch> pending = new Queue<GmshShellSpacingPatch>();
                pending.Enqueue(seed);
                while (pending.Count > 0)
                {
                    GmshShellSpacingPatch patch = pending.Dequeue();
                    foreach (int edgeId in patch.EdgeIds)
                    {
                        if (edges.ContainsKey(edgeId)) continue;
                        SweepSideNodes nodes = SweepSideNodes.CreateForShellEdge(edgeId);
                        edges.Add(edgeId, nodes != null ? new GmshShellSpacingEdge(nodes) : null);
                    }
                    if (!patch.Prepare(edges, ShapeSamples, ShapeTolerance)) continue;
                    chain.Add(patch);
                    foreach (int edgeId in patch.EdgeIds)
                    {
                        foreach (GmshShellSpacingPatch neighbour in edgePatches[edgeId])
                        {
                            if (visited.Add(neighbour)) pending.Enqueue(neighbour);
                        }
                    }
                }
                if (chain.Count > 0) chains.Add(chain);
            }
            return chains;
        }
        private static bool MatchChain(List<GmshShellSpacingPatch> chain, Dictionary<int, GmshShellSpacingEdge> edges,
                                       Action<int, bool, HashSet<int>> configureEdge, HashSet<int> matchedSurfaceIds,
                                       Func<int, double?> getFreeQuality)
        {
            Dictionary<int, double?> originalFreeQualities = new Dictionary<int, double?>();
            HashSet<int> protectedFaces = new HashSet<int>();
            while (chain.Count > 0)
            {
                double lean = GetMaximumLean(chain);
                if (lean == 0) return false;
                bool exact = lean < MinLeanDegrees;
                int[] edgeIds = chain.SelectMany(patch => patch.EdgeIds).Distinct().OrderBy(id => id).ToArray();
                foreach (int edgeId in edgeIds) configureEdge(edgeId, exact, protectedFaces);
                GmshShellSpacingEdge[] chainEdges = edgeIds.Select(id => edges[id]).ToArray();
                if (!ProposeSpacing(chain, chainEdges, exact)) return false;
                //
                Dictionary<SubmapFace, double> gridsToCheck = GetGridsToCheck(chain, edges, exact);
                if (gridsToCheck.Count > 0)
                {
                    foreach (GmshShellSpacingEdge edge in chainEdges) edge.RestoreNodes();
                    List<int> refused = new List<int>();
                    bool restore = false;
                    foreach (var entry in gridsToCheck)
                    {
                        bool proposedRefused = double.IsNegativeInfinity(entry.Value);
                        if (exact && (proposedRefused || !HasRoundHole(entry.Key)))
                        {
                            restore = true;
                            break;
                        }
                        if (GmshSubmapMesher.Evaluate(entry.Key, out double quality, out _, true))
                            restore |= entry.Value < quality - QualityTolerance;
                        else if (proposedRefused) refused.Add(entry.Key.FaceId);
                    }
                    if (restore || refused.Count > 0)
                    {
                        foreach (GmshShellSpacingEdge edge in chainEdges) edge.ResetNodes();
                        if (refused.Count == 0) return false;
                        chain.RemoveAll(patch => refused.Contains(patch.FaceId));
                        matchedSurfaceIds.ExceptWith(refused);
                        continue;
                    }
                    foreach (GmshShellSpacingEdge edge in chainEdges) edge.ApplyNodes();
                }
                //
                if (exact || getFreeQuality == null ||
                    !FixPoorFreeFaces(edgeIds, edges, chainEdges, matchedSurfaceIds, protectedFaces,
                                      getFreeQuality, originalFreeQualities)) return true;
                foreach (GmshShellSpacingEdge edge in chainEdges)
                {
                    edge.RestoreNodes();
                    edge.ResetNodes();
                }
            }
            return false;
        }
        private static bool ProposeSpacing(List<GmshShellSpacingPatch> chain, GmshShellSpacingEdge[] chainEdges,
                                           bool exact)
        {
            HashSet<GmshShellSpacingEdge> known = new HashSet<GmshShellSpacingEdge>();
            foreach (GmshShellSpacingPatch patch in chain) patch.MarkSplitEdges(known);
            foreach (GmshShellSpacingPatch patch in chain) patch.CopyKnownSpacing(known);
            for (int step = 0; step < MaxRelaxationSteps; step++)
            {
                foreach (GmshShellSpacingPatch patch in chain)
                {
                    foreach (var pair in patch.Pairs)
                    {
                        double error = pair.Second.Coordinate - pair.First.Coordinate;
                        pair.First.AddError(error);
                        pair.Second.AddError(-error);
                    }
                }
                double change = 0;
                foreach (GmshShellSpacingEdge edge in chainEdges) change = Math.Max(change, edge.Relax());
                if (change <= RelaxationTolerance) break;
            }
            if (exact && GetMaximumLean(chain) > StraightLeanDegrees)
            {
                foreach (GmshShellSpacingEdge edge in chainEdges) edge.ResetNodes();
                return false;
            }
            //
            foreach (GmshShellSpacingEdge edge in chainEdges) edge.PrepareNodes(FractionTolerance);
            if (!chainEdges.Any(edge => edge.HasProposedNodes)) return false;
            foreach (GmshShellSpacingEdge edge in chainEdges) edge.ApplyNodes();
            return true;
        }
        private static bool HasRoundHole(SubmapFace face)
        {
            return face.Loops.Any(loop => loop.IsRound);
        }
        private static Dictionary<SubmapFace, double> GetGridsToCheck(List<GmshShellSpacingPatch> chain,
                                                                     Dictionary<int, GmshShellSpacingEdge> edges,
                                                                    bool exact)
        {
            Dictionary<SubmapFace, double> gridsToCheck = new Dictionary<SubmapFace, double>();
            foreach (GmshShellSpacingPatch patch in chain)
            {
                if (patch.Submap == null || !patch.EdgeIds.Any(id => edges[id].HasProposedNodes)) continue;
                if (!GmshSubmapMesher.Evaluate(patch.Submap, out double quality, out _, true))
                    gridsToCheck.Add(patch.Submap, double.NegativeInfinity);
                else if (exact ? HasRoundHole(patch.Submap) || quality < RightAngleQuality :
                                 quality < GmshShellGridQuality.MinScaledJacobian)
                    gridsToCheck.Add(patch.Submap, quality);
            }
            return gridsToCheck;
        }
        private static bool FixPoorFreeFaces(int[] edgeIds, Dictionary<int, GmshShellSpacingEdge> edges,
                                             GmshShellSpacingEdge[] chainEdges, HashSet<int> matchedSurfaceIds,
                                             HashSet<int> protectedFaces, Func<int, double?> getFreeQuality,
                                             Dictionary<int, double?> originalQualities)
        {
            SortedSet<int> freeFaces = new SortedSet<int>();
            foreach (int edgeId in edgeIds)
            {
                if (!edges[edgeId].HasProposedNodes) continue;
                Gmsh.Model.GetAdjacencies(1, edgeId, out int[] adjacent, out _);
                foreach (int id in adjacent)
                {
                    if (!matchedSurfaceIds.Contains(id) && !protectedFaces.Contains(id)) freeFaces.Add(id);
                }
            }
            Dictionary<int, double> poorFaces = new Dictionary<int, double>();
            foreach (int id in freeFaces)
            {
                double? quality = getFreeQuality(id);
                if (quality < GmshShellGridQuality.MinScaledJacobian) poorFaces.Add(id, quality.Value);
            }
            if (poorFaces.Keys.Any(id => !originalQualities.ContainsKey(id)))
            {
                foreach (GmshShellSpacingEdge edge in chainEdges) edge.RestoreNodes();
                foreach (int id in poorFaces.Keys)
                {
                    if (!originalQualities.ContainsKey(id)) originalQualities.Add(id, getFreeQuality(id));
                }
                foreach (GmshShellSpacingEdge edge in chainEdges) edge.ApplyNodes();
            }
            bool retry = false;
            foreach (var face in poorFaces)
            {
                if (face.Value < originalQualities[face.Key] - QualityTolerance)
                    retry |= protectedFaces.Add(face.Key);
            }
            return retry;
        }
        private static double GetMaximumLean(List<GmshShellSpacingPatch> chain)
        {
            double tangent = 0;
            foreach (GmshShellSpacingPatch patch in chain)
            {
                foreach (var pair in patch.Pairs)
                {
                    double offset = Math.Abs(pair.Second.Coordinate - pair.First.Coordinate);
                    if (offset == 0) continue;
                    double distance = GmshPoleChart.GetDistance(pair.First.Point, pair.Second.Point);
                    tangent = Math.Max(tangent, offset / distance);
                }
            }
            return Math.Atan(tangent) * 180 / Math.PI;
        }
    }
}
