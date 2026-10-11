// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// The grading of the mesh sizes the way Netgen grades them: no size larger than a size of the curve mesh nearby plus
    /// the grading times the distance to it. gmsh asks for the size of every curve, surface and volume it meshes, so the
    /// curves are graded as well as what lies between them: a long edge between two fine ones is no longer left at the
    /// maximum size, with the surface going from the fine size to the maximum across the gap between them.
    ///
    /// The sizes are those of the curve mesh gmsh makes on its own, before the grading. They are kept in a k-d tree
    /// whose every node carries the smallest size beneath it, and a node is passed over once that size plus the grading
    /// times the distance to its box cannot be smaller than the size found so far.
    /// </summary>
    public class GmshSizeGrading
    {
        // Variables
        private const int _leafSize = 8;
        private readonly double _grading;
        private readonly double[] _coor;
        private readonly double[] _sizes;
        private readonly int[] _pointIds;
        private readonly List<double[]> _nodeBoxes = new List<double[]>();
        private readonly List<double> _nodeMinSizes = new List<double>();
        private readonly List<int> _nodeStarts = new List<int>();
        private readonly List<int> _nodeCounts = new List<int>();
        private readonly List<int> _nodeChildren = new List<int>();


        // Properties
        public int NumOfSizes { get { return _sizes.Length; } }


        // Constructors
        private GmshSizeGrading(double grading, double[] coor, double[] sizes)
        {
            _grading = grading;
            _coor = coor;
            _sizes = sizes;
            _pointIds = Enumerable.Range(0, sizes.Length).ToArray();
            if (sizes.Length > 0) Build(0, sizes.Length, 0);
        }


        // Methods
        /// <summary>
        /// The sizes of the curve mesh there is: one at the middle of every line element, as long as the element. Elements
        /// as long as the maximum size grade nothing and are left out.
        /// </summary>
        public static GmshSizeGrading FromCurveMesh(double grading, double maxSize)
        {
            List<double> coor = new List<double>();
            List<double> sizes = new List<double>();
            Tuple<int, int>[] curveDimTags;
            Gmsh.Model.GetEntities(out curveDimTags, 1);
            foreach (var curveDimTag in curveDimTags)
            {
                int[] elementTypes;
                IntPtr[][] elementTags;
                IntPtr[][] elementNodeTags;
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out elementNodeTags, 1, curveDimTag.Item2);
                IntPtr[] nodeTags;
                double[] nodeCoor;
                Gmsh.Model.Mesh.GetNodes(out nodeTags, out nodeCoor, 1, curveDimTag.Item2, true, false);
                Dictionary<IntPtr, int> nodeTagIndex = new Dictionary<IntPtr, int>();
                for (int i = 0; i < nodeTags.Length; i++) nodeTagIndex[nodeTags[i]] = i;
                //
                for (int t = 0; t < elementTypes.Length; t++)
                {
                    if (elementTags[t].Length == 0) continue;
                    int numOfNodes = elementNodeTags[t].Length / elementTags[t].Length;
                    for (int e = 0; e < elementTags[t].Length; e++)
                    {
                        int i1;
                        int i2;
                        if (!nodeTagIndex.TryGetValue(elementNodeTags[t][e * numOfNodes], out i1) ||
                            !nodeTagIndex.TryGetValue(elementNodeTags[t][e * numOfNodes + 1], out i2)) continue;
                        double dx = nodeCoor[3 * i2] - nodeCoor[3 * i1];
                        double dy = nodeCoor[3 * i2 + 1] - nodeCoor[3 * i1 + 1];
                        double dz = nodeCoor[3 * i2 + 2] - nodeCoor[3 * i1 + 2];
                        double size = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                        if (size <= 0 || size >= maxSize) continue;
                        //
                        coor.Add(nodeCoor[3 * i1] + 0.5 * dx);
                        coor.Add(nodeCoor[3 * i1 + 1] + 0.5 * dy);
                        coor.Add(nodeCoor[3 * i1 + 2] + 0.5 * dz);
                        sizes.Add(size);
                    }
                }
            }
            return new GmshSizeGrading(grading, coor.ToArray(), sizes.ToArray());
        }
        /// <summary>
        /// The size gmsh asks for at a point: the size it computed itself, unless a size of the curve mesh plus the
        /// grading times the distance to it is smaller.
        /// </summary>
        public double GetSize(int dim, int tag, double x, double y, double z, double lc)
        {
            if (_sizes.Length == 0) return lc;
            return GetSize(0, x, y, z, lc);
        }
        private double GetSize(int node, double x, double y, double z, double best)
        {
            if (GetLowerBound(node, x, y, z) >= best) return best;
            //
            int child = _nodeChildren[node];
            if (child < 0)
            {
                int end = _nodeStarts[node] + _nodeCounts[node];
                for (int i = _nodeStarts[node]; i < end; i++)
                {
                    int id = _pointIds[i];
                    double dx = _coor[3 * id] - x;
                    double dy = _coor[3 * id + 1] - y;
                    double dz = _coor[3 * id + 2] - z;
                    double size = _sizes[id] + _grading * Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (size < best) best = size;
                }
                return best;
            }
            // The nearer child first, so that the farther one is more often passed over
            int first = child;
            int second = child + 1;
            if (GetLowerBound(second, x, y, z) < GetLowerBound(first, x, y, z))
            {
                first = child + 1;
                second = child;
            }
            best = GetSize(first, x, y, z, best);
            return GetSize(second, x, y, z, best);
        }
        private double GetLowerBound(int node, double x, double y, double z)
        {
            double[] box = _nodeBoxes[node];
            double dx = Math.Max(0, Math.Max(box[0] - x, x - box[3]));
            double dy = Math.Max(0, Math.Max(box[1] - y, y - box[4]));
            double dz = Math.Max(0, Math.Max(box[2] - z, z - box[5]));
            return _nodeMinSizes[node] + _grading * Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        /// <summary>
        /// Builds the node of the points from start on, split at the median of their widest direction, and returns its
        /// index. The two children of a node follow each other.
        /// </summary>
        private int Build(int start, int count, int node)
        {
            double[] box = new double[] { double.MaxValue, double.MaxValue, double.MaxValue,
                                          double.MinValue, double.MinValue, double.MinValue };
            double minSize = double.MaxValue;
            for (int i = start; i < start + count; i++)
            {
                int id = _pointIds[i];
                for (int k = 0; k < 3; k++)
                {
                    box[k] = Math.Min(box[k], _coor[3 * id + k]);
                    box[k + 3] = Math.Max(box[k + 3], _coor[3 * id + k]);
                }
                minSize = Math.Min(minSize, _sizes[id]);
            }
            if (node == _nodeBoxes.Count)
            {
                _nodeBoxes.Add(box);
                _nodeMinSizes.Add(minSize);
                _nodeStarts.Add(start);
                _nodeCounts.Add(count);
                _nodeChildren.Add(-1);
            }
            else
            {
                _nodeBoxes[node] = box;
                _nodeMinSizes[node] = minSize;
                _nodeStarts[node] = start;
                _nodeCounts[node] = count;
            }
            if (count <= _leafSize) return node;
            //
            int axis = 0;
            for (int k = 1; k < 3; k++)
            {
                if (box[k + 3] - box[k] > box[axis + 3] - box[axis]) axis = k;
            }
            Array.Sort(_pointIds, start, count,
                       Comparer<int>.Create((a, b) => _coor[3 * a + axis].CompareTo(_coor[3 * b + axis])));
            int half = count / 2;
            int child = _nodeBoxes.Count;
            // Both children are reserved before either is built, so that they follow each other
            for (int i = 0; i < 2; i++)
            {
                _nodeBoxes.Add(null);
                _nodeMinSizes.Add(0);
                _nodeStarts.Add(0);
                _nodeCounts.Add(0);
                _nodeChildren.Add(-1);
            }
            _nodeChildren[node] = child;
            Build(start, half, child);
            Build(start + half, count - half, child + 1);
            return node;
        }
    }
}
