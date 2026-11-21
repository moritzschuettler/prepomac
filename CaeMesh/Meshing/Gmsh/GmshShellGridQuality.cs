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
using CaeMesh.Sweep;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// The grid the nodes on the four sides of a face give, by transfinite interpolation in the parameters of its
    /// surface as gmsh lays it, kept only when its worst corner in space is below MinScaledJacobian and worse than the
    /// corners of the face itself, which no mesh improves. Such a face is meshed free first, and TryRestore lays the
    /// grid over that mesh unless the free mesh has the better worst corner, so the face never ends worse than its
    /// grid and no other face is meshed twice.
    /// </summary>
    internal class GmshShellGridQuality
    {
        // Variables                                                                                                    
        internal const double MinScaledJacobian = 0.5;
        private const double ShapeMargin = 0.05;
        private readonly TransfiniteFace _face;
        private readonly bool _recombine;
        private readonly double _quadQuality;
        private readonly double _orientation;
        private int _width;
        private int _height;
        private double[][] _uv;
        private double[][] _xyz;
        private int[][] _elements;
        private double _quality;


        // Properties                                                                                                   
        public int FaceId { get { return _face.FaceId; } }
        public double LeanDegrees { get { return Math.Acos(Math.Max(-1, _quadQuality)) * 180 / Math.PI; } }


        // Constructors                                                                                                 
        private GmshShellGridQuality(TransfiniteFace face, bool recombine, double quadQuality, double orientation,
                                    int width, int height, double[][] uv, double[][] xyz, int[][] elements,
                                    double quality)
        {
            _face = face;
            _recombine = recombine;
            _quadQuality = quadQuality;
            _orientation = orientation;
            _width = width;
            _height = height;
            _uv = uv;
            _xyz = xyz;
            _elements = elements;
            _quality = quality;
        }


        // Methods                                                                                                      
        public static GmshShellGridQuality CreatePoorGrid(TransfiniteFace face, bool recombine)
        {
            GetSides(face, out _, out List<double[]>[] sides);
            double[][][] uv = sides.Select(side => GmshThreeBlockMesher.GetParametricCoordinates(face.FaceId, side))
                                   .ToArray();
            //
            int width = sides[0].Count;
            int height = sides[1].Count;
            if (sides[2].Count != width || sides[3].Count != height) return null;
            UnwrapParameters(face.FaceId, uv);
            double[] horizontal = GetGridFractions(sides[0], sides[2]);
            double[] vertical = GetGridFractions(sides[1], sides[3]);
            double[] parameters = new double[2 * width * height];
            for (int j = 0; j < height; j++)
            {
                double v = vertical[j];
                for (int i = 0; i < width; i++)
                {
                    double u = horizontal[i];
                    for (int d = 0; d < 2; d++)
                    {
                        parameters[2 * (j * width + i) + d] =
                            (1 - v) * uv[0][i][d] + u * uv[1][j][d] +
                            v * uv[2][width - 1 - i][d] + (1 - u) * uv[3][height - 1 - j][d] -
                            ((1 - u) * (1 - v) * uv[0][0][d] + u * (1 - v) * uv[1][0][d] +
                             u * v * uv[2][0][d] + (1 - u) * v * uv[3][0][d]);
                    }
                }
            }
            //
            double[][] gridUv = ToPoints(parameters, 2);
            SetBoundary(width, height, uv, gridUv);
            if (!GetGridGeometry(face.FaceId, gridUv, out double[][] xyz, out double[][] normals)) return null;
            SetBoundary(width, height, sides.Select(side => side.ToArray()).ToArray(), xyz);
            int[][] elements = GetElements(width, height, true);
            // The normal gmsh gives is turned on a reversed face, so the grid is judged the way it runs as a whole
            double orientation = GetOrientation(elements, xyz, normals);
            double quality = GetMinimum(elements, xyz, normals, orientation);
            // A corner of the face itself is as sharp in any mesh: only a grid worse than its own corners leans
            int[] cornerElements = new int[] { 0, width - 2, elements.Length - 1, (height - 2) * (width - 1) };
            double cornerQuality = 1;
            for (int c = 0; c < 4; c++)
                cornerQuality = Math.Min(cornerQuality,
                                         GmshQuadMesher.GetSpaceScaledJacobian(elements[cornerElements[c]], c, xyz,
                                                                               normals, orientation));
            if (!(quality < MinScaledJacobian) || !(quality < cornerQuality - ShapeMargin)) return null;
            //
            double quadQuality = quality;
            if (!recombine)
            {
                elements = GetElements(width, height, false);
                quality = GetMinimum(elements, xyz, normals, orientation);
            }
            return new GmshShellGridQuality(face, recombine, quadQuality, orientation, width, height, gridUv,
                                            xyz, elements, quality);
        }
        public bool TryRestore(bool requireQuads)
        {
            GetSides(_face, out List<IntPtr>[] sides, out List<double[]>[] sideXyz);
            int width = sides[0].Count;
            int height = sides[1].Count;
            if (sides[2].Count != width || sides[3].Count != height) return false;
            if ((width != _width || height != _height) && !Subdivide(width, height, sideXyz)) return false;
            //
            double freeQuality = GetFreeQuality(requireQuads, out double orientation);
            if (freeQuality > _quality) return false;
            //
            IntPtr[] tags = new IntPtr[_uv.Length];
            SetBoundary(width, height, sides.Select(side => side.ToArray()).ToArray(), tags);
            List<IntPtr> boundaryTags = new List<IntPtr>();
            List<double[]> innerUv = new List<double[]>();
            List<double[]> innerXyz = new List<double[]>();
            int[] ids = new int[tags.Length];
            for (int k = 0; k < tags.Length; k++)
            {
                if (tags[k] == IntPtr.Zero) continue;
                ids[k] = boundaryTags.Count;
                boundaryTags.Add(tags[k]);
            }
            for (int k = 0; k < tags.Length; k++)
            {
                if (tags[k] != IntPtr.Zero) continue;
                ids[k] = boundaryTags.Count + innerUv.Count;
                innerUv.Add(_uv[k]);
                innerXyz.Add(_xyz[k]);
            }
            int[][] elements = _elements.Select(element => element.Select(k => ids[k]).ToArray()).ToArray();
            GmshThreeBlockMesher.ReplaceSurfaceElements(FaceId, boundaryTags.ToArray(), elements, innerUv.ToArray(),
                                                         innerXyz.ToArray(), _orientation != orientation, true);
            return true;
        }
        private bool Subdivide(int width, int height, List<double[]>[] sideXyz)
        {
            if (width < _width || height < _height || (width - 1) % (_width - 1) != 0 ||
                (height - 1) % (_height - 1) != 0) return false;
            //
            int horizontal = (width - 1) / (_width - 1);
            int vertical = (height - 1) / (_height - 1);
            double[][] uv = new double[width * height][];
            for (int j = 0; j < height; j++)
            {
                int row = Math.Min(j / vertical, _height - 2);
                double v = (double)(j - row * vertical) / vertical;
                for (int i = 0; i < width; i++)
                {
                    int col = Math.Min(i / horizontal, _width - 2);
                    double u = (double)(i - col * horizontal) / horizontal;
                    int n = row * _width + col;
                    double[] point = new double[2];
                    for (int d = 0; d < 2; d++)
                    {
                        point[d] = (1 - v) * ((1 - u) * _uv[n][d] + u * _uv[n + 1][d]) +
                                   v * ((1 - u) * _uv[n + _width][d] + u * _uv[n + _width + 1][d]);
                    }
                    uv[j * width + i] = point;
                }
            }
            double[][][] boundaryUv = sideXyz
                .Select(side => GmshThreeBlockMesher.GetParametricCoordinates(FaceId, side)).ToArray();
            UnwrapParameters(FaceId, boundaryUv);
            SetBoundary(width, height, boundaryUv, uv);
            if (!GetGridGeometry(FaceId, uv, out double[][] xyz, out double[][] normals)) return false;
            SetBoundary(width, height, sideXyz.Select(side => side.ToArray()).ToArray(), xyz);
            _width = width;
            _height = height;
            _uv = uv;
            _xyz = xyz;
            _elements = GetElements(width, height, _recombine);
            _quality = GetMinimum(_elements, xyz, normals, _orientation);
            return true;
        }
        private double GetFreeQuality(bool requireQuads, out double orientation)
        {
            orientation = _orientation;
            Gmsh.Model.Mesh.GetElements(out int[] types, out IntPtr[][] elementTags, out IntPtr[][] elementNodes,
                                         2, FaceId);
            Gmsh.Model.Mesh.GetNodes(out IntPtr[] tags, out double[] xyz, 2, FaceId, true, false);
            if (xyz.Length != 3 * tags.Length || tags.Length == 0) return double.NegativeInfinity;
            Gmsh.Model.GetParametrization(2, FaceId, xyz, out double[] uv);
            if (uv.Length != 2 * tags.Length) return double.NegativeInfinity;
            //
            Dictionary<IntPtr, int> ids = new Dictionary<IntPtr, int>();
            for (int k = 0; k < tags.Length; k++) ids[tags[k]] = k;
            List<int[]> elements = new List<int[]>();
            bool hasTriangles = false;
            for (int t = 0; t < types.Length; t++)
            {
                int n = types[t] == (int)GmshElementType.LinearQuadrilateral ? 4 :
                        types[t] == (int)GmshElementType.LinearTriangle ? 3 : 0;
                if (n == 0 || elementNodes[t].Length != n * elementTags[t].Length) return double.NegativeInfinity;
                hasTriangles |= n == 3 && elementTags[t].Length > 0;
                for (int e = 0; e < elementNodes[t].Length; e += n)
                {
                    int[] element = new int[n];
                    for (int c = 0; c < n; c++)
                    {
                        if (!ids.TryGetValue(elementNodes[t][e + c], out element[c])) return double.NegativeInfinity;
                    }
                    elements.Add(element);
                }
            }
            if (elements.Count == 0) return double.NegativeInfinity;
            double[][] points = ToPoints(xyz, 3);
            double[][] normals = GmshQuadMesher.GetNormals(FaceId, ToPoints(uv, 2));
            double sum = 0;
            double min = 1;
            double max = -1;
            foreach (int[] element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                {
                    double corner = GmshQuadMesher.GetSpaceScaledJacobian(element, c, points, normals, 1);
                    sum += corner;
                    min = Math.Min(min, corner);
                    max = Math.Max(max, corner);
                }
            }
            orientation = sum < 0 ? -1 : 1;
            if (requireQuads && hasTriangles) return double.NegativeInfinity;
            return orientation < 0 ? -max : min;
        }
        private static double GetOrientation(IEnumerable<int[]> elements, double[][] xyz, double[][] normals)
        {
            double sum = 0;
            foreach (int[] element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                    sum += GmshQuadMesher.GetSpaceScaledJacobian(element, c, xyz, normals, 1);
            }
            return sum < 0 ? -1 : 1;
        }
        private static double GetMinimum(IEnumerable<int[]> elements, double[][] xyz, double[][] normals,
                                         double orientation)
        {
            double min = 1;
            foreach (int[] element in elements)
            {
                for (int c = 0; c < element.Length; c++)
                    min = Math.Min(min, GmshQuadMesher.GetSpaceScaledJacobian(element, c, xyz, normals, orientation));
            }
            return min;
        }
        private static bool GetGridGeometry(int surfaceId, double[][] uv, out double[][] xyz, out double[][] normals)
        {
            double[] parameters = uv.SelectMany(point => point).ToArray();
            Gmsh.Model.GetValue(2, surfaceId, parameters, out double[] flatXyz);
            Gmsh.Model.GetNormal(surfaceId, parameters, out double[] flatNormals);
            xyz = null;
            normals = null;
            if (flatXyz.Length != 3 * uv.Length || flatNormals.Length != flatXyz.Length) return false;
            xyz = ToPoints(flatXyz, 3);
            normals = ToPoints(flatNormals, 3);
            return true;
        }
        private static int[][] GetElements(int width, int height, bool quads)
        {
            int[][] elements = new int[(width - 1) * (height - 1) * (quads ? 1 : 2)][];
            int e = 0;
            for (int j = 0; j < height - 1; j++)
            {
                for (int i = 0; i < width - 1; i++)
                {
                    int a = j * width + i;
                    if (quads) elements[e++] = new int[] { a, a + 1, a + 1 + width, a + width };
                    else
                    {
                        elements[e++] = new int[] { a, a + 1, a + 1 + width };
                        elements[e++] = new int[] { a, a + 1 + width, a + width };
                    }
                }
            }
            return elements;
        }
        private static void GetSides(TransfiniteFace face, out List<IntPtr>[] tags, out List<double[]>[] xyz)
        {
            tags = new List<IntPtr>[4];
            xyz = new List<double[]>[4];
            for (int k = 0; k < 4; k++)
                GmshThreeBlockMesher.GetSideNodes(face.FaceId, face.CornerNodeIds[k],
                                                 face.CornerNodeIds[(k + 1) % 4], face.SideEdgeIds[k],
                                                 out tags[k], out xyz[k]);
        }
        private static void SetBoundary<T>(int width, int height, T[][] sides, T[] grid)
        {
            for (int i = 0; i < width; i++)
            {
                grid[i] = sides[0][i];
                grid[(height - 1) * width + i] = sides[2][width - 1 - i];
            }
            for (int j = 1; j < height - 1; j++)
            {
                grid[j * width] = sides[3][height - 1 - j];
                grid[j * width + width - 1] = sides[1][j];
            }
        }
        private static void UnwrapParameters(int surfaceId, double[][][] uv)
        {
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out double[] min, out double[] max);
            for (int d = 0; d < 2; d++)
            {
                double period = GmshPoleChart.GetPeriod(surfaceId, d, min, max);
                if (!(period > 0)) continue;
                double gap = period - (max[d] - min[d]);
                double reference = uv[0][0][d];
                foreach (double[][] side in uv)
                {
                    foreach (double[] point in side)
                    {
                        double start = gap > 1E-6 * period ? min[d] - gap / 2 : reference - period / 2;
                        point[d] = GmshPoleChart.Wrap(point[d], start, period);
                        reference = point[d];
                    }
                }
            }
        }
        private static double[] GetGridFractions(List<double[]> first, List<double[]> second)
        {
            int last = first.Count - 1;
            double[] fractions = new double[first.Count];
            for (int k = 1; k <= last; k++)
                fractions[k] = fractions[k - 1] + GetDistance(first[k], first[k - 1]) +
                               GetDistance(second[last - k], second[last - k + 1]);
            for (int k = 1; k < last; k++) fractions[k] /= fractions[last];
            fractions[last] = 1;
            return fractions;
        }
        private static double GetDistance(double[] a, double[] b)
        {
            double dx = a[0] - b[0];
            double dy = a[1] - b[1];
            double dz = a[2] - b[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        private static double[][] ToPoints(double[] flat, int dimension)
        {
            double[][] points = new double[flat.Length / dimension][];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new double[dimension];
                Array.Copy(flat, dimension * i, points[i], 0, dimension);
            }
            return points;
        }
    }
}
