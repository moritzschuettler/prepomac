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
    // Static 3-D k-d tree over a list of positions; FindNearest returns the index of the closest
    // position in the list that was used to build the tree
    public class KdTree3D
    {
        // Variables
        private List<double[]> _points;
        private int[] _indices;


        // Constructor
        public KdTree3D(List<double[]> points)
        {
            _points = points;
            _indices = new int[points.Count];
            for (int i = 0; i < points.Count; i++) _indices[i] = i;
            Build(_indices, 0, _indices.Length - 1, 0);
        }


        // Methods
        private void Build(int[] idx, int lo, int hi, int axis)
        {
            if (lo >= hi) return;
            int mid = (lo + hi) / 2;
            PartialSort(idx, lo, hi, mid, axis);
            Build(idx, lo, mid - 1, (axis + 1) % 3);
            Build(idx, mid + 1, hi, (axis + 1) % 3);
        }

        private void PartialSort(int[] idx, int lo, int hi, int k, int axis)
        {
            while (lo < hi)
            {
                int pivot = Partition(idx, lo, hi, axis);
                if (pivot == k) return;
                if (pivot < k) lo = pivot + 1;
                else hi = pivot - 1;
            }
        }

        private int Partition(int[] idx, int lo, int hi, int axis)
        {
            double pivot = _points[idx[hi]][axis];
            int i = lo;
            for (int j = lo; j < hi; j++)
            {
                if (_points[idx[j]][axis] <= pivot)
                {
                    int tmp = idx[i]; idx[i] = idx[j]; idx[j] = tmp;
                    i++;
                }
            }
            int t = idx[i]; idx[i] = idx[hi]; idx[hi] = t;
            return i;
        }

        public int FindNearest(double[] query)
        {
            if (_indices.Length == 0) return -1;
            int best = _indices[_indices.Length / 2];
            double bestDist = Dist2(query, _points[best]);
            Search(_indices, 0, _indices.Length - 1, 0, query, ref best, ref bestDist);
            return best;
        }

        private void Search(int[] idx, int lo, int hi, int axis, double[] q, ref int best, ref double bestDist)
        {
            if (lo > hi) return;
            int mid = (lo + hi) / 2;
            int pointIdx = idx[mid];
            double d = Dist2(q, _points[pointIdx]);
            if (d < bestDist) { bestDist = d; best = pointIdx; }
            //
            double diff = q[axis] - _points[pointIdx][axis];
            int near = diff <= 0 ? lo : mid + 1;
            int far = diff <= 0 ? mid + 1 : lo;
            int nearHi = diff <= 0 ? mid - 1 : hi;
            int farHi = diff <= 0 ? hi : mid - 1;
            //
            Search(idx, near, nearHi, (axis + 1) % 3, q, ref best, ref bestDist);
            if (diff * diff < bestDist)
                Search(idx, far, farHi, (axis + 1) % 3, q, ref best, ref bestDist);
        }

        private static double Dist2(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
