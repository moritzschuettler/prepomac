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
    /// A planar chart of a face round a pole of its surface - a point at a bound of one parameter where the surface
    /// collapses for every value of the other, the angular one: the pole of a sphere, the apex of a cone. The
    /// parameters of the surface are singular there, the chart is regular, isometric at the pole and conformal: a point
    /// lies in the direction phi = kappa (u - u0), u0 the middle of the angular range of the face and kappa the ratio
    /// of the true angle round the pole to the parametric one (1 on a sphere, the sine of the half angle on a cone),
    /// measured on a small circle round the pole, at the distance s(rho) from the origin, rho the arc length from the
    /// pole along the iso-line of the angular parameter and s the radius that keeps the angles (GetConformalRadii): the
    /// development of a cone, the stereographic projection of a sphere. The face has no seam, so its angular range is
    /// an interval: the bounds of the face where they lie within one period, else the range of its boundary nodes
    /// unwrapped at the largest gap between them. The arc lengths come from tables at NumOfAngleDivisions + 1 values of
    /// the angular parameter and NumOfSamples + 1 of the other.
    /// </summary>
    public class GmshPoleChart
    {
        // Variables                                                                                                                
        private int _surfaceId;
        private int _p;
        private double _poleV;
        private double _farV;
        private double[] _pole;
        private double _period;
        private double _startU;
        private double _span;
        private double _middleU;
        private double _kappa;
        private double[] _min;
        private double[] _max;
        private double[][] _tableV;
        private double[][] _tableR;
        private double[] _meanR;
        private double[] _tableS;
        internal const int NumOfSamples = 256;
        private const double MinCircumferenceShare = 1E-3;
        private const int NumOfAngleDivisions = 64;
        private const int KappaSample = 2;
        private const double MaxPoleDistance = 1E-6;


        // Properties                                                                                                               
        public int SurfaceId { get { return _surfaceId; } }
        /// <summary>
        /// The parameter, 0 or 1, that runs round the pole.
        /// </summary>
        public int AngularDirection { get { return _p; } }
        /// <summary>
        /// The value of the other parameter at the pole.
        /// </summary>
        public double PoleParameter { get { return _poleV; } }
        public double[] Pole { get { return _pole; } }
        public double Kappa { get { return _kappa; } }
        /// <summary>
        /// The middle of the angular range of the face.
        /// </summary>
        public double MiddleU { get { return _middleU; } }


        // Constructor                                                                                                              
        /// <summary>
        /// The chart of the surface round the pole that FindPole found, over the angular range of faceUv, the
        /// parameters of the boundary nodes of the face other than its poles.
        /// </summary>
        public GmshPoleChart(int surfaceId, double[] pole, int p, double poleV, double farV, IList<double[]> faceUv)
        {
            _surfaceId = surfaceId;
            _pole = pole;
            _p = p;
            _poleV = poleV;
            _farV = farV;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out _min, out _max);
            _period = GetPeriod(surfaceId, p, _min, _max);
            double boundsSpan = _max[p] - _min[p];
            if (_period > 0 && boundsSpan > 0 && boundsSpan < _period * (1 - 1E-6))
            {
                _startU = _min[p];
                _span = boundsSpan;
            }
            else GetAngularRange(faceUv.Select(uv => uv[p]).ToArray(), _period, out _startU, out _span);
            if (!(_span > 0))
            {
                _startU = _min[p];
                _span = boundsSpan;
            }
            _middleU = _startU + _span / 2;
            GetArcLengthTables(surfaceId, p, _startU, _span, NumOfAngleDivisions, NumOfAngleDivisions + 1, poleV, farV,
                               out _tableV, out _tableR);
            _kappa = GetKappa();
            GetConformalRadii(out _meanR, out _tableS);
        }


        // Static methods                                                                                                           
        /// <summary>
        /// Whether the point is a pole of the surface: a bound of one parameter at which the surface collapses into the
        /// point for every value of the other, p, within MaxPoleDistance of the size; the value of the other parameter
        /// at the pole and at its other bound.
        /// </summary>
        public static bool FindPole(int surfaceId, double[] point, double size, out int p, out double poleV,
                                    out double farV)
        {
            double[] min;
            double[] max;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
            for (p = 0; p < 2; p++)
            {
                if (GetPoleParameter(surfaceId, p, min, max, point, size, out poleV, out farV)) return true;
            }
            p = -1;
            poleV = 0;
            farV = 0;
            return false;
        }
        /// <summary>
        /// The bound of the parameter other than p at which the surface collapses into the pole, and the other bound;
        /// false when neither bound does.
        /// </summary>
        internal static bool GetPoleParameter(int surfaceId, int p, double[] min, double[] max, double[] pole,
                                              double size, out double poleV, out double farV)
        {
            poleV = 0;
            farV = 0;
            int numOfAngles = 8;
            foreach (var atMin in new bool[] { true, false })
            {
                double v = atMin ? min[1 - p] : max[1 - p];
                double[] uv = new double[2 * numOfAngles];
                for (int i = 0; i < numOfAngles; i++)
                {
                    uv[2 * i + p] = min[p] + (max[p] - min[p]) * (i + 0.5) / numOfAngles;
                    uv[2 * i + 1 - p] = v;
                }
                double[] coor;
                Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
                bool collapses = coor.Length == 3 * numOfAngles;
                for (int i = 0; i < numOfAngles && collapses; i++)
                    collapses = GetDistance(new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] }, pole) <=
                                MaxPoleDistance * size;
                if (!collapses) continue;
                poleV = v;
                farV = atMin ? max[1 - p] : min[1 - p];
                return true;
            }
            return false;
        }
        /// <summary>
        /// The arc length along the iso-lines of the parameter p from the pole: at numOfAngles values of p from startU
        /// in steps of span / numOfDivisions, NumOfSamples + 1 values of the other parameter from the pole to the far
        /// bound, tableV, and the lengths up to them, tableR.
        /// </summary>
        internal static void GetArcLengthTables(int surfaceId, int p, double startU, double span, int numOfDivisions,
                                                int numOfAngles, double poleV, double farV, out double[][] tableV,
                                                out double[][] tableR)
        {
            tableV = new double[numOfAngles][];
            tableR = new double[numOfAngles][];
            double[] v = new double[NumOfSamples + 1];
            for (int s = 0; s <= NumOfSamples; s++) v[s] = poleV + (farV - poleV) * s / NumOfSamples;
            double[] uv = new double[2 * numOfAngles * (NumOfSamples + 1)];
            int count = 0;
            for (int k = 0; k < numOfAngles; k++)
            {
                tableV[k] = v;
                for (int s = 0; s <= NumOfSamples; s++)
                {
                    uv[count + p] = startU + span * k / numOfDivisions;
                    uv[count + 1 - p] = v[s];
                    count += 2;
                }
            }
            double[] coor;
            Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
            for (int k = 0; k < numOfAngles; k++)
            {
                tableR[k] = new double[NumOfSamples + 1];
                for (int s = 1; s <= NumOfSamples; s++)
                {
                    int q1 = 3 * (k * (NumOfSamples + 1) + s);
                    int q0 = q1 - 3;
                    tableR[k][s] = tableR[k][s - 1] + Math.Sqrt(Math.Pow(coor[q1] - coor[q0], 2) +
                                                                Math.Pow(coor[q1 + 1] - coor[q0 + 1], 2) +
                                                                Math.Pow(coor[q1 + 2] - coor[q0 + 2], 2));
                }
            }
        }
        /// <summary>
        /// Linear interpolation of ys at x in the increasing or decreasing xs, held at the ends.
        /// </summary>
        internal static double Interpolate(double[] xs, double[] ys, double x)
        {
            int last = xs.Length - 1;
            bool increasing = xs[last] >= xs[0];
            if (increasing ? x <= xs[0] : x >= xs[0]) return ys[0];
            if (increasing ? x >= xs[last] : x <= xs[last]) return ys[last];
            int low = 0;
            int high = last;
            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if ((xs[middle] <= x) == increasing) low = middle;
                else high = middle;
            }
            double span = xs[high] - xs[low];
            double share = span != 0 ? (x - xs[low]) / span : 0;
            return ys[low] + (ys[high] - ys[low]) * share;
        }
        /// <summary>
        /// The period of the parameter p of the surface, 2 pi when the surface repeats itself after it, else 0.
        /// </summary>
        public static double GetPeriod(int surfaceId, int p, double[] min, double[] max)
        {
            double period = 2 * Math.PI;
            int numOfPoints = 4;
            double[] uv = new double[4 * numOfPoints];
            for (int i = 0; i < numOfPoints; i++)
            {
                double u = min[p] + (max[p] - min[p]) * (i + 0.5) / numOfPoints;
                double v = min[1 - p] + (max[1 - p] - min[1 - p]) * (i + 0.5) / numOfPoints;
                uv[4 * i + p] = u;
                uv[4 * i + 1 - p] = v;
                uv[4 * i + 2 + p] = u + period;
                uv[4 * i + 3 - p] = v;
            }
            double[] coor;
            Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
            if (coor.Length != 6 * numOfPoints) return 0;
            double size = 0;
            double maxDistance = 0;
            for (int i = 0; i < numOfPoints; i++)
            {
                double[] a = new double[] { coor[6 * i], coor[6 * i + 1], coor[6 * i + 2] };
                double[] b = new double[] { coor[6 * i + 3], coor[6 * i + 4], coor[6 * i + 5] };
                maxDistance = Math.Max(maxDistance, GetDistance(a, b));
                for (int c = 0; c < 3; c++) size = Math.Max(size, Math.Abs(a[c]));
            }
            return maxDistance <= 1E-9 * Math.Max(1, size) ? period : 0;
        }
        /// <summary>
        /// The interval the values of an angular parameter cover: from the least to the greatest, or on a periodic
        /// parameter from the value after the largest gap between them round the period to the one before it.
        /// </summary>
        private static void GetAngularRange(double[] us, double period, out double startU, out double span)
        {
            startU = 0;
            span = 0;
            if (us.Length == 0) return;
            if (!(period > 0))
            {
                startU = us.Min();
                span = us.Max() - startU;
                return;
            }
            double[] sorted = us.Select(u => Wrap(u, 0, period)).OrderBy(u => u).ToArray();
            int n = sorted.Length;
            double largestGap = sorted[0] + period - sorted[n - 1];
            int after = 0;
            for (int i = 1; i < n; i++)
            {
                double gap = sorted[i] - sorted[i - 1];
                if (gap > largestGap)
                {
                    largestGap = gap;
                    after = i;
                }
            }
            startU = sorted[after];
            span = period - largestGap;
        }
        internal static double Wrap(double value, double start, double period)
        {
            double wrapped = (value - start) % period;
            if (wrapped < 0) wrapped += period;
            return start + wrapped;
        }
        internal static double GetDistance(double[] p, double[] q)
        {
            return Math.Sqrt((p[0] - q[0]) * (p[0] - q[0]) + (p[1] - q[1]) * (p[1] - q[1]) +
                             (p[2] - q[2]) * (p[2] - q[2]));
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The angular parameter brought into the range of the face, a value in the gap outside it to the nearer end.
        /// </summary>
        public double UnwrapU(double u)
        {
            if (!(_period > 0)) return u;
            double gapMiddle = _startU - (_period - _span) / 2;
            return Wrap(u, gapMiddle, _period);
        }
        /// <summary>
        /// The chart point of the parameters of a point of the face.
        /// </summary>
        public double[] ToChart(double[] uv)
        {
            double u = UnwrapU(uv[_p]);
            int k0;
            int k1;
            double t;
            GetAngleIndex(u, out k0, out k1, out t);
            double rho = (1 - t) * Interpolate(_tableV[k0], _tableR[k0], uv[1 - _p]) +
                         t * Interpolate(_tableV[k1], _tableR[k1], uv[1 - _p]);
            double radius = Interpolate(_meanR, _tableS, rho);
            double phi = _kappa * (u - _middleU);
            return new double[] { radius * Math.Cos(phi), radius * Math.Sin(phi) };
        }
        /// <summary>
        /// The parameters of a chart point, the angular one within the bounds of the surface where it is periodic.
        /// </summary>
        public double[] ToUv(double[] point)
        {
            double radius = Math.Sqrt(point[0] * point[0] + point[1] * point[1]);
            double rho = Interpolate(_tableS, _meanR, radius);
            double phi = radius > 0 ? Math.Atan2(point[1], point[0]) : 0;
            double u = _middleU + phi / _kappa;
            int k0;
            int k1;
            double t;
            GetAngleIndex(u, out k0, out k1, out t);
            double[] uv = new double[2];
            uv[1 - _p] = (1 - t) * Interpolate(_tableR[k0], _tableV[k0], rho) +
                         t * Interpolate(_tableR[k1], _tableV[k1], rho);
            double gap = Math.Max(0, _period - (_max[_p] - _min[_p]));
            uv[_p] = _period > 0 && (u < _min[_p] || u > _max[_p]) ? Wrap(u, _min[_p] - gap / 2, _period) : u;
            return uv;
        }
        /// <summary>
        /// The coordinates of chart points given as x0, y0, x1, y1, ..., as x0, y0, z0, x1, ...
        /// </summary>
        public double[] GetValues(double[] points)
        {
            double[] uv = new double[points.Length];
            for (int i = 0; i < points.Length / 2; i++)
            {
                double[] pointUv = ToUv(new double[] { points[2 * i], points[2 * i + 1] });
                uv[2 * i] = pointUv[0];
                uv[2 * i + 1] = pointUv[1];
            }
            double[] coor;
            Gmsh.Model.GetValue(2, _surfaceId, uv, out coor);
            return coor;
        }
        // The table index below the angular parameter u and the share of the way to the next one
        private void GetAngleIndex(double u, out int k0, out int k1, out double t)
        {
            double position = (u - _startU) / _span * NumOfAngleDivisions;
            position = Math.Max(0, Math.Min(NumOfAngleDivisions, position));
            k0 = Math.Min(NumOfAngleDivisions - 1, (int)Math.Floor(position));
            k1 = k0 + 1;
            t = position - k0;
        }
        /// <summary>
        /// The length of the iso-line of the other parameter close to the pole over the angular range against the
        /// parametric angle times its distance from the pole.
        /// </summary>
        private double GetKappa()
        {
            int s = KappaSample;
            double[] uv = new double[2 * (NumOfAngleDivisions + 1)];
            double meanRho = 0;
            for (int k = 0; k <= NumOfAngleDivisions; k++)
            {
                uv[2 * k + _p] = _startU + _span * k / NumOfAngleDivisions;
                uv[2 * k + 1 - _p] = _tableV[k][s];
                meanRho += _tableR[k][s] / (NumOfAngleDivisions + 1);
            }
            double[] coor;
            Gmsh.Model.GetValue(2, _surfaceId, uv, out coor);
            double length = 0;
            for (int k = 1; k <= NumOfAngleDivisions; k++)
            {
                length += Math.Sqrt(Math.Pow(coor[3 * k] - coor[3 * k - 3], 2) +
                                    Math.Pow(coor[3 * k + 1] - coor[3 * k - 2], 2) +
                                    Math.Pow(coor[3 * k + 2] - coor[3 * k - 1], 2));
            }
            double kappa = length / (meanRho * _span);
            return kappa > 1E-6 && !double.IsNaN(kappa) && !double.IsInfinity(kappa) ? kappa : 1;
        }
        /// <summary>
        /// The radius in the chart that makes it conformal, s(rho) with d ln s = kappa d rho / c(rho), c the length of
        /// the iso-line at the distance rho from the pole per unit of the angular parameter, s = rho at the pole: the
        /// development of a cone, the stereographic projection of a sphere. It is integrated over the mean distances of
        /// the samples, meanR, as rho exp(integral of kappa / c - 1 / rho); where the iso-lines shrink to a second pole
        /// the radius grows on linearly.
        /// </summary>
        private void GetConformalRadii(out double[] meanR, out double[] radii)
        {
            int numOfAngles = NumOfAngleDivisions + 1;
            double[] uv = new double[2 * numOfAngles * (NumOfSamples + 1)];
            int count = 0;
            for (int s = 0; s <= NumOfSamples; s++)
            {
                for (int k = 0; k < numOfAngles; k++)
                {
                    uv[count + _p] = _startU + _span * k / NumOfAngleDivisions;
                    uv[count + 1 - _p] = _tableV[k][s];
                    count += 2;
                }
            }
            double[] coor;
            Gmsh.Model.GetValue(2, _surfaceId, uv, out coor);
            meanR = new double[NumOfSamples + 1];
            double[] circumferences = new double[NumOfSamples + 1];
            for (int s = 0; s <= NumOfSamples; s++)
            {
                for (int k = 0; k < numOfAngles; k++)
                {
                    meanR[s] += _tableR[k][s] / numOfAngles;
                    if (k == 0) continue;
                    int q1 = 3 * (s * numOfAngles + k);
                    int q0 = q1 - 3;
                    circumferences[s] += Math.Sqrt(Math.Pow(coor[q1] - coor[q0], 2) +
                                                   Math.Pow(coor[q1 + 1] - coor[q0 + 1], 2) +
                                                   Math.Pow(coor[q1 + 2] - coor[q0 + 2], 2)) / _span;
                }
            }
            double maxCircumference = circumferences.Max();
            int widest = Array.IndexOf(circumferences, maxCircumference);
            radii = new double[NumOfSamples + 1];
            double integral = 0;
            double previous = 0;
            for (int s = 1; s <= NumOfSamples; s++)
            {
                double rho = meanR[s];
                bool shrunk = s > widest && circumferences[s] < MinCircumferenceShare * maxCircumference;
                if (shrunk || !(rho > meanR[s - 1]) || !(circumferences[s] > 0))
                {
                    double step = s > 1 ? radii[s - 1] - radii[s - 2] : rho;
                    radii[s] = radii[s - 1] + Math.Max(step, 0);
                    continue;
                }
                double g = _kappa / circumferences[s] - 1 / rho;
                if (s > 1) integral += (previous + g) / 2 * (rho - meanR[s - 1]);
                previous = g;
                radii[s] = rho * Math.Exp(integral);
            }
        }
    }
}
