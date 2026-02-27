using System;
using System.Collections.Generic;

namespace CaeGlobals
{
    public class Vec3D
    {
        // Variables                                                                                                                
        public double X;
        public double Y;
        public double Z;


        // Properties                                                                                                               
        public double Len { get { return Math.Sqrt(X * X + Y * Y + Z * Z); } }
        public double Len2 { get { return X * X + Y * Y + Z * Z; } }
        public double[] Coor
        {
            get { return new double[] { X, Y, Z }; }
            set 
            {
                X = value[0];
                Y = value[1];
                Z = value[2];
            }
        }


        // Constructors                                                                                                             
        public Vec3D()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }
        public Vec3D(double x0, double y0, double z0)
        {
            X = x0;
            Y = y0;
            Z = z0;
        }
        public Vec3D(double[] xyz)
        {
            X = xyz[0];
            Y = xyz[1];
            Z = xyz[2];
        }
        public Vec3D(Vec3D vec)
        {
            X = vec.X;
            Y = vec.Y;
            Z = vec.Z;
        }


        // Methods                                                                                                                  
        public double Normalize()
        {
            double n = Math.Sqrt(X * X + Y * Y + Z * Z);
            if (n > 0 && n != 1)
            {
                X /= n;
                Y /= n;
                Z /= n;
            }
            return n;
        }
        public void Abs()
        {
            if (X < 0) X = -X;
            if (Y < 0) Y = -Y;
            if (Z < 0) Z = -Z;
        }
        public Vec3D DeepCopy()
        {
            return new Vec3D(this);
        }
        public double[] CoorRounded(int numDigits)
        {
            return new double[] { Math.Round(X, numDigits),
                                  Math.Round(Y, numDigits),
                                  Math.Round(Z, numDigits) };
        }

        #region STATIC UTILITIES

        // Copy
        public static void Copy(double[] vec1, ref double[] vec)
        {
            vec[0] = vec1[0];
            vec[1] = vec1[1];
            vec[2] = vec1[2];
        }
        public static void CopyNormalize(double[] vec1, ref double[] vec)
        {
            vec[0] = vec1[0];
            vec[1] = vec1[1];
            vec[2] = vec1[2];
            Vec3D.Normalize(ref vec);
        }

        // Normalize vector
        public static void Normalize(ref double[] vec)
        {
            double n = Math.Sqrt(vec[0] * vec[0] + vec[1] * vec[1] + vec[2] * vec[2]);
            if (n > 0)
            {
                vec[0] = vec[0] / n;
                vec[1] = vec[1] / n;
                vec[2] = vec[2] / n;
            }
        }

        // Get normalized direction vector
        public static void GetNorDirVec(double[] p1, double[] p2, ref double[] vec)
        {
            vec[0] = p2[0] - p1[0];
            vec[1] = p2[1] - p1[1];
            vec[2] = p2[2] - p1[2];
            Vec3D.Normalize(ref vec);
        }
        public static Vec3D GetNorDirVec(double[] p1, double[] p2)
        {
            Vec3D vector = new Vec3D();
            vector.X = p2[0] - p1[0];
            vector.Y = p2[1] - p1[1];
            vector.Z = p2[2] - p1[2];
            vector.Normalize();
            return vector;
        }
        public static Vec3D GetDirVec(double[] p1, double[] p2)
        {
            Vec3D vector = new Vec3D();
            vector.X = p2[0] - p1[0];
            vector.Y = p2[1] - p1[1];
            vector.Z = p2[2] - p1[2];
            return vector;
        }

        // Add
        public static Vec3D operator +(Vec3D v1, Vec3D v2)
        {
            return new Vec3D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }
        // Subtract
        public static Vec3D operator -(Vec3D v1, Vec3D v2)
        {
            return new Vec3D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }
        // Multiply
        public static Vec3D operator *(double a, Vec3D v)
        {
            return new Vec3D(a * v.X, a * v.Y, a * v.Z);
        }
        public static Vec3D operator *(Vec3D v, double a)
        {
            return new Vec3D(a * v.X, a * v.Y, a * v.Z);
        }
        // Cross product
        public static void CrossProduct(Vec3D u, Vec3D v, ref Vec3D vec)
        {
            vec.X = u.Y * v.Z - v.Y * u.Z;
            vec.Y = v.X * u.Z - u.X * v.Z;
            vec.Z = u.X * v.Y - v.X * u.Y;
        }
        public static Vec3D CrossProduct(Vec3D u, Vec3D v)
        {
            Vec3D vector = new Vec3D();
            vector.X = u.Y * v.Z - v.Y * u.Z;
            vector.Y = v.X * u.Z - u.X * v.Z;
            vector.Z = u.X * v.Y - v.X * u.Y;
            return vector;
        }
        public static void CrossProduct(double[] u, double[] v, ref double[] vec)
        {
            vec[0] = u[1] * v[2] - v[1] * u[2];
            vec[1] = v[0] * u[2] - u[0] * v[2];
            vec[2] = u[0] * v[1] - v[0] * u[1];
        }

        // Dot product
        public static double DotProduct(Vec3D u, Vec3D v)
        {
            return (u.X * v.X + u.Y * v.Y + u.Z * v.Z);
        }

        // Angle
        public static double GetAngleAtP2Deg(Vec3D p1, Vec3D p2, Vec3D p3)
        {
            Vec3D v1 = p1 - p2;
            Vec3D v2 = p3 - p2;
            v1.Normalize();
            v2.Normalize();
            //
            double dot = Vec3D.DotProduct(v1, v2);
            if (dot > 1) dot = 1;
            else if (dot < -1) dot = -1;
            double angle = Math.Acos(dot);
            // Convert the angle from radians to degrees
            angle *= (180.0 / Math.PI);
            //
            return angle;
        }

        // Circle
        public static void GetCircle(Vec3D baseV1, Vec3D baseV2, Vec3D baseV3, out double r, out Vec3D center, out Vec3D axis)
        {
            Vec3D n12 = baseV1 - baseV2;
            Vec3D n21 = baseV2 - baseV1;
            Vec3D n23 = baseV2 - baseV3;
            Vec3D n32 = baseV3 - baseV2;
            Vec3D n13 = baseV1 - baseV3;
            Vec3D n31 = baseV3 - baseV1;
            Vec3D n12xn23 = CrossProduct(n12, n23);
            //
            r = (n12.Len * n23.Len * n31.Len) / (2 * n12xn23.Len);
            //
            double denominator = 2 * n12xn23.Len2;
            double alpha = n23.Len2 * DotProduct(n12, n13) / denominator;
            double beta = n13.Len2 * DotProduct(n21, n23) / denominator;
            double gama = n12.Len2 * DotProduct(n31, n32) / denominator;
            //
            center = alpha * baseV1 + beta * baseV2 + gama * baseV3;
            //
            axis = CrossProduct(n21, n32);
            axis.Normalize();
        }
        public static void GetCircle(Vec3D baseV1, Vec3D baseV2, Vec3D baseV3, out double r, out double arcAngleDeg, 
                                     out Vec3D center, out Vec3D axis)
        {
            Vec3D n12 = baseV1 - baseV2;
            Vec3D n21 = baseV2 - baseV1;
            Vec3D n23 = baseV2 - baseV3;
            Vec3D n32 = baseV3 - baseV2;
            Vec3D n13 = baseV1 - baseV3;
            Vec3D n31 = baseV3 - baseV1;
            Vec3D n12xn23 = Vec3D.CrossProduct(n12, n23);
            //
            r = (n12.Len * n23.Len * n31.Len) / (2 * n12xn23.Len);
            //
            double denominator = 2 * n12xn23.Len2;
            double alpha = n23.Len2 * DotProduct(n12, n13) / denominator;
            double beta = n13.Len2 * DotProduct(n21, n23) / denominator;
            double gama = n12.Len2 * DotProduct(n31, n32) / denominator;
            //
            center = alpha * baseV1 + beta * baseV2 + gama * baseV3;
            //
            axis = CrossProduct(n21, n32);
            axis.Normalize();
            //
            Vec3D r1 = baseV1 - center;
            Vec3D r2 = baseV2 - center;
            Vec3D r3 = baseV3 - center;
            r1.Normalize();
            r2.Normalize();
            r3.Normalize();
            arcAngleDeg = Math.Acos(DotProduct(r1, r2)) * 180 / Math.PI;
            arcAngleDeg += Math.Acos(DotProduct(r2, r3)) * 180 / Math.PI;
        }
        public static void GetCircleR(Vec3D baseV1, Vec3D baseV2, Vec3D baseV3, out double r)
        {
            Vec3D n12 = baseV1 - baseV2;
            Vec3D n23 = baseV2 - baseV3;
            Vec3D n31 = baseV3 - baseV1;
            Vec3D n12xn23 = CrossProduct(n12, n23);
            //
            r = (n12.Len * n23.Len * n31.Len) / (2 * n12xn23.Len);
        }
        // List
        public static List<Vec3D> MergeNormals(List<Vec3D> normals, double angleDeg)
        {
            double cosTol = Math.Cos(angleDeg * Math.PI / 180.0);
            var clusters = new List<List<Vec3D>>();
            //
            foreach (var normal in normals)
            {
                Vec3D n = new Vec3D(normal);
                normal.Normalize();
                //
                List<Vec3D> matchingCluster = null;
                foreach (var cluster in clusters)
                {
                    foreach (var existing in cluster)
                    {
                        if (Vec3D.DotProduct(n, existing) >= cosTol)
                        {
                            matchingCluster = cluster;
                            break;
                        }
                    }
                    if (matchingCluster != null) break;
                }
                if (matchingCluster != null) matchingCluster.Add(n);
                else clusters.Add(new List<Vec3D> { n });
            }
            // Merge clusters by averaging
            var result = new List<Vec3D>();
            foreach (var cluster in clusters)
            {
                Vec3D sum = new Vec3D(0, 0, 0);
                foreach (var v in cluster) sum += v;
                //
                sum.Normalize();
                result.Add(sum);
            }
            return result;
        }

        public static bool TryIntersectPlanesLeastSquares(Vec3D basePoint, Vec3D[] normals, double[] offsets, 
                                                          out Vec3D avgNormal, double conditioningTolerance = 1e-10)
        {
            if (normals == null || offsets == null)
                throw new ArgumentNullException();
            //
            if (normals.Length != offsets.Length || normals.Length < 2)
                throw new ArgumentException("Need at least two planes.");
            //
            int n = normals.Length;
            // Build normal equation components
            double a11 = 0, a12 = 0, a13 = 0;
            double a22 = 0, a23 = 0;
            double a33 = 0;
            double b1 = 0, b2 = 0, b3 = 0;
            //
            for (int i = 0; i < n; i++)
            {
                Vec3D ni = new Vec3D(normals[i]);
                ni.Normalize();
                double di = offsets[i];
                //
                a11 += ni.X * ni.X;
                a12 += ni.X * ni.Y;
                a13 += ni.X * ni.Z;
                a22 += ni.Y * ni.Y;
                a23 += ni.Y * ni.Z;
                a33 += ni.Z * ni.Z;
                //
                b1 += ni.X * di;
                b2 += ni.Y * di;
                b3 += ni.Z * di;
            }
            // Symmetric matrix
            double[,] M = new double[3, 3] {{ a11, a12, a13 },
                                            { a12, a22, a23 },
                                            { a13, a23, a33 }};
            //
            Vec3D intersection;
            Vec3D rhs = new Vec3D(b1, b2, b3);
            //
            if (!TrySolve3x3(M, rhs, out intersection, conditioningTolerance))
            {
                // Fallback: averaged normal extrusion
                Vec3D avg = new Vec3D(0, 0, 0);
                foreach (var nrm in normals)
                {
                    Vec3D nn = new Vec3D(nrm);
                    nn.Normalize();
                    avg += nn;
                }
                //
                avgNormal = avg;
                avgNormal *= 1.0 / normals.Length;
                //
                return false;
            }
            avgNormal = intersection - basePoint;
            return true;
        }
        private static bool TrySolve3x3(double[,] M, Vec3D b, out Vec3D x, double tol)
        {
            x = new Vec3D();
            //
            double det = M[0, 0] * (M[1, 1] * M[2, 2] - M[1, 2] * M[2, 1]) - 
                         M[0, 1] * (M[1, 0] * M[2, 2] - M[1, 2] * M[2, 0]) +
                         M[0, 2] * (M[1, 0] * M[2, 1] - M[1, 1] * M[2, 0]);
            //
            if (Math.Abs(det) < tol)
                return false; // ill-conditioned
            //
            double invDet = 1.0 / det;
            //
            double i00 = (M[1, 1] * M[2, 2] - M[1, 2] * M[2, 1]) * invDet;
            double i01 = -(M[0, 1] * M[2, 2] - M[0, 2] * M[2, 1]) * invDet;
            double i02 = (M[0, 1] * M[1, 2] - M[0, 2] * M[1, 1]) * invDet;
            //
            double i10 = -(M[1, 0] * M[2, 2] - M[1, 2] * M[2, 0]) * invDet;
            double i11 = (M[0, 0] * M[2, 2] - M[0, 2] * M[2, 0]) * invDet;
            double i12 = -(M[0, 0] * M[1, 2] - M[0, 2] * M[1, 0]) * invDet;
            //
            double i20 = (M[1, 0] * M[2, 1] - M[1, 1] * M[2, 0]) * invDet;
            double i21 = -(M[0, 0] * M[2, 1] - M[0, 1] * M[2, 0]) * invDet;
            double i22 = (M[0, 0] * M[1, 1] - M[0, 1] * M[1, 0]) * invDet;
            //
            x = new Vec3D(i00 * b.X + i01 * b.Y + i02 * b.Z,
                          i10 * b.X + i11 * b.Y + i12 * b.Z,
                          i20 * b.X + i21 * b.Y + i22 * b.Z);
            //
            return true;
        }
        #endregion
    }
}
