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
    /// Finds the faces of a part that RingMapping meshes: faces of exactly two boundary loops, a closed edge being a loop
    /// of its own, that are a height field over the plane of their outer loop. The loops are ordered as
    /// SubmapFaceDetector.GetBoundaryLoops orders them; the outer loop is the one that encloses the larger area by
    /// Newell's method, and the inner loop is turned to run the same way round.
    /// The edges are paired when the loops have as many and, for one cyclic shift of the inner loop, every inner edge
    /// faces its outer partner: the midpoint of each is the nearest midpoint of the other loop to the midpoint of the
    /// other, and the same of their first vertices. A loop of one closed edge pairs with the other. The inner loop is then
    /// started at the edge that faces outer edge 0.
    /// A face is judged as SubmapFaceDetector judges its grids: the ring grid is laid in the plane of the outer loop with
    /// counts in proportion to the lengths, one step for both loops, a pair of edges taking the mean of its two lengths
    /// and an unpaired ring the mean of the two totals spread over each loop, with the layers RingMapping gives for that
    /// step, and the face is left free when the grid folds or has a quad corner below SubmapFaceDetector.MinScaledJacobian.
    /// That keeps out an L with a small hole and a bore far off the centre of a disc. A face whose inner loop is shorter
    /// than MinLengthRatio of its outer loop is left free as well. The face keeps the smallest
    /// distance between the polylines of its loops and the mean distance between the nodes that grid joins. A face may be
    /// a SubmapFace as well; the mesher chooses between the two (RingFace.IsThinWall).
    /// </summary>
    public static class RingFaceDetector
    {
        // Variables                                                                                                                
        /// <summary>
        /// A face that is both a ring and a submapped face is meshed as a ring when the smallest and the mean distance
        /// between its loops are below this many times the element size, and by its lattice otherwise.
        /// </summary>
        public const double ThinWallFactor = 1.5;
        /// <summary>
        /// The shortest inner loop of a ring against its outer loop. Both loops carry as many nodes, so a shorter inner
        /// loop is lined with quads more than two and a half times as long across the ring as along the loop.
        /// </summary>
        public const double MinLengthRatio = 0.25;
        private const int NumOfDivisions = 64;


        // Methods                                                                                                                  
        public static RingFace[] Detect(FeMesh mesh, BasePart part)
        {
            VisualizationData vis = part.Visualization;
            if (vis == null || vis.FaceCount == 0 || vis.FaceEdgeIds == null || vis.CellIdsByFace == null)
                return new RingFace[0];
            //
            Dictionary<int, int[]> edgeIdVertexIds = new Dictionary<int, int[]>();
            List<RingFace> ringFaces = new List<RingFace>();
            for (int i = 0; i < vis.FaceCount; i++)
            {
                RingFace ringFace = DetectFace(mesh, vis, i, edgeIdVertexIds);
                if (ringFace != null) ringFaces.Add(ringFace);
            }
            return ringFaces.ToArray();
        }
        private static RingFace DetectFace(FeMesh mesh, VisualizationData vis, int faceId, Dictionary<int, int[]> edgeIdVertexIds)
        {
            int[] faceEdgeIds = vis.FaceEdgeIds[faceId];
            if (faceEdgeIds == null || faceEdgeIds.Length < 2 || faceId >= vis.CellIdsByFace.Length) return null;
            List<int[]> loopsEdgeIds;
            List<int[]> loopsVertexIds;
            if (!SubmapFaceDetector.GetBoundaryLoops(vis, faceEdgeIds, edgeIdVertexIds, out loopsEdgeIds, out loopsVertexIds) ||
                loopsEdgeIds.Count != 2) return null;
            // The polylines of the edges of both loops and the normals of the loops by Newell's method
            List<double[]>[][] polylines = new List<double[]>[2][];
            double[][] normals = new double[2][];
            for (int l = 0; l < 2; l++)
            {
                polylines[l] = new List<double[]>[loopsEdgeIds[l].Length];
                List<double[]> points = new List<double[]>();
                for (int k = 0; k < polylines[l].Length; k++)
                {
                    polylines[l][k] = TransfiniteFaceDetector.GetEdgePoints(mesh, vis, loopsEdgeIds[l][k], loopsVertexIds[l][k]);
                    if (polylines[l][k] == null || polylines[l][k].Count < 2) return null;
                    for (int i = 0; i < polylines[l][k].Count - 1; i++) points.Add(polylines[l][k][i]);
                }
                normals[l] = GetNewellNormal(points);
            }
            int outer = Length(normals[0]) >= Length(normals[1]) ? 0 : 1;
            int inner = 1 - outer;
            List<double[]> outerPoints = new List<double[]>();
            foreach (var polyline in polylines[outer])
            {
                for (int i = 0; i < polyline.Count - 1; i++) outerPoints.Add(polyline[i]);
            }
            double[][] frame = SubmapFaceDetector.GetPlaneFrame(outerPoints);
            if (frame == null || !TransfiniteFaceDetector.IsHeightFieldOverPlane(mesh, vis, faceId, frame[1])) return null;
            // The inner loop runs the same way round as the outer loop
            int[] innerEdgeIds = loopsEdgeIds[inner];
            int[] innerVertexIds = loopsVertexIds[inner];
            List<double[]>[] innerPolylines = polylines[inner];
            double[] innerNormal = normals[inner];
            if (innerNormal[0] * frame[1][0] + innerNormal[1] * frame[1][1] + innerNormal[2] * frame[1][2] < 0)
            {
                int n = innerEdgeIds.Length;
                int[] edgeIds = new int[n];
                int[] vertexIds = new int[n];
                List<double[]>[] reversed = new List<double[]>[n];
                for (int k = 0; k < n; k++)
                {
                    edgeIds[k] = innerEdgeIds[n - 1 - k];
                    vertexIds[k] = innerVertexIds[(n - k) % n];
                    reversed[k] = new List<double[]>(innerPolylines[n - 1 - k]);
                    reversed[k].Reverse();
                }
                innerEdgeIds = edgeIds;
                innerVertexIds = vertexIds;
                innerPolylines = reversed;
            }
            // Paired edges start the inner loop at the edge that faces outer edge 0
            int pairShift = GetPairShift(polylines[outer], innerPolylines);
            bool paired = pairShift >= 0;
            if (pairShift > 0)
            {
                int n = innerEdgeIds.Length;
                int[] edgeIds = new int[n];
                int[] vertexIds = new int[n];
                List<double[]>[] turned = new List<double[]>[n];
                for (int k = 0; k < n; k++)
                {
                    edgeIds[k] = innerEdgeIds[(k + pairShift) % n];
                    vertexIds[k] = innerVertexIds[(k + pairShift) % n];
                    turned[k] = innerPolylines[(k + pairShift) % n];
                }
                innerEdgeIds = edgeIds;
                innerVertexIds = vertexIds;
                innerPolylines = turned;
            }
            //
            double meanDistance;
            double quality = GetGridQuality(polylines[outer], innerPolylines, paired, frame, out meanDistance);
            if (!(quality >= SubmapFaceDetector.MinScaledJacobian)) return null;
            return new RingFace(faceId, loopsVertexIds[outer], loopsEdgeIds[outer], innerVertexIds, innerEdgeIds, paired,
                                GetMinDistance(polylines[outer], innerPolylines), meanDistance);
        }
        /// <summary>
        /// The smallest distance between the polylines of two loops: of every point of one to every segment of the other.
        /// </summary>
        private static double GetMinDistance(List<double[]>[] polylinesA, List<double[]>[] polylinesB)
        {
            double minDistance = double.MaxValue;
            for (int pass = 0; pass < 2; pass++)
            {
                List<double[]>[] points = pass == 0 ? polylinesA : polylinesB;
                List<double[]>[] segments = pass == 0 ? polylinesB : polylinesA;
                foreach (var pointPolyline in points)
                {
                    foreach (var p in pointPolyline)
                    {
                        foreach (var segmentPolyline in segments)
                        {
                            for (int i = 0; i < segmentPolyline.Count - 1; i++)
                                minDistance = Math.Min(minDistance, GetSegmentDistance(p, segmentPolyline[i],
                                                                                       segmentPolyline[i + 1]));
                        }
                    }
                }
            }
            return minDistance;
        }
        private static double GetSegmentDistance(double[] p, double[] a, double[] b)
        {
            double[] ab = new double[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double[] ap = new double[] { p[0] - a[0], p[1] - a[1], p[2] - a[2] };
            double lengthSquared = ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2];
            double t = lengthSquared > 0 ? (ap[0] * ab[0] + ap[1] * ab[1] + ap[2] * ab[2]) / lengthSquared : 0;
            t = Math.Max(0, Math.Min(1, t));
            double[] d = new double[] { ap[0] - t * ab[0], ap[1] - t * ab[1], ap[2] - t * ab[2] };
            return Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]);
        }
        /// <summary>
        /// The cyclic shift s for which inner edge k + s faces outer edge k, both loops running the same way round: their
        /// midpoints are each the nearest of the other loop to the other, one to one, and so are their first vertices.
        /// 0 for loops of one edge each; -1 when the loops have different numbers of edges or no shift pairs them.
        /// </summary>
        private static int GetPairShift(List<double[]>[] outerPolylines, List<double[]>[] innerPolylines)
        {
            int n = outerPolylines.Length;
            if (innerPolylines.Length != n) return -1;
            if (n == 1) return 0;
            double[][] outerMiddles = new double[n][];
            double[][] innerMiddles = new double[n][];
            double[][] outerStarts = new double[n][];
            double[][] innerStarts = new double[n][];
            double length;
            for (int k = 0; k < n; k++)
            {
                double[][] samples = TransfiniteFaceDetector.Resample(outerPolylines[k], 2, out length);
                if (samples == null) return -1;
                outerMiddles[k] = samples[1];
                outerStarts[k] = samples[0];
                samples = TransfiniteFaceDetector.Resample(innerPolylines[k], 2, out length);
                if (samples == null) return -1;
                innerMiddles[k] = samples[1];
                innerStarts[k] = samples[0];
            }
            for (int s = 0; s < n; s++)
            {
                bool facing = true;
                for (int k = 0; k < n && facing; k++)
                {
                    int partner = (k + s) % n;
                    facing = GetNearest(outerMiddles, innerMiddles[partner]) == k &&
                             GetNearest(innerMiddles, outerMiddles[k]) == partner &&
                             GetNearest(outerStarts, innerStarts[partner]) == k &&
                             GetNearest(innerStarts, outerStarts[k]) == partner;
                }
                if (facing) return s;
            }
            return -1;
        }
        private static int GetNearest(double[][] points, double[] point)
        {
            int nearest = -1;
            double nearestDistance = double.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                double distance = 0;
                for (int d = 0; d < 3; d++) distance += (points[i][d] - point[d]) * (points[i][d] - point[d]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }
            return nearest;
        }
        /// <summary>
        /// The smallest scaled Jacobian of a quad corner of the ring grid laid in the plane of the frame with counts in
        /// proportion to the lengths of the edges, the loops running the same way round, and the mean distance between
        /// the nodes it joins; -1 when the grid folds or cannot be made.
        /// </summary>
        private static double GetGridQuality(List<double[]>[] outerPolylines, List<double[]>[] innerPolylines, bool paired,
                                             double[][] frame, out double meanDistance)
        {
            meanDistance = 0;
            double[] outerLengths = GetLengths(outerPolylines);
            double[] innerLengths = GetLengths(innerPolylines);
            if (outerLengths == null || innerLengths == null) return -1;
            double outerTotal = 0;
            double innerTotal = 0;
            foreach (var length in outerLengths) outerTotal += length;
            foreach (var length in innerLengths) innerTotal += length;
            if (innerTotal < MinLengthRatio * outerTotal) return -1;
            double step = 0.5 * (outerTotal + innerTotal) / NumOfDivisions;
            int[] outerCounts;
            int[] innerCounts;
            if (paired)
            {
                outerCounts = new int[outerLengths.Length];
                for (int k = 0; k < outerCounts.Length; k++)
                    outerCounts[k] = Math.Max(1, (int)Math.Round(0.5 * (outerLengths[k] + innerLengths[k]) / step));
                innerCounts = outerCounts;
            }
            else
            {
                int total = Math.Max(Math.Max(outerLengths.Length, innerLengths.Length), NumOfDivisions);
                outerCounts = GetProportionalCounts(outerLengths, total);
                innerCounts = GetProportionalCounts(innerLengths, total);
            }
            double[][] outerXyz;
            double[][] outerUv;
            double[][] innerXyz;
            double[][] innerUv;
            GetLoopNodes(outerPolylines, outerCounts, frame, out outerXyz, out outerUv);
            GetLoopNodes(innerPolylines, innerCounts, frame, out innerXyz, out innerUv);
            RingGrid grid;
            try
            {
                int numOfLayers = RingMapping.GetNumberOfLayers(outerUv, outerXyz, innerUv, innerXyz);
                grid = RingMapping.Map(outerUv, outerXyz, innerUv, innerXyz, numOfLayers, null);
            }
            catch (CaeException)
            {
                return -1;
            }
            if (grid.NumberOfFoldedQuads > 0) return -1;
            int n = outerXyz.Length;
            for (int i = 0; i < n; i++)
            {
                double[] a = outerXyz[i];
                double[] b = innerXyz[(i + grid.Shift) % n];
                meanDistance += Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]) +
                                          (b[2] - a[2]) * (b[2] - a[2])) / n;
            }
            return grid.MinScaledJacobian;
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
        /// The total spread over the edges in proportion to their lengths, at least one each, the rest by the largest
        /// remainders, ties to the first edge.
        /// </summary>
        public static int[] GetProportionalCounts(double[] lengths, int total)
        {
            int n = lengths.Length;
            double sum = 0;
            foreach (var length in lengths) sum += length;
            double[] exact = new double[n];
            int[] counts = new int[n];
            int numOfElements = 0;
            for (int k = 0; k < n; k++)
            {
                exact[k] = sum > 0 ? lengths[k] / sum * total : (double)total / n;
                counts[k] = Math.Max(1, (int)Math.Floor(exact[k]));
                numOfElements += counts[k];
            }
            while (numOfElements < total)
            {
                int best = 0;
                for (int k = 1; k < n; k++)
                {
                    if (exact[k] - counts[k] > exact[best] - counts[best]) best = k;
                }
                counts[best]++;
                numOfElements++;
            }
            while (numOfElements > total)
            {
                int best = -1;
                for (int k = 0; k < n; k++)
                {
                    if (counts[k] > 1 && (best < 0 || exact[k] - counts[k] < exact[best] - counts[best])) best = k;
                }
                if (best < 0) break;
                counts[best]--;
                numOfElements--;
            }
            return counts;
        }
        /// <summary>
        /// The nodes of a loop, every edge resampled into its count, in space and in the plane of the frame, the last
        /// node of an edge being the first of the next and not repeated.
        /// </summary>
        private static void GetLoopNodes(List<double[]>[] polylines, int[] counts, double[][] frame, out double[][] xyz,
                                         out double[][] uv)
        {
            List<double[]> points = new List<double[]>();
            double length;
            for (int k = 0; k < polylines.Length; k++)
            {
                double[][] samples = TransfiniteFaceDetector.Resample(polylines[k], counts[k], out length);
                for (int i = 0; i < counts[k]; i++) points.Add(samples[i]);
            }
            xyz = points.ToArray();
            uv = new double[xyz.Length][];
            for (int i = 0; i < xyz.Length; i++) uv[i] = SubmapFaceDetector.ToPlane(frame, xyz[i]);
        }
        private static double[] GetNewellNormal(List<double[]> points)
        {
            double[] normal = new double[3];
            for (int i = 0; i < points.Count; i++)
            {
                double[] a = points[i];
                double[] b = points[(i + 1) % points.Count];
                normal[0] += (a[1] - b[1]) * (a[2] + b[2]);
                normal[1] += (a[2] - b[2]) * (a[0] + b[0]);
                normal[2] += (a[0] - b[0]) * (a[1] + b[1]);
            }
            return normal;
        }
        private static double Length(double[] vector)
        {
            return Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
        }
    }
}
