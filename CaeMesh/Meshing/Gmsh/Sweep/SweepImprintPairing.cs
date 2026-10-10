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
    /// Pairs the edges splitting the target of a block that is not a prism with the edges splitting its source, for
    /// the detection and the mesher alike. Two such edges are a pair when each end of the target edge lies on a wall
    /// with one end of the source edge, the two ends on different walls: the two tangency lines of a bent plate on
    /// its front and on its back run between the same two end walls. An end wall cut at a slant, or along a curve,
    /// leaves the two lines of a pair of different lengths, and they are then no translation of each other; the
    /// sweep lines pinned between them, node by node, are straight all the same. Of the pairs the walls allow, the
    /// nearest ends are taken first, one source edge for every target edge.
    /// </summary>
    public static class SweepImprintPairing
    {
        // Methods
        /// <summary>
        /// The source edge of every target edge, or null when a target edge has none. The ends are the two vertices
        /// of every edge, the walls of a vertex are the walls it lies on.
        /// </summary>
        public static Dictionary<int, int> PairByWalls(Dictionary<int, int[]> targetEdgeIdEnds,
                                                       Dictionary<int, int[]> sourceEdgeIdEnds,
                                                       Func<int, HashSet<int>> getVertexWallIds,
                                                       Func<int, double[]> getVertexCoor)
        {
            if (targetEdgeIdEnds.Count == 0 || targetEdgeIdEnds.Count != sourceEdgeIdEnds.Count) return null;
            //
            List<Tuple<double, int, int>> candidates = new List<Tuple<double, int, int>>();
            foreach (var target in targetEdgeIdEnds)
            {
                if (target.Value.Length != 2) return null;
                foreach (var source in sourceEdgeIdEnds)
                {
                    if (source.Value.Length != 2) continue;
                    double distance = double.MaxValue;
                    for (int flip = 0; flip < 2; flip++)
                    {
                        int sourceStart = source.Value[flip];
                        int sourceEnd = source.Value[1 - flip];
                        HashSet<int> startWallIds = new HashSet<int>(getVertexWallIds(target.Value[0]));
                        startWallIds.IntersectWith(getVertexWallIds(sourceStart));
                        HashSet<int> endWallIds = new HashSet<int>(getVertexWallIds(target.Value[1]));
                        endWallIds.IntersectWith(getVertexWallIds(sourceEnd));
                        if (startWallIds.Count == 0 || endWallIds.Count == 0) continue;
                        if (startWallIds.Count == 1 && endWallIds.SetEquals(startWallIds)) continue;
                        //
                        distance = Math.Min(distance, Distance(getVertexCoor(target.Value[0]), getVertexCoor(sourceStart)) +
                                                      Distance(getVertexCoor(target.Value[1]), getVertexCoor(sourceEnd)));
                    }
                    if (distance < double.MaxValue) candidates.Add(new Tuple<double, int, int>(distance, target.Key, source.Key));
                }
            }
            //
            Dictionary<int, int> targetEdgeIdSourceEdgeId = new Dictionary<int, int>();
            HashSet<int> usedSourceEdgeIds = new HashSet<int>();
            foreach (var candidate in candidates.OrderBy(entry => entry.Item1))
            {
                if (targetEdgeIdSourceEdgeId.ContainsKey(candidate.Item2) || usedSourceEdgeIds.Contains(candidate.Item3))
                    continue;
                targetEdgeIdSourceEdgeId.Add(candidate.Item2, candidate.Item3);
                usedSourceEdgeIds.Add(candidate.Item3);
            }
            return targetEdgeIdSourceEdgeId.Count == targetEdgeIdEnds.Count ? targetEdgeIdSourceEdgeId : null;
        }
        private static double Distance(double[] a, double[] b)
        {
            return Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]) + (a[2] - b[2]) * (a[2] - b[2]));
        }
    }
}
