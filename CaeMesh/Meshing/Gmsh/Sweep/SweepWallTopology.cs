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
    /// A chain of edges following the sweep direction from a vertex of the source, with the vertices it
    /// runs through: one more than the edges, the first one on the source.
    /// </summary>
    public class SweepChain
    {
        // Variables
        private readonly int[] _edgeIds;
        private readonly int[] _vertexIds;


        // Properties
        public int[] EdgeIds { get { return _edgeIds; } }
        public int[] VertexIds { get { return _vertexIds; } }


        // Constructors
        public SweepChain(int[] edgeIds, int[] vertexIds)
        {
            _edgeIds = edgeIds;
            _vertexIds = vertexIds;
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
        ///
        /// A body split along the sweep direction has its walls in rings, and a wall in the middle
        /// touches neither the source nor the target. When the edges following the sweep direction
        /// are known - the model records them ring by ring - a wall's other edges are its two sides,
        /// and which side is which follows from the source outwards: the side a wall shares with the
        /// source, or with the far side of a wall already sorted, is its source side. The walls come
        /// back in that order, so that the far side of one ring can hand its element counts to the
        /// next. Without the direction edges, or for a wall they do not cover, an edge on neither the
        /// source nor the target is taken to follow the sweep direction, as for a body in one ring.
        /// </summary>
        public static List<SweepWall> Classify(IEnumerable<int> sourceSurfaceIds, IEnumerable<int> wallSurfaceIds,
                                               IEnumerable<int> targetSurfaceIds,
                                               Dictionary<int, int[]> surfaceIdEdgeIds,
                                               HashSet<int> directionEdgeIds = null)
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
            // The walls whose direction edges are known, with their cross edges still to be sided
            List<SweepWall> walls = new List<SweepWall>();
            List<int> ringWallIds = new List<int>();
            Dictionary<int, int[]> ringWallDirectionEdgeIds = new Dictionary<int, int[]>();
            Dictionary<int, HashSet<int>> ringWallCrossEdgeIds = new Dictionary<int, HashSet<int>>();
            foreach (var wallSurfaceId in wallSurfaceIds)
            {
                if (!surfaceIdEdgeIds.TryGetValue(wallSurfaceId, out edgeIds)) continue;
                //
                int[] wallEdgeIds = edgeIds.Distinct().ToArray();
                if (directionEdgeIds != null && wallEdgeIds.Any(id => directionEdgeIds.Contains(id)))
                {
                    ringWallIds.Add(wallSurfaceId);
                    ringWallDirectionEdgeIds.Add(wallSurfaceId, wallEdgeIds.Where(id => directionEdgeIds.Contains(id)).ToArray());
                    ringWallCrossEdgeIds.Add(wallSurfaceId, wallEdgeIds.Where(id => !directionEdgeIds.Contains(id)).ToHashSet());
                    continue;
                }
                //
                List<int> onSource = new List<int>();
                List<int> onTarget = new List<int>();
                List<int> onDirection = new List<int>();
                foreach (var edgeId in wallEdgeIds)
                {
                    if (sourceEdgeIds.Contains(edgeId)) onSource.Add(edgeId);
                    else if (targetEdgeIds.Contains(edgeId)) onTarget.Add(edgeId);
                    else onDirection.Add(edgeId);
                }
                walls.Add(new SweepWall(wallSurfaceId, onSource.ToArray(), onTarget.ToArray(), onDirection.ToArray()));
            }
            // From the source outwards: a wall touching the frontier has its source side there, and its
            // other cross edges become the frontier for the walls of the next ring
            HashSet<int> frontierEdgeIds = new HashSet<int>(sourceEdgeIds);
            while (ringWallIds.Count > 0)
            {
                HashSet<int> nextFrontierEdgeIds = new HashSet<int>();
                List<int> sorted = new List<int>();
                foreach (var wallSurfaceId in ringWallIds)
                {
                    HashSet<int> crossEdgeIds = ringWallCrossEdgeIds[wallSurfaceId];
                    int[] onSource = crossEdgeIds.Where(id => frontierEdgeIds.Contains(id)).ToArray();
                    if (onSource.Length == 0) continue;
                    int[] onTarget = crossEdgeIds.Where(id => !frontierEdgeIds.Contains(id)).ToArray();
                    walls.Add(new SweepWall(wallSurfaceId, onSource, onTarget, ringWallDirectionEdgeIds[wallSurfaceId]));
                    nextFrontierEdgeIds.UnionWith(onTarget);
                    sorted.Add(wallSurfaceId);
                }
                // A wall that no frontier reaches is sided by the source and the target alone
                if (sorted.Count == 0)
                {
                    foreach (var wallSurfaceId in ringWallIds)
                    {
                        HashSet<int> crossEdgeIds = ringWallCrossEdgeIds[wallSurfaceId];
                        walls.Add(new SweepWall(wallSurfaceId, crossEdgeIds.Where(id => sourceEdgeIds.Contains(id)).ToArray(),
                                                crossEdgeIds.Where(id => !sourceEdgeIds.Contains(id)).ToArray(),
                                                ringWallDirectionEdgeIds[wallSurfaceId]));
                    }
                    break;
                }
                foreach (var wallSurfaceId in sorted) ringWallIds.Remove(wallSurfaceId);
                frontierEdgeIds = nextFrontierEdgeIds;
            }
            //
            return walls;
        }
        /// <summary>
        /// The four corner vertices of a wall, or null when they cannot be identified. They are the
        /// ends of the two chains of edges that follow the sweep direction - the vertices where an odd
        /// number of those edges meet, since a vertex partway along a split side joins two of them. A
        /// wall without exactly four is left for gmsh to work out.
        /// </summary>
        public static int[] GetCorners(SweepWall wall)
        {
            if (wall.DirectionEdgeIds.Length < 2) return null;
            //
            int[] vertexIds;
            Dictionary<int, int> vertexIdDegree = new Dictionary<int, int>();
            foreach (var edgeId in wall.DirectionEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                if (vertexIds.Distinct().Count() != 2) return null;
                foreach (var vertexId in vertexIds)
                {
                    if (vertexIdDegree.ContainsKey(vertexId)) vertexIdDegree[vertexId]++;
                    else vertexIdDegree[vertexId] = 1;
                }
            }
            int[] corners = vertexIdDegree.Where(entry => entry.Value % 2 == 1).Select(entry => entry.Key).ToArray();
            return corners.Length == 4 ? corners : null;
        }
        /// <summary>
        /// The edges of the two cross-sweep sides of a wall in the order they run, the target side read the
        /// way the source side runs: the two chains start at the ends that the same run of direction edges
        /// joins. False when either side is not a chain with two ends, when the sides hold a different number
        /// of edges, or when the ends cannot be matched through the direction edges.
        ///
        /// A wall whose two sides are split at corresponding places needs it: the rim of a bent plate is
        /// split where the bend begins on the source and again where it begins on the target, and unless the
        /// two segments opposite each other carry the same number of elements, the grid column that starts at
        /// the split of one side ends somewhere in the middle of the other.
        /// </summary>
        public static bool OrderWallSides(SweepWall wall, out int[] sourceEdgeIds, out int[] targetEdgeIds)
        {
            sourceEdgeIds = null;
            targetEdgeIds = null;
            if (wall.SourceEdgeIds.Length != wall.TargetEdgeIds.Length) return false;
            //
            int[] sourceEndVertexIds;
            int[] targetEndVertexIds;
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            if (!OrderSideEdges(wall.SourceEdgeIds, edgeIdVertexIds, out sourceEdgeIds, out sourceEndVertexIds) ||
                !OrderSideEdges(wall.TargetEdgeIds, edgeIdVertexIds, out targetEdgeIds, out targetEndVertexIds))
                return false;
            // The vertices the direction edges reach from the first vertex of the source side: the end of the
            // target side among them is the end the source side runs from
            int[] vertexIds;
            bool added = true;
            HashSet<int> reachedVertexIds = new HashSet<int>() { sourceEndVertexIds[0] };
            while (added)
            {
                added = false;
                foreach (var edgeId in wall.DirectionEdgeIds)
                {
                    vertexIds = GetEdgeVertexIds(edgeId, edgeIdVertexIds);
                    if (vertexIds == null) continue;
                    if (reachedVertexIds.Contains(vertexIds[0]) && reachedVertexIds.Add(vertexIds[1])) added = true;
                    if (reachedVertexIds.Contains(vertexIds[1]) && reachedVertexIds.Add(vertexIds[0])) added = true;
                }
            }
            if (reachedVertexIds.Contains(targetEndVertexIds[0])) return true;
            if (!reachedVertexIds.Contains(targetEndVertexIds[1])) return false;
            //
            targetEdgeIds = targetEdgeIds.Reverse().ToArray();
            return true;
        }
        /// <summary>
        /// The edges of one side of a wall in the order they run, from one end of the chain to the other,
        /// together with the two end vertices. False when the edges do not form a single open chain.
        /// </summary>
        private static bool OrderSideEdges(int[] edgeIds, Dictionary<int, int[]> edgeIdVertexIds,
                                           out int[] orderedEdgeIds, out int[] endVertexIds)
        {
            orderedEdgeIds = null;
            endVertexIds = null;
            //
            int[] vertexIds;
            List<int> edgeIdsAtVertex;
            Dictionary<int, List<int>> vertexIdEdgeIds = new Dictionary<int, List<int>>();
            foreach (var edgeId in edgeIds)
            {
                vertexIds = GetEdgeVertexIds(edgeId, edgeIdVertexIds);
                if (vertexIds == null) return false;
                foreach (var vertexId in vertexIds)
                {
                    if (!vertexIdEdgeIds.TryGetValue(vertexId, out edgeIdsAtVertex))
                        vertexIdEdgeIds.Add(vertexId, edgeIdsAtVertex = new List<int>());
                    edgeIdsAtVertex.Add(edgeId);
                }
            }
            int[] ends = vertexIdEdgeIds.Where(entry => entry.Value.Count == 1).Select(entry => entry.Key).ToArray();
            if (ends.Length != 2) return false;
            //
            int currentVertexId = ends[0];
            List<int> ordered = new List<int>();
            HashSet<int> usedEdgeIds = new HashSet<int>();
            while (ordered.Count < edgeIds.Length)
            {
                int nextEdgeId = -1;
                foreach (var edgeId in vertexIdEdgeIds[currentVertexId])
                {
                    if (usedEdgeIds.Contains(edgeId)) continue;
                    nextEdgeId = edgeId;
                    break;
                }
                if (nextEdgeId < 0) return false;
                usedEdgeIds.Add(nextEdgeId);
                ordered.Add(nextEdgeId);
                vertexIds = edgeIdVertexIds[nextEdgeId];
                currentVertexId = vertexIds[0] == currentVertexId ? vertexIds[1] : vertexIds[0];
            }
            if (currentVertexId != ends[1]) return false;
            //
            orderedEdgeIds = ordered.ToArray();
            endVertexIds = ends;
            return true;
        }
        /// <summary>
        /// The two end vertices of an edge, or null for an edge that has not exactly two.
        /// </summary>
        private static int[] GetEdgeVertexIds(int edgeId, Dictionary<int, int[]> edgeIdVertexIds)
        {
            int[] vertexIds;
            if (edgeIdVertexIds.TryGetValue(edgeId, out vertexIds)) return vertexIds;
            //
            Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
            vertexIds = vertexIds.Distinct().ToArray();
            if (vertexIds.Length != 2) vertexIds = null;
            edgeIdVertexIds.Add(edgeId, vertexIds);
            return vertexIds;
        }
        /// <summary>
        /// Follows the edges running from the source of a block towards its target: from every vertex of
        /// the source, along every wall edge leaving it, and on through the edge that continues most
        /// smoothly at every vertex, until nothing continues within 25 degrees. The edges the model marked
        /// as following the sweep direction are the only candidates when they are given. Every chain
        /// comes back with its edges and its vertices in order from the source.
        /// </summary>
        public static List<SweepChain> FollowChains(IEnumerable<int> sourceEdgeIds, IEnumerable<int> wallEdgeIds,
                                                    HashSet<int> allowedEdgeIds)
        {
            HashSet<int> sourceEdgeIdSet = new HashSet<int>(sourceEdgeIds);
            HashSet<int> candidateEdgeIds = new HashSet<int>(wallEdgeIds);
            candidateEdgeIds.ExceptWith(sourceEdgeIdSet);
            if (allowedEdgeIds != null) candidateEdgeIds.IntersectWith(allowedEdgeIds);
            //
            int[] vertexIds;
            List<int> edgeIdsAtVertex;
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            Dictionary<int, List<int>> vertexIdEdgeIds = new Dictionary<int, List<int>>();
            foreach (var edgeId in candidateEdgeIds.OrderBy(id => id))
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                edgeIdVertexIds.Add(edgeId, vertexIds.Distinct().ToArray());
                foreach (var vertexId in edgeIdVertexIds[edgeId])
                {
                    if (!vertexIdEdgeIds.TryGetValue(vertexId, out edgeIdsAtVertex))
                    {
                        edgeIdsAtVertex = new List<int>();
                        vertexIdEdgeIds.Add(vertexId, edgeIdsAtVertex);
                    }
                    edgeIdsAtVertex.Add(edgeId);
                }
            }
            HashSet<int> startVertexIds = new HashSet<int>();
            foreach (var edgeId in sourceEdgeIdSet)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                startVertexIds.UnionWith(vertexIds);
            }
            //
            HashSet<int> usedEdgeIds = new HashSet<int>();
            List<SweepChain> chains = new List<SweepChain>();
            foreach (var startVertexId in startVertexIds.OrderBy(id => id))
            {
                if (!vertexIdEdgeIds.TryGetValue(startVertexId, out edgeIdsAtVertex)) continue;
                foreach (var startEdgeId in edgeIdsAtVertex.ToArray())
                {
                    if (usedEdgeIds.Contains(startEdgeId)) continue;
                    //
                    int edgeId = startEdgeId;
                    int vertexId = startVertexId;
                    List<int> chain = new List<int>();
                    List<int> chainVertexIds = new List<int>() { startVertexId };
                    while (true)
                    {
                        usedEdgeIds.Add(edgeId);
                        chain.Add(edgeId);
                        int[] endVertexIds = edgeIdVertexIds[edgeId];
                        if (endVertexIds.Length != 2)
                        {
                            chainVertexIds.Add(vertexId);
                            break;
                        }
                        int nextVertexId = endVertexIds[0] == vertexId ? endVertexIds[1] : endVertexIds[0];
                        chainVertexIds.Add(nextVertexId);
                        // The direction of travel into the vertex is against the direction the edge leaves it in
                        double[] incoming = GetEdgeTangent(edgeId, nextVertexId);
                        int bestEdgeId = -1;
                        double bestCosine = 0.9;
                        foreach (var candidateId in vertexIdEdgeIds[nextVertexId])
                        {
                            if (usedEdgeIds.Contains(candidateId)) continue;
                            double[] outgoing = GetEdgeTangent(candidateId, nextVertexId);
                            double cosine = -(incoming[0] * outgoing[0] + incoming[1] * outgoing[1] + incoming[2] * outgoing[2]) /
                                            (GetLength(incoming) * GetLength(outgoing));
                            if (cosine > bestCosine)
                            {
                                bestCosine = cosine;
                                bestEdgeId = candidateId;
                            }
                        }
                        if (bestEdgeId < 0) break;
                        edgeId = bestEdgeId;
                        vertexId = nextVertexId;
                    }
                    chains.Add(new SweepChain(chain.ToArray(), chainVertexIds.ToArray()));
                }
            }
            return chains;
        }
        /// <summary>
        /// The direction an edge leaves one of its vertices in.
        /// </summary>
        private static double[] GetEdgeTangent(int edgeId, int vertexId)
        {
            double[] min;
            double[] max;
            double[] vertex;
            double[] start;
            double[] end;
            double[] near;
            Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
            Gmsh.Model.GetValue(0, vertexId, new double[3], out vertex);
            Gmsh.Model.GetValue(1, edgeId, new double[] { min[0] }, out start);
            Gmsh.Model.GetValue(1, edgeId, new double[] { max[0] }, out end);
            bool atStart = GetDistance(start, vertex) <= GetDistance(end, vertex);
            double delta = 1E-3 * (max[0] - min[0]);
            Gmsh.Model.GetValue(1, edgeId, new double[] { atStart ? min[0] + delta : max[0] - delta }, out near);
            double[] origin = atStart ? start : end;
            return new double[] { near[0] - origin[0], near[1] - origin[1], near[2] - origin[2] };
        }
        private static double GetLength(double[] vector)
        {
            return Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]) + 1E-300;
        }
        private static double GetDistance(double[] coor1, double[] coor2)
        {
            return Math.Sqrt(Math.Pow(coor1[0] - coor2[0], 2) + Math.Pow(coor1[1] - coor2[1], 2) +
                             Math.Pow(coor1[2] - coor2[2], 2));
        }
        /// <summary>
        /// The number of elements on every edge following the sweep direction, from the levels along the
        /// sweep. The chains of a family - chains sharing an edge, or running along the two sides of one
        /// wall - are cut into levels: two chain vertices joined by a cross edge of a wall, or lying on one
        /// cross section face - a source or a target of a block - are on one level, and so are vertices of
        /// different chains at nearly the same fraction of their chain's length when nothing orders them.
        /// The levels are ordered along the chains, every interval between two consecutive levels carries
        /// one count, an edge carries the sum over the intervals it spans, and so the two sides of every wall
        /// add up to the same total whatever way each of them is split. A count pinned on an edge spanning
        /// one interval is that interval's count; a count pinned on a longer edge is shared out over its
        /// intervals; otherwise the counts measured from the 1D mesh decide.
        /// </summary>
        public static Dictionary<int, int> ResolveDirectionEdgeElements(List<SweepChain> chains, List<SweepWall> walls,
                                                                        List<int[]> crossSectionVertexIds,
                                                                        Dictionary<int, int> pinnedEdgeIdNumOfElements,
                                                                        List<int> chainBlockIndices)
        {
            Dictionary<int, int> edgeIdNumOfElements = new Dictionary<int, int>();
            if (chains.Count == 0) return edgeIdNumOfElements;
            // Families: chains joined by a shared edge or by a wall they are the two sides of
            int[] parent = Enumerable.Range(0, chains.Count).ToArray();
            Dictionary<int, List<int>> edgeIdChainIndices = new Dictionary<int, List<int>>();
            for (int i = 0; i < chains.Count; i++)
            {
                foreach (var edgeId in chains[i].EdgeIds)
                {
                    List<int> indices;
                    if (!edgeIdChainIndices.TryGetValue(edgeId, out indices))
                    {
                        indices = new List<int>();
                        edgeIdChainIndices.Add(edgeId, indices);
                    }
                    indices.Add(i);
                }
            }
            foreach (var indices in edgeIdChainIndices.Values) Unite(parent, indices);
            foreach (var wall in walls)
            {
                List<int> indices = new List<int>();
                foreach (var edgeId in wall.DirectionEdgeIds)
                {
                    List<int> chainIndices;
                    if (edgeIdChainIndices.TryGetValue(edgeId, out chainIndices)) indices.AddRange(chainIndices);
                }
                Unite(parent, indices);
            }
            // The vertices that lie on one level: the two of every cross edge of a wall, and those of every
            // cross section face
            int[] vertexIds;
            List<int[]> levelVertexIds = new List<int[]>(crossSectionVertexIds);
            foreach (var wall in walls)
            {
                foreach (var edgeId in wall.SourceEdgeIds.Concat(wall.TargetEdgeIds))
                {
                    Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                    if (vertexIds.Distinct().Count() == 2) levelVertexIds.Add(vertexIds.Distinct().ToArray());
                }
            }
            //
            List<List<int>> families = Enumerable.Range(0, chains.Count).GroupBy(i => Find(parent, i))
                                                 .Select(group => group.ToList()).ToList();
            int[] familyTotals = new int[families.Count];
            bool[] familySpansAllLevels = new bool[families.Count];
            for (int f = 0; f < families.Count; f++)
            {
                ResolveFamilyElements(families[f].Select(i => chains[i]).ToList(), levelVertexIds,
                                      pinnedEdgeIdNumOfElements, edgeIdNumOfElements, 0, out familyTotals[f],
                                      out familySpansAllLevels[f]);
            }
            // Every family of one block counts the same layers, the sweep walking one grid over all the walls
            // of a block, so the families of a block are made to add up to the same total. Left to itself a
            // family reads the 1D mesh of its own edges - the seam of a bore measures differently from the
            // outline of the plate the bore goes through - and one column of the grid then ends before the
            // next, which the sweep can only refuse. Only the families whose chains all run the whole way are
            // reconciled: one mixing chains of different extents - the collar plate swept through its
            // thickness beside the post that carries it - has a span and not a layer count, and two blocks
            // swept in different directions across a wall they share keep their own totals in any case, their
            // chains sharing no edge and no wall side
            int[] familyParent = Enumerable.Range(0, families.Count).ToArray();
            if (chainBlockIndices != null)
            {
                List<int> familyIndices;
                Dictionary<int, List<int>> blockIndexFamilyIndices = new Dictionary<int, List<int>>();
                for (int f = 0; f < families.Count; f++)
                {
                    if (!familySpansAllLevels[f]) continue;
                    foreach (var i in families[f])
                    {
                        if (i >= chainBlockIndices.Count) continue;
                        if (!blockIndexFamilyIndices.TryGetValue(chainBlockIndices[i], out familyIndices))
                            blockIndexFamilyIndices.Add(chainBlockIndices[i], familyIndices = new List<int>());
                        if (!familyIndices.Contains(f)) familyIndices.Add(f);
                    }
                }
                foreach (var indices in blockIndexFamilyIndices.Values) Unite(familyParent, indices);
            }
            foreach (var group in Enumerable.Range(0, families.Count).GroupBy(f => Find(familyParent, f)))
            {
                List<int> familyGroup = group.Where(f => familySpansAllLevels[f]).ToList();
                if (familyGroup.Count < 2) continue;
                //
                int forcedTotal = familyGroup.Max(f => familyTotals[f]);
                foreach (var f in familyGroup)
                {
                    if (familyTotals[f] == forcedTotal || forcedTotal < 1) continue;
                    ResolveFamilyElements(families[f].Select(i => chains[i]).ToList(), levelVertexIds,
                                          pinnedEdgeIdNumOfElements, edgeIdNumOfElements, forcedTotal, out _, out _);
                }
            }
            return edgeIdNumOfElements;
        }
        private static void ResolveFamilyElements(List<SweepChain> chains, List<int[]> levelVertexIds,
                                                  Dictionary<int, int> pinnedEdgeIdNumOfElements,
                                                  Dictionary<int, int> edgeIdNumOfElements, int forcedTotal,
                                                  out int total, out bool spansAllLevels)
        {
            chains = OrientChainsAlike(chains);
            // Every chain vertex, with the fraction of its chain's length it lies at
            double length;
            List<int> chainOfVertex = new List<int>();
            List<int> indexOfVertex = new List<int>();
            List<double> fractionOfVertex = new List<double>();
            Dictionary<int, List<int>> vertexIdPositions = new Dictionary<int, List<int>>();
            for (int c = 0; c < chains.Count; c++)
            {
                int[] edgeIds = chains[c].EdgeIds;
                double[] fractions = new double[edgeIds.Length + 1];
                for (int i = 0; i < edgeIds.Length; i++)
                {
                    Gmsh.Model.OCC.GetMass(1, edgeIds[i], out length);
                    fractions[i + 1] = fractions[i] + length;
                }
                for (int i = 0; i <= edgeIds.Length; i++)
                {
                    List<int> positions;
                    int vertexId = chains[c].VertexIds[i];
                    if (!vertexIdPositions.TryGetValue(vertexId, out positions))
                    {
                        positions = new List<int>();
                        vertexIdPositions.Add(vertexId, positions);
                    }
                    positions.Add(chainOfVertex.Count);
                    chainOfVertex.Add(c);
                    indexOfVertex.Add(i);
                    fractionOfVertex.Add(fractions[edgeIds.Length] > 0 ? fractions[i] / fractions[edgeIds.Length] : 0);
                }
            }
            // The levels: one vertex reached by several chains is one level, and so are the vertices joined by
            // a cross edge or lying on one cross section face
            int[] levelOfPosition = Enumerable.Range(0, chainOfVertex.Count).ToArray();
            foreach (var positions in vertexIdPositions.Values) Unite(levelOfPosition, positions);
            foreach (var group in levelVertexIds)
            {
                List<int> positions;
                List<int> groupPositions = new List<int>();
                foreach (var vertexId in group)
                {
                    if (vertexIdPositions.TryGetValue(vertexId, out positions)) groupPositions.Add(positions[0]);
                }
                // A face holding two vertices of one chain crosses this family's sweep rather than cutting it -
                // the end of a block swept the other way across a shared wall - and says nothing about levels
                if (groupPositions.Select(position => chainOfVertex[position]).Distinct().Count() < groupPositions.Count) continue;
                Unite(levelOfPosition, groupPositions);
            }
            List<List<int>> levels = Enumerable.Range(0, chainOfVertex.Count).GroupBy(p => Find(levelOfPosition, p))
                                               .Select(group => group.ToList()).ToList();
            // The levels in the order the chains run through them; by the fractions alone where the chains do
            // not agree. Two levels no chain orders, at nearly the same fraction, are one level, so that a
            // split partway along one chain does not put a sliver of a layer next to a split of another
            const double tolerance = 0.02;
            while (true)
            {
                bool[,] before = GetLevelOrder(chains, levels, chainOfVertex, indexOfVertex, fractionOfVertex);
                int merged = -1;
                for (int k = 0; k + 1 < levels.Count && merged < 0; k++)
                {
                    if (before[k, k + 1] || before[k + 1, k]) continue;
                    double gap = Math.Abs(levels[k + 1].Average(p => fractionOfVertex[p]) -
                                          levels[k].Average(p => fractionOfVertex[p]));
                    if (gap > tolerance) continue;
                    if (levels[k].Select(p => chainOfVertex[p]).Intersect(levels[k + 1].Select(p => chainOfVertex[p])).Any())
                        continue;
                    merged = k;
                }
                if (merged < 0) break;
                levels[merged].AddRange(levels[merged + 1]);
                levels.RemoveAt(merged + 1);
            }
            double[] levelFractions = levels.Select(level => level.Average(p => fractionOfVertex[p])).ToArray();
            int[][] chainLevelIndices = chains.Select(chain => new int[chain.VertexIds.Length]).ToArray();
            for (int k = 0; k < levels.Count; k++)
            {
                foreach (var p in levels[k]) chainLevelIndices[chainOfVertex[p]][indexOfVertex[p]] = k;
            }
            //
            int numIntervals = levels.Count - 1;
            int[] intervalCounts = new int[numIntervals];
            int[] pinnedCounts = new int[numIntervals];
            double[] measuredSums = new double[numIntervals];
            int[] measuredCounts = new int[numIntervals];
            // The edges spanning one interval speak for it: a pinned count first, else the measured average
            int numOfElements;
            for (int c = 0; c < chains.Count; c++)
            {
                for (int i = 0; i < chains[c].EdgeIds.Length; i++)
                {
                    int first = chainLevelIndices[c][i];
                    int last = chainLevelIndices[c][i + 1];
                    if (last - first != 1) continue;
                    if (pinnedEdgeIdNumOfElements.TryGetValue(chains[c].EdgeIds[i], out numOfElements))
                        pinnedCounts[first] = Math.Max(pinnedCounts[first], numOfElements);
                    else
                    {
                        measuredSums[first] += GetElementCount(chains[c].EdgeIds[i]);
                        measuredCounts[first]++;
                    }
                }
            }
            for (int k = 0; k < numIntervals; k++)
            {
                if (pinnedCounts[k] > 0) intervalCounts[k] = pinnedCounts[k];
                else if (measuredCounts[k] > 0)
                    intervalCounts[k] = (int)Math.Round(measuredSums[k] / measuredCounts[k], 0, MidpointRounding.AwayFromZero);
            }
            // An edge spanning several intervals shares its count out over them in proportion to their
            // lengths: a pinned one always, a measured one only where no edge spanning a single interval spoke
            for (int pass = 0; pass < 2; pass++)
            {
                for (int c = 0; c < chains.Count; c++)
                {
                    for (int i = 0; i < chains[c].EdgeIds.Length; i++)
                    {
                        int first = chainLevelIndices[c][i];
                        int last = chainLevelIndices[c][i + 1];
                        if (last - first < 2) continue;
                        bool pinned = pinnedEdgeIdNumOfElements.TryGetValue(chains[c].EdgeIds[i], out numOfElements);
                        if (pinned != (pass == 1)) continue;
                        if (!pinned) numOfElements = GetElementCount(chains[c].EdgeIds[i]);
                        //
                        double[] lengths = new double[last - first];
                        for (int k = first; k < last; k++) lengths[k - first] = levelFractions[k + 1] - levelFractions[k];
                        int[] shares = DistributeElements(numOfElements, lengths);
                        for (int k = first; k < last; k++)
                        {
                            if (pinned || intervalCounts[k] == 0) intervalCounts[k] = shares[k - first];
                        }
                    }
                }
            }
            for (int k = 0; k < numIntervals; k++)
            {
                if (intervalCounts[k] < 1) intervalCounts[k] = 1;
            }
            // Whether every chain of the family runs the whole way, which is what makes its total the number
            // of layers of the block rather than the span of a family that mixes chains of different extents
            spansAllLevels = numIntervals > 0;
            for (int c = 0; c < chains.Count && spansAllLevels; c++)
            {
                spansAllLevels = chainLevelIndices[c][0] == 0 &&
                                 chainLevelIndices[c][chains[c].VertexIds.Length - 1] == levels.Count - 1;
            }
            // Made to add up to the total the other families of the block resolved, keeping the proportions
            // this family found on its own
            if (forcedTotal > 0 && numIntervals > 0)
            {
                double[] weights = new double[numIntervals];
                for (int k = 0; k < numIntervals; k++) weights[k] = intervalCounts[k];
                intervalCounts = DistributeElements(forcedTotal, weights);
            }
            total = 0;
            for (int k = 0; k < numIntervals; k++) total += intervalCounts[k];
            // An edge whose ends do not follow the levels in order - a body the levels cannot describe - keeps
            // its own count
            for (int c = 0; c < chains.Count; c++)
            {
                for (int i = 0; i < chains[c].EdgeIds.Length; i++)
                {
                    int first = chainLevelIndices[c][i];
                    int last = chainLevelIndices[c][i + 1];
                    int count = 0;
                    if (last > first)
                    {
                        for (int k = first; k < last; k++) count += intervalCounts[k];
                    }
                    else if (!pinnedEdgeIdNumOfElements.TryGetValue(chains[c].EdgeIds[i], out count))
                        count = GetElementCount(chains[c].EdgeIds[i]);
                    edgeIdNumOfElements[chains[c].EdgeIds[i]] = Math.Max(1, count);
                }
            }
        }
        /// <summary>
        /// Sorts the levels the way the chains run through them - a topological order of the successions
        /// along every chain, ties broken by the mean fraction - and returns which level comes before which.
        /// Levels the chains order inconsistently are sorted by their mean fraction alone.
        /// </summary>
        /// <summary>
        /// The chains of a family, every one of them running the way its neighbours run. A chain is built
        /// from the source it starts at, and two chains that meet can therefore walk the edge they share in
        /// opposite directions - the seam of a bore is walked down by the chain that came up the post it
        /// passes through and up by the chain of the plate. The levels of the two then order each other both
        /// ways round, the order comes out cyclic, and the fractions of the chains decide instead, which puts
        /// a level of one chain between two levels of another and gives the edges between them the count of
        /// two layers instead of one. Reversing the chains that disagree with their neighbours removes the
        /// cycle at its source.
        /// </summary>
        private static List<SweepChain> OrientChainsAlike(List<SweepChain> chains)
        {
            // The chains meeting on each edge, with the direction each of them walks it in
            Dictionary<int, List<int>> edgeIdChains = new Dictionary<int, List<int>>();
            Dictionary<int, List<bool>> edgeIdForward = new Dictionary<int, List<bool>>();
            for (int c = 0; c < chains.Count; c++)
            {
                for (int i = 0; i < chains[c].EdgeIds.Length; i++)
                {
                    int edgeId = chains[c].EdgeIds[i];
                    if (!edgeIdChains.ContainsKey(edgeId))
                    {
                        edgeIdChains.Add(edgeId, new List<int>());
                        edgeIdForward.Add(edgeId, new List<bool>());
                    }
                    edgeIdChains[edgeId].Add(c);
                    edgeIdForward[edgeId].Add(chains[c].VertexIds[i] < chains[c].VertexIds[i + 1]);
                }
            }
            // Spread an orientation from each chain to the ones it shares an edge with
            bool[] reversed = new bool[chains.Count];
            bool[] oriented = new bool[chains.Count];
            for (int start = 0; start < chains.Count; start++)
            {
                if (oriented[start]) continue;
                oriented[start] = true;
                Queue<int> pending = new Queue<int>();
                pending.Enqueue(start);
                while (pending.Count > 0)
                {
                    int c = pending.Dequeue();
                    for (int i = 0; i < chains[c].EdgeIds.Length; i++)
                    {
                        int edgeId = chains[c].EdgeIds[i];
                        bool forward = edgeIdForward[edgeId][edgeIdChains[edgeId].IndexOf(c)] != reversed[c];
                        List<int> others = edgeIdChains[edgeId];
                        for (int k = 0; k < others.Count; k++)
                        {
                            int other = others[k];
                            if (other == c || oriented[other]) continue;
                            oriented[other] = true;
                            reversed[other] = edgeIdForward[edgeId][k] != forward;
                            pending.Enqueue(other);
                        }
                    }
                }
            }
            //
            List<SweepChain> orientedChains = new List<SweepChain>();
            for (int c = 0; c < chains.Count; c++)
            {
                if (!reversed[c]) { orientedChains.Add(chains[c]); continue; }
                //
                int[] edgeIds = chains[c].EdgeIds.Reverse().ToArray();
                int[] vertexIds = chains[c].VertexIds.Reverse().ToArray();
                orientedChains.Add(new SweepChain(edgeIds, vertexIds));
            }
            return orientedChains;
        }
        private static bool[,] GetLevelOrder(List<SweepChain> chains, List<List<int>> levels, List<int> chainOfVertex,
                                             List<int> indexOfVertex, List<double> fractionOfVertex)
        {
            int count = levels.Count;
            int[] levelOfPosition = new int[chainOfVertex.Count];
            for (int k = 0; k < count; k++)
            {
                foreach (var p in levels[k]) levelOfPosition[p] = k;
            }
            bool[,] before = new bool[count, count];
            for (int p = 0; p < chainOfVertex.Count; p++)
            {
                if (indexOfVertex[p] + 1 >= chains[chainOfVertex[p]].VertexIds.Length) continue;
                // The position of the next vertex of the same chain
                int next = p + 1;
                if (levelOfPosition[p] != levelOfPosition[next]) before[levelOfPosition[p], levelOfPosition[next]] = true;
            }
            for (int m = 0; m < count; m++)
            {
                for (int a = 0; a < count; a++)
                {
                    if (!before[a, m]) continue;
                    for (int b = 0; b < count; b++)
                    {
                        if (before[m, b]) before[a, b] = true;
                    }
                }
            }
            double[] fractions = levels.Select(level => level.Average(p => fractionOfVertex[p])).ToArray();
            List<int> order = new List<int>();
            bool[] placed = new bool[count];
            bool consistent = true;
            for (int step = 0; step < count && consistent; step++)
            {
                int best = -1;
                for (int k = 0; k < count; k++)
                {
                    if (placed[k]) continue;
                    bool free = true;
                    for (int a = 0; a < count && free; a++) free = placed[a] || a == k || !before[a, k];
                    if (free && (best < 0 || fractions[k] < fractions[best])) best = k;
                }
                if (best < 0) consistent = false;
                else
                {
                    placed[best] = true;
                    order.Add(best);
                }
            }
            if (!consistent) order = Enumerable.Range(0, count).OrderBy(k => fractions[k]).ToList();
            //
            List<List<int>> sorted = order.Select(k => levels[k]).ToList();
            levels.Clear();
            levels.AddRange(sorted);
            bool[,] sortedBefore = new bool[count, count];
            for (int a = 0; a < count; a++)
            {
                for (int b = 0; b < count; b++) sortedBefore[a, b] = before[order[a], order[b]];
            }
            return sortedBefore;
        }
        private static int Find(int[] parent, int i)
        {
            while (parent[i] != i) i = parent[i];
            return i;
        }
        private static void Unite(int[] parent, List<int> indices)
        {
            for (int i = 1; i < indices.Count; i++)
            {
                int a = Find(parent, indices[0]);
                int b = Find(parent, indices[i]);
                if (a != b) parent[b] = a;
            }
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
        /// <summary>
        /// The closed loops the cross edges of the walls form, every edge mapped to the loop it lies on: the
        /// source and target sides of all the walls, joined through the vertices they share. A loop is one cross
        /// section of a column of blocks - the boundary of a source or a target, or the ring where one ring of
        /// walls hands over to the next.
        /// </summary>
        public static Dictionary<int, int[]> GetCrossSectionLoops(List<SweepWall> walls)
        {
            int[] vertexIds;
            List<int> edgeIdsAtVertex;
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            Dictionary<int, List<int>> vertexIdEdgeIds = new Dictionary<int, List<int>>();
            foreach (var wall in walls)
            {
                foreach (var edgeId in wall.SourceEdgeIds.Concat(wall.TargetEdgeIds))
                {
                    if (edgeIdVertexIds.ContainsKey(edgeId)) continue;
                    Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                    edgeIdVertexIds.Add(edgeId, vertexIds);
                    foreach (var vertexId in vertexIds)
                    {
                        if (!vertexIdEdgeIds.TryGetValue(vertexId, out edgeIdsAtVertex))
                            vertexIdEdgeIds.Add(vertexId, edgeIdsAtVertex = new List<int>());
                        edgeIdsAtVertex.Add(edgeId);
                    }
                }
            }
            //
            Dictionary<int, int[]> edgeIdLoop = new Dictionary<int, int[]>();
            foreach (var startEdgeId in edgeIdVertexIds.Keys)
            {
                if (edgeIdLoop.ContainsKey(startEdgeId)) continue;
                List<int> loop = new List<int>();
                Queue<int> pending = new Queue<int>();
                HashSet<int> reachedEdgeIds = new HashSet<int>() { startEdgeId };
                pending.Enqueue(startEdgeId);
                while (pending.Count > 0)
                {
                    int edgeId = pending.Dequeue();
                    loop.Add(edgeId);
                    foreach (var vertexId in edgeIdVertexIds[edgeId])
                    {
                        foreach (var nextEdgeId in vertexIdEdgeIds[vertexId])
                        {
                            if (reachedEdgeIds.Add(nextEdgeId)) pending.Enqueue(nextEdgeId);
                        }
                    }
                }
                int[] loopEdgeIds = loop.ToArray();
                foreach (var edgeId in loopEdgeIds) edgeIdLoop.Add(edgeId, loopEdgeIds);
            }
            return edgeIdLoop;
        }
        /// <summary>
        /// Counts a cross section loop from its length, in place of the counts gmsh measured on its edges one
        /// by one. Gmsh rounds the count of every curve up, so the sum over a loop cut into several edges is
        /// inflated by up to one element per edge, and two cuts of one closed curve come out different; the
        /// length of the curve over the size it was meshed at is the count they share. The size is recovered
        /// from what gmsh produced over all the given cuts of the curve: a curve of length L given n elements
        /// says the size is at least L / n, so the largest L / n over the edges the user left to gmsh is the
        /// size. An edge the user seeded keeps its count exactly, an edge already assigned keeps its count too,
        /// and only what the loop has left over is shared out by length among the rest of its edges. The counts
        /// are written into the table of the assigned edges.
        /// </summary>
        public static void RecountCrossSection(int[] loopEdgeIds, List<int[]> loops,
                                               Dictionary<int, int> seededEdgeIdNumOfElements,
                                               Dictionary<int, int> assignedEdgeIdNumOfElements)
        {
            double length;
            double size = 0;
            foreach (var loop in loops)
            {
                foreach (var edgeId in loop)
                {
                    if (seededEdgeIdNumOfElements.ContainsKey(edgeId)) continue;
                    Gmsh.Model.OCC.GetMass(1, edgeId, out length);
                    size = Math.Max(size, length / GetElementCount(edgeId));
                }
            }
            //
            int numOfElements;
            int fixedElements = 0;
            List<int> restIndices = new List<int>();
            double[] lengths = GetEdgeLengths(loopEdgeIds);
            for (int i = 0; i < loopEdgeIds.Length; i++)
            {
                if (assignedEdgeIdNumOfElements.TryGetValue(loopEdgeIds[i], out numOfElements)) fixedElements += numOfElements;
                else if (seededEdgeIdNumOfElements.TryGetValue(loopEdgeIds[i], out numOfElements))
                {
                    assignedEdgeIdNumOfElements[loopEdgeIds[i]] = numOfElements;
                    fixedElements += numOfElements;
                }
                else restIndices.Add(i);
            }
            if (restIndices.Count == 0) return;
            //
            int total = size > 0 ? (int)Math.Round(lengths.Sum() / size, MidpointRounding.AwayFromZero) : 0;
            double[] restLengths = new double[restIndices.Count];
            for (int k = 0; k < restIndices.Count; k++) restLengths[k] = lengths[restIndices[k]];
            int[] counts = DistributeElements(total - fixedElements, restLengths);
            for (int k = 0; k < restIndices.Count; k++) assignedEdgeIdNumOfElements[loopEdgeIds[restIndices[k]]] = counts[k];
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
