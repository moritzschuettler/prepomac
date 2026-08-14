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
    /// Scratch space for the quality measures. The smoother evaluates the elements around a node
    /// twice for every trial move, so the buffers are allocated once by the caller and reused
    /// rather than being allocated inside the measure itself.
    /// </summary>
    public class SweepQualityBuffers
    {
        public readonly int[] Nodes = new int[8];
        public readonly double[] E1 = new double[3];
        public readonly double[] E2 = new double[3];
        public readonly double[] E3 = new double[3];
    }
    /// <summary>
    /// Element validity and shape measures for the swept solid elements.
    ///
    /// A swept block is a tensor product of the source mesh and the layer table, so the solid
    /// elements incident to any sweep node follow from the source connectivity alone: they are the
    /// source elements around that source node, taken on the layer below and the layer above. No
    /// separate three dimensional node to element map is needed, which is what makes the quality
    /// guard in the smoother cheap enough to run on every trial move.
    ///
    /// The scaled Jacobian below is the determinant of the three edge vectors at a corner divided
    /// by their lengths, minimised over the corners. It is not normalised so that an ideal element
    /// scores exactly one - for a prism it cannot be - but it is monotone in element distortion and
    /// it is negative exactly when the element is inverted, which is what both the smoother and the
    /// final report need.
    /// </summary>
    public static class SweepQuality
    {
        // Variables
        private static readonly int[,] _hexaCorners = new int[8, 3]
        {
            { 1, 3, 4 }, { 2, 0, 5 }, { 3, 1, 6 }, { 0, 2, 7 },
            { 7, 5, 0 }, { 4, 6, 1 }, { 5, 7, 2 }, { 6, 4, 3 }
        };
        private static readonly int[,] _wedgeCorners = new int[6, 3]
        {
            { 1, 2, 3 }, { 2, 0, 4 }, { 0, 1, 5 },
            { 5, 4, 0 }, { 3, 5, 1 }, { 4, 3, 2 }
        };


        // Methods
        /// <summary>
        /// Fills nodes with the sweep node indices of the solid element that sits between
        /// bottomLayer and bottomLayer + 1 on the given source element.
        /// </summary>
        public static void GetElementNodes(SweepMeshData data, int elementIndex, int bottomLayer, bool flip,
                                           int[] nodes)
        {
            int[] element = data.SourceElements[elementIndex];
            int n = element.Length;
            int lower = flip ? bottomLayer + 1 : bottomLayer;
            int upper = flip ? bottomLayer : bottomLayer + 1;
            for (int i = 0; i < n; i++)
            {
                nodes[i] = data.Index(lower, element[i]);
                nodes[n + i] = data.Index(upper, element[i]);
            }
        }
        /// <summary>
        /// Decides once for the whole block whether the source element winding produces solid
        /// elements with a positive or a negative volume, so the emitted connectivity is valid
        /// regardless of how the source faces happen to be oriented in the imported geometry.
        /// </summary>
        public static bool ComputeOrientationFlip(SweepMeshData data)
        {
            double sum = 0;
            SweepQualityBuffers buffers = new SweepQualityBuffers();
            for (int e = 0; e < data.SourceElements.Length; e++)
            {
                GetElementNodes(data, e, 0, false, buffers.Nodes);
                sum += SignedVolume(data.Coords, buffers, data.SourceElements[e].Length);
            }
            return sum < 0;
        }
        public static double SignedVolume(double[] coords, SweepQualityBuffers buffers, int numSourceNodes)
        {
            int[] nodes = buffers.Nodes;
            if (numSourceNodes == 3)
            {
                // A prism splits into three tetrahedra that all have a positive volume for the
                // reference element
                return TetraVolume(coords, buffers, nodes[0], nodes[1], nodes[2], nodes[3]) +
                       TetraVolume(coords, buffers, nodes[1], nodes[2], nodes[3], nodes[4]) +
                       TetraVolume(coords, buffers, nodes[2], nodes[3], nodes[4], nodes[5]);
            }
            else
            {
                // Volume from the trilinear Jacobian at the element centre. The three sums below are
                // eight times the Jacobian columns, and the reference cube spans a volume of eight
                // in the parametric coordinates, so the determinant is divided by 8 * 8 * 8 / 8.
                double[] a = buffers.E1;
                double[] b = buffers.E2;
                double[] c = buffers.E3;
                for (int i = 0; i < 3; i++)
                {
                    a[i] = (coords[3 * nodes[1] + i] + coords[3 * nodes[2] + i] +
                            coords[3 * nodes[6] + i] + coords[3 * nodes[5] + i]) -
                           (coords[3 * nodes[0] + i] + coords[3 * nodes[3] + i] +
                            coords[3 * nodes[7] + i] + coords[3 * nodes[4] + i]);
                    b[i] = (coords[3 * nodes[3] + i] + coords[3 * nodes[2] + i] +
                            coords[3 * nodes[6] + i] + coords[3 * nodes[7] + i]) -
                           (coords[3 * nodes[0] + i] + coords[3 * nodes[1] + i] +
                            coords[3 * nodes[5] + i] + coords[3 * nodes[4] + i]);
                    c[i] = (coords[3 * nodes[4] + i] + coords[3 * nodes[5] + i] +
                            coords[3 * nodes[6] + i] + coords[3 * nodes[7] + i]) -
                           (coords[3 * nodes[0] + i] + coords[3 * nodes[1] + i] +
                            coords[3 * nodes[2] + i] + coords[3 * nodes[3] + i]);
                }
                return Determinant(a, b, c) / 64;
            }
        }
        public static double ScaledJacobian(double[] coords, SweepQualityBuffers buffers, int numSourceNodes)
        {
            int[] nodes = buffers.Nodes;
            double[] e1 = buffers.E1;
            double[] e2 = buffers.E2;
            double[] e3 = buffers.E3;
            bool wedge = numSourceNodes == 3;
            int numCorners = wedge ? 6 : 8;
            double minimum = double.MaxValue;
            //
            for (int corner = 0; corner < numCorners; corner++)
            {
                int c0 = nodes[corner];
                Subtract(coords, nodes[wedge ? _wedgeCorners[corner, 0] : _hexaCorners[corner, 0]], c0, e1);
                Subtract(coords, nodes[wedge ? _wedgeCorners[corner, 1] : _hexaCorners[corner, 1]], c0, e2);
                Subtract(coords, nodes[wedge ? _wedgeCorners[corner, 2] : _hexaCorners[corner, 2]], c0, e3);
                //
                double l1 = Length(e1);
                double l2 = Length(e2);
                double l3 = Length(e3);
                if (l1 <= 0 || l2 <= 0 || l3 <= 0) return 0;
                //
                double value = Determinant(e1, e2, e3) / (l1 * l2 * l3);
                if (value < minimum) minimum = value;
            }
            //
            return minimum;
        }
        /// <summary>
        /// Smallest scaled Jacobian over the solid elements that touch one sweep node.
        /// </summary>
        public static double MinQualityAroundNode(SweepMeshData data, SweepQualityBuffers buffers, int layer,
                                                  int node, bool flip)
        {
            double minimum = double.MaxValue;
            int firstLayer = Math.Max(0, layer - 1);
            int lastLayer = Math.Min(data.NumLayers - 2, layer);
            //
            for (int k = data.NodeElementStart[node]; k < data.NodeElementStart[node + 1]; k++)
            {
                int e = data.NodeElements[k];
                int numSourceNodes = data.SourceElements[e].Length;
                for (int bottom = firstLayer; bottom <= lastLayer; bottom++)
                {
                    GetElementNodes(data, e, bottom, flip, buffers.Nodes);
                    double value = ScaledJacobian(data.Coords, buffers, numSourceNodes);
                    if (value < minimum) minimum = value;
                }
            }
            //
            return minimum;
        }
        /// <summary>
        /// Smallest and largest scaled Jacobian over every solid element of the block.
        /// </summary>
        public static void GetQualityRange(SweepMeshData data, bool flip, out double minimum, out double maximum,
                                           out int numInverted)
        {
            SweepQualityBuffers buffers = new SweepQualityBuffers();
            minimum = double.MaxValue;
            maximum = double.MinValue;
            numInverted = 0;
            //
            for (int e = 0; e < data.SourceElements.Length; e++)
            {
                int numSourceNodes = data.SourceElements[e].Length;
                for (int bottom = 0; bottom < data.NumLayers - 1; bottom++)
                {
                    GetElementNodes(data, e, bottom, flip, buffers.Nodes);
                    double value = ScaledJacobian(data.Coords, buffers, numSourceNodes);
                    if (value < minimum) minimum = value;
                    if (value > maximum) maximum = value;
                    if (value <= 0) numInverted++;
                }
            }
            //
            if (minimum == double.MaxValue) { minimum = 0; maximum = 0; }
        }
        //
        private static double TetraVolume(double[] coords, SweepQualityBuffers buffers, int a, int b, int c, int d)
        {
            Subtract(coords, b, a, buffers.E1);
            Subtract(coords, c, a, buffers.E2);
            Subtract(coords, d, a, buffers.E3);
            return Determinant(buffers.E1, buffers.E2, buffers.E3) / 6;
        }
        private static void Subtract(double[] coords, int to, int from, double[] result)
        {
            result[0] = coords[3 * to] - coords[3 * from];
            result[1] = coords[3 * to + 1] - coords[3 * from + 1];
            result[2] = coords[3 * to + 2] - coords[3 * from + 2];
        }
        private static double Length(double[] v)
        {
            return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        }
        private static double Determinant(double[] a, double[] b, double[] c)
        {
            return a[0] * (b[1] * c[2] - b[2] * c[1]) -
                   a[1] * (b[0] * c[2] - b[2] * c[0]) +
                   a[2] * (b[0] * c[1] - b[1] * c[0]);
        }
    }
}
