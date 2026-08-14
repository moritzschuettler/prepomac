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
using System.Threading.Tasks;

namespace CaeMesh
{
    /// <summary>
    /// Mesh based recognition of the analytic type of the visualization edges:
    /// line, circle, ellipse, parabola and hyperbola.
    ///
    /// The recognition is discrete and does not need the CAD geometry. It is used for the meshed
    /// parts where the mapping to the geometry edges does not exist any more.
    ///
    /// Only the corner nodes of the visualization edge cells are used since the mid-side nodes of the
    /// parabolic elements do not necessarily lie on the underlying geometry.
    ///
    /// The algorithm - one pass over the edge cells followed by closed form fits. Each step is only
    /// reached when the previous one is rejected:
    ///
    /// 1 ... Line - from the eigenvalues of the covariance matrix of the edge nodes.
    /// 2 ... The edge nodes must be planar for any of the remaining curves - the plane is the one
    ///       belonging to the smallest eigenvalue of the same covariance matrix.
    /// 3 ... Circle - an algebraic fit in the plane of the edge.
    /// 4 ... Ellipse, parabola and hyperbola - an algebraic fit of a general conic in the plane of the
    ///       edge. The type follows from the discriminant B^2 - 4AC of the recognized conic.
    ///
    /// A type is only accepted when the deviation of the edge nodes from the recognized curve is small
    /// and when the fit is not much worse than the deviation of the nodes from a local quadratic curve,
    /// which is the noise floor of the mesh. Without the second test a short spline would be recognized
    /// as a circle or an ellipse.
    /// </summary>
    public static class EdgeTypeRecognition
    {
        // Variables
        /// <summary>
        /// Recognize the edge types when a mesh is read
        /// </summary>
        public static bool AutoDetect = true;
        /// <summary>
        /// The maximum rms distance of the edge nodes from the fitted line, relative to the edge extent
        /// </summary>
        public static double LineRelTol = 5E-3;
        /// <summary>
        /// The maximum rms distance of the edge nodes from the plane of the edge, relative to the edge extent
        /// </summary>
        public static double PlanarRelTol = 1E-3;
        /// <summary>
        /// The maximum rms distance of the edge nodes from the fitted curve, relative to the radius of a
        /// circle and to the edge extent for the other curves
        /// </summary>
        public static double FitRelTol = 5E-3;
        /// <summary>
        /// The maximum relative value of the discriminant of a conic to be recognized as a parabola
        /// </summary>
        public static double ParabolaRelTol = 0.02;
        //
        private const double _tiny = 1E-30;


        // Methods
        /// <summary>
        /// Recognizes the types of all visualization edges. The optional edgeData array must be of the
        /// same length as the number of the edges and is filled with the recognized curve parameters.
        /// </summary>
        public static GeomCurveType[] GetEdgeTypes(VisualizationData visualization, Dictionary<int, FeNode> nodes,
                                                   GeomCurveData[] edgeData = null)
        {
            if (visualization == null || visualization.EdgeCellIdsByEdge == null ||
                visualization.EdgeCells == null || nodes == null) return null;
            //
            int numOfEdges = visualization.EdgeCellIdsByEdge.Length;
            GeomCurveType[] edgeTypes = new GeomCurveType[numOfEdges];
            //
            Parallel.For(0, numOfEdges, edgeId =>
            {
                GeomCurveData data;
                // The recognition must never break the extraction of the visualization
                try { data = GetEdgeData(visualization, nodes, edgeId); }
                catch { data = new GeomCurveData(); }
                //
                edgeTypes[edgeId] = data.Type;
                if (edgeData != null) edgeData[edgeId] = data;
            });
            //
            return edgeTypes;
        }
        /// <summary>
        /// Recognizes the type and the parameters of a single visualization edge.
        /// </summary>
        public static GeomCurveData GetEdgeData(VisualizationData visualization, Dictionary<int, FeNode> nodes, int edgeId)
        {
            GeomCurveData edgeData = new GeomCurveData();
            //
            if (visualization == null || visualization.EdgeCellIdsByEdge == null || visualization.EdgeCells == null ||
                nodes == null || edgeId < 0 || edgeId >= visualization.EdgeCellIdsByEdge.Length) return edgeData;
            //
            int[] edgeCellIds = visualization.EdgeCellIdsByEdge[edgeId];
            if (edgeCellIds == null || edgeCellIds.Length == 0) return edgeData;
            // Collect the edge nodes - the edge cells are stored in a consecutive order and only the two
            // corner nodes of an edge cell are used
            bool ordered = true;
            int[] edgeCell;
            List<double[]> pointList = new List<double[]>(edgeCellIds.Length + 1);
            HashSet<int> usedNodeIds = new HashSet<int>();
            int firstNodeId = -1;
            int previousNodeId = -1;
            //
            for (int i = 0; i < edgeCellIds.Length; i++)
            {
                edgeCell = visualization.EdgeCells[edgeCellIds[i]];
                if (edgeCell.Length != 2 && edgeCell.Length != 3) return edgeData;   // unsupported cell type
                //
                if (i == 0)
                {
                    firstNodeId = edgeCell[0];
                    AddPoint(pointList, usedNodeIds, nodes, edgeCell[0]);
                }
                else if (edgeCell[0] != previousNodeId) ordered = false;
                //
                AddPoint(pointList, usedNodeIds, nodes, edgeCell[1]);
                previousNodeId = edgeCell[1];
            }
            //
            bool closed = ordered && previousNodeId == firstNodeId && edgeCellIds.Length > 2;
            //
            int numOfNodes = pointList.Count;
            double[][] points = pointList.ToArray();
            edgeData.NodeCount = numOfNodes;
            edgeData.Closed = closed;
            // The length is measured along the edge cells
            double length = 0;
            if (ordered)
            {
                for (int i = 1; i < numOfNodes; i++) length += Distance(points[i], points[i - 1]);
                if (closed) length += Distance(points[0], points[numOfNodes - 1]);
            }
            edgeData.Length = length;
            //
            if (numOfNodes < 2) return edgeData;
            // The centroid and the extent of the edge
            double[] centroid = new double[3];
            for (int i = 0; i < numOfNodes; i++)
            {
                centroid[0] += points[i][0];
                centroid[1] += points[i][1];
                centroid[2] += points[i][2];
            }
            centroid[0] /= numOfNodes;
            centroid[1] /= numOfNodes;
            centroid[2] /= numOfNodes;
            //
            double extent = 0;
            for (int i = 0; i < numOfNodes; i++) extent = Math.Max(extent, Distance(points[i], centroid));
            if (extent <= 0) return edgeData;
            // Two nodes always define a line
            if (numOfNodes == 2)
            {
                edgeData.Type = GeomCurveType.Line;
                edgeData.Point = centroid;
                edgeData.Direction = Direction(points[0], points[1]);
                return edgeData;
            }
            // The deviation of the edge nodes from a local quadratic curve - the noise floor of the mesh
            double noiseFloor = ordered ? GetLocalFitDeviation(points) : double.MaxValue;
            // 1 Line
            double[] eigenValues;
            double[][] eigenVectors;
            double[][] covariance = GetPointCovariance(points, centroid);
            if (!SmallMatrixTools.SymmetricEigen(covariance, out eigenValues, out eigenVectors)) return edgeData;
            //
            double rms = Math.Sqrt(Math.Max(eigenValues[1] + eigenValues[2], 0) / numOfNodes);
            if (IsAccepted(rms, extent, noiseFloor, LineRelTol))
            {
                edgeData.Type = GeomCurveType.Line;
                edgeData.Point = centroid;
                edgeData.Direction = eigenVectors[0];
                edgeData.Residual = rms / extent;
                return edgeData;
            }
            // 2 The remaining curves are planar
            double planeRms = Math.Sqrt(Math.Max(eigenValues[2], 0) / numOfNodes);
            if (planeRms > PlanarRelTol * extent)
            {
                edgeData.Type = GeomCurveType.OtherCurve;       // a three dimensional curve
                edgeData.Point = centroid;
                return edgeData;
            }
            // The coordinates of the edge nodes in the plane of the edge
            double[] u = eigenVectors[0];
            double[] v = eigenVectors[1];
            double[] normal = eigenVectors[2];
            double[] x = new double[numOfNodes];
            double[] y = new double[numOfNodes];
            double[] d = new double[3];
            for (int i = 0; i < numOfNodes; i++)
            {
                d[0] = points[i][0] - centroid[0];
                d[1] = points[i][1] - centroid[1];
                d[2] = points[i][2] - centroid[2];
                x[i] = Dot(d, u);
                y[i] = Dot(d, v);
            }
            // 3 Circle
            double x0;
            double y0;
            double radius;
            double circleResidual;
            if (FitCircle(x, y, extent, out x0, out y0, out radius, out circleResidual) && radius > _tiny &&
                IsAccepted(circleResidual, Math.Min(radius, extent), noiseFloor, FitRelTol))
            {
                edgeData.Type = GeomCurveType.Circle;
                edgeData.Point = new double[] { centroid[0] + x0 * u[0] + y0 * v[0],
                                                centroid[1] + x0 * u[1] + y0 * v[1],
                                                centroid[2] + x0 * u[2] + y0 * v[2] };
                edgeData.Direction = normal;
                edgeData.Radius = radius;
                edgeData.ArcAngleDeg = ordered ? GetSweepAngleDeg(x, y, x0, y0, closed) : 0;
                edgeData.Residual = circleResidual / radius;
                return edgeData;
            }
            // 4 Ellipse, parabola and hyperbola
            if (TryFitConic(edgeData, x, y, extent, centroid, u, v, normal, noiseFloor, ordered, closed)) return edgeData;
            // 5 Free form
            edgeData.Type = GeomCurveType.OtherCurve;
            edgeData.Point = centroid;
            edgeData.Direction = normal;
            //
            return edgeData;
        }
        /// <summary>
        /// An algebraic fit of a general conic A x^2 + B xy + C y^2 + D x + E y + F = 0 in the plane of
        /// the edge. The coefficients are the eigenvector of the 6 x 6 scatter matrix belonging to the
        /// smallest eigenvalue. The type of the conic follows from the discriminant B^2 - 4AC.
        /// </summary>
        private static bool TryFitConic(GeomCurveData edgeData, double[] x, double[] y, double scale,
                                        double[] centroid, double[] u, double[] v, double[] normal,
                                        double noiseFloor, bool ordered, bool closed)
        {
            int n = x.Length;
            if (n < 6) return false;
            //
            double xs;
            double ys;
            double[] row = new double[6];
            double[][] m = GetZeroMatrix(6);
            //
            for (int i = 0; i < n; i++)
            {
                xs = x[i] / scale;
                ys = y[i] / scale;
                //
                row[0] = xs * xs;
                row[1] = xs * ys;
                row[2] = ys * ys;
                row[3] = xs;
                row[4] = ys;
                row[5] = 1;
                //
                for (int j = 0; j < 6; j++)
                {
                    for (int k = j; k < 6; k++) m[j][k] += row[j] * row[k];
                }
            }
            for (int j = 0; j < 6; j++)
            {
                for (int k = 0; k < j; k++) m[j][k] = m[k][j];
            }
            //
            double[] eigenValues;
            double[][] eigenVectors;
            if (!SmallMatrixTools.SymmetricEigen(m, out eigenValues, out eigenVectors)) return false;
            //
            double a = eigenVectors[5][0];
            double b = eigenVectors[5][1];
            double c = eigenVectors[5][2];
            double dd = eigenVectors[5][3];
            double e = eigenVectors[5][4];
            double f = eigenVectors[5][5];
            // A conic which is not curved is a line - it was already tested for
            double quadratic = b * b + 4 * a * a + 4 * c * c;
            if (quadratic <= 1E-12) return false;
            // The Sampson distance is a first order approximation of the distance from the conic
            double q;
            double gx;
            double gy;
            double grad2;
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                xs = x[i] / scale;
                ys = y[i] / scale;
                //
                q = a * xs * xs + b * xs * ys + c * ys * ys + dd * xs + e * ys + f;
                gx = 2 * a * xs + b * ys + dd;
                gy = b * xs + 2 * c * ys + e;
                grad2 = gx * gx + gy * gy;
                if (grad2 <= _tiny) return false;
                //
                sum += q * q / grad2;
            }
            double residual = Math.Sqrt(sum / n) * scale;
            if (!IsAccepted(residual, scale, noiseFloor, FitRelTol)) return false;
            // The type of the conic
            double discriminant = (b * b - 4 * a * c) / quadratic;
            //
            edgeData.Direction = normal;
            edgeData.Residual = residual / scale;
            //
            if (Math.Abs(discriminant) <= ParabolaRelTol)
            {
                edgeData.Type = GeomCurveType.Parabola;
                edgeData.Point = ToGlobal(centroid, u, v, 0, 0);        // the vertex is not computed
                return true;
            }
            // The center of the conic: [2A B; B 2C] [x0; y0] = [-D; -E]
            double det = 4 * a * c - b * b;
            if (Math.Abs(det) <= 1E-12) return false;
            double x0 = (-2 * c * dd + b * e) / det;
            double y0 = (-2 * a * e + b * dd) / det;
            double f0 = a * x0 * x0 + b * x0 * y0 + c * y0 * y0 + dd * x0 + e * y0 + f;
            //
            edgeData.Point = ToGlobal(centroid, u, v, x0 * scale, y0 * scale);
            //
            if (discriminant > 0)
            {
                edgeData.Type = GeomCurveType.Hyperbola;
                return true;
            }
            // Ellipse - the semi axes follow from the eigenvalues of [A B/2; B/2 C]
            double average = (a + c) / 2;
            double difference = Math.Sqrt((a - c) * (a - c) / 4 + b * b / 4);
            double lambda1 = average + difference;
            double lambda2 = average - difference;
            if (Math.Abs(lambda1) <= _tiny || Math.Abs(lambda2) <= _tiny) return false;
            //
            double radius1 = -f0 / lambda1;
            double radius2 = -f0 / lambda2;
            if (radius1 <= 0 || radius2 <= 0) return false;
            radius1 = Math.Sqrt(radius1) * scale;
            radius2 = Math.Sqrt(radius2) * scale;
            // The eigenvector belonging to lambda1
            double[] axis1 = new double[] { b / 2, lambda1 - a };
            if (Math.Abs(axis1[0]) + Math.Abs(axis1[1]) <= _tiny) axis1 = new double[] { 1, 0 };
            double axisLength = Math.Sqrt(axis1[0] * axis1[0] + axis1[1] * axis1[1]);
            axis1[0] /= axisLength;
            axis1[1] /= axisLength;
            // The major axis belongs to the larger radius
            double[] majorAxis;
            if (radius1 >= radius2)
            {
                edgeData.Radius = radius1;
                edgeData.MinorRadius = radius2;
                majorAxis = axis1;
            }
            else
            {
                edgeData.Radius = radius2;
                edgeData.MinorRadius = radius1;
                majorAxis = new double[] { -axis1[1], axis1[0] };
            }
            //
            edgeData.Type = GeomCurveType.Ellipse;
            edgeData.MajorAxis = new double[] { majorAxis[0] * u[0] + majorAxis[1] * v[0],
                                                majorAxis[0] * u[1] + majorAxis[1] * v[1],
                                                majorAxis[0] * u[2] + majorAxis[1] * v[2] };
            edgeData.ArcAngleDeg = ordered ? GetSweepAngleDeg(x, y, x0 * scale, y0 * scale, closed) : 0;
            //
            return true;
        }
        /// <summary>
        /// The rms deviation of the edge nodes from a quadratic curve fitted through a sliding window of
        /// five consecutive nodes. It is the noise floor of the mesh: a fit which is much worse than this
        /// value is not the underlying curve of the edge.
        /// </summary>
        private static double GetLocalFitDeviation(double[][] points)
        {
            int n = points.Length;
            int window = 5;
            if (n < window + 2) return double.MaxValue;
            //
            int count = 0;
            double sum = 0;
            double t;
            double[] parameter = new double[window];
            //
            for (int start = 0; start + window <= n; start += 2)
            {
                // The parameter is the accumulated chord length
                parameter[0] = 0;
                for (int i = 1; i < window; i++)
                    parameter[i] = parameter[i - 1] + Distance(points[start + i], points[start + i - 1]);
                if (parameter[window - 1] <= _tiny) continue;
                // Normalize the parameter to keep the system well conditioned
                double scale = parameter[window - 1];
                for (int i = 0; i < window; i++) parameter[i] /= scale;
                // Fit x(t), y(t) and z(t) as quadratic polynomials - the same matrix for all three
                double[][] normalMatrix = GetZeroMatrix(3);
                double[][] normalRhs = new double[3][];
                for (int i = 0; i < 3; i++) normalRhs[i] = new double[3];
                //
                double[] basis = new double[3];
                for (int i = 0; i < window; i++)
                {
                    t = parameter[i];
                    basis[0] = 1;
                    basis[1] = t;
                    basis[2] = t * t;
                    //
                    for (int j = 0; j < 3; j++)
                    {
                        for (int k = 0; k < 3; k++) normalMatrix[j][k] += basis[j] * basis[k];
                        for (int k = 0; k < 3; k++) normalRhs[k][j] += basis[j] * points[start + i][k];
                    }
                }
                //
                bool solved = true;
                double[][] coefficients = new double[3][];
                for (int k = 0; k < 3; k++)
                {
                    if (!SmallMatrixTools.Solve(normalMatrix, normalRhs[k], out coefficients[k]))
                    {
                        solved = false;
                        break;
                    }
                }
                if (!solved) continue;
                // The deviation of the window nodes from the fitted quadratic curve
                double dk;
                for (int i = 0; i < window; i++)
                {
                    t = parameter[i];
                    for (int k = 0; k < 3; k++)
                    {
                        dk = points[start + i][k] - (coefficients[k][0] + coefficients[k][1] * t +
                                                     coefficients[k][2] * t * t);
                        sum += dk * dk;
                    }
                }
                count += window - 3;
            }
            //
            if (count <= 0) return double.MaxValue;
            //
            return Math.Sqrt(sum / count);
        }
        /// <summary>
        /// The angle swept by the edge nodes around the center of the fitted curve.
        /// </summary>
        private static double GetSweepAngleDeg(double[] x, double[] y, double x0, double y0, bool closed)
        {
            if (closed) return 360;
            //
            int n = x.Length;
            double angle;
            double previousAngle = Math.Atan2(y[0] - y0, x[0] - x0);
            double sweep = 0;
            //
            for (int i = 1; i < n; i++)
            {
                angle = Math.Atan2(y[i] - y0, x[i] - x0);
                double delta = angle - previousAngle;
                // Unwrap
                while (delta > Math.PI) delta -= 2 * Math.PI;
                while (delta < -Math.PI) delta += 2 * Math.PI;
                //
                sweep += delta;
                previousAngle = angle;
            }
            //
            return Math.Abs(sweep) * 180 / Math.PI;
        }
        //
        private static bool IsAccepted(double residual, double scale, double noiseFloor, double relTol)
        {
            // A fit must be inside the absolute tolerance and it must not be much worse than the noise
            // floor of the mesh. The second test is what separates a short spline - which a conic fits
            // well - from a true conic. Without a noise floor only the absolute tolerance is used.
            return residual <= relTol * scale &&
                   residual <= Math.Max(3 * noiseFloor, 0.1 * relTol * scale);
        }
        private static bool FitCircle(double[] x, double[] y, double scale, out double x0, out double y0,
                                      out double radius, out double residual)
        {
            x0 = 0;
            y0 = 0;
            radius = 0;
            residual = 0;
            //
            int n = x.Length;
            if (n < 3 || scale <= 0) return false;
            // Algebraic (Kasa) fit: x^2 + y^2 + D x + E y + F = 0 - solved in the scaled coordinates
            double xs;
            double ys;
            double zs;
            double[] rhs = new double[3];
            double[][] m = GetZeroMatrix(3);
            //
            for (int i = 0; i < n; i++)
            {
                xs = x[i] / scale;
                ys = y[i] / scale;
                zs = xs * xs + ys * ys;
                //
                m[0][0] += xs * xs;
                m[0][1] += xs * ys;
                m[0][2] += xs;
                m[1][1] += ys * ys;
                m[1][2] += ys;
                m[2][2] += 1;
                //
                rhs[0] -= zs * xs;
                rhs[1] -= zs * ys;
                rhs[2] -= zs;
            }
            m[1][0] = m[0][1];
            m[2][0] = m[0][2];
            m[2][1] = m[1][2];
            //
            double[] c;
            if (!SmallMatrixTools.Solve(m, rhs, out c)) return false;
            //
            x0 = -c[0] / 2;
            y0 = -c[1] / 2;
            double r2 = x0 * x0 + y0 * y0 - c[2];
            if (r2 <= 0) return false;
            radius = Math.Sqrt(r2);
            // Back to the real size
            x0 *= scale;
            y0 *= scale;
            radius *= scale;
            // The distance from the fitted circle
            double dr;
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                dr = Math.Sqrt((x[i] - x0) * (x[i] - x0) + (y[i] - y0) * (y[i] - y0)) - radius;
                sum += dr * dr;
            }
            residual = Math.Sqrt(sum / n);
            //
            return true;
        }
        private static double[][] GetPointCovariance(double[][] points, double[] centroid)
        {
            double[] d = new double[3];
            double[][] m = GetZeroMatrix(3);
            //
            for (int i = 0; i < points.Length; i++)
            {
                d[0] = points[i][0] - centroid[0];
                d[1] = points[i][1] - centroid[1];
                d[2] = points[i][2] - centroid[2];
                //
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++) m[j][k] += d[j] * d[k];
                }
            }
            return m;
        }
        //
        private static void AddPoint(List<double[]> pointList, HashSet<int> usedNodeIds,
                                     Dictionary<int, FeNode> nodes, int nodeId)
        {
            FeNode node = nodes[nodeId];
            if (usedNodeIds.Add(nodeId)) pointList.Add(new double[] { node.X, node.Y, node.Z });
        }
        private static double[] ToGlobal(double[] origin, double[] u, double[] v, double x, double y)
        {
            return new double[] { origin[0] + x * u[0] + y * v[0],
                                  origin[1] + x * u[1] + y * v[1],
                                  origin[2] + x * u[2] + y * v[2] };
        }
        private static double[][] GetZeroMatrix(int n)
        {
            double[][] m = new double[n][];
            for (int i = 0; i < n; i++) m[i] = new double[n];
            return m;
        }
        private static double[] Direction(double[] p1, double[] p2)
        {
            double[] d = new double[] { p2[0] - p1[0], p2[1] - p1[1], p2[2] - p1[2] };
            double length = Math.Sqrt(Dot(d, d));
            if (length > 0)
            {
                d[0] /= length;
                d[1] /= length;
                d[2] /= length;
            }
            return d;
        }
        private static double Dot(double[] u, double[] v)
        {
            return u[0] * v[0] + u[1] * v[1] + u[2] * v[2];
        }
        private static double Distance(double[] u, double[] v)
        {
            double dx = u[0] - v[0];
            double dy = u[1] - v[1];
            double dz = u[2] - v[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
