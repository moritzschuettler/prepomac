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
    /// <summary>
    /// Small dense linear algebra used by the mesh based face type recognition.
    /// All methods work on small matrices (3 x 3, 4 x 4, 6 x 6) and are allocation friendly.
    /// </summary>
    public static class SmallMatrixTools
    {
        // Methods
        /// <summary>
        /// Jacobi eigenvalue decomposition of a small symmetric matrix.
        /// Eigenvalues are sorted in a descending order and eigenVectors[i] belongs to eigenValues[i].
        /// </summary>
        public static bool SymmetricEigen(double[][] matrix, out double[] eigenValues, out double[][] eigenVectors)
        {
            eigenValues = null;
            eigenVectors = null;
            //
            if (matrix == null || matrix.Length < 2) return false;
            int n = matrix.Length;
            // Copy the matrix and prepare the eigenvector matrix
            double norm = 0;
            double[][] a = new double[n][];
            double[][] v = new double[n][];
            for (int i = 0; i < n; i++)
            {
                a[i] = new double[n];
                v[i] = new double[n];
                v[i][i] = 1;
                for (int j = 0; j < n; j++)
                {
                    a[i][j] = matrix[i][j];
                    norm += matrix[i][j] * matrix[i][j];
                }
            }
            // Cyclic Jacobi sweeps
            if (norm > 0)
            {
                double c;
                double s;
                double t;
                double off;
                double theta;
                double eps = 1E-24 * norm;
                //
                for (int sweep = 0; sweep < 50; sweep++)
                {
                    off = 0;
                    for (int i = 0; i < n - 1; i++)
                    {
                        for (int j = i + 1; j < n; j++) off += a[i][j] * a[i][j];
                    }
                    if (off <= eps) break;
                    //
                    for (int p = 0; p < n - 1; p++)
                    {
                        for (int q = p + 1; q < n; q++)
                        {
                            if (a[p][q] == 0) continue;
                            //
                            theta = (a[q][q] - a[p][p]) / (2 * a[p][q]);
                            t = (theta >= 0 ? 1.0 : -1.0) / (Math.Abs(theta) + Math.Sqrt(theta * theta + 1));
                            c = 1 / Math.Sqrt(t * t + 1);
                            s = t * c;
                            // Columns
                            for (int k = 0; k < n; k++)
                            {
                                double akp = a[k][p];
                                double akq = a[k][q];
                                a[k][p] = c * akp - s * akq;
                                a[k][q] = s * akp + c * akq;
                            }
                            // Rows
                            for (int k = 0; k < n; k++)
                            {
                                double apk = a[p][k];
                                double aqk = a[q][k];
                                a[p][k] = c * apk - s * aqk;
                                a[q][k] = s * apk + c * aqk;
                            }
                            // Eigenvectors
                            for (int k = 0; k < n; k++)
                            {
                                double vkp = v[k][p];
                                double vkq = v[k][q];
                                v[k][p] = c * vkp - s * vkq;
                                v[k][q] = s * vkp + c * vkq;
                            }
                        }
                    }
                }
            }
            // Sort the eigenvalues in a descending order - simple selection sort for small n
            int[] order = new int[n];
            double[] values = new double[n];
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
                values[i] = a[i][i];
            }
            for (int i = 0; i < n - 1; i++)
            {
                int maxId = i;
                for (int j = i + 1; j < n; j++)
                {
                    if (values[order[j]] > values[order[maxId]]) maxId = j;
                }
                if (maxId != i)
                {
                    int tmp = order[i];
                    order[i] = order[maxId];
                    order[maxId] = tmp;
                }
            }
            // Collect
            eigenValues = new double[n];
            eigenVectors = new double[n][];
            for (int i = 0; i < n; i++)
            {
                eigenValues[i] = values[order[i]];
                eigenVectors[i] = new double[n];
                for (int j = 0; j < n; j++) eigenVectors[i][j] = v[j][order[i]];
            }
            //
            return true;
        }
        /// <summary>
        /// Solves a small linear system using the Gauss elimination with partial pivoting.
        /// The matrix and the right hand side are not modified.
        /// </summary>
        public static bool Solve(double[][] matrix, double[] rhs, out double[] x)
        {
            x = null;
            if (matrix == null || rhs == null || matrix.Length != rhs.Length) return false;
            //
            int n = matrix.Length;
            double norm = 0;
            double[][] a = new double[n][];
            double[] b = new double[n];
            for (int i = 0; i < n; i++)
            {
                a[i] = new double[n];
                b[i] = rhs[i];
                for (int j = 0; j < n; j++)
                {
                    a[i][j] = matrix[i][j];
                    norm = Math.Max(norm, Math.Abs(matrix[i][j]));
                }
            }
            if (norm <= 0) return false;
            double eps = 1E-12 * norm;
            // Elimination
            for (int k = 0; k < n; k++)
            {
                // Pivot
                int pivot = k;
                for (int i = k + 1; i < n; i++)
                {
                    if (Math.Abs(a[i][k]) > Math.Abs(a[pivot][k])) pivot = i;
                }
                if (Math.Abs(a[pivot][k]) <= eps) return false;     // singular
                if (pivot != k)
                {
                    double[] tmpRow = a[k];
                    a[k] = a[pivot];
                    a[pivot] = tmpRow;
                    double tmp = b[k];
                    b[k] = b[pivot];
                    b[pivot] = tmp;
                }
                //
                for (int i = k + 1; i < n; i++)
                {
                    double f = a[i][k] / a[k][k];
                    if (f == 0) continue;
                    for (int j = k; j < n; j++) a[i][j] -= f * a[k][j];
                    b[i] -= f * b[k];
                }
            }
            // Back substitution
            x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = b[i];
                for (int j = i + 1; j < n; j++) sum -= a[i][j] * x[j];
                x[i] = sum / a[i][i];
            }
            //
            return true;
        }
    }
}
