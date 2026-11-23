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
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// Where the edges of a model are split for its mirror planes, and the pieces the splits cut them into. The edges
    /// of a part that is its own mirror image need not be split alike on both sides of a plane: a vertex on one side
    /// - the end of an edge that goes on straight, the seam of a circle - may have a point inside an edge as its
    /// image. A mirrored mesh has a node there, so the edge is split at it, and at the images of that point across
    /// the other planes. A piece is the part of an edge between two of its vertices and splits that follow each
    /// other; an edge that is not split is its one piece and has its own id, the pieces of a split edge have negative
    /// ids. The planes map pieces onto pieces, and the balance of the counts gives every piece its number of
    /// elements, those of an edge adding up to its own.
    /// </summary>
    public class GmshMirrorSplits
    {
        // Variables                                                                                                    
        private const int NumOfLengthSamples = 64;
        private readonly Dictionary<int, List<double>> _edgeIdParameters;
        private readonly Dictionary<int, List<double[]>> _edgeIdPoints;
        private readonly Dictionary<int, int[]> _edgeIdPieceIds;
        private readonly Dictionary<int, int> _pieceIdEdgeId;
        private readonly Dictionary<int, int> _pieceIdIndex;
        private readonly Dictionary<int, double[]> _edgeIdBounds;
        private readonly Dictionary<int, double> _pieceIdShare;


        // Properties                                                                                                   
        public IEnumerable<int> SplitEdgeIds { get { return _edgeIdPieceIds.Keys.OrderBy(id => id); } }
        public int NumberOfSplitEdges { get { return _edgeIdPieceIds.Count; } }


        // Constructors                                                                                                 
        public GmshMirrorSplits()
        {
            _edgeIdParameters = new Dictionary<int, List<double>>();
            _edgeIdPoints = new Dictionary<int, List<double[]>>();
            _edgeIdPieceIds = new Dictionary<int, int[]>();
            _pieceIdEdgeId = new Dictionary<int, int>();
            _pieceIdIndex = new Dictionary<int, int>();
            _edgeIdBounds = new Dictionary<int, double[]>();
            _pieceIdShare = new Dictionary<int, double>();
        }


        // Methods                                                                                                      
        /// <summary>
        /// Splits the edge at the point of the parameter, unless it is split within the tolerance of it already. The
        /// splits are added before the pieces are numbered.
        /// </summary>
        public bool Add(int edgeId, double parameter, double[] point, double tolerance)
        {
            List<double> parameters;
            if (!_edgeIdParameters.TryGetValue(edgeId, out parameters))
            {
                parameters = new List<double>();
                _edgeIdParameters.Add(edgeId, parameters);
                _edgeIdPoints.Add(edgeId, new List<double[]>());
            }
            List<double[]> points = _edgeIdPoints[edgeId];
            foreach (var other in points)
            {
                double distance = Math.Sqrt((other[0] - point[0]) * (other[0] - point[0]) +
                                            (other[1] - point[1]) * (other[1] - point[1]) +
                                            (other[2] - point[2]) * (other[2] - point[2]));
                if (distance <= tolerance) return false;
            }
            int index = 0;
            while (index < parameters.Count && parameters[index] < parameter) index++;
            parameters.Insert(index, parameter);
            points.Insert(index, point);
            return true;
        }
        /// <summary>
        /// Numbers the pieces of the split edges, in the order of the ids of the edges and along each.
        /// </summary>
        public void Number()
        {
            int pieceId = 0;
            foreach (var edgeId in _edgeIdParameters.Keys.OrderBy(id => id))
            {
                int[] pieceIds = new int[_edgeIdParameters[edgeId].Count + 1];
                for (int i = 0; i < pieceIds.Length; i++)
                {
                    pieceIds[i] = --pieceId;
                    _pieceIdEdgeId.Add(pieceId, edgeId);
                    _pieceIdIndex.Add(pieceId, i);
                }
                _edgeIdPieceIds.Add(edgeId, pieceIds);
            }
        }
        public bool IsSplit(int edgeId)
        {
            return _edgeIdPieceIds.ContainsKey(edgeId);
        }
        public int[] GetPieceIds(int edgeId)
        {
            int[] pieceIds;
            return _edgeIdPieceIds.TryGetValue(edgeId, out pieceIds) ? pieceIds : new int[] { edgeId };
        }
        public int GetEdgeId(int pieceId)
        {
            return pieceId < 0 ? _pieceIdEdgeId[pieceId] : pieceId;
        }
        /// <summary>
        /// The points the edge is split at, along its parameter.
        /// </summary>
        public double[][] GetSplitPoints(int edgeId)
        {
            List<double[]> points;
            return _edgeIdPoints.TryGetValue(edgeId, out points) ? points.ToArray() : new double[0][];
        }
        public double[] GetSplitParameters(int edgeId)
        {
            List<double> parameters;
            return _edgeIdParameters.TryGetValue(edgeId, out parameters) ? parameters.ToArray() : new double[0];
        }
        /// <summary>
        /// The parameters of the edge the piece runs between.
        /// </summary>
        public void GetSpan(int pieceId, out double first, out double last)
        {
            int edgeId = GetEdgeId(pieceId);
            double[] bounds = GetBounds(edgeId);
            first = bounds[0];
            last = bounds[1];
            if (pieceId > 0) return;
            List<double> parameters = _edgeIdParameters[edgeId];
            int index = _pieceIdIndex[pieceId];
            if (index > 0) first = parameters[index - 1];
            if (index < parameters.Count) last = parameters[index];
        }
        /// <summary>
        /// The piece of the edge the parameter lies in.
        /// </summary>
        public int GetPieceAt(int edgeId, double parameter)
        {
            int[] pieceIds = GetPieceIds(edgeId);
            if (pieceIds.Length == 1) return pieceIds[0];
            List<double> parameters = _edgeIdParameters[edgeId];
            int index = 0;
            while (index < parameters.Count && parameters[index] < parameter) index++;
            return pieceIds[index];
        }
        /// <summary>
        /// The share of the length of its edge the piece has.
        /// </summary>
        public double GetLengthShare(int pieceId)
        {
            if (pieceId > 0) return 1;
            double share;
            if (_pieceIdShare.TryGetValue(pieceId, out share)) return share;
            int edgeId = _pieceIdEdgeId[pieceId];
            int[] pieceIds = _edgeIdPieceIds[edgeId];
            double[] lengths = new double[pieceIds.Length];
            double first;
            double last;
            double[] points;
            for (int i = 0; i < pieceIds.Length; i++)
            {
                GetSpan(pieceIds[i], out first, out last);
                double[] parameters = new double[NumOfLengthSamples + 1];
                for (int s = 0; s <= NumOfLengthSamples; s++)
                    parameters[s] = first + (last - first) * s / NumOfLengthSamples;
                Gmsh.Model.GetValue(1, edgeId, parameters, out points);
                for (int s = 1; s <= NumOfLengthSamples && points.Length == 3 * parameters.Length; s++)
                {
                    int p = 3 * s;
                    lengths[i] += Math.Sqrt((points[p] - points[p - 3]) * (points[p] - points[p - 3]) +
                                            (points[p + 1] - points[p - 2]) * (points[p + 1] - points[p - 2]) +
                                            (points[p + 2] - points[p - 1]) * (points[p + 2] - points[p - 1]));
                }
            }
            double total = lengths.Sum();
            for (int i = 0; i < pieceIds.Length; i++)
                _pieceIdShare[pieceIds[i]] = total > 0 ? lengths[i] / total : 1.0 / pieceIds.Length;
            return _pieceIdShare[pieceId];
        }
        /// <summary>
        /// For the balance of the counts: every split edge among the given ones carries as many elements as its
        /// pieces together, and a piece aims at its share of the number its edge aims at, one at least.
        /// </summary>
        public void AddPieceRows(ICollection<int> edgeIds, Func<int, int> getTarget, MirrorCountRows rows)
        {
            foreach (var edgeId in SplitEdgeIds)
            {
                if (!edgeIds.Contains(edgeId)) continue;
                int[] pieceIds = _edgeIdPieceIds[edgeId];
                rows.EqualSides.Add(new int[][] { new int[] { edgeId }, pieceIds });
                int target = getTarget(edgeId);
                foreach (var pieceId in pieceIds)
                    rows.PieceTargets[pieceId] = Math.Max(1, (int)Math.Round(GetLengthShare(pieceId) * target));
            }
        }
        private double[] GetBounds(int edgeId)
        {
            double[] bounds;
            if (_edgeIdBounds.TryGetValue(edgeId, out bounds)) return bounds;
            double[] min;
            double[] max;
            Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
            bounds = new double[] { min[0], max[0] };
            _edgeIdBounds.Add(edgeId, bounds);
            return bounds;
        }
    }
}
