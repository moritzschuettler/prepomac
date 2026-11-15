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
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// A face of a geometry part that is meshed by SubmapMapping: its one boundary loop as the vertices and the edges
    /// in the order the loop runs, every edge with the logical direction it runs in. Edge k runs from vertex k to
    /// vertex k + 1, and the directions turn left at an end, keep their way at a side and turn right at a corner, so
    /// that the logical loop is counter-clockwise.
    /// </summary>
    [PmxType(2148)]
    public class SubmapFace
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int _faceId;
        [PmxKey(1)] private int[] _vertexNodeIds;
        [PmxKey(2)] private int[] _edgeIds;
        [PmxKey(3)] private int[] _edgeDirections;
        [PmxKey(4)] private double[] _vertexAnglesDeg;
        private const double SeparationTolerance = 1E-3;


        // Properties                                                                                                               
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The vertex node ids of the loop: vertex k is where edge k - 1 ends and edge k starts.
        /// </summary>
        public int[] VertexNodeIds { get { return _vertexNodeIds; } }
        public int[] EdgeIds { get { return _edgeIds; } }
        /// <summary>
        /// The logical direction of every edge: 0, 1, 2 and 3 for +i, +j, -i and -j.
        /// </summary>
        public int[] EdgeDirections { get { return _edgeDirections; } }
        /// <summary>
        /// The interior angle of the face at every vertex.
        /// </summary>
        public double[] VertexAnglesDeg { get { return _vertexAnglesDeg; } }


        // Constructors                                                                                                             
        public SubmapFace(int faceId, int[] vertexNodeIds, int[] edgeIds, int[] edgeDirections, double[] vertexAnglesDeg)
        {
            _faceId = faceId;
            _vertexNodeIds = vertexNodeIds;
            _edgeIds = edgeIds;
            _edgeDirections = edgeDirections;
            _vertexAnglesDeg = vertexAnglesDeg;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The two pairs of sides whose numbers of elements must be equal for the logical outline to close: the edges
        /// along +i against those along -i, and the edges along +j against those along -j.
        /// </summary>
        public TransfiniteSidePair[] GetSidePairs()
        {
            TransfiniteSidePair[] pairs = new TransfiniteSidePair[2];
            for (int axis = 0; axis < 2; axis++)
            {
                List<int> forward = new List<int>();
                List<int> backward = new List<int>();
                for (int k = 0; k < _edgeIds.Length; k++)
                {
                    if (_edgeDirections[k] == axis) forward.Add(_edgeIds[k]);
                    else if (_edgeDirections[k] == axis + 2) backward.Add(_edgeIds[k]);
                }
                pairs[axis] = new TransfiniteSidePair(_faceId, forward.ToArray(), backward.ToArray());
            }
            return pairs;
        }
        /// <summary>
        /// The pairs of parallel sides that face each other across the face and must keep at least the bound of lattice
        /// cells between them, a side being a run of loop edges in one direction. Two sides are such a pair when, in the
        /// ideal outline the edge lengths draw (SubmapFaceDetector.GetIdealOutline), they lie apart and their extents
        /// along them overlap, both beyond a tolerance relative to the size of the outline. edgeLengths holds the length
        /// of every edge in loop order. The pairs do not prove the lattice outline simple; they keep the order of the
        /// sides the face has.
        /// </summary>
        public List<SubmapSeparation> GetSeparations(double[] edgeLengths, int bound)
        {
            List<SubmapSeparation> separations = new List<SubmapSeparation>();
            double[][] outline = SubmapFaceDetector.GetIdealOutline(_edgeDirections, edgeLengths);
            if (outline == null) return separations;
            //
            int n = _edgeIds.Length;
            List<int> sideStarts = new List<int>();
            for (int k = 0; k < n; k++)
            {
                if (_edgeDirections[k] != _edgeDirections[(k + n - 1) % n]) sideStarts.Add(k);
            }
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            foreach (var point in outline)
            {
                minX = Math.Min(minX, point[0]);
                maxX = Math.Max(maxX, point[0]);
                minY = Math.Min(minY, point[1]);
                maxY = Math.Max(maxY, point[1]);
            }
            double tolerance = SeparationTolerance * Math.Max(maxX - minX, maxY - minY);
            // Every side by its axis, its coordinate across and its extent along
            int numOfSides = sideStarts.Count;
            int[] axes = new int[numOfSides];
            double[] across = new double[numOfSides];
            double[] alongMin = new double[numOfSides];
            double[] alongMax = new double[numOfSides];
            for (int s = 0; s < numOfSides; s++)
            {
                int start = sideStarts[s];
                int end = sideStarts[(s + 1) % numOfSides];
                axes[s] = _edgeDirections[start] % 2;
                across[s] = outline[start][1 - axes[s]];
                alongMin[s] = Math.Min(outline[start][axes[s]], outline[end][axes[s]]);
                alongMax[s] = Math.Max(outline[start][axes[s]], outline[end][axes[s]]);
            }
            //
            for (int a = 0; a < numOfSides; a++)
            {
                for (int b = a + 1; b < numOfSides; b++)
                {
                    if (axes[a] != axes[b] || Math.Abs(across[b] - across[a]) <= tolerance) continue;
                    if (Math.Min(alongMax[a], alongMax[b]) - Math.Max(alongMin[a], alongMin[b]) <= tolerance) continue;
                    //
                    int first = across[a] < across[b] ? a : b;
                    int second = first == a ? b : a;
                    // The lattice coordinate across grows along +j for sides along i and along +i for sides along j
                    int towards = axes[a] == 0 ? 1 : 0;
                    List<int> towardsEdgeIds = new List<int>();
                    List<int> backEdgeIds = new List<int>();
                    for (int k = sideStarts[first]; k != sideStarts[second]; k = (k + 1) % n)
                    {
                        if (_edgeDirections[k] == towards) towardsEdgeIds.Add(_edgeIds[k]);
                        else if (_edgeDirections[k] == towards + 2) backEdgeIds.Add(_edgeIds[k]);
                    }
                    separations.Add(new SubmapSeparation(_faceId, towardsEdgeIds.ToArray(), backEdgeIds.ToArray(), bound,
                                                         _edgeIds[sideStarts[first]], _edgeIds[sideStarts[second]]));
                }
            }
            return separations;
        }
        /// <summary>
        /// The numbers of elements of the edges in loop order, read from the counts of the edges by their ids.
        /// </summary>
        public int[] GetEdgeCounts(Dictionary<int, int> edgeIdCount)
        {
            int[] counts = new int[_edgeIds.Length];
            for (int k = 0; k < _edgeIds.Length; k++) counts[k] = edgeIdCount[_edgeIds[k]];
            return counts;
        }
    }
}
