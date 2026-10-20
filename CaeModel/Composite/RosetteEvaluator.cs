// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;

namespace CaeModel
{
    // Turns a rosette into a material reference direction at an arbitrary point.
    // A cartesian rosette returns its global X axis everywhere; a curve rosette returns the
    // direction of the curve segment that is closest to the point.
    public class RosetteEvaluator
    {
        // Variables
        private Rosette _rosette;
        private Vec3D _axisX;
        private Vec3D _axisY;
        private KdTree3D _tree;
        // The tree is built over the polyline vertices, not over the segment midpoints: a midpoint
        // is a poor proxy for a long straight segment, a vertex is always incident to the segments
        // around it
        private List<double[]> _vertices;
        private List<int> _vertexSegmentIndex;      // index of the segment that starts at the vertex
        private List<double[]> _segmentStarts;
        private List<double[]> _segmentDirections;
        private List<int> _segmentCurveIds;


        // Properties
        public Rosette Rosette { get { return _rosette; } }
        public Vec3D AxisX { get { return _axisX; } }
        public Vec3D AxisY { get { return _axisY; } }
        public bool IsUsable
        {
            get
            {
                return _rosette.Type == RosetteType.Cartesian ||
                       (_segmentStarts != null && _segmentStarts.Count > 0);
            }
        }
        public int SegmentCount { get { return _segmentStarts == null ? 0 : _segmentStarts.Count; } }


        // Constructor
        public RosetteEvaluator(Rosette rosette)
        {
            _rosette = rosette;
            _axisX = new Vec3D(rosette.AxisX);
            if (_axisX.Normalize() < 1E-12) _axisX = new Vec3D(1, 0, 0);
            _axisY = new Vec3D(rosette.AxisY);
            if (_axisY.Normalize() < 1E-12) _axisY = new Vec3D(0, 1, 0);
            //
            if (rosette.Type == RosetteType.FromCurves) BuildSegmentIndex();
        }


        // Methods
        private void BuildSegmentIndex()
        {
            _vertices = new List<double[]>();
            _vertexSegmentIndex = new List<int>();
            _segmentStarts = new List<double[]>();
            _segmentDirections = new List<double[]>();
            _segmentCurveIds = new List<int>();
            //
            if (_rosette.Curves == null) return;
            for (int c = 0; c < _rosette.Curves.Count; c++)
            {
                RosetteCurve curve = _rosette.Curves[c];
                if (!curve.Included) continue;
                double[][] points = curve.GetEffectivePoints();
                if (points == null || points.Length < 2) continue;
                //
                for (int i = 0; i < points.Length - 1; i++)
                {
                    double dx = points[i + 1][0] - points[i][0];
                    double dy = points[i + 1][1] - points[i][1];
                    double dz = points[i + 1][2] - points[i][2];
                    if (dx * dx + dy * dy + dz * dz < 1E-24) continue;
                    //
                    _vertices.Add(points[i]);
                    _vertexSegmentIndex.Add(_segmentStarts.Count);
                    _segmentStarts.Add(points[i]);
                    _segmentDirections.Add(new double[] { dx, dy, dz });
                    _segmentCurveIds.Add(c);
                }
                // The last vertex closes the polyline; it belongs to the segment that ends there
                _vertices.Add(points[points.Length - 1]);
                _vertexSegmentIndex.Add(_segmentStarts.Count - 1);
            }
            if (_vertices.Count > 0) _tree = new KdTree3D(_vertices);
        }

        // Reference direction adjusted for the guide angle: when the curve tangent is declared to be
        // the 90 degree direction, the material X axis must be perpendicular to the curve. Rotating
        // the tangent by -90 degrees about the shell normal (t x n) gives a local X whose local Y
        // (n x X) is the tangent itself.
        public bool TryGetRefDirection(double[] point, Vec3D normal, out Vec3D refDirection)
        {
            if (!TryGetRefDirection(point, out refDirection)) return false;
            if (_rosette.Type != RosetteType.FromCurves ||
                _rosette.GuideAngle != RosetteGuideAngle.Deg90 || normal == null) return true;
            //
            Vec3D rotated = Vec3D.CrossProduct(refDirection, normal);
            if (rotated.Normalize() < 1E-10) return true;      // tangent parallel to the normal
            refDirection = rotated;
            return true;
        }

        public bool TryGetRefDirection(double[] point, out Vec3D refDirection)
        {
            if (_rosette.Type == RosetteType.Cartesian)
            {
                refDirection = _axisX;
                return true;
            }
            refDirection = null;
            if (_tree == null || _vertices.Count == 0) return false;
            //
            int nearestVertex = _tree.FindNearest(point);
            if (nearestVertex < 0) return false;
            int seed = _vertexSegmentIndex[nearestVertex];
            if (seed < 0 || seed >= _segmentStarts.Count) return false;
            // The closest segment is incident to the closest vertex or lies a few segments away
            // along the same curve; the exact point to segment distance decides
            int best = seed;
            double bestDistance = DistanceToSegment(point, seed);
            int curveId = _segmentCurveIds[seed];
            for (int offset = -3; offset <= 3; offset++)
            {
                int index = seed + offset;
                if (index < 0 || index >= _segmentStarts.Count || index == seed) continue;
                if (_segmentCurveIds[index] != curveId) continue;
                double distance = DistanceToSegment(point, index);
                if (distance < bestDistance) { bestDistance = distance; best = index; }
            }
            //
            Vec3D direction = new Vec3D(_segmentDirections[best]);
            if (direction.Normalize() < 1E-12) return false;
            refDirection = direction;
            return true;
        }

        private double DistanceToSegment(double[] point, int index)
        {
            double[] a = _segmentStarts[index];
            double[] d = _segmentDirections[index];
            double length2 = d[0] * d[0] + d[1] * d[1] + d[2] * d[2];
            double t = ((point[0] - a[0]) * d[0] + (point[1] - a[1]) * d[1] + (point[2] - a[2]) * d[2]) / length2;
            if (t < 0) t = 0;
            else if (t > 1) t = 1;
            double dx = point[0] - (a[0] + t * d[0]);
            double dy = point[1] - (a[1] + t * d[1]);
            double dz = point[2] - (a[2] + t * d[2]);
            return dx * dx + dy * dy + dz * dz;
        }

        // Projects a reference direction onto the plane defined by the normal and builds the local
        // orientation frame; the fallback chain matches the one of the composite layup writer
        public static void ProjectOntoPlane(Vec3D refDirection, Vec3D normal, Vec3D axisYHint,
                                            out Vec3D localX, out Vec3D localY)
        {
            double dot = Vec3D.DotProduct(refDirection, normal);
            localX = new Vec3D(refDirection.X - dot * normal.X,
                               refDirection.Y - dot * normal.Y,
                               refDirection.Z - dot * normal.Z);
            double length = localX.Normalize();
            if (length < 1E-6)
            {
                // The reference direction is parallel to the normal - use the Y axis hint instead
                Vec3D fallback = axisYHint != null ? axisYHint : new Vec3D(0, 1, 0);
                double dotY = Vec3D.DotProduct(fallback, normal);
                localX = new Vec3D(fallback.X - dotY * normal.X,
                                   fallback.Y - dotY * normal.Y,
                                   fallback.Z - dotY * normal.Z);
                length = localX.Normalize();
                if (length < 1E-6)
                {
                    double dotX = normal.X;
                    localX = new Vec3D(1.0 - dotX * normal.X, -dotX * normal.Y, -dotX * normal.Z);
                    localX.Normalize();
                }
            }
            localY = Vec3D.CrossProduct(normal, localX);
            localY.Normalize();
        }

        // Quantizes a unit direction so that elements with similar directions share one orientation.
        // Opposite directions are mapped onto the same key because a fiber direction has no sign.
        public static string GetDirectionKey(Vec3D direction, double toleranceDeg)
        {
            double x = direction.X, y = direction.Y, z = direction.Z;
            // Canonical hemisphere
            if (z < -1E-12 || (Math.Abs(z) <= 1E-12 && (y < -1E-12 || (Math.Abs(y) <= 1E-12 && x < 0))))
            {
                x = -x; y = -y; z = -z;
            }
            double step = Math.Max(0.1, toleranceDeg) * Math.PI / 180.0;
            double theta = Math.Acos(Math.Max(-1.0, Math.Min(1.0, z)));
            double phi = Math.Atan2(y, x);
            long thetaIndex = (long)Math.Round(theta / step);
            long phiIndex = (long)Math.Round(phi / step);
            // Near the poles every phi maps onto the same direction
            if (thetaIndex == 0) phiIndex = 0;
            return thetaIndex + "_" + phiIndex;
        }

        // Builds the evaluators of the rosettes that are referenced by a composite layup
        public static Dictionary<string, RosetteEvaluator> BuildEvaluators(
            CompositeLayup layup, CaeGlobals.OrderedDictionary<string, Rosette> rosettes, List<string> warnings)
        {
            Dictionary<string, RosetteEvaluator> evaluators = new Dictionary<string, RosetteEvaluator>();
            if (layup == null || rosettes == null) return evaluators;
            //
            HashSet<string> names = new HashSet<string>();
            if (layup.RosetteEnabled && !string.IsNullOrEmpty(layup.OrientationRosette))
                names.Add(layup.OrientationRosette);
            if (layup.Plies != null)
                foreach (Ply ply in layup.Plies)
                    if (!string.IsNullOrEmpty(ply.OrientationRosette)) names.Add(ply.OrientationRosette);
            //
            foreach (string name in names)
            {
                Rosette rosette;
                if (!rosettes.TryGetValue(name, out rosette))
                {
                    if (warnings != null)
                        warnings.Add(string.Format(
                            "Composite layup '{0}': the rosette '{1}' does not exist; the ply rotation " +
                            "angle is used instead.", layup.Name, name));
                    continue;
                }
                RosetteEvaluator evaluator = new RosetteEvaluator(rosette);
                if (!evaluator.IsUsable)
                {
                    if (warnings != null)
                        warnings.Add(string.Format(
                            "Composite layup '{0}': the rosette '{1}' contains no usable curve; the ply " +
                            "rotation angle is used instead.", layup.Name, name));
                    continue;
                }
                evaluators.Add(name, evaluator);
            }
            return evaluators;
        }
    }
}
