// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// Gives the paired sides of the structured faces the same number of elements, for the coarser mesh. Edges that
    /// are whole sides opposite each other must carry one number, so they are joined into a class: the number the user
    /// set on one of them, or the lowest the mesh size asks for on any. The sides that are chains of edges are then
    /// balanced over these classes, by lowering the larger side at its class that is the most refined for its size;
    /// a side is raised only where the other cannot be lowered, and a class raised so is not lowered below that
    /// again. The sides of a three-sided face are balanced the same way, to an even sum in which no side carries as
    /// many elements as the other two together. The numbers the user set are never changed, so sides they leave no way to agree are returned, for the
    /// meshing to be refused.
    /// </summary>
    public static class TransfiniteEdgeCountBalancer
    {
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
            // Classes of the edges that are whole opposite sides
            Dictionary<int, int> parent = edgeIdCount.Keys.ToDictionary(edgeId => edgeId, edgeId => edgeId);
            Func<int, int> find = null;
            find = edgeId => parent[edgeId] == edgeId ? edgeId : (parent[edgeId] = find(parent[edgeId]));
            foreach (var pair in pairs)
            {
                if (pair.SideA.Length == 1 && pair.SideB.Length == 1) parent[find(pair.SideA[0])] = find(pair.SideB[0]);
            }
            Dictionary<int, List<int>> classEdgeIds = edgeIdCount.Keys.GroupBy(find)
                                                                 .ToDictionary(group => group.Key, group => group.ToList());
            // The number of every class, which the user fixes when an edge of it is pinned
            Dictionary<int, int> value = new Dictionary<int, int>();
            Dictionary<int, int> floor = new Dictionary<int, int>();
            Dictionary<int, double> naturalSize = new Dictionary<int, double>();
            HashSet<int> fixedClasses = new HashSet<int>();
            HashSet<int> conflictingClasses = new HashSet<int>();
            foreach (var entry in classEdgeIds)
            {
                int[] pinnedValues = entry.Value.Where(pinnedEdgeIds.Contains).Select(edgeId => edgeIdCount[edgeId])
                                                .Distinct().ToArray();
                int[] naturals = entry.Value.Select(edgeId => Math.Max(1, naturalCounts.TryGetValue(edgeId, out int n) ?
                                                                          n : edgeIdCount[edgeId])).ToArray();
                naturalSize[entry.Key] = naturals.Average();
                if (pinnedValues.Length > 0)
                {
                    value[entry.Key] = pinnedValues.Max();
                    fixedClasses.Add(entry.Key);
                    if (pinnedValues.Length > 1) conflictingClasses.Add(entry.Key);
                }
                else value[entry.Key] = naturals.Min();
                floor[entry.Key] = 1;
            }
            // The sides that are chains of edges, balanced over the classes
            List<TransfiniteSidePair> chainPairs = pairs.Where(pair => pair.SideA.Length > 1 || pair.SideB.Length > 1)
                                                        .ToList();
            long maxIterations = 1000L * Math.Max(1, chainPairs.Count + triples.Count) +
                                 10L * value.Values.Sum(count => (long)count);
            bool changed = true;
            for (long iteration = 0; changed && iteration < maxIterations; iteration++)
            {
                changed = false;
                foreach (var pair in chainPairs)
                {
                    int sumA = pair.SideA.Sum(edgeId => value[find(edgeId)]);
                    int sumB = pair.SideB.Sum(edgeId => value[find(edgeId)]);
                    if (sumA == sumB) continue;
                    //
                    int[] larger = sumA > sumB ? pair.SideA : pair.SideB;
                    int[] smaller = sumA > sumB ? pair.SideB : pair.SideA;
                    // A class on both sides changes them both, so only what it does to the difference counts
                    Func<int, int> net = classId => larger.Count(edgeId => find(edgeId) == classId) -
                                                    smaller.Count(edgeId => find(edgeId) == classId);
                    int[] lowerable = larger.Select(find).Distinct()
                                            .Where(classId => !fixedClasses.Contains(classId) &&
                                                              value[classId] > floor[classId] && net(classId) > 0)
                                            .ToArray();
                    if (lowerable.Length > 0)
                    {
                        int classId = lowerable.OrderByDescending(id => value[id] / naturalSize[id])
                                               .ThenByDescending(id => value[id]).First();
                        value[classId]--;
                        changed = true;
                        continue;
                    }
                    int[] raisable = smaller.Select(find).Distinct()
                                            .Where(classId => !fixedClasses.Contains(classId) && net(classId) < 0)
                                            .ToArray();
                    if (raisable.Length > 0)
                    {
                        int classId = raisable.OrderBy(id => value[id] / naturalSize[id]).ThenBy(id => value[id]).First();
                        value[classId]++;
                        floor[classId] = value[classId];
                        changed = true;
                    }
                }
                foreach (var triple in triples)
                {
                    if (BalanceTriple(triple, find, value, floor, fixedClasses, naturalSize)) changed = true;
                }
            }
            //
            foreach (var edgeId in edgeIdCount.Keys.ToArray())
            {
                if (!pinnedEdgeIds.Contains(edgeId)) edgeIdCount[edgeId] = value[find(edgeId)];
            }
            unbalancedTriples = triples.Where(triple => !TransfiniteSideTriple.AreCountsValid(
                triple.Sides.Select(side => side.Sum(edgeId => edgeIdCount[edgeId])).ToArray())).ToList();
            return pairs.Where(pair => conflictingClasses.Contains(find(pair.SideA[0])) ||
                                       pair.SideA.Sum(edgeId => edgeIdCount[edgeId]) !=
                                       pair.SideB.Sum(edgeId => edgeIdCount[edgeId])).ToList();
        }
        /// <summary>
        /// One step towards valid numbers on a three-sided face: a side with too many elements against the other two
        /// is lowered, or the others are raised, and an odd sum is lowered by one, or raised. Returns whether a number
        /// changed.
        /// </summary>
        private static bool BalanceTriple(TransfiniteSideTriple triple, Func<int, int> find, Dictionary<int, int> value,
                                          Dictionary<int, int> floor, HashSet<int> fixedClasses,
                                          Dictionary<int, double> naturalSize)
        {
            int[] sums = triple.Sides.Select(side => side.Sum(edgeId => value[find(edgeId)])).ToArray();
            if (TransfiniteSideTriple.AreCountsValid(sums)) return false;
            //
            Func<int, bool> canLower = classId => !fixedClasses.Contains(classId) && value[classId] > floor[classId];
            Func<IEnumerable<int>, int> mostRefined = classIds =>
                classIds.OrderByDescending(id => value[id] / naturalSize[id]).ThenByDescending(id => value[id]).First();
            Func<IEnumerable<int>, int> leastRefined = classIds =>
                classIds.OrderBy(id => value[id] / naturalSize[id]).ThenBy(id => value[id]).First();
            // The inner line opposite a side gets no element when the side carries more than the others less two
            for (int k = 0; k < 3; k++)
            {
                if (sums[k] <= sums[(k + 1) % 3] + sums[(k + 2) % 3] - 2) continue;
                // Only a class on this side alone shortens it against the others
                HashSet<int> sideClassIds = new HashSet<int>(triple.Sides[k].Select(find));
                HashSet<int> otherClassIds = new HashSet<int>(triple.Sides[(k + 1) % 3].Concat(triple.Sides[(k + 2) % 3])
                                                                                       .Select(find));
                int[] lowerable = sideClassIds.Where(id => canLower(id) && !otherClassIds.Contains(id)).ToArray();
                if (lowerable.Length > 0)
                {
                    value[mostRefined(lowerable)]--;
                    return true;
                }
                int[] raisable = otherClassIds.Where(id => !fixedClasses.Contains(id) && !sideClassIds.Contains(id))
                                              .ToArray();
                if (raisable.Length > 0)
                {
                    int classId = leastRefined(raisable);
                    value[classId]++;
                    floor[classId] = value[classId];
                    return true;
                }
                return false;
            }
            // An odd sum, changed by a class that occurs an odd number of times on the face
            int[] allEdgeIds = triple.Sides.SelectMany(side => side).ToArray();
            int[] oddClassIds = allEdgeIds.Select(find).Distinct()
                                          .Where(id => allEdgeIds.Count(edgeId => find(edgeId) == id) % 2 == 1).ToArray();
            int[] lowerableOdd = oddClassIds.Where(canLower).ToArray();
            if (lowerableOdd.Length > 0)
            {
                value[mostRefined(lowerableOdd)]--;
                return true;
            }
            int[] raisableOdd = oddClassIds.Where(id => !fixedClasses.Contains(id)).ToArray();
            if (raisableOdd.Length > 0)
            {
                int classId = leastRefined(raisableOdd);
                value[classId]++;
                floor[classId] = value[classId];
                return true;
            }
            return false;
        }
    }
}
