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
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// One wall of a sweep block, with its edges sorted into the three roles they can play.
    /// </summary>
    public class SweepWall
    {
        // Variables
        private readonly int _surfaceId;
        private readonly int[] _sourceEdgeIds;
        private readonly int[] _targetEdgeIds;
        private readonly int[] _directionEdgeIds;


        // Properties
        public int SurfaceId { get { return _surfaceId; } }
        /// <summary>Edges shared with the source surfaces - more than one after a source split.</summary>
        public int[] SourceEdgeIds { get { return _sourceEdgeIds; } }
        /// <summary>Edges shared with the target surfaces.</summary>
        public int[] TargetEdgeIds { get { return _targetEdgeIds; } }
        /// <summary>The remaining edges, which follow the sweep direction and are shared with the neighbouring walls.</summary>
        public int[] DirectionEdgeIds { get { return _directionEdgeIds; } }
        /// <summary>Whether either side of the wall is built from more than one edge.</summary>
        public bool HasCompositeSide { get { return _sourceEdgeIds.Length > 1 || _targetEdgeIds.Length > 1; } }


        // Constructors
        public SweepWall(int surfaceId, int[] sourceEdgeIds, int[] targetEdgeIds, int[] directionEdgeIds)
        {
            _surfaceId = surfaceId;
            _sourceEdgeIds = sourceEdgeIds;
            _targetEdgeIds = targetEdgeIds;
            _directionEdgeIds = directionEdgeIds;
        }
    }
    /// <summary>
    /// Lets a sweep wall keep its structured grid after the source face has been split.
    ///
    /// Splitting a source face to make it easier to mesh puts a new vertex on the source boundary,
    /// which splits the edge the wall shares with it. The wall then has five or more edges and
    /// gmsh's transfinite algorithm can no longer guess its corners, even though the wall still has
    /// only four real ones. Two things fix that, and both are needed:
    ///
    ///   - Nominate the four corners explicitly. The corners are the ends of the two edges that
    ///     follow the sweep direction; every extra vertex sits partway along a side and is not a
    ///     corner. The order the corners are given in does not matter.
    ///   - Make the element counts on opposite sides agree. gmsh raises an error rather than
    ///     falling back when they do not, and the sweep needs the agreement anyway: the source
    ///     boundary loop and the target boundary loop must carry the same number of nodes for the
    ///     sweep lines to pair up one to one.
    ///
    /// None of this assumes a side is split in two. A side may be split into any number of edges,
    /// and both sides may be split at once.
    ///
    /// The same situation arises without any source splitting once many-to-one sweeping lands: an
    /// imprint loop, where a new source face joins partway along the body, cuts a wall side exactly
    /// this way.
    /// </summary>
    public static class SweepWallTopology
    {
        // Methods
        /// <summary>
        /// Sorts the edges of every wall into the source side, the target side and the edges that
        /// follow the sweep direction.
        /// </summary>
        public static List<SweepWall> Classify(IEnumerable<int> sourceSurfaceIds, IEnumerable<int> wallSurfaceIds,
                                               IEnumerable<int> targetSurfaceIds,
                                               Dictionary<int, int[]> surfaceIdEdgeIds)
        {
            int[] edgeIds;
            HashSet<int> sourceEdgeIds = new HashSet<int>();
            HashSet<int> targetEdgeIds = new HashSet<int>();
            foreach (var id in sourceSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) sourceEdgeIds.UnionWith(edgeIds);
            }
            foreach (var id in targetSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) targetEdgeIds.UnionWith(edgeIds);
            }
            //
            List<SweepWall> walls = new List<SweepWall>();
            foreach (var wallSurfaceId in wallSurfaceIds)
            {
                if (!surfaceIdEdgeIds.TryGetValue(wallSurfaceId, out edgeIds)) continue;
                //
                List<int> onSource = new List<int>();
                List<int> onTarget = new List<int>();
                List<int> onDirection = new List<int>();
                foreach (var edgeId in edgeIds.Distinct())
                {
                    if (sourceEdgeIds.Contains(edgeId)) onSource.Add(edgeId);
                    else if (targetEdgeIds.Contains(edgeId)) onTarget.Add(edgeId);
                    else onDirection.Add(edgeId);
                }
                walls.Add(new SweepWall(wallSurfaceId, onSource.ToArray(), onTarget.ToArray(), onDirection.ToArray()));
            }
            //
            return walls;
        }
        /// <summary>
        /// The four corner vertices of a wall, or null when they cannot be identified. They are the
        /// end vertices of the two edges that follow the sweep direction; a wall that does not have
        /// exactly two of those, or whose direction edges meet, is left for gmsh to work out.
        /// </summary>
        public static int[] GetCorners(SweepWall wall)
        {
            if (wall.DirectionEdgeIds.Length != 2) return null;
            //
            int[] vertexIds;
            HashSet<int> corners = new HashSet<int>();
            foreach (var edgeId in wall.DirectionEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                corners.UnionWith(vertexIds);
            }
            if (corners.Count != 4) return null;
            //
            return corners.ToArray();
        }
        /// <summary>
        /// Splits a number of elements over a chain of edges, in proportion to their lengths and
        /// with at least one element on each. The returned counts always add up to the total.
        /// </summary>
        public static int[] DistributeElements(int total, double[] lengths)
        {
            int n = lengths.Length;
            int[] result = new int[n];
            if (n == 0) return result;
            // Every edge needs at least one element, so that is handed out first and only the rest
            // is shared out by length
            if (total < n) total = n;
            for (int i = 0; i < n; i++) result[i] = 1;
            //
            int remaining = total - n;
            if (remaining <= 0) return result;
            //
            double lengthSum = 0;
            for (int i = 0; i < n; i++) lengthSum += Math.Max(0, lengths[i]);
            //
            int assigned = 0;
            double[] fraction = new double[n];
            for (int i = 0; i < n; i++)
            {
                double share = lengthSum > 0 ? remaining * Math.Max(0, lengths[i]) / lengthSum
                                             : (double)remaining / n;
                int whole = (int)Math.Floor(share);
                result[i] += whole;
                fraction[i] = share - whole;
                assigned += whole;
            }
            // The leftover from the rounding goes to the largest fractions first
            int[] order = Enumerable.Range(0, n).OrderByDescending(i => fraction[i]).ToArray();
            for (int k = 0; k < remaining - assigned; k++) result[order[k % n]]++;
            //
            return result;
        }
        public static double[] GetEdgeLengths(int[] edgeIds)
        {
            double length;
            double[] lengths = new double[edgeIds.Length];
            for (int i = 0; i < edgeIds.Length; i++)
            {
                Gmsh.Model.OCC.GetMass(1, edgeIds[i], out length);
                lengths[i] = length;
            }
            return lengths;
        }
        /// <summary>
        /// Number of elements currently meshed on an edge.
        /// </summary>
        public static int GetElementCount(int edgeId)
        {
            IntPtr[] nodeTags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out nodeTags, out coor, 1, edgeId, true, false);
            return Math.Max(1, nodeTags.Length - 1);
        }
    }
}
