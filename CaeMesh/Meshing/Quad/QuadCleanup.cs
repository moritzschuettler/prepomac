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
    /// edge changes. No node is added.
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
    /// leave a corner of 180 degrees on a straight boundary, and two of them are not paired with each other; while one
    /// is left with another triangle to pair it with, it is moved off the boundary with the two quads beside and above
    /// it (LiftBoundaryTriangles). A pair is kept when, after its region is smoothed if it must be, no element folds and
    /// the worst corner of its region is not below the acceptance or below what it was, whichever is lower; otherwise
    /// it is undone, tried from the other end and, in the wider passes, along the path the walk finds with the edges
    /// taken the other way round, and the triangle then looks for another partner. The passes accept 0.5 first and
    /// then the floor, SubmapFaceDetector.MinScaledJacobian, each smoothing one ring of elements around the path and
    /// then two, and the floor also three.
    /// 2. The topology is improved by local operations that lower the sum of |valence - ideal| over the nodes, ideal
    /// being 4 inside and, on the boundary, one more than the number of quads the angle of the boundary asks for (one
    /// for every 90 degrees, at least one): an inner node joined to two quads only, a doublet, is removed and its quads
    /// merged; a quad whose diagonal joins two inner nodes of valence 3 is collapsed along that diagonal when that does
    /// not raise the sum; the edge between two quads is rotated to one of the two other diagonals of their hexagon when
    /// that lowers the sum. Every operation is kept under the rule of the pairs with the acceptance 0.5.
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
        private const double GoodQuality = 0.5;
        private readonly double[][] _points;
        private readonly bool[] _fixed;
        private readonly bool[] _boundary;
        private readonly int[] _idealValence;
        private readonly double _orientation;
        private readonly double _floor;
        private double _acceptance;
        private readonly List<int[]> _elements;
        private readonly List<int>[] _nodeElements;
        private readonly Dictionary<long, int[]> _edgeElements;
        private readonly bool[] _touchedNodes;
        private readonly List<int> _visitStamps;
        private readonly List<int> _parents;
        private readonly List<int> _queue;
        private int _stamp;
        // The operation being tried, which is undone when it is not kept
        private int _numOfElementsAtStart;
        private readonly List<int> _removedIds;
        private readonly List<int[]> _removedElements;
        private readonly List<int> _movedNodes;
        private readonly List<double[]> _oldPositions;
        //
        private int _numOfPairs;
        private int _numOfDoublets;
        private int _numOfCollapses;
        private int _numOfRotations;


        // Constructors                                                                                                             
        private QuadCleanup(double[][] points, int[][] elements, bool[] fixedNodes)
        {
            int numOfNodes = points.Length;
            _points = new double[numOfNodes][];
            for (int n = 0; n < numOfNodes; n++) _points[n] = new double[] { points[n][0], points[n][1] };
            _fixed = (bool[])fixedNodes.Clone();
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
            if (points == null || elements == null || fixedNodes == null || fixedNodes.Length != points.Length)
                throw new CaeException("The cleanup needs the positions of the nodes, the elements and which nodes are fixed.");
            Stopwatch watch = Stopwatch.StartNew();
            QuadCleanup cleanup = new QuadCleanup(points, elements, fixedNodes);
            int numOfTrianglesBefore = cleanup.CountTriangles();
            double minScaledJacobianBefore = QuadSmoothing.GetMinScaledJacobian(cleanup._points, cleanup.GetElements(),
                                                                                cleanup._orientation);
            int numOfIrregularBefore = cleanup.CountIrregularNodes();
            //
            if (numOfTrianglesBefore > 1)
            {
                // The pairs that keep their regions good first, then those down to the floor
                foreach (var acceptance in new double[] { GoodQuality, cleanup._floor })
                {
                    cleanup._acceptance = acceptance;
                    cleanup.PairTriangles(1);
                    if (cleanup.LiftBoundaryTriangles()) cleanup.PairTriangles(1);
                    cleanup.PairTriangles(2);
                    if (acceptance < GoodQuality) cleanup.PairTriangles(3);
                }
            }
            cleanup._acceptance = GoodQuality;
            cleanup.ImproveTopology();
            cleanup.SmoothFinal();
            //
            int[][] result = cleanup.GetElements();
            bool[] isNodeUsed = new bool[points.Length];
            foreach (var element in result)
            {
                foreach (var node in element) isNodeUsed[node] = true;
            }
            watch.Stop();
            return new QuadCleanupResult(result, cleanup._points, isNodeUsed, numOfTrianglesBefore, cleanup.CountTriangles(),
                                         QuadSmoothing.CountFoldedElements(cleanup._points, result, cleanup._orientation),
                                         minScaledJacobianBefore,
                                         QuadSmoothing.GetMinScaledJacobian(cleanup._points, result, cleanup._orientation),
                                         numOfIrregularBefore, cleanup.CountIrregularNodes(), cleanup._numOfPairs,
                                         cleanup._numOfDoublets, cleanup._numOfCollapses, cleanup._numOfRotations,
                                         watch.Elapsed.TotalSeconds);
        }
        private static long GetEdgeKey(int a, int b)
        {
            return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
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
        private void SetBoundary()
        {
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
                if (angle < 0) angle += 2 * Math.PI;
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
            if (!SmoothNodes(regionNodes, numOfRings * numOfRings * NumOfLocalSweeps)) return false;
            after = 1;
            foreach (var id in regionElementIds) after = Math.Min(after, GetMinCorner(_elements[id]));
            return after > 0 && after >= threshold;
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
                    // the other way round
                    bool paired = false;
                    for (int option = 0; option < (numOfRings > 1 ? 4 : 2) && !paired; option++)
                    {
                        List<int> optionPath = option < 2 ? new List<int>(path) : FindPath(first, failedPairs, true);
                        if (optionPath == null) break;
                        // The triangle on the boundary stays, the other one moves to it
                        if ((option % 2 == 1) != IsOnBoundary(optionPath[0])) optionPath.Reverse();
                        List<int> seedNodes = new List<int>();
                        foreach (var id in optionPath) seedNodes.AddRange(_elements[id]);
                        double before = GetRegionQuality(seedNodes, numOfRings);
                        Begin();
                        paired = MoveTriangle(optionPath) && Finish(seedNodes, before, numOfRings);
                        if (!paired) Rollback();
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
                        if (startOnBoundary && IsOnBoundary(neighbour)) continue;
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
        /// Carries the first triangle of the path through its quads to the last one and merges the two into a quad.
        /// </summary>
        private bool MoveTriangle(List<int> path)
        {
            int triangle = path[0];
            for (int i = 1; i < path.Count - 1; i++)
            {
                triangle = StepTriangle(triangle, path[i], path[i + 1]);
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
        /// Splits the pentagon of the triangle and the quad next to it into a quad and a triangle at the next element;
        /// returns the id of the new triangle, -1 when it cannot be made.
        /// </summary>
        private int StepTriangle(int triangleId, int quadId, int nextId)
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
            int best = qualities[1] > qualities[0] ? 1 : 0;
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
