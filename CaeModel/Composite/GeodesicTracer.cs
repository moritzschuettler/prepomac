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
    // Traces straightest geodesics across a triangle mesh (Polthier-Schmies triangle-unfolding
    // method). This replaces the Python reference's potpourri3d.GeodesicTracer, a C++/pybind11
    // library with no .NET equivalent, so the walk-and-unfold algorithm is implemented from scratch:
    //
    // Starting at a (face, barycentric point) with a direction lying in that face's plane, the walk
    // finds where the straight ray leaves the current triangle, crosses into the neighbor triangle
    // across that edge, and "unfolds" the travel direction into the neighbor's own plane so the path
    // continues straight when the two triangles are flattened out along their shared edge. This is
    // done without maintaining one large persistent 2D layout: at each crossing, the direction is
    // decomposed into a component along the shared edge (which is identical in both triangles' planes
    // by construction) and a component perpendicular to it, pointing toward each triangle's own apex;
    // continuing straight across the seam means keeping the edge-aligned component and flipping the
    // sign of the perpendicular one (Python's own vertex-walk uses an analogous "nudge" trick for the
    // degenerate case of passing exactly through a mesh vertex, applied below the same way).
    public class GeodesicTracer
    {
        // Variables
        private readonly TapeGeodesicMesh _mesh;


        // Constructors
        public GeodesicTracer(TapeGeodesicMesh mesh)
        {
            _mesh = mesh;
        }


        // Methods
        // Traces a geodesic starting at (startFace, startBary) in the given direction. The magnitude
        // of 'direction' is the trace length (same convention as potpourri3d's
        // trace_geodesic_from_face, where direction_xyz = unit_direction * trace_length). Returns the
        // polyline of 3D points visited, including the start point and the final point (either the
        // point reached when the length budget is exhausted, or the point where a free mesh boundary
        // was hit).
        public double[][] TraceGeodesicFromFace(int startFace, double[] startBary, double[] direction)
        {
            double totalLength = Vec3Util.Norm(direction);
            int[] tri = _mesh.Triangles[startFace];
            double[] v0 = _mesh.Nodes[tri[0]], v1 = _mesh.Nodes[tri[1]], v2 = _mesh.Nodes[tri[2]];
            double[] startPoint = Vec3Util.Add(
                Vec3Util.Add(Vec3Util.Scale(v0, startBary[0]), Vec3Util.Scale(v1, startBary[1])),
                Vec3Util.Scale(v2, startBary[2]));
            //
            List<double[]> outPts = new List<double[]> { startPoint };
            if (totalLength < 1e-12) return outPts.ToArray();
            //
            double[] normal0 = _mesh.Normals[startFace];
            double[] unitDir0 = Vec3Util.Scale(direction, 1.0 / totalLength);
            // project the caller-supplied direction onto the starting triangle's plane
            double[] dirInPlane = Vec3Util.Sub(unitDir0, Vec3Util.Scale(normal0, Vec3Util.Dot(unitDir0, normal0)));
            double dirInPlaneLen = Vec3Util.Norm(dirInPlane);
            if (dirInPlaneLen < 1e-12) return outPts.ToArray();
            double[] currentDir = Vec3Util.Scale(dirInPlane, 1.0 / dirInPlaneLen);
            //
            int currentFace = startFace;
            double[] currentPoint = startPoint;
            double remaining = totalLength;
            int steps = 0;
            const int maxSteps = 50000;
            //
            while (remaining > 1e-9 && steps < maxSteps)
            {
                steps++;
                double[] exitPoint;
                double tUsed;
                int edgeA, edgeB;
                bool found = SolveExitInTriangle(currentFace, currentPoint, currentDir, remaining,
                                                  out exitPoint, out tUsed, out edgeA, out edgeB);
                if (!found)
                {
                    // the length budget runs out (or no forward edge exists) inside this triangle
                    currentPoint = Vec3Util.Add(currentPoint, Vec3Util.Scale(currentDir, remaining));
                    outPts.Add(currentPoint);
                    break;
                }
                remaining -= tUsed;
                outPts.Add(exitPoint);
                if (remaining <= 1e-9) break;
                //
                int localEdge = FindLocalEdgeIndex(currentFace, edgeA, edgeB);
                int neighborFace = localEdge >= 0 ? _mesh.Neighbors[currentFace][localEdge] : -1;
                if (neighborFace < 0) break; // free mesh boundary reached
                //
                double[] edgeOrigin = _mesh.Nodes[edgeA];
                double[] edgeDir = Vec3Util.Normalize(Vec3Util.Sub(_mesh.Nodes[edgeB], edgeOrigin));
                double[] vHatCurrent = EdgePerpToApex(currentFace, edgeA, edgeB, edgeDir, edgeOrigin);
                double[] vHatNeighbor = EdgePerpToApex(neighborFace, edgeA, edgeB, edgeDir, edgeOrigin);
                double dirU = Vec3Util.Dot(currentDir, edgeDir);
                double dirV = Vec3Util.Dot(currentDir, vHatCurrent);
                // unfold: keep the edge-aligned component, flip the sign of the perpendicular one
                double[] newDir = Vec3Util.Add(Vec3Util.Scale(edgeDir, dirU), Vec3Util.Scale(vHatNeighbor, -dirV));
                double newDirLen = Vec3Util.Norm(newDir);
                if (newDirLen < 1e-12) break;
                newDir = Vec3Util.Scale(newDir, 1.0 / newDirLen);
                //
                // vertex-crossing degeneracy: nudge a crossing point that landed on (or very near) a
                // mesh vertex slightly toward the entered triangle's centroid, so the next iteration's
                // edge solve stays well-conditioned
                double edgeLen = Vec3Util.Distance(_mesh.Nodes[edgeA], _mesh.Nodes[edgeB]);
                double vertexEps = edgeLen * 1e-6;
                if (Vec3Util.Distance(exitPoint, edgeOrigin) < vertexEps ||
                    Vec3Util.Distance(exitPoint, _mesh.Nodes[edgeB]) < vertexEps)
                {
                    double[] centroid = TriangleCentroid(neighborFace);
                    double[] toCentroid = Vec3Util.Sub(centroid, exitPoint);
                    double toCentroidLen = Vec3Util.Norm(toCentroid);
                    if (toCentroidLen > 1e-15)
                    {
                        double nudge = Math.Min(edgeLen * 1e-4, toCentroidLen);
                        exitPoint = Vec3Util.Add(exitPoint, Vec3Util.Scale(toCentroid, nudge / toCentroidLen));
                        outPts[outPts.Count - 1] = exitPoint;
                    }
                }
                //
                currentFace = neighborFace;
                currentPoint = exitPoint;
                currentDir = newDir;
            }
            return outPts.ToArray();
        }

        // Traces bidirectionally from the seed point and merges backward-reversed + forward into one
        // continuous path (Python's merge_bidirectional_paths), so the seed point ends up interior to
        // the returned polyline rather than at one end.
        public double[][] TraceGeodesicSymmetric(int startFace, double[] startBary, double[] direction)
        {
            double[][] fwd = TraceGeodesicFromFace(startFace, startBary, direction);
            double[][] bwd = TraceGeodesicFromFace(startFace, startBary, Vec3Util.Scale(direction, -1.0));
            List<double[]> merged = new List<double[]>(bwd.Length + fwd.Length - 1);
            for (int i = bwd.Length - 1; i >= 0; i--) merged.Add(bwd[i]);
            for (int i = 1; i < fwd.Length; i++) merged.Add(fwd[i]);
            return merged.ToArray();
        }

        // Finds where the ray (point, dir) leaves the current triangle: the closest forward edge
        // crossing within [0, remaining]. Works in a local 2D basis of the triangle purely to solve the
        // 2D line-vs-3-edges problem; the crossing point/length returned are basis-independent 3D/scalar
        // results. Returns false when the ray's length budget is exhausted before reaching any edge.
        private bool SolveExitInTriangle(int faceId, double[] point, double[] dir, double remaining,
                                          out double[] exitPoint, out double tUsed, out int edgeA, out int edgeB)
        {
            exitPoint = null; tUsed = 0; edgeA = -1; edgeB = -1;
            int[] tri = _mesh.Triangles[faceId];
            double[] v0 = _mesh.Nodes[tri[0]], v1 = _mesh.Nodes[tri[1]], v2 = _mesh.Nodes[tri[2]];
            double[] normal = _mesh.Normals[faceId];
            double[] e1 = Vec3Util.Normalize(Vec3Util.Sub(v1, v0));
            double[] e2 = Vec3Util.Normalize(Vec3Util.Cross(normal, e1));
            //
            double[] p2 = To2D(point, v0, e1, e2);
            double[] d2 = { Vec3Util.Dot(dir, e1), Vec3Util.Dot(dir, e2) };
            double[][] verts2D = { To2D(v0, v0, e1, e2), To2D(v1, v0, e1, e2), To2D(v2, v0, e1, e2) };
            int[] vids = { tri[0], tri[1], tri[2] };
            int[][] localEdges = { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 0 } };
            //
            double edgeLenScale = Math.Max(Vec3Util.Distance(v0, v1),
                                            Math.Max(Vec3Util.Distance(v1, v2), Vec3Util.Distance(v2, v0)));
            double tEps = 1e-9 * Math.Max(edgeLenScale, 1.0);
            const double sEps = 1e-9;
            //
            double bestT = double.MaxValue;
            int bestEdge = -1;
            for (int ei = 0; ei < 3; ei++)
            {
                double[] a2 = verts2D[localEdges[ei][0]];
                double[] b2 = verts2D[localEdges[ei][1]];
                double[] eVec = { b2[0] - a2[0], b2[1] - a2[1] };
                double[] diff = { a2[0] - p2[0], a2[1] - p2[1] };
                double det = eVec[0] * d2[1] - eVec[1] * d2[0];             // cross2(E, D)
                if (Math.Abs(det) < 1e-14) continue;                        // ray parallel to this edge
                double t = (eVec[0] * diff[1] - eVec[1] * diff[0]) / det;   // cross2(E, diff) / det
                double s = (d2[0] * diff[1] - d2[1] * diff[0]) / det;       // cross2(D, diff) / det
                if (t <= tEps) continue;                                    // behind us / the entry edge
                if (s < -sEps || s > 1 + sEps) continue;                    // outside the edge segment
                if (t < bestT) { bestT = t; bestEdge = ei; }
            }
            if (bestEdge < 0 || bestT > remaining) return false;
            //
            tUsed = bestT;
            exitPoint = Vec3Util.Add(point, Vec3Util.Scale(dir, bestT));
            edgeA = vids[localEdges[bestEdge][0]];
            edgeB = vids[localEdges[bestEdge][1]];
            return true;
        }

        private static double[] To2D(double[] p, double[] origin, double[] axisU, double[] axisV)
        {
            double[] rel = Vec3Util.Sub(p, origin);
            return new double[] { Vec3Util.Dot(rel, axisU), Vec3Util.Dot(rel, axisV) };
        }

        // In-plane unit vector of triangle 'faceId', perpendicular to edge (edgeA, edgeB), pointing
        // from the edge toward the triangle's own third vertex (its apex relative to this edge).
        private double[] EdgePerpToApex(int faceId, int edgeA, int edgeB, double[] edgeDir, double[] edgeOrigin)
        {
            int[] tri = _mesh.Triangles[faceId];
            int apexId = -1;
            for (int i = 0; i < 3; i++)
                if (tri[i] != edgeA && tri[i] != edgeB) { apexId = tri[i]; break; }
            double[] apex = _mesh.Nodes[apexId];
            double[] rel = Vec3Util.Sub(apex, edgeOrigin);
            double along = Vec3Util.Dot(rel, edgeDir);
            double[] perp = Vec3Util.Sub(rel, Vec3Util.Scale(edgeDir, along));
            double len = Vec3Util.Norm(perp);
            return len > 1e-15 ? Vec3Util.Scale(perp, 1.0 / len) : new double[] { 0, 0, 0 };
        }

        private int FindLocalEdgeIndex(int faceId, int a, int b)
        {
            int[] tri = _mesh.Triangles[faceId];
            int[][] edges = { new[] { tri[0], tri[1] }, new[] { tri[1], tri[2] }, new[] { tri[2], tri[0] } };
            for (int i = 0; i < 3; i++)
                if ((edges[i][0] == a && edges[i][1] == b) || (edges[i][0] == b && edges[i][1] == a)) return i;
            return -1;
        }

        private double[] TriangleCentroid(int faceId)
        {
            int[] tri = _mesh.Triangles[faceId];
            double[] a = _mesh.Nodes[tri[0]], b = _mesh.Nodes[tri[1]], c = _mesh.Nodes[tri[2]];
            return new double[] { (a[0] + b[0] + c[0]) / 3.0, (a[1] + b[1] + c[1]) / 3.0, (a[2] + b[2] + c[2]) / 3.0 };
        }
    }
}
