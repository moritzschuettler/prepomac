// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Diagnostics;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// Turns the mixed mesh of one face, quads and triangles, into quads only, on the same boundary nodes. The mesh is
    /// given in a plane frame close to isometric, the parameters of the face in the frame of ThreeBlockMapping; every
    /// node on the boundary of the face is fixed and is never moved, removed or joined to another one, and no boundary
    /// edge changes. A node is added only where two triangles cannot be paired otherwise (see below). A boundary node
    /// where the boundary turns back on itself, as at the end of a cut, has a corner of 360 degrees.
    /// 1. The triangles are paired and every pair becomes a quad. With an even number of boundary segments the number
    /// of triangles is even; with an odd one, one triangle is left. A triangle finds the nearest other triangle by a
    /// breadth-first walk through the quads, across their edges, and one of the two is carried along that path: the
    /// triangle and the quad next to it form a pentagon, which is split again into a quad and a triangle that lies on
    /// the far side, at the next element of the path; of the two such splits the one with the better worst corner is
    /// taken, and next to the other triangle the quad the two will merge into counts instead of the triangle. A step
    /// whose elements fall below the floor is smoothed at once. When the triangle reaches the other one, the two merge
    /// into a quad. Two triangles that meet at a node only are joined by the path around it. The pairs are made
    /// greedily: in every round all triangles look for their nearest partner, a triangle with an edge on the boundary
    /// first and then the shortest paths, the smaller triangle id first among equal lengths; a path that crosses one
    /// changed earlier in the round waits for the next round. A triangle on the boundary is not carried, since it would
    /// leave a corner of 180 degrees on a straight boundary, and two of them are not paired with each other by the walk.
    /// While a triangle on the boundary is left with another triangle to pair it with, it is moved off the boundary with
    /// the two quads beside and above it (LiftBoundaryTriangles). A pair is kept when, after its region is smoothed if it
    /// must be, no element folds and the worst corner of its region is not below the acceptance or below what it was,
    /// whichever is lower; otherwise it is undone, tried from the other end and, in the wider passes, along the path
    /// the walk finds with the edges taken the other way round, and the triangle then looks for another partner. The
    /// passes accept 0.5 first and then the floor, SubmapFaceDetector.MinScaledJacobian, each smoothing one ring of
    /// elements around the path and then two, and the floor also three.
    /// When two triangles or more are left, the cleanup starts again from the input with two more ways to pair, and
    /// that result is taken when it leaves fewer triangles and no fold: two triangles on the boundary with one quad
    /// between them are paired after the first pass, one carried through the quad to the other, or else, when two
    /// corners of the hexagon of the three lie on a straight boundary, the hexagon is split into three quads around a
    /// new node (PairBoundaryTriangles); and a path whose better splits are not kept is tried with every choice of the
    /// splits, up to MaxNumOfChosenSteps quads. Gmsh's Blossom recombination leaves its few triangles on the boundary,
    /// where this is what pairs them. The triangles that run still leaves are taken by its last passes
    /// (PairLeftoverTriangles): the pairs again, down to what the sharpest corner of the boundary allows - half the
    /// sine of a corner below 90 degrees - when that is below the floor; then two triangles on the boundary paired by
    /// the walk as well; then the region of a pair - the path between them alone, with one element beside it or with
    /// all of them - meshed again with quads only on the nodes around it, by every split of that polygon into quads
    /// along its diagonals and by quads around one new node (RemeshRegion); and last the path between a pair split
    /// along its length, a new node on every edge it crosses and one where it turns (SplitPath), two triangles that
    /// share an edge along a path round to each other or after one of them is carried one quad on. A quad these passes
    /// make must open by more than MinFixedCorner at a fixed node, which smoothing cannot move, and a pass that adds
    /// nodes - a region meshed again round a new node, a split path, a split hexagon - is kept only when the worst
    /// shape around it (GetMinShape, which also sees how narrow an element is) is not below ShapeFloor or below what
    /// it was.
    /// An operation whose region keeps a corner below the acceptance that no free node of the region moves is refused
    /// without smoothing it (HasStuckCorner), which the check after the smoothing would refuse as well.
    /// 2. The topology is improved by local operations that lower the sum of |valence - ideal| over the nodes, ideal
    /// being 4 inside and, on the boundary, one more than the number of quads the angle of the boundary asks for (one
    /// for every 90 degrees, at least one): an inner node joined to two quads only, a doublet, is removed and its quads
    /// merged; a quad whose diagonal joins two inner nodes of valence 3 is collapsed along that diagonal when that does
    /// not raise the sum; the edge between two quads is rotated to one of the two other diagonals of their hexagon when
    /// that lowers the sum. Every operation is kept under the rule of the pairs with the acceptance 0.5, on a mesh that
    /// had no triangle the worst corner of the input when that is higher.
    /// 3. The nodes the operations touched, the nodes of elements whose worst corner is below the floor and their
    /// neighbours are smoothed by QuadSmoothing.SmoothRegion, which never lowers the worst corner.
    /// The quality of a corner is its scaled Jacobian, a triangle scaled so that 60 degrees count as 1. The arithmetic
    /// is plain and ordered and every choice is made in the order of the ids, so that a port gives the same mesh.
    /// </summary>
    public class QuadCleanup
    {
        // Variables                                                                                                                
        private const int MaxNumOfPairingRounds = 100;
        private const int MaxNumOfTopologyPasses = 8;
        private const int NumOfLocalSweeps = 3;
        private const int NumOfFinalSweeps = 8;
        private const int MaxNumOfChosenSteps = 4;
        private const double GoodQuality = 0.5;
        private const double SharpCornerShare = 0.5;
        private const int MaxNumOfCavityNodes = 12;
        private const int MaxNumOfCavityPathElements = 6;
        private const int MaxNumOfCavityTrials = 6;
        private const double MinFixedCorner = 1E-3;
        private const double ShapeFloor = 0.2;
        private double[][] _points;
        private bool[] _fixed;
        private bool[] _boundary;
        private int[] _idealValence;
        private readonly double _orientation;
        private readonly double _floor;
        private double _cornerFloor;
        private readonly bool _extendedPairing;
        private readonly bool _lastPasses;
        private bool _pairBoundaryTriangles;
        private double _acceptance;
        private readonly List<int[]> _elements;
        private List<int>[] _nodeElements;
        private readonly Dictionary<long, int[]> _edgeElements;
        private bool[] _touchedNodes;
        private readonly List<int> _visitStamps;
        private readonly List<int> _parents;
        private readonly List<int> _queue;
        private int _stamp;
        private int _numOfUsedNodes;
        // The operation being tried, which is undone when it is not kept
        private int _numOfElementsAtStart;
        private int _numOfUsedNodesAtStart;
        private readonly List<int> _removedIds;
        private readonly List<int[]> _removedElements;
        private readonly List<int> _movedNodes;
        private readonly List<double[]> _oldPositions;
        //
        private int _numOfPairs;
        private int _numOfDoublets;
        private int _numOfCollapses;
        private int _numOfRotations;
        private int _numOfRemeshedRegions;
        private int _numOfSplitPaths;


        // Constructors                                                                                                             
        private QuadCleanup(double[][] points, int[][] elements, bool[] fixedNodes, bool extendedPairing,
                            bool lastPasses)
        {
            _extendedPairing = extendedPairing;
            _lastPasses = lastPasses;
            // Room for the nodes the splits of hexagons add, one for every two triangles at most
            int numOfInputTriangles = 0;
            foreach (var element in elements)
            {
                if (element.Length == 3) numOfInputTriangles++;
            }
            _numOfUsedNodes = points.Length;
            int numOfNodes = points.Length + numOfInputTriangles / 2;
            _points = new double[numOfNodes][];
            for (int n = 0; n < numOfNodes; n++)
                _points[n] = n < points.Length ? new double[] { points[n][0], points[n][1] } : new double[2];
            _fixed = new bool[numOfNodes];
            Array.Copy(fixedNodes, _fixed, fixedNodes.Length);
            _floor = SubmapFaceDetector.MinScaledJacobian;
            _elements = new List<int[]>();
            _nodeElements = new List<int>[numOfNodes];
            for (int n = 0; n < numOfNodes; n++) _nodeElements[n] = new List<int>();
            _edgeElements = new Dictionary<long, int[]>();
            _touchedNodes = new bool[numOfNodes];
            _visitStamps = new List<int>();
            _parents = new List<int>();
            _queue = new List<int>();
            _removedIds = new List<int>();
            _removedElements = new List<int[]>();
            _movedNodes = new List<int>();
            _oldPositions = new List<double[]>();
            double area = 0;
            foreach (var element in elements)
            {
                if (element.Length != 3 && element.Length != 4)
                    throw new CaeException("The cleanup takes quads and triangles only, not elements of " + element.Length +
                                           " nodes.");
                area += GetSignedArea(element);
                AddElement((int[])element.Clone());
            }
            _orientation = area < 0 ? -1 : 1;
            _boundary = new bool[numOfNodes];
            _idealValence = new int[numOfNodes];
            SetBoundary();
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Cleans the mesh of a face given by the positions of its nodes in a plane frame, its quads and triangles, which
        /// turn the same way, and the nodes that are fixed, every node on the boundary of the face among them.
        /// </summary>
        public static QuadCleanupResult Clean(double[][] points, int[][] elements, bool[] fixedNodes)
        {
            return Clean(points, elements, fixedNodes, true);
        }
        /// <summary>
        /// The cleanup with its last passes (PairLeftoverTriangles) or without them, as it was before they existed.
        /// </summary>
        public static QuadCleanupResult Clean(double[][] points, int[][] elements, bool[] fixedNodes, bool lastPasses)
        {
            if (points == null || elements == null || fixedNodes == null || fixedNodes.Length != points.Length)
                throw new CaeException("The cleanup needs the positions of the nodes, the elements and which nodes are fixed.");
            Stopwatch watch = Stopwatch.StartNew();
            QuadCleanupResult result = new QuadCleanup(points, elements, fixedNodes, false, false).Run(watch);
            // Triangles that could not be paired are tried again from the start with the pairs of triangles on the
            // boundary and the other choices of the splits, which are kept only when they leave fewer
            if (result.NumberOfTriangles > 1)
            {
                QuadCleanupResult extended = new QuadCleanup(points, elements, fixedNodes, true, lastPasses).Run(watch);
                if (extended.NumberOfTriangles < result.NumberOfTriangles && extended.NumberOfFoldedElements == 0)
                    result = extended;
            }
            return result;
        }
        private QuadCleanupResult Run(Stopwatch watch)
        {
            int numOfTrianglesBefore = CountTriangles();
            double minScaledJacobianBefore = QuadSmoothing.GetMinScaledJacobian(_points, GetElements(), _orientation);
            int numOfIrregularBefore = CountIrregularNodes();
            //
            if (numOfTrianglesBefore > 1)
            {
                // The pairs that keep their regions good first, then those down to the floor
                foreach (var acceptance in new double[] { GoodQuality, _floor })
                {
                    _acceptance = acceptance;
                    PairTriangles(1);
                    bool changed = _extendedPairing && PairBoundaryTriangles();
                    if (LiftBoundaryTriangles()) changed = true;
                    if (changed) PairTriangles(1);
                    PairTriangles(2);
                    if (acceptance < GoodQuality) PairTriangles(3);
                }
                if (_lastPasses && CountTriangles() > 1) PairLeftoverTriangles();
            }
            // A mesh of quads only keeps its worst corner
            _acceptance = numOfTrianglesBefore == 0 ? Math.Max(GoodQuality, minScaledJacobianBefore) : GoodQuality;
            ImproveTopology();
            SmoothFinal();
            //
            int[][] result = GetElements();
            double[][] resultPoints = new double[_numOfUsedNodes][];
            Array.Copy(_points, resultPoints, resultPoints.Length);
            bool[] isNodeUsed = new bool[resultPoints.Length];
            foreach (var element in result)
            {
                foreach (var node in element) isNodeUsed[node] = true;
            }
            return new QuadCleanupResult(result, resultPoints, isNodeUsed, numOfTrianglesBefore, CountTriangles(),
                                         QuadSmoothing.CountFoldedElements(_points, result, _orientation),
                                         minScaledJacobianBefore,
                                         QuadSmoothing.GetMinScaledJacobian(_points, result, _orientation),
                                         numOfIrregularBefore, CountIrregularNodes(), _numOfPairs, _numOfDoublets,
                                         _numOfCollapses, _numOfRotations, _numOfRemeshedRegions, _numOfSplitPaths,
                                         watch.Elapsed.TotalSeconds);
        }
        private static long GetEdgeKey(int a, int b)
        {
            return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        }
        /// <summary>
        /// Adds a free inner node at the given position, the arrays of the nodes grown when they are full; the node is
        /// removed again when the operation is undone.
        /// </summary>
        private int AddNode(double x, double y)
        {
            if (_numOfUsedNodes == _points.Length)
            {
                int capacity = 2 * _points.Length + 8;
                int numOfNodes = _points.Length;
                Array.Resize(ref _points, capacity);
                Array.Resize(ref _fixed, capacity);
                Array.Resize(ref _boundary, capacity);
                Array.Resize(ref _idealValence, capacity);
                Array.Resize(ref _nodeElements, capacity);
                Array.Resize(ref _touchedNodes, capacity);
                for (int n = numOfNodes; n < capacity; n++)
                {
                    _points[n] = new double[2];
                    _nodeElements[n] = new List<int>();
                    _idealValence[n] = 4;
                }
            }
            int node = _numOfUsedNodes++;
            _points[node][0] = x;
            _points[node][1] = y;
            return node;
        }
        private int AddElement(int[] nodes)
        {
            int id = _elements.Count;
            _elements.Add(nodes);
            Register(id, nodes);
            return id;
        }
        private void Register(int id, int[] nodes)
        {
            int n = nodes.Length;
            for (int c = 0; c < n; c++)
            {
                _nodeElements[nodes[c]].Add(id);
                long key = GetEdgeKey(nodes[c], nodes[(c + 1) % n]);
                int[] slots;
                if (!_edgeElements.TryGetValue(key, out slots))
                {
                    slots = new int[] { -1, -1 };
                    _edgeElements.Add(key, slots);
                }
                if (slots[0] < 0) slots[0] = id;
                else if (slots[1] < 0) slots[1] = id;
                else throw new CaeException("The mesh of the face has an edge shared by more than two elements.");
            }
        }
        private void Unregister(int id, int[] nodes)
        {
            int n = nodes.Length;
            for (int c = 0; c < n; c++)
            {
                _nodeElements[nodes[c]].Remove(id);
                long key = GetEdgeKey(nodes[c], nodes[(c + 1) % n]);
                int[] slots = _edgeElements[key];
                if (slots[0] == id) slots[0] = -1;
                else if (slots[1] == id) slots[1] = -1;
                if (slots[0] < 0 && slots[1] < 0) _edgeElements.Remove(key);
            }
        }
        private void RemoveElement(int id)
        {
            int[] nodes = _elements[id];
            Unregister(id, nodes);
            _elements[id] = null;
            _removedIds.Add(id);
            _removedElements.Add(nodes);
        }
        /// <summary>
        /// The element on the other side of the edge from a and b, -1 for none.
        /// </summary>
        private int GetNeighbour(int id, int a, int b)
        {
            int[] slots;
            if (!_edgeElements.TryGetValue(GetEdgeKey(a, b), out slots)) return -1;
            if (slots[0] == id) return slots[1];
            if (slots[1] == id) return slots[0];
            return -1;
        }
        private bool EdgeExists(int a, int b)
        {
            return _edgeElements.ContainsKey(GetEdgeKey(a, b));
        }
        /// <summary>
        /// Marks the boundary nodes and their ideal valences, and lowers the floor of the last passes, _cornerFloor, to
        /// what the sharpest corner of the boundary allows: SharpCornerShare times the sine of a corner below 90
        /// degrees.
        /// </summary>
        private void SetBoundary()
        {
            _cornerFloor = _floor;
            int numOfNodes = _points.Length;
            int[] outgoing = new int[numOfNodes];
            int[] incoming = new int[numOfNodes];
            int[] numOfOutgoing = new int[numOfNodes];
            for (int id = 0; id < _elements.Count; id++)
            {
                int[] nodes = _elements[id];
                int n = nodes.Length;
                for (int c = 0; c < n; c++)
                {
                    int a = nodes[c];
                    int b = nodes[(c + 1) % n];
                    if (GetNeighbour(id, a, b) >= 0) continue;
                    _boundary[a] = true;
                    _boundary[b] = true;
                    outgoing[a] = b;
                    incoming[b] = a;
                    numOfOutgoing[a]++;
                }
            }
            for (int v = 0; v < numOfNodes; v++)
            {
                if (!_boundary[v])
                {
                    _idealValence[v] = 4;
                    continue;
                }
                if (numOfOutgoing[v] != 1)
                {
                    _idealValence[v] = -1;
                    continue;
                }
                double[] p = _points[v];
                double[] next = _points[outgoing[v]];
                double[] previous = _points[incoming[v]];
                double ax = next[0] - p[0];
                double ay = next[1] - p[1];
                double bx = previous[0] - p[0];
                double by = previous[1] - p[1];
                double angle = Math.Atan2(_orientation * (ax * by - ay * bx), ax * bx + ay * by);
                if (angle <= 0) angle += 2 * Math.PI;
                if (angle < 0.5 * Math.PI) _cornerFloor = Math.Min(_cornerFloor, SharpCornerShare * Math.Sin(angle));
                int numOfQuads = Math.Max(1, (int)Math.Round(angle / (0.5 * Math.PI)));
                _idealValence[v] = numOfQuads + 1;
            }
        }
        private int GetValence(int node)
        {
            return _nodeElements[node].Count + (_boundary[node] ? 1 : 0);
        }
        private int GetIrregularity(int node, int valence)
        {
            if (_idealValence[node] < 0) return 0;
            return Math.Abs(valence - _idealValence[node]);
        }
        private int CountIrregularNodes()
        {
            int count = 0;
            for (int v = 0; v < _points.Length; v++)
            {
                if (!_boundary[v] && _nodeElements[v].Count > 0 && GetValence(v) != 4) count++;
            }
            return count;
        }
        private int CountTriangles()
        {
            int count = 0;
            foreach (var element in _elements)
            {
                if (element != null && element.Length == 3) count++;
            }
            return count;
        }
        private int[][] GetElements()
        {
            List<int[]> elements = new List<int[]>();
            foreach (var element in _elements)
            {
                if (element != null) elements.Add(element);
            }
            return elements.ToArray();
        }
        private double GetSignedArea(int[] nodes)
        {
            double area = 0;
            for (int c = 0; c < nodes.Length; c++)
            {
                double[] p = _points[nodes[c]];
                double[] q = _points[nodes[(c + 1) % nodes.Length]];
                area += p[0] * q[1] - q[0] * p[1];
            }
            return 0.5 * area;
        }
        private double GetMinCorner(int[] nodes)
        {
            double min = double.MaxValue;
            for (int c = 0; c < nodes.Length; c++)
                min = Math.Min(min, QuadSmoothing.GetElementScaledJacobian(_points, nodes, c, _orientation));
            return min;
        }
        private void Begin()
        {
            _numOfElementsAtStart = _elements.Count;
            _numOfUsedNodesAtStart = _numOfUsedNodes;
            _removedIds.Clear();
            _removedElements.Clear();
            _movedNodes.Clear();
            _oldPositions.Clear();
        }
        private void Commit(List<int> seedNodes)
        {
            foreach (var node in seedNodes) _touchedNodes[node] = true;
            foreach (var node in _movedNodes) _touchedNodes[node] = true;
        }
        private void Rollback()
        {
            for (int id = _elements.Count - 1; id >= _numOfElementsAtStart; id--)
            {
                if (_elements[id] != null) Unregister(id, _elements[id]);
            }
            _elements.RemoveRange(_numOfElementsAtStart, _elements.Count - _numOfElementsAtStart);
            for (int i = _removedIds.Count - 1; i >= 0; i--)
            {
                if (_removedIds[i] >= _numOfElementsAtStart) continue;
                _elements[_removedIds[i]] = _removedElements[i];
                Register(_removedIds[i], _removedElements[i]);
            }
            for (int i = _movedNodes.Count - 1; i >= 0; i--)
            {
                _points[_movedNodes[i]][0] = _oldPositions[i][0];
                _points[_movedNodes[i]][1] = _oldPositions[i][1];
            }
            for (int node = _numOfUsedNodesAtStart; node < _numOfUsedNodes; node++)
            {
                _points[node][0] = 0;
                _points[node][1] = 0;
            }
            _numOfUsedNodes = _numOfUsedNodesAtStart;
            _removedIds.Clear();
            _removedElements.Clear();
            _movedNodes.Clear();
            _oldPositions.Clear();
        }
        private void SaveNode(int node)
        {
            _movedNodes.Add(node);
            _oldPositions.Add(new double[] { _points[node][0], _points[node][1] });
        }
        /// <summary>
        /// The seed nodes and every node within the given number of rings of elements around them, in ascending order.
        /// </summary>
        private List<int> GetRegionNodes(List<int> seedNodes, int numOfRings)
        {
            List<int> nodes = new List<int>();
            int stamp = NextStamp(_points.Length);
            foreach (var node in seedNodes)
            {
                if (_visitStamps[node] == stamp) continue;
                _visitStamps[node] = stamp;
                nodes.Add(node);
            }
            int ringStart = 0;
            for (int ring = 0; ring < numOfRings; ring++)
            {
                int ringEnd = nodes.Count;
                for (int i = ringStart; i < ringEnd; i++)
                {
                    foreach (var id in _nodeElements[nodes[i]])
                    {
                        foreach (var other in _elements[id])
                        {
                            if (_visitStamps[other] == stamp) continue;
                            _visitStamps[other] = stamp;
                            nodes.Add(other);
                        }
                    }
                }
                ringStart = ringEnd;
            }
            nodes.Sort();
            return nodes;
        }
        /// <summary>
        /// The elements of the given nodes, in ascending order of their ids.
        /// </summary>
        private List<int> GetNodesElements(List<int> nodes)
        {
            List<int> ids = new List<int>();
            HashSet<int> added = new HashSet<int>();
            foreach (var node in nodes)
            {
                foreach (var id in _nodeElements[node])
                {
                    if (added.Add(id)) ids.Add(id);
                }
            }
            ids.Sort();
            return ids;
        }
        private int NextStamp(int size)
        {
            while (_visitStamps.Count < size) _visitStamps.Add(0);
            return ++_stamp;
        }
        private double GetRegionQuality(List<int> seedNodes, int numOfRings)
        {
            double min = 1;
            foreach (var id in GetNodesElements(GetRegionNodes(seedNodes, numOfRings)))
                min = Math.Min(min, GetMinCorner(_elements[id]));
            return min;
        }
        /// <summary>
        /// Smooths the region of the seed nodes after an operation and tells whether the operation is kept: no element
        /// folds and the worst corner is not below the floor or below the worst corner before, whichever is lower. The
        /// region is the given number of rings of elements around the seed nodes, smoothed for as many times the sweeps
        /// of one ring.
        /// </summary>
        private bool SmoothAndCheck(List<int> seedNodes, double before, int numOfRings)
        {
            double threshold = Math.Min(_acceptance, before);
            List<int> regionNodes = GetRegionNodes(seedNodes, numOfRings);
            List<int> regionElementIds = GetNodesElements(regionNodes);
            double after = 1;
            foreach (var id in regionElementIds) after = Math.Min(after, GetMinCorner(_elements[id]));
            if (after > 0 && after >= threshold) return true;
            // Smoothed only when the operation is not good enough as it is; the final smoothing evens out the rest
            if (HasStuckCorner(regionNodes, regionElementIds, threshold)) return false;
            if (!SmoothNodes(regionNodes, numOfRings * numOfRings * NumOfLocalSweeps)) return false;
            after = 1;
            foreach (var id in regionElementIds) after = Math.Min(after, GetMinCorner(_elements[id]));
            return after > 0 && after >= threshold;
        }
        /// <summary>
        /// Whether an element of the region has a corner below the threshold, or one that folds, that smoothing the
        /// free nodes among the region nodes cannot change: its node and both nodes beside it fixed or outside the
        /// region. The check after the smoothing would refuse the operation then, so the smoothing is not done.
        /// </summary>
        private bool HasStuckCorner(List<int> regionNodes, List<int> regionElementIds, double threshold)
        {
            HashSet<int> freeNodes = new HashSet<int>();
            foreach (var node in regionNodes)
            {
                if (!_fixed[node] && _nodeElements[node].Count > 0) freeNodes.Add(node);
            }
            foreach (var id in regionElementIds)
            {
                int[] nodes = _elements[id];
                int n = nodes.Length;
                for (int c = 0; c < n; c++)
                {
                    if (freeNodes.Contains(nodes[c]) || freeNodes.Contains(nodes[(c + 1) % n]) ||
                        freeNodes.Contains(nodes[(c + n - 1) % n])) continue;
                    double quality = QuadSmoothing.GetElementScaledJacobian(_points, nodes, c, _orientation);
                    if (!(quality > 0 && quality >= threshold)) return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Smooths the free nodes among the given ones, their positions saved for the operation to be undone; false when
        /// none is free.
        /// </summary>
        private bool SmoothNodes(List<int> nodes, int numOfSweeps)
        {
            List<int> freeNodes = new List<int>();
            foreach (var node in nodes)
            {
                if (!_fixed[node] && _nodeElements[node].Count > 0) freeNodes.Add(node);
            }
            if (freeNodes.Count == 0) return false;
            List<int> elementIds = GetNodesElements(freeNodes);
            int[][] elements = new int[elementIds.Count][];
            for (int i = 0; i < elements.Length; i++) elements[i] = _elements[elementIds[i]];
            foreach (var node in freeNodes) SaveNode(node);
            QuadSmoothing.SmoothRegion(_points, elements, freeNodes.ToArray(), _orientation, numOfSweeps, _floor);
            return true;
        }
        /// <summary>
        /// Finish for an operation that adds nodes, which may halve elements that are narrow already: it is kept only
        /// when the worst shape of the elements around the seed nodes is not below ShapeFloor or below the worst shape
        /// there before, whichever is lower (GetMinShape).
        /// </summary>
        private bool FinishShaped(List<int> seedNodes, double before, double beforeShape, int numOfRings)
        {
            if (SmoothAndCheck(seedNodes, before, numOfRings) &&
                GetRegionShape(seedNodes, numOfRings) >= Math.Min(ShapeFloor, beforeShape))
            {
                Commit(seedNodes);
                return true;
            }
            Rollback();
            return false;
        }
        /// <summary>
        /// The worst shape of the elements within the given number of rings around the seed nodes.
        /// </summary>
        private double GetRegionShape(List<int> seedNodes, int numOfRings)
        {
            double min = double.MaxValue;
            foreach (var id in GetNodesElements(GetRegionNodes(seedNodes, numOfRings)))
                min = Math.Min(min, GetMinShape(_elements[id]));
            return min;
        }
        /// <summary>
        /// The worst shape of the corners of an element: the scaled Jacobian times 2 |a| |b| / (|a|^2 + |b|^2), a and b
        /// the edges at the corner, which is 2 |a x b| / (|a|^2 + |b|^2) - 1 at a square corner, 0.38 at a corner of a
        /// 5:1 rectangle, 0.2 at one of 10:1 - and a triangle measured against the equilateral one, which counts 1.
        /// </summary>
        internal static double GetMinShape(double[][] points, int[] nodes, double orientation)
        {
            double min = double.MaxValue;
            int n = nodes.Length;
            for (int c = 0; c < n; c++)
            {
                double[] p = points[nodes[c]];
                double[] a = points[nodes[(c + 1) % n]];
                double[] b = points[nodes[(c + n - 1) % n]];
                double aLength2 = (a[0] - p[0]) * (a[0] - p[0]) + (a[1] - p[1]) * (a[1] - p[1]);
                double bLength2 = (b[0] - p[0]) * (b[0] - p[0]) + (b[1] - p[1]) * (b[1] - p[1]);
                double factor = aLength2 + bLength2 > 0 ?
                                2 * Math.Sqrt(aLength2 * bLength2) / (aLength2 + bLength2) : 0;
                min = Math.Min(min, QuadSmoothing.GetElementScaledJacobian(points, nodes, c, orientation) * factor);
            }
            return min;
        }
        private double GetMinShape(int[] nodes)
        {
            return GetMinShape(_points, nodes, _orientation);
        }
        private bool Finish(List<int> seedNodes, double before, int numOfRings)
        {
            if (SmoothAndCheck(seedNodes, before, numOfRings))
            {
                Commit(seedNodes);
                return true;
            }
            Rollback();
            return false;
        }
        /// <summary>
        /// Pairs the triangles round by round, the shortest paths first, the regions of the pairs smoothed over the given
        /// number of rings. A path that cannot be kept is tried from its other end.
        /// </summary>
        private void PairTriangles(int numOfRings)
        {
            HashSet<long> failedPairs = new HashSet<long>();
            for (int round = 0; round < MaxNumOfPairingRounds; round++)
            {
                List<List<int>> paths = new List<List<int>>();
                int numOfElements = _elements.Count;
                for (int id = 0; id < numOfElements; id++)
                {
                    if (_elements[id] == null || _elements[id].Length != 3) continue;
                    List<int> path = FindPath(id, failedPairs, false);
                    if (path != null) paths.Add(path);
                }
                if (paths.Count == 0) break;
                // A triangle on the boundary first, as it can only be paired by the other triangle coming to it
                bool[] onBoundary = new bool[paths.Count];
                for (int i = 0; i < paths.Count; i++) onBoundary[i] = IsOnBoundary(paths[i][0]);
                int[] order = new int[paths.Count];
                for (int i = 0; i < order.Length; i++) order[i] = i;
                Array.Sort(order, (a, b) => onBoundary[a] != onBoundary[b] ? (onBoundary[a] ? -1 : 1) :
                                            paths[a].Count != paths[b].Count ? paths[a].Count.CompareTo(paths[b].Count) :
                                            paths[a][0].CompareTo(paths[b][0]));
                List<List<int>> sortedPaths = new List<List<int>>();
                foreach (var i in order) sortedPaths.Add(paths[i]);
                paths = sortedPaths;
                int numOfAttempts = 0;
                foreach (var path in paths)
                {
                    bool alive = true;
                    foreach (var id in path)
                    {
                        if (_elements[id] == null)
                        {
                            alive = false;
                            break;
                        }
                    }
                    if (!alive) continue;
                    numOfAttempts++;
                    int first = path[0];
                    int last = path[path.Count - 1];
                    // The path from either end, and over more rings also the path the walk finds with the edges taken
                    // the other way round; then the same with every other choice of the splits along a short path
                    bool paired = false;
                    for (int pass = 0; pass < 2 && !paired; pass++)
                    {
                        for (int option = 0; option < (numOfRings > 1 ? 4 : 2) && !paired; option++)
                        {
                            List<int> optionPath = option < 2 ? new List<int>(path) : FindPath(first, failedPairs, true);
                            if (optionPath == null) break;
                            // The triangle on the boundary stays, the other one moves to it
                            if ((option % 2 == 1) != IsOnBoundary(optionPath[0])) optionPath.Reverse();
                            List<int> seedNodes = new List<int>();
                            foreach (var id in optionPath) seedNodes.AddRange(_elements[id]);
                            double before = GetRegionQuality(seedNodes, numOfRings);
                            if (pass == 0)
                            {
                                Begin();
                                paired = MoveTriangle(optionPath, -1) && Finish(seedNodes, before, numOfRings);
                                if (!paired) Rollback();
                            }
                            else if (_extendedPairing)
                                paired = MoveTriangleByChoices(optionPath, seedNodes, before, numOfRings);
                        }
                    }
                    if (paired) _numOfPairs++;
                    else
                    {
                        failedPairs.Add(((long)first << 32) | (uint)last);
                        failedPairs.Add(((long)last << 32) | (uint)first);
                    }
                }
                if (numOfAttempts == 0) break;
            }
        }
        /// <summary>
        /// The last passes, for the triangles the others leave: the pairs down to the floor the sharpest corner of the
        /// boundary allows, then two triangles on the boundary paired by the walk as well, then the region of a pair
        /// meshed again with quads only (RemeshRegion) and last the path between a pair split along its length
        /// (SplitPath).
        /// </summary>
        private void PairLeftoverTriangles()
        {
            _acceptance = _cornerFloor;
            if (_cornerFloor < _floor)
            {
                PairTriangles(1);
                if (LiftBoundaryTriangles()) PairTriangles(1);
                PairTriangles(2);
                PairTriangles(3);
            }
            _pairBoundaryTriangles = true;
            if (CountTriangles() > 1) PairTriangles(1);
            if (CountTriangles() > 1) PairRegions(false);
            if (CountTriangles() > 1) PairRegions(true);
            _pairBoundaryTriangles = false;
        }
        /// <summary>
        /// Pairs every triangle with the nearest other one it has not failed with, by meshing their region again or,
        /// with split, by splitting the path between them, until a round pairs none.
        /// </summary>
        private void PairRegions(bool split)
        {
            HashSet<long> failedPairs = new HashSet<long>();
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int id = 0; id < _elements.Count; id++)
                {
                    if (_elements[id] == null || _elements[id].Length != 3) continue;
                    List<int> path = FindPath(id, failedPairs, false);
                    if (path == null) continue;
                    bool paired;
                    if (!split) paired = RemeshRegion(path);
                    else if (path.Count > 2) paired = SplitPath(path);
                    else
                    {
                        List<int> detour = FindDetour(path[0], path[1]);
                        paired = detour != null && SplitPath(detour) || CarryAndSplit(path[0], path[1]) ||
                                 CarryAndSplit(path[1], path[0]);
                    }
                    if (paired)
                    {
                        if (split) _numOfSplitPaths++;
                        else _numOfRemeshedRegions++;
                        changed = true;
                    }
                    else
                    {
                        int last = path[path.Count - 1];
                        failedPairs.Add(((long)id << 32) | (uint)last);
                        failedPairs.Add(((long)last << 32) | (uint)id);
                    }
                }
            }
        }
        /// <summary>
        /// The shortest path through the quads from one triangle to the other that does not cross the edge they share,
        /// both included; null for none. Split along its length, it gives every node of that edge an edge inside, which
        /// two triangles on a straight boundary with a common apex need.
        /// </summary>
        private List<int> FindDetour(int start, int end)
        {
            int stamp = NextStamp(_elements.Count);
            while (_parents.Count < _visitStamps.Count) _parents.Add(-1);
            List<int> queue = _queue;
            queue.Clear();
            queue.Add(start);
            _visitStamps[start] = stamp;
            for (int head = 0; head < queue.Count; head++)
            {
                int id = queue[head];
                int[] nodes = _elements[id];
                for (int c = 0; c < nodes.Length; c++)
                {
                    int neighbour = GetNeighbour(id, nodes[c], nodes[(c + 1) % nodes.Length]);
                    if (neighbour < 0 || _visitStamps[neighbour] == stamp) continue;
                    if (neighbour == end)
                    {
                        if (id == start) continue;
                        List<int> path = new List<int> { end };
                        for (int current = id; current != start; current = _parents[current]) path.Add(current);
                        path.Add(start);
                        path.Reverse();
                        return path;
                    }
                    if (_elements[neighbour].Length != 4) continue;
                    _visitStamps[neighbour] = stamp;
                    _parents[neighbour] = id;
                    queue.Add(neighbour);
                }
            }
            return null;
        }
        /// <summary>
        /// Meshes the region of two triangles again with quads only, on the nodes around it: the path between them, the
        /// path with one element beside it, and the path with every element beside it, as long as the region is a disc
        /// of at most MaxNumOfCavityNodes nodes around it whose inner nodes are free. Every way to split the polygon
        /// around the region into quads by its diagonals is tried, and the quads around one new node joined to every
        /// second corner; the ones of the better worst corner first, MaxNumOfCavityTrials of them at most, each kept
        /// under the rule of the pairs with two rings of elements smoothed around the region.
        /// </summary>
        private bool RemeshRegion(List<int> path)
        {
            if (path.Count > MaxNumOfCavityPathElements) return false;
            List<List<int>> regions = new List<List<int>>();
            regions.Add(path);
            List<int> neighbours = GetAdjacentElements(path);
            foreach (var id in neighbours) regions.Add(new List<int>(path) { id });
            if (neighbours.Count > 1)
            {
                List<int> region = new List<int>(path);
                region.AddRange(neighbours);
                regions.Add(region);
            }
            List<int> candidateRegions = new List<int>();
            List<List<int>> candidatePolygons = new List<List<int>>();
            List<List<int[]>> candidateQuads = new List<List<int[]>>();
            List<double[]> candidateCentres = new List<double[]>();
            List<double> scores = new List<double>();
            for (int r = 0; r < regions.Count; r++)
            {
                List<int> polygon = GetRegionPolygon(regions[r]);
                if (polygon == null || polygon.Count > MaxNumOfCavityNodes) continue;
                HashSet<long> innerEdges = GetInnerEdges(regions[r]);
                foreach (var quads in GetQuadrangulations(polygon, innerEdges))
                {
                    double score = double.MaxValue;
                    foreach (var quad in quads) score = Math.Min(score, GetMinCorner(quad));
                    candidateRegions.Add(r);
                    candidatePolygons.Add(polygon);
                    candidateQuads.Add(quads);
                    candidateCentres.Add(null);
                    scores.Add(score);
                }
                if (polygon.Count < 6) continue;
                // The quads around a new node in the middle
                double[] centre = new double[2];
                foreach (var node in polygon)
                {
                    centre[0] += _points[node][0] / polygon.Count;
                    centre[1] += _points[node][1] / polygon.Count;
                }
                for (int offset = 0; offset < 2; offset++)
                {
                    List<int[]> quads = new List<int[]>();
                    double score = double.MaxValue;
                    for (int k = offset; k < polygon.Count; k += 2)
                    {
                        int[] quad = new int[] { -1, polygon[k], polygon[(k + 1) % polygon.Count],
                                                 polygon[(k + 2) % polygon.Count] };
                        if (!HasOpenFixedCorners(quad, centre)) score = double.NegativeInfinity;
                        quads.Add(quad);
                        score = Math.Min(score, GetMinCorner(quad, centre));
                    }
                    candidateRegions.Add(r);
                    candidatePolygons.Add(polygon);
                    candidateQuads.Add(quads);
                    candidateCentres.Add(centre);
                    scores.Add(score);
                }
            }
            int[] order = new int[scores.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, (a, b) => scores[a] != scores[b] ? scores[b].CompareTo(scores[a]) : a.CompareTo(b));
            for (int i = 0; i < order.Length && i < MaxNumOfCavityTrials; i++)
            {
                int candidate = order[i];
                if (double.IsNegativeInfinity(scores[candidate])) break;
                List<int> seedNodes = new List<int>(candidatePolygons[candidate]);
                double before = GetRegionQuality(seedNodes, 2);
                double beforeShape = GetRegionShape(seedNodes, 2);
                Begin();
                foreach (var id in regions[candidateRegions[candidate]]) RemoveElement(id);
                int centre = -1;
                if (candidateCentres[candidate] != null)
                {
                    centre = AddNode(candidateCentres[candidate][0], candidateCentres[candidate][1]);
                    seedNodes.Add(centre);
                }
                foreach (var quad in candidateQuads[candidate])
                {
                    int[] nodes = (int[])quad.Clone();
                    for (int c = 0; c < nodes.Length; c++)
                    {
                        if (nodes[c] < 0) nodes[c] = centre;
                    }
                    AddElement(nodes);
                }
                if (candidateCentres[candidate] == null ? Finish(seedNodes, before, 2) :
                                                          FinishShaped(seedNodes, before, beforeShape, 2)) return true;
            }
            return false;
        }
        /// <summary>
        /// The elements that share an edge with one of the given ones and are not among them, in ascending order.
        /// </summary>
        private List<int> GetAdjacentElements(List<int> ids)
        {
            HashSet<int> given = new HashSet<int>(ids);
            List<int> adjacent = new List<int>();
            foreach (var id in ids)
            {
                int[] nodes = _elements[id];
                for (int c = 0; c < nodes.Length; c++)
                {
                    int neighbour = GetNeighbour(id, nodes[c], nodes[(c + 1) % nodes.Length]);
                    if (neighbour >= 0 && !given.Contains(neighbour) && !adjacent.Contains(neighbour))
                        adjacent.Add(neighbour);
                }
            }
            adjacent.Sort();
            return adjacent;
        }
        /// <summary>
        /// The edges two of the given elements share.
        /// </summary>
        private HashSet<long> GetInnerEdges(List<int> ids)
        {
            HashSet<int> given = new HashSet<int>(ids);
            HashSet<long> edges = new HashSet<long>();
            foreach (var id in ids)
            {
                int[] nodes = _elements[id];
                for (int c = 0; c < nodes.Length; c++)
                {
                    int neighbour = GetNeighbour(id, nodes[c], nodes[(c + 1) % nodes.Length]);
                    if (neighbour >= 0 && given.Contains(neighbour))
                        edges.Add(GetEdgeKey(nodes[c], nodes[(c + 1) % nodes.Length]));
                }
            }
            return edges;
        }
        /// <summary>
        /// The nodes around the given elements in the order of their turn, starting from the smallest id; null when
        /// they do not form a disc or an inner node of theirs is fixed.
        /// </summary>
        private List<int> GetRegionPolygon(List<int> ids)
        {
            HashSet<int> given = new HashSet<int>(ids);
            Dictionary<int, int> next = new Dictionary<int, int>();
            HashSet<int> regionNodes = new HashSet<int>();
            foreach (var id in ids)
            {
                int[] nodes = _elements[id];
                for (int c = 0; c < nodes.Length; c++)
                {
                    int a = nodes[c];
                    int b = nodes[(c + 1) % nodes.Length];
                    regionNodes.Add(a);
                    int neighbour = GetNeighbour(id, a, b);
                    if (neighbour >= 0 && given.Contains(neighbour)) continue;
                    if (next.ContainsKey(a)) return null;
                    next.Add(a, b);
                }
            }
            int start = int.MaxValue;
            foreach (var node in next.Keys) start = Math.Min(start, node);
            List<int> polygon = new List<int>();
            int current = start;
            do
            {
                polygon.Add(current);
                if (polygon.Count > next.Count || !next.TryGetValue(current, out current)) return null;
            }
            while (current != start);
            if (polygon.Count != next.Count) return null;
            foreach (var node in regionNodes)
            {
                if (_fixed[node] && !next.ContainsKey(node)) return null;
            }
            return polygon;
        }
        /// <summary>
        /// Every way to split the polygon into quads by its diagonals with no corner that folds or is straight: the
        /// quad on the edge from its first node to its second, and the smaller polygons beside it split in turn. A
        /// diagonal must not cross an edge of the polygon or join two nodes an edge outside the region joins already.
        /// </summary>
        private List<List<int[]>> GetQuadrangulations(List<int> polygon, HashSet<long> innerEdges)
        {
            List<List<int[]>> result = new List<List<int[]>>();
            int n = polygon.Count;
            if (n == 4)
            {
                int[] quad = polygon.ToArray();
                if (HasOpenFixedCorners(quad, null)) result.Add(new List<int[]> { quad });
                return result;
            }
            for (int a = 2; a < n; a += 2)
            {
                for (int b = a + 1; b < n; b += 2)
                {
                    int[] quad = new int[] { polygon[0], polygon[1], polygon[a], polygon[b] };
                    if (!HasOpenFixedCorners(quad, null)) continue;
                    if (a > 2 && !IsDiagonal(polygon, 1, a, innerEdges) ||
                        b > a + 1 && !IsDiagonal(polygon, a, b, innerEdges) ||
                        b < n - 1 && !IsDiagonal(polygon, b, 0, innerEdges)) continue;
                    List<List<int[]>> combined = new List<List<int[]>> { new List<int[]> { quad } };
                    if (a > 2) combined = Combine(combined, GetQuadrangulations(polygon.GetRange(1, a), innerEdges));
                    if (b > a + 1)
                        combined = Combine(combined, GetQuadrangulations(polygon.GetRange(a, b - a + 1), innerEdges));
                    if (b < n - 1)
                    {
                        List<int> rest = polygon.GetRange(b, n - b);
                        rest.Add(polygon[0]);
                        combined = Combine(combined, GetQuadrangulations(rest, innerEdges));
                    }
                    result.AddRange(combined);
                }
            }
            return result;
        }
        /// <summary>
        /// Whether every corner of the element at a fixed node opens to at least MinFixedCorner: a fixed node cannot be
        /// moved, so a quad that is straight or folds there cannot be smoothed into shape. The node -1 lies at the
        /// given position.
        /// </summary>
        private bool HasOpenFixedCorners(int[] nodes, double[] position)
        {
            for (int c = 0; c < nodes.Length; c++)
            {
                if (nodes[c] < 0 || !_fixed[nodes[c]]) continue;
                if (!(GetCorner(nodes, c, position) >= MinFixedCorner)) return false;
            }
            return true;
        }
        private static List<List<int[]>> Combine(List<List<int[]>> first, List<List<int[]>> second)
        {
            List<List<int[]>> combined = new List<List<int[]>>();
            foreach (var a in first)
            {
                foreach (var b in second)
                {
                    List<int[]> quads = new List<int[]>(a);
                    quads.AddRange(b);
                    combined.Add(quads);
                }
            }
            return combined;
        }
        /// <summary>
        /// Whether the nodes i and j of the polygon can be joined inside it: no edge outside the region joins them and
        /// the segment crosses no edge of the polygon.
        /// </summary>
        private bool IsDiagonal(List<int> polygon, int i, int j, HashSet<long> innerEdges)
        {
            int p = polygon[i];
            int q = polygon[j];
            if (EdgeExists(p, q) && !innerEdges.Contains(GetEdgeKey(p, q))) return false;
            int n = polygon.Count;
            for (int k = 0; k < n; k++)
            {
                int a = polygon[k];
                int b = polygon[(k + 1) % n];
                if (a == p || a == q || b == p || b == q) continue;
                if (SegmentsCross(_points[p], _points[q], _points[a], _points[b])) return false;
            }
            return true;
        }
        private static bool SegmentsCross(double[] p, double[] q, double[] a, double[] b)
        {
            double d1 = Cross(p, q, a);
            double d2 = Cross(p, q, b);
            double d3 = Cross(a, b, p);
            double d4 = Cross(a, b, q);
            return d1 * d2 <= 0 && d3 * d4 <= 0;
        }
        private static double Cross(double[] o, double[] a, double[] b)
        {
            return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]);
        }
        /// <summary>
        /// The worst corner of an element whose node -1 lies at the given position.
        /// </summary>
        private double GetMinCorner(int[] nodes, double[] position)
        {
            double min = double.MaxValue;
            for (int c = 0; c < nodes.Length; c++) min = Math.Min(min, GetCorner(nodes, c, position));
            return min;
        }
        private double GetCorner(int[] nodes, int c, double[] position)
        {
            if (position == null) return QuadSmoothing.GetElementScaledJacobian(_points, nodes, c, _orientation);
            double[][] points = new double[nodes.Length][];
            int[] local = new int[nodes.Length];
            for (int k = 0; k < nodes.Length; k++)
            {
                points[k] = nodes[k] < 0 ? position : _points[nodes[k]];
                local[k] = k;
            }
            return QuadSmoothing.GetElementScaledJacobian(points, local, c, _orientation);
        }
        /// <summary>
        /// Turns two triangles and the path of quads between them into quads only by splitting the path along its
        /// length: a new node in the middle of every edge the path crosses, so that each triangle gains a node on an
        /// edge and becomes a quad, and each quad of the path is split in two between its two new nodes, or, where the
        /// path turns, along its diagonal through the corner it turns round, each half gaining a node. The new nodes
        /// are smoothed with two rings of elements around the path, three when that is not enough, and the split is
        /// kept under the rule of the pairs.
        /// </summary>
        private bool SplitPath(List<int> path)
        {
            if (path.Count < 3) return false;
            List<int> pathNodes = new List<int>();
            foreach (var id in path) pathNodes.AddRange(_elements[id]);
            for (int numOfRings = 2; numOfRings <= 3; numOfRings++)
            {
                double before = GetRegionQuality(pathNodes, numOfRings);
                double beforeShape = GetRegionShape(pathNodes, numOfRings);
                Begin();
                List<int> seedNodes = new List<int>(pathNodes);
                if (!SplitPathElements(path, seedNodes))
                {
                    Rollback();
                    return false;
                }
                if (FinishShaped(seedNodes, before, beforeShape, numOfRings)) return true;
            }
            return false;
        }
        /// <summary>
        /// Pairs two triangles that share an edge and have no other way round to each other, as two on a straight
        /// boundary at the end of a strip one element wide: the second is carried one quad on, into each quad beside it
        /// and to each element beyond that quad with either split in turn, and the path from the first through the quad
        /// the step leaves to the carried triangle is split, with two rings of elements smoothed.
        /// </summary>
        private bool CarryAndSplit(int first, int second)
        {
            List<int> quadIds = GetAdjacentElements(new List<int> { second });
            foreach (var quadId in quadIds)
            {
                if (quadId == first || _elements[quadId] == null || _elements[quadId].Length != 4) continue;
                foreach (var nextId in GetAdjacentElements(new List<int> { quadId }))
                {
                    if (nextId == second) continue;
                    for (int split = 0; split < 2; split++)
                    {
                        List<int> seedNodes = new List<int>(_elements[first]);
                        seedNodes.AddRange(_elements[second]);
                        seedNodes.AddRange(_elements[quadId]);
                        double before = GetRegionQuality(seedNodes, 2);
                        double beforeShape = GetRegionShape(seedNodes, 2);
                        Begin();
                        int carried = StepTriangle(second, quadId, nextId, split);
                        List<int> path = new List<int> { first, carried + 1, carried };
                        if (carried < 0 || !AreAdjacent(first, carried + 1) || !AreAdjacent(carried + 1, carried) ||
                            AreAdjacent(first, carried) || !SplitPathElements(path, seedNodes))
                        {
                            Rollback();
                            continue;
                        }
                        if (FinishShaped(seedNodes, before, beforeShape, 2)) return true;
                    }
                }
            }
            return false;
        }
        private bool AreAdjacent(int firstId, int secondId)
        {
            int[] nodes = _elements[firstId];
            for (int c = 0; c < nodes.Length; c++)
            {
                if (GetNeighbour(firstId, nodes[c], nodes[(c + 1) % nodes.Length]) == secondId) return true;
            }
            return false;
        }
        /// <summary>
        /// Splits the path within the operation begun, the new nodes added to the seed nodes; false when the elements
        /// of the path do not follow each other across one edge each.
        /// </summary>
        private bool SplitPathElements(List<int> path, List<int> seedNodes)
        {
            Dictionary<long, int> middles = new Dictionary<long, int>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                int[] nodes = _elements[path[i]];
                for (int c = 0; c < nodes.Length; c++)
                {
                    int a = nodes[c];
                    int b = nodes[(c + 1) % nodes.Length];
                    if (GetNeighbour(path[i], a, b) != path[i + 1]) continue;
                    int middle = AddNode(0.5 * (_points[a][0] + _points[b][0]), 0.5 * (_points[a][1] + _points[b][1]));
                    middles.Add(GetEdgeKey(a, b), middle);
                    seedNodes.Add(middle);
                }
            }
            if (middles.Count != path.Count - 1) return false;
            List<int[]> created = new List<int[]>();
            foreach (var id in path)
            {
                int[] nodes = _elements[id];
                List<int> cycle = new List<int>();
                List<int> middlePositions = new List<int>();
                for (int c = 0; c < nodes.Length; c++)
                {
                    cycle.Add(nodes[c]);
                    int middle;
                    if (!middles.TryGetValue(GetEdgeKey(nodes[c], nodes[(c + 1) % nodes.Length]), out middle)) continue;
                    middlePositions.Add(cycle.Count);
                    cycle.Add(middle);
                }
                if (cycle.Count == 4) created.Add(cycle.ToArray());
                else
                {
                    int[] hexagon = cycle.ToArray();
                    if (middlePositions[1] - middlePositions[0] == 3)
                    {
                        // Between the two new nodes
                        hexagon = Rotate(hexagon, middlePositions[0]);
                        created.Add(new int[] { hexagon[0], hexagon[1], hexagon[2], hexagon[3] });
                        created.Add(new int[] { hexagon[3], hexagon[4], hexagon[5], hexagon[0] });
                    }
                    else
                    {
                        // Where the path turns, three quads around a new node joined to both new nodes and to the
                        // corner across from the one the path turns round
                        hexagon = Rotate(hexagon, middlePositions[1] - middlePositions[0] == 2 ? middlePositions[0] :
                                                                                                 middlePositions[1]);
                        double x = 0;
                        double y = 0;
                        foreach (var node in hexagon)
                        {
                            x += _points[node][0] / 6;
                            y += _points[node][1] / 6;
                        }
                        int centre = AddNode(x, y);
                        seedNodes.Add(centre);
                        for (int k = 0; k < 6; k += 2)
                            created.Add(new int[] { centre, hexagon[k], hexagon[k + 1], hexagon[(k + 2) % 6] });
                    }
                }
            }
            foreach (var id in path) RemoveElement(id);
            foreach (var element in created) AddElement(element);
            return true;
        }
        /// <summary>
        /// Pairs two triangles that sit on the boundary by an edge with one quad between them, which the walk leaves
        /// apart: one of them is carried through the quad to the other, which stays. Around a node of the boundary
        /// where both meet, the two and the quad become two quads. Returns whether a pair was made.
        /// </summary>
        private bool PairBoundaryTriangles()
        {
            bool paired = false;
            for (int id = 0; id < _elements.Count; id++)
            {
                if (_elements[id] == null || _elements[id].Length != 3 || !IsOnBoundary(id)) continue;
                bool done = false;
                for (int c = 0; c < 3 && !done; c++)
                {
                    int[] triangle = _elements[id];
                    int quadId = GetNeighbour(id, triangle[c], triangle[(c + 1) % 3]);
                    if (quadId < 0 || _elements[quadId].Length != 4) continue;
                    for (int q = 0; q < 4 && !done; q++)
                    {
                        int[] quad = _elements[quadId];
                        int otherId = GetNeighbour(quadId, quad[q], quad[(q + 1) % 4]);
                        if (otherId < 0 || otherId == id || _elements[otherId].Length != 3 || !IsOnBoundary(otherId))
                            continue;
                        for (int option = 0; option < 2 && !done; option++)
                        {
                            List<int> path = option == 0 ? new List<int> { otherId, quadId, id } :
                                                           new List<int> { id, quadId, otherId };
                            List<int> seedNodes = new List<int>();
                            foreach (var pathId in path) seedNodes.AddRange(_elements[pathId]);
                            done = MoveTriangleByChoices(path, seedNodes, GetRegionQuality(seedNodes, 1), 1);
                        }
                        if (!done) done = SplitHexagon(id, quadId, otherId);
                    }
                }
                if (done)
                {
                    _numOfPairs++;
                    paired = true;
                }
            }
            return paired;
        }
        /// <summary>
        /// Turns two triangles and the quad between them, which together bound a hexagon, into three quads around a new
        /// node, joined to every second corner of the hexagon: the pair that cannot be merged when two corners of the
        /// hexagon lie on a straight boundary, each needing an edge inside. Returns whether it was kept.
        /// </summary>
        private bool SplitHexagon(int firstId, int quadId, int secondId)
        {
            // The edges of the three elements that none of the others has, chained into the hexagon
            Dictionary<int, int> next = new Dictionary<int, int>();
            HashSet<long> directedEdges = new HashSet<long>();
            int[] ids = new int[] { firstId, quadId, secondId };
            foreach (var id in ids)
            {
                int[] nodes = _elements[id];
                for (int c = 0; c < nodes.Length; c++)
                    directedEdges.Add(((long)nodes[c] << 32) | (uint)nodes[(c + 1) % nodes.Length]);
            }
            foreach (var id in ids)
            {
                int[] nodes = _elements[id];
                for (int c = 0; c < nodes.Length; c++)
                {
                    int a = nodes[c];
                    int b = nodes[(c + 1) % nodes.Length];
                    if (directedEdges.Contains(((long)b << 32) | (uint)a)) continue;
                    if (next.ContainsKey(a)) return false;
                    next.Add(a, b);
                }
            }
            if (next.Count != 6) return false;
            int[] hexagon = new int[6];
            hexagon[0] = _elements[firstId][0];
            for (int i = 1; i < 6; i++)
            {
                if (!next.TryGetValue(hexagon[i - 1], out hexagon[i])) return false;
            }
            if (next[hexagon[5]] != hexagon[0]) return false;
            double x = 0;
            double y = 0;
            foreach (var node in hexagon)
            {
                x += _points[node][0] / 6;
                y += _points[node][1] / 6;
            }
            List<int> seedNodes = new List<int>(hexagon);
            double before = GetRegionQuality(seedNodes, 1);
            double beforeShape = GetRegionShape(seedNodes, 1);
            for (int offset = 0; offset < 2; offset++)
            {
                Begin();
                int center = AddNode(x, y);
                foreach (var id in ids) RemoveElement(id);
                for (int k = offset; k < 6; k += 2)
                    AddElement(new int[] { center, hexagon[k], hexagon[(k + 1) % 6], hexagon[(k + 2) % 6] });
                seedNodes.Add(center);
                if (FinishShaped(seedNodes, before, beforeShape, 1)) return true;
                seedNodes.Remove(center);
            }
            return false;
        }
        /// <summary>
        /// Moves the triangles that sit on the boundary by an edge off it while another triangle is left to pair them
        /// with: such a triangle cannot be carried off a straight boundary, and two of them cannot be merged there. The
        /// triangle (b0, b1, a) on the boundary edge b0 b1, the quad (a, b1, b2, c) beside it and the quad (a, c, e, f)
        /// above that one become the quads (b1, b2, c, e) and (b0, b1, e, a) and the triangle (a, e, f), which lies
        /// inside; or the same mirrored to the other side. Returns whether a triangle was moved.
        /// </summary>
        private bool LiftBoundaryTriangles()
        {
            List<int> triangles = new List<int>();
            for (int id = 0; id < _elements.Count; id++)
            {
                if (_elements[id] != null && _elements[id].Length == 3 && IsOnBoundary(id)) triangles.Add(id);
            }
            if (triangles.Count == 0 || CountTriangles() < 2) return false;
            bool lifted = false;
            foreach (var id in triangles)
            {
                if (_elements[id] == null) continue;
                for (int side = 0; side < 2; side++)
                {
                    if (TryLift(id, side == 1))
                    {
                        lifted = true;
                        break;
                    }
                }
            }
            return lifted;
        }
        private bool TryLift(int id, bool mirrored)
        {
            int[] triangle = _elements[id];
            int c0 = -1;
            int numOfBoundaryEdges = 0;
            for (int k = 0; k < 3; k++)
            {
                if (GetNeighbour(id, triangle[k], triangle[(k + 1) % 3]) < 0)
                {
                    c0 = k;
                    numOfBoundaryEdges++;
                }
            }
            if (numOfBoundaryEdges != 1) return false;
            int b0 = triangle[c0];
            int b1 = triangle[(c0 + 1) % 3];
            int a = triangle[(c0 + 2) % 3];
            // Mirrored, the quad beside is across the edge a b0 and every element is read the other way round
            int sideId = mirrored ? GetNeighbour(id, a, b0) : GetNeighbour(id, b1, a);
            if (sideId < 0 || _elements[sideId].Length != 4) return false;
            int[] side = Rotate(_elements[sideId], IndexOf(_elements[sideId], a));
            if (mirrored) side = new int[] { side[0], side[3], side[2], side[1] };
            if (side[1] != (mirrored ? b0 : b1)) return false;
            int b2 = side[2];
            int c = side[3];
            int upperId = GetNeighbour(sideId, c, a);
            if (upperId < 0 || _elements[upperId].Length != 4) return false;
            int[] upper = Rotate(_elements[upperId], IndexOf(_elements[upperId], a));
            if (mirrored) upper = new int[] { upper[0], upper[3], upper[2], upper[1] };
            if (upper[1] != c) return false;
            int e = upper[2];
            int f = upper[3];
            int near = mirrored ? b1 : b0;
            int far = mirrored ? b0 : b1;
            int[] nodes = new int[] { near, far, b2, c, e, f, a };
            for (int i = 0; i < nodes.Length; i++)
            {
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    if (nodes[i] == nodes[j]) return false;
                }
            }
            if (EdgeExists(far, e) || EdgeExists(a, e)) return false;
            int[][] created = new int[][] { new int[] { far, b2, c, e }, new int[] { near, far, e, a },
                                            new int[] { a, e, f } };
            if (mirrored)
            {
                for (int k = 0; k < created.Length; k++) Array.Reverse(created[k]);
            }
            List<int> seedNodes = new List<int>(nodes);
            double before = GetRegionQuality(seedNodes, 2);
            Begin();
            RemoveElement(id);
            RemoveElement(sideId);
            RemoveElement(upperId);
            foreach (var element in created) AddElement(element);
            return Finish(seedNodes, before, 2);
        }
        /// <summary>
        /// Whether the element has an edge on the boundary. A triangle there cannot be carried off a straight boundary,
        /// whose node would be left with a corner of 180 degrees, and two such triangles cannot be merged there.
        /// </summary>
        private bool IsOnBoundary(int id)
        {
            int[] nodes = _elements[id];
            for (int c = 0; c < nodes.Length; c++)
            {
                if (GetNeighbour(id, nodes[c], nodes[(c + 1) % nodes.Length]) < 0) return true;
            }
            return false;
        }
        /// <summary>
        /// The path through the quads from the triangle to the nearest other triangle it has not failed to pair with,
        /// both included; null for none. Backwards, the edges of every element are taken in the opposite order.
        /// </summary>
        private List<int> FindPath(int start, HashSet<long> failedPairs, bool backwards)
        {
            bool startOnBoundary = IsOnBoundary(start);
            int stamp = NextStamp(_elements.Count);
            while (_parents.Count < _visitStamps.Count) _parents.Add(-1);
            List<int> queue = _queue;
            queue.Clear();
            queue.Add(start);
            _visitStamps[start] = stamp;
            for (int head = 0; head < queue.Count; head++)
            {
                int id = queue[head];
                int[] nodes = _elements[id];
                int n = nodes.Length;
                for (int c = 0; c < n; c++)
                {
                    int e = backwards ? n - 1 - c : c;
                    int neighbour = GetNeighbour(id, nodes[e], nodes[(e + 1) % n]);
                    if (neighbour < 0 || _visitStamps[neighbour] == stamp) continue;
                    _visitStamps[neighbour] = stamp;
                    if (_elements[neighbour].Length == 3)
                    {
                        if (failedPairs.Contains(((long)start << 32) | (uint)neighbour)) continue;
                        if (startOnBoundary && !_pairBoundaryTriangles && IsOnBoundary(neighbour)) continue;
                        List<int> path = new List<int>();
                        path.Add(neighbour);
                        int current = id;
                        while (current != start)
                        {
                            path.Add(current);
                            current = _parents[current];
                        }
                        path.Add(start);
                        path.Reverse();
                        return path;
                    }
                    _parents[neighbour] = id;
                    queue.Add(neighbour);
                }
            }
            return null;
        }
        /// <summary>
        /// Carries the first triangle of the path through its quads to the last one and merges the two into a quad. Bit
        /// i of the choices is the split taken at step i, -1 for the better split at every step.
        /// </summary>
        private bool MoveTriangle(List<int> path, int choices)
        {
            int triangle = path[0];
            for (int i = 1; i < path.Count - 1; i++)
            {
                triangle = StepTriangle(triangle, path[i], path[i + 1], choices < 0 ? -1 : (choices >> (i - 1)) & 1);
                if (triangle < 0) return false;
                // A long path is kept in shape step by step: the pentagon is smoothed when it is poor
                int[] newTriangle = _elements[triangle];
                int[] newQuad = _elements[triangle + 1];
                if (Math.Min(GetMinCorner(newTriangle), GetMinCorner(newQuad)) < _floor)
                {
                    List<int> pentagonNodes = new List<int>(newQuad);
                    foreach (var node in newTriangle)
                    {
                        if (!pentagonNodes.Contains(node)) pentagonNodes.Add(node);
                    }
                    SmoothNodes(pentagonNodes, NumOfLocalSweeps);
                }
            }
            return MergeTriangles(triangle, path[path.Count - 1]);
        }
        /// <summary>
        /// Carries the triangle along a path of at most MaxNumOfChosenSteps quads with every choice of the splits in turn,
        /// for a path whose better splits were not kept: a split that looks worse at its step may be the one that lets
        /// the last merge be good. Returns whether a choice was kept.
        /// </summary>
        private bool MoveTriangleByChoices(List<int> path, List<int> seedNodes, double before, int numOfRings)
        {
            int numOfSteps = path.Count - 2;
            if (numOfSteps < 1 || numOfSteps > MaxNumOfChosenSteps) return false;
            for (int choices = 0; choices < 1 << numOfSteps; choices++)
            {
                Begin();
                if (MoveTriangle(path, choices) && Finish(seedNodes, before, numOfRings)) return true;
                Rollback();
            }
            return false;
        }
        /// <summary>
        /// Splits the pentagon of the triangle and the quad next to it into a quad and a triangle at the next element,
        /// the split of the better worst corner or the given one; returns the id of the new triangle, -1 when it cannot
        /// be made.
        /// </summary>
        private int StepTriangle(int triangleId, int quadId, int nextId, int split)
        {
            bool nextIsTriangle = _elements[nextId].Length == 3;
            int[] triangle = _elements[triangleId];
            int[] quad = _elements[quadId];
            int j = -1;
            int numOfShared = 0;
            for (int c = 0; c < 4; c++)
            {
                if (GetNeighbour(quadId, quad[c], quad[(c + 1) % 4]) == triangleId)
                {
                    j = c;
                    numOfShared++;
                }
            }
            if (numOfShared != 1) return -1;
            int apex = -1;
            foreach (var node in triangle)
            {
                if (node != quad[j] && node != quad[(j + 1) % 4]) apex = node;
            }
            foreach (var node in quad)
            {
                if (node == apex) return -1;
            }
            int[] pentagon = new int[] { quad[(j + 1) % 4], quad[(j + 2) % 4], quad[(j + 3) % 4], quad[j], apex };
            int k = -1;
            for (int c = 0; c < 3; c++)
            {
                if (GetNeighbour(quadId, pentagon[c], pentagon[c + 1]) == nextId) k = c;
            }
            if (k < 0) return -1;
            // The triangle on the edge to the next element, its free corner before or after the edge
            int[][] triangles = new int[2][];
            int[][] quads = new int[2][];
            double[] qualities = new double[] { double.NegativeInfinity, double.NegativeInfinity };
            for (int option = 0; option < 2; option++)
            {
                int s = option == 0 ? k + 4 : k;
                triangles[option] = new int[] { pentagon[s % 5], pentagon[(s + 1) % 5], pentagon[(s + 2) % 5] };
                quads[option] = new int[] { pentagon[(s + 2) % 5], pentagon[(s + 3) % 5], pentagon[(s + 4) % 5],
                                            pentagon[s % 5] };
                if (EdgeExists(pentagon[s % 5], pentagon[(s + 2) % 5])) continue;
                // Next to the other triangle, the quad the two merge into counts instead of the triangle
                int[] merged = nextIsTriangle ? GetMergedQuad(triangles[option], _elements[nextId]) : null;
                if (nextIsTriangle && merged == null) continue;
                qualities[option] = Math.Min(GetMinCorner(nextIsTriangle ? merged : triangles[option]),
                                             GetMinCorner(quads[option]));
            }
            int best = split >= 0 ? split : qualities[1] > qualities[0] ? 1 : 0;
            if (double.IsNegativeInfinity(qualities[best])) return -1;
            RemoveElement(triangleId);
            RemoveElement(quadId);
            int newTriangle = AddElement(triangles[best]);
            AddElement(quads[best]);
            return newTriangle;
        }
        /// <summary>
        /// The quad two triangles that share an edge merge into, null when they share none or have the same third node.
        /// </summary>
        private static int[] GetMergedQuad(int[] first, int[] second)
        {
            for (int c = 0; c < 3; c++)
            {
                int u = first[c];
                int v = first[(c + 1) % 3];
                int k = IndexOf(second, v);
                if (k < 0 || second[(k + 1) % 3] != u) continue;
                int x = first[(c + 2) % 3];
                int y = second[(k + 2) % 3];
                if (y == x) return null;
                return new int[] { v, x, u, y };
            }
            return null;
        }
        private bool MergeTriangles(int firstId, int secondId)
        {
            int[] first = _elements[firstId];
            int[] second = _elements[secondId];
            for (int c = 0; c < 3; c++)
            {
                int u = first[c];
                int v = first[(c + 1) % 3];
                if (GetNeighbour(firstId, u, v) != secondId) continue;
                int x = first[(c + 2) % 3];
                int y = -1;
                foreach (var node in second)
                {
                    if (node != u && node != v) y = node;
                }
                if (y < 0 || y == x) return false;
                RemoveElement(firstId);
                RemoveElement(secondId);
                AddElement(new int[] { v, x, u, y });
                return true;
            }
            return false;
        }
        /// <summary>
        /// Removes doublets, collapses quads and rotates edges, pass after pass, until a pass changes nothing.
        /// </summary>
        private void ImproveTopology()
        {
            for (int pass = 0; pass < MaxNumOfTopologyPasses; pass++)
            {
                bool changed = false;
                for (int v = 0; v < _points.Length; v++)
                {
                    if (TryRemoveDoublet(v)) changed = true;
                }
                int numOfElements = _elements.Count;
                for (int id = 0; id < numOfElements; id++)
                {
                    if (TryCollapse(id)) changed = true;
                }
                numOfElements = _elements.Count;
                for (int id = 0; id < numOfElements; id++)
                {
                    if (_elements[id] == null || _elements[id].Length != 4) continue;
                    for (int c = 0; c < 4; c++)
                    {
                        if (_elements[id] == null) break;
                        if (TryRotate(id, c)) changed = true;
                    }
                }
                if (!changed) break;
            }
        }
        private static int[] Rotate(int[] nodes, int first)
        {
            int n = nodes.Length;
            int[] rotated = new int[n];
            for (int c = 0; c < n; c++) rotated[c] = nodes[(first + c) % n];
            return rotated;
        }
        private static int IndexOf(int[] nodes, int node)
        {
            for (int c = 0; c < nodes.Length; c++)
            {
                if (nodes[c] == node) return c;
            }
            return -1;
        }
        private bool TryRemoveDoublet(int v)
        {
            if (_fixed[v] || _boundary[v] || _nodeElements[v].Count != 2) return false;
            int firstId = Math.Min(_nodeElements[v][0], _nodeElements[v][1]);
            int secondId = Math.Max(_nodeElements[v][0], _nodeElements[v][1]);
            if (_elements[firstId].Length != 4 || _elements[secondId].Length != 4) return false;
            int[] first = Rotate(_elements[firstId], IndexOf(_elements[firstId], v));
            int[] second = Rotate(_elements[secondId], IndexOf(_elements[secondId], v));
            int a = first[1];
            int x = first[2];
            int b = first[3];
            if (second[1] != b || second[3] != a) return false;
            int y = second[2];
            if (x == y) return false;
            List<int> seedNodes = new List<int>() { v, a, x, b, y };
            double before = GetRegionQuality(seedNodes, 1);
            Begin();
            RemoveElement(firstId);
            RemoveElement(secondId);
            AddElement(new int[] { a, x, b, y });
            if (!Finish(seedNodes, before, 1)) return false;
            _numOfDoublets++;
            return true;
        }
        private bool TryCollapse(int id)
        {
            for (int d = 0; d < 2; d++)
            {
                int[] quad = _elements[id];
                if (quad == null || quad.Length != 4) return false;
                int a = quad[d];
                int b = quad[d + 1];
                int c = quad[d + 2];
                int e = quad[(d + 3) % 4];
                if (_fixed[a] || _fixed[c] || _boundary[a] || _boundary[c]) continue;
                int valenceA = GetValence(a);
                int valenceC = GetValence(c);
                if (valenceA != 3 || valenceC != 3) continue;
                if (!HaveOnlyCommonNeighbours(a, c, b, e)) continue;
                // All the elements of both nodes must be quads, and none but this one joins them
                bool valid = true;
                foreach (var other in _nodeElements[c])
                {
                    if (other != id && IndexOf(_elements[other], a) >= 0) valid = false;
                }
                if (!valid) continue;
                int before = GetIrregularity(a, valenceA) + GetIrregularity(c, valenceC) +
                             GetIrregularity(b, GetValence(b)) + GetIrregularity(e, GetValence(e));
                int after = GetIrregularity(a, valenceA + valenceC - 2) + GetIrregularity(b, GetValence(b) - 1) +
                            GetIrregularity(e, GetValence(e) - 1);
                if (after > before) continue;
                if (GetValence(b) - 1 < 3 && !_boundary[b] || GetValence(e) - 1 < 3 && !_boundary[e]) continue;
                if (_nodeElements[b].Count < 2 || _nodeElements[e].Count < 2) continue;
                List<int> seedNodes = new List<int>() { a, b, c, e };
                double quality = GetRegionQuality(seedNodes, 1);
                Begin();
                SaveNode(a);
                List<int> others = new List<int>(_nodeElements[c]);
                others.Sort();
                RemoveElement(id);
                foreach (var other in others)
                {
                    if (other == id) continue;
                    int[] nodes = (int[])_elements[other].Clone();
                    nodes[IndexOf(nodes, c)] = a;
                    RemoveElement(other);
                    AddElement(nodes);
                }
                _points[a][0] = 0.5 * (_points[a][0] + _points[c][0]);
                _points[a][1] = 0.5 * (_points[a][1] + _points[c][1]);
                if (Finish(seedNodes, quality, 1))
                {
                    _numOfCollapses++;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Whether the only nodes both nodes share an edge with are the two given.
        /// </summary>
        private bool HaveOnlyCommonNeighbours(int a, int c, int b, int e)
        {
            List<int> neighboursA = GetNeighbourNodes(a);
            foreach (var node in GetNeighbourNodes(c))
            {
                if (node != b && node != e && neighboursA.Contains(node)) return false;
            }
            return true;
        }
        private List<int> GetNeighbourNodes(int node)
        {
            List<int> neighbours = new List<int>();
            foreach (var id in _nodeElements[node])
            {
                int[] nodes = _elements[id];
                int c = IndexOf(nodes, node);
                int next = nodes[(c + 1) % nodes.Length];
                int previous = nodes[(c + nodes.Length - 1) % nodes.Length];
                if (!neighbours.Contains(next)) neighbours.Add(next);
                if (!neighbours.Contains(previous)) neighbours.Add(previous);
            }
            return neighbours;
        }
        /// <summary>
        /// Rotates the edge after corner c of the quad, shared with another quad of a larger id, to the other diagonal
        /// of their hexagon that lowers the sum of the irregularities most.
        /// </summary>
        private bool TryRotate(int id, int c)
        {
            int[] quad = _elements[id];
            int u = quad[c];
            int v = quad[(c + 1) % 4];
            int otherId = GetNeighbour(id, u, v);
            if (otherId <= id || _elements[otherId].Length != 4) return false;
            int[] other = Rotate(_elements[otherId], IndexOf(_elements[otherId], v));
            if (other[1] != u) return false;
            int[] hexagon = new int[] { v, quad[(c + 2) % 4], quad[(c + 3) % 4], u, other[2], other[3] };
            for (int i = 0; i < 6; i++)
            {
                for (int j = i + 1; j < 6; j++)
                {
                    if (hexagon[i] == hexagon[j]) return false;
                }
            }
            int valenceU = GetValence(u);
            int valenceV = GetValence(v);
            int bestDelta = 0;
            int best = -1;
            for (int s = 1; s <= 2; s++)
            {
                int p = hexagon[s];
                int q = hexagon[s + 3];
                if (EdgeExists(p, q)) continue;
                if (valenceU - 1 < 3 && !_boundary[u] || valenceV - 1 < 3 && !_boundary[v]) continue;
                if (valenceU - 1 < 2 || valenceV - 1 < 2) continue;
                int delta = GetIrregularity(u, valenceU - 1) - GetIrregularity(u, valenceU) +
                            GetIrregularity(v, valenceV - 1) - GetIrregularity(v, valenceV) +
                            GetIrregularity(p, GetValence(p) + 1) - GetIrregularity(p, GetValence(p)) +
                            GetIrregularity(q, GetValence(q) + 1) - GetIrregularity(q, GetValence(q));
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = s;
                }
            }
            if (best < 0) return false;
            List<int> seedNodes = new List<int>(hexagon);
            double before = GetRegionQuality(seedNodes, 1);
            Begin();
            RemoveElement(id);
            RemoveElement(otherId);
            AddElement(new int[] { hexagon[best], hexagon[best + 1], hexagon[best + 2], hexagon[best + 3] });
            AddElement(new int[] { hexagon[best + 3], hexagon[(best + 4) % 6], hexagon[(best + 5) % 6], hexagon[best] });
            if (!Finish(seedNodes, before, 1)) return false;
            _numOfRotations++;
            return true;
        }
        /// <summary>
        /// Smooths the nodes the operations touched and the nodes of the elements below the floor, with their neighbours.
        /// </summary>
        private void SmoothFinal()
        {
            List<int> seedNodes = new List<int>();
            for (int v = 0; v < _points.Length; v++)
            {
                if (_touchedNodes[v] && _nodeElements[v].Count > 0) seedNodes.Add(v);
            }
            foreach (var element in _elements)
            {
                if (element == null || GetMinCorner(element) >= _floor) continue;
                seedNodes.AddRange(element);
            }
            if (seedNodes.Count == 0) return;
            List<int> regionNodes = GetRegionNodes(seedNodes, 1);
            List<int> freeNodes = new List<int>();
            foreach (var node in regionNodes)
            {
                if (!_fixed[node] && _nodeElements[node].Count > 0) freeNodes.Add(node);
            }
            if (freeNodes.Count == 0) return;
            List<int> elementIds = GetNodesElements(freeNodes);
            int[][] elements = new int[elementIds.Count][];
            for (int i = 0; i < elements.Length; i++) elements[i] = _elements[elementIds[i]];
            QuadSmoothing.SmoothRegion(_points, elements, freeNodes.ToArray(), _orientation, NumOfFinalSweeps, _floor);
        }
    }
}
