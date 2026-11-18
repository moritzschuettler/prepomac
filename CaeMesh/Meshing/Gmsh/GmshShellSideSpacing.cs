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
    /// Matches boundary nodes across shared edges of four-sided and submapped shell faces without round holes.
    /// A chain of faces whose facing nodes lean by less than MinLeanDegrees is matched only when that makes it exact:
    /// moving no edge beside a face outside the chains, the lean falls below StraightLeanDegrees and every submapped
    /// face with a moved edge gets a grid of RightAngleQuality. Another chain is put back when the matching takes the
    /// grid of a submapped face or leaves it a poor grid with a worse corner.
    /// </summary>
    internal static class GmshShellSideSpacing
    {
        // Variables                                                                                                    
        private const int ShapeSamples = 9;
        private const double ShapeTolerance = 0.02;
        private const double MinLeanDegrees = 5;
        private const double StraightLeanDegrees = 0.1;
        private const double RightAngleQuality = 0.9999;
        private const double SubmapQualityTolerance = 0.02;
        private const double FractionTolerance = 1E-8;
        private const double RelaxationTolerance = 1E-10;
        private const double FreeFaceDepth = 3;
        private const int MaxRelaxationSteps = 512;


        // Methods                                                                                                      
        public static bool Match(IEnumerable<TransfiniteFace> shellFaces, IEnumerable<SubmapFace> submapFaces,
                                 HashSet<int> transfiniteSurfaceIds, HashSet<int> fixedSurfaceIds,
                                 Func<int, double> getSurfaceArea, Action meshEdges, bool recombine,
                                 Action<GmshShellGridQuality> tryFree)
        {
            List<GmshShellSpacingPatch> patches = new List<GmshShellSpacingPatch>();
            foreach (TransfiniteFace face in shellFaces.OrderBy(face => face.FaceId))
            {
                int[] faceEdges = face.SideEdgeIds.SelectMany(side => side).ToArray();
                if (faceEdges.Distinct().Count() != faceEdges.Length) continue;
                patches.Add(new GmshShellSpacingPatch(face, 0));
                patches.Add(new GmshShellSpacingPatch(face, 1));
            }
            foreach (SubmapFace face in submapFaces.OrderBy(face => face.FaceId))
            {
                GmshShellSpacingPatch patch = new GmshShellSpacingPatch(face);
                if (patch.EdgeIds.Distinct().Count() == patch.EdgeIds.Length) patches.Add(patch);
            }
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
            HashSet<int> turnEdges = new HashSet<int>(matched.Where(patch => patch.Submap != null)
                                                            .SelectMany(patch => patch.Submap.AllTurnEdgeIds));
            Action<int, bool> configureEdge = (edgeId, keepFreeFaces) =>
            {
                GmshShellSpacingEdge edge = edges[edgeId];
                if (turnEdges.Contains(edgeId)) return;
                Gmsh.Model.GetAdjacencies(1, edgeId, out int[] adjacent, out _);
                double room = FreeFaceDepth * edge.Length * edge.Length / (edge.NumOfNodes - 1);
                edge.SetMovable(!adjacent.Any(fixedSurfaceIds.Contains) &&
                               adjacent.Where(id => !matchedSurfaceIds.Contains(id))
                                       .All(id => !keepFreeFaces && getSurfaceArea(id) >= room));
            };
            //
            bool moved = false;
            foreach (List<GmshShellSpacingPatch> chain in chains) moved |= MatchChain(chain, edges, configureEdge);
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
                                       Action<int, bool> configureEdge)
        {
            double lean = GetMaximumLean(chain);
            if (lean == 0) return false;
            bool exact = lean < MinLeanDegrees;
            //
            int[] edgeIds = chain.SelectMany(patch => patch.EdgeIds).Distinct().OrderBy(id => id).ToArray();
            foreach (int edgeId in edgeIds) configureEdge(edgeId, exact);
            GmshShellSpacingEdge[] chainEdges = edgeIds.Select(id => edges[id]).ToArray();
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
            Dictionary<SubmapFace, double> poorGrids = new Dictionary<SubmapFace, double>();
            foreach (GmshShellSpacingPatch patch in chain)
            {
                if (patch.Submap == null || !patch.EdgeIds.Any(id => edges[id].HasProposedNodes)) continue;
                if (!GmshSubmapMesher.Evaluate(patch.Submap, out double quality, out _, true))
                    poorGrids.Add(patch.Submap, double.NegativeInfinity);
                else if (quality < (exact ? RightAngleQuality : GmshShellGridQuality.MinScaledJacobian))
                    poorGrids.Add(patch.Submap, quality);
            }
            if (poorGrids.Count == 0) return true;
            //
            foreach (GmshShellSpacingEdge edge in chainEdges) edge.RestoreNodes();
            if (exact)
            {
                foreach (GmshShellSpacingEdge edge in chainEdges) edge.ResetNodes();
                return false;
            }
            foreach (var entry in poorGrids)
            {
                if (GmshSubmapMesher.Evaluate(entry.Key, out double quality, out _, true) &&
                    entry.Value < quality - SubmapQualityTolerance)
                {
                    foreach (GmshShellSpacingEdge edge in chainEdges) edge.ResetNodes();
                    return false;
                }
            }
            foreach (GmshShellSpacingEdge edge in chainEdges) edge.ApplyNodes();
            return true;
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
