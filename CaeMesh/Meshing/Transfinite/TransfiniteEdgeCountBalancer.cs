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
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// Gives the paired sides of the structured faces the same number of elements, for the coarser mesh, and the sides of
    /// a three-sided face an even sum in which no side carries as many elements as the other two together. The balance
    /// is the interval assignment: every pair and every three-sided face is a row of one lp_solve problem, and the counts
    /// closest to the ones the mesh size asks for, lower ones before higher ones, keep them all. The numbers the user set
    /// are never changed, so sides they leave no way to agree are returned, for the meshing to be refused. Lowering
    /// evens out counts that are close: an edge that borders a free face keeps at least 0.8 of the count the mesh size
    /// asks for, since the free mesh beside it is made to that size, unless the numbers the user set leave no room.
    /// An element size the user set is kept like a number, within the range of counts that keeps it. No edge is lowered
    /// below the count that keeps its elements within the maximum element size, unless the numbers the user set leave
    /// no other way.
    /// </summary>
    public static class TransfiniteEdgeCountBalancer
    {
        // Variables
        private const int _timeoutSeconds = 20;
        private const double _freeFaceLowestShare = 0.8;
        // An element size the user set is kept by a number of elements up to this factor off the one it asks for, which
        // is as far as an edge beside a free face is lowered
        private const double _elementSizeCountFactor = 1.25;
        // and as close to that one as the faces allow, ahead of the counts the mesh size asks for
        private const double _elementSizeWeight = 1000;


        // Methods
        /// <summary>
        /// Sets edgeIdCount, which holds every edge of the pairs, to the balanced numbers of elements. naturalCounts
        /// are the numbers the mesh size asks for and freeFaceEdgeIds the edges that border a face meshed free.
        /// Returns the pairs left unequal, none when every pair is balanced, and the three-sided faces left without
        /// valid numbers in unbalancedTriples. sizedEdgeIdCounts holds the numbers of elements the element sizes the
        /// user set ask for, which the balance keeps within GetElementSizeCountRange, and maxSizeCounts the fewest
        /// elements that keep the elements of an edge within the maximum element size.
        /// </summary>
        public static List<TransfiniteSidePair> Balance(Dictionary<int, int> edgeIdCount,
                                                        Dictionary<int, int> naturalCounts,
                                                        HashSet<int> pinnedEdgeIds, List<TransfiniteSidePair> pairs,
                                                        List<TransfiniteSideTriple> triples,
                                                        out List<TransfiniteSideTriple> unbalancedTriples,
                                                        HashSet<int> freeFaceEdgeIds = null,
                                                        Dictionary<int, int> sizedEdgeIdCounts = null,
                                                        Dictionary<int, int> maxSizeCounts = null)
        {
            int count;
            List<TransfiniteSidePair> unbalanced;
            Dictionary<int, int> edgeIdLowestCount = new Dictionary<int, int>();
            if (freeFaceEdgeIds != null)
            {
                foreach (var edgeId in edgeIdCount.Keys)
                {
                    if (freeFaceEdgeIds.Contains(edgeId) && !pinnedEdgeIds.Contains(edgeId) &&
                        naturalCounts.TryGetValue(edgeId, out count))
                        edgeIdLowestCount[edgeId] = (int)Math.Ceiling(_freeFaceLowestShare * count - 1E-9);
                }
            }
            if (edgeIdLowestCount.Count > 0)
            {
                Dictionary<int, int> balancedCounts = new Dictionary<int, int>(edgeIdCount);
                if (BalanceBySolve(balancedCounts, naturalCounts, pinnedEdgeIds, edgeIdLowestCount, sizedEdgeIdCounts,
                                   maxSizeCounts, pairs, triples, out unbalanced, out unbalancedTriples) &&
                    unbalanced.Count == 0 && unbalancedTriples.Count == 0)
                {
                    foreach (var entry in balancedCounts) edgeIdCount[entry.Key] = entry.Value;
                    return unbalanced;
                }
            }
            // The numbers the user set leave no room for the lowest counts, so they give way
            if (!BalanceBySolve(edgeIdCount, naturalCounts, pinnedEdgeIds, new Dictionary<int, int>(), sizedEdgeIdCounts,
                                maxSizeCounts, pairs, triples, out unbalanced, out unbalancedTriples))
                throw new CaeException("lp_solve found no element counts for the structured faces.");
            return unbalanced;
        }
        /// <summary>
        /// The balance as one integer problem. A side pinned against another is a row no counts keep, and the relaxed
        /// problem, which gives such rows up at a high price, names it. False when lp_solve finds no counts at all.
        /// </summary>
        private static bool BalanceBySolve(Dictionary<int, int> edgeIdCount, Dictionary<int, int> naturalCounts,
                                           HashSet<int> pinnedEdgeIds, Dictionary<int, int> edgeIdLowestCount,
                                           Dictionary<int, int> sizedEdgeIdCounts, Dictionary<int, int> maxSizeCounts,
                                           List<TransfiniteSidePair> pairs, List<TransfiniteSideTriple> triples,
                                           out List<TransfiniteSidePair> unbalanced,
                                           out List<TransfiniteSideTriple> unbalancedTriples)
        {
            unbalanced = null;
            unbalancedTriples = null;
            int count;
            IntervalProblem problem = new IntervalProblem(edgeId => naturalCounts.TryGetValue(edgeId, out count) ? count :
                                                                    edgeIdCount.TryGetValue(edgeId, out count) ? count : 1);
            int minCount;
            int maxCount;
            foreach (var edgeId in edgeIdCount.Keys)
            {
                if (pinnedEdgeIds.Contains(edgeId)) problem.FixEdge(edgeId, edgeIdCount[edgeId]);
                else if (sizedEdgeIdCounts != null && sizedEdgeIdCounts.TryGetValue(edgeId, out count))
                {
                    GetElementSizeCountRange(count, out minCount, out maxCount);
                    if (edgeIdLowestCount.TryGetValue(edgeId, out count)) minCount = Math.Max(minCount, Math.Min(count, maxCount));
                    problem.SetMinimum(edgeId, minCount);
                    problem.SetMaximum(edgeId, maxCount);
                    problem.SetWeight(edgeId, _elementSizeWeight);
                }
                else if (edgeIdLowestCount.TryGetValue(edgeId, out count)) problem.SetMinimum(edgeId, count);
                else problem.AddEdge(edgeId);
                if (!pinnedEdgeIds.Contains(edgeId) && maxSizeCounts != null &&
                    maxSizeCounts.TryGetValue(edgeId, out count)) problem.SetMaxSizeCount(edgeId, count);
            }
            foreach (var pair in pairs)
                problem.AddEqualSums("Surface " + pair.FaceId, IntervalRowTierEnum.Hard, pair.SideA, pair.SideB, pair.FaceId);
            foreach (var triple in triples)
                problem.AddThreeSided("Surface " + triple.FaceId, IntervalRowTierEnum.Hard, triple.Sides, triple.FaceId);
            //
            IntervalSolution solution = problem.Solve(false, _timeoutSeconds);
            if (!solution.HasCounts)
            {
                if (solution.Status != LpSolve.Infeasible) return false;
                solution = problem.Solve(true, _timeoutSeconds);
                if (!solution.HasCounts) return false;
            }
            foreach (var entry in solution.EdgeIdNumOfElements)
            {
                if (edgeIdCount.ContainsKey(entry.Key) && !pinnedEdgeIds.Contains(entry.Key)) edgeIdCount[entry.Key] = entry.Value;
            }
            // The rows come in the order of the pairs and then of the triples
            Func<int, int> getCount = edgeId => edgeIdCount.TryGetValue(edgeId, out count) ? count :
                                                solution.EdgeIdNumOfElements[edgeId];
            unbalanced = new List<TransfiniteSidePair>();
            unbalancedTriples = new List<TransfiniteSideTriple>();
            for (int i = 0; i < problem.Rows.Count; i++)
            {
                if (problem.Rows[i].IsSatisfied(getCount)) continue;
                if (i < pairs.Count) unbalanced.Add(pairs[i]);
                else unbalancedTriples.Add(triples[i - pairs.Count]);
            }
            return true;
        }
        /// <summary>
        /// The numbers of elements that keep the element size the user set on an edge, which asks for numOfElements:
        /// up to _elementSizeCountFactor off it, and one element off it at least.
        /// </summary>
        public static void GetElementSizeCountRange(int numOfElements, out int minCount, out int maxCount)
        {
            minCount = Math.Max(1, Math.Min(numOfElements - 1,
                                            (int)Math.Ceiling(numOfElements / _elementSizeCountFactor - 1E-9)));
            maxCount = Math.Max(numOfElements + 1, (int)Math.Floor(numOfElements * _elementSizeCountFactor + 1E-9));
        }
    }
}
