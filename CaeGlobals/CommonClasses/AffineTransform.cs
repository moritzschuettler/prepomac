// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeGlobals
{
    /// <summary>
    /// A transform x' = A x + b, which is what a translation, a rotation, a mirror and a scaling of a part are,
    /// and what any sequence of them is.
    /// </summary>
    [Serializable]
    public class AffineTransform
    {
        // Variables                                                                                                                
        private double[][] _matrix;
        private double[] _translation;


        // Properties                                                                                                               
        public bool IsIdentity
        {
            get
            {
                for (int i = 0; i < 3; i++)
                {
                    if (_translation[i] != 0) return false;
                    for (int j = 0; j < 3; j++)
                    {
                        if (_matrix[i][j] != (i == j ? 1 : 0)) return false;
                    }
                }
                return true;
            }
        }


        // Constructors                                                                                                             
        public AffineTransform(double[][] matrix, double[] translation)
        {
            _matrix = new double[3][];
            for (int i = 0; i < 3; i++) _matrix[i] = new double[] { matrix[i][0], matrix[i][1], matrix[i][2] };
            _translation = new double[] { translation[0], translation[1], translation[2] };
        }


        // Static methods                                                                                                           
        public static AffineTransform Identity()
        {
            return Scaling(new double[3], new double[] { 1, 1, 1 });
        }
        public static AffineTransform Translation(double[] translateVector)
        {
            double[][] matrix = new double[][] { new double[] { 1, 0, 0 }, new double[] { 0, 1, 0 },
                                                 new double[] { 0, 0, 1 } };
            return new AffineTransform(matrix, translateVector);
        }
        // The rotation matrix is the one the mesh rotates its nodes by, so a point follows the nodes exactly
        public static AffineTransform Rotation(double[] rotateCenter, double[] rotateAxis, double rotateAngleRad)
        {
            Vec3D u = new Vec3D(rotateAxis);
            u.Normalize();
            double cosPhi = Math.Cos(rotateAngleRad);
            double sinPhi = Math.Sin(rotateAngleRad);
            //
            double[][] m = new double[3][];
            m[0] = new double[] { cosPhi + (Math.Pow(u.X, 2) * (1 - cosPhi)),
                                  u.X * u.Y * (1 - cosPhi) - u.Z * sinPhi,
                                  u.X * u.Z * (1 - cosPhi) + u.Y * sinPhi };
            m[1] = new double[] { u.Y * u.X * (1 - cosPhi) + u.Z * sinPhi,
                                  cosPhi + (Math.Pow(u.Y, 2) * (1 - cosPhi)),
                                  u.Y * u.Z * (1 - cosPhi) - u.X * sinPhi };
            m[2] = new double[] { u.Z * u.X * (1 - cosPhi) - u.Y * sinPhi,
                                  u.Z * u.Y * (1 - cosPhi) + u.X * sinPhi,
                                  cosPhi + (Math.Pow(u.Z, 2) * (1 - cosPhi)) };
            return AboutCenter(m, rotateCenter);
        }
        public static AffineTransform Mirror(double[] mirrorPoint, double[] mirrorDirection)
        {
            Vec3D n = new Vec3D(mirrorDirection);
            n.Normalize();
            double[] normal = n.Coor;
            //
            double[][] m = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                m[i] = new double[3];
                for (int j = 0; j < 3; j++) m[i][j] = (i == j ? 1 : 0) - 2 * normal[i] * normal[j];
            }
            return AboutCenter(m, mirrorPoint);
        }
        public static AffineTransform Scaling(double[] scaleCenter, double[] scaleFactors)
        {
            double[][] m = new double[][] { new double[] { scaleFactors[0], 0, 0 }, new double[] { 0, scaleFactors[1], 0 },
                                            new double[] { 0, 0, scaleFactors[2] } };
            return AboutCenter(m, scaleCenter);
        }
        private static AffineTransform AboutCenter(double[][] matrix, double[] center)
        {
            double[] translation = new double[3];
            for (int i = 0; i < 3; i++)
            {
                translation[i] = center[i];
                for (int j = 0; j < 3; j++) translation[i] -= matrix[i][j] * center[j];
            }
            return new AffineTransform(matrix, translation);
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The transform that applies this transform first and the next one after it.
        /// </summary>
        public AffineTransform Then(AffineTransform next)
        {
            double[][] matrix = new double[3][];
            double[] translation = new double[3];
            for (int i = 0; i < 3; i++)
            {
                matrix[i] = new double[3];
                translation[i] = next._translation[i];
                for (int j = 0; j < 3; j++)
                {
                    translation[i] += next._matrix[i][j] * _translation[j];
                    for (int k = 0; k < 3; k++) matrix[i][j] += next._matrix[i][k] * _matrix[k][j];
                }
            }
            return new AffineTransform(matrix, translation);
        }
        public AffineTransform Inverse()
        {
            double[][] a = _matrix;
            double determinant = a[0][0] * (a[1][1] * a[2][2] - a[1][2] * a[2][1]) -
                                 a[0][1] * (a[1][0] * a[2][2] - a[1][2] * a[2][0]) +
                                 a[0][2] * (a[1][0] * a[2][1] - a[1][1] * a[2][0]);
            if (determinant == 0) throw new CaeException("A transform that collapses space cannot be inverted.");
            //
            double[][] inverse = new double[3][];
            inverse[0] = new double[] { (a[1][1] * a[2][2] - a[1][2] * a[2][1]) / determinant,
                                        (a[0][2] * a[2][1] - a[0][1] * a[2][2]) / determinant,
                                        (a[0][1] * a[1][2] - a[0][2] * a[1][1]) / determinant };
            inverse[1] = new double[] { (a[1][2] * a[2][0] - a[1][0] * a[2][2]) / determinant,
                                        (a[0][0] * a[2][2] - a[0][2] * a[2][0]) / determinant,
                                        (a[0][2] * a[1][0] - a[0][0] * a[1][2]) / determinant };
            inverse[2] = new double[] { (a[1][0] * a[2][1] - a[1][1] * a[2][0]) / determinant,
                                        (a[0][1] * a[2][0] - a[0][0] * a[2][1]) / determinant,
                                        (a[0][0] * a[1][1] - a[0][1] * a[1][0]) / determinant };
            double[] translation = new double[3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) translation[i] -= inverse[i][j] * _translation[j];
            }
            return new AffineTransform(inverse, translation);
        }
        public double[] TransformPoint(double[] point)
        {
            double[] result = TransformDirection(point);
            for (int i = 0; i < 3; i++) result[i] += _translation[i];
            return result;
        }
        public double[] TransformDirection(double[] direction)
        {
            double[] result = new double[3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) result[i] += _matrix[i][j] * direction[j];
            }
            return result;
        }
        // A plane normal stays perpendicular to the transformed plane when it is transformed by the inverse transpose
        public double[] TransformNormal(double[] normal)
        {
            double[][] inverse = Inverse()._matrix;
            double[] result = new double[3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) result[i] += inverse[j][i] * normal[j];
            }
            return result;
        }
    }
}
