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
using CaeMesh;

namespace CaeModel
{
    // Small double[3] vector helpers shared by the tape placement (AFP) geodesic propagation
    // algorithm (TapeGeodesicMesh, GeodesicTracer, TapePropagation). Kept as raw double[] to match
    // the convention already used by KinDrape.cs / PlySurfaceSampler.cs in this folder.
    internal static class Vec3Util
    {
        public static double[] Sub(double[] a, double[] b) { return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
        public static double[] Add(double[] a, double[] b) { return new double[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] }; }
        public static double[] Scale(double[] a, double s) { return new double[] { a[0] * s, a[1] * s, a[2] * s }; }
        public static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
        public static double[] Cross(double[] a, double[] b)
        {
            return new double[]
            {
                a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0]
            };
        }
        public static double Norm(double[] a) { return Math.Sqrt(Dot(a, a)); }
        public static double[] Normalize(double[] a)
        {
            double n = Norm(a);
            return n > 1e-15 ? new double[] { a[0] / n, a[1] / n, a[2] / n } : new double[] { 0, 0, 0 };
        }
        public static double Distance(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    // Triangulated surface used by the tape placement (AFP) geodesic propagation algorithm.
    // Nodes/triangles follow the same double[][] / int[][] convention as PlySurfaceSampler.
    public class TapeGeodesicMesh
    {
        // Variables
        private double[][] _nodes;
        private int[][] _triangles;
        private double[][] _normals;
        private int[][] _neighbors;      // [triId][0..2] -> neighbor triangle id across edge (v0v1, v1v2, v2v0), or -1 = free boundary
        private HashSet<int> _boundaryVertices;
        private double _averageEdgeLength;
        private BoundingBox _box;
        private double _cellSize;
        private int _nx, _ny, _nz;
        private List<int>[] _cells;


        // Properties
        public double[][] Nodes { get { return _nodes; } }
        public int[][] Triangles { get { return _triangles; } }
        public double[][] Normals { get { return _normals; } }
        public int[][] Neighbors { get { return _neighbors; } }
        public int TriangleCount { get { return _triangles.Length; } }
        public double AverageEdgeLength { get { return _averageEdgeLength; } }


        // Constructors
        private TapeGeodesicMesh()
        {
        }


        // Methods
        // Builds a triangulated mesh from flat node coordinates and polygon connectivity - the same
        // shape the Controller already assembles for draping (elementNodeCounts / elementNodeIds).
        // Quads (and any n-gon) are fan-triangulated from the first vertex, the same rule KinDrape.cs
        // uses to build its triangle list.
        public static TapeGeodesicMesh Build(double[] flatNodeCoords, int[] elementNodeCounts, int[] elementNodeIds)
        {
            int nodeCount = flatNodeCoords.Length / 3;
            double[][] nodes = new double[nodeCount][];
            for (int i = 0; i < nodeCount; i++)
                nodes[i] = new double[] { flatNodeCoords[i * 3], flatNodeCoords[i * 3 + 1], flatNodeCoords[i * 3 + 2] };
            //
            List<int[]> triList = new List<int[]>();
            int offset = 0;
            for (int e = 0; e < elementNodeCounts.Length; e++)
            {
                int n = elementNodeCounts[e];
                if (n >= 3)
                {
                    int n0 = elementNodeIds[offset];
                    for (int t = 1; t < n - 1; t++)
                        triList.Add(new int[] { n0, elementNodeIds[offset + t], elementNodeIds[offset + t + 1] });
                }
                offset += n;
            }
            //
            TapeGeodesicMesh mesh = new TapeGeodesicMesh();
            mesh._nodes = nodes;
            mesh._triangles = triList.ToArray();
            mesh.ComputeNormals();
            mesh.BuildAdjacency();
            mesh.ComputeBoundaryVerticesAndEdgeStats();
            mesh.BuildGrid();
            return mesh;
        }

        // Free-boundary vertices (endpoints of edges with no second triangle) and the mean edge
        // length, used by the propagation algorithm to decide when a traced course has reached the
        // panel's edge and to size its default boundary-proximity tolerance.
        private void ComputeBoundaryVerticesAndEdgeStats()
        {
            _boundaryVertices = new HashSet<int>();
            double sumLen = 0;
            int count = 0;
            for (int t = 0; t < _triangles.Length; t++)
            {
                int[] tri = _triangles[t];
                int[][] edges = { new[] { tri[0], tri[1] }, new[] { tri[1], tri[2] }, new[] { tri[2], tri[0] } };
                for (int e = 0; e < 3; e++)
                {
                    sumLen += Vec3Util.Distance(_nodes[edges[e][0]], _nodes[edges[e][1]]);
                    count++;
                    if (_neighbors[t][e] < 0)
                    {
                        _boundaryVertices.Add(edges[e][0]);
                        _boundaryVertices.Add(edges[e][1]);
                    }
                }
            }
            _averageEdgeLength = count > 0 ? sumLen / count : 1.0;
        }

        // True when 'point' lies within 'tol' of any free-boundary vertex (Python's
        // is_point_near_boundary).
        public bool IsNearFreeBoundary(double[] point, double tol)
        {
            if (_boundaryVertices == null || _boundaryVertices.Count == 0) return false;
            double tol2 = tol * tol;
            foreach (int vid in _boundaryVertices)
            {
                double[] v = _nodes[vid];
                double dx = v[0] - point[0], dy = v[1] - point[1], dz = v[2] - point[2];
                if (dx * dx + dy * dy + dz * dz <= tol2) return true;
            }
            return false;
        }

        private void ComputeNormals()
        {
            _normals = new double[_triangles.Length][];
            for (int i = 0; i < _triangles.Length; i++)
            {
                double[] a = _nodes[_triangles[i][0]];
                double[] b = _nodes[_triangles[i][1]];
                double[] c = _nodes[_triangles[i][2]];
                double[] n = Vec3Util.Cross(Vec3Util.Sub(b, a), Vec3Util.Sub(c, a));
                double len = Vec3Util.Norm(n);
                _normals[i] = len > 1e-15 ? new double[] { n[0] / len, n[1] / len, n[2] / len } : new double[] { 0, 0, 1 };
            }
        }

        // Builds, per triangle, the neighbor triangle id across each of its 3 edges (or -1 for a
        // free boundary edge). Mirrors the Python reference's build_edge_adjacency_map /
        // boundary_vertex_set, but resolved directly to a per-triangle neighbor table.
        private void BuildAdjacency()
        {
            Dictionary<long, List<int[]>> edgeMap = new Dictionary<long, List<int[]>>();
            for (int t = 0; t < _triangles.Length; t++)
            {
                int[] tri = _triangles[t];
                int[][] edges = { new[] { tri[0], tri[1] }, new[] { tri[1], tri[2] }, new[] { tri[2], tri[0] } };
                for (int le = 0; le < 3; le++)
                {
                    long key = EdgeKey(edges[le][0], edges[le][1]);
                    List<int[]> list;
                    if (!edgeMap.TryGetValue(key, out list)) { list = new List<int[]>(); edgeMap[key] = list; }
                    list.Add(new int[] { t, le });
                }
            }
            //
            _neighbors = new int[_triangles.Length][];
            for (int t = 0; t < _triangles.Length; t++) _neighbors[t] = new int[] { -1, -1, -1 };
            //
            foreach (KeyValuePair<long, List<int[]>> kv in edgeMap)
            {
                List<int[]> list = kv.Value;
                if (list.Count == 2)
                {
                    int t0 = list[0][0], le0 = list[0][1];
                    int t1 = list[1][0], le1 = list[1][1];
                    _neighbors[t0][le0] = t1;
                    _neighbors[t1][le1] = t0;
                }
                // list.Count == 1 -> free boundary edge, neighbor stays -1
                // list.Count > 2 -> non-manifold edge; only the first pair is connected, the rest are
                // treated as boundary - acceptable for the shell regions this algorithm runs on
            }
        }

        private static long EdgeKey(int a, int b)
        {
            int lo = Math.Min(a, b), hi = Math.Max(a, b);
            return ((long)lo << 32) | (uint)hi;
        }

        // --- Nearest point on mesh (grid-accelerated) --------------------------------------------------
        // Same uniform-grid binning strategy as PlySurfaceSampler.cs (~8 triangles/cell), but answering
        // a closest-point query instead of a ray-drill.
        private void BuildGrid()
        {
            _box = new BoundingBox();
            foreach (double[] node in _nodes) _box.IncludeCoor(node);
            double diagonal = _box.GetDiagonal();
            if (diagonal <= 0) diagonal = 1;
            int targetCount = Math.Max(1, (int)Math.Ceiling(Math.Pow(Math.Max(1, _triangles.Length / 8.0), 1.0 / 3.0)));
            if (targetCount > 64) targetCount = 64;
            _cellSize = diagonal / targetCount;
            if (_cellSize <= 0) _cellSize = 1;
            //
            _nx = GetCellCount(_box.MaxX - _box.MinX);
            _ny = GetCellCount(_box.MaxY - _box.MinY);
            _nz = GetCellCount(_box.MaxZ - _box.MinZ);
            _cells = new List<int>[_nx * _ny * _nz];
            //
            for (int i = 0; i < _triangles.Length; i++)
            {
                int[] tri = _triangles[i];
                double[] a = _nodes[tri[0]], b = _nodes[tri[1]], c = _nodes[tri[2]];
                int minI = GetCellIndex(Min3(a[0], b[0], c[0]), _box.MinX, _nx);
                int maxI = GetCellIndex(Max3(a[0], b[0], c[0]), _box.MinX, _nx);
                int minJ = GetCellIndex(Min3(a[1], b[1], c[1]), _box.MinY, _ny);
                int maxJ = GetCellIndex(Max3(a[1], b[1], c[1]), _box.MinY, _ny);
                int minK = GetCellIndex(Min3(a[2], b[2], c[2]), _box.MinZ, _nz);
                int maxK = GetCellIndex(Max3(a[2], b[2], c[2]), _box.MinZ, _nz);
                for (int k = minK; k <= maxK; k++)
                    for (int j = minJ; j <= maxJ; j++)
                        for (int ii = minI; ii <= maxI; ii++)
                        {
                            int cell = (k * _ny + j) * _nx + ii;
                            if (_cells[cell] == null) _cells[cell] = new List<int>();
                            _cells[cell].Add(i);
                        }
            }
        }
        private static double Min3(double a, double b, double c) { return Math.Min(a, Math.Min(b, c)); }
        private static double Max3(double a, double b, double c) { return Math.Max(a, Math.Max(b, c)); }
        private int GetCellCount(double length)
        {
            int count = (int)Math.Ceiling(length / _cellSize);
            if (count < 1) count = 1;
            if (count > 256) count = 256;
            return count;
        }
        private int GetCellIndex(double value, double min, int count)
        {
            int index = (int)((value - min) / _cellSize);
            if (index < 0) index = 0;
            if (index >= count) index = count - 1;
            return index;
        }

        // Finds the closest point on the mesh surface to an arbitrary 3D point (Python's
        // project_point_to_mesh). Expands the search ring by ring around the point's grid cell and
        // stops once no further ring can contain a closer triangle.
        public bool FindNearestPoint(double[] point, out int faceId, out double[] bary, out double[] closestPoint)
        {
            faceId = -1;
            bary = null;
            closestPoint = null;
            if (_triangles.Length == 0) return false;
            //
            int ci = GetCellIndex(point[0], _box.MinX, _nx);
            int cj = GetCellIndex(point[1], _box.MinY, _ny);
            int ck = GetCellIndex(point[2], _box.MinZ, _nz);
            //
            double bestDist2 = double.MaxValue;
            double[] bestPoint = null;
            int bestFace = -1;
            HashSet<int> tested = new HashSet<int>();
            int maxRing = Math.Max(_nx, Math.Max(_ny, _nz));
            //
            for (int ring = 0; ring <= maxRing; ring++)
            {
                if (bestFace >= 0 && ring >= 1)
                {
                    double safeDist = (ring - 1) * _cellSize;
                    if (safeDist > 0 && safeDist * safeDist > bestDist2) break;
                }
                int kMin = Math.Max(0, ck - ring), kMax = Math.Min(_nz - 1, ck + ring);
                int jMin = Math.Max(0, cj - ring), jMax = Math.Min(_ny - 1, cj + ring);
                int iMin = Math.Max(0, ci - ring), iMax = Math.Min(_nx - 1, ci + ring);
                for (int k = kMin; k <= kMax; k++)
                    for (int j = jMin; j <= jMax; j++)
                        for (int i = iMin; i <= iMax; i++)
                        {
                            List<int> cell = _cells[(k * _ny + j) * _nx + i];
                            if (cell == null) continue;
                            for (int t = 0; t < cell.Count; t++)
                            {
                                int triId = cell[t];
                                if (!tested.Add(triId)) continue;
                                int[] tri = _triangles[triId];
                                double u, v, w;
                                double[] cp = ClosestPointOnTriangle(point, _nodes[tri[0]], _nodes[tri[1]], _nodes[tri[2]],
                                                                      out u, out v, out w);
                                double dx = cp[0] - point[0], dy = cp[1] - point[1], dz = cp[2] - point[2];
                                double d2 = dx * dx + dy * dy + dz * dz;
                                if (d2 < bestDist2) { bestDist2 = d2; bestPoint = cp; bestFace = triId; }
                            }
                        }
                // The whole grid has been covered - stop regardless of the distance-based early exit
                if (kMin == 0 && kMax == _nz - 1 && jMin == 0 && jMax == _ny - 1 && iMin == 0 && iMax == _nx - 1) break;
            }
            if (bestFace < 0) return false;
            //
            faceId = bestFace;
            int[] triv = _triangles[bestFace];
            bary = BarycentricCoords(bestPoint, _nodes[triv[0]], _nodes[triv[1]], _nodes[triv[2]]);
            // Nudge the barycentric weights strictly off any edge/vertex: a point that lands exactly
            // on a mesh vertex is incident to several triangles, but only some of them contain a given
            // trace direction in their own angular wedge - GeodesicTracer would then fail to find any
            // valid exit edge for whichever face happened to be picked. A negligible pull toward the
            // centroid (relative to the triangle's own size) makes the returned point strictly
            // interior to 'faceId', removing that ambiguity for every caller of this method.
            const double eps = 1e-7;
            bary[0] = bary[0] * (1 - eps) + eps / 3.0;
            bary[1] = bary[1] * (1 - eps) + eps / 3.0;
            bary[2] = bary[2] * (1 - eps) + eps / 3.0;
            double[] a = _nodes[triv[0]], b = _nodes[triv[1]], c = _nodes[triv[2]];
            closestPoint = Vec3Util.Add(Vec3Util.Add(Vec3Util.Scale(a, bary[0]), Vec3Util.Scale(b, bary[1])),
                                         Vec3Util.Scale(c, bary[2]));
            return true;
        }

        // Clamped barycentric coordinates of a point known to already lie (at least approximately) in
        // the plane of triangle (a, b, c). Mirrors the Python reference's barycentric_coords: negative
        // weights from numerical noise are clamped to 0 and the result renormalized.
        public static double[] BarycentricCoords(double[] p, double[] a, double[] b, double[] c)
        {
            double[] v0 = Vec3Util.Sub(b, a);
            double[] v1 = Vec3Util.Sub(c, a);
            double[] v2 = Vec3Util.Sub(p, a);
            double d00 = Vec3Util.Dot(v0, v0);
            double d01 = Vec3Util.Dot(v0, v1);
            double d11 = Vec3Util.Dot(v1, v1);
            double d20 = Vec3Util.Dot(v2, v0);
            double d21 = Vec3Util.Dot(v2, v1);
            double denom = d00 * d11 - d01 * d01;
            if (Math.Abs(denom) < 1e-15) return new double[] { 1.0 / 3, 1.0 / 3, 1.0 / 3 };
            double v = (d11 * d20 - d01 * d21) / denom;
            double w = (d00 * d21 - d01 * d20) / denom;
            double u = 1.0 - v - w;
            u = Math.Max(u, 0.0); v = Math.Max(v, 0.0); w = Math.Max(w, 0.0);
            double s = u + v + w;
            if (s < 1e-15) s = 1;
            return new double[] { u / s, v / s, w / s };
        }

        // Robust closest point on a triangle to point p (Ericson, "Real-Time Collision Detection" 5.1.5).
        // Returns the closest point and its (u, v, w) barycentric weights for (a, b, c).
        public static double[] ClosestPointOnTriangle(double[] p, double[] a, double[] b, double[] c,
                                                        out double u, out double v, out double w)
        {
            double[] ab = Vec3Util.Sub(b, a);
            double[] ac = Vec3Util.Sub(c, a);
            double[] ap = Vec3Util.Sub(p, a);
            double d1 = Vec3Util.Dot(ab, ap);
            double d2 = Vec3Util.Dot(ac, ap);
            if (d1 <= 0.0 && d2 <= 0.0) { u = 1; v = 0; w = 0; return a; }
            //
            double[] bp = Vec3Util.Sub(p, b);
            double d3 = Vec3Util.Dot(ab, bp);
            double d4 = Vec3Util.Dot(ac, bp);
            if (d3 >= 0.0 && d4 <= d3) { u = 0; v = 1; w = 0; return b; }
            //
            double vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
            {
                double t = d1 / (d1 - d3);
                u = 1 - t; v = t; w = 0;
                return Vec3Util.Add(a, Vec3Util.Scale(ab, t));
            }
            //
            double[] cp = Vec3Util.Sub(p, c);
            double d5 = Vec3Util.Dot(ab, cp);
            double d6 = Vec3Util.Dot(ac, cp);
            if (d6 >= 0.0 && d5 <= d6) { u = 0; v = 0; w = 1; return c; }
            //
            double vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
            {
                double t = d2 / (d2 - d6);
                u = 1 - t; v = 0; w = t;
                return Vec3Util.Add(a, Vec3Util.Scale(ac, t));
            }
            //
            double va = d3 * d6 - d5 * d4;
            if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
            {
                double t = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                u = 0; v = 1 - t; w = t;
                return Vec3Util.Add(b, Vec3Util.Scale(Vec3Util.Sub(c, b), t));
            }
            //
            double denom = 1.0 / (va + vb + vc);
            double vv = vb * denom;
            double ww = vc * denom;
            u = 1 - vv - ww; v = vv; w = ww;
            return Vec3Util.Add(a, Vec3Util.Add(Vec3Util.Scale(ab, vv), Vec3Util.Scale(ac, ww)));
        }
    }
}
