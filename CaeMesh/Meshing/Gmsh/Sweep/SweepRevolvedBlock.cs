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
using CaeGlobals;
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// A sweep block that turns its source about an axis the source touches, which the sweep between two ends cannot
    /// mesh: the lines of the source nodes on the axis collapse into a point, and the elements beside the axis
    /// collapse into wedges, pyramids and tetrahedra.
    ///
    /// The block keeps the CAD entities of the model, so that its neighbours share its surfaces. Its walls may be split
    /// along the revolution, by the faces of a neighbour standing on them, and every angle a vertex of the block stands
    /// at is a level the layers end on: between two levels the layers are equal. gmsh meshes the source and the
    /// curves, with the counts the block sets before: every arc gets the layers of the levels it spans, and every edge
    /// standing at a level the count of the source edges turned onto it. Once the surfaces are meshed the block builds
    /// everything else by turning the source mesh. Every turned source entity is found where it lands - on a vertex,
    /// on an edge, inside a wall or inside the volume - the nodes already on the edges are moved to where the turning
    /// puts them, in their order along the edge, and the walls, the target and the volume are meshed by the turned
    /// source. It is built before the blocks swept between two ends, and before the cells left to tetrahedra, so that
    /// both read its surfaces as they end.
    /// </summary>
    public class SweepRevolvedBlock
    {
        // Variables
        private readonly SweepBlock _block;
        private readonly double[] _center;
        private readonly double[] _direction;
        private readonly double _angleRad;
        private readonly double _tolerance;
        private readonly double _angleTolerance;
        private readonly HashSet<int> _sourceVertexIds;
        private readonly HashSet<int> _sourceEdgeIds;
        private readonly HashSet<int> _boundaryEdgeIds;
        private readonly HashSet<int> _boundaryVertexIds;
        private readonly HashSet<int> _targetVertexIds;
        private readonly HashSet<int> _targetEdgeIds;
        private readonly HashSet<int> _wallVertexIds;
        private readonly HashSet<int> _wallEdgeIds;
        private readonly Dictionary<int, double[]> _wallSpans;
        private readonly Dictionary<int, double[]> _vertexCoor;
        private readonly double[] _levels;
        private readonly List<Tuple<int, double, double>> _arcs;
        private readonly List<Tuple<int[], int>> _edgeChains;


        // Properties
        public SweepBlock Block { get { return _block; } }
        /// <summary>
        /// The angles the vertices of the block stand at, measured from the source in the sense it is turned, from 0
        /// to the angle of the block.
        /// </summary>
        public double[] Levels { get { return _levels; } }
        /// <summary>The edges that turn about the axis, each with the angles it starts and ends at.</summary>
        public List<Tuple<int, double, double>> Arcs { get { return _arcs; } }
        public IEnumerable<int> ArcEdgeIds { get { return _arcs.Select(arc => arc.Item1); } }
        /// <summary>
        /// Every edge standing at a level, a target edge or one splitting a wall along the revolution, and the source
        /// edges turned onto it, which give it the sum of their counts.
        /// </summary>
        public List<Tuple<int[], int>> TargetEdgeChains { get { return _edgeChains; } }


        // Constructors
        public SweepRevolvedBlock(SweepBlock block)
        {
            _block = block;
            _center = new double[] { block.RevolveAxis[0], block.RevolveAxis[1], block.RevolveAxis[2] };
            _direction = Normalize(new double[] { block.RevolveAxis[3], block.RevolveAxis[4], block.RevolveAxis[5] });
            _angleRad = block.RevolveAxis[6] * Math.PI / 180;
            if (!(_angleRad > 0) || _angleRad >= 2 * Math.PI - 1E-9)
                throw new CaeException("The angle of " + GetDescription() + " must be larger than 0° and smaller than 360°.");
            _angleTolerance = 1E-6;
            //
            int[] upward;
            int[] downward;
            // The edges of the source, those used by one source surface only being its boundary
            Dictionary<int, int> edgeUseCount = new Dictionary<int, int>();
            foreach (var surfaceId in block.SourceSurfaceIds)
            {
                Gmsh.Model.GetAdjacencies(2, surfaceId, out upward, out downward);
                foreach (var edgeId in downward.Select(Math.Abs).Distinct())
                {
                    edgeUseCount.TryGetValue(edgeId, out int count);
                    edgeUseCount[edgeId] = count + 1;
                }
            }
            _sourceEdgeIds = new HashSet<int>(edgeUseCount.Keys);
            Dictionary<int, int[]> edgeVertexIds = new Dictionary<int, int[]>();
            foreach (var edgeId in _sourceEdgeIds) edgeVertexIds.Add(edgeId, GetEdgeVertexIds(edgeId));
            _sourceVertexIds = new HashSet<int>(_sourceEdgeIds.SelectMany(id => edgeVertexIds[id]));
            // The edges and the vertices of the walls and of the target
            _wallEdgeIds = new HashSet<int>();
            foreach (var surfaceId in block.WallSurfaceIds)
            {
                Gmsh.Model.GetAdjacencies(2, surfaceId, out upward, out downward);
                _wallEdgeIds.UnionWith(downward.Select(Math.Abs));
            }
            _targetEdgeIds = new HashSet<int>();
            foreach (var surfaceId in block.TargetSurfaceIds)
            {
                Gmsh.Model.GetAdjacencies(2, surfaceId, out upward, out downward);
                _targetEdgeIds.UnionWith(downward.Select(Math.Abs));
            }
            _wallEdgeIds.ExceptWith(_sourceEdgeIds);
            _targetEdgeIds.ExceptWith(_sourceEdgeIds);
            foreach (var edgeId in _wallEdgeIds.Concat(_targetEdgeIds))
            {
                if (!edgeVertexIds.ContainsKey(edgeId)) edgeVertexIds.Add(edgeId, GetEdgeVertexIds(edgeId));
            }
            _wallVertexIds = new HashSet<int>(_wallEdgeIds.SelectMany(id => edgeVertexIds[id]));
            _targetVertexIds = new HashSet<int>(_targetEdgeIds.SelectMany(id => edgeVertexIds[id]));
            // The tolerance follows the size of the block
            BoundingBox box = new BoundingBox();
            _vertexCoor = new Dictionary<int, double[]>();
            foreach (var vertexId in _sourceVertexIds.Concat(_wallVertexIds).Concat(_targetVertexIds).Distinct())
            {
                _vertexCoor.Add(vertexId, GetVertexCoor(vertexId));
                box.IncludeCoor(_vertexCoor[vertexId]);
            }
            _tolerance = 1E-6 * Math.Max(box.GetDiagonal(), 1E-6);
            // The boundary of the source off the axis
            _boundaryEdgeIds = new HashSet<int>(_sourceEdgeIds.Where(id => edgeUseCount[id] == 1 &&
                                                                           !IsAxisEdge(id, edgeVertexIds[id])));
            _boundaryVertexIds = new HashSet<int>(_boundaryEdgeIds.SelectMany(id => edgeVertexIds[id])
                                                                  .Where(id => !IsOnAxis(_vertexCoor[id])));
            // The angle of every vertex, from the source vertex on its circle
            Dictionary<int, double> vertexAngles = new Dictionary<int, double>();
            foreach (var entry in _vertexCoor)
            {
                if (IsOnAxis(entry.Value)) continue;
                if (_sourceVertexIds.Contains(entry.Key)) vertexAngles.Add(entry.Key, 0);
                else vertexAngles.Add(entry.Key, GetAngleFromSource(entry.Value));
            }
            List<double> levels = new List<double>() { 0, _angleRad };
            foreach (var angle in vertexAngles.Values)
            {
                if (!levels.Any(level => Math.Abs(level - angle) <= _angleTolerance)) levels.Add(angle);
            }
            _levels = levels.OrderBy(level => level).ToArray();
            // The angles a wall spans, from its vertices off the axis
            _wallSpans = new Dictionary<int, double[]>();
            foreach (var surfaceId in block.WallSurfaceIds)
            {
                Gmsh.Model.GetAdjacencies(2, surfaceId, out upward, out downward);
                double[] angles = downward.Select(Math.Abs).SelectMany(id => edgeVertexIds.ContainsKey(id) ?
                                                                       edgeVertexIds[id] : GetEdgeVertexIds(id))
                                          .Where(id => vertexAngles.ContainsKey(id)).Select(id => vertexAngles[id])
                                          .ToArray();
                if (angles.Length == 0)
                    throw new CaeException("A wall of " + GetDescription() + " has no vertex off the axis.");
                _wallSpans.Add(surfaceId, new double[] { angles.Min(), angles.Max() });
            }
            // Every edge of the walls and of the target either turns about the axis or stands at a level
            _arcs = new List<Tuple<int, double, double>>();
            Dictionary<int, double> levelEdgeAngles = new Dictionary<int, double>();
            // An edge on the axis, like the degenerate edge at the pole of a sphere, has no node off it to turn, and is
            // no edge a turned node can land on
            int[] axisEdgeIds = _wallEdgeIds.Concat(_targetEdgeIds)
                                            .Where(id => !edgeVertexIds[id].Any(vertexId => vertexAngles.ContainsKey(vertexId)))
                                            .ToArray();
            _wallEdgeIds.ExceptWith(axisEdgeIds);
            _targetEdgeIds.ExceptWith(axisEdgeIds);
            foreach (var edgeId in _wallEdgeIds.Concat(_targetEdgeIds).Distinct())
            {
                int[] vertexIds = edgeVertexIds[edgeId];
                int[] offAxisIds = vertexIds.Where(id => vertexAngles.ContainsKey(id)).ToArray();
                if (offAxisIds.Length == 2 && vertexIds.Length == 2 && IsArc(edgeId, vertexIds))
                {
                    double start = vertexAngles[vertexIds[0]];
                    double end = vertexAngles[vertexIds[1]];
                    _arcs.Add(new Tuple<int, double, double>(edgeId, Math.Min(start, end), Math.Max(start, end)));
                    continue;
                }
                double[] angles = offAxisIds.Select(id => vertexAngles[id]).ToArray();
                if (angles.Length == 0 || angles.Max() - angles.Min() > _angleTolerance)
                    throw new CaeException(NameEdge(edgeId) + " of " + GetDescription() + " neither turns about the " +
                                           "axis nor stands at one angle of the revolution.");
                levelEdgeAngles.Add(edgeId, angles[0]);
            }
            // Every edge standing at a level is the image of the source edges turned onto it there
            _edgeChains = new List<Tuple<int[], int>>();
            foreach (var entry in levelEdgeAngles)
            {
                bool atTarget = Math.Abs(entry.Value - _angleRad) <= _angleTolerance;
                IEnumerable<int> candidates = atTarget ? _sourceEdgeIds.Where(id => !IsAxisEdge(id, edgeVertexIds[id]))
                                                       : _boundaryEdgeIds;
                int[] chain = candidates.Where(id => IsOnEdge(entry.Key, Turn(GetEdgeMiddle(id), entry.Value)))
                                        .OrderBy(id => id).ToArray();
                if (chain.Length == 0)
                    throw new CaeException(NameEdge(entry.Key) + " of " + GetDescription() + " stands at an angle of " +
                                           "the revolution, but no edge of the source is turned onto it.");
                _edgeChains.Add(new Tuple<int[], int>(chain, entry.Key));
            }
        }


        // Methods
        /// <summary>
        /// Builds the mesh of the block from the meshed source: the source mesh is turned through the layers, equal
        /// between two of the given levels and as many there as the counts say, and every turned node is put where it
        /// lands. A wall two revolved blocks share is meshed by the one built first, and the nodes it made there are
        /// kept by their source node and layer, so that the other block uses them rather than meshing the wall again.
        /// </summary>
        public void Build(double[] levels, int[] intervalCounts, HashSet<int> builtWallIds,
                          Dictionary<Tuple<IntPtr, int>, IntPtr> wallNodeTags)
        {
            int numLayers = intervalCounts.Sum();
            double[] angles = new double[numLayers + 1];
            bool[] atLevel = new bool[numLayers + 1];
            int[] layerInterval = new int[numLayers];
            int layer = 0;
            atLevel[0] = true;
            for (int i = 0; i < intervalCounts.Length; i++)
            {
                for (int j = 0; j < intervalCounts[i]; j++)
                {
                    layerInterval[layer] = i;
                    angles[layer] = levels[i] + (levels[i + 1] - levels[i]) * j / intervalCounts[i];
                    layer++;
                }
                atLevel[layer] = true;
            }
            angles[numLayers] = _angleRad;
            HashSet<int> builtBefore = new HashSet<int>(builtWallIds);
            // The walls are meshed again from the turned source edges, and the target from the turned source
            List<Tuple<int, int>> clearDimTags = new List<Tuple<int, int>>();
            foreach (var wallId in _wallSpans.Keys)
            {
                if (!builtBefore.Contains(wallId)) clearDimTags.Add(new Tuple<int, int>(2, wallId));
            }
            foreach (var targetId in _block.TargetSurfaceIds) clearDimTags.Add(new Tuple<int, int>(2, targetId));
            Gmsh.Model.Mesh.Clear(clearDimTags.ToArray());
            // The source nodes and the entities they are classified on
            Dictionary<IntPtr, double[]> sourceCoor = new Dictionary<IntPtr, double[]>();
            Dictionary<IntPtr, Tuple<int, int>> sourceEntity = new Dictionary<IntPtr, Tuple<int, int>>();
            AddSourceNodes(0, _sourceVertexIds, sourceCoor, sourceEntity);
            AddSourceNodes(1, _sourceEdgeIds, sourceCoor, sourceEntity);
            AddSourceNodes(2, _block.SourceSurfaceIds, sourceCoor, sourceEntity);
            Dictionary<int, double[]> edgeMiddles = new Dictionary<int, double[]>();
            foreach (var edgeId in _sourceEdgeIds) edgeMiddles.Add(edgeId, GetEdgeMiddle(edgeId));
            // The line of every source node through the layers
            IntPtr maxNodeTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxNodeTag);
            long nextNodeTag = maxNodeTag.ToInt64() + 1;
            List<IntPtr> volumeTags = new List<IntPtr>();
            List<double> volumeCoor = new List<double>();
            Dictionary<int, List<IntPtr>> wallTags = new Dictionary<int, List<IntPtr>>();
            Dictionary<int, List<double>> wallCoor = new Dictionary<int, List<double>>();
            List<IntPtr> targetTags = new List<IntPtr>();
            List<double> targetCoor = new List<double>();
            Dictionary<IntPtr, IntPtr[]> lines = new Dictionary<IntPtr, IntPtr[]>();
            Dictionary<int, List<Tuple<IntPtr[], int, double[]>>> edgeRequests =
                new Dictionary<int, List<Tuple<IntPtr[], int, double[]>>>();
            Dictionary<Tuple<int, int, int>, Tuple<int, int>> locations = new Dictionary<Tuple<int, int, int>, Tuple<int, int>>();
            foreach (var entry in sourceCoor)
            {
                IntPtr[] line = new IntPtr[numLayers + 1];
                line[0] = entry.Key;
                lines.Add(entry.Key, line);
                double[] point = entry.Value;
                if (IsOnAxis(point))
                {
                    for (int k = 1; k <= numLayers; k++) line[k] = entry.Key;
                    continue;
                }
                int dim = sourceEntity[entry.Key].Item1;
                int tag = sourceEntity[entry.Key].Item2;
                bool boundary = (dim == 0 && _boundaryVertexIds.Contains(tag)) || (dim == 1 && _boundaryEdgeIds.Contains(tag));
                for (int k = 1; k <= numLayers; k++)
                {
                    double[] turned = Turn(point, angles[k]);
                    if (!boundary && k < numLayers)
                    {
                        line[k] = (IntPtr)nextNodeTag++;
                        volumeTags.Add(line[k]);
                        volumeCoor.AddRange(turned);
                        continue;
                    }
                    // Where the source entity lands at this layer, found once for all its nodes
                    Tuple<int, int, int> key = new Tuple<int, int, int>(dim, tag, k);
                    Tuple<int, int> location;
                    if (!locations.TryGetValue(key, out location))
                    {
                        double[] representative = dim == 0 ? _vertexCoor[tag] : dim == 1 ? edgeMiddles[tag] : point;
                        location = Locate(Turn(representative, angles[k]), angles[k], dim, atLevel[k], k == numLayers);
                        locations.Add(key, location);
                    }
                    if (location.Item1 == 0) line[k] = GetVertexNodeTag(location.Item2);
                    else if (location.Item1 == 1)
                    {
                        if (!edgeRequests.ContainsKey(location.Item2))
                            edgeRequests.Add(location.Item2, new List<Tuple<IntPtr[], int, double[]>>());
                        edgeRequests[location.Item2].Add(new Tuple<IntPtr[], int, double[]>(line, k, turned));
                    }
                    else if (k == numLayers)
                    {
                        line[k] = (IntPtr)nextNodeTag++;
                        targetTags.Add(line[k]);
                        targetCoor.AddRange(turned);
                    }
                    else
                    {
                        int wallId = location.Item2;
                        Tuple<IntPtr, int> wallKey = new Tuple<IntPtr, int>(entry.Key, k);
                        if (builtBefore.Contains(wallId))
                        {
                            if (!wallNodeTags.TryGetValue(wallKey, out line[k]))
                                throw new CaeException("A wall " + GetDescription() + " shares with a revolved block " +
                                                       "built before it has no node where the turned source puts one.");
                            continue;
                        }
                        if (!wallTags.ContainsKey(wallId))
                        {
                            wallTags.Add(wallId, new List<IntPtr>());
                            wallCoor.Add(wallId, new List<double>());
                        }
                        line[k] = (IntPtr)nextNodeTag++;
                        wallTags[wallId].Add(line[k]);
                        wallCoor[wallId].AddRange(turned);
                        wallNodeTags[wallKey] = line[k];
                    }
                }
            }
            // The nodes on the edges are paired with the turned source nodes in their order along the edge, and moved
            // onto them
            IntPtr[] tags;
            double[] coor;
            foreach (var entry in edgeRequests)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 1, entry.Key, false, false);
                if (tags.Length != entry.Value.Count)
                    throw new CaeException(NameEdge(entry.Key) + " of " + GetDescription() + " has " + tags.Length +
                                           " nodes inside it, but the turned source puts " + entry.Value.Count +
                                           " there.");
                Dictionary<IntPtr, double[]> nodeCoor = new Dictionary<IntPtr, double[]>();
                for (int i = 0; i < tags.Length; i++)
                    nodeCoor.Add(tags[i], new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] });
                MoveEdgeNodes(entry.Key, tags.ToList(), nodeCoor, entry.Value);
            }
            // The new nodes: inside the volume, on the walls, and on the target surfaces
            if (volumeTags.Count > 0)
                Gmsh.Model.Mesh.AddNodes(3, _block.VolumeId, volumeTags.ToArray(), volumeCoor.ToArray());
            foreach (var entry in wallTags)
            {
                double[] wallPoints = wallCoor[entry.Key].ToArray();
                double[] parametric;
                Gmsh.Model.GetParametrization(2, entry.Key, wallPoints, out parametric);
                Gmsh.Model.Mesh.AddNodes(2, entry.Key, entry.Value.ToArray(), wallPoints, parametric);
            }
            Dictionary<IntPtr, int> targetNodeSurfaceIds = AddTargetNodes(targetTags, targetCoor);
            // The elements
            AddElements(lines, sourceCoor, angles, layerInterval, levels, edgeMiddles, targetNodeSurfaceIds, builtBefore);
            CheckSurfaceBoundaries(_block.TargetSurfaceIds.Concat(_wallSpans.Keys.Where(id => !builtBefore.Contains(id))));
            builtWallIds.UnionWith(_wallSpans.Keys);
        }
        /// <summary>
        /// Where a turned source entity lands, as the dimension and the tag of the entity: a vertex, an edge, a wall
        /// or, on the target layer, a target surface. A vertex is found at any layer, an edge at a level or, for a
        /// source vertex, on the arc it turns along, and a wall between the levels or where no edge stands.
        /// </summary>
        private Tuple<int, int> Locate(double[] point, double angle, int dim, bool atLevel, bool atTarget)
        {
            if (dim == 0)
            {
                foreach (var vertexId in atTarget ? _targetVertexIds : _wallVertexIds)
                {
                    if (Distance(_vertexCoor[vertexId], point) <= 100 * _tolerance) return new Tuple<int, int>(0, vertexId);
                }
            }
            if (dim <= 1 && (atLevel || dim == 0))
            {
                foreach (var edgeId in atTarget ? _targetEdgeIds : _wallEdgeIds)
                {
                    if (IsOnEdge(edgeId, point)) return new Tuple<int, int>(1, edgeId);
                }
            }
            if (atTarget) return new Tuple<int, int>(2, -1);
            return new Tuple<int, int>(2, GetWallId(point, angle));
        }
        /// <summary>
        /// The wall a point at the given angle lies on: the one spanning the angle whose surface passes through it.
        /// </summary>
        private int GetWallId(double[] point, double angle)
        {
            int wallId = -1;
            double bestDistance = double.MaxValue;
            foreach (var entry in _wallSpans)
            {
                if (angle < entry.Value[0] - _angleTolerance || angle > entry.Value[1] + _angleTolerance) continue;
                //
                double[] closest;
                double[] parametric;
                Gmsh.Model.GetClosestPoint(2, entry.Key, point, out closest, out parametric);
                double distance = Distance(closest, point);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    wallId = entry.Key;
                }
            }
            if (wallId < 0 || bestDistance > 1E3 * _tolerance)
                throw new CaeException("A turned edge of the source of " + GetDescription() + " lies on none of its " +
                                       "walls.");
            return wallId;
        }
        private void AddSourceNodes(int dim, IEnumerable<int> entityIds, Dictionary<IntPtr, double[]> sourceCoor,
                                    Dictionary<IntPtr, Tuple<int, int>> sourceEntity)
        {
            IntPtr[] tags;
            double[] coor;
            foreach (var entityId in entityIds)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, dim, entityId, false, false);
                for (int i = 0; i < tags.Length; i++)
                {
                    if (sourceCoor.ContainsKey(tags[i])) continue;
                    sourceCoor.Add(tags[i], new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] });
                    sourceEntity.Add(tags[i], new Tuple<int, int>(dim, entityId));
                }
            }
        }
        /// <summary>
        /// Pairs the nodes inside an edge with the points the turned source puts there, both in their order along the
        /// edge, moves every node onto its point and puts it in the line that asked for it.
        /// </summary>
        private static void MoveEdgeNodes(int edgeId, List<IntPtr> nodeTags, Dictionary<IntPtr, double[]> nodeCoor,
                                          List<Tuple<IntPtr[], int, double[]>> requests)
        {
            if (nodeTags.Count == 0) return;
            //
            double[] nodePoints = nodeTags.SelectMany(tag => nodeCoor[tag]).ToArray();
            double[] requestPoints = requests.SelectMany(request => request.Item3).ToArray();
            double[] nodeParameters;
            double[] requestParameters;
            Gmsh.Model.GetParametrization(1, edgeId, nodePoints, out nodeParameters);
            Gmsh.Model.GetParametrization(1, edgeId, requestPoints, out requestParameters);
            int[] nodeOrder = Enumerable.Range(0, nodeTags.Count).OrderBy(i => nodeParameters[i]).ToArray();
            int[] requestOrder = Enumerable.Range(0, requests.Count).OrderBy(i => requestParameters[i]).ToArray();
            for (int i = 0; i < nodeOrder.Length; i++)
            {
                IntPtr tag = nodeTags[nodeOrder[i]];
                Tuple<IntPtr[], int, double[]> request = requests[requestOrder[i]];
                request.Item1[request.Item2] = tag;
                Gmsh.Model.Mesh.SetNode(tag, request.Item3, new double[] { requestParameters[requestOrder[i]] });
            }
        }
        /// <summary>
        /// Adds the turned interior source nodes to the target surfaces they lie on and returns the surface of each.
        /// </summary>
        private Dictionary<IntPtr, int> AddTargetNodes(List<IntPtr> targetTags, List<double> targetCoor)
        {
            Dictionary<IntPtr, int> targetNodeSurfaceIds = new Dictionary<IntPtr, int>();
            int count = targetTags.Count;
            if (count == 0) return targetNodeSurfaceIds;
            //
            double[] input = targetCoor.ToArray();
            double[] bestCoor = new double[3 * count];
            double[] bestParam = new double[2 * count];
            double[] bestDistance = new double[count];
            int[] bestSurfaceId = new int[count];
            SweepProjector.ProjectOntoNearestSurface(input, _block.TargetSurfaceIds, bestCoor, bestParam, bestDistance,
                                                     bestSurfaceId);
            Dictionary<int, List<int>> surfaceIndices = new Dictionary<int, List<int>>();
            for (int i = 0; i < count; i++)
            {
                if (bestSurfaceId[i] < 0)
                    throw new CaeException("A node of the target of " + GetDescription() + " could not be put on a " +
                                           "target surface.");
                if (!surfaceIndices.ContainsKey(bestSurfaceId[i])) surfaceIndices.Add(bestSurfaceId[i], new List<int>());
                surfaceIndices[bestSurfaceId[i]].Add(i);
                targetNodeSurfaceIds.Add(targetTags[i], bestSurfaceId[i]);
            }
            foreach (var entry in surfaceIndices)
            {
                List<int> indices = entry.Value;
                IntPtr[] tags = new IntPtr[indices.Count];
                double[] coor = new double[3 * indices.Count];
                double[] parametric = new double[2 * indices.Count];
                for (int j = 0; j < indices.Count; j++)
                {
                    int i = indices[j];
                    tags[j] = targetTags[i];
                    Array.Copy(input, 3 * i, coor, 3 * j, 3);
                    Array.Copy(bestParam, 2 * i, parametric, 2 * j, 2);
                }
                Gmsh.Model.Mesh.AddNodes(2, entry.Key, tags, coor, parametric);
            }
            return targetNodeSurfaceIds;
        }
        private void AddElements(Dictionary<IntPtr, IntPtr[]> lines, Dictionary<IntPtr, double[]> sourceCoor,
                                 double[] angles, int[] layerInterval, double[] levels,
                                 Dictionary<int, double[]> edgeMiddles, Dictionary<IntPtr, int> targetNodeSurfaceIds,
                                 HashSet<int> builtBefore)
        {
            int numLayers = angles.Length - 1;
            IntPtr maxElementTag;
            Gmsh.Model.Mesh.GetMaxElementTag(out maxElementTag);
            long nextElementTag = maxElementTag.ToInt64() + 1;
            Dictionary<Tuple<int, int, int>, List<IntPtr>> batchTags = new Dictionary<Tuple<int, int, int>, List<IntPtr>>();
            Dictionary<Tuple<int, int, int>, List<IntPtr>> batchNodes = new Dictionary<Tuple<int, int, int>, List<IntPtr>>();
            Action<int, int, GmshElementType, IntPtr[]> add = (dim, entityId, type, elementNodeTags) =>
            {
                Tuple<int, int, int> key = new Tuple<int, int, int>(dim, entityId, (int)type);
                if (!batchTags.ContainsKey(key))
                {
                    batchTags.Add(key, new List<IntPtr>());
                    batchNodes.Add(key, new List<IntPtr>());
                }
                batchTags[key].Add((IntPtr)nextElementTag++);
                batchNodes[key].AddRange(elementNodeTags);
            };
            // The source elements, and the side of every source edge its element lies on
            List<IntPtr[]> sourceElements = new List<IntPtr[]>();
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            foreach (var surfaceId in _block.SourceSurfaceIds)
            {
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    int numNodes;
                    if (elementTypes[i] == (int)GmshElementType.LinearTriangle) numNodes = 3;
                    else if (elementTypes[i] == (int)GmshElementType.LinearQuadrilateral) numNodes = 4;
                    else throw new CaeException("The source of " + GetDescription() + " is meshed by elements that " +
                                                "are neither linear triangles nor linear quadrilaterals.");
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        IntPtr[] element = new IntPtr[numNodes];
                        Array.Copy(nodeTags[i], j * numNodes, element, 0, numNodes);
                        sourceElements.Add(element);
                    }
                }
            }
            if (sourceElements.Count == 0) throw new CaeException("The source of " + GetDescription() + " is not meshed.");
            Dictionary<Tuple<IntPtr, IntPtr>, double[]> edgeElementCenter = new Dictionary<Tuple<IntPtr, IntPtr>, double[]>();
            foreach (var element in sourceElements)
            {
                double[] elementCenter = GetCenter(element, sourceCoor);
                for (int i = 0; i < element.Length; i++)
                    edgeElementCenter[GetEdgeKey(element[i], element[(i + 1) % element.Length])] = elementCenter;
            }
            // The volume and the target cap, element by element
            foreach (var element in sourceElements)
            {
                IntPtr[] oriented = RevolveMethods.GetOutwardSourceNodeIds(element, sourceCoor, _center, _direction,
                                                                           angles[1]);
                int numAxisNodes;
                oriented = RevolveMethods.RotateSourceNodeIdsToAxis(oriented, sourceCoor, _center, _direction,
                                                                    _tolerance, out numAxisNodes);
                for (int k = 0; k < numLayers; k++) AddVolumeElements(oriented, numAxisNodes, lines, k, add);
                //
                IntPtr[] cap;
                if (oriented.Length == 3)
                    cap = new IntPtr[] { lines[oriented[0]][numLayers], lines[oriented[2]][numLayers],
                                         lines[oriented[1]][numLayers] };
                else
                    cap = new IntPtr[] { lines[oriented[0]][numLayers], lines[oriented[3]][numLayers],
                                         lines[oriented[2]][numLayers], lines[oriented[1]][numLayers] };
                add(2, GetCapSurfaceId(cap, targetNodeSurfaceIds, sourceCoor, oriented),
                    oriented.Length == 3 ? GmshElementType.LinearTriangle : GmshElementType.LinearQuadrilateral, cap);
            }
            // The walls, from the elements of the boundary edges, each layer on the wall the edge turns across there,
            // their normals away from the source element beside them
            Dictionary<Tuple<int, int>, int> edgeIntervalWallIds = new Dictionary<Tuple<int, int>, int>();
            foreach (var edgeId in _boundaryEdgeIds)
            {
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 1, edgeId);
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    if (elementTypes[i] != (int)GmshElementType.LinearBeam) continue;
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        IntPtr node0 = nodeTags[i][2 * j];
                        IntPtr node1 = nodeTags[i][2 * j + 1];
                        bool onAxis0 = IsOnAxis(sourceCoor[node0]);
                        bool onAxis1 = IsOnAxis(sourceCoor[node1]);
                        if (onAxis0 && onAxis1) continue;
                        //
                        double[] elementCenter;
                        if (!edgeElementCenter.TryGetValue(GetEdgeKey(node0, node1), out elementCenter)) continue;
                        double[] middle = GetCenter(new IntPtr[] { node0, node1 }, sourceCoor);
                        double[] outward = Subtract(middle, elementCenter);
                        double[] along = Subtract(sourceCoor[node1], sourceCoor[node0]);
                        double[] turning = Cross(_direction, Subtract(middle, _center));
                        bool reverse = Dot(Cross(along, turning), outward) < 0;
                        for (int k = 0; k < numLayers; k++)
                        {
                            Tuple<int, int> key = new Tuple<int, int>(edgeId, layerInterval[k]);
                            int wallId;
                            if (!edgeIntervalWallIds.TryGetValue(key, out wallId))
                            {
                                int interval = layerInterval[k];
                                double angle = 0.5 * (levels[interval] + levels[interval + 1]);
                                wallId = GetWallId(Turn(edgeMiddles[edgeId], angle), angle);
                                edgeIntervalWallIds.Add(key, wallId);
                            }
                            if (builtBefore.Contains(wallId)) continue;
                            //
                            IntPtr[] face;
                            if (onAxis0) face = new IntPtr[] { lines[node0][k], lines[node1][k], lines[node1][k + 1] };
                            else if (onAxis1) face = new IntPtr[] { lines[node0][k], lines[node1][k], lines[node0][k + 1] };
                            else face = new IntPtr[] { lines[node0][k], lines[node1][k], lines[node1][k + 1],
                                                       lines[node0][k + 1] };
                            if (reverse) Array.Reverse(face);
                            add(2, wallId, face.Length == 3 ? GmshElementType.LinearTriangle :
                                                              GmshElementType.LinearQuadrilateral, face);
                        }
                    }
                }
            }
            //
            foreach (var entry in batchTags)
            {
                Gmsh.Model.Mesh.AddElements(entry.Key.Item1, entry.Key.Item2, new int[] { entry.Key.Item3 },
                                            new IntPtr[][] { entry.Value.ToArray() },
                                            new IntPtr[][] { batchNodes[entry.Key].ToArray() });
            }
        }
        /// <summary>
        /// Checks that the mesh of every wall and every target surface the block built ends on the line elements of
        /// the edges around it, so that a surface the block does not close is refused here rather than by the mesher
        /// of a neighbouring cell.
        /// </summary>
        private void CheckSurfaceBoundaries(IEnumerable<int> surfaceIds)
        {
            int[] upward;
            int[] edgeIds;
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            foreach (var surfaceId in surfaceIds)
            {
                // The element edges used once are the boundary of the surface mesh
                Dictionary<Tuple<IntPtr, IntPtr>, int> edgeUseCount = new Dictionary<Tuple<IntPtr, IntPtr>, int>();
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    int numNodes = elementTypes[i] == (int)GmshElementType.LinearTriangle ? 3 : 4;
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        for (int k = 0; k < numNodes; k++)
                        {
                            Tuple<IntPtr, IntPtr> key = GetEdgeKey(nodeTags[i][j * numNodes + k],
                                                                   nodeTags[i][j * numNodes + (k + 1) % numNodes]);
                            edgeUseCount.TryGetValue(key, out int count);
                            edgeUseCount[key] = count + 1;
                        }
                    }
                }
                HashSet<Tuple<IntPtr, IntPtr>> boundary =
                    new HashSet<Tuple<IntPtr, IntPtr>>(edgeUseCount.Where(entry => entry.Value == 1).Select(entry => entry.Key));
                // The line elements of the edges around the surface
                HashSet<Tuple<IntPtr, IntPtr>> lines = new HashSet<Tuple<IntPtr, IntPtr>>();
                Gmsh.Model.GetAdjacencies(2, surfaceId, out upward, out edgeIds);
                foreach (var edgeId in edgeIds.Select(Math.Abs))
                {
                    Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 1, edgeId);
                    for (int i = 0; i < elementTypes.Length; i++)
                    {
                        for (int j = 0; j < elementTags[i].Length; j++)
                            lines.Add(GetEdgeKey(nodeTags[i][2 * j], nodeTags[i][2 * j + 1]));
                    }
                }
                if (!boundary.SetEquals(lines))
                    throw new CaeException("The mesh " + GetDescription() + " builds on surface " + surfaceId + " does " +
                                           "not end on the line elements of the edges around it.");
            }
        }
        /// <summary>
        /// The elements one oriented source element makes in one layer. Its nodes on the axis come first, and the
        /// lines through them stay in one point, so an element touching the axis collapses: a triangle with a node on
        /// it into a pyramid and with an edge on it into a tetrahedron, a quadrilateral with a node on it into two
        /// pyramids and with an edge on it into a wedge.
        /// </summary>
        private void AddVolumeElements(IntPtr[] n, int numAxisNodes, Dictionary<IntPtr, IntPtr[]> lines, int k,
                                       Action<int, int, GmshElementType, IntPtr[]> add)
        {
            int volumeId = _block.VolumeId;
            Func<int, int, IntPtr> at = (i, layer) => lines[n[i]][layer];
            if (n.Length == 3)
            {
                if (numAxisNodes == 0)
                    add(3, volumeId, GmshElementType.LinearWedge,
                        new IntPtr[] { at(0, k + 1), at(1, k + 1), at(2, k + 1), at(0, k), at(1, k), at(2, k) });
                else if (numAxisNodes == 1)
                    add(3, volumeId, GmshElementType.LinearPyramid,
                        new IntPtr[] { at(1, k), at(2, k), at(2, k + 1), at(1, k + 1), at(0, k) });
                else if (numAxisNodes == 2)
                    add(3, volumeId, GmshElementType.LinearTetra,
                        new IntPtr[] { at(0, k), at(1, k), at(2, k + 1), at(2, k) });
                else throw new NotSupportedException();
            }
            else
            {
                if (numAxisNodes == 0)
                    add(3, volumeId, GmshElementType.LinearHexa,
                        new IntPtr[] { at(0, k + 1), at(1, k + 1), at(2, k + 1), at(3, k + 1),
                                       at(0, k), at(1, k), at(2, k), at(3, k) });
                else if (numAxisNodes == 1)
                {
                    add(3, volumeId, GmshElementType.LinearPyramid,
                        new IntPtr[] { at(1, k), at(2, k), at(2, k + 1), at(1, k + 1), at(0, k) });
                    add(3, volumeId, GmshElementType.LinearPyramid,
                        new IntPtr[] { at(2, k), at(3, k), at(3, k + 1), at(2, k + 1), at(0, k) });
                }
                else if (numAxisNodes == 2)
                    add(3, volumeId, GmshElementType.LinearWedge,
                        new IntPtr[] { at(3, k), at(0, k), at(3, k + 1), at(2, k), at(1, k), at(2, k + 1) });
                else throw new NotSupportedException();
            }
        }
        private int GetCapSurfaceId(IntPtr[] cap, Dictionary<IntPtr, int> targetNodeSurfaceIds,
                                    Dictionary<IntPtr, double[]> sourceCoor, IntPtr[] oriented)
        {
            if (_block.TargetSurfaceIds.Count == 1) return _block.TargetSurfaceIds.First();
            // The surface most of its nodes inside a target surface lie on, or the one its centre lies on
            Dictionary<int, int> votes = new Dictionary<int, int>();
            foreach (var tag in cap)
            {
                if (!targetNodeSurfaceIds.TryGetValue(tag, out int surfaceId)) continue;
                votes.TryGetValue(surfaceId, out int count);
                votes[surfaceId] = count + 1;
            }
            if (votes.Count > 0) return votes.OrderByDescending(entry => entry.Value).First().Key;
            //
            double[] center = Turn(GetCenter(oriented, sourceCoor), _angleRad);
            double[] bestDistance = new double[1];
            int[] bestSurfaceId = new int[1];
            SweepProjector.ProjectOntoNearestSurface(center, _block.TargetSurfaceIds, null, null, bestDistance,
                                                     bestSurfaceId);
            if (bestSurfaceId[0] < 0)
                throw new CaeException("A target element of " + GetDescription() + " could not be assigned to a target " +
                                       "surface.");
            return bestSurfaceId[0];
        }
        /// <summary>
        /// The angle a point is turned through from the source vertex on its circle, in the sense the block is turned.
        /// </summary>
        private double GetAngleFromSource(double[] point)
        {
            double radius = RevolveMethods.DistanceFromAxis(point, _center, _direction);
            double height = Dot(Subtract(point, _center), _direction);
            foreach (var vertexId in _sourceVertexIds)
            {
                double[] source = _vertexCoor[vertexId];
                if (Math.Abs(RevolveMethods.DistanceFromAxis(source, _center, _direction) - radius) > 100 * _tolerance) continue;
                if (Math.Abs(Dot(Subtract(source, _center), _direction) - height) > 100 * _tolerance) continue;
                //
                double[] from = GetRadial(source);
                double[] to = GetRadial(point);
                double angle = Math.Atan2(Dot(Cross(from, to), _direction), Dot(from, to));
                if (angle < -_angleTolerance) angle += 2 * Math.PI;
                return Math.Max(0, angle);
            }
            throw new CaeException("A vertex of " + GetDescription() + " lies on no circle a vertex of its source turns on.");
        }
        private double[] GetRadial(double[] point)
        {
            double[] arm = Subtract(point, _center);
            double along = Dot(arm, _direction);
            return new double[] { arm[0] - along * _direction[0], arm[1] - along * _direction[1],
                                  arm[2] - along * _direction[2] };
        }
        /// <summary>
        /// Whether an edge between two vertices on one circle about the axis runs along that circle.
        /// </summary>
        private bool IsArc(int edgeId, int[] vertexIds)
        {
            double[] first = _vertexCoor[vertexIds[0]];
            double[] second = _vertexCoor[vertexIds[1]];
            double radius = RevolveMethods.DistanceFromAxis(first, _center, _direction);
            double height = Dot(Subtract(first, _center), _direction);
            if (Math.Abs(RevolveMethods.DistanceFromAxis(second, _center, _direction) - radius) > 100 * _tolerance) return false;
            if (Math.Abs(Dot(Subtract(second, _center), _direction) - height) > 100 * _tolerance) return false;
            double[] middle = GetEdgeMiddle(edgeId);
            return Math.Abs(RevolveMethods.DistanceFromAxis(middle, _center, _direction) - radius) <= 100 * _tolerance &&
                   Math.Abs(Dot(Subtract(middle, _center), _direction) - height) <= 100 * _tolerance &&
                   Distance(first, second) > 100 * _tolerance;
        }
        private bool IsOnAxis(double[] point)
        {
            return RevolveMethods.DistanceFromAxis(point, _center, _direction) <= _tolerance;
        }
        private bool IsAxisEdge(int edgeId, int[] vertexIds)
        {
            foreach (var vertexId in vertexIds)
            {
                if (!IsOnAxis(GetVertexCoor(vertexId))) return false;
            }
            return IsOnAxis(GetEdgeMiddle(edgeId));
        }
        /// <summary>
        /// Whether a point lies on an edge between its ends, not only on the curve the edge is cut from.
        /// </summary>
        private bool IsOnEdge(int edgeId, double[] point)
        {
            double[] min;
            double[] max;
            double[] closest;
            double[] parametric;
            // The parameter is read off the closest point, since the one the closest point query returns is not set
            Gmsh.Model.GetClosestPoint(1, edgeId, point, out closest, out parametric);
            if (Distance(closest, point) > 100 * _tolerance) return false;
            Gmsh.Model.GetParametrization(1, edgeId, closest, out parametric);
            Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
            double margin = 1E-9 * Math.Max(1, Math.Abs(max[0] - min[0]));
            return parametric[0] >= min[0] - margin && parametric[0] <= max[0] + margin;
        }
        private double[] Turn(double[] point, double angle)
        {
            return RevolveMethods.Rotate(point, _center, _direction, angle);
        }
        private string GetDescription()
        {
            return _block.GetDescription();
        }
        private static string NameEdge(int edgeId)
        {
            return "The edge " + edgeId;
        }
        private static int[] GetEdgeVertexIds(int edgeId)
        {
            int[] upward;
            int[] downward;
            Gmsh.Model.GetAdjacencies(1, edgeId, out upward, out downward);
            return downward.Select(Math.Abs).Distinct().ToArray();
        }
        private static double[] GetVertexCoor(int vertexId)
        {
            double[] coor;
            Gmsh.Model.GetValue(0, vertexId, new double[0], out coor);
            return coor;
        }
        private static double[] GetEdgeMiddle(int edgeId)
        {
            double[] min;
            double[] max;
            double[] coor;
            Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
            Gmsh.Model.GetValue(1, edgeId, new double[] { 0.5 * (min[0] + max[0]) }, out coor);
            return coor;
        }
        private static IntPtr GetVertexNodeTag(int vertexId)
        {
            IntPtr[] tags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out tags, out coor, 0, vertexId, false, false);
            if (tags.Length != 1) throw new CaeException("A vertex of a revolved sweep block has no mesh node.");
            return tags[0];
        }
        private static Tuple<IntPtr, IntPtr> GetEdgeKey(IntPtr node0, IntPtr node1)
        {
            return node0.ToInt64() < node1.ToInt64() ? new Tuple<IntPtr, IntPtr>(node0, node1)
                                                      : new Tuple<IntPtr, IntPtr>(node1, node0);
        }
        private static double[] GetCenter(IntPtr[] nodeTags, Dictionary<IntPtr, double[]> coor)
        {
            double[] center = new double[3];
            foreach (var tag in nodeTags)
            {
                center[0] += coor[tag][0];
                center[1] += coor[tag][1];
                center[2] += coor[tag][2];
            }
            return new double[] { center[0] / nodeTags.Length, center[1] / nodeTags.Length, center[2] / nodeTags.Length };
        }
        private static double[] Normalize(double[] vector)
        {
            double length = Math.Sqrt(Dot(vector, vector));
            if (length <= 0) throw new CaeException("The axis of a revolved sweep block has no direction.");
            return new double[] { vector[0] / length, vector[1] / length, vector[2] / length };
        }
        private static double[] Subtract(double[] a, double[] b)
        {
            return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        }
        private static double[] Cross(double[] a, double[] b)
        {
            return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        }
        private static double Dot(double[] a, double[] b)
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }
        private static double Distance(double[] a, double[] b)
        {
            return Math.Sqrt(Dot(Subtract(a, b), Subtract(a, b)));
        }
    }
}
