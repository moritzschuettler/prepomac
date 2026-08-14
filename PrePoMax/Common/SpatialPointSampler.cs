// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax
{
    internal static class SpatialPointSampler
    {
        private class Cluster
        {
            public int Start;
            public int Count;
            public int Order;
            public double DirectionX;
            public double DirectionY;
            public double DirectionZ;
            public double SplitValue;
            public double Priority;
        }

        private class ClusterMaxHeap
        {
            private readonly List<Cluster> _items = new List<Cluster>();

            public int Count { get { return _items.Count; } }
            public Cluster First { get { return _items[0]; } }
            public IList<Cluster> Items { get { return _items; } }

            public void Add(Cluster cluster)
            {
                int index = _items.Count;
                _items.Add(cluster);
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (!HasHigherPriority(_items[index], _items[parent])) break;
                    Swap(index, parent);
                    index = parent;
                }
            }

            public Cluster RemoveFirst()
            {
                Cluster first = _items[0];
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);
                if (_items.Count == 0) return first;
                //
                int index = 0;
                while (true)
                {
                    int left = 2 * index + 1;
                    if (left >= _items.Count) break;
                    int right = left + 1;
                    int child = right < _items.Count && HasHigherPriority(_items[right], _items[left])
                        ? right : left;
                    if (!HasHigherPriority(_items[child], _items[index])) break;
                    Swap(index, child);
                    index = child;
                }
                return first;
            }

            private static bool HasHigherPriority(Cluster first, Cluster second)
            {
                if (first.Priority != second.Priority) return first.Priority > second.Priority;
                if (first.Count != second.Count) return first.Count > second.Count;
                return first.Order < second.Order;
            }

            private void Swap(int first, int second)
            {
                Cluster tmp = _items[first];
                _items[first] = _items[second];
                _items[second] = tmp;
            }
        }

        // Select one representative from each occupied cell of an axis-aligned grid.
        // This legacy sampler is retained so existing symbol plots remain visually unchanged.
        public static int[] GetSpatiallyEquallyDistributedCoor(double[][] coordinates, int maxDivisions,
                                                               bool[] visible)
        {
            if (coordinates == null || coordinates.Length == 0) return null;
            //
            BoundingBox box = new BoundingBox();
            box.IncludeCoors(coordinates);
            //
            double max = Math.Max(box.MaxX - box.MinX, box.MaxY - box.MinY);
            max = Math.Max(max, box.MaxZ - box.MinZ);
            double maxDelta = max / maxDivisions;
            if (maxDelta < 1) maxDelta = 1;
            //
            int[] xyz = new int[3];
            xyz[0] = GetGridDivisionCount(box.MaxX - box.MinX, maxDelta);
            xyz[1] = GetGridDivisionCount(box.MaxY - box.MinY, maxDelta);
            xyz[2] = GetGridDivisionCount(box.MaxZ - box.MinZ, maxDelta);
            if (maxDivisions == 1)
            {
                xyz[0] = 1;
                xyz[1] = 1;
                xyz[2] = 1;
            }
            //
            return GetSpatialGridSampleIndices(coordinates, box, xyz, visible);
        }

        // Split the spatially largest occupied cluster until all clusters are smaller than
        // the requested fraction of the complete occupied span. Unlike a point percentage,
        // maxDivisions therefore produces a mesh-density-independent visual resolution.
        public static int[] GetSampleIndicesBySpatialResolution(double[][] coordinates, int maxDivisions,
                                                                 bool[] visible)
        {
            if (coordinates == null || coordinates.Length == 0 || maxDivisions <= 0) return new int[0];
            //
            int[] indices = GetVisibleIndices(coordinates.Length, visible);
            if (indices.Length == 0) return new int[0];
            //
            int nextOrder = 0;
            Cluster root = CreateCluster(coordinates, indices, 0, indices.Length, nextOrder++);
            ClusterMaxHeap clusters = new ClusterMaxHeap();
            clusters.Add(root);
            double maximumClusterSpan = root.Priority / maxDivisions;
            while (clusters.First.Count > 1 && clusters.First.Priority > maximumClusterSpan)
            {
                Cluster parent = clusters.RemoveFirst();
                SplitCluster(coordinates, indices, parent, out int leftCount);
                clusters.Add(CreateCluster(coordinates, indices, parent.Start, leftCount, nextOrder++));
                clusters.Add(CreateCluster(coordinates, indices, parent.Start + leftCount,
                                           parent.Count - leftCount, nextOrder++));
            }
            //
            int[] selectedIndices = new int[clusters.Count];
            for (int i = 0; i < clusters.Items.Count; i++)
                selectedIndices[i] = GetRepresentativeIndex(coordinates, indices, clusters.Items[i]);
            Array.Sort(selectedIndices);
            return selectedIndices;
        }
        public static int[] GetSampleIndicesByCount(double[][] coordinates, int targetCount, int[] protectedIndices)
        {
            if (coordinates == null || coordinates.Length == 0 || targetCount <= 0) return new int[0];
            //
            int nodeCount = coordinates.Length;
            List<int> protectedIds = GetValidUniqueIndices(protectedIndices, nodeCount);
            targetCount = Math.Min(nodeCount, Math.Max(targetCount, protectedIds.Count));
            if (targetCount >= nodeCount)
            {
                int[] allIndices = new int[nodeCount];
                for (int i = 0; i < nodeCount; i++) allIndices[i] = i;
                return allIndices;
            }
            // Clusters share one index array, avoiding a list allocation for every branch.
            int[] indices = new int[nodeCount];
            for (int i = 0; i < nodeCount; i++) indices[i] = i;
            int nextOrder = 0;
            ClusterMaxHeap clusters = new ClusterMaxHeap();
            clusters.Add(CreateCluster(coordinates, indices, 0, nodeCount, nextOrder++));
            while (clusters.Count < targetCount)
            {
                Cluster parent = clusters.RemoveFirst();
                SplitCluster(coordinates, indices, parent, out int leftCount);
                clusters.Add(CreateCluster(coordinates, indices, parent.Start, leftCount, nextOrder++));
                clusters.Add(CreateCluster(coordinates, indices, parent.Start + leftCount,
                                           parent.Count - leftCount, nextOrder++));
            }
            //
            int[] selectedIndices = new int[targetCount];
            for (int i = 0; i < clusters.Items.Count; i++)
                selectedIndices[i] = GetRepresentativeIndex(coordinates, indices, clusters.Items[i]);
            IncludeProtectedIndices(coordinates, selectedIndices, protectedIds);
            Array.Sort(selectedIndices);
            return selectedIndices;
        }

        private static Cluster CreateCluster(double[][] coordinates, int[] indices, int start, int count, int order)
        {
            Cluster cluster = new Cluster();
            cluster.Start = start;
            cluster.Count = count;
            cluster.Order = order;
            if (count <= 1)
            {
                cluster.Priority = -1;
                return cluster;
            }
            //
            GetMean(coordinates, indices, start, count, out double meanX, out double meanY, out double meanZ);
            double xx = 0;
            double xy = 0;
            double xz = 0;
            double yy = 0;
            double yz = 0;
            double zz = 0;
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = -double.MaxValue;
            double maxY = -double.MaxValue;
            double maxZ = -double.MaxValue;
            for (int i = start; i < start + count; i++)
            {
                double[] coordinate = coordinates[indices[i]];
                double x = GetCoordinate(coordinate, 0);
                double y = GetCoordinate(coordinate, 1);
                double z = GetCoordinate(coordinate, 2);
                double dx = x - meanX;
                double dy = y - meanY;
                double dz = z - meanZ;
                xx += dx * dx;
                xy += dx * dy;
                xz += dx * dz;
                yy += dy * dy;
                yz += dy * dz;
                zz += dz * dz;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (z < minZ) minZ = z;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                if (z > maxZ) maxZ = z;
            }
            GetPrincipalDirection(xx, xy, xz, yy, yz, zz,
                                  maxX - minX, maxY - minY, maxZ - minZ,
                                  out cluster.DirectionX, out cluster.DirectionY, out cluster.DirectionZ);
            // Use the occupied midpoint so refined mesh regions do not receive extra samples.
            double minProjection = double.MaxValue;
            double maxProjection = -double.MaxValue;
            for (int i = start; i < start + count; i++)
            {
                double projection = GetProjection(coordinates[indices[i]], cluster);
                if (projection < minProjection) minProjection = projection;
                if (projection > maxProjection) maxProjection = projection;
            }
            cluster.SplitValue = 0.5 * (minProjection + maxProjection);
            cluster.Priority = maxProjection - minProjection;
            if (!IsFinite(cluster.Priority) || cluster.Priority < 0) cluster.Priority = 0;
            return cluster;
        }

        private static void SplitCluster(double[][] coordinates, int[] indices, Cluster cluster, out int leftCount)
        {
            int left = cluster.Start;
            int right = cluster.Start + cluster.Count - 1;
            while (left <= right)
            {
                while (left <= right && GetProjection(coordinates[indices[left]], cluster) < cluster.SplitValue)
                    left++;
                while (left <= right && GetProjection(coordinates[indices[right]], cluster) >= cluster.SplitValue)
                    right--;
                if (left < right)
                {
                    int tmp = indices[left];
                    indices[left] = indices[right];
                    indices[right] = tmp;
                    left++;
                    right--;
                }
            }
            leftCount = left - cluster.Start;
            // Coincident coordinates are split by their deterministic existing order.
            if (leftCount <= 0 || leftCount >= cluster.Count) leftCount = cluster.Count / 2;
        }

        private static int GetRepresentativeIndex(double[][] coordinates, int[] indices, Cluster cluster)
        {
            GetMean(coordinates, indices, cluster.Start, cluster.Count,
                    out double meanX, out double meanY, out double meanZ);
            int bestIndex = indices[cluster.Start];
            double bestDistance = double.MaxValue;
            for (int i = cluster.Start; i < cluster.Start + cluster.Count; i++)
            {
                int index = indices[i];
                double distance = GetDistanceSquared(coordinates[index], meanX, meanY, meanZ);
                if (distance < bestDistance || (distance == bestDistance && index < bestIndex))
                {
                    bestIndex = index;
                    bestDistance = distance;
                }
            }
            return bestIndex;
        }

        private static void IncludeProtectedIndices(double[][] coordinates, int[] selectedIndices,
                                                     List<int> protectedIndices)
        {
            HashSet<int> selected = new HashSet<int>(selectedIndices);
            HashSet<int> protectedSet = new HashSet<int>(protectedIndices);
            foreach (int protectedIndex in protectedIndices)
            {
                if (selected.Contains(protectedIndex)) continue;
                // Replace the closest non-protected sample to minimize the coverage change.
                int replaceAt = -1;
                double nearestDistance = double.MaxValue;
                for (int i = 0; i < selectedIndices.Length; i++)
                {
                    int candidate = selectedIndices[i];
                    if (protectedSet.Contains(candidate)) continue;
                    double distance = GetDistanceSquared(coordinates[candidate], coordinates[protectedIndex]);
                    if (distance < nearestDistance ||
                        (distance == nearestDistance && (replaceAt == -1 || candidate < selectedIndices[replaceAt])))
                    {
                        replaceAt = i;
                        nearestDistance = distance;
                    }
                }
                if (replaceAt == -1) continue;
                selected.Remove(selectedIndices[replaceAt]);
                selectedIndices[replaceAt] = protectedIndex;
                selected.Add(protectedIndex);
            }
        }

        private static int GetGridDivisionCount(double length, double maxDelta)
        {
            double divisionCount = length / maxDelta;
            if (divisionCount < 1E-2) return 1;
            else if (divisionCount < 2) return 2;
            else return (int)divisionCount;
        }

        private static int[] GetSpatialGridSampleIndices(double[][] coordinates, BoundingBox box, int[] divisions,
                                                         bool[] visible)
        {
            GetSpatialGridParameters(box, divisions, out int nX, out int nY, out int nZ,
                                     out double minX, out double minY, out double minZ,
                                     out double deltaX, out double deltaY, out double deltaZ);
            bool useVisibility = visible != null && visible.Length > 0;
            Dictionary<long, int> closestIds = new Dictionary<long, int>();
            Dictionary<long, double> closestDistances = new Dictionary<long, double>();
            for (int i = 0; i < coordinates.Length; i++)
            {
                if (useVisibility && (i >= visible.Length || !visible[i])) continue;
                //
                long cellId = GetSpatialGridCellId(coordinates[i], nX, nY, nZ, minX, minY, minZ,
                                                   deltaX, deltaY, deltaZ,
                                                   out int idX, out int idY, out int idZ);
                double centerX = minX + (idX + 0.5) * deltaX;
                double centerY = minY + (idY + 0.5) * deltaY;
                double centerZ = minZ + (idZ + 0.5) * deltaZ;
                double dx = centerX - coordinates[i][0];
                double dy = centerY - coordinates[i][1];
                double dz = centerZ - coordinates[i][2];
                double distance = dx * dx + dy * dy + dz * dz;
                if (!closestDistances.TryGetValue(cellId, out double closestDistance) ||
                    distance < closestDistance)
                {
                    closestIds[cellId] = i;
                    closestDistances[cellId] = distance;
                }
            }
            //
            long[] cellIds = closestIds.Keys.ToArray();
            Array.Sort(cellIds);
            int[] centerIds = new int[cellIds.Length];
            for (int i = 0; i < cellIds.Length; i++) centerIds[i] = closestIds[cellIds[i]];
            return centerIds;
        }

        private static void GetSpatialGridParameters(BoundingBox box, int[] divisions,
                                                     out int nX, out int nY, out int nZ,
                                                     out double minX, out double minY, out double minZ,
                                                     out double deltaX, out double deltaY, out double deltaZ)
        {
            nX = Math.Max(1, divisions[0]);
            nY = Math.Max(1, divisions[1]);
            nZ = Math.Max(1, divisions[2]);
            deltaX = box.MaxX != box.MinX ? ((box.MaxX - box.MinX) / nX) * 1.01 : 1;
            deltaY = box.MaxY != box.MinY ? ((box.MaxY - box.MinY) / nY) * 1.01 : 1;
            deltaZ = box.MaxZ != box.MinZ ? ((box.MaxZ - box.MinZ) / nZ) * 1.01 : 1;
            minX = box.MinX - deltaX * 0.005;
            minY = box.MinY - deltaY * 0.005;
            minZ = box.MinZ - deltaZ * 0.005;
        }

        private static long GetSpatialGridCellId(double[] coordinate, int nX, int nY, int nZ,
                                                 double minX, double minY, double minZ,
                                                 double deltaX, double deltaY, double deltaZ,
                                                 out int idX, out int idY, out int idZ)
        {
            idX = Math.Max(0, Math.Min(nX - 1, (int)Math.Floor((coordinate[0] - minX) / deltaX)));
            idY = Math.Max(0, Math.Min(nY - 1, (int)Math.Floor((coordinate[1] - minY) / deltaY)));
            idZ = Math.Max(0, Math.Min(nZ - 1, (int)Math.Floor((coordinate[2] - minZ) / deltaZ)));
            return ((long)idX * nY + idY) * nZ + idZ;
        }

        private static int[] GetVisibleIndices(int count, bool[] visible)
        {
            bool useVisibility = visible != null && visible.Length > 0;
            if (!useVisibility)
            {
                int[] allIndices = new int[count];
                for (int i = 0; i < count; i++) allIndices[i] = i;
                return allIndices;
            }
            //
            List<int> visibleIndices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (i < visible.Length && visible[i]) visibleIndices.Add(i);
            }
            return visibleIndices.ToArray();
        }
        private static List<int> GetValidUniqueIndices(int[] indices, int count)
        {
            List<int> valid = new List<int>();
            if (indices == null) return valid;
            HashSet<int> added = new HashSet<int>();
            foreach (int index in indices)
            {
                if (index >= 0 && index < count && added.Add(index)) valid.Add(index);
            }
            return valid;
        }

        private static void GetMean(double[][] coordinates, int[] indices, int start, int count,
                                    out double meanX, out double meanY, out double meanZ)
        {
            meanX = 0;
            meanY = 0;
            meanZ = 0;
            for (int i = start; i < start + count; i++)
            {
                double[] coordinate = coordinates[indices[i]];
                meanX += GetCoordinate(coordinate, 0);
                meanY += GetCoordinate(coordinate, 1);
                meanZ += GetCoordinate(coordinate, 2);
            }
            meanX /= count;
            meanY /= count;
            meanZ /= count;
        }

        private static void GetPrincipalDirection(double xx, double xy, double xz, double yy, double yz, double zz,
                                                  double lengthX, double lengthY, double lengthZ,
                                                  out double vx, out double vy, out double vz)
        {
            vx = 0;
            vy = 0;
            vz = 0;
            if (xx >= yy && xx >= zz) vx = 1;
            else if (yy >= zz) vy = 1;
            else vz = 1;
            for (int i = 0; i < 8; i++)
            {
                double nx = xx * vx + xy * vy + xz * vz;
                double ny = xy * vx + yy * vy + yz * vz;
                double nz = xz * vx + yz * vy + zz * vz;
                double norm = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (!IsFinite(norm) || norm <= 1E-30) break;
                vx = nx / norm;
                vy = ny / norm;
                vz = nz / norm;
            }
            double directionNorm = Math.Sqrt(vx * vx + vy * vy + vz * vz);
            if (!IsFinite(directionNorm) || directionNorm <= 1E-30)
            {
                vx = 0;
                vy = 0;
                vz = 0;
                if (lengthX >= lengthY && lengthX >= lengthZ) vx = 1;
                else if (lengthY >= lengthZ) vy = 1;
                else vz = 1;
            }
        }

        private static double GetProjection(double[] coordinate, Cluster cluster)
        {
            return GetCoordinate(coordinate, 0) * cluster.DirectionX +
                   GetCoordinate(coordinate, 1) * cluster.DirectionY +
                   GetCoordinate(coordinate, 2) * cluster.DirectionZ;
        }

        private static double GetDistanceSquared(double[] first, double[] second)
        {
            return GetDistanceSquared(first, GetCoordinate(second, 0), GetCoordinate(second, 1),
                                      GetCoordinate(second, 2));
        }

        private static double GetDistanceSquared(double[] coordinate, double x, double y, double z)
        {
            double dx = GetCoordinate(coordinate, 0) - x;
            double dy = GetCoordinate(coordinate, 1) - y;
            double dz = GetCoordinate(coordinate, 2) - z;
            return dx * dx + dy * dy + dz * dz;
        }

        private static double GetCoordinate(double[] coordinate, int component)
        {
            if (coordinate == null || coordinate.Length <= component || !IsFinite(coordinate[component])) return 0;
            return coordinate[component];
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}