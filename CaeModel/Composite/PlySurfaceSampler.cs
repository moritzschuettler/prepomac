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
    // Core sampling of a ply surface: a ray is cast from a point along a direction and the closest
    // intersection with the triangulated ply surface is searched for. The triangles are binned into a
    // uniform 3D grid so that only the cells overlapped by the drill segment have to be tested.
    public class PlySurfaceSampler
    {
        // Variables
        private double[][] _nodes;
        private int[][] _triangles;
        private BoundingBox _box;
        private double _cellSize;
        private int _nx;
        private int _ny;
        private int _nz;
        private List<int>[] _cells;


        // Properties
        public BoundingBox Box { get { return _box; } }
        public int TriangleCount { get { return _triangles.Length; } }


        // Constructors
        public PlySurfaceSampler(double[][] nodes, int[][] triangles)
        {
            _nodes = nodes;
            _triangles = triangles;
            //
            _box = new BoundingBox();
            foreach (double[] node in _nodes) _box.IncludeCoor(node);
            //
            BuildGrid();
        }


        // Methods
        private void BuildGrid()
        {
            // Aim at about 8 triangles per cell but keep the number of cells bounded
            double diagonal = _box.GetDiagonal();
            if (diagonal <= 0) diagonal = 1;
            //
            int targetCount = Math.Max(1, (int)Math.Ceiling(Math.Pow(Math.Max(1, _triangles.Length / 8.0), 1.0 / 3.0)));
            if (targetCount > 64) targetCount = 64;
            _cellSize = diagonal / targetCount;
            if (_cellSize <= 0) _cellSize = 1;
            //
            _nx = GetCellCount(_box.MaxX - _box.MinX);
            _ny = GetCellCount(_box.MaxY - _box.MinY);
            _nz = GetCellCount(_box.MaxZ - _box.MinZ);
            //
            _cells = new List<int>[_nx * _ny * _nz];
            //
            for (int i = 0; i < _triangles.Length; i++)
            {
                int[] triangle = _triangles[i];
                double[] n0 = _nodes[triangle[0]];
                double[] n1 = _nodes[triangle[1]];
                double[] n2 = _nodes[triangle[2]];
                //
                int minI = GetCellIndex(Math.Min(n0[0], Math.Min(n1[0], n2[0])), _box.MinX, _nx);
                int maxI = GetCellIndex(Math.Max(n0[0], Math.Max(n1[0], n2[0])), _box.MinX, _nx);
                int minJ = GetCellIndex(Math.Min(n0[1], Math.Min(n1[1], n2[1])), _box.MinY, _ny);
                int maxJ = GetCellIndex(Math.Max(n0[1], Math.Max(n1[1], n2[1])), _box.MinY, _ny);
                int minK = GetCellIndex(Math.Min(n0[2], Math.Min(n1[2], n2[2])), _box.MinZ, _nz);
                int maxK = GetCellIndex(Math.Max(n0[2], Math.Max(n1[2], n2[2])), _box.MinZ, _nz);
                //
                for (int k = minK; k <= maxK; k++)
                {
                    for (int j = minJ; j <= maxJ; j++)
                    {
                        for (int i2 = minI; i2 <= maxI; i2++)
                        {
                            int cell = (k * _ny + j) * _nx + i2;
                            if (_cells[cell] == null) _cells[cell] = new List<int>();
                            _cells[cell].Add(i);
                        }
                    }
                }
            }
        }
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
        // Drills from origin along +-direction and returns the intersection closest to the origin
        public bool Drill(double[] origin, double[] direction, double maxDistance, double tolerance,
                          out double signedDistance)
        {
            signedDistance = 0;
            //
            if (origin[0] + maxDistance < _box.MinX - tolerance || origin[0] - maxDistance > _box.MaxX + tolerance) return false;
            if (origin[1] + maxDistance < _box.MinY - tolerance || origin[1] - maxDistance > _box.MaxY + tolerance) return false;
            if (origin[2] + maxDistance < _box.MinZ - tolerance || origin[2] - maxDistance > _box.MaxZ + tolerance) return false;
            // The bounding box of the drill segment
            double minX = origin[0] - Math.Abs(direction[0]) * maxDistance - tolerance;
            double maxX = origin[0] + Math.Abs(direction[0]) * maxDistance + tolerance;
            double minY = origin[1] - Math.Abs(direction[1]) * maxDistance - tolerance;
            double maxY = origin[1] + Math.Abs(direction[1]) * maxDistance + tolerance;
            double minZ = origin[2] - Math.Abs(direction[2]) * maxDistance - tolerance;
            double maxZ = origin[2] + Math.Abs(direction[2]) * maxDistance + tolerance;
            //
            if (maxX < _box.MinX || minX > _box.MaxX) return false;
            if (maxY < _box.MinY || minY > _box.MaxY) return false;
            if (maxZ < _box.MinZ || minZ > _box.MaxZ) return false;
            //
            int minI = GetCellIndex(minX, _box.MinX, _nx);
            int maxI = GetCellIndex(maxX, _box.MinX, _nx);
            int minJ = GetCellIndex(minY, _box.MinY, _ny);
            int maxJ = GetCellIndex(maxY, _box.MinY, _ny);
            int minK = GetCellIndex(minZ, _box.MinZ, _nz);
            int maxK = GetCellIndex(maxZ, _box.MinZ, _nz);
            //
            bool hit = false;
            double bestDistance = 0;
            HashSet<int> tested = new HashSet<int>();
            //
            for (int k = minK; k <= maxK; k++)
            {
                for (int j = minJ; j <= maxJ; j++)
                {
                    for (int i = minI; i <= maxI; i++)
                    {
                        List<int> cell = _cells[(k * _ny + j) * _nx + i];
                        if (cell == null) continue;
                        //
                        for (int t = 0; t < cell.Count; t++)
                        {
                            int triangleId = cell[t];
                            if (!tested.Add(triangleId)) continue;
                            //
                            double distance;
                            if (!IntersectTriangle(origin, direction, _triangles[triangleId], tolerance, out distance)) continue;
                            if (Math.Abs(distance) > maxDistance) continue;
                            //
                            if (!hit || Math.Abs(distance) < Math.Abs(bestDistance))
                            {
                                bestDistance = distance;
                                hit = true;
                            }
                        }
                    }
                }
            }
            //
            signedDistance = bestDistance;
            return hit;
        }
        // Moller-Trumbore ray triangle intersection with a barycentric slack that accounts for the tolerance
        private bool IntersectTriangle(double[] origin, double[] direction, int[] triangle, double tolerance,
                                       out double distance)
        {
            distance = 0;
            //
            double[] v0 = _nodes[triangle[0]];
            double[] v1 = _nodes[triangle[1]];
            double[] v2 = _nodes[triangle[2]];
            //
            double e1x = v1[0] - v0[0];
            double e1y = v1[1] - v0[1];
            double e1z = v1[2] - v0[2];
            double e2x = v2[0] - v0[0];
            double e2y = v2[1] - v0[1];
            double e2z = v2[2] - v0[2];
            //
            double px = direction[1] * e2z - direction[2] * e2y;
            double py = direction[2] * e2x - direction[0] * e2z;
            double pz = direction[0] * e2y - direction[1] * e2x;
            //
            double determinant = e1x * px + e1y * py + e1z * pz;
            if (Math.Abs(determinant) < 1E-14) return false;    // the ray is parallel to the triangle
            //
            double inverseDeterminant = 1.0 / determinant;
            //
            double tx = origin[0] - v0[0];
            double ty = origin[1] - v0[1];
            double tz = origin[2] - v0[2];
            //
            double u = (tx * px + ty * py + tz * pz) * inverseDeterminant;
            //
            double qx = ty * e1z - tz * e1y;
            double qy = tz * e1x - tx * e1z;
            double qz = tx * e1y - ty * e1x;
            //
            double v = (direction[0] * qx + direction[1] * qy + direction[2] * qz) * inverseDeterminant;
            // Convert the length tolerance into a barycentric slack using the triangle size
            double size = Math.Sqrt(Math.Max(e1x * e1x + e1y * e1y + e1z * e1z, e2x * e2x + e2y * e2y + e2z * e2z));
            double slack = size > 0 ? tolerance / size : 0;
            //
            if (u < -slack || v < -slack || u + v > 1 + 2 * slack) return false;
            //
            distance = (e2x * qx + e2y * qy + e2z * qz) * inverseDeterminant;
            return true;
        }
    }
}
