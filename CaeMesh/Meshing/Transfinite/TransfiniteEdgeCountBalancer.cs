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
    /// are never changed, so sides they leave no way to agree are returned, for the meshing to be refused.
    /// </summary>
    public static class TransfiniteEdgeCountBalancer
    {
        // Variables
        private const int _timeoutSeconds = 20;


        // Methods
        /// <summary>
        /// Sets edgeIdCount, which holds every edge of the pairs, to the balanced numbers of elements. naturalCounts
        /// are the numbers the mesh size asks for. Returns the pairs left unequal, none when every pair is balanced,
        /// and the three-sided faces left without valid numbers in unbalancedTriples.
        /// </summary>
        public static List<TransfiniteSidePair> Balance(Dictionary<int, int> edgeIdCount,
                                                        Dictionary<int, int> naturalCounts,
                                                        HashSet<int> pinnedEdgeIds, List<TransfiniteSidePair> pairs,
                                                        List<TransfiniteSideTriple> triples,
                                                        out List<TransfiniteSideTriple> unbalancedTriples)
        {
            List<TransfiniteSidePair> unbalanced;
            if (!BalanceBySolve(edgeIdCount, naturalCounts, pinnedEdgeIds, pairs, triples, out unbalanced,
                                out unbalancedTriples))
                throw new CaeException("lp_solve found no element counts for the structured faces.");
            return unbalanced;
        }
        /// <summary>
        /// The balance as one integer problem. A side pinned against another is a row no counts keep, and the relaxed
        /// problem, which gives such rows up at a high price, names it. False when lp_solve finds no counts at all.
        /// </summary>
        private static bool BalanceBySolve(Dictionary<int, int> edgeIdCount, Dictionary<int, int> naturalCounts,
                                           HashSet<int> pinnedEdgeIds, List<TransfiniteSidePair> pairs,
                                           List<TransfiniteSideTriple> triples, out List<TransfiniteSidePair> unbalanced,
                                           out List<TransfiniteSideTriple> unbalancedTriples)
        {
            unbalanced = null;
            unbalancedTriples = null;
            int count;
            IntervalProblem problem = new IntervalProblem(edgeId => naturalCounts.TryGetValue(edgeId, out count) ? count :
                                                                    edgeIdCount.TryGetValue(edgeId, out count) ? count : 1);
            foreach (var edgeId in edgeIdCount.Keys)
            {
                if (pinnedEdgeIds.Contains(edgeId)) problem.FixEdge(edgeId, edgeIdCount[edgeId]);
                else problem.AddEdge(edgeId);
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
    }
}
