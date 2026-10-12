// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh
{
    [Serializable]
    public class FeReferencePointMotion
    {
        // Variables                                                                                                                
        private int[] _nodeIds;
        private double[] _weights;
        private bool[] _directions;
        private bool _twoD;


        // Properties                                                                                                               
        public int[] NodeIds { get { return _nodeIds; } }


        // Constructor                                                                                                              
        public FeReferencePointMotion(int[] nodeIds, double[] weights, bool[] directions, bool twoD)
        {
            double sum = 0;
            foreach (double weight in weights) sum += weight;
            //
            _nodeIds = nodeIds;
            _weights = new double[weights.Length];
            for (int i = 0; i < weights.Length; i++) _weights[i] = weights[i] / sum;
            _directions = new bool[] { directions[0], directions[1], directions[2] && !twoD };
            _twoD = twoD;
        }


        // Methods                                                                                                                  
        public bool IsDirectionFollowed(int direction)
        {
            // A 2D model does not move out of plane
            if (_twoD && direction == 2) return true;
            return _directions[direction];
        }
        public bool TryGetDisplacement(double[] point, double[][] coor, double[][] displacements, bool smallRotation,
                                       out double[] displacement, out bool[] computed)
        {
            // The motion of the nodes the point is connected to is fitted by a rigid motion in the directions they follow
            // it, as a small rotation, which a mode and a harmonic response are, and in a static increment also as a
            // rotation of any size; the fit that matches the nodes better moves the point, since a linear step moves them
            // by a small rotation and a step with large deformations by the rotation itself. A direction the nodes do not
            // follow, or in which a rotation about the point moves them as its translation does, is not computed and is
            // left undeformed
            displacement = null;
            computed = null;
            int numOfNodes = _nodeIds.Length;
            if (numOfNodes == 0 || coor.Length != numOfNodes || displacements.Length != numOfNodes) return false;
            //
            double[] center = new double[3];
            double[] deformedCenter = new double[3];
            for (int i = 0; i < numOfNodes; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    center[j] += _weights[i] * coor[i][j];
                    deformedCenter[j] += _weights[i] * (coor[i][j] + displacements[i][j]);
                }
            }
            double length = 0;
            for (int i = 0; i < numOfNodes; i++) length += _weights[i] * SquaredDistance(coor[i], center);
            length = Math.Sqrt(length);
            double[][] arms = new double[numOfNodes][];
            for (int i = 0; i < numOfNodes; i++) arms[i] = Subtract(coor[i], point);
            // A single point does not rotate
            if (length <= 1E-12 * Math.Max(1, Norm(center)))
            {
                displacement = Subtract(deformedCenter, center);
                computed = new bool[3];
                for (int j = 0; j < 3; j++)
                {
                    computed[j] = IsDirectionFollowed(j);
                    if (!_directions[j]) displacement[j] = 0;
                }
                return true;
            }
            // Small rotation
            double[] rotation;
            double[] linearDisplacement;
            bool[] determined;
            SolveRigidMotion(arms, displacements, length, out linearDisplacement, out rotation, out determined);
            double linearResidual = GetResidual(arms, displacements, linearDisplacement, rotation);
            // Rotation of any size, from the best fit of all the components refined on the ones the nodes follow
            double[][] matrix = smallRotation ? null : GetInitialRotationMatrix(coor, displacements, center, deformedCenter);
            double[][] rotatedArms = new double[numOfNodes][];
            double[][] remainders = new double[numOfNodes][];
            double[] largeDisplacement = null;
            double largeResidual = double.MaxValue;
            for (int iteration = 0; iteration < 50 && matrix != null; iteration++)
            {
                for (int i = 0; i < numOfNodes; i++)
                {
                    rotatedArms[i] = Multiply(matrix, arms[i]);
                    remainders[i] = new double[3];
                    for (int j = 0; j < 3; j++) remainders[i][j] = arms[i][j] + displacements[i][j] - rotatedArms[i][j];
                }
                SolveRigidMotion(rotatedArms, remainders, length, out largeDisplacement, out rotation, out _);
                largeResidual = GetResidual(rotatedArms, remainders, largeDisplacement, rotation);
                if (Norm(rotation) <= 1E-14) break;
                //
                matrix = MultiplyMatrices(GetRotationMatrix(rotation), matrix);
            }
            //
            displacement = largeDisplacement != null && largeResidual < linearResidual ? largeDisplacement
                                                                                         : linearDisplacement;
            computed = new bool[3];
            for (int j = 0; j < 3; j++)
            {
                computed[j] = (_twoD && j == 2) || determined[j];
                if (!determined[j]) displacement[j] = 0;
            }
            return true;
        }
        private void SolveRigidMotion(double[][] arms, double[][] values, double length, out double[] translation,
                                      out double[] rotation, out bool[] determined)
        {
            // The least squares translation of the point and the rotation, in the directions the nodes follow, with the
            // rotation scaled by the size of the region; a motion the nodes do not determine is left out, and so is a
            // translation of the point that such a motion contains
            double[][] matrix = new double[6][];
            for (int i = 0; i < 6; i++) matrix[i] = new double[6];
            double[] rhs = new double[6];
            double[] row = new double[6];
            double[] arm;
            for (int i = 0; i < arms.Length; i++)
            {
                arm = new double[] { arms[i][0] / length, arms[i][1] / length, arms[i][2] / length };
                for (int direction = 0; direction < 3; direction++)
                {
                    if (!_directions[direction]) continue;
                    //
                    Array.Clear(row, 0, 6);
                    row[direction] = 1;
                    // The component of rotation x arm in the direction
                    if (direction == 0) { row[4] = arm[2]; row[5] = -arm[1]; }
                    else if (direction == 1) { row[3] = -arm[2]; row[5] = arm[0]; }
                    else { row[3] = arm[1]; row[4] = -arm[0]; }
                    //
                    for (int j = 0; j < 6; j++)
                    {
                        rhs[j] += _weights[i] * row[j] * values[i][direction];
                        for (int k = 0; k < 6; k++) matrix[j][k] += _weights[i] * row[j] * row[k];
                    }
                }
            }
            //
            double[] solution = new double[6];
            double[] eigenValues;
            double[][] eigenVectors;
            determined = new bool[3];
            if (SmallMatrixTools.SymmetricEigen(matrix, out eigenValues, out eigenVectors) && eigenValues[0] > 0)
            {
                for (int j = 0; j < 3; j++) determined[j] = _directions[j];
                double projection;
                for (int i = 0; i < 6; i++)
                {
                    if (eigenValues[i] <= 1E-10 * eigenValues[0])
                    {
                        for (int j = 0; j < 3; j++) if (Math.Abs(eigenVectors[i][j]) > 1E-6) determined[j] = false;
                        continue;
                    }
                    //
                    projection = 0;
                    for (int j = 0; j < 6; j++) projection += eigenVectors[i][j] * rhs[j];
                    projection /= eigenValues[i];
                    for (int j = 0; j < 6; j++) solution[j] += projection * eigenVectors[i][j];
                }
            }
            translation = new double[] { solution[0], solution[1], solution[2] };
            rotation = new double[] { solution[3] / length, solution[4] / length, solution[5] / length };
        }
        private double GetResidual(double[][] arms, double[][] values, double[] translation, double[] rotation)
        {
            double residual = 0;
            double difference;
            double[] rotated;
            for (int i = 0; i < arms.Length; i++)
            {
                rotated = Cross(rotation, arms[i]);
                for (int direction = 0; direction < 3; direction++)
                {
                    if (!_directions[direction]) continue;
                    //
                    difference = values[i][direction] - translation[direction] - rotated[direction];
                    residual += _weights[i] * difference * difference;
                }
            }
            return residual;
        }
        private double[][] GetInitialRotationMatrix(double[][] coor, double[][] displacements, double[] center,
                                                    double[] deformedCenter)
        {
            int numOfNodes = coor.Length;
            double[][] initial = new double[numOfNodes][];
            double[][] deformed = new double[numOfNodes][];
            for (int i = 0; i < numOfNodes; i++)
            {
                initial[i] = Subtract(coor[i], center);
                deformed[i] = new double[3];
                for (int j = 0; j < 3; j++) deformed[i][j] = coor[i][j] + displacements[i][j] - deformedCenter[j];
            }
            // A 2D model rotates in plane, also when its nodes lie on a line
            if (_twoD)
            {
                double sin = 0;
                double cos = 0;
                for (int i = 0; i < numOfNodes; i++)
                {
                    sin += _weights[i] * (initial[i][0] * deformed[i][1] - initial[i][1] * deformed[i][0]);
                    cos += _weights[i] * (initial[i][0] * deformed[i][0] + initial[i][1] * deformed[i][1]);
                }
                if (sin == 0 && cos == 0) return null;
                return GetRotationMatrix(new double[] { 0, 0, Math.Atan2(sin, cos) });
            }
            //
            double[] eigenValues;
            double[][] eigenVectors;
            if (!SmallMatrixTools.SymmetricEigen(GetScatterMatrix(initial, initial), out eigenValues, out eigenVectors))
                return null;
            // Nodes on a line give no rotation about it, so the line is turned the shortest way
            if (eigenValues[1] <= 1E-10 * eigenValues[0])
            {
                double[] direction = eigenVectors[0];
                double[] deformedDirection = new double[3];
                for (int i = 0; i < numOfNodes; i++)
                {
                    double projection = Dot(initial[i], direction);
                    for (int j = 0; j < 3; j++) deformedDirection[j] += _weights[i] * projection * deformed[i][j];
                }
                double norm = Norm(deformedDirection);
                if (norm == 0) return null;
                for (int j = 0; j < 3; j++) deformedDirection[j] /= norm;
                double[] axis = Cross(direction, deformedDirection);
                double angle = Math.Atan2(Norm(axis), Dot(direction, deformedDirection));
                norm = Norm(axis);
                if (norm == 0) return GetRotationMatrix(new double[3]);
                return GetRotationMatrix(new double[] { axis[0] / norm * angle, axis[1] / norm * angle,
                                                        axis[2] / norm * angle });
            }
            // The rotation of the best fit of the deformed nodes by Horn's quaternion method
            double[][] s = GetScatterMatrix(initial, deformed);
            double[][] n = new double[][]
            {
                new double[] { s[0][0] + s[1][1] + s[2][2], s[1][2] - s[2][1], s[2][0] - s[0][2], s[0][1] - s[1][0] },
                new double[] { s[1][2] - s[2][1], s[0][0] - s[1][1] - s[2][2], s[0][1] + s[1][0], s[2][0] + s[0][2] },
                new double[] { s[2][0] - s[0][2], s[0][1] + s[1][0], -s[0][0] + s[1][1] - s[2][2], s[1][2] + s[2][1] },
                new double[] { s[0][1] - s[1][0], s[2][0] + s[0][2], s[1][2] + s[2][1], -s[0][0] - s[1][1] + s[2][2] }
            };
            if (!SmallMatrixTools.SymmetricEigen(n, out eigenValues, out eigenVectors)) return null;
            double[] q = eigenVectors[0];
            return new double[][]
            {
                new double[] { q[0] * q[0] + q[1] * q[1] - q[2] * q[2] - q[3] * q[3], 2 * (q[1] * q[2] - q[0] * q[3]),
                               2 * (q[1] * q[3] + q[0] * q[2]) },
                new double[] { 2 * (q[1] * q[2] + q[0] * q[3]), q[0] * q[0] - q[1] * q[1] + q[2] * q[2] - q[3] * q[3],
                               2 * (q[2] * q[3] - q[0] * q[1]) },
                new double[] { 2 * (q[1] * q[3] - q[0] * q[2]), 2 * (q[2] * q[3] + q[0] * q[1]),
                               q[0] * q[0] - q[1] * q[1] - q[2] * q[2] + q[3] * q[3] }
            };
        }
        private double[][] GetScatterMatrix(double[][] first, double[][] second)
        {
            double[][] matrix = new double[][] { new double[3], new double[3], new double[3] };
            for (int i = 0; i < first.Length; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++) matrix[j][k] += _weights[i] * first[i][j] * second[i][k];
                }
            }
            return matrix;
        }
        static private double[][] GetRotationMatrix(double[] rotation)
        {
            // Rodrigues' formula
            double angle = Norm(rotation);
            double[][] matrix = new double[][] { new double[] { 1, 0, 0 }, new double[] { 0, 1, 0 },
                                                 new double[] { 0, 0, 1 } };
            if (angle == 0) return matrix;
            //
            double[] axis = new double[] { rotation[0] / angle, rotation[1] / angle, rotation[2] / angle };
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            double[][] cross = new double[][] { new double[] { 0, -axis[2], axis[1] }, new double[] { axis[2], 0, -axis[0] },
                                                new double[] { -axis[1], axis[0], 0 } };
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                    matrix[i][j] = (i == j ? cos : 0) + sin * cross[i][j] + (1 - cos) * axis[i] * axis[j];
            }
            return matrix;
        }
        static private double[][] MultiplyMatrices(double[][] first, double[][] second)
        {
            double[][] matrix = new double[][] { new double[3], new double[3], new double[3] };
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++) matrix[i][j] += first[i][k] * second[k][j];
                }
            }
            return matrix;
        }
        static private double[] Multiply(double[][] matrix, double[] vector)
        {
            return new double[] { Dot(matrix[0], vector), Dot(matrix[1], vector), Dot(matrix[2], vector) };
        }
        static private double[] Subtract(double[] first, double[] second)
        {
            return new double[] { first[0] - second[0], first[1] - second[1], first[2] - second[2] };
        }
        static private double[] Cross(double[] first, double[] second)
        {
            return new double[] { first[1] * second[2] - first[2] * second[1], first[2] * second[0] - first[0] * second[2],
                                  first[0] * second[1] - first[1] * second[0] };
        }
        static private double Dot(double[] first, double[] second)
        {
            return first[0] * second[0] + first[1] * second[1] + first[2] * second[2];
        }
        static private double Norm(double[] vector)
        {
            return Math.Sqrt(Dot(vector, vector));
        }
        static private double SquaredDistance(double[] first, double[] second)
        {
            double[] difference = Subtract(first, second);
            return Dot(difference, difference);
        }
    }
}
