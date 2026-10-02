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
    /// The block keeps the CAD entities of the model, so that its neighbours share its surfaces. gmsh meshes the source
    /// and the curves, with the counts the block sets before: every arc the source vertices turn along gets the layer
    /// count, and every edge of the target the count of the source edge it is the image of. Once the surfaces are
    /// meshed the block builds everything else by turning the source mesh: the nodes on the arcs and on the target
    /// edges are moved to where the turning puts them, the walls and the target are meshed again by the turned source
    /// edges and the turned source, and the volume by the turned source elements. It is built before the blocks
    /// swept between two ends, and before the cells left to tetrahedra, so that both read its surfaces as they end.
    /// </summary>
    public class SweepRevolvedBlock
    {
        // Variables
        private readonly SweepBlock _block;
        private readonly double[] _center;
        private readonly double[] _direction;
        private readonly double _angleRad;
        private readonly double _tolerance;
        private readonly HashSet<int> _sourceVertexIds;
        private readonly HashSet<int> _sourceEdgeIds;
        private readonly HashSet<int> _boundaryEdgeIds;
        private readonly Dictionary<int, int> _targetVertexIds;
        private readonly Dictionary<int, int> _targetEdgeIds;
        private readonly Dictionary<int, int> _arcEdgeIds;
        private readonly Dictionary<int, int> _wallSurfaceIds;
        private readonly Dictionary<int, int> _vertexTargetEdgeIds;
        private readonly Dictionary<int, int> _vertexWallSurfaceIds;
        private readonly List<Tuple<int[], int>> _targetEdgeChains;


        // Properties
        public SweepBlock Block { get { return _block; } }
        /// <summary>The edges the source vertices off the axis turn along, one for each of them.</summary>
        public IEnumerable<int> ArcEdgeIds { get { return _arcEdgeIds.Values; } }
        /// <summary>
        /// Every edge of the source off the axis, and the edge of the target it is turned onto. An edge inside a split
        /// source may be turned into a target that is not split, and then has none.
        /// </summary>
        public Dictionary<int, int> TargetEdgeIds { get { return _targetEdgeIds; } }
        /// <summary>
        /// The chains of source edges and the target edge each chain is turned onto: one edge, or several when the
        /// vertices between them only split an edge of the source boundary and are turned onto points along the
        /// target edge, which then carries as many elements as the chain.
        /// </summary>
        public List<Tuple<int[], int>> TargetEdgeChains { get { return _targetEdgeChains; } }


        // Constructors
        public SweepRevolvedBlock(SweepBlock block)
        {
            _block = block;
            _center = new double[] { block.RevolveAxis[0], block.RevolveAxis[1], block.RevolveAxis[2] };
            _direction = Normalize(new double[] { block.RevolveAxis[3], block.RevolveAxis[4], block.RevolveAxis[5] });
            _angleRad = block.RevolveAxis[6] * Math.PI / 180;
            if (!(_angleRad > 0) || _angleRad >= 2 * Math.PI - 1E-9)
                throw new CaeException("The angle of " + GetDescription() + " must be larger than 0° and smaller than 360°.");
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
            _sourceVertexIds = new HashSet<int>();
            foreach (var edgeId in _sourceEdgeIds)
            {
                edgeVertexIds.Add(edgeId, GetEdgeVertexIds(edgeId));
                _sourceVertexIds.UnionWith(edgeVertexIds[edgeId]);
            }
            // The tolerance follows the size of the source
            BoundingBox box = new BoundingBox();
            Dictionary<int, double[]> vertexCoor = new Dictionary<int, double[]>();
            foreach (var vertexId in _sourceVertexIds)
            {
                vertexCoor.Add(vertexId, GetVertexCoor(vertexId));
                box.IncludeCoor(vertexCoor[vertexId]);
            }
            _tolerance = 1E-6 * Math.Max(box.GetDiagonal(), 1E-6);
            // The target entities, which the turned source entities are matched against
            HashSet<int> targetEdgeIds = new HashSet<int>();
            foreach (var surfaceId in block.TargetSurfaceIds)
            {
                Gmsh.Model.GetAdjacencies(2, surfaceId, out upward, out downward);
                targetEdgeIds.UnionWith(downward.Select(Math.Abs));
            }
            HashSet<int> targetVertexIds = new HashSet<int>();
            foreach (var edgeId in targetEdgeIds)
            {
                if (!edgeVertexIds.ContainsKey(edgeId)) edgeVertexIds.Add(edgeId, GetEdgeVertexIds(edgeId));
                targetVertexIds.UnionWith(edgeVertexIds[edgeId]);
            }
            foreach (var vertexId in targetVertexIds)
            {
                if (!vertexCoor.ContainsKey(vertexId)) vertexCoor.Add(vertexId, GetVertexCoor(vertexId));
            }
            // Every source vertex off the axis turns onto a vertex of the target; one on the axis stays where it is. A
            // vertex inside a split source may turn into the inside of the target, which need not be split there
            HashSet<int> boundaryVertexIds = new HashSet<int>(edgeUseCount.Where(entry => entry.Value == 1)
                                                                          .SelectMany(entry => edgeVertexIds[entry.Key]));
            _targetVertexIds = new Dictionary<int, int>();
            foreach (var vertexId in _sourceVertexIds)
            {
                if (IsOnAxis(vertexCoor[vertexId]))
                {
                    _targetVertexIds.Add(vertexId, vertexId);
                    continue;
                }
                double[] turned = Turn(vertexCoor[vertexId], _angleRad);
                int targetVertexId = targetVertexIds.Where(id => Distance(vertexCoor[id], turned) <= 100 * _tolerance)
                                                    .DefaultIfEmpty(-1).First();
                if (targetVertexId >= 0) _targetVertexIds.Add(vertexId, targetVertexId);
            }
            // Every source edge off the axis turns onto an edge of the target joining the turned vertices. A vertex
            // splitting an edge of the source boundary turns onto a point along an edge of the target, so the edges of
            // the boundary are followed in chains from one vertex turned onto a vertex to the next
            _targetEdgeIds = new Dictionary<int, int>();
            _boundaryEdgeIds = new HashSet<int>();
            _targetEdgeChains = new List<Tuple<int[], int>>();
            _vertexTargetEdgeIds = new Dictionary<int, int>();
            Dictionary<int, List<int>> vertexBoundaryEdgeIds = new Dictionary<int, List<int>>();
            foreach (var edgeId in _sourceEdgeIds)
            {
                int[] vertexIds = edgeVertexIds[edgeId];
                if (IsAxisEdge(edgeId, vertexIds, vertexCoor)) continue;
                //
                if (edgeUseCount[edgeId] == 1)
                {
                    _boundaryEdgeIds.Add(edgeId);
                    foreach (var vertexId in vertexIds)
                    {
                        if (!vertexBoundaryEdgeIds.ContainsKey(vertexId)) vertexBoundaryEdgeIds.Add(vertexId, new List<int>());
                        vertexBoundaryEdgeIds[vertexId].Add(edgeId);
                    }
                }
                else if (vertexIds.All(id => _targetVertexIds.ContainsKey(id)))
                {
                    // An edge inside the source has an edge on the target only when the target is split there too
                    int targetEdgeId = FindTargetEdge(new int[] { edgeId }, vertexIds, edgeVertexIds, targetEdgeIds);
                    if (targetEdgeId >= 0)
                    {
                        _targetEdgeIds.Add(edgeId, targetEdgeId);
                        _targetEdgeChains.Add(new Tuple<int[], int>(new int[] { edgeId }, targetEdgeId));
                    }
                }
            }
            HashSet<int> chainedEdgeIds = new HashSet<int>();
            foreach (var edgeId in _boundaryEdgeIds)
            {
                if (chainedEdgeIds.Contains(edgeId)) continue;
                //
                List<int> chainEdgeIds;
                List<int> innerVertexIds;
                int[] ends = GetBoundaryChain(edgeId, edgeVertexIds, vertexBoundaryEdgeIds, out chainEdgeIds,
                                              out innerVertexIds);
                chainedEdgeIds.UnionWith(chainEdgeIds);
                if (ends == null || !ends.All(id => _targetVertexIds.ContainsKey(id)))
                    throw new CaeException("A vertex of the source of " + GetDescription() + " is not turned onto the " +
                                           "boundary of its target.");
                int targetEdgeId = FindTargetEdge(chainEdgeIds.ToArray(), ends, edgeVertexIds, targetEdgeIds);
                if (targetEdgeId < 0)
                    throw new CaeException("An edge of the source of " + GetDescription() + " is not turned onto an " +
                                           "edge of its target.");
                foreach (var chainEdgeId in chainEdgeIds) _targetEdgeIds.Add(chainEdgeId, targetEdgeId);
                foreach (var vertexId in innerVertexIds) _vertexTargetEdgeIds.Add(vertexId, targetEdgeId);
                _targetEdgeChains.Add(new Tuple<int[], int>(chainEdgeIds.ToArray(), targetEdgeId));
            }
            // The wall every boundary edge sweeps, and the arc every boundary vertex off the axis turns along
            _wallSurfaceIds = new Dictionary<int, int>();
            HashSet<int> wallEdgeIds = new HashSet<int>();
            foreach (var edgeId in _boundaryEdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out upward, out downward);
                int[] wallIds = upward.Select(Math.Abs).Where(id => block.WallSurfaceIds.Contains(id)).Distinct().ToArray();
                if (wallIds.Length != 1)
                    throw new CaeException("The wall an edge of the source of " + GetDescription() + " sweeps could not " +
                                           "be determined.");
                _wallSurfaceIds.Add(edgeId, wallIds[0]);
                Gmsh.Model.GetAdjacencies(2, wallIds[0], out upward, out downward);
                wallEdgeIds.UnionWith(downward.Select(Math.Abs));
            }
            // A vertex turned onto a point along a target edge turns across the wall of its two edges
            _vertexWallSurfaceIds = new Dictionary<int, int>();
            foreach (var vertexId in _vertexTargetEdgeIds.Keys)
            {
                int[] wallIds = vertexBoundaryEdgeIds[vertexId].Select(id => _wallSurfaceIds[id]).Distinct().ToArray();
                if (wallIds.Length != 1)
                    throw new CaeException("A vertex of the source of " + GetDescription() + " joins two walls but is " +
                                           "turned onto no vertex of its target.");
                _vertexWallSurfaceIds.Add(vertexId, wallIds[0]);
            }
            _arcEdgeIds = new Dictionary<int, int>();
            foreach (var edgeId in _boundaryEdgeIds)
            {
                foreach (var vertexId in edgeVertexIds[edgeId])
                {
                    if (_arcEdgeIds.ContainsKey(vertexId) || !_targetVertexIds.ContainsKey(vertexId) ||
                        _targetVertexIds[vertexId] == vertexId) continue;
                    //
                    HashSet<int> ends = new HashSet<int>() { vertexId, _targetVertexIds[vertexId] };
                    int[] arcIds = wallEdgeIds.Where(id => !_sourceEdgeIds.Contains(id) && !targetEdgeIds.Contains(id))
                                              .Where(id => GetEdgeVertexIds(id).ToHashSet().SetEquals(ends)).ToArray();
                    if (arcIds.Length != 1)
                        throw new CaeException("A vertex of the source of " + GetDescription() + " does not turn along " +
                                               "one edge onto the target, so the walls of the block are split along " +
                                               "the revolution.");
                    _arcEdgeIds.Add(vertexId, arcIds[0]);
                }
            }
        }


        // Methods
        /// <summary>
        /// Builds the mesh of the block from the meshed source: the nodes on the arcs and the target edges are moved
        /// onto the turned source, and the walls, the target and the volume are meshed by turning the source mesh
        /// through the given number of equal layers. A wall two revolved blocks share is meshed by the one built
        /// first, and the nodes it made there are kept by their source node and layer, so that the other block uses
        /// them rather than meshing the wall again.
        /// </summary>
        public void Build(int numLayers, HashSet<int> builtWallIds, Dictionary<Tuple<IntPtr, int>, IntPtr> wallNodeTags)
        {
            if (numLayers < 1) numLayers = 1;
            double[] angles = new double[numLayers + 1];
            for (int k = 0; k <= numLayers; k++) angles[k] = _angleRad * k / numLayers;
            // The walls are meshed again from the turned source edges, and the target from the turned source
            List<Tuple<int, int>> clearDimTags = new List<Tuple<int, int>>();
            foreach (var wallId in _wallSurfaceIds.Values.Distinct())
            {
                if (!builtWallIds.Contains(wallId)) clearDimTags.Add(new Tuple<int, int>(2, wallId));
            }
            foreach (var targetId in _block.TargetSurfaceIds) clearDimTags.Add(new Tuple<int, int>(2, targetId));
            Gmsh.Model.Mesh.Clear(clearDimTags.ToArray());
            // The source nodes and the entities they are classified on
            IntPtr[] tags;
            double[] coor;
            Dictionary<IntPtr, double[]> sourceCoor = new Dictionary<IntPtr, double[]>();
            Dictionary<IntPtr, Tuple<int, int>> sourceEntity = new Dictionary<IntPtr, Tuple<int, int>>();
            AddSourceNodes(0, _sourceVertexIds, sourceCoor, sourceEntity);
            AddSourceNodes(1, _sourceEdgeIds, sourceCoor, sourceEntity);
            AddSourceNodes(2, _block.SourceSurfaceIds, sourceCoor, sourceEntity);
            // The nodes already there, on the arcs and on the target edges, to be moved onto the turned source
            Dictionary<int, List<IntPtr>> edgeNodes = new Dictionary<int, List<IntPtr>>();
            Dictionary<IntPtr, double[]> edgeNodeCoor = new Dictionary<IntPtr, double[]>();
            foreach (var edgeId in _arcEdgeIds.Values.Concat(_targetEdgeIds.Values).Distinct())
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 1, edgeId, false, false);
                edgeNodes.Add(edgeId, tags.ToList());
                for (int i = 0; i < tags.Length; i++)
                    edgeNodeCoor[tags[i]] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
            }
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
            Action<int, IntPtr[], int, double[]> requestEdgeNode = (edgeId, nodeLine, layer, turnedPoint) =>
            {
                if (!edgeRequests.ContainsKey(edgeId)) edgeRequests.Add(edgeId, new List<Tuple<IntPtr[], int, double[]>>());
                edgeRequests[edgeId].Add(new Tuple<IntPtr[], int, double[]>(nodeLine, layer, turnedPoint));
            };
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
                for (int k = 1; k < numLayers; k++)
                {
                    double[] turned = Turn(point, angles[k]);
                    if (dim == 0 && _arcEdgeIds.ContainsKey(tag)) requestEdgeNode(_arcEdgeIds[tag], line, k, turned);
                    else if ((dim == 1 && _boundaryEdgeIds.Contains(tag)) || (dim == 0 && _vertexWallSurfaceIds.ContainsKey(tag)))
                    {
                        int wallId = dim == 1 ? _wallSurfaceIds[tag] : _vertexWallSurfaceIds[tag];
                        Tuple<IntPtr, int> key = new Tuple<IntPtr, int>(entry.Key, k);
                        if (builtWallIds.Contains(wallId))
                        {
                            if (!wallNodeTags.TryGetValue(key, out line[k]))
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
                        wallNodeTags[key] = line[k];
                    }
                    else
                    {
                        line[k] = (IntPtr)nextNodeTag++;
                        volumeTags.Add(line[k]);
                        volumeCoor.AddRange(turned);
                    }
                }
                double[] end = Turn(point, _angleRad);
                if (dim == 0 && _targetVertexIds.ContainsKey(tag)) line[numLayers] = GetVertexNodeTag(_targetVertexIds[tag]);
                else if (dim == 0 && _vertexTargetEdgeIds.ContainsKey(tag))
                    requestEdgeNode(_vertexTargetEdgeIds[tag], line, numLayers, end);
                else if (dim == 1 && _targetEdgeIds.ContainsKey(tag)) requestEdgeNode(_targetEdgeIds[tag], line, numLayers, end);
                else
                {
                    line[numLayers] = (IntPtr)nextNodeTag++;
                    targetTags.Add(line[numLayers]);
                    targetCoor.AddRange(end);
                }
            }
            // The nodes on the arcs and on the target edges are paired with the turned source nodes in their order
            // along the edge, and moved onto them
            foreach (var entry in edgeNodes)
            {
                List<Tuple<IntPtr[], int, double[]>> requests;
                if (!edgeRequests.TryGetValue(entry.Key, out requests))
                    requests = new List<Tuple<IntPtr[], int, double[]>>();
                if (requests.Count != entry.Value.Count)
                    throw new CaeException("The edge " + entry.Key + " of " + GetDescription() + " has " +
                                           entry.Value.Count + " nodes inside it, but the turned source puts " +
                                           requests.Count + " there.");
                MoveEdgeNodes(entry.Key, entry.Value, edgeNodeCoor, requests);
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
            AddElements(lines, sourceCoor, numLayers, targetNodeSurfaceIds, builtWallIds);
            builtWallIds.UnionWith(_wallSurfaceIds.Values);
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
            nodeTags.Clear();
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
        private void AddElements(Dictionary<IntPtr, IntPtr[]> lines, Dictionary<IntPtr, double[]> sourceCoor, int numLayers,
                                 Dictionary<IntPtr, int> targetNodeSurfaceIds, HashSet<int> builtWallIds)
        {
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
            double firstAngle = _angleRad / numLayers;
            foreach (var element in sourceElements)
            {
                IntPtr[] oriented = RevolveMethods.GetOutwardSourceNodeIds(element, sourceCoor, _center, _direction,
                                                                           firstAngle);
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
                add(2, GetCapSurfaceId(cap, targetNodeSurfaceIds, lines, sourceCoor, oriented),
                    oriented.Length == 3 ? GmshElementType.LinearTriangle : GmshElementType.LinearQuadrilateral, cap);
            }
            // The walls, from the elements of the boundary edges, their normals away from the source element beside them
            foreach (var edgeId in _boundaryEdgeIds)
            {
                int wallId = _wallSurfaceIds[edgeId];
                if (builtWallIds.Contains(wallId)) continue;
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
                        double[] outward = Subtract(GetCenter(new IntPtr[] { node0, node1 }, sourceCoor), elementCenter);
                        for (int k = 0; k < numLayers; k++)
                        {
                            IntPtr[] face;
                            if (onAxis0) face = new IntPtr[] { lines[node0][k], lines[node1][k], lines[node1][k + 1] };
                            else if (onAxis1) face = new IntPtr[] { lines[node0][k], lines[node1][k], lines[node0][k + 1] };
                            else face = new IntPtr[] { lines[node0][k], lines[node1][k], lines[node1][k + 1],
                                                       lines[node0][k + 1] };
                            // The first layer of a face is where the source edge lies, so the normal is measured there
                            double[] p0 = sourceCoor[node0];
                            double[] p1 = sourceCoor[node1];
                            double[] along = Subtract(p1, p0);
                            double[] turning = Cross(_direction, Subtract(GetCenter(new IntPtr[] { node0, node1 },
                                                                                    sourceCoor), _center));
                            if (Dot(Cross(along, turning), outward) < 0) Array.Reverse(face);
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
            CheckSurfaceBoundaries();
        }
        /// <summary>
        /// Checks that the mesh of every wall and every target surface the block built ends on the line elements of
        /// the edges around it, so that a surface the block does not close is refused here rather than by the mesher
        /// of a neighbouring cell.
        /// </summary>
        private void CheckSurfaceBoundaries()
        {
            int[] upward;
            int[] edgeIds;
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            foreach (var surfaceId in _block.TargetSurfaceIds.Concat(_wallSurfaceIds.Values.Distinct()))
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
                                    Dictionary<IntPtr, IntPtr[]> lines, Dictionary<IntPtr, double[]> sourceCoor,
                                    IntPtr[] oriented)
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
        /// The chain of boundary edges through the given one, running on through the vertices that are turned onto no
        /// vertex of the target, and the two vertices it ends on. Null ends for a chain that closes on itself.
        /// </summary>
        private int[] GetBoundaryChain(int edgeId, Dictionary<int, int[]> edgeVertexIds,
                                       Dictionary<int, List<int>> vertexBoundaryEdgeIds, out List<int> chainEdgeIds,
                                       out List<int> innerVertexIds)
        {
            chainEdgeIds = new List<int>() { edgeId };
            innerVertexIds = new List<int>();
            int[] ends = new int[2];
            int[] vertexIds = edgeVertexIds[edgeId];
            for (int side = 0; side < 2; side++)
            {
                int currentEdgeId = edgeId;
                int vertexId = vertexIds.Length == 2 ? vertexIds[side] : vertexIds[0];
                while (!_targetVertexIds.ContainsKey(vertexId))
                {
                    List<int> nextEdgeIds = vertexBoundaryEdgeIds[vertexId].Where(id => id != currentEdgeId).ToList();
                    if (nextEdgeIds.Count != 1 || chainEdgeIds.Contains(nextEdgeIds[0])) return null;
                    //
                    innerVertexIds.Add(vertexId);
                    currentEdgeId = nextEdgeIds[0];
                    chainEdgeIds.Add(currentEdgeId);
                    int[] nextVertexIds = edgeVertexIds[currentEdgeId];
                    vertexId = nextVertexIds[0] == vertexId ? nextVertexIds[nextVertexIds.Length - 1] : nextVertexIds[0];
                }
                ends[side] = vertexId;
            }
            return ends;
        }
        /// <summary>
        /// The target edge joining the turned ends of the source edges and passing nearest to where a point of the
        /// first of them is turned, or -1 when none passes there.
        /// </summary>
        private int FindTargetEdge(int[] sourceEdgeIds, int[] ends, Dictionary<int, int[]> edgeVertexIds,
                                   HashSet<int> targetEdgeIds)
        {
            HashSet<int> turnedEnds = new HashSet<int>(ends.Select(id => _targetVertexIds[id]));
            double[] turnedMiddle = Turn(GetEdgeMiddle(sourceEdgeIds[0]), _angleRad);
            int targetEdgeId = -1;
            double bestDistance = double.MaxValue;
            foreach (var candidateId in targetEdgeIds)
            {
                if (_sourceEdgeIds.Contains(candidateId)) continue;
                if (!turnedEnds.SetEquals(edgeVertexIds[candidateId])) continue;
                //
                double distance = GetDistanceToEdge(candidateId, turnedMiddle);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    targetEdgeId = candidateId;
                }
            }
            return bestDistance <= 1E3 * _tolerance ? targetEdgeId : -1;
        }
        private static double GetDistanceToEdge(int edgeId, double[] point)
        {
            double[] parametric;
            double[] closest;
            Gmsh.Model.GetClosestPoint(1, edgeId, point, out closest, out parametric);
            return Distance(closest, point);
        }
        private bool IsOnAxis(double[] point)
        {
            return RevolveMethods.DistanceFromAxis(point, _center, _direction) <= _tolerance;
        }
        private bool IsAxisEdge(int edgeId, int[] vertexIds, Dictionary<int, double[]> vertexCoor)
        {
            foreach (var vertexId in vertexIds)
            {
                if (!IsOnAxis(vertexCoor[vertexId])) return false;
            }
            return IsOnAxis(GetEdgeMiddle(edgeId));
        }
        private double[] Turn(double[] point, double angle)
        {
            return RevolveMethods.Rotate(point, _center, _direction, angle);
        }
        private string GetDescription()
        {
            return _block.GetDescription();
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
