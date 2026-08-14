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
        private bool[] _isBoundaryNode;                 // source node lies on the source/wall boundary
        private int[] _interiorNodes;                   // source node indices that are not boundary nodes
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
            //
            return data;
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
            if (data._interiorNodes.Length == 0)
                throw new CaeException("The sweep source mesh has no interior nodes.");
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
                throw new CaeException("No shared edges between the sweep source and wall surfaces were found.");
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
                    throw new CaeException("The sweep wall surfaces could not be walked layer by layer.");
                //
                nextLayer = new HashSet<IntPtr>();
                foreach (var tag in currentLayer)
                {
                    candidates = wallNeighbours[tag].Except(previousTwoLayers).ToHashSet();
                    if (candidates.Count != 1)
                        throw new CaeException("The number of element layers is not the same on all sweep wall " +
                                               "surfaces. Use the same number of elements on all edges that " +
                                               "follow the sweep direction.");
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
                throw new CaeException("The sweep wall surfaces must be divided into at least one element layer.");
            foreach (var entry in lines)
            {
                if (entry.Value.Count != numLayers)
                    throw new CaeException("The sweep wall surfaces do not form a single structured grid.");
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
            if (_interiorNodes.Length == 0)
                throw new CaeException("The sweep source mesh has no interior nodes.");
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
