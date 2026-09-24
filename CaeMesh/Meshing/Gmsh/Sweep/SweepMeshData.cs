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
    /// All mesh data of one sweep block in a flat, densely indexed layout.
    ///
    /// A one-to-one sweep is structurally a tensor product: every source node owns exactly one
    /// sweep line and every sweep line has the same number of layers. Everything is therefore held
    /// in rectangular tables indexed by (layer, source node) instead of dictionaries keyed by node
    /// tag. This keeps the morphing and smoothing loops cache friendly, and it lets the elements
    /// incident to any sweep node be derived from the source connectivity alone, with no 3D
    /// node-to-element map to build or maintain.
    ///
    /// Layer 0 is the source layer, layer NumLayers - 1 is the target layer.
    /// </summary>
    public class SweepMeshData
    {
        // Variables
        private int _numNodes;                          // number of source nodes = number of sweep lines
        private int _numLayers;                         // number of node rows, including source and target
        private IntPtr[] _nodeTags;                     // [layer * numNodes + node]
        private double[] _coords;                       // 3 * [layer * numNodes + node]
        private bool[] _isBoundaryNode;                 // source node lies on the source/wall boundary, or is pinned
        private int[] _interiorNodes;                   // source node indices that are not boundary nodes
        private int[] _pinnedNodes;                     // source nodes on a carried imprint, whose lines are prescribed
        private int[][] _sourceElements;                // source element -> source node indices (3 or 4)
        private int[] _sourceElementTypes;              // gmsh element type of each source element
        private int[] _sourceElementSurfaceIds;         // gmsh surface the source element came from
        private int[] _adjStart;                        // CSR 1-ring adjacency of the source nodes
        private int[] _adjNodes;
        private int[] _nodeElementStart;                // CSR source node -> incident source elements
        private int[] _nodeElements;
        private IntPtr[] _sourceTags;                   // dense source node index -> gmsh tag
        private Dictionary<IntPtr, int> _sourceTagIndex; // gmsh tag -> dense source node index


        // Properties
        public int NumNodes { get { return _numNodes; } }
        public int NumLayers { get { return _numLayers; } }
        public IntPtr[] NodeTags { get { return _nodeTags; } }
        public double[] Coords { get { return _coords; } }
        public bool[] IsBoundaryNode { get { return _isBoundaryNode; } }
        public int[] InteriorNodes { get { return _interiorNodes; } }
        /// <summary>
        /// Source nodes whose sweep lines are prescribed from a carried imprint. They count as boundary for
        /// the morph and the smoother, but unlike wall nodes their intermediate layers do not exist yet and
        /// have to be added with the interior ones; their target node is an existing node of the imprint.
        /// </summary>
        public int[] PinnedNodes { get { return _pinnedNodes ?? new int[0]; } }
        public int[][] SourceElements { get { return _sourceElements; } }
        public int[] SourceElementTypes { get { return _sourceElementTypes; } }
        public int[] SourceElementSurfaceIds { get { return _sourceElementSurfaceIds; } }
        public int[] AdjStart { get { return _adjStart; } }
        public int[] AdjNodes { get { return _adjNodes; } }
        public int[] NodeElementStart { get { return _nodeElementStart; } }
        public int[] NodeElements { get { return _nodeElements; } }


        // Constructors
        private SweepMeshData() { }


        // Methods
        /// <summary>
        /// Index of a sweep node in the node tag and coordinate tables.
        /// </summary>
        public int Index(int layer, int node)
        {
            return layer * _numNodes + node;
        }
        public void GetCoor(int layer, int node, double[] coor)
        {
            int i = 3 * (layer * _numNodes + node);
            coor[0] = _coords[i];
            coor[1] = _coords[i + 1];
            coor[2] = _coords[i + 2];
        }
        public void SetCoor(int layer, int node, double[] coor)
        {
            int i = 3 * (layer * _numNodes + node);
            _coords[i] = coor[0];
            _coords[i + 1] = coor[1];
            _coords[i + 2] = coor[2];
        }
        /// <summary>
        /// A representative element edge length of the source mesh. Used to turn absolute
        /// tolerances into tolerances that scale with the model.
        /// </summary>
        public double GetAverageSourceEdgeLength()
        {
            int count = 0;
            double sum = 0;
            for (int e = 0; e < _sourceElements.Length; e++)
            {
                int[] element = _sourceElements[e];
                for (int i = 0; i < element.Length; i++)
                {
                    int a = 3 * element[i];
                    int b = 3 * element[(i + 1) % element.Length];
                    sum += Math.Sqrt(Math.Pow(_coords[b] - _coords[a], 2) +
                                     Math.Pow(_coords[b + 1] - _coords[a + 1], 2) +
                                     Math.Pow(_coords[b + 2] - _coords[a + 2], 2));
                    count++;
                }
            }
            return count > 0 ? sum / count : 0;
        }
        /// <summary>
        /// Reads the meshed source and wall surfaces of a block and lays out the sweep line table.
        /// The source layer and every boundary sweep line are filled with their exact meshed
        /// positions; the interior of layers 1 .. NumLayers - 1 is left for the morph to fill.
        /// </summary>
        public static SweepMeshData Create(SweepBlock block, Dictionary<int, int[]> surfaceIdEdgeIds)
        {
            SweepMeshData data = new SweepMeshData();
            // Coordinates of everything that is already meshed - the source and the wall surfaces
            Dictionary<IntPtr, double[]> tagCoor =
                GetNodeCoordinates(block.SourceSurfaceIds.Union(block.WallSurfaceIds));
            // Source connectivity, densely indexed
            data.ReadSourceMesh(block);
            // Sweep lines read off the structured wall grid
            Dictionary<IntPtr, List<IntPtr>> boundaryLines = TraceWallSweepLines(block, surfaceIdEdgeIds);
            data.LayOutSweepLines(boundaryLines, tagCoor);
            // Sweep lines prescribed between an edge splitting the source and the edge of the target above it
            data.PinImprintLines(block);
            //
            return data;
        }
        /// <summary>
        /// Pins the sweep lines between the edges splitting the source inside and the edges of the target above
        /// them: every node of such a source edge is paired with the node of the target edge it stands below,
        /// the layers between them are interpolated, and the target node of the line is the target edge's node
        /// itself, so that a neighbour standing on the target, whose walls start from those nodes, shares them,
        /// and so that the target edge keeps a line mesh the cap elements agree with. The morph then treats the
        /// pinned lines as boundary and fills around them.
        ///
        /// The pairing is made between two edges that are a translation of each other, which a block that is
        /// not a prism can have as well - the two tangency lines of a bent plate are one such pair - so the
        /// straight line between two paired nodes is a sweep line whether or not the whole block is prismatic.
        /// </summary>
        private void PinImprintLines(SweepBlock block)
        {
            if (block.ImprintEdgeCopyIds == null || block.ImprintEdgeCopyIds.Count == 0) return;
            //
            int node;
            int index;
            IntPtr[] sourceTags;
            IntPtr[] targetTags;
            double[] sourceCoor;
            double[] targetCoor;
            List<int> pinned = new List<int>();
            foreach (var entry in block.ImprintEdgeCopyIds)
            {
                // The nodes of both edges in the order along the edge, the target edge read the way the source
                // edge runs. Pairing by order rather than by position survives a 2D algorithm that moves the
                // curve nodes once the curves are meshed, as the quasi-structured quad algorithm does
                GetOrderedCurveNodes(entry.Value, out sourceTags, out sourceCoor);
                GetOrderedCurveNodes(entry.Key, out targetTags, out targetCoor);
                if (sourceTags.Length != targetTags.Length)
                    throw new CaeException("An edge splitting the sweep target and the edge of the source below it carry " +
                                           "a different number of nodes.");
                AlignCurveNodes(sourceCoor, ref targetTags, ref targetCoor);
                //
                for (int i = 0; i < sourceTags.Length; i++)
                {
                    if (!_sourceTagIndex.TryGetValue(sourceTags[i], out node))
                        throw new CaeException("A node of an edge splitting the sweep source was not found in the " +
                                               "source mesh.");
                    // A node on the wall boundary is a wall line already, and is left to the wall grid
                    if (_isBoundaryNode[node]) continue;
                    // A straight line from the source node to the target node, the two edges being a translation
                    // of each other
                    _isBoundaryNode[node] = true;
                    pinned.Add(node);
                    for (int layer = 1; layer < _numLayers; layer++)
                    {
                        double ratio = (double)layer / (_numLayers - 1);
                        index = Index(layer, node);
                        for (int k = 0; k < 3; k++)
                            _coords[3 * index + k] = sourceCoor[3 * i + k] + ratio * (targetCoor[3 * i + k] - sourceCoor[3 * i + k]);
                    }
                    _nodeTags[Index(_numLayers - 1, node)] = targetTags[i];
                }
            }
            if (pinned.Count == 0) return;
            //
            _pinnedNodes = pinned.ToArray();
            List<int> interior = new List<int>();
            for (int i = 0; i < _numNodes; i++) if (!_isBoundaryNode[i]) interior.Add(i);
            _interiorNodes = interior.ToArray();
        }
        /// <summary>
        /// The nodes of a meshed curve, its end vertices included, in the order along the curve: from the node
        /// at the curve's first vertex, each next node is the nearest one not yet taken.
        /// </summary>
        private static void GetOrderedCurveNodes(int edgeId, out IntPtr[] tags, out double[] coor)
        {
            IntPtr[] allTags;
            double[] allCoor;
            Gmsh.Model.Mesh.GetNodes(out allTags, out allCoor, 1, edgeId, true, false);
            int[] vertexIds;
            double[] current;
            Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
            Gmsh.Model.GetValue(0, vertexIds[0], new double[3], out current);
            //
            int count = allTags.Length;
            bool[] taken = new bool[count];
            tags = new IntPtr[count];
            coor = new double[3 * count];
            for (int k = 0; k < count; k++)
            {
                int nearest = -1;
                double nearestDistance = double.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    if (taken[i]) continue;
                    double distance = Math.Pow(allCoor[3 * i] - current[0], 2) + Math.Pow(allCoor[3 * i + 1] - current[1], 2) +
                                      Math.Pow(allCoor[3 * i + 2] - current[2], 2);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = i;
                    }
                }
                taken[nearest] = true;
                tags[k] = allTags[nearest];
                Array.Copy(allCoor, 3 * nearest, coor, 3 * k, 3);
                current = new double[] { allCoor[3 * nearest], allCoor[3 * nearest + 1], allCoor[3 * nearest + 2] };
            }
        }
        /// <summary>
        /// Reverses the target nodes when they run against the source nodes, judged by the summed distance
        /// between the source nodes carried onto the target and the target nodes in either order.
        /// </summary>
        private static void AlignCurveNodes(double[] sourceCoor, ref IntPtr[] targetTags, ref double[] targetCoor)
        {
            int count = targetTags.Length;
            // The vector between the two edges, which is what the source has to be carried by before the two
            // runs can be compared. Taken from the nodes themselves, so that no translation has to be passed in
            double[] carry = new double[3];
            for (int i = 0; i < count; i++)
                for (int k = 0; k < 3; k++) carry[k] += (targetCoor[3 * i + k] - sourceCoor[3 * i + k]) / count;
            //
            double forward = 0;
            double backward = 0;
            for (int i = 0; i < count; i++)
            {
                int j = count - 1 - i;
                for (int k = 0; k < 3; k++)
                {
                    double carried = sourceCoor[3 * i + k] + carry[k];
                    forward += Math.Abs(carried - targetCoor[3 * i + k]);
                    backward += Math.Abs(carried - targetCoor[3 * j + k]);
                }
            }
            if (backward >= forward) return;
            //
            IntPtr[] reversedTags = new IntPtr[count];
            double[] reversedCoor = new double[3 * count];
            for (int i = 0; i < count; i++)
            {
                reversedTags[i] = targetTags[count - 1 - i];
                Array.Copy(targetCoor, 3 * (count - 1 - i), reversedCoor, 3 * i, 3);
            }
            targetTags = reversedTags;
            targetCoor = reversedCoor;
        }
        /// <summary>
        /// Builds the data for a block whose source mesh and boundary sweep lines are already
        /// known, instead of reading them back from meshed gmsh surfaces.
        ///
        /// This is the entry point a many-to-one sweep needs for every block after the first: there
        /// the source layer is the target layer of the previous block and exists only in memory,
        /// never as a meshed gmsh surface. The node tag and coordinate tables must already be sized
        /// numLayers * numNodes, with the source layer and every boundary sweep line filled in.
        /// </summary>
        public static SweepMeshData CreateFromLayers(int[][] sourceElements, int[] sourceElementSurfaceIds,
                                                     IntPtr[] nodeTags, double[] coords, bool[] isBoundaryNode,
                                                     int numLayers)
        {
            if (sourceElements == null || sourceElements.Length == 0)
                throw new CaeException("The sweep source mesh is empty.");
            if (numLayers < 2)
                throw new CaeException("A sweep block must have at least one element layer.");
            //
            SweepMeshData data = new SweepMeshData();
            data._numNodes = isBoundaryNode.Length;
            data._numLayers = numLayers;
            data._sourceElements = sourceElements;
            data._isBoundaryNode = isBoundaryNode;
            data._nodeTags = nodeTags;
            data._coords = coords;
            //
            if (nodeTags.Length != numLayers * data._numNodes || coords.Length != 3 * numLayers * data._numNodes)
                throw new CaeException("The sweep node tables have an inconsistent size.");
            //
            data._sourceElementTypes = new int[sourceElements.Length];
            for (int i = 0; i < sourceElements.Length; i++)
            {
                if (sourceElements[i].Length == 3)
                    data._sourceElementTypes[i] = (int)GmshElementType.LinearTriangle;
                else if (sourceElements[i].Length == 4)
                    data._sourceElementTypes[i] = (int)GmshElementType.LinearQuadrilateral;
                else throw new CaeException("A sweep source element must have three or four nodes.");
            }
            data._sourceElementSurfaceIds = sourceElementSurfaceIds ?? new int[sourceElements.Length];
            //
            data._sourceTags = new IntPtr[data._numNodes];
            data._sourceTagIndex = new Dictionary<IntPtr, int>();
            for (int i = 0; i < data._numNodes; i++)
            {
                data._sourceTags[i] = nodeTags[i];
                data._sourceTagIndex[nodeTags[i]] = i;
            }
            //
            List<int> interior = new List<int>();
            for (int i = 0; i < data._numNodes; i++) if (!isBoundaryNode[i]) interior.Add(i);
            data._interiorNodes = interior.ToArray();
            //
            data.BuildAdjacency();
            //
            return data;
        }
        //
        private static Dictionary<IntPtr, double[]> GetNodeCoordinates(IEnumerable<int> surfaceIds)
        {
            double[] coor;
            double[] allCoor;
            IntPtr[] tags;
            Dictionary<IntPtr, double[]> tagCoor = new Dictionary<IntPtr, double[]>();
            foreach (var id in surfaceIds)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out allCoor, 2, id, true, false);
                for (int i = 0; i < tags.Length; i++)
                {
                    coor = new double[3];
                    Array.Copy(allCoor, 3 * i, coor, 0, 3);
                    tagCoor[tags[i]] = coor;
                }
            }
            return tagCoor;
        }
        private void ReadSourceMesh(SweepBlock block)
        {
            int numNodes;
            int index;
            IntPtr tag;
            int[] element;
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            //
            List<int[]> elements = new List<int[]>();
            List<int> elementTypeList = new List<int>();
            List<int> elementSurfaceIds = new List<int>();
            List<IntPtr> denseTags = new List<IntPtr>();
            Dictionary<IntPtr, int> tagIndex = new Dictionary<IntPtr, int>();
            //
            foreach (var surfaceId in block.SourceSurfaceIds)
            {
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
                //
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    if (elementTypes[i] == (int)GmshElementType.LinearTriangle) numNodes = 3;
                    else if (elementTypes[i] == (int)GmshElementType.LinearQuadrilateral) numNodes = 4;
                    else throw new CaeException("The sweep source surfaces must be meshed with linear triangles " +
                                                "or linear quadrilaterals.");
                    //
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        element = new int[numNodes];
                        for (int k = 0; k < numNodes; k++)
                        {
                            tag = nodeTags[i][j * numNodes + k];
                            if (!tagIndex.TryGetValue(tag, out index))
                            {
                                index = denseTags.Count;
                                tagIndex.Add(tag, index);
                                denseTags.Add(tag);
                            }
                            element[k] = index;
                        }
                        elements.Add(element);
                        elementTypeList.Add(elementTypes[i]);
                        elementSurfaceIds.Add(surfaceId);
                    }
                }
            }
            //
            if (elements.Count == 0) throw new CaeException("The sweep source surfaces contain no elements.");
            //
            _numNodes = denseTags.Count;
            _sourceElements = elements.ToArray();
            _sourceElementTypes = elementTypeList.ToArray();
            _sourceElementSurfaceIds = elementSurfaceIds.ToArray();
            _sourceTags = denseTags.ToArray();
            _sourceTagIndex = tagIndex;
            //
            BuildAdjacency();
        }
        private void BuildAdjacency()
        {
            int n;
            int delta;
            int[] element;
            HashSet<int>[] neighbours = new HashSet<int>[_numNodes];
            List<int>[] nodeElements = new List<int>[_numNodes];
            for (int i = 0; i < _numNodes; i++)
            {
                neighbours[i] = new HashSet<int>();
                nodeElements[i] = new List<int>();
            }
            //
            for (int e = 0; e < _sourceElements.Length; e++)
            {
                element = _sourceElements[e];
                n = element.Length;
                delta = n == 3 ? 1 : 2;                 // ring neighbours only, no quad diagonals
                for (int i = 0; i < n; i++)
                {
                    neighbours[element[i]].Add(element[(i + 1) % n]);
                    neighbours[element[i]].Add(element[(i + 1 + delta) % n]);
                    nodeElements[element[i]].Add(e);
                }
            }
            //
            _adjStart = new int[_numNodes + 1];
            for (int i = 0; i < _numNodes; i++) _adjStart[i + 1] = _adjStart[i] + neighbours[i].Count;
            _adjNodes = new int[_adjStart[_numNodes]];
            for (int i = 0; i < _numNodes; i++) neighbours[i].CopyTo(_adjNodes, _adjStart[i]);
            //
            _nodeElementStart = new int[_numNodes + 1];
            for (int i = 0; i < _numNodes; i++) _nodeElementStart[i + 1] = _nodeElementStart[i] + nodeElements[i].Count;
            _nodeElements = new int[_nodeElementStart[_numNodes]];
            for (int i = 0; i < _numNodes; i++) nodeElements[i].CopyTo(_nodeElements, _nodeElementStart[i]);
        }
        /// <summary>
        /// Walks the structured wall grid from the source boundary to the target boundary and
        /// returns the ordered node tags of every boundary sweep line.
        /// </summary>
        private static Dictionary<IntPtr, List<IntPtr>> TraceWallSweepLines(SweepBlock block,
                                                                            Dictionary<int, int[]> surfaceIdEdgeIds)
        {
            int[] edgeIds;
            IntPtr[] tags;
            HashSet<int> sourceEdgeIds = new HashSet<int>();
            HashSet<int> wallEdgeIds = new HashSet<int>();
            foreach (var id in block.SourceSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) sourceEdgeIds.UnionWith(edgeIds);
            }
            foreach (var id in block.WallSurfaceIds)
            {
                if (surfaceIdEdgeIds.TryGetValue(id, out edgeIds)) wallEdgeIds.UnionWith(edgeIds);
            }
            HashSet<int> startEdgeIds = sourceEdgeIds.Intersect(wallEdgeIds).ToHashSet();
            //
            HashSet<IntPtr> startNodeTags = new HashSet<IntPtr>();
            foreach (var id in startEdgeIds)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out _, 1, id, true, false);
                startNodeTags.UnionWith(tags);
            }
            if (startNodeTags.Count == 0)
                throw new CaeException("No edges shared between the source and the side surfaces of " +
                                       block.GetDescription() + " were found.");
            //
            Dictionary<IntPtr, HashSet<IntPtr>> wallNeighbours = GetNodeNeighbours(block.WallSurfaceIds);
            //
            IntPtr next;
            IntPtr lineStart;
            HashSet<IntPtr> candidates;
            HashSet<IntPtr> nextLayer;
            Dictionary<IntPtr, List<IntPtr>> lines = new Dictionary<IntPtr, List<IntPtr>>();
            Dictionary<IntPtr, IntPtr> tagLineStart = new Dictionary<IntPtr, IntPtr>();
            foreach (var tag in startNodeTags) lines.Add(tag, new List<IntPtr>() { tag });
            //
            HashSet<IntPtr> currentLayer = new HashSet<IntPtr>(startNodeTags);
            HashSet<IntPtr> previousTwoLayers = new HashSet<IntPtr>(startNodeTags);
            int visitedCount = currentLayer.Count;
            int numLayers = 1;
            // The number of node rows can never exceed the number of wall nodes; the cap turns a
            // malformed wall grid into an error message instead of an endless loop.
            int maxLayers = wallNeighbours.Count + 1;
            //
            while (visitedCount < wallNeighbours.Count)
            {
                if (numLayers > maxLayers)
                    throw new CaeException("The side surfaces of " + block.GetDescription() + " could not be walked " +
                                           "layer by layer.");
                //
                nextLayer = new HashSet<IntPtr>();
                foreach (var tag in currentLayer)
                {
                    candidates = wallNeighbours[tag].Except(previousTwoLayers).ToHashSet();
                    if (candidates.Count != 1)
                        throw new CaeException("The number of element layers is not the same on all side surfaces " +
                                               "of " + block.GetDescription() + ": it changes at a node of " +
                                               DescribeSurfacesAtNode(block.WallSurfaceIds, tag) + ". Use the " +
                                               "same number of elements on all edges that follow the sweep " +
                                               "direction.");
                    //
                    next = candidates.First();
                    if (!tagLineStart.TryGetValue(tag, out lineStart)) lineStart = tag;
                    tagLineStart[next] = lineStart;
                    lines[lineStart].Add(next);
                    nextLayer.Add(next);
                }
                //
                previousTwoLayers = new HashSet<IntPtr>(currentLayer);
                previousTwoLayers.UnionWith(nextLayer);
                currentLayer = nextLayer;
                visitedCount += currentLayer.Count;
                numLayers++;
            }
            //
            if (numLayers < 2)
                throw new CaeException("The side surfaces of " + block.GetDescription() + " must be divided into at " +
                                       "least one element layer.");
            foreach (var entry in lines)
            {
                if (entry.Value.Count != numLayers)
                    throw new CaeException("The side surfaces of " + block.GetDescription() + " do not form a single " +
                                           "structured grid.");
            }
            //
            return lines;
        }
        private void LayOutSweepLines(Dictionary<IntPtr, List<IntPtr>> boundaryLines,
                                      Dictionary<IntPtr, double[]> tagCoor)
        {
            int node;
            int index;
            IntPtr tag;
            double[] coor;
            //
            _numLayers = boundaryLines.First().Value.Count;
            _nodeTags = new IntPtr[_numLayers * _numNodes];
            _coords = new double[3 * _numLayers * _numNodes];
            _isBoundaryNode = new bool[_numNodes];
            // Source layer
            for (int i = 0; i < _numNodes; i++)
            {
                _nodeTags[i] = _sourceTags[i];
                if (!tagCoor.TryGetValue(_sourceTags[i], out coor))
                    throw new CaeException("A sweep source node without coordinates was encountered.");
                _coords[3 * i] = coor[0];
                _coords[3 * i + 1] = coor[1];
                _coords[3 * i + 2] = coor[2];
            }
            // Boundary sweep lines - exact positions taken from the wall mesh
            foreach (var entry in boundaryLines)
            {
                if (!_sourceTagIndex.TryGetValue(entry.Key, out node))
                    throw new CaeException("A sweep wall node was not found in the source surface mesh.");
                //
                _isBoundaryNode[node] = true;
                for (int layer = 1; layer < _numLayers; layer++)
                {
                    tag = entry.Value[layer];
                    index = Index(layer, node);
                    _nodeTags[index] = tag;
                    if (!tagCoor.TryGetValue(tag, out coor))
                        throw new CaeException("A sweep wall node without coordinates was encountered.");
                    _coords[3 * index] = coor[0];
                    _coords[3 * index + 1] = coor[1];
                    _coords[3 * index + 2] = coor[2];
                }
            }
            //
            List<int> interior = new List<int>();
            for (int i = 0; i < _numNodes; i++) if (!_isBoundaryNode[i]) interior.Add(i);
            _interiorNodes = interior.ToArray();
            // A source meshed one element across has no interior node at all; every sweep line is then a wall
            // line and the morph has nothing to fill
            // New tags for the interior sweep lines
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            int nextFreeNodeTag = maxTag.ToInt32() + 1;
            for (int layer = 1; layer < _numLayers; layer++)
            {
                for (int i = 0; i < _interiorNodes.Length; i++)
                    _nodeTags[Index(layer, _interiorNodes[i])] = (IntPtr)nextFreeNodeTag++;
            }
        }
        /// <summary>
        /// The surfaces a node lies on, by their gmsh tags, for a message to say where the grid breaks down.
        /// </summary>
        private static string DescribeSurfacesAtNode(IEnumerable<int> surfaceIds, IntPtr nodeTag)
        {
            IntPtr[] tags;
            List<string> items = new List<string>();
            foreach (var surfaceId in surfaceIds.OrderBy(id => id))
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out _, 2, surfaceId, true, false);
                if (tags.Contains(nodeTag)) items.Add("surface " + surfaceId);
            }
            return string.Join(" and ", items);
        }
        private static Dictionary<IntPtr, HashSet<IntPtr>> GetNodeNeighbours(IEnumerable<int> surfaceIds)
        {
            int numNodes;
            int delta;
            IntPtr tag;
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            HashSet<IntPtr> neighbours;
            Dictionary<IntPtr, HashSet<IntPtr>> tagNeighbours = new Dictionary<IntPtr, HashSet<IntPtr>>();
            //
            foreach (var surfaceId in surfaceIds)
            {
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
                //
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    Gmsh.Model.Mesh.GetElementProperties(elementTypes[i], out _, out _, out numNodes, out _, out _);
                    if (numNodes == 3) delta = 1;
                    else if (numNodes == 4) delta = 2;
                    else throw new CaeException("The sweep wall surfaces must be meshed with linear triangles " +
                                                "or linear quadrilaterals.");
                    //
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        for (int k = 0; k < numNodes; k++)
                        {
                            tag = nodeTags[i][j * numNodes + k];
                            if (!tagNeighbours.TryGetValue(tag, out neighbours))
                            {
                                neighbours = new HashSet<IntPtr>();
                                tagNeighbours.Add(tag, neighbours);
                            }
                            neighbours.Add(nodeTags[i][j * numNodes + (k + 1) % numNodes]);
                            neighbours.Add(nodeTags[i][j * numNodes + (k + 1 + delta) % numNodes]);
                        }
                    }
                }
            }
            //
            return tagNeighbours;
        }
    }
}
