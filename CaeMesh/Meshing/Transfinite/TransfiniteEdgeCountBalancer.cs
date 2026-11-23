// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    /// is the interval assignment: every pair and every three-sided face is a row of one HiGHS problem, and the counts
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
        /// elements that keep the elements of an edge within the maximum element size. The edges of edgeIdMultiples are
        /// given a number of elements that is a multiple of theirs, 2 for an even one, where the numbers the user set
        /// allow it; one they do not is left to the caller.
        /// </summary>
        public static List<TransfiniteSidePair> Balance(Dictionary<int, int> edgeIdCount,
                                                        Dictionary<int, int> naturalCounts,
                                                        HashSet<int> pinnedEdgeIds, List<TransfiniteSidePair> pairs,
                                                        List<TransfiniteSideTriple> triples,
                                                        out List<TransfiniteSideTriple> unbalancedTriples,
                                                        HashSet<int> freeFaceEdgeIds = null,
                                                        Dictionary<int, int> sizedEdgeIdCounts = null,
                                                        Dictionary<int, int> maxSizeCounts = null,
                                                        Dictionary<int, int> edgeIdMultiples = null)
        {
            List<SubmapSeparation> unbalancedSeparations;
            return Balance(edgeIdCount, naturalCounts, pinnedEdgeIds, pairs, triples, null, out unbalancedTriples,
                           out unbalancedSeparations, freeFaceEdgeIds, sizedEdgeIdCounts, maxSizeCounts, edgeIdMultiples);
        }
        /// <summary>
        /// The balance with the separations of the submapped faces as well, each a row that keeps two sides of a face
        /// apart in its lattice, with the tier of the pairs; the ones left unkept are returned in
        /// unbalancedSeparations. Every one of evenSides, the edges around a face meshed free and then turned into quads
        /// only, is asked for an even total at the weakest tier, which gives way to everything else.
        /// Every one of structuredEvenSides, the rim of a structured cap, is asked for an even total of
        /// minStructuredEvenSum elements at least, with the tier of the pairs. Every one of evenLoops, the loop of a
        /// lens face, is asked for an even total, and every one of minSumSides, a side of a lens face, for minSideSum
        /// elements at least, both with the tier of the pairs. Every one of equalSumSides, the two sides of a thin lens
        /// face, is asked for equal totals at the preferred tier, which gives way to the rows of the pairs and to the
        /// numbers the user set; such a row left unkept is not returned. Every one of minSumLoops, the loop of a lens
        /// face with one corner or none, is asked for minLoopSum elements at least, with the tier of the pairs.
        /// The mirror planes of the part ask for the rows of mirrorRows (MirrorCountRows), at the preferred tier, so
        /// a mirrored mesh never costs a structured face or a number the user set; the pieces of the edges that are
        /// split for the planes are counted like edges, each with the number it aims at, and get their numbers back.
        /// Every one of roundHoles, the edges of a round hole of a submapped face, is asked for a multiple of four, and
        /// every one of raisedRoundHoles, a round hole whose face makes a poor grid on fewer, for
        /// SubmapFace.MinNumberOfElementsRound elements at least, both with the tier of the pairs.
        /// </summary>
        public static List<TransfiniteSidePair> Balance(Dictionary<int, int> edgeIdCount,
                                                        Dictionary<int, int> naturalCounts,
                                                        HashSet<int> pinnedEdgeIds, List<TransfiniteSidePair> pairs,
                                                        List<TransfiniteSideTriple> triples,
                                                        List<SubmapSeparation> separations,
                                                        out List<TransfiniteSideTriple> unbalancedTriples,
                                                        out List<SubmapSeparation> unbalancedSeparations,
                                                        HashSet<int> freeFaceEdgeIds = null,
                                                        Dictionary<int, int> sizedEdgeIdCounts = null,
                                                        Dictionary<int, int> maxSizeCounts = null,
                                                        Dictionary<int, int> edgeIdMultiples = null,
                                                        List<int[]> evenSides = null,
                                                        List<int[]> structuredEvenSides = null,
                                                        int minStructuredEvenSum = 0, List<int[]> evenLoops = null,
                                                        List<int[]> minSumSides = null, int minSideSum = 0,
                                                        List<int[][]> equalSumSides = null,
                                                        List<int[]> minSumLoops = null, int minLoopSum = 0,
                                                        MirrorCountRows mirrorRows = null, List<int[]> roundHoles = null,
                                                        List<int[]> raisedRoundHoles = null)
        {
            if (separations == null) separations = new List<SubmapSeparation>();
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
                                   maxSizeCounts, pairs, triples, separations, edgeIdMultiples, evenSides, structuredEvenSides,
                                   minStructuredEvenSum, evenLoops, minSumSides, minSideSum, equalSumSides, minSumLoops,
                                   minLoopSum, mirrorRows, roundHoles, raisedRoundHoles, out unbalanced,
                                   out unbalancedTriples, out unbalancedSeparations) &&
                    unbalanced.Count == 0 && unbalancedTriples.Count == 0 && unbalancedSeparations.Count == 0)
                {
                    foreach (var entry in balancedCounts) edgeIdCount[entry.Key] = entry.Value;
                    return unbalanced;
                }
            }
            // The numbers the user set leave no room for the lowest counts, so they give way
            if (!BalanceBySolve(edgeIdCount, naturalCounts, pinnedEdgeIds, new Dictionary<int, int>(), sizedEdgeIdCounts,
                                maxSizeCounts, pairs, triples, separations, edgeIdMultiples, evenSides, structuredEvenSides,
                                minStructuredEvenSum, evenLoops, minSumSides, minSideSum, equalSumSides, minSumLoops,
                                minLoopSum, mirrorRows, roundHoles, raisedRoundHoles, out unbalanced,
                                out unbalancedTriples, out unbalancedSeparations))
                throw new CaeException("HiGHS found no element counts for the structured faces.");
            return unbalanced;
        }
        /// <summary>
        /// The balance as one integer problem. A side pinned against another is a row no counts keep, and the relaxed
        /// problem, which gives such rows up at a high price, names it. False when HiGHS finds no counts at all.
        /// </summary>
        private static bool BalanceBySolve(Dictionary<int, int> edgeIdCount, Dictionary<int, int> naturalCounts,
                                           HashSet<int> pinnedEdgeIds, Dictionary<int, int> edgeIdLowestCount,
                                           Dictionary<int, int> sizedEdgeIdCounts, Dictionary<int, int> maxSizeCounts,
                                           List<TransfiniteSidePair> pairs, List<TransfiniteSideTriple> triples,
                                           List<SubmapSeparation> separations, Dictionary<int, int> edgeIdMultiples,
                                           List<int[]> evenSides, List<int[]> structuredEvenSides,
                                           int minStructuredEvenSum, List<int[]> evenLoops, List<int[]> minSumSides,
                                           int minSideSum, List<int[][]> equalSumSides, List<int[]> minSumLoops,
                                           int minLoopSum, MirrorCountRows mirrorRows, List<int[]> roundHoles,
                                           List<int[]> raisedRoundHoles,
                                           out List<TransfiniteSidePair> unbalanced,
                                           out List<TransfiniteSideTriple> unbalancedTriples,
                                           out List<SubmapSeparation> unbalancedSeparations)
        {
            unbalanced = null;
            unbalancedTriples = null;
            unbalancedSeparations = null;
            int count;
            IntervalProblem problem = new IntervalProblem(edgeId =>
                naturalCounts.TryGetValue(edgeId, out count) ? count :
                edgeIdCount.TryGetValue(edgeId, out count) ? count :
                mirrorRows != null && mirrorRows.PieceTargets.TryGetValue(edgeId, out count) ? count : 1);
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
            foreach (var separation in separations)
                problem.AddAtLeastDifference("Surface " + separation.FaceId, IntervalRowTierEnum.Hard,
                                             separation.TowardsEdgeIds, separation.BackEdgeIds, separation.Bound,
                                             separation.FaceId);
            if (edgeIdMultiples != null)
            {
                int multiple;
                foreach (var edgeId in edgeIdCount.Keys)
                {
                    if (edgeIdMultiples.TryGetValue(edgeId, out multiple) && multiple > 1)
                        problem.AddEvenSum("Edge " + edgeId, IntervalRowTierEnum.Hard, new int[] { edgeId }, multiple);
                }
            }
            if (evenSides != null)
            {
                for (int i = 0; i < evenSides.Count; i++)
                    problem.AddEvenSum("Even total " + i, IntervalRowTierEnum.Preferred, evenSides[i]);
            }
            if (structuredEvenSides != null)
            {
                for (int i = 0; i < structuredEvenSides.Count; i++)
                {
                    problem.AddEvenSum("Structured even total " + i, IntervalRowTierEnum.Hard, structuredEvenSides[i]);
                    if (minStructuredEvenSum > 0)
                        problem.AddAtLeastSum("Structured even total " + i, IntervalRowTierEnum.Hard, structuredEvenSides[i],
                                              minStructuredEvenSum);
                }
            }
            if (evenLoops != null)
            {
                for (int i = 0; i < evenLoops.Count; i++)
                    problem.AddEvenSum("Even loop " + i, IntervalRowTierEnum.Hard, evenLoops[i]);
            }
            if (minSumSides != null)
            {
                for (int i = 0; i < minSumSides.Count; i++)
                    problem.AddAtLeastSum("Least side " + i, IntervalRowTierEnum.Hard, minSumSides[i], minSideSum);
            }
            if (equalSumSides != null)
            {
                for (int i = 0; i < equalSumSides.Count; i++)
                    problem.AddEqualSums("Equal sides " + i, IntervalRowTierEnum.Preferred, equalSumSides[i][0],
                                         equalSumSides[i][1]);
            }
            if (minSumLoops != null)
            {
                for (int i = 0; i < minSumLoops.Count; i++)
                    problem.AddAtLeastSum("Least loop " + i, IntervalRowTierEnum.Hard, minSumLoops[i], minLoopSum);
            }
            if (roundHoles != null)
            {
                for (int i = 0; i < roundHoles.Count; i++)
                    problem.AddEvenSum("Round hole " + i, IntervalRowTierEnum.Hard, roundHoles[i], 4);
            }
            if (raisedRoundHoles != null)
            {
                for (int i = 0; i < raisedRoundHoles.Count; i++)
                    problem.AddAtLeastSum("Least round hole " + i, IntervalRowTierEnum.Hard, raisedRoundHoles[i],
                                          SubmapFace.MinNumberOfElementsRound);
            }
            if (mirrorRows != null)
            {
                for (int i = 0; i < mirrorRows.EqualSides.Count; i++)
                    problem.AddEqualSums("Mirrored edges " + i, IntervalRowTierEnum.Preferred,
                                         mirrorRows.EqualSides[i][0], mirrorRows.EqualSides[i][1]);
                for (int i = 0; i < mirrorRows.MultipleSides.Count; i++)
                    problem.AddEvenSum("Mirrored total " + i, IntervalRowTierEnum.Preferred,
                                       mirrorRows.MultipleSides[i].Item1, mirrorRows.MultipleSides[i].Item2);
            }
            //
            IntervalSolution solution = problem.Solve(false, _timeoutSeconds);
            if (!solution.HasCounts)
            {
                if (solution.Status != Highs.Infeasible) return false;
                solution = problem.Solve(true, _timeoutSeconds);
                if (!solution.HasCounts) return false;
            }
            if (mirrorRows != null) mirrorRows.PieceCounts.Clear();
            foreach (var entry in solution.EdgeIdNumOfElements)
            {
                if (edgeIdCount.ContainsKey(entry.Key) && !pinnedEdgeIds.Contains(entry.Key)) edgeIdCount[entry.Key] = entry.Value;
                else if (mirrorRows != null && mirrorRows.PieceTargets.ContainsKey(entry.Key))
                    mirrorRows.PieceCounts[entry.Key] = entry.Value;
            }
            // The rows come in the order of the pairs, the triples and the separations, followed by the even edges
            Func<int, int> getCount = edgeId => edgeIdCount.TryGetValue(edgeId, out count) ? count :
                                                solution.EdgeIdNumOfElements[edgeId];
            unbalanced = new List<TransfiniteSidePair>();
            unbalancedTriples = new List<TransfiniteSideTriple>();
            unbalancedSeparations = new List<SubmapSeparation>();
            for (int i = 0; i < pairs.Count + triples.Count + separations.Count; i++)
            {
                if (problem.Rows[i].IsSatisfied(getCount)) continue;
                if (i < pairs.Count) unbalanced.Add(pairs[i]);
                else if (i < pairs.Count + triples.Count) unbalancedTriples.Add(triples[i - pairs.Count]);
                else unbalancedSeparations.Add(separations[i - pairs.Count - triples.Count]);
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
