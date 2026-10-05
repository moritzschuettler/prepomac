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
            return OrderWallSideChains(wall, out sourceEdgeIds, out targetEdgeIds);
        }
        /// <summary>
        /// The same ordering for a wall whose two sides are cut into a different number of edges, which the
        /// counts of a split target are read off the source with.
        /// </summary>
        public static bool OrderWallSideChains(SweepWall wall, out int[] sourceEdgeIds, out int[] targetEdgeIds)
        {
            sourceEdgeIds = null;
            targetEdgeIds = null;
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
                        double bestCosine = Math.Cos(Globals.SweepSwallowedWallAngle * Math.PI / 180);
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
        internal static double[] GetEdgeTangent(int edgeId, int vertexId)
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
        /// The vertices partway along two opposite sides of a wall that lie at the same fraction of the length of
        /// their sides: the two sides that follow the sweep, measured from the source side of the wall, and the
        /// source and the target side when both are layer edges of the family - the wall of a block swept across
        /// the family, whose two cross sides the family's own sweep runs along.
        /// </summary>
        private static List<int[]> GetWallSideVertexPairs(SweepWall wall, Func<int, bool> isChainEdge)
        {
            List<int[]> pairs = new List<int[]>();
            int[] vertexIds;
            HashSet<int> sourceVertexIds = new HashSet<int>();
            foreach (var edgeId in wall.SourceEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                sourceVertexIds.UnionWith(vertexIds);
            }
            // The two sides that follow the sweep, each from its vertex on the source side on
            List<List<Tuple<int, double>>> sides = new List<List<Tuple<int, double>>>();
            HashSet<int> usedEdgeIds = new HashSet<int>();
            foreach (var edgeId in wall.DirectionEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                foreach (var vertexId in vertexIds.Where(sourceVertexIds.Contains))
                {
                    List<Tuple<int, double>> side = GetOrderedSideVertices(wall.DirectionEdgeIds, vertexId, usedEdgeIds);
                    if (side != null) sides.Add(side);
                }
            }
            if (sides.Count != 2) return pairs;
            AddSideVertexPairs(sides[0], sides[1], pairs);
            sides = sides.Select(side => side.Take(side.Count - 1).ToList()).ToList();
            // The source and the target side, each from the end the first side along the sweep joins
            if (wall.SourceEdgeIds.Concat(wall.TargetEdgeIds).All(isChainEdge))
            {
                List<Tuple<int, double>> source = GetOrderedSideVertices(wall.SourceEdgeIds, sides[0][0].Item1,
                                                                         new HashSet<int>());
                List<Tuple<int, double>> target = GetOrderedSideVertices(wall.TargetEdgeIds, sides[0].Last().Item1,
                                                                         new HashSet<int>());
                if (source != null && target != null) AddSideVertexPairs(source, target, pairs);
            }
            return pairs;
        }
        /// <summary>
        /// The vertices a chain of edges passes from the given vertex on, with the fraction of the chain's length
        /// each lies at. The edges walked are added to the used ones. Null when the vertex starts no chain.
        /// </summary>
        private static List<Tuple<int, double>> GetOrderedSideVertices(int[] edgeIds, int startVertexId,
                                                                      HashSet<int> usedEdgeIds)
        {
            int[] vertexIds;
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            foreach (var edgeId in edgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                if (vertexIds.Length != 2) return null;
                edgeIdVertexIds.Add(edgeId, vertexIds);
            }
            double length;
            double position = 0;
            int vertexId = startVertexId;
            List<Tuple<int, double>> side = new List<Tuple<int, double>>() { new Tuple<int, double>(vertexId, 0) };
            while (true)
            {
                int edgeId = edgeIdVertexIds.Keys.FirstOrDefault(id => !usedEdgeIds.Contains(id) &&
                                                                       edgeIdVertexIds[id].Contains(vertexId));
                if (edgeId == 0) break;
                usedEdgeIds.Add(edgeId);
                Gmsh.Model.OCC.GetMass(1, edgeId, out length);
                position += length;
                vertexId = edgeIdVertexIds[edgeId][0] == vertexId ? edgeIdVertexIds[edgeId][1] : edgeIdVertexIds[edgeId][0];
                side.Add(new Tuple<int, double>(vertexId, position));
            }
            if (side.Count < 2 || position <= 0) return null;
            // The last entry keeps the length of the side, for the pairing to compare
            List<Tuple<int, double>> fractions = side.Select(entry => new Tuple<int, double>(entry.Item1,
                                                                                         entry.Item2 / position)).ToList();
            fractions.Add(new Tuple<int, double>(-1, position));
            return fractions;
        }
        /// <summary>
        /// The stretches of the chains that must carry the same number of elements, in pairs of edge lists: between two
        /// levels along the sweep every chain running through both carries one count. The levels are the ones the layer
        /// counts are resolved over - a vertex reached by several chains of a family, the vertices of a cross-sweep side
        /// of a wall and the vertex pairs across it within the family of the wall, and the ends of a block across every
        /// family of its chains, which makes the families of a block count the same layers.
        /// </summary>
        public static List<int[][]> GetLevelSegmentPairs(List<SweepChain> chains, List<SweepWall> walls,
                                                         List<int[]> crossSectionVertexIds,
                                                         List<int> crossSectionBlockIndices, List<int> chainBlockIndices)
        {
            List<int[][]> pairs = new List<int[][]>();
            if (chains.Count == 0) return pairs;
            //
            Dictionary<int, List<int>> edgeIdChainIndices;
            int[] familyParent = GetChainFamilyParents(chains, walls, out edgeIdChainIndices);
            // The faces at one end of a block meet at vertices no chain runs through - the top of a plate and the tops of
            // the bosses standing on it - and are one level all the same
            List<int[]> mergedSectionVertexIds = new List<int[]>();
            List<int> mergedSectionBlockIndices = new List<int>();
            foreach (var block in Enumerable.Range(0, crossSectionVertexIds.Count).GroupBy(g => crossSectionBlockIndices[g]))
            {
                int[] sections = block.ToArray();
                int[] sectionParent = Enumerable.Range(0, sections.Length).ToArray();
                for (int i = 0; i < sections.Length; i++)
                {
                    for (int j = i + 1; j < sections.Length; j++)
                    {
                        if (crossSectionVertexIds[sections[i]].Intersect(crossSectionVertexIds[sections[j]]).Any())
                            Unite(sectionParent, new List<int>() { i, j });
                    }
                }
                foreach (var merged in Enumerable.Range(0, sections.Length).GroupBy(i => Find(sectionParent, i)))
                {
                    mergedSectionVertexIds.Add(merged.SelectMany(i => crossSectionVertexIds[sections[i]]).Distinct().ToArray());
                    mergedSectionBlockIndices.Add(block.Key);
                }
            }
            List<int> groupBlockIndices;
            List<int[]> groups = GetLevelVertexGroups(walls, mergedSectionVertexIds, mergedSectionBlockIndices,
                                                      edgeIdChainIndices.ContainsKey, out groupBlockIndices);
            Func<int, int> getBlock = c => chainBlockIndices != null && c < chainBlockIndices.Count ? chainBlockIndices[c] : -1;
            // Every chain vertex is a position
            List<int> chainOfPosition = new List<int>();
            int[] firstPosition = new int[chains.Count];
            Dictionary<int, List<int>> vertexIdPositions = new Dictionary<int, List<int>>();
            for (int c = 0; c < chains.Count; c++)
            {
                firstPosition[c] = chainOfPosition.Count;
                foreach (var vertexId in chains[c].VertexIds)
                {
                    List<int> positions;
                    if (!vertexIdPositions.TryGetValue(vertexId, out positions))
                        vertexIdPositions.Add(vertexId, positions = new List<int>());
                    positions.Add(chainOfPosition.Count);
                    chainOfPosition.Add(c);
                }
            }
            int[] level = Enumerable.Range(0, chainOfPosition.Count).ToArray();
            Func<int, int> getFamily = position => Find(familyParent, chainOfPosition[position]);
            foreach (var positions in vertexIdPositions.Values)
            {
                foreach (var family in positions.GroupBy(getFamily)) Unite(level, family.ToList());
            }
            for (int g = 0; g < groups.Count; g++)
            {
                List<int> positions;
                List<int> groupPositions = new List<int>();
                foreach (var vertexId in groups[g])
                {
                    if (!vertexIdPositions.TryGetValue(vertexId, out positions)) continue;
                    groupPositions.AddRange(positions.Where(position => groupBlockIndices[g] < 0 ||
                                                                        getBlock(chainOfPosition[position]) < 0 ||
                                                                        getBlock(chainOfPosition[position]) == groupBlockIndices[g]));
                }
                // A face holding two vertices of one chain crosses the sweep of that family rather than cutting it
                List<List<int>> familyParts = groupPositions.Distinct().GroupBy(getFamily).Select(part => part.ToList())
                                                            .Where(part => part.Select(position => chainOfPosition[position])
                                                                               .Distinct().Count() == part.Count)
                                                            .ToList();
                if (groupBlockIndices[g] >= 0)
                {
                    Unite(level, familyParts.SelectMany(part => part).ToList());
                    continue;
                }
                foreach (var part in familyParts) Unite(level, part);
            }
            // Every stretch of a chain between two of its levels, keyed by the two levels
            Dictionary<long, int[]> levelPairEdgeIds = new Dictionary<long, int[]>();
            for (int c = 0; c < chains.Count; c++)
            {
                int[] edgeIds = chains[c].EdgeIds;
                for (int i = 0; i < edgeIds.Length; i++)
                {
                    for (int j = i + 1; j <= edgeIds.Length; j++)
                    {
                        long levelI = Find(level, firstPosition[c] + i);
                        long levelJ = Find(level, firstPosition[c] + j);
                        if (levelI == levelJ) continue;
                        long key = Math.Min(levelI, levelJ) * chainOfPosition.Count + Math.Max(levelI, levelJ);
                        int[] stretch = edgeIds.Skip(i).Take(j - i).ToArray();
                        int[] other;
                        if (!levelPairEdgeIds.TryGetValue(key, out other)) levelPairEdgeIds.Add(key, stretch);
                        else if (!other.OrderBy(id => id).SequenceEqual(stretch.OrderBy(id => id)))
                            pairs.Add(new int[][] { other, stretch });
                    }
                }
            }
            return pairs;
        }
        /// <summary>
        /// The families of the chains: chains joined by a shared edge or by a wall they are the two sides of, as the
        /// parents of a union, with the chains every edge lies on.
        /// </summary>
        private static int[] GetChainFamilyParents(List<SweepChain> chains, List<SweepWall> walls,
                                                   out Dictionary<int, List<int>> edgeIdChainIndices)
        {
            int[] parent = Enumerable.Range(0, chains.Count).ToArray();
            edgeIdChainIndices = new Dictionary<int, List<int>>();
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
            return parent;
        }
        /// <summary>
        /// The groups of vertices that lie on one level along the sweep, each with the block it is a level of, or -1 for
        /// a level of every block.
        /// </summary>
        private static List<int[]> GetLevelVertexGroups(List<SweepWall> walls, List<int[]> crossSectionVertexIds,
                                                        List<int> crossSectionBlockIndices, Func<int, bool> isChainEdge,
                                                        out List<int> levelGroupBlockIndices)
        {
            // The vertices that lie on one level: those of every cross-sweep side of a wall, and those of every
            // cross section face. A side is taken whole, not an edge at a time: a split carried across onto the
            // end of a block cuts the side into several edges, and the vertex between them lies on no chain, so
            // edge by edge the two ends of the side would no longer be tied to one level
            // A cross section face is the end of one block and is a level only of that block's chains: the end of a
            // block swept across a bar holds vertices of the bar's chains at different places along it, and
            // with every chain of a compound in one family would tie them all to one level
            int[] vertexIds;
            List<int[]> levelVertexIds = new List<int[]>(crossSectionVertexIds);
            levelGroupBlockIndices = new List<int>(crossSectionBlockIndices);
            foreach (var wall in walls)
            {
                foreach (var sideEdgeIds in new int[][] { wall.SourceEdgeIds, wall.TargetEdgeIds })
                {
                    int[] runParent = null;
                    Dictionary<int, int> vertexIdIndex = new Dictionary<int, int>();
                    List<int[]> edgeVertexIds = new List<int[]>();
                    foreach (var edgeId in sideEdgeIds)
                    {
                        Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                        edgeVertexIds.Add(vertexIds);
                        foreach (var vertexId in vertexIds)
                        {
                            if (!vertexIdIndex.ContainsKey(vertexId)) vertexIdIndex.Add(vertexId, vertexIdIndex.Count);
                        }
                    }
                    runParent = Enumerable.Range(0, vertexIdIndex.Count).ToArray();
                    foreach (var ids in edgeVertexIds) Unite(runParent, ids.Select(id => vertexIdIndex[id]).ToList());
                    foreach (var run in vertexIdIndex.GroupBy(entry => Find(runParent, entry.Value)))
                    {
                        if (run.Count() < 2) continue;
                        levelVertexIds.Add(run.Select(entry => entry.Key).ToArray());
                        levelGroupBlockIndices.Add(-1);
                    }
                }
                // The two sides of a wall along the sweep run in step, so a vertex partway along one of them and
                // a vertex at the same fraction of the other are where one grid line of the wall crosses them.
                // Splits carried across the neighbours of the wall put such vertices on both sides at one place
                // with no edge between them - the two splits lie on the faces on either side of the wall - and
                // only this ties them to one level
                foreach (var pair in GetWallSideVertexPairs(wall, isChainEdge))
                {
                    levelVertexIds.Add(pair);
                    levelGroupBlockIndices.Add(-1);
                }
            }
            return levelVertexIds;
        }
        /// <summary>
        /// Pairs the vertices partway along two sides of equal length that lie at the same fraction of them. Two
        /// sides of different lengths are not in step, and the same fraction says nothing about them.
        /// </summary>
        private static void AddSideVertexPairs(List<Tuple<int, double>> side1, List<Tuple<int, double>> side2,
                                               List<int[]> pairs)
        {
            const double tolerance = 1E-3;
            double length1 = side1.Last().Item2;
            double length2 = side2.Last().Item2;
            if (Math.Abs(length1 - length2) > 1E-2 * Math.Max(length1, length2)) return;
            for (int i = 1; i < side1.Count - 2; i++)
            {
                for (int j = 1; j < side2.Count - 2; j++)
                {
                    if (Math.Abs(side1[i].Item2 - side2[j].Item2) <= tolerance)
                        pairs.Add(new int[] { side1[i].Item1, side2[j].Item1 });
                }
            }
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
