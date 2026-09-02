// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;

namespace CaeMesh
{
    public class QuadWildModelEdge
    {
        // Variables
        // The single place where the model-edge recovery tolerance policy is defined. Two independent
        // scales govern it. The lateral drift of an output node away from the source feature line scales
        // with the source triangulation, while how far the closest output node can sit from a source point
        // measured along the feature scales with the output quad size. The output quad size is a free
        // parameter of the mesher, so every tolerance that bounds a longitudinal distance has to be
        // expressed in it - a tolerance built only from the source segment length collapses as soon as the
        // requested quad edge length is a multiple of the traced triangle edge length.
        private const double MinimumRelativeTolerance = 1E-7;
        private const double StrictSegmentToleranceFactor = 0.06;
        private const double RelaxedSegmentToleranceFactor = 0.2;
        private const double StrictMeshToleranceFactor = 0.1;
        private const double RelaxedMeshToleranceFactor = 0.25;
        private const double RelaxedMinimumToleranceFactor = 10;
        private const double AnchorMeshToleranceFactor = 0.75;
        private const double CoverageToleranceFactor = 1.5;
        private const double CoverageMeshToleranceFactor = 0.35;
        private const double CoverageLengthFactor = 0.03;
        //
        private int _id;
        private double[][] _points;
        private int[] _nodeIds;
        private bool _isClosed;
        private bool _isOnFreeBoundary;
        private double _length;
        private double _medianSegmentLength;
        private double[] _boxMin;
        private double[] _boxMax;
        private double _modelDiagonal;


        // Properties
        public int Id { get { return _id; } }
        public double[][] Points { get { return _points; } }
        public int[] NodeIds { get { return _nodeIds; } }
        public bool IsClosed { get { return _isClosed; } }
        public bool IsOnFreeBoundary { get { return _isOnFreeBoundary; } }
        public double Length { get { return _length; } }
        public double MedianSegmentLength { get { return _medianSegmentLength; } }
        public double[] BoxMin { get { return _boxMin; } }
        public double[] BoxMax { get { return _boxMax; } }
        public double ModelDiagonal { get { return _modelDiagonal; } }
        public int SegmentCount { get { return _isClosed ? _points.Length : _points.Length - 1; } }
        public int StartNodeId { get { return _nodeIds[0]; } }
        public int EndNodeId { get { return _nodeIds[_nodeIds.Length - 1]; } }
        public double[] StartPoint { get { return _points[0]; } }
        public double[] EndPoint { get { return _points[_points.Length - 1]; } }


        // Constructors
        public QuadWildModelEdge(int id, double[][] points, int[] nodeIds, bool isClosed, bool isOnFreeBoundary,
                                 double modelDiagonal)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (nodeIds == null) throw new ArgumentNullException(nameof(nodeIds));
            if (points.Length != nodeIds.Length)
                throw new CaeException("Model edge " + id + " has a mismatched point and node count.");
            if (points.Length < (isClosed ? 3 : 2))
                throw new CaeException("Model edge " + id + " does not contain enough points.");
            if (modelDiagonal <= 0 || Double.IsNaN(modelDiagonal) || Double.IsInfinity(modelDiagonal))
                throw new CaeException("The QuadWild model-edge geometry is degenerate.");
            //
            _id = id;
            _points = points;
            _nodeIds = nodeIds;
            _isClosed = isClosed;
            _isOnFreeBoundary = isOnFreeBoundary;
            _modelDiagonal = modelDiagonal;
            //
            ComputeBoundingBox();
            ComputeLengths();
        }


        // Methods
        public void GetSegment(int index, out double[] start, out double[] end)
        {
            start = _points[index];
            end = _points[index + 1 == _points.Length ? 0 : index + 1];
        }
        public double GetDistanceSquared(double x, double y, double z)
        {
            double minDistanceSquared = Double.MaxValue;
            int segmentCount = SegmentCount;
            double[] start;
            double[] end;
            for (int i = 0; i < segmentCount; i++)
            {
                GetSegment(i, out start, out end);
                double distanceSquared = GetPointToSegmentDistanceSquared(x, y, z, start[0], start[1], start[2],
                                                                          end[0], end[1], end[2]);
                if (distanceSquared < minDistanceSquared) minDistanceSquared = distanceSquared;
            }
            return minDistanceSquared;
        }
        public List<double[]> GetSamplePoints()
        {
            int segmentCount = SegmentCount;
            List<double[]> samplePoints = new List<double[]>(2 * segmentCount + 1);
            double[] start;
            double[] end;
            for (int i = 0; i < segmentCount; i++)
            {
                GetSegment(i, out start, out end);
                samplePoints.Add(start);
                samplePoints.Add(new double[] { 0.5 * (start[0] + end[0]), 0.5 * (start[1] + end[1]),
                                                0.5 * (start[2] + end[2]) });
            }
            if (!_isClosed) samplePoints.Add(_points[_points.Length - 1]);
            return samplePoints;
        }
        public double[] GetMidPoint()
        {
            double halfLength = 0.5 * _length;
            double walked = 0;
            int segmentCount = SegmentCount;
            double[] start;
            double[] end;
            for (int i = 0; i < segmentCount; i++)
            {
                GetSegment(i, out start, out end);
                double segmentLength = GetPointToPointDistance(start, end);
                if (segmentLength > 0 && walked + segmentLength >= halfLength)
                {
                    double t = (halfLength - walked) / segmentLength;
                    return new double[] { start[0] + t * (end[0] - start[0]),
                                          start[1] + t * (end[1] - start[1]),
                                          start[2] + t * (end[2] - start[2]) };
                }
                walked += segmentLength;
            }
            return _points[_points.Length / 2];
        }
        public bool BoxOverlaps(double[] box, double tolerance)
        {
            return box[0] - tolerance <= _boxMax[0] && box[3] + tolerance >= _boxMin[0] &&
                   box[1] - tolerance <= _boxMax[1] && box[4] + tolerance >= _boxMin[1] &&
                   box[2] - tolerance <= _boxMax[2] && box[5] + tolerance >= _boxMin[2];
        }
        private void ComputeBoundingBox()
        {
            _boxMin = new double[] { Double.MaxValue, Double.MaxValue, Double.MaxValue };
            _boxMax = new double[] { -Double.MaxValue, -Double.MaxValue, -Double.MaxValue };
            foreach (double[] point in _points)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (point[i] < _boxMin[i]) _boxMin[i] = point[i];
                    if (point[i] > _boxMax[i]) _boxMax[i] = point[i];
                }
            }
        }
        private void ComputeLengths()
        {
            int segmentCount = SegmentCount;
            List<double> segmentLengths = new List<double>(segmentCount);
            double[] start;
            double[] end;
            _length = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                GetSegment(i, out start, out end);
                double segmentLength = GetPointToPointDistance(start, end);
                _length += segmentLength;
                if (segmentLength > 0) segmentLengths.Add(segmentLength);
            }
            // A median is used instead of the shortest segment so that a single sliver in the source
            // triangulation cannot collapse the tolerance of the whole model edge.
            if (segmentLengths.Count == 0) _medianSegmentLength = 0;
            else
            {
                segmentLengths.Sort();
                _medianSegmentLength = segmentLengths[segmentLengths.Count / 2];
            }
        }
        // The lateral half width of the band of output mesh edges that may belong to this model edge. It is
        // kept well below half the output quad size so that the neighbouring row of mesh edges can never be
        // collected, and above the source segment based drift so that a fine source stays supported.
        public double GetTubeTolerance(bool relaxed, double meshEdgeLength)
        {
            double minimumTolerance = _modelDiagonal * MinimumRelativeTolerance;
            if (relaxed)
                return Math.Max(Math.Max(minimumTolerance * RelaxedMinimumToleranceFactor,
                                         _medianSegmentLength * RelaxedSegmentToleranceFactor),
                                meshEdgeLength * RelaxedMeshToleranceFactor);
            return Math.Max(Math.Max(minimumTolerance, _medianSegmentLength * StrictSegmentToleranceFactor),
                            meshEdgeLength * StrictMeshToleranceFactor);
        }
        // How far a sampled source point may sit from the traced path. A coarse path chords a curved source
        // edge, so the allowance carries an output quad size term next to the source based ones.
        public double GetCoverageTolerance(double meshEdgeLength)
        {
            return Math.Max(Math.Max(GetTubeTolerance(true, meshEdgeLength) * CoverageToleranceFactor,
                                     _length * CoverageLengthFactor),
                            meshEdgeLength * CoverageMeshToleranceFactor);
        }
        // How far the output node closest to a source model vertex may sit from it. A model vertex that is
        // not a geometric corner is not preserved by QuadWild, so the closest output node lies up to half an
        // output quad edge away along the feature. This bound is deliberately independent of the model edge,
        // so that every model edge meeting at one source vertex resolves to the same output node.
        public static double GetAnchorSearchRadius(double meshEdgeLength, double modelDiagonal)
        {
            return Math.Max(meshEdgeLength * AnchorMeshToleranceFactor,
                            modelDiagonal * MinimumRelativeTolerance * RelaxedMinimumToleranceFactor);
        }
        private static double GetPointToPointDistance(double[] point1, double[] point2)
        {
            double dx = point2[0] - point1[0];
            double dy = point2[1] - point1[1];
            double dz = point2[2] - point1[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public static double GetPointToSegmentDistanceSquared(double x, double y, double z,
                                                              double x0, double y0, double z0,
                                                              double x1, double y1, double z1)
        {
            double vx = x1 - x0;
            double vy = y1 - y0;
            double vz = z1 - z0;
            double wx = x - x0;
            double wy = y - y0;
            double wz = z - z0;
            double lengthSquared = vx * vx + vy * vy + vz * vz;
            double t;
            if (lengthSquared <= 0) t = 0;
            else
            {
                t = (wx * vx + wy * vy + wz * vz) / lengthSquared;
                if (t < 0) t = 0;
                else if (t > 1) t = 1;
            }
            double dx = x - (x0 + t * vx);
            double dy = y - (y0 + t * vy);
            double dz = z - (z0 + t * vz);
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
