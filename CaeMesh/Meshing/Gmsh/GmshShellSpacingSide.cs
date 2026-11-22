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
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// A four-sided face's oriented edge chain, with node fractions and arc-length samples for comparing shapes.
    /// </summary>
    internal class GmshShellSpacingSide
    {
        // Variables                                                                                                    
        private readonly GmshShellSpacingEdge[] _edges;
        private readonly int[] _nodeEdgeIndices;
        private readonly int[] _localNodeIndices;
        private readonly double[] _offsets;
        private readonly double[] _scales;
        private readonly double _length;


        // Properties                                                                                                   
        public int NumOfNodes { get { return _nodeEdgeIndices.Length; } }
        public double Length { get { return _length; } }


        // Constructors                                                                                                 
        private GmshShellSpacingSide(GmshShellSpacingEdge[] edges, int numOfNodes, double length)
        {
            _edges = edges;
            _length = length;
            _nodeEdgeIndices = new int[numOfNodes];
            _localNodeIndices = new int[numOfNodes];
            _offsets = new double[edges.Length];
            _scales = new double[edges.Length];
        }


        // Methods                                                                                                      
        public static GmshShellSpacingSide Create(TransfiniteFace face, int sideIndex,
                                                  Dictionary<int, GmshShellSpacingEdge> edgeById)
        {
            return Create(face.SideEdgeIds[sideIndex], face.CornerNodeIds[sideIndex],
                          face.CornerNodeIds[(sideIndex + 1) % 4], edgeById);
        }
        public static GmshShellSpacingSide Create(int[] edgeIds, int startVertexId, int endVertexId,
                                                  Dictionary<int, GmshShellSpacingEdge> edgeById)
        {
            GmshShellSpacingEdge[] edges = new GmshShellSpacingEdge[edgeIds.Length];
            double length = 0;
            int numOfNodes = 1;
            for (int e = 0; e < edges.Length; e++)
            {
                if (!edgeById.TryGetValue(edgeIds[e], out edges[e]) || edges[e] == null) return null;
                length += edges[e].Length;
                numOfNodes += edges[e].NumOfNodes - 1;
            }
            //
            GmshShellSpacingSide side = new GmshShellSpacingSide(edges, numOfNodes, length);
            int vertexId = startVertexId;
            int node = 0;
            double offset = 0;
            for (int e = 0; e < edges.Length; e++)
            {
                GmshShellSpacingEdge edge = edges[e];
                bool reversed = vertexId == edge.EndVertexId;
                if (!reversed && vertexId != edge.StartVertexId) return null;
                double scale = edge.Length / length;
                side._offsets[e] = reversed ? offset + scale : offset;
                side._scales[e] = reversed ? -scale : scale;
                offset += scale;
                int last = edge.NumOfNodes - 1;
                int count = e == edges.Length - 1 ? last + 1 : last;
                for (int k = 0; k < count; k++, node++)
                {
                    side._nodeEdgeIndices[node] = e;
                    side._localNodeIndices[node] = reversed ? last - k : k;
                }
                vertexId = reversed ? edge.StartVertexId : edge.EndVertexId;
            }
            if (vertexId != endVertexId) return null;
            return side;
        }
        public GmshShellSpacingNode GetNode(int node, double length, bool reverse)
        {
            int e = _nodeEdgeIndices[node];
            double offset = reverse ? 1 - _offsets[e] : _offsets[e];
            double scale = reverse ? -_scales[e] : _scales[e];
            return new GmshShellSpacingNode(_edges[e], _localNodeIndices[node], length * offset, length * scale);
        }
        public double[][] GetShapePoints(int count)
        {
            double[] start = GetNode(0, 1, false).Point;
            double[] end = GetNode(NumOfNodes - 1, 1, false).Point;
            double[][] points = new double[count][];
            points[0] = start;
            points[count - 1] = end;
            bool straight = Math.Abs(GmshPoleChart.GetDistance(start, end) / _length - 1) < 1E-8;
            int edge = 0;
            double edgeEnd = Math.Max(_offsets[edge], _offsets[edge] + _scales[edge]);
            for (int k = 1; k < count - 1; k++)
            {
                double fraction = (double)k / (count - 1);
                if (straight)
                {
                    points[k] = new double[3];
                    for (int c = 0; c < 3; c++) points[k][c] = start[c] + fraction * (end[c] - start[c]);
                }
                else
                {
                    while (edge < _edges.Length - 1 && fraction > edgeEnd)
                    {
                        edge++;
                        edgeEnd = Math.Max(_offsets[edge], _offsets[edge] + _scales[edge]);
                    }
                    points[k] = _edges[edge].EvaluatePoint((fraction - _offsets[edge]) / _scales[edge]);
                }
            }
            return points;
        }
        public void MarkSplitEdges(HashSet<GmshShellSpacingEdge> known)
        {
            if (_edges.Length > 1) known.UnionWith(_edges);
        }
        public void CopyKnownSpacing(GmshShellSpacingSide opposite, HashSet<GmshShellSpacingEdge> known)
        {
            if (_edges.Length != 1 || known.Contains(_edges[0]) || !opposite._edges.All(known.Contains)) return;
            int last = NumOfNodes - 1;
            for (int k = 1; k < last; k++)
                AddError(k, 1 - GetFraction(k) - opposite.GetFraction(last - k));
            _edges[0].Relax(1);
            known.Add(_edges[0]);
        }
        public double GetFraction(int node)
        {
            int e = _nodeEdgeIndices[node];
            return _offsets[e] + _scales[e] * _edges[e].GetFraction(_localNodeIndices[node]);
        }
        public void AddError(int node, double error)
        {
            int e = _nodeEdgeIndices[node];
            _edges[e].AddError(_localNodeIndices[node], _scales[e], error);
        }
    }
}
