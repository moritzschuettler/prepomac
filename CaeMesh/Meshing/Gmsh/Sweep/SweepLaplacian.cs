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

namespace CaeMesh.Sweep
{
    /// <summary>
    /// The discrete Laplacian of the source surface mesh, used to extend a boundary displacement
    /// into the interior of a sweep layer.
    ///
    /// The weights are the off-diagonal entries of the finite element stiffness matrix of the
    /// source mesh, assembled element by element in each element's own plane. For a linear
    /// triangle this reproduces the classical cotangent weights exactly; bilinear quadrilaterals
    /// are integrated with 2x2 Gauss quadrature, so mixed triangle and quadrilateral source
    /// meshes are treated consistently. Negative weights, which badly shaped elements can produce,
    /// are clamped to zero and the diagonal is rebuilt as the row sum. That makes the matrix an
    /// M-matrix, so the harmonic extension satisfies a maximum principle and cannot overshoot the
    /// boundary data - the property that keeps the morphed layers free of folds.
    ///
    /// The quadrilateral stiffness couples the diagonal node pairs as well, so the graph here is
    /// built from all node pairs inside an element rather than from the ring adjacency held by
    /// SweepMeshData.
    /// </summary>
    public class SweepLaplacian
    {
        // Variables
        private readonly int _numNodes;
        private readonly int[] _rowStart;               // CSR graph over all source nodes
        private readonly int[] _columns;
        private readonly double[] _weights;             // non-negative off-diagonal weights
        private readonly double[] _diagonal;            // row sum of the weights


        // Properties
        public int NumNodes { get { return _numNodes; } }


        // Constructors
        private SweepLaplacian(int numNodes, int[] rowStart, int[] columns, double[] weights, double[] diagonal)
        {
            _numNodes = numNodes;
            _rowStart = rowStart;
            _columns = columns;
            _weights = weights;
            _diagonal = diagonal;
        }


        // Methods
        public static SweepLaplacian Create(SweepMeshData data)
        {
            int n = data.NumNodes;
            int[][] elements = data.SourceElements;
            // Graph - every pair of nodes that share an element
            HashSet<int>[] connections = new HashSet<int>[n];
            for (int i = 0; i < n; i++) connections[i] = new HashSet<int>();
            for (int e = 0; e < elements.Length; e++)
            {
                int[] element = elements[e];
                for (int i = 0; i < element.Length; i++)
                {
                    for (int j = 0; j < element.Length; j++)
                    {
                        if (i != j) connections[element[i]].Add(element[j]);
                    }
                }
            }
            //
            int[] rowStart = new int[n + 1];
            for (int i = 0; i < n; i++) rowStart[i + 1] = rowStart[i] + connections[i].Count;
            int[] columns = new int[rowStart[n]];
            for (int i = 0; i < n; i++) connections[i].CopyTo(columns, rowStart[i]);
            double[] weights = new double[columns.Length];
            // Assemble the element stiffness matrices
            double[] localX = new double[4];
            double[] localY = new double[4];
            double[,] ke = new double[4, 4];
            for (int e = 0; e < elements.Length; e++)
            {
                int[] element = elements[e];
                if (!ProjectToElementPlane(data, element, localX, localY)) continue;
                //
                if (element.Length == 3) TriangleStiffness(localX, localY, ke);
                else if (element.Length == 4) QuadrilateralStiffness(localX, localY, ke);
                else continue;
                //
                for (int i = 0; i < element.Length; i++)
                {
                    for (int j = 0; j < element.Length; j++)
                    {
                        if (i == j) continue;
                        // The weight is the negated off-diagonal stiffness entry
                        AddWeight(rowStart, columns, weights, element[i], element[j], -ke[i, j]);
                    }
                }
            }
            // Clamp and rebuild the diagonal as the row sum
            double[] diagonal = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int k = rowStart[i]; k < rowStart[i + 1]; k++)
                {
                    if (weights[k] < 0) weights[k] = 0;
                    sum += weights[k];
                }
                // An isolated node would give a singular row; fall back to a unit diagonal
                diagonal[i] = sum > 0 ? sum : 1;
            }
            //
            return new SweepLaplacian(n, rowStart, columns, weights, diagonal);
        }
        private static void AddWeight(int[] rowStart, int[] columns, double[] weights, int row, int column,
                                      double value)
        {
            for (int k = rowStart[row]; k < rowStart[row + 1]; k++)
            {
                if (columns[k] == column) { weights[k] += value; return; }
            }
        }
        /// <summary>
        /// Projects the nodes of one element into the plane of that element. Returns false for a
        /// degenerate element, which is then skipped.
        /// </summary>
        private static bool ProjectToElementPlane(SweepMeshData data, int[] element, double[] localX, double[] localY)
        {
            int n = element.Length;
            double[] coords = data.Coords;
            // Newell normal - robust for slightly non planar quadrilaterals
            double nx = 0, ny = 0, nz = 0;
            for (int i = 0; i < n; i++)
            {
                int a = 3 * element[i];
                int b = 3 * element[(i + 1) % n];
                nx += (coords[a + 1] - coords[b + 1]) * (coords[a + 2] + coords[b + 2]);
                ny += (coords[a + 2] - coords[b + 2]) * (coords[a] + coords[b]);
                nz += (coords[a] - coords[b]) * (coords[a + 1] + coords[b + 1]);
            }
            double nLength = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (nLength < 1E-30) return false;
            nx /= nLength; ny /= nLength; nz /= nLength;
            // First in plane axis along the first edge
            int p0 = 3 * element[0];
            int p1 = 3 * element[1];
            double ux = coords[p1] - coords[p0];
            double uy = coords[p1 + 1] - coords[p0 + 1];
            double uz = coords[p1 + 2] - coords[p0 + 2];
            double uLength = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            if (uLength < 1E-30) return false;
            ux /= uLength; uy /= uLength; uz /= uLength;
            // Second in plane axis
            double vx = ny * uz - nz * uy;
            double vy = nz * ux - nx * uz;
            double vz = nx * uy - ny * ux;
            //
            for (int i = 0; i < n; i++)
            {
                int p = 3 * element[i];
                double dx = coords[p] - coords[p0];
                double dy = coords[p + 1] - coords[p0 + 1];
                double dz = coords[p + 2] - coords[p0 + 2];
                localX[i] = dx * ux + dy * uy + dz * uz;
                localY[i] = dx * vx + dy * vy + dz * vz;
            }
            return true;
        }
        private static void TriangleStiffness(double[] x, double[] y, double[,] ke)
        {
            double[] b = new double[3];
            double[] c = new double[3];
            b[0] = y[1] - y[2]; b[1] = y[2] - y[0]; b[2] = y[0] - y[1];
            c[0] = x[2] - x[1]; c[1] = x[0] - x[2]; c[2] = x[1] - x[0];
            double twoArea = x[0] * b[0] + x[1] * b[1] + x[2] * b[2];
            //
            Array.Clear(ke, 0, ke.Length);
            if (Math.Abs(twoArea) < 1E-30) return;
            double f = 1.0 / (2 * Math.Abs(twoArea));
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) ke[i, j] = f * (b[i] * b[j] + c[i] * c[j]);
            }
        }
        private static readonly double[] _xiCorner = new double[] { -1, 1, 1, -1 };
        private static readonly double[] _etaCorner = new double[] { -1, -1, 1, 1 };
        private static void QuadrilateralStiffness(double[] x, double[] y, double[,] ke)
        {
            double g = 1.0 / Math.Sqrt(3.0);
            double[] gaussXi = new double[] { -g, g, g, -g };
            double[] gaussEta = new double[] { -g, -g, g, g };
            double[] dNdXi = new double[4];
            double[] dNdEta = new double[4];
            double[] dNdX = new double[4];
            double[] dNdY = new double[4];
            //
            Array.Clear(ke, 0, ke.Length);
            //
            for (int p = 0; p < 4; p++)
            {
                double xi = gaussXi[p];
                double eta = gaussEta[p];
                for (int i = 0; i < 4; i++)
                {
                    dNdXi[i] = 0.25 * _xiCorner[i] * (1 + _etaCorner[i] * eta);
                    dNdEta[i] = 0.25 * _etaCorner[i] * (1 + _xiCorner[i] * xi);
                }
                double j11 = 0, j12 = 0, j21 = 0, j22 = 0;
                for (int i = 0; i < 4; i++)
                {
                    j11 += dNdXi[i] * x[i];
                    j12 += dNdXi[i] * y[i];
                    j21 += dNdEta[i] * x[i];
                    j22 += dNdEta[i] * y[i];
                }
                double det = j11 * j22 - j12 * j21;
                if (Math.Abs(det) < 1E-30) continue;
                double invDet = 1.0 / det;
                for (int i = 0; i < 4; i++)
                {
                    dNdX[i] = invDet * (j22 * dNdXi[i] - j12 * dNdEta[i]);
                    dNdY[i] = invDet * (-j21 * dNdXi[i] + j11 * dNdEta[i]);
                }
                double w = Math.Abs(det);
                for (int i = 0; i < 4; i++)
                {
                    for (int k = 0; k < 4; k++) ke[i, k] += w * (dNdX[i] * dNdX[k] + dNdY[i] * dNdY[k]);
                }
            }
        }
        /// <summary>
        /// Harmonic extension of a boundary field into the interior: solves the Laplace equation
        /// on the source mesh with the boundary values as Dirichlet data.
        ///
        /// The values array holds three components per node. Boundary entries are the prescribed
        /// data and are left untouched; interior entries are used as the initial guess on entry
        /// and hold the solution on exit, which lets consecutive layers warm start from each other
        /// and cuts the iteration count to a handful after the first layer.
        ///
        /// Solved with a Jacobi preconditioned conjugate gradient on the three components at once,
        /// so all three share a single matrix-vector product.
        /// </summary>
        public int SolveHarmonic(double[] values, bool[] isBoundaryNode, int maxIterations, double tolerance)
        {
            int n = _numNodes;
            double[] x = new double[3 * n];
            double[] r = new double[3 * n];
            double[] z = new double[3 * n];
            double[] p = new double[3 * n];
            double[] ap = new double[3 * n];
            // Unknowns, zero on the boundary
            for (int i = 0; i < n; i++)
            {
                if (isBoundaryNode[i]) continue;
                x[3 * i] = values[3 * i];
                x[3 * i + 1] = values[3 * i + 1];
                x[3 * i + 2] = values[3 * i + 2];
            }
            // Right hand side from the boundary values, and the initial residual r = b - A x
            for (int i = 0; i < n; i++)
            {
                if (isBoundaryNode[i]) continue;
                double bx = 0, by = 0, bz = 0;
                for (int k = _rowStart[i]; k < _rowStart[i + 1]; k++)
                {
                    int j = _columns[k];
                    if (!isBoundaryNode[j]) continue;
                    bx += _weights[k] * values[3 * j];
                    by += _weights[k] * values[3 * j + 1];
                    bz += _weights[k] * values[3 * j + 2];
                }
                r[3 * i] = bx;
                r[3 * i + 1] = by;
                r[3 * i + 2] = bz;
            }
            double normB = Math.Sqrt(Dot(r, r));
            if (normB < 1E-300)
            {
                // The boundary does not move; the harmonic extension is identically zero
                for (int i = 0; i < n; i++)
                {
                    if (isBoundaryNode[i]) continue;
                    values[3 * i] = 0;
                    values[3 * i + 1] = 0;
                    values[3 * i + 2] = 0;
                }
                return 0;
            }
            Multiply(x, ap, isBoundaryNode);
            for (int i = 0; i < r.Length; i++) r[i] -= ap[i];
            //
            ApplyJacobi(r, z, isBoundaryNode);
            Array.Copy(z, p, p.Length);
            double rz = Dot(r, z);
            int iteration = 0;
            double target = tolerance * normB;
            //
            for (; iteration < maxIterations; iteration++)
            {
                if (Math.Sqrt(Dot(r, r)) <= target) break;
                //
                Multiply(p, ap, isBoundaryNode);
                double pap = Dot(p, ap);
                if (Math.Abs(pap) < 1E-300) break;
                double alpha = rz / pap;
                for (int i = 0; i < x.Length; i++)
                {
                    x[i] += alpha * p[i];
                    r[i] -= alpha * ap[i];
                }
                ApplyJacobi(r, z, isBoundaryNode);
                double rzNew = Dot(r, z);
                double beta = rzNew / rz;
                rz = rzNew;
                for (int i = 0; i < p.Length; i++) p[i] = z[i] + beta * p[i];
            }
            //
            for (int i = 0; i < n; i++)
            {
                if (isBoundaryNode[i]) continue;
                values[3 * i] = x[3 * i];
                values[3 * i + 1] = x[3 * i + 1];
                values[3 * i + 2] = x[3 * i + 2];
            }
            return iteration;
        }
        private void Multiply(double[] v, double[] result, bool[] isBoundaryNode)
        {
            int n = _numNodes;
            for (int i = 0; i < n; i++)
            {
                if (isBoundaryNode[i])
                {
                    result[3 * i] = 0;
                    result[3 * i + 1] = 0;
                    result[3 * i + 2] = 0;
                    continue;
                }
                double sx = _diagonal[i] * v[3 * i];
                double sy = _diagonal[i] * v[3 * i + 1];
                double sz = _diagonal[i] * v[3 * i + 2];
                for (int k = _rowStart[i]; k < _rowStart[i + 1]; k++)
                {
                    int j = _columns[k];
                    if (isBoundaryNode[j]) continue;
                    sx -= _weights[k] * v[3 * j];
                    sy -= _weights[k] * v[3 * j + 1];
                    sz -= _weights[k] * v[3 * j + 2];
                }
                result[3 * i] = sx;
                result[3 * i + 1] = sy;
                result[3 * i + 2] = sz;
            }
        }
        private void ApplyJacobi(double[] v, double[] result, bool[] isBoundaryNode)
        {
            for (int i = 0; i < _numNodes; i++)
            {
                if (isBoundaryNode[i])
                {
                    result[3 * i] = 0;
                    result[3 * i + 1] = 0;
                    result[3 * i + 2] = 0;
                    continue;
                }
                double d = _diagonal[i];
                result[3 * i] = v[3 * i] / d;
                result[3 * i + 1] = v[3 * i + 1] / d;
                result[3 * i + 2] = v[3 * i + 2] / d;
            }
        }
        private static double Dot(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++) sum += a[i] * b[i];
            return sum;
        }
    }
}
