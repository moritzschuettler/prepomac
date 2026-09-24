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
    /// The 1D mesh nodes of one side of a sweep wall - a chain of meshed edges - in order from one end of the
    /// chain to the other, each with the fraction of the side's length it lies at. The length is measured
    /// along the curves, so that two sides compare by where their nodes really are, and a node can be moved to
    /// a given fraction of the side without leaving the edge it lies on.
    /// </summary>
    public class SweepSideNodes
    {
        // Variables
        private const int _numOfSamples = 512;
        //
        private int[] _edgeIds;                         // the edges in chain order
        private bool[] _edgeReversed;                   // the chain walks the edge against its parameter
        private double[] _edgeOffsets;                  // the length of the side before the edge
        private double[] _edgeLengths;
        private double[][] _edgeSampleParameters;
        private double[][] _edgeSampleLengths;
        private double _length;
        private List<IntPtr> _nodeTags;                 // IntPtr.Zero for the vertices of the chain
        private List<int> _nodeEdgeIndices;             // -1 for the vertices of the chain
        private List<double> _nodeFractions;
        private double[] _startPoint;
        private double[] _endPoint;


        // Properties
        public int NumOfNodes { get { return _nodeTags.Count; } }
        public double[] StartPoint { get { return _startPoint; } }
        public double[] EndPoint { get { return _endPoint; } }


        // Constructors
        private SweepSideNodes() { }


        // Methods
        /// <summary>
        /// The nodes of a chain of meshed edges. Null when the edges do not form one open chain of edges with
        /// two distinct ends each.
        /// </summary>
        public static SweepSideNodes Create(int[] edgeIds)
        {
            int[] vertexIds;
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            Dictionary<int, List<int>> vertexIdEdgeIds = new Dictionary<int, List<int>>();
            foreach (var edgeId in edgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                if (vertexIds.Length != 2 || vertexIds[0] == vertexIds[1]) return null;
                edgeIdVertexIds[edgeId] = vertexIds;
                foreach (var vertexId in vertexIds)
                {
                    if (!vertexIdEdgeIds.ContainsKey(vertexId)) vertexIdEdgeIds[vertexId] = new List<int>();
                    vertexIdEdgeIds[vertexId].Add(edgeId);
                }
            }
            int[] endVertexIds = vertexIdEdgeIds.Where(entry => entry.Value.Count == 1).Select(entry => entry.Key).ToArray();
            if (endVertexIds.Length != 2 || vertexIdEdgeIds.Values.Any(ids => ids.Count > 2)) return null;
            //
            SweepSideNodes side = new SweepSideNodes();
            int numOfEdges = edgeIds.Length;
            side._edgeIds = new int[numOfEdges];
            side._edgeReversed = new bool[numOfEdges];
            side._edgeOffsets = new double[numOfEdges];
            side._edgeLengths = new double[numOfEdges];
            side._edgeSampleParameters = new double[numOfEdges][];
            side._edgeSampleLengths = new double[numOfEdges][];
            side._nodeTags = new List<IntPtr>();
            side._nodeEdgeIndices = new List<int>();
            List<double> nodeLengths = new List<double>();
            //
            double[] vertex;
            double[] start;
            double[] end;
            double[] min;
            double[] max;
            double[] coor;
            double[] parameters;
            IntPtr[] tags;
            HashSet<int> usedEdgeIds = new HashSet<int>();
            int currentVertexId = endVertexIds[0];
            double offset = 0;
            for (int i = 0; i < numOfEdges; i++)
            {
                int edgeId = vertexIdEdgeIds[currentVertexId].FirstOrDefault(id => !usedEdgeIds.Contains(id));
                if (edgeId == 0) return null;
                usedEdgeIds.Add(edgeId);
                //
                Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
                Gmsh.Model.GetValue(1, edgeId, new double[] { min[0] }, out start);
                Gmsh.Model.GetValue(1, edgeId, new double[] { max[0] }, out end);
                Gmsh.Model.GetValue(0, currentVertexId, new double[0], out vertex);
                bool reversed = GetDistance(vertex, start) > GetDistance(vertex, end);
                // The length along the edge as a function of its parameter
                double[] sampleParameters = new double[_numOfSamples + 1];
                double[] sampleLengths = new double[_numOfSamples + 1];
                double[] previous = null;
                double[] point;
                for (int s = 0; s <= _numOfSamples; s++)
                {
                    sampleParameters[s] = min[0] + (max[0] - min[0]) * s / _numOfSamples;
                    Gmsh.Model.GetValue(1, edgeId, new double[] { sampleParameters[s] }, out point);
                    if (previous != null) sampleLengths[s] = sampleLengths[s - 1] + GetDistance(previous, point);
                    previous = point;
                }
                double edgeLength = sampleLengths[_numOfSamples];
                if (edgeLength <= 0) return null;
                //
                side._edgeIds[i] = edgeId;
                side._edgeReversed[i] = reversed;
                side._edgeOffsets[i] = offset;
                side._edgeLengths[i] = edgeLength;
                side._edgeSampleParameters[i] = sampleParameters;
                side._edgeSampleLengths[i] = sampleLengths;
                if (i == 0)
                {
                    side._startPoint = reversed ? end : start;
                    side._nodeTags.Add(IntPtr.Zero);
                    side._nodeEdgeIndices.Add(-1);
                    nodeLengths.Add(0);
                }
                // The interior nodes of the edge in the order the chain walks them
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 1, edgeId, false, false);
                parameters = new double[0];
                if (tags.Length > 0) Gmsh.Model.GetParametrization(1, edgeId, coor, out parameters);
                if (parameters.Length != tags.Length) return null;
                double[] alongLengths = parameters.Select(p => GetLength(sampleParameters, sampleLengths, p)).ToArray();
                if (reversed) alongLengths = alongLengths.Select(length => edgeLength - length).ToArray();
                foreach (var j in Enumerable.Range(0, tags.Length).OrderBy(j => alongLengths[j]))
                {
                    side._nodeTags.Add(tags[j]);
                    side._nodeEdgeIndices.Add(i);
                    nodeLengths.Add(offset + alongLengths[j]);
                }
                offset += edgeLength;
                side._nodeTags.Add(IntPtr.Zero);
                side._nodeEdgeIndices.Add(-1);
                nodeLengths.Add(offset);
                //
                currentVertexId = edgeIdVertexIds[edgeId][0] == currentVertexId ? edgeIdVertexIds[edgeId][1]
                                                                                 : edgeIdVertexIds[edgeId][0];
                if (i == numOfEdges - 1) side._endPoint = reversed ? start : end;
            }
            side._length = offset;
            side._nodeFractions = nodeLengths.Select(length => length / offset).ToList();
            return side;
        }
        /// <summary>
        /// The fraction of the side's length the node lies at.
        /// </summary>
        public double GetFraction(int nodeIndex)
        {
            return _nodeFractions[nodeIndex];
        }
        /// <summary>
        /// The edge the node lies inside, or 0 for a vertex of the chain.
        /// </summary>
        public int GetEdgeId(int nodeIndex)
        {
            int edgeIndex = _nodeEdgeIndices[nodeIndex];
            return edgeIndex < 0 ? 0 : _edgeIds[edgeIndex];
        }
        /// <summary>
        /// The fractions of the side's length at which the edge the node lies inside starts and ends.
        /// </summary>
        public void GetEdgeFractions(int nodeIndex, out double start, out double end)
        {
            int edgeIndex = _nodeEdgeIndices[nodeIndex];
            start = _edgeOffsets[edgeIndex] / _length;
            end = (_edgeOffsets[edgeIndex] + _edgeLengths[edgeIndex]) / _length;
        }
        /// <summary>
        /// Moves a node lying inside an edge to the given fraction of the side's length, on the curve.
        /// </summary>
        public void MoveNode(int nodeIndex, double fraction)
        {
            int edgeIndex = _nodeEdgeIndices[nodeIndex];
            if (edgeIndex < 0) return;
            //
            double length = fraction * _length - _edgeOffsets[edgeIndex];
            if (_edgeReversed[edgeIndex]) length = _edgeLengths[edgeIndex] - length;
            double parameter = GetParameter(_edgeSampleParameters[edgeIndex], _edgeSampleLengths[edgeIndex], length);
            double[] point;
            Gmsh.Model.GetValue(1, _edgeIds[edgeIndex], new double[] { parameter }, out point);
            Gmsh.Model.Mesh.SetNode(_nodeTags[nodeIndex], point, new double[] { parameter });
            _nodeFractions[nodeIndex] = fraction;
        }
        private static double GetLength(double[] sampleParameters, double[] sampleLengths, double parameter)
        {
            int s = 1;
            while (s < sampleParameters.Length - 1 && sampleParameters[s] < parameter) s++;
            double span = sampleParameters[s] - sampleParameters[s - 1];
            double t = span > 0 ? (parameter - sampleParameters[s - 1]) / span : 0;
            return sampleLengths[s - 1] + t * (sampleLengths[s] - sampleLengths[s - 1]);
        }
        private static double GetParameter(double[] sampleParameters, double[] sampleLengths, double length)
        {
            int s = 1;
            while (s < sampleLengths.Length - 1 && sampleLengths[s] < length) s++;
            double span = sampleLengths[s] - sampleLengths[s - 1];
            double t = span > 0 ? (length - sampleLengths[s - 1]) / span : 0;
            return sampleParameters[s - 1] + t * (sampleParameters[s] - sampleParameters[s - 1]);
        }
        private static double GetDistance(double[] coor1, double[] coor2)
        {
            return Math.Sqrt(Math.Pow(coor1[0] - coor2[0], 2) + Math.Pow(coor1[1] - coor2[1], 2) +
                             Math.Pow(coor1[2] - coor2[2], 2));
        }
    }
}
