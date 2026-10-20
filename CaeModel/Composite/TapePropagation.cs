// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;

namespace CaeModel
{
    // One generated tape course: the seed reference curve (Side = 0, Layer = 0) or one of its
    // parallel offset courses (Side = +-1, Layer = 1, 2, ...), spaced by the tape width.
    [Serializable]
    public class TapePropagationTape
    {
        public int Side;
        public int Layer;
        public double[][] Points;
    }

    // Orchestrates the tape placement (AFP) "natural" (geodesic) propagation algorithm: ports the
    // contour-free path of the Python reference's CAM_FiberPropagation / compute_multiple_parallels.
    // Starting from a seed point, a symmetric geodesic reference curve is traced, then parallel
    // offset courses are traced on both sides by repeatedly stepping outward by the tape width,
    // stopping when a course runs off the free edge of the mesh region.
    public static class TapePropagation
    {
        private const int DefaultMaxLayers = 200;

        // Projects the seed point onto the mesh, traces a symmetric geodesic in 'direction' (already
        // rotated into the seed triangle's tangent plane and by the ply angle - the caller mirrors the
        // same local-rosette convention used for the ply orientation arrows), and resamples it at
        // 'stepLength'. 'traceLength' is the one-sided trace budget (mirrors Python's ref_trace_length,
        // typically twice the mesh's bounding-box diagonal so the trace always reaches a free edge).
        public static double[][] BuildRefCurve(TapeGeodesicMesh mesh, GeodesicTracer tracer, double[] seedPoint,
                                                 double[] direction, double traceLength, double stepLength,
                                                 out bool success)
        {
            int faceId;
            double[] bary, closest;
            success = mesh.FindNearestPoint(seedPoint, out faceId, out bary, out closest);
            if (!success) return null;
            //
            double[] normal = mesh.Normals[faceId];
            double[] dirInPlane = Vec3Util.Sub(direction, Vec3Util.Scale(normal, Vec3Util.Dot(direction, normal)));
            double dirLen = Vec3Util.Norm(dirInPlane);
            if (dirLen < 1e-9)
            {
                dirInPlane = ArbitraryInPlaneVector(normal);
                dirLen = Vec3Util.Norm(dirInPlane);
                if (dirLen < 1e-9) { success = false; return null; }
            }
            double[] unitDir = Vec3Util.Scale(dirInPlane, 1.0 / dirLen);
            //
            double[][] raw = tracer.TraceGeodesicSymmetric(faceId, bary, Vec3Util.Scale(unitDir, traceLength));
            return ResamplePolyline(raw, stepLength);
        }

        // Builds successive parallel offset courses on both sides of the seed geodesic. Each course is
        // obtained by tracing a short orthogonal geodesic (length = tapeWidth) from every sample of the
        // current curve, extending the result to the mesh's free boundary, and using it to seed the
        // next course out. A side stops once a course collapses to fewer than 2 points (mirrors
        // Python's compute_multiple_parallels, contour-free branch - no contour/overlap clipping).
        public static List<TapePropagationTape> ComputeParallelOffsets(TapeGeodesicMesh mesh, GeodesicTracer tracer,
                                                                          double[][] refCurve, double tapeWidth,
                                                                          double stepLength, int maxLayers = DefaultMaxLayers)
        {
            List<TapePropagationTape> results = new List<TapePropagationTape>();
            if (refCurve == null || refCurve.Length < 2 || tapeWidth <= 0) return results;
            //
            double boundaryTol = Math.Max(mesh.AverageEdgeLength * 0.5, 1e-9);
            double maxExtend = ComputeMeshDiagonal(mesh) * 2.0;
            //
            int[] sides = { 1, -1 };
            foreach (int side in sides)
            {
                double[][] current = refCurve;
                for (int layer = 1; layer <= maxLayers; layer++)
                {
                    double[][] layerPts = ComputeParallelPath(mesh, tracer, current, side, tapeWidth);
                    if (layerPts.Length < 2) break;
                    layerPts = ExtendToBoundary(mesh, tracer, layerPts, maxExtend, boundaryTol);
                    if (layerPts.Length < 2) break;
                    results.Add(new TapePropagationTape { Side = side, Layer = layer, Points = layerPts });
                    //
                    double[][] resampled = ResamplePolyline(layerPts, stepLength);
                    if (resampled.Length < 2) break;
                    current = resampled;
                }
            }
            return results;
        }

        // Traces one orthogonal-offset course from every sample of 'currentPts' (Python's
        // compute_parallel_path): the offset direction at each sample is side * (tangent x normal),
        // projected back into the tangent plane; a trace that's cut short by a lateral free edge
        // (reached less than ~85% of tapeWidth) is discarded rather than kept as a truncated course.
        private static double[][] ComputeParallelPath(TapeGeodesicMesh mesh, GeodesicTracer tracer,
                                                        double[][] currentPts, int side, double offsetDistance)
        {
            List<double[]> outPts = new List<double[]>();
            int n = currentPts.Length;
            for (int i = 0; i < n; i++)
            {
                int faceId;
                double[] bary, closest;
                if (!mesh.FindNearestPoint(currentPts[i], out faceId, out bary, out closest)) continue;
                //
                double[] tangent = ComputeTangent(currentPts, i);
                double[] normal = mesh.Normals[faceId];
                double[] offsetDir = Vec3Util.Scale(Vec3Util.Cross(tangent, normal), side);
                offsetDir = Vec3Util.Sub(offsetDir, Vec3Util.Scale(normal, Vec3Util.Dot(offsetDir, normal)));
                double offsetLen = Vec3Util.Norm(offsetDir);
                if (offsetLen < 1e-12) continue;
                offsetDir = Vec3Util.Scale(offsetDir, 1.0 / offsetLen);
                //
                double[][] trace = tracer.TraceGeodesicFromFace(faceId, bary, Vec3Util.Scale(offsetDir, offsetDistance));
                double traceLen = PolylineLength(trace);
                if (traceLen >= offsetDistance * 0.85) outPts.Add(trace[trace.Length - 1]);
            }
            return outPts.ToArray();
        }

        // Extends an open course's two ends to the mesh's free boundary if they don't already reach
        // one, by tracing one more geodesic continuation in the local end tangent (Python's
        // extend_path_to_boundary).
        private static double[][] ExtendToBoundary(TapeGeodesicMesh mesh, GeodesicTracer tracer, double[][] pts,
                                                     double maxLen, double boundaryTol)
        {
            if (pts.Length < 2) return pts;
            List<double[]> result = new List<double[]>(pts);
            //
            ExtendEnd(mesh, tracer, result, maxLen, boundaryTol, atStart: false);
            ExtendEnd(mesh, tracer, result, maxLen, boundaryTol, atStart: true);
            //
            return result.ToArray();
        }

        private static void ExtendEnd(TapeGeodesicMesh mesh, GeodesicTracer tracer, List<double[]> pts,
                                       double maxLen, double boundaryTol, bool atStart)
        {
            double[] endPoint = atStart ? pts[0] : pts[pts.Count - 1];
            if (mesh.IsNearFreeBoundary(endPoint, boundaryTol)) return;
            //
            int nRef = Math.Min(4, pts.Count - 1);
            double[] refPoint = atStart ? pts[nRef] : pts[pts.Count - 1 - nRef];
            double[] tangent = Vec3Util.Sub(endPoint, refPoint);
            double tLen = Vec3Util.Norm(tangent);
            if (tLen < 1e-12) return;
            tangent = Vec3Util.Scale(tangent, 1.0 / tLen);
            //
            int faceId;
            double[] bary, closest;
            if (!mesh.FindNearestPoint(endPoint, out faceId, out bary, out closest)) return;
            double[][] ext = tracer.TraceGeodesicFromFace(faceId, bary, Vec3Util.Scale(tangent, maxLen));
            if (ext.Length <= 1) return;
            //
            // ext[0] duplicates endPoint (every trace starts with its own seed point) - skip it.
            // ext[1..] are new points walking away from endPoint toward the free boundary.
            if (atStart)
            {
                // Final order must read boundary -> ... -> ext[1] -> (existing pts, starting at endPoint).
                List<double[]> prefix = new List<double[]>(ext.Length - 1);
                for (int i = ext.Length - 1; i >= 1; i--) prefix.Add(ext[i]);
                pts.InsertRange(0, prefix);
            }
            else
            {
                for (int i = 1; i < ext.Length; i++) pts.Add(ext[i]);
            }
        }

        private static double[] ComputeTangent(double[][] pts, int i)
        {
            double[] t;
            if (pts.Length < 2) return new double[] { 1, 0, 0 };
            if (i == 0) t = Vec3Util.Sub(pts[1], pts[0]);
            else if (i == pts.Length - 1) t = Vec3Util.Sub(pts[i], pts[i - 1]);
            else t = Vec3Util.Sub(pts[i + 1], pts[i - 1]);
            double len = Vec3Util.Norm(t);
            return len > 1e-15 ? Vec3Util.Scale(t, 1.0 / len) : new double[] { 1, 0, 0 };
        }

        private static double[] ArbitraryInPlaneVector(double[] normal)
        {
            double[] guess = Math.Abs(normal[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            return Vec3Util.Sub(guess, Vec3Util.Scale(normal, Vec3Util.Dot(guess, normal)));
        }

        private static double PolylineLength(double[][] pts)
        {
            double total = 0;
            for (int i = 1; i < pts.Length; i++) total += Vec3Util.Distance(pts[i - 1], pts[i]);
            return total;
        }

        private static double ComputeMeshDiagonal(TapeGeodesicMesh mesh)
        {
            double[] node0 = mesh.Nodes.Length > 0 ? mesh.Nodes[0] : new double[] { 0, 0, 0 };
            double minX = node0[0], maxX = node0[0], minY = node0[1], maxY = node0[1], minZ = node0[2], maxZ = node0[2];
            foreach (double[] n in mesh.Nodes)
            {
                if (n[0] < minX) minX = n[0]; if (n[0] > maxX) maxX = n[0];
                if (n[1] < minY) minY = n[1]; if (n[1] > maxY) maxY = n[1];
                if (n[2] < minZ) minZ = n[2]; if (n[2] > maxZ) maxZ = n[2];
            }
            double dx = maxX - minX, dy = maxY - minY, dz = maxZ - minZ;
            double diag = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            return diag > 1e-9 ? diag : 1.0;
        }

        // Resamples a 3D polyline at uniform arc-length intervals (Python's resample_polyline),
        // always including the exact endpoint.
        public static double[][] ResamplePolyline(double[][] pts, double step)
        {
            if (pts == null || pts.Length < 2 || step <= 0) return pts;
            double[] cumLen = new double[pts.Length];
            for (int i = 1; i < pts.Length; i++) cumLen[i] = cumLen[i - 1] + Vec3Util.Distance(pts[i - 1], pts[i]);
            double totalLen = cumLen[pts.Length - 1];
            if (totalLen < step) return pts;
            //
            int nSamples = (int)(totalLen / step);
            List<double> sampleDists = new List<double>(nSamples + 2);
            for (int i = 0; i <= nSamples; i++) sampleDists.Add(i * step);
            double last = sampleDists[sampleDists.Count - 1];
            if (last < totalLen && (totalLen - last) > step * 0.01) sampleDists.Add(totalLen);
            else sampleDists[sampleDists.Count - 1] = totalLen;
            //
            double[][] result = new double[sampleDists.Count][];
            int segIdx = 0;
            for (int i = 0; i < sampleDists.Count; i++)
            {
                double d = sampleDists[i];
                while (segIdx < pts.Length - 2 && cumLen[segIdx + 1] < d) segIdx++;
                double segLen = cumLen[segIdx + 1] - cumLen[segIdx];
                double t = segLen > 1e-15 ? (d - cumLen[segIdx]) / segLen : 0.0;
                result[i] = Vec3Util.Add(pts[segIdx], Vec3Util.Scale(Vec3Util.Sub(pts[segIdx + 1], pts[segIdx]), t));
            }
            return result;
        }
    }
}
