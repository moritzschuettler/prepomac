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

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Where a coarse mesh meets a narrow part of a source surface, the recombination may leave a quadrilateral with a
    /// corner of 180 degrees or more. Every element swept from it is inverted, and so is the pyramid the tetrahedra of
    /// a neighbouring cell stand on its copy. Such a quadrilateral is split into two triangles along the diagonal from
    /// that corner, which sweep into wedges; a fine mesh has none and is left as it is.
    /// </summary>
    public static class SweepSourceRepair
    {
        // Methods
        public static void SplitNonConvexQuadrilaterals(IEnumerable<int> surfaceIds, Action<string> writeOutput)
        {
            foreach (var surfaceId in surfaceIds)
            {
                int numSplit = SplitNonConvexQuadrilaterals(surfaceId);
                if (numSplit > 0)
                {
                    writeOutput?.Invoke("Sweep source surface " + surfaceId + ": " + numSplit + " non-convex " +
                                        "quadrilateral/s split into triangles.");
                }
            }
        }
        /// <summary>
        /// The number of quadrilaterals with a corner of 180 degrees or more on every given surface that has any.
        /// </summary>
        public static Dictionary<int, int> CountNonConvexQuadrilaterals(IEnumerable<int> surfaceIds)
        {
            Dictionary<int, int> surfaceIdCount = new Dictionary<int, int>();
            foreach (var surfaceId in surfaceIds)
            {
                int[] elementTypes;
                IntPtr[][] elementTags;
                IntPtr[][] nodeTags;
                Dictionary<IntPtr, double[]> tagCoor;
                int quadIndex = GetSurfaceMesh(surfaceId, out elementTypes, out elementTags, out nodeTags, out tagCoor);
                if (quadIndex < 0) continue;
                //
                int count = 0;
                double minTurn;
                for (int j = 0; j < elementTags[quadIndex].Length; j++)
                {
                    GetLeastTurningCorner(nodeTags[quadIndex].Skip(4 * j).Take(4).Select(tag => tagCoor[tag]).ToArray(),
                                          out minTurn);
                    if (minTurn <= 0) count++;
                }
                if (count > 0) surfaceIdCount.Add(surfaceId, count);
            }
            return surfaceIdCount;
        }
        private static int SplitNonConvexQuadrilaterals(int surfaceId)
        {
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            Dictionary<IntPtr, double[]> tagCoor;
            int quadIndex = GetSurfaceMesh(surfaceId, out elementTypes, out elementTags, out nodeTags, out tagCoor);
            if (quadIndex < 0) return 0;
            //
            List<IntPtr[]> triangles = new List<IntPtr[]>();
            List<IntPtr[]> quadrilaterals = new List<IntPtr[]>();
            int triangleIndex = Array.IndexOf(elementTypes, (int)GmshElementType.LinearTriangle);
            if (triangleIndex >= 0)
            {
                for (int j = 0; j < elementTags[triangleIndex].Length; j++)
                    triangles.Add(nodeTags[triangleIndex].Skip(3 * j).Take(3).ToArray());
            }
            int numSplit = 0;
            for (int j = 0; j < elementTags[quadIndex].Length; j++)
            {
                IntPtr[] quad = nodeTags[quadIndex].Skip(4 * j).Take(4).ToArray();
                int corner;
                if (TryGetSplitCorner(quad.Select(tag => tagCoor[tag]).ToArray(), out corner))
                {
                    IntPtr a = quad[corner];
                    IntPtr b = quad[(corner + 1) % 4];
                    IntPtr c = quad[(corner + 2) % 4];
                    IntPtr d = quad[(corner + 3) % 4];
                    triangles.Add(new IntPtr[] { a, b, c });
                    triangles.Add(new IntPtr[] { a, c, d });
                    numSplit++;
                }
                else quadrilaterals.Add(quad);
            }
            if (numSplit == 0) return 0;
            //
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            long elementTag = maxTag.ToInt64();
            List<int> newTypes = new List<int>();
            List<IntPtr[]> newTags = new List<IntPtr[]>();
            List<IntPtr[]> newNodeTags = new List<IntPtr[]>();
            foreach (var entry in new[] { new Tuple<int, List<IntPtr[]>>((int)GmshElementType.LinearTriangle, triangles),
                                          new Tuple<int, List<IntPtr[]>>((int)GmshElementType.LinearQuadrilateral,
                                                                         quadrilaterals) })
            {
                if (entry.Item2.Count == 0) continue;
                newTypes.Add(entry.Item1);
                newTags.Add(entry.Item2.Select(element => new IntPtr(++elementTag)).ToArray());
                newNodeTags.Add(entry.Item2.SelectMany(element => element).ToArray());
            }
            Gmsh.Model.Mesh.RemoveElements(2, surfaceId);
            Gmsh.Model.Mesh.AddElements(2, surfaceId, newTypes.ToArray(), newTags.ToArray(), newNodeTags.ToArray());
            return numSplit;
        }
        /// <summary>
        /// The corner a quadrilateral is split from: the one turning least, when it does not turn the way the
        /// quadrilateral winds. A quadrilateral whose two triangles from that corner would not both wind that way
        /// crosses itself, and splitting it would not help.
        /// </summary>
        private static bool TryGetSplitCorner(double[][] points, out int corner)
        {
            double minTurn;
            corner = GetLeastTurningCorner(points, out minTurn);
            if (minTurn > 0) return false;
            //
            double[] normal = GetNormal(points);
            double[] a = points[corner];
            double[] b = points[(corner + 1) % 4];
            double[] c = points[(corner + 2) % 4];
            double[] d = points[(corner + 3) % 4];
            return Dot(Cross(Subtract(b, a), Subtract(c, a)), normal) > 0 &&
                   Dot(Cross(Subtract(c, a), Subtract(d, a)), normal) > 0;
        }
        /// <summary>
        /// The elements of a surface by type, the coordinates of its nodes by tag and the index of its
        /// quadrilaterals among the types; -1 when it holds none, or elements that are neither triangles nor
        /// quadrilaterals.
        /// </summary>
        private static int GetSurfaceMesh(int surfaceId, out int[] elementTypes, out IntPtr[][] elementTags,
                                          out IntPtr[][] nodeTags, out Dictionary<IntPtr, double[]> tagCoor)
        {
            tagCoor = null;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
            int quadIndex = Array.IndexOf(elementTypes, (int)GmshElementType.LinearQuadrilateral);
            if (quadIndex < 0) return -1;
            if (elementTypes.Any(type => type != (int)GmshElementType.LinearQuadrilateral &&
                                         type != (int)GmshElementType.LinearTriangle)) return -1;
            //
            IntPtr[] tags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out tags, out coor, 2, surfaceId, true, false);
            tagCoor = new Dictionary<IntPtr, double[]>();
            for (int i = 0; i < tags.Length; i++)
                tagCoor[tags[i]] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
            return quadIndex;
        }
        /// <summary>
        /// The corner of a quadrilateral turning least the way it winds, measured against the normal its diagonals
        /// span: at a corner of 180 degrees or more the turn is zero or negative.
        /// </summary>
        private static int GetLeastTurningCorner(double[][] points, out double minTurn)
        {
            int corner = -1;
            double[] normal = GetNormal(points);
            minTurn = double.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                double[] incoming = Subtract(points[i], points[(i + 3) % 4]);
                double[] outgoing = Subtract(points[(i + 1) % 4], points[i]);
                double turn = Dot(Cross(incoming, outgoing), normal);
                if (turn < minTurn)
                {
                    minTurn = turn;
                    corner = i;
                }
            }
            return corner;
        }
        private static double[] GetNormal(double[][] points)
        {
            return Cross(Subtract(points[2], points[0]), Subtract(points[3], points[1]));
        }
        private static double[] Subtract(double[] a, double[] b)
        {
            return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        }
        private static double[] Cross(double[] a, double[] b)
        {
            return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        }
        private static double Dot(double[] a, double[] b)
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }
    }
}
