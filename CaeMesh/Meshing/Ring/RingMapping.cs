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
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The structured mapping of a face of two boundary loops that run roughly parallel, the same for every mesher. Both
    /// loops carry N nodes and run the same way round; node i of the outer loop is joined to a node of the inner loop and
    /// the lines between them are cut into the same number of layers of quads, so the grid has no corner at all.
    /// The nodes are placed in the parameter space, in the frame of ThreeBlockMapping that is close to isometric, fitted
    /// over both loops. The inner loop is turned by the cyclic shift that minimises the sum of the squared distances
    /// between the nodes joined, so that a seam or a vertex that sits elsewhere on one loop does not twist the ring;
    /// ties go to the smallest shift. The nodes of the layers are interpolated linearly along the lines. Only a grid
    /// that folds or has a quad corner below SubmapFaceDetector.MinScaledJacobian is smoothed: its folded quads are
    /// untangled and the condition energy descent of QuadSmoothing evens out the quads without folding one, and the
    /// interpolated grid is kept when the smoothed one folds more quads or, folding as many, has a worse corner. The
    /// descent costs a minute on a ring of 16000 quads, and on rings that do not fold it moves the worst corner by a few
    /// hundredths at most, since that corner lies on a loop where the lines meet it.
    /// The number of layers is the mean distance between the nodes joined over the mean element length along the two
    /// loops, rounded, at least one (GetNumberOfLayers).
    /// The arithmetic is plain and ordered, so that a port gives the same grid.
    /// </summary>
    public static class RingMapping
    {
        // Variables                                                                                                                
        /// <summary>
        /// How small the area a loop encloses in the parameter space may be against the square of its length, 4 pi A / P²,
        /// before the parameters are taken not to run around it, as on a surface whose parameters are periodic.
        /// </summary>
        private const double MinLoopRoundness = 1E-4;


        // Methods                                                                                                                  
        /// <summary>
        /// The shift s that joins node i of the outer loop to node (i + s) mod N of the inner loop with the smallest sum
        /// of the squared distances between the points; the smallest of equal sums.
        /// </summary>
        public static int GetShift(double[][] outerPoints, double[][] innerPoints)
        {
            int n = outerPoints.Length;
            int bestShift = 0;
            double bestSum = double.MaxValue;
            for (int s = 0; s < n; s++)
            {
                double sum = 0;
                for (int i = 0; i < n && sum < bestSum; i++)
                {
                    double[] a = outerPoints[i];
                    double[] b = innerPoints[(i + s) % n];
                    for (int d = 0; d < a.Length; d++) sum += (b[d] - a[d]) * (b[d] - a[d]);
                }
                if (sum < bestSum)
                {
                    bestSum = sum;
                    bestShift = s;
                }
            }
            return bestShift;
        }
        /// <summary>
        /// The number of layers of the ring whose loops run through the nodes of the given parameters and coordinates, as
        /// Map takes them: the mean distance in space between the nodes the shift of Map joins, over the mean length of
        /// the segments of both loops, rounded, at least one.
        /// </summary>
        public static int GetNumberOfLayers(double[][] outerUv, double[][] outerXyz, double[][] innerUv,
                                            double[][] innerXyz)
        {
            CheckLoops(outerUv, outerXyz, innerUv, innerXyz);
            int n = outerUv.Length;
            double[] w = GetFrame(outerUv, outerXyz, innerUv, innerXyz);
            int shift = GetShift(ToFrame(w, outerUv), ToFrame(w, innerUv));
            double distance = 0;
            double length = 0;
            for (int i = 0; i < n; i++)
            {
                distance += Distance(outerXyz[i], innerXyz[(i + shift) % n]);
                length += Distance(outerXyz[i], outerXyz[(i + 1) % n]) + Distance(innerXyz[i], innerXyz[(i + 1) % n]);
            }
            if (!(length > 0)) return 1;
            return Math.Max(1, (int)Math.Round(distance / (0.5 * length)));
        }
        /// <summary>
        /// Maps the ring whose outer loop runs through the nodes of the parameters outerUv and the coordinates outerXyz
        /// and whose inner loop, the same way round, through innerUv and innerXyz, N nodes each in loop order, the first
        /// not repeated at the end, with numOfLayers layers of quads between them. evaluate takes the parameters of nodes
        /// as u0, v0, u1, v1, ... and returns their coordinates as x0, y0, z0, x1, ...; without it the grid carries no
        /// coordinates of the inner nodes.
        /// </summary>
        public static RingGrid Map(double[][] outerUv, double[][] outerXyz, double[][] innerUv, double[][] innerXyz,
                                   int numOfLayers, Func<double[], double[]> evaluate)
        {
            CheckLoops(outerUv, outerXyz, innerUv, innerXyz);
            if (numOfLayers < 1) throw new CaeException("The ring needs at least one layer of quads, not " + numOfLayers + ".");
            int n = outerUv.Length;
            double[] w = GetFrame(outerUv, outerXyz, innerUv, innerXyz);
            double[][] outerPoints = ToFrame(w, outerUv);
            double[][] innerPoints = ToFrame(w, innerUv);
            double outerArea = GetSignedArea(outerPoints);
            double innerArea = GetSignedArea(innerPoints);
            if (!IsRound(outerPoints, outerArea) || !IsRound(innerPoints, innerArea))
                throw new CaeException("The parameters of the face do not run around its loops.");
            if (Math.Sign(outerArea) != Math.Sign(innerArea))
                throw new CaeException("The two loops of the face do not run the same way round.");
            int shift = GetShift(outerPoints, innerPoints);
            // The nodes of the layers on the lines between the nodes joined
            int numOfInnerNodes = (numOfLayers - 1) * n;
            double[][] points = new double[2 * n + numOfInnerNodes][];
            for (int i = 0; i < n; i++)
            {
                points[i] = outerPoints[i];
                points[n + i] = innerPoints[i];
            }
            for (int l = 1; l < numOfLayers; l++)
            {
                double f = (double)l / numOfLayers;
                for (int i = 0; i < n; i++)
                {
                    double[] a = outerPoints[i];
                    double[] b = innerPoints[(i + shift) % n];
                    points[2 * n + (l - 1) * n + i] = new double[] { a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f };
                }
            }
            RingGrid layout = new RingGrid(n, numOfLayers, shift, new double[0][], null, null, 0, 0);
            List<int[]> quadList = new List<int[]>();
            for (int l = 0; l < numOfLayers; l++)
            {
                for (int i = 0; i < n; i++)
                    quadList.Add(new int[] { layout.GetNodeId(l, i), layout.GetNodeId(l, i + 1), layout.GetNodeId(l + 1, i + 1),
                                             layout.GetNodeId(l + 1, i) });
            }
            int[][] quads = quadList.ToArray();
            double orientation = outerArea < 0 ? -1 : 1;
            // A grid that folds or is poor evened out, untangled first where it folds
            int numOfBoundaryNodes = 2 * n;
            int numOfFoldedQuads = QuadSmoothing.CountFoldedElements(points, quads, orientation);
            double minScaledJacobian = QuadSmoothing.GetMinScaledJacobian(points, quads, orientation);
            if (numOfInnerNodes > 0 &&
                (numOfFoldedQuads > 0 || minScaledJacobian < SubmapFaceDetector.MinScaledJacobian))
            {
                double[][] smoothed = new double[points.Length][];
                for (int p = 0; p < points.Length; p++) smoothed[p] = new double[] { points[p][0], points[p][1] };
                int[][] neighbours = ThreeBlockMapping.GetNeighbours(points.Length, quadList);
                int[] cornerStart;
                int[] cornerQuads;
                int[] cornerIndices;
                QuadSmoothing.GetCorners(quads, points.Length, numOfBoundaryNodes, out cornerStart, out cornerQuads,
                                         out cornerIndices);
                if (numOfFoldedQuads > 0)
                    QuadSmoothing.Untangle(smoothed, neighbours, quads, cornerStart, cornerQuads, cornerIndices,
                                           numOfBoundaryNodes, orientation);
                QuadSmoothing.LowerConditionEnergy(smoothed, neighbours, quads, cornerStart, cornerQuads, cornerIndices,
                                                   numOfBoundaryNodes, orientation);
                int smoothedFolds = QuadSmoothing.CountFoldedElements(smoothed, quads, orientation);
                double smoothedQuality = QuadSmoothing.GetMinScaledJacobian(smoothed, quads, orientation);
                if (smoothedFolds < numOfFoldedQuads ||
                    (smoothedFolds == numOfFoldedQuads && smoothedQuality >= minScaledJacobian))
                {
                    points = smoothed;
                    numOfFoldedQuads = smoothedFolds;
                    minScaledJacobian = smoothedQuality;
                }
            }
            //
            double[][] innerUvs = new double[numOfInnerNodes][];
            for (int p = 0; p < numOfInnerNodes; p++) innerUvs[p] = ThreeBlockMapping.FromFrame(w, points[numOfBoundaryNodes + p]);
            double[][] innerXyzs = null;
            if (evaluate != null)
            {
                double[] flatUv = new double[2 * numOfInnerNodes];
                for (int p = 0; p < numOfInnerNodes; p++)
                {
                    flatUv[2 * p] = innerUvs[p][0];
                    flatUv[2 * p + 1] = innerUvs[p][1];
                }
                double[] flatXyz = numOfInnerNodes > 0 ? evaluate(flatUv) : new double[0];
                if (flatXyz == null || flatXyz.Length != 3 * numOfInnerNodes)
                    throw new CaeException("The inner nodes of the face could not be placed on its surface.");
                innerXyzs = new double[numOfInnerNodes][];
                for (int p = 0; p < numOfInnerNodes; p++)
                    innerXyzs[p] = new double[] { flatXyz[3 * p], flatXyz[3 * p + 1], flatXyz[3 * p + 2] };
            }
            return new RingGrid(n, numOfLayers, shift, innerUvs, innerXyzs, quads, numOfFoldedQuads, minScaledJacobian);
        }
        private static void CheckLoops(double[][] outerUv, double[][] outerXyz, double[][] innerUv, double[][] innerXyz)
        {
            if (outerUv == null || outerXyz == null || innerUv == null || innerXyz == null || outerUv.Length != outerXyz.Length ||
                innerUv.Length != innerXyz.Length)
                throw new CaeException("The loops of the face are given by different numbers of parameters and coordinates.");
            if (outerUv.Length != innerUv.Length)
                throw new CaeException("The outer loop of the face carries " + outerUv.Length + " elements and its inner " +
                                       "loop " + innerUv.Length + ", which must be equal.");
            if (outerUv.Length < 3)
                throw new CaeException("The loops of the face carry " + outerUv.Length + " elements, fewer than three.");
        }
        /// <summary>
        /// The frame of ThreeBlockMapping.GetMetricFrame fitted over the segments of both loops, each closed.
        /// </summary>
        private static double[] GetFrame(double[][] outerUv, double[][] outerXyz, double[][] innerUv, double[][] innerXyz)
        {
            return ThreeBlockMapping.GetMetricFrame(new double[][][] { Close(outerUv), Close(innerUv), new double[0][] },
                                                    new double[][][] { Close(outerXyz), Close(innerXyz), new double[0][] });
        }
        private static double[][] Close(double[][] loop)
        {
            double[][] closed = new double[loop.Length + 1][];
            Array.Copy(loop, closed, loop.Length);
            closed[loop.Length] = loop[0];
            return closed;
        }
        private static double[][] ToFrame(double[] w, double[][] uv)
        {
            double[][] points = new double[uv.Length][];
            for (int i = 0; i < uv.Length; i++) points[i] = ThreeBlockMapping.ToFrame(w, uv[i]);
            return points;
        }
        private static double GetSignedArea(double[][] points)
        {
            return ThreeBlockMapping.GetSignedArea(points, points.Length);
        }
        /// <summary>
        /// Whether the loop encloses an area: 4 pi A / P² at least MinLoopRoundness.
        /// </summary>
        private static bool IsRound(double[][] points, double signedArea)
        {
            double perimeter = 0;
            for (int i = 0; i < points.Length; i++)
            {
                double[] a = points[i];
                double[] b = points[(i + 1) % points.Length];
                perimeter += Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]));
            }
            return perimeter > 0 && 4 * Math.PI * Math.Abs(signedArea) >= MinLoopRoundness * perimeter * perimeter;
        }
        private static double Distance(double[] a, double[] b)
        {
            double sum = 0;
            for (int d = 0; d < a.Length; d++) sum += (b[d] - a[d]) * (b[d] - a[d]);
            return Math.Sqrt(sum);
        }
    }
}
