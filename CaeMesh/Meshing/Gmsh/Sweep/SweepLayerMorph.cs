// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Places the interior nodes of every sweep layer.
    ///
    /// The wall surfaces already give the exact position of every boundary node on every layer, so
    /// each layer is a known deformation of the source boundary. The interior is filled in two
    /// steps:
    ///
    ///   1. A best fit affine transform is computed from the source boundary loop onto the layer
    ///      boundary loop and applied to every interior source node. This reproduces translation,
    ///      rotation, scaling, taper and shear of the layer exactly, and because it is applied to
    ///      the source positions themselves it carries the grading of the source mesh along.
    ///   2. Whatever the affine transform cannot match is left as a residual displacement on the
    ///      boundary. That residual is extended harmonically into the interior and added.
    ///
    /// If a layer really is an affine image of the source, the residual is zero and the layer is
    /// exact. Otherwise the correction is small and smooth, which is also why the conjugate
    /// gradient solves converge in a few iterations once they warm start from the previous layer.
    ///
    /// This replaces the original scheme of accumulating averaged neighbour offsets and then
    /// smoothing the offsets: that integrated a differential quantity outwards from the boundary
    /// and drifted, and it could not represent a rotating or tapering layer at all.
    /// </summary>
    public static class SweepLayerMorph
    {
        // Methods
        public static void MorphLayers(SweepMeshData data, SweepLaplacian laplacian, int maxIterations,
                                       double tolerance)
        {
            int n = data.NumNodes;
            double[] coords = data.Coords;
            bool[] isBoundary = data.IsBoundaryNode;
            int[] interior = data.InteriorNodes;
            double[,] a = new double[3, 3];
            double[] sourceCenter = new double[3];
            double[] layerCenter = new double[3];
            // Holds the boundary residual on entry and the harmonic extension on exit. It is
            // allocated once so that every layer warm starts from the previous one.
            double[] displacement = new double[3 * n];
            //
            for (int layer = 1; layer < data.NumLayers; layer++)
            {
                FitAffine(data, layer, a, sourceCenter, layerCenter);
                // Boundary residual that the affine transform could not match
                for (int i = 0; i < n; i++)
                {
                    if (!isBoundary[i]) continue;
                    //
                    int s = 3 * i;
                    int t = 3 * data.Index(layer, i);
                    double dx = coords[s] - sourceCenter[0];
                    double dy = coords[s + 1] - sourceCenter[1];
                    double dz = coords[s + 2] - sourceCenter[2];
                    displacement[s] = coords[t] - (a[0, 0] * dx + a[0, 1] * dy + a[0, 2] * dz + layerCenter[0]);
                    displacement[s + 1] = coords[t + 1] - (a[1, 0] * dx + a[1, 1] * dy + a[1, 2] * dz + layerCenter[1]);
                    displacement[s + 2] = coords[t + 2] - (a[2, 0] * dx + a[2, 1] * dy + a[2, 2] * dz + layerCenter[2]);
                }
                //
                laplacian.SolveHarmonic(displacement, isBoundary, maxIterations, tolerance);
                // Affine prediction plus the harmonic correction
                for (int k = 0; k < interior.Length; k++)
                {
                    int i = interior[k];
                    int s = 3 * i;
                    int t = 3 * data.Index(layer, i);
                    double dx = coords[s] - sourceCenter[0];
                    double dy = coords[s + 1] - sourceCenter[1];
                    double dz = coords[s + 2] - sourceCenter[2];
                    coords[t] = a[0, 0] * dx + a[0, 1] * dy + a[0, 2] * dz + layerCenter[0] + displacement[s];
                    coords[t + 1] = a[1, 0] * dx + a[1, 1] * dy + a[1, 2] * dz + layerCenter[1] + displacement[s + 1];
                    coords[t + 2] = a[2, 0] * dx + a[2, 1] * dy + a[2, 2] * dz + layerCenter[2] + displacement[s + 2];
                }
            }
        }
        /// <summary>
        /// Least squares affine transform taking the source boundary nodes onto their positions on
        /// the given layer. Both point sets are centred first, so the translation is exact and only
        /// the 3x3 linear part has to be solved for.
        ///
        /// A planar source boundary leaves the out of plane column of the linear part undetermined,
        /// which is why the fit is regularised towards the identity. The regularisation is scaled
        /// by the spread of the source points so that it stays negligible in the directions that
        /// are well determined.
        /// </summary>
        private static void FitAffine(SweepMeshData data, int layer, double[,] a, double[] sourceCenter,
                                      double[] layerCenter)
        {
            int n = data.NumNodes;
            double[] coords = data.Coords;
            bool[] isBoundary = data.IsBoundaryNode;
            //
            int count = 0;
            sourceCenter[0] = sourceCenter[1] = sourceCenter[2] = 0;
            layerCenter[0] = layerCenter[1] = layerCenter[2] = 0;
            for (int i = 0; i < n; i++)
            {
                if (!isBoundary[i]) continue;
                //
                int s = 3 * i;
                int t = 3 * data.Index(layer, i);
                sourceCenter[0] += coords[s];
                sourceCenter[1] += coords[s + 1];
                sourceCenter[2] += coords[s + 2];
                layerCenter[0] += coords[t];
                layerCenter[1] += coords[t + 1];
                layerCenter[2] += coords[t + 2];
                count++;
            }
            for (int i = 0; i < 3; i++)
            {
                sourceCenter[i] /= count;
                layerCenter[i] /= count;
            }
            // Covariance of the source points and cross covariance with the layer points
            double[,] c = new double[3, 3];
            double[,] d = new double[3, 3];
            double[] u = new double[3];
            double[] v = new double[3];
            for (int i = 0; i < n; i++)
            {
                if (!isBoundary[i]) continue;
                //
                int s = 3 * i;
                int t = 3 * data.Index(layer, i);
                u[0] = coords[s] - sourceCenter[0];
                u[1] = coords[s + 1] - sourceCenter[1];
                u[2] = coords[s + 2] - sourceCenter[2];
                v[0] = coords[t] - layerCenter[0];
                v[1] = coords[t + 1] - layerCenter[1];
                v[2] = coords[t + 2] - layerCenter[2];
                for (int r = 0; r < 3; r++)
                {
                    for (int q = 0; q < 3; q++)
                    {
                        c[r, q] += u[r] * u[q];
                        d[r, q] += v[r] * u[q];
                    }
                }
            }
            // Regularise towards the identity
            double lambda = 1E-6 * (c[0, 0] + c[1, 1] + c[2, 2]);
            if (lambda <= 0) lambda = 1E-30;
            for (int i = 0; i < 3; i++)
            {
                c[i, i] += lambda;
                d[i, i] += lambda;
            }
            //
            double[,] inverse = new double[3, 3];
            if (!Invert3x3(c, inverse))
            {
                // Degenerate boundary - fall back to a pure translation
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++) a[i, j] = i == j ? 1 : 0;
                }
                return;
            }
            //
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    a[i, j] = d[i, 0] * inverse[0, j] + d[i, 1] * inverse[1, j] + d[i, 2] * inverse[2, j];
                }
            }
        }
        private static bool Invert3x3(double[,] m, double[,] inverse)
        {
            double c00 = m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1];
            double c01 = m[1, 2] * m[2, 0] - m[1, 0] * m[2, 2];
            double c02 = m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0];
            double det = m[0, 0] * c00 + m[0, 1] * c01 + m[0, 2] * c02;
            //
            double scale = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) scale = Math.Max(scale, Math.Abs(m[i, j]));
            }
            if (scale <= 0 || Math.Abs(det) < 1E-12 * scale * scale * scale) return false;
            //
            double invDet = 1.0 / det;
            inverse[0, 0] = c00 * invDet;
            inverse[1, 0] = c01 * invDet;
            inverse[2, 0] = c02 * invDet;
            inverse[0, 1] = (m[0, 2] * m[2, 1] - m[0, 1] * m[2, 2]) * invDet;
            inverse[1, 1] = (m[0, 0] * m[2, 2] - m[0, 2] * m[2, 0]) * invDet;
            inverse[2, 1] = (m[0, 1] * m[2, 0] - m[0, 0] * m[2, 1]) * invDet;
            inverse[0, 2] = (m[0, 1] * m[1, 2] - m[0, 2] * m[1, 1]) * invDet;
            inverse[1, 2] = (m[0, 2] * m[1, 0] - m[0, 0] * m[1, 2]) * invDet;
            inverse[2, 2] = (m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0]) * invDet;
            //
            return true;
        }
    }
}
