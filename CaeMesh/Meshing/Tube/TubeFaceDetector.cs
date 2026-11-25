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

namespace CaeMesh
{
    /// <summary>
    /// Finds the faces of a part that are meshed as structured tubes: faces closed along exactly one seam and bounded by
    /// exactly two closed rims, the seam running from one rim to the other (VisualizationData.IsSurfaceACylinderLike),
    /// with no degenerate edge. Each rim starts at its vertex on the seam, and the second rim is turned to run the same
    /// way round as the first: the face lies on the other side of it, as the two loops of an annulus have their face on
    /// different sides.
    /// The edges are paired when the rims have as many and their vertices lie at the same shares of the length round
    /// from the seam, within MaxPairDeviation: the grid joins the nodes of the rims by their count from the seam, so
    /// edges that face each other this way carry the same number of elements and their vertices end on one line. A
    /// rim of one closed edge pairs with the other.
    /// A face is judged as RingFaceDetector judges its rings, in space: the tube grid is laid with counts in proportion to
    /// the lengths, one step for both rims, a pair of edges taking the mean of its two lengths and an unpaired tube the
    /// same total spread over each rim, the seam divided at the same step into at most MaxNumberOfLayers layers. The
    /// offsets of the nodes of each rim from the seam are interpolated between the rims along the seam, once in a fixed
    /// frame, which lays a surface of revolution of any profile, and once in a frame carried along the seam without
    /// twisting (rotation minimising, by double reflection), which lays a bend; the better of the two judges. The face
    /// is left free when that grid folds - a quad turned against one beside it - or has a quad corner below
    /// SubmapFaceDetector.MinScaledJacobian against the normal of the quad.
    /// </summary>
    public static class TubeFaceDetector
    {
        // Variables                                                                                                                
        /// <summary>
        /// The largest difference between the shares of the length round from the seam at which the vertices of the two
        /// rims lie for their edges to face each other.
        /// </summary>
        public const double MaxPairDeviation = 0.05;
        /// <summary>
        /// The most layers of the grid the detection lays along the seam; the corners hardly change with the length of
        /// the quads along it.
        /// </summary>
        public const int MaxNumberOfLayers = 64;
        private const int NumOfDivisions = 64;
        private const double MaxDegenerateLength = 1E-9;


        // Methods                                                                                                                  
        public static TubeFace[] Detect(FeMesh mesh, BasePart part)
        {
            VisualizationData vis = part.Visualization;
            if (vis == null || vis.FaceCount == 0 || vis.FaceEdgeIds == null || vis.CellIdsByFace == null ||
                vis.EdgeLengths == null || vis.FaceAreas == null) return new TubeFace[0];
            //
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            List<TubeFace> tubeFaces = new List<TubeFace>();
            for (int i = 0; i < vis.FaceCount; i++)
            {
                TubeFace tubeFace = DetectFace(mesh, vis, i, edgeIdVertexIds);
                if (tubeFace != null) tubeFaces.Add(tubeFace);
            }
            return tubeFaces.ToArray();
        }
        private static TubeFace DetectFace(FeMesh mesh, VisualizationData vis, int faceId, Dictionary<int, int[]> edgeIdVertexIds)
        {
            if (faceId >= vis.CellIdsByFace.Length || vis.FaceEdgeIds[faceId] == null) return null;
            int seamEdgeId;
            if (!vis.IsSurfaceACylinderLike(faceId, out seamEdgeId)) return null;
            int[] rimEdgeIds = vis.FaceEdgeIds[faceId].Distinct().Where(edgeId => edgeId != seamEdgeId).ToArray();
            double faceSize = Math.Sqrt(Math.Max(0, vis.FaceAreas[faceId]));
            foreach (var edgeId in rimEdgeIds.Concat(new int[] { seamEdgeId }))
            {
                if (!(vis.EdgeLengths[edgeId] > MaxDegenerateLength * faceSize)) return null;
            }
            List<int[]> loopsEdgeIds;
            List<int[]> loopsVertexIds;
            if (!SubmapFaceDetector.GetBoundaryLoops(vis, rimEdgeIds, edgeIdVertexIds, out loopsEdgeIds, out loopsVertexIds) ||
                loopsEdgeIds.Count != 2) return null;
            int[] seamVertexIds = vis.GetVertexNodeIdsForEdgeId(seamEdgeId);
            if (seamVertexIds == null || seamVertexIds.Length != 2) return null;
            // Each rim from its vertex on the seam
            int[][] rimEdges = new int[2][];
            int[][] rimVertices = new int[2][];
            for (int r = 0; r < 2; r++)
            {
                for (int l = 0; l < 2 && rimEdges[r] == null; l++)
                {
                    int start = Array.IndexOf(loopsVertexIds[l], seamVertexIds[r]);
                    if (start < 0) continue;
                    int n = loopsEdgeIds[l].Length;
                    rimEdges[r] = new int[n];
                    rimVertices[r] = new int[n];
                    for (int k = 0; k < n; k++)
                    {
                        rimEdges[r][k] = loopsEdgeIds[l][(start + k) % n];
                        rimVertices[r][k] = loopsVertexIds[l][(start + k) % n];
                    }
                }
                if (rimEdges[r] == null) return null;
            }
            if (rimEdges[0].Intersect(rimEdges[1]).Any()) return null;
            List<double[]>[][] polylines = new List<double[]>[2][];
            for (int r = 0; r < 2; r++)
            {
                polylines[r] = new List<double[]>[rimEdges[r].Length];
                for (int k = 0; k < rimEdges[r].Length; k++)
                {
                    polylines[r][k] = TransfiniteFaceDetector.GetEdgePoints(mesh, vis, rimEdges[r][k], rimVertices[r][k]);
                    if (polylines[r][k] == null || polylines[r][k].Count < 2) return null;
                }
            }
            List<double[]> seamPolyline = TransfiniteFaceDetector.GetEdgePoints(mesh, vis, seamEdgeId, seamVertexIds[0]);
            if (seamPolyline == null || seamPolyline.Count < 2) return null;
            // The second rim the same way round as the first: the face on its other side
            int firstSide = SubmapFaceDetector.GetFaceSide(vis, faceId, rimEdges[0][0], rimVertices[0][0]);
            int secondSide = SubmapFaceDetector.GetFaceSide(vis, faceId, rimEdges[1][0], rimVertices[1][0]);
            if (firstSide == 0 || secondSide == 0) return null;
            if (firstSide == secondSide)
            {
                int n = rimEdges[1].Length;
                int[] edgeIds = new int[n];
                int[] vertexIds = new int[n];
                List<double[]>[] reversed = new List<double[]>[n];
                for (int k = 0; k < n; k++)
                {
                    edgeIds[k] = rimEdges[1][n - 1 - k];
                    vertexIds[k] = rimVertices[1][(n - k) % n];
                    reversed[k] = new List<double[]>(polylines[1][n - 1 - k]);
                    reversed[k].Reverse();
                }
                rimEdges[1] = edgeIds;
                rimVertices[1] = vertexIds;
                polylines[1] = reversed;
            }
            //
            double[][] lengths = new double[2][];
            for (int r = 0; r < 2; r++)
            {
                lengths[r] = GetLengths(polylines[r]);
                if (lengths[r] == null) return null;
            }
            bool paired = ArePaired(lengths[0], lengths[1]);
            double quality = GetGridQuality(polylines, lengths, paired, seamPolyline);
            if (!(quality >= SubmapFaceDetector.MinScaledJacobian)) return null;
            return new TubeFace(faceId, seamEdgeId, rimVertices[0], rimEdges[0], rimVertices[1], rimEdges[1], paired);
        }
        /// <summary>
        /// Whether the rims of the given edge lengths, in order from the seam, have as many edges and their vertices at the
        /// same shares of their lengths within MaxPairDeviation.
        /// </summary>
        private static bool ArePaired(double[] firstLengths, double[] secondLengths)
        {
            int n = firstLengths.Length;
            if (secondLengths.Length != n) return false;
            double firstTotal = firstLengths.Sum();
            double secondTotal = secondLengths.Sum();
            double firstSum = 0;
            double secondSum = 0;
            for (int k = 0; k < n - 1; k++)
            {
                firstSum += firstLengths[k];
                secondSum += secondLengths[k];
                if (Math.Abs(firstSum / firstTotal - secondSum / secondTotal) > MaxPairDeviation) return false;
            }
            return true;
        }
        private static double[] GetLengths(List<double[]>[] polylines)
        {
            double[] lengths = new double[polylines.Length];
            for (int k = 0; k < lengths.Length; k++)
            {
                TransfiniteFaceDetector.Resample(polylines[k], 1, out lengths[k]);
                if (!(lengths[k] > 0)) return null;
            }
            return lengths;
        }
        /// <summary>
        /// The smallest scaled Jacobian in space of a quad corner of the tube grid laid with the rims carried along the
        /// seam in a fixed frame or in frames that turn with it, whichever gives the better grid; -1 when both fold or the
        /// grid cannot be made. The fixed frame lays a surface of revolution of any profile, the turning one a bend.
        /// </summary>
        private static double GetGridQuality(List<double[]>[][] polylines, double[][] lengths, bool paired,
                                             List<double[]> seamPolyline)
        {
            double firstTotal = lengths[0].Sum();
            double secondTotal = lengths[1].Sum();
            double step = 0.5 * (firstTotal + secondTotal) / NumOfDivisions;
            int[][] counts = new int[2][];
            if (paired)
            {
                counts[0] = new int[lengths[0].Length];
                for (int k = 0; k < counts[0].Length; k++)
                    counts[0][k] = Math.Max(1, (int)Math.Round(0.5 * (lengths[0][k] + lengths[1][k]) / step));
                counts[1] = counts[0];
            }
            else
            {
                int total = Math.Max(Math.Max(lengths[0].Length, lengths[1].Length), NumOfDivisions);
                counts[0] = RingFaceDetector.GetProportionalCounts(lengths[0], total);
                counts[1] = RingFaceDetector.GetProportionalCounts(lengths[1], total);
            }
            double[][][] rims = new double[2][][];
            for (int r = 0; r < 2; r++) rims[r] = GetRimPoints(polylines[r], counts[r]);
            int n = rims[0].Length;
            if (n < 3 || rims[1].Length != n) return -1;
            double seamLength;
            TransfiniteFaceDetector.Resample(seamPolyline, 1, out seamLength);
            if (!(seamLength > 0)) return -1;
            int m = Math.Max(1, Math.Min(MaxNumberOfLayers, (int)Math.Round(seamLength / step)));
            double[][] seam = TransfiniteFaceDetector.Resample(seamPolyline, m, out seamLength);
            double[][][] frames = GetSeamFrames(seam);
            if (frames == null) return -1;
            //
            double best = -1;
            foreach (var turning in new bool[] { false, true })
            {
                double[][] firstOffsets = new double[n][];
                double[][] secondOffsets = new double[n][];
                for (int i = 0; i < n; i++)
                {
                    firstOffsets[i] = ToFrame(frames[0], Subtract(rims[0][i], seam[0]));
                    secondOffsets[i] = ToFrame(frames[turning ? m : 0], Subtract(rims[1][i], seam[m]));
                }
                double[][][] grid = new double[m + 1][][];
                for (int j = 0; j <= m; j++)
                {
                    grid[j] = new double[n][];
                    double s = (double)j / m;
                    for (int i = 0; i < n; i++)
                    {
                        if (j == 0) grid[j][i] = rims[0][i];
                        else if (j == m) grid[j][i] = rims[1][i];
                        else if (i == 0) grid[j][i] = seam[j];
                        else
                        {
                            double[] offset = new double[3];
                            for (int d = 0; d < 3; d++)
                                offset[d] = firstOffsets[i][d] + (secondOffsets[i][d] - firstOffsets[i][d]) * s;
                            grid[j][i] = FromFrame(frames[turning ? j : 0], seam[j], offset);
                        }
                    }
                }
                best = Math.Max(best, GetMinScaledJacobian(grid));
            }
            return best;
        }
        /// <summary>
        /// The smallest scaled Jacobian of a quad corner of the grid, line j of the nodes i round from the seam, against
        /// the normal of the quad by its diagonals; -1 when a quad turns against one beside it or has a corner that does.
        /// </summary>
        private static double GetMinScaledJacobian(double[][][] grid)
        {
            int m = grid.Length - 1;
            int n = grid[0].Length;
            double[][] quadNormals = new double[m * n][];
            for (int j = 0; j < m; j++)
            {
                for (int i = 0; i < n; i++)
                    quadNormals[j * n + i] = Cross(Subtract(grid[j + 1][(i + 1) % n], grid[j][i]),
                                                   Subtract(grid[j + 1][i], grid[j][(i + 1) % n]));
            }
            double min = 1;
            for (int j = 0; j < m; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    double[] quadNormal = quadNormals[j * n + i];
                    if (Dot(quadNormal, quadNormals[j * n + (i + 1) % n]) <= 0) return -1;
                    if (j + 1 < m && Dot(quadNormal, quadNormals[(j + 1) * n + i]) <= 0) return -1;
                    double length = Math.Sqrt(Dot(quadNormal, quadNormal));
                    if (!(length > 0)) return -1;
                    double[] normal = Scale(quadNormal, 1 / length);
                    int[][] corners = new int[][] { new int[] { j, i }, new int[] { j, (i + 1) % n },
                                                    new int[] { j + 1, (i + 1) % n }, new int[] { j + 1, i } };
                    for (int c = 0; c < 4; c++)
                    {
                        int[] at = corners[c];
                        int[] next = corners[(c + 1) % 4];
                        int[] previous = corners[(c + 3) % 4];
                        double sine = GetSine(grid[at[0]][at[1]], grid[next[0]][next[1]], grid[previous[0]][previous[1]],
                                              normal);
                        if (!(sine > 0)) return -1;
                        min = Math.Min(min, sine);
                    }
                }
            }
            return min;
        }
        /// <summary>
        /// The nodes of a rim from its vertex on the seam, every edge resampled into its count, the last node of an edge
        /// being the first of the next and not repeated.
        /// </summary>
        private static double[][] GetRimPoints(List<double[]>[] polylines, int[] counts)
        {
            List<double[]> points = new List<double[]>();
            double length;
            for (int k = 0; k < polylines.Length; k++)
            {
                double[][] samples = TransfiniteFaceDetector.Resample(polylines[k], counts[k], out length);
                for (int i = 0; i < counts[k]; i++) points.Add(samples[i]);
            }
            return points.ToArray();
        }
        /// <summary>
        /// A frame at every point of the seam, its tangent and two normals, carried from the first point by double
        /// reflection (Wang et al., Computation of rotation minimizing frames, 2008); null where the seam has no tangent.
        /// </summary>
        private static double[][][] GetSeamFrames(double[][] seam)
        {
            int m = seam.Length - 1;
            double[][] tangents = new double[m + 1][];
            for (int j = 0; j <= m; j++)
            {
                double[] tangent = Subtract(seam[Math.Min(m, j + 1)], seam[Math.Max(0, j - 1)]);
                double length = Math.Sqrt(Dot(tangent, tangent));
                if (!(length > 0)) return null;
                tangents[j] = Scale(tangent, 1 / length);
            }
            double[] axis = Math.Abs(tangents[0][0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double[] r = Subtract(axis, Scale(tangents[0], Dot(axis, tangents[0])));
            r = Scale(r, 1 / Math.Sqrt(Dot(r, r)));
            double[][][] frames = new double[m + 1][][];
            frames[0] = new double[][] { r, Cross(tangents[0], r), tangents[0] };
            for (int j = 0; j < m; j++)
            {
                double[] v1 = Subtract(seam[j + 1], seam[j]);
                double c1 = Dot(v1, v1);
                double[] rL = Subtract(r, Scale(v1, 2 / c1 * Dot(v1, r)));
                double[] tL = Subtract(tangents[j], Scale(v1, 2 / c1 * Dot(v1, tangents[j])));
                double[] v2 = Subtract(tangents[j + 1], tL);
                double c2 = Dot(v2, v2);
                r = c2 > 0 ? Subtract(rL, Scale(v2, 2 / c2 * Dot(v2, rL))) : rL;
                frames[j + 1] = new double[][] { r, Cross(tangents[j + 1], r), tangents[j + 1] };
            }
            return frames;
        }
        private static double[] ToFrame(double[][] frame, double[] vector)
        {
            return new double[] { Dot(frame[0], vector), Dot(frame[1], vector), Dot(frame[2], vector) };
        }
        private static double[] FromFrame(double[][] frame, double[] origin, double[] offset)
        {
            double[] point = new double[3];
            for (int d = 0; d < 3; d++)
                point[d] = origin[d] + frame[0][d] * offset[0] + frame[1][d] * offset[1] + frame[2][d] * offset[2];
            return point;
        }
        /// <summary>
        /// The sine of the angle at a corner from the edge to the next corner to the edge to the previous one, against the
        /// normal; negative where the corner turns against it.
        /// </summary>
        private static double GetSine(double[] p, double[] next, double[] previous, double[] normal)
        {
            double[] a = Subtract(next, p);
            double[] b = Subtract(previous, p);
            double lengths = Math.Sqrt(Dot(a, a) * Dot(b, b));
            if (!(lengths > 0)) return 0;
            return Dot(Cross(a, b), normal) / lengths;
        }
        private static double[] Subtract(double[] a, double[] b)
        {
            return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        }
        private static double[] Scale(double[] a, double factor)
        {
            return new double[] { a[0] * factor, a[1] * factor, a[2] * factor };
        }
        private static double Dot(double[] a, double[] b)
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }
        private static double[] Cross(double[] a, double[] b)
        {
            return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        }
    }
}
