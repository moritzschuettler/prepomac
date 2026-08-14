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
    /// Mesh based recognition of the analytic type of the visualization faces:
    /// plane, cylinder, cone, sphere, torus, surface of revolution and surface of extrusion.
    ///
    /// The recognition is discrete and does not need the CAD geometry. It is used for the meshed
    /// parts where the mapping to the geometry faces does not exist any more.
    ///
    /// Only the corner nodes of the visualization cells are used since the mid-side nodes of the
    /// parabolic elements do not necessarily lie on the underlying geometry.
    ///
    /// The algorithm - one pass over the face cells followed by closed form fits. Each step is only
    /// reached when the previous one is rejected, so the common faces (planes) are the cheapest:
    ///
    /// 1 ... Plane - from the eigenvalues of the covariance matrix of the face nodes.
    /// 2 ... Cylinder and cone - the cell normals of both lie in a plane in the direction space, so
    ///       the candidate axis direction is the eigenvector of the covariance matrix of the centered
    ///       cell normals belonging to the smallest eigenvalue.
    /// 3 ... Sphere - an algebraic fit which needs no axis.
    /// 4 ... Torus and a general surface of revolution - the axis direction is recognized from the
    ///       fact that all the surface normals intersect the axis. Two lines intersect when their
    ///       Pluecker product vanishes, which gives a homogeneous 6 x 6 eigenvalue problem.
    /// 5 ... Surface of extrusion - all the cell normals are perpendicular to the rulings.
    ///
    /// For a candidate axis direction the position of the axis is a linear least squares problem
    /// (p_i - q) . (n_i x a) = 0. For a cylinder and a cone the position is then refined by an
    /// algebraic fit which uses the node positions only. The face nodes are mapped to the meridian
    /// plane (z, r) of the axis where
    ///       r = const                      -> cylinder
    ///       r = alpha * z + beta           -> cone
    ///       a circle offset from the axis  -> torus
    ///       a thin band                    -> a general surface of revolution
    ///
    /// A type is only accepted when the deviation of the face NODES from the recognized surface is
    /// small. The nodes lie on the underlying geometry - in contrast to the cell normals, which are
    /// affected by the faceting - so this test is independent of the mesh density. A fit is also
    /// rejected when it is much worse than the width of the meridian band, which is the noise floor
    /// of the mesh: a shallow torus patch (a fillet) is close to a cylinder and a parabolic meridian
    /// is close to a circle, and both must be told apart.
    /// </summary>
    public static class FaceTypeRecognition
    {
        // Variables
        /// <summary>
        /// Recognize the face types each time the part visualization is extracted
        /// </summary>
        public static bool AutoDetect = true;
        /// <summary>
        /// The maximum rms distance of the face nodes from the fitted plane, relative to the face extent
        /// </summary>
        public static double PlaneRelTol = 1E-3;
        /// <summary>
        /// The maximum rms distance of the face nodes from the fitted curved surface, relative to its radius
        /// </summary>
        public static double FitRelTol = 5E-3;
        /// <summary>
        /// The maximum ratio of the smallest to the largest eigenvalue of the covariance matrix of the
        /// centered cell normals for the normals to be considered coplanar (cylinder, cone, extrusion)
        /// </summary>
        public static double NormalRankRelTol = 0.02;
        /// <summary>
        /// The maximum half angle of a cone to be recognized as a cylinder
        /// </summary>
        public static double CylinderAngleTolDeg = 0.5;
        /// <summary>
        /// The maximum distance of the meridian circle center from the axis, relative to the circle
        /// radius, for a torus to be recognized as a sphere
        /// </summary>
        public static double SphereOffAxisRelTol = 0.05;
        /// <summary>
        /// The maximum rms width of the meridian band, relative to the face extent, for a general
        /// surface of revolution
        /// </summary>
        public static double RevolutionBandRelTol = 5E-3;
        //
        private const double _tiny = 1E-30;


        // Methods
        /// <summary>
        /// Recognizes the types of all visualization faces. The optional faceData array must be of the
        /// same length as the number of the faces and is filled with the recognized surface parameters.
        /// </summary>
        public static GeomFaceType[] GetFaceTypes(VisualizationData visualization, Dictionary<int, FeNode> nodes,
                                                  GeomFaceData[] faceData = null)
        {
            if (visualization == null || visualization.CellIdsByFace == null || visualization.Cells == null ||
                nodes == null) return null;
            //
            int numOfFaces = visualization.CellIdsByFace.Length;
            GeomFaceType[] faceTypes = new GeomFaceType[numOfFaces];
            //
            Parallel.For(0, numOfFaces, faceId =>
            {
                GeomFaceData data;
                // The recognition must never break the extraction of the visualization
                try { data = GetFaceData(visualization, nodes, faceId); }
                catch { data = new GeomFaceData(); }
                //
                faceTypes[faceId] = data.Type;
                if (faceData != null) faceData[faceId] = data;
            });
            //
            return faceTypes;
        }
        /// <summary>
        /// Recognizes the type and the parameters of a single visualization face.
        /// </summary>
        public static GeomFaceData GetFaceData(VisualizationData visualization, Dictionary<int, FeNode> nodes, int faceId)
        {
            GeomFaceData faceData = new GeomFaceData();
            //
            if (visualization == null || visualization.CellIdsByFace == null || visualization.Cells == null ||
                nodes == null || faceId < 0 || faceId >= visualization.CellIdsByFace.Length) return faceData;
            //
            int[] faceCellIds = visualization.CellIdsByFace[faceId];
            if (faceCellIds == null || faceCellIds.Length == 0) return faceData;
            // Collect the cell and the node data - only the corner nodes are used
            int numOfCells = faceCellIds.Length;
            double totalArea = 0;
            double[] cellAreas = new double[numOfCells];
            double[][] cellNormals = new double[numOfCells][];
            //
            int index;
            int numOfCorners;
            int[] cell;
            double area;
            double[] normal;
            double[] nodeNormal;
            double[][] cellCoor = new double[4][];
            Dictionary<int, int> nodeIdIndex = new Dictionary<int, int>(numOfCells * 3);
            List<double[]> pointList = new List<double[]>(numOfCells * 3);
            List<double[]> normalList = new List<double[]>(numOfCells * 3);
            //
            for (int i = 0; i < numOfCells; i++)
            {
                cell = visualization.Cells[faceCellIds[i]];
                numOfCorners = GetNumOfCorners(cell.Length);
                if (numOfCorners == 0) return faceData;     // unsupported cell type
                //
                for (int j = 0; j < numOfCorners; j++)
                {
                    FeNode node = nodes[cell[j]];
                    cellCoor[j] = new double[] { node.X, node.Y, node.Z };
                }
                //
                area = GetCellAreaAndNormal(cellCoor, numOfCorners, out normal);
                cellAreas[i] = area;
                cellNormals[i] = normal;
                totalArea += area;
                // Area weighted nodal normals
                for (int j = 0; j < numOfCorners; j++)
                {
                    if (!nodeIdIndex.TryGetValue(cell[j], out index))
                    {
                        index = pointList.Count;
                        nodeIdIndex.Add(cell[j], index);
                        pointList.Add(cellCoor[j]);
                        normalList.Add(new double[3]);
                    }
                    nodeNormal = normalList[index];
                    nodeNormal[0] += normal[0] * area;
                    nodeNormal[1] += normal[1] * area;
                    nodeNormal[2] += normal[2] * area;
                }
            }
            //
            int numOfNodes = pointList.Count;
            faceData.CellCount = numOfCells;
            faceData.NodeCount = numOfNodes;
            faceData.Size = Math.Sqrt(totalArea);
            //
            if (numOfNodes < 3 || totalArea <= 0) return faceData;
            //
            double[][] points = pointList.ToArray();
            double[][] nodeNormals = normalList.ToArray();
            for (int i = 0; i < numOfNodes; i++) Normalize(nodeNormals[i]);
            // The mean cell normal is used to orient the results
            double[] meanCellNormal = new double[3];
            for (int i = 0; i < numOfCells; i++)
            {
                meanCellNormal[0] += cellNormals[i][0] * cellAreas[i];
                meanCellNormal[1] += cellNormals[i][1] * cellAreas[i];
                meanCellNormal[2] += cellNormals[i][2] * cellAreas[i];
            }
            Normalize(meanCellNormal);
            // The centroid and the extent of the face
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
            if (extent <= 0) return faceData;
            // 1 Plane
            double[] eigenValues;
            double[][] eigenVectors;
            double[][] covariance = GetPointCovariance(points, centroid);
            if (SmallMatrixTools.SymmetricEigen(covariance, out eigenValues, out eigenVectors))
            {
                double rms = Math.Sqrt(Math.Max(eigenValues[2], 0) / numOfNodes);
                if (rms <= PlaneRelTol * extent)
                {
                    faceData.Type = GeomFaceType.Plane;
                    faceData.Point = centroid;
                    faceData.Axis = Oriented(eigenVectors[2], meanCellNormal);
                    faceData.Residual = rms / extent;
                    return faceData;
                }
            }
            // 2 Cylinder and cone - the cell normals are coplanar
            bool coplanarNormals = false;
            double[] extrusionDirection = null;
            double normalResidual = 0;
            double[][] normalCovariance = GetNormalCovariance(cellNormals, cellAreas, totalArea);
            if (SmallMatrixTools.SymmetricEigen(normalCovariance, out eigenValues, out eigenVectors) &&
                eigenValues[0] > _tiny &&
                eigenValues[2] <= NormalRankRelTol * eigenValues[0] &&
                eigenValues[1] > NormalRankRelTol * eigenValues[0])
            {
                coplanarNormals = true;
                extrusionDirection = eigenVectors[2];
                normalResidual = Math.Sqrt(Math.Max(eigenValues[2], 0) / eigenValues[0]);
                //
                if (TryFitRevolution(faceData, points, nodeNormals, extrusionDirection, centroid, extent)) return faceData;
            }
            // 3 Sphere - an algebraic fit which does not need the axis
            if (TryFitSphere(faceData, points, nodeNormals, centroid, extent)) return faceData;
            // 4 Torus and a general surface of revolution
            double[] axisDirection = GetRevolutionAxisDirection(points, nodeNormals, centroid, extent);
            if (axisDirection != null &&
                TryFitRevolution(faceData, points, nodeNormals, axisDirection, centroid, extent)) return faceData;
            // 5 Surface of extrusion - all the cell normals are perpendicular to a single direction
            if (coplanarNormals)
            {
                faceData.Type = GeomFaceType.SurfaceOfExtrusion;
                faceData.Point = centroid;
                faceData.Axis = extrusionDirection;
                faceData.Residual = normalResidual;
                return faceData;
            }
            // 6 Free form
            faceData.Type = GeomFaceType.OtherSurface;
            faceData.Point = centroid;
            //
            return faceData;
        }
        //
        /// <summary>
        /// An algebraic (Kasa) fit of a sphere: |p - c|^2 = R^2 is linear in c and in R^2 - |c|^2.
        /// The axis of revolution is not needed and the fit is exact for a spherical face.
        /// </summary>
        private static bool TryFitSphere(GeomFaceData faceData, double[][] points, double[][] normals,
                                         double[] centroid, double extent)
        {
            int numOfNodes = points.Length;
            if (numOfNodes < 4) return false;
            //
            double value;
            double[] d = new double[3];
            double[] row = new double[4];
            double[] rhs = new double[4];
            double[][] m = GetZeroMatrix(4);
            //
            for (int i = 0; i < numOfNodes; i++)
            {
                d[0] = (points[i][0] - centroid[0]) / extent;
                d[1] = (points[i][1] - centroid[1]) / extent;
                d[2] = (points[i][2] - centroid[2]) / extent;
                //
                row[0] = 2 * d[0];
                row[1] = 2 * d[1];
                row[2] = 2 * d[2];
                row[3] = 1;
                value = Dot(d, d);
                //
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++) m[j][k] += row[j] * row[k];
                    rhs[j] += row[j] * value;
                }
            }
            //
            double[] c;
            if (!SmallMatrixTools.Solve(m, rhs, out c)) return false;
            //
            double radius2 = c[3] + c[0] * c[0] + c[1] * c[1] + c[2] * c[2];
            if (radius2 <= 0) return false;
            //
            double radius = Math.Sqrt(radius2) * extent;
            double[] center = new double[] { centroid[0] + c[0] * extent,
                                             centroid[1] + c[1] * extent,
                                             centroid[2] + c[2] * extent };
            // The distance from the fitted sphere
            double dr;
            double sum = 0;
            for (int i = 0; i < numOfNodes; i++)
            {
                dr = Distance(points[i], center) - radius;
                sum += dr * dr;
            }
            double residual = Math.Sqrt(sum / numOfNodes);
            if (residual > FitRelTol * Math.Min(radius, extent)) return false;
            //
            faceData.Type = GeomFaceType.Sphere;
            faceData.Point = center;
            faceData.Axis = null;               // a sphere has no axis
            faceData.Radius = radius;
            faceData.Residual = residual / radius;
            faceData.Convex = IsConvexAroundPoint(points, normals, center);
            //
            return true;
        }
        private static bool TryFitRevolution(GeomFaceData faceData, double[][] points, double[][] normals,
                                             double[] axisDirection, double[] centroid, double extent)
        {
            double[] a = new double[] { axisDirection[0], axisDirection[1], axisDirection[2] };
            if (Normalize(a) <= 0) return false;
            // The position of the axis
            double[] q = GetAxisPoint(points, normals, a, centroid);
            if (q == null) return false;
            //
            int numOfNodes = points.Length;
            double[] z = new double[numOfNodes];
            double[] r = new double[numOfNodes];
            double meanR;
            double zMin;
            double zMax;
            double bandWidth;
            // 1 Cylinder and cone
            // The position of the axis is refined by an algebraic fit of a cone with a known axis
            // direction. The refined position is computed from the node positions only and it is exact
            // for a cylinder and for a cone - in contrast to the position computed from the cell
            // normals which are affected by the faceting of the mesh.
            double[] qCone = GetRefinedAxisPointForCone(points, a, q, extent);
            GetMeridianCoordinates(points, qCone, a, z, r, out meanR, out zMin, out zMax);
            if (meanR > _tiny)
            {
                bandWidth = GetMeridianBandWidth(z, r, extent);
                // Cylinder - the radius is constant
                // This is tested before the line fit which is unstable for the faces with a small axial extent
                double dr;
                double sum = 0;
                for (int i = 0; i < numOfNodes; i++)
                {
                    dr = r[i] - meanR;
                    sum += dr * dr;
                }
                double radiusResidual = Math.Sqrt(sum / numOfNodes);
                if (IsAccepted(radiusResidual, meanR, bandWidth))
                {
                    faceData.Type = GeomFaceType.Cylinder;
                    faceData.Axis = a;
                    faceData.Point = PointOnAxisClosestTo(qCone, a, centroid);
                    faceData.Radius = meanR;
                    faceData.Residual = radiusResidual / meanR;
                    faceData.Convex = IsConvexAroundAxis(points, normals, qCone, a);
                    return true;
                }
                // Cone - the meridian is a straight line
                double alpha;
                double beta;         // the radius at the axis point qCone
                double lineResidual;
                if (zMax - zMin > 1E-6 * extent && FitLine(z, r, out alpha, out beta, out lineResidual) &&
                    IsAccepted(lineResidual, meanR, bandWidth))
                {
                    // Orient the axis in the direction of the increasing radius
                    if (alpha < 0)
                    {
                        for (int i = 0; i < 3; i++) a[i] = -a[i];
                        alpha = -alpha;
                    }
                    //
                    if (alpha <= Math.Tan(CylinderAngleTolDeg * Math.PI / 180))
                    {
                        // A cone with a negligible half angle
                        faceData.Type = GeomFaceType.Cylinder;
                        faceData.Axis = a;
                        faceData.Point = PointOnAxisClosestTo(qCone, a, centroid);
                        faceData.Radius = meanR;
                        faceData.Residual = lineResidual / meanR;
                    }
                    else
                    {
                        faceData.Type = GeomFaceType.Cone;
                        faceData.Axis = a;
                        faceData.Point = new double[] { qCone[0] - beta / alpha * a[0],
                                                        qCone[1] - beta / alpha * a[1],
                                                        qCone[2] - beta / alpha * a[2] };   // the apex
                        faceData.HalfAngleDeg = Math.Atan(alpha) * 180 / Math.PI;
                        faceData.Radius = meanR;
                        faceData.Residual = lineResidual / meanR;
                    }
                    faceData.Convex = IsConvexAroundAxis(points, normals, qCone, a);
                    return true;
                }
            }
            // 2 Sphere, torus and a general surface of revolution - around the axis from the cell normals
            GetMeridianCoordinates(points, q, a, z, r, out meanR, out zMin, out zMax);
            bandWidth = GetMeridianBandWidth(z, r, extent);
            // The meridian is a circle
            double z0;
            double r0;
            double rho;
            double circleResidual;
            if (FitCircle(z, r, extent, out z0, out r0, out rho, out circleResidual) && rho > _tiny &&
                IsAccepted(circleResidual, Math.Min(rho, extent), bandWidth))
            {
                double[] center = new double[] { q[0] + z0 * a[0], q[1] + z0 * a[1], q[2] + z0 * a[2] };
                //
                if (Math.Abs(r0) <= SphereOffAxisRelTol * rho)
                {
                    // Sphere
                    faceData.Type = GeomFaceType.Sphere;
                    faceData.Axis = null;       // a sphere has no axis
                    faceData.Point = center;
                    faceData.Radius = rho;
                    faceData.Residual = circleResidual / rho;
                    faceData.Convex = IsConvexAroundPoint(points, normals, center);
                }
                else
                {
                    // Torus
                    if (r0 < 0)
                    {
                        for (int i = 0; i < 3; i++) a[i] = -a[i];
                        r0 = -r0;
                    }
                    faceData.Type = GeomFaceType.Torus;
                    faceData.Axis = a;
                    faceData.Point = center;
                    faceData.Radius = r0;       // major radius
                    faceData.MinorRadius = rho; // minor radius
                    faceData.Residual = circleResidual / rho;
                    faceData.Convex = IsConvexAroundTorusTube(points, normals, center, a, r0);
                }
                return true;
            }
            // The meridian points collapse onto a thin band
            if (bandWidth <= RevolutionBandRelTol * extent)
            {
                faceData.Type = GeomFaceType.SurfaceOfRevolution;
                faceData.Axis = a;
                faceData.Point = PointOnAxisClosestTo(q, a, centroid);
                faceData.Residual = bandWidth / extent;
                faceData.Convex = IsConvexAroundAxis(points, normals, q, a);
                return true;
            }
            //
            return false;
        }
        /// <summary>
        /// The direction of the axis of a surface of revolution. All the surface normals intersect the
        /// axis: for the axis line (a, m) and the normal lines (n_i, q_i = p_i x n_i) in the Pluecker
        /// coordinates this reads q_i . a + n_i . m = 0. The axis is the null space vector of the
        /// accumulated 6 x 6 matrix.
        /// </summary>
        private static double[] GetRevolutionAxisDirection(double[][] points, double[][] normals,
                                                           double[] centroid, double extent)
        {
            int numOfNodes = points.Length;
            double[] v = new double[6];
            double[] p = new double[3];
            double[][] m = GetZeroMatrix(6);
            //
            for (int i = 0; i < numOfNodes; i++)
            {
                // Scaled coordinates keep the matrix well conditioned
                p[0] = (points[i][0] - centroid[0]) / extent;
                p[1] = (points[i][1] - centroid[1]) / extent;
                p[2] = (points[i][2] - centroid[2]) / extent;
                //
                v[0] = p[1] * normals[i][2] - p[2] * normals[i][1];
                v[1] = p[2] * normals[i][0] - p[0] * normals[i][2];
                v[2] = p[0] * normals[i][1] - p[1] * normals[i][0];
                v[3] = normals[i][0];
                v[4] = normals[i][1];
                v[5] = normals[i][2];
                //
                for (int j = 0; j < 6; j++)
                {
                    for (int k = j; k < 6; k++) m[j][k] += v[j] * v[k];
                }
            }
            for (int j = 0; j < 6; j++)
            {
                for (int k = 0; k < j; k++) m[j][k] = m[k][j];
            }
            //
            double[] eigenValues;
            double[][] eigenVectors;
            if (!SmallMatrixTools.SymmetricEigen(m, out eigenValues, out eigenVectors)) return null;
            //
            double[] axis = new double[] { eigenVectors[5][0], eigenVectors[5][1], eigenVectors[5][2] };
            // A vector with a vanishing direction part is a line at infinity
            if (Normalize(axis) < 1E-2) return null;
            //
            return axis;
        }
        /// <summary>
        /// The position of the axis for a known axis direction. The normal of a surface of revolution
        /// intersects the axis: (p_i - q) . (n_i x a) = 0 - a linear least squares problem for q.
        /// The component of q along the axis is fixed by the centroid.
        /// </summary>
        private static double[] GetAxisPoint(double[][] points, double[][] normals, double[] a, double[] centroid)
        {
            int count = 0;
            double kp;
            double[] k = new double[3];
            double[] rhs = new double[3];
            double[][] m = GetZeroMatrix(3);
            //
            for (int i = 0; i < points.Length; i++)
            {
                // k = n x a - its length is the sine of the angle between the normal and the axis and
                // it weights the equation: normals parallel to the axis carry no information
                k[0] = normals[i][1] * a[2] - normals[i][2] * a[1];
                k[1] = normals[i][2] * a[0] - normals[i][0] * a[2];
                k[2] = normals[i][0] * a[1] - normals[i][1] * a[0];
                //
                if (Math.Abs(k[0]) + Math.Abs(k[1]) + Math.Abs(k[2]) < 1E-8) continue;
                //
                kp = Dot(k, points[i]);
                for (int j = 0; j < 3; j++)
                {
                    for (int l = 0; l < 3; l++) m[j][l] += k[j] * k[l];
                    rhs[j] += k[j] * kp;
                }
                count++;
            }
            if (count < 3) return null;
            // Fix the component along the axis - the matrix is singular in this direction
            double scale = (m[0][0] + m[1][1] + m[2][2]) / 3;
            if (scale <= _tiny) return null;
            double ac = Dot(a, centroid);
            for (int j = 0; j < 3; j++)
            {
                for (int l = 0; l < 3; l++) m[j][l] += scale * a[j] * a[l];
                rhs[j] += scale * a[j] * ac;
            }
            //
            double[] q;
            if (!SmallMatrixTools.Solve(m, rhs, out q)) return null;
            //
            return q;
        }
        /// <summary>
        /// Refines the position of the axis for a known axis direction by an algebraic fit of a cone:
        /// x^2 + y^2 + D x + E y + F - G z^2 - H z = 0, where x, y, z are the coordinates of the face
        /// nodes in the local frame of the axis. The fit is linear and it is exact both for a cylinder
        /// (G = H = 0) and for a cone. Only the node positions are used, so the result is not affected
        /// by the faceting of the mesh. The axis is moved by (-D/2, -E/2) in the plane perpendicular
        /// to the axis.
        /// </summary>
        private static double[] GetRefinedAxisPointForCone(double[][] points, double[] a, double[] q, double extent)
        {
            double[] u;
            double[] v;
            GetPerpendicularBasis(a, out u, out v);
            //
            double x;
            double y;
            double zc;
            double value;
            double[] d = new double[3];
            double[] row = new double[5];
            double[] rhs = new double[5];
            double[][] m = GetZeroMatrix(5);
            //
            for (int i = 0; i < points.Length; i++)
            {
                d[0] = (points[i][0] - q[0]) / extent;
                d[1] = (points[i][1] - q[1]) / extent;
                d[2] = (points[i][2] - q[2]) / extent;
                x = Dot(d, u);
                y = Dot(d, v);
                zc = Dot(d, a);
                //
                row[0] = x;
                row[1] = y;
                row[2] = 1;
                row[3] = -zc * zc;
                row[4] = -zc;
                value = -(x * x + y * y);
                //
                for (int j = 0; j < 5; j++)
                {
                    for (int k = 0; k < 5; k++) m[j][k] += row[j] * row[k];
                    rhs[j] += row[j] * value;
                }
            }
            //
            double[] c;
            if (!SmallMatrixTools.Solve(m, rhs, out c)) return q;
            //
            double cx = -c[0] / 2 * extent;
            double cy = -c[1] / 2 * extent;
            if (double.IsNaN(cx) || double.IsNaN(cy) ||
                Math.Abs(cx) + Math.Abs(cy) > 1E4 * extent) return q;      // an unrealistic offset
            //
            return new double[] { q[0] + cx * u[0] + cy * v[0],
                                  q[1] + cx * u[1] + cy * v[1],
                                  q[2] + cx * u[2] + cy * v[2] };
        }
        private static void GetMeridianCoordinates(double[][] points, double[] q, double[] a, double[] z, double[] r,
                                                   out double meanR, out double zMin, out double zMax)
        {
            meanR = 0;
            zMin = double.MaxValue;
            zMax = -double.MaxValue;
            double[] d = new double[3];
            //
            for (int i = 0; i < points.Length; i++)
            {
                d[0] = points[i][0] - q[0];
                d[1] = points[i][1] - q[1];
                d[2] = points[i][2] - q[2];
                z[i] = Dot(d, a);
                r[i] = Math.Sqrt(Math.Max(Dot(d, d) - z[i] * z[i], 0));
                meanR += r[i];
                zMin = Math.Min(zMin, z[i]);
                zMax = Math.Max(zMax, z[i]);
            }
            meanR /= points.Length;
        }
        /// <summary>
        /// The band width is the deviation of the face nodes from the best possible meridian curve - the
        /// noise floor of the mesh. A fit which is much worse than the noise floor is rejected even when
        /// it lies inside the absolute tolerance: a shallow torus patch (a fillet) is close to a cylinder
        /// and a parabolic meridian is close to a circle.
        /// </summary>
        private static bool IsAccepted(double residual, double scale, double bandWidth)
        {
            // The band width is an upper bound of the noise of the mesh and not a noise estimate - the
            // meridian of a torus is double valued, which makes the band as wide as the tube. It can
            // therefore only reject a fit and never relax the absolute tolerance.
            return residual <= FitRelTol * scale &&
                   residual <= Math.Max(3 * bandWidth, 0.1 * FitRelTol * scale);
        }
        //
        private static bool FitLine(double[] x, double[] y, out double alpha, out double beta, out double residual)
        {
            alpha = 0;
            beta = 0;
            residual = 0;
            //
            int n = x.Length;
            double sx = 0;
            double sy = 0;
            double sxx = 0;
            double sxy = 0;
            for (int i = 0; i < n; i++)
            {
                sx += x[i];
                sy += y[i];
                sxx += x[i] * x[i];
                sxy += x[i] * y[i];
            }
            //
            double den = n * sxx - sx * sx;
            if (Math.Abs(den) <= _tiny) return false;
            //
            alpha = (n * sxy - sx * sy) / den;
            beta = (sy - alpha * sx) / n;
            // The perpendicular distance from the fitted line
            double dy;
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                dy = y[i] - alpha * x[i] - beta;
                sum += dy * dy;
            }
            residual = Math.Sqrt(sum / n) / Math.Sqrt(1 + alpha * alpha);
            //
            return true;
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
            if (n < 4 || scale <= 0) return false;
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
        /// <summary>
        /// The rms width of the band the meridian points collapse onto. The points are binned by the
        /// axial coordinate and inside each bin a straight line is subtracted to account for the slope
        /// of the meridian curve. Returns double.MaxValue when the width cannot be computed.
        /// </summary>
        private static double GetMeridianBandWidth(double[] z, double[] r, double extent)
        {
            int n = z.Length;
            if (n < 12) return double.MaxValue;
            //
            double zMin = double.MaxValue;
            double zMax = -double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                zMin = Math.Min(zMin, z[i]);
                zMax = Math.Max(zMax, z[i]);
            }
            if (zMax - zMin <= 1E-6 * extent) return double.MaxValue;
            //
            int numOfBins = Math.Max(3, n / 10);
            int[] count = new int[numOfBins];
            double[] sx = new double[numOfBins];
            double[] sy = new double[numOfBins];
            double[] sxx = new double[numOfBins];
            double[] sxy = new double[numOfBins];
            double[] syy = new double[numOfBins];
            //
            int bin;
            double f = numOfBins / (zMax - zMin);
            for (int i = 0; i < n; i++)
            {
                bin = (int)((z[i] - zMin) * f);
                if (bin < 0) bin = 0;
                else if (bin >= numOfBins) bin = numOfBins - 1;
                //
                count[bin]++;
                sx[bin] += z[i];
                sy[bin] += r[i];
                sxx[bin] += z[i] * z[i];
                sxy[bin] += z[i] * r[i];
                syy[bin] += r[i] * r[i];
            }
            // The sum of the squared residuals of a straight line inside each bin
            int dof = 0;
            double den;
            double alpha;
            double beta;
            double sum = 0;
            int usedBins = 0;
            for (int i = 0; i < numOfBins; i++)
            {
                if (count[i] < 4) continue;
                //
                den = count[i] * sxx[i] - sx[i] * sx[i];
                if (Math.Abs(den) <= _tiny) continue;
                //
                alpha = (count[i] * sxy[i] - sx[i] * sy[i]) / den;
                beta = (sy[i] - alpha * sx[i]) / count[i];
                // sum((y - alpha x - beta)^2) computed from the accumulated sums
                sum += syy[i] + alpha * alpha * sxx[i] + count[i] * beta * beta
                       - 2 * alpha * sxy[i] - 2 * beta * sy[i] + 2 * alpha * beta * sx[i];
                dof += count[i] - 2;
                usedBins++;
            }
            if (usedBins < 3 || dof <= 0) return double.MaxValue;
            //
            return Math.Sqrt(Math.Max(sum, 0) / dof);
        }
        //
        private static bool IsConvexAroundAxis(double[][] points, double[][] normals, double[] q, double[] a)
        {
            double zi;
            double sum = 0;
            double[] d = new double[3];
            //
            for (int i = 0; i < points.Length; i++)
            {
                d[0] = points[i][0] - q[0];
                d[1] = points[i][1] - q[1];
                d[2] = points[i][2] - q[2];
                zi = Dot(d, a);
                d[0] -= zi * a[0];
                d[1] -= zi * a[1];
                d[2] -= zi * a[2];
                //
                if (Normalize(d) <= 0) continue;
                sum += Dot(d, normals[i]);
            }
            return sum >= 0;
        }
        private static bool IsConvexAroundPoint(double[][] points, double[][] normals, double[] center)
        {
            double sum = 0;
            double[] d = new double[3];
            //
            for (int i = 0; i < points.Length; i++)
            {
                d[0] = points[i][0] - center[0];
                d[1] = points[i][1] - center[1];
                d[2] = points[i][2] - center[2];
                //
                if (Normalize(d) <= 0) continue;
                sum += Dot(d, normals[i]);
            }
            return sum >= 0;
        }
        private static bool IsConvexAroundTorusTube(double[][] points, double[][] normals, double[] center,
                                                    double[] a, double majorRadius)
        {
            double zi;
            double sum = 0;
            double[] d = new double[3];
            double[] radial = new double[3];
            //
            for (int i = 0; i < points.Length; i++)
            {
                d[0] = points[i][0] - center[0];
                d[1] = points[i][1] - center[1];
                d[2] = points[i][2] - center[2];
                zi = Dot(d, a);
                radial[0] = d[0] - zi * a[0];
                radial[1] = d[1] - zi * a[1];
                radial[2] = d[2] - zi * a[2];
                //
                if (Normalize(radial) <= 0) continue;
                // The vector from the center of the tube to the point
                d[0] -= majorRadius * radial[0];
                d[1] -= majorRadius * radial[1];
                d[2] -= majorRadius * radial[2];
                //
                if (Normalize(d) <= 0) continue;
                sum += Dot(d, normals[i]);
            }
            return sum >= 0;
        }
        //
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
        private static double[][] GetNormalCovariance(double[][] normals, double[] areas, double totalArea)
        {
            double[] mean = new double[3];
            for (int i = 0; i < normals.Length; i++)
            {
                mean[0] += normals[i][0] * areas[i];
                mean[1] += normals[i][1] * areas[i];
                mean[2] += normals[i][2] * areas[i];
            }
            mean[0] /= totalArea;
            mean[1] /= totalArea;
            mean[2] /= totalArea;
            //
            double[] d = new double[3];
            double[][] m = GetZeroMatrix(3);
            for (int i = 0; i < normals.Length; i++)
            {
                d[0] = normals[i][0] - mean[0];
                d[1] = normals[i][1] - mean[1];
                d[2] = normals[i][2] - mean[2];
                //
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++) m[j][k] += d[j] * d[k] * areas[i];
                }
            }
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++) m[j][k] /= totalArea;
            }
            //
            return m;
        }
        //
        private static int GetNumOfCorners(int numOfCellNodes)
        {
            // Only the corner nodes are used - the mid-side nodes of the parabolic elements do not
            // necessarily lie on the underlying geometry
            switch (numOfCellNodes)
            {
                case 3:
                case 6:
                    return 3;
                case 4:
                case 8:
                    return 4;
                default:
                    return 0;
            }
        }
        private static double GetCellAreaAndNormal(double[][] coor, int numOfCorners, out double[] normal)
        {
            normal = new double[3];
            //
            AddTriangleNormal(coor[0], coor[1], coor[2], normal);
            if (numOfCorners == 4) AddTriangleNormal(coor[0], coor[2], coor[3], normal);
            //
            double area = 0.5 * Length(normal);
            Normalize(normal);
            //
            return area;
        }
        private static void AddTriangleNormal(double[] p1, double[] p2, double[] p3, double[] normal)
        {
            double ux = p2[0] - p1[0];
            double uy = p2[1] - p1[1];
            double uz = p2[2] - p1[2];
            double vx = p3[0] - p1[0];
            double vy = p3[1] - p1[1];
            double vz = p3[2] - p1[2];
            //
            normal[0] += uy * vz - uz * vy;
            normal[1] += uz * vx - ux * vz;
            normal[2] += ux * vy - uy * vx;
        }
        //
        private static double[][] GetZeroMatrix(int n)
        {
            double[][] m = new double[n][];
            for (int i = 0; i < n; i++) m[i] = new double[n];
            return m;
        }
        private static double[] PointOnAxisClosestTo(double[] q, double[] a, double[] point)
        {
            double t = (point[0] - q[0]) * a[0] + (point[1] - q[1]) * a[1] + (point[2] - q[2]) * a[2];
            return new double[] { q[0] + t * a[0], q[1] + t * a[1], q[2] + t * a[2] };
        }
        private static void GetPerpendicularBasis(double[] a, out double[] u, out double[] v)
        {
            double[] t = Math.Abs(a[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            //
            u = new double[] { a[1] * t[2] - a[2] * t[1], a[2] * t[0] - a[0] * t[2], a[0] * t[1] - a[1] * t[0] };
            Normalize(u);
            //
            v = new double[] { a[1] * u[2] - a[2] * u[1], a[2] * u[0] - a[0] * u[2], a[0] * u[1] - a[1] * u[0] };
            Normalize(v);
        }
        private static double[] Oriented(double[] vector, double[] reference)
        {
            double[] v = new double[] { vector[0], vector[1], vector[2] };
            if (Dot(v, reference) < 0)
            {
                v[0] = -v[0];
                v[1] = -v[1];
                v[2] = -v[2];
            }
            return v;
        }
        private static double Dot(double[] u, double[] v)
        {
            return u[0] * v[0] + u[1] * v[1] + u[2] * v[2];
        }
        private static double Length(double[] v)
        {
            return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        }
        private static double Distance(double[] u, double[] v)
        {
            double dx = u[0] - v[0];
            double dy = u[1] - v[1];
            double dz = u[2] - v[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        private static double Normalize(double[] v)
        {
            double len = Length(v);
            if (len > 0)
            {
                v[0] /= len;
                v[1] /= len;
                v[2] /= len;
            }
            return len;
        }
    }
}
