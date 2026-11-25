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
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The structured mapping of a four-sided face whose opposite sides carry equal numbers of elements: one block of
    /// quads, placed as ThreeBlockMapping places its blocks - in a frame of the parameters fitted to the lengths of the
    /// boundary segments, first by the transfinite interpolation of the sides, then the barycentric (Tutte) embedding
    /// and the condition energy lowered node by node, so that no step folds a quad.
    /// </summary>
    public static class FourSidedMapping
    {
        // Variables                                                                                                                
        private const int MaxNumOfSweeps = 20;


        // Methods                                                                                                                  
        /// <summary>
        /// Maps the face whose side k runs from corner k to corner k + 1 through the nodes sideUv[k] in the parameter
        /// space and sideXyz[k] in space; the last node of a side is the first of the next. evaluate takes the
        /// parameters of nodes as u0, v0, u1, v1, ... and returns their coordinates as x0, y0, z0, x1, ...; without
        /// it the grid carries no coordinates of the inner nodes.
        /// </summary>
        public static ThreeBlockGrid Map(double[][][] sideUv, double[][][] sideXyz, Func<double[], double[]> evaluate)
        {
            int[] s = new int[4];
            for (int k = 0; k < 4; k++)
            {
                s[k] = sideUv[k].Length - 1;
                if (sideXyz[k].Length != sideUv[k].Length)
                    throw new CaeException("Side " + k + " of the face has " + sideUv[k].Length + " parameters and " +
                                           sideXyz[k].Length + " coordinates of its nodes.");
            }
            if (s[0] != s[2] || s[1] != s[3] || s.Min() < 1)
                throw new CaeException("The sides of the face carry " + string.Join(", ", s) + " elements, which one " +
                                       "block of quads cannot fill.");
            int a = s[0];
            int b = s[1];
            int numOfBoundaryNodes = 2 * (a + b);
            int[] offsets = new int[] { 0, a, a + b, 2 * a + b };
            Func<int, int, int> boundaryId = (k, i) => i == s[k] ? offsets[(k + 1) % 4] : offsets[k] + i;
            double[] w = ThreeBlockMapping.GetMetricFrame(sideUv, sideXyz);
            List<double[]> y = new List<double[]>();
            for (int k = 0; k < 4; k++)
            {
                for (int i = 0; i < s[k]; i++) y.Add(ThreeBlockMapping.ToFrame(w, sideUv[k][i]));
            }
            // Side 0 along i at j = 0, side 1 along j at i = a, side 2 back along i at j = b, side 3 back at i = 0
            int[,] ids = new int[a + 1, b + 1];
            for (int i = 0; i <= a; i++)
            {
                ids[i, 0] = boundaryId(0, i);
                ids[i, b] = boundaryId(2, a - i);
            }
            for (int j = 0; j <= b; j++)
            {
                ids[a, j] = boundaryId(1, j);
                ids[0, j] = boundaryId(3, b - j);
            }
            for (int i = 1; i < a; i++)
            {
                for (int j = 1; j < b; j++)
                {
                    ids[i, j] = y.Count;
                    y.Add(GetTransfinitePoint(y, ids, a, b, i, j));
                }
            }
            List<int[]> quads = new List<int[]>();
            for (int i = 0; i < a; i++)
            {
                for (int j = 0; j < b; j++)
                    quads.Add(new int[] { ids[i, j], ids[i + 1, j], ids[i + 1, j + 1], ids[i, j + 1] });
            }
            //
            double[][] points = y.ToArray();
            int[][] quadArray = quads.ToArray();
            int[][] neighbours = ThreeBlockMapping.GetNeighbours(points.Length, quads);
            ThreeBlockMapping.SolveBarycentricEmbedding(points, neighbours, numOfBoundaryNodes);
            double orientation = ThreeBlockMapping.GetSignedArea(points, numOfBoundaryNodes) < 0 ? -1 : 1;
            int[] cornerStart;
            int[] cornerQuads;
            int[] cornerIndices;
            QuadSmoothing.GetCorners(quadArray, points.Length, numOfBoundaryNodes, out cornerStart, out cornerQuads,
                                     out cornerIndices);
            QuadSmoothing.LowerConditionEnergy(points, neighbours, quadArray, cornerStart, cornerQuads, cornerIndices,
                                               numOfBoundaryNodes, orientation, MaxNumOfSweeps);
            //
            int numOfFoldedQuads = 0;
            foreach (var quad in quadArray)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (QuadSmoothing.GetScaledJacobian(points, quad, c, orientation) <= 0)
                    {
                        numOfFoldedQuads++;
                        break;
                    }
                }
            }
            //
            int numOfInnerNodes = points.Length - numOfBoundaryNodes;
            double[][] innerUv = new double[numOfInnerNodes][];
            for (int i = 0; i < numOfInnerNodes; i++)
                innerUv[i] = ThreeBlockMapping.FromFrame(w, points[numOfBoundaryNodes + i]);
            double[][] innerXyz = null;
            if (evaluate != null)
            {
                double[] flatXyz = numOfInnerNodes > 0 ? evaluate(innerUv.SelectMany(uv => uv).ToArray()) :
                                                         new double[0];
                if (flatXyz == null || flatXyz.Length != 3 * numOfInnerNodes)
                    throw new CaeException("The inner nodes of the face could not be placed on its surface.");
                innerXyz = new double[numOfInnerNodes][];
                for (int i = 0; i < numOfInnerNodes; i++)
                    innerXyz[i] = new double[] { flatXyz[3 * i], flatXyz[3 * i + 1], flatXyz[3 * i + 2] };
            }
            return new ThreeBlockGrid(s, innerUv, innerXyz, quadArray, numOfFoldedQuads);
        }
        /// <summary>
        /// The transfinite (Coons) interpolation of the block between its four sides, by the index along each.
        /// </summary>
        private static double[] GetTransfinitePoint(List<double[]> y, int[,] ids, int a, int b, int i, int j)
        {
            double xi = (double)i / a;
            double eta = (double)j / b;
            double[] p = new double[2];
            for (int d = 0; d < 2; d++)
            {
                p[d] = (1 - eta) * y[ids[i, 0]][d] + eta * y[ids[i, b]][d] + (1 - xi) * y[ids[0, j]][d] +
                       xi * y[ids[a, j]][d] -
                       ((1 - xi) * (1 - eta) * y[ids[0, 0]][d] + xi * (1 - eta) * y[ids[a, 0]][d] +
                        xi * eta * y[ids[a, b]][d] + (1 - xi) * eta * y[ids[0, b]][d]);
            }
            return p;
        }
    }
}
